namespace FFBatchConverter.Avalonia;

public class Settings
{
    public bool ShouldOverrideFFmpegPath { get; set; }
    public string FFmpegPath { get; set; } = string.Empty;

    public bool ShouldOverrideFFprobePath { get; set; }
    public string FFprobePath { get; set; } = string.Empty;
}