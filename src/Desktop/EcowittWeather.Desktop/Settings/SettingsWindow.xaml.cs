using System.Net.Http;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using EcowittWeather.Core.Models;
using EcowittWeather.Infrastructure.Configuration;
using EcowittWeather.Infrastructure.Ecowitt.Cloud;
using EcowittWeather.Infrastructure.Ecowitt.Local;
using Color = System.Windows.Media.Color;
using Brushes = System.Windows.Media.Brushes;
using TextBox = System.Windows.Controls.TextBox;
using MessageBox = System.Windows.MessageBox;

namespace EcowittWeather.Desktop.Settings;

public partial class SettingsWindow : Window
{
    private readonly AppSettings _previous;
    private readonly List<StationProfile> _profiles;
    private readonly Dictionary<int, TextBox> _aliases = [];
    private string _currentId = "";
    private bool _loading;
    private sealed record SourceChoice(SourceMode Mode, string Label);

    private readonly SourceChoice[] _sourceModes =
    [
        new(SourceMode.Cloud, "Web API – Ecowitt Cloud"),
        new(SourceMode.Local, "Local API – lokálny gateway"),
        new(SourceMode.Auto, "Auto – Local s Web zálohou")
    ];

    public AppSettings ResultSettings { get; private set; }
    public CloudCredentials ResultCredentials { get; private set; }

    public SettingsWindow(AppSettings settings, CloudCredentials credentials)
    {
        InitializeComponent();
        settings.Normalize();
        _previous = settings;
        _profiles = settings.Profiles.Select(p => p with
        {
            SensorAliases = new Dictionary<int, string>(p.SensorAliases)
        }).ToList();

        ResultSettings = settings;
        ResultCredentials = credentials;
        RefreshSecondsBox.Text = settings.RefreshSeconds.ToString();
        ApplicationKeyBox.Password = credentials.ApplicationKey;
        ApiKeyBox.Password = credentials.ApiKey;
        SourceModeCombo.ItemsSource = _sourceModes;

        for (var channel = 1; channel <= 8; channel++)
        {
            var row = new Grid { Margin = new Thickness(0, 0, 4, 8) };
            row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(56) });
            row.ColumnDefinitions.Add(new ColumnDefinition());

            var label = new TextBlock
            {
                Text = $"CH{channel}",
                Foreground = new SolidColorBrush(Color.FromRgb(197, 213, 227)),
                VerticalAlignment = VerticalAlignment.Center,
                FontWeight = FontWeights.SemiBold
            };
            var textBox = new TextBox();
            Grid.SetColumn(textBox, 1);
            row.Children.Add(label);
            row.Children.Add(textBox);
            AliasPanel.Children.Add(row);
            _aliases.Add(channel, textBox);
        }

        _loading = true;
        ProfileCombo.ItemsSource = _profiles;
        ProfileCombo.SelectedIndex = 0;
        _loading = false;
        LoadProfile(_profiles[0]);
    }

    private void PersistEditor()
    {
        if (string.IsNullOrWhiteSpace(_currentId)) return;
        var index = _profiles.FindIndex(p => p.Id == _currentId);
        if (index < 0) return;

        _profiles[index] = _profiles[index] with
        {
            Name = ProfileNameBox.Text.Trim(),
            CloudMac = MacBox.Text.Trim().Replace('-', ':').ToUpperInvariant(),
            LocalGatewayHost = LocalGatewayBox.Text.Trim(),
            SourceMode = (SourceModeCombo.SelectedItem as SourceChoice)?.Mode ?? SourceMode.Cloud,
            SensorAliases = _aliases.ToDictionary(pair => pair.Key, pair => pair.Value.Text.Trim())
        };
    }

    private void LoadProfile(StationProfile profile)
    {
        _loading = true;
        _currentId = profile.Id;
        ProfileNameBox.Text = profile.Name;
        MacBox.Text = profile.CloudMac;
        LocalGatewayBox.Text = profile.LocalGatewayHost;
        SourceModeCombo.SelectedItem = _sourceModes.First(x => x.Mode == profile.SourceMode);
        UpdateSourceModeHelp(profile.SourceMode);
        foreach (var (channel, box) in _aliases)
            box.Text = profile.SensorAliases.GetValueOrDefault(channel, "");

        StationCombo.SelectedItem = null;
        if (StationCombo.ItemsSource is IEnumerable<EcowittDevice> devices)
        {
            var selected = devices.FirstOrDefault(d =>
                string.Equals(d.Mac, profile.CloudMac, StringComparison.OrdinalIgnoreCase));
            if (selected != null) StationCombo.SelectedItem = selected;
        }
        _loading = false;
    }

    private void RefreshProfileList(string id)
    {
        _loading = true;
        ProfileCombo.ItemsSource = null;
        ProfileCombo.ItemsSource = _profiles;
        ProfileCombo.SelectedItem = _profiles.First(x => x.Id == id);
        _loading = false;
        LoadProfile(_profiles.First(x => x.Id == id));
    }

    private void ProfileSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_loading || ProfileCombo.SelectedItem is not StationProfile selected) return;
        PersistEditor();
        LoadProfile(_profiles.First(x => x.Id == selected.Id));
    }

    private void SourceModeSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_loading || SourceModeCombo.SelectedItem is not SourceChoice option)
            return;
        UpdateSourceModeHelp(option.Mode);
    }

    private void UpdateSourceModeHelp(SourceMode mode)
    {
        LocalGatewayPanel.Visibility = mode == SourceMode.Cloud
            ? Visibility.Collapsed : Visibility.Visible;
        SourceModeDescription.Text = mode switch
        {
            SourceMode.Cloud => "Web API: merania zo serverov Ecowitt; vyžaduje API kľúče a MAC.",
            SourceMode.Local => "Local API: merania priamo z gatewaya v LAN; bez internetu a bez API kľúčov.",
            _ => "Auto: prednostne Local API. Ak LAN nie je dostupná, prejde na Web API. " +
                 "Lokálny gateway pravidelne skúša obnoviť."
        };
    }

    private void ProfileNameLostFocus(object sender, RoutedEventArgs e)
    {
        if (_loading || string.IsNullOrWhiteSpace(_currentId)) return;
        PersistEditor();
        RefreshProfileList(_currentId);
    }

    private void AddProfileClick(object sender, RoutedEventArgs e)
    {
        PersistEditor();
        var profile = new StationProfile
        {
            Id = Guid.NewGuid().ToString("N"),
            Name = "Nová stanica"
        };
        _profiles.Add(profile);
        RefreshProfileList(profile.Id);
    }

    private void RemoveProfileClick(object sender, RoutedEventArgs e)
    {
        if (_profiles.Count <= 1)
        {
            MessageBox.Show(this, "Aspoň jedna stanica musí zostať nastavená.",
                "Stanice", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        if (MessageBox.Show(this,
            "Odstrániť aktuálny profil? Widgety tejto stanice sa presunú na prvú zostávajúcu stanicu.",
            "Odstránenie stanice", MessageBoxButton.YesNo,
            MessageBoxImage.Question) != MessageBoxResult.Yes)
            return;

        _profiles.RemoveAll(p => p.Id == _currentId);
        _currentId = "";
        RefreshProfileList(_profiles[0].Id);
    }

    private async void LoadDevicesClick(object sender, RoutedEventArgs e)
    {
        var creds = new CloudCredentials(
            ApplicationKeyBox.Password.Trim(),
            ApiKeyBox.Password.Trim());

        if (!creds.IsConfigured)
        {
            DeviceStatus.Text = "Najprv vyplň Application Key a API Key.";
            DeviceStatus.Foreground = Brushes.LightSalmon;
            return;
        }

        LoadDevicesButton.IsEnabled = false;
        StationCombo.IsEnabled = false;
        DeviceStatus.Text = "Načítavam zoznam staníc...";
        DeviceStatus.Foreground = Brushes.White;

        try
        {
            using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(20) };
            var devices = await new EcowittDeviceCatalog(http).ListAsync(creds);
            if (!IsLoaded) return;

            StationCombo.ItemsSource = devices;
            StationCombo.IsEnabled = devices.Count > 0;

            var currentMac = MacBox.Text.Trim().Replace('-', ':');
            var current = devices.FirstOrDefault(d =>
                string.Equals(d.Mac, currentMac, StringComparison.OrdinalIgnoreCase));

            if (current is not null)
                StationCombo.SelectedItem = current;
            else if (string.IsNullOrWhiteSpace(currentMac) && devices.Count == 1)
                StationCombo.SelectedIndex = 0;

            DeviceStatus.Text = devices.Count == 0
                ? "Účet nevrátil žiadnu meteorologickú stanicu s MAC adresou. Zadaj MAC ručne."
                : $"Načítané stanice: {devices.Count}. Vyber stanicu zo zoznamu.";
            DeviceStatus.Foreground = devices.Count > 0
                ? new SolidColorBrush(Color.FromRgb(136, 237, 205))
                : Brushes.LightSalmon;
        }
        catch (Exception)
        {
            if (!IsLoaded) return;
            DeviceStatus.Text = "Načítanie zlyhalo. Skontroluj API kľúče, internet a oprávnenie účtu.";
            DeviceStatus.Foreground = Brushes.LightSalmon;
        }
        finally
        {
            if (IsLoaded) LoadDevicesButton.IsEnabled = true;
        }
    }

    private void DeviceSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_loading || StationCombo.SelectedItem is not EcowittDevice selected) return;
        MacBox.Text = selected.Mac;

        if (string.IsNullOrWhiteSpace(ProfileNameBox.Text) ||
            ProfileNameBox.Text is "Moja stanica" or "Nová stanica")
            ProfileNameBox.Text = selected.Name;
    }

    private void SaveClick(object sender, RoutedEventArgs e)
    {
        PersistEditor();

        if (!int.TryParse(RefreshSecondsBox.Text, out var interval) ||
            interval is < 30 or > 3600)
        {
            MessageBox.Show(this, "Interval musí byť medzi 30 a 3600 sekundami.",
                "Neplatný interval", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        if (_profiles.Any(p => p.SourceMode != SourceMode.Local) &&
            (string.IsNullOrWhiteSpace(ApplicationKeyBox.Password) ||
             string.IsNullOrWhiteSpace(ApiKeyBox.Password)))
        {
            MessageBox.Show(this, "Web API a Auto režim vyžadujú Application Key aj API Key.",
                "Chýbajúce údaje", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        foreach (var profile in _profiles)
        {
            if (string.IsNullOrWhiteSpace(profile.Name))
            {
                MessageBox.Show(this, "Každý profil musí mať názov.",
                    "Neplatná stanica", MessageBoxButton.OK, MessageBoxImage.Warning);
                RefreshProfileList(profile.Id);
                return;
            }

            if (profile.SourceMode != SourceMode.Local &&
                !System.Text.RegularExpressions.Regex.IsMatch(
                    profile.CloudMac, @"^([0-9A-F]{2}:){5}[0-9A-F]{2}$"))
            {
                MessageBox.Show(this,
                    $"Profil '{profile.Name}': pre Web/Auto zadaj platnú MAC adresu.",
                    "Neplatná MAC", MessageBoxButton.OK, MessageBoxImage.Warning);
                RefreshProfileList(profile.Id);
                return;
            }

            if (profile.SourceMode != SourceMode.Cloud)
            {
                try { EcowittLocalSource.GetGatewayUrl(profile.LocalGatewayHost); }
                catch (InvalidOperationException)
                {
                    MessageBox.Show(this,
                        $"Profil '{profile.Name}': zadaj privátnu IP adresu lokálneho gatewaya " +
                        "alebo jeho názov (napr. 192.168.1.100:80).",
                        "Neplatný gateway", MessageBoxButton.OK, MessageBoxImage.Warning);
                    RefreshProfileList(profile.Id);
                    return;
                }
            }
        }

        ResultSettings = new AppSettings
        {
            Profiles = _profiles.ToList(),
            RefreshSeconds = interval,
            Widgets = _previous.Widgets
        };
        ResultSettings.Normalize();

        ResultCredentials = new CloudCredentials(
            ApplicationKeyBox.Password.Trim(),
            ApiKeyBox.Password.Trim());

        DialogResult = true;
        Close();
    }

    private void UpdatesClick(object sender, RoutedEventArgs e)
    {
        var dialog = new Updates.UpdatesWindow { Owner = this };
        dialog.ShowDialog();
    }

    private void AboutClick(object sender, RoutedEventArgs e)
    {
        var about = new About.AboutWindow { Owner = this };
        about.ShowDialog();
    }

    private void CancelClick(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }
}
