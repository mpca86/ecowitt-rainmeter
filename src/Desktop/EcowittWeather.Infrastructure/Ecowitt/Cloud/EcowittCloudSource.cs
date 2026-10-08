using System.Net;
using System.Text.RegularExpressions;
using EcowittWeather.Core.Abstractions;
using EcowittWeather.Core.Models;

namespace EcowittWeather.Infrastructure.Ecowitt.Cloud;

/// <summary>Ecowitt Web API v3 source. Deliberately does not log URLs containing keys.</summary>
public sealed class EcowittCloudSource(HttpClient httpClient, CloudCredentials credentials) : IWeatherSource
{
    public async Task<WeatherSnapshot> FetchAsync(
        StationProfile profile,
        CancellationToken cancellationToken = default)
    {
        if (!credentials.IsConfigured)
            throw new InvalidOperationException("V Nastaveniach chýba Application Key alebo API Key.");

        var mac = profile.CloudMac.Replace('-', ':').Trim();
        if (!Regex.IsMatch(mac, @"^([0-9a-fA-F]{2}:){5}[0-9a-fA-F]{2}$"))
            throw new InvalidOperationException("Zadaj platnú MAC adresu stanice (AA:BB:CC:DD:EE:FF).");

        string Enc(string value) => Uri.EscapeDataString(value);
        var query =
            "application_key=" + Enc(credentials.ApplicationKey) +
            "&api_key=" + Enc(credentials.ApiKey) +
            "&mac=" + Enc(mac) +
            "&call_back=all&temp_unitid=1&pressure_unitid=3" +
            "&wind_speed_unitid=7&rainfall_unitid=12&solar_irradiance_unitid=16";

        // HTTPS is fixed to the trusted Ecowitt API host. Never print this URL:
        // Ecowitt authenticates with credentials in query parameters.
        var endpoint = new Uri("https://api.ecowitt.net/api/v3/device/real_time?" + query);

        try
        {
            using var response = await httpClient.GetAsync(
                endpoint, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
            if (response.StatusCode != HttpStatusCode.OK)
                throw new InvalidOperationException(
                    $"Ecowitt Cloud API odpovedalo stavom HTTP {(int)response.StatusCode}.");

            var body = await response.Content.ReadAsStringAsync(cancellationToken);
            return EcowittCloudParser.Parse(body, DateTimeOffset.UtcNow);
        }
        catch (HttpRequestException)
        {
            // Do not expose HttpRequestException.Message: it can contain the query URL.
            throw new InvalidOperationException("Nepodarilo sa pripojiť k Ecowitt Cloud API.");
        }
    }
}
