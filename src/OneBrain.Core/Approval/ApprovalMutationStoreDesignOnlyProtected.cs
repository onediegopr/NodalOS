namespace OneBrain.Core.Approval;

public enum ApprovalMutationStoreDesignStatus
{
    DesignOnly,
    ReadOnly,
    PreviewOnly,
    Blocked,
    FutureProtected,
    NotImplemented
}

public enum ApprovalMutationBlockedReason
{
    NoDurableAuditTrail,
    NoMutationPolicy,
    NoActorIdentityBoundary,
    NoConcurrencyPolicy,
    NoReplayProtection,
    NoInvalidationPolicy,
    NoWriterPolicyBoundary,
    NoRuntimeGate,
    NoStateStore,
    NoExternalAudit,
    NoDatabasePolicy,
    NoFilesystemPolicy
}

public enum ApprovalMutationPreviewKind
{
    ReviewRecordedFuture,
    ApprovedFuture,
    RejectedFuture,
    ChangesRequestedFuture,
    ExpiredFuture,
    InvalidatedFuture,
    SupersededFuture,
    EvidenceAttachedFuture,
    ExecutionEligibilityMarkedFuture
}

public enum ApprovalMutationActorKind
{
    HumanReviewerFuture,
    OwnerFuture,
    AuditorFuture,
    SystemObserverPreview,
    AutomationActorBlocked,
    UnknownBlocked
}

public enum ApprovalMutationInvalidationReason
{
    ContextChanged,
    EvidenceChanged,
    PolicyVersionChanged,
    TargetChanged,
    ActorChanged,
    RiskChanged,
    ApprovalExpired,
    SupersededByNewReview,
    RuntimeGateChanged,
    ExportPolicyChanged
}

public sealed record ApprovalMutationStoreReadiness(
    string DesignReadinessPercentRange,
    int ImplementationReadinessPercent,
    int RuntimeReadinessPercent,
    int DurableStoreReadinessPercent,
    int DatabaseReadinessPercent,
    int FilesystemWriteReadinessPercent)
{
    public bool KeepsImplementationBlocked =>
        ImplementationReadinessPercent == 0
        && RuntimeReadinessPercent == 0
        && DurableStoreReadinessPercent == 0
        && DatabaseReadinessPercent == 0
        && FilesystemWriteReadinessPercent == 0;
}

public sealed record ApprovalMutationStoreCapabilityStatus(
    bool HasRealStore,
    bool HasRepository,
    bool HasDatabase,
    bool HasFilesystemWrite,
    bool HasDurableAuditTrail,
    bool HasRuntime,
    bool HasServiceRegistration,
    bool HasCommandHandler,
    bool CanPersistMutation,
    bool CanReplayMutation,
    bool CanCommitMutation);

public sealed record ApprovalMutationAttemptPreview(
    string AttemptId,
    string ApprovalId,
    ApprovalMutationPreviewKind RequestedTransition,
    string ActorRefPreview,
    IReadOnlyList<string> EvidenceRefs,
    string ContextHashPreview,
    string PolicyVersionPreview,
    string RequestedAtPreview,
    IReadOnlyList<ApprovalMutationBlockedReason> BlockedReasons,
    bool IsPreviewOnly,
    bool CanMutate);

public sealed record ApprovalMutationActorBoundaryDesign(
    string ActorRefPreview,
    ApprovalMutationActorKind ActorKind,
    bool RequiredHumanActor,
    bool ServiceActorAllowed,
    bool AnonymousActorAllowed,
    bool AutomationActorAllowed,
    bool RequiresIdentityProofFuture,
    bool HasIdentityProvider,
    IReadOnlyList<ApprovalMutationBlockedReason> PermissionBlockers);

public sealed record ApprovalMutationRecordPreview(
    string RecordIdPreview,
    ApprovalMutationPreviewKind MutationKind,
    string PreviousStatePreview,
    string ProposedStatePreview,
    IReadOnlyList<string> EvidenceRefs,
    ApprovalMutationActorBoundaryDesign ActorBoundary,
    string AuditTrailRequirement,
    string InvalidationRequirement,
    string ReplayProtectionRequirement,
    bool IsDurable,
    bool IsPersisted);

public sealed record ApprovalStalenessDesign(
    bool ContextHashRequiredFuture,
    bool EvidenceSnapshotRequiredFuture,
    bool PolicyVersionRequiredFuture,
    bool ExpirationRequiredFuture,
    bool TargetFingerprintRequiredFuture,
    bool CurrentContextMatchRequiredFuture,
    bool PerformsContextScanNow,
    bool ComputesFilesystemHashNow);

public sealed record ApprovalInvalidationDesign(
    IReadOnlyList<ApprovalMutationInvalidationReason> InvalidationReasons,
    bool CanInvalidateNow,
    bool UpdatesStateNow);

public sealed record ApprovalSupersedingDesign(
    string SupersededApprovalRefPreview,
    string ReplacementApprovalRefPreview,
    string SupersedingReason,
    bool RequiresAuditTrailFuture,
    bool CanSupersedeNow);

public sealed record ApprovalMutationReplayProtectionDesign(
    bool MutationNonceRequiredFuture,
    bool IdempotencyKeyRequiredFuture,
    bool PreviousEventHashRequiredFuture,
    bool ActorSessionBindingRequiredFuture,
    bool ReplayWindowPolicyRequiredFuture,
    bool CanReplayNow);

public sealed record ApprovalMutationConcurrencyModelDesign(
    bool ExpectedStateRequiredFuture,
    bool CompareAndSwapRequiredFuture,
    bool VersionTokenRequiredFuture,
    bool ConcurrentMutationBlocked,
    bool LastWriterWinsAllowed,
    bool RequiresDurableStoreFuture,
    bool HasLocksNow,
    bool HasTransactionsNow,
    bool HasMutableGlobalStateNow);

public sealed record ApprovalMutationIdempotencyDesign(
    string SameAttemptSameResultFuture,
    string DuplicateAttemptBlockedFuture,
    string ReplayDetectedFuture,
    bool IdempotencyStoreImplemented);

public sealed record ApprovalMutationEvidenceRequirementDesign(
    IReadOnlyList<string> RequiredFutureEvidence,
    IReadOnlyList<string> EvidenceBlockers,
    IReadOnlyList<string> AuditRequirements,
    bool EvidencePersistedNow,
    bool AuditTrailWrittenNow,
    bool RedactionRuntimeAvailable,
    bool ExportAvailable);

public sealed record ApprovalMutationStoreAntiCapabilityProof(
    bool CannotPersistMutation,
    bool CannotCommitMutation,
    bool CannotReplayMutation,
    bool CannotUpdateApprovalState,
    bool CannotWriteAuditTrail,
    bool CannotUseDatabase,
    bool CannotUseFilesystem,
    bool CannotRegisterService,
    bool CannotCreateCommandHandler,
    bool CannotInvokeWriter,
    bool CannotInvokePolicyProductivePath,
    bool CannotStartRuntime,
    bool CannotExportPhysicalFile,
    bool CannotUseClipboardDownload,
    bool CannotUseProviderCloud,
    bool CannotUseLlmLive,
    bool CannotUseDurableMemory,
    bool CannotUseBrowserCdp,
    bool CannotUseWcuOcr,
    bool CannotExecuteRecipe,
    bool CannotClaimReleaseReady,
    ApprovalReviewNoSideEffectProof NoSideEffectProof)
{
    public bool Passes =>
        CannotPersistMutation
        && CannotCommitMutation
        && CannotReplayMutation
        && CannotUpdateApprovalState
        && CannotWriteAuditTrail
        && CannotUseDatabase
        && CannotUseFilesystem
        && CannotRegisterService
        && CannotCreateCommandHandler
        && CannotInvokeWriter
        && CannotInvokePolicyProductivePath
        && CannotStartRuntime
        && CannotExportPhysicalFile
        && CannotUseClipboardDownload
        && CannotUseProviderCloud
        && CannotUseLlmLive
        && CannotUseDurableMemory
        && CannotUseBrowserCdp
        && CannotUseWcuOcr
        && CannotExecuteRecipe
        && CannotClaimReleaseReady
        && NoSideEffectProof.Passes;
}

public sealed record ApprovalMutationStoreFutureImplementationChecklist(
    IReadOnlyList<string> RequiredBeforeRealStore,
    IReadOnlyList<string> RequiredExternalAudits,
    IReadOnlyList<string> RequiredPolicyDecisions,
    IReadOnlyList<string> ExplicitNonGoals);

public sealed record ApprovalMutationStoreDesignOnlyProtected(
    string DesignId,
    string Title,
    ApprovalMutationStoreDesignStatus Status,
    string Mode,
    ApprovalMutationStoreReadiness Readiness,
    ApprovalMutationStoreCapabilityStatus CapabilityStatus,
    IReadOnlyList<ApprovalMutationBlockedReason> BlockedReasons,
    IReadOnlyList<ApprovalMutationAttemptPreview> AttemptPreviews,
    IReadOnlyList<ApprovalMutationRecordPreview> RecordPreviews,
    IReadOnlyList<ApprovalMutationActorBoundaryDesign> ActorBoundaries,
    ApprovalStalenessDesign StalenessDesign,
    ApprovalInvalidationDesign InvalidationDesign,
    ApprovalSupersedingDesign SupersedingDesign,
    ApprovalMutationReplayProtectionDesign ReplayProtectionDesign,
    ApprovalMutationConcurrencyModelDesign ConcurrencyModelDesign,
    ApprovalMutationIdempotencyDesign IdempotencyDesign,
    ApprovalMutationEvidenceRequirementDesign EvidenceRequirementDesign,
    ApprovalMutationStoreAntiCapabilityProof AntiCapabilityProof,
    ApprovalMutationStoreFutureImplementationChecklist FutureImplementationChecklist,
    IReadOnlyList<string> CurrentMutationBoundaryBaseline,
    IReadOnlyList<string> Warnings,
    string NextSafeStep)
{
    public int ProductActionCount => 0;
    public int StateMutationCount => AttemptPreviews.Count(attempt => attempt.CanMutate)
        + RecordPreviews.Count(record => record.IsPersisted || record.IsDurable);
    public int ExportActionCount => EvidenceRequirementDesign.ExportAvailable ? 1 : 0;
    public bool HasRealStore => CapabilityStatus.HasRealStore;
    public bool HasRepository => CapabilityStatus.HasRepository;
    public bool HasDatabase => CapabilityStatus.HasDatabase;
    public bool HasFilesystemWrite => CapabilityStatus.HasFilesystemWrite;
    public bool HasRuntimeLive => CapabilityStatus.HasRuntime;
    public bool HasServiceRegistration => CapabilityStatus.HasServiceRegistration;
    public bool HasCommandHandler => CapabilityStatus.HasCommandHandler;
    public bool CanMutate => AttemptPreviews.Any(attempt => attempt.CanMutate);
    public bool PassesSafetyProof =>
        Readiness.KeepsImplementationBlocked
        && AntiCapabilityProof.Passes
        && !HasRealStore
        && !HasRepository
        && !HasDatabase
        && !HasFilesystemWrite
        && !HasRuntimeLive
        && !HasServiceRegistration
        && !HasCommandHandler
        && ProductActionCount == 0
        && StateMutationCount == 0
        && ExportActionCount == 0;
}

public static class ApprovalMutationStoreDesignOnlyProtectedPresenter
{
    public static ApprovalMutationStoreDesignOnlyProtected CreateFixture() => BuildDefault();

    public static ApprovalMutationStoreDesignOnlyProtected BuildDefault()
    {
        var proof = ApprovalReviewNoSideEffectProof.FixtureReadOnly();
        var actorBoundaries = ActorBoundaries();

        return new ApprovalMutationStoreDesignOnlyProtected(
            DesignId: "nodal-os.approval.mutation-store.design-only.protected.fixture.v1",
            Title: "Approval Mutation Store Design-Only Protected Spec",
            Status: ApprovalMutationStoreDesignStatus.DesignOnly,
            Mode: "DESIGN_ONLY_READ_ONLY_PREVIEW_NO_STORE_NO_MUTATION_NO_RUNTIME",
            Readiness: GetReadiness(),
            CapabilityStatus: CapabilityStatus(),
            BlockedReasons: GetBlockedReasons(),
            AttemptPreviews: AttemptPreviews(),
            RecordPreviews: RecordPreviews(actorBoundaries.Single(boundary => boundary.ActorKind == ApprovalMutationActorKind.HumanReviewerFuture)),
            ActorBoundaries: actorBoundaries,
            StalenessDesign: StalenessDesign(),
            InvalidationDesign: InvalidationDesign(),
            SupersedingDesign: SupersedingDesign(),
            ReplayProtectionDesign: ReplayProtectionDesign(),
            ConcurrencyModelDesign: ConcurrencyModelDesign(),
            IdempotencyDesign: IdempotencyDesign(),
            EvidenceRequirementDesign: EvidenceRequirementDesign(),
            AntiCapabilityProof: GetAntiCapabilityProof(),
            FutureImplementationChecklist: GetFutureImplementationChecklist(),
            CurrentMutationBoundaryBaseline: CurrentMutationBoundaryBaseline(),
            Warnings:
            [
                "Mutation store design readiness may increase. Approval state mutation readiness remains 0%.",
                "Every mutation record is a preview and cannot persist approval state.",
                "No actor, policy gate, evidence reference or runtime gate can unlock a real store in this hito."
            ],
            NextSafeStep: "NODAL_OS_APPROVAL_MUTATION_STORE_DESIGN_EXTERNAL_AUDIT");
    }

    public static ApprovalMutationStoreReadiness GetReadiness() =>
        new(
            DesignReadinessPercentRange: "70-85%",
            ImplementationReadinessPercent: 0,
            RuntimeReadinessPercent: 0,
            DurableStoreReadinessPercent: 0,
            DatabaseReadinessPercent: 0,
            FilesystemWriteReadinessPercent: 0);

    public static IReadOnlyList<ApprovalMutationBlockedReason> GetBlockedReasons() =>
        Enum.GetValues<ApprovalMutationBlockedReason>();

    public static ApprovalMutationStoreAntiCapabilityProof GetAntiCapabilityProof()
    {
        var proof = ApprovalReviewNoSideEffectProof.FixtureReadOnly();

        return new ApprovalMutationStoreAntiCapabilityProof(
            CannotPersistMutation: true,
            CannotCommitMutation: true,
            CannotReplayMutation: true,
            CannotUpdateApprovalState: true,
            CannotWriteAuditTrail: true,
            CannotUseDatabase: true,
            CannotUseFilesystem: true,
            CannotRegisterService: true,
            CannotCreateCommandHandler: true,
            CannotInvokeWriter: true,
            CannotInvokePolicyProductivePath: true,
            CannotStartRuntime: true,
            CannotExportPhysicalFile: true,
            CannotUseClipboardDownload: true,
            CannotUseProviderCloud: true,
            CannotUseLlmLive: true,
            CannotUseDurableMemory: true,
            CannotUseBrowserCdp: true,
            CannotUseWcuOcr: true,
            CannotExecuteRecipe: true,
            CannotClaimReleaseReady: true,
            NoSideEffectProof: proof);
    }

    public static ApprovalMutationStoreFutureImplementationChecklist GetFutureImplementationChecklist() =>
        new(
            RequiredBeforeRealStore:
            [
                "durable audit trail design external audit",
                "mutation policy formal approval",
                "actor identity and authority model",
                "concurrency and replay protection model",
                "invalidation and superseding policy",
                "writer/policy boundary external audit",
                "cross-phase runtime gate external audit",
                "state storage architecture and migration review",
                "security review with explicit implementation approval"
            ],
            RequiredExternalAudits:
            [
                "mutation store design external audit",
                "durable audit trail design external audit",
                "writer/policy integration boundary audit",
                "runtime readiness gate audit",
                "release/commercial claim audit"
            ],
            RequiredPolicyDecisions:
            [
                "who can request mutation",
                "which evidence refs are mandatory",
                "how stale approvals are invalidated",
                "how replay attempts are blocked",
                "how concurrent mutations are rejected",
                "how redaction and retention apply"
            ],
            ExplicitNonGoals:
            [
                "no real store",
                "no repository",
                "no database",
                "no filesystem product IO",
                "no durable audit trail implementation",
                "no runtime/live",
                "no approval execution",
                "no product action controls",
                "no release/commercial readiness"
            ]);

    private static ApprovalMutationStoreCapabilityStatus CapabilityStatus() =>
        new(
            HasRealStore: false,
            HasRepository: false,
            HasDatabase: false,
            HasFilesystemWrite: false,
            HasDurableAuditTrail: false,
            HasRuntime: false,
            HasServiceRegistration: false,
            HasCommandHandler: false,
            CanPersistMutation: false,
            CanReplayMutation: false,
            CanCommitMutation: false);

