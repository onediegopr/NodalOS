namespace OneBrain.Core.Approval;

public enum DurableAuditTrailAppendOnlyCandidateDecision
{
    Blocked,
    CandidateAcceptedNoWrite
}

public enum DurableAuditTrailAppendOnlyCandidateBlockReason
{
    MissingUserExplicitGo,
    MissingPreImplementationExternalAuditGo,
    MissingScopeLock,
    MissingNegativeTests,
    MissingNoUnresolvedBlockingFindingsProof,
    MissingIsolatedBoundary,
    MissingFailClosedPlan,
    MissingNoSideEffectProof,
    MissingPostImplementationAuditPlan,
    UnexpectedCandidate,
    UnexpectedTargetScope,
    UnexpectedEventKind
}

public sealed record DurableAuditTrailAppendOnlyCandidateGateState(
    bool UserExplicitGo,
    bool PreImplementationExternalAuditGo,
    bool ScopeLocked,
    bool NegativeTestsReady,
    bool NoUnresolvedP0P1P2,
    bool IsolatedBoundary,
    bool FailClosedPlanAccepted,
    bool NoSideEffectProofAccepted,
    bool PostImplementationAuditPlanned);

public sealed record DurableAuditTrailAppendOnlyCandidateRequest(
    string CandidateId,
    string TargetScopeId,
    string EventKind,
    string ActorReference,
    string ApprovalReference,
    IReadOnlyList<string> EvidenceReferences,
    DurableAuditTrailAppendOnlyCandidateGateState Gates);

public sealed record DurableAuditTrailAppendOnlyCandidatePreview(
    string CapabilityId,
    string TargetScopeId,
    string EventKind,
    string ActorReference,
    string ApprovalReference,
    IReadOnlyList<string> EvidenceReferences,
    bool AppendWritten,
    bool EventPersisted,
    bool EnablementAllowed,
    bool ContainsRawPayload,
    bool FilesystemOutputAllowed,
    bool NetworkAllowed,
    bool ProductActionAllowed);

public sealed record DurableAuditTrailAppendOnlyCandidateCounts(
    int DurableAuditTrailRealEnabledCount,
    int AppendWriteCount,
    int PersistedEventCount,
    int RuntimeInvocationCount,
    int ExecutionEnabledCount,
    int MutationEnabledCount,
    int ExportEnabledCount,
    int ServiceRegistrationCount,
    int CommandHandlerCount,
    int ProductActionCount,
    int FilesystemOutputCount,
    int DbMigrationCount,
    int NetworkProviderCallCount,
    int BrowserCdpLiveActionCount,
    int WcuOcrLiveActionCount,
    int RecipesExecutionCount,
    int ReleaseCommercialReadyCount);

public sealed record DurableAuditTrailAppendOnlyCandidateResult(
    DurableAuditTrailAppendOnlyCandidateDecision Decision,
    IReadOnlyList<DurableAuditTrailAppendOnlyCandidateBlockReason> BlockReasons,
    DurableAuditTrailAppendOnlyCandidatePreview? Preview,
    DurableAuditTrailAppendOnlyCandidateCounts Counts,
    bool SafeToEnableNow,
    bool RequiresPostImplementationExternalAuditBeforeEnablement,
    bool NoSideEffectProof,
    string EnablementStatus,
    string ReleaseCommercialStatus,
    int RuntimeLiveReadinessPercent,
    int DurableAuditTrailRealReadinessPercent,
    IReadOnlyList<string> ExcludedCapabilities);

public static class DurableAuditTrailAppendOnlyCandidate
{
    public const string CapabilityId = "DURABLE_AUDIT_TRAIL_APPEND_ONLY_MINIMAL";
    public const string ScopeId = "approval.audit-trail.append-only.minimal.v1";
    public const string SupportedEventKind = "approval.reviewed";

    public static DurableAuditTrailAppendOnlyCandidateRequest CreateFixtureRequest() =>
        new(
            CandidateId: CapabilityId,
            TargetScopeId: ScopeId,
            EventKind: SupportedEventKind,
            ActorReference: "human-operator:explicit-go",
            ApprovalReference: "approval-request-durable-audit-candidate-001",
            EvidenceReferences:
            [
                "docs/qa/nodal-os-selected-capability-scope-external-audit-read-only/report.md",
                "docs/qa/nodal-os-selected-capability-implementation-candidate-prep-read-only/report.md"
            ],
            Gates: new DurableAuditTrailAppendOnlyCandidateGateState(
                UserExplicitGo: true,
                PreImplementationExternalAuditGo: true,
                ScopeLocked: true,
                NegativeTestsReady: true,
                NoUnresolvedP0P1P2: true,
                IsolatedBoundary: true,
                FailClosedPlanAccepted: true,
                NoSideEffectProofAccepted: true,
                PostImplementationAuditPlanned: true));

