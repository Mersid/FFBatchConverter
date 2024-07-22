using Avalonia.Controls;
using Avalonia.Interactivity;

namespace FFBatchConverter.Avalonia.Views;

public partial class SettingsWindow : Window
{
    public SettingsWindow()
    {
        InitializeComponent();
    }

    public void Close(object? sender, RoutedEventArgs e)
    {
        Close();
    }
}