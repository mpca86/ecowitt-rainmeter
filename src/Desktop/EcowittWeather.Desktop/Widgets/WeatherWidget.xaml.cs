using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using EcowittWeather.Core.Models;

namespace EcowittWeather.Desktop.Widgets;

/// <summary>
/// Clicking the widget opens its context menu. Shift+left-drag, or choosing
/// "Presunúť widget" in that menu and then dragging, repositions the window.
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
    private bool _moveOnNextClick;

    public WeatherWidget()
    {
        InitializeComponent();
        _menu.Items.Add(_stations);
        _menu.Items.Add(new Separator());
        AddMenuAction("Nastavenia", () => SettingsRequested?.Invoke(this, EventArgs.Empty));
        AddMenuAction("Aktualizácie", () => UpdateRequested?.Invoke(this, EventArgs.Empty));
        _menu.Items.Add(new Separator());
        AddMenuAction("Presunúť widget (alebo Shift + potiahnuť)",
            () =>
            {
                _moveOnNextClick = true;
                Cursor = Cursors.SizeAll;
            });
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

    private void ShowWidgetMenu(object sender, MouseButtonEventArgs e)
    {
        if (e.ChangedButton != MouseButton.Left) return;
        e.Handled = true;

        if (_moveOnNextClick || Keyboard.Modifiers.HasFlag(ModifierKeys.Shift))
        {
            _moveOnNextClick = false;
            Cursor = Cursors.Arrow;
            try { DragMove(); }
            catch (InvalidOperationException) { }
            finally { PositionCommitted?.Invoke(this, EventArgs.Empty); }
            return;
        }

        _menu.PlacementTarget = sender as UIElement;
        _menu.Placement = PlacementMode.MousePoint;
        _menu.IsOpen = true;
    }
}
