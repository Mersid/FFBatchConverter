using Avalonia.Controls;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;

namespace FFBatchConverter.Avalonia.ViewModels;

public class MainWindowViewModel : ReactiveObject
{
    [Reactive]
    public UserControl CurrentView { get; set; }
}