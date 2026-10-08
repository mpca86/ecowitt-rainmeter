using System.Net.Http;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using EcowittWeather.Core.Models;
using EcowittWeather.Infrastructure.Configuration;
using EcowittWeather.Infrastructure.Ecowitt.Cloud;
using TextBox = System.Windows.Controls.TextBox;
using MessageBox = System.Windows.MessageBox;

namespace EcowittWeather.Desktop.Settings;

public partial class SettingsWindow : Window
{
    private readonly AppSettings _previous;
    private readonly Dictionary<int, TextBox> _aliases = [];

    public AppSettings ResultSettings { get; private set; }
    public CloudCredentials ResultCredentials { get; private set; }

    public SettingsWindow(AppSettings settings, CloudCredentials credentials)
    {
        InitializeComponent();
        _previous = settings;
        ResultSettings = settings;
        ResultCredentials = credentials;

        ProfileNameBox.Text = settings.Profile.Name;
        MacBox.Text = settings.Profile.CloudMac;
        RefreshSecondsBox.Text = settings.RefreshSeconds.ToString();
        ApplicationKeyBox.Password = credentials.ApplicationKey;
        ApiKeyBox.Password = credentials.ApiKey;

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
            var textBox = new TextBox
            {
                Text = settings.SensorAliases.GetValueOrDefault(channel, "")
            };

            Grid.SetColumn(textBox, 1);
            row.Children.Add(label);
            row.Children.Add(textBox);
            AliasPanel.Children.Add(row);
            _aliases.Add(channel, textBox);
        }
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
            // The keys are used only for this request. They are not saved until Uložiť.
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
            // Avoid displaying exception messages, which could contain API credentials.
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
        if (StationCombo.SelectedItem is not EcowittDevice selected) return;

        MacBox.Text = selected.Mac;

        // Preserve the user's own profile label if they already chose one.
        if (string.IsNullOrWhiteSpace(ProfileNameBox.Text) ||
            ProfileNameBox.Text == "Moja stanica")
            ProfileNameBox.Text = selected.Name;
    }

    private void SaveClick(object sender, RoutedEventArgs e)
    {
        if (!int.TryParse(RefreshSecondsBox.Text, out var interval) ||
            interval is < 30 or > 3600)
        {
            MessageBox.Show(this, "Interval musí byť medzi 30 a 3600 sekundami.",
                "Neplatný interval", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        var name = ProfileNameBox.Text.Trim();
        if (string.IsNullOrWhiteSpace(name))
        {
            MessageBox.Show(this, "Zadaj názov stanice.",
                "Neplatný názov", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        var mac = MacBox.Text.Trim().Replace('-', ':').ToUpperInvariant();
        if (!System.Text.RegularExpressions.Regex.IsMatch(
                mac, @"^([0-9A-F]{2}:){5}[0-9A-F]{2}$"))
        {
            MessageBox.Show(this, "MAC musí mať tvar AA:BB:CC:DD:EE:FF.",
                "Neplatná MAC", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        if (string.IsNullOrWhiteSpace(ApplicationKeyBox.Password) ||
            string.IsNullOrWhiteSpace(ApiKeyBox.Password))
        {
            MessageBox.Show(this, "Vyplň Application Key aj API Key.",
                "Chýbajúce údaje", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        ResultSettings = new AppSettings
        {
            Profile = _previous.Profile with
            {
                Name = name,
                CloudMac = mac,
                SourceMode = SourceMode.Cloud
            },
            RefreshSeconds = interval,
            SensorAliases = _aliases.ToDictionary(pair => pair.Key, pair => pair.Value.Text.Trim()),
            Widgets = _previous.Widgets
        };
        ResultCredentials = new CloudCredentials(
            ApplicationKeyBox.Password.Trim(),
            ApiKeyBox.Password.Trim());

        DialogResult = true;
        Close();
    }

    private void CancelClick(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }
}
