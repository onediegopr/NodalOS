namespace OneBrain.Core.Approval;

public enum ApprovalExecutionDesignStatus
{
    DesignOnly,
    ReadOnly,
    PreviewOnly,
    FutureProtected,
    Blocked
}

public enum ApprovalExecutionBlockedReason
{
    ExecutionNotAuthorized,
    StateMutationNotAuthorized,
    WriterPolicyPathNotAuthorized,
    RuntimeLiveNotAuthorized,
    PhysicalExportNotAuthorized,
    ClipboardDownloadNotAuthorized,
    FilesystemProductIoNotAuthorized,
    DatabaseNotAuthorized,
    ProviderCloudNotAuthorized,
    LlmLiveNotAuthorized,
    ServiceRegistrationNotAuthorized,
    ReleaseCommercialNotAuthorized
}

public sealed record ApprovalExecutionReadiness(
    int ApprovalExecutionReadinessPercent,
    int ApprovalStateMutationReadinessPercent,
    int RuntimeLiveReadinessPercent,
    int PhysicalExportReadinessPercent,
    bool ProductiveWriterPolicyPathAvailable,
    bool CommandHandlerAvailable,
    bool ProductServiceRegistered,
    bool ReleaseCommercialReady)
{
    public bool BlocksRealExecution =>
        ApprovalExecutionReadinessPercent == 0
        && ApprovalStateMutationReadinessPercent == 0
        && RuntimeLiveReadinessPercent == 0
        && PhysicalExportReadinessPercent == 0
        && !ProductiveWriterPolicyPathAvailable
        && !CommandHandlerAvailable
        && !ProductServiceRegistered
        && !ReleaseCommercialReady;
}

public sealed record ApprovalExecutionGate(
    string GateId,
    string Title,
    ApprovalExecutionDesignStatus Status,
    ApprovalExecutionBlockedReason BlockedReason,
    string FutureRequirement,
    bool AllowsRealExecution,
    bool AllowsStateMutation,
    bool AllowsRuntimeLive,
    bool AllowsPhysicalExport,
    bool AllowsProductAction);

public sealed record ApprovalExecutionPreview(
    string PreviewId,
    ApprovalDecisionOptionKind DecisionOption,
    string Label,
    ApprovalExecutionDesignStatus Status,
    IReadOnlyList<string> RequiredGateIds,
    IReadOnlyList<string> BlockedReasons,
    bool PreviewOnly,
    bool ExecutesApproval,
    bool MutatesState,
    bool ExposesProductAction,
    bool StartsRuntime,
    bool CreatesPhysicalExport);

public sealed record ApprovalExecutionAntiCapabilityProof(
    bool ReadOnly,
    bool DesignOnly,
    bool Deterministic,
    bool InMemoryOnly,
    bool NoRealExecution,
    bool NoStateMutation,
    bool NoProductiveWriterPolicyPath,
    bool NoCommandHandler,
    bool NoProductServiceRegistration,
    bool NoRuntimeLive,
    bool NoPhysicalExport,
    bool NoClipboardDownload,
    bool NoFilesystemProductIo,
    bool NoDatabase,
    bool NoProviderCloud,
    bool NoLlmLive,
    bool NoDurableMemory,
    bool NoBrowserCdpLive,
    bool NoWcuOcrLive,
    bool NoRecipeExecution,
    ApprovalReviewNoSideEffectProof NoSideEffectProof)
{
    public bool Passes =>
        ReadOnly
        && DesignOnly
        && Deterministic
        && InMemoryOnly
        && NoRealExecution
        && NoStateMutation
        && NoProductiveWriterPolicyPath
        && NoCommandHandler
        && NoProductServiceRegistration
        && NoRuntimeLive
        && NoPhysicalExport
        && NoClipboardDownload
        && NoFilesystemProductIo
        && NoDatabase
        && NoProviderCloud
        && NoLlmLive
        && NoDurableMemory
        && NoBrowserCdpLive
        && NoWcuOcrLive
        && NoRecipeExecution
        && NoSideEffectProof.Passes;
}

public sealed record ApprovalExecutionDesignSpec(
    string SpecId,
    string Title,
    ApprovalExecutionDesignStatus Status,
    string Mode,
    ApprovalExecutionReadiness Readiness,
    IReadOnlyList<ApprovalExecutionGate> Gates,
    IReadOnlyList<ApprovalExecutionPreview> Previews,
    IReadOnlyList<string> AntiCapabilities,
    IReadOnlyList<string> FutureProtectedRequirements,
    IReadOnlyList<string> Warnings,
    IReadOnlyList<string> Blockers,
    string NextSafeStep,
    ApprovalExecutionAntiCapabilityProof AntiCapabilityProof)
{
    public bool ReadOnly => AntiCapabilityProof.ReadOnly;
    public bool DesignOnly => AntiCapabilityProof.DesignOnly;
    public bool PreviewOnly => Previews.All(preview => preview.PreviewOnly);
    public bool HasRealExecution => Previews.Any(preview => preview.ExecutesApproval) || Gates.Any(gate => gate.AllowsRealExecution);
    public bool HasStateMutation => Previews.Any(preview => preview.MutatesState) || Gates.Any(gate => gate.AllowsStateMutation);
    public bool HasRuntimeLive => Previews.Any(preview => preview.StartsRuntime) || Gates.Any(gate => gate.AllowsRuntimeLive);
    public bool HasPhysicalExport => Previews.Any(preview => preview.CreatesPhysicalExport) || Gates.Any(gate => gate.AllowsPhysicalExport);
    public bool HasProductActions => Previews.Any(preview => preview.ExposesProductAction) || Gates.Any(gate => gate.AllowsProductAction);
}

public static class ApprovalExecutionDesignOnlyProtectedPresenter
{
    public static ApprovalExecutionDesignSpec CreateFixture()
    {
        var proof = ApprovalReviewNoSideEffectProof.FixtureReadOnly();
        var antiCapabilityProof = new ApprovalExecutionAntiCapabilityProof(
            ReadOnly: true,
            DesignOnly: true,
            Deterministic: true,
            InMemoryOnly: true,
            NoRealExecution: true,
            NoStateMutation: true,
            NoProductiveWriterPolicyPath: true,
            NoCommandHandler: true,
            NoProductServiceRegistration: true,
            NoRuntimeLive: true,
            NoPhysicalExport: true,
            NoClipboardDownload: true,
            NoFilesystemProductIo: true,
            NoDatabase: true,
            NoProviderCloud: true,
            NoLlmLive: true,
            NoDurableMemory: true,
            NoBrowserCdpLive: true,
            NoWcuOcrLive: true,
            NoRecipeExecution: true,
            NoSideEffectProof: proof);

        var readiness = new ApprovalExecutionReadiness(
            ApprovalExecutionReadinessPercent: 0,
            ApprovalStateMutationReadinessPercent: 0,
            RuntimeLiveReadinessPercent: 0,
            PhysicalExportReadinessPercent: 0,
            ProductiveWriterPolicyPathAvailable: false,
            CommandHandlerAvailable: false,
            ProductServiceRegistered: false,
            ReleaseCommercialReady: false);

        var gates = Gates();
        var gateIds = gates.Select(gate => gate.GateId).ToList();
        var previews = new[]
        {
            Preview("approval.execution.preview.approve", ApprovalDecisionOptionKind.ApprovePreviewOnly, "Approve execution design preview", gateIds),
            Preview("approval.execution.preview.reject", ApprovalDecisionOptionKind.RejectPreviewOnly, "Reject execution design preview", gateIds),
            Preview("approval.execution.preview.request-evidence", ApprovalDecisionOptionKind.RequestMoreEvidence, "Request evidence design preview", gateIds),
            Preview("approval.execution.preview.request-context-refresh", ApprovalDecisionOptionKind.RequestContextRefresh, "Request context refresh design preview", gateIds),
            Preview("approval.execution.preview.defer", ApprovalDecisionOptionKind.Defer, "Defer design preview", gateIds)
        };

        return new ApprovalExecutionDesignSpec(
            SpecId: "nodal-os.approval.execution.design-only.protected.fixture.v1",
            Title: "Approval Execution Design-Only Protected Spec",
            Status: ApprovalExecutionDesignStatus.DesignOnly,
            Mode: "DESIGN_ONLY_READ_ONLY_PREVIEW_NO_EXECUTION_NO_MUTATION_NO_RUNTIME",
            Readiness: readiness,
            Gates: gates,
            Previews: previews,
            AntiCapabilities: AntiCapabilities(),
            FutureProtectedRequirements: FutureProtectedRequirements(),
            Warnings:
            [
                "Approval execution is only modeled as a future protected design.",
                "Approval action previews are labels, not commands.",
                "No design item can unlock runtime, mutation, export or release readiness."
            ],
            Blockers: gates.Select(gate => $"{gate.GateId}: {gate.BlockedReason}.").ToList(),
            NextSafeStep: "NODAL_OS_APPROVAL_EXECUTION_DESIGN_ONLY_EXTERNAL_AUDIT_OR_PAUSE",
            AntiCapabilityProof: antiCapabilityProof);
    }

    private static IReadOnlyList<ApprovalExecutionGate> Gates() =>
    [
        Gate("approval.execution.authorization", "Execution authorization gate", ApprovalExecutionBlockedReason.ExecutionNotAuthorized, "future explicit operator authorization and protected execution hito"),
        Gate("approval.state.mutation.authorization", "State mutation authorization gate", ApprovalExecutionBlockedReason.StateMutationNotAuthorized, "future durable state model and mutation audit trail"),
        Gate("writer.policy.boundary", "Writer/policy boundary gate", ApprovalExecutionBlockedReason.WriterPolicyPathNotAuthorized, "future protected writer and policy design review"),
        Gate("runtime.live.boundary", "Runtime/live boundary gate", ApprovalExecutionBlockedReason.RuntimeLiveNotAuthorized, "future runtime authority and live safety review"),
        Gate("physical.export.boundary", "Physical export boundary gate", ApprovalExecutionBlockedReason.PhysicalExportNotAuthorized, "future export policy and operator approval"),
        Gate("clipboard.download.boundary", "Clipboard/download boundary gate", ApprovalExecutionBlockedReason.ClipboardDownloadNotAuthorized, "future copy/download policy and safety review"),
        Gate("filesystem.product.io.boundary", "Filesystem product IO gate", ApprovalExecutionBlockedReason.FilesystemProductIoNotAuthorized, "future product IO policy and audit trail"),
        Gate("database.boundary", "Database boundary gate", ApprovalExecutionBlockedReason.DatabaseNotAuthorized, "future DB schema, migration and persistence review"),
        Gate("provider.cloud.boundary", "Provider/cloud boundary gate", ApprovalExecutionBlockedReason.ProviderCloudNotAuthorized, "future provider, network and cloud policy"),
        Gate("llm.live.boundary", "LLM live boundary gate", ApprovalExecutionBlockedReason.LlmLiveNotAuthorized, "future live model policy and audit review"),
        Gate("service.registration.boundary", "Service registration boundary gate", ApprovalExecutionBlockedReason.ServiceRegistrationNotAuthorized, "future product service registration review"),
        Gate("release.commercial.boundary", "Release/commercial boundary gate", ApprovalExecutionBlockedReason.ReleaseCommercialNotAuthorized, "future release and commercial audit")
    ];

    private static ApprovalExecutionGate Gate(
        string gateId,
        string title,
        ApprovalExecutionBlockedReason reason,
        string futureRequirement) =>
        new(
            GateId: gateId,
            Title: title,
            Status: ApprovalExecutionDesignStatus.Blocked,
            BlockedReason: reason,
            FutureRequirement: futureRequirement,
            AllowsRealExecution: false,
            AllowsStateMutation: false,
            AllowsRuntimeLive: false,
            AllowsPhysicalExport: false,
            AllowsProductAction: false);

    private static ApprovalExecutionPreview Preview(
        string previewId,
        ApprovalDecisionOptionKind option,
        string label,
        IReadOnlyList<string> requiredGateIds) =>
        new(
            PreviewId: previewId,
            DecisionOption: option,
            Label: label,
            Status: ApprovalExecutionDesignStatus.PreviewOnly,
            RequiredGateIds: requiredGateIds,
            BlockedReasons: Enum.GetNames<ApprovalExecutionBlockedReason>(),
            PreviewOnly: true,
            ExecutesApproval: false,
            MutatesState: false,
            ExposesProductAction: false,
            StartsRuntime: false,
            CreatesPhysicalExport: false);

    private static IReadOnlyList<string> AntiCapabilities() =>
    [
        "No real approval execution.",
        "No approval state mutation.",
        "No productive writer/policy boundary.",
        "No runtime/live.",
        "No physical export.",
        "No clipboard/download.",
        "No filesystem product IO.",
        "No DB.",
        "No provider/cloud/network.",
        "No LLM live.",
        "No durable memory.",
        "No browser/CDP live.",
        "No WCU/OCR live.",
        "No recipe execution.",
        "No product service registration.",
        "No release/commercial readiness."
    ];

    private static IReadOnlyList<string> FutureProtectedRequirements() =>
    [
        "Define operator authority model before any real approval execution.",
        "Define durable state mutation and audit trail before any state change.",
        "Define writer/policy boundary in a protected design hito before integration.",
        "Define physical export, clipboard and download policies before any artifact leaves memory.",
        "Define provider/cloud, DB, LLM live and durable memory policies before any live backend is attached.",
        "Run external audit before considering runtime or release readiness."
    ];
}
