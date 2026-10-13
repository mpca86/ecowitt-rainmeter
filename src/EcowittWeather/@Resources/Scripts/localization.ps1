# Shared localization helper for Rainmeter-launched PowerShell scripts.
# The locale is allowlisted before it is used to select a compiled language file.

function Get-EcowittUiCatalog {
    param(
        [Parameter(Mandatory=$true)]
        [string]$SkinPath,
        [string]$UiLanguage = "en"
    )

    $allowed = @("sk", "en", "cs", "pl")
    $locale = if ($allowed -contains $UiLanguage.ToLowerInvariant()) {
        $UiLanguage.ToLowerInvariant()
    }
    else {
        "en"
    }

    $locales = @($locale)
    if ($locale -ne "en") { $locales += "en" }

    foreach ($candidate in $locales) {
        $path = Join-Path $SkinPath ("@Resources\Languages\" + $candidate + ".inc")
        if (-not (Test-Path -LiteralPath $path -PathType Leaf)) { continue }

        try {
            $contents = [System.IO.File]::ReadAllText($path, [System.Text.Encoding]::Unicode)
            $values = @{}
            $inVariables = $false
            foreach ($line in ($contents -split "`r?`n")) {
                if ($line -match '^\s*\[([^\]]+)\]\s*$') {
                    $inVariables = $Matches[1] -ieq "Variables"
                    continue
                }
                if ($inVariables -and $line -match '^\s*(L_[A-Za-z0-9_]+)=(.*)$') {
                    $values[$Matches[1]] = $Matches[2]
                }
            }
            if ($values.Count -gt 0) { return $values }
        }
        catch {
            # Try the safe English catalog if a localized include is unreadable.
        }
    }

    return @{}
}

function Get-EcowittUiText {
    param(
        [Parameter(Mandatory=$true)]
        [string]$Key,
        [hashtable]$Values = @{}
    )

    $variable = "L_" + ($Key -replace '[.]', '_')
    $text = [string]$script:EcowittUiCatalog[$variable]
    if ([string]::IsNullOrWhiteSpace($text)) { $text = $Key }

    foreach ($name in $Values.Keys) {
        $text = $text.Replace("{$name}", [string]$Values[$name])
    }
    return $text
}
