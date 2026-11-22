<#
.SYNOPSIS
    Compila EVO BOTTLENECK DETECTOR (EVO.sln).

.DESCRIPTION
    Requiere: Windows x64, .NET 10 SDK.
    Ejecuta 'dotnet build' con Configuration=Debug por defecto (param -Configuration Release disponible).
    Se verá sintaxis compatible con PowerShell 5.1.
#>
param(
    [string]$Configuration = "Debug"
)

$ErrorActionPreference = "Stop"

$Root = Split-Path -Parent $PSScriptRoot
$Root = Split-Path -Parent $Root

Write-Host "EVO BOTTLENECK DETECTOR — FOUNDATION" -ForegroundColor Yellow
Write-Host "Configuration: $Configuration" -ForegroundColor Cyan

if (-not (Get-Command dotnet -ErrorAction SilentlyContinue)) {
    Write-Host "FAIL: dotnet SDK no encontrado en PATH." -ForegroundColor Red
    Write-Host "Instala el SDK .NET 10 (ver README.md)." -ForegroundColor Red
    exit 2
}

$version = dotnet --version
Write-Host ".NET SDK: $version" -ForegroundColor Green

Push-Location $Root
try {
    Write-Host "`nCompilando EVO.sln..." -ForegroundColor Cyan
    & dotnet build EVO.sln -c $Configuration
    if ($LASTEXITCODE -ne 0) {
        Write-Host "FAIL: build con errores." -ForegroundColor Red
        exit $LASTEXITCODE
    }
    Write-Host "BUILD OK" -ForegroundColor Green
    exit 0
}
finally {
    Pop-Location
}
