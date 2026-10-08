using System.Windows;
using System.Windows.Input;

namespace EcowittWeather.Desktop.Widgets;

public partial class WeatherWidget : Window
{
    public event EventHandler? SettingsRequested;
    public event EventHandler? RemoveRequested;

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
        }
    }

    private void OpenSettings(object sender, RoutedEventArgs e) =>
        SettingsRequested?.Invoke(this, EventArgs.Empty);

    private void RemoveWidget(object sender, RoutedEventArgs e) =>
        RemoveRequested?.Invoke(this, EventArgs.Empty);
}
