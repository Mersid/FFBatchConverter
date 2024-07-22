using System;
using Avalonia.Controls;
using FFBatchConverter.Avalonia.Views;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;

namespace FFBatchConverter.Avalonia.ViewModels;

public class MainWindowViewModel : ReactiveObject
{
    [Reactive]
    public UserControl CurrentView { get; set; }

    private UserControl BatchVideoEncoderView { get; set; }
    private UserControl TestView { get; set; }

    public MainWindowViewModel()
    {
        BatchVideoEncoderView = new BatchVideoEncoderView
        {
            DataContext = new BatchVideoEncoderViewModel()
        };

        TestView = new TestView
        {
            DataContext = new TestViewModel()
        };

        CurrentView = BatchVideoEncoderView;
    }

    public void MenuExit()
    {
        Environment.Exit(0);
    }

    public void MenuNavigateBatchVideoEncoder()
    {
        CurrentView = BatchVideoEncoderView;
    }

    public void MenuNavigateTest()
    {
        CurrentView = TestView;
    }
}