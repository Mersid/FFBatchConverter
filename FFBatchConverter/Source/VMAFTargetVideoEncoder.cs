using System.Diagnostics;
using System.Text;

namespace FFBatchConverter;

public class VMAFTargetVideoEncoder
{
    public VMAFVideoEncoder? VideoEncoder { get; private set; }
    public StringBuilder Log { get; } = new StringBuilder();

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
    public VMAFTargetVideoEncoder(string ffProbePath, string ffmpegPath, string inputFilePath)
    {
        FFprobePath = ffProbePath;
        FFmpegPath = ffmpegPath;
        InputFilePath = inputFilePath;

        // Make a video encoder to get the duration.
        VideoEncoder = new VMAFVideoEncoder(ffProbePath, ffmpegPath, inputFilePath);
        Duration = VideoEncoder.Duration;

        Log.AppendLine(VideoEncoder.Log.ToString());
    }

    /// <summary>
    ///
    /// </summary>
    /// <param name="ffmpegArguments">Arguments to pass to ffmpeg. Do not include -c:v (or -vcodec) or -crf flags.</param>
    /// <param name="h265">True to use h265 (-c:v libx265) encoding, false to use h264. This is why we should not pass in -c:v in ffmpegArguments.</param>
    /// <param name="outputFilePath"></param>
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
        string tempFile = Path.Combine(_tempDirectory, $"{ThisCrf}-{Guid.NewGuid()}{Path.GetExtension(OutputFilePath)}");
        string arguments = $"{FFmpegArguments} -c:v {(H265 ? "libx265" : "libx264")} -crf {ThisCrf}";

        VideoEncoder.InfoUpdate += OnEncoderInfoUpdate;

        VideoEncoder.Start(arguments, H265, ThisCrf, tempFile);
        State = EncodingState.Encoding;
    }

    private void OnEncoderInfoUpdate(VMAFVideoEncoder encoder, DataReceivedEventArgs? args)
    {
        Log.AppendLine(args?.Data);

        if (encoder.State == EncodingState.Error)
        {
            // TODO: Handle error
            State = EncodingState.Error;
            Cleanup();
        }

        if (encoder.State == EncodingState.Success)
        {
            VideoEncoder.InfoUpdate -= OnEncoderInfoUpdate;

            // Scan to see if we found the boundary.
            CrfToVmafMaps.Add(new CrfToVMAFMap
            {
                Crf = ThisCrf,
                VmafScore = encoder.VMAFScorer.VMAFScore,
                FilePath = encoder.VMAFScorer.DistortedFilePath
            });

            List<CrfToVMAFMap> maps = CrfToVmafMaps.OrderBy(x => x.Crf).ToList();
            for (int i = 1; i < maps.Count; i++)
            {
                if (maps[i].VmafScore < TargetVMAF && maps[i - 1].VmafScore >= TargetVMAF && maps[i].Crf - maps[i - 1].Crf == 1)
                {
                    // Found the boundary. Should also note there's an inverse relationship between CRF and VMAF.
                    CrfToVMAFMap target = maps[i - 1];

                    File.Copy(target.FilePath, OutputFilePath);
                    State = EncodingState.Success;
                    Cleanup();
                    return;
                }
            }

            // Did not find boundary. Need to adjust CRF and try again.
            VideoEncoder = new VMAFVideoEncoder(FFprobePath, FFmpegPath, InputFilePath);
            VideoEncoder.InfoUpdate += OnEncoderInfoUpdate;

            // Check if we overshot of undershot the target, then binary search our way down.
            // The algorithm in the else blocks don't really work well when the CRF range begins to converge.
            // For example, if high = 39, low = 36, this = 37, the algorithm will pick 37 - 1 = 36, leaving us in a loop.
            if (HighCrf - LowCrf <= 4)
            {
                ThisCrf++;
            }
            else if (encoder.VMAFScorer.VMAFScore > TargetVMAF)
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

            // We shouldn't ever encode a video twice; if we do, the algorithm probably got stuck in a loop.
            if (CrfToVmafMaps.Any(x => x.Crf == ThisCrf))
            {
                Log.AppendLine("Algorithm got stuck in a loop. Aborting.");
                State = EncodingState.Error;
                Cleanup();
                return;
            }

            string tempFile = Path.Combine(_tempDirectory, $"{ThisCrf}-{Guid.NewGuid()}{Path.GetExtension(OutputFilePath)}");
            string arguments = $"{FFmpegArguments} -c:v {(H265 ? "libx265" : "libx264")} -crf {ThisCrf}";
            Log.AppendLine($"Trying CRF {ThisCrf}");
            VideoEncoder.Start(arguments, H265, ThisCrf, tempFile);
        }
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