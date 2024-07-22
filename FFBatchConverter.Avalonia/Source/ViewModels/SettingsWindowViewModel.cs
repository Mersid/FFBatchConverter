using ReactiveUI;
using ReactiveUI.Fody.Helpers;

namespace FFBatchConverter.Avalonia.ViewModels;

public class SettingsWindowViewModel : ReactiveObject
{
    [Reactive] public bool ShouldOverrideFFmpegPath { get; set; }
    [Reactive] public string FFmpegPath { get; set; } = "";
    [Reactive] public string DefaultFFmpegPath { get; set; } = Helpers.GetFFmpegPath() ?? "";

    [Reactive] public bool ShouldOverrideFFprobePath { get; set; }
    [Reactive] public string FFprobePath { get; set; } = "";
    [Reactive] public string DefaultFFprobePath { get; set; } = Helpers.GetFFprobePath() ?? "";

    public SettingsWindowViewModel()
    {
        SettingsManager settingsManager = App.Instance.SettingsManager;
        ShouldOverrideFFmpegPath = settingsManager.Settings.ShouldOverrideFFmpegPath;
        FFmpegPath = settingsManager.Settings.FFmpegPath;
        ShouldOverrideFFprobePath = settingsManager.Settings.ShouldOverrideFFprobePath;
        FFprobePath = settingsManager.Settings.FFprobePath;
    }

    public void Save()
    {
        SettingsManager settingsManager = App.Instance.SettingsManager;
        settingsManager.Settings.ShouldOverrideFFmpegPath = ShouldOverrideFFmpegPath;
        settingsManager.Settings.FFmpegPath = FFmpegPath;
        settingsManager.Settings.ShouldOverrideFFprobePath = ShouldOverrideFFprobePath;
        settingsManager.Settings.FFprobePath = FFprobePath;
        settingsManager.SaveSettings();
    }
}