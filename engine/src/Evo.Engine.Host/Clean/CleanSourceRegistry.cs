namespace Evo.Engine.Host.Clean;

public static class CleanSourceRegistry
{
    public static IReadOnlyList<CleanSource> GetSources() =>
    [
        new CleanSource(
            "win.userTemp",
            "Archivos temporales de usuario",
            "Windows temporal",
            "Archivos en %TEMP% que no están bloqueados por procesos activos.",
            CleanClassification.Safe,
            CleanRiskLevel.Low,
            Reversible: true),
        new CleanSource(
            "win.crashDumps",
            "Volcados de fallos (Crash Dumps)",
            "Windows diagnóstico",
            "Evidencia diagnóstica en LocalAppData\\CrashDumps; informativa por defecto.",
            CleanClassification.Informative,
            CleanRiskLevel.Medium,
            Reversible: true),
        new CleanSource(
            "dev.packageCaches",
            "Cachés de gestores de paquetes",
            "Desarrollo",
            "Cachés locales de npm/pnpm/NuGet; condicionadas por coste de red al reconstruir.",
            CleanClassification.Conditional,
            CleanRiskLevel.Medium,
            Reversible: true),
        new CleanSource(
            "browser.caches",
            "Cachés de navegadores (Edge/Chrome/Firefox)",
            "Navegadores",
            "Cachés web temporales; condicionadas porque su borrado puede provocar recargas.",
            CleanClassification.Conditional,
            CleanRiskLevel.Low,
            Reversible: true),
        new CleanSource(
            "win.wer",
            "Informes de errores de Windows (WER)",
            "Windows diagnóstico",
            "Reportes de errores en ProgramData/LocalAppData\\Microsoft\\Windows\\WER.",
            CleanClassification.Safe,
            CleanRiskLevel.Low,
            Reversible: true),
        new CleanSource(
            "win.thumbnails",
            "Caché de miniaturas del Explorador",
            "Explorador",
            "Bases de datos thumbcache_*.db; condicionadas por uso del Explorador.",
            CleanClassification.Conditional,
            CleanRiskLevel.Low,
            Reversible: true)
    ];
}