    private static IReadOnlyList<ApprovalMutationAttemptPreview> AttemptPreviews() =>
        Enum.GetValues<ApprovalMutationPreviewKind>()
            .Select(kind => new ApprovalMutationAttemptPreview(
                AttemptId: $"mutation-attempt-preview.{kind.ToString().ToLowerInvariant()}",
                ApprovalId: "approval-preview.future",
                RequestedTransition: kind,
                ActorRefPreview: "actor-preview.human-reviewer.future",
                EvidenceRefs: ["approval-packet-ref.future", "human-review-packet-ref.future", "policy-decision-ref.future"],
                ContextHashPreview: "context-hash-preview.not-computed",
                PolicyVersionPreview: "policy-version-preview.future",
                RequestedAtPreview: "timestamp-preview.not-runtime",
                BlockedReasons: GetBlockedReasons(),
                IsPreviewOnly: true,
                CanMutate: false))
            .ToList();

    private static IReadOnlyList<ApprovalMutationRecordPreview> RecordPreviews(ApprovalMutationActorBoundaryDesign actorBoundary) =>
        Enum.GetValues<ApprovalMutationPreviewKind>()
            .Select(kind => new ApprovalMutationRecordPreview(
                RecordIdPreview: $"mutation-record-preview.{kind.ToString().ToLowerInvariant()}",
                MutationKind: kind,
                PreviousStatePreview: "previous-approval-state-preview",
                ProposedStatePreview: $"proposed-{kind.ToString().ToLowerInvariant()}",
                EvidenceRefs: ["approval-packet-ref.future", "context-fingerprint-ref.future", "actor-proof-ref.future"],
                ActorBoundary: actorBoundary,
                AuditTrailRequirement: "durable audit trail required before any real mutation",
                InvalidationRequirement: "stale, superseded and changed context checks required before mutation",
                ReplayProtectionRequirement: "nonce, idempotency key and previous event hash required future",
                IsDurable: false,
                IsPersisted: false))
            .ToList();

    private static IReadOnlyList<ApprovalMutationActorBoundaryDesign> ActorBoundaries() =>
    [
        ActorBoundary("actor-preview.human-reviewer.future", ApprovalMutationActorKind.HumanReviewerFuture, requiredHumanActor: true, serviceActorAllowed: false, anonymousActorAllowed: false, automationActorAllowed: false),
        ActorBoundary("actor-preview.owner.future", ApprovalMutationActorKind.OwnerFuture, requiredHumanActor: true, serviceActorAllowed: false, anonymousActorAllowed: false, automationActorAllowed: false),
        ActorBoundary("actor-preview.auditor.future", ApprovalMutationActorKind.AuditorFuture, requiredHumanActor: true, serviceActorAllowed: false, anonymousActorAllowed: false, automationActorAllowed: false),
        ActorBoundary("actor-preview.system-observer.preview", ApprovalMutationActorKind.SystemObserverPreview, requiredHumanActor: false, serviceActorAllowed: false, anonymousActorAllowed: false, automationActorAllowed: false),
        ActorBoundary("actor-preview.automation.blocked", ApprovalMutationActorKind.AutomationActorBlocked, requiredHumanActor: true, serviceActorAllowed: false, anonymousActorAllowed: false, automationActorAllowed: false),
        ActorBoundary("actor-preview.unknown.blocked", ApprovalMutationActorKind.UnknownBlocked, requiredHumanActor: true, serviceActorAllowed: false, anonymousActorAllowed: false, automationActorAllowed: false)
    ];

    private static ApprovalMutationActorBoundaryDesign ActorBoundary(
        string actorRefPreview,
        ApprovalMutationActorKind kind,
        bool requiredHumanActor,
        bool serviceActorAllowed,
        bool anonymousActorAllowed,
        bool automationActorAllowed) =>
        new(
            ActorRefPreview: actorRefPreview,
            ActorKind: kind,
            RequiredHumanActor: requiredHumanActor,
            ServiceActorAllowed: serviceActorAllowed,
            AnonymousActorAllowed: anonymousActorAllowed,
            AutomationActorAllowed: automationActorAllowed,
            RequiresIdentityProofFuture: true,
            HasIdentityProvider: false,
            PermissionBlockers:
            [
                ApprovalMutationBlockedReason.NoActorIdentityBoundary,
                ApprovalMutationBlockedReason.NoMutationPolicy,
                ApprovalMutationBlockedReason.NoDurableAuditTrail,
                ApprovalMutationBlockedReason.NoReplayProtection
            ]);

