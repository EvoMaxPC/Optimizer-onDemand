namespace Evo.Engine.Host;

public sealed record PlannedOperation(
    string ActionId,
    string ModuleId,
    string Title,
    string RiskLevel,
    string Reason,
    bool AlreadyApplied);

public sealed record OperationPlan(
    string Profile,
    int TotalRecommended,
    List<PlannedOperation> Operations,
    string GeneratedAtUtc);

public static class AdaptiveCore
{
    public static OperationPlan GeneratePlan(string profile)
    {
        string normalized = profile.ToLowerInvariant() switch
        {
            "gaming" => "gaming",
            "minimal" => "minimal",
            _ => "balanced"
        };

        var diag = DiagnosticsCollector.Collect();
        var appliedSet = diag.AppliedActionIds.ToHashSet(StringComparer.Ordinal);
        var catalog = ActionExecutor.LoadCatalog();

        var ops = new List<PlannedOperation>();
        foreach (var action in catalog)
        {
            bool include = normalized switch
            {
                "gaming" => true,
                "minimal" => action.RiskLevel == "low",
                _ => action.RiskLevel == "low" || action.ModuleId is "memory" or "storage"
            };

            if (!include) continue;

            string reason = normalized == "gaming"
                ? $"Recomendado para perfil Gaming ({action.ModuleId})."
                : $"Compatible con perfil {normalized} (riesgo {action.RiskLevel}).";

            ops.Add(new PlannedOperation(
                action.Id,
                action.ModuleId,
                action.Title,
                action.RiskLevel,
                reason,
                appliedSet.Contains(action.Id)));
        }

        return new OperationPlan(
            normalized,
            ops.Count(o => !o.AlreadyApplied),
            ops,
            DateTime.UtcNow.ToString("O"));
    }
}
