using System.Globalization;
using System.Text.Json;
using System.Text.RegularExpressions;
using EcowittWeather.Core.Models;

namespace EcowittWeather.Infrastructure.Ecowitt.Cloud;

/// <summary>
/// Parses Ecowitt Web API v3 device/real_time. Never contains API credentials.
/// </summary>
public static partial class EcowittCloudParser
{
    public static WeatherSnapshot Parse(string json, DateTimeOffset retrievedAt)
    {
        using var document = JsonDocument.Parse(json);
        var root = document.RootElement;

        if (!root.TryGetProperty("code", out var code) ||
            !int.TryParse(code.ToString(), out var resultCode) ||
            resultCode != 0)
        {
            throw new InvalidDataException("Ecowitt Cloud API vrátilo neúspešný stav.");
        }

        if (!root.TryGetProperty("data", out var data) || data.ValueKind != JsonValueKind.Object)
        {
            throw new InvalidDataException("Odpoveď Ecowitt Cloud API neobsahuje meteorologické dáta.");
        }

        var sensors = new List<SensorReading>();
        for (var ch = 1; ch <= 8; ch++)
        {
            var section = "temp_and_humidity_ch" + ch;
            var temperature = Read(data, section, "temperature");
            var humidity = Read(data, section, "humidity");
            if (temperature is null && humidity is null)
                continue;

            sensors.Add(new SensorReading(
                ch,
                temperature,
                humidity,
                Read(data, "battery", "temp_humidity_sensor_ch" + ch),
                Unit(data, "battery", "temp_humidity_sensor_ch" + ch)));
        }

        var time = ReadTime(root, data);

        return new WeatherSnapshot
        {
            RetrievedAt = retrievedAt,
            ObservedAt = time,
            Source = "Ecowitt Web API",
            OutdoorTemperatureC = Read(data, "outdoor", "temperature"),
            OutdoorHumidityPercent = Read(data, "outdoor", "humidity"),
            IndoorTemperatureC = Read(data, "indoor", "temperature"),
            IndoorHumidityPercent = Read(data, "indoor", "humidity"),
            RelativePressureHpa = Pressure(data, "relative"),
            AbsolutePressureHpa = Pressure(data, "absolute"),
            WindSpeedMs = Wind(data, "wind_speed"),
            WindGustMs = Wind(data, "wind_gust"),
            WindDirectionDegrees = Read(data, "wind", "wind_direction"),
            RainRateMmH = Rain(data, "rain_rate"),
            DailyRainMm = Rain(data, "daily"),
            SolarWattsM2 = Read(data, "solar_and_uvi", "solar"),
            UvIndex = Read(data, "solar_and_uvi", "uvi"),
            Sensors = sensors
        };
    }

    private static JsonElement? Item(JsonElement data, string section, string name)
    {
        if (!data.TryGetProperty(section, out var block) || block.ValueKind != JsonValueKind.Object)
            return null;
        if (!block.TryGetProperty(name, out var item))
            return null;
        return item;
    }

    private static double? Read(JsonElement data, string section, string name)
    {
        var item = Item(data, section, name);
        if (item is null) return null;

        var value = item.Value;
        if (value.ValueKind == JsonValueKind.Object)
        {
            if (!value.TryGetProperty("value", out value)) return null;
        }

        if (value.ValueKind is JsonValueKind.Null or JsonValueKind.Undefined)
            return null;

        var text = value.ToString().Replace(',', '.');
        var match = NumberPattern().Match(text);
        return match.Success &&
            double.TryParse(match.Value, NumberStyles.Float, CultureInfo.InvariantCulture, out var n)
                ? n : null;
    }

    private static string? Unit(JsonElement data, string section, string name)
    {
        var item = Item(data, section, name);
        if (item is null || item.Value.ValueKind != JsonValueKind.Object ||
            !item.Value.TryGetProperty("unit", out var unit)) return null;
        return unit.ToString();
    }

    private static double? Wind(JsonElement data, string name)
    {
        var value = Read(data, "wind", name);
        var unit = Unit(data, "wind", name)?.ToLowerInvariant() ?? "";
        if (value is null) return null;
        if (unit.Contains("km/h") || unit.Contains("kmh")) return value / 3.6;
        if (unit.Contains("mph")) return value * 0.44704;
        if (unit.Contains("knot") || unit is "kt" or "kts") return value * 0.514444;
        return value;
    }

    private static double? Pressure(JsonElement data, string name)
    {
        var value = Read(data, "pressure", name);
        var unit = Unit(data, "pressure", name)?.ToLowerInvariant() ?? "";
        if (value is null) return null;
        if (unit.Contains("inhg") || unit.Contains("in/hg")) return value * 33.8638866667;
        if (unit.Contains("mmhg")) return value * 1.3332239;
        if (unit == "kpa") return value * 10;
        if (unit == "pa") return value / 100;
        return value;
    }

    private static double? Rain(JsonElement data, string name)
    {
        var value = Read(data, "rainfall", name);
        var unit = Unit(data, "rainfall", name)?.ToLowerInvariant() ?? "";
        if (value is null) return null;
        if (unit.Contains("in") && !unit.Contains("min")) return value * 25.4;
        return value;
    }

    private static DateTimeOffset? ReadTime(JsonElement root, JsonElement data)
    {
        long timestamp = 0;
        if (root.TryGetProperty("time", out var raw))
            long.TryParse(raw.ToString(), out timestamp);

        foreach (var block in data.EnumerateObject())
        {
            if (block.Value.ValueKind != JsonValueKind.Object) continue;
            foreach (var item in block.Value.EnumerateObject())
            {
                if (item.Value.ValueKind != JsonValueKind.Object ||
                    !item.Value.TryGetProperty("time", out var t) ||
                    !long.TryParse(t.ToString(), out var candidate))
                    continue;

                if (candidate > timestamp) timestamp = candidate;
            }
        }

        if (timestamp <= 0) return null;

        try
        {
            return timestamp > 20_000_000_000L
                ? DateTimeOffset.FromUnixTimeMilliseconds(timestamp)
                : DateTimeOffset.FromUnixTimeSeconds(timestamp);
        }
        catch (ArgumentOutOfRangeException)
        {
            return null;
        }
    }

    [GeneratedRegex(@"[-+]?\d+(?:\.\d+)?", RegexOptions.CultureInvariant)]
    private static partial Regex NumberPattern();
}
