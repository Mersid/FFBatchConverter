using System.Diagnostics;
using BidirectionalMap;
using FFBatchConverter.Encoders;
using FFBatchConverter.Misc;
using FFBatchConverter.Models;
using FFBatchConverter.Tokens;

namespace FFBatchConverter.Controllers;

public class BatchVideoEncoder
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
    /// Do not use absolute paths!
    /// </summary>
    public string OutputSubdirectory { get; set; } = string.Empty;

    /// <summary>
    /// File extension to use for the output files.
    /// </summary>
    public string Extension { get; set; } = string.Empty;

    public required string FFmpegPath { get; init; }
    public required string FFprobePath { get; init; }

    /// <summary>
    /// FFmpeg arguments.
    /// </summary>
    public string Arguments { get; set; } = string.Empty;

    private BiMap<VideoEncoderToken, VideoEncoder> Encoders { get; } = [];
    private bool IsEncoding { get; set; }

    private readonly object _lock = new object();

    /// <summary>
    /// Event that is raised when there's an update to the status of any encoder.
    /// There is no guarantee which thread this event will be raised on!
    /// If using this with UI, caller is responsible for marshalling to the UI thread. <br />
    /// In general, an event is raised when a video encoder starts, finishes, or has received an update to its progress.
    /// </summary>
    public event EventHandler<InformationUpdateEventArgs<VideoEncoderStatusReport>>? InformationUpdate;

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

        List<VideoEncoder> encoders = files
            .AsParallel()
            .AsOrdered()
            .WithDegreeOfParallelism(Environment.ProcessorCount)
            .Select(t => new VideoEncoder(FFprobePath, FFmpegPath, t))
            .OrderByDescending(t => t.Duration) // Process the longest files first. If two files are of the same length, process the largest file first.
            .ThenByDescending(t => t.FileSize)
            .ToList();

        foreach (VideoEncoder encoder in encoders)
        {
            Encoders.Add(new VideoEncoderToken(), encoder);
            encoder.InfoUpdate += EncoderInfoUpdate;

            InformationUpdate?.Invoke(this, new InformationUpdateEventArgs<VideoEncoderStatusReport>
            {
                Report = GetReport(Encoders.Reverse[encoder]),
                ModificationType = DataModificationType.Add
            });
        }
    }

    public void RemoveEntries(IEnumerable<VideoEncoderToken> tokens)
    {
        foreach (VideoEncoderToken token in tokens)
        {
            VideoEncoder encoder = Encoders.Forward[token];

            if (encoder.State is EncodingState.Encoding)
                throw new InvalidOperationException("Cannot remove an encoder that is currently encoding.");

            encoder.InfoUpdate -= EncoderInfoUpdate;

            // Get the report before removing, as we will not be able to do so afterward without a direct reference to the encoder.
            VideoEncoderStatusReport report = GetReport(token);
            Encoders.Remove(token);

            InformationUpdate?.Invoke(this, new InformationUpdateEventArgs<VideoEncoderStatusReport>
            {
                Report = report,
                ModificationType = DataModificationType.Remove
            });
        }
    }

    public void ResetEntries(IEnumerable<VideoEncoderToken> tokens)
    {
        foreach (VideoEncoderToken token in tokens)
        {
            VideoEncoder encoder = Encoders.Forward[token];

            if (encoder.State is EncodingState.Encoding)
                throw new InvalidOperationException("Cannot reset an encoder that is currently encoding.");

            encoder.Reset();
            InformationUpdate?.Invoke(this, new InformationUpdateEventArgs<VideoEncoderStatusReport>
            {
                Report = GetReport(token),
                ModificationType = DataModificationType.Update
            });
        }
    }

    /// <summary>
    /// Produces a report for a specific encoder by its public token.
    /// </summary>
    /// <param name="token"></param>
    /// <returns></returns>
    public VideoEncoderStatusReport GetReport(VideoEncoderToken token)
    {
        VideoEncoder encoder = Encoders.Forward[token];
        VideoEncoderStatusReport report = new VideoEncoderStatusReport
        {
            Token = token,
            State = encoder.State,
            InputFilePath = encoder.InputFilePath,
            FileSize = encoder.FileSize,
            CurrentDuration = encoder.CurrentDuration,
            Duration = encoder.Duration
        };

        return report;
    }

    public string GetLogs(VideoEncoderToken token)
    {
        return Encoders.Forward[token].LogString;
    }

    private void EncoderInfoUpdate(VideoEncoder encoder, DataReceivedEventArgs? info)
    {
        ProcessActions();

        InformationUpdate?.Invoke(this, new InformationUpdateEventArgs<VideoEncoderStatusReport>
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

            VideoEncoder? encoder = Encoders.Forward.Values.FirstOrDefault(t => t.State == EncodingState.Pending);
            if (encoder is null)
                return;

            string directory = Path.GetDirectoryName(encoder.InputFilePath) ?? ".";
            string outputSubdirectory = Path.Combine(directory, OutputSubdirectory);
            string fileName = Path.GetFileNameWithoutExtension(encoder.InputFilePath);
            string newFilePath = Path.Combine(outputSubdirectory, $"{fileName}.{Extension}");

            // Create the output directory if it doesn't exist
            if (!Directory.Exists(outputSubdirectory))
                Directory.CreateDirectory(outputSubdirectory);

            encoder.Start(Arguments, newFilePath);
        }
    }
}