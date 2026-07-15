using OneBrain.Core.History;
using OneBrain.Core.Memory;

namespace OneBrain.Core.Skills;

public static class ExecutableSkillProcessMemoryProjection
{
    public static ProcessMemoryEntry ToProcessMemoryEntry(this ExecutableSkill skill)
    {
        ArgumentNullException.ThrowIfNull(skill);

        var status = skill.State switch
        {
            ExecutableSkillState.Observed => ProcessMemoryStatuses.Observed,
            ExecutableSkillState.Candidate => ProcessMemoryStatuses.Candidate,
            ExecutableSkillState.Supervised => ProcessMemoryStatuses.Supervised,
            ExecutableSkillState.Verified => ProcessMemoryStatuses.Stable,
            ExecutableSkillState.Degraded => ProcessMemoryStatuses.Candidate,
            ExecutableSkillState.Invalidated => ProcessMemoryStatuses.Rejected,
            ExecutableSkillState.Archived => ProcessMemoryStatuses.Archived,
            _ => ProcessMemoryStatuses.Candidate
        };
        var active = skill.Transitions
            .Where(transition => string.IsNullOrWhiteSpace(transition.SupersededByTransitionId))
            .OrderBy(transition => transition.FromStateFingerprint, StringComparer.Ordinal)
            .ThenBy(transition => transition.TransitionId, StringComparer.Ordinal)
            .ToArray();
        var successfulRuns = active.Sum(transition => transition.SuccessfulRuns);
        var failedRuns = active.Sum(transition => transition.FailedRuns);
        var confidence = skill.State switch
        {
            ExecutableSkillState.Verified => Math.Clamp(75 + successfulRuns * 3 - failedRuns * 8, 0, 100),
            ExecutableSkillState.Supervised => 60,
            ExecutableSkillState.Degraded => Math.Clamp(45 + successfulRuns - failedRuns * 10, 0, 69),
            ExecutableSkillState.Invalidated => 0,
            ExecutableSkillState.Archived => 0,
            ExecutableSkillState.Candidate => 30,
            _ => 15
        };
        var processMemoryId = string.IsNullOrWhiteSpace(skill.ProcessMemoryId)
            ? $"process-memory-skill-{skill.SkillId}"
            : skill.ProcessMemoryId;
        var source = string.IsNullOrWhiteSpace(skill.RecipeId)
            ? ProcessMemorySources.RunHistory
            : ProcessMemorySources.Recipe;
        var lastVerified = skill.Transitions
            .Where(transition => transition.SuccessfulRuns > 0)
            .Select(transition => (DateTimeOffset?)transition.LastVerifiedAtUtc)
            .OrderByDescending(value => value)
            .FirstOrDefault();
        var tags = new[]
            {
                "living-skill",
                $"state-{skill.State.ToString().ToLowerInvariant()}",
                $"profile-{skill.AppProfileId}"
            }
            .Concat(skill.RequiredCapabilities.Select(capability => $"capability-{capability}"))
            .Select(HistorySanitizer.SanitizeText)
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Take(32)
            .ToArray();
        var stepSummaries = active.Select(transition =>
                $"{transition.Action.Operation} via {transition.Action.CapabilityId}: {transition.State}; verified={transition.SuccessfulRuns}; failures={transition.FailedRuns}.")
            .Select(HistorySanitizer.SanitizeText)
            .Take(50)
            .ToArray();
        var keyRisks = active
            .Where(transition => transition.State != VerifiedSkillTransitionState.Verified)
            .Select(transition =>
                $"Transition {transition.TransitionId} is {transition.State}; replay remains blocked until localized repair and re-verification.")
            .Append("Skill memory never grants execution authority and cannot bypass mission policy.")
            .Select(HistorySanitizer.SanitizeText)
            .Distinct(StringComparer.Ordinal)
            .Take(20)
            .ToArray();
        var nextActions = skill.State switch
        {
            ExecutableSkillState.Verified => new[]
            {
                "Select a transition only from an exact semantic state match inside an already authorized mission.",
                "Run SemanticVerifierV2 again after execution before recording another successful use."
            },
            ExecutableSkillState.Degraded => new[]
            {
                "Reobserve only the affected application state and repair the failed transition locally.",
                "Promote the repair only after semantic outcome and side-effect verification pass."
            },
            ExecutableSkillState.Invalidated => new[]
            {
                "Keep replay disabled and collect a new supervised, semantically verified transition."
            },
            _ => new[]
            {
                "Complete supervision and semantic verification before enabling replay selection."
            }
        };
        var decisions = active.Select(transition =>
                new ProcessMemoryDecision(
                    DecisionId: $"decision-{transition.TransitionId}",
                    CreatedAtUtc: transition.LastVerifiedAtUtc.UtcDateTime.ToString("O"),
                    Summary: $"Transition state: {transition.State}",
                    Reason: transition.State == VerifiedSkillTransitionState.Verified
                        ? "Semantic transition, outcome, side effects and evidence were verified."
                        : "A controlled failure observation changed replay eligibility.",
                    Outcome: transition.ReplayEligible ? "eligible-by-reference" : "replay-blocked"))
            .Take(50)
            .ToArray();
        var errors = active
            .Where(transition => transition.LastFailureClass is not null)
            .Select(transition => new ProcessMemoryError(
                Code: transition.LastFailureClass!.Value.ToString(),
                Message: $"Transition {transition.TransitionId} last failed as {transition.LastFailureKind?.ToString() ?? "Unverified"}.",
                LastSeenAtUtc: transition.LastFailureAtUtc?.UtcDateTime.ToString("O") ?? skill.UpdatedAtUtc.UtcDateTime.ToString("O")))
            .Take(50)
            .ToArray();
        var evidenceLinks = skill.EvidenceRefs.Count == 0
            ? Array.Empty<ProcessMemoryEvidenceLink>()
            : new[]
            {
                new ProcessMemoryEvidenceLink(
                    Kind: "reference-only-skill-evidence",
                    RelativePath: string.Empty,
                    Label: $"{skill.EvidenceRefs.Count} sanitized evidence references remain in runtime skill memory.")
            };
        var notes = new[]
        {
            $"Executable skill fingerprint: {skill.SkillFingerprint}",
            $"Skill version: {skill.Version}",
            $"App profile version: {skill.AppProfileVersion}",
            "This is a projection into the existing Process Memory model, not a second memory store.",
            "No action authority, raw screenshot, raw DOM, raw parameter value or secret is stored here."
        }.Select(HistorySanitizer.SanitizeText).ToArray();

        return new ProcessMemoryEntry(
            Id: HistorySanitizer.SanitizeText(processMemoryId),
            Title: HistorySanitizer.SanitizeText(skill.TitleRedacted),
            Description: HistorySanitizer.SanitizeText(
                $"Verified semantic skill for app profile {skill.AppProfileId}; {active.Length} active transitions."),
            Source: source,
            Status: status,
            AppOrSite: HistorySanitizer.SanitizeText(skill.AppProfileId),
            Domain: string.Empty,
            Tags: tags,
            RiskLevel: HistorySanitizer.SanitizeText(skill.RiskLevel),
            ConfidenceScore: confidence,
            CreatedAtUtc: skill.CreatedAtUtc.UtcDateTime.ToString("O"),
            UpdatedAtUtc: skill.UpdatedAtUtc.UtcDateTime.ToString("O"),
            LastUsedAtUtc: lastVerified?.UtcDateTime.ToString("O"),
            Summary: new ProcessMemorySummary(
                Summary: HistorySanitizer.SanitizeText(
                    $"State {skill.State}; {active.Count(transition => transition.ReplayEligible)} replay-eligible verified transitions; {successfulRuns} successful verifications."),
                StepSummaries: stepSummaries,
                KeyRisks: keyRisks,
                NextActions: nextActions.Select(HistorySanitizer.SanitizeText).ToArray()),
            Links: new ProcessMemoryLink(
                RecordingSessionId: null,
                TimelineId: null,
                CandidateFlowId: skill.SkillId,
                RecipeDraftId: null,
                RecipeId: skill.RecipeId,
                ApprovalRequestId: null,
                ApprovalDecisionId: null,
                RunId: skill.LastRunId,
                AiAuditId: null,
                ConfidenceId: skill.SkillFingerprint),
            Decisions: decisions,
            Errors: errors,
            EvidenceLinks: evidenceLinks,
            ArtifactPaths: [],
            Notes: notes);
    }
}
