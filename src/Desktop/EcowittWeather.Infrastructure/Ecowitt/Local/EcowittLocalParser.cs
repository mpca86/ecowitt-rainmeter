using System.Globalization;
using System.Text.Json;
using System.Text.RegularExpressions;
using EcowittWeather.Core.Models;

namespace EcowittWeather.Infrastructure.Ecowitt.Local;

/// <summary>
/// Normalizes /get_livedata_info data. Follows the same field registry as the
/// existing Rainmeter Local API parser (common_list, rain, wh25, ch_aisle).
/// </summary>
public static partial class EcowittLocalParser
{
    public static WeatherSnapshot Parse(string json, DateTimeOffset retrievedAt)
    {
        using var document = JsonDocument.Parse(json);
        var data = document.RootElement;
        if (data.ValueKind != JsonValueKind.Object)
            throw new InvalidDataException("Lokálne API nevrátilo JSON objekt.");

        var sensors = new List<SensorReading>();
        if (data.TryGetProperty("ch_aisle", out var channels) &&
            channels.ValueKind == JsonValueKind.Array)
        {
            foreach (var item in channels.EnumerateArray())
            {
                if (item.ValueKind != JsonValueKind.Object ||
                    !item.TryGetProperty("channel", out var indexElement) ||
                    !int.TryParse(indexElement.ToString(), out var channel) ||
                    channel is < 1 or > 8 ||
                    sensors.Any(s => s.Channel == channel))
                    continue;

                var temperature = ConvertTemperature(Field(item, "temp"), StringField(item, "unit"));
                var humidity = Field(item, "humidity");
                if (temperature is null && humidity is null) continue;

                sensors.Add(new SensorReading(
                    channel, temperature, humidity, Field(item, "battery"), null));
            }
        }

        var result = new WeatherSnapshot
        {
            RetrievedAt = retrievedAt,
            Source = "Ecowitt Local API",
            OutdoorTemperatureC = ConvertTemperature(Common(data, "0x02"), CommonUnit(data, "0x02")),
            OutdoorHumidityPercent = Common(data, "0x07"),
            IndoorTemperatureC = ConvertTemperature(Station(data, "intemp"), StationUnit(data)),
            IndoorHumidityPercent = Station(data, "inhumi"),
            RelativePressureHpa = StationPressure(data, "rel"),
            AbsolutePressureHpa = StationPressure(data, "abs"),
            WindSpeedMs = ConvertWind(Common(data, "0x0B"), CommonUnit(data, "0x0B")),
            WindGustMs = ConvertWind(Common(data, "0x19"), CommonUnit(data, "0x19")),
            WindDirectionDegrees = Common(data, "0x0A"),
            RainRateMmH = ConvertRain(Rain(data, "0x0E"), RainUnit(data, "0x0E")),
            DailyRainMm = ConvertRain(Rain(data, "0x10"), RainUnit(data, "0x10")),
            SolarWattsM2 = Common(data, "0x15"),
            UvIndex = Common(data, "0x17"),
            Sensors = sensors.OrderBy(s => s.Channel).ToArray()
        };

        // An unrelated JSON response must not masquerade as an available
        // local station; Auto mode should fall back to Web instead.
        if (result.OutdoorTemperatureC is null &&
            result.IndoorTemperatureC is null &&
            result.RelativePressureHpa is null &&
            result.WindSpeedMs is null &&
            result.Sensors.Count == 0)
            throw new InvalidDataException("Gateway nevrátil rozpoznateľné meteorologické merania.");

        return result;
    }

    private static JsonElement? Item(JsonElement root, string section, string hexId)
    {
        if (!root.TryGetProperty(section, out var values) || values.ValueKind != JsonValueKind.Array)
            return null;

        var idNumber = Convert.ToInt32(hexId[2..], 16);
        foreach (var element in values.EnumerateArray())
        {
            if (element.ValueKind != JsonValueKind.Object ||
                !element.TryGetProperty("id", out var id))
                continue;
            var s = id.ToString();
            var parsed = s.StartsWith("0x", StringComparison.OrdinalIgnoreCase)
                ? int.TryParse(s[2..], NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var h)
                    ? h : -1
                : int.TryParse(s, NumberStyles.Integer, CultureInfo.InvariantCulture, out var d)
                    ? d : -1;
            if (parsed == idNumber) return element;
        }
        return null;
    }

    private static string? ItemUnit(JsonElement? item)
    {
        if (item is null) return null;
        var e = item.Value;
        var unit = StringField(e, "unit");
        var val = StringField(e, "val");
        return string.IsNullOrWhiteSpace(unit) ? val : unit;
    }

    private static string? StringField(JsonElement item, string field) =>
        item.TryGetProperty(field, out var value) ? value.ToString() : null;

    private static double? Field(JsonElement item, string field)
    {
        var raw = StringField(item, field);
        if (string.IsNullOrWhiteSpace(raw)) return null;
        var match = NumberPattern().Match(raw);
        return match.Success &&
            double.TryParse(match.Value.Replace(',', '.'), NumberStyles.Float,
                CultureInfo.InvariantCulture, out var value) && double.IsFinite(value)
            ? value : null;
    }

    private static double? Common(JsonElement data, string id) =>
        Item(data, "common_list", id) is { } found ? Field(found, "val") : null;

    private static string? CommonUnit(JsonElement data, string id) =>
        ItemUnit(Item(data, "common_list", id));

    private static double? Rain(JsonElement data, string id) =>
        Item(data, "rain", id) is { } found ? Field(found, "val") : null;

    private static string? RainUnit(JsonElement data, string id) => ItemUnit(Item(data, "rain", id));

    private static JsonElement? StationBlock(JsonElement data)
    {
        foreach (var section in new[] { "wh25", "wh32" })
        {
            if (data.TryGetProperty(section, out var array) &&
                array.ValueKind == JsonValueKind.Array &&
                array.GetArrayLength() > 0 &&
                array[0].ValueKind == JsonValueKind.Object)
                return array[0];
        }
        return null;
    }

    private static double? Station(JsonElement data, string field) =>
        StationBlock(data) is { } block ? Field(block, field) : null;

    private static string? StationUnit(JsonElement data) =>
        StationBlock(data) is { } block ? StringField(block, "unit") : null;

    private static double? ConvertTemperature(double? value, string? unit)
    {
        if (value is null) return null;
        var text = (unit ?? "").Trim().ToUpperInvariant();
        return text == "F" || text.Contains("°F") || text.Contains("ºF") ||
               text.EndsWith(" F", StringComparison.Ordinal)
            ? (value - 32) * (5.0 / 9.0)
            : value;
    }

    private static double? StationPressure(JsonElement data, string field)
    {
        var block = StationBlock(data);
        var value = block is { } b ? Field(b, field) : null;
        var unit = block is { } v ? StringField(v, field) : null;
        return ConvertPressure(value, unit);
    }

    private static double? ConvertWind(double? value, string? unit)
    {
        if (value is null) return null;
        var text = (unit ?? "").ToLowerInvariant();
        if (text.Contains("km/h") || text.Contains("kmh")) return value / 3.6;
        if (text.Contains("mph")) return value * 0.44704;
        if (text.Contains("knot") || text.Contains("kts")) return value * 0.514444;
        return value;
    }

    private static double? ConvertRain(double? value, string? unit)
    {
        if (value is null) return null;
        var text = (unit ?? "").ToLowerInvariant();
        // Local API rainfall strings commonly use "0.0 mm/Hr".
        return text.Contains("in/") || text.Contains("inch") || text.EndsWith(" in")
            ? value * 25.4 : value;
    }

    private static double? ConvertPressure(double? value, string? unit)
    {
        if (value is null) return null;
        var text = (unit ?? "").ToLowerInvariant();
        if (text.Contains("inhg")) return value * 33.8638866667;
        if (text.Contains("mmhg")) return value * 1.3332239;
        if (text.Contains("kpa")) return value * 10;
        if (text.Contains(" hpa") || text.EndsWith("hpa")) return value;
        if (text.Contains(" pa") || text.EndsWith(" pa")) return value / 100;
        return value;
    }

    [GeneratedRegex(@"[-+]?\d+(?:[.,]\d+)?", RegexOptions.CultureInvariant)]
    private static partial Regex NumberPattern();
}
