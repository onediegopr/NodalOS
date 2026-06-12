namespace OneBrain.Core.Memory;

public static class WorkflowRetrievalService
{
    public static WorkflowRetrievalResult Search(
        IEnumerable<ProcessMemoryEntry> entries,
        WorkflowRetrievalQuery query,
        int maxMatches = 10)
    {
        var matches = entries
            .Select(entry => Score(entry, query))
            .Where(match => match.Score > 0)
            .OrderByDescending(match => match.Score)
            .ThenBy(match => match.Title, StringComparer.OrdinalIgnoreCase)
            .Take(Math.Max(1, maxMatches))
            .ToList();

        return new WorkflowRetrievalResult(query, matches);
    }

    private static WorkflowRetrievalMatch Score(ProcessMemoryEntry entry, WorkflowRetrievalQuery query)
    {
        var score = 0.0;
        var reasons = new List<string>();
        var text = query.Text?.Trim();
        var hasExplicitFilter = !string.IsNullOrWhiteSpace(query.Text) ||
                                (query.Tags?.Count > 0) ||
                                !string.IsNullOrWhiteSpace(query.AppOrSite) ||
                                !string.IsNullOrWhiteSpace(query.Domain) ||
                                !string.IsNullOrWhiteSpace(query.RiskLevel) ||
                                !string.IsNullOrWhiteSpace(query.Status) ||
                                query.MinConfidenceScore.HasValue;
        var hasDirectMatch = false;

        if (!string.IsNullOrWhiteSpace(text))
        {
            if (Contains(entry.Title, text))
            {
                score += 35;
                hasDirectMatch = true;
                reasons.Add("title match");
            }
            if (Contains(entry.Description, text) || Contains(entry.Summary.Summary, text))
            {
                score += 20;
                hasDirectMatch = true;
                reasons.Add("description/summary match");
            }
            if (entry.Summary.StepSummaries.Any(step => Contains(step, text)))
            {
                score += 10;
                hasDirectMatch = true;
                reasons.Add("step summary match");
            }
        }

        foreach (var tag in query.Tags ?? [])
        {
            if (entry.Tags.Any(candidate => string.Equals(candidate, tag, StringComparison.OrdinalIgnoreCase)))
            {
                score += 18;
                hasDirectMatch = true;
                reasons.Add($"tag match: {tag}");
            }
        }

        if (!string.IsNullOrWhiteSpace(query.AppOrSite) && Contains(entry.AppOrSite, query.AppOrSite))
        {
            score += 18;
            hasDirectMatch = true;
            reasons.Add("app/site match");
        }

        if (!string.IsNullOrWhiteSpace(query.Domain) && Contains(entry.Domain, query.Domain))
        {
            score += 18;
            hasDirectMatch = true;
            reasons.Add("domain match");
        }

        if (!string.IsNullOrWhiteSpace(query.RiskLevel) &&
            string.Equals(entry.RiskLevel, query.RiskLevel, StringComparison.OrdinalIgnoreCase))
        {
            score += 8;
            hasDirectMatch = true;
            reasons.Add("risk match");
        }

        if (!string.IsNullOrWhiteSpace(query.Status) &&
            string.Equals(entry.Status, query.Status, StringComparison.OrdinalIgnoreCase))
        {
            score += 12;
            hasDirectMatch = true;
            reasons.Add("status match");
        }

        if (query.MinConfidenceScore is int minConfidence)
        {
            if (entry.ConfidenceScore >= minConfidence)
            {
                score += 8;
                hasDirectMatch = true;
                reasons.Add("confidence threshold met");
            }
            else
            {
                score -= 10;
                reasons.Add("confidence below threshold");
            }
        }

        if (!hasExplicitFilter || hasDirectMatch)
        {
            score += entry.Status switch
            {
                ProcessMemoryStatuses.Stable => 16,
                ProcessMemoryStatuses.Supervised => 10,
                ProcessMemoryStatuses.Candidate => 3,
                ProcessMemoryStatuses.Rejected => -20,
                ProcessMemoryStatuses.Archived => -15,
                _ => 0
            };

            if (entry.Status is ProcessMemoryStatuses.Stable or ProcessMemoryStatuses.Supervised)
                reasons.Add($"status boost: {entry.Status}");
            if (entry.Status is ProcessMemoryStatuses.Rejected or ProcessMemoryStatuses.Archived)
                reasons.Add($"status penalty: {entry.Status}");

            var confidenceBoost = Math.Clamp(entry.ConfidenceScore, 0, 100) / 10.0;
            score += confidenceBoost;
            reasons.Add($"confidence boost: {entry.ConfidenceScore}");
        }

        var safeToSuggest = entry.Status is not (ProcessMemoryStatuses.Rejected or ProcessMemoryStatuses.Archived) &&
                            entry.RiskLevel is not ("high" or "critical");
        var requiresHumanReview = !safeToSuggest ||
                                  entry.Status is ProcessMemoryStatuses.Observed or ProcessMemoryStatuses.Annotated or ProcessMemoryStatuses.Candidate ||
                                  entry.RiskLevel is "medium" or "high" or "critical";

        return new WorkflowRetrievalMatch(
            ProcessMemoryId: entry.Id,
            Title: entry.Title,
            Score: Math.Round(Math.Max(0, score), 2),
            Reasons: reasons,
            RecipeId: entry.Links.RecipeId,
            CandidateFlowId: entry.Links.CandidateFlowId,
            TimelineId: entry.Links.TimelineId,
            SafeToSuggest: safeToSuggest,
            RequiresHumanReview: requiresHumanReview);
    }

    private static bool Contains(string? value, string? term)
    {
        return !string.IsNullOrWhiteSpace(value) &&
               !string.IsNullOrWhiteSpace(term) &&
               value.Contains(term, StringComparison.OrdinalIgnoreCase);
    }
}
