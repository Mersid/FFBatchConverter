using ReactiveUI;
using ReactiveUI.Fody.Helpers;

namespace FFBatchConverter.Avalonia.ViewModels;

public class TaskTypeBatchVMAFTargetEncoderViewModel : ReactiveObject
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
    public string TargetVMAF { get; set; } = "86";

    [Reactive]
    public string Arguments { get; set; } = "-c:a aac";

    /// <summary>
    /// True if encoding is currently in progress.
    /// </summary>
    [Reactive]
    public bool Encoding { get; set; }

    public TaskTypeBatchVMAFTargetEncoderViewModel(TaskCreateViewModel parent)
    {
        Parent = parent;
    }
}