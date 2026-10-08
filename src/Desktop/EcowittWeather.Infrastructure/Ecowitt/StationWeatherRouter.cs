using EcowittWeather.Core.Abstractions;
using EcowittWeather.Core.Models;

namespace EcowittWeather.Infrastructure.Ecowitt;

/// <summary>
/// Per-station API selection. Auto prefers LAN, switches to Cloud after an
/// unavailable/invalid local response, and probes LAN periodically for recovery.
/// A single router is shared by all widgets of the same app process.
/// </summary>
public sealed class StationWeatherRouter(
    IWeatherSource local,
    IWeatherSource cloud,
    Func<DateTimeOffset>? utcNow = null)
{
    private readonly Dictionary<string, DateTimeOffset> _retryLocalAfter = [];
    private readonly Func<DateTimeOffset> _utcNow = utcNow ?? (() => DateTimeOffset.UtcNow);
    public static readonly TimeSpan LocalRetryInterval = TimeSpan.FromMinutes(2);

    public async Task<WeatherSnapshot> FetchAsync(
        StationProfile station,
        CancellationToken cancellationToken = default)
    {
        if (station.SourceMode == SourceMode.Cloud)
            return await cloud.FetchAsync(station, cancellationToken);

        if (station.SourceMode == SourceMode.Local)
            return await local.FetchAsync(station, cancellationToken);

        if (station.SourceMode != SourceMode.Auto)
            throw new InvalidOperationException("Nepodporovaný režim dátového zdroja.");

        // Auto: retry local after 2 minutes if the last LAN attempt failed,
        // rather than delaying every Cloud refresh with a 6-second LAN timeout.
        if (!_retryLocalAfter.TryGetValue(station.Id, out var retryAt) ||
            _utcNow() >= retryAt)
        {
            try
            {
                var reading = await local.FetchAsync(station, cancellationToken);
                _retryLocalAfter.Remove(station.Id);
                return reading;
            }
            catch (Exception) when (!cancellationToken.IsCancellationRequested)
            {
                _retryLocalAfter[station.Id] = _utcNow() + LocalRetryInterval;
            }
        }

        var fallback = await cloud.FetchAsync(station, cancellationToken);
        return fallback with { Source = "Ecowitt Web API (záložný zdroj)" };
    }

    public void Reset() => _retryLocalAfter.Clear();
}
