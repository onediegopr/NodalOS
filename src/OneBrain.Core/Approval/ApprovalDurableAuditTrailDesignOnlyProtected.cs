namespace OneBrain.Core.Approval;

public enum ApprovalAuditTrailDesignStatus
{
    DesignOnly,
    ReadOnly,
    PreviewOnly,
    Blocked,
    FutureProtected,
    NotImplemented
}

public enum ApprovalAuditTrailBlockedReason
{
    NoDurableStore,
    NoAppendOnlyLedger,
    NoAuditRepository,
    NoDatabasePolicy,
    NoFilesystemPolicy,
    NoHashChainImplementation,
    NoReplayProtectionImplementation,
    NoRedactionRuntime,
    NoRetentionPolicy,
    NoDeletionPolicy,
    NoActorIdentityModel,
    NoPolicyVersioningContract,
    NoExternalAuditApproval,
    NoRuntimeGateApproval
}

public enum ApprovalAuditEventKind
{
    ApprovalProposedPreview,
    HumanReviewOpenedPreview,
    HumanReviewedPreview,
    MutationAttemptedFuture,
    MutationBlockedFuture,
    MutationAcceptedFuture,
    ApprovalInvalidatedFuture,
    ApprovalSupersededFuture,
    PolicyEvaluatedFuture,
    RuntimeGateEvaluatedFuture,
    ExportRequestedFuture,
    ExportBlockedFuture,
    RollbackRequiredFuture,
    ExternalAuditRequestedFuture,
    ExternalAuditCompletedFuture
}

public enum ApprovalAuditRetentionClass
{
    ApprovalPacketFuture,
    HumanReviewFuture,
    MutationAttemptFuture,
    PolicyDecisionFuture,
    RuntimeGateFuture,
    ExportRequestFuture,
    ExternalAuditFuture
}

public sealed record ApprovalAuditTrailReadiness(
    string DesignReadinessPercentRange,
    int ImplementationReadinessPercent,
    int DurableStoreReadinessPercent,
    int AppendOnlyLedgerReadinessPercent,
    int DatabaseReadinessPercent,
    int FilesystemReadinessPercent,
    int HashChainImplementationReadinessPercent,
    int RedactionRuntimeReadinessPercent,
    int RetentionWorkflowReadinessPercent,
    int DeletionWorkflowReadinessPercent,
    int RuntimeLiveReadinessPercent,
    bool ReleaseCommercialReady)
{
    public bool KeepsImplementationBlocked =>
        ImplementationReadinessPercent == 0
        && DurableStoreReadinessPercent == 0
        && AppendOnlyLedgerReadinessPercent == 0
        && DatabaseReadinessPercent == 0
        && FilesystemReadinessPercent == 0
        && HashChainImplementationReadinessPercent == 0
        && RedactionRuntimeReadinessPercent == 0
        && RetentionWorkflowReadinessPercent == 0
        && DeletionWorkflowReadinessPercent == 0
        && RuntimeLiveReadinessPercent == 0
        && !ReleaseCommercialReady;
}

public sealed record ApprovalAuditTrailCapabilityStatus(
    bool HasDurableAuditTrail,
    bool HasAppendOnlyLedger,
    bool HasAuditRepository,
    bool HasDatabase,
    bool HasFilesystemWrite,
    bool HasFileHashing,
    bool HasRuntime,
    bool HasServiceRegistration,
    bool HasCommandHandler,
    bool CanAppendAuditEvent,
    bool CanPersistAuditTrail,
    bool CanReadWorkspaceFiles,
    bool CanHashRealFiles,
    bool CanReplayAuditTrail,
    bool CanExportAuditTrail);

public sealed record ApprovalAuditEventPreview(
    string EventIdPreview,
    ApprovalAuditEventKind EventKind,
    string ActorRefPreview,
    string ApprovalRefPreview,
    string MutationAttemptRefPreview,
    IReadOnlyList<string> EvidenceRefs,
    string PolicyVersionPreview,
    IReadOnlyList<string> TargetRefsPreview,
    string ContextFingerprintFuture,
    string PreviousEventHashFuture,
    string RedactionStatusFuture,
    ApprovalAuditRetentionClass RetentionClassFuture,
    bool IsPreviewOnly,
    bool IsPersisted,
    bool IsDurable,
    bool CanAppend);

public sealed record ApprovalAuditEventFieldRequirement(
    string FieldName,
    IReadOnlyList<ApprovalAuditEventKind> RequiredForEventKinds,
    bool IsImplementedNow,
    bool IsPreviewOnly,
    bool RequiredBeforeRealMutation,
    bool RequiredBeforeRealExecution,
    bool RequiredBeforePhysicalExport);

public sealed record ApprovalAuditTrailRedactionRequirement(
    bool PayloadMustBeRedactedFuture,
    bool EvidenceRefsOnlyFuture,
    bool SecretScanRequiredFuture,
    bool PiiPolicyRequiredFuture,
    bool RedactionRuntimeImplemented,
    bool CanStoreRawPayload,
    bool PrivacyExportAvailable);

public sealed record ApprovalAuditTrailRetentionRequirement(
    ApprovalAuditRetentionClass RetentionClass,
    bool RetentionPolicyRequiredFuture,
    bool WorkspacePolicyRequiredFuture,
    bool UserConsentRequiredFuture,
    bool RetentionStoreImplemented);

public sealed record ApprovalAuditTrailDeletionRequirement(
    bool DeletionPolicyRequiredFuture,
    bool DeletionEligibilityRequiredFuture,
    bool AuditTombstoneFuture,
    bool LegalHoldFuture,
    bool DeletionWorkflowImplemented);

public sealed record ApprovalAuditTrailHashChainDesign(
    bool PreviousEventHashRequiredFuture,
    bool EventHashRequiredFuture,
    bool ChainValidationRequiredFuture,
    bool TamperEvidenceRequiredFuture,
    bool HashAlgorithmPolicyRequiredFuture,
    bool HashChainImplemented,
    bool CanHashRealFiles,
    bool CanHashRealEvents);

public sealed record ApprovalAuditTrailReplayProtectionDesign(
    bool ReplayNonceRequiredFuture,
    bool IdempotencyKeyRequiredFuture,
    bool ActorSessionBindingRequiredFuture,
    bool ReplayWindowPolicyRequiredFuture,
    bool DuplicateEventDetectionFuture,
    bool ReplayProtectionImplemented,
    bool CanDetectReplayNow,
    bool IdempotencyStoreImplemented);

public sealed record ApprovalAuditTrailExternalAuditRequirement(
    string Scope,
    bool RequiredBeforeImplementation,
    bool RequiredBeforeMutationStore,
    bool RequiredBeforeRuntime,
    bool RequiredBeforePhysicalExport,
    IReadOnlyList<string> RequiredEvidence,
    IReadOnlyList<string> RequiredNegativeCapabilityProof,
    IReadOnlyList<string> RequiredValidationSuites,
    IReadOnlyList<string> RequiredNoGoChecks,
    bool ExternalProviderEnabled,
    bool NetworkEnabled,
    bool ServiceRegistered);

public sealed record ApprovalAuditTrailAntiCapabilityProof(
    bool CannotAppendAuditEvent,
    bool CannotPersistAuditTrail,
    bool CannotUseAppendOnlyLedger,
    bool CannotUseAuditRepository,
    bool CannotUseDatabase,
    bool CannotUseFilesystem,
    bool CannotReadWorkspaceFiles,
    bool CannotHashRealFiles,
    bool CannotHashRealEvents,
    bool CannotRunRedactionRuntime,
    bool CannotRunRetentionWorkflow,
    bool CannotRunDeletionWorkflow,
    bool CannotRegisterService,
    bool CannotCreateCommandHandler,
    bool CannotMutateApprovalState,
    bool CannotExecuteApproval,
    bool CannotInvokeWriter,
    bool CannotInvokePolicyProductivePath,
    bool CannotStartRuntime,
    bool CannotExportPhysicalFile,
    bool CannotUseClipboardDownload,
    bool CannotUseProviderCloud,
    bool CannotUseLlmLive,
    bool CannotUseVectorBackend,
    bool CannotUseDurableMemory,
    bool CannotUseBrowserCdp,
    bool CannotUseWcuOcr,
    bool CannotExecuteRecipe,
    bool CannotClaimReleaseReady,
    ApprovalReviewNoSideEffectProof NoSideEffectProof)
{
    public bool Passes =>
        CannotAppendAuditEvent
        && CannotPersistAuditTrail
        && CannotUseAppendOnlyLedger
        && CannotUseAuditRepository
        && CannotUseDatabase
        && CannotUseFilesystem
        && CannotReadWorkspaceFiles
        && CannotHashRealFiles
        && CannotHashRealEvents
        && CannotRunRedactionRuntime
        && CannotRunRetentionWorkflow
        && CannotRunDeletionWorkflow
        && CannotRegisterService
        && CannotCreateCommandHandler
        && CannotMutateApprovalState
        && CannotExecuteApproval
        && CannotInvokeWriter
        && CannotInvokePolicyProductivePath
        && CannotStartRuntime
        && CannotExportPhysicalFile
        && CannotUseClipboardDownload
        && CannotUseProviderCloud
        && CannotUseLlmLive
        && CannotUseVectorBackend
        && CannotUseDurableMemory
        && CannotUseBrowserCdp
        && CannotUseWcuOcr
        && CannotExecuteRecipe
        && CannotClaimReleaseReady
        && NoSideEffectProof.Passes;
}

