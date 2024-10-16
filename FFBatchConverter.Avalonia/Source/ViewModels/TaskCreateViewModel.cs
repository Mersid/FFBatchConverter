using System;
using System.IO;
using System.Reactive.Linq;
using FFBatchConverter.Misc;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;

namespace FFBatchConverter.Avalonia.ViewModels;

public class TaskCreateViewModel : ReactiveObject
{
    private MainWindowViewModel Parent { get; }

    [Reactive]
    public string TaskName { get; set; } = "New task";

    [Reactive]
    public int TaskTypeSelection { get; set; } = -1;

    [Reactive] public string FFmpegPath { get; set; } = Helpers.GetFFmpegPath() ?? "FFmpeg not found!";

    [Reactive] public string FFprobePath { get; set; } = Helpers.GetFFprobePath() ?? "FFprobe not found!";

    public bool RequiresTempDirectory => TaskTypeSelection == 2;

    [Reactive] public string TempDirectory { get; set; } = Path.Combine(Path.GetTempPath(), "FFBatchConverter");

    public TaskCreateViewModel(MainWindowViewModel parent)
    {
        Parent = parent;

        // When the task name changes, update the selected task name in the side panel.
        this
            .WhenAnyValue(x => x.TaskName)
            .Skip(1) // Don't set the value on initialization, as the side panel item is not yet created, so it will overwrite the current item. https://stackoverflow.com/questions/29636910/possible-to-ignore-the-initial-value-for-a-reactiveobject
            .Subscribe(x => Parent.RenameSelectedTask(x));

        // RequiresTempDirectory depends on TaskTypeSelection, so if that changes, also notify the computed property.
        this
            .WhenAnyValue(x => x.TaskTypeSelection)
            .Subscribe(_ => this.RaisePropertyChanged(nameof(RequiresTempDirectory)));
    }

    public void CreateTaskClicked()
    {
        TaskCreateInfo info = new TaskCreateInfo
        {
            SelectionIndex = TaskTypeSelection,
            FFmpegPath = FFmpegPath,
            FFprobePath = FFprobePath,
            TempDirectory = TempDirectory
        };

        Parent.CreateEncoderForSelectedTask(info);
    }
}