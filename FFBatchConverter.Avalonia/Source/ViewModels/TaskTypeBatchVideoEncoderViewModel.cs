using System.Collections.ObjectModel;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;

namespace FFBatchConverter.Avalonia.ViewModels;

public class TaskTypeBatchVideoEncoderViewModel : ReactiveObject
{
    private TaskCreateViewModel Parent { get; }

    public ObservableCollection<EncoderTableRow> TableRows { get; set; } = [];

    [Reactive]
    public string Concurrency { get; set; } = "1";

    [Reactive]
    public string Subdirectory { get; set; } = "FFBatch";

    [Reactive]
    public string Extension { get; set; } = "mkv";

    [Reactive]
    public string Arguments { get; set; } = "-c:v libx265 -c:a aac";

    /// <summary>
    /// True if encoding is currently in progress.
    /// </summary>
    [Reactive]
    public bool Encoding { get; set; }

    public TaskTypeBatchVideoEncoderViewModel(TaskCreateViewModel parent)
    {
        Parent = parent;
    }
}