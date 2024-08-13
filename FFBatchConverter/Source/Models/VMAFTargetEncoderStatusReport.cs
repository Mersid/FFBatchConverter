using FFBatchConverter.Encoders;
using FFBatchConverter.Misc;

namespace FFBatchConverter.Models;

public class VMAFTargetEncoderStatusReport
{
    public required VMAFTargetVideoEncoder Encoder { get; init; }
    public required EncodingState State { get; init; }
    public required double CurrentDuration { get; init; }
    public required int LowCrf { get; init; }
    public required int HighCrf { get; init; }
    public required int ThisCrf { get; init; }
    public required double? LastVMAF { get; init; }
}