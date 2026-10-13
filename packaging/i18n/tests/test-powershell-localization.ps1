$ErrorActionPreference = "Stop"

$skinRoot = (Resolve-Path (Join-Path $PSScriptRoot "../../../src/EcowittWeather")).Path
$helper = Join-Path $skinRoot "@Resources/Scripts/localization.ps1"
. $helper

$expectedMissing = @{
    sk = "Chýbajú údaje"
    en = "Missing details"
    cs = "Chybí údaje"
    pl = "Brakujące dane"
}

foreach ($locale in @("sk", "en", "cs", "pl")) {
    $catalog = Get-EcowittUiCatalog -SkinPath $skinRoot -UiLanguage $locale
    if ($catalog.Count -lt 121) { throw "${locale}: expected the expanded catalog, got $($catalog.Count) values" }
    $script:EcowittUiCatalog = $catalog

    $missing = Get-EcowittUiText -Key "settings.cloud_test_missing" -Values @{ items = "API Key, MAC" }
    if (-not $missing.StartsWith($expectedMissing[$locale])) { throw "${locale}: localized missing-fields text is wrong: $missing" }
    if ($missing.Contains("{items}")) { throw "${locale}: placeholder was not replaced" }

    $status = Get-EcowittUiText -Key "updates.status_available" -Values @{ version = "v1.2.3" }
    if (-not $status.Contains("v1.2.3") -or $status.Contains("{version}")) {
        throw "${locale}: version placeholder was not replaced: $status"
    }

    $notice = Get-EcowittUiText -Key "settings.transfer_import_notice" -Values @{
        newline = [Environment]::NewLine
        backup = "C:\Sandbox\backup"
    }
    if (-not $notice.Contains("C:\Sandbox\backup") -or $notice.Contains("{backup}")) {
        throw "${locale}: import backup placeholder was not replaced"
    }

    Write-Output "PASS: $locale PowerShell UI strings and placeholders"
}

$fallback = Get-EcowittUiCatalog -SkinPath $skinRoot -UiLanguage "unsupported"
$script:EcowittUiCatalog = $fallback
$fallbackText = Get-EcowittUiText -Key "settings.cloud_test_missing" -Values @{ items = "API Key" }
if (-not $fallbackText.StartsWith("Missing details")) { throw "Unsupported locale did not use English fallback" }
Write-Output "PASS: unsupported PowerShell locale falls back to English"
