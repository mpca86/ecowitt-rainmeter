namespace EcowittWeather.Core.Models;

/// <summary>
/// Shared unit-normalized weather data for all editions and future widget types.
/// Temperature: °C; wind: m/s; pressure: hPa; rain: mm / mm/h.
/// </summary>
public sealed record WeatherSnapshot
{
    public required DateTimeOffset RetrievedAt { get; init; }
    public DateTimeOffset? ObservedAt { get; init; }
    public required string Source { get; init; }
    public double? OutdoorTemperatureC { get; init; }
    public double? OutdoorHumidityPercent { get; init; }
    public double? IndoorTemperatureC { get; init; }
    public double? IndoorHumidityPercent { get; init; }
    public double? RelativePressureHpa { get; init; }
    public double? AbsolutePressureHpa { get; init; }
    public double? WindSpeedMs { get; init; }
    public double? WindGustMs { get; init; }
    public double? WindDirectionDegrees { get; init; }
    public double? RainRateMmH { get; init; }
    public double? DailyRainMm { get; init; }
    public double? SolarWattsM2 { get; init; }
    public double? UvIndex { get; init; }
    public IReadOnlyList<SensorReading> Sensors { get; init; } = [];
}

public sealed record SensorReading(
    int Channel,
    double? TemperatureC,
    double? HumidityPercent,
    double? Battery,
    string? BatteryUnit = null);
