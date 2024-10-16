namespace FFBatchConverter.Avalonia;

public class TaskCreateInfo
{
    /// <summary>
    /// 0 for regular, 1 for scoring, and 2 for target.
    /// </summary>
    public required int SelectionIndex { get; init; }
    public required string FFmpegPath { get; init; }
    public required string FFprobePath { get; init; }
    public required string TempDirectory { get; init; }
}