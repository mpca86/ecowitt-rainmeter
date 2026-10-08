using System.Diagnostics;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Windows;
using MessageBox = System.Windows.MessageBox;

namespace EcowittWeather.Desktop.About;

public partial class AboutWindow : Window
{
    public AboutWindow()
    {
        InitializeComponent();
        var attr = Assembly.GetExecutingAssembly()
            .GetCustomAttribute<AssemblyInformationalVersionAttribute>();
        var fullVersion = attr?.InformationalVersion ?? "vývojová";
        var parts = fullVersion.Split('+', 2);
        // Show a readable build identifier while retaining the full version in a tooltip.
        var shortCommit = parts.Length == 2 && parts[1].Length >= 7
            ? " · build " + parts[1][..7] : "";
        VersionText.Text = "Verzia: " + parts[0] + shortCommit;
        VersionText.ToolTip = fullVersion;
        // Never hard-code the framework version in the About dialog.
        TechnologyText.Text = "Technológia: " + RuntimeInformation.FrameworkDescription +
                              " · WPF · Ecowitt Web API v3";
    }

    private void CloseClick(object sender, RoutedEventArgs e) => Close();

    private void GithubClick(object sender, RoutedEventArgs e) =>
        OpenUrl("https://github.com/mpca86/ecowitt-rainmeter");

    private void WebsiteClick(object sender, RoutedEventArgs e) =>
        OpenUrl("https://martinsturcel.sk");

    private void OpenUrl(string url)
    {
        try
        {
            Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
        }
        catch
        {
            MessageBox.Show(this, "Odkaz sa nepodarilo otvoriť: " + url,
                "Ecowitt Weather", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }
}
