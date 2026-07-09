using CommonBoundaryClaim = OneBrain.Core.Approval.NodalOsCommonBoundaryClaimsCandidate.Claim;
using CommonBoundaryClaimState = OneBrain.Core.Approval.NodalOsCommonBoundaryClaimsCandidate.ClaimState;

namespace OneBrain.Core.Approval;

public enum ReentryDecisionPacketStatus
{
    ReadOnly,
    Paused,
    BlockedNoGo
}

public enum ReentryNextSafeOptionStatus
{
    SafeDesignOnly,
    SafeReadOnly,
    BlockedNoGo
}

public sealed record ReentryRepositoryStateSummary(
    string Repo,
    string Branch,
    string InputHead,
    string ExpectedOriginSync,
    string WorktreeExpectation,
    string StashPolicy);

public sealed record ReentryTrackClosureSummary(
    string TrackId,
    string Status,
    string EvidenceRef,
    bool Closed,
    bool Audited,
    bool ReadOnly,
    bool OpensRealCapability);

public sealed record ReentryCapabilityReadinessSummary(
    int RuntimeLiveRealPercent,
    int ExecutionRealPercent,
    int MutationRealPercent,
    int PhysicalExportRealPercent,
    int RedactionRuntimeRealPercent,
    int SecretPiiScanRealPercent,
    int RetentionDeletionRuntimeRealPercent,
    int DurableAuditTrailImplementationPercent,
    int MutationStoreImplementationPercent,
    int WriterPolicyProductiveIntegrationPercent,
    string ReleaseCommercialReadiness,
    string ProductUsableEndToEndEstimate,
    string DocsCanonicalConsistency,
    string PrivacyExportControlledExecutionDesign)
{
    public bool KeepsRealReadinessAtZero =>
        RuntimeLiveRealPercent == 0
        && ExecutionRealPercent == 0
        && MutationRealPercent == 0
        && PhysicalExportRealPercent == 0
        && RedactionRuntimeRealPercent == 0
        && SecretPiiScanRealPercent == 0
        && RetentionDeletionRuntimeRealPercent == 0
        && DurableAuditTrailImplementationPercent == 0
        && MutationStoreImplementationPercent == 0
        && WriterPolicyProductiveIntegrationPercent == 0
        && ReleaseCommercialReadiness == "NO-GO";
}

public sealed record ReentryActionCounts(
    int ProductActionCount,
    int StateMutationCount,
    int ExportActionCount,
    int FileOutputCount,
    int RedactionActionCount,
    int RetentionActionCount,
    int DeletionActionCount,
    int ServiceRegistrationCount,
    int CommandHandlerCount,
    int RuntimeInvocationCount,
    int ProviderNetworkCallCount,
    int BrowserCdpLiveActionCount,
    int WcuOcrLiveActionCount)
{
    public bool AllZero =>
        ProductActionCount == 0
        && StateMutationCount == 0
        && ExportActionCount == 0
        && FileOutputCount == 0
        && RedactionActionCount == 0
        && RetentionActionCount == 0
        && DeletionActionCount == 0
        && ServiceRegistrationCount == 0
        && CommandHandlerCount == 0
        && RuntimeInvocationCount == 0
        && ProviderNetworkCallCount == 0
        && BrowserCdpLiveActionCount == 0
        && WcuOcrLiveActionCount == 0;
}

public sealed record ReentryCapabilityStatus(
    bool HasRuntimeLive,
    bool HasExecutionReal,
    bool HasMutationReal,
    bool HasPhysicalExportReal,
    bool HasRedactionRuntime,
    bool HasSecretPiiScanReal,
    bool HasRetentionDeletionRuntime,
    bool HasDurableAuditTrailReal,
    bool HasMutationStoreReal,
    bool HasWriterPolicyProductivePath,
    bool HasServiceRegistration,
    bool HasCommandHandler,
    bool HasProductActions,
    bool HasFilesystemProductIo,
    bool HasDatabaseMigration,
    bool HasProviderCloudNetwork,
    bool HasLlmLive,
    bool HasBrowserCdpLive,
    bool HasWcuOcrLive,
    bool HasRecipeExecutionReal,
    bool HasReleaseCommercialReadiness)
{
    public bool NoRealCapabilities =>
        !HasRuntimeLive
        && !HasExecutionReal
        && !HasMutationReal
        && !HasPhysicalExportReal
        && !HasRedactionRuntime
        && !HasSecretPiiScanReal
        && !HasRetentionDeletionRuntime
        && !HasDurableAuditTrailReal
        && !HasMutationStoreReal
        && !HasWriterPolicyProductivePath
        && !HasServiceRegistration
        && !HasCommandHandler
        && !HasProductActions
        && !HasFilesystemProductIo
        && !HasDatabaseMigration
        && !HasProviderCloudNetwork
        && !HasLlmLive
        && !HasBrowserCdpLive
        && !HasWcuOcrLive
        && !HasRecipeExecutionReal
        && !HasReleaseCommercialReadiness;
}

public sealed record ReentryExternalAuditGateRequirement(
    string GateId,
    string Capability,
    bool RequiredBeforeImplementation,
    bool SatisfiedNow,
    string Status,
    IReadOnlyList<string> RequiredBeforeOpening);

public sealed record ReentryNextSafeOption(
    string OptionId,
    string Title,
    ReentryNextSafeOptionStatus Status,
    bool ReadOnly,
    bool DesignOnly,
    bool OpensRealCapability,
    IReadOnlyList<string> RequiredGuardrails);

public sealed record ReentryNoSideEffectProof(
    bool ReadOnly,
    bool Deterministic,
    bool FixtureSafe,
    bool UsesOnlyDocEvidenceRefs,
    ReentryActionCounts Counts,
    ApprovalReviewNoSideEffectProof ApprovalProof)
{
    public bool Passes =>
        ReadOnly
        && Deterministic
        && FixtureSafe
        && UsesOnlyDocEvidenceRefs
        && Counts.AllZero
        && ApprovalProof.Passes
        && !ApprovalProof.RuntimeTouched
        && !ApprovalProof.ApprovalExecutionStarted
        && !ApprovalProof.ApprovalStateMutationAttempted
        && !ApprovalProof.ProductActionExposed
        && !ApprovalProof.ProductServiceRegistered;
}

