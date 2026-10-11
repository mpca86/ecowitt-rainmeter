# Build a public EcowittWeather ZIP release and SHA256 checksum.
param(
    [Parameter(Mandatory=$true)]
    [string]$Version,
    [string]$Source = (Join-Path $PSScriptRoot "..\src\EcowittWeather"),
    [string]$OutDir = (Join-Path $PSScriptRoot "..\dist")
)

$ErrorActionPreference = "Stop"
$Version = $Version.TrimStart("v")

if (-not (Test-Path -LiteralPath $Source -PathType Container)) {
    throw "Source not found: $Source"
}

New-Item -ItemType Directory -Path $OutDir -Force | Out-Null
$stage = Join-Path ([System.IO.Path]::GetTempPath()) ("ecowitt-release-" + [guid]::NewGuid().ToString("N"))
$package = Join-Path $stage "EcowittWeather"
New-Item -ItemType Directory -Path $package -Force | Out-Null

$excludedNames = @(
    "CloudSecrets.inc",
    "UserVariables.inc",
    "meteo_history.csv",
    "ecowitt_cloud_debug.txt",
    "UpdateState.inc",
    "Changelog.txt"
)

Get-ChildItem -LiteralPath $Source -File -Recurse | ForEach-Object {
    if ($excludedNames -contains $_.Name) { return }

    $relative = $_.FullName.Substring($Source.Length).TrimStart("\")
    $dest = Join-Path $package $relative
    New-Item -ItemType Directory -Path (Split-Path -Parent $dest) -Force | Out-Null
    Copy-Item -LiteralPath $_.FullName -Destination $dest -Force
}

# Rainmeter must read a Unicode-capable Lua ScriptFile as UTF-16 LE.
# Source control keeps Lua in UTF-8; change only the staged copy.
$luaPath = Join-Path $package "@Resources\Scripts\main_cloud.lua"
if (-not (Test-Path -LiteralPath $luaPath -PathType Leaf)) {
    throw "Cloud Lua script not found in staged package: $luaPath"
}
$luaText = [System.IO.File]::ReadAllText($luaPath, [System.Text.Encoding]::UTF8)
[System.IO.File]::WriteAllText($luaPath, $luaText, [System.Text.Encoding]::Unicode)
$luaBytes = [System.IO.File]::ReadAllBytes($luaPath)
if ($luaBytes.Length -lt 2 -or $luaBytes[0] -ne 255 -or $luaBytes[1] -ne 254) {
    throw "Staged main_cloud.lua must have UTF-16 LE BOM."
}

$zipName = "EcowittWeather-v$Version.zip"
$zipPath = Join-Path $OutDir $zipName
if (Test-Path -LiteralPath $zipPath) { Remove-Item -LiteralPath $zipPath -Force }

Compress-Archive -Path $package -DestinationPath $zipPath -CompressionLevel Optimal
$hash = (Get-FileHash -LiteralPath $zipPath -Algorithm SHA256).Hash.ToLowerInvariant()
$checksumPath = "$zipPath.sha256"
"$hash  $zipName" | Set-Content -LiteralPath $checksumPath -Encoding ASCII

Remove-Item -LiteralPath $stage -Recurse -Force

Write-Host "Created:"
Write-Host "  $zipPath"
Write-Host "  $checksumPath"
