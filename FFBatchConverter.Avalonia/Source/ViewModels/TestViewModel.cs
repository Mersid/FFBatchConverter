using ReactiveUI;

namespace FFBatchConverter.Avalonia.ViewModels;

public class TestViewModel : ReactiveObject
{
    public void Save()
    {
        App.Instance.SettingsManager.SaveSettings();
        int t = 8;
    }

    public void Load()
    {
        App.Instance.SettingsManager.LoadSettings();
        int y = 8;
    }
}