<#
.SYNOPSIS
    Verifica el estado de la ETAPA 01 FOUNDATION (checkpoints F01-F06).

.DESCRIPTION
    Chequea la estructura, la solucion, los proyectos, la configuracion y el logging.
    Escribe foundation-results.txt y retorna exit 0 (OK) o exit 2 (FAIL).

.EXIT CODES
    0 - todos los chequeos F01-F05 OK.
    2 - uno o mas chequeos fallaron (o no se encontro la raiz del repo).
#>

$Root = $null
$dir = (Get-Location).Path
for ($i = 0; $i -lt 10; $i++) {
    if (Test-Path (Join-Path $dir "EVO.sln")) { $Root = $dir; break }
    $parent = Split-Path -Parent $dir
    if (-not $parent) { break }
    $dir = $parent
}
if (-not $Root) {
    Write-Host "ERROR: no se encontro EVO.sln en el directorio actual ni en sus padres"
    exit 2
}

$Failures = 0
$Warnings = 0
$Results = New-Object System.Collections.Generic.List[string]

function Write-Pass($msg) {
    Write-Host "  [PASS] $msg" -ForegroundColor Green
    $script:Results.Add("[PASS] $msg")
}
function Write-Fail($msg) {
    Write-Host "  [FAIL] $msg" -ForegroundColor Red
    $script:Failures++
    $script:Results.Add("[FAIL] $msg")
}
function Write-Warn($msg) {
    Write-Host "  [WARN] $msg" -ForegroundColor Yellow
    $script:Warnings++
    $script:Results.Add("[WARN] $msg")
}
function Check-Path($relativePath, $label) {
    $full = Join-Path $Root $relativePath
    if (Test-Path $full) { Write-Pass $label } else { Write-Fail "$label - no encontrado: $relativePath" }
}
function Check-Dir($relativePath, $label) {
    $full = Join-Path $Root $relativePath
    if (Test-Path $full -PathType Container) { Write-Pass $label } else { Write-Fail "$label - directorio no encontrado: $relativePath" }
}

Write-Host ""
Write-Host "EVO BOTTLENECK DETECTOR - FOUNDATION VERIFICATION" -ForegroundColor Yellow
$script:Results.Add("== EVO BOTTLENECK DETECTOR - FOUNDATION VERIFICATION ==")
$script:Results.Add("Fecha: " + (Get-Date -Format "yyyy-MM-ddTHH:mm:ssZ"))

Write-Host ""
Write-Host "[F01] Estructura y decisiones" -ForegroundColor Cyan
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

Write-Host ""
Write-Host "[F02/F03] Solución y proyectos" -ForegroundColor Cyan
$script:Results.Add("")
$script:Results.Add("[F02/F03] Solución y proyectos")
Check-Path "EVO.sln" "EVO.sln"
Check-Path "src\EVO.Core\EVO.Core.csproj" "EVO.Core.csproj"
Check-Path "src\EVO.Desktop\EVO.Desktop.csproj" "EVO.Desktop.csproj"
Check-Path "tests\EVO.Core.Tests\EVO.Core.Tests.csproj" "EVO.Core.Tests.csproj"
Check-Path "src\EVO.Collectors\EVO.Collectors.csproj" "EVO.Collectors.csproj"
Check-Path "tests\EVO.Collectors.Tests\EVO.Collectors.Tests.csproj" "EVO.Collectors.Tests.csproj"
$sln = Get-Content (Join-Path $Root "EVO.sln") -Raw -ErrorAction SilentlyContinue
if ($sln -match "EVO.Core.csproj") { Write-Pass "EVO.sln referencia EVO.Core" } else { Write-Fail "EVO.sln no menciona EVO.Core" }
if ($sln -match "EVO.Desktop.csproj") { Write-Pass "EVO.sln referencia EVO.Desktop" } else { Write-Fail "EVO.sln no menciona EVO.Desktop" }
if ($sln -match "EVO.Core.Tests.csproj") { Write-Pass "EVO.sln referencia EVO.Core.Tests" } else { Write-Fail "EVO.sln no menciona EVO.Core.Tests" }
if ($sln -match "EVO.Collectors.csproj") { Write-Pass "EVO.sln referencia EVO.Collectors" } else { Write-Fail "EVO.sln no menciona EVO.Collectors" }
if ($sln -match "EVO.Collectors.Tests.csproj") { Write-Pass "EVO.sln referencia EVO.Collectors.Tests" } else { Write-Fail "EVO.sln no menciona EVO.Collectors.Tests" }

