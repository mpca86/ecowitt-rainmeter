using EcowittWeather.Core.Models;

namespace EcowittWeather.Core.Abstractions;

public interface IWeatherSource
{
    Task<WeatherSnapshot> FetchAsync(
        StationProfile profile,
        CancellationToken cancellationToken = default);
}
