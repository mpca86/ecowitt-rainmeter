using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using Cursors = System.Windows.Input.Cursors;
using EcowittWeather.Core.Models;

namespace EcowittWeather.Desktop.Widgets;

/// <summary>
/// Left mouse drag repositions the widget; right mouse opens a shared,
/// centrally themed context menu with station selection and settings.
/// </summary>
public partial class WeatherWidget : Window
{
    public event EventHandler? SettingsRequested;
    public event EventHandler? RemoveRequested;
    public event EventHandler? PositionCommitted;
    public event Action<string>? StationSwitchRequested;
    public event EventHandler? UpdateRequested;

    private readonly ContextMenu _menu = new();
    private readonly MenuItem _stations = new() { Header = "Zmeniť meteostanicu" };

    public WeatherWidget()
    {
        InitializeComponent();
        _menu.Items.Add(_stations);
        _menu.Items.Add(new Separator());
        AddMenuAction("Nastavenia", () => SettingsRequested?.Invoke(this, EventArgs.Empty));
        AddMenuAction("Aktualizácie", () => UpdateRequested?.Invoke(this, EventArgs.Empty));
        _menu.Items.Add(new Separator());
        AddMenuAction("Zavrieť widget", () => RemoveRequested?.Invoke(this, EventArgs.Empty));
    }

    private void AddMenuAction(string caption, Action action)
    {
        var item = new MenuItem { Header = caption };
        item.Click += (_, _) => action();
        _menu.Items.Add(item);
    }

    public void SetProfiles(IEnumerable<StationProfile> profiles, string selectedId)
    {
        _stations.Items.Clear();
        foreach (var profile in profiles)
        {
            var id = profile.Id;
            var item = new MenuItem
            {
                Header = profile.Name,
                IsCheckable = true,
                IsChecked = id == selectedId
            };
            item.Click += (_, _) => StationSwitchRequested?.Invoke(id);
            _stations.Items.Add(item);
        }
    }

    private void DragWidget(object sender, MouseButtonEventArgs e)
    {
        if (e.ChangedButton != MouseButton.Left || e.ButtonState != MouseButtonState.Pressed)
            return;
        e.Handled = true;
        try { DragMove(); }
        catch (InvalidOperationException) { }
        finally { PositionCommitted?.Invoke(this, EventArgs.Empty); }
    }

    private void ShowWidgetMenu(object sender, MouseButtonEventArgs e)
    {
        if (e.ChangedButton != MouseButton.Right) return;
        e.Handled = true;
        _menu.PlacementTarget = sender as UIElement;
        _menu.Placement = PlacementMode.MousePoint;
        _menu.IsOpen = true;
    }
}