    private static ApprovalStalenessDesign StalenessDesign() =>
        new(
            ContextHashRequiredFuture: true,
            EvidenceSnapshotRequiredFuture: true,
            PolicyVersionRequiredFuture: true,
            ExpirationRequiredFuture: true,
            TargetFingerprintRequiredFuture: true,
            CurrentContextMatchRequiredFuture: true,
            PerformsContextScanNow: false,
            ComputesFilesystemHashNow: false);

    private static ApprovalInvalidationDesign InvalidationDesign() =>
        new(
            InvalidationReasons: Enum.GetValues<ApprovalMutationInvalidationReason>(),
            CanInvalidateNow: false,
            UpdatesStateNow: false);

    private static ApprovalSupersedingDesign SupersedingDesign() =>
        new(
            SupersededApprovalRefPreview: "approval-preview.superseded.future",
            ReplacementApprovalRefPreview: "approval-preview.replacement.future",
            SupersedingReason: "future review supersedes stale approval only after durable audit trail exists",
            RequiresAuditTrailFuture: true,
            CanSupersedeNow: false);

    private static ApprovalMutationReplayProtectionDesign ReplayProtectionDesign() =>
        new(
            MutationNonceRequiredFuture: true,
            IdempotencyKeyRequiredFuture: true,
            PreviousEventHashRequiredFuture: true,
            ActorSessionBindingRequiredFuture: true,
            ReplayWindowPolicyRequiredFuture: true,
            CanReplayNow: false);

    private static ApprovalMutationConcurrencyModelDesign ConcurrencyModelDesign() =>
        new(
            ExpectedStateRequiredFuture: true,
            CompareAndSwapRequiredFuture: true,
            VersionTokenRequiredFuture: true,
            ConcurrentMutationBlocked: true,
            LastWriterWinsAllowed: false,
            RequiresDurableStoreFuture: true,
            HasLocksNow: false,
            HasTransactionsNow: false,
            HasMutableGlobalStateNow: false);

    private static ApprovalMutationIdempotencyDesign IdempotencyDesign() =>
        new(
            SameAttemptSameResultFuture: "same future mutation attempt must produce same blocked or accepted result",
            DuplicateAttemptBlockedFuture: "duplicate future mutation attempts require durable idempotency proof",
            ReplayDetectedFuture: "replay attempts require durable detection before mutation can exist",
            IdempotencyStoreImplemented: false);

    private static ApprovalMutationEvidenceRequirementDesign EvidenceRequirementDesign() =>
        new(
            RequiredFutureEvidence:
            [
                "original approval packet",
                "human review packet",
                "risk summary",
                "policy decision",
                "actor identity proof",
                "context fingerprint",
                "target fingerprint",
                "evidence snapshot",
                "stale check",
                "invalidation check",
                "runtime readiness gate",
                "export policy if export-related",
                "writer/policy boundary if execution-related"
            ],
            EvidenceBlockers:
            [
                "MissingApprovalPacket",
                "MissingHumanReview",
                "MissingPolicyDecision",
                "MissingActorProof",
                "MissingContextFingerprint",
                "MissingTargetFingerprint",
                "MissingStaleCheck",
                "MissingDurableAuditTrail"
            ],
            AuditRequirements:
            [
                "AppendOnlyFuture",
                "RedactedPayloadFuture",
                "EvidenceRefsOnlyFuture",
                "HashChainFuture",
                "ReplayProtectionFuture",
                "RetentionPolicyFuture"
            ],
            EvidencePersistedNow: false,
            AuditTrailWrittenNow: false,
            RedactionRuntimeAvailable: false,
            ExportAvailable: false);

    private static IReadOnlyList<string> CurrentMutationBoundaryBaseline() =>
    [
        "ControlledExecutionReadinessDesignTrack already models mutation boundary candidates as blocked.",
        "ApprovalExecutionDesignOnlyProtected keeps approval state mutation readiness at 0%.",
        "No repository, DB, durable audit trail, filesystem product IO, service registration or command handler exists.",
        "Durable audit trail remains design-only and is required before any future mutation store can exist.",
        "Writer/policy productive path remains disconnected and cannot turn approval into execution."
    ];
}
