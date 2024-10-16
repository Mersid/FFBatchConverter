﻿using System.Diagnostics;
using System.Text;
using FFBatchConverter.Misc;
using FFBatchConverter.Models;

namespace FFBatchConverter.Encoders;

/// <summary>
/// This encoder can be used to encode a video in x264 or x265 with a targeted VMAF value. The encoder will then
/// attempt to find the CRF value that puts the video above the target VMAF value, such that the next CRF down would
/// put it below the target VMAF value. In short, it encodes the video to a minimum perceptual quality level.
/// </summary>
internal class VMAFTargetVideoEncoder
{
    private VMAFVideoEncoder? VideoEncoder { get; set; }
    private StringBuilder Log { get; } = new StringBuilder();
    public string LogString => Log.ToString();

    public double Duration { get; private set; }

    /// <summary>
    /// Current duration of the processing video encoder's current phase, in seconds.
    /// </summary>
    internal double CurrentDuration => VideoEncoder.CurrentDuration;

    /// <summary>
    /// Size of the input file, in bytes.
    /// </summary>
    public long FileSize { get; private set; }

    public string InputFilePath { get; private set; }
    /// <summary>
    /// Full path of the output video. The container type of the encoded video is determined by the file extension here.
    /// Ensure the path exists, as the encoder will not create directories.
    /// Null until the Start method is called, as that is when the output file is provided.
    /// </summary>
    private string? OutputFilePath { get; set; }

    private bool H265 { get; set; }

    internal EncodingState State { get; private set; } = EncodingState.Pending;

    private string FFprobePath { get; set; }
    private string FFmpegPath { get; set; }

    /// <summary>
    /// The directory to store temporary files generated by this encoder.
    /// </summary>
    private string TempDirectory { get; set; } = Path.Combine(Path.GetTempPath(), "FFBatchConverter");

    /// <summary>
    /// Arguments to pass to ffmpeg. Do not include -c:v (or -vcodec) or -crf flags, as those are handled by
    /// other settings.
    /// Null until the Start method is called, as that is when the arguments are provided.
    /// </summary>
    private string? FFmpegArguments { get; set; }



    private List<CrfToVMAFMap> CrfToVmafMaps { get; } = [];

    /// <summary>
    /// Lossless
    /// </summary>
    private const int MinCrf = 0;

    /// <summary>
    /// Technically 63 is the max for x264 10-bit, but all other cases are 51.
    /// </summary>
    private const int MaxCrf = 51;

    private const int DefaultH264Crf = 23;
    private const int DefaultH265Crf = 28;

    internal int HighCrf { get; set; } = MaxCrf;
    internal int LowCrf { get; set; } = MinCrf;

    /// <summary>
    /// The CRF value we're trying for this iteration of the VMAF encoder.
    /// </summary>
    internal int ThisCrf { get; set; }
    private double TargetVMAF { get; set; }

    /// <summary>
    /// The VMAF score of the last encoded video.
    /// Null if we haven't finished scoring at least one video yet.
    /// </summary>
    internal double? LastVMAF { get; set; }

    /// <summary>
    /// Step 0: Run with CRF 0 to get the upper bound of the VMAF score.
    /// Step 1: Run with max CRF to get the lower bound of the VMAF score.
    /// Step 2: Use divide/conquer to narrow the range down to about 4 CRF values.
    /// Step 3: Iterate across those values.
    /// Step 4: Linearly walk in the correct direction until we find the correct CRF.
    /// Step -1: Flag step to terminate encoding.
    /// </summary>
    internal int PredictionStep { get; set; } = 0;

    public VMAFVideoEncodingPhase EncodingPhase => VideoEncoder.EncodingPhase;

    public event Action<VMAFTargetVideoEncoder, DataReceivedEventArgs?>? InfoUpdate;

    /// <summary>
    /// Initializes a new instance of the <see cref="VMAFTargetVideoEncoder"/> class.
    /// </summary>
    /// <param name="ffProbePath">Full path to the ffprobe program.</param>
    /// <param name="ffmpegPath">Full path to the ffmpeg program.</param>
    /// <param name="inputFilePath">Full path to the input video file.</param>
    /// <param name="tempDirectory">The directory to store temporary files.</param>
    internal VMAFTargetVideoEncoder(string ffProbePath, string ffmpegPath, string inputFilePath, string tempDirectory)
    {
        Initialize();

        FFprobePath = ffProbePath;
        FFmpegPath = ffmpegPath;
        InputFilePath = inputFilePath;
        TempDirectory = tempDirectory;

        FileSize = new FileInfo(inputFilePath).Length;

        // Make a video encoder to get the duration.
        VideoEncoder = new VMAFVideoEncoder(ffProbePath, ffmpegPath, inputFilePath);
        Duration = VideoEncoder.Duration;

        Log.AppendLine(VideoEncoder.Log.ToString());

        if (Duration == 0)
        {
            Log.AppendLine("Could not determine duration. Please verify if this is a valid video.");
            State = EncodingState.Error;
        }
    }

    internal void Reset() => Initialize();

    private void Initialize()
    {
        if (State == EncodingState.Encoding)
            throw new InvalidOperationException("Cannot initialize or re-initialize an encoder that is currently encoding.");

        State = EncodingState.Pending;
        CrfToVmafMaps.Clear();
        HighCrf = MaxCrf;
        LowCrf = MinCrf;
        LastVMAF = null;
        PredictionStep = 0;

        if (VideoEncoder is not null)
        {
            VideoEncoder.Reset();
            VideoEncoder.InfoUpdate -= OnEncoderInfoUpdate;
        }
    }

    /// <summary>
    /// Starts the encoding process. A video will be encoded with the given settings, and then
    /// repeatedly encoded with different CRF values until the VMAF score is above the target value.
    /// </summary>
    /// <param name="ffmpegArguments">Arguments to pass to ffmpeg. Do not include -c:v (or -vcodec) or -crf flags.</param>
    /// <param name="h265">True to use h265 (-c:v libx265) encoding, false to use h264. This is why we should not pass in -c:v in ffmpegArguments.</param>
    /// <param name="targetVMAF">The minimum VMAF score to aim for without excessively exceeding.</param>
    /// <param name="outputFilePath">Full path to the output video file.</param>
    internal void Start(string ffmpegArguments, bool h265, double targetVMAF, string outputFilePath)
    {
        FFmpegArguments = ffmpegArguments;
        OutputFilePath = outputFilePath;
        TargetVMAF = targetVMAF;
        H265 = h265;
        ThisCrf = H265 ? DefaultH265Crf : DefaultH264Crf; // TODO Remove me
        ThisCrf = 0;

        // Dot is included in Path.GetExtension.
        if (!Directory.Exists(TempDirectory))
            Directory.CreateDirectory(TempDirectory);
        string tempFile = Path.Combine(TempDirectory, $"{ThisCrf}-{Guid.NewGuid()}{Path.GetExtension(OutputFilePath)}");
        string arguments = $"{FFmpegArguments} -c:v {(H265 ? "libx265" : "libx264")} -crf {ThisCrf}";

        VideoEncoder.InfoUpdate += OnEncoderInfoUpdate;

        VideoEncoder.Start(arguments, H265, ThisCrf, tempFile);
        State = EncodingState.Encoding;
        PredictionStep = 0;
    }

    private void OnEncoderInfoUpdate(VMAFVideoEncoder encoder, DataReceivedEventArgs? args)
    {
        Log.AppendLine(args?.Data);

        if (encoder.State == EncodingState.Error)
        {
            State = EncodingState.Error;
            Cleanup();
        }

        if (encoder.State == EncodingState.Success)
        {
            Debug.Assert(encoder.OutputFilePath != null, "encoder.OutputFilePath != null");
            Debug.Assert(OutputFilePath != null, nameof(OutputFilePath) + " != null");

            VideoEncoder.InfoUpdate -= OnEncoderInfoUpdate;

            // Scan to see if we found the boundary.
            CrfToVmafMaps.Add(new CrfToVMAFMap
            {
                Crf = ThisCrf,
                VmafScore = encoder.VMAFScore,
                FilePath = encoder.OutputFilePath
            });

            LastVMAF = encoder.VMAFScore;

            if      (PredictionStep is 0) Validate0();
            else if (PredictionStep is 1) Validate1();
            else if (PredictionStep is 2) Validate2();
            else if (PredictionStep is 3) Validate3();
            else if (PredictionStep is 4) Validate4();

            if      (PredictionStep is 1) Step1();
            else if (PredictionStep is 2) Step2();
            else if (PredictionStep is 3) Step3();
            else if (PredictionStep is 4) Step4();
            else if (PredictionStep is -1) ErrorStep();

            List<CrfToVMAFMap> maps = CrfToVmafMaps.OrderBy(x => x.Crf).ToList();
            for (int i = 1; i < maps.Count; i++)
            {
                if (maps[i].VmafScore < TargetVMAF && maps[i - 1].VmafScore >= TargetVMAF && maps[i].Crf - maps[i - 1].Crf == 1)
                {
                    // Found the boundary. Should also note there's an inverse relationship between CRF and VMAF.
                    CrfToVMAFMap target = maps[i - 1];

                    File.Copy(target.FilePath, OutputFilePath, true);
                    State = EncodingState.Success;
                    LastVMAF = target.VmafScore;
                    ThisCrf = target.Crf;
                    Cleanup();
                    InfoUpdate?.Invoke(this, args);
                    return;
                }
            }

            // This could be set in ErrorStep, in which case we will need to exit processing.
            if (State is EncodingState.Error)
                return;

            // Did not find boundary. Need to adjust CRF and try again.
            VideoEncoder = new VMAFVideoEncoder(FFprobePath, FFmpegPath, InputFilePath);
            VideoEncoder.InfoUpdate += OnEncoderInfoUpdate;

            // We shouldn't ever encode a video twice; if we do, the algorithm probably got stuck in a loop.
            // TODO: Delete/modify this.
            // if (CrfToVmafMaps.Any(x => x.Crf == ThisCrf))
            // {
            //     Log.AppendLine($"Algorithm got stuck in a loop. Aborting. CRF range is {LowCrf} to {HighCrf}.");
            //     State = EncodingState.Error;
            //     Cleanup();
            //     InfoUpdate?.Invoke(this, args);
            //     return;
            // }

            string tempFile = Path.Combine(TempDirectory, $"{ThisCrf}-{Guid.NewGuid()}{Path.GetExtension(OutputFilePath)}");
            string arguments = $"{FFmpegArguments} -c:v {(H265 ? "libx265" : "libx264")} -crf {ThisCrf}";
            Log.AppendLine($"Trying CRF {ThisCrf}");
            VideoEncoder.Start(arguments, H265, ThisCrf, tempFile);
        }

        InfoUpdate?.Invoke(this, args);
    }

    private void Validate0()
    {
        if (LastVMAF < TargetVMAF)
        {
            Log.AppendLine($"VMAF with CRF 0 is {LastVMAF}. It needs to be greater than {TargetVMAF}. Exiting.");
            PredictionStep = -1;
        }
        else
        {
            PredictionStep = 1;
        }
    }

    private void Validate1()
    {
        if (TargetVMAF < LastVMAF)
        {
            Log.AppendLine($"VMAF with max CRF ({MaxCrf}) is {LastVMAF}. It needs to be greater than {TargetVMAF}. Exiting.");
            PredictionStep = -1;
        }
        else
        {
            PredictionStep = 2;
        }
    }

    private void Validate2()
    {
        if (HighCrf - LowCrf <= 4)
        {
            PredictionStep = 3;
        }
    }

    private void Validate3()
    {
        if (ThisCrf == HighCrf)
        {
            PredictionStep = 4;
        }
    }

    private void Validate4()
    {

    }

    private void Step1()
    {
        ThisCrf = MaxCrf;
    }

    private void Step2()
    {
        // Check if we overshot of undershot the target, then binary search our way down.
        // The algorithm in the else blocks don't really work well when the CRF range begins to converge.
        // For example, if high = 39, low = 36, this = 37, the algorithm will pick 37 - 1 = 36, leaving us in a loop.
        if (VideoEncoder.VMAFScore > TargetVMAF)
        {
            // Too high. Decrease VMAF, increase CRF range.
            LowCrf = ThisCrf - 1;
            ThisCrf = (LowCrf + HighCrf) / 2;
        }
        else
        {
            // Too low. Increase VMAF, decrease CRF range.
            HighCrf = ThisCrf;
            ThisCrf = (LowCrf + HighCrf) / 2;
        }
    }

    private void Step3()
    {
        ThisCrf++;
    }

    private void Step4()
    {
        if (LastVMAF < TargetVMAF)
            ThisCrf--;
        else
            ThisCrf++;
    }

    private void ErrorStep()
    {
        Log.AppendLine("Encoding error!");
        State = EncodingState.Error;
        Cleanup();
        InfoUpdate?.Invoke(this, null);
    }

    private void Cleanup()
    {
        List<string> tempFiles = CrfToVmafMaps.Select(x => x.FilePath).ToList();
        foreach (string tempFile in tempFiles)
        {
            if (File.Exists(tempFile))
                File.Delete(tempFile);
        }
    }
}