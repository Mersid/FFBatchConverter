using Avalonia.Controls;
using Avalonia.Interactivity;
using FFBatchConverter.Avalonia.ViewModels;

namespace FFBatchConverter.Avalonia.Views;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
    }

    private void MenuSettingsClick(object? sender, RoutedEventArgs e)
    {
        SettingsWindow settingsWindow = new SettingsWindow
        {
            DataContext = new SettingsWindowViewModel()
        };

        settingsWindow.ShowDialog(this);
    }
}