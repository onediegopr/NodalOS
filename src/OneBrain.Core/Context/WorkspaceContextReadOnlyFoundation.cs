using OneBrain.Core.Evidence;

namespace OneBrain.Core.Context;

public enum WorkspaceContextItemKind
{
    WorkspaceIdentity,
    WorkspaceBoundary,
    ContextPacketSummary,
    SelectedContext,
    LockedContext,
    ExcludedContext,
    EvidenceLinkedContextReference,
    AuthorityPolicy,
    FreshnessSignal,
    ContradictionMemoryPreview,
    RiskMemoryPreview,
    DecisionMemoryPreview,
    ClaimMemoryPreview,
    ActionMemoryPreview,
    MissingContextWarning,
    StaleContextWarning,
    SensitiveUnsafeContextBlocker,
    ProviderCloudDisabledNotice,
    SemanticVectorDisabledNotice,
    SafeNextStep,
    NoSideEffectProof,
    DeferredCapability
}

public enum WorkspaceContextItemStatus
{
    Ready,
    Selected,
    Locked,
    Excluded,
    Warning,
    Blocked,
    Disabled,
    Deferred
}

public enum WorkspaceContextSourceKind
{
    Fixture,
    EilReadOnlyEvidence,
    EilTimelineExportPreview,
    HumanProvidedFixture,
    CapabilityNotice,
    ProviderCloudDerived,
    SemanticVectorDerived,
    LegacyWithoutProvenance,
    NoSideEffectProof,
    DocumentedDebt
}

public enum WorkspaceContextAuthorityLevel
{
    Informational,
    EvidenceLinked,
    HumanReviewRequired,
    LockedBySafety,
    ExcludedBySafety
}

public enum WorkspaceContextFreshnessStatus
{
    FixtureCurrent,
    Fresh,
    Stale,
    Missing,
    NotApplicable
}

public enum WorkspaceContextSensitivityLevel
{
    Safe,
    Sensitive,
    Unknown,
    RawPayload,
    SensitiveNeverUse
}

public enum WorkspaceContextAuthorityFreshnessDecision
{
    AllowedReadOnly,
    WarningReadOnlyOnly,
    NeedsHumanReview,
    Blocked,
    Excluded
}

public enum WorkspaceContextAuthorityFreshnessIssueKind
{
    None,
    FixtureOnlyNotRealWorldTrusted,
    StaleContext,
    MissingFreshness,
    UnknownAuthority,
    LowAuthority,
    ContradictoryContext,
    MemoryCandidateWithoutEvidence,
    MemoryCandidateWithStaleEvidence,
    SelectedUnknownAuthority,
    LockedContextStale,
    ExcludedContextSelected,
    SensitiveContextWithoutClearance,
    RawPayloadContext,
    ProviderDerivedWhileDisabled,
    SemanticDerivedWhileDisabled,
    LegacyWithoutProvenance,
    SafeNextStepReliesOnStaleContext,
    DecisionMemoryMissingHumanReview
}

public enum WorkspaceContextSelectionState
{
    NotSelected,
    Selected,
    Empty
}

public enum WorkspaceContextLockState
{
    Unlocked,
    LockedBySafety,
    LockedRequiresHumanReview,
    Conflicting
}

public enum WorkspaceContextExclusionState
{
    NotExcluded,
    Excluded,
    Conflicting
}

public enum WorkspaceContextSelectionLockExclusionDecision
{
    AllowedReadOnly,
    WarningReadOnlyOnly,
    NeedsHumanReview,
    Blocked,
    Excluded
}

public enum WorkspaceContextSelectionLockExclusionIssueKind
{
    None,
    SelectedExcluded,
    SelectedLockedBySafety,
    SelectedStale,
    SelectedUnknownAuthority,
    SelectedMissingFreshness,
    SelectedContradictory,
    LockedStale,
    LockedMissingEvidence,
    LockedMemoryPromotion,
    ExcludedReferencedByMemory,
    ExcludedReferencedBySafeNextStep,
    ExcludedReferencedByClaimActionPreview,
    ExcludedReferencedByGraph,
    UnsafeSelectedContent,
    ProviderDerivedWhileDisabled,
    SemanticDerivedWhileDisabled,
    LegacyWithoutProvenance,
    DuplicateConflictingLockState,
    EmptySelectionWithDependentSafeNextStep,
    LockedMissingHumanReview,
    ExcludedAppearsInExportDashboardCandidate,
    AuthorityFreshnessBlocked
}

public sealed record WorkspaceContextNoSideEffectProof(
    bool ReadOnly,
    bool Deterministic,
    bool FixtureSafe,
    bool WorkspaceFilesystemReadAttempted,
    bool FilesystemWriteAttempted,
    bool DatabaseTouched,
    bool DurablePersistenceActive,
    bool DurableMemoryActive,
    bool VectorSemanticBackendTouched,
    bool LlmProviderTouched,
    bool ProviderCloudTouched,
    bool MigrationRunnerStarted,
    bool MigrationExecuted,
    bool RuntimeTouched,
    bool BrowserCdpTouched,
    bool WcuTouched,
    bool OcrTouched,
    bool ProductActionExposed,
    bool ProductServiceRegistered)
{
    public bool Passes =>
        ReadOnly
        && Deterministic
        && FixtureSafe
        && !WorkspaceFilesystemReadAttempted
        && !FilesystemWriteAttempted
        && !DatabaseTouched
        && !DurablePersistenceActive
        && !DurableMemoryActive
        && !VectorSemanticBackendTouched
        && !LlmProviderTouched
        && !ProviderCloudTouched
        && !MigrationRunnerStarted
        && !MigrationExecuted
        && !RuntimeTouched
        && !BrowserCdpTouched
        && !WcuTouched
        && !OcrTouched
        && !ProductActionExposed
        && !ProductServiceRegistered;

    public static WorkspaceContextNoSideEffectProof FixtureReadOnly() =>
        new(
            ReadOnly: true,
            Deterministic: true,
            FixtureSafe: true,
            WorkspaceFilesystemReadAttempted: false,
            FilesystemWriteAttempted: false,
            DatabaseTouched: false,
            DurablePersistenceActive: false,
            DurableMemoryActive: false,
            VectorSemanticBackendTouched: false,
            LlmProviderTouched: false,
            ProviderCloudTouched: false,
            MigrationRunnerStarted: false,
            MigrationExecuted: false,
            RuntimeTouched: false,
            BrowserCdpTouched: false,
            WcuTouched: false,
            OcrTouched: false,
            ProductActionExposed: false,
            ProductServiceRegistered: false);
}

public sealed record WorkspaceContextSourceDescriptor(
    string SourceId,
    string Label,
    WorkspaceContextSourceKind SourceKind,
    WorkspaceContextAuthorityLevel Authority,
    WorkspaceContextFreshnessStatus Freshness,
    bool FixtureOnly,
    bool ReadsWorkspaceFilesystem,
    bool UsesProviderCloud,
    bool UsesVectorSemanticBackend,
    IReadOnlyList<string> EvidenceRefs,
    IReadOnlyList<string> Warnings,
    IReadOnlyList<string> Blockers,
    WorkspaceContextNoSideEffectProof NoSideEffectProof);

public sealed record WorkspaceContextItem(
    string ItemId,
    WorkspaceContextItemKind Kind,
    WorkspaceContextItemStatus Status,
    string Title,
    string Summary,
    WorkspaceContextSourceKind Source,
    WorkspaceContextAuthorityLevel Authority,
    WorkspaceContextFreshnessStatus Freshness,
    IReadOnlyList<string> EvidenceRefs,
    IReadOnlyList<string> Warnings,
    IReadOnlyList<string> Blockers,
    bool Selected,
    bool Locked,
    bool Excluded,
    WorkspaceContextNoSideEffectProof NoSideEffectProof);

public sealed record WorkspaceContextMemoryCandidate(
    string CandidateId,
    WorkspaceContextItemKind Kind,
    WorkspaceContextItemStatus Status,
    string Title,
    string Preview,
    WorkspaceContextAuthorityLevel Authority,
    WorkspaceContextFreshnessStatus Freshness,
    IReadOnlyList<string> EvidenceRefs,
    IReadOnlyList<string> Warnings,
    IReadOnlyList<string> Blockers,
    bool DurableMemoryEnabled,
    bool Selected,
    bool Locked,
    bool Excluded,
    WorkspaceContextNoSideEffectProof NoSideEffectProof);

public sealed record WorkspaceContextAuthorityFreshnessFixture(
    string FixtureId,
    WorkspaceContextSourceKind SourceKind,
    WorkspaceContextAuthorityLevel Authority,
    WorkspaceContextFreshnessStatus Freshness,
    WorkspaceContextSensitivityLevel Sensitivity,
    IReadOnlyList<string> EvidenceRefs,
    bool Selected,
    bool Locked,
    bool Excluded,
    bool Contradictory,
    bool MemoryCandidate,
    bool SafeNextStep,
    bool DecisionMemory,
    bool HumanReviewed,
    WorkspaceContextAuthorityFreshnessDecision ExpectedDecision,
    WorkspaceContextAuthorityFreshnessIssueKind ExpectedIssue,
    string ExpectedMessage,
    WorkspaceContextNoSideEffectProof NoSideEffectProof);

public sealed record WorkspaceContextAuthorityFreshnessIssue(
    WorkspaceContextAuthorityFreshnessIssueKind IssueKind,
    string Message,
    bool BlocksDecision,
    bool BlocksSafeNextStep,
    bool RequiresHumanReview);

public sealed record WorkspaceContextAuthorityFreshnessResult(
    string FixtureId,
    WorkspaceContextAuthorityFreshnessDecision Decision,
    IReadOnlyList<WorkspaceContextAuthorityFreshnessIssue> Issues,
    IReadOnlyList<string> Warnings,
    IReadOnlyList<string> Blockers,
    bool AllowsReadOnlySummary,
    bool AllowsDecisionUse,
    bool AllowsSafeNextStepUse,
    bool AllowsMemoryCandidateUse,
    bool RequiresHumanReview,
    bool ProviderCloudDisabled,
    bool SemanticVectorDisabled,
    WorkspaceContextNoSideEffectProof NoSideEffectProof)
{
    public bool Blocked => Decision is WorkspaceContextAuthorityFreshnessDecision.Blocked or WorkspaceContextAuthorityFreshnessDecision.Excluded;
    public bool HasIssue(WorkspaceContextAuthorityFreshnessIssueKind issueKind) => Issues.Any(issue => issue.IssueKind == issueKind);
}

public sealed record WorkspaceContextSelectionLockExclusionFixture(
    string FixtureId,
    WorkspaceContextSelectionState SelectionState,
    WorkspaceContextLockState LockState,
    WorkspaceContextExclusionState ExclusionState,
    WorkspaceContextSourceKind SourceKind,
    WorkspaceContextAuthorityLevel Authority,
    WorkspaceContextFreshnessStatus Freshness,
    WorkspaceContextSensitivityLevel Sensitivity,
    IReadOnlyList<string> EvidenceRefs,
    IReadOnlyList<string> DependentMemoryRefs,
    IReadOnlyList<string> DependentSafeNextStepRefs,
    IReadOnlyList<string> DependentClaimActionPreviewRefs,
    IReadOnlyList<string> DependentGraphRefs,
    bool Contradictory,
    bool MemoryCandidate,
    bool MemoryPromotionAttempted,
    bool DuplicateConflictingLockStates,
    bool HumanReviewed,
    bool AppearsInExportDashboardCandidateList,
    WorkspaceContextSelectionLockExclusionDecision ExpectedDecision,
    WorkspaceContextSelectionLockExclusionIssueKind ExpectedIssue,
    string ExpectedMessage,
    WorkspaceContextNoSideEffectProof NoSideEffectProof);

public sealed record WorkspaceContextSelectionLockExclusionIssue(
    WorkspaceContextSelectionLockExclusionIssueKind IssueKind,
    string Message,
    bool BlocksDecision,
    bool BlocksSafeNextStep,
    bool BlocksMemoryInfluence,
    bool BlocksExportDashboardAppearance,
    bool RequiresHumanReview);

public sealed record WorkspaceContextSelectionLockExclusionResult(
    string FixtureId,
    WorkspaceContextSelectionLockExclusionDecision Decision,
    IReadOnlyList<WorkspaceContextSelectionLockExclusionIssue> Issues,
    IReadOnlyList<string> Warnings,
    IReadOnlyList<string> Blockers,
    bool AllowsReadOnlySummary,
    bool AllowsDecisionUse,
    bool AllowsSafeNextStepUse,
    bool AllowsMemoryInfluence,
    bool AllowsExportDashboardAppearance,
    bool RequiresHumanReview,
    WorkspaceContextNoSideEffectProof NoSideEffectProof)
{
    public bool Blocked => Decision is WorkspaceContextSelectionLockExclusionDecision.Blocked or WorkspaceContextSelectionLockExclusionDecision.Excluded;
    public bool HasIssue(WorkspaceContextSelectionLockExclusionIssueKind issueKind) => Issues.Any(issue => issue.IssueKind == issueKind);
}

public sealed record WorkspaceContextPacketReadOnly(
    string PacketId,
    string WorkspaceId,
    string Title,
    string Mode,
    string SourceLabel,
    string Summary,
    IReadOnlyList<WorkspaceContextSourceDescriptor> Sources,
    IReadOnlyList<WorkspaceContextItem> Items,
    IReadOnlyList<WorkspaceContextMemoryCandidate> MemoryCandidates,
    IReadOnlyList<WorkspaceContextItem> SelectedContext,
    IReadOnlyList<WorkspaceContextItem> LockedContext,
    IReadOnlyList<WorkspaceContextItem> ExcludedContext,
    IReadOnlyList<string> Warnings,
    IReadOnlyList<string> Blockers,
    IReadOnlyList<string> DocumentedDebt,
    string ProviderCloudNotice,
    string SemanticVectorNotice,
    string SafeNextStep,
    string ReadOnlySummary,
    WorkspaceContextNoSideEffectProof NoSideEffectProof)
{
    public bool ReadOnly => NoSideEffectProof.ReadOnly;
    public bool Deterministic => NoSideEffectProof.Deterministic;
    public bool FixtureSafe => NoSideEffectProof.FixtureSafe;
    public bool HasDurableMemory => MemoryCandidates.Any(candidate => candidate.DurableMemoryEnabled)
        || NoSideEffectProof.DurableMemoryActive;
    public bool HasProductActions => NoSideEffectProof.ProductActionExposed;
}

public static class WorkspaceContextAuthorityFreshnessGuard
{
    public static IReadOnlyList<WorkspaceContextAuthorityFreshnessFixture> CreateFixtureCatalog()
    {
        var proof = WorkspaceContextNoSideEffectProof.FixtureReadOnly();

        return
        [
            Fixture("ctx.evidence-linked-current", WorkspaceContextSourceKind.EilReadOnlyEvidence, WorkspaceContextAuthorityLevel.EvidenceLinked, WorkspaceContextFreshnessStatus.FixtureCurrent, WorkspaceContextSensitivityLevel.Safe, ["ev.context.current"], selected: true, locked: false, excluded: false, contradictory: false, memory: false, safeNext: false, decision: false, human: false, WorkspaceContextAuthorityFreshnessDecision.AllowedReadOnly, WorkspaceContextAuthorityFreshnessIssueKind.None, "Evidence-linked current context can be summarized read-only.", proof),
            Fixture("ctx.human-reviewed-current", WorkspaceContextSourceKind.HumanProvidedFixture, WorkspaceContextAuthorityLevel.HumanReviewRequired, WorkspaceContextFreshnessStatus.Fresh, WorkspaceContextSensitivityLevel.Safe, ["ev.context.human"], selected: true, locked: false, excluded: false, contradictory: false, memory: false, safeNext: false, decision: false, human: true, WorkspaceContextAuthorityFreshnessDecision.AllowedReadOnly, WorkspaceContextAuthorityFreshnessIssueKind.None, "Human-reviewed current context can be summarized read-only.", proof),
            Fixture("ctx.fixture-only-warning", WorkspaceContextSourceKind.Fixture, WorkspaceContextAuthorityLevel.Informational, WorkspaceContextFreshnessStatus.FixtureCurrent, WorkspaceContextSensitivityLevel.Safe, [], selected: true, locked: false, excluded: false, contradictory: false, memory: false, safeNext: false, decision: false, human: false, WorkspaceContextAuthorityFreshnessDecision.WarningReadOnlyOnly, WorkspaceContextAuthorityFreshnessIssueKind.FixtureOnlyNotRealWorldTrusted, "Fixture-only context is preview-only.", proof),
            Fixture("ctx.stale-context", WorkspaceContextSourceKind.EilReadOnlyEvidence, WorkspaceContextAuthorityLevel.EvidenceLinked, WorkspaceContextFreshnessStatus.Stale, WorkspaceContextSensitivityLevel.Safe, ["ev.context.stale"], selected: true, locked: false, excluded: false, contradictory: false, memory: false, safeNext: false, decision: true, human: false, WorkspaceContextAuthorityFreshnessDecision.Blocked, WorkspaceContextAuthorityFreshnessIssueKind.StaleContext, "Stale context cannot feed decision use.", proof),
            Fixture("ctx.missing-freshness", WorkspaceContextSourceKind.EilReadOnlyEvidence, WorkspaceContextAuthorityLevel.EvidenceLinked, WorkspaceContextFreshnessStatus.Missing, WorkspaceContextSensitivityLevel.Safe, ["ev.context.missing-freshness"], selected: true, locked: false, excluded: false, contradictory: false, memory: false, safeNext: false, decision: false, human: false, WorkspaceContextAuthorityFreshnessDecision.Blocked, WorkspaceContextAuthorityFreshnessIssueKind.MissingFreshness, "Missing freshness blocks context use.", proof),
            Fixture("ctx.unknown-authority", WorkspaceContextSourceKind.Fixture, WorkspaceContextAuthorityLevel.Informational, WorkspaceContextFreshnessStatus.Fresh, WorkspaceContextSensitivityLevel.Unknown, ["ev.context.unknown-authority"], selected: false, locked: false, excluded: false, contradictory: false, memory: false, safeNext: false, decision: false, human: false, WorkspaceContextAuthorityFreshnessDecision.Blocked, WorkspaceContextAuthorityFreshnessIssueKind.UnknownAuthority, "Unknown authority blocks context use.", proof),
            Fixture("ctx.low-authority-source", WorkspaceContextSourceKind.Fixture, WorkspaceContextAuthorityLevel.Informational, WorkspaceContextFreshnessStatus.Fresh, WorkspaceContextSensitivityLevel.Safe, [], selected: false, locked: false, excluded: false, contradictory: false, memory: false, safeNext: false, decision: false, human: false, WorkspaceContextAuthorityFreshnessDecision.WarningReadOnlyOnly, WorkspaceContextAuthorityFreshnessIssueKind.LowAuthority, "Low-authority context stays preview-only.", proof),
            Fixture("ctx.contradictory", WorkspaceContextSourceKind.EilReadOnlyEvidence, WorkspaceContextAuthorityLevel.EvidenceLinked, WorkspaceContextFreshnessStatus.Fresh, WorkspaceContextSensitivityLevel.Safe, ["ev.context.contradiction"], selected: true, locked: false, excluded: false, contradictory: true, memory: false, safeNext: false, decision: true, human: false, WorkspaceContextAuthorityFreshnessDecision.Blocked, WorkspaceContextAuthorityFreshnessIssueKind.ContradictoryContext, "Contradictory context blocks decision use.", proof),
            Fixture("memory.no-evidence", WorkspaceContextSourceKind.Fixture, WorkspaceContextAuthorityLevel.EvidenceLinked, WorkspaceContextFreshnessStatus.Fresh, WorkspaceContextSensitivityLevel.Safe, [], selected: true, locked: false, excluded: false, contradictory: false, memory: true, safeNext: false, decision: false, human: false, WorkspaceContextAuthorityFreshnessDecision.Blocked, WorkspaceContextAuthorityFreshnessIssueKind.MemoryCandidateWithoutEvidence, "Memory candidates require evidence refs.", proof),
            Fixture("memory.stale-evidence", WorkspaceContextSourceKind.EilReadOnlyEvidence, WorkspaceContextAuthorityLevel.EvidenceLinked, WorkspaceContextFreshnessStatus.Stale, WorkspaceContextSensitivityLevel.Safe, ["ev.memory.stale"], selected: true, locked: false, excluded: false, contradictory: false, memory: true, safeNext: false, decision: false, human: false, WorkspaceContextAuthorityFreshnessDecision.Blocked, WorkspaceContextAuthorityFreshnessIssueKind.MemoryCandidateWithStaleEvidence, "Memory candidates with stale evidence are blocked.", proof),
            Fixture("ctx.selected-unknown-authority", WorkspaceContextSourceKind.Fixture, WorkspaceContextAuthorityLevel.Informational, WorkspaceContextFreshnessStatus.Fresh, WorkspaceContextSensitivityLevel.Unknown, ["ev.context.selected-unknown"], selected: true, locked: false, excluded: false, contradictory: false, memory: false, safeNext: false, decision: false, human: false, WorkspaceContextAuthorityFreshnessDecision.Blocked, WorkspaceContextAuthorityFreshnessIssueKind.SelectedUnknownAuthority, "Selected context with unknown authority is blocked.", proof),
            Fixture("ctx.locked-stale", WorkspaceContextSourceKind.EilReadOnlyEvidence, WorkspaceContextAuthorityLevel.LockedBySafety, WorkspaceContextFreshnessStatus.Stale, WorkspaceContextSensitivityLevel.Safe, ["ev.context.locked-stale"], selected: false, locked: true, excluded: false, contradictory: false, memory: false, safeNext: false, decision: false, human: false, WorkspaceContextAuthorityFreshnessDecision.NeedsHumanReview, WorkspaceContextAuthorityFreshnessIssueKind.LockedContextStale, "Locked stale context requires human review.", proof),
            Fixture("ctx.excluded-selected", WorkspaceContextSourceKind.CapabilityNotice, WorkspaceContextAuthorityLevel.ExcludedBySafety, WorkspaceContextFreshnessStatus.NotApplicable, WorkspaceContextSensitivityLevel.SensitiveNeverUse, [], selected: true, locked: false, excluded: true, contradictory: false, memory: false, safeNext: false, decision: false, human: false, WorkspaceContextAuthorityFreshnessDecision.Excluded, WorkspaceContextAuthorityFreshnessIssueKind.ExcludedContextSelected, "Excluded context cannot be selected.", proof),
            Fixture("ctx.sensitive-without-clearance", WorkspaceContextSourceKind.HumanProvidedFixture, WorkspaceContextAuthorityLevel.HumanReviewRequired, WorkspaceContextFreshnessStatus.Fresh, WorkspaceContextSensitivityLevel.Sensitive, ["ev.context.sensitive"], selected: true, locked: false, excluded: false, contradictory: false, memory: false, safeNext: false, decision: false, human: false, WorkspaceContextAuthorityFreshnessDecision.Blocked, WorkspaceContextAuthorityFreshnessIssueKind.SensitiveContextWithoutClearance, "Sensitive context requires human clearance.", proof),
            Fixture("ctx.raw-payload", WorkspaceContextSourceKind.Fixture, WorkspaceContextAuthorityLevel.ExcludedBySafety, WorkspaceContextFreshnessStatus.NotApplicable, WorkspaceContextSensitivityLevel.RawPayload, [], selected: false, locked: true, excluded: true, contradictory: false, memory: false, safeNext: false, decision: false, human: false, WorkspaceContextAuthorityFreshnessDecision.Excluded, WorkspaceContextAuthorityFreshnessIssueKind.RawPayloadContext, "Excluded payload context is blocked.", proof),
            Fixture("ctx.provider-derived-disabled", WorkspaceContextSourceKind.ProviderCloudDerived, WorkspaceContextAuthorityLevel.EvidenceLinked, WorkspaceContextFreshnessStatus.Fresh, WorkspaceContextSensitivityLevel.Safe, ["ev.context.provider"], selected: false, locked: false, excluded: false, contradictory: false, memory: false, safeNext: false, decision: false, human: false, WorkspaceContextAuthorityFreshnessDecision.Blocked, WorkspaceContextAuthorityFreshnessIssueKind.ProviderDerivedWhileDisabled, "Provider-derived context is blocked while provider/cloud is disabled.", proof),
            Fixture("ctx.semantic-derived-disabled", WorkspaceContextSourceKind.SemanticVectorDerived, WorkspaceContextAuthorityLevel.EvidenceLinked, WorkspaceContextFreshnessStatus.Fresh, WorkspaceContextSensitivityLevel.Safe, ["ev.context.semantic"], selected: false, locked: false, excluded: false, contradictory: false, memory: false, safeNext: false, decision: false, human: false, WorkspaceContextAuthorityFreshnessDecision.Blocked, WorkspaceContextAuthorityFreshnessIssueKind.SemanticDerivedWhileDisabled, "Semantic-derived context is blocked while semantic/vector is disabled.", proof),
            Fixture("ctx.legacy-no-provenance", WorkspaceContextSourceKind.LegacyWithoutProvenance, WorkspaceContextAuthorityLevel.Informational, WorkspaceContextFreshnessStatus.Missing, WorkspaceContextSensitivityLevel.Unknown, [], selected: false, locked: false, excluded: false, contradictory: false, memory: false, safeNext: false, decision: false, human: false, WorkspaceContextAuthorityFreshnessDecision.Blocked, WorkspaceContextAuthorityFreshnessIssueKind.LegacyWithoutProvenance, "Legacy context without provenance is blocked.", proof),
            Fixture("safe-next-step.stale", WorkspaceContextSourceKind.EilReadOnlyEvidence, WorkspaceContextAuthorityLevel.EvidenceLinked, WorkspaceContextFreshnessStatus.Stale, WorkspaceContextSensitivityLevel.Safe, ["ev.safe-next-step.stale"], selected: true, locked: false, excluded: false, contradictory: false, memory: false, safeNext: true, decision: false, human: false, WorkspaceContextAuthorityFreshnessDecision.Blocked, WorkspaceContextAuthorityFreshnessIssueKind.SafeNextStepReliesOnStaleContext, "Safe next step cannot rely on stale context.", proof),
            Fixture("memory.decision-missing-human-review", WorkspaceContextSourceKind.EilReadOnlyEvidence, WorkspaceContextAuthorityLevel.HumanReviewRequired, WorkspaceContextFreshnessStatus.Fresh, WorkspaceContextSensitivityLevel.Safe, ["ev.memory.decision"], selected: true, locked: false, excluded: false, contradictory: false, memory: true, safeNext: false, decision: true, human: false, WorkspaceContextAuthorityFreshnessDecision.Blocked, WorkspaceContextAuthorityFreshnessIssueKind.DecisionMemoryMissingHumanReview, "Decision memory requires human review.", proof)
        ];
    }

    public static WorkspaceContextAuthorityFreshnessResult Evaluate(WorkspaceContextAuthorityFreshnessFixture fixture)
    {
        var issues = new List<WorkspaceContextAuthorityFreshnessIssue>();

        AddIssues(fixture, issues);

        var decision = Decide(fixture, issues);
        var blockers = issues.Where(issue => issue.BlocksDecision || issue.BlocksSafeNextStep).Select(issue => issue.Message).ToList();
        var warnings = issues.Where(issue => !issue.BlocksDecision && !issue.BlocksSafeNextStep).Select(issue => issue.Message).ToList();
        var blocksDecision = issues.Any(issue => issue.BlocksDecision);
        var blocksSafeNextStep = issues.Any(issue => issue.BlocksSafeNextStep);
        var requiresHumanReview = issues.Any(issue => issue.RequiresHumanReview) || fixture.Authority == WorkspaceContextAuthorityLevel.HumanReviewRequired && !fixture.HumanReviewed;

        return new WorkspaceContextAuthorityFreshnessResult(
            fixture.FixtureId,
            decision,
            issues,
            warnings,
            blockers,
            AllowsReadOnlySummary: decision is WorkspaceContextAuthorityFreshnessDecision.AllowedReadOnly or WorkspaceContextAuthorityFreshnessDecision.WarningReadOnlyOnly or WorkspaceContextAuthorityFreshnessDecision.NeedsHumanReview,
            AllowsDecisionUse: !blocksDecision && decision == WorkspaceContextAuthorityFreshnessDecision.AllowedReadOnly && !requiresHumanReview,
            AllowsSafeNextStepUse: !blocksSafeNextStep && decision == WorkspaceContextAuthorityFreshnessDecision.AllowedReadOnly && !requiresHumanReview,
            AllowsMemoryCandidateUse: fixture.MemoryCandidate && decision == WorkspaceContextAuthorityFreshnessDecision.AllowedReadOnly && fixture.EvidenceRefs.Count > 0 && !requiresHumanReview,
            RequiresHumanReview: requiresHumanReview || decision == WorkspaceContextAuthorityFreshnessDecision.NeedsHumanReview,
            ProviderCloudDisabled: true,
            SemanticVectorDisabled: true,
            NoSideEffectProof: fixture.NoSideEffectProof);
    }

    public static IReadOnlyList<WorkspaceContextAuthorityFreshnessResult> EvaluateCatalog() =>
        CreateFixtureCatalog().Select(Evaluate).ToList();

    private static void AddIssues(WorkspaceContextAuthorityFreshnessFixture fixture, List<WorkspaceContextAuthorityFreshnessIssue> issues)
    {
        if (fixture.Excluded && fixture.Selected)
            issues.Add(Issue(WorkspaceContextAuthorityFreshnessIssueKind.ExcludedContextSelected, "Excluded context cannot be selected.", blockDecision: true, blockSafeNext: true, human: true));

        if (fixture.Sensitivity == WorkspaceContextSensitivityLevel.RawPayload)
            issues.Add(Issue(WorkspaceContextAuthorityFreshnessIssueKind.RawPayloadContext, "Excluded payload context is blocked.", blockDecision: true, blockSafeNext: true, human: true));

        if (fixture.Sensitivity == WorkspaceContextSensitivityLevel.SensitiveNeverUse)
            issues.Add(Issue(WorkspaceContextAuthorityFreshnessIssueKind.SensitiveContextWithoutClearance, "Sensitive-never-use context is blocked.", blockDecision: true, blockSafeNext: true, human: true));

        if (fixture.Sensitivity == WorkspaceContextSensitivityLevel.Sensitive && !fixture.HumanReviewed)
            issues.Add(Issue(WorkspaceContextAuthorityFreshnessIssueKind.SensitiveContextWithoutClearance, "Sensitive context requires human clearance.", blockDecision: true, blockSafeNext: true, human: true));

        if (fixture.Sensitivity == WorkspaceContextSensitivityLevel.Unknown)
            issues.Add(Issue(fixture.Selected ? WorkspaceContextAuthorityFreshnessIssueKind.SelectedUnknownAuthority : WorkspaceContextAuthorityFreshnessIssueKind.UnknownAuthority, "Unknown authority or sensitivity blocks context use.", blockDecision: true, blockSafeNext: true, human: true));

        if (fixture.Freshness == WorkspaceContextFreshnessStatus.Missing)
            issues.Add(Issue(WorkspaceContextAuthorityFreshnessIssueKind.MissingFreshness, "Missing freshness blocks context use.", blockDecision: true, blockSafeNext: true, human: true));

        if (fixture.Freshness == WorkspaceContextFreshnessStatus.Stale && fixture.Locked && !fixture.DecisionMemory && !fixture.SafeNextStep && !fixture.MemoryCandidate)
            issues.Add(Issue(WorkspaceContextAuthorityFreshnessIssueKind.LockedContextStale, "Locked stale context requires human review.", blockDecision: false, blockSafeNext: false, human: true));
        else if (fixture.Freshness == WorkspaceContextFreshnessStatus.Stale)
            issues.Add(Issue(fixture.MemoryCandidate ? WorkspaceContextAuthorityFreshnessIssueKind.MemoryCandidateWithStaleEvidence : fixture.SafeNextStep ? WorkspaceContextAuthorityFreshnessIssueKind.SafeNextStepReliesOnStaleContext : fixture.Locked ? WorkspaceContextAuthorityFreshnessIssueKind.LockedContextStale : WorkspaceContextAuthorityFreshnessIssueKind.StaleContext, "Stale context cannot feed decisions or safe next steps.", blockDecision: true, blockSafeNext: true, human: true));

        if (fixture.SourceKind == WorkspaceContextSourceKind.Fixture && fixture.Authority == WorkspaceContextAuthorityLevel.Informational && fixture.EvidenceRefs.Count == 0)
            issues.Add(Issue(WorkspaceContextAuthorityFreshnessIssueKind.FixtureOnlyNotRealWorldTrusted, "Fixture-only context is read-only preview only.", blockDecision: false, blockSafeNext: false, human: true));

        if (fixture.Authority == WorkspaceContextAuthorityLevel.Informational && fixture.EvidenceRefs.Count == 0 && fixture.Sensitivity == WorkspaceContextSensitivityLevel.Safe)
            issues.Add(Issue(WorkspaceContextAuthorityFreshnessIssueKind.LowAuthority, "Low-authority context cannot be elevated by default.", blockDecision: false, blockSafeNext: false, human: true));

        if (fixture.Contradictory)
            issues.Add(Issue(WorkspaceContextAuthorityFreshnessIssueKind.ContradictoryContext, "Contradictory context is blocked.", blockDecision: true, blockSafeNext: true, human: true));

        if (fixture.MemoryCandidate && fixture.EvidenceRefs.Count == 0)
            issues.Add(Issue(WorkspaceContextAuthorityFreshnessIssueKind.MemoryCandidateWithoutEvidence, "Memory candidates require evidence refs.", blockDecision: true, blockSafeNext: true, human: true));

        if (fixture.DecisionMemory && fixture.Authority == WorkspaceContextAuthorityLevel.HumanReviewRequired && !fixture.HumanReviewed)
            issues.Add(Issue(WorkspaceContextAuthorityFreshnessIssueKind.DecisionMemoryMissingHumanReview, "Decision memory requires human review.", blockDecision: true, blockSafeNext: true, human: true));

        if (fixture.SourceKind == WorkspaceContextSourceKind.ProviderCloudDerived)
            issues.Add(Issue(WorkspaceContextAuthorityFreshnessIssueKind.ProviderDerivedWhileDisabled, "Provider-derived context is blocked while provider/cloud is disabled.", blockDecision: true, blockSafeNext: true, human: true));

        if (fixture.SourceKind == WorkspaceContextSourceKind.SemanticVectorDerived)
            issues.Add(Issue(WorkspaceContextAuthorityFreshnessIssueKind.SemanticDerivedWhileDisabled, "Semantic-derived context is blocked while semantic/vector is disabled.", blockDecision: true, blockSafeNext: true, human: true));

        if (fixture.SourceKind == WorkspaceContextSourceKind.LegacyWithoutProvenance)
            issues.Add(Issue(WorkspaceContextAuthorityFreshnessIssueKind.LegacyWithoutProvenance, "Legacy context without provenance is blocked.", blockDecision: true, blockSafeNext: true, human: true));
    }

    private static WorkspaceContextAuthorityFreshnessDecision Decide(
        WorkspaceContextAuthorityFreshnessFixture fixture,
        IReadOnlyList<WorkspaceContextAuthorityFreshnessIssue> issues)
    {
        if (fixture.Excluded || issues.Any(issue => issue.IssueKind is WorkspaceContextAuthorityFreshnessIssueKind.ExcludedContextSelected or WorkspaceContextAuthorityFreshnessIssueKind.RawPayloadContext))
            return WorkspaceContextAuthorityFreshnessDecision.Excluded;

        if (issues.Any(issue => issue.BlocksDecision || issue.BlocksSafeNextStep))
            return WorkspaceContextAuthorityFreshnessDecision.Blocked;

        if (issues.Any(issue => issue.IssueKind == WorkspaceContextAuthorityFreshnessIssueKind.LockedContextStale))
            return WorkspaceContextAuthorityFreshnessDecision.NeedsHumanReview;

        if (issues.Count > 0)
            return WorkspaceContextAuthorityFreshnessDecision.WarningReadOnlyOnly;

        if (fixture.Authority == WorkspaceContextAuthorityLevel.HumanReviewRequired && !fixture.HumanReviewed)
            return WorkspaceContextAuthorityFreshnessDecision.NeedsHumanReview;

        return WorkspaceContextAuthorityFreshnessDecision.AllowedReadOnly;
    }

    private static WorkspaceContextAuthorityFreshnessIssue Issue(
        WorkspaceContextAuthorityFreshnessIssueKind issueKind,
        string message,
        bool blockDecision,
        bool blockSafeNext,
        bool human) =>
        new(issueKind, message, blockDecision, blockSafeNext, human);

    private static WorkspaceContextAuthorityFreshnessFixture Fixture(
        string fixtureId,
        WorkspaceContextSourceKind sourceKind,
        WorkspaceContextAuthorityLevel authority,
        WorkspaceContextFreshnessStatus freshness,
        WorkspaceContextSensitivityLevel sensitivity,
        IReadOnlyList<string> evidenceRefs,
        bool selected,
        bool locked,
        bool excluded,
        bool contradictory,
        bool memory,
        bool safeNext,
        bool decision,
        bool human,
        WorkspaceContextAuthorityFreshnessDecision expectedDecision,
        WorkspaceContextAuthorityFreshnessIssueKind expectedIssue,
        string expectedMessage,
        WorkspaceContextNoSideEffectProof proof) =>
        new(fixtureId, sourceKind, authority, freshness, sensitivity, evidenceRefs, selected, locked, excluded, contradictory, memory, safeNext, decision, human, expectedDecision, expectedIssue, expectedMessage, proof);
}

public static class WorkspaceContextSelectionLockExclusionGuard
{
    public static IReadOnlyList<WorkspaceContextSelectionLockExclusionFixture> CreateFixtureCatalog()
    {
        var proof = WorkspaceContextNoSideEffectProof.FixtureReadOnly();

        return
        [
            Fixture("ctx.selected-evidence-fresh", WorkspaceContextSelectionState.Selected, WorkspaceContextLockState.Unlocked, WorkspaceContextExclusionState.NotExcluded, WorkspaceContextSourceKind.EilReadOnlyEvidence, WorkspaceContextAuthorityLevel.EvidenceLinked, WorkspaceContextFreshnessStatus.FixtureCurrent, WorkspaceContextSensitivityLevel.Safe, ["ev.context.current"], [], [], [], [], contradictory: false, memory: false, memoryPromotion: false, duplicateLock: false, human: false, exportDashboard: false, WorkspaceContextSelectionLockExclusionDecision.AllowedReadOnly, WorkspaceContextSelectionLockExclusionIssueKind.None, "Selected evidence-linked fresh context is allowed read-only.", proof),
            Fixture("ctx.selected-excluded", WorkspaceContextSelectionState.Selected, WorkspaceContextLockState.Unlocked, WorkspaceContextExclusionState.Excluded, WorkspaceContextSourceKind.CapabilityNotice, WorkspaceContextAuthorityLevel.ExcludedBySafety, WorkspaceContextFreshnessStatus.NotApplicable, WorkspaceContextSensitivityLevel.SensitiveNeverUse, [], [], [], [], [], contradictory: false, memory: false, memoryPromotion: false, duplicateLock: false, human: false, exportDashboard: false, WorkspaceContextSelectionLockExclusionDecision.Excluded, WorkspaceContextSelectionLockExclusionIssueKind.SelectedExcluded, "Excluded context wins over selected context.", proof),
            Fixture("ctx.selected-locked-by-safety", WorkspaceContextSelectionState.Selected, WorkspaceContextLockState.LockedBySafety, WorkspaceContextExclusionState.NotExcluded, WorkspaceContextSourceKind.CapabilityNotice, WorkspaceContextAuthorityLevel.LockedBySafety, WorkspaceContextFreshnessStatus.NotApplicable, WorkspaceContextSensitivityLevel.Safe, ["ev.context.locked"], [], [], [], [], contradictory: false, memory: false, memoryPromotion: false, duplicateLock: false, human: false, exportDashboard: false, WorkspaceContextSelectionLockExclusionDecision.NeedsHumanReview, WorkspaceContextSelectionLockExclusionIssueKind.SelectedLockedBySafety, "Selected locked context requires human review.", proof),
            Fixture("ctx.selected-stale", WorkspaceContextSelectionState.Selected, WorkspaceContextLockState.Unlocked, WorkspaceContextExclusionState.NotExcluded, WorkspaceContextSourceKind.EilReadOnlyEvidence, WorkspaceContextAuthorityLevel.EvidenceLinked, WorkspaceContextFreshnessStatus.Stale, WorkspaceContextSensitivityLevel.Safe, ["ev.context.stale"], [], ["safe.step.stale"], [], [], contradictory: false, memory: false, memoryPromotion: false, duplicateLock: false, human: false, exportDashboard: false, WorkspaceContextSelectionLockExclusionDecision.Blocked, WorkspaceContextSelectionLockExclusionIssueKind.SelectedStale, "Selected stale context cannot feed decisions or safe next steps.", proof),
            Fixture("ctx.selected-unknown-authority", WorkspaceContextSelectionState.Selected, WorkspaceContextLockState.Unlocked, WorkspaceContextExclusionState.NotExcluded, WorkspaceContextSourceKind.Fixture, WorkspaceContextAuthorityLevel.Informational, WorkspaceContextFreshnessStatus.Fresh, WorkspaceContextSensitivityLevel.Unknown, ["ev.context.unknown"], [], [], [], [], contradictory: false, memory: false, memoryPromotion: false, duplicateLock: false, human: false, exportDashboard: false, WorkspaceContextSelectionLockExclusionDecision.Blocked, WorkspaceContextSelectionLockExclusionIssueKind.SelectedUnknownAuthority, "Selected unknown authority is blocked.", proof),
            Fixture("ctx.selected-missing-freshness", WorkspaceContextSelectionState.Selected, WorkspaceContextLockState.Unlocked, WorkspaceContextExclusionState.NotExcluded, WorkspaceContextSourceKind.EilReadOnlyEvidence, WorkspaceContextAuthorityLevel.EvidenceLinked, WorkspaceContextFreshnessStatus.Missing, WorkspaceContextSensitivityLevel.Safe, ["ev.context.missing"], [], [], [], [], contradictory: false, memory: false, memoryPromotion: false, duplicateLock: false, human: false, exportDashboard: false, WorkspaceContextSelectionLockExclusionDecision.Blocked, WorkspaceContextSelectionLockExclusionIssueKind.SelectedMissingFreshness, "Selected context with missing freshness is blocked.", proof),
            Fixture("ctx.selected-contradictory", WorkspaceContextSelectionState.Selected, WorkspaceContextLockState.Unlocked, WorkspaceContextExclusionState.NotExcluded, WorkspaceContextSourceKind.EilReadOnlyEvidence, WorkspaceContextAuthorityLevel.EvidenceLinked, WorkspaceContextFreshnessStatus.Fresh, WorkspaceContextSensitivityLevel.Safe, ["ev.context.contradiction"], [], [], ["claim.preview.contradiction"], [], contradictory: true, memory: false, memoryPromotion: false, duplicateLock: false, human: false, exportDashboard: false, WorkspaceContextSelectionLockExclusionDecision.Blocked, WorkspaceContextSelectionLockExclusionIssueKind.SelectedContradictory, "Selected contradictory context is blocked.", proof),
            Fixture("ctx.locked-stale", WorkspaceContextSelectionState.NotSelected, WorkspaceContextLockState.LockedRequiresHumanReview, WorkspaceContextExclusionState.NotExcluded, WorkspaceContextSourceKind.EilReadOnlyEvidence, WorkspaceContextAuthorityLevel.LockedBySafety, WorkspaceContextFreshnessStatus.Stale, WorkspaceContextSensitivityLevel.Safe, ["ev.context.locked-stale"], [], [], [], [], contradictory: false, memory: false, memoryPromotion: false, duplicateLock: false, human: false, exportDashboard: false, WorkspaceContextSelectionLockExclusionDecision.Blocked, WorkspaceContextSelectionLockExclusionIssueKind.LockedStale, "Locked stale context requires human review and cannot feed decisions.", proof),
            Fixture("ctx.locked-missing-evidence", WorkspaceContextSelectionState.NotSelected, WorkspaceContextLockState.LockedBySafety, WorkspaceContextExclusionState.NotExcluded, WorkspaceContextSourceKind.CapabilityNotice, WorkspaceContextAuthorityLevel.LockedBySafety, WorkspaceContextFreshnessStatus.NotApplicable, WorkspaceContextSensitivityLevel.Safe, [], [], [], [], [], contradictory: false, memory: false, memoryPromotion: false, duplicateLock: false, human: false, exportDashboard: false, WorkspaceContextSelectionLockExclusionDecision.Blocked, WorkspaceContextSelectionLockExclusionIssueKind.LockedMissingEvidence, "Locked context requires evidence refs.", proof),
            Fixture("memory.locked-promote", WorkspaceContextSelectionState.Selected, WorkspaceContextLockState.LockedBySafety, WorkspaceContextExclusionState.NotExcluded, WorkspaceContextSourceKind.EilReadOnlyEvidence, WorkspaceContextAuthorityLevel.LockedBySafety, WorkspaceContextFreshnessStatus.Fresh, WorkspaceContextSensitivityLevel.Safe, ["ev.memory.locked"], ["memory.promote.locked"], [], [], [], contradictory: false, memory: true, memoryPromotion: true, duplicateLock: false, human: false, exportDashboard: false, WorkspaceContextSelectionLockExclusionDecision.Blocked, WorkspaceContextSelectionLockExclusionIssueKind.LockedMemoryPromotion, "Memory candidate cannot promote locked context.", proof),
            Fixture("memory.excluded-reference", WorkspaceContextSelectionState.NotSelected, WorkspaceContextLockState.Unlocked, WorkspaceContextExclusionState.Excluded, WorkspaceContextSourceKind.CapabilityNotice, WorkspaceContextAuthorityLevel.ExcludedBySafety, WorkspaceContextFreshnessStatus.NotApplicable, WorkspaceContextSensitivityLevel.SensitiveNeverUse, [], ["memory.ref.excluded"], [], [], [], contradictory: false, memory: true, memoryPromotion: false, duplicateLock: false, human: false, exportDashboard: false, WorkspaceContextSelectionLockExclusionDecision.Excluded, WorkspaceContextSelectionLockExclusionIssueKind.ExcludedReferencedByMemory, "Memory candidate cannot reference excluded context.", proof),
            Fixture("safe-next-step.excluded-reference", WorkspaceContextSelectionState.NotSelected, WorkspaceContextLockState.Unlocked, WorkspaceContextExclusionState.Excluded, WorkspaceContextSourceKind.CapabilityNotice, WorkspaceContextAuthorityLevel.ExcludedBySafety, WorkspaceContextFreshnessStatus.NotApplicable, WorkspaceContextSensitivityLevel.SensitiveNeverUse, [], [], ["safe.step.excluded"], [], [], contradictory: false, memory: false, memoryPromotion: false, duplicateLock: false, human: false, exportDashboard: false, WorkspaceContextSelectionLockExclusionDecision.Excluded, WorkspaceContextSelectionLockExclusionIssueKind.ExcludedReferencedBySafeNextStep, "Safe next step cannot reference excluded context.", proof),
            Fixture("claim-action.excluded-reference", WorkspaceContextSelectionState.NotSelected, WorkspaceContextLockState.Unlocked, WorkspaceContextExclusionState.Excluded, WorkspaceContextSourceKind.CapabilityNotice, WorkspaceContextAuthorityLevel.ExcludedBySafety, WorkspaceContextFreshnessStatus.NotApplicable, WorkspaceContextSensitivityLevel.SensitiveNeverUse, [], [], [], ["claim.preview.excluded", "action.preview.excluded"], [], contradictory: false, memory: false, memoryPromotion: false, duplicateLock: false, human: false, exportDashboard: false, WorkspaceContextSelectionLockExclusionDecision.Excluded, WorkspaceContextSelectionLockExclusionIssueKind.ExcludedReferencedByClaimActionPreview, "Claim/action previews cannot reference excluded context.", proof),
            Fixture("graph.excluded-reference", WorkspaceContextSelectionState.NotSelected, WorkspaceContextLockState.Unlocked, WorkspaceContextExclusionState.Excluded, WorkspaceContextSourceKind.CapabilityNotice, WorkspaceContextAuthorityLevel.ExcludedBySafety, WorkspaceContextFreshnessStatus.NotApplicable, WorkspaceContextSensitivityLevel.SensitiveNeverUse, [], [], [], [], ["graph.edge.excluded"], contradictory: false, memory: false, memoryPromotion: false, duplicateLock: false, human: false, exportDashboard: false, WorkspaceContextSelectionLockExclusionDecision.Excluded, WorkspaceContextSelectionLockExclusionIssueKind.ExcludedReferencedByGraph, "Graph refs cannot reference excluded context.", proof),
            Fixture("ctx.selected-raw-sensitive", WorkspaceContextSelectionState.Selected, WorkspaceContextLockState.LockedBySafety, WorkspaceContextExclusionState.Excluded, WorkspaceContextSourceKind.Fixture, WorkspaceContextAuthorityLevel.ExcludedBySafety, WorkspaceContextFreshnessStatus.NotApplicable, WorkspaceContextSensitivityLevel.RawPayload, [], [], [], [], [], contradictory: false, memory: false, memoryPromotion: false, duplicateLock: false, human: false, exportDashboard: false, WorkspaceContextSelectionLockExclusionDecision.Excluded, WorkspaceContextSelectionLockExclusionIssueKind.UnsafeSelectedContent, "Selected raw or sensitive unsafe context is excluded.", proof),
            Fixture("ctx.selected-provider-disabled", WorkspaceContextSelectionState.Selected, WorkspaceContextLockState.Unlocked, WorkspaceContextExclusionState.NotExcluded, WorkspaceContextSourceKind.ProviderCloudDerived, WorkspaceContextAuthorityLevel.EvidenceLinked, WorkspaceContextFreshnessStatus.Fresh, WorkspaceContextSensitivityLevel.Safe, ["ev.context.provider"], [], [], [], [], contradictory: false, memory: false, memoryPromotion: false, duplicateLock: false, human: false, exportDashboard: false, WorkspaceContextSelectionLockExclusionDecision.Blocked, WorkspaceContextSelectionLockExclusionIssueKind.ProviderDerivedWhileDisabled, "Provider-derived selected context is blocked while provider/cloud is disabled.", proof),
            Fixture("ctx.selected-semantic-disabled", WorkspaceContextSelectionState.Selected, WorkspaceContextLockState.Unlocked, WorkspaceContextExclusionState.NotExcluded, WorkspaceContextSourceKind.SemanticVectorDerived, WorkspaceContextAuthorityLevel.EvidenceLinked, WorkspaceContextFreshnessStatus.Fresh, WorkspaceContextSensitivityLevel.Safe, ["ev.context.semantic"], [], [], [], [], contradictory: false, memory: false, memoryPromotion: false, duplicateLock: false, human: false, exportDashboard: false, WorkspaceContextSelectionLockExclusionDecision.Blocked, WorkspaceContextSelectionLockExclusionIssueKind.SemanticDerivedWhileDisabled, "Semantic-derived selected context is blocked while semantic/vector is disabled.", proof),
            Fixture("ctx.selected-legacy-no-provenance", WorkspaceContextSelectionState.Selected, WorkspaceContextLockState.Unlocked, WorkspaceContextExclusionState.NotExcluded, WorkspaceContextSourceKind.LegacyWithoutProvenance, WorkspaceContextAuthorityLevel.Informational, WorkspaceContextFreshnessStatus.Missing, WorkspaceContextSensitivityLevel.Unknown, [], [], [], [], [], contradictory: false, memory: false, memoryPromotion: false, duplicateLock: false, human: false, exportDashboard: false, WorkspaceContextSelectionLockExclusionDecision.Blocked, WorkspaceContextSelectionLockExclusionIssueKind.LegacyWithoutProvenance, "Selected legacy context without provenance is blocked.", proof),
            Fixture("ctx.duplicate-conflicting-lock", WorkspaceContextSelectionState.Selected, WorkspaceContextLockState.Conflicting, WorkspaceContextExclusionState.NotExcluded, WorkspaceContextSourceKind.EilReadOnlyEvidence, WorkspaceContextAuthorityLevel.EvidenceLinked, WorkspaceContextFreshnessStatus.Fresh, WorkspaceContextSensitivityLevel.Safe, ["ev.context.duplicate"], [], [], [], [], contradictory: false, memory: false, memoryPromotion: false, duplicateLock: true, human: false, exportDashboard: false, WorkspaceContextSelectionLockExclusionDecision.Blocked, WorkspaceContextSelectionLockExclusionIssueKind.DuplicateConflictingLockState, "Duplicate selected context has conflicting lock states.", proof),
            Fixture("ctx.empty-selected-safe-next-step", WorkspaceContextSelectionState.Empty, WorkspaceContextLockState.Unlocked, WorkspaceContextExclusionState.NotExcluded, WorkspaceContextSourceKind.NoSideEffectProof, WorkspaceContextAuthorityLevel.HumanReviewRequired, WorkspaceContextFreshnessStatus.NotApplicable, WorkspaceContextSensitivityLevel.Safe, [], [], ["safe.step.requires.context"], [], [], contradictory: false, memory: false, memoryPromotion: false, duplicateLock: false, human: false, exportDashboard: false, WorkspaceContextSelectionLockExclusionDecision.Blocked, WorkspaceContextSelectionLockExclusionIssueKind.EmptySelectionWithDependentSafeNextStep, "Safe next step requiring context cannot run with empty selection.", proof),
            Fixture("ctx.locked-review-missing", WorkspaceContextSelectionState.Selected, WorkspaceContextLockState.LockedRequiresHumanReview, WorkspaceContextExclusionState.NotExcluded, WorkspaceContextSourceKind.EilReadOnlyEvidence, WorkspaceContextAuthorityLevel.HumanReviewRequired, WorkspaceContextFreshnessStatus.Fresh, WorkspaceContextSensitivityLevel.Safe, ["ev.context.locked-review"], [], [], [], [], contradictory: false, memory: false, memoryPromotion: false, duplicateLock: false, human: false, exportDashboard: false, WorkspaceContextSelectionLockExclusionDecision.Blocked, WorkspaceContextSelectionLockExclusionIssueKind.LockedMissingHumanReview, "Locked context requires missing human review.", proof),
            Fixture("dashboard.excluded-candidate", WorkspaceContextSelectionState.NotSelected, WorkspaceContextLockState.Unlocked, WorkspaceContextExclusionState.Excluded, WorkspaceContextSourceKind.EilTimelineExportPreview, WorkspaceContextAuthorityLevel.ExcludedBySafety, WorkspaceContextFreshnessStatus.NotApplicable, WorkspaceContextSensitivityLevel.SensitiveNeverUse, [], [], [], [], [], contradictory: false, memory: false, memoryPromotion: false, duplicateLock: false, human: false, exportDashboard: true, WorkspaceContextSelectionLockExclusionDecision.Excluded, WorkspaceContextSelectionLockExclusionIssueKind.ExcludedAppearsInExportDashboardCandidate, "Excluded context cannot appear as export/dashboard candidate.", proof)
        ];
    }

    public static WorkspaceContextSelectionLockExclusionResult Evaluate(WorkspaceContextSelectionLockExclusionFixture fixture)
    {
        var issues = new List<WorkspaceContextSelectionLockExclusionIssue>();

        AddIssues(fixture, issues);

        var decision = Decide(fixture, issues);
        var blockers = issues.Where(issue => issue.BlocksDecision || issue.BlocksSafeNextStep || issue.BlocksMemoryInfluence || issue.BlocksExportDashboardAppearance).Select(issue => issue.Message).ToList();
        var warnings = issues.Where(issue => !issue.BlocksDecision && !issue.BlocksSafeNextStep && !issue.BlocksMemoryInfluence && !issue.BlocksExportDashboardAppearance).Select(issue => issue.Message).ToList();
        var requiresHumanReview = issues.Any(issue => issue.RequiresHumanReview) || fixture.LockState is WorkspaceContextLockState.LockedBySafety or WorkspaceContextLockState.LockedRequiresHumanReview;

        return new WorkspaceContextSelectionLockExclusionResult(
            fixture.FixtureId,
            decision,
            issues,
            warnings,
            blockers,
            AllowsReadOnlySummary: decision is WorkspaceContextSelectionLockExclusionDecision.AllowedReadOnly or WorkspaceContextSelectionLockExclusionDecision.WarningReadOnlyOnly or WorkspaceContextSelectionLockExclusionDecision.NeedsHumanReview,
            AllowsDecisionUse: decision == WorkspaceContextSelectionLockExclusionDecision.AllowedReadOnly && !requiresHumanReview && issues.All(issue => !issue.BlocksDecision),
            AllowsSafeNextStepUse: decision == WorkspaceContextSelectionLockExclusionDecision.AllowedReadOnly && !requiresHumanReview && issues.All(issue => !issue.BlocksSafeNextStep),
            AllowsMemoryInfluence: fixture.MemoryCandidate && decision == WorkspaceContextSelectionLockExclusionDecision.AllowedReadOnly && !requiresHumanReview && issues.All(issue => !issue.BlocksMemoryInfluence),
            AllowsExportDashboardAppearance: decision != WorkspaceContextSelectionLockExclusionDecision.Excluded && issues.All(issue => !issue.BlocksExportDashboardAppearance),
            RequiresHumanReview: requiresHumanReview || decision == WorkspaceContextSelectionLockExclusionDecision.NeedsHumanReview,
            NoSideEffectProof: fixture.NoSideEffectProof);
    }

    public static IReadOnlyList<WorkspaceContextSelectionLockExclusionResult> EvaluateCatalog() =>
        CreateFixtureCatalog().Select(Evaluate).ToList();

    private static void AddIssues(WorkspaceContextSelectionLockExclusionFixture fixture, List<WorkspaceContextSelectionLockExclusionIssue> issues)
    {
        if (fixture.SelectionState == WorkspaceContextSelectionState.Selected && fixture.ExclusionState != WorkspaceContextExclusionState.NotExcluded)
            issues.Add(Issue(WorkspaceContextSelectionLockExclusionIssueKind.SelectedExcluded, "Excluded context wins over selected context.", blockDecision: true, blockSafeNext: true, blockMemory: true, blockExportDashboard: true, human: true));

        if (fixture.ExclusionState != WorkspaceContextExclusionState.NotExcluded && fixture.DependentMemoryRefs.Count > 0)
            issues.Add(Issue(WorkspaceContextSelectionLockExclusionIssueKind.ExcludedReferencedByMemory, "Memory candidate cannot reference excluded context.", blockDecision: true, blockSafeNext: true, blockMemory: true, blockExportDashboard: true, human: true));

        if (fixture.ExclusionState != WorkspaceContextExclusionState.NotExcluded && fixture.DependentSafeNextStepRefs.Count > 0)
            issues.Add(Issue(WorkspaceContextSelectionLockExclusionIssueKind.ExcludedReferencedBySafeNextStep, "Safe next step cannot reference excluded context.", blockDecision: true, blockSafeNext: true, blockMemory: true, blockExportDashboard: true, human: true));

        if (fixture.ExclusionState != WorkspaceContextExclusionState.NotExcluded && fixture.DependentClaimActionPreviewRefs.Count > 0)
            issues.Add(Issue(WorkspaceContextSelectionLockExclusionIssueKind.ExcludedReferencedByClaimActionPreview, "Claim/action previews cannot reference excluded context.", blockDecision: true, blockSafeNext: true, blockMemory: true, blockExportDashboard: true, human: true));

        if (fixture.ExclusionState != WorkspaceContextExclusionState.NotExcluded && fixture.DependentGraphRefs.Count > 0)
            issues.Add(Issue(WorkspaceContextSelectionLockExclusionIssueKind.ExcludedReferencedByGraph, "Graph refs cannot reference excluded context.", blockDecision: true, blockSafeNext: true, blockMemory: true, blockExportDashboard: true, human: true));

        if (fixture.AppearsInExportDashboardCandidateList && fixture.ExclusionState != WorkspaceContextExclusionState.NotExcluded)
            issues.Add(Issue(WorkspaceContextSelectionLockExclusionIssueKind.ExcludedAppearsInExportDashboardCandidate, "Excluded context cannot appear as export/dashboard candidate.", blockDecision: false, blockSafeNext: false, blockMemory: false, blockExportDashboard: true, human: true));

        if (fixture.SelectionState == WorkspaceContextSelectionState.Selected && fixture.LockState == WorkspaceContextLockState.LockedBySafety)
            issues.Add(Issue(WorkspaceContextSelectionLockExclusionIssueKind.SelectedLockedBySafety, "Selected locked context requires human review.", blockDecision: false, blockSafeNext: false, blockMemory: true, blockExportDashboard: false, human: true));

        if (fixture.LockState == WorkspaceContextLockState.LockedRequiresHumanReview && !fixture.HumanReviewed)
            issues.Add(Issue(WorkspaceContextSelectionLockExclusionIssueKind.LockedMissingHumanReview, "Locked context requires missing human review.", blockDecision: true, blockSafeNext: true, blockMemory: true, blockExportDashboard: false, human: true));

        if (fixture.LockState == WorkspaceContextLockState.LockedBySafety && fixture.EvidenceRefs.Count == 0 && fixture.ExclusionState == WorkspaceContextExclusionState.NotExcluded)
            issues.Add(Issue(WorkspaceContextSelectionLockExclusionIssueKind.LockedMissingEvidence, "Locked context requires evidence refs.", blockDecision: true, blockSafeNext: true, blockMemory: true, blockExportDashboard: false, human: true));

        if (fixture.Freshness == WorkspaceContextFreshnessStatus.Stale && fixture.LockState is WorkspaceContextLockState.LockedBySafety or WorkspaceContextLockState.LockedRequiresHumanReview && !fixture.HumanReviewed)
            issues.Add(Issue(WorkspaceContextSelectionLockExclusionIssueKind.LockedStale, "Locked stale context requires human review and cannot feed decisions.", blockDecision: true, blockSafeNext: true, blockMemory: true, blockExportDashboard: false, human: true));
        else if (fixture.SelectionState == WorkspaceContextSelectionState.Selected && fixture.Freshness == WorkspaceContextFreshnessStatus.Stale)
            issues.Add(Issue(WorkspaceContextSelectionLockExclusionIssueKind.SelectedStale, "Selected stale context cannot feed decisions or safe next steps.", blockDecision: true, blockSafeNext: true, blockMemory: true, blockExportDashboard: false, human: true));

        if (fixture.SelectionState == WorkspaceContextSelectionState.Selected && fixture.Sensitivity == WorkspaceContextSensitivityLevel.Unknown)
            issues.Add(Issue(WorkspaceContextSelectionLockExclusionIssueKind.SelectedUnknownAuthority, "Selected unknown authority is blocked.", blockDecision: true, blockSafeNext: true, blockMemory: true, blockExportDashboard: true, human: true));

        if (fixture.SelectionState == WorkspaceContextSelectionState.Selected && fixture.Freshness == WorkspaceContextFreshnessStatus.Missing)
            issues.Add(Issue(WorkspaceContextSelectionLockExclusionIssueKind.SelectedMissingFreshness, "Selected context with missing freshness is blocked.", blockDecision: true, blockSafeNext: true, blockMemory: true, blockExportDashboard: true, human: true));

        if (fixture.SelectionState == WorkspaceContextSelectionState.Selected && fixture.Contradictory)
            issues.Add(Issue(WorkspaceContextSelectionLockExclusionIssueKind.SelectedContradictory, "Selected contradictory context is blocked.", blockDecision: true, blockSafeNext: true, blockMemory: true, blockExportDashboard: true, human: true));

        if (fixture.SelectionState == WorkspaceContextSelectionState.Selected && fixture.Sensitivity is WorkspaceContextSensitivityLevel.RawPayload or WorkspaceContextSensitivityLevel.Sensitive or WorkspaceContextSensitivityLevel.SensitiveNeverUse)
            issues.Add(Issue(WorkspaceContextSelectionLockExclusionIssueKind.UnsafeSelectedContent, "Selected raw or sensitive unsafe context is excluded.", blockDecision: true, blockSafeNext: true, blockMemory: true, blockExportDashboard: true, human: true));

        if (fixture.SelectionState == WorkspaceContextSelectionState.Selected && fixture.SourceKind == WorkspaceContextSourceKind.ProviderCloudDerived)
            issues.Add(Issue(WorkspaceContextSelectionLockExclusionIssueKind.ProviderDerivedWhileDisabled, "Provider-derived selected context is blocked while provider/cloud is disabled.", blockDecision: true, blockSafeNext: true, blockMemory: true, blockExportDashboard: true, human: true));

        if (fixture.SelectionState == WorkspaceContextSelectionState.Selected && fixture.SourceKind == WorkspaceContextSourceKind.SemanticVectorDerived)
            issues.Add(Issue(WorkspaceContextSelectionLockExclusionIssueKind.SemanticDerivedWhileDisabled, "Semantic-derived selected context is blocked while semantic/vector is disabled.", blockDecision: true, blockSafeNext: true, blockMemory: true, blockExportDashboard: true, human: true));

        if (fixture.SelectionState == WorkspaceContextSelectionState.Selected && fixture.SourceKind == WorkspaceContextSourceKind.LegacyWithoutProvenance)
            issues.Add(Issue(WorkspaceContextSelectionLockExclusionIssueKind.LegacyWithoutProvenance, "Selected legacy context without provenance is blocked.", blockDecision: true, blockSafeNext: true, blockMemory: true, blockExportDashboard: true, human: true));

        if (fixture.DuplicateConflictingLockStates || fixture.LockState == WorkspaceContextLockState.Conflicting)
            issues.Add(Issue(WorkspaceContextSelectionLockExclusionIssueKind.DuplicateConflictingLockState, "Duplicate selected context has conflicting lock states.", blockDecision: true, blockSafeNext: true, blockMemory: true, blockExportDashboard: true, human: true));

        if (fixture.SelectionState == WorkspaceContextSelectionState.Empty && fixture.DependentSafeNextStepRefs.Count > 0)
            issues.Add(Issue(WorkspaceContextSelectionLockExclusionIssueKind.EmptySelectionWithDependentSafeNextStep, "Safe next step requiring context cannot run with empty selection.", blockDecision: true, blockSafeNext: true, blockMemory: false, blockExportDashboard: false, human: true));

        if (fixture.MemoryPromotionAttempted && fixture.LockState != WorkspaceContextLockState.Unlocked)
            issues.Add(Issue(WorkspaceContextSelectionLockExclusionIssueKind.LockedMemoryPromotion, "Memory candidate cannot promote locked context.", blockDecision: true, blockSafeNext: true, blockMemory: true, blockExportDashboard: true, human: true));
    }

    private static WorkspaceContextSelectionLockExclusionDecision Decide(
        WorkspaceContextSelectionLockExclusionFixture fixture,
        IReadOnlyList<WorkspaceContextSelectionLockExclusionIssue> issues)
    {
        if (fixture.ExclusionState != WorkspaceContextExclusionState.NotExcluded || issues.Any(issue => issue.IssueKind is WorkspaceContextSelectionLockExclusionIssueKind.SelectedExcluded or WorkspaceContextSelectionLockExclusionIssueKind.UnsafeSelectedContent))
            return WorkspaceContextSelectionLockExclusionDecision.Excluded;

        if (issues.Any(issue => issue.BlocksDecision || issue.BlocksSafeNextStep))
            return WorkspaceContextSelectionLockExclusionDecision.Blocked;

        if (issues.Any(issue => issue.RequiresHumanReview))
            return WorkspaceContextSelectionLockExclusionDecision.NeedsHumanReview;

        if (issues.Count > 0)
            return WorkspaceContextSelectionLockExclusionDecision.WarningReadOnlyOnly;

        return WorkspaceContextSelectionLockExclusionDecision.AllowedReadOnly;
    }

    private static WorkspaceContextSelectionLockExclusionIssue Issue(
        WorkspaceContextSelectionLockExclusionIssueKind issueKind,
        string message,
        bool blockDecision,
        bool blockSafeNext,
        bool blockMemory,
        bool blockExportDashboard,
        bool human) =>
        new(issueKind, message, blockDecision, blockSafeNext, blockMemory, blockExportDashboard, human);

    private static WorkspaceContextSelectionLockExclusionFixture Fixture(
        string fixtureId,
        WorkspaceContextSelectionState selection,
        WorkspaceContextLockState locked,
        WorkspaceContextExclusionState exclusion,
        WorkspaceContextSourceKind sourceKind,
        WorkspaceContextAuthorityLevel authority,
        WorkspaceContextFreshnessStatus freshness,
        WorkspaceContextSensitivityLevel sensitivity,
        IReadOnlyList<string> evidenceRefs,
        IReadOnlyList<string> memoryRefs,
        IReadOnlyList<string> safeNextRefs,
        IReadOnlyList<string> claimActionRefs,
        IReadOnlyList<string> graphRefs,
        bool contradictory,
        bool memory,
        bool memoryPromotion,
        bool duplicateLock,
        bool human,
        bool exportDashboard,
        WorkspaceContextSelectionLockExclusionDecision expectedDecision,
        WorkspaceContextSelectionLockExclusionIssueKind expectedIssue,
        string expectedMessage,
        WorkspaceContextNoSideEffectProof proof) =>
        new(fixtureId, selection, locked, exclusion, sourceKind, authority, freshness, sensitivity, evidenceRefs, memoryRefs, safeNextRefs, claimActionRefs, graphRefs, contradictory, memory, memoryPromotion, duplicateLock, human, exportDashboard, expectedDecision, expectedIssue, expectedMessage, proof);
}

public static class WorkspaceContextReadOnlyPresenter
{
    public static WorkspaceContextPacketReadOnly CreateFixture()
    {
        var auditDashboard = EvidenceIntelligenceAuditDashboardReadOnlyPresenter.CreateFixture();
        var proof = WorkspaceContextNoSideEffectProof.FixtureReadOnly();
        var evidenceRefs = auditDashboard.TimelineExport.Manifest.IncludedEvidenceRefs.Take(8).ToList();
        var warnings = Warnings();
        var blockers = Blockers();
        var debt = DocumentedDebt();
        var sources = Sources(evidenceRefs, proof);
        var items = Items(evidenceRefs, warnings, blockers, proof);
        var memoryCandidates = MemoryCandidates(evidenceRefs, proof);
        var selected = items.Where(item => item.Selected).ToList();
        var locked = items.Where(item => item.Locked).ToList();
        var excluded = items.Where(item => item.Excluded).ToList();
        var summary = Summary(items, memoryCandidates);

        return new WorkspaceContextPacketReadOnly(
            PacketId: "phase-d.workspace-context.packet.read-only.fixture.v1",
            WorkspaceId: "workspace.fixture.nodal-os.phase-d",
            Title: "Workspace Context and Memory Read-Only Foundation",
            Mode: "READ_ONLY_FIXTURE_SAFE_NO_MEMORY_RUNTIME",
            SourceLabel: "EvidenceIntelligenceAuditDashboardReadOnlyPresenter.CreateFixture",
            Summary: "Deterministic fixture context packet for Phase D foundation; no workspace scan, durable memory, provider, vector backend or runtime is enabled.",
            Sources: sources,
            Items: items,
            MemoryCandidates: memoryCandidates,
            SelectedContext: selected,
            LockedContext: locked,
            ExcludedContext: excluded,
            Warnings: warnings,
            Blockers: blockers,
            DocumentedDebt: debt,
            ProviderCloudNotice: "Provider/cloud calls are disabled for context and memory foundation.",
            SemanticVectorNotice: "Semantic/vector backend is disabled; memory candidates are lexical/read-only previews only.",
            SafeNextStep: "Harden context authority and freshness guards before expanding memory candidates or mounting a UI surface.",
            ReadOnlySummary: summary,
            NoSideEffectProof: proof);
    }

    private static IReadOnlyList<WorkspaceContextSourceDescriptor> Sources(
        IReadOnlyList<string> evidenceRefs,
        WorkspaceContextNoSideEffectProof proof) =>
    [
        new(
            "source.workspace-identity.fixture",
            "Workspace identity fixture",
            WorkspaceContextSourceKind.Fixture,
            WorkspaceContextAuthorityLevel.Informational,
            WorkspaceContextFreshnessStatus.FixtureCurrent,
            FixtureOnly: true,
            ReadsWorkspaceFilesystem: false,
            UsesProviderCloud: false,
            UsesVectorSemanticBackend: false,
            EvidenceRefs: [],
            Warnings: [],
            Blockers: [],
            NoSideEffectProof: proof),
        new(
            "source.eil.audit-dashboard.fixture",
            "EIL read-only audit dashboard fixture",
            WorkspaceContextSourceKind.EilReadOnlyEvidence,
            WorkspaceContextAuthorityLevel.EvidenceLinked,
            WorkspaceContextFreshnessStatus.FixtureCurrent,
            FixtureOnly: true,
            ReadsWorkspaceFilesystem: false,
            UsesProviderCloud: false,
            UsesVectorSemanticBackend: false,
            EvidenceRefs: evidenceRefs,
            Warnings: ["EIL is referenced as read-only evidence only."],
            Blockers: [],
            NoSideEffectProof: proof),
        new(
            "source.capability-notices.fixture",
            "Capability notices fixture",
            WorkspaceContextSourceKind.CapabilityNotice,
            WorkspaceContextAuthorityLevel.LockedBySafety,
            WorkspaceContextFreshnessStatus.NotApplicable,
            FixtureOnly: true,
            ReadsWorkspaceFilesystem: false,
            UsesProviderCloud: false,
            UsesVectorSemanticBackend: false,
            EvidenceRefs: [],
            Warnings: [],
            Blockers: ["Filesystem scan, durable memory, provider/cloud and vector backend remain disabled."],
            NoSideEffectProof: proof)
    ];

    private static IReadOnlyList<WorkspaceContextItem> Items(
        IReadOnlyList<string> evidenceRefs,
        IReadOnlyList<string> warnings,
        IReadOnlyList<string> blockers,
        WorkspaceContextNoSideEffectProof proof) =>
    [
        Item("workspace-identity", WorkspaceContextItemKind.WorkspaceIdentity, WorkspaceContextItemStatus.Ready, "Workspace identity fixture", "Synthetic workspace identity for Phase D foundation.", WorkspaceContextSourceKind.Fixture, WorkspaceContextAuthorityLevel.Informational, WorkspaceContextFreshnessStatus.FixtureCurrent, [], [], [], selected: true, locked: false, excluded: false, proof),
        Item("workspace-boundary", WorkspaceContextItemKind.WorkspaceBoundary, WorkspaceContextItemStatus.Locked, "Workspace boundary descriptor", "Boundary is declared from fixture metadata; no workspace filesystem is scanned.", WorkspaceContextSourceKind.Fixture, WorkspaceContextAuthorityLevel.LockedBySafety, WorkspaceContextFreshnessStatus.FixtureCurrent, [], [], ["Real workspace scan is disabled."], selected: true, locked: true, excluded: false, proof),
        Item("context-packet-summary", WorkspaceContextItemKind.ContextPacketSummary, WorkspaceContextItemStatus.Ready, "Context packet summary", "Read-only packet combines fixture context, EIL evidence refs and disabled capability notices.", WorkspaceContextSourceKind.Fixture, WorkspaceContextAuthorityLevel.EvidenceLinked, WorkspaceContextFreshnessStatus.FixtureCurrent, evidenceRefs, [], [], selected: true, locked: false, excluded: false, proof),
        Item("selected-context-list", WorkspaceContextItemKind.SelectedContext, WorkspaceContextItemStatus.Selected, "Selected context list", "Selected context contains only fixture-safe identity, boundary and EIL evidence references.", WorkspaceContextSourceKind.HumanProvidedFixture, WorkspaceContextAuthorityLevel.EvidenceLinked, WorkspaceContextFreshnessStatus.FixtureCurrent, evidenceRefs, [], [], selected: true, locked: false, excluded: false, proof),
        Item("locked-context-list", WorkspaceContextItemKind.LockedContext, WorkspaceContextItemStatus.Locked, "Locked context list", "Runtime, provider/cloud, vector backend and filesystem scanning are locked out.", WorkspaceContextSourceKind.CapabilityNotice, WorkspaceContextAuthorityLevel.LockedBySafety, WorkspaceContextFreshnessStatus.NotApplicable, [], [], blockers, selected: false, locked: true, excluded: false, proof),
        Item("excluded-context-list", WorkspaceContextItemKind.ExcludedContext, WorkspaceContextItemStatus.Excluded, "Excluded context list", "Raw workspace files, secrets, provider output and durable memory records are excluded.", WorkspaceContextSourceKind.CapabilityNotice, WorkspaceContextAuthorityLevel.ExcludedBySafety, WorkspaceContextFreshnessStatus.NotApplicable, [], warnings, ["Sensitive/raw context remains excluded."], selected: false, locked: false, excluded: true, proof),
        Item("evidence-linked-context-refs", WorkspaceContextItemKind.EvidenceLinkedContextReference, WorkspaceContextItemStatus.Ready, "Evidence-linked context refs", "EIL refs are included as labels only; no EIL persistence store is read.", WorkspaceContextSourceKind.EilReadOnlyEvidence, WorkspaceContextAuthorityLevel.EvidenceLinked, WorkspaceContextFreshnessStatus.FixtureCurrent, evidenceRefs, [], [], selected: true, locked: false, excluded: false, proof),
        Item("authority-levels", WorkspaceContextItemKind.AuthorityPolicy, WorkspaceContextItemStatus.Ready, "Authority levels", "Informational, evidence-linked, human-review, locked and excluded authority levels are modeled.", WorkspaceContextSourceKind.Fixture, WorkspaceContextAuthorityLevel.LockedBySafety, WorkspaceContextFreshnessStatus.NotApplicable, [], [], [], selected: true, locked: true, excluded: false, proof),
        Item("freshness-staleness", WorkspaceContextItemKind.FreshnessSignal, WorkspaceContextItemStatus.Warning, "Freshness/staleness status", "Fixture context is current for tests, while real workspace freshness remains unavailable.", WorkspaceContextSourceKind.Fixture, WorkspaceContextAuthorityLevel.HumanReviewRequired, WorkspaceContextFreshnessStatus.Stale, [], ["Real workspace freshness is not evaluated."], [], selected: false, locked: false, excluded: false, proof),
        Item("missing-context-warning", WorkspaceContextItemKind.MissingContextWarning, WorkspaceContextItemStatus.Warning, "Missing context warnings", "Installed-extension QA and real workspace inventory remain out of scope.", WorkspaceContextSourceKind.DocumentedDebt, WorkspaceContextAuthorityLevel.HumanReviewRequired, WorkspaceContextFreshnessStatus.Missing, [], ["Manual context confirmation remains required."], [], selected: false, locked: false, excluded: false, proof),
        Item("stale-context-warning", WorkspaceContextItemKind.StaleContextWarning, WorkspaceContextItemStatus.Warning, "Stale context warnings", "Stale signals block any future memory authority escalation.", WorkspaceContextSourceKind.DocumentedDebt, WorkspaceContextAuthorityLevel.HumanReviewRequired, WorkspaceContextFreshnessStatus.Stale, [], ["Stale context cannot authorize actions."], [], selected: false, locked: false, excluded: false, proof),
        Item("sensitive-context-blocker", WorkspaceContextItemKind.SensitiveUnsafeContextBlocker, WorkspaceContextItemStatus.Blocked, "Sensitive/unsafe context blockers", "Secrets, raw payloads and unknown-sensitivity context remain blocked.", WorkspaceContextSourceKind.CapabilityNotice, WorkspaceContextAuthorityLevel.ExcludedBySafety, WorkspaceContextFreshnessStatus.NotApplicable, [], [], ["Sensitive context must be redacted and approved before any future use."], selected: false, locked: true, excluded: true, proof),
        Item("provider-cloud-disabled", WorkspaceContextItemKind.ProviderCloudDisabledNotice, WorkspaceContextItemStatus.Disabled, "Provider/cloud disabled notice", "No provider/cloud/network calls are enabled.", WorkspaceContextSourceKind.CapabilityNotice, WorkspaceContextAuthorityLevel.LockedBySafety, WorkspaceContextFreshnessStatus.NotApplicable, [], [], ["Provider/cloud disabled."], selected: false, locked: true, excluded: false, proof),
        Item("semantic-vector-disabled", WorkspaceContextItemKind.SemanticVectorDisabledNotice, WorkspaceContextItemStatus.Disabled, "Semantic/vector disabled notice", "No embeddings, vector store or semantic backend are enabled.", WorkspaceContextSourceKind.CapabilityNotice, WorkspaceContextAuthorityLevel.LockedBySafety, WorkspaceContextFreshnessStatus.NotApplicable, [], [], ["Semantic/vector backend disabled."], selected: false, locked: true, excluded: false, proof),
        Item("safe-next-step", WorkspaceContextItemKind.SafeNextStep, WorkspaceContextItemStatus.Ready, "Safe next step", "Harden authority/freshness guards before expanding candidates.", WorkspaceContextSourceKind.NoSideEffectProof, WorkspaceContextAuthorityLevel.HumanReviewRequired, WorkspaceContextFreshnessStatus.NotApplicable, [], [], [], selected: true, locked: false, excluded: false, proof),
        Item("no-side-effect-proof", WorkspaceContextItemKind.NoSideEffectProof, WorkspaceContextItemStatus.Ready, "No-side-effect proof", "Proof asserts no filesystem, DB, provider, vector, memory, persistence or runtime side effects.", WorkspaceContextSourceKind.NoSideEffectProof, WorkspaceContextAuthorityLevel.LockedBySafety, WorkspaceContextFreshnessStatus.NotApplicable, [], [], [], selected: true, locked: true, excluded: false, proof),
        Item("deferred-capabilities", WorkspaceContextItemKind.DeferredCapability, WorkspaceContextItemStatus.Deferred, "Deferred capabilities / debt", "Durable memory, real workspace scan, provider/cloud and semantic/vector backend remain future explicit milestones.", WorkspaceContextSourceKind.DocumentedDebt, WorkspaceContextAuthorityLevel.HumanReviewRequired, WorkspaceContextFreshnessStatus.NotApplicable, [], warnings, [], selected: false, locked: false, excluded: false, proof)
    ];

    private static IReadOnlyList<WorkspaceContextMemoryCandidate> MemoryCandidates(
        IReadOnlyList<string> evidenceRefs,
        WorkspaceContextNoSideEffectProof proof) =>
    [
        Candidate("memory.contradiction-preview", WorkspaceContextItemKind.ContradictionMemoryPreview, "Contradiction memory preview", "Contradiction patterns are previewed from EIL fixture refs only.", WorkspaceContextAuthorityLevel.EvidenceLinked, WorkspaceContextFreshnessStatus.FixtureCurrent, evidenceRefs.Take(2).ToList(), [], [], selected: true, locked: false, excluded: false, proof),
        Candidate("memory.risk-preview", WorkspaceContextItemKind.RiskMemoryPreview, "Risk memory preview", "Risk notes remain non-durable and require human review.", WorkspaceContextAuthorityLevel.HumanReviewRequired, WorkspaceContextFreshnessStatus.FixtureCurrent, evidenceRefs.Take(2).ToList(), ["Risk memory is preview-only."], [], selected: true, locked: false, excluded: false, proof),
        Candidate("memory.decision-preview", WorkspaceContextItemKind.DecisionMemoryPreview, "Decision memory preview", "Decision context references Fase C NO-GO runtime/release gates.", WorkspaceContextAuthorityLevel.EvidenceLinked, WorkspaceContextFreshnessStatus.FixtureCurrent, evidenceRefs.Take(1).ToList(), [], [], selected: true, locked: false, excluded: false, proof),
        Candidate("memory.claim-preview", WorkspaceContextItemKind.ClaimMemoryPreview, "Claim memory preview", "Claim memory is a deterministic fixture label, not semantic memory.", WorkspaceContextAuthorityLevel.EvidenceLinked, WorkspaceContextFreshnessStatus.FixtureCurrent, evidenceRefs.Take(2).ToList(), [], [], selected: true, locked: false, excluded: false, proof),
        Candidate("memory.action-preview", WorkspaceContextItemKind.ActionMemoryPreview, "Action memory preview", "Action memory preserves no-runtime boundaries and cannot dispatch work.", WorkspaceContextAuthorityLevel.LockedBySafety, WorkspaceContextFreshnessStatus.FixtureCurrent, evidenceRefs.Take(2).ToList(), [], ["Runtime action dispatch is blocked."], selected: false, locked: true, excluded: false, proof)
    ];

    private static WorkspaceContextItem Item(
        string itemId,
        WorkspaceContextItemKind kind,
        WorkspaceContextItemStatus status,
        string title,
        string summary,
        WorkspaceContextSourceKind source,
        WorkspaceContextAuthorityLevel authority,
        WorkspaceContextFreshnessStatus freshness,
        IReadOnlyList<string> evidenceRefs,
        IReadOnlyList<string> warnings,
        IReadOnlyList<string> blockers,
        bool selected,
        bool locked,
        bool excluded,
        WorkspaceContextNoSideEffectProof proof) =>
        new(itemId, kind, status, title, Sanitize(summary), source, authority, freshness, evidenceRefs, Sanitize(warnings), Sanitize(blockers), selected, locked, excluded, proof);

    private static WorkspaceContextMemoryCandidate Candidate(
        string candidateId,
        WorkspaceContextItemKind kind,
        string title,
        string preview,
        WorkspaceContextAuthorityLevel authority,
        WorkspaceContextFreshnessStatus freshness,
        IReadOnlyList<string> evidenceRefs,
        IReadOnlyList<string> warnings,
        IReadOnlyList<string> blockers,
        bool selected,
        bool locked,
        bool excluded,
        WorkspaceContextNoSideEffectProof proof) =>
        new(candidateId, kind, WorkspaceContextItemStatus.Ready, title, Sanitize(preview), authority, freshness, evidenceRefs, Sanitize(warnings), Sanitize(blockers), DurableMemoryEnabled: false, selected, locked, excluded, proof);

    private static IReadOnlyList<string> Warnings() =>
    [
        "Workspace context is fixture-only and does not read the real workspace.",
        "Memory candidates are non-durable previews.",
        "Semantic/vector and provider/cloud capabilities remain disabled."
    ];

    private static IReadOnlyList<string> Blockers() =>
    [
        "Real workspace filesystem reads are blocked.",
        "Durable memory and persistence are blocked.",
        "Provider/cloud/network and LLM live calls are blocked.",
        "Semantic/vector backend is blocked.",
        "Runtime/live/browser/CDP/WCU/OCR remain blocked."
    ];

    private static IReadOnlyList<string> DocumentedDebt() =>
    [
        "Context authority/freshness guards.",
        "Memory candidate contradiction/risk hardening.",
        "Workspace context packet visible surface.",
        "Manual QA before any real workspace scan or durable memory."
    ];

    private static string Summary(
        IReadOnlyList<WorkspaceContextItem> items,
        IReadOnlyList<WorkspaceContextMemoryCandidate> candidates) =>
        string.Join(
            "\n",
            new[]
            {
                "# Workspace Context and Memory Read-Only Foundation",
                "Mode: READ_ONLY_FIXTURE_SAFE_NO_MEMORY_RUNTIME",
                $"Context items: {items.Count}.",
                $"Memory candidates: {candidates.Count}.",
                $"Selected context: {items.Count(item => item.Selected)}.",
                $"Locked context: {items.Count(item => item.Locked)}.",
                $"Excluded context: {items.Count(item => item.Excluded)}.",
                "No workspace filesystem read, durable memory, provider/cloud, semantic/vector backend or runtime is enabled."
            });

    private static IReadOnlyList<string> Sanitize(IReadOnlyList<string> lines) =>
        lines.Select(Sanitize).ToList();

    private static string Sanitize(string value) =>
        value
            .Replace("\r", " ", StringComparison.Ordinal)
            .Replace("\n", " ", StringComparison.Ordinal)
            .Replace("raw payload", "excluded payload", StringComparison.OrdinalIgnoreCase)
            .Replace("secret", "sensitive value", StringComparison.OrdinalIgnoreCase);
}
