using System.Net;
using System.Text.Json;
using System.Text.RegularExpressions;
using EcowittWeather.Core.Models;

namespace EcowittWeather.Infrastructure.Ecowitt.Cloud;

/// <summary>
/// Lists weather stations registered to the Ecowitt API account.
/// GET /api/v3/device/list requires Application Key + API Key but no MAC.
/// The returned list is not an inventory of every wireless sensor / LAN device.
/// </summary>
public sealed class EcowittDeviceCatalog(HttpClient client)
{
    public async Task<IReadOnlyList<EcowittDevice>> ListAsync(
        CloudCredentials credentials,
        CancellationToken cancellationToken = default)
    {
        if (!credentials.IsConfigured)
            throw new InvalidOperationException("Zadaj Application Key aj API Key.");

        // Keys are required in the Ecowitt query string. Never log or display this URI.
        var uri = new Uri(
            "https://api.ecowitt.net/api/v3/device/list?application_key=" +
            Uri.EscapeDataString(credentials.ApplicationKey) +
            "&api_key=" + Uri.EscapeDataString(credentials.ApiKey));

        try
        {
            using var response = await client.GetAsync(
                uri, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
            if (response.StatusCode != HttpStatusCode.OK)
                throw new InvalidOperationException(
                    $"Ecowitt zoznam staníc: HTTP {(int)response.StatusCode}.");

            var json = await response.Content.ReadAsStringAsync(cancellationToken);
            return Parse(json);
        }
        catch (HttpRequestException)
        {
            // HttpRequestException.Message might expose the credential-bearing URI.
            throw new InvalidOperationException(
                "Zoznam staníc sa nepodarilo načítať. Skontroluj internetové pripojenie.");
        }
    }

    public static IReadOnlyList<EcowittDevice> Parse(string json)
    {
        using var document = JsonDocument.Parse(json);
        var root = document.RootElement;

        if (!root.TryGetProperty("code", out var code) ||
            !int.TryParse(code.ToString(), out var result) || result != 0)
            throw new InvalidDataException(
                "Ecowitt odmietol požiadavku na zoznam staníc. Skontroluj API kľúče.");

        if (!root.TryGetProperty("data", out var data) ||
            data.ValueKind != JsonValueKind.Object ||
            !data.TryGetProperty("list", out var list) ||
            list.ValueKind != JsonValueKind.Array)
            throw new InvalidDataException("Ecowitt neposlal platný zoznam staníc.");

        var stations = new List<EcowittDevice>();
        foreach (var item in list.EnumerateArray())
        {
            if (item.ValueKind != JsonValueKind.Object) continue;

            // Ecowitt type=1 is a weather station; type=2 may be a camera.
            if (!TryNumber(item, "type", out var type) || type != 1) continue;

            var mac = Value(item, "mac").Replace('-', ':').ToUpperInvariant();
            if (!Regex.IsMatch(mac, @"^([0-9A-F]{2}:){5}[0-9A-F]{2}$"))
                continue;

            var name = Value(item, "name");
            var model = Value(item, "stationtype");
            stations.Add(new EcowittDevice(
                string.IsNullOrWhiteSpace(name) ? "Meteostanica" : name,
                mac,
                model));
        }

        return stations
            .DistinctBy(x => x.Mac, StringComparer.OrdinalIgnoreCase)
            .OrderBy(x => x.Name, StringComparer.CurrentCultureIgnoreCase)
            .ToArray();
    }

    private static string Value(JsonElement element, string key) =>
        element.TryGetProperty(key, out var found) && found.ValueKind == JsonValueKind.String
            ? found.GetString() ?? "" : "";

    private static bool TryNumber(JsonElement element, string key, out int number)
    {
        number = 0;
        return element.TryGetProperty(key, out var value) &&
               int.TryParse(value.ToString(), out number);
    }
}

public sealed record EcowittDevice(string Name, string Mac, string Model)
{
    public string Display => string.IsNullOrWhiteSpace(Model)
        ? $"{Name}  •  {Mac}"
        : $"{Name}  •  {Model}  •  {Mac}";
}