public sealed record ApprovalAuditTrailFutureImplementationChecklist(
    IReadOnlyList<string> RequiredBeforeRealAuditTrail,
    IReadOnlyList<string> RequiredExternalAudits,
    IReadOnlyList<string> RequiredPolicyDecisions,
    IReadOnlyList<string> ExplicitNonGoals);

public sealed record ApprovalDurableAuditTrailDesignOnlyProtected(
    string DesignId,
    string Title,
    ApprovalAuditTrailDesignStatus Status,
    string Mode,
    ApprovalAuditTrailReadiness Readiness,
    ApprovalAuditTrailCapabilityStatus CapabilityStatus,
    IReadOnlyList<ApprovalAuditTrailBlockedReason> BlockedReasons,
    IReadOnlyList<ApprovalAuditEventPreview> EventPreviews,
    IReadOnlyList<ApprovalAuditEventFieldRequirement> FieldRequirements,
    ApprovalAuditTrailRedactionRequirement RedactionRequirement,
    IReadOnlyList<ApprovalAuditTrailRetentionRequirement> RetentionRequirements,
    ApprovalAuditTrailDeletionRequirement DeletionRequirement,
    ApprovalAuditTrailHashChainDesign HashChainDesign,
    ApprovalAuditTrailReplayProtectionDesign ReplayProtectionDesign,
    IReadOnlyList<ApprovalAuditTrailExternalAuditRequirement> ExternalAuditRequirements,
    ApprovalAuditTrailAntiCapabilityProof AntiCapabilityProof,
    ApprovalAuditTrailFutureImplementationChecklist FutureImplementationChecklist,
    IReadOnlyList<string> CurrentApprovalAuditTrailBaseline,
    IReadOnlyList<string> Warnings,
    string NextSafeStep)
{
    public int ProductActionCount => 0;
    public int StateMutationCount => 0;
    public int AuditAppendCount => EventPreviews.Count(auditEvent => auditEvent.CanAppend);
    public int PersistedEventCount => EventPreviews.Count(auditEvent => auditEvent.IsPersisted || auditEvent.IsDurable);
    public int ExportActionCount => CapabilityStatus.CanExportAuditTrail || RedactionRequirement.PrivacyExportAvailable ? 1 : 0;
    public bool HasDurableAuditTrail => CapabilityStatus.HasDurableAuditTrail;
    public bool HasAppendOnlyLedger => CapabilityStatus.HasAppendOnlyLedger;
    public bool HasAuditRepository => CapabilityStatus.HasAuditRepository;
    public bool HasDatabase => CapabilityStatus.HasDatabase;
    public bool HasFilesystemWrite => CapabilityStatus.HasFilesystemWrite;
    public bool HasFileHashing => CapabilityStatus.HasFileHashing;
    public bool HasRuntimeLive => CapabilityStatus.HasRuntime;
    public bool HasServiceRegistration => CapabilityStatus.HasServiceRegistration;
    public bool HasCommandHandler => CapabilityStatus.HasCommandHandler;
    public bool PassesSafetyProof =>
        Readiness.KeepsImplementationBlocked
        && AntiCapabilityProof.Passes
        && !HasDurableAuditTrail
        && !HasAppendOnlyLedger
        && !HasAuditRepository
        && !HasDatabase
        && !HasFilesystemWrite
        && !HasFileHashing
        && !HasRuntimeLive
        && !HasServiceRegistration
        && !HasCommandHandler
        && ProductActionCount == 0
        && StateMutationCount == 0
        && AuditAppendCount == 0
        && PersistedEventCount == 0
        && ExportActionCount == 0;
}

public static class ApprovalDurableAuditTrailDesignOnlyProtectedPresenter
{
    public static ApprovalDurableAuditTrailDesignOnlyProtected CreateFixture() => BuildDefault();

    public static ApprovalDurableAuditTrailDesignOnlyProtected BuildDefault() =>
        new(
            DesignId: "nodal-os.approval.durable-audit-trail.design-only.protected.fixture.v1",
            Title: "Durable Approval Audit Trail Design-Only Protected Spec",
            Status: ApprovalAuditTrailDesignStatus.DesignOnly,
            Mode: "DESIGN_ONLY_READ_ONLY_PREVIEW_NO_LEDGER_NO_STORAGE_NO_RUNTIME",
            Readiness: GetReadiness(),
            CapabilityStatus: GetCapabilityStatus(),
            BlockedReasons: GetBlockedReasons(),
            EventPreviews: GetEventPreviews(),
            FieldRequirements: GetFieldRequirements(),
            RedactionRequirement: GetRedactionRequirements(),
            RetentionRequirements: GetRetentionRequirements(),
            DeletionRequirement: GetDeletionRequirements(),
            HashChainDesign: GetHashChainDesign(),
            ReplayProtectionDesign: GetReplayProtectionDesign(),
            ExternalAuditRequirements: GetExternalAuditRequirements(),
            AntiCapabilityProof: GetAntiCapabilityProof(),
            FutureImplementationChecklist: GetFutureImplementationChecklist(),
            CurrentApprovalAuditTrailBaseline: CurrentApprovalAuditTrailBaseline(),
            Warnings:
            [
                "Durable audit trail design readiness may increase. Durable audit trail implementation readiness remains 0%.",
                "All audit events are previews and cannot be appended, persisted, replayed or exported.",
                "Hash-chain, replay protection, retention and deletion are future requirements only."
            ],
            NextSafeStep: "NODAL_OS_DURABLE_APPROVAL_AUDIT_TRAIL_DESIGN_EXTERNAL_AUDIT");

    public static ApprovalAuditTrailReadiness GetReadiness() =>
        new(
            DesignReadinessPercentRange: "70-85%",
            ImplementationReadinessPercent: 0,
            DurableStoreReadinessPercent: 0,
            AppendOnlyLedgerReadinessPercent: 0,
            DatabaseReadinessPercent: 0,
            FilesystemReadinessPercent: 0,
            HashChainImplementationReadinessPercent: 0,
            RedactionRuntimeReadinessPercent: 0,
            RetentionWorkflowReadinessPercent: 0,
            DeletionWorkflowReadinessPercent: 0,
            RuntimeLiveReadinessPercent: 0,
            ReleaseCommercialReady: false);

    public static ApprovalAuditTrailCapabilityStatus GetCapabilityStatus() =>
        new(
            HasDurableAuditTrail: false,
            HasAppendOnlyLedger: false,
            HasAuditRepository: false,
            HasDatabase: false,
            HasFilesystemWrite: false,
            HasFileHashing: false,
            HasRuntime: false,
            HasServiceRegistration: false,
            HasCommandHandler: false,
            CanAppendAuditEvent: false,
            CanPersistAuditTrail: false,
            CanReadWorkspaceFiles: false,
            CanHashRealFiles: false,
            CanReplayAuditTrail: false,
            CanExportAuditTrail: false);

    public static IReadOnlyList<ApprovalAuditTrailBlockedReason> GetBlockedReasons() =>
        Enum.GetValues<ApprovalAuditTrailBlockedReason>();

    public static IReadOnlyList<ApprovalAuditEventPreview> GetEventPreviews() =>
        Enum.GetValues<ApprovalAuditEventKind>()
            .Select(kind => new ApprovalAuditEventPreview(
                EventIdPreview: $"audit-event-preview.{kind.ToString().ToLowerInvariant()}",
                EventKind: kind,
                ActorRefPreview: "actor-ref-preview.future",
                ApprovalRefPreview: "approval-ref-preview.future",
                MutationAttemptRefPreview: kind.ToString().Contains("Mutation", StringComparison.Ordinal)
                    ? "mutation-attempt-ref-preview.future"
                    : "mutation-attempt-ref-preview.not-applicable",
                EvidenceRefs: ["approval-packet-ref.future", "human-review-ref.future", "policy-decision-ref.future"],
                PolicyVersionPreview: "policy-version-preview.future",
                TargetRefsPreview: ["target-ref-preview.future"],
                ContextFingerprintFuture: "context-fingerprint.future.not-computed",
                PreviousEventHashFuture: "previous-event-hash.future.not-computed",
                RedactionStatusFuture: "redaction-status.future.not-runtime",
                RetentionClassFuture: RetentionClassFor(kind),
                IsPreviewOnly: true,
                IsPersisted: false,
                IsDurable: false,
                CanAppend: false))
            .ToList();

    public static IReadOnlyList<ApprovalAuditEventFieldRequirement> GetFieldRequirements()
    {
        var allKinds = Enum.GetValues<ApprovalAuditEventKind>();
        var mutationKinds = allKinds.Where(kind => kind.ToString().Contains("Mutation", StringComparison.Ordinal)).ToList();
        var exportKinds = allKinds.Where(kind => kind.ToString().Contains("Export", StringComparison.Ordinal)).ToList();

        return
        [
            Field("EventIdPreview", allKinds, mutation: true, execution: true, export: true),
            Field("ActorRefPreview", allKinds, mutation: true, execution: true, export: true),
            Field("HumanReviewSessionRefFuture", [ApprovalAuditEventKind.HumanReviewOpenedPreview, ApprovalAuditEventKind.HumanReviewedPreview], mutation: true, execution: true, export: false),
            Field("ApprovalIdPreview", allKinds, mutation: true, execution: true, export: true),
            Field("MutationAttemptIdFuture", mutationKinds, mutation: true, execution: false, export: false),
            Field("EvidenceRefs", allKinds, mutation: true, execution: true, export: true),
            Field("PolicyVersionPreview", allKinds, mutation: true, execution: true, export: true),
            Field("PolicyDecisionRefFuture", [ApprovalAuditEventKind.PolicyEvaluatedFuture, ApprovalAuditEventKind.MutationAcceptedFuture, ApprovalAuditEventKind.RuntimeGateEvaluatedFuture], mutation: true, execution: true, export: false),
            Field("TargetRefsPreview", allKinds, mutation: true, execution: true, export: true),
            Field("ContextFingerprintFuture", allKinds, mutation: true, execution: true, export: false),
            Field("TargetFingerprintFuture", allKinds, mutation: true, execution: true, export: true),
            Field("PreviousEventHashFuture", allKinds, mutation: true, execution: true, export: true),
            Field("EventHashFuture", allKinds, mutation: true, execution: true, export: true),
            Field("RedactionStatusFuture", allKinds, mutation: false, execution: false, export: true),
            Field("RetentionClassFuture", allKinds, mutation: true, execution: true, export: true),
            Field("DeletionEligibilityFuture", allKinds, mutation: false, execution: false, export: true),
            Field("ReplayNonceFuture", allKinds, mutation: true, execution: true, export: true),
            Field("IdempotencyKeyFuture", allKinds, mutation: true, execution: true, export: true),
            Field("RuntimeGateSnapshotFuture", [ApprovalAuditEventKind.RuntimeGateEvaluatedFuture], mutation: false, execution: true, export: false),
            Field("ExportPolicySnapshotFuture", exportKinds, mutation: false, execution: false, export: true)
        ];
    }

    public static ApprovalAuditTrailRedactionRequirement GetRedactionRequirements() =>
        new(
            PayloadMustBeRedactedFuture: true,
            EvidenceRefsOnlyFuture: true,
            SecretScanRequiredFuture: true,
            PiiPolicyRequiredFuture: true,
            RedactionRuntimeImplemented: false,
            CanStoreRawPayload: false,
            PrivacyExportAvailable: false);

    public static IReadOnlyList<ApprovalAuditTrailRetentionRequirement> GetRetentionRequirements() =>
        Enum.GetValues<ApprovalAuditRetentionClass>()
            .Select(retentionClass => new ApprovalAuditTrailRetentionRequirement(
                RetentionClass: retentionClass,
                RetentionPolicyRequiredFuture: true,
                WorkspacePolicyRequiredFuture: true,
                UserConsentRequiredFuture: true,
                RetentionStoreImplemented: false))
            .ToList();

    public static ApprovalAuditTrailDeletionRequirement GetDeletionRequirements() =>
        new(
            DeletionPolicyRequiredFuture: true,
            DeletionEligibilityRequiredFuture: true,
            AuditTombstoneFuture: true,
            LegalHoldFuture: true,
            DeletionWorkflowImplemented: false);

    public static ApprovalAuditTrailHashChainDesign GetHashChainDesign() =>
        new(
            PreviousEventHashRequiredFuture: true,
            EventHashRequiredFuture: true,
            ChainValidationRequiredFuture: true,
            TamperEvidenceRequiredFuture: true,
            HashAlgorithmPolicyRequiredFuture: true,
            HashChainImplemented: false,
            CanHashRealFiles: false,
            CanHashRealEvents: false);

    public static ApprovalAuditTrailReplayProtectionDesign GetReplayProtectionDesign() =>
        new(
            ReplayNonceRequiredFuture: true,
            IdempotencyKeyRequiredFuture: true,
            ActorSessionBindingRequiredFuture: true,
            ReplayWindowPolicyRequiredFuture: true,
            DuplicateEventDetectionFuture: true,
            ReplayProtectionImplemented: false,
            CanDetectReplayNow: false,
            IdempotencyStoreImplemented: false);

    public static IReadOnlyList<ApprovalAuditTrailExternalAuditRequirement> GetExternalAuditRequirements() =>
    [
        ExternalAudit(
            "event types and field requirements",
            ["event preview list", "field requirement list", "event-to-field coverage proof"],
            ["all events preview-only", "all fields not implemented now"]),
        ExternalAudit(
            "redaction retention deletion",
            ["redaction requirement", "retention requirement", "deletion requirement"],
            ["redaction runtime false", "retention store false", "deletion workflow false"]),
        ExternalAudit(
            "hash-chain replay and storage boundary",
            ["hash-chain future design", "replay protection future design", "storage blockers"],
            ["no real hash", "no event persistence", "no DB/filesystem"]),
        ExternalAudit(
            "mutation execution runtime and export boundary",
            ["mutation store linkage", "runtime gate linkage", "physical export linkage"],
            ["no mutation", "no execution", "no runtime", "no physical export"])
    ];

    public static ApprovalAuditTrailAntiCapabilityProof GetAntiCapabilityProof()
    {
        var proof = ApprovalReviewNoSideEffectProof.FixtureReadOnly();

        return new ApprovalAuditTrailAntiCapabilityProof(
            CannotAppendAuditEvent: true,
            CannotPersistAuditTrail: true,
            CannotUseAppendOnlyLedger: true,
            CannotUseAuditRepository: true,
            CannotUseDatabase: true,
            CannotUseFilesystem: true,
            CannotReadWorkspaceFiles: true,
            CannotHashRealFiles: true,
            CannotHashRealEvents: true,
            CannotRunRedactionRuntime: true,
            CannotRunRetentionWorkflow: true,
            CannotRunDeletionWorkflow: true,
            CannotRegisterService: true,
            CannotCreateCommandHandler: true,
            CannotMutateApprovalState: true,
            CannotExecuteApproval: true,
            CannotInvokeWriter: true,
            CannotInvokePolicyProductivePath: true,
            CannotStartRuntime: true,
            CannotExportPhysicalFile: true,
            CannotUseClipboardDownload: true,
            CannotUseProviderCloud: true,
            CannotUseLlmLive: true,
            CannotUseVectorBackend: true,
            CannotUseDurableMemory: true,
            CannotUseBrowserCdp: true,
            CannotUseWcuOcr: true,
            CannotExecuteRecipe: true,
            CannotClaimReleaseReady: true,
            NoSideEffectProof: proof);
    }

    public static ApprovalAuditTrailFutureImplementationChecklist GetFutureImplementationChecklist() =>
        new(
            RequiredBeforeRealAuditTrail:
            [
                "durable storage architecture and migration review",
                "append-only ledger policy",
                "actor identity and authority model",
                "policy versioning contract",
                "redaction runtime and secret/PII scan policy",
                "retention and deletion/privacy policy",
                "hash-chain algorithm and chain validation policy",
                "replay protection and idempotency policy",
                "external audit approval before implementation"
            ],
            RequiredExternalAudits:
            [
                "durable audit trail design external audit",
                "mutation store integration audit",
                "writer/policy boundary audit",
                "runtime readiness gate audit",
                "release/commercial claim audit"
            ],
            RequiredPolicyDecisions:
            [
                "which audit events are mandatory",
                "which fields are mandatory per event kind",
                "which payloads must be redacted",
                "how retention classes map to workspaces",
                "how deletion eligibility and legal hold interact",
                "how hash-chain verification handles redaction"
            ],
            ExplicitNonGoals:
            [
                "no durable audit trail real",
                "no append-only ledger real",
                "no audit repository",
                "no database",
                "no filesystem product IO",
                "no file read or file hash real",
                "no event persistence",
                "no approval mutation",
                "no approval execution",
                "no runtime/live",
                "no physical export",
                "no release/commercial readiness"
            ]);

    private static ApprovalAuditEventFieldRequirement Field(
        string fieldName,
        IReadOnlyList<ApprovalAuditEventKind> requiredForEventKinds,
        bool mutation,
        bool execution,
        bool export) =>
        new(
            FieldName: fieldName,
            RequiredForEventKinds: requiredForEventKinds,
            IsImplementedNow: false,
            IsPreviewOnly: true,
            RequiredBeforeRealMutation: mutation,
            RequiredBeforeRealExecution: execution,
            RequiredBeforePhysicalExport: export);

    private static ApprovalAuditTrailExternalAuditRequirement ExternalAudit(
        string scope,
        IReadOnlyList<string> requiredEvidence,
        IReadOnlyList<string> requiredNegativeCapabilityProof) =>
        new(
            Scope: scope,
            RequiredBeforeImplementation: true,
            RequiredBeforeMutationStore: true,
            RequiredBeforeRuntime: true,
            RequiredBeforePhysicalExport: true,
            RequiredEvidence: requiredEvidence,
            RequiredNegativeCapabilityProof: requiredNegativeCapabilityProof,
            RequiredValidationSuites: ["PhaseE Safety", "PhaseE Recipes", "audit trail focused tests", "overclaim scan", "storage capability scan"],
            RequiredNoGoChecks: ["active IO", "active DB", "service registration", "command handler", "runtime/live", "release/commercial claim"],
            ExternalProviderEnabled: false,
            NetworkEnabled: false,
            ServiceRegistered: false);

    private static ApprovalAuditRetentionClass RetentionClassFor(ApprovalAuditEventKind eventKind) =>
        eventKind switch
        {
            ApprovalAuditEventKind.PolicyEvaluatedFuture => ApprovalAuditRetentionClass.PolicyDecisionFuture,
            ApprovalAuditEventKind.RuntimeGateEvaluatedFuture => ApprovalAuditRetentionClass.RuntimeGateFuture,
            ApprovalAuditEventKind.ExportRequestedFuture or ApprovalAuditEventKind.ExportBlockedFuture => ApprovalAuditRetentionClass.ExportRequestFuture,
            ApprovalAuditEventKind.ExternalAuditRequestedFuture or ApprovalAuditEventKind.ExternalAuditCompletedFuture => ApprovalAuditRetentionClass.ExternalAuditFuture,
            ApprovalAuditEventKind.MutationAttemptedFuture or ApprovalAuditEventKind.MutationBlockedFuture or ApprovalAuditEventKind.MutationAcceptedFuture => ApprovalAuditRetentionClass.MutationAttemptFuture,
            ApprovalAuditEventKind.HumanReviewOpenedPreview or ApprovalAuditEventKind.HumanReviewedPreview => ApprovalAuditRetentionClass.HumanReviewFuture,
            _ => ApprovalAuditRetentionClass.ApprovalPacketFuture
        };

    private static IReadOnlyList<string> CurrentApprovalAuditTrailBaseline() =>
    [
        "ControlledExecutionReadinessDesignTrack includes a shallow durable audit trail design with all storage flags false.",
        "ApprovalMutationStoreDesignOnlyProtected requires durable audit trail before any future mutation store can exist.",
        "ApprovalExecutionDesignOnlyProtected keeps approval state mutation, runtime/live and physical export readiness at 0%.",
        "No audit repository, append-only ledger, DB, filesystem write, file hash, service registration or command handler exists.",
        "Existing PhaseE tests assert read-only preview behavior and no-side-effect proof."
    ];
}
