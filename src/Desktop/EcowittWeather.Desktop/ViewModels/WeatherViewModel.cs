using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.Runtime.CompilerServices;
using EcowittWeather.Core.Models;

namespace EcowittWeather.Desktop.ViewModels;

public sealed class WeatherViewModel : INotifyPropertyChanged
{
    private string _stationName = "METEO CLOUD";
    private string _status = "Pripravujem pripojenie...";
    private string _updated = "";
    private string _outdoor = "—";
    private string _outdoorHumidity = "—";
    private string _indoor = "—";
    private string _wind = "—";
    private string _pressure = "—";
    private string _rain = "—";
    private string _uv = "—";
    private string _solar = "—";

    public event PropertyChangedEventHandler? PropertyChanged;

    public string StationName { get => _stationName; private set => Set(ref _stationName, value); }
    public string Status { get => _status; private set => Set(ref _status, value); }
    public string Updated { get => _updated; private set => Set(ref _updated, value); }
    public string Outdoor { get => _outdoor; private set => Set(ref _outdoor, value); }
    public string OutdoorHumidity { get => _outdoorHumidity; private set => Set(ref _outdoorHumidity, value); }
    public string Indoor { get => _indoor; private set => Set(ref _indoor, value); }
    public string Wind { get => _wind; private set => Set(ref _wind, value); }
    public string Pressure { get => _pressure; private set => Set(ref _pressure, value); }
    public string Rain { get => _rain; private set => Set(ref _rain, value); }
    public string Uv { get => _uv; private set => Set(ref _uv, value); }
    public string Solar { get => _solar; private set => Set(ref _solar, value); }
    public ObservableCollection<SensorDisplay> Sensors { get; } = [];

    public void ShowStatus(string status) => Status = status;

    public void ShowSnapshot(WeatherSnapshot s, string name, IReadOnlyDictionary<int, string> aliases)
    {
        StationName = name.ToUpperInvariant();
        Outdoor = Degrees(s.OutdoorTemperatureC);
        OutdoorHumidity = Percent(s.OutdoorHumidityPercent);
        Indoor = Degrees(s.IndoorTemperatureC) + " / " + Percent(s.IndoorHumidityPercent);
        Wind = Format(s.WindSpeedMs, "0.0", " m/s") + " / " +
               Format(s.WindGustMs, "0.0", " m/s");
        Pressure = Format(s.RelativePressureHpa, "0.0", " hPa");
        Rain = Format(s.RainRateMmH, "0.0", " mm/h") + " / " +
               Format(s.DailyRainMm, "0.0", " mm");
        Uv = Format(s.UvIndex, "0.0", "");
        Solar = Format(s.SolarWattsM2, "0", " W/m²");

        Sensors.Clear();
        foreach (var sensor in s.Sensors.OrderBy(x => x.Channel))
        {
            var label = aliases.TryGetValue(sensor.Channel, out var alias) &&
                        !string.IsNullOrWhiteSpace(alias)
                ? alias : "CH" + sensor.Channel;

            Sensors.Add(new SensorDisplay(
                label,
                Degrees(sensor.TemperatureC) + " / " + Percent(sensor.HumidityPercent)));
        }

        var when = s.ObservedAt?.ToLocalTime().ToString("dd.MM. HH:mm", CultureInfo.CurrentCulture)
                   ?? s.RetrievedAt.ToLocalTime().ToString("dd.MM. HH:mm", CultureInfo.CurrentCulture);
        Updated = "Meranie: " + when;
        Status = s.Source;
    }

    private static string Degrees(double? x) => Format(x, "0.0", " °C");
    private static string Percent(double? x) => Format(x, "0", " %");

    private static string Format(double? value, string format, string suffix) =>
        value is null ? "—" : value.Value.ToString(format, CultureInfo.CurrentCulture) + suffix;

    private void Set(ref string field, string value, [CallerMemberName] string? property = null)
    {
        if (field == value) return;
        field = value;
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(property));
    }
}

public sealed record SensorDisplay(string Name, string Value);
