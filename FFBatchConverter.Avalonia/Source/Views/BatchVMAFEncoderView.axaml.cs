﻿using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using FFBatchConverter.Avalonia.ViewModels;

namespace FFBatchConverter.Avalonia.Views;

public partial class BatchVMAFEncoderView : UserControl
{
    public BatchVMAFEncoderView()
    {
        InitializeComponent();

        AddHandler(DragDrop.DropEvent, Drop);
    }

    private BatchVMAFEncoderViewModel ViewModel
    {
        get
        {
            Debug.Assert(DataContext is BatchVMAFEncoderViewModel, "DataContext is BatchVMAFEncoderViewModel");
            return (BatchVMAFEncoderViewModel)DataContext;
        }
    }

    public async void AddFilesButtonClicked(object? sender, RoutedEventArgs routedEventArgs)
    {
        TopLevel? topLevel = TopLevel.GetTopLevel(this);
        Debug.Assert(topLevel != null, nameof(topLevel) + " != null");

        IReadOnlyList<IStorageFile> files = await topLevel.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = "Select files to encode",
            AllowMultiple = true
        });

        ViewModel.AddFiles(files.Select(t => t.Path.LocalPath));
    }

    private async void AddFoldersButtonClicked(object? sender, RoutedEventArgs e)
    {
        TopLevel? topLevel = TopLevel.GetTopLevel(this);
        Debug.Assert(topLevel != null, nameof(topLevel) + " != null");

        IReadOnlyList<IStorageFolder> folders = await topLevel.StorageProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions
        {
            Title = "Select folders to encode",
            AllowMultiple = true
        });

        ViewModel.AddFiles(folders.Select(t => t.Path.LocalPath));
    }

    private void Drop(object? sender, DragEventArgs e)
    {
        IEnumerable<IStorageItem>? data = e.Data.GetFiles();

        if (data is null)
            return;

        IEnumerable<string> files = data.Select(t => t.Path.LocalPath);
        ViewModel.AddFiles(files);
    }

    private void CopyLogsMenuItemClicked(object? sender, RoutedEventArgs e)
    {
        // Item can be null if no item is selected (we start and right click on header)
        if (DataGrid.SelectedItem is not VMAFEncoderTableRow item)
            return;

        TopLevel? topLevel = TopLevel.GetTopLevel(this);
        Debug.Assert(topLevel != null, nameof(topLevel) + " != null");
        Debug.Assert(topLevel.Clipboard != null, nameof(topLevel.Clipboard) + " != null");

        topLevel.Clipboard.SetTextAsync(ViewModel.EncoderToRow.Reverse[item].LogString);
    }

    private void OpenLogsEditorMenuItemClicked(object? sender, RoutedEventArgs e)
    {
        List<VMAFEncoderTableRow> items = DataGrid.SelectedItems.Cast<VMAFEncoderTableRow>().ToList();
        foreach (VMAFEncoderTableRow item in items)
        {
            LogHelper.OpenLog(ViewModel.EncoderToRow.Reverse[item].LogString);
        }
    }
}