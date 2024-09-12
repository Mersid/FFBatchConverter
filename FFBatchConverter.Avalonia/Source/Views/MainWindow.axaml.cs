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

    private void MenuAboutClick(object? sender, RoutedEventArgs e)
    {
        AboutView aboutView = new AboutView
        {
            DataContext = new AboutViewModel()
        };

        aboutView.ShowDialog(this);
    }
}