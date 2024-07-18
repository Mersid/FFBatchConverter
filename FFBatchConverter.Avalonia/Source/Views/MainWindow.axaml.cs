using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using FFBatchConverter.Avalonia.ViewModels;

namespace FFBatchConverter.Avalonia.Views;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();


        AddHandler(DragDrop.DropEvent, Drop);

    }

    private MainWindowViewModel ViewModel
    {
        get
        {
            Debug.Assert(DataContext is MainWindowViewModel, "DataContext is MainWindowViewModel");
            return DataContext as MainWindowViewModel;
        }
    }

    public async void AddFilesButtonClicked(object? sender, RoutedEventArgs routedEventArgs)
    {
        TopLevel? topLevel = GetTopLevel(this);
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
        TopLevel? topLevel = GetTopLevel(this);
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
}