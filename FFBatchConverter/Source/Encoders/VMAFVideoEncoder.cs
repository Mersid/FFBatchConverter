using System.Diagnostics;
using System.Text;
using FFBatchConverter.Misc;
using FFBatchConverter.Models;

namespace FFBatchConverter.Encoders;

/// <summary>
/// This encoder can be used to encode a video in x264 or x265 with a specific CRF value, and then score it with VMAF
/// to determine the quality of the encoding.
/// </summary>
internal class VMAFVideoEncoder
{
    private VideoEncoder VideoEncoder { get; set; }

    /// <summary>
    /// If this is not null, we're in the scoring phase.
    /// </summary>
    private VMAFScorer? VMAFScorer { get; set; }
    internal StringBuilder Log { get; } = new StringBuilder();
    public string LogString => Log.ToString();

    /// <summary>
    /// Duration of the video in seconds. Zero if the duration could not be determined (e.g. file does not exist or is not a video).
    /// </summary>
    public double Duration { get; private set; }

    /// <summary>
    /// Gets the processed duration of the video, in seconds, for the current stage of the process.
    /// If we're encoding, then this is the current duration of the encoded video. If we're scoring, then this is the current duration of the scored video.
    /// </summary>
    public double CurrentDuration => VMAFScorer?.CurrentDuration ?? VideoEncoder.CurrentDuration;

    /// <summary>
    /// Size of the input file, in bytes.
    /// </summary>
    public long FileSize { get; private set; }

    public VMAFVideoEncodingPhase EncodingPhase => VMAFScorer != null ? VMAFVideoEncodingPhase.Scoring : VMAFVideoEncodingPhase.Encoding;

    /// <summary>
    /// Full path of the input video.
    /// </summary>
    public string InputFilePath { get; }

    /// <summary>
    /// Full path of the output video. The container type of the encoded video is determined by the file extension here.
    /// Ensure the path exists, as the encoder will not create directories.
    /// Null until the Start method is called, as that is when the output file is provided.
    /// </summary>
    public string? OutputFilePath { get; private set; }

    /// <summary>
    /// Set to true to use H.265 instead of H.264. (-c:v libx265 vs -c:v libx264)
    /// </summary>
    private bool H265 { get; set; }

    /// <summary>
    /// Set a value [0, 51] for the CRF value. Lower values are higher quality.
    /// Use -1 to use the default value for the codec (23 for x264, 28 for x265).
    /// </summary>
    private int Crf { get; set; }

    public EncodingState State { get; private set; } = EncodingState.Pending;

    private string FFprobePath { get; set; }
    private string FFmpegPath { get; set; }

    /// <summary>
    /// Arguments to pass to ffmpeg. Do not include -c:v (or -vcodec) or -crf flags, as those are handled by
    /// other settings.
    /// Null until the Start method is called, as that is when the arguments are provided.
    /// </summary>
    private string? FFmpegArguments { get; set; }

    /// <summary>
    /// The VMAF score of the encoded video. Zero until the encoding and scoring process is complete.
    /// </summary>
    public double VMAFScore => VMAFScorer?.VMAFScore ?? 0;

    public event Action<VMAFVideoEncoder, DataReceivedEventArgs?>? InfoUpdate;

    /// <summary>
    /// Creates a new VMAF video encoder.
    /// </summary>
    /// <param name="ffProbePath">Full path to the ffprobe program.</param>
    /// <param name="ffmpegPath">Full path to the ffmpeg program.</param>
    /// <param name="inputFilePath">Full path to the input video file.</param>
    internal VMAFVideoEncoder(string ffProbePath, string ffmpegPath, string inputFilePath)
    {
        FFprobePath = ffProbePath;
        FFmpegPath = ffmpegPath;
        InputFilePath = Path.GetFullPath(inputFilePath);

        FileSize = new FileInfo(inputFilePath).Length;

        // Make a video encoder to get the duration.
        VideoEncoder = new VideoEncoder(ffProbePath, ffmpegPath, inputFilePath);
        Duration = VideoEncoder.Duration;

        Log.AppendLine(VideoEncoder.Log.ToString());

        if (Duration == 0)
        {
            Log.AppendLine("Could not determine duration. Please verify if this is a valid video.");
            State = EncodingState.Error;
        }
    }

    /// <summary>
    /// Starts the encoding process. A video will be encoded with the given settings, and then scored with VMAF.
    /// </summary>
    /// <param name="ffmpegArguments">Arguments to pass to ffmpeg. Do not include -c:v (or -vcodec) or -crf flags.</param>
    /// <param name="h265">True to use h265 (-c:v libx265) encoding, false to use h264. This is why we should not pass in -c:v in ffmpegArguments.</param>
    /// <param name="crf">The CRF value to use for the encoder. Use -1 to use the default value for the codec.</param>
    /// <param name="outputFilePath">Full path to the output video file.</param>
    internal void Start(string ffmpegArguments, bool h265, int crf, string outputFilePath)
    {
        FFmpegArguments = ffmpegArguments;
        OutputFilePath = outputFilePath;
        H265 = h265;
        Crf = crf;

        string arguments = $"{FFmpegArguments} -c:v {(H265 ? "libx265" : "libx264")}";
        if (Crf != -1)
            arguments += $" -crf {Crf}";

        VideoEncoder.InfoUpdate += OnEncoderInfoUpdate;

        VideoEncoder.Start(arguments, OutputFilePath);
        State = EncodingState.Encoding;
    }

    internal void Reset() => Initialize();

    private void Initialize()
    {
        if (State == EncodingState.Encoding)
            throw new InvalidOperationException("Cannot initialize or re-initialize an encoder that is currently encoding.");

        VideoEncoder.Reset();
        State = EncodingState.Pending;
        VMAFScorer = null;
    }

    private void OnEncoderInfoUpdate(VideoEncoder encoder, DataReceivedEventArgs? args)
    {
        Log.AppendLine(args?.Data);

        if (encoder.State == EncodingState.Error)
        {
            VideoEncoder.InfoUpdate -= OnEncoderInfoUpdate;
            Log.AppendLine("Encoding failed.");
            State = EncodingState.Error;
        }

        if (encoder.State == EncodingState.Success)
        {
            VideoEncoder.InfoUpdate -= OnEncoderInfoUpdate;
            VMAFScorer = new VMAFScorer(FFprobePath, InputFilePath, encoder.OutputFilePath);
            VMAFScorer.InfoUpdate += OnScorerInfoUpdate;
            Log.AppendLine("Encoding complete. Starting VMAF scoring.");
            VMAFScorer.Start(FFmpegPath);
        }

        InfoUpdate?.Invoke(this, args);
    }

    private void OnScorerInfoUpdate(VMAFScorer scorer, DataReceivedEventArgs? args)
    {
        Log.AppendLine(args?.Data);

        if (scorer.State == EncodingState.Error)
        {
            Log.AppendLine("VMAF scoring failed.");

            scorer.InfoUpdate -= OnScorerInfoUpdate;
            State = EncodingState.Error;
        }

        if (scorer.State == EncodingState.Success)
        {
            scorer.InfoUpdate -= OnScorerInfoUpdate;
            State = EncodingState.Success;
        }

        InfoUpdate?.Invoke(this, args);
    }
}