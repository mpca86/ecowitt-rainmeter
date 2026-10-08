using System.Net;
using System.Security.Cryptography;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace EcowittWeather.Infrastructure.Updates;

public sealed record DesktopRelease(
    string Tag,
    string Version,
    string Notes,
    Uri PackageUrl,
    Uri ChecksumUrl);

/// <summary>
/// Desktop-only prerelease channel. Rainmeter tags (v1.11...) are ignored.
/// GitHub public releases never require Ecowitt API credentials.
/// </summary>
public sealed partial class DesktopUpdateService(HttpClient http)
{
    public const string Repository = "mpca86/ecowitt-rainmeter";
    public const string ReleasePrefix = "desktop-v";
    private const long MaxArchiveBytes = 250L * 1024L * 1024L;

    public async Task<DesktopRelease?> FindUpdateAsync(
        string currentVersion, CancellationToken cancellationToken = default)
    {
        using var request = new HttpRequestMessage(
            HttpMethod.Get, $"https://api.github.com/repos/{Repository}/releases?per_page=40");
        request.Headers.UserAgent.ParseAdd("EcowittWeather-Desktop/0.2");
        request.Headers.Accept.ParseAdd("application/vnd.github+json");

        using var response = await http.SendAsync(request, cancellationToken);
        if (!response.IsSuccessStatusCode)
            throw new InvalidOperationException(
                $"Kontrola aktualizácií: GitHub HTTP {(int)response.StatusCode}.");

        var json = await response.Content.ReadAsStringAsync(cancellationToken);
        return SelectUpdate(json, currentVersion);
    }

    public static DesktopRelease? SelectUpdate(string githubJson, string installedVersion)
    {
        if (!TryVersion(installedVersion, out var current)) return null;

        using var document = JsonDocument.Parse(githubJson);
        if (document.RootElement.ValueKind != JsonValueKind.Array) return null;

        DesktopRelease? selected = null;
        (int major, int minor, int patch, int prerelease) newest = current;

        foreach (var release in document.RootElement.EnumerateArray())
        {
            if (release.ValueKind != JsonValueKind.Object ||
                !release.TryGetProperty("tag_name", out var tagElement) ||
                release.TryGetProperty("draft", out var draft) && draft.ValueKind == JsonValueKind.True)
                continue;

            var tag = tagElement.GetString() ?? "";
            if (!tag.StartsWith(ReleasePrefix, StringComparison.Ordinal)) continue;
            var version = tag[ReleasePrefix.Length..];
            if (!TryVersion(version, out var candidate) || Compare(candidate, newest) <= 0)
                continue;

            var basename = "EcowittWeather-Desktop-v" + version + "-win-x64.zip";
            Uri? zip = null;
            Uri? checksum = null;
            if (!release.TryGetProperty("assets", out var assets) ||
                assets.ValueKind != JsonValueKind.Array) continue;

            foreach (var asset in assets.EnumerateArray())
            {
                if (!asset.TryGetProperty("name", out var nameElement) ||
                    !asset.TryGetProperty("browser_download_url", out var urlElement) ||
                    !Uri.TryCreate(urlElement.GetString(), UriKind.Absolute, out var uri) ||
                    uri.Scheme != Uri.UriSchemeHttps ||
                    !string.Equals(uri.Host, "github.com", StringComparison.OrdinalIgnoreCase))
                    continue;

                var name = nameElement.GetString() ?? "";
                if (name == basename) zip = uri;
                if (name == basename + ".sha256") checksum = uri;
            }
            if (zip is null || checksum is null) continue;
            selected = new DesktopRelease(
                tag, version,
                release.TryGetProperty("body", out var body) ? body.GetString() ?? "" : "",
                zip, checksum);
            newest = candidate;
        }

        return selected;
    }

    public static bool TryVersion(
        string version, out (int major, int minor, int patch, int prerelease) parsed)
    {
        parsed = default;
        var match = VersionPattern().Match(version);
        return match.Success &&
            int.TryParse(match.Groups["a"].Value, out parsed.major) &&
            int.TryParse(match.Groups["b"].Value, out parsed.minor) &&
            int.TryParse(match.Groups["c"].Value, out parsed.patch) &&
            int.TryParse(match.Groups["d"].Value, out parsed.prerelease);
    }

    private static int Compare(
        (int major,int minor,int patch,int prerelease) a,
        (int major,int minor,int patch,int prerelease) b)
    {
        var result = a.major.CompareTo(b.major);
        if (result != 0) return result;
        result = a.minor.CompareTo(b.minor);
        if (result != 0) return result;
        result = a.patch.CompareTo(b.patch);
        return result != 0 ? result : a.prerelease.CompareTo(b.prerelease);
    }

    [GeneratedRegex(@"^(?<a>\d+)\.(?<b>\d+)\.(?<c>\d+)-alpha\.(?<d>\d+)(?:\+.*)?$")]
    private static partial Regex VersionPattern();

    /// <summary>
    /// Download two separately published assets, compare SHA256, then stage
    /// a safely extracted portable directory; never overwrite running binaries.
    /// </summary>
    public async Task<string> DownloadAndStageAsync(
        DesktopRelease release, IProgress<double>? progress = null,
        CancellationToken cancellationToken = default)
    {
        var root = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "EcowittWeather", "Desktop", "Updates");
        Directory.CreateDirectory(root);

        var stagingRoot = Path.Combine(root, release.Tag + "-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(stagingRoot);
        var archivePath = Path.Combine(stagingRoot, "package.zip");
        var payloadPath = Path.Combine(stagingRoot, "payload");

        try
        {
            using var checksumResponse = await http.GetAsync(release.ChecksumUrl, cancellationToken);
            checksumResponse.EnsureSuccessStatusCode();
            var checksumText = await checksumResponse.Content.ReadAsStringAsync(cancellationToken);
            var hashMatch = ShaPattern().Match(checksumText);
            if (!hashMatch.Success)
                throw new InvalidDataException("Release nemá platný súbor SHA-256.");

            var expected = hashMatch.Value;
            using var response = await http.GetAsync(
                release.PackageUrl, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
            response.EnsureSuccessStatusCode();
            if (response.Content.Headers.ContentLength is long length && length > MaxArchiveBytes)
                throw new InvalidDataException("Aktualizačný balík je priveľký.");

            await using (var source = await response.Content.ReadAsStreamAsync(cancellationToken))
            await using (var target = File.Create(archivePath))
            {
                var buffer = new byte[128 * 1024];
                long done = 0;
                int read;
                while ((read = await source.ReadAsync(buffer, cancellationToken)) > 0)
                {
                    done += read;
                    if (done > MaxArchiveBytes)
                        throw new InvalidDataException("Aktualizačný balík je priveľký.");
                    await target.WriteAsync(buffer.AsMemory(0, read), cancellationToken);
                    if (length > 0) progress?.Report((double)done / length);
                }
            }

            await using (var stream = File.OpenRead(archivePath))
            {
                var digest = Convert.ToHexString(await SHA256.HashDataAsync(stream, cancellationToken));
                if (!string.Equals(digest, expected, StringComparison.OrdinalIgnoreCase))
                    throw new InvalidDataException("SHA-256 aktualizačného balíka nesúhlasí.");
            }

            System.IO.Compression.ZipFile.ExtractToDirectory(archivePath, payloadPath);

            var exe = Path.Combine(payloadPath, "EcowittWeather.Desktop.exe");
            if (!File.Exists(exe))
                throw new InvalidDataException("V balíku chýba EcowittWeather.Desktop.exe.");

            return payloadPath;
        }
        catch
        {
            try { Directory.Delete(stagingRoot, recursive: true); } catch { }
            throw;
        }
    }

    [GeneratedRegex(@"\b[0-9A-Fa-f]{64}\b")]
    private static partial Regex ShaPattern();
}
