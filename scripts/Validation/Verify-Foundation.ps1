<#
.SYNOPSIS
    Verifica el estado de la ETAPA 01 FOUNDATION (checkpoints F01–F06).

.DESCRIPTION
    Chequea la estructura, la solución, los proyectos, la configuración y el logging.
    F06 (compilación + pruebas) se reporta como pendiente: requiere Windows + .NET 10 SDK.
    Escribe foundation-results.txt (para CI) y retorna exit 0 (OK) o exit 2 (FAIL).

.EXIT CODES
    0 — todos los chequeos F01–F05 OK.
    2 — uno o más chequeos fallaron.
#>

$ErrorActionPreference = "Stop"

$Root = Split-Path -Parent $PSScriptRoot
$Root = Split-Path -Parent $Root
$Failures = 0
$Warnings = 0
$Results = New-Object System.Collections.Generic.List[string]

function Pass($msg) { Write-Host "  [PASS] $msg" -ForegroundColor Green; $script:Results.Add("[PASS] $msg") }
function Fail($msg) { Write-Host "  [FAIL] $msg" -ForegroundColor Red;   $script:Failures++; $script:Results.Add("[FAIL] $msg") }
function Warn($msg) { Write-Host "  [WARN] $msg" -ForegroundColor Yellow; $script:Warnings++; $script:Results.Add("[WARN] $msg") }

function Check-Path($relativePath, $label) {
    $full = Join-Path $Root $relativePath
    if (Test-Path $full) { Pass "$label" } else { Fail "$label — no encontrado: $relativePath" }
}
function Check-Dir($relativePath, $label) {
    $full = Join-Path $Root $relativePath
    if (Test-Path $full -PathType Container) { Pass "$label" } else { Fail "$label — directorio no encontrado: $relativePath" }
}

Write-Host "`nEVO BOTTLENECK DETECTOR — FOUNDATION VERIFICATION" -ForegroundColor Yellow
$script:Results.Add("== EVO BOTTLENECK DETECTOR — FOUNDATION VERIFICATION ==")
$script:Results.Add("Fecha: $(Get-Date -Format "yyyy-MM-ddTHH:mm:ssZ")")

Write-Host "`n[F01] Estructura y decisiones" -ForegroundColor Cyan
$script:Results.Add("")
$script:Results.Add("[F01] Estructura y decisiones")
Check-Dir "src" "src/"
Check-Dir "tests" "tests/"
Check-Dir "scripts" "scripts/"
Check-Dir "config" "config/"
Check-Dir "docs\Architecture" "docs/Architecture/"
Check-Dir "docs\Technical" "docs/Technical/"
Check-Path "docs\Architecture\DECISIONS.md" "DECISIONS.md"
Check-Path "docs\Technical\TECHNICAL_SPECIFICATION.md" "TECHNICAL_SPECIFICATION.md"
Check-Path "config\appsettings.json" "appsettings.json"

Write-Host "`n[F02/F03] Solución y proyectos" -ForegroundColor Cyan
$script:Results.Add("")
$script:Results.Add("[F02/F03] Solución y proyectos")
Check-Path "EVO.sln" "EVO.sln"
Check-Path "src\EVO.Core\EVO.Core.csproj" "EVO.Core.csproj"
Check-Path "src\EVO.Desktop\EVO.Desktop.csproj" "EVO.Desktop.csproj"
Check-Path "tests\EVO.Core.Tests\EVO.Core.Tests.csproj" "EVO.Core.Tests.csproj"
$sln = Get-Content (Join-Path $Root "EVO.sln") -Raw -ErrorAction SilentlyContinue
if ($sln -match "EVO.Core.csproj") { Pass "EVO.sln referencia EVO.Core" } else { Fail "EVO.sln no menciona EVO.Core" }
if ($sln -match "EVO.Desktop.csproj") { Pass "EVO.sln referencia EVO.Desktop" } else { Fail "EVO.sln no menciona EVO.Desktop" }
if ($sln -match "EVO.Core.Tests.csproj") { Pass "EVO.sln referencia EVO.Core.Tests" } else { Fail "EVO.sln no menciona EVO.Core.Tests" }

Write-Host "`n[F04] Contratos y modelos" -ForegroundColor Cyan
$script:Results.Add("")
$script:Results.Add("[F04] Contratos y modelos")
Check-Path "src\EVO.Core\Contracts\ICollector.cs" "ICollector.cs"
Check-Path "src\EVO.Core\Contracts\Metric.cs" "Metric.cs"
Check-Path "src\EVO.Core\Contracts\Anomaly.cs" "Anomaly.cs"
Check-Path "src\EVO.Core\Findings\DiagnosticFinding.cs" "DiagnosticFinding.cs"
Check-Path "src\EVO.Core\Findings\Evidence.cs" "Evidence.cs"
Check-Path "src\EVO.Core\Findings\FindingConfidence.cs" "FindingConfidence.cs"
Check-Path "src\EVO.Core\Findings\Severity.cs" "Severity.cs"

Write-Host "`n[F05] Configuración y logging" -ForegroundColor Cyan
$script:Results.Add("")
$script:Results.Add("[F05] Configuración y logging")
Check-Path "src\EVO.Core\Configuration\AppSettings.cs" "AppSettings.cs"
Check-Path "src\EVO.Core\Logging\LogLevel.cs" "LogLevel.cs"
Check-Path "src\EVO.Core\Logging\SimpleFileLogger.cs" "SimpleFileLogger.cs"

Write-Host "`n[F06] Pendiente — requiere Windows + .NET 10 SDK" -ForegroundColor Cyan
$script:Results.Add("")
$script:Results.Add("[F06] Pendiente — requiere Windows + .NET 10 SDK (F06 requiere: dotnet build, dotnet test)")
if (Get-Command dotnet -ErrorAction SilentlyContinue) {
    $sdks = dotnet --list-sdks
    $script:Results.Add("  SDK: $($sdks -join ", ")")
    if ($sdks -match "10\.") {
        Pass ".NET 10 SDK encontrado"
    } else {
        Warn ".NET 10 SDK no encontrado en $env:USERPROFILE (o no en PATH); F06 queda pendiente"
    }
} else {
    Warn "dotnet SDK no encontrado"
}

$script:Results.Add("")
$script:Results.Add("RESULTADO: $Warnings warnings, $Failures failures")

# Escribir resultados (CI los sube como artefacto)
$ResultsPath = Join-Path $Root "foundation-results.txt"
$script:Results | Out-File -FilePath $ResultsPath -Encoding utf8

Write-Host "`nRESULTADO: $Warnings warnings, $Failures failures" -ForegroundColor Yellow
Write-Host "Resultados en: $ResultsPath" -ForegroundColor Gray

if ($Failures -gt 0) {
    Write-Host "FOUNDATION VERIFICATION FAILED" -ForegroundColor Red
    exit 2
}
Write-Host "FOUNDATION VERIFICATION PASSED (F06 pendiente)" -ForegroundColor Green
exit 0
