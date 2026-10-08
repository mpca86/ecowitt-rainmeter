using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using EcowittWeather.Core.Models;
using EcowittWeather.Infrastructure.Configuration;

namespace EcowittWeather.Infrastructure.Security;

/// <summary>
/// Protects credentials with Windows DPAPI CurrentUser.
/// The file cannot be moved to a different Windows account or PC and decrypted there.
/// </summary>
public sealed class CloudSecretStore
{
    public CloudCredentials Load()
    {
        if (!File.Exists(ConfigurationPaths.CredentialsPath))
            return new CloudCredentials("", "");

        try
        {
            var protectedBytes = File.ReadAllBytes(ConfigurationPaths.CredentialsPath);
            var jsonBytes = ProtectedData.Unprotect(
                protectedBytes, optionalEntropy: null, DataProtectionScope.CurrentUser);
            return JsonSerializer.Deserialize<CloudCredentials>(jsonBytes)
                ?? new CloudCredentials("", "");
        }
        catch (CryptographicException)
        {
            throw new InvalidDataException(
                "API kľúče nebolo možné dešifrovať pre aktuálne konto Windows. " +
                "Zadaj ich znova v Nastaveniach.");
        }
    }

    public void Save(CloudCredentials credentials)
    {
        Directory.CreateDirectory(ConfigurationPaths.DirectoryPath);
        var bytes = JsonSerializer.SerializeToUtf8Bytes(credentials);
        var encrypted = ProtectedData.Protect(
            bytes, optionalEntropy: null, DataProtectionScope.CurrentUser);

        var temp = ConfigurationPaths.CredentialsPath + ".tmp";
        File.WriteAllBytes(temp, encrypted);
        File.Move(temp, ConfigurationPaths.CredentialsPath, true);
        CryptographicOperations.ZeroMemory(bytes);
    }
}
