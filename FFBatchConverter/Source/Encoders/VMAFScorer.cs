using System.Diagnostics;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using FFBatchConverter.Misc;

namespace FFBatchConverter.Encoders;

internal class VMAFScorer
{
    private string OriginalFilePath { get; }
    private string DistortedFilePath { get; }

    private StringBuilder Log { get; } = new StringBuilder();

    /// <summary>
    /// Duration of the video in seconds.
    /// </summary>
    public double Duration { get; }
    public double CurrentDuration { get; private set; }
    public EncodingState State { get; private set; } = EncodingState.Pending;

    /// <summary>
    /// The resulting VMAF score. This is only valid when State is Success.
    /// <remarks>
    /// We could probably make this a report, but for now this is only part of the internal API, so we'll hold off.
    /// </remarks>
    /// </summary>
    public double VMAFScore { get; private set; }

    /// <summary>
    /// Null when Start() has not yet been called.
    /// </summary>
    private Process? Process { get; set; }

    /// <summary>
    /// This is run from the process thread!
    /// </summary>
    public event Action<VMAFScorer, DataReceivedEventArgs?>? InfoUpdate;

    /// <summary>
    ///
    /// </summary>
    /// <param name="ffprobePath">Path to the ffprobe program.</param>
    /// <param name="originalFilePath">Path to the original (reference) video.</param>
    /// <param name="distortedFilePath">Path of the distorted (encoded) video.</param>
    internal VMAFScorer(string ffprobePath, string originalFilePath, string distortedFilePath)
    {
        OriginalFilePath = originalFilePath;
        DistortedFilePath = distortedFilePath;

        string probeOutput = Helpers.Probe(ffprobePath, originalFilePath);

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

    internal void Start(string ffmpegPath)
    {
        if (State != EncodingState.Pending)
        {
            Log.AppendLine($"Cannot start encoding, state is not pending. Current state: {State}");
            return;
        }

        Process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = ffmpegPath,
                Arguments = $"-i \"{OriginalFilePath}\" -i \"{DistortedFilePath}\" -y -filter_complex \"[0:v]setpts=PTS-STARTPTS[reference]; [1:v]setpts=PTS-STARTPTS[distorted]; [distorted][reference]libvmaf=model=version=vmaf_v0.6.1:n_threads=30\" -f null -",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true,
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

        Process.Exited += OnProcessExitedEvent;
    }

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

    private void OnProcessExitedEvent(object? sender, EventArgs e)
    {
        Debug.Assert(Process != null, nameof(Process) + " != null");

        State = Process.ExitCode == 0 ? EncodingState.Success : EncodingState.Error;

        Log.AppendLine($"Process exited with code {Process.ExitCode}");

        Regex regex = new Regex("(?<=VMAF score: )[0-9.]+");
        string vmafScoreString = regex.Match(Log.ToString()).Value;

        if (double.TryParse(vmafScoreString, out double vmafScore))
        {
            VMAFScore = vmafScore;
        }
        else
        {
            Log.AppendLine("Could not parse VMAF score.");
            VMAFScore = 0;
            State = EncodingState.Error;
        }

        InfoUpdate?.Invoke(this, null);

        Process.OutputDataReceived -= OnStreamDataReceivedEvent;
        Process.ErrorDataReceived -= OnStreamDataReceivedEvent;
        Process.Exited -= OnProcessExitedEvent;
    }
}