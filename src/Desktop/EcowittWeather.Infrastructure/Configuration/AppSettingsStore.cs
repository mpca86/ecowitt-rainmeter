using System.Text;
using System.Text.Json;

namespace EcowittWeather.Infrastructure.Configuration;

/// <summary>Stores preferences only; API keys must never go into settings.json.</summary>
public sealed class AppSettingsStore
{
    private static readonly JsonSerializerOptions Options = new()
    {
        WriteIndented = true,
        PropertyNameCaseInsensitive = true
    };

    public AppSettings Load()
    {
        if (!File.Exists(ConfigurationPaths.PreferencesPath))
            return new AppSettings();

        var settings = JsonSerializer.Deserialize<AppSettings>(
            File.ReadAllText(ConfigurationPaths.PreferencesPath, Encoding.UTF8), Options)
            ?? new AppSettings();

        settings.Normalize();
        return settings;
    }

    public void Save(AppSettings settings)
    {
        settings.Normalize();
        Directory.CreateDirectory(ConfigurationPaths.DirectoryPath);
        var json = JsonSerializer.Serialize(settings, Options);
        var temp = ConfigurationPaths.PreferencesPath + ".tmp";
        File.WriteAllText(temp, json, new UTF8Encoding(false));
        File.Move(temp, ConfigurationPaths.PreferencesPath, true);
    }
}
