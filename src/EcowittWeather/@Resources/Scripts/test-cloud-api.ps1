# Safe Ecowitt Cloud API connectivity test for Rainmeter.
# Reads credentials from CloudSecrets.inc and never prints them.

param(
    [Parameter(Mandatory=$true)]
    [string]$SkinPath,
    [string]$ApiBase = "https://api.ecowitt.net/api/v3/device/real_time"
)

$ErrorActionPreference = "Stop"
$ProgressPreference = "SilentlyContinue"
$SkinPath = [System.IO.Path]::GetFullPath($SkinPath)
$OutputEncoding = [System.Text.Encoding]::UTF8
[Console]::OutputEncoding = [System.Text.Encoding]::UTF8

function Read-IniVariables([string]$Path) {
    $result = @{}
    if (-not (Test-Path -LiteralPath $Path -PathType Leaf)) { return $result }

    foreach ($line in Get-Content -LiteralPath $Path -ErrorAction Stop) {
        if ($line -match "^\s*([^;#][^=]*)=(.*)$") {
            $key = $Matches[1].Trim()
            $value = $Matches[2].Trim()
            $result[$key] = $value
        }
    }
    return $result
}

try {
    $secretPath = Join-Path $SkinPath "@Resources\Includes\CloudSecrets.inc"
    $secrets = Read-IniVariables $secretPath

    $app = [string]$secrets["EcowittApplicationKey"]
    $api = [string]$secrets["EcowittApiKey"]
    $mac = [string]$secrets["EcowittMac"]

    $missing = @()
    if ([string]::IsNullOrWhiteSpace($app)) { $missing += "Application Key" }
    if ([string]::IsNullOrWhiteSpace($api)) { $missing += "API Key" }
    if ([string]::IsNullOrWhiteSpace($mac)) { $missing += "MAC" }

    if ($missing.Count -gt 0) {
        Write-Output ("CHYBA | Chýba: " + ($missing -join ", "))
        exit 0
    }

    $query = @{
        application_key = $app
        api_key = $api
        mac = $mac
        call_back = "all"
        temp_unitid = "1"
        pressure_unitid = "3"
        wind_speed_unitid = "7"
        rainfall_unitid = "12"
        solar_irradiance_unitid = "16"
    }

    $pairs = foreach ($key in $query.Keys) {
        [System.Uri]::EscapeDataString([string]$key) + "=" +
        [System.Uri]::EscapeDataString([string]$query[$key])
    }
    $uri = $ApiBase + "?" + ($pairs -join "&")

    $headers = @{
        "User-Agent" = "Ecowitt-Rainmeter-Cloud-Test"
        "Accept" = "application/json"
    }

    $response = Invoke-RestMethod -Uri $uri -Headers $headers -Method Get -TimeoutSec 20

    $code = [string]$response.code
    $msg = [string]$response.msg
    if ([string]::IsNullOrWhiteSpace($msg)) {
        $msg = [string]$response.message
    }

    if ([string]::IsNullOrWhiteSpace($code)) { $code = "?" }
    if ([string]::IsNullOrWhiteSpace($msg)) { $msg = "bez správy" }

    if ($code -eq "0") {
        $sections = 0
        if ($response.data) {
            $sections = @($response.data.PSObject.Properties).Count
        }
        Write-Output "OK | code=0 | $msg | sekcie=$sections"
    }
    else {
        Write-Output "CHYBA API | code=$code | $msg"
    }
}
catch {
    $message = $_.Exception.Message -replace "[\r\n]+"," "
    Write-Output "CHYBA HTTP | $message"
}
