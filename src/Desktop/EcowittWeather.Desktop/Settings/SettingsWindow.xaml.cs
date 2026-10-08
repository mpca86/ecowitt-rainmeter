using System.Windows;
using System.Windows.Controls;
using TextBox = System.Windows.Controls.TextBox;
using MessageBox = System.Windows.MessageBox;
using EcowittWeather.Core.Models;
using EcowittWeather.Infrastructure.Configuration;

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
            var row = new Grid { Margin = new Thickness(0, 0, 8, 7) };
            row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(55) });
            row.ColumnDefinitions.Add(new ColumnDefinition());

            var label = new TextBlock
            {
                Text = $"CH{channel}",
                Foreground = System.Windows.Media.Brushes.LightGray,
                VerticalAlignment = VerticalAlignment.Center
            };
            var textBox = new TextBox
            {
                Padding = new Thickness(6),
                Text = settings.SensorAliases.GetValueOrDefault(channel, "")
            };

            Grid.SetColumn(textBox, 1);
            row.Children.Add(label);
            row.Children.Add(textBox);
            AliasPanel.Children.Add(row);
            _aliases.Add(channel, textBox);
        }
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
