namespace EcowittWeather.Infrastructure.Configuration;

public static class ConfigurationPaths
{
    public static string DirectoryPath => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "EcowittWeather", "Desktop");

    public static string PreferencesPath => Path.Combine(DirectoryPath, "settings.json");
    public static string CredentialsPath => Path.Combine(DirectoryPath, "cloud-secrets.dat");
}
