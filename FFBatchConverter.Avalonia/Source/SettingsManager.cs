using System.IO;
using System.Text.Json;

namespace FFBatchConverter.Avalonia;

public class SettingsManager
{
    private string SettingsPath { get; } = "settings.json";
    public Settings Settings { get; private set; } = new Settings();

    public SettingsManager()
    {
        LoadSettings();
    }

    public void LoadSettings()
    {
        if (File.Exists(SettingsPath))
        {
            string json = File.ReadAllText(SettingsPath);
            Settings = JsonSerializer.Deserialize(json, SourceGenerationContext.Default.Settings) ?? Settings;
        }
    }

    public void SaveSettings()
    {
        string json = JsonSerializer.Serialize(Settings, SourceGenerationContext.Default.Settings);
        File.WriteAllText(SettingsPath, json);
    }
}