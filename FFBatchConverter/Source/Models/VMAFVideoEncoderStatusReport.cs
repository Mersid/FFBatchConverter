using FFBatchConverter.Encoders;
using FFBatchConverter.Misc;

namespace FFBatchConverter.Models;

public class VMAFVideoEncoderStatusReport
{
    public required VMAFVideoEncoder Encoder { get; init; }
    public required EncodingState State { get; init; }
    public required double CurrentDuration { get; init; }
    public required double VMAFScore { get; init; }
}