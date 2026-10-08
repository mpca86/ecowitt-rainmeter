# This is a structural regression check, not a pixel-perfect screenshot test.
# One shared menu style must cover the root menu, submenu items and checkmarks.
$ErrorActionPreference = 'Stop'

$desktop = Split-Path -Parent $PSScriptRoot
$appPath = Join-Path $desktop 'EcowittWeather.Desktop/App.xaml'
$widgetPath = Join-Path $desktop 'EcowittWeather.Desktop/Widgets/WeatherWidget.xaml'
$codePath = Join-Path $desktop 'EcowittWeather.Desktop/Widgets/WeatherWidget.xaml.cs'
$settingsPath = Join-Path $desktop 'EcowittWeather.Desktop/Settings/SettingsWindow.xaml'

[xml]$app = Get-Content -LiteralPath $appPath -Raw
[xml]$widget = Get-Content -LiteralPath $widgetPath -Raw
[xml]$settings = Get-Content -LiteralPath $settingsPath -Raw
$widgetCode = Get-Content -LiteralPath $codePath -Raw

function Assert($condition, [string]$message) {
    if (-not $condition) { throw "Desktop UI contract failed: $message" }
}

$ctxStyle = $app.SelectSingleNode("//*[local-name()='Style' and @TargetType='{x:Type ContextMenu}']")
$itemStyle = $app.SelectSingleNode("//*[local-name()='Style' and @TargetType='{x:Type MenuItem}']")
Assert ($null -ne $ctxStyle) 'shared ContextMenu theme must be defined'
Assert ($null -ne $itemStyle) 'shared MenuItem theme must be defined'
Assert ($null -ne $ctxStyle.SelectSingleNode(".//*[local-name()='ControlTemplate']")) 'ContextMenu must override Windows chrome'
Assert ($null -ne $itemStyle.SelectSingleNode(".//*[local-name()='ControlTemplate']")) 'MenuItem must override light selection/checkmark gutter'
Assert ($null -ne $itemStyle.SelectSingleNode(".//*[local-name()='Popup' and @*[local-name()='Name' and .='PART_Popup']]")) 'submenus must share dark Popup chrome'

$border = $widget.SelectSingleNode("//*[local-name()='Window']/*[local-name()='Border']")
Assert ($null -ne $border) 'widget root border missing'
Assert ($border.GetAttribute('MouseLeftButtonDown') -eq 'DragWidget') 'left drag must move widget'
Assert ($border.GetAttribute('MouseRightButtonUp') -eq 'ShowWidgetMenu') 'right click must show widget menu'
Assert ($null -eq $widget.SelectSingleNode("//*[local-name()='Button']")) 'weather widget should contain no visible header buttons'
Assert ($widgetCode -match 'MouseButton.Right' -and $widgetCode -match 'DragMove\(\)') 'mouse handlers do not match intent'

Assert ($null -eq $border.Attributes['ToolTip']) 'widget hover tooltip must be absent'
foreach ($tab in @('Začíname', 'Stanice', 'Pripojenie', 'Senzory', 'Aplikácia')) {
    Assert ($null -ne $settings.SelectSingleNode("//*[local-name()='TabItem' and @Header='$tab']")) "settings tab missing: $tab"
}
Assert ($null -ne $settings.SelectSingleNode("//*[local-name()='Button' and @Click='GuideClick']")) 'API key help link missing'
Assert ($null -ne $settings.SelectSingleNode("//*[local-name()='Button' and @Click='TestConnectionClick']")) 'connection preflight button missing'
Assert ($null -ne $settings.SelectSingleNode("//*[local-name()='ComboBox' and @Name='SourceModeCombo']")) 'source-mode picker missing'
Assert ($null -ne $settings.SelectSingleNode("//*[local-name()='TextBox' and @Name='LocalGatewayBox']")) 'Local API gateway field missing'

Write-Host 'PASS: shared dark menu and widget gesture contract'
