namespace Evo.Engine.Host.Clean;

public enum CleanClassification
{
    Safe,
    Conditional,
    Informative,
    Protected
}

public enum CleanRiskLevel
{
    Low,
    Medium,
    High
}

public sealed record CleanSource(
    string Id,
    string Name,
    string Family,
    string Description,
    CleanClassification DefaultClassification,
    CleanRiskLevel RiskLevel,
    bool Reversible);

public sealed record CleanCandidate(
    string SourceId,
    string Path,
    long SizeBytes,
    string CreatedAtUtc,
    string LastWriteUtc,
    string Classification,
    string RiskLevel,
    bool IsLocked,
    string Reason);

public sealed record CleanPlan(
    string PlanId,
    string CreatedAtUtc,
    string ExpiresAtUtc,
    string PlanHashSha256,
    long TotalRecoverableBytes,
    int TotalCandidates,
    List<CleanCandidate> Candidates);
