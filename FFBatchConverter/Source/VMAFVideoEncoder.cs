using System.Diagnostics;

namespace FFBatchConverter;

public class VMAFVideoEncoder
{
    public VideoEncoder? VideoEncoder { get; private set; }
    public VMAFScorer? VMAFScorer { get; private set; }

    public double Duration { get; private set; }
    public string InputFilePath { get; private set; }
    private string OutputFilePath { get; set; }
    public bool H265 { get; private set; }

    public EncodingState State { get; private set; } = EncodingState.Pending;

    private string FFprobePath { get; set; }
    private string FFmpegPath { get; set; }
    private string FFmpegArguments { get; set; }

    private readonly string _tempDirectory = Path.Combine(Path.GetTempPath(), "FFBatchConverter");

    private List<CrfToVMAFMap> CrfToVmafMaps { get; } = [];

    /// <summary>
    /// Lossless
    /// </summary>
    public const int MinCrf = 0;

    /// <summary>
    /// Technically 63 is the max for x264 10-bit, but all other cases are 51.
    /// </summary>
    public const int MaxCrf = 51;

    public const int DefaultH264Crf = 23;
    public const int DefaultH265Crf = 28;

    private int HighCrf { get; set; } = MaxCrf;
    private int LowCrf { get; set; } = MinCrf;
    private int ThisCrf { get; set; }
    public double TargetVMAF { get; set; }

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
    }

    /// <summary>
    ///
    /// </summary>
    /// <param name="ffmpegArguments">Arguments to pass to ffmpeg. Do not include -c:v (or -vcodec) or -crf flags.</param>
    /// <param name="h265">True to use h265 (-c:v libx265) encoding, false to use h264. This is why we should not pass in -c:v in ffmpegArguments.</param>
    /// <param name="outputFilePath"> TODO: Make this output file instead of directory?</param>
    public void Start(string ffmpegArguments, bool h265, double targetVMAF, string outputFilePath)
    {
        FFmpegArguments = ffmpegArguments;
        OutputFilePath = outputFilePath;
        TargetVMAF = targetVMAF;
        H265 = h265;
        ThisCrf = H265 ? DefaultH265Crf : DefaultH264Crf;

        // Dot is included in Path.GetExtension.
        if (!Directory.Exists(_tempDirectory))
            Directory.CreateDirectory(_tempDirectory);
        string tempFile = Path.Combine(_tempDirectory, $"{ThisCrf}-{Guid.NewGuid()}{Path.GetExtension(InputFilePath)}");
        string arguments = $"{FFmpegArguments} -c:v {(H265 ? "libx265" : "libx264")} -crf {ThisCrf}";

        VideoEncoder.InfoUpdate += OnEncoderInfoUpdate;

        VideoEncoder.Start(arguments, tempFile);
    }

    private void OnEncoderInfoUpdate(VideoEncoder encoder, DataReceivedEventArgs? args)
    {
        if (encoder.State == EncodingState.Error)
        {
            // TODO: Handle error
        }

        if (encoder.State == EncodingState.Success)
        {
            VideoEncoder.InfoUpdate -= OnEncoderInfoUpdate;
            VMAFScorer = new VMAFScorer(FFprobePath, InputFilePath, encoder.OutputFilePath);
            VMAFScorer.InfoUpdate += OnScorerInfoUpdate;
            VMAFScorer.Start(FFmpegPath);
        }
    }

    private void OnScorerInfoUpdate(VMAFScorer scorer, DataReceivedEventArgs? args)
    {
        if (scorer.State == EncodingState.Error)
        {
            // TODO: Handle error
        }

        if (scorer.State == EncodingState.Success)
        {
            VMAFScorer.InfoUpdate -= OnScorerInfoUpdate;

            // Scan to see if we found the boundary.
            CrfToVmafMaps.Add(new CrfToVMAFMap
            {
                Crf = ThisCrf,
                VmafScore = scorer.VMAFScore,
                FilePath = scorer.DistortedFilePath
            });

            List<CrfToVMAFMap> maps = CrfToVmafMaps.OrderBy(x => x.Crf).ToList();
            for (int i = 1; i < maps.Count; i++)
            {
                if (maps[i].VmafScore < TargetVMAF && maps[i - 1].VmafScore >= TargetVMAF && maps[i].Crf - maps[i - 1].Crf == 1)
                {
                    // Found the boundary. Should also note there's an inverse relationship between CRF and VMAF.
                    // TODO: Write this block. This should end the process as we're done.
                    int t = 8;
                }
            }

            // Did not find boundary. Need to adjust CRF and try again.
            VideoEncoder = new VideoEncoder(FFprobePath, FFmpegPath, InputFilePath);
            VideoEncoder.InfoUpdate += OnEncoderInfoUpdate;

            // Check if we overshot of undershot the target, then binary search our way down.
            // The algorithm in the else blocks don't really work well when the CRF range begins to converge.
            // For example, if high = 39, low = 36, this = 37, the algorithm will pick 37 - 1 = 36, leaving us in a loop.
            if (HighCrf - LowCrf <= 4)
            {
                ThisCrf++;
            }
            else if (scorer.VMAFScore > TargetVMAF)
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

            string tempFile = Path.Combine(_tempDirectory, $"{ThisCrf}-{Guid.NewGuid()}{Path.GetExtension(InputFilePath)}");
            string arguments = $"{FFmpegArguments} -c:v {(H265 ? "libx265" : "libx264")} -crf {ThisCrf}";
            VideoEncoder.Start(arguments, tempFile);
        }
    }

    private static int SuggestNextCrf(int thisCrf, int targetCrf)
    {
        // I'm just guessing!

        // Sign is positive if our CRF needs to go up, otherwise negative.
        int sign = Math.Sign(targetCrf - thisCrf);
        int distance = Math.Abs(targetCrf - thisCrf);

        if (thisCrf == targetCrf)
            return thisCrf;

        if (distance <= 3)
        {
            return sign;
        }

        return sign * 2;
    }
}