﻿using System;
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
    private UserControl VMAFVideoEncoderView { get; set; }
    private UserControl VMAFTargetVideoEncoderView { get; set; }

    public MainWindowViewModel()
    {
        BatchVideoEncoderView = new BatchVideoEncoderView
        {
            DataContext = new BatchVideoEncoderViewModel()
        };

        VMAFVideoEncoderView = new BatchVMAFEncoderView()
        {
            DataContext = new BatchVMAFEncoderViewModel()
        };

        VMAFTargetVideoEncoderView = new BatchVMAFTargetEncoderView
        {
            DataContext = new BatchVMAFTargetEncoderViewModel()
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

    public void MenuNavigateVMAFVideoEncoder()
    {
        CurrentView = VMAFVideoEncoderView;
    }

    public void MenuNavigateVMAFTargetVideoEncoder()
    {
        CurrentView = VMAFTargetVideoEncoderView;
    }
}