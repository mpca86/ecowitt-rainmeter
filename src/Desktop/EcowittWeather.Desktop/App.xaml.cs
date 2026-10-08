using System.Net.Http;
using System.Windows;
using System.Windows.Threading;
using MessageBox = System.Windows.MessageBox;
using EcowittWeather.Core.Models;
using EcowittWeather.Desktop.Settings;
using EcowittWeather.Desktop.ViewModels;
using EcowittWeather.Desktop.Widgets;
using EcowittWeather.Infrastructure.Configuration;
using EcowittWeather.Infrastructure.Ecowitt.Cloud;
using EcowittWeather.Infrastructure.Security;
using Forms = System.Windows.Forms;

namespace EcowittWeather.Desktop;

public partial class App : System.Windows.Application
{
    private readonly AppSettingsStore _settingsStore = new();
    private readonly CloudSecretStore _secretsStore = new();
    private readonly HttpClient _http = new() { Timeout = TimeSpan.FromSeconds(20) };
    private readonly WeatherViewModel _weather = new();
    private readonly Dictionary<string, WeatherWidget> _widgets = [];

    private AppSettings _settings = new();
    private CloudCredentials _credentials = new("", "");
    private EcowittCloudSource? _source;
    private DispatcherTimer? _timer;
    private Forms.NotifyIcon? _tray;
    private bool _isFetching;

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
                MessageBoxButton.OK,
                MessageBoxImage.Warning);
        }

        _source = new EcowittCloudSource(_http, _credentials);
        InitializeTray();

        foreach (var layout in _settings.Widgets)
            CreateWidget(layout, saveSettings: false);

        _timer = new DispatcherTimer
        {
            Interval = TimeSpan.FromSeconds(_settings.RefreshSeconds)
        };
        _timer.Tick += async (_, _) => await RefreshAsync();
        _timer.Start();

        if (!_credentials.IsConfigured || string.IsNullOrWhiteSpace(_settings.Profile.CloudMac))
        {
            _weather.ShowStatus("Nastav Ecowitt Cloud API cez ozubené koliesko.");
            ShowSettings();
        }
        else
        {
            _ = RefreshAsync();
        }
    }

    private void InitializeTray()
    {
        var menu = new Forms.ContextMenuStrip();
        var open = new Forms.ToolStripMenuItem("Zobraziť widgety");
        open.Click += (_, _) => Dispatcher.Invoke(ShowWidgets);
        var add = new Forms.ToolStripMenuItem("Pridať widget");
        add.Click += (_, _) => Dispatcher.Invoke(AddWidget);
        var settings = new Forms.ToolStripMenuItem("Nastavenia");
        settings.Click += (_, _) => Dispatcher.Invoke(ShowSettings);
        var refresh = new Forms.ToolStripMenuItem("Obnoviť dáta");
        refresh.Click += (_, _) => Dispatcher.Invoke(() => _ = RefreshAsync());
        var exit = new Forms.ToolStripMenuItem("Ukončiť aplikáciu");
        exit.Click += (_, _) => Dispatcher.Invoke(Shutdown);

        menu.Items.AddRange([open, add, settings, refresh, new Forms.ToolStripSeparator(), exit]);

        _tray = new Forms.NotifyIcon
        {
            Icon = System.Drawing.SystemIcons.Information,
            Text = "Ecowitt Weather Desktop",
            ContextMenuStrip = menu,
            Visible = true
        };
        _tray.DoubleClick += (_, _) => Dispatcher.Invoke(ShowWidgets);
    }

    private void AddWidget()
    {
        var placement = new WidgetPlacement();
        _settings.Widgets.Add(placement);
        CreateWidget(placement, saveSettings: true);
    }

    private void CreateWidget(WidgetPlacement placement, bool saveSettings)
    {
        var widget = new WeatherWidget
        {
            DataContext = _weather,
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
        widget.LocationChanged += (_, _) =>
        {
            if (!widget.IsLoaded) return;
            placement.Left = widget.Left;
            placement.Top = widget.Top;
        };
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

    private void ShowSettings()
    {
        var dialog = new SettingsWindow(_settings, _credentials);
        var owner = _widgets.Values.FirstOrDefault(w => w.IsVisible);
        if (owner is not null) dialog.Owner = owner;

        if (dialog.ShowDialog() != true)
            return;

        try
        {
            _secretsStore.Save(dialog.ResultCredentials);
            _settingsStore.Save(dialog.ResultSettings);
            _credentials = dialog.ResultCredentials;
            _settings = dialog.ResultSettings;
            _source = new EcowittCloudSource(_http, _credentials);

            if (_timer is not null)
            {
                _timer.Interval = TimeSpan.FromSeconds(_settings.RefreshSeconds);
                _timer.Stop();
                _timer.Start();
            }

            _ = RefreshAsync();
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                "Nastavenia sa nepodarilo uložiť: " + ex.Message,
                "Ecowitt Weather",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
    }

    private async Task RefreshAsync()
    {
        if (_isFetching || _source is null) return;

        if (!_credentials.IsConfigured || string.IsNullOrWhiteSpace(_settings.Profile.CloudMac))
        {
            _weather.ShowStatus("Najprv vyplň Cloud API údaje v Nastaveniach.");
            return;
        }

        _isFetching = true;
        try
        {
            var snapshot = await _source.FetchAsync(_settings.Profile);
            _weather.ShowSnapshot(snapshot, _settings.Profile.Name, _settings.SensorAliases);
        }
        catch (TaskCanceledException)
        {
            _weather.ShowStatus("Vypršal časový limit spojenia s Ecowitt API.");
        }
        catch (Exception ex)
        {
            // The adapter never includes the credential-bearing request URL in errors.
            _weather.ShowStatus("Chyba načítania: " + ex.Message);
        }
        finally
        {
            _isFetching = false;
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
        _http.Dispose();
        base.OnExit(e);
    }
}
