namespace OneBrain.Core.Recipes;

public enum RecipeLabSectionStatus
{
    NotStarted,
    Ready,
    Warning,
    Blocked,
    NeedsHuman,
    MissingEvidence,
    Redacted,
    FutureGated,
    LiveBlocked,
    FixtureOnly,
    ReferenceOnly
}

public enum RecipeLabCellKind
{
    Overview,
    Preflight,
    Workitem,
    TriggerObservation,
    ToolTrust,
    SecretReference,
    ActionResolution,
    PlannedAction,
    Validation,
    Evidence,
    Timeline,
    HumanIntervention,
    ApprovalNarrative,
    Failure,
    SafeNextAction,
    LocatorCandidate,
    LocatorRepair,
    Handoff,
    Cleanup
}

public enum RecipeLabCellStatus
{
    NotStarted,
    Ready,
    Warning,
    Blocked,
    NeedsHuman,
    MissingEvidence,
    Redacted,
    FutureGated,
    LiveBlocked,
    FixtureOnly,
    ReferenceOnly
}

public sealed record RecipeLabSnapshotRef(string SnapshotId);

public sealed record RecipeLabBlockingIssue(
    string IssueId,
    RecipeReadinessStatus Status,
    RecipeReadinessIssueSeverity Severity,
    string RedactedSummary,
    string? SourceRef = null);

public sealed record RecipeLabSafeNextAction(
    RecipeSafeNextActionKind Kind,
    string Summary,
    bool AllowsLiveRuntime = false,
    bool AllowsExternalMutation = false)
{
    public bool ActionAuthorityGranted => false;
}

public sealed record RecipeLabCapabilitySummary(
    IReadOnlyList<string> RequiredCapabilities,
    IReadOnlyList<string> RequiredToolTrustRefs,
    IReadOnlyList<string> RequiredSecretAliasesOrRefs,
    IReadOnlyList<string> TriggerRefs,
    IReadOnlyList<string> DetectorRefs,
    bool LiveRuntimeEnabled = false,
    bool RawSecretValuesShown = false);

public sealed record RecipeLabOperatorSummary(
    string ShortExplanation,
    string FixtureSafeStatus,
    IReadOnlyList<string> RawDataOmitted,
    bool SafeForProductPreview = true);

public sealed record RecipeLabReadinessSummary(
    RecipeReadinessStatus CanonicalStatus,
    bool IsReady,
    IReadOnlyList<RecipeLabBlockingIssue> BlockingIssues,
    IReadOnlyList<RecipeLabBlockingIssue> Warnings,
    string CanonicalEvaluatorName = "RecipePolicyPreflightEvaluator",
    bool FoundationOnlyReadinessUsedAsCanonical = false,
    bool LiveRuntimeEnabled = false,
    bool ActionAuthorityGranted = false);

public sealed record RecipeLabSection(
    string SectionId,
    string Label,
    RecipeLabSectionStatus Status,
    string RedactedSummary,
    IReadOnlyList<string> SourceRefs);

public sealed record RecipeLabViewModel(
    string ViewModelId,
    IReadOnlyList<RecipeLabSection> Sections,
    RecipeLabOperatorSummary OperatorSummary,
    bool ReadOnly = true,
    bool PreviewSafe = true)
{
    public bool CanStartRecipeRun => false;
    public bool CanProcessWorkitems => false;
    public bool CanUnlockLiveRuntime => false;
}

public sealed record RecipeLabSnapshot(
    string SnapshotId,
    string RecipeId,
    string RecipeVersion,
    string DisplayName,
    string? Category,
    string? SystemTarget,
    string? RegionCountry,
    RecipeRunMode RunMode,
    RecipeLabReadinessSummary Readiness,
    string LimitsSummary,
    string CompleteCriteriaSummary,
    string TerminateCriteriaSummary,
    string ValidationSummary,
    string RiskSummary,
    string DeterministicActionResolutionSummary,
    string EvidenceCompletenessSummary,
    string TimelineProjectionSummary,
    string ApprovalHumanInterventionSummary,
    RecipeLabCapabilitySummary CapabilitySummary,
    string TriggerObserveOnlySummary,
    string WorkitemQueueSummary,
    string LocatorRepairSummary,
    RecipeLabSafeNextAction SafeNextAction,
    IReadOnlyList<string> RemainsBlocked,
    string RedactionSafetySummary,
    RecipeLabOperatorSummary OperatorSummary,
    RecipeLabViewModel ViewModel)
{
    public bool ReadOnly => true;
    public bool PreviewSafe => true;
    public bool CanStartRecipeRun => false;
    public bool CanProcessWorkitems => false;
    public bool CanUnlockLiveRuntime => false;
    public bool LiveRuntimeEnabled => false;
    public bool RawSecretValuesExposed => false;
}

public sealed record RecipeLabCellRef(string CellId);

public sealed record RecipeLabCellEvidenceSummary(
    IReadOnlyList<string> EvidenceRefs,
    string RedactedSummary,
    bool ReferenceOnly = true,
    bool RawPayloadExposed = false);

public sealed record RecipeLabCellActionSummary(
    string IntendedAction,
    string TargetSummary,
    bool PlannedOnly = true,
    bool CanExecute = false,
    bool LiveRuntimeEnabled = false);

public sealed record RecipeLabCellRiskSummary(
    RecipeRiskLevel RiskLevel,
    IReadOnlySet<SensitiveActionCategory> SensitiveCategories,
    bool RequiresHumanOrApproval);

public sealed record RecipeLabCell(
    string CellId,
    RecipeLabCellKind Kind,
    RecipeLabCellStatus Status,
    string Label,
    string RedactedSummary,
    RecipeLabCellEvidenceSummary? EvidenceSummary = null,
    RecipeLabCellActionSummary? ActionSummary = null,
    RecipeLabCellRiskSummary? RiskSummary = null,
    IReadOnlyList<string>? SourceRefs = null)
{
    public bool InspectionOnly => true;
    public bool CanExecute => false;
    public bool CanApplyRepair => false;
    public bool CanStartRecipeRun => false;
    public bool CanApproveLiveRuntime => false;
    public bool RawSecretValuesShown => false;
}

public sealed record RecipeLabNotebook(
    string NotebookId,
    string RecipeId,
    string RecipeVersion,
    IReadOnlyList<RecipeLabCell> Cells,
    bool ReadOnly = true)
{
    public bool CanExecuteCells => false;
    public bool CanApplyLocatorRepair => false;
    public bool CanStartRecipeRun => false;
}

public enum RecipeLocatorStrategy
{
    KnownTarget,
    StableSelector,
    DomOrAccessibility,
    VisibleText,
    SemanticTarget,
    VisualAnchor,
    RelativeCoordinate,
    AIFallback,
    HumanHandoff,
    Abort
}

public enum RecipeLocatorConfidence
{
    Unknown,
    Low,
    Medium,
    High,
    Deterministic,
    Unsafe
}

public enum RecipeLocatorDriftStatus
{
    Unknown,
    Stable,
    WeakSignal,
    DriftSuspected,
    Broken,
    Ambiguous,
    Unsafe
}

public enum RecipeLocatorReplayEligibility
{
    NotEligible,
    FixtureOnly,
    PreviewOnly,
    ManualReviewOnly,
    FutureRuntimeBlocked,
    BlockedByPolicy,
    BlockedMissingEvidence,
    BlockedLiveRuntimeDisabled
}

public enum RecipeLocatorSafetyStatus
{
    SafeForPreview,
    NeedsHumanReview,
    RequiresApproval,
    BlockedSensitiveTarget,
    BlockedAIFallback,
    BlockedRelativeCoordinate,
    BlockedMissingEvidence,
    BlockedLiveRuntimeDisabled,
    UnknownUnsafe
}

public enum RecipeLocatorRepairDecisionStatus
{
    SuggestedPreviewOnly,
    RequiresHumanReview,
    BlockedSensitiveTarget,
    BlockedAIFallback,
    BlockedRelativeCoordinate,
    BlockedMissingEvidence,
    BlockedLiveRuntimeDisabled,
    UnknownUnsafe
}

public sealed record RecipeLocatorCandidateRef(string LocatorId);

public sealed record RecipeLocatorEvidenceRef(string EvidenceRefId);

public sealed record RecipeLocatorDriftReport(
    RecipeLocatorDriftStatus DriftStatus,
    string RedactedSummary,
    IReadOnlyList<string> EvidenceRefs,
    bool UsesLiveObservation = false);

public sealed record RecipeLocatorRepairSuggestion(
    string SuggestionId,
    RecipeLocatorStrategy SuggestedStrategy,
    string RedactedSelectorOrTargetSummary,
    string Reason,
    IReadOnlyList<string> EvidenceRefs,
    bool AppliesLive = false);

public sealed record RecipeLocatorRepairDecision(
    RecipeLocatorRepairDecisionStatus Status,
    string Summary,
    RecipeLocatorReplayEligibility ReplayEligibility,
    RecipeLocatorSafetyStatus SafetyStatus,
    bool CanApplyLive = false,
    bool CanReplayLive = false,
    bool ActionAuthorityGranted = false);

public sealed record RecipeLocatorCandidate(
    string LocatorId,
    string RecipeId,
    string RecipeVersion,
    string? BlockId,
    string? StepId,
    RecipeLocatorStrategy Strategy,
    string RedactedSelectorOrTargetSummary,
    RecipeLocatorConfidence Confidence,
    IReadOnlyList<string> SourceEvidenceRefs,
    RecipeLocatorDriftReport DriftReport,
    RecipeLocatorRepairSuggestion? RepairSuggestion,
    RecipeLocatorSafetyStatus SafetyStatus,
    RecipeLocatorReplayEligibility ReplayEligibility,
    bool ApprovalRequired,
    bool HumanReviewRequired,
    RecipeEvidenceRedactionStatus RedactionStatus,
    string PreferredOrRejectedReason,
    int FallbackOrder,
    string? LastKnownSafeObservationRef)
{
    public bool CanApplyRepairLive => false;
    public bool CanReplayLive => false;
    public bool LiveRuntimeEnabled => false;
    public bool ActionAuthorityGranted => false;
    public bool ReferenceOnlyEvidence => SourceEvidenceRefs.Count > 0 && !DriftReport.UsesLiveObservation;
}

public sealed record RecipeLocatorStudioSnapshot(
    string SnapshotId,
    string RecipeId,
    string RecipeVersion,
    IReadOnlyList<RecipeLocatorCandidate> Candidates,
    RecipeLocatorRepairDecision Decision,
    string SafeNextAction,
    bool PreviewOnly = true)
{
    public bool CanApplyLiveRepair => false;
    public bool CanReplayLive => false;
    public bool LiveRuntimeEnabled => false;
    public bool ActionAuthorityGranted => false;
}

public static class RecipeLocatorRepairPolicy
{
    public static RecipeLocatorRepairDecision EvaluateCandidate(
        RecipeLocatorCandidate candidate,
        bool sensitiveAction,
        bool aiFallbackAllowedByPolicy = false)
    {
        if (candidate.SourceEvidenceRefs.Count == 0)
            return Blocked(RecipeLocatorRepairDecisionStatus.BlockedMissingEvidence, RecipeLocatorSafetyStatus.BlockedMissingEvidence, RecipeLocatorReplayEligibility.BlockedMissingEvidence, "Locator repair requires reference-only evidence.");

        if (candidate.SafetyStatus == RecipeLocatorSafetyStatus.BlockedLiveRuntimeDisabled ||
            candidate.ReplayEligibility == RecipeLocatorReplayEligibility.BlockedLiveRuntimeDisabled ||
            candidate.ReplayEligibility == RecipeLocatorReplayEligibility.FutureRuntimeBlocked)
            return Blocked(RecipeLocatorRepairDecisionStatus.BlockedLiveRuntimeDisabled, RecipeLocatorSafetyStatus.BlockedLiveRuntimeDisabled, RecipeLocatorReplayEligibility.BlockedLiveRuntimeDisabled, "Locator replay or repair remains live-blocked.");

        if (candidate.Strategy == RecipeLocatorStrategy.AIFallback && (sensitiveAction || !aiFallbackAllowedByPolicy))
            return Blocked(RecipeLocatorRepairDecisionStatus.BlockedAIFallback, RecipeLocatorSafetyStatus.BlockedAIFallback, RecipeLocatorReplayEligibility.ManualReviewOnly, "AI fallback locator requires policy and human review for sensitive actions.");

        if (candidate.Strategy == RecipeLocatorStrategy.RelativeCoordinate)
            return Blocked(RecipeLocatorRepairDecisionStatus.BlockedRelativeCoordinate, RecipeLocatorSafetyStatus.BlockedRelativeCoordinate, RecipeLocatorReplayEligibility.ManualReviewOnly, "RelativeCoordinate is last-resort and requires manual review.");

        if (candidate.Confidence == RecipeLocatorConfidence.Unsafe || candidate.DriftReport.DriftStatus == RecipeLocatorDriftStatus.Unsafe)
            return Blocked(RecipeLocatorRepairDecisionStatus.UnknownUnsafe, RecipeLocatorSafetyStatus.UnknownUnsafe, RecipeLocatorReplayEligibility.BlockedByPolicy, "Unsafe locator state remains blocked.");

        if (candidate.DriftReport.DriftStatus is RecipeLocatorDriftStatus.Broken or RecipeLocatorDriftStatus.Ambiguous)
            return new(
                RecipeLocatorRepairDecisionStatus.RequiresHumanReview,
                "Broken or ambiguous locator requires human review before preview-only repair suggestion.",
                RecipeLocatorReplayEligibility.ManualReviewOnly,
                RecipeLocatorSafetyStatus.NeedsHumanReview);

        return new(
            RecipeLocatorRepairDecisionStatus.SuggestedPreviewOnly,
            "Locator candidate is available for preview/fixture-only review.",
            candidate.ReplayEligibility is RecipeLocatorReplayEligibility.NotEligible ? RecipeLocatorReplayEligibility.PreviewOnly : candidate.ReplayEligibility,
            RecipeLocatorSafetyStatus.SafeForPreview);
    }

    public static RecipeLocatorStudioSnapshot CreateSnapshot(
        string snapshotId,
        string recipeId,
        string recipeVersion,
        IReadOnlyList<RecipeLocatorCandidate> candidates,
        bool sensitiveAction = false)
    {
        var ordered = candidates.OrderBy(c => c.FallbackOrder).ToArray();
        var decision = ordered.Length == 0
            ? Blocked(RecipeLocatorRepairDecisionStatus.BlockedMissingEvidence, RecipeLocatorSafetyStatus.BlockedMissingEvidence, RecipeLocatorReplayEligibility.BlockedMissingEvidence, "No locator candidates are available.")
            : EvaluateCandidate(ordered[0], sensitiveAction);

        return new(
            snapshotId,
            recipeId,
            recipeVersion,
            ordered,
            decision,
            decision.Status == RecipeLocatorRepairDecisionStatus.SuggestedPreviewOnly
                ? "Review locator suggestion in preview; no live apply is available."
                : "Keep locator repair blocked or request human review.");
    }

    private static RecipeLocatorRepairDecision Blocked(
        RecipeLocatorRepairDecisionStatus status,
        RecipeLocatorSafetyStatus safety,
        RecipeLocatorReplayEligibility replay,
        string summary) =>
        new(status, summary, replay, safety);
}

public static class RecipeLabSnapshotFactory
{
    public static RecipeLabSnapshot Create(
        string snapshotId,
        RecipeDefinition recipe,
        RecipeRunMode runMode,
        RecipePolicyPreflightResult canonicalReadiness,
        RecipeEvidencePack? evidencePack = null,
        RecipeTimelineProjection? timelineProjection = null,
        RecipeApprovalNarrative? approvalNarrative = null,
        RecipeToolTrustRegistry? toolTrustRegistry = null,
        IReadOnlyList<RecipeSecretRef>? secretRefs = null,
        RecipeTriggerReadiness? triggerReadiness = null,
        RecipeLocatorStudioSnapshot? locatorSnapshot = null)
    {
        var blocking = canonicalReadiness.BlockingIssues.Select(ToLabIssue).ToArray();
        var warnings = canonicalReadiness.Warnings.Select(ToLabIssue).ToArray();
        var readiness = new RecipeLabReadinessSummary(
            canonicalReadiness.Status,
            canonicalReadiness.IsReady,
            blocking,
            warnings,
            LiveRuntimeEnabled: false,
            ActionAuthorityGranted: false);

        var sections = BuildSections(recipe, readiness, evidencePack, timelineProjection, approvalNarrative, toolTrustRegistry, secretRefs, triggerReadiness, locatorSnapshot);
        var safeNext = canonicalReadiness.IsReady
            ? new RecipeLabSafeNextAction(RecipeSafeNextActionKind.ContinueFixtureOnly, "Continue with fixture-safe review only.")
            : new RecipeLabSafeNextAction(RecipeSafeNextActionKind.KeepBlocked, "Resolve blocking issues before fixture review.");

        var rawDataOmitted = new[] { "raw secrets", "raw payloads", "real screenshots", "real DOM", "HAR/network logs", "live accessibility capture" };
        var operatorSummary = new RecipeLabOperatorSummary(
            canonicalReadiness.IsReady
                ? "Recipe is inspectable in fixture-safe Recipe Lab."
                : "Recipe has blocking issues visible in Recipe Lab.",
            "Read-only preview; no live execution.",
            rawDataOmitted);

        var viewModel = new RecipeLabViewModel(
            $"view:{snapshotId}",
            sections,
            operatorSummary);

        return new(
            snapshotId,
            recipe.RecipeId ?? recipe.Name,
            recipe.Version ?? "0.0.0",
            recipe.DisplayName ?? recipe.Name,
            recipe.Category,
            recipe.SystemTarget,
            recipe.RegionCountry,
            runMode,
            readiness,
            SummarizeLimits(recipe.RunLimits),
            SummarizeCompleteCriteria(recipe.CompleteCriteria),
            SummarizeTerminateCriteria(recipe.TerminateCriteria),
            $"{recipe.ValidationPolicy?.Requirements.Count ?? 0} validation requirements.",
            recipe.RuntimeRiskProfile is null ? "No risk profile." : $"{recipe.RuntimeRiskProfile.OverallRisk} risk; {recipe.RuntimeRiskProfile.SensitiveCategories.Count} sensitive categories.",
            recipe.ActionResolutionPolicy is null ? "Missing action resolution policy." : $"{recipe.ActionResolutionPolicy.Attempts.Count} action resolution attempts.",
            evidencePack is null ? "No evidence pack attached." : $"{evidencePack.CompletenessStatus}; capture mode {evidencePack.CaptureMode}.",
            timelineProjection is null ? "No timeline projection attached." : $"{timelineProjection.Status}; {timelineProjection.Events.Count} events.",
            approvalNarrative is null ? "No approval narrative attached." : $"Approval narrative {approvalNarrative.NarrativeId}; decisions remain narrative-bound.",
            new RecipeLabCapabilitySummary(
                recipe.RequiredCapabilities,
                recipe.RequiredToolTrustRefs,
                (secretRefs ?? []).Select(s => $"{s.SecretRefId}:{s.DisplayAlias}").Concat(recipe.RequiredSecretRefs).Distinct().ToArray(),
                recipe.TriggerRefs,
                recipe.DetectorRefs),
            triggerReadiness is null ? "No trigger attached." : $"{triggerReadiness.Status}; observe-only and no autorun.",
            "Workitems are inspection refs only; no automatic processing.",
            locatorSnapshot is null ? "No locator repair snapshot attached." : $"{locatorSnapshot.Decision.Status}; preview-only.",
            safeNext,
            canonicalReadiness.BlockingIssues.Select(i => i.Message).Concat(["live runtime", "automatic recipe run", "automatic workitem processing"]).Distinct().ToArray(),
            "Redacted summaries only; raw payloads and secret values omitted.",
            operatorSummary,
            viewModel);
    }

    public static RecipeLabNotebook CreateNotebook(
        string notebookId,
        RecipeLabSnapshot snapshot,
        RecipeLocatorStudioSnapshot? locatorSnapshot = null)
    {
        var cells = new List<RecipeLabCell>
        {
            Cell("overview", RecipeLabCellKind.Overview, RecipeLabCellStatus.Ready, "Overview", snapshot.OperatorSummary.ShortExplanation),
            Cell("preflight", RecipeLabCellKind.Preflight, snapshot.Readiness.IsReady ? RecipeLabCellStatus.Ready : RecipeLabCellStatus.Blocked, "Preflight", snapshot.Readiness.CanonicalStatus.ToString()),
            Cell("validation", RecipeLabCellKind.Validation, snapshot.Readiness.BlockingIssues.Any(i => i.Status == RecipeReadinessStatus.BlockedMissingValidation) ? RecipeLabCellStatus.Blocked : RecipeLabCellStatus.Ready, "Validation", snapshot.ValidationSummary),
            Cell("evidence", RecipeLabCellKind.Evidence, snapshot.EvidenceCompletenessSummary.Contains("Blocked", StringComparison.OrdinalIgnoreCase) ? RecipeLabCellStatus.MissingEvidence : RecipeLabCellStatus.ReferenceOnly, "Evidence", snapshot.EvidenceCompletenessSummary),
            Cell("timeline", RecipeLabCellKind.Timeline, RecipeLabCellStatus.ReferenceOnly, "Timeline", snapshot.TimelineProjectionSummary),
            Cell("approval", RecipeLabCellKind.ApprovalNarrative, snapshot.ApprovalHumanInterventionSummary.StartsWith("No ", StringComparison.OrdinalIgnoreCase) ? RecipeLabCellStatus.NotStarted : RecipeLabCellStatus.NeedsHuman, "Approval narrative", snapshot.ApprovalHumanInterventionSummary),
            Cell("tool-trust", RecipeLabCellKind.ToolTrust, RecipeLabCellStatus.ReferenceOnly, "Tool trust", string.Join(", ", snapshot.CapabilitySummary.RequiredToolTrustRefs)),
            Cell("secret-ref", RecipeLabCellKind.SecretReference, RecipeLabCellStatus.Redacted, "Secret refs", string.Join(", ", snapshot.CapabilitySummary.RequiredSecretAliasesOrRefs)),
            Cell("trigger", RecipeLabCellKind.TriggerObservation, RecipeLabCellStatus.FixtureOnly, "Trigger observation", snapshot.TriggerObserveOnlySummary),
            Cell("safe-next", RecipeLabCellKind.SafeNextAction, snapshot.SafeNextAction.Kind == RecipeSafeNextActionKind.KeepBlocked ? RecipeLabCellStatus.Blocked : RecipeLabCellStatus.FixtureOnly, "Safe next action", snapshot.SafeNextAction.Summary)
        };

        if (locatorSnapshot is not null)
        {
            cells.Add(Cell("locator-candidate", RecipeLabCellKind.LocatorCandidate, RecipeLabCellStatus.ReferenceOnly, "Locator candidate", $"{locatorSnapshot.Candidates.Count} candidates."));
            cells.Add(Cell("locator-repair", RecipeLabCellKind.LocatorRepair, locatorSnapshot.Decision.Status == RecipeLocatorRepairDecisionStatus.SuggestedPreviewOnly ? RecipeLabCellStatus.FixtureOnly : RecipeLabCellStatus.NeedsHuman, "Locator repair", locatorSnapshot.Decision.Summary));
        }

        return new(notebookId, snapshot.RecipeId, snapshot.RecipeVersion, cells);
    }

    private static IReadOnlyList<RecipeLabSection> BuildSections(
        RecipeDefinition recipe,
        RecipeLabReadinessSummary readiness,
        RecipeEvidencePack? evidencePack,
        RecipeTimelineProjection? timelineProjection,
        RecipeApprovalNarrative? approvalNarrative,
        RecipeToolTrustRegistry? toolTrustRegistry,
        IReadOnlyList<RecipeSecretRef>? secretRefs,
        RecipeTriggerReadiness? triggerReadiness,
        RecipeLocatorStudioSnapshot? locatorSnapshot) =>
        [
            new("overview", "Overview", RecipeLabSectionStatus.Ready, recipe.DisplayName ?? recipe.Name, [recipe.RecipeId ?? recipe.Name]),
            new("readiness", "Canonical readiness", readiness.IsReady ? RecipeLabSectionStatus.Ready : RecipeLabSectionStatus.Blocked, readiness.CanonicalStatus.ToString(), []),
            new("evidence", "Evidence", evidencePack is null ? RecipeLabSectionStatus.NotStarted : RecipeLabSectionStatus.ReferenceOnly, evidencePack?.CompletenessStatus.ToString() ?? "No evidence pack.", evidencePack is null ? [] : [evidencePack.EvidencePackId]),
            new("timeline", "Timeline", timelineProjection is null ? RecipeLabSectionStatus.NotStarted : RecipeLabSectionStatus.ReferenceOnly, timelineProjection?.Status.ToString() ?? "No timeline projection.", timelineProjection is null ? [] : [timelineProjection.ProjectionId]),
            new("approval", "Human/approval", approvalNarrative is null ? RecipeLabSectionStatus.NotStarted : RecipeLabSectionStatus.NeedsHuman, approvalNarrative?.OperatorVisibleExplanation ?? "No approval narrative.", approvalNarrative is null ? [] : [approvalNarrative.NarrativeId]),
            new("tool-trust", "Tool trust", toolTrustRegistry is null ? RecipeLabSectionStatus.NotStarted : RecipeLabSectionStatus.ReferenceOnly, $"{toolTrustRegistry?.Entries.Count ?? 0} tool trust entries.", toolTrustRegistry?.Entries.Select(e => e.ToolId).ToArray() ?? []),
            new("secrets", "Secret refs", RecipeLabSectionStatus.Redacted, $"{secretRefs?.Count ?? recipe.RequiredSecretRefs.Count} secret refs/aliases; values omitted.", secretRefs?.Select(s => s.SecretRefId).ToArray() ?? recipe.RequiredSecretRefs.ToArray()),
            new("trigger", "Trigger observe-only", triggerReadiness is null ? RecipeLabSectionStatus.NotStarted : triggerReadiness.IsReady ? RecipeLabSectionStatus.FixtureOnly : RecipeLabSectionStatus.FutureGated, triggerReadiness?.Status.ToString() ?? "No trigger readiness.", []),
            new("locator", "Locator repair", locatorSnapshot is null ? RecipeLabSectionStatus.NotStarted : locatorSnapshot.Decision.Status == RecipeLocatorRepairDecisionStatus.SuggestedPreviewOnly ? RecipeLabSectionStatus.FixtureOnly : RecipeLabSectionStatus.NeedsHuman, locatorSnapshot?.Decision.Summary ?? "No locator studio snapshot.", locatorSnapshot?.Candidates.Select(c => c.LocatorId).ToArray() ?? [])
        ];

    private static RecipeLabBlockingIssue ToLabIssue(RecipeReadinessIssue issue) =>
        new(issue.IssueId, issue.Status, issue.Severity, issue.Message, issue.BlockId);

    private static RecipeLabCell Cell(
        string id,
        RecipeLabCellKind kind,
        RecipeLabCellStatus status,
        string label,
        string summary) =>
        new(
            id,
            kind,
            status,
            label,
            summary,
            new RecipeLabCellEvidenceSummary([], "Reference-only cell summary."),
            kind == RecipeLabCellKind.PlannedAction ? new RecipeLabCellActionSummary("planned only", "redacted target") : null,
            null,
            []);

    private static string SummarizeLimits(RecipeRunLimits? limits) =>
        limits is null ? "Missing limits." : $"MaxSteps={limits.MaxSteps}; LiveRuntimeAllowed={limits.LiveRuntimeAllowed}.";

    private static string SummarizeCompleteCriteria(RecipeCompleteCriteria? criteria) =>
        criteria is null ? "Missing complete criteria." : $"{criteria.Criteria.Count} complete criteria.";

    private static string SummarizeTerminateCriteria(RecipeTerminateCriteria? criteria) =>
        criteria is null ? "Missing terminate criteria." : $"{criteria.Criteria.Count} terminate criteria.";
}
