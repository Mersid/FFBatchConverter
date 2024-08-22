using FFBatchConverter.Encoders;
using FFBatchConverter.Misc;
using FFBatchConverter.Tokens;

namespace FFBatchConverter.Models;

public class VideoEncoderStatusReport
{
    public required VideoEncoderToken Token { get; init; }
    public required EncodingState State { get; init; }
    public required string InputFilePath { get; init; }
    /// <summary>
    /// For input file, in bytes.
    /// </summary>
    public required long FileSize { get; init; }
    public required double Duration { get; init; }
    public required double CurrentDuration { get; init; }
}