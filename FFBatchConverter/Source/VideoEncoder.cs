﻿using System.Data;
using System.Diagnostics;
using System.Text;
using System.Text.Json;

namespace FFBatchConverter;

/// <summary>
/// Represents the encoder for a single video.
/// </summary>
public class VideoEncoder
{
    public string InputFilePath { get; private set; }
    public StringBuilder Log { get; } = new StringBuilder();
    private Process Process { get; set; }

    /// <summary>
    /// Duration of the video in seconds.
    /// </summary>
    public double Duration { get; private set; }
    public double CurrentDuration { get; private set; }
    public EncodingState State { get; private set; } = EncodingState.Pending;
    public DataRow DataRow { get; }

    public event Action<VideoEncoder, DataReceivedEventArgs?>? InfoUpdate;

    public VideoEncoder(string inputFilePath, DataRow dataRow)
    {
        InputFilePath = inputFilePath;
        DataRow = dataRow;

        Process probe = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = Helpers.GetFFprobePath(),
                Arguments = $"-v quiet -print_format json -show_format \"{inputFilePath}\"",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            }
        };

        probe.Start();

        // Run ffprobe to get duration data
        StringBuilder probeOutput = new StringBuilder();
        while (!probe.StandardOutput.EndOfStream)
        {
            string? info = probe.StandardOutput.ReadLine();
            if (info != null)
                probeOutput.AppendLine(info);
        }

        // Json output is in probeOutput
        JsonDocument json = JsonDocument.Parse(probeOutput.ToString());

        // Copy probeOutput to Log for debugging, if we need it.
        Log.AppendLine(probeOutput.ToString());

        try
        {
            Duration = double.Parse(json.RootElement.GetProperty("format").GetProperty("duration").GetString() ?? throw new InvalidOperationException());
        }
        catch (Exception e)
        {
            Log.AppendLine(e.Message);
            State = EncodingState.Error;
        }

        TimeSpan duration = TimeSpan.FromSeconds(Duration);
        DataRow[0] = inputFilePath;
        DataRow[1] = $"{duration.Hours:D2}:{duration.Minutes:D2}:{duration.Seconds:D2}";
        DataRow[2] = $"{(new FileInfo(inputFilePath).Length / 1024d / 1024):F2} MiB";
        DataRow[3] = State.ToString();
    }

    public void Start(string ffmpegArguments, string outputDirectoryRelative, string extension)
    {
        if (State != EncodingState.Pending)
        {
            Log.AppendLine($"Cannot start encoding, state is not pending. Current state: {State}");
            return;
        }

        string directory = Path.GetDirectoryName(InputFilePath);
        string outputSubdirectory = Path.Combine(directory, outputDirectoryRelative);
        string fileName = Path.GetFileNameWithoutExtension(InputFilePath);
        string newFilePath = Path.Combine(outputSubdirectory, $"{fileName}.{extension}");

        // Create the output directory if it doesn't exist
        if (!Directory.Exists(outputSubdirectory))
            Directory.CreateDirectory(outputSubdirectory);

        Process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = Helpers.GetFFmpegPath(),
                Arguments = $"-i \"{InputFilePath}\" -y {ffmpegArguments} \"{newFilePath}\"",
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

        Process.Exited += OnProcessOnExited;
    }

    private void OnProcessOnExited(object? sender, EventArgs args)
    {
        State = Process.ExitCode == 0 ? EncodingState.Success : EncodingState.Error;
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