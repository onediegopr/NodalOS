using OneBrain.Core.Recording;

namespace OneBrain.Core.Confidence;

public static class RecipeConfidenceScorer
{
    public static RecipeConfidenceProfile Score(RecipeConfidenceInput input)
    {
        var notes = input.Notes.Select(SensitiveTextSanitizer.Sanitize).ToList();
        var risk = NormalizeRisk(input.RiskLevel);
        var runs = Math.Max(0, input.Runs);
        var successes = Math.Clamp(input.Successes, 0, runs);
        var failures = Math.Clamp(input.Failures, 0, runs);
        var score = CalculateScore(risk, runs, successes, failures, input.Status);
        var status = DetermineStatus(input.Status, risk, runs, successes, failures, score, input.ApprovalRequiredUntil, notes);

        if (status == RecipeConfidenceStatuses.Blocked)
            score = Math.Min(score, 20);

        return new RecipeConfidenceProfile(
            RecipeId: SensitiveTextSanitizer.Sanitize(input.RecipeId),
            CandidateFlowId: SensitiveTextSanitizer.Sanitize(input.CandidateFlowId),
            Status: status,
            ConfidenceScore: Math.Clamp(score, 0, 100),
            RiskLevel: risk,
            Runs: runs,
            Successes: successes,
            Failures: failures,
            LastError: SensitiveTextSanitizer.Sanitize(input.LastError),
            LastVerifiedAt: SensitiveTextSanitizer.Sanitize(input.LastVerifiedAt),
            ApprovalRequiredUntil: SensitiveTextSanitizer.Sanitize(input.ApprovalRequiredUntil),
            Notes: notes);
    }

    private static int CalculateScore(string risk, int runs, int successes, int failures, string status)
    {
        var score = string.Equals(status, RecipeConfidenceStatuses.Disabled, StringComparison.OrdinalIgnoreCase) ? 0 : 30;

        if (runs > 0)
        {
            var successRate = successes / (double)runs;
            score += (int)Math.Round(successRate * 45, MidpointRounding.AwayFromZero);
        }

        if (runs >= 3 && failures == 0)
            score += 10;
        if (runs >= 5 && failures <= 1)
            score += 5;

        score -= Math.Min(25, failures * 5);

        score -= risk switch
        {
            RecipeConfidenceRiskLevels.Critical => 20,
            RecipeConfidenceRiskLevels.High => 10,
            RecipeConfidenceRiskLevels.Medium => 5,
            _ => 0
        };

        return score;
    }

    private static string DetermineStatus(
        string requestedStatus,
        string risk,
        int runs,
        int successes,
        int failures,
        int score,
        string? approvalRequiredUntil,
        List<string> notes)
    {
        if (string.Equals(requestedStatus, RecipeConfidenceStatuses.Disabled, StringComparison.OrdinalIgnoreCase))
            return RecipeConfidenceStatuses.Disabled;

        if (risk == RecipeConfidenceRiskLevels.Critical && string.IsNullOrWhiteSpace(approvalRequiredUntil))
        {
            notes.Add("critical flow blocked until explicit approvalRequiredUntil is set");
            return RecipeConfidenceStatuses.Blocked;
        }

        if (risk == RecipeConfidenceRiskLevels.Critical)
            return RecipeConfidenceStatuses.Critical;

        if (runs == 0)
            return string.Equals(requestedStatus, RecipeConfidenceStatuses.New, StringComparison.OrdinalIgnoreCase)
                ? RecipeConfidenceStatuses.New
                : RecipeConfidenceStatuses.Candidate;

        if (score >= 85 && runs >= 3 && failures == 0)
            return RecipeConfidenceStatuses.Stable;

        if (score >= 55 && successes > 0)
            return RecipeConfidenceStatuses.Supervised;

        return RecipeConfidenceStatuses.Candidate;
    }

    private static string NormalizeRisk(string risk)
    {
        return risk?.ToLowerInvariant() switch
        {
            RecipeConfidenceRiskLevels.Low => RecipeConfidenceRiskLevels.Low,
            RecipeConfidenceRiskLevels.Medium => RecipeConfidenceRiskLevels.Medium,
            RecipeConfidenceRiskLevels.High => RecipeConfidenceRiskLevels.High,
            RecipeConfidenceRiskLevels.Critical => RecipeConfidenceRiskLevels.Critical,
            _ => RecipeConfidenceRiskLevels.Medium
        };
    }
}
