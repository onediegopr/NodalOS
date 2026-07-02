namespace OneBrain.Core.Approval;

public enum PhysicalExportPolicyDesignStatus
{
    DesignOnly,
    ReadOnly,
    PreviewOnly,
    Blocked,
    FutureProtected,
    NotImplemented
}

public enum PhysicalExportFormatKind
{
    PdfFuture,
    DocxFuture,
    JsonFuture,
    MarkdownFuture,
    ClipboardFuture,
    DownloadFuture
}

public enum PhysicalExportBlockedReason
{
    NoRedactionRuntime,
    NoDurableAuditTrailImplementation,
    NoDestinationPolicy,
    NoUserConsentPolicy,
    NoEvidenceSelectionPolicy,
    NoRetentionPolicy,
    NoDeletionPolicy,
    NoFilesystemPolicy,
    NoClipboardPolicy,
    NoDownloadPolicy,
    NoFormatRenderer,
    NoExternalAuditApproval,
    NoRuntimeGateApproval,
    NoReleaseCommercialApproval
}

public enum PhysicalExportRequirementScope
{
    Redaction,
    Consent,
    Destination,
    EvidenceSelection,
    AuditTrail,
    RetentionDeletion
}

public sealed record PhysicalExportReadiness(
    string DesignReadinessPercentRange,
    int PhysicalExportImplementationReadinessPercent,
    int FileWriteReadinessPercent,
    int ClipboardReadinessPercent,
    int DownloadReadinessPercent,
    int PdfReadinessPercent,
    int DocxReadinessPercent,
    int JsonFileReadinessPercent,
    int MarkdownFileReadinessPercent,
    int RedactionRuntimeReadinessPercent,
    int DurableAuditTrailImplementationReadinessPercent,
    int RuntimeLiveReadinessPercent,
    bool ReleaseCommercialReady)
{
    public bool KeepsImplementationBlocked =>
        PhysicalExportImplementationReadinessPercent == 0
        && FileWriteReadinessPercent == 0
        && ClipboardReadinessPercent == 0
        && DownloadReadinessPercent == 0
        && PdfReadinessPercent == 0
        && DocxReadinessPercent == 0
        && JsonFileReadinessPercent == 0
        && MarkdownFileReadinessPercent == 0
        && RedactionRuntimeReadinessPercent == 0
        && DurableAuditTrailImplementationReadinessPercent == 0
        && RuntimeLiveReadinessPercent == 0
        && !ReleaseCommercialReady;
}

public sealed record PhysicalExportCapabilityStatus(
    bool HasPhysicalExport,
    bool HasFileWriter,
    bool HasPdfWriter,
    bool HasDocxWriter,
    bool HasJsonFileWriter,
    bool HasMarkdownFileWriter,
    bool HasClipboard,
    bool HasDownload,
    bool HasBrowserDownload,
    bool HasStreamWriter,
    bool HasFilesystemWrite,
    bool HasRedactionRuntime,
    bool HasDurableAuditTrail,
    bool HasServiceRegistration,
    bool HasCommandHandler,
    bool CanExportPhysicalFile,
    bool CanWriteExportFile,
    bool CanCopyToClipboard,
    bool CanDownload);

public sealed record PhysicalExportFormatPreview(
    PhysicalExportFormatKind FormatKind,
    string FormatName,
    IReadOnlyList<PhysicalExportBlockedReason> BlockedReasons,
    bool IsPreviewOnly,
    bool IsPhysicalOutput,
    bool IsGenerated,
    bool IsDownloaded,
    bool IsCopiedToClipboard,
    bool RequiresRedactionRuntimeFuture,
    bool RequiresDurableAuditTrailFuture,
    bool RequiresUserConsentFuture,
    bool RequiresDestinationPolicyFuture,
    bool RequiresExternalAuditFuture);

public sealed record PhysicalExportRedactionRequirement(
    PhysicalExportRequirementScope Scope,
    bool RedactionRuntimeRequiredFuture,
    bool SecretScanRequiredFuture,
    bool PiiPolicyRequiredFuture,
    bool RawPayloadExportForbidden,
    bool EvidenceRefsPreferredFuture,
    bool RedactionRuntimeImplemented,
    bool CanExportRawPayload,
    IReadOnlyList<PhysicalExportBlockedReason> BlockedReasons);

public sealed record PhysicalExportDestinationRequirement(
    PhysicalExportRequirementScope Scope,
    bool DestinationPolicyRequiredFuture,
    bool WorkspaceBoundaryRequiredFuture,
    bool SafePathPolicyRequiredFuture,
    bool ExternalDestinationBlocked,
    bool DestinationValidationImplemented,
    IReadOnlyList<PhysicalExportBlockedReason> BlockedReasons);

public sealed record PhysicalExportConsentRequirement(
    PhysicalExportRequirementScope Scope,
    bool ExplicitUserConsentRequiredFuture,
    bool ConsentImplemented,
    bool ConsentRecordRequiresDurableAuditTrailFuture,
    bool ConsentCanTriggerExport,
    IReadOnlyList<PhysicalExportBlockedReason> BlockedReasons);

public sealed record PhysicalExportEvidenceSelectionRequirement(
    PhysicalExportRequirementScope Scope,
    IReadOnlyList<string> AllowedEvidenceClassesFuture,
    bool SensitiveEvidenceExcludedByDefaultFuture,
    bool EvidenceSelectionPolicyRequiredFuture,
    bool EvidenceSelectionImplemented,
    IReadOnlyList<PhysicalExportBlockedReason> BlockedReasons);

public sealed record PhysicalExportAuditTrailRequirement(
    PhysicalExportRequirementScope Scope,
    bool ExportRequestAuditEventRequiredFuture,
    bool ExportBlockedAuditEventRequiredFuture,
    bool ExportCompletedAuditEventFuture,
    bool DurableAuditTrailImplementationRequiredFuture,
    bool AuditAppendImplemented,
    int AuditAppendCount,
    int PersistedExportEventCount,
    IReadOnlyList<PhysicalExportBlockedReason> BlockedReasons);

public sealed record PhysicalExportRetentionDeletionRequirement(
    PhysicalExportRequirementScope Scope,
    bool RetentionClassRequiredFuture,
    bool DeletionEligibilityRequiredFuture,
    bool TombstoneRequiredFuture,
    bool RetentionWorkflowImplemented,
    bool DeletionWorkflowImplemented,
    IReadOnlyList<PhysicalExportBlockedReason> BlockedReasons);

public sealed record PhysicalExportAntiCapabilityProof(
    bool CannotExportPhysicalFile,
    bool CannotWriteExportFile,
    bool CannotGeneratePdf,
    bool CannotGenerateDocx,
    bool CannotWriteJsonFile,
    bool CannotWriteMarkdownFile,
    bool CannotCopyToClipboard,
    bool CannotDownload,
    bool CannotUseBrowserDownload,
    bool CannotUseFilesystem,
    bool CannotUseStreamWriter,
    bool CannotRunRedactionRuntime,
    bool CannotAppendAuditEvent,
    bool CannotPersistAuditTrail,
    bool CannotRegisterService,
    bool CannotCreateCommandHandler,
    bool CannotMutateApprovalState,
    bool CannotExecuteApproval,
    bool CannotStartRuntime,
    bool CannotUseProviderCloud,
    bool CannotUseLlmLive,
    bool CannotUseBrowserCdp,
    bool CannotUseWcuOcr,
    bool CannotExecuteRecipe,
    bool CannotClaimReleaseReady,
    ApprovalReviewNoSideEffectProof NoSideEffectProof)
{
    public bool Passes =>
        CannotExportPhysicalFile
        && CannotWriteExportFile
        && CannotGeneratePdf
        && CannotGenerateDocx
        && CannotWriteJsonFile
        && CannotWriteMarkdownFile
        && CannotCopyToClipboard
        && CannotDownload
        && CannotUseBrowserDownload
        && CannotUseFilesystem
        && CannotUseStreamWriter
        && CannotRunRedactionRuntime
        && CannotAppendAuditEvent
        && CannotPersistAuditTrail
        && CannotRegisterService
        && CannotCreateCommandHandler
        && CannotMutateApprovalState
        && CannotExecuteApproval
        && CannotStartRuntime
        && CannotUseProviderCloud
        && CannotUseLlmLive
        && CannotUseBrowserCdp
        && CannotUseWcuOcr
        && CannotExecuteRecipe
        && CannotClaimReleaseReady
        && NoSideEffectProof.Passes;
}

public sealed record PhysicalExportFutureImplementationChecklist(
    IReadOnlyList<string> RequiredBeforeRealPhysicalExport,
    IReadOnlyList<string> RequiredExternalAudits,
    IReadOnlyList<string> RequiredPolicyDecisions,
    IReadOnlyList<string> ExplicitNonGoals);

public sealed record PhysicalExportPolicyDesignOnlyProtected(
    string DesignId,
    string Title,
    PhysicalExportPolicyDesignStatus Status,
    string Mode,
    PhysicalExportReadiness Readiness,
    PhysicalExportCapabilityStatus CapabilityStatus,
    IReadOnlyList<PhysicalExportFormatPreview> ExportFormatPreviews,
    IReadOnlyList<PhysicalExportBlockedReason> BlockedReasons,
    PhysicalExportRedactionRequirement RedactionRequirement,
    PhysicalExportDestinationRequirement DestinationRequirement,
    PhysicalExportConsentRequirement ConsentRequirement,
    PhysicalExportEvidenceSelectionRequirement EvidenceSelectionRequirement,
    PhysicalExportAuditTrailRequirement AuditTrailRequirement,
    PhysicalExportRetentionDeletionRequirement RetentionDeletionRequirement,
    PhysicalExportAntiCapabilityProof AntiCapabilityProof,
    PhysicalExportFutureImplementationChecklist FutureImplementationChecklist,
    IReadOnlyList<string> CurrentPhysicalExportBaseline,
    IReadOnlyList<string> Warnings,
    string NextSafeStep)
{
    public int ProductActionCount => 0;
    public int ExportActionCount => ExportFormatPreviews.Count(format => format.IsGenerated || format.IsPhysicalOutput || format.IsDownloaded || format.IsCopiedToClipboard);
    public int FileOutputCount => ExportFormatPreviews.Count(format => format.IsGenerated || format.IsPhysicalOutput);
    public int ClipboardActionCount => ExportFormatPreviews.Count(format => format.IsCopiedToClipboard);
    public int DownloadActionCount => ExportFormatPreviews.Count(format => format.IsDownloaded);
    public bool HasPhysicalExport => CapabilityStatus.HasPhysicalExport;
    public bool HasFileWriter => CapabilityStatus.HasFileWriter;
    public bool HasClipboard => CapabilityStatus.HasClipboard;
    public bool HasDownload => CapabilityStatus.HasDownload;
    public bool HasStreamWriter => CapabilityStatus.HasStreamWriter;
    public bool HasFilesystemWrite => CapabilityStatus.HasFilesystemWrite;
    public bool HasRuntimeLive => Readiness.RuntimeLiveReadinessPercent > 0;
    public bool HasServiceRegistration => CapabilityStatus.HasServiceRegistration;
    public bool HasCommandHandler => CapabilityStatus.HasCommandHandler;
    public bool PassesSafetyProof =>
        Readiness.KeepsImplementationBlocked
        && AntiCapabilityProof.Passes
        && !HasPhysicalExport
        && !HasFileWriter
        && !HasClipboard
        && !HasDownload
        && !HasStreamWriter
        && !HasFilesystemWrite
        && !HasRuntimeLive
        && !HasServiceRegistration
        && !HasCommandHandler
        && !RedactionRequirement.RedactionRuntimeImplemented
        && !RedactionRequirement.CanExportRawPayload
        && !DestinationRequirement.DestinationValidationImplemented
        && !ConsentRequirement.ConsentImplemented
        && !ConsentRequirement.ConsentCanTriggerExport
        && !EvidenceSelectionRequirement.EvidenceSelectionImplemented
        && !AuditTrailRequirement.AuditAppendImplemented
        && !RetentionDeletionRequirement.RetentionWorkflowImplemented
        && !RetentionDeletionRequirement.DeletionWorkflowImplemented
        && ProductActionCount == 0
        && ExportActionCount == 0
        && FileOutputCount == 0
        && ClipboardActionCount == 0
        && DownloadActionCount == 0
        && AuditTrailRequirement.AuditAppendCount == 0
        && AuditTrailRequirement.PersistedExportEventCount == 0;
}

public static class PhysicalExportPolicyDesignOnlyProtectedPresenter
{
    public static PhysicalExportPolicyDesignOnlyProtected CreateFixture() => BuildDefault();

    public static PhysicalExportPolicyDesignOnlyProtected BuildDefault() =>
        new(
            DesignId: "nodal-os.approval.physical-export-policy.design-only.protected.fixture.v1",
            Title: "Physical Export Policy Design-Only Protected Spec",
            Status: PhysicalExportPolicyDesignStatus.DesignOnly,
            Mode: "DESIGN_ONLY_READ_ONLY_PREVIEW_NO_PHYSICAL_EXPORT_NO_IO_NO_RUNTIME",
            Readiness: GetReadiness(),
            CapabilityStatus: GetCapabilityStatus(),
            ExportFormatPreviews: GetExportFormatPreviews(),
            BlockedReasons: GetBlockedReasons(),
            RedactionRequirement: GetRedactionRequirement(),
            DestinationRequirement: GetDestinationRequirement(),
            ConsentRequirement: GetConsentRequirement(),
            EvidenceSelectionRequirement: GetEvidenceSelectionRequirement(),
            AuditTrailRequirement: GetAuditTrailRequirement(),
            RetentionDeletionRequirement: GetRetentionDeletionRequirement(),
            AntiCapabilityProof: GetAntiCapabilityProof(),
            FutureImplementationChecklist: GetFutureImplementationChecklist(),
            CurrentPhysicalExportBaseline: CurrentPhysicalExportBaseline(),
            Warnings:
            [
                "Physical export policy design may increase. Physical export readiness remains 0%.",
                "Every export format is preview-only and cannot create files, clipboard content or downloads.",
                "Redaction, consent, destination, audit trail, retention and deletion requirements are future-only blockers."
            ],
            NextSafeStep: "NODAL_OS_PHYSICAL_EXPORT_POLICY_EXTERNAL_AUDIT");

    public static PhysicalExportReadiness GetReadiness() =>
        new(
            DesignReadinessPercentRange: "70-85%",
            PhysicalExportImplementationReadinessPercent: 0,
            FileWriteReadinessPercent: 0,
            ClipboardReadinessPercent: 0,
            DownloadReadinessPercent: 0,
            PdfReadinessPercent: 0,
            DocxReadinessPercent: 0,
            JsonFileReadinessPercent: 0,
            MarkdownFileReadinessPercent: 0,
            RedactionRuntimeReadinessPercent: 0,
            DurableAuditTrailImplementationReadinessPercent: 0,
            RuntimeLiveReadinessPercent: 0,
            ReleaseCommercialReady: false);

    public static PhysicalExportCapabilityStatus GetCapabilityStatus() =>
        new(
            HasPhysicalExport: false,
            HasFileWriter: false,
            HasPdfWriter: false,
            HasDocxWriter: false,
            HasJsonFileWriter: false,
            HasMarkdownFileWriter: false,
            HasClipboard: false,
            HasDownload: false,
            HasBrowserDownload: false,
            HasStreamWriter: false,
            HasFilesystemWrite: false,
            HasRedactionRuntime: false,
            HasDurableAuditTrail: false,
            HasServiceRegistration: false,
            HasCommandHandler: false,
            CanExportPhysicalFile: false,
            CanWriteExportFile: false,
            CanCopyToClipboard: false,
            CanDownload: false);

    public static IReadOnlyList<PhysicalExportFormatPreview> GetExportFormatPreviews() =>
        Enum.GetValues<PhysicalExportFormatKind>()
            .Select(kind => new PhysicalExportFormatPreview(
                FormatKind: kind,
                FormatName: kind.ToString(),
                BlockedReasons: GetBlockedReasons(),
                IsPreviewOnly: true,
                IsPhysicalOutput: false,
                IsGenerated: false,
                IsDownloaded: false,
                IsCopiedToClipboard: false,
                RequiresRedactionRuntimeFuture: true,
                RequiresDurableAuditTrailFuture: true,
                RequiresUserConsentFuture: true,
                RequiresDestinationPolicyFuture: true,
                RequiresExternalAuditFuture: true))
            .ToList();

    public static IReadOnlyList<PhysicalExportBlockedReason> GetBlockedReasons() =>
        Enum.GetValues<PhysicalExportBlockedReason>();

    public static PhysicalExportRedactionRequirement GetRedactionRequirement() =>
        new(
            Scope: PhysicalExportRequirementScope.Redaction,
            RedactionRuntimeRequiredFuture: true,
            SecretScanRequiredFuture: true,
            PiiPolicyRequiredFuture: true,
            RawPayloadExportForbidden: true,
            EvidenceRefsPreferredFuture: true,
            RedactionRuntimeImplemented: false,
            CanExportRawPayload: false,
            BlockedReasons:
            [
                PhysicalExportBlockedReason.NoRedactionRuntime,
                PhysicalExportBlockedReason.NoEvidenceSelectionPolicy,
                PhysicalExportBlockedReason.NoExternalAuditApproval
            ]);

    public static PhysicalExportDestinationRequirement GetDestinationRequirement() =>
        new(
            Scope: PhysicalExportRequirementScope.Destination,
            DestinationPolicyRequiredFuture: true,
            WorkspaceBoundaryRequiredFuture: true,
            SafePathPolicyRequiredFuture: true,
            ExternalDestinationBlocked: true,
            DestinationValidationImplemented: false,
            BlockedReasons:
            [
                PhysicalExportBlockedReason.NoDestinationPolicy,
                PhysicalExportBlockedReason.NoFilesystemPolicy,
                PhysicalExportBlockedReason.NoDownloadPolicy,
                PhysicalExportBlockedReason.NoExternalAuditApproval
            ]);

    public static PhysicalExportConsentRequirement GetConsentRequirement() =>
        new(
            Scope: PhysicalExportRequirementScope.Consent,
            ExplicitUserConsentRequiredFuture: true,
            ConsentImplemented: false,
            ConsentRecordRequiresDurableAuditTrailFuture: true,
            ConsentCanTriggerExport: false,
            BlockedReasons:
            [
                PhysicalExportBlockedReason.NoUserConsentPolicy,
                PhysicalExportBlockedReason.NoDurableAuditTrailImplementation,
                PhysicalExportBlockedReason.NoRuntimeGateApproval
            ]);

    public static PhysicalExportEvidenceSelectionRequirement GetEvidenceSelectionRequirement() =>
        new(
            Scope: PhysicalExportRequirementScope.EvidenceSelection,
            AllowedEvidenceClassesFuture:
            [
                "approval packet refs future",
                "human review packet refs future",
                "risk summary refs future",
                "policy decision refs future",
                "redacted evidence snapshot refs future"
            ],
            SensitiveEvidenceExcludedByDefaultFuture: true,
            EvidenceSelectionPolicyRequiredFuture: true,
            EvidenceSelectionImplemented: false,
            BlockedReasons:
            [
                PhysicalExportBlockedReason.NoEvidenceSelectionPolicy,
                PhysicalExportBlockedReason.NoRedactionRuntime,
                PhysicalExportBlockedReason.NoExternalAuditApproval
            ]);

    public static PhysicalExportAuditTrailRequirement GetAuditTrailRequirement() =>
        new(
            Scope: PhysicalExportRequirementScope.AuditTrail,
            ExportRequestAuditEventRequiredFuture: true,
            ExportBlockedAuditEventRequiredFuture: true,
            ExportCompletedAuditEventFuture: true,
            DurableAuditTrailImplementationRequiredFuture: true,
            AuditAppendImplemented: false,
            AuditAppendCount: 0,
            PersistedExportEventCount: 0,
            BlockedReasons:
            [
                PhysicalExportBlockedReason.NoDurableAuditTrailImplementation,
                PhysicalExportBlockedReason.NoExternalAuditApproval,
                PhysicalExportBlockedReason.NoRuntimeGateApproval
            ]);

    public static PhysicalExportRetentionDeletionRequirement GetRetentionDeletionRequirement() =>
        new(
            Scope: PhysicalExportRequirementScope.RetentionDeletion,
            RetentionClassRequiredFuture: true,
            DeletionEligibilityRequiredFuture: true,
            TombstoneRequiredFuture: true,
            RetentionWorkflowImplemented: false,
            DeletionWorkflowImplemented: false,
            BlockedReasons:
            [
                PhysicalExportBlockedReason.NoRetentionPolicy,
                PhysicalExportBlockedReason.NoDeletionPolicy,
                PhysicalExportBlockedReason.NoDurableAuditTrailImplementation
            ]);

    public static PhysicalExportAntiCapabilityProof GetAntiCapabilityProof()
    {
        var proof = ApprovalReviewNoSideEffectProof.FixtureReadOnly();

        return new PhysicalExportAntiCapabilityProof(
            CannotExportPhysicalFile: true,
            CannotWriteExportFile: true,
            CannotGeneratePdf: true,
            CannotGenerateDocx: true,
            CannotWriteJsonFile: true,
            CannotWriteMarkdownFile: true,
            CannotCopyToClipboard: true,
            CannotDownload: true,
            CannotUseBrowserDownload: true,
            CannotUseFilesystem: true,
            CannotUseStreamWriter: true,
            CannotRunRedactionRuntime: true,
            CannotAppendAuditEvent: true,
            CannotPersistAuditTrail: true,
            CannotRegisterService: true,
            CannotCreateCommandHandler: true,
            CannotMutateApprovalState: true,
            CannotExecuteApproval: true,
            CannotStartRuntime: true,
            CannotUseProviderCloud: true,
            CannotUseLlmLive: true,
            CannotUseBrowserCdp: true,
            CannotUseWcuOcr: true,
            CannotExecuteRecipe: true,
            CannotClaimReleaseReady: true,
            NoSideEffectProof: proof);
    }

    public static PhysicalExportFutureImplementationChecklist GetFutureImplementationChecklist() =>
        new(
            RequiredBeforeRealPhysicalExport:
            [
                "redaction runtime design external audit",
                "durable audit trail implementation approval",
                "destination policy and workspace boundary approval",
                "explicit user consent policy",
                "evidence selection policy",
                "retention and deletion policy",
                "format renderer review for each future format",
                "runtime gate approval",
                "release/commercial claim audit"
            ],
            RequiredExternalAudits:
            [
                "physical export policy external audit",
                "redaction runtime external audit",
                "durable audit trail implementation audit",
                "destination and sensitive evidence leakage audit",
                "release/commercial claim audit"
            ],
            RequiredPolicyDecisions:
            [
                "which evidence classes can be selected",
                "which payloads must remain refs only",
                "which destinations are allowed",
                "how user consent is recorded",
                "which audit events must exist before and after an export request",
                "how retention and deletion apply to exported artifacts"
            ],
            ExplicitNonGoals:
            [
                "no physical export",
                "no file read or write",
                "no clipboard",
                "no download",
                "no stream writer",
                "no PDF or DOCX generation",
                "no JSON or Markdown physical output",
                "no redaction runtime",
                "no durable audit trail implementation",
                "no approval execution or mutation",
                "no runtime/live",
                "no release/commercial readiness"
            ]);

    private static IReadOnlyList<string> CurrentPhysicalExportBaseline() =>
    [
        "HumanReviewPacketExportReadOnlyPreview exposes markdown-like and json-like text previews in memory only.",
        "Existing export preview manifest keeps PhysicalFileCreated, ClipboardUsed, DownloadStarted and ExportActionsCount at zero.",
        "ControlledExecutionReadinessDesignTrack models physical export policy as future-only and blocked.",
        "ApprovalDurableAuditTrailDesignOnlyProtected requires export-related audit events but cannot append or persist them.",
        "No file writer, clipboard, browser download, stream writer, DB, provider/cloud, service registration or command handler exists."
    ];
}
