namespace Evo.Engine.Host.Clean;

public sealed record PrioritizedCandidate(
    CleanCandidate Candidate,
    double PriorityScore);

public static class CleanupIntelligence
{
    public static List<PrioritizedCandidate> Prioritize(CleanPlan plan)
    {
        return plan.Candidates
            .Where(c => !c.IsLocked && c.Classification != CleanClassification.Protected.ToString())
            .Select(c =>
            {
                double riskWeight = c.RiskLevel switch
                {
                    "Low" => 1.0,
                    "Medium" => 2.5,
                    _ => 10.0
                };
                double score = Math.Round((c.SizeBytes / 1024.0) / riskWeight, 2);
                return new PrioritizedCandidate(c, score);
            })
            .OrderByDescending(p => p.PriorityScore)
            .ToList();
    }
}
