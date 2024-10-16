using System;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using FFBatchConverter.Avalonia.ViewModels;
using FFBatchConverter.Avalonia.Views;

namespace FFBatchConverter.Avalonia;

public partial class App : Application
{
    public static App Instance { get; private set; } = null!;

    public SettingsManager SettingsManager { get; } = new SettingsManager();

    /// <summary>
    /// Event that is raised when the video encoders in the application have been regenerated.
    /// This is raised because some encoder settings can only be set at initialization.
    /// </summary>
    public event Action? EncoderRebuilt;

    public override void Initialize()
    {
        Instance = this;

        SettingsManager.SettingsChanged += UpdateEncoderPaths;
        UpdateEncoderPaths(SettingsManager.Settings);

        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            desktop.MainWindow = new MainWindow
            {
                DataContext = new MainWindowViewModel(),
            };
        }

        base.OnFrameworkInitializationCompleted();
    }

    private void UpdateEncoderPaths(Settings settings)
    {
        EncoderRebuilt?.Invoke();
    }
}