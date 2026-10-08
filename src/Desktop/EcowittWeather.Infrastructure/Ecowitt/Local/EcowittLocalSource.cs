using System.Net;
using System.Net.Sockets;
using EcowittWeather.Core.Abstractions;
using EcowittWeather.Core.Models;

namespace EcowittWeather.Infrastructure.Ecowitt.Local;

public sealed class EcowittLocalSource(HttpClient http) : IWeatherSource
{
    public async Task<WeatherSnapshot> FetchAsync(
        StationProfile profile, CancellationToken cancellationToken = default)
    {
        var address = GetGatewayUrl(profile.LocalGatewayHost);
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeout.CancelAfter(TimeSpan.FromSeconds(6));

        try
        {
            using var response = await http.GetAsync(
                address, HttpCompletionOption.ResponseHeadersRead, timeout.Token);
            if (!response.IsSuccessStatusCode)
                throw new InvalidDataException($"Local API: HTTP {(int)response.StatusCode}.");

            var json = await response.Content.ReadAsStringAsync(timeout.Token);
            if (json.Length > 1_000_000)
                throw new InvalidDataException("Odpoveď Local API je priveľká.");

            return EcowittLocalParser.Parse(json, DateTimeOffset.UtcNow);
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            throw new TimeoutException("Lokálny gateway neodpovedal v limite 6 sekúnd.");
        }
        catch (HttpRequestException)
        {
            throw new InvalidOperationException(
                "Lokálny gateway je nedostupný. Skontroluj adresu a LAN.");
        }
    }

    /// <summary>
    /// Accept only a LAN host or private IPv4/IPv6 address, optional port.
    /// This avoids using configured Local hosts as arbitrary public HTTP targets.
    /// </summary>
    public static Uri GetGatewayUrl(string host)
    {
        var value = (host ?? "").Trim();
        if (string.IsNullOrWhiteSpace(value))
            throw new InvalidOperationException("Chýba adresa lokálneho gatewaya.");

        if (value.Contains('/') || value.Contains('?') || value.Contains('#') ||
            value.Contains('@') || value.Contains('\\') || value.Any(char.IsWhiteSpace))
            throw new InvalidOperationException(
                "Zadaj iba IP adresu alebo lokálny hostname, voliteľne s portom.");

        if (!Uri.TryCreate("http://" + value + "/get_livedata_info",
            UriKind.Absolute, out var uri) ||
            uri.Scheme != Uri.UriSchemeHttp ||
            uri.Port is < 1 or > 65535 || uri.Host.Length == 0 ||
            !IsLocalHost(uri.Host))
            throw new InvalidOperationException(
                "Zadaj privátnu IP adresu LAN alebo názov lokálneho gatewaya.");

        return uri;
    }

    private static bool IsLocalHost(string host)
    {
        if (IPAddress.TryParse(host, out var ip))
        {
            if (ip.AddressFamily == AddressFamily.InterNetwork)
            {
                var bytes = ip.GetAddressBytes();
                return bytes[0] == 10 ||
                       bytes[0] == 172 && bytes[1] is >= 16 and <= 31 ||
                       bytes[0] == 192 && bytes[1] == 168 ||
                       bytes[0] == 127 ||
                       bytes[0] == 169 && bytes[1] == 254;
            }

            var v6 = ip.GetAddressBytes();
            return IPAddress.IsLoopback(ip) || ip.IsIPv6LinkLocal ||
                (v6[0] & 0xfe) == 0xfc;
        }

        if (host.Equals("localhost", StringComparison.OrdinalIgnoreCase)) return true;

        var domain = host.ToLowerInvariant();
        return !domain.Contains('.') ||
               domain.EndsWith(".local", StringComparison.Ordinal) ||
               domain.EndsWith(".lan", StringComparison.Ordinal) ||
               domain.EndsWith(".home.arpa", StringComparison.Ordinal);
    }
}
