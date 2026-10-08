using System.Text.Json.Serialization;
using EcowittWeather.Core.Models;

namespace EcowittWeather.Infrastructure.Configuration;

/// <summary>
/// Profiles own stations and sensor aliases. Every widget binds to one ProfileId.
/// Legacy Profile and SensorAliases properties are read once for beta preview migration.
/// API keys are stored separately via DPAPI and never serialized here.
/// </summary>
public sealed class AppSettings
{
    public List<StationProfile> Profiles { get; set; } = [];
    public int RefreshSeconds { get; set; } = 60;
    public List<WidgetPlacement> Widgets { get; set; } = [new()];

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public StationProfile? Profile { get; set; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public Dictionary<int, string>? SensorAliases { get; set; }

    public void Normalize()
    {
        Profiles ??= [];
        Widgets ??= [];
        RefreshSeconds = Math.Clamp(RefreshSeconds, 30, 3600);

        // Upgrade old single-station settings. Keep its MAC, name and labels.
        if (Profiles.Count == 0)
        {
            var migrated = Profile ?? new StationProfile();
            Profiles.Add(migrated with
            {
                Id = string.IsNullOrWhiteSpace(migrated.Id)
                    ? Guid.NewGuid().ToString("N") : migrated.Id,
                SensorAliases = SensorAliases is { Count: > 0 }
                    ? new Dictionary<int, string>(SensorAliases)
                    : new Dictionary<int, string>(migrated.SensorAliases ?? [])
            });
        }

        var known = new HashSet<string>(StringComparer.Ordinal);
        for (var i = 0; i < Profiles.Count; i++)
        {
            var profile = Profiles[i];
            var id = profile.Id;
            if (string.IsNullOrWhiteSpace(id) || !known.Add(id))
            {
                id = Guid.NewGuid().ToString("N");
                known.Add(id);
            }

            Profiles[i] = profile with
            {
                Id = id,
                Name = string.IsNullOrWhiteSpace(profile.Name) ? "Moja stanica" : profile.Name,
                SensorAliases = profile.SensorAliases ?? []
            };
        }

        var validIds = new HashSet<string>(Profiles.Select(x => x.Id), StringComparer.Ordinal);
        foreach (var widget in Widgets)
        {
            if (string.IsNullOrWhiteSpace(widget.Id))
                widget.Id = Guid.NewGuid().ToString("N");
            if (string.IsNullOrWhiteSpace(widget.ProfileId) || !validIds.Contains(widget.ProfileId))
                widget.ProfileId = Profiles[0].Id;
        }

        Profile = null;
        SensorAliases = null;
    }
}

public sealed class WidgetPlacement
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public string ProfileId { get; set; } = "";
    public double? Left { get; set; }
    public double? Top { get; set; }
    public bool AlwaysOnTop { get; set; }
    public string Template { get; set; } = "CurrentWeather";
}