public sealed record ReentryDecisionPacketReadOnly(
    string PacketId,
    string GeneratedAtUtc,
    ReentryDecisionPacketStatus Status,
    string Mode,
    string CanonicalState,
    string CurrentDecision,
    ReentryRepositoryStateSummary RepositoryStateSummary,
    IReadOnlyList<ReentryTrackClosureSummary> TrackClosureSummary,
    ReentryCapabilityReadinessSummary CapabilityReadinessSummary,
    ReentryCapabilityStatus CapabilityStatus,
    IReadOnlyList<string> Blockers,
    ReentryNoSideEffectProof NoSideEffectProof,
    IReadOnlyList<ReentryExternalAuditGateRequirement> RequiredExternalAuditGates,
    IReadOnlyList<ReentryNextSafeOption> NextSafeOptions,
    IReadOnlyList<ReentryNextSafeOption> BlockedRealCapabilityOptions,
    string ReleaseCommercialStatus,
    IReadOnlyList<string> EvidenceLinks,
    IReadOnlyList<string> Warnings,
    string HumanOperatorRecommendation)
{
    public int ProductActionCount => NoSideEffectProof.Counts.ProductActionCount;
    public int StateMutationCount => NoSideEffectProof.Counts.StateMutationCount;
    public int ExportActionCount => NoSideEffectProof.Counts.ExportActionCount;
    public int FileOutputCount => NoSideEffectProof.Counts.FileOutputCount;
    public int RedactionActionCount => NoSideEffectProof.Counts.RedactionActionCount;
    public int RetentionActionCount => NoSideEffectProof.Counts.RetentionActionCount;
    public int DeletionActionCount => NoSideEffectProof.Counts.DeletionActionCount;
    public int ServiceRegistrationCount => NoSideEffectProof.Counts.ServiceRegistrationCount;
    public int CommandHandlerCount => NoSideEffectProof.Counts.CommandHandlerCount;
    public int RuntimeInvocationCount => NoSideEffectProof.Counts.RuntimeInvocationCount;
    public int ProviderNetworkCallCount => NoSideEffectProof.Counts.ProviderNetworkCallCount;
    public int BrowserCdpLiveActionCount => NoSideEffectProof.Counts.BrowserCdpLiveActionCount;
    public int WcuOcrLiveActionCount => NoSideEffectProof.Counts.WcuOcrLiveActionCount;

    public bool PassesSafetyProof =>
        Status == ReentryDecisionPacketStatus.ReadOnly
        && CanonicalState == "PAUSED_READ_ONLY_NO_RUNTIME_NO_EXECUTION_NO_MUTATION_NO_PHYSICAL_EXPORT_NO_REDACTION_RUNTIME"
        && CapabilityReadinessSummary.KeepsRealReadinessAtZero
        && CapabilityStatus.NoRealCapabilities
        && NoSideEffectProof.Passes
        && NextSafeOptions.Count > 0
        && NextSafeOptions.All(option => !option.OpensRealCapability)
        && NextSafeOptions.All(option => option.Status is ReentryNextSafeOptionStatus.SafeDesignOnly or ReentryNextSafeOptionStatus.SafeReadOnly)
        && BlockedRealCapabilityOptions.Count > 0
        && BlockedRealCapabilityOptions.All(option => option.Status == ReentryNextSafeOptionStatus.BlockedNoGo)
        && BlockedRealCapabilityOptions.All(option => option.OpensRealCapability)
        && RequiredExternalAuditGates.All(gate => gate.RequiredBeforeImplementation && !gate.SatisfiedNow)
        && ReleaseCommercialStatus == "NO-GO"
        && CommonBoundaryClaimsRemainFailClosed();

    private static bool CommonBoundaryClaimsRemainFailClosed() =>
        CommonBoundaryClaimsRemainFailClosed(NodalOsCommonBoundaryClaimsCandidate.DefaultBlocked());

    private static bool CommonBoundaryClaimsRemainFailClosed(NodalOsCommonBoundaryClaimsCandidate? candidate) =>
        candidate is not null
        && candidate.ParallelOnly
        && candidate.NonAuthoritative
        && !candidate.ExistingHardBlockAuthorityReplaced
        && !candidate.AllowsRuntimeProductOrAuthority()
        && NodalOsCommonBoundaryClaimsCandidate.ExpectedClosedStates.All(expected =>
            CommonBoundaryClaimRemainsFailClosed(candidate, expected.Key, expected.Value));

    private static bool CommonBoundaryClaimRemainsFailClosed(
        NodalOsCommonBoundaryClaimsCandidate candidate,
        CommonBoundaryClaim claim,
        CommonBoundaryClaimState expectedState) =>
        candidate.StateFor(claim) == expectedState
        && candidate.IsFailClosed(claim)
        && !candidate.CanOverrideExistingHardBlock(claim);
}

