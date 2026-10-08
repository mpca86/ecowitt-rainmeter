using EcowittWeather.Core.Models;

namespace EcowittWeather.Infrastructure.Configuration;

/// <summary>
/// Non-secret local preferences. A future release will support multiple stations.
/// Widgets are independent windows, but currently share the single Cloud profile.
/// </summary>
public sealed class AppSettings
{
    public StationProfile Profile { get; set; } = new();
    public int RefreshSeconds { get; set; } = 60;
    public Dictionary<int, string> SensorAliases { get; set; } = [];
    public List<WidgetPlacement> Widgets { get; set; } = [new()];

    public void Normalize()
    {
        Profile ??= new StationProfile();
        SensorAliases ??= [];
        Widgets ??= [];
        RefreshSeconds = Math.Clamp(RefreshSeconds, 30, 3600);
    }
}

public sealed class WidgetPlacement
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public double? Left { get; set; }
    public double? Top { get; set; }
    public bool AlwaysOnTop { get; set; }
    public string Template { get; set; } = "CurrentWeather";
}
