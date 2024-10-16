using System.Diagnostics;
using System.Text;
using System.Text.Json;
using FFBatchConverter.Misc;

namespace FFBatchConverter.Encoders;

/// <summary>
/// Represents the encoder for a single video.
/// </summary>
internal class VideoEncoder
{
    /// <summary>
    /// Path to the input file.
    /// </summary>
    public string InputFilePath { get; }

    /// <summary>
    /// Full path of the output file.
    /// This is null until Start() is called.
    /// </summary>
    public string? OutputFilePath { get; private set; }

    internal StringBuilder Log { get; } = new StringBuilder();
    public string LogString => Log.ToString();

    private string FFprobePath { get; set; }
    private string FFmpegPath { get; set; }

    /// <summary>
    /// Duration of the video in seconds. Zero if the duration could not be determined (e.g. file does not exist or is not a video).
    /// </summary>
    public double Duration { get; }

    /// <summary>
    /// How much we've encoded so far, in seconds.
    /// </summary>
    internal double CurrentDuration { get; private set; }

    /// <summary>
    /// Size of the input file, in bytes.
    /// </summary>
    public long FileSize { get; private set; }

    internal EncodingState State { get; private set; } = EncodingState.Pending;

    /// <summary>
    /// Null when Start() has not yet been called.
    /// </summary>
    private Process? Process { get; set; }

    /// <summary>
    /// Should only run on main thread (same one processing UI events)
    /// </summary>
    public event Action<VideoEncoder, DataReceivedEventArgs?>? InfoUpdate;

    internal VideoEncoder(string ffprobePath, string ffmpegPath, string inputFilePath)
    {
        Initialize();

        FFprobePath = ffprobePath;
        FFmpegPath = ffmpegPath;
        InputFilePath = inputFilePath;

        FileSize = new FileInfo(inputFilePath).Length;

        string probeOutput = Helpers.Probe(FFprobePath, inputFilePath);

        // Json output is in probeOutput
        JsonDocument json = JsonDocument.Parse(probeOutput);

        // Copy probeOutput to Log for debugging, if we need it.
        Log.AppendLine(probeOutput);

        try
        {
            Duration = double.Parse(json.RootElement.GetProperty("format").GetProperty("duration").GetString() ?? throw new InvalidOperationException());
        }
        catch (Exception e)
        {
            Log.AppendLine(e.Message);
            State = EncodingState.Error;
        }
    }

    internal void Reset() => Initialize();

    private void Initialize()
    {
        if (State == EncodingState.Encoding)
            throw new InvalidOperationException("Cannot initialize or re-initialize an encoder that is currently encoding.");

        CurrentDuration = 0;
        State = EncodingState.Pending;
    }

    internal void Start(string ffmpegArguments, string outputFilePath)
    {
        if (State != EncodingState.Pending)
        {
            Log.AppendLine($"Cannot start encoding, state is not pending. Current state: {State}");
            return;
        }

        OutputFilePath = outputFilePath;

        Process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = FFmpegPath,
                Arguments = $"-i \"{InputFilePath}\" -y {ffmpegArguments} \"{OutputFilePath}\"",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            },
            EnableRaisingEvents = true
        };

        State = EncodingState.Encoding;

        // Attach event handlers for the process
        Process.OutputDataReceived += OnStreamDataReceivedEvent;
        Process.ErrorDataReceived += OnStreamDataReceivedEvent;

        Process.Start();

        Process.BeginOutputReadLine();
        Process.BeginErrorReadLine();

        Process.Exited += OnProcessOnExited;
    }

    private void OnProcessOnExited(object? sender, EventArgs args)
    {
        Debug.Assert(Process != null, nameof(Process) + " != null");

        State = Process.ExitCode == 0 ? EncodingState.Success : EncodingState.Error;

        Log.AppendLine($"Process exited with code {Process.ExitCode}");

        InfoUpdate?.Invoke(this, null);

        Process.OutputDataReceived -= OnStreamDataReceivedEvent;
        Process.ErrorDataReceived -= OnStreamDataReceivedEvent;
        Process.Exited -= OnProcessOnExited;
    }

    /// <summary>
    /// Fired when data is written by the underlying ffmpeg process.
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="args"></param>
    private void OnStreamDataReceivedEvent(object sender, DataReceivedEventArgs args)
    {
        if (State != EncodingState.Encoding)
            return;

        // Extract timestamp
        if (args.Data != null && args.Data.Contains("time="))
        {
            string time = args.Data.Split("time=")[1].Split(" ")[0];

            try
            {
                // Sometimes time is N/A. We don't need to worry, since it's likely that the video is done.
                CurrentDuration = TimeSpan.Parse(time).TotalSeconds;
            }
            catch (Exception e)
            {
                Log.AppendLine(e.Message);
            }
        }

        Log.AppendLine(args.Data);

        InfoUpdate?.Invoke(this, args);
    }
}