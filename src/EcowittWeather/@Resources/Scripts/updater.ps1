# Ecowitt Rainmeter self-updater
param(
    [ValidateSet("Check","Install")]
    [string]$Action = "Check",
    [ValidateSet("stable","beta","development")]
    [string]$Channel = "stable",
    [Parameter(Mandatory=$true)]
    [string]$SkinPath,
    [string]$Repository = "mpca86/ecowitt-rainmeter",
    [string]$DevelopmentBranch = "cloud-api",
    [string]$CurrentVersion = ""
)

$ErrorActionPreference = "Stop"
$ProgressPreference = "SilentlyContinue"
$SkinPath = [System.IO.Path]::GetFullPath($SkinPath)
$Headers = @{
    "User-Agent" = "Ecowitt-Rainmeter-Updater"
    "Accept" = "application/vnd.github+json"
}

$StatePath = Join-Path $SkinPath "@Resources\Update\UpdateState.inc"
$UpdateDir = Split-Path -Parent $StatePath
$BackupRoot = Join-Path $env:LOCALAPPDATA "EcowittRainmeter\Backups"

function Ensure-Directory([string]$Path) {
    if (-not (Test-Path -LiteralPath $Path)) {
        New-Item -ItemType Directory -Path $Path -Force | Out-Null
    }
}

function Escape-IniValue([string]$Value) {
    if ($null -eq $Value) { return "" }
    return ($Value -replace "[\r\n]+", " " -replace "#", "")
}

function Read-StateValue([string]$Name) {
    if (-not (Test-Path -LiteralPath $StatePath)) { return "" }
    $line = Get-Content -LiteralPath $StatePath -ErrorAction SilentlyContinue |
        Where-Object { $_ -match "^$([regex]::Escape($Name))=" } |
        Select-Object -First 1
    if (-not $line) { return "" }
    return ($line -split "=",2)[1]
}

function Write-State {
    param(
        [string]$Status,
        [string]$RemoteVersion = "",
        [int]$Available = 0,
        [string]$InstalledReleaseTag = "",
        [string]$InstalledCommit = "",
        [string]$LastError = ""
    )

    Ensure-Directory $UpdateDir

    if (-not $InstalledReleaseTag) { $InstalledReleaseTag = Read-StateValue "InstalledReleaseTag" }
    if (-not $InstalledCommit) { $InstalledCommit = Read-StateValue "InstalledCommit" }

    $content = @(
        "[Variables]",
        "UpdateStatus=$(Escape-IniValue $Status)",
        "RemoteVersion=$(Escape-IniValue $RemoteVersion)",
        "UpdateAvailable=$Available",
        "InstalledReleaseTag=$(Escape-IniValue $InstalledReleaseTag)",
        "InstalledCommit=$(Escape-IniValue $InstalledCommit)",
        "LastUpdateError=$(Escape-IniValue $LastError)",
        "LastUpdateCheck=$([DateTime]::Now.ToString('yyyy-MM-dd HH:mm:ss'))"
    ) -join [Environment]::NewLine

    [System.IO.File]::WriteAllText($StatePath, $content, [System.Text.Encoding]::Unicode)
}

function Get-Releases {
    $uri = "https://api.github.com/repos/$Repository/releases?per_page=30"
    return @(Invoke-RestMethod -Uri $uri -Headers $Headers -Method Get)
}

function Get-SelectedRelease {
    $releases = Get-Releases | Where-Object { -not $_.draft }
    if ($Channel -eq "stable") {
        return $releases | Where-Object { -not $_.prerelease } | Select-Object -First 1
    }
    if ($Channel -eq "beta") {
        return $releases | Where-Object { $_.prerelease } | Select-Object -First 1
    }
    return $null
}

function Get-DevelopmentInfo {
    $uri = "https://api.github.com/repos/$Repository/branches/$DevelopmentBranch"
    return Invoke-RestMethod -Uri $uri -Headers $Headers -Method Get
}

function Get-LocalGitHead {
    $git = Get-Command git -ErrorAction SilentlyContinue
    if (-not $git) { return "" }

    $candidates = @($SkinPath)
    try {
        $item = Get-Item -LiteralPath $SkinPath -ErrorAction Stop
        if ($item.Target) {
            foreach ($target in @($item.Target)) {
                if ($target) { $candidates += [string]$target }
            }
        }
    }
    catch {}

    foreach ($candidate in $candidates | Select-Object -Unique) {
        try {
            $head = & git -C $candidate rev-parse HEAD 2>$null
            if ($LASTEXITCODE -eq 0 -and $head) {
                return ([string]$head).Trim()
            }
        }
        catch {}
    }

    return ""
}

function Test-GitCheckout {
    $git = Get-Command git -ErrorAction SilentlyContinue
    if (-not $git) { return $false }
    try {
        $null = & git -C $SkinPath rev-parse --show-toplevel 2>$null
        return ($LASTEXITCODE -eq 0)
    }
    catch { return $false }
}

function Get-ReleaseAsset($Release) {
    if (-not $Release) { return $null }
    return $Release.assets | Where-Object { $_.name -match "^EcowittWeather.*\.zip$" } | Select-Object -First 1
}

function Get-ChecksumFromRelease($Release, $ZipAsset, [string]$DownloadDir) {
    if ($ZipAsset.digest -and $ZipAsset.digest -match "^sha256:(.+)$") {
        return $Matches[1].ToLowerInvariant()
    }

    $checksumAsset = $Release.assets |
        Where-Object { $_.name -eq ($ZipAsset.name + ".sha256") -or $_.name -eq "SHA256SUMS.txt" } |
        Select-Object -First 1

    if (-not $checksumAsset) { return "" }

    $checksumFile = Join-Path $DownloadDir $checksumAsset.name
    Invoke-WebRequest -Uri $checksumAsset.browser_download_url -Headers $Headers -OutFile $checksumFile

    foreach ($line in Get-Content -LiteralPath $checksumFile) {
        if ($line -match "^([A-Fa-f0-9]{64})\s+\*?(.+)$") {
            if (($Matches[2]).Trim() -eq $ZipAsset.name -or $checksumAsset.name -ne "SHA256SUMS.txt") {
                return $Matches[1].ToLowerInvariant()
            }
        }
    }
    return ""
}

function Find-PackageRoot([string]$ExtractPath, [bool]$Development) {
    if ($Development) {
        $top = Get-ChildItem -LiteralPath $ExtractPath -Directory | Select-Object -First 1
        if ($top) {
            $candidate = Join-Path $top.FullName "src\EcowittWeather"
            if (Test-Path -LiteralPath $candidate -PathType Container) { return $candidate }
        }
    }

    $direct = Join-Path $ExtractPath "EcowittWeather"
    if (Test-Path -LiteralPath $direct -PathType Container) { return $direct }

    foreach ($dir in Get-ChildItem -LiteralPath $ExtractPath -Directory -Recurse) {
        if ($dir.Name -eq "EcowittWeather" -and
            (Test-Path -LiteralPath (Join-Path $dir.FullName "Meteo")) -and
            (Test-Path -LiteralPath (Join-Path $dir.FullName "@Resources"))) {
            return $dir.FullName
        }
    }

    if ((Test-Path -LiteralPath (Join-Path $ExtractPath "Meteo")) -and
        (Test-Path -LiteralPath (Join-Path $ExtractPath "@Resources"))) {
        return $ExtractPath
    }

    throw "V stiahnutom balíku sa nenašiel koreň EcowittWeather."
}

function Preserve-UserFiles([string]$TempPreserve) {
    $paths = @(
        "@Resources\Includes\CloudSecrets.inc",
        "@Resources\Includes\UserVariables.inc",
        "Meteo\meteo_history.csv",
        "@Resources\Diagnostics\ecowitt_cloud_debug.txt",
        "@Resources\Update\UpdateState.inc"
    )

    foreach ($relative in $paths) {
        $source = Join-Path $SkinPath $relative
        if (Test-Path -LiteralPath $source -PathType Leaf) {
            $dest = Join-Path $TempPreserve $relative
            Ensure-Directory (Split-Path -Parent $dest)
            Copy-Item -LiteralPath $source -Destination $dest -Force
        }
    }
}

function Restore-UserFiles([string]$TempPreserve) {
    if (-not (Test-Path -LiteralPath $TempPreserve)) { return }
    Get-ChildItem -LiteralPath $TempPreserve -File -Recurse | ForEach-Object {
        $relative = $_.FullName.Substring($TempPreserve.Length).TrimStart("\")
        $dest = Join-Path $SkinPath $relative
        Ensure-Directory (Split-Path -Parent $dest)
        Copy-Item -LiteralPath $_.FullName -Destination $dest -Force
    }
}

function Backup-CurrentSkin {
    Ensure-Directory $BackupRoot
    $stamp = [DateTime]::Now.ToString("yyyyMMdd-HHmmss")
    $zip = Join-Path $BackupRoot "EcowittWeather-$stamp.zip"
    Compress-Archive -Path (Join-Path $SkinPath "*") -DestinationPath $zip -CompressionLevel Optimal -Force
    return $zip
}

function Refresh-Rainmeter {
    $pf86 = [Environment]::GetFolderPath("ProgramFilesX86")
    $candidates = @(
        (Join-Path $env:ProgramFiles "Rainmeter\Rainmeter.exe"),
        (Join-Path $pf86 "Rainmeter\Rainmeter.exe"),
        (Join-Path $env:LOCALAPPDATA "Programs\Rainmeter\Rainmeter.exe")
    ) | Where-Object { $_ -and (Test-Path -LiteralPath $_ -PathType Leaf) }

    $exe = $candidates | Select-Object -First 1
    if ($exe) { Start-Process -FilePath $exe -ArgumentList "!RefreshApp" -WindowStyle Hidden }
}

try {
    if (-not (Test-Path -LiteralPath $SkinPath -PathType Container)) {
        throw "SkinPath neexistuje: $SkinPath"
    }

    if ($Action -eq "Check") {
        if ($Channel -eq "development") {
            $info = Get-DevelopmentInfo
            $sha = [string]$info.commit.sha
            $short = $sha.Substring(0,7)

            $installed = Get-LocalGitHead
            if ([string]::IsNullOrWhiteSpace($installed)) {
                $installed = Read-StateValue "InstalledCommit"
            }

            $available = [int]($sha -ne $installed)
            if ($available) {
                $status = "Dostupný development build $short"
                $result = "UPDATE|dev-$short"
            }
            else {
                $status = "Development build je aktuálny ($short)"
                $result = "CURRENT|dev-$short"
            }

            Write-State -Status $status -RemoteVersion "dev-$short" -Available $available -InstalledCommit $installed
            Write-Output $result
            exit 0
        }

        $release = Get-SelectedRelease
        if (-not $release) {
            Write-State -Status "Pre kanál $Channel zatiaľ nie je vydaná verzia." -Available 0
            Write-Output "CURRENT|--"
            exit 0
        }

        $tag = [string]$release.tag_name
        $installedTag = Read-StateValue "InstalledReleaseTag"
        $normalizedCurrent = $CurrentVersion.TrimStart("v")
        $normalizedTag = $tag.TrimStart("v")
        $available = [int](($tag -ne $installedTag) -and ($normalizedCurrent -ne $normalizedTag))

        if ($available) {
            $status = "Dostupná verzia $tag"
            $result = "UPDATE|$tag"
        }
        else {
            $status = "Používaš aktuálnu verziu $tag"
            $result = "CURRENT|$tag"
        }

        Write-State -Status $status -RemoteVersion $tag -Available $available
        Write-Output $result
        exit 0
    }

    if (Test-GitCheckout) {
        Write-State -Status "Táto inštalácia je spravovaná Gitom. Použi update-ecowitt-cloud.ps1." -Available 0
        exit 2
    }

    $work = Join-Path ([System.IO.Path]::GetTempPath()) ("ecowitt-update-" + [guid]::NewGuid().ToString("N"))
    $downloadDir = Join-Path $work "download"
    $extractDir = Join-Path $work "extract"
    $preserveDir = Join-Path $work "preserve"
    Ensure-Directory $downloadDir
    Ensure-Directory $extractDir
    Ensure-Directory $preserveDir

    $isDev = $Channel -eq "development"
    $release = $null
    $remoteVersion = ""
    $installedReleaseTag = ""
    $installedCommit = ""
    $zipPath = Join-Path $downloadDir "EcowittWeather-update.zip"

    if ($isDev) {
        $info = Get-DevelopmentInfo
        $installedCommit = [string]$info.commit.sha
        $remoteVersion = "dev-" + $installedCommit.Substring(0,7)
        $url = "https://github.com/$Repository/archive/refs/heads/$DevelopmentBranch.zip"
        Invoke-WebRequest -Uri $url -Headers $Headers -OutFile $zipPath
    }
    else {
        $release = Get-SelectedRelease
        if (-not $release) { throw "Pre kanál $Channel nie je dostupný release." }
        $asset = Get-ReleaseAsset $release
        if (-not $asset) { throw "Release $($release.tag_name) neobsahuje EcowittWeather ZIP asset." }

        $installedReleaseTag = [string]$release.tag_name
        $remoteVersion = $installedReleaseTag
        $zipPath = Join-Path $downloadDir $asset.name
        Invoke-WebRequest -Uri $asset.browser_download_url -Headers $Headers -OutFile $zipPath

        $expectedHash = Get-ChecksumFromRelease $release $asset $downloadDir
        if ($expectedHash) {
            $actualHash = (Get-FileHash -LiteralPath $zipPath -Algorithm SHA256).Hash.ToLowerInvariant()
            if ($actualHash -ne $expectedHash) { throw "SHA256 kontrola zlyhala. Balík nebude nainštalovaný." }
        }
    }

    Expand-Archive -LiteralPath $zipPath -DestinationPath $extractDir -Force
    $packageRoot = Find-PackageRoot $extractDir $isDev

    $backup = Backup-CurrentSkin
    Preserve-UserFiles $preserveDir
    Copy-Item -Path (Join-Path $packageRoot "*") -Destination $SkinPath -Recurse -Force
    Restore-UserFiles $preserveDir

    $status = "Aktualizované na $remoteVersion. Backup: $backup"
    Write-State -Status $status -RemoteVersion $remoteVersion -Available 0 -InstalledReleaseTag $installedReleaseTag -InstalledCommit $installedCommit

    Remove-Item -LiteralPath $work -Recurse -Force -ErrorAction SilentlyContinue
    Refresh-Rainmeter
    exit 0
}
catch {
    $message = $_.Exception.Message
    Write-State -Status "Aktualizácia zlyhala: $message" -Available 0 -LastError $message
    Write-Output ("ERROR|" + $message)
    exit 1
}
