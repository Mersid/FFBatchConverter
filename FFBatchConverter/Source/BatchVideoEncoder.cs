using System.Diagnostics;

namespace FFBatchConverter;

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
    /// </summary>
    public string OutputPath { get; set; } = string.Empty;

    /// <summary>
    /// File extension to use for the output files.
    /// </summary>
    public string Extension { get; set; } = string.Empty;

    public string FfmpegPath { get; set; } = string.Empty;
    public string FfprobePath { get; set; } = string.Empty;

    /// <summary>
    /// FFmpeg arguments.
    /// </summary>
    public string Arguments { get; set; } = string.Empty;

    private List<VideoEncoder> Encoders { get; } = [];
    private bool IsEncoding { get; set; }

    /// <summary>
    /// Event that is raised when there's an update to the status of any encoder.
    /// There is no guarantee which thread this event will be raised on!
    /// If using this with UI, caller is responsible for marshalling to the UI thread.
    /// </summary>
    public event EventHandler<InformationUpdateEventArgs>? InformationUpdate;

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
            files.AddRange(GetFilesRecursive(p));
        }

        List<VideoEncoder> encoders = files
            .AsParallel()
            .AsOrdered()
            .WithDegreeOfParallelism(Environment.ProcessorCount)
            .Select(t => new VideoEncoder(FfprobePath, t))
            .OrderByDescending(t => t.Duration) // Process the longest files first. If two files are of the same length, process the largest file first.
            .ThenByDescending(t => (new FileInfo(t.InputFilePath).Length))
            .ToList();

        Encoders.AddRange(encoders);

        foreach (VideoEncoder encoder in encoders)
        {
            encoder.InfoUpdate += EncoderOnInfoUpdate;

            InformationUpdate?.Invoke(this, new InformationUpdateEventArgs
            {
                Encoder = encoder,
                ModificationType = DataModificationType.Add
            });
        }
    }

    private void EncoderOnInfoUpdate(VideoEncoder encoder, DataReceivedEventArgs? info)
    {
        ProcessActions();

        InformationUpdate?.Invoke(this, new InformationUpdateEventArgs
        {
            Encoder = encoder,
            ModificationType = DataModificationType.Update
        });
    }

    /// <summary>
    /// Basically acts as an event loop that gets triggered by various actions.
    /// </summary>
    private void ProcessActions()
    {
        if (!IsEncoding)
            return;

        if (Encoders.Count(e => e.State == EncodingState.Encoding) >= Concurrency)
            return;

        Encoders.FirstOrDefault(t => t.State == EncodingState.Pending)?.Start(FfmpegPath, Arguments, OutputPath, Extension);
    }

    private static List<string> GetFilesRecursive(string path)
    {
        List<string> files = [];
        if (Directory.Exists(path))
        {
            foreach (string dir in Directory.GetFileSystemEntries(path))
            {
                files.AddRange(GetFilesRecursive(dir));
            }
        }

        if (File.Exists(path))
        {
            files.Add(path);
        }

        return files;
    }
}