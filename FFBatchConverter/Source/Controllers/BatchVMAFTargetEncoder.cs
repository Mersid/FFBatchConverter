using System.Diagnostics;
using BidirectionalMap;
using FFBatchConverter.Encoders;
using FFBatchConverter.Misc;
using FFBatchConverter.Models;
using FFBatchConverter.Tokens;

namespace FFBatchConverter.Controllers;

public class BatchVMAFTargetEncoder
{
    private int _concurrency;
    public int Concurrency
    {
        get => _concurrency;
        set
        {
            _concurrency = value;
            ProcessActions();
        }
    }

    /// <summary>
    /// Output directory, relative to the input file.
    /// </summary>
    public string OutputSubdirectory { get; set; } = string.Empty;

    /// <summary>
    /// File extension to use for the output files.
    /// </summary>
    public string Extension { get; set; } = string.Empty;

    /// <summary>
    /// Set to true to use H.265 instead of H.264.
    /// </summary>
    public bool H265 { get; set; }

    /// <summary>
    /// The minimum VMAF value to target.
    /// We aim for the CRF value with that puts the video above this value, such that the next CRF up would put it below this value.
    /// </summary>
    public double TargetVMAF { get; set; }

    public required string FFmpegPath { get; set; } = string.Empty;
    public required string FFprobePath { get; set; } = string.Empty;
    public required string TempDirectory { get; set; } = string.Empty;

    /// <summary>
    /// FFmpeg arguments.
    /// </summary>
    public string Arguments { get; set; } = string.Empty;

    private BiMap<VMAFTargetEncoderToken, VMAFTargetVideoEncoder> Encoders { get; } = [];
    private bool IsEncoding { get; set; }

    private readonly object _lock = new object();

    /// <summary>
    /// Event that is raised when there's an update to the status of any encoder.
    /// There is no guarantee which thread this event will be raised on!
    /// If using this with UI, caller is responsible for marshalling to the UI thread.
    /// </summary>
    public event EventHandler<InformationUpdateEventArgs<VMAFTargetEncoderStatusReport>>? InformationUpdate;

    public void StartEncoding()
    {
        IsEncoding = true;
        ProcessActions();
    }

    public void StopEncoding()
    {
        IsEncoding = false;
        ProcessActions();
    }

    /// <summary>
    /// Takes a list of string paths, and for each, if it's a file, it will add it to the encoding list.
    /// If it's a directory, this will recursively search for all files within it and add them all to the encoding list.
    /// No action will be taken if the path is invalid.
    /// </summary>
    /// <param name="path"></param>
    public void AddEntries(IEnumerable<string> path)
    {
        List<string> files = [];
        foreach (string p in path)
        {
            files.AddRange(Helpers.GetFilesRecursive(p));
        }

        List<VMAFTargetVideoEncoder> encoders = files
            .AsParallel()
            .AsOrdered()
            .WithDegreeOfParallelism(Environment.ProcessorCount)
            .Select(t => new VMAFTargetVideoEncoder(FFprobePath, FFmpegPath, t, TempDirectory))
            .OrderByDescending(t => t.Duration) // Process the longest files first. If two files are of the same length, process the largest file first.
            .ThenByDescending(t => (new FileInfo(t.InputFilePath).Length))
            .ToList();

        foreach (VMAFTargetVideoEncoder encoder in encoders)
        {
            Encoders.Add(new VMAFTargetEncoderToken(), encoder);
            encoder.InfoUpdate += EncoderInfoUpdate;

            InformationUpdate?.Invoke(this, new InformationUpdateEventArgs<VMAFTargetEncoderStatusReport>
            {
                Report = GetReport(Encoders.Reverse[encoder]),
                ModificationType = DataModificationType.Add
            });
        }
    }

    public void RemoveEntries(IEnumerable<VMAFTargetEncoderToken> tokens)
    {
        foreach (VMAFTargetEncoderToken token in tokens)
        {
            VMAFTargetVideoEncoder encoder = Encoders.Forward[token];

            if (encoder.State is EncodingState.Encoding)
                throw new InvalidOperationException("Cannot remove an encoder that is currently encoding.");

            encoder.InfoUpdate -= EncoderInfoUpdate;

            // Get the report before removing, as we will not be able to do so afterward without a direct reference to the encoder.
            VMAFTargetEncoderStatusReport report = GetReport(token);
            Encoders.Remove(token);

            InformationUpdate?.Invoke(this, new InformationUpdateEventArgs<VMAFTargetEncoderStatusReport>
            {
                Report = report,
                ModificationType = DataModificationType.Remove
            });
        }
    }

    public void ResetEntries(IEnumerable<VMAFTargetEncoderToken> tokens)
    {
        foreach (VMAFTargetEncoderToken token in tokens)
        {
            VMAFTargetVideoEncoder encoder = Encoders.Forward[token];

            if (encoder.State is EncodingState.Encoding)
                throw new InvalidOperationException("Cannot reset an encoder that is currently encoding.");

            encoder.Reset();

            InformationUpdate?.Invoke(this, new InformationUpdateEventArgs<VMAFTargetEncoderStatusReport>
            {
                Report = GetReport(token),
                ModificationType = DataModificationType.Update
            });
        }
    }

    public VMAFTargetEncoderStatusReport GetReport(VMAFTargetEncoderToken token)
    {
        VMAFTargetVideoEncoder encoder = Encoders.Forward[token];
        VMAFTargetEncoderStatusReport report = new VMAFTargetEncoderStatusReport
        {
            Token = token,
            State = encoder.State,
            EncodingPhase = encoder.EncodingPhase,
            InputFilePath = encoder.InputFilePath,
            FileSize = encoder.FileSize,
            CurrentDuration = encoder.CurrentDuration,
            Duration = encoder.Duration,
            LastVMAF = encoder.LastVMAF,
            HighCrf = encoder.HighCrf,
            LowCrf = encoder.LowCrf,
            ThisCrf = encoder.ThisCrf,
        };

        return report;
    }

    public string GetLogs(VMAFTargetEncoderToken token)
    {
        VMAFTargetVideoEncoder encoder = Encoders.Forward[token];
        return encoder.LogString;
    }

    private void EncoderInfoUpdate(VMAFTargetVideoEncoder encoder, DataReceivedEventArgs? info)
    {
        ProcessActions();

        InformationUpdate?.Invoke(this, new InformationUpdateEventArgs<VMAFTargetEncoderStatusReport>
        {
            Report = GetReport(Encoders.Reverse[encoder]),
            ModificationType = DataModificationType.Update
        });
    }

    /// <summary>
    /// Basically acts as an event loop that gets triggered by various actions.
    /// </summary>
    private void ProcessActions()
    {
        lock (_lock)
        {
            if (!IsEncoding)
                return;

            if (Encoders.Forward.Values.Count(e => e.State == EncodingState.Encoding) >= Concurrency)
                return;

            VMAFTargetVideoEncoder? encoder = Encoders.Forward.Values.FirstOrDefault(t => t.State == EncodingState.Pending);
            if (encoder is null)
                return;

            string directory = Path.GetDirectoryName(encoder.InputFilePath) ?? ".";
            string outputSubdirectory = Path.Combine(directory, OutputSubdirectory);
            string fileName = Path.GetFileNameWithoutExtension(encoder.InputFilePath);
            string newFilePath = Path.Combine(outputSubdirectory, $"{fileName}.{Extension}");

            // Create the output directory if it doesn't exist
            if (!Directory.Exists(outputSubdirectory))
                Directory.CreateDirectory(outputSubdirectory);

            encoder.Start(Arguments, H265, TargetVMAF, newFilePath);
        }
    }
}