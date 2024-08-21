using System.Diagnostics;
using FFBatchConverter.Encoders;
using FFBatchConverter.Misc;
using FFBatchConverter.Models;

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

    private List<VideoEncoder> Encoders { get; } = [];
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

        Encoders.AddRange(encoders);

        foreach (VideoEncoder encoder in encoders)
        {
            encoder.InfoUpdate += EncoderInfoUpdate;

            InformationUpdate?.Invoke(this, new InformationUpdateEventArgs<VideoEncoderStatusReport>
            {
                Report = encoder.Report,
                ModificationType = DataModificationType.Add
            });
        }
    }

    /// <summary>
    /// Takes a list of encoders. If this encoder is pending or done (failed or successful), it will remove it from the list.
    /// It is an error to remove an encoder that is currently encoding.
    /// </summary>
    /// <param name="encoders"></param>
    public void RemoveEntries(IEnumerable<VideoEncoder> encoders)
    {
        lock (_lock)
        {
            foreach (VideoEncoder encoder in encoders)
            {
                if (encoder.State == EncodingState.Encoding)
                    throw new InvalidOperationException("Cannot remove an encoder that is currently encoding.");

                if (!Encoders.Remove(encoder))
                    continue;

                encoder.InfoUpdate -= EncoderInfoUpdate;

                InformationUpdate?.Invoke(this, new InformationUpdateEventArgs<VideoEncoderStatusReport>
                {
                    Report = encoder.Report,
                    ModificationType = DataModificationType.Remove
                });
            }
        }
    }

    private void EncoderInfoUpdate(VideoEncoder encoder, DataReceivedEventArgs? info)
    {
        ProcessActions();

        InformationUpdate?.Invoke(this, new InformationUpdateEventArgs<VideoEncoderStatusReport>
        {
            Report = encoder.Report,
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

            if (Encoders.Count(e => e.State == EncodingState.Encoding) >= Concurrency)
                return;

            VideoEncoder? encoder = Encoders.FirstOrDefault(t => t.State == EncodingState.Pending);
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