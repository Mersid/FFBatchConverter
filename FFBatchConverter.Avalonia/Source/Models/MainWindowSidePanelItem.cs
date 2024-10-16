using Avalonia.Controls;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;

namespace FFBatchConverter.Avalonia.Models;

public class MainWindowSidePanelItem : ReactiveObject
{
    [Reactive]
    public required string Name { get; set; }

    [Reactive]
    public required UserControl View { get; set; }
}