# Resolve the Ecowitt Rainmeter UI language without altering either INI file.
# This standalone resolver is used by the forthcoming language-selection runtime.
# Rainmeter.ini [Rainmeter] Language contains the UI LCID, not the Windows locale.
param(
    [Parameter(Mandatory = $true)]
    [string]$RainmeterIni,
    [Parameter(Mandatory = $true)]
    [string]$UserVariablesIni
)

$ErrorActionPreference = "Stop"

function Get-IniValue {
    param(
        [string]$Path,
        [string]$Section,
        [string]$Key
    )
    if (-not [System.IO.File]::Exists($Path)) { return $null }

    try {
        # ReadAllLines detects UTF-16 LE/BE BOM as well as UTF-8 BOM.
        $lines = [System.IO.File]::ReadAllLines($Path)
    } catch {
        return $null
    }

    $inSection = $false
    $found = $null
    foreach ($line in $lines) {
        $trimmed = $line.Trim()
        if ($trimmed -match '^\s*[;#]') { continue }

        if ($trimmed -match '^\[([^\]]+)\]\s*$') {
            $inSection = ($Matches[1] -ieq $Section)
            continue
        }

        if ($inSection -and $trimmed -match '^([^=]+)=(.*)$') {
            if ($Matches[1].Trim() -ieq $Key) {
                $found = $Matches[2].Trim()
            }
        }
    }
    return $found
}

function Resolve-EcowittLanguage {
    param([string]$UserMode, [string]$RainmeterLanguage)

    $mode = [string]$UserMode
    $mode = $mode.Trim().ToLowerInvariant()

    if ($mode -in @("sk", "en", "cs", "pl")) {
        return $mode
    }

    # Invalid/missing value safely falls back to automatic language detection.
    if ($RainmeterLanguage -notmatch '^\d{1,5}$') { return "en" }
    $lcid = [int]$RainmeterLanguage
    if ($lcid -lt 1 -or $lcid -gt 65535) { return "en" }

    # Windows LANGID: the low ten bits identify the primary language.
    # This accepts regional English variants as English, etc.
    switch ($lcid -band 0x3ff) {
        0x1b { return "sk" } # Slovak, 1051
        0x09 { return "en" } # English, 1033 and English variants
        0x05 { return "cs" } # Czech, 1029
        0x15 { return "pl" } # Polish, 1045
        default { return "en" }
    }
}

$userMode = Get-IniValue -Path $UserVariablesIni -Section "Variables" -Key "UiLanguage"
$rainmeterLcid = Get-IniValue -Path $RainmeterIni -Section "Rainmeter" -Key "Language"
Resolve-EcowittLanguage -UserMode $userMode -RainmeterLanguage $rainmeterLcid
