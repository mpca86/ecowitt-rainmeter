using System.Net.Http;
using System.Reflection;
using System.Windows;
using System.Windows.Threading;
using EcowittWeather.Core.Models;
using EcowittWeather.Desktop.About;
using EcowittWeather.Desktop.Settings;
using EcowittWeather.Desktop.ViewModels;
using EcowittWeather.Desktop.Widgets;
using EcowittWeather.Infrastructure.Configuration;
using EcowittWeather.Infrastructure.Ecowitt.Cloud;
using EcowittWeather.Infrastructure.Security;
using Forms = System.Windows.Forms;
using MessageBox = System.Windows.MessageBox;

namespace EcowittWeather.Desktop;

public partial class App : System.Windows.Application
{
    private readonly AppSettingsStore _settingsStore = new();
    private readonly CloudSecretStore _secretsStore = new();
    private readonly HttpClient _http = new() { Timeout = TimeSpan.FromSeconds(20) };
    private readonly Dictionary<string, WeatherViewModel> _models = [];
    private readonly Dictionary<string, WeatherWidget> _widgets = [];
    private readonly Dictionary<string, DateTimeOffset> _lastAttempt = [];
    private readonly HashSet<string> _fetching = [];

    private AppSettings _settings = new();
    private CloudCredentials _credentials = new("", "");
    private EcowittCloudSource? _source;
    private DispatcherTimer? _timer;
    private Forms.NotifyIcon? _tray;
    private Forms.ToolStripMenuItem? _addWidgetMenu;
    private System.Drawing.Icon? _appIcon;
    private int _generation;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        ShutdownMode = ShutdownMode.OnExplicitShutdown;

        try
        {
            _settings = _settingsStore.Load();
            _credentials = _secretsStore.Load();
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                "Nepodarilo sa načítať uloženú konfiguráciu: " + ex.Message,
                "Ecowitt Weather",
                MessageBoxButton.OK, MessageBoxImage.Warning);
        }

        _settings.Normalize();
        _source = new EcowittCloudSource(_http, _credentials);
        InitializeTray();

        foreach (var layout in _settings.Widgets.ToArray())
            CreateWidget(layout, saveSettings: false);

        _timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
        _timer.Tick += async (_, _) => await RefreshAllAsync(force: false);
        _timer.Start();

        if (!_credentials.IsConfigured ||
            !_settings.Profiles.Any(p => !string.IsNullOrWhiteSpace(p.CloudMac)))
        {
            foreach (var model in _models.Values)
                model.ShowStatus("Nastav Ecowitt Cloud API v Nastaveniach.");
            ShowSettings();
        }
        else
        {
            _ = RefreshAllAsync(force: true);
        }
    }

    private WeatherViewModel ModelFor(string profileId)
    {
        if (_models.TryGetValue(profileId, out var model))
            return model;

        model = new WeatherViewModel();
        _models[profileId] = model;
        return model;
    }

    private void InitializeTray()
    {
        var menu = new Forms.ContextMenuStrip();
        var open = new Forms.ToolStripMenuItem("Zobraziť widgety");
        open.Click += (_, _) => Dispatcher.Invoke(ShowWidgets);

        _addWidgetMenu = new Forms.ToolStripMenuItem("Pridať widget – vybrať stanicu");
        _addWidgetMenu.DropDownOpening += (_, _) => BuildAddMenu();

        var settings = new Forms.ToolStripMenuItem("Stanice a nastavenia");
        settings.Click += (_, _) => Dispatcher.Invoke(ShowSettings);
        var refresh = new Forms.ToolStripMenuItem("Obnoviť dáta");
        refresh.Click += (_, _) => Dispatcher.Invoke(() => _ = RefreshAllAsync(force: true));
        var about = new Forms.ToolStripMenuItem("O programe");
        about.Click += (_, _) => Dispatcher.Invoke(ShowAbout);
        var exit = new Forms.ToolStripMenuItem("Ukončiť aplikáciu");
        exit.Click += (_, _) => Dispatcher.Invoke(Shutdown);

        menu.Items.AddRange([
            open, _addWidgetMenu, settings, refresh,
            new Forms.ToolStripSeparator(), about,
            new Forms.ToolStripSeparator(), exit
        ]);

        using (var iconStream = Assembly.GetExecutingAssembly()
            .GetManifestResourceStream("EcowittWeather.Icon"))
        {
            if (iconStream != null)
                _appIcon = new System.Drawing.Icon(iconStream);
        }

        _tray = new Forms.NotifyIcon
        {
            Icon = _appIcon ?? System.Drawing.SystemIcons.Information,
            Text = "Ecowitt Weather Desktop",
            ContextMenuStrip = menu,
            Visible = true
        };
        _tray.DoubleClick += (_, _) => Dispatcher.Invoke(ShowWidgets);
    }

    private void BuildAddMenu()
    {
        if (_addWidgetMenu is null) return;
        _addWidgetMenu.DropDownItems.Clear();

        foreach (var station in _settings.Profiles)
        {
            var id = station.Id;
            var item = new Forms.ToolStripMenuItem(station.Name);
            item.Click += (_, _) => Dispatcher.Invoke(() => AddWidget(id));
            _addWidgetMenu.DropDownItems.Add(item);
        }
    }

    private void AddWidget(string? profileId = null)
    {
        var profile = _settings.Profiles.FirstOrDefault(p => p.Id == profileId)
            ?? _settings.Profiles[0];
        var placement = new WidgetPlacement { ProfileId = profile.Id };
        _settings.Widgets.Add(placement);
        CreateWidget(placement, saveSettings: true);
        _ = RefreshAllAsync(force: true);
    }

    private void CreateWidget(WidgetPlacement placement, bool saveSettings)
    {
        var widget = new WeatherWidget
        {
            DataContext = ModelFor(placement.ProfileId),
            Topmost = placement.AlwaysOnTop
        };

        if (placement.Left is double x && placement.Top is double y &&
            double.IsFinite(x) && double.IsFinite(y))
        {
            widget.Left = x;
            widget.Top = y;
        }
        else
        {
            widget.WindowStartupLocation = WindowStartupLocation.CenterScreen;
        }

        widget.SettingsRequested += (_, _) => ShowSettings();
        widget.RemoveRequested += (_, _) =>
        {
            _widgets.Remove(placement.Id);
            _settings.Widgets.Remove(placement);
            widget.Close();
            SaveLayout();
        };
        widget.StationSwitchRequested += profileId =>
        {
            if (!_settings.Profiles.Any(p => p.Id == profileId)) return;
            placement.ProfileId = profileId;
            widget.DataContext = ModelFor(profileId);
            widget.SetProfiles(_settings.Profiles, profileId);
            SaveLayout();
            _ = RefreshAllAsync(force: true);
        };
        widget.LocationChanged += (_, _) =>
        {
            if (!widget.IsLoaded) return;
            placement.Left = widget.Left;
            placement.Top = widget.Top;
        };
        widget.PositionCommitted += (_, _) => SaveLayout();

        widget.SetProfiles(_settings.Profiles, placement.ProfileId);
        widget.Show();
        _widgets[placement.Id] = widget;
        if (saveSettings) SaveLayout();
    }

    private void ShowWidgets()
    {
        if (_widgets.Count == 0)
            AddWidget();

        foreach (var widget in _widgets.Values)
        {
            if (!widget.IsVisible) widget.Show();
            widget.Activate();
        }
    }

    private void ShowAbout()
    {
        var dialog = new AboutWindow();
        var owner = _widgets.Values.FirstOrDefault(w => w.IsVisible);
        if (owner != null) dialog.Owner = owner;
        dialog.ShowDialog();
    }

    private void ShowSettings()
    {
        var dialog = new SettingsWindow(_settings, _credentials);
        var owner = _widgets.Values.FirstOrDefault(w => w.IsVisible);
        if (owner != null) dialog.Owner = owner;

        if (dialog.ShowDialog() != true) return;

        try
        {
            _secretsStore.Save(dialog.ResultCredentials);
            _settingsStore.Save(dialog.ResultSettings);
            _credentials = dialog.ResultCredentials;
            _settings = dialog.ResultSettings;
            _source = new EcowittCloudSource(_http, _credentials);
            _generation++;
            _lastAttempt.Clear();

            var ids = _settings.Profiles.Select(p => p.Id).ToHashSet();
            foreach (var layout in _settings.Widgets)
            {
                if (!ids.Contains(layout.ProfileId))
                    layout.ProfileId = _settings.Profiles[0].Id;

                if (_widgets.TryGetValue(layout.Id, out var widget))
                {
                    widget.DataContext = ModelFor(layout.ProfileId);
                    widget.SetProfiles(_settings.Profiles, layout.ProfileId);
                }
            }

            foreach (var profile in _settings.Profiles)
                if (_models.TryGetValue(profile.Id, out var model))
                    model.ShowStatus("Obnovujem údaje stanice...");

            SaveLayout();
            _ = RefreshAllAsync(force: true);
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                "Nastavenia sa nepodarilo uložiť: " + ex.Message,
                "Ecowitt Weather", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private Task RefreshAllAsync(bool force)
    {
        if (_source is null || !_credentials.IsConfigured)
        {
            foreach (var model in _models.Values)
                model.ShowStatus("Vyplň Cloud API kľúče v Nastaveniach.");
            return Task.CompletedTask;
        }

        var now = DateTimeOffset.UtcNow;
        var activeProfileIds = _settings.Widgets
            .Select(w => w.ProfileId).Distinct().ToHashSet();
        var tasks = new List<Task>();

        foreach (var profile in _settings.Profiles.Where(p => activeProfileIds.Contains(p.Id)))
        {
            if (_fetching.Contains(profile.Id)) continue;
            if (!force && _lastAttempt.TryGetValue(profile.Id, out var last) &&
                (now - last).TotalSeconds < _settings.RefreshSeconds)
                continue;

            _lastAttempt[profile.Id] = now;
            _fetching.Add(profile.Id);
            tasks.Add(FetchProfileAsync(profile, _source, _generation));
        }

        return Task.WhenAll(tasks);
    }

    private async Task FetchProfileAsync(
        StationProfile profile, EcowittCloudSource source, int generation)
    {
        try
        {
            var snapshot = await source.FetchAsync(profile);
            if (generation != _generation) return;
            ModelFor(profile.Id).ShowSnapshot(
                snapshot, profile.Name, profile.SensorAliases);
        }
        catch (TaskCanceledException)
        {
            if (generation == _generation)
                ModelFor(profile.Id).ShowStatus("Časový limit spojenia s Ecowitt API.");
        }
        catch (Exception)
        {
            if (generation == _generation)
                ModelFor(profile.Id).ShowStatus(
                    "Nepodarilo sa načítať stanicu. Skontroluj MAC a Cloud API.");
        }
        finally
        {
            _fetching.Remove(profile.Id);
        }
    }

    private void SaveLayout()
    {
        try { _settingsStore.Save(_settings); }
        catch (Exception ex)
        {
            MessageBox.Show("Nepodarilo sa uložiť rozloženie: " + ex.Message,
                "Ecowitt Weather", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }

    protected override void OnExit(ExitEventArgs e)
    {
        _timer?.Stop();
        SaveLayout();
        _tray?.Dispose();
        _appIcon?.Dispose();
        _http.Dispose();
        base.OnExit(e);
    }
}
