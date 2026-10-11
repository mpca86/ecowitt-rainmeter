# Standalone, Pester-free tests for Rainmeter UI language resolution.
$ErrorActionPreference = "Stop"
$script = Join-Path $PSScriptRoot "..\resolve-rainmeter-language.ps1"
if (-not (Test-Path -LiteralPath $script)) { throw "Resolver is missing" }

$cases = @(
    @{ name="Slovak auto"; rain="1051"; mode="auto"; expected="sk" },
    @{ name="English auto"; rain="1033"; mode="auto"; expected="en" },
    @{ name="Czech auto"; rain="1029"; mode="auto"; expected="cs" },
    @{ name="Polish auto"; rain="1045"; mode="auto"; expected="pl" },
    @{ name="English UK auto"; rain="2057"; mode="auto"; expected="en" },
    @{ name="German unsupported"; rain="1031"; mode="auto"; expected="en" },
    @{ name="French unsupported"; rain="1036"; mode="auto"; expected="en" },
    @{ name="Russian unsupported"; rain="1049"; mode="auto"; expected="en" },
    @{ name="Bad Rainmeter language"; rain="nonsense"; mode="auto"; expected="en" },
    @{ name="Manual PL overrides SK"; rain="1051"; mode="pl"; expected="pl" },
    @{ name="Manual CS overrides EN"; rain="1033"; mode="cs"; expected="cs" },
    @{ name="Manual EN overrides SK"; rain="1051"; mode="en"; expected="en" },
    @{ name="Manual SK overrides unsupported"; rain="1031"; mode="sk"; expected="sk" },
    @{ name="Invalid preference uses Rainmeter"; rain="1045"; mode="xx"; expected="pl" },
    @{ name="Uppercase auto"; rain="1029"; mode="AUTO"; expected="cs" }
)

$work = Join-Path ([System.IO.Path]::GetTempPath()) ("ecowitt-lang-test-" + [Guid]::NewGuid().ToString("N"))
New-Item -ItemType Directory -Path $work -Force | Out-Null
try {
    $rm = Join-Path $work "Rainmeter.ini"
    $user = Join-Path $work "UserVariables.inc"
    foreach ($case in $cases) {
        # Test the native Rainmeter UTF-16 LE format with a BOM.
        [System.IO.File]::WriteAllText(
            $rm,
            "[Other]`r`nLanguage=1049`r`n[Rainmeter]`r`nLanguage=$($case.rain)`r`n",
            [System.Text.Encoding]::Unicode
        )
        [System.IO.File]::WriteAllText(
            $user,
            "[Variables]`r`nUiLanguage=$($case.mode)`r`nChannel1Label=Pracovňa`r`n",
            [System.Text.Encoding]::Unicode
        )
        $beforeRm = (Get-FileHash -LiteralPath $rm -Algorithm SHA256).Hash
        $beforeUser = (Get-FileHash -LiteralPath $user -Algorithm SHA256).Hash
        $actual = & $script -RainmeterIni $rm -UserVariablesIni $user
        if ($actual -ne $case.expected) {
            throw "$($case.name): expected $($case.expected), got $actual"
        }
        if ((Get-FileHash -LiteralPath $rm -Algorithm SHA256).Hash -ne $beforeRm) {
            throw "$($case.name): Rainmeter.ini was modified"
        }
        if ((Get-FileHash -LiteralPath $user -Algorithm SHA256).Hash -ne $beforeUser) {
            throw "$($case.name): UserVariables.inc was modified"
        }
        Write-Output "PASS: $($case.name) -> $actual"
    }

    # No configuration or missing / corrupt files must never crash.
    $absent = Join-Path $work "missing.ini"
    $fallback = & $script -RainmeterIni $absent -UserVariablesIni $absent
    if ($fallback -ne "en") { throw "Missing files should fall back to en, got $fallback" }
    Write-Output "PASS: missing files -> en"
}
finally {
    Remove-Item -LiteralPath $work -Recurse -Force -ErrorAction SilentlyContinue
}