    public static DurableAuditTrailAppendOnlyCandidateResult Evaluate(DurableAuditTrailAppendOnlyCandidateRequest request)
    {
        var reasons = CollectBlockReasons(request);
        if (reasons.Count > 0)
        {
            return new DurableAuditTrailAppendOnlyCandidateResult(
                Decision: DurableAuditTrailAppendOnlyCandidateDecision.Blocked,
                BlockReasons: reasons,
                Preview: null,
                Counts: ZeroCounts(),
                SafeToEnableNow: false,
                RequiresPostImplementationExternalAuditBeforeEnablement: true,
                NoSideEffectProof: true,
                EnablementStatus: "BLOCKED_FAIL_CLOSED_NO_WRITE",
                ReleaseCommercialStatus: "NO_GO",
                RuntimeLiveReadinessPercent: 0,
                DurableAuditTrailRealReadinessPercent: 0,
                ExcludedCapabilities: ExcludedCapabilities());
        }

        return new DurableAuditTrailAppendOnlyCandidateResult(
            Decision: DurableAuditTrailAppendOnlyCandidateDecision.CandidateAcceptedNoWrite,
            BlockReasons: [],
            Preview: new DurableAuditTrailAppendOnlyCandidatePreview(
                CapabilityId: request.CandidateId,
                TargetScopeId: request.TargetScopeId,
                EventKind: request.EventKind,
                ActorReference: request.ActorReference,
                ApprovalReference: request.ApprovalReference,
                EvidenceReferences: request.EvidenceReferences,
                AppendWritten: false,
                EventPersisted: false,
                EnablementAllowed: false,
                ContainsRawPayload: false,
                FilesystemOutputAllowed: false,
                NetworkAllowed: false,
                ProductActionAllowed: false),
            Counts: ZeroCounts(),
            SafeToEnableNow: false,
            RequiresPostImplementationExternalAuditBeforeEnablement: true,
            NoSideEffectProof: true,
            EnablementStatus: "POST_IMPLEMENTATION_EXTERNAL_AUDIT_REQUIRED_BEFORE_ENABLEMENT",
            ReleaseCommercialStatus: "NO_GO",
            RuntimeLiveReadinessPercent: 0,
            DurableAuditTrailRealReadinessPercent: 0,
            ExcludedCapabilities: ExcludedCapabilities());
    }

    private static IReadOnlyList<DurableAuditTrailAppendOnlyCandidateBlockReason> CollectBlockReasons(DurableAuditTrailAppendOnlyCandidateRequest request)
    {
        var reasons = new List<DurableAuditTrailAppendOnlyCandidateBlockReason>();

        if (!string.Equals(request.CandidateId, CapabilityId, StringComparison.Ordinal))
        {
            reasons.Add(DurableAuditTrailAppendOnlyCandidateBlockReason.UnexpectedCandidate);
        }

        if (!string.Equals(request.TargetScopeId, ScopeId, StringComparison.Ordinal))
        {
            reasons.Add(DurableAuditTrailAppendOnlyCandidateBlockReason.UnexpectedTargetScope);
        }

        if (!string.Equals(request.EventKind, SupportedEventKind, StringComparison.Ordinal))
        {
            reasons.Add(DurableAuditTrailAppendOnlyCandidateBlockReason.UnexpectedEventKind);
        }

        if (!request.Gates.UserExplicitGo)
        {
            reasons.Add(DurableAuditTrailAppendOnlyCandidateBlockReason.MissingUserExplicitGo);
        }

        if (!request.Gates.PreImplementationExternalAuditGo)
        {
            reasons.Add(DurableAuditTrailAppendOnlyCandidateBlockReason.MissingPreImplementationExternalAuditGo);
        }

        if (!request.Gates.ScopeLocked)
        {
            reasons.Add(DurableAuditTrailAppendOnlyCandidateBlockReason.MissingScopeLock);
        }

        if (!request.Gates.NegativeTestsReady)
        {
            reasons.Add(DurableAuditTrailAppendOnlyCandidateBlockReason.MissingNegativeTests);
        }

        if (!request.Gates.NoUnresolvedP0P1P2)
        {
            reasons.Add(DurableAuditTrailAppendOnlyCandidateBlockReason.MissingNoUnresolvedBlockingFindingsProof);
        }

        if (!request.Gates.IsolatedBoundary)
        {
            reasons.Add(DurableAuditTrailAppendOnlyCandidateBlockReason.MissingIsolatedBoundary);
        }

        if (!request.Gates.FailClosedPlanAccepted)
        {
            reasons.Add(DurableAuditTrailAppendOnlyCandidateBlockReason.MissingFailClosedPlan);
        }

        if (!request.Gates.NoSideEffectProofAccepted)
        {
            reasons.Add(DurableAuditTrailAppendOnlyCandidateBlockReason.MissingNoSideEffectProof);
        }

        if (!request.Gates.PostImplementationAuditPlanned)
        {
            reasons.Add(DurableAuditTrailAppendOnlyCandidateBlockReason.MissingPostImplementationAuditPlan);
        }

        return reasons;
    }

    private static DurableAuditTrailAppendOnlyCandidateCounts ZeroCounts() =>
        new(
            DurableAuditTrailRealEnabledCount: 0,
            AppendWriteCount: 0,
            PersistedEventCount: 0,
            RuntimeInvocationCount: 0,
            ExecutionEnabledCount: 0,
            MutationEnabledCount: 0,
            ExportEnabledCount: 0,
            ServiceRegistrationCount: 0,
            CommandHandlerCount: 0,
            ProductActionCount: 0,
            FilesystemOutputCount: 0,
            DbMigrationCount: 0,
            NetworkProviderCallCount: 0,
            BrowserCdpLiveActionCount: 0,
            WcuOcrLiveActionCount: 0,
            RecipesExecutionCount: 0,
            ReleaseCommercialReadyCount: 0);

    private static IReadOnlyList<string> ExcludedCapabilities() =>
    [
        "RUNTIME_LIVE",
        "EXECUTION_REAL",
        "MUTATION_REAL",
        "PHYSICAL_EXPORT_REAL",
        "REDACTION_RUNTIME_REAL",
        "RETENTION_DELETION_RUNTIME_REAL",
        "SERVICE_REGISTRATION",
        "COMMAND_HANDLERS",
        "PRODUCT_ACTIONS",
        "FILESYSTEM_PRODUCT_IO",
        "DB_MIGRATION",
        "PROVIDER_CLOUD_NETWORK",
        "BROWSER_CDP_LIVE",
        "WCU_OCR_LIVE",
        "RECIPES_EXECUTION_REAL",
        "RELEASE_COMMERCIAL_READY"
    ];
}
