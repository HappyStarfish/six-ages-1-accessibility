# Deploy-Mod.ps1 — Copies built mod to BepInEx plugins folder
# Pass -GameDir <path> or set $env:SIXAGES1_GAME_DIR to point at your installed
# Six Ages 1 (Ride Like the Wind) folder.
param(
    [string]$GameDir = $env:SIXAGES1_GAME_DIR
)

$ErrorActionPreference = "Stop"

if (-not $GameDir) {
    Write-Host "GameDir not set. Pass -GameDir <path> or set `$env:SIXAGES1_GAME_DIR." -ForegroundColor Red
    exit 1
}

$ProjectRoot = Split-Path $PSScriptRoot -Parent
$BuildDir = Join-Path $ProjectRoot "build"
$PluginsDir = Join-Path $GameDir "BepInEx\plugins\SixAgesAccessibility"

$modDll = Join-Path $BuildDir "SixAgesAccessibility.dll"

if (-not (Test-Path $modDll)) {
    Write-Host "Mod DLL not found. Run Build-Mod.ps1 first." -ForegroundColor Red
    exit 1
}

if (-not (Test-Path $PluginsDir)) {
    New-Item -ItemType Directory -Path $PluginsDir -Force | Out-Null
}

Copy-Item $modDll $PluginsDir -Force
Write-Host "Deployed: $modDll -> $PluginsDir" -ForegroundColor Green

$tolkDll = Join-Path $GameDir "Tolk.dll"
$nvdaDll = Join-Path $GameDir "nvdaControllerClient32.dll"

if (-not (Test-Path $tolkDll)) {
    Write-Host "WARNING: Tolk.dll not found in game directory. Screen reader output will be disabled." -ForegroundColor Yellow
    Write-Host "  Copy Tolk.dll to: $GameDir" -ForegroundColor Yellow
}

if (-not (Test-Path $nvdaDll)) {
    Write-Host "WARNING: nvdaControllerClient32.dll not found. NVDA support requires this file." -ForegroundColor Yellow
    Write-Host "  Copy nvdaControllerClient32.dll to: $GameDir" -ForegroundColor Yellow
}

Write-Host "Deploy complete. Start the game to test." -ForegroundColor Cyan
