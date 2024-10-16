using System;
using System.Collections.ObjectModel;
using Avalonia.Controls;
using FFBatchConverter.Avalonia.Models;
using FFBatchConverter.Avalonia.Views;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;

namespace FFBatchConverter.Avalonia.ViewModels;

public class MainWindowViewModel : ReactiveObject
{
    [Reactive]
    public UserControl? CurrentView { get; set; }

    public ObservableCollection<MainWindowSidePanelItem> SidePanelItems { get; } = [];

    [Reactive]
    public int SelectedSidePanelIndex { get; set; } = -1;

    public MainWindowViewModel()
    {

        // If SelectedSidePanelIndex changes, update the current view to match.
        this
            .WhenAnyValue(x => x.SelectedSidePanelIndex)
            .Subscribe(x =>
            {
                if (x >= 0 && SidePanelItems.Count > 0)
                {
                    CurrentView = SidePanelItems[x].View;
                }
                else
                {
                    CurrentView = null;
                }
            });
    }

    public void MenuExit()
    {
        Environment.Exit(0);
    }

    public void MenuNavigateBatchVideoEncoder()
    {
        // CurrentView = BatchVideoEncoderView;
    }

    public void MenuNavigateVMAFVideoEncoder()
    {
        // CurrentView = VMAFVideoEncoderView;
    }

    public void MenuNavigateVMAFTargetVideoEncoder()
    {
        // CurrentView = VMAFTargetVideoEncoderView;
    }

    public void AddButtonClicked()
    {
        SidePanelItems.Add(new MainWindowSidePanelItem
        {
            Name = "New task",
            View = new TaskCreateView
            {
                DataContext = new TaskCreateViewModel(this)
            }
        });
    }

    public void RemoveButtonClicked()
    {
        if (SidePanelItems.Count > 0 && SelectedSidePanelIndex >= 0)
        {
            SidePanelItems.RemoveAt(SelectedSidePanelIndex);
        }
    }

    public void RenameSelectedTask(string newName)
    {
        // When the new task is initialized, this gets run as part of its initialization.
        // Check this so we don't get NPE when initializing the task.
        if (SidePanelItems.Count > 0 && SelectedSidePanelIndex >= 0)
            SidePanelItems[SelectedSidePanelIndex].Name = newName;
    }

    /// <summary>
    /// Receives information from the task creation view and creates a new task.
    /// </summary>
    public void CreateEncoderForSelectedTask(TaskCreateInfo createInfo)
    {
        UserControl view = createInfo.SelectionIndex switch
        {
            0 => new BatchVideoEncoderView { DataContext = new BatchVideoEncoderViewModel(createInfo) },
            1 => new BatchVMAFEncoderView { DataContext = new BatchVMAFEncoderViewModel(createInfo) },
            2 => new BatchVMAFTargetEncoderView { DataContext = new BatchVMAFTargetEncoderViewModel(createInfo) },
            _ => throw new ArgumentOutOfRangeException()
        };

        SidePanelItems[SelectedSidePanelIndex].View = view;
        CurrentView = view;
    }
}