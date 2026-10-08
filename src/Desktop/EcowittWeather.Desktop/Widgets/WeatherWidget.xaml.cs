using System.Windows;
using System.Windows.Input;
using System.Windows.Controls;
using EcowittWeather.Core.Models;

namespace EcowittWeather.Desktop.Widgets;

public partial class WeatherWidget : Window
{
    public event EventHandler? SettingsRequested;
    public event EventHandler? RemoveRequested;
    public event EventHandler? PositionCommitted;
    public event Action<string>? StationSwitchRequested;
    private readonly ContextMenu _profileMenu = new();

    public WeatherWidget()
    {
        InitializeComponent();
    }

    private void DragWidget(object sender, MouseButtonEventArgs e)
    {
        if (e.ChangedButton == MouseButton.Left && e.ButtonState == MouseButtonState.Pressed)
        {
            try { DragMove(); }
            catch (InvalidOperationException) { /* Windows may end the drag early. */ }
            finally { PositionCommitted?.Invoke(this, EventArgs.Empty); }
        }
    }

    public void SetProfiles(IEnumerable<StationProfile> profiles, string selectedId)
    {
        _profileMenu.Items.Clear();
        foreach (var profile in profiles)
        {
            var id = profile.Id;
            var item = new MenuItem
            {
                Header = profile.Name,
                IsCheckable = true,
                IsChecked = profile.Id == selectedId
            };
            item.Click += (_, _) => StationSwitchRequested?.Invoke(id);
            _profileMenu.Items.Add(item);
        }
    }

    private void ChooseStation(object sender, RoutedEventArgs e)
    {
        _profileMenu.PlacementTarget = sender as UIElement;
        _profileMenu.IsOpen = true;
    }

    private void OpenSettings(object sender, RoutedEventArgs e) =>
        SettingsRequested?.Invoke(this, EventArgs.Empty);

    private void RemoveWidget(object sender, RoutedEventArgs e) =>
        RemoveRequested?.Invoke(this, EventArgs.Empty);
}
