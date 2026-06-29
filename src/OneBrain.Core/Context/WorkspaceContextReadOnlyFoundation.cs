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

public enum WorkspaceMemoryCandidateRiskSeverity
{
    None,
    Low,
    Medium,
    Critical,
    Missing
}

public enum WorkspaceMemoryCandidateContradictionStatus
{
    None,
    EvidenceLinked,
    Unresolved,
    ContradictoryEvidence,
    MissingEvidence
}

public enum WorkspaceMemoryCandidateConfidenceStatus
{
    Present,
    Missing,
    Unknown
}

public enum WorkspaceMemoryCandidateInfluenceDecision
{
    AllowedReadOnlyWarning,
    WarningReadOnlyOnly,
    NeedsHumanReview,
    Blocked,
    Excluded
}

public enum WorkspaceMemoryCandidateContradictionRiskIssueKind
{
    None,
    CandidateWithoutEvidence,
    CandidateUsesStaleContext,
    CandidateUsesExcludedContext,
    CandidateUsesLockedUnsafeContext,
    ContradictionRequiresHumanReview,
    RiskMissingSeverity,
    RiskCannotBecomeDecisionMemory,
    DecisionMissingHumanReview,
    DecisionWithContradictoryEvidence,
    ClaimMissingConfidence,
    ClaimStaleEvidence,
    ActionMissingRequiredHumanAction,
    ActionReferencesExcludedContext,
    SafeNextStepReliesOnCriticalRisk,
    SafeNextStepReliesOnUnresolvedContradiction,
    ProviderDerivedWhileDisabled,
    SemanticDerivedWhileDisabled,
    LegacyWithoutProvenance,
    FixtureOnlyNotDurableTrusted,
    DuplicateConflictingCandidates,
    RawSensitivePayload,
    UnknownAuthority,
    MissingFreshness
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

public sealed record WorkspaceMemoryCandidateContradictionRiskFixture(
    string FixtureId,
    WorkspaceContextItemKind CandidateKind,
    WorkspaceContextSourceKind SourceKind,
    WorkspaceContextAuthorityLevel Authority,
    WorkspaceContextFreshnessStatus Freshness,
    WorkspaceContextSensitivityLevel Sensitivity,
    WorkspaceContextSelectionState SelectionState,
    WorkspaceContextLockState LockState,
    WorkspaceContextExclusionState ExclusionState,
    WorkspaceMemoryCandidateRiskSeverity RiskSeverity,
    WorkspaceMemoryCandidateContradictionStatus ContradictionStatus,
    WorkspaceMemoryCandidateConfidenceStatus ConfidenceStatus,
    IReadOnlyList<string> EvidenceRefs,
    bool HumanReviewed,
    bool RequiredHumanActionPresent,
    bool AttemptsDecisionMemory,
    bool SafeNextStepCandidate,
    bool FixtureOnly,
    bool DuplicateConflictingCandidate,
    WorkspaceMemoryCandidateInfluenceDecision ExpectedDecision,
    WorkspaceMemoryCandidateContradictionRiskIssueKind ExpectedIssue,
    string ExpectedMessage,
    WorkspaceContextNoSideEffectProof NoSideEffectProof);

public sealed record WorkspaceMemoryCandidateContradictionRiskIssue(
    WorkspaceMemoryCandidateContradictionRiskIssueKind IssueKind,
    string Message,
    bool BlocksDecision,
    bool BlocksSafeNextStep,
    bool BlocksCandidateInfluence,
    bool BlocksDashboardExport,
    bool RequiresHumanReview);

public sealed record WorkspaceMemoryCandidateContradictionRiskResult(
    string FixtureId,
    WorkspaceMemoryCandidateInfluenceDecision Decision,
    IReadOnlyList<WorkspaceMemoryCandidateContradictionRiskIssue> Issues,
    IReadOnlyList<string> Warnings,
    IReadOnlyList<string> Blockers,
    bool AllowsReadOnlyPreview,
    bool AllowsDecisionUse,
    bool AllowsSafeNextStepUse,
    bool AllowsCandidateInfluence,
    bool AllowsDashboardExportAppearance,
    bool DurableMemoryEnabled,
    bool RequiresHumanReview,
    WorkspaceContextNoSideEffectProof NoSideEffectProof)
{
    public bool Blocked => Decision is WorkspaceMemoryCandidateInfluenceDecision.Blocked or WorkspaceMemoryCandidateInfluenceDecision.Excluded;
    public bool HasIssue(WorkspaceMemoryCandidateContradictionRiskIssueKind issueKind) => Issues.Any(issue => issue.IssueKind == issueKind);
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

public enum WorkspaceContextPacketSurfaceSeverity
{
    Info,
    Warning,
    Blocked,
    Disabled,
    Deferred
}

public sealed record WorkspaceContextPacketSurfaceSection(
    string SectionId,
    string Title,
    WorkspaceContextItemStatus Status,
    WorkspaceContextPacketSurfaceSeverity Severity,
    WorkspaceContextSourceKind Source,
    IReadOnlyList<string> EvidenceRefs,
    IReadOnlyList<string> Warnings,
    IReadOnlyList<string> Blockers,
    int ProductActionsCount,
    int ExportActionsCount,
    WorkspaceContextNoSideEffectProof NoSideEffectProof);

public sealed record WorkspaceContextPacketSurfaceReadOnly(
    string SurfaceId,
    string Title,
    string Mode,
    string SourceLabel,
    WorkspaceContextPacketReadOnly Packet,
    IReadOnlyList<WorkspaceContextPacketSurfaceSection> Sections,
    IReadOnlyList<string> GuardSummaries,
    IReadOnlyList<string> CandidateSummaries,
    IReadOnlyList<string> HumanReviewRequirements,
    IReadOnlyList<string> DisabledNotices,
    IReadOnlyList<string> Warnings,
    IReadOnlyList<string> Blockers,
    string NextRecommendedBlock,
    string ReadOnlySummary,
    WorkspaceContextNoSideEffectProof NoSideEffectProof)
{
    public bool ReadOnly => NoSideEffectProof.ReadOnly;
    public bool Deterministic => NoSideEffectProof.Deterministic;
    public bool FixtureSafe => NoSideEffectProof.FixtureSafe;
    public int ProductActionsCount => Sections.Sum(section => section.ProductActionsCount);
    public int ExportActionsCount => Sections.Sum(section => section.ExportActionsCount);
    public bool HasDurableMemory => Packet.HasDurableMemory || NoSideEffectProof.DurableMemoryActive;
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

public static class WorkspaceMemoryCandidateContradictionRiskGuard
{
    public static IReadOnlyList<WorkspaceMemoryCandidateContradictionRiskFixture> CreateFixtureCatalog()
    {
        var proof = WorkspaceContextNoSideEffectProof.FixtureReadOnly();

        return
        [
            Fixture("memory.contradiction.evidence-linked", WorkspaceContextItemKind.ContradictionMemoryPreview, WorkspaceContextSourceKind.EilReadOnlyEvidence, WorkspaceContextAuthorityLevel.EvidenceLinked, WorkspaceContextFreshnessStatus.FixtureCurrent, WorkspaceContextSensitivityLevel.Safe, WorkspaceContextSelectionState.Selected, WorkspaceContextLockState.Unlocked, WorkspaceContextExclusionState.NotExcluded, WorkspaceMemoryCandidateRiskSeverity.None, WorkspaceMemoryCandidateContradictionStatus.EvidenceLinked, WorkspaceMemoryCandidateConfidenceStatus.Present, ["ev.contradiction.current"], human: false, humanAction: true, decision: false, safeNext: false, fixtureOnly: false, duplicate: false, WorkspaceMemoryCandidateInfluenceDecision.NeedsHumanReview, WorkspaceMemoryCandidateContradictionRiskIssueKind.ContradictionRequiresHumanReview, "Evidence-linked contradiction remains read-only and requires human review.", proof),
            Fixture("memory.contradiction.no-evidence", WorkspaceContextItemKind.ContradictionMemoryPreview, WorkspaceContextSourceKind.Fixture, WorkspaceContextAuthorityLevel.EvidenceLinked, WorkspaceContextFreshnessStatus.Fresh, WorkspaceContextSensitivityLevel.Safe, WorkspaceContextSelectionState.Selected, WorkspaceContextLockState.Unlocked, WorkspaceContextExclusionState.NotExcluded, WorkspaceMemoryCandidateRiskSeverity.None, WorkspaceMemoryCandidateContradictionStatus.MissingEvidence, WorkspaceMemoryCandidateConfidenceStatus.Present, [], human: false, humanAction: true, decision: false, safeNext: false, fixtureOnly: false, duplicate: false, WorkspaceMemoryCandidateInfluenceDecision.Blocked, WorkspaceMemoryCandidateContradictionRiskIssueKind.CandidateWithoutEvidence, "Memory candidate without evidence is blocked.", proof),
            Fixture("memory.contradiction.stale-context", WorkspaceContextItemKind.ContradictionMemoryPreview, WorkspaceContextSourceKind.EilReadOnlyEvidence, WorkspaceContextAuthorityLevel.EvidenceLinked, WorkspaceContextFreshnessStatus.Stale, WorkspaceContextSensitivityLevel.Safe, WorkspaceContextSelectionState.Selected, WorkspaceContextLockState.Unlocked, WorkspaceContextExclusionState.NotExcluded, WorkspaceMemoryCandidateRiskSeverity.None, WorkspaceMemoryCandidateContradictionStatus.EvidenceLinked, WorkspaceMemoryCandidateConfidenceStatus.Present, ["ev.contradiction.stale"], human: false, humanAction: true, decision: false, safeNext: false, fixtureOnly: false, duplicate: false, WorkspaceMemoryCandidateInfluenceDecision.Blocked, WorkspaceMemoryCandidateContradictionRiskIssueKind.CandidateUsesStaleContext, "Memory candidate using stale context is blocked.", proof),
            Fixture("memory.contradiction.excluded-context", WorkspaceContextItemKind.ContradictionMemoryPreview, WorkspaceContextSourceKind.CapabilityNotice, WorkspaceContextAuthorityLevel.ExcludedBySafety, WorkspaceContextFreshnessStatus.NotApplicable, WorkspaceContextSensitivityLevel.SensitiveNeverUse, WorkspaceContextSelectionState.NotSelected, WorkspaceContextLockState.Unlocked, WorkspaceContextExclusionState.Excluded, WorkspaceMemoryCandidateRiskSeverity.None, WorkspaceMemoryCandidateContradictionStatus.EvidenceLinked, WorkspaceMemoryCandidateConfidenceStatus.Present, ["ev.contradiction.excluded"], human: false, humanAction: true, decision: false, safeNext: false, fixtureOnly: false, duplicate: false, WorkspaceMemoryCandidateInfluenceDecision.Excluded, WorkspaceMemoryCandidateContradictionRiskIssueKind.CandidateUsesExcludedContext, "Memory candidate using excluded context is excluded.", proof),
            Fixture("memory.contradiction.locked-unsafe", WorkspaceContextItemKind.ContradictionMemoryPreview, WorkspaceContextSourceKind.EilReadOnlyEvidence, WorkspaceContextAuthorityLevel.LockedBySafety, WorkspaceContextFreshnessStatus.Fresh, WorkspaceContextSensitivityLevel.Safe, WorkspaceContextSelectionState.Selected, WorkspaceContextLockState.LockedRequiresHumanReview, WorkspaceContextExclusionState.NotExcluded, WorkspaceMemoryCandidateRiskSeverity.None, WorkspaceMemoryCandidateContradictionStatus.EvidenceLinked, WorkspaceMemoryCandidateConfidenceStatus.Present, ["ev.contradiction.locked"], human: false, humanAction: true, decision: false, safeNext: false, fixtureOnly: false, duplicate: false, WorkspaceMemoryCandidateInfluenceDecision.Blocked, WorkspaceMemoryCandidateContradictionRiskIssueKind.CandidateUsesLockedUnsafeContext, "Memory candidate using locked unsafe context is blocked.", proof),
            Fixture("memory.risk.evidence-fresh", WorkspaceContextItemKind.RiskMemoryPreview, WorkspaceContextSourceKind.EilReadOnlyEvidence, WorkspaceContextAuthorityLevel.EvidenceLinked, WorkspaceContextFreshnessStatus.FixtureCurrent, WorkspaceContextSensitivityLevel.Safe, WorkspaceContextSelectionState.Selected, WorkspaceContextLockState.Unlocked, WorkspaceContextExclusionState.NotExcluded, WorkspaceMemoryCandidateRiskSeverity.Medium, WorkspaceMemoryCandidateContradictionStatus.None, WorkspaceMemoryCandidateConfidenceStatus.Present, ["ev.risk.current"], human: false, humanAction: true, decision: false, safeNext: false, fixtureOnly: false, duplicate: false, WorkspaceMemoryCandidateInfluenceDecision.AllowedReadOnlyWarning, WorkspaceMemoryCandidateContradictionRiskIssueKind.None, "Risk candidate with evidence is read-only warning only.", proof),
            Fixture("memory.risk.missing-severity", WorkspaceContextItemKind.RiskMemoryPreview, WorkspaceContextSourceKind.EilReadOnlyEvidence, WorkspaceContextAuthorityLevel.EvidenceLinked, WorkspaceContextFreshnessStatus.Fresh, WorkspaceContextSensitivityLevel.Safe, WorkspaceContextSelectionState.Selected, WorkspaceContextLockState.Unlocked, WorkspaceContextExclusionState.NotExcluded, WorkspaceMemoryCandidateRiskSeverity.Missing, WorkspaceMemoryCandidateContradictionStatus.None, WorkspaceMemoryCandidateConfidenceStatus.Present, ["ev.risk.missing-severity"], human: false, humanAction: true, decision: false, safeNext: false, fixtureOnly: false, duplicate: false, WorkspaceMemoryCandidateInfluenceDecision.Blocked, WorkspaceMemoryCandidateContradictionRiskIssueKind.RiskMissingSeverity, "Risk candidate without severity is blocked.", proof),
            Fixture("memory.risk.promotes-decision", WorkspaceContextItemKind.RiskMemoryPreview, WorkspaceContextSourceKind.EilReadOnlyEvidence, WorkspaceContextAuthorityLevel.EvidenceLinked, WorkspaceContextFreshnessStatus.Fresh, WorkspaceContextSensitivityLevel.Safe, WorkspaceContextSelectionState.Selected, WorkspaceContextLockState.Unlocked, WorkspaceContextExclusionState.NotExcluded, WorkspaceMemoryCandidateRiskSeverity.Medium, WorkspaceMemoryCandidateContradictionStatus.None, WorkspaceMemoryCandidateConfidenceStatus.Present, ["ev.risk.decision"], human: false, humanAction: true, decision: true, safeNext: false, fixtureOnly: false, duplicate: false, WorkspaceMemoryCandidateInfluenceDecision.Blocked, WorkspaceMemoryCandidateContradictionRiskIssueKind.RiskCannotBecomeDecisionMemory, "Risk candidate cannot become decision memory.", proof),
            Fixture("memory.decision.no-human-review", WorkspaceContextItemKind.DecisionMemoryPreview, WorkspaceContextSourceKind.EilReadOnlyEvidence, WorkspaceContextAuthorityLevel.HumanReviewRequired, WorkspaceContextFreshnessStatus.Fresh, WorkspaceContextSensitivityLevel.Safe, WorkspaceContextSelectionState.Selected, WorkspaceContextLockState.Unlocked, WorkspaceContextExclusionState.NotExcluded, WorkspaceMemoryCandidateRiskSeverity.None, WorkspaceMemoryCandidateContradictionStatus.None, WorkspaceMemoryCandidateConfidenceStatus.Present, ["ev.decision.review"], human: false, humanAction: true, decision: true, safeNext: false, fixtureOnly: false, duplicate: false, WorkspaceMemoryCandidateInfluenceDecision.Blocked, WorkspaceMemoryCandidateContradictionRiskIssueKind.DecisionMissingHumanReview, "Decision candidate requires human review.", proof),
            Fixture("memory.decision.contradictory-evidence", WorkspaceContextItemKind.DecisionMemoryPreview, WorkspaceContextSourceKind.EilReadOnlyEvidence, WorkspaceContextAuthorityLevel.EvidenceLinked, WorkspaceContextFreshnessStatus.Fresh, WorkspaceContextSensitivityLevel.Safe, WorkspaceContextSelectionState.Selected, WorkspaceContextLockState.Unlocked, WorkspaceContextExclusionState.NotExcluded, WorkspaceMemoryCandidateRiskSeverity.None, WorkspaceMemoryCandidateContradictionStatus.ContradictoryEvidence, WorkspaceMemoryCandidateConfidenceStatus.Present, ["ev.decision.contradiction"], human: true, humanAction: true, decision: true, safeNext: false, fixtureOnly: false, duplicate: false, WorkspaceMemoryCandidateInfluenceDecision.Blocked, WorkspaceMemoryCandidateContradictionRiskIssueKind.DecisionWithContradictoryEvidence, "Decision candidate with contradictory evidence is blocked.", proof),
            Fixture("memory.claim.missing-confidence", WorkspaceContextItemKind.ClaimMemoryPreview, WorkspaceContextSourceKind.EilReadOnlyEvidence, WorkspaceContextAuthorityLevel.EvidenceLinked, WorkspaceContextFreshnessStatus.Fresh, WorkspaceContextSensitivityLevel.Safe, WorkspaceContextSelectionState.Selected, WorkspaceContextLockState.Unlocked, WorkspaceContextExclusionState.NotExcluded, WorkspaceMemoryCandidateRiskSeverity.None, WorkspaceMemoryCandidateContradictionStatus.None, WorkspaceMemoryCandidateConfidenceStatus.Missing, ["ev.claim.confidence"], human: false, humanAction: true, decision: false, safeNext: false, fixtureOnly: false, duplicate: false, WorkspaceMemoryCandidateInfluenceDecision.Blocked, WorkspaceMemoryCandidateContradictionRiskIssueKind.ClaimMissingConfidence, "Claim candidate missing confidence is blocked.", proof),
            Fixture("memory.claim.stale-evidence", WorkspaceContextItemKind.ClaimMemoryPreview, WorkspaceContextSourceKind.EilReadOnlyEvidence, WorkspaceContextAuthorityLevel.EvidenceLinked, WorkspaceContextFreshnessStatus.Stale, WorkspaceContextSensitivityLevel.Safe, WorkspaceContextSelectionState.Selected, WorkspaceContextLockState.Unlocked, WorkspaceContextExclusionState.NotExcluded, WorkspaceMemoryCandidateRiskSeverity.None, WorkspaceMemoryCandidateContradictionStatus.None, WorkspaceMemoryCandidateConfidenceStatus.Present, ["ev.claim.stale"], human: false, humanAction: true, decision: false, safeNext: false, fixtureOnly: false, duplicate: false, WorkspaceMemoryCandidateInfluenceDecision.Blocked, WorkspaceMemoryCandidateContradictionRiskIssueKind.ClaimStaleEvidence, "Claim candidate with stale evidence is blocked.", proof),
            Fixture("memory.action.missing-human-action", WorkspaceContextItemKind.ActionMemoryPreview, WorkspaceContextSourceKind.EilReadOnlyEvidence, WorkspaceContextAuthorityLevel.HumanReviewRequired, WorkspaceContextFreshnessStatus.Fresh, WorkspaceContextSensitivityLevel.Safe, WorkspaceContextSelectionState.Selected, WorkspaceContextLockState.Unlocked, WorkspaceContextExclusionState.NotExcluded, WorkspaceMemoryCandidateRiskSeverity.None, WorkspaceMemoryCandidateContradictionStatus.None, WorkspaceMemoryCandidateConfidenceStatus.Present, ["ev.action.human"], human: false, humanAction: false, decision: false, safeNext: false, fixtureOnly: false, duplicate: false, WorkspaceMemoryCandidateInfluenceDecision.Blocked, WorkspaceMemoryCandidateContradictionRiskIssueKind.ActionMissingRequiredHumanAction, "Action candidate requires required human action.", proof),
            Fixture("memory.action.excluded-context", WorkspaceContextItemKind.ActionMemoryPreview, WorkspaceContextSourceKind.CapabilityNotice, WorkspaceContextAuthorityLevel.ExcludedBySafety, WorkspaceContextFreshnessStatus.NotApplicable, WorkspaceContextSensitivityLevel.SensitiveNeverUse, WorkspaceContextSelectionState.NotSelected, WorkspaceContextLockState.Unlocked, WorkspaceContextExclusionState.Excluded, WorkspaceMemoryCandidateRiskSeverity.None, WorkspaceMemoryCandidateContradictionStatus.None, WorkspaceMemoryCandidateConfidenceStatus.Present, ["ev.action.excluded"], human: false, humanAction: true, decision: false, safeNext: false, fixtureOnly: false, duplicate: false, WorkspaceMemoryCandidateInfluenceDecision.Excluded, WorkspaceMemoryCandidateContradictionRiskIssueKind.ActionReferencesExcludedContext, "Action candidate referencing excluded context is excluded.", proof),
            Fixture("memory.safe-next.critical-risk", WorkspaceContextItemKind.SafeNextStep, WorkspaceContextSourceKind.EilReadOnlyEvidence, WorkspaceContextAuthorityLevel.EvidenceLinked, WorkspaceContextFreshnessStatus.Fresh, WorkspaceContextSensitivityLevel.Safe, WorkspaceContextSelectionState.Selected, WorkspaceContextLockState.Unlocked, WorkspaceContextExclusionState.NotExcluded, WorkspaceMemoryCandidateRiskSeverity.Critical, WorkspaceMemoryCandidateContradictionStatus.None, WorkspaceMemoryCandidateConfidenceStatus.Present, ["ev.safe.risk"], human: false, humanAction: true, decision: false, safeNext: true, fixtureOnly: false, duplicate: false, WorkspaceMemoryCandidateInfluenceDecision.Blocked, WorkspaceMemoryCandidateContradictionRiskIssueKind.SafeNextStepReliesOnCriticalRisk, "Safe next step is blocked by critical risk.", proof),
            Fixture("memory.safe-next.unresolved-contradiction", WorkspaceContextItemKind.SafeNextStep, WorkspaceContextSourceKind.EilReadOnlyEvidence, WorkspaceContextAuthorityLevel.EvidenceLinked, WorkspaceContextFreshnessStatus.Fresh, WorkspaceContextSensitivityLevel.Safe, WorkspaceContextSelectionState.Selected, WorkspaceContextLockState.Unlocked, WorkspaceContextExclusionState.NotExcluded, WorkspaceMemoryCandidateRiskSeverity.None, WorkspaceMemoryCandidateContradictionStatus.Unresolved, WorkspaceMemoryCandidateConfidenceStatus.Present, ["ev.safe.contradiction"], human: false, humanAction: true, decision: false, safeNext: true, fixtureOnly: false, duplicate: false, WorkspaceMemoryCandidateInfluenceDecision.Blocked, WorkspaceMemoryCandidateContradictionRiskIssueKind.SafeNextStepReliesOnUnresolvedContradiction, "Safe next step is blocked by unresolved contradiction.", proof),
            Fixture("memory.provider-derived-disabled", WorkspaceContextItemKind.RiskMemoryPreview, WorkspaceContextSourceKind.ProviderCloudDerived, WorkspaceContextAuthorityLevel.EvidenceLinked, WorkspaceContextFreshnessStatus.Fresh, WorkspaceContextSensitivityLevel.Safe, WorkspaceContextSelectionState.Selected, WorkspaceContextLockState.Unlocked, WorkspaceContextExclusionState.NotExcluded, WorkspaceMemoryCandidateRiskSeverity.Low, WorkspaceMemoryCandidateContradictionStatus.None, WorkspaceMemoryCandidateConfidenceStatus.Present, ["ev.memory.provider"], human: false, humanAction: true, decision: false, safeNext: false, fixtureOnly: false, duplicate: false, WorkspaceMemoryCandidateInfluenceDecision.Blocked, WorkspaceMemoryCandidateContradictionRiskIssueKind.ProviderDerivedWhileDisabled, "Provider-derived memory candidate is blocked while provider/cloud is disabled.", proof),
            Fixture("memory.semantic-derived-disabled", WorkspaceContextItemKind.ClaimMemoryPreview, WorkspaceContextSourceKind.SemanticVectorDerived, WorkspaceContextAuthorityLevel.EvidenceLinked, WorkspaceContextFreshnessStatus.Fresh, WorkspaceContextSensitivityLevel.Safe, WorkspaceContextSelectionState.Selected, WorkspaceContextLockState.Unlocked, WorkspaceContextExclusionState.NotExcluded, WorkspaceMemoryCandidateRiskSeverity.None, WorkspaceMemoryCandidateContradictionStatus.None, WorkspaceMemoryCandidateConfidenceStatus.Present, ["ev.memory.semantic"], human: false, humanAction: true, decision: false, safeNext: false, fixtureOnly: false, duplicate: false, WorkspaceMemoryCandidateInfluenceDecision.Blocked, WorkspaceMemoryCandidateContradictionRiskIssueKind.SemanticDerivedWhileDisabled, "Semantic-derived memory candidate is blocked while semantic/vector is disabled.", proof),
            Fixture("memory.legacy-no-provenance", WorkspaceContextItemKind.DecisionMemoryPreview, WorkspaceContextSourceKind.LegacyWithoutProvenance, WorkspaceContextAuthorityLevel.Informational, WorkspaceContextFreshnessStatus.Missing, WorkspaceContextSensitivityLevel.Unknown, WorkspaceContextSelectionState.NotSelected, WorkspaceContextLockState.Unlocked, WorkspaceContextExclusionState.NotExcluded, WorkspaceMemoryCandidateRiskSeverity.None, WorkspaceMemoryCandidateContradictionStatus.None, WorkspaceMemoryCandidateConfidenceStatus.Unknown, [], human: false, humanAction: true, decision: false, safeNext: false, fixtureOnly: false, duplicate: false, WorkspaceMemoryCandidateInfluenceDecision.Blocked, WorkspaceMemoryCandidateContradictionRiskIssueKind.LegacyWithoutProvenance, "Legacy memory candidate without provenance is blocked.", proof),
            Fixture("memory.fixture-only", WorkspaceContextItemKind.ClaimMemoryPreview, WorkspaceContextSourceKind.Fixture, WorkspaceContextAuthorityLevel.Informational, WorkspaceContextFreshnessStatus.FixtureCurrent, WorkspaceContextSensitivityLevel.Safe, WorkspaceContextSelectionState.Selected, WorkspaceContextLockState.Unlocked, WorkspaceContextExclusionState.NotExcluded, WorkspaceMemoryCandidateRiskSeverity.None, WorkspaceMemoryCandidateContradictionStatus.None, WorkspaceMemoryCandidateConfidenceStatus.Present, ["ev.memory.fixture"], human: false, humanAction: true, decision: false, safeNext: false, fixtureOnly: true, duplicate: false, WorkspaceMemoryCandidateInfluenceDecision.WarningReadOnlyOnly, WorkspaceMemoryCandidateContradictionRiskIssueKind.FixtureOnlyNotDurableTrusted, "Fixture-only memory candidate is read-only and not durable/trusted.", proof),
            Fixture("memory.duplicate-conflicting", WorkspaceContextItemKind.DecisionMemoryPreview, WorkspaceContextSourceKind.EilReadOnlyEvidence, WorkspaceContextAuthorityLevel.EvidenceLinked, WorkspaceContextFreshnessStatus.Fresh, WorkspaceContextSensitivityLevel.Safe, WorkspaceContextSelectionState.Selected, WorkspaceContextLockState.Unlocked, WorkspaceContextExclusionState.NotExcluded, WorkspaceMemoryCandidateRiskSeverity.None, WorkspaceMemoryCandidateContradictionStatus.ContradictoryEvidence, WorkspaceMemoryCandidateConfidenceStatus.Present, ["ev.memory.duplicate"], human: true, humanAction: true, decision: false, safeNext: false, fixtureOnly: false, duplicate: true, WorkspaceMemoryCandidateInfluenceDecision.Blocked, WorkspaceMemoryCandidateContradictionRiskIssueKind.DuplicateConflictingCandidates, "Duplicate memory candidates with conflicting conclusions are blocked.", proof),
            Fixture("memory.raw-sensitive-payload", WorkspaceContextItemKind.RiskMemoryPreview, WorkspaceContextSourceKind.Fixture, WorkspaceContextAuthorityLevel.ExcludedBySafety, WorkspaceContextFreshnessStatus.NotApplicable, WorkspaceContextSensitivityLevel.RawPayload, WorkspaceContextSelectionState.NotSelected, WorkspaceContextLockState.LockedBySafety, WorkspaceContextExclusionState.Excluded, WorkspaceMemoryCandidateRiskSeverity.Critical, WorkspaceMemoryCandidateContradictionStatus.None, WorkspaceMemoryCandidateConfidenceStatus.Unknown, [], human: false, humanAction: false, decision: false, safeNext: false, fixtureOnly: false, duplicate: false, WorkspaceMemoryCandidateInfluenceDecision.Excluded, WorkspaceMemoryCandidateContradictionRiskIssueKind.RawSensitivePayload, "Memory candidate containing raw or sensitive payload is excluded.", proof),
            Fixture("memory.unknown-authority", WorkspaceContextItemKind.ClaimMemoryPreview, WorkspaceContextSourceKind.Fixture, WorkspaceContextAuthorityLevel.Informational, WorkspaceContextFreshnessStatus.Fresh, WorkspaceContextSensitivityLevel.Unknown, WorkspaceContextSelectionState.Selected, WorkspaceContextLockState.Unlocked, WorkspaceContextExclusionState.NotExcluded, WorkspaceMemoryCandidateRiskSeverity.None, WorkspaceMemoryCandidateContradictionStatus.None, WorkspaceMemoryCandidateConfidenceStatus.Present, ["ev.memory.unknown"], human: false, humanAction: true, decision: false, safeNext: false, fixtureOnly: false, duplicate: false, WorkspaceMemoryCandidateInfluenceDecision.Blocked, WorkspaceMemoryCandidateContradictionRiskIssueKind.UnknownAuthority, "Memory candidate with unknown authority is blocked.", proof),
            Fixture("memory.missing-freshness", WorkspaceContextItemKind.ClaimMemoryPreview, WorkspaceContextSourceKind.EilReadOnlyEvidence, WorkspaceContextAuthorityLevel.EvidenceLinked, WorkspaceContextFreshnessStatus.Missing, WorkspaceContextSensitivityLevel.Safe, WorkspaceContextSelectionState.Selected, WorkspaceContextLockState.Unlocked, WorkspaceContextExclusionState.NotExcluded, WorkspaceMemoryCandidateRiskSeverity.None, WorkspaceMemoryCandidateContradictionStatus.None, WorkspaceMemoryCandidateConfidenceStatus.Present, ["ev.memory.missing-freshness"], human: false, humanAction: true, decision: false, safeNext: false, fixtureOnly: false, duplicate: false, WorkspaceMemoryCandidateInfluenceDecision.Blocked, WorkspaceMemoryCandidateContradictionRiskIssueKind.MissingFreshness, "Memory candidate with missing freshness is blocked.", proof)
        ];
    }

    public static WorkspaceMemoryCandidateContradictionRiskResult Evaluate(WorkspaceMemoryCandidateContradictionRiskFixture fixture)
    {
        var issues = new List<WorkspaceMemoryCandidateContradictionRiskIssue>();

        AddIssues(fixture, issues);

        var decision = Decide(fixture, issues);
        var blockers = issues.Where(issue => issue.BlocksDecision || issue.BlocksSafeNextStep || issue.BlocksCandidateInfluence || issue.BlocksDashboardExport).Select(issue => issue.Message).ToList();
        var warnings = issues.Where(issue => !issue.BlocksDecision && !issue.BlocksSafeNextStep && !issue.BlocksCandidateInfluence && !issue.BlocksDashboardExport).Select(issue => issue.Message).ToList();
        var requiresHumanReview = issues.Any(issue => issue.RequiresHumanReview)
            || decision == WorkspaceMemoryCandidateInfluenceDecision.NeedsHumanReview;

        return new WorkspaceMemoryCandidateContradictionRiskResult(
            fixture.FixtureId,
            decision,
            issues,
            warnings,
            blockers,
            AllowsReadOnlyPreview: decision is WorkspaceMemoryCandidateInfluenceDecision.AllowedReadOnlyWarning or WorkspaceMemoryCandidateInfluenceDecision.WarningReadOnlyOnly or WorkspaceMemoryCandidateInfluenceDecision.NeedsHumanReview,
            AllowsDecisionUse: false,
            AllowsSafeNextStepUse: false,
            AllowsCandidateInfluence: decision == WorkspaceMemoryCandidateInfluenceDecision.AllowedReadOnlyWarning && !requiresHumanReview && issues.All(issue => !issue.BlocksCandidateInfluence),
            AllowsDashboardExportAppearance: decision != WorkspaceMemoryCandidateInfluenceDecision.Excluded && issues.All(issue => !issue.BlocksDashboardExport),
            DurableMemoryEnabled: false,
            RequiresHumanReview: requiresHumanReview,
            NoSideEffectProof: fixture.NoSideEffectProof);
    }

    public static IReadOnlyList<WorkspaceMemoryCandidateContradictionRiskResult> EvaluateCatalog() =>
        CreateFixtureCatalog().Select(Evaluate).ToList();

    private static void AddIssues(WorkspaceMemoryCandidateContradictionRiskFixture fixture, List<WorkspaceMemoryCandidateContradictionRiskIssue> issues)
    {
        if (fixture.EvidenceRefs.Count == 0)
            issues.Add(Issue(WorkspaceMemoryCandidateContradictionRiskIssueKind.CandidateWithoutEvidence, "Memory candidate without evidence is blocked.", blockDecision: true, blockSafeNext: true, blockInfluence: true, blockDashboard: true, human: true));

        if (fixture.Freshness == WorkspaceContextFreshnessStatus.Stale)
            issues.Add(Issue(fixture.CandidateKind == WorkspaceContextItemKind.ClaimMemoryPreview ? WorkspaceMemoryCandidateContradictionRiskIssueKind.ClaimStaleEvidence : WorkspaceMemoryCandidateContradictionRiskIssueKind.CandidateUsesStaleContext, "Memory candidate using stale context is blocked.", blockDecision: true, blockSafeNext: true, blockInfluence: true, blockDashboard: false, human: true));

        if (fixture.ExclusionState != WorkspaceContextExclusionState.NotExcluded)
            issues.Add(Issue(fixture.CandidateKind == WorkspaceContextItemKind.ActionMemoryPreview ? WorkspaceMemoryCandidateContradictionRiskIssueKind.ActionReferencesExcludedContext : WorkspaceMemoryCandidateContradictionRiskIssueKind.CandidateUsesExcludedContext, "Memory candidate using excluded context is excluded.", blockDecision: true, blockSafeNext: true, blockInfluence: true, blockDashboard: true, human: true));

        if (fixture.LockState is WorkspaceContextLockState.LockedBySafety or WorkspaceContextLockState.LockedRequiresHumanReview && !fixture.HumanReviewed)
            issues.Add(Issue(WorkspaceMemoryCandidateContradictionRiskIssueKind.CandidateUsesLockedUnsafeContext, "Memory candidate using locked unsafe context is blocked.", blockDecision: true, blockSafeNext: true, blockInfluence: true, blockDashboard: false, human: true));

        if (fixture.ContradictionStatus == WorkspaceMemoryCandidateContradictionStatus.EvidenceLinked && !fixture.HumanReviewed)
            issues.Add(Issue(WorkspaceMemoryCandidateContradictionRiskIssueKind.ContradictionRequiresHumanReview, "Evidence-linked contradiction remains read-only and requires human review.", blockDecision: true, blockSafeNext: true, blockInfluence: false, blockDashboard: false, human: true));

        if (fixture.RiskSeverity == WorkspaceMemoryCandidateRiskSeverity.Missing)
            issues.Add(Issue(WorkspaceMemoryCandidateContradictionRiskIssueKind.RiskMissingSeverity, "Risk candidate without severity is blocked.", blockDecision: true, blockSafeNext: true, blockInfluence: true, blockDashboard: true, human: true));

        if (fixture.CandidateKind == WorkspaceContextItemKind.RiskMemoryPreview && fixture.AttemptsDecisionMemory)
            issues.Add(Issue(WorkspaceMemoryCandidateContradictionRiskIssueKind.RiskCannotBecomeDecisionMemory, "Risk candidate cannot become decision memory.", blockDecision: true, blockSafeNext: true, blockInfluence: true, blockDashboard: true, human: true));

        if (fixture.CandidateKind == WorkspaceContextItemKind.DecisionMemoryPreview && !fixture.HumanReviewed)
            issues.Add(Issue(WorkspaceMemoryCandidateContradictionRiskIssueKind.DecisionMissingHumanReview, "Decision candidate requires human review.", blockDecision: true, blockSafeNext: true, blockInfluence: true, blockDashboard: false, human: true));

        if (fixture.CandidateKind == WorkspaceContextItemKind.DecisionMemoryPreview && fixture.ContradictionStatus == WorkspaceMemoryCandidateContradictionStatus.ContradictoryEvidence)
            issues.Add(Issue(WorkspaceMemoryCandidateContradictionRiskIssueKind.DecisionWithContradictoryEvidence, "Decision candidate with contradictory evidence is blocked.", blockDecision: true, blockSafeNext: true, blockInfluence: true, blockDashboard: true, human: true));

        if (fixture.CandidateKind == WorkspaceContextItemKind.ClaimMemoryPreview && fixture.ConfidenceStatus != WorkspaceMemoryCandidateConfidenceStatus.Present)
            issues.Add(Issue(WorkspaceMemoryCandidateContradictionRiskIssueKind.ClaimMissingConfidence, "Claim candidate missing confidence is blocked.", blockDecision: true, blockSafeNext: true, blockInfluence: true, blockDashboard: true, human: true));

        if (fixture.CandidateKind == WorkspaceContextItemKind.ActionMemoryPreview && !fixture.RequiredHumanActionPresent)
            issues.Add(Issue(WorkspaceMemoryCandidateContradictionRiskIssueKind.ActionMissingRequiredHumanAction, "Action candidate requires required human action.", blockDecision: true, blockSafeNext: true, blockInfluence: true, blockDashboard: true, human: true));

        if (fixture.SafeNextStepCandidate && fixture.RiskSeverity == WorkspaceMemoryCandidateRiskSeverity.Critical)
            issues.Add(Issue(WorkspaceMemoryCandidateContradictionRiskIssueKind.SafeNextStepReliesOnCriticalRisk, "Safe next step is blocked by critical risk.", blockDecision: true, blockSafeNext: true, blockInfluence: true, blockDashboard: false, human: true));

        if (fixture.SafeNextStepCandidate && fixture.ContradictionStatus == WorkspaceMemoryCandidateContradictionStatus.Unresolved)
            issues.Add(Issue(WorkspaceMemoryCandidateContradictionRiskIssueKind.SafeNextStepReliesOnUnresolvedContradiction, "Safe next step is blocked by unresolved contradiction.", blockDecision: true, blockSafeNext: true, blockInfluence: true, blockDashboard: false, human: true));

        if (fixture.SourceKind == WorkspaceContextSourceKind.ProviderCloudDerived)
            issues.Add(Issue(WorkspaceMemoryCandidateContradictionRiskIssueKind.ProviderDerivedWhileDisabled, "Provider-derived memory candidate is blocked while provider/cloud is disabled.", blockDecision: true, blockSafeNext: true, blockInfluence: true, blockDashboard: true, human: true));

        if (fixture.SourceKind == WorkspaceContextSourceKind.SemanticVectorDerived)
            issues.Add(Issue(WorkspaceMemoryCandidateContradictionRiskIssueKind.SemanticDerivedWhileDisabled, "Semantic-derived memory candidate is blocked while semantic/vector is disabled.", blockDecision: true, blockSafeNext: true, blockInfluence: true, blockDashboard: true, human: true));

        if (fixture.SourceKind == WorkspaceContextSourceKind.LegacyWithoutProvenance)
            issues.Add(Issue(WorkspaceMemoryCandidateContradictionRiskIssueKind.LegacyWithoutProvenance, "Legacy memory candidate without provenance is blocked.", blockDecision: true, blockSafeNext: true, blockInfluence: true, blockDashboard: true, human: true));

        if (fixture.FixtureOnly)
            issues.Add(Issue(WorkspaceMemoryCandidateContradictionRiskIssueKind.FixtureOnlyNotDurableTrusted, "Fixture-only memory candidate is read-only and not durable/trusted.", blockDecision: false, blockSafeNext: false, blockInfluence: true, blockDashboard: false, human: false));

        if (fixture.DuplicateConflictingCandidate)
            issues.Add(Issue(WorkspaceMemoryCandidateContradictionRiskIssueKind.DuplicateConflictingCandidates, "Duplicate memory candidates with conflicting conclusions are blocked.", blockDecision: true, blockSafeNext: true, blockInfluence: true, blockDashboard: true, human: true));

        if (fixture.Sensitivity is WorkspaceContextSensitivityLevel.RawPayload or WorkspaceContextSensitivityLevel.Sensitive or WorkspaceContextSensitivityLevel.SensitiveNeverUse)
            issues.Add(Issue(WorkspaceMemoryCandidateContradictionRiskIssueKind.RawSensitivePayload, "Memory candidate containing raw or sensitive payload is excluded.", blockDecision: true, blockSafeNext: true, blockInfluence: true, blockDashboard: true, human: true));

        if (fixture.Sensitivity == WorkspaceContextSensitivityLevel.Unknown)
            issues.Add(Issue(WorkspaceMemoryCandidateContradictionRiskIssueKind.UnknownAuthority, "Memory candidate with unknown authority is blocked.", blockDecision: true, blockSafeNext: true, blockInfluence: true, blockDashboard: true, human: true));

        if (fixture.Freshness == WorkspaceContextFreshnessStatus.Missing)
            issues.Add(Issue(WorkspaceMemoryCandidateContradictionRiskIssueKind.MissingFreshness, "Memory candidate with missing freshness is blocked.", blockDecision: true, blockSafeNext: true, blockInfluence: true, blockDashboard: true, human: true));
    }

    private static WorkspaceMemoryCandidateInfluenceDecision Decide(
        WorkspaceMemoryCandidateContradictionRiskFixture fixture,
        IReadOnlyList<WorkspaceMemoryCandidateContradictionRiskIssue> issues)
    {
        if (fixture.ExclusionState != WorkspaceContextExclusionState.NotExcluded
            || fixture.Sensitivity is WorkspaceContextSensitivityLevel.RawPayload or WorkspaceContextSensitivityLevel.SensitiveNeverUse)
            return WorkspaceMemoryCandidateInfluenceDecision.Excluded;

        if (issues.Any(issue => issue.BlocksDecision || issue.BlocksSafeNextStep || issue.BlocksCandidateInfluence)
            && !issues.All(issue => issue.IssueKind is WorkspaceMemoryCandidateContradictionRiskIssueKind.ContradictionRequiresHumanReview or WorkspaceMemoryCandidateContradictionRiskIssueKind.FixtureOnlyNotDurableTrusted))
            return WorkspaceMemoryCandidateInfluenceDecision.Blocked;

        if (issues.Any(issue => issue.RequiresHumanReview))
            return WorkspaceMemoryCandidateInfluenceDecision.NeedsHumanReview;

        if (issues.Count > 0)
            return WorkspaceMemoryCandidateInfluenceDecision.WarningReadOnlyOnly;

        return WorkspaceMemoryCandidateInfluenceDecision.AllowedReadOnlyWarning;
    }

    private static WorkspaceMemoryCandidateContradictionRiskIssue Issue(
        WorkspaceMemoryCandidateContradictionRiskIssueKind issueKind,
        string message,
        bool blockDecision,
        bool blockSafeNext,
        bool blockInfluence,
        bool blockDashboard,
        bool human) =>
        new(issueKind, message, blockDecision, blockSafeNext, blockInfluence, blockDashboard, human);

    private static WorkspaceMemoryCandidateContradictionRiskFixture Fixture(
        string fixtureId,
        WorkspaceContextItemKind kind,
        WorkspaceContextSourceKind sourceKind,
        WorkspaceContextAuthorityLevel authority,
        WorkspaceContextFreshnessStatus freshness,
        WorkspaceContextSensitivityLevel sensitivity,
        WorkspaceContextSelectionState selection,
        WorkspaceContextLockState locked,
        WorkspaceContextExclusionState exclusion,
        WorkspaceMemoryCandidateRiskSeverity risk,
        WorkspaceMemoryCandidateContradictionStatus contradiction,
        WorkspaceMemoryCandidateConfidenceStatus confidence,
        IReadOnlyList<string> evidenceRefs,
        bool human,
        bool humanAction,
        bool decision,
        bool safeNext,
        bool fixtureOnly,
        bool duplicate,
        WorkspaceMemoryCandidateInfluenceDecision expectedDecision,
        WorkspaceMemoryCandidateContradictionRiskIssueKind expectedIssue,
        string expectedMessage,
        WorkspaceContextNoSideEffectProof proof) =>
        new(fixtureId, kind, sourceKind, authority, freshness, sensitivity, selection, locked, exclusion, risk, contradiction, confidence, evidenceRefs, human, humanAction, decision, safeNext, fixtureOnly, duplicate, expectedDecision, expectedIssue, expectedMessage, proof);
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

public static class WorkspaceContextPacketReadOnlySurfacePresenter
{
    public static WorkspaceContextPacketSurfaceReadOnly CreateFixture()
    {
        var packet = WorkspaceContextReadOnlyPresenter.CreateFixture();
        var proof = WorkspaceContextNoSideEffectProof.FixtureReadOnly();
        var authority = WorkspaceContextAuthorityFreshnessGuard.EvaluateCatalog();
        var selection = WorkspaceContextSelectionLockExclusionGuard.EvaluateCatalog();
        var memory = WorkspaceMemoryCandidateContradictionRiskGuard.EvaluateCatalog();
        var sections = Sections(packet, authority, selection, memory, proof);
        var guardSummaries = GuardSummaries(authority, selection, memory);
        var candidateSummaries = CandidateSummaries(packet, memory);
        var humanReview = HumanReviewRequirements(authority, selection, memory, packet);
        var disabled = DisabledNotices(packet);
        var warnings = sections.SelectMany(section => section.Warnings).Concat(packet.Warnings).Distinct().ToList();
        var blockers = sections.SelectMany(section => section.Blockers).Concat(packet.Blockers).Distinct().ToList();

        return new WorkspaceContextPacketSurfaceReadOnly(
            SurfaceId: "phase-d.workspace-context.packet.surface.read-only.fixture.v1",
            Title: "Workspace Context Packet Read-Only Surface",
            Mode: "READ_ONLY_FIXTURE_SAFE_NO_ACTIONS_NO_EXPORT",
            SourceLabel: "WorkspaceContextReadOnlyPresenter.CreateFixture",
            Packet: packet,
            Sections: sections,
            GuardSummaries: guardSummaries,
            CandidateSummaries: candidateSummaries,
            HumanReviewRequirements: humanReview,
            DisabledNotices: disabled,
            Warnings: warnings,
            Blockers: blockers,
            NextRecommendedBlock: "PHASE_D_CONTEXT_PACKET_READ_ONLY_EXPORT_PREVIEW",
            ReadOnlySummary: Summary(packet, sections, guardSummaries, candidateSummaries),
            NoSideEffectProof: proof);
    }

    private static IReadOnlyList<WorkspaceContextPacketSurfaceSection> Sections(
        WorkspaceContextPacketReadOnly packet,
        IReadOnlyList<WorkspaceContextAuthorityFreshnessResult> authority,
        IReadOnlyList<WorkspaceContextSelectionLockExclusionResult> selection,
        IReadOnlyList<WorkspaceMemoryCandidateContradictionRiskResult> memory,
        WorkspaceContextNoSideEffectProof proof) =>
    [
        Section("context.packet.executive-summary", "Context packet executive summary", WorkspaceContextItemStatus.Ready, WorkspaceContextPacketSurfaceSeverity.Info, WorkspaceContextSourceKind.Fixture, [], packet.Warnings, [], proof),
        Section("workspace.identity.fixture", "Workspace identity fixture", WorkspaceContextItemStatus.Ready, WorkspaceContextPacketSurfaceSeverity.Info, WorkspaceContextSourceKind.Fixture, [], [], [], proof),
        Section("selected.context", "Selected context", WorkspaceContextItemStatus.Selected, WorkspaceContextPacketSurfaceSeverity.Info, WorkspaceContextSourceKind.HumanProvidedFixture, packet.SelectedContext.SelectMany(item => item.EvidenceRefs).Distinct().ToList(), [], [], proof),
        Section("locked.context", "Locked context", WorkspaceContextItemStatus.Locked, WorkspaceContextPacketSurfaceSeverity.Blocked, WorkspaceContextSourceKind.CapabilityNotice, [], [], packet.LockedContext.SelectMany(item => item.Blockers).Distinct().ToList(), proof),
        Section("excluded.context", "Excluded context", WorkspaceContextItemStatus.Excluded, WorkspaceContextPacketSurfaceSeverity.Blocked, WorkspaceContextSourceKind.CapabilityNotice, [], packet.ExcludedContext.SelectMany(item => item.Warnings).Distinct().ToList(), packet.ExcludedContext.SelectMany(item => item.Blockers).Distinct().ToList(), proof),
        Section("authority.freshness.guard.summary", "Authority/freshness guard summary", WorkspaceContextItemStatus.Ready, WorkspaceContextPacketSurfaceSeverity.Warning, WorkspaceContextSourceKind.Fixture, authority.SelectMany(result => result.Issues.Select(issue => result.FixtureId)).Take(8).ToList(), authority.SelectMany(result => result.Warnings).Distinct().ToList(), authority.SelectMany(result => result.Blockers).Distinct().ToList(), proof),
        Section("selection.lock.exclusion.guard.summary", "Selection/lock/exclusion guard summary", WorkspaceContextItemStatus.Ready, WorkspaceContextPacketSurfaceSeverity.Warning, WorkspaceContextSourceKind.Fixture, selection.SelectMany(result => result.Issues.Select(issue => result.FixtureId)).Take(8).ToList(), selection.SelectMany(result => result.Warnings).Distinct().ToList(), selection.SelectMany(result => result.Blockers).Distinct().ToList(), proof),
        Section("memory.candidate.guard.summary", "Memory candidate contradiction/risk guard summary", WorkspaceContextItemStatus.Ready, WorkspaceContextPacketSurfaceSeverity.Warning, WorkspaceContextSourceKind.Fixture, memory.SelectMany(result => result.Issues.Select(issue => result.FixtureId)).Take(8).ToList(), memory.SelectMany(result => result.Warnings).Distinct().ToList(), memory.SelectMany(result => result.Blockers).Distinct().ToList(), proof),
        Section("contradiction.candidates", "Contradiction candidates", WorkspaceContextItemStatus.Warning, WorkspaceContextPacketSurfaceSeverity.Warning, WorkspaceContextSourceKind.EilReadOnlyEvidence, CandidateRefs(packet, WorkspaceContextItemKind.ContradictionMemoryPreview), [], memory.Where(result => result.FixtureId.Contains("contradiction", StringComparison.Ordinal)).SelectMany(result => result.Blockers).Distinct().ToList(), proof),
        Section("risk.candidates", "Risk candidates", WorkspaceContextItemStatus.Warning, WorkspaceContextPacketSurfaceSeverity.Warning, WorkspaceContextSourceKind.EilReadOnlyEvidence, CandidateRefs(packet, WorkspaceContextItemKind.RiskMemoryPreview), memory.Where(result => result.FixtureId.Contains(".risk.", StringComparison.Ordinal)).SelectMany(result => result.Warnings).Distinct().ToList(), memory.Where(result => result.FixtureId.Contains(".risk.", StringComparison.Ordinal)).SelectMany(result => result.Blockers).Distinct().ToList(), proof),
        Section("decision.candidates", "Decision candidates", WorkspaceContextItemStatus.Blocked, WorkspaceContextPacketSurfaceSeverity.Blocked, WorkspaceContextSourceKind.EilReadOnlyEvidence, CandidateRefs(packet, WorkspaceContextItemKind.DecisionMemoryPreview), [], memory.Where(result => result.FixtureId.Contains(".decision.", StringComparison.Ordinal)).SelectMany(result => result.Blockers).Distinct().ToList(), proof),
        Section("claim.candidates", "Claim candidates", WorkspaceContextItemStatus.Warning, WorkspaceContextPacketSurfaceSeverity.Warning, WorkspaceContextSourceKind.EilReadOnlyEvidence, CandidateRefs(packet, WorkspaceContextItemKind.ClaimMemoryPreview), [], memory.Where(result => result.FixtureId.Contains(".claim.", StringComparison.Ordinal)).SelectMany(result => result.Blockers).Distinct().ToList(), proof),
        Section("action.candidates", "Action candidates", WorkspaceContextItemStatus.Blocked, WorkspaceContextPacketSurfaceSeverity.Blocked, WorkspaceContextSourceKind.EilReadOnlyEvidence, CandidateRefs(packet, WorkspaceContextItemKind.ActionMemoryPreview), [], memory.Where(result => result.FixtureId.Contains(".action.", StringComparison.Ordinal)).SelectMany(result => result.Blockers).Distinct().ToList(), proof),
        Section("safe.next.step.status", "Safe next step status", WorkspaceContextItemStatus.Ready, WorkspaceContextPacketSurfaceSeverity.Warning, WorkspaceContextSourceKind.NoSideEffectProof, [], ["Next step is read-only only."], memory.Where(result => result.FixtureId.Contains("safe-next", StringComparison.Ordinal)).SelectMany(result => result.Blockers).Distinct().ToList(), proof),
        Section("human.review.requirements", "Human review requirements", WorkspaceContextItemStatus.Warning, WorkspaceContextPacketSurfaceSeverity.Warning, WorkspaceContextSourceKind.DocumentedDebt, [], HumanReviewRequirements(authority, selection, memory, packet), [], proof),
        Section("missing.stale.context.warnings", "Missing/stale context warnings", WorkspaceContextItemStatus.Warning, WorkspaceContextPacketSurfaceSeverity.Warning, WorkspaceContextSourceKind.DocumentedDebt, [], packet.Items.Where(item => item.Freshness is WorkspaceContextFreshnessStatus.Stale or WorkspaceContextFreshnessStatus.Missing).Select(item => item.Title).ToList(), [], proof),
        Section("blocked.context.candidate.list", "Blocked context/candidate list", WorkspaceContextItemStatus.Blocked, WorkspaceContextPacketSurfaceSeverity.Blocked, WorkspaceContextSourceKind.CapabilityNotice, [], [], BlockedList(authority, selection, memory, packet), proof),
        Section("provider.cloud.disabled", "Provider/cloud disabled notice", WorkspaceContextItemStatus.Disabled, WorkspaceContextPacketSurfaceSeverity.Disabled, WorkspaceContextSourceKind.CapabilityNotice, [], [], [packet.ProviderCloudNotice], proof),
        Section("semantic.vector.disabled", "Semantic/vector disabled notice", WorkspaceContextItemStatus.Disabled, WorkspaceContextPacketSurfaceSeverity.Disabled, WorkspaceContextSourceKind.CapabilityNotice, [], [], [packet.SemanticVectorNotice], proof),
        Section("durable.memory.disabled", "Durable memory disabled notice", WorkspaceContextItemStatus.Disabled, WorkspaceContextPacketSurfaceSeverity.Disabled, WorkspaceContextSourceKind.CapabilityNotice, [], [], ["Durable memory is disabled; packet and candidates are preview-only."], proof),
        Section("runtime.live.disabled", "Runtime/live disabled notice", WorkspaceContextItemStatus.Disabled, WorkspaceContextPacketSurfaceSeverity.Disabled, WorkspaceContextSourceKind.CapabilityNotice, [], [], ["Runtime/live/browser/CDP/WCU/OCR remain disabled."], proof),
        Section("no.side.effect.proof", "No-side-effect proof", WorkspaceContextItemStatus.Ready, WorkspaceContextPacketSurfaceSeverity.Info, WorkspaceContextSourceKind.NoSideEffectProof, [], [], [], proof),
        Section("documented.debt", "Documented debt", WorkspaceContextItemStatus.Deferred, WorkspaceContextPacketSurfaceSeverity.Deferred, WorkspaceContextSourceKind.DocumentedDebt, [], packet.DocumentedDebt, [], proof),
        Section("next.recommended.block", "Next recommended block", WorkspaceContextItemStatus.Deferred, WorkspaceContextPacketSurfaceSeverity.Deferred, WorkspaceContextSourceKind.DocumentedDebt, [], ["PHASE_D_CONTEXT_PACKET_READ_ONLY_EXPORT_PREVIEW"], [], proof)
    ];

    private static WorkspaceContextPacketSurfaceSection Section(
        string id,
        string title,
        WorkspaceContextItemStatus status,
        WorkspaceContextPacketSurfaceSeverity severity,
        WorkspaceContextSourceKind source,
        IReadOnlyList<string> evidenceRefs,
        IReadOnlyList<string> warnings,
        IReadOnlyList<string> blockers,
        WorkspaceContextNoSideEffectProof proof) =>
        new(id, title, status, severity, source, evidenceRefs, Clean(warnings), Clean(blockers), ProductActionsCount: 0, ExportActionsCount: 0, proof);

    private static IReadOnlyList<string> GuardSummaries(
        IReadOnlyList<WorkspaceContextAuthorityFreshnessResult> authority,
        IReadOnlyList<WorkspaceContextSelectionLockExclusionResult> selection,
        IReadOnlyList<WorkspaceMemoryCandidateContradictionRiskResult> memory) =>
    [
        $"Authority/freshness fixtures: {authority.Count}; blocked/excluded: {authority.Count(result => result.Blocked)}.",
        $"Selection/lock/exclusion fixtures: {selection.Count}; blocked/excluded: {selection.Count(result => result.Blocked)}.",
        $"Memory candidate fixtures: {memory.Count}; blocked/excluded: {memory.Count(result => result.Blocked)}."
    ];

    private static IReadOnlyList<string> CandidateSummaries(
        WorkspaceContextPacketReadOnly packet,
        IReadOnlyList<WorkspaceMemoryCandidateContradictionRiskResult> memory) =>
    [
        $"Preview candidates: {packet.MemoryCandidates.Count}; durable memory capability disabled.",
        $"Contradiction/risk guard warnings: {memory.Count(result => result.Warnings.Count > 0)}.",
        $"Contradiction/risk guard blockers: {memory.Count(result => result.Blockers.Count > 0)}.",
        "Candidate is not memory; risk is not decision."
    ];

    private static IReadOnlyList<string> HumanReviewRequirements(
        IReadOnlyList<WorkspaceContextAuthorityFreshnessResult> authority,
        IReadOnlyList<WorkspaceContextSelectionLockExclusionResult> selection,
        IReadOnlyList<WorkspaceMemoryCandidateContradictionRiskResult> memory,
        WorkspaceContextPacketReadOnly packet) =>
        authority.Where(result => result.RequiresHumanReview).Select(result => $"Authority/freshness review: {result.FixtureId}.")
            .Concat(selection.Where(result => result.RequiresHumanReview).Select(result => $"Selection/lock/exclusion review: {result.FixtureId}."))
            .Concat(memory.Where(result => result.RequiresHumanReview).Select(result => $"Memory candidate review: {result.FixtureId}."))
            .Concat(packet.Warnings.Select(warning => $"Packet warning review: {warning}"))
            .Distinct()
            .Take(16)
            .ToList();

    private static IReadOnlyList<string> DisabledNotices(WorkspaceContextPacketReadOnly packet) =>
    [
        packet.ProviderCloudNotice,
        packet.SemanticVectorNotice,
        "Durable memory disabled.",
        "Runtime/live disabled.",
        "Export actions disabled."
    ];

    private static IReadOnlyList<string> BlockedList(
        IReadOnlyList<WorkspaceContextAuthorityFreshnessResult> authority,
        IReadOnlyList<WorkspaceContextSelectionLockExclusionResult> selection,
        IReadOnlyList<WorkspaceMemoryCandidateContradictionRiskResult> memory,
        WorkspaceContextPacketReadOnly packet) =>
        packet.Items.Where(item => item.Status is WorkspaceContextItemStatus.Blocked or WorkspaceContextItemStatus.Excluded).Select(item => item.ItemId)
            .Concat(authority.Where(result => result.Blocked).Select(result => result.FixtureId))
            .Concat(selection.Where(result => result.Blocked).Select(result => result.FixtureId))
            .Concat(memory.Where(result => result.Blocked).Select(result => result.FixtureId))
            .Distinct()
            .Take(24)
            .ToList();

    private static IReadOnlyList<string> CandidateRefs(WorkspaceContextPacketReadOnly packet, WorkspaceContextItemKind kind) =>
        packet.MemoryCandidates.Where(candidate => candidate.Kind == kind).SelectMany(candidate => candidate.EvidenceRefs).Distinct().ToList();

    private static string Summary(
        WorkspaceContextPacketReadOnly packet,
        IReadOnlyList<WorkspaceContextPacketSurfaceSection> sections,
        IReadOnlyList<string> guardSummaries,
        IReadOnlyList<string> candidateSummaries) =>
        string.Join(
            "\n",
            new[]
            {
                "# Workspace Context Packet Read-Only Surface",
                "Mode: READ_ONLY_FIXTURE_SAFE_NO_ACTIONS_NO_EXPORT",
                $"Workspace: {packet.WorkspaceId}.",
                $"Sections: {sections.Count}.",
                $"Product actions: {sections.Sum(section => section.ProductActionsCount)}.",
                $"Export actions: {sections.Sum(section => section.ExportActionsCount)}.",
                string.Join(" ", guardSummaries),
                string.Join(" ", candidateSummaries),
                "No workspace filesystem read, durable memory, provider/cloud, semantic/vector backend, export or runtime is enabled."
            });

    private static IReadOnlyList<string> Clean(IReadOnlyList<string> values) =>
        values.Select(Clean).Distinct().ToList();

    private static string Clean(string value) =>
        value
            .Replace("\r", " ", StringComparison.Ordinal)
            .Replace("\n", " ", StringComparison.Ordinal)
            .Replace("raw payload", "excluded payload", StringComparison.OrdinalIgnoreCase)
            .Replace("secret", "sensitive value", StringComparison.OrdinalIgnoreCase);
}