public static class ReentryDecisionPacketReadOnlyPresenter
{
    public static ReentryDecisionPacketReadOnly CreateFixture() =>
        new(
            PacketId: "nodal-os.reentry.decision-packet.read-only.fixture.v1",
            GeneratedAtUtc: "2026-07-03T00:00:00Z",
            Status: ReentryDecisionPacketStatus.ReadOnly,
            Mode: "READ_ONLY_REENTRY_PACKET_NO_RUNTIME_NO_EXECUTION_NO_MUTATION_NO_EXPORT_NO_REDACTION_RUNTIME",
            CanonicalState: "PAUSED_READ_ONLY_NO_RUNTIME_NO_EXECUTION_NO_MUTATION_NO_PHYSICAL_EXPORT_NO_REDACTION_RUNTIME",
            CurrentDecision: "GO_NODAL_OS_CANONICAL_STATUS_DOCS_HARDENING_DESIGN_ONLY_READY",
            RepositoryStateSummary: RepositoryState(),
            TrackClosureSummary: TrackClosures(),
            CapabilityReadinessSummary: Readiness(),
            CapabilityStatus: CapabilityStatus(),
            Blockers: Blockers(),
            NoSideEffectProof: NoSideEffectProof(),
            RequiredExternalAuditGates: ExternalAuditGates(),
            NextSafeOptions: NextSafeOptions(),
            BlockedRealCapabilityOptions: BlockedRealCapabilityOptions(),
            ReleaseCommercialStatus: "NO-GO",
            EvidenceLinks: EvidenceLinks(),
            Warnings: Warnings(),
            HumanOperatorRecommendation: "Continue with design-only implementation planning or pause; do not open runtime/live, execution, mutation, physical export, redaction runtime, retention/deletion runtime or release/commercial readiness.");

    private static ReentryRepositoryStateSummary RepositoryState() =>
        new(
            Repo: "C:/DESARROLLO/NodalOS/Codigo-m12-audit",
            Branch: "chrome-lab-001-extension-local-ai-bridge",
            InputHead: "82a3f1a1d670d7d6842f20a7830a8f9808e5e1c0",
            ExpectedOriginSync: "0 0",
            WorktreeExpectation: "clean",
            StashPolicy: "list-only; do not apply, drop, modify or inspect destructively");

    private static IReadOnlyList<ReentryTrackClosureSummary> TrackClosures() =>
    [
        Track("privacy.export.controlled-execution.design", "CLOSED_AUDITED_READ_ONLY", "docs/handoff/nodal-os-final-privacy-export-controlled-execution-closeout-handoff.md"),
        Track("canonical.status.docs-hardening", "CLOSED_DOCS_ONLY", "docs/qa/nodal-os-canonical-status-docs-hardening/report.md"),
        Track("approval.execution.design", "DESIGN_ONLY", "src/OneBrain.Core/Approval/ApprovalExecutionDesignOnlyProtected.cs"),
        Track("approval.mutation.design", "DESIGN_ONLY", "src/OneBrain.Core/Approval/ApprovalMutationStoreDesignOnlyProtected.cs"),
        Track("durable.audit-trail.design", "DESIGN_ONLY", "src/OneBrain.Core/Approval/ApprovalDurableAuditTrailDesignOnlyProtected.cs"),
        Track("physical.export.policy.design", "DESIGN_ONLY", "src/OneBrain.Core/Approval/PhysicalExportPolicyDesignOnlyProtected.cs"),
        Track("redaction.retention.deletion.policy.design", "DESIGN_ONLY", "src/OneBrain.Core/Approval/RedactionRetentionDeletionPolicyDesignOnlyProtected.cs")
    ];

    private static ReentryTrackClosureSummary Track(string id, string status, string evidenceRef) =>
        new(id, status, evidenceRef, Closed: true, Audited: true, ReadOnly: true, OpensRealCapability: false);

    private static ReentryCapabilityReadinessSummary Readiness() =>
        new(
            RuntimeLiveRealPercent: 0,
            ExecutionRealPercent: 0,
            MutationRealPercent: 0,
            PhysicalExportRealPercent: 0,
            RedactionRuntimeRealPercent: 0,
            SecretPiiScanRealPercent: 0,
            RetentionDeletionRuntimeRealPercent: 0,
            DurableAuditTrailImplementationPercent: 0,
            MutationStoreImplementationPercent: 0,
            WriterPolicyProductiveIntegrationPercent: 0,
            ReleaseCommercialReadiness: "NO-GO",
            ProductUsableEndToEndEstimate: "20-30%",
            DocsCanonicalConsistency: "92-95%",
            PrivacyExportControlledExecutionDesign: "100% closed/audited");

    private static ReentryCapabilityStatus CapabilityStatus() =>
        new(
            HasRuntimeLive: false,
            HasExecutionReal: false,
            HasMutationReal: false,
            HasPhysicalExportReal: false,
            HasRedactionRuntime: false,
            HasSecretPiiScanReal: false,
            HasRetentionDeletionRuntime: false,
            HasDurableAuditTrailReal: false,
            HasMutationStoreReal: false,
            HasWriterPolicyProductivePath: false,
            HasServiceRegistration: false,
            HasCommandHandler: false,
            HasProductActions: false,
            HasFilesystemProductIo: false,
            HasDatabaseMigration: false,
            HasProviderCloudNetwork: false,
            HasLlmLive: false,
            HasBrowserCdpLive: false,
            HasWcuOcrLive: false,
            HasRecipeExecutionReal: false,
            HasReleaseCommercialReadiness: false);

    private static IReadOnlyList<string> Blockers() =>
    [
        "Requires explicit user gate before runtime/live.",
        "Requires external audit before runtime, export, mutation, redaction or retention/deletion implementation.",
        "Requires implementation planning gate before any real capability.",
        "Requires negative tests before any real path.",
        "Requires isolated scope for IO, DB, provider, browser and runtime.",
        "Release/commercial remains NO-GO."
    ];

    private static ReentryNoSideEffectProof NoSideEffectProof() =>
        new(
            ReadOnly: true,
            Deterministic: true,
            FixtureSafe: true,
            UsesOnlyDocEvidenceRefs: true,
            Counts: new ReentryActionCounts(
                ProductActionCount: 0,
                StateMutationCount: 0,
                ExportActionCount: 0,
                FileOutputCount: 0,
                RedactionActionCount: 0,
                RetentionActionCount: 0,
                DeletionActionCount: 0,
                ServiceRegistrationCount: 0,
                CommandHandlerCount: 0,
                RuntimeInvocationCount: 0,
                ProviderNetworkCallCount: 0,
                BrowserCdpLiveActionCount: 0,
                WcuOcrLiveActionCount: 0),
            ApprovalProof: ApprovalReviewNoSideEffectProof.FixtureReadOnly());

    private static IReadOnlyList<ReentryExternalAuditGateRequirement> ExternalAuditGates() =>
    [
        Gate("runtime-live", "runtime/live"),
        Gate("approval-execution", "approval execution"),
        Gate("approval-mutation", "approval mutation"),
        Gate("physical-export", "physical export"),
        Gate("redaction-runtime", "redaction runtime"),
        Gate("secret-pii-scan", "secret/PII scan"),
        Gate("retention-deletion-runtime", "retention/deletion runtime"),
        Gate("release-commercial", "release/commercial readiness")
    ];

    private static ReentryExternalAuditGateRequirement Gate(string gateId, string capability) =>
        new(
            GateId: gateId,
            Capability: capability,
            RequiredBeforeImplementation: true,
            SatisfiedNow: false,
            Status: "REQUIRED_BEFORE_IMPLEMENTATION",
            RequiredBeforeOpening:
            [
                "explicit user authorization",
                "implementation planning gate",
                "external audit",
                "negative tests",
                "no-side-effect regression proof"
            ]);

    private static IReadOnlyList<ReentryNextSafeOption> NextSafeOptions() =>
    [
        Safe("PAUSE_AFTER_REENTRY_PACKET_NO_CHANGES", "Pause after the read-only reentry packet.", ReentryNextSafeOptionStatus.SafeReadOnly, readOnly: true, designOnly: false),
        Safe("NODAL_OS_IMPLEMENTATION_PLANNING_GATE_DESIGN_ONLY", "Plan future implementation gates without opening capabilities.", ReentryNextSafeOptionStatus.SafeDesignOnly, readOnly: true, designOnly: true),
        Safe("NODAL_OS_READ_ONLY_PRODUCT_STATUS_SURFACE_EXPANSION", "Expand read-only status surfaces without product actions.", ReentryNextSafeOptionStatus.SafeReadOnly, readOnly: true, designOnly: false),
        Safe("NODAL_OS_EXTERNAL_AUDIT_PRE_RUNTIME_GATE_READ_ONLY", "Run an external read-only pre-runtime gate audit.", ReentryNextSafeOptionStatus.SafeReadOnly, readOnly: true, designOnly: false)
    ];

    private static ReentryNextSafeOption Safe(string optionId, string title, ReentryNextSafeOptionStatus status, bool readOnly, bool designOnly) =>
        new(
            OptionId: optionId,
            Title: title,
            Status: status,
            ReadOnly: readOnly,
            DesignOnly: designOnly,
            OpensRealCapability: false,
            RequiredGuardrails:
            [
                "repo guard",
                "no runtime/live",
                "no execution",
                "no mutation",
                "no physical export",
                "no redaction runtime",
                "no retention/deletion runtime",
                "release/commercial NO-GO"
            ]);

    private static IReadOnlyList<ReentryNextSafeOption> BlockedRealCapabilityOptions() =>
    [
        Blocked("RUNTIME_LIVE_REAL", "Runtime/live real remains blocked."),
        Blocked("APPROVAL_EXECUTION_REAL", "Approval execution real remains blocked."),
        Blocked("APPROVAL_MUTATION_REAL", "Approval mutation real remains blocked."),
        Blocked("PHYSICAL_EXPORT_REAL", "Physical export real remains blocked."),
        Blocked("REDACTION_RUNTIME_REAL", "Redaction runtime real remains blocked."),
        Blocked("RETENTION_DELETION_RUNTIME_REAL", "Retention/deletion runtime real remains blocked."),
        Blocked("RELEASE_COMMERCIAL_READY", "Release/commercial readiness remains NO-GO.")
    ];

    private static ReentryNextSafeOption Blocked(string optionId, string title) =>
        new(
            OptionId: optionId,
            Title: title,
            Status: ReentryNextSafeOptionStatus.BlockedNoGo,
            ReadOnly: false,
            DesignOnly: false,
            OpensRealCapability: true,
            RequiredGuardrails:
            [
                "not a safe next option",
                "requires explicit user gate",
                "requires implementation planning gate",
                "requires external audit"
            ]);

    private static IReadOnlyList<string> EvidenceLinks() =>
    [
        "docs/qa/nodal-os-canonical-status-docs-hardening/report.md",
        "docs/qa/nodal-os-canonical-status-docs-hardening/report.json",
        "docs/handoff/nodal-os-final-privacy-export-controlled-execution-closeout-handoff.md",
        "docs/qa/nodal-os-final-privacy-export-controlled-execution-closeout/report.md",
        "docs/decision-log.md",
        "docs/roadmap/nodal-os-roadmap-vnext.md",
        "docs/roadmap/nodal-os-unified-roadmap-post-pause.md",
        "docs/roadmap/read-only-cross-phase-closeout-index.md"
    ];

    private static IReadOnlyList<string> Warnings() =>
    [
        "This packet is a read-only decision surface and does not execute, mutate, export, scan, retain, delete, persist or call providers.",
        "Docs canonical consistency is separate from implementation readiness.",
        "Product usable end-to-end remains an estimate, not release/commercial readiness.",
        "Runtime/live, execution, mutation, physical export, redaction runtime, secret/PII scan and retention/deletion runtime remain 0%."
    ];
}
