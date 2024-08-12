using System.Diagnostics;
using System.Text;

namespace FFBatchConverter;

public class VMAFVideoEncoder
{
    public VideoEncoder? VideoEncoder { get; set; }
    public VMAFScorer? VMAFScorer { get; set; }
    public StringBuilder Log { get; } = new StringBuilder();

    public double Duration { get; private set; }
    public string InputFilePath { get; private set; }
    private string OutputFilePath { get; set; }
    public bool H265 { get; private set; }
    public int Crf { get; private set; }

    public EncodingState State { get; private set; } = EncodingState.Pending;

    private string FFprobePath { get; set; }
    private string FFmpegPath { get; set; }
    private string FFmpegArguments { get; set; }

    private readonly string _tempDirectory = Path.Combine(Path.GetTempPath(), "FFBatchConverter");

    public event Action<VMAFVideoEncoder, DataReceivedEventArgs?>? InfoUpdate;

    /// <summary>
    ///
    /// </summary>
    /// <param name="ffProbePath">Full path to the ffprobe program.</param>
    /// <param name="ffmpegPath">Full path to the ffmpeg program.</param>
    /// <param name="inputFilePath">Full path to the input video file.</param>
    public VMAFVideoEncoder(string ffProbePath, string ffmpegPath, string inputFilePath)
    {
        FFprobePath = ffProbePath;
        FFmpegPath = ffmpegPath;
        InputFilePath = inputFilePath;

        // Make a video encoder to get the duration.
        VideoEncoder = new VideoEncoder(ffProbePath, ffmpegPath, inputFilePath);
        Duration = VideoEncoder.Duration;

        Log.AppendLine(VideoEncoder.Log.ToString());
    }

    /// <summary>
    ///
    /// </summary>
    /// <param name="ffmpegArguments">Arguments to pass to ffmpeg. Do not include -c:v (or -vcodec) or -crf flags.</param>
    /// <param name="h265">True to use h265 (-c:v libx265) encoding, false to use h264. This is why we should not pass in -c:v in ffmpegArguments.</param>
    /// <param name="outputFilePath"></param>
    public void Start(string ffmpegArguments, bool h265, int crf, string outputFilePath)
    {
        FFmpegArguments = ffmpegArguments;
        OutputFilePath = outputFilePath;
        H265 = h265;
        Crf = crf;

        // Dot is included in Path.GetExtension.
        if (!Directory.Exists(_tempDirectory))
            Directory.CreateDirectory(_tempDirectory);
        string tempFile = Path.Combine(_tempDirectory, $"{Crf}-{Guid.NewGuid()}{Path.GetExtension(OutputFilePath)}");
        string arguments = $"{FFmpegArguments} -c:v {(H265 ? "libx265" : "libx264")} -crf {Crf}";

        VideoEncoder.InfoUpdate += OnEncoderInfoUpdate;

        VideoEncoder.Start(arguments, tempFile);
        State = EncodingState.Encoding;
    }

    private void OnEncoderInfoUpdate(VideoEncoder encoder, DataReceivedEventArgs? args)
    {
        Log.AppendLine(args?.Data);

        if (encoder.State == EncodingState.Error)
        {
            // TODO: Handle error
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
            // TODO: Handle error
            State = EncodingState.Error;
        }

        if (scorer.State == EncodingState.Success)
        {
            VMAFScorer.InfoUpdate -= OnScorerInfoUpdate;
            State = EncodingState.Success;
        }

        InfoUpdate?.Invoke(this, args);
    }
}