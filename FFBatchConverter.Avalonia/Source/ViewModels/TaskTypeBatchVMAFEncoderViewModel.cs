using ReactiveUI;
using ReactiveUI.Fody.Helpers;

namespace FFBatchConverter.Avalonia.ViewModels;

public class TaskTypeBatchVMAFEncoderViewModel : ReactiveObject
{
    private TaskCreateViewModel Parent { get; }

    [Reactive]
    public string Concurrency { get; set; } = "1";

    [Reactive]
    public string Subdirectory { get; set; } = "FFBatch";

    [Reactive]
    public string Extension { get; set; } = "mkv";

    /// <summary>
    /// 0 is x265, 1 is x264, since it's by index, and we put x265 at the top.
    /// </summary>
    [Reactive]
    public int EncoderSelection { get; set; } = 0;

    // TODO: Add verification.
    [Reactive]
    public string Crf { get; set; } = "";

    [Reactive]
    public string Arguments { get; set; } = "-c:a aac";

    /// <summary>
    /// True if encoding is currently in progress.
    /// </summary>
    [Reactive]
    public bool Encoding { get; set; }

    public TaskTypeBatchVMAFEncoderViewModel(TaskCreateViewModel parent)
    {
        Parent = parent;
    }
}