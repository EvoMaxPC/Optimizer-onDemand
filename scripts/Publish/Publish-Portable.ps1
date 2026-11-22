<#
.SYNOPSIS
    Publica EVO BOTTLENECK DETECTOR como ejecutable portable self-contained.

.DESCRIPTION
    Requiere: Windows x64, .NET 10 SDK.
    Genera dist/EVO_BOTTLENECK/ con runtime incluido (no requiere .NET instalado).
    Distributable: ejecutable único Windows x64; datos/config/logs/reports locales.
    NOTA: la publicación portatil NO fue ejecutada en el entorno de creación
    (Linux sandbox sin SDK .NET); debe ejecutarse en máquina Windows.

.PARAMETER Configuration
    Configuration (default: Release).
.PARAMETER OutputDir
    Directorio de publicación (default: <root>/dist/EVO_BOTTLENECK).
#>
param(
    [string]$Configuration = "Release",
    [string]$OutputDir
)

$ErrorActionPreference = "Stop"

$Root = Split-Path -Parent $PSScriptRoot
$Root = Split-Path -Parent $Root

if ([string]::IsNullOrWhiteSpace($OutputDir)) {
    $OutputDir = Join-Path $Root "dist\EVO_BOTTLENECK"
}

Write-Host "EVO BOTTLENECK DETECTOR — PUBLICATION" -ForegroundColor Yellow
Write-Host "Configuration: $Configuration" -ForegroundColor Cyan
Write-Host "Output: $OutputDir" -ForegroundColor Cyan

if (-not (Get-Command dotnet -ErrorAction SilentlyContinue)) {
    Write-Host "FAIL: dotnet SDK no encontrado en PATH." -ForegroundColor Red
    exit 2
}

Push-Location $Root
try {
    & dotnet publish (Join-Path $Root "src\EVO.Desktop\EVO.Desktop.csproj") `
        -c $Configuration `
        -r win-x64 `
        --self-contained true `
        -p:PublishSingleFile=true `
        -p:EnableCompressionInSingleFile=true `
        -p:IncludeNativeLibrariesForSelfExtract=true `
        -o $OutputDir

    if ($LASTEXITCODE -ne 0) {
        Write-Host "FAIL: publish abortado." -ForegroundColor Red
        exit $LASTEXITCODE
    }

    Write-Host "PUBLISH OK → $OutputDir" -ForegroundColor Green
    exit 0
}
finally {
    Pop-Location
}
