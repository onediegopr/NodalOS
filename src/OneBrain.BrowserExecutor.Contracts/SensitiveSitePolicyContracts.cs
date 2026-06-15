namespace OneBrain.BrowserExecutor.Contracts;

public enum SensitiveSiteCategory
{
    Fiscal,
    Banking,
    Financial,
    ERP,
    Payroll,
    Healthcare,
    Legal,
    Government,
    Identity,
    Payments,
    ProductionAdmin,
    CustomerData,
    Unknown
}

public enum SensitiveSiteRiskLevel
{
    LowRisk,
    MediumRisk,
    HighRisk,
    Critical,
    Prohibited
}

public enum SensitiveSiteCapability
{
    ReadOnly,
    SafeDownload,
    SafeUpload,
    FormFill,
    DraftSave,
    Submit,
    Payment,
    Signing,
    ProductiveReplay,
    ProductiveRecorder
}

public enum SensitiveSiteActionKind
{
    ReadOnlyView,
    DownloadDocument,
    UploadDocument,
    FillForm,
    SaveDraft,
    Submit,
    Pay,
    Delete,
    Publish,
    Approve,
    Sign,
    ChangeCredentials,
    ChangeProfile,
    ExportData,
    ImportData
}

public enum SensitiveSiteDecisionKind
{
    AllowReadOnlyWithApproval,
    RequiresApproval,
    RequiresDoubleApproval,
    Blocked,
    Prohibited
}

public enum SensitiveSiteHumanApprovalRequirement
{
    NoApprovalRequired,
    SingleApprovalRequired,
    DoubleApprovalRequired,
    ExplicitTypedConfirmationRequired,
    Prohibited
}

public enum SensitiveSiteReason
{
    UnknownDomain,
    UnknownCategory,
    SensitiveSiteRequiresApproval,
    SensitiveDownloadRequiresSafeDownload,
    SensitiveUploadRequiresSafeUpload,
    IrreversibleActionBlocked,
    PaymentBlocked,
    SubmitBlocked,
    SigningBlocked,
    ProfileRawBlocked,
    ProductiveReplayBlocked,
    ProductiveRecorderBlocked,
    GateFailed,
    UnsafeNetworkCapture,
    AllowedReadOnlySimulation
}

public sealed record SensitiveSiteClassification(
    string Host,
    SensitiveSiteCategory Category,
    SensitiveSiteRiskLevel RiskLevel,
    bool TestOnlySimulation);

public sealed record SensitiveSiteAuditRequirement(
    bool IncludeCategory,
    bool IncludeRiskLevel,
    bool IncludeActionKind,
    bool IncludePolicyDecision,
    bool IncludeApprovalRefs,
    bool IncludeGateRefs,
    bool IncludeEvidenceRefs,
    bool ExcludeSecretsCookiesBodies);

public sealed record SensitiveSitePolicy(
    IReadOnlyDictionary<string, SensitiveSiteClassification> Classifications,
    bool AllowReadOnlySimulation,
    bool AllowRealPilot,
    bool RequireSafeDownloadForDocumentDownload,
    bool RequireSafeUploadForDocumentUpload)
{
    public static SensitiveSitePolicy Default(IReadOnlyDictionary<string, SensitiveSiteClassification>? classifications = null) =>
        new(
            classifications ?? new Dictionary<string, SensitiveSiteClassification>(StringComparer.OrdinalIgnoreCase),
            AllowReadOnlySimulation: true,
            AllowRealPilot: false,
            RequireSafeDownloadForDocumentDownload: true,
            RequireSafeUploadForDocumentUpload: true);
}

public sealed record SensitiveSitePolicyContext(
    bool UserConsentValid,
    SensitiveSiteHumanApprovalRequirement ApprovalProvided,
    BrowserRuntimePhaseCloseReport? GateReport,
    BrowserRuntimeProfileState ProfileState,
    BrowserRuntimeVaultState VaultState,
    BrowserRuntimeReplayState ReplayState,
    BrowserRuntimeRecorderState RecorderState,
    BrowserNetworkCaptureMode NetworkCaptureMode,
    bool RequestBodyCaptureSupported,
    bool ResponseBodyCaptureSupported,
    bool SensitiveHeaderValueCaptureSupported,
    bool SafeDownloadAvailable,
    bool SafeUploadAvailable);

public sealed record SensitiveSitePolicyRequest(
    string RunId,
    string ActionId,
    string CorrelationId,
    Uri SiteUri,
    SensitiveSiteActionKind ActionKind,
    bool DataSensitive,
    IReadOnlyList<string> HumanApprovalRefs,
    IReadOnlyList<string> EvidenceRefs,
    SensitiveSitePolicyContext Context);

public sealed record SensitiveSitePolicyViolation(SensitiveSiteReason Reason, string Message);

public sealed record SensitiveSitePolicyDecision(
    SensitiveSiteDecisionKind Decision,
    SensitiveSiteClassification? Classification,
    SensitiveSiteHumanApprovalRequirement ApprovalRequirement,
    IReadOnlyList<SensitiveSiteReason> ReasonCodes,
    IReadOnlyList<SensitiveSitePolicyViolation> Violations,
    IReadOnlyList<string> WhatRemainsBlocked,
    SensitiveSiteAuditRequirement AuditRequirement,
    BrowserAuditLedgerEvent AuditEvent,
    bool Redacted)
{
    public bool Allowed =>
        Decision is SensitiveSiteDecisionKind.AllowReadOnlyWithApproval or SensitiveSiteDecisionKind.RequiresApproval &&
        Violations.Count == 0 &&
        Redacted;
}

