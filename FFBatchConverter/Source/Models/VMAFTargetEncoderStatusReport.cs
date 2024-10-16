using FFBatchConverter.Misc;
using FFBatchConverter.Tokens;

namespace FFBatchConverter.Models;

public class VMAFTargetEncoderStatusReport
{
    public required VMAFTargetEncoderToken Token { get; init; }
    public required EncodingState State { get; init; }
    public required VMAFVideoEncodingPhase EncodingPhase { get; init; }
    public required string InputFilePath { get; init; }
    /// <summary>
    /// For input file, in bytes.
    /// </summary>
    public required long FileSize { get; init; }
    public required double Duration { get; init; }
    public required double CurrentDuration { get; init; }
    public required int LowCrf { get; init; }
    public required int HighCrf { get; init; }
    public required int ThisCrf { get; init; }
    public required double? LastVMAF { get; init; }
}