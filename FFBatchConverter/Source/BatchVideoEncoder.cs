using System.Diagnostics;
using System.Reactive.Concurrency;
using Terminal.Gui;

namespace FFBatchConverter;

public class BatchVideoEncoder
{
    /// <summary>
    /// Event that is raised when there's an update to the status of any encoder.
    /// </summary>
    public event EventHandler<InformationUpdateEventArgs>? InformationUpdate;
    private List<VideoEncoder2> Encoders { get; } = [];

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
    private int ActiveEncodingCount { get; set; }
    private bool IsEncoding { get; set; }

    /// <summary>
    /// FFmpeg arguments.
    /// </summary>
    public string Arguments { get; set; }

    /// <summary>
    /// Output path, relative to the input file.
    /// </summary>
    public string OutputPath { get; set; }

    /// <summary>
    /// File extension to use for the output files.
    /// </summary>
    public string Extension { get; set; }

    public void StartEncoding()
    {
        WarnIfNotOnMainThread();
        IsEncoding = true;
        ProcessActions();
    }

    public void StopEncoding()
    {
        WarnIfNotOnMainThread();
        IsEncoding = false;
        ProcessActions();
    }

    /// <summary>
    /// Basically acts as an event loop that gets triggered by various actions.
    /// </summary>
    private void ProcessActions()
    {
        if (!IsEncoding)
            return;

        List<VideoEncoder2> queuedEncoders = Encoders.Where(e => e.State == EncodingState.Pending).ToList();
        for (int i = ActiveEncodingCount; i < Concurrency; i++)
        {
            if (queuedEncoders.Count == 0)
                break;

            VideoEncoder2 encoder = Encoders[0];
            Encoders.RemoveAt(0);

            encoder.Start(Arguments, OutputPath, Extension);

            ActiveEncodingCount++;
        }
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

        List<VideoEncoder2> encoders = files
            .AsParallel()
            .AsOrdered()
            .WithDegreeOfParallelism(Environment.ProcessorCount)
            .Select(t => new VideoEncoder2(t))
            .OrderByDescending(t => t.Duration) // Process the longest files first. If two files are of the same length, process the largest file first.
            .ThenByDescending(t => (new FileInfo(t.InputFilePath).Length))
            .ToList();

        Encoders.AddRange(encoders);

        foreach (VideoEncoder2 encoder in encoders)
        {
            encoder.InfoUpdate += EncoderOnInfoUpdate;

            InformationUpdate?.Invoke(this, new InformationUpdateEventArgs
            {
                Encoder = encoder,
                ModificationType = DataModificationType.Add
            });
        }
    }

    private void EncoderOnInfoUpdate(VideoEncoder2 encoder, DataReceivedEventArgs? info)
    {
        // If encoding is done (or fail) we need to decrement the active encoding count to allow the next
        // video in the queue to start encoding.
        if (encoder.State != EncodingState.Encoding)
        {
            ActiveEncodingCount--;
            ProcessActions();
        }

        InformationUpdate?.Invoke(this, new InformationUpdateEventArgs
        {
            Encoder = encoder,
            ModificationType = DataModificationType.Update
        });
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

    private static void WarnIfNotOnMainThread()
    {
        if (SynchronizationContext.Current is null)
        {
            MessageBox.ErrorQuery("Not on main thread!", "Batch encoder code is not running on the main thread! This could cause problems!", "Continue");
        }
    }
}