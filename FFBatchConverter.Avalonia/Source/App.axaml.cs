using System;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using FFBatchConverter.Avalonia.ViewModels;
using FFBatchConverter.Avalonia.Views;
using FFBatchConverter.Controllers;
using FFBatchConverter.Misc;

namespace FFBatchConverter.Avalonia;

public partial class App : Application
{
    public static App Instance { get; private set; } = null!;

    public SettingsManager SettingsManager { get; } = new SettingsManager();

    public BatchVideoEncoder Encoder { get; private set; } = null!;
    public BatchVMAFTargetEncoder VMAFEncoder { get; private set; } = null!;

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
        Encoder = new BatchVideoEncoder
        {
            FFprobePath = settings.ShouldOverrideFFprobePath ? settings.FFprobePath : Helpers.GetFFprobePath(),
            FFmpegPath = settings.ShouldOverrideFFmpegPath ? settings.FFmpegPath : Helpers.GetFFmpegPath(),
        };

        VMAFEncoder = new BatchVMAFTargetEncoder
        {
            FFprobePath = settings.ShouldOverrideFFprobePath ? settings.FFprobePath : Helpers.GetFFprobePath(),
            FFmpegPath = settings.ShouldOverrideFFmpegPath ? settings.FFmpegPath : Helpers.GetFFmpegPath(),
        };

        EncoderRebuilt?.Invoke();
    }
}