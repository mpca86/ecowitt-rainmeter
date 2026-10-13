# Ecowitt Weather configuration export / import
# Exports only local user configuration and Cloud API credentials.
# The .ecowittconfig file is a ZIP container and is NOT encrypted.

param(
    [Parameter(Mandatory=$true)]
    [ValidateSet("Export","Import")]
    [string]$Action,

    [Parameter(Mandatory=$true)]
    [string]$SkinPath,

    [ValidateSet("sk","en","cs","pl")]
    [string]$UiLanguage = "en"
)

$ErrorActionPreference = "Stop"
$ProgressPreference = "SilentlyContinue"
$SkinPath = [System.IO.Path]::GetFullPath($SkinPath)
. (Join-Path $PSScriptRoot "localization.ps1")
$script:EcowittUiCatalog = Get-EcowittUiCatalog -SkinPath $SkinPath -UiLanguage $UiLanguage
$nl = [Environment]::NewLine

$Utf8NoBom = New-Object System.Text.UTF8Encoding($false)
[Console]::OutputEncoding = $Utf8NoBom
$OutputEncoding = $Utf8NoBom

Add-Type -AssemblyName System.Windows.Forms
Add-Type -AssemblyName System.IO.Compression
Add-Type -AssemblyName System.IO.Compression.FileSystem

$IncludesPath = Join-Path $SkinPath "@Resources\Includes"
$SecretsPath = Join-Path $IncludesPath "CloudSecrets.inc"
$UserPath = Join-Path $IncludesPath "UserVariables.inc"
$VariablesPath = Join-Path $IncludesPath "Variables.inc"
$BackupRoot = Join-Path $env:LOCALAPPDATA "EcowittRainmeter\ConfigBackups"

function Read-IniValue {
    param([string]$Path, [string]$Name)

    if (-not (Test-Path -LiteralPath $Path -PathType Leaf)) { return "" }

    foreach ($line in Get-Content -LiteralPath $Path -ErrorAction Stop) {
        if ($line -match "^\s*$([regex]::Escape($Name))\s*=(.*)$") {
            return $Matches[1].Trim()
        }
    }
    return ""
}

function Find-RainmeterExe {
    $pf86 = [Environment]::GetFolderPath("ProgramFilesX86")
    $candidates = @(
        (Join-Path $env:ProgramFiles "Rainmeter\Rainmeter.exe"),
        (Join-Path $pf86 "Rainmeter\Rainmeter.exe"),
        (Join-Path $env:LOCALAPPDATA "Programs\Rainmeter\Rainmeter.exe")
    )

    return $candidates |
        Where-Object { $_ -and (Test-Path -LiteralPath $_ -PathType Leaf) } |
        Select-Object -First 1
}

function Refresh-Rainmeter {
    $exe = Find-RainmeterExe
    if ($exe) {
        Start-Process -FilePath $exe -ArgumentList "!RefreshApp" -WindowStyle Hidden
    }
}

function Show-Info([string]$Text) {
    [System.Windows.Forms.MessageBox]::Show(
        $Text,
        "Ecowitt Weather",
        [System.Windows.Forms.MessageBoxButtons]::OK,
        [System.Windows.Forms.MessageBoxIcon]::Information
    ) | Out-Null
}

function Show-Warning([string]$Text) {
    [System.Windows.Forms.MessageBox]::Show(
        $Text,
        "Ecowitt Weather",
        [System.Windows.Forms.MessageBoxButtons]::OK,
        [System.Windows.Forms.MessageBoxIcon]::Warning
    ) | Out-Null
}

function Backup-CurrentConfig {
    $stamp = [DateTime]::Now.ToString("yyyyMMdd-HHmmss")
    $dir = Join-Path $BackupRoot $stamp
    New-Item -ItemType Directory -Path $dir -Force | Out-Null

    if (Test-Path -LiteralPath $SecretsPath -PathType Leaf) {
        Copy-Item -LiteralPath $SecretsPath -Destination (Join-Path $dir "CloudSecrets.inc") -Force
    }

    if (Test-Path -LiteralPath $UserPath -PathType Leaf) {
        Copy-Item -LiteralPath $UserPath -Destination (Join-Path $dir "UserVariables.inc") -Force
    }

    return $dir
}

function Copy-ZipEntryToFile {
    param(
        [System.IO.Compression.ZipArchiveEntry]$Entry,
        [string]$Destination
    )

    $input = $Entry.Open()
    try {
        $output = [System.IO.File]::Open(
            $Destination,
            [System.IO.FileMode]::Create,
            [System.IO.FileAccess]::Write,
            [System.IO.FileShare]::None
        )
        try {
            $input.CopyTo($output)
        }
        finally {
            $output.Dispose()
        }
    }
    finally {
        $input.Dispose()
    }
}

try {
    if (-not (Test-Path -LiteralPath $SkinPath -PathType Container)) {
        throw (Get-EcowittUiText -Key "settings.transfer_skin_missing" -Values @{ path = $SkinPath })
    }

    if ($Action -eq "Export") {
        if (-not (Test-Path -LiteralPath $SecretsPath -PathType Leaf)) {
            throw (Get-EcowittUiText -Key "settings.transfer_secrets_missing")
        }
        if (-not (Test-Path -LiteralPath $UserPath -PathType Leaf)) {
            throw (Get-EcowittUiText -Key "settings.transfer_variables_missing")
        }

        $dialog = New-Object System.Windows.Forms.SaveFileDialog
        $dialog.Title = Get-EcowittUiText -Key "settings.transfer_export_title"
        $dialog.Filter = Get-EcowittUiText -Key "settings.transfer_export_filter"
        $dialog.DefaultExt = "ecowittconfig"
        $dialog.AddExtension = $true
        $dialog.FileName = "EcowittWeather-config-" + [DateTime]::Now.ToString("yyyyMMdd-HHmm") + ".ecowittconfig"
        $dialog.InitialDirectory = [Environment]::GetFolderPath("Desktop")

        if ($dialog.ShowDialog() -ne [System.Windows.Forms.DialogResult]::OK) {
            Write-Output ("CANCEL|" + (Get-EcowittUiText -Key "settings.transfer_cancel_export"))
            exit 0
        }

        $work = Join-Path ([System.IO.Path]::GetTempPath()) ("ecowitt-config-" + [guid]::NewGuid().ToString("N"))
        $stage = Join-Path $work "stage"
        $zipPath = Join-Path $work "config.zip"
        New-Item -ItemType Directory -Path $stage -Force | Out-Null

        Copy-Item -LiteralPath $SecretsPath -Destination (Join-Path $stage "CloudSecrets.inc") -Force
        Copy-Item -LiteralPath $UserPath -Destination (Join-Path $stage "UserVariables.inc") -Force

        $manifest = [ordered]@{
            app = "EcowittWeather"
            schemaVersion = 1
            createdAt = [DateTime]::Now.ToString("o")
            sourceVersion = (Read-IniValue -Path $VariablesPath -Name "SkinVersion")
            containsSecrets = $true
        } | ConvertTo-Json -Depth 3

        [System.IO.File]::WriteAllText(
            (Join-Path $stage "manifest.json"),
            $manifest,
            $Utf8NoBom
        )

        [System.IO.Compression.ZipFile]::CreateFromDirectory(
            $stage,
            $zipPath,
            [System.IO.Compression.CompressionLevel]::Optimal,
            $false
        )

        if (Test-Path -LiteralPath $dialog.FileName -PathType Leaf) {
            Remove-Item -LiteralPath $dialog.FileName -Force
        }
        Move-Item -LiteralPath $zipPath -Destination $dialog.FileName -Force
        Remove-Item -LiteralPath $work -Recurse -Force -ErrorAction SilentlyContinue

        $exportNotice = Get-EcowittUiText -Key "settings.transfer_export_notice" -Values @{ newline = $nl }
        Show-Warning $exportNotice
        Write-Output ("OK|" + (Get-EcowittUiText -Key "settings.transfer_export_complete"))
        exit 0
    }

    $dialog = New-Object System.Windows.Forms.OpenFileDialog
    $dialog.Title = Get-EcowittUiText -Key "settings.transfer_import_title"
    $dialog.Filter = Get-EcowittUiText -Key "settings.transfer_filter"
    $dialog.CheckFileExists = $true
    $dialog.Multiselect = $false

    if ($dialog.ShowDialog() -ne [System.Windows.Forms.DialogResult]::OK) {
        Write-Output ("CANCEL|" + (Get-EcowittUiText -Key "settings.transfer_cancel_import"))
        exit 0
    }

    $archive = [System.IO.Compression.ZipFile]::OpenRead($dialog.FileName)
    try {
        $manifestEntry = $archive.Entries |
            Where-Object { $_.FullName -ieq "manifest.json" } |
            Select-Object -First 1
        $secretsEntry = $archive.Entries |
            Where-Object { $_.FullName -ieq "CloudSecrets.inc" } |
            Select-Object -First 1
        $userEntry = $archive.Entries |
            Where-Object { $_.FullName -ieq "UserVariables.inc" } |
            Select-Object -First 1

        if (-not $manifestEntry -or -not $secretsEntry -or -not $userEntry) {
            throw (Get-EcowittUiText -Key "settings.transfer_invalid_archive")
        }

        $reader = New-Object System.IO.StreamReader($manifestEntry.Open(), [System.Text.Encoding]::UTF8)
        try {
            $manifest = ($reader.ReadToEnd() | ConvertFrom-Json)
        }
        finally {
            $reader.Dispose()
        }

        if ([string]$manifest.app -ne "EcowittWeather") {
            throw (Get-EcowittUiText -Key "settings.transfer_wrong_app")
        }

        if ([int]$manifest.schemaVersion -ne 1) {
            throw (Get-EcowittUiText -Key "settings.transfer_unsupported_version" -Values @{ version = [string]$manifest.schemaVersion })
        }

        $backup = Backup-CurrentConfig
        New-Item -ItemType Directory -Path $IncludesPath -Force | Out-Null

        Copy-ZipEntryToFile -Entry $secretsEntry -Destination $SecretsPath
        Copy-ZipEntryToFile -Entry $userEntry -Destination $UserPath
    }
    finally {
        $archive.Dispose()
    }

    $importNotice = Get-EcowittUiText -Key "settings.transfer_import_notice" -Values @{ newline = $nl; backup = $backup }
    Show-Info $importNotice
    Write-Output ("OK|" + (Get-EcowittUiText -Key "settings.transfer_import_complete"))
    Start-Sleep -Milliseconds 300
    Refresh-Rainmeter
    exit 0
}
catch {
    $message = $_.Exception.Message -replace "[\r\n]+"," "
    $failure = Get-EcowittUiText -Key "settings.transfer_failure" -Values @{ newline = $nl; message = $message }
    Show-Warning $failure
    Write-Output ("ERROR|" + $failure)
    exit 1
}
