namespace OneBrain.Core.Approval;

public enum ControlledExecutionDesignStatus
{
    DesignOnly,
    ReadOnly,
    PreviewOnly,
    Blocked,
    NeedsAudit,
    FutureEligible,
    NotImplemented
}

public enum ApprovalExecutionConceptualState
{
    Draft,
    Proposed,
    HumanReviewRequired,
    Reviewed,
    ApprovedConceptual,
    RejectedConceptual,
    ExpiredConceptual,
    InvalidatedConceptual,
    SupersededConceptual,
    Blocked,
    ExecutionEligibleFuture,
    ExecutionStartedFuture,
    ExecutionCompletedFuture,
    ExecutionFailedFuture,
    RollbackRequiredFuture,
    RollbackCompletedFuture
}

public enum ApprovalMutationCandidateKind
{
    RecordReview,
    Approve,
    Reject,
    RequestChanges,
    Expire,
    Invalidate,
    Supersede,
    AttachEvidence,
    MarkExecutionEligibleFuture
}

public enum ControlledExecutionReadinessGateCategory
{
    Approval,
    MutationBoundary,
    AuditTrail,
    WriterPolicy,
    Evidence,
    Redaction,
    WorkspaceBoundary,
    ExportPolicy,
    ProductActionControls,
    RuntimeSafety,
    ExternalAudit,
    CommercialClaim
}

public sealed record ControlledExecutionReadinessSummary(
    int ApprovalExecutionDesignReadinessPercent,
    int ControlledExecutionReadinessDesignPercent,
    int ApprovalExecutionImplementationReadinessPercent,
    int ApprovalStateMutationReadinessPercent,
    int RuntimeLiveReadinessPercent,
    int PhysicalExportReadinessPercent,
    bool ReleaseCommercialReady)
{
    public bool KeepsImplementationBlocked =>
        ApprovalExecutionImplementationReadinessPercent == 0
        && ApprovalStateMutationReadinessPercent == 0
        && RuntimeLiveReadinessPercent == 0
        && PhysicalExportReadinessPercent == 0
        && !ReleaseCommercialReady;
}

public sealed record ApprovalExecutionStateTransitionPreview(
    string TransitionId,
    ApprovalExecutionConceptualState From,
    ApprovalExecutionConceptualState To,
    ControlledExecutionDesignStatus Status,
    IReadOnlyList<string> RequiredEvidence,
    IReadOnlyList<string> RequiredHumanApproval,
    IReadOnlyList<string> BlockedReasons,
    bool FutureStateNotImplemented,
    bool PreviewOnly,
    bool ExecutesWork,
    bool MutatesState,
    bool StartsRuntime);

public sealed record ApprovalExecutionStateMachineDesignOnly(
    string DesignId,
    IReadOnlyList<ApprovalExecutionConceptualState> States,
    IReadOnlyList<ApprovalExecutionStateTransitionPreview> Transitions,
    IReadOnlyList<string> ExpirationInvalidationRules,
    ApprovalReviewNoSideEffectProof NoSideEffectProof)
{
    public bool PreviewOnly => Transitions.All(transition => transition.PreviewOnly);
    public bool HasExecution => Transitions.Any(transition => transition.ExecutesWork);
    public bool HasMutation => Transitions.Any(transition => transition.MutatesState);
    public bool HasRuntime => Transitions.Any(transition => transition.StartsRuntime);
}

public sealed record ApprovalMutationCandidatePreview(
    ApprovalMutationCandidateKind Kind,
    ControlledExecutionDesignStatus Status,
    IReadOnlyList<string> RequiredFutureControls,
    IReadOnlyList<string> BlockedReasons,
    bool RequiresActor,
    bool RequiresTimestamp,
    bool RequiresEvidenceRefs,
    bool RequiresPolicyDecision,
    bool RequiresDurableAuditTrailFuture,
    bool HasMutationMethod,
    bool WritesState,
    bool UsesRepository,
    bool UsesDatabase,
    bool UsesFilesystem);

public sealed record ApprovalMutationBoundaryDesignOnly(
    string BoundaryId,
    IReadOnlyList<ApprovalMutationCandidatePreview> MutationCandidates,
    IReadOnlyList<string> BoundaryRules,
    ApprovalReviewNoSideEffectProof NoSideEffectProof)
{
    public bool HasMutationMethod => MutationCandidates.Any(candidate => candidate.HasMutationMethod);
    public bool WritesState => MutationCandidates.Any(candidate => candidate.WritesState);
    public bool UsesStore => MutationCandidates.Any(candidate => candidate.UsesRepository || candidate.UsesDatabase || candidate.UsesFilesystem);
}

public sealed record ApprovalWriterPolicyIntegrationBoundaryDesignOnly(
    string BoundaryId,
    string ConceptualFlow,
    IReadOnlyList<string> Contracts,
    IReadOnlyList<string> Rules,
    IReadOnlyList<string> Risks,
    bool ApprovalImpliesExecution,
    bool ProductivePolicyPathAvailable,
    bool WriterInvoked,
    bool PolicyPreviewCanWrite,
    bool WriterCandidateCanRun,
    bool ApprovalCanBypassPolicy,
    bool ServiceRegistered,
    bool CommandHandlerRegistered,
    bool ExecutionBlocked);

public sealed record ApprovalDurableAuditTrailDesignOnly(
    string DesignId,
    IReadOnlyList<string> EventTypes,
    IReadOnlyList<string> ConceptualFields,
    IReadOnlyList<string> RequiredBeforeRealMutation,
    bool DurableTrailImplemented,
    bool DatabaseUsed,
    bool FilesystemWriteUsed,
    bool MigrationRunnerUsed,
    bool CloudLogUsed);

public sealed record ApprovalPhysicalExportPolicyDesignOnly(
    string PolicyId,
    IReadOnlyList<string> ExportTargetsFuture,
    IReadOnlyList<string> ExportBlockers,
    IReadOnlyList<string> ExportPacketPreviewSections,
    bool PhysicalFileCreated,
    bool ClipboardUsed,
    bool DownloadStarted,
    bool StreamWritten,
    bool FilesystemWritten,
    bool ExternalProcessStarted);

public sealed record ProductActionControlReadinessDesignOnly(
    string ControlId,
    string Label,
    ControlledExecutionDesignStatus Status,
    IReadOnlyList<string> DisabledReasons,
    IReadOnlyList<string> FutureEnablementChecklist,
    bool PreviewOnly,
    bool EnabledNow,
    bool CommandBindingAvailable,
    bool ServiceRouteAvailable,
    bool HandlerAvailable);

public sealed record CrossPhaseRuntimeReadinessGateDesignOnly(
    string GateId,
    ControlledExecutionReadinessGateCategory Category,
    ControlledExecutionDesignStatus Status,
    IReadOnlyList<string> MissingRequirements,
    bool BlocksRuntime,
    bool AllowsRuntimeLive,
    bool AllowsReleaseCommercial);

public sealed record ControlledExecutionNegativeCapabilityContract(
    string CapabilityId,
    bool CannotExecuteApproval,
    bool CannotMutateApprovalState,
    bool CannotInvokeWriter,
    bool CannotInvokePolicyProductivePath,
    bool CannotStartRuntime,
    bool CannotRegisterService,
    bool CannotCreateCommandHandler,
    bool CannotWriteFilesystem,
    bool CannotUseDatabase,
    bool CannotUseProviderCloud,
    bool CannotUseLlmLive,
    bool CannotUseVectorBackend,
    bool CannotUseDurableMemory,
    bool CannotUseBrowserCdp,
    bool CannotUseWcuOcr,
    bool CannotExecuteRecipe,
    bool CannotExportPhysicalFile,
    bool CannotUseClipboardDownload,
    bool CannotClaimReleaseCommercialReady)
{
    public bool Passes =>
        CannotExecuteApproval
        && CannotMutateApprovalState
        && CannotInvokeWriter
        && CannotInvokePolicyProductivePath
        && CannotStartRuntime
        && CannotRegisterService
        && CannotCreateCommandHandler
        && CannotWriteFilesystem
        && CannotUseDatabase
        && CannotUseProviderCloud
        && CannotUseLlmLive
        && CannotUseVectorBackend
        && CannotUseDurableMemory
        && CannotUseBrowserCdp
        && CannotUseWcuOcr
        && CannotExecuteRecipe
        && CannotExportPhysicalFile
        && CannotUseClipboardDownload
        && CannotClaimReleaseCommercialReady;
}

public sealed record ControlledExecutionReadinessDesignTrack(
    string TrackId,
    string Title,
    string Mode,
    ControlledExecutionDesignStatus Status,
    ControlledExecutionReadinessSummary Readiness,
    ApprovalExecutionStateMachineDesignOnly StateMachine,
    ApprovalMutationBoundaryDesignOnly MutationBoundary,
    ApprovalWriterPolicyIntegrationBoundaryDesignOnly WriterPolicyBoundary,
    ApprovalDurableAuditTrailDesignOnly DurableAuditTrail,
    ApprovalPhysicalExportPolicyDesignOnly PhysicalExportPolicy,
    IReadOnlyList<ProductActionControlReadinessDesignOnly> ProductActionControls,
    IReadOnlyList<CrossPhaseRuntimeReadinessGateDesignOnly> RuntimeReadinessGates,
    IReadOnlyList<ControlledExecutionNegativeCapabilityContract> NegativeCapabilities,
    IReadOnlyList<string> CurrentApprovalReadOnlyDesignMap,
    IReadOnlyList<string> FutureProtectedDebt,
    IReadOnlyList<string> Warnings,
    string NextSafeStep,
    ApprovalReviewNoSideEffectProof NoSideEffectProof)
{
    public int ProductActionCount => ProductActionControls.Count(control => control.EnabledNow);
    public int StateMutationCount => MutationBoundary.MutationCandidates.Count(candidate => candidate.WritesState);
    public int ExportActionCount => PhysicalExportPolicy.PhysicalFileCreated || PhysicalExportPolicy.ClipboardUsed || PhysicalExportPolicy.DownloadStarted ? 1 : 0;
    public bool HasRealExecution => StateMachine.HasExecution || !WriterPolicyBoundary.ExecutionBlocked || NegativeCapabilities.Any(contract => !contract.CannotExecuteApproval);
    public bool HasStateMutation => StateMachine.HasMutation || MutationBoundary.WritesState || NegativeCapabilities.Any(contract => !contract.CannotMutateApprovalState);
    public bool HasRuntimeLive => StateMachine.HasRuntime || RuntimeReadinessGates.Any(gate => gate.AllowsRuntimeLive) || NegativeCapabilities.Any(contract => !contract.CannotStartRuntime);
    public bool HasPhysicalExport => ExportActionCount > 0 || NegativeCapabilities.Any(contract => !contract.CannotExportPhysicalFile);
    public bool HasProductActions => ProductActionCount > 0 || ProductActionControls.Any(control => control.CommandBindingAvailable || control.ServiceRouteAvailable || control.HandlerAvailable);
    public bool PassesNegativeCapabilities => NegativeCapabilities.All(contract => contract.Passes);
    public bool RuntimeGateBlocked => RuntimeReadinessGates.All(gate => gate.BlocksRuntime && !gate.AllowsRuntimeLive);
}

public static class ControlledExecutionReadinessDesignTrackPresenter
{
    public static ControlledExecutionReadinessDesignTrack CreateFixture()
    {
        var proof = ApprovalReviewNoSideEffectProof.FixtureReadOnly();

        return new ControlledExecutionReadinessDesignTrack(
            TrackId: "nodal-os.controlled-execution-readiness.design-track.fixture.v1",
            Title: "Controlled Execution Readiness Design Track",
            Mode: "DESIGN_ONLY_READ_ONLY_NO_EXECUTION_NO_MUTATION_NO_RUNTIME",
            Status: ControlledExecutionDesignStatus.DesignOnly,
            Readiness: new ControlledExecutionReadinessSummary(
                ApprovalExecutionDesignReadinessPercent: 92,
                ControlledExecutionReadinessDesignPercent: 80,
                ApprovalExecutionImplementationReadinessPercent: 0,
                ApprovalStateMutationReadinessPercent: 0,
                RuntimeLiveReadinessPercent: 0,
                PhysicalExportReadinessPercent: 0,
                ReleaseCommercialReady: false),
            StateMachine: StateMachine(proof),
            MutationBoundary: MutationBoundary(proof),
            WriterPolicyBoundary: WriterPolicyBoundary(),
            DurableAuditTrail: DurableAuditTrail(),
            PhysicalExportPolicy: PhysicalExportPolicy(),
            ProductActionControls: ProductActionControls(),
            RuntimeReadinessGates: RuntimeReadinessGates(),
            NegativeCapabilities: NegativeCapabilities(),
            CurrentApprovalReadOnlyDesignMap: CurrentApprovalReadOnlyDesignMap(),
            FutureProtectedDebt: FutureProtectedDebt(),
            Warnings:
            [
                "Design readiness may increase. Runtime readiness remains 0%.",
                "Conceptual approval does not imply executable approval.",
                "Every future execution, mutation, export and runtime path remains blocked."
            ],
            NextSafeStep: "NODAL_OS_CONTROLLED_EXECUTION_READINESS_DESIGN_EXTERNAL_AUDIT",
            NoSideEffectProof: proof);
    }

    private static ApprovalExecutionStateMachineDesignOnly StateMachine(ApprovalReviewNoSideEffectProof proof)
    {
        var states = Enum.GetValues<ApprovalExecutionConceptualState>();
        var transitions = new[]
        {
            Transition("draft.to.proposed", ApprovalExecutionConceptualState.Draft, ApprovalExecutionConceptualState.Proposed, false),
            Transition("proposed.to.human-review-required", ApprovalExecutionConceptualState.Proposed, ApprovalExecutionConceptualState.HumanReviewRequired, false),
            Transition("human-review-required.to.reviewed", ApprovalExecutionConceptualState.HumanReviewRequired, ApprovalExecutionConceptualState.Reviewed, false),
            Transition("reviewed.to.approved-conceptual", ApprovalExecutionConceptualState.Reviewed, ApprovalExecutionConceptualState.ApprovedConceptual, false),
            Transition("reviewed.to.rejected-conceptual", ApprovalExecutionConceptualState.Reviewed, ApprovalExecutionConceptualState.RejectedConceptual, false),
            Transition("approved-conceptual.to.execution-eligible-future", ApprovalExecutionConceptualState.ApprovedConceptual, ApprovalExecutionConceptualState.ExecutionEligibleFuture, true),
            Transition("execution-eligible-future.to.execution-started-future", ApprovalExecutionConceptualState.ExecutionEligibleFuture, ApprovalExecutionConceptualState.ExecutionStartedFuture, true),
            Transition("execution-started-future.to.execution-completed-future", ApprovalExecutionConceptualState.ExecutionStartedFuture, ApprovalExecutionConceptualState.ExecutionCompletedFuture, true),
            Transition("execution-started-future.to.execution-failed-future", ApprovalExecutionConceptualState.ExecutionStartedFuture, ApprovalExecutionConceptualState.ExecutionFailedFuture, true),
            Transition("execution-failed-future.to.rollback-required-future", ApprovalExecutionConceptualState.ExecutionFailedFuture, ApprovalExecutionConceptualState.RollbackRequiredFuture, true),
            Transition("rollback-required-future.to.rollback-completed-future", ApprovalExecutionConceptualState.RollbackRequiredFuture, ApprovalExecutionConceptualState.RollbackCompletedFuture, true),
            Transition("any.to.expired-conceptual", ApprovalExecutionConceptualState.Proposed, ApprovalExecutionConceptualState.ExpiredConceptual, false),
            Transition("any.to.invalidated-conceptual", ApprovalExecutionConceptualState.Reviewed, ApprovalExecutionConceptualState.InvalidatedConceptual, false),
            Transition("any.to.superseded-conceptual", ApprovalExecutionConceptualState.Proposed, ApprovalExecutionConceptualState.SupersededConceptual, false),
            Transition("any.to.blocked", ApprovalExecutionConceptualState.Proposed, ApprovalExecutionConceptualState.Blocked, false)
        };

        return new ApprovalExecutionStateMachineDesignOnly(
            DesignId: "approval.execution.state-machine.design-only.v1",
            States: states,
            Transitions: transitions,
            ExpirationInvalidationRules:
            [
                "Stale context invalidates future execution eligibility.",
                "Missing evidence keeps approval in blocked preview state.",
                "Superseded approvals require future durable audit chain before any real mutation."
            ],
            NoSideEffectProof: proof);
    }

    private static ApprovalExecutionStateTransitionPreview Transition(
        string transitionId,
        ApprovalExecutionConceptualState from,
        ApprovalExecutionConceptualState to,
        bool futureState) =>
        new(
            TransitionId: transitionId,
            From: from,
            To: to,
            Status: futureState ? ControlledExecutionDesignStatus.NotImplemented : ControlledExecutionDesignStatus.PreviewOnly,
            RequiredEvidence: ["phase-c evidence refs future", "phase-d context refs future", "human review proof future"],
            RequiredHumanApproval: ["operator authority future", "actor identity future"],
            BlockedReasons: ["no durable audit trail", "no writer/policy boundary", "no runtime gate", "no product action controls"],
            FutureStateNotImplemented: futureState,
            PreviewOnly: true,
            ExecutesWork: false,
            MutatesState: false,
            StartsRuntime: false);

    private static ApprovalMutationBoundaryDesignOnly MutationBoundary(ApprovalReviewNoSideEffectProof proof) =>
        new(
            BoundaryId: "approval.mutation-boundary.design-only.v1",
            MutationCandidates: Enum.GetValues<ApprovalMutationCandidateKind>().Select(MutationCandidate).ToList(),
            BoundaryRules:
            [
                "Mutation requires actor, timestamp, evidence refs, policy decision and future durable audit trail.",
                "Mutation must not execute work.",
                "Mutation must not exist until storage, concurrency and human identity models are audited."
            ],
            NoSideEffectProof: proof);

    private static ApprovalMutationCandidatePreview MutationCandidate(ApprovalMutationCandidateKind kind) =>
        new(
            Kind: kind,
            Status: ControlledExecutionDesignStatus.Blocked,
            RequiredFutureControls:
            [
                "durable audit trail future",
                "writer/policy boundary future",
                "state store future",
                "human identity model future",
                "concurrency policy future"
            ],
            BlockedReasons:
            [
                "no durable audit trail",
                "no writer/policy integration",
                "no runtime gate",
                "no product action controls",
                "no state store",
                "no human identity model",
                "no concurrency policy"
            ],
            RequiresActor: true,
            RequiresTimestamp: true,
            RequiresEvidenceRefs: true,
            RequiresPolicyDecision: true,
            RequiresDurableAuditTrailFuture: true,
            HasMutationMethod: false,
            WritesState: false,
            UsesRepository: false,
            UsesDatabase: false,
            UsesFilesystem: false);

    private static ApprovalWriterPolicyIntegrationBoundaryDesignOnly WriterPolicyBoundary() =>
        new(
            BoundaryId: "approval.writer-policy.integration-boundary.design-only.v1",
            ConceptualFlow: "Approval preview -> Human review -> Policy gate -> Writer candidate -> Execution future",
            Contracts:
            [
                "ApprovalPolicyReadiness preview",
                "WriterPolicyBoundary preview",
                "WriterCandidatePreview",
                "PolicyDecisionPreview",
                "ExecutionDeniedReason",
                "RequiredGate"
            ],
            Rules:
            [
                "Approval never equals execution.",
                "Approval never skips policy.",
                "Policy preview never writes.",
                "Writer candidate never runs without approval, policy and runtime gates.",
                "Writer real path is not connected in this track.",
                "Policy preview cannot dispatch commands or write state.",
                "Writer candidate cannot register services or bind command handlers.",
                "Approval cannot bypass policy or runtime gates."
            ],
            Risks:
            [
                "approval laundering",
                "stale approval",
                "context drift",
                "policy mismatch",
                "missing evidence",
                "expired review",
                "actor mismatch",
                "target mismatch"
            ],
            ApprovalImpliesExecution: false,
            ProductivePolicyPathAvailable: false,
            WriterInvoked: false,
            PolicyPreviewCanWrite: false,
            WriterCandidateCanRun: false,
            ApprovalCanBypassPolicy: false,
            ServiceRegistered: false,
            CommandHandlerRegistered: false,
            ExecutionBlocked: true);

    private static ApprovalDurableAuditTrailDesignOnly DurableAuditTrail() =>
        new(
            DesignId: "approval.durable-audit-trail.design-only.v1",
            EventTypes:
            [
                "approval proposed",
                "human reviewed",
                "decision recorded future",
                "approval expired",
                "approval invalidated",
                "policy evaluated future",
                "execution eligibility evaluated future",
                "export generated future",
                "rollback required future"
            ],
            ConceptualFields:
            [
                "event id",
                "actor",
                "timestamp",
                "approval id",
                "evidence refs",
                "context hash future",
                "policy version",
                "target refs",
                "redaction status",
                "invalidation refs",
                "previous event hash future",
                "chain/hash future"
            ],
            RequiredBeforeRealMutation:
            [
                "storage policy",
                "redaction policy",
                "retention policy",
                "deletion/privacy policy",
                "chain validation",
                "concurrency handling",
                "replay protection"
            ],
            DurableTrailImplemented: false,
            DatabaseUsed: false,
            FilesystemWriteUsed: false,
            MigrationRunnerUsed: false,
            CloudLogUsed: false);

    private static ApprovalPhysicalExportPolicyDesignOnly PhysicalExportPolicy() =>
        new(
            PolicyId: "approval.physical-export-policy.design-only.v1",
            ExportTargetsFuture: ["PDF future", "DOCX future", "JSON future", "Markdown future", "clipboard future", "download future"],
            ExportBlockers:
            [
                "no redaction proof",
                "no user consent",
                "no path policy",
                "no export destination policy",
                "no evidence selection policy",
                "no sensitive data scan",
                "no durable audit trail",
                "no product export control"
            ],
            ExportPacketPreviewSections: ["title", "sections", "evidence refs", "decision summary", "risk summary", "redaction notes", "disabled notices"],
            PhysicalFileCreated: false,
            ClipboardUsed: false,
            DownloadStarted: false,
            StreamWritten: false,
            FilesystemWritten: false,
            ExternalProcessStarted: false);

    private static IReadOnlyList<ProductActionControlReadinessDesignOnly> ProductActionControls() =>
    [
        Control("approve.future", "Approve future"),
        Control("reject.future", "Reject future"),
        Control("request-changes.future", "Request changes future"),
        Control("export.future", "Export future"),
        Control("execute.future", "Execute future"),
        Control("rollback.future", "Rollback future"),
        Control("attach-evidence.future", "Attach evidence future")
    ];

    private static ProductActionControlReadinessDesignOnly Control(string id, string label) =>
        new(
            ControlId: id,
            Label: label,
            Status: ControlledExecutionDesignStatus.Blocked,
            DisabledReasons:
            [
                "design-only",
                "no runtime",
                "no mutation boundary",
                "no audit trail",
                "no writer/policy",
                "no export policy",
                "no human identity",
                "no service registration",
                "no command handler"
            ],
            FutureEnablementChecklist:
            [
                "policy gate ready",
                "audit trail ready",
                "mutation store ready",
                "UI action confirmation ready",
                "rollback/failure policy ready",
                "evidence proof ready",
                "security audit GO",
                "explicit user approval"
            ],
            PreviewOnly: true,
            EnabledNow: false,
            CommandBindingAvailable: false,
            ServiceRouteAvailable: false,
            HandlerAvailable: false);

    private static IReadOnlyList<CrossPhaseRuntimeReadinessGateDesignOnly> RuntimeReadinessGates() =>
        Enum.GetValues<ControlledExecutionReadinessGateCategory>()
            .Select(category => new CrossPhaseRuntimeReadinessGateDesignOnly(
                GateId: $"runtime.gate.{category.ToString().ToLowerInvariant()}",
                Category: category,
                Status: ControlledExecutionDesignStatus.Blocked,
                MissingRequirements: ["future protected implementation", "future external audit", "explicit user authorization"],
                BlocksRuntime: true,
                AllowsRuntimeLive: false,
                AllowsReleaseCommercial: false))
            .ToList();

    private static IReadOnlyList<ControlledExecutionNegativeCapabilityContract> NegativeCapabilities() =>
    [
        new(
            CapabilityId: "controlled.execution.negative-capability.contract.v1",
            CannotExecuteApproval: true,
            CannotMutateApprovalState: true,
            CannotInvokeWriter: true,
            CannotInvokePolicyProductivePath: true,
            CannotStartRuntime: true,
            CannotRegisterService: true,
            CannotCreateCommandHandler: true,
            CannotWriteFilesystem: true,
            CannotUseDatabase: true,
            CannotUseProviderCloud: true,
            CannotUseLlmLive: true,
            CannotUseVectorBackend: true,
            CannotUseDurableMemory: true,
            CannotUseBrowserCdp: true,
            CannotUseWcuOcr: true,
            CannotExecuteRecipe: true,
            CannotExportPhysicalFile: true,
            CannotUseClipboardDownload: true,
            CannotClaimReleaseCommercialReady: true)
    ];

    private static IReadOnlyList<string> CurrentApprovalReadOnlyDesignMap() =>
    [
        "ApprovalPacketReadOnlySurface: grouped read-only human review sections and disabled notices.",
        "HumanReviewPacketExportReadOnlyPreview: in-memory copy preview with no physical export.",
        "ApprovalExecutionDesignOnlyProtected: protected execution design spec with all gates blocked.",
        "PhaseE Safety tests: anti-side-effect and overclaim assertions.",
        "PhaseE Recipes tests: deterministic fixture behavior and preview-only assertions."
    ];

    private static IReadOnlyList<string> FutureProtectedDebt() =>
    [
        "External audit of controlled execution readiness design.",
        "State machine implementation design audit before any real transition.",
        "Mutation boundary implementation design audit before any state store.",
        "Writer/policy integration design audit before any productive path.",
        "Durable audit trail design audit before any real mutation.",
        "Physical export policy audit before any artifact leaves memory.",
        "Product action controls security review before any enabled control.",
        "Cross-phase runtime gate audit before runtime/live.",
        "Release/commercial audit remains required and NO-GO."
    ];
}
