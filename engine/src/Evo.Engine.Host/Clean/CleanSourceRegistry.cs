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
            Reversible: true)
    ];
}
