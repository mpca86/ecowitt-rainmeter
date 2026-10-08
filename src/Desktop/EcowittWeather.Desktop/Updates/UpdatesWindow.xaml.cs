using System.IO;
using System.Net.Http;
using System.Diagnostics;
using System.Reflection;
using System.Text;
using System.Windows;
using EcowittWeather.Infrastructure.Updates;
using MessageBox = System.Windows.MessageBox;

namespace EcowittWeather.Desktop.Updates;

/// <summary>
/// Explicit check and verified self-update for portable Windows Desktop builds.
/// The helper runs after the UI process exits, and maintains a rollback backup.
/// </summary>
public partial class UpdatesWindow : Window
{
    // Release ZIPs can be tens of MB, so never reuse the 20-second weather client.
    private readonly HttpClient _downloadClient = new()
    {
        Timeout = TimeSpan.FromMinutes(10)
    };
    private readonly DesktopUpdateService _updates;
    private DesktopRelease? _release;
    private bool _busy;
    public static string CurrentVersion =>
        Assembly.GetExecutingAssembly()
            .GetCustomAttribute<AssemblyInformationalVersionAttribute>()?
            .InformationalVersion ?? "0.2.0-alpha.1";

    public UpdatesWindow()
    {
        InitializeComponent();
        _updates = new DesktopUpdateService(_downloadClient);
        CurrentLabel.Text = "Nainštalovaná verzia: " + CurrentVersion.Split('+')[0];
        LatestLabel.Text = "Najnovšia verzia: —";
        Loaded += async (_, _) => await CheckAsync();
    }

    private async void CheckClick(object sender, RoutedEventArgs e) => await CheckAsync();

    private void SetBusy(bool busy)
    {
        _busy = busy;
        CheckButton.IsEnabled = !busy;
        InstallButton.IsEnabled = !busy && _release != null;
    }

    private async Task CheckAsync()
    {
        if (_busy) return;
        SetBusy(true);
        StatusLabel.Text = "Kontrolujem nové verzie na GitHube...";
        try
        {
            _release = await _updates.FindUpdateAsync(CurrentVersion);
            if (!IsLoaded) return;

            if (_release == null)
            {
                LatestLabel.Text = "Najnovšia verzia: aktuálna";
                StatusLabel.Text = "Používaš aktuálne vydanie alpha kanála.";
                ReleaseNotes.Text = "Pre túto inštaláciu nie je dostupná novšia alpha verzia.";
            }
            else
            {
                LatestLabel.Text = "Najnovšia verzia: " + _release.Version;
                StatusLabel.Text = "Je dostupná nová verzia.";
                ReleaseNotes.Text = _release.Notes;
            }
        }
        catch (Exception)
        {
            if (IsLoaded) StatusLabel.Text =
                "Kontrola zlyhala. Skontroluj pripojenie alebo dostupnosť GitHub API.";
        }
        finally { if (IsLoaded) SetBusy(false); }
    }

    private async void InstallClick(object sender, RoutedEventArgs e)
    {
        if (_busy || _release == null) return;
        if (MessageBox.Show(this,
                $"Aktualizovať Desktop Edition na {_release.Version}?\n\n" +
                "Aplikácia sa ukončí, vytvorí zálohu programových súborov a reštartuje.",
                "Potvrdiť aktualizáciu", MessageBoxButton.YesNo, MessageBoxImage.Question)
            != MessageBoxResult.Yes) return;

        SetBusy(true);
        DownloadProgress.Visibility = Visibility.Visible;
        DownloadProgress.Value = 0;
        StatusLabel.Text = "Sťahujem a overujem aktualizačný balík...";
        try
        {
            var progress = new Progress<double>(x => DownloadProgress.Value = x * 100);
            var payload = await _updates.DownloadAndStageAsync(_release, progress);
            StatusLabel.Text = "SHA-256 overené. Pripravujem aktualizáciu...";
            StartDetachedUpdater(payload);
            System.Windows.Application.Current.Shutdown();
        }
        catch (Exception)
        {
            StatusLabel.Text = "Aktualizácia zlyhala. Aplikácia zostáva nezmenená.";
            MessageBox.Show(this,
                "Aktualizácia sa nepodarila. Skontroluj sieť a oprávnenia priečinka. " +
                "Ak chyba pretrvá, stiahni nový ZIP z GitHub Releases.",
                "Chyba aktualizácie", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
        finally { if (IsLoaded) SetBusy(false); }
    }

    private static void StartDetachedUpdater(string source)
    {
        var exe = Environment.ProcessPath ??
            throw new InvalidOperationException("Neznáma cesta spustiteľného súboru.");

        if (!string.Equals(Path.GetFileName(exe), "EcowittWeather.Desktop.exe",
                StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("Aktualizácia je dostupná iba pre balenú Windows aplikáciu.");

        var directory = Path.GetDirectoryName(exe) ??
            throw new InvalidOperationException("Neznámy priečinok aplikácie.");

        using var resource = Assembly.GetExecutingAssembly()
            .GetManifestResourceStream("EcowittWeather.UpdateHelper")
            ?? throw new InvalidOperationException("Chýba pomocný aktualizačný skript.");
        using var reader = new StreamReader(resource, Encoding.UTF8);
        var script = reader.ReadToEnd();

        // The extracted package is already SHA-256 validated. The helper itself
        // is compiled into the currently running application, not downloaded.
        var helper = Path.Combine(Path.GetDirectoryName(source)!,
            "install-update.ps1");
        File.WriteAllText(helper, script, new UTF8Encoding(true));
        var psi = new ProcessStartInfo
        {
            FileName = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.System),
                "WindowsPowerShell", "v1.0", "powershell.exe"),
            UseShellExecute = false,
            CreateNoWindow = true,
            WorkingDirectory = Path.GetDirectoryName(source)!
        };
        foreach (var argument in new[]
        {
            "-NoProfile", "-NonInteractive", "-ExecutionPolicy", "Bypass",
            "-File", helper,
            "-ParentPid", Environment.ProcessId.ToString(),
            "-Source", source,
            "-Destination", directory,
            "-Executable", "EcowittWeather.Desktop.exe"
        }) psi.ArgumentList.Add(argument);

        using var process = Process.Start(psi)
            ?? throw new InvalidOperationException("Aktualizátor sa nepodarilo spustiť.");
    }

    protected override void OnClosed(EventArgs e)
    {
        _downloadClient.Dispose();
        base.OnClosed(e);
    }

    private void CloseClick(object sender, RoutedEventArgs e) => Close();
}