Write-Host ""
Write-Host "[F04] Contratos y modelos" -ForegroundColor Cyan
$script:Results.Add("")
$script:Results.Add("[F04] Contratos y modelos")
Check-Path "src\EVO.Core\Contracts\ICollector.cs" "ICollector.cs"
Check-Path "src\EVO.Core\Contracts\Metric.cs" "Metric.cs"
Check-Path "src\EVO.Core\Contracts\Anomaly.cs" "Anomaly.cs"
Check-Path "src\EVO.Core\Findings\DiagnosticFinding.cs" "DiagnosticFinding.cs"
Check-Path "src\EVO.Core\Findings\Evidence.cs" "Evidence.cs"
Check-Path "src\EVO.Core\Findings\FindingConfidence.cs" "FindingConfidence.cs"
Check-Path "src\EVO.Core\Findings\Severity.cs" "Severity.cs"
Check-Path "src\EVO.Core\Contracts\Metric.cs" "Metric.cs (string+numeric)"
Check-Path "src\EVO.Collectors\Hardware\SystemIntelligenceCollector.cs" "SystemIntelligenceCollector.cs"
Check-Path "src\EVO.Collectors\CPU\CpuAnalyzerCollector.cs" "CpuAnalyzerCollector.cs (M02)"
Check-Path "src\EVO.Collectors\Memory\RamAnalyzerCollector.cs" "RamAnalyzerCollector.cs (M03)"
Check-Path "src\EVO.Collectors\Storage\StorageAnalyzerCollector.cs" "StorageAnalyzerCollector.cs (M04)"
Check-Path "src\EVO.Collectors\Processes\ProcessAnalyzerCollector.cs" "ProcessAnalyzerCollector.cs (M06)"
Check-Path "src\EVO.Collectors\Services\ServicesAuditorCollector.cs" "ServicesAuditorCollector.cs (M07)"
Check-Path "src\EVO.Collectors\Startup\StartupAnalyzerCollector.cs" "StartupAnalyzerCollector.cs (M08)"
Check-Path "src\EVO.Collectors\Drivers\DriverAnalyzerCollector.cs" "DriverAnalyzerCollector.cs (M09)"
Check-Path "src\EVO.Collectors\Windows\WindowsHealthCollector.cs" "WindowsHealthCollector.cs (M12)"
Check-Path "src\EVO.Core\Correlation\BottleneckEngineCollector.cs" "BottleneckEngineCollector.cs (M14)"

Write-Host ""
Write-Host "[F05] Configuracion y logging" -ForegroundColor Cyan
$script:Results.Add("")
$script:Results.Add("[F05] Configuracion y logging")
Check-Path "src\EVO.Core\Configuration\AppSettings.cs" "AppSettings.cs"
Check-Path "src\EVO.Core\Logging\LogLevel.cs" "LogLevel.cs"
Check-Path "src\EVO.Core\Logging\SimpleFileLogger.cs" "SimpleFileLogger.cs"

Write-Host ""
Write-Host "[F06] Pendiente - requiere Windows + .NET 10 SDK" -ForegroundColor Cyan
$script:Results.Add("")
$script:Results.Add("[F06] Pendiente - requiere Windows + .NET 10 SDK (F06 requiere: dotnet build, dotnet test)")
$dotnet = Get-Command dotnet -ErrorAction SilentlyContinue
if ($dotnet) {
    try {
        $sdks = dotnet --list-sdks
        $script:Results.Add("  SDK: " + ($sdks -join ", "))
        if ($sdks -match "10\.") { Write-Pass ".NET 10 SDK encontrado" } else { Write-Warn ".NET 10 SDK no encontrado; F06 queda pendiente" }
    } catch {
        Write-Warn "No se pudo ejecutar dotnet: $($_.Exception.Message)"
    }
} else {
    Write-Warn "dotnet SDK no encontrado"
}

$script:Results.Add("")
$script:Results.Add("RESULTADO: $Warnings warnings, $Failures failures")

$ResultsPath = Join-Path $Root "foundation-results.txt"
try {
    $script:Results | Out-File -FilePath $ResultsPath -Encoding utf8
} catch {
    Write-Host "ERROR: no se pudo escribir $ResultsPath`n$($_.Exception.Message)" -ForegroundColor Red
    $Failures++
}

Write-Host ""
Write-Host "RESULTADO: $Warnings warnings, $Failures failures" -ForegroundColor Yellow
Write-Host "Resultados en: $ResultsPath" -ForegroundColor Gray

if ($Failures -gt 0) {
    Write-Host "FOUNDATION VERIFICATION FAILED" -ForegroundColor Red
    exit 2
}
Write-Host "FOUNDATION VERIFICATION PASSED (F06 pendiente)" -ForegroundColor Green
exit 0
