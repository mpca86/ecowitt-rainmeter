param(
    [Parameter(Mandatory=$true)][int]$ParentPid,
    [Parameter(Mandatory=$true)][string]$Source,
    [Parameter(Mandatory=$true)][string]$Destination,
    [Parameter(Mandatory=$true)][string]$Executable
)

$ErrorActionPreference = "Stop"
$ProgressPreference = "SilentlyContinue"
$logDir = Join-Path $env:LOCALAPPDATA "EcowittWeather\Desktop"
New-Item -ItemType Directory -Force -Path $logDir | Out-Null
$log = Join-Path $logDir "update.log"
$backupRoot = Join-Path $logDir "UpdateBackups"
$backup = Join-Path $backupRoot (Get-Date -Format "yyyyMMdd-HHmmss")
$created = New-Object System.Collections.Generic.List[string]
$completed = $false

function Log([string]$message) {
    Add-Content -LiteralPath $log -Value ("$(Get-Date -Format o) " + $message)
}

try {
    # The parent app must release the executable before we replace it.
    for ($retry = 0; $retry -lt 90; $retry++) {
        if (-not (Get-Process -Id $ParentPid -ErrorAction SilentlyContinue)) { break }
        Start-Sleep -Seconds 1
    }
    if (Get-Process -Id $ParentPid -ErrorAction SilentlyContinue) {
        throw "Pôvodná aplikácia sa nestihla ukončiť."
    }

    $Source = [System.IO.Path]::GetFullPath($Source)
    $Destination = [System.IO.Path]::GetFullPath($Destination)
    $exePath = Join-Path $Destination $Executable

    if (-not (Test-Path -LiteralPath (Join-Path $Source $Executable) -PathType Leaf)) {
        throw "Nový balík neobsahuje očakávaný spustiteľný súbor."
    }
    if (-not (Test-Path -LiteralPath $exePath -PathType Leaf)) {
        throw "Pôvodný spustiteľný súbor neexistuje."
    }

    $files = @(Get-ChildItem -LiteralPath $Source -File -Recurse)
    if ($files.Count -lt 1 -or $files.Count -gt 200) {
        throw "Neplatný počet súborov v aktualizácii."
    }

    New-Item -ItemType Directory -Force -Path $backup | Out-Null
    # Prepare a complete backup before overwriting anything.
    foreach ($file in $files) {
        $relative = $file.FullName.Substring($Source.Length).TrimStart('\','/')
        $oldFile = Join-Path $Destination $relative
        if (Test-Path -LiteralPath $oldFile -PathType Leaf) {
            $bak = Join-Path $backup $relative
            New-Item -ItemType Directory -Force -Path (Split-Path -Parent $bak) | Out-Null
            Copy-Item -LiteralPath $oldFile -Destination $bak -Force
        } else {
            [void]$created.Add($oldFile)
        }
    }

    Log "Začínam inštaláciu overeného aktualizačného balíka."
    foreach ($file in $files) {
        $relative = $file.FullName.Substring($Source.Length).TrimStart('\','/')
        $target = Join-Path $Destination $relative
        New-Item -ItemType Directory -Force -Path (Split-Path -Parent $target) | Out-Null
        Copy-Item -LiteralPath $file.FullName -Destination $target -Force
    }

    Start-Process -FilePath $exePath -WorkingDirectory $Destination
    $completed = $true
    Log "Aktualizácia dokončená, aplikácia reštartovaná."
}
catch {
    Log ("CHYBA: " + $_.Exception.Message)
    if (Test-Path -LiteralPath $backup -PathType Container) {
        try {
            foreach ($file in (Get-ChildItem -LiteralPath $backup -File -Recurse)) {
                $relative = $file.FullName.Substring($backup.Length).TrimStart('\','/')
                $target = Join-Path $Destination $relative
                New-Item -ItemType Directory -Force -Path (Split-Path -Parent $target) | Out-Null
                Copy-Item -LiteralPath $file.FullName -Destination $target -Force
            }
            foreach ($path in $created) {
                if (Test-Path -LiteralPath $path -PathType Leaf) {
                    Remove-Item -LiteralPath $path -Force
                }
            }
            Log "Pôvodné súbory boli obnovené zo zálohy."
        }
        catch {
            Log ("CHYBA OBNOVY: " + $_.Exception.Message)
        }
    }
    try { Start-Process -FilePath $exePath -WorkingDirectory $Destination }
    catch { Log ("Nedá sa reštartovať pôvodná aplikácia: " + $_.Exception.Message) }
}
finally {
    if (-not $completed) { Log "Aktualizácia sa nedokončila." }
}
