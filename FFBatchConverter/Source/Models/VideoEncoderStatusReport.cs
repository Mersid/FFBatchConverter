using FFBatchConverter.Encoders;
using FFBatchConverter.Misc;

namespace FFBatchConverter.Models;

public class VideoEncoderStatusReport
{
    public required VideoEncoder Encoder { get; init; }
    public required EncodingState State { get; init; }
    public required double CurrentDuration { get; init; }
}