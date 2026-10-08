namespace EcowittWeather.Core.Models;

public enum SourceMode
{
    Cloud,
    Local,
    Auto
}

public sealed record StationProfile
{
    public string Id { get; init; } = "default";
    public string Name { get; init; } = "Moja stanica";
    public SourceMode SourceMode { get; init; } = SourceMode.Cloud;
    public string CloudMac { get; init; } = "";
    public string LocalGatewayHost { get; init; } = "";
}

public sealed record CloudCredentials(string ApplicationKey, string ApiKey)
{
    public bool IsConfigured =>
        !string.IsNullOrWhiteSpace(ApplicationKey) &&
        !string.IsNullOrWhiteSpace(ApiKey);
}
