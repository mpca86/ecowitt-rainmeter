using System.Diagnostics;
using System.Reflection;
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
        VersionText.Text = "Verzia: " + (attr?.InformationalVersion ?? "vývojová");
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
