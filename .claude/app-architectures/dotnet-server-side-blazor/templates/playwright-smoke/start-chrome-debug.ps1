<#
.SYNOPSIS
    Startet Chrome mit Remote Debugging fuer die Playwright-Smoke-Tests (Windows).
.DESCRIPTION
    Linux/macOS: start-chrome-debug.sh (gleiche Logik in Bash).
    Verwendung: .\start-chrome-debug.ps1 -> bei der Anwendung einloggen -> dotnet test

    Chrome 136+ verlangt fuer Remote Debugging ein separates User-Data-Verzeichnis;
    das Standardprofil wird aus Sicherheitsgruenden abgelehnt (App-Bound Encryption).
    Quelle: https://developer.chrome.com/blog/remote-debugging-port
#>
[CmdletBinding()]
param(
    [int]$Port = $(if ($env:PLAYWRIGHT_CDP_PORT) { [int]$env:PLAYWRIGHT_CDP_PORT } else { 9222 }),
    [string]$BaseUrl = $(if ($env:PLAYWRIGHT_BASE_URL) { $env:PLAYWRIGHT_BASE_URL } else { '{{BASE_URL}}' })
)

$ErrorActionPreference = 'Stop'
$profileDir = Join-Path $env:LOCALAPPDATA 'PlaywrightTestProfile'
$testProjekt = '{{BLAZOR_PROJEKT}}.PlaywrightTests'

function Test-DebugEndpunkt {
    try {
        Invoke-RestMethod -Uri "http://localhost:$Port/json/version" -TimeoutSec 2 | Out-Null
        return $true
    }
    catch {
        return $false
    }
}

if (Test-DebugEndpunkt) {
    Write-Host "Chrome laeuft bereits mit Remote Debugging auf Port $Port."
    Write-Host "Bei der Anwendung einloggen, dann: dotnet test $testProjekt"
    return
}

$kandidaten = @(
    "$env:ProgramFiles\Google\Chrome\Application\chrome.exe",
    "${env:ProgramFiles(x86)}\Google\Chrome\Application\chrome.exe",
    "$env:LOCALAPPDATA\Google\Chrome\Application\chrome.exe"
)
$chrome = $kandidaten | Where-Object { Test-Path $_ } | Select-Object -First 1
if (-not $chrome) {
    $chrome = (Get-Command chrome.exe -ErrorAction SilentlyContinue).Source
}
if (-not $chrome) {
    throw 'Chrome nicht gefunden. Chrome installieren oder Pfad in $kandidaten eintragen.'
}

Write-Host '============================================================'
Write-Host 'Chrome Remote Debugging fuer Playwright-Tests'
Write-Host '============================================================'
Write-Host "Chrome:        $chrome"
Write-Host "Port:          $Port"
Write-Host "Test-Profil:   $profileDir  (separates Profil, Chrome 136+)"
Write-Host "Anwendung:     $BaseUrl"
Write-Host ''
Write-Host 'Beim ERSTEN Start einmalig einloggen; die Session bleibt im Test-Profil.'
Write-Host "Chrome-Fenster offen lassen, dann: dotnet test $testProjekt"
Write-Host ''

New-Item -ItemType Directory -Path $profileDir -Force | Out-Null
Start-Process -FilePath $chrome -ArgumentList @(
    "--remote-debugging-port=$Port",
    "--user-data-dir=`"$profileDir`"",
    $BaseUrl
)

# Auf den Debug-Endpunkt warten (Bedingung mit Obergrenze statt fester Wartezeit)
foreach ($versuch in 1..20) {
    if (Test-DebugEndpunkt) {
        Write-Host "[OK] Remote Debugging aktiv auf Port $Port"
        return
    }
    Start-Sleep -Milliseconds 500
}

Write-Warning 'Remote Debugging nach 10 s nicht erreichbar - Chrome-Fenster pruefen.'
exit 1
