namespace OneBrain.BrowserExecutor.Contracts;

public enum BrowserSensitiveSimulationStatus
{
    Blocked,
    Failed,
    Verified
}

public sealed record BrowserSensitiveReadOnlySimulationFixtureServer(
    Uri BaseUri,
    IReadOnlyDictionary<string, string> Endpoints)
{
    public static BrowserSensitiveReadOnlySimulationFixtureServer FiscalLocal() =>
        new(
            new Uri("http://127.0.0.1/sensitive-simulation/"),
            new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["/fiscal/login"] = "synthetic fiscal login fixture",
                ["/fiscal/dashboard"] = "synthetic fiscal dashboard",
                ["/fiscal/read-only-status"] = "synthetic read-only status",
                ["/fiscal/form"] = "synthetic blocked form",
                ["/fiscal/submit-blocked"] = "synthetic submit blocked",
                ["/banking/dashboard"] = "synthetic banking dashboard",
                ["/erp/dashboard"] = "synthetic ERP dashboard"
            });
}

public sealed record BrowserSensitiveReadOnlySimulationPolicy(
    SensitiveSitePolicy SensitiveSitePolicy,
    bool RequireApproval,
    bool RequireGate,
    bool RequireSemanticProof,
    bool RequireReadOnlyGuard);

public sealed record BrowserSensitiveReadOnlySimulationRequest(
    string RunId,
    string ActionId,
    string CorrelationId,
    Uri SiteUri,
    SensitiveSiteCategory SimulatedCategory,
    IReadOnlyList<string> ApprovalRefs,
    BrowserRuntimePhaseCloseReport? GateReport,
    bool DashboardVisible,
    bool StatusVisible,
    bool ReadOnlyGuardActive,
    bool SubmitBlocked,
    bool PaymentBlocked,
    bool SigningBlocked,
    bool DeleteBlocked,
    bool SemanticProofPresent);

public sealed record BrowserSensitiveReadOnlySimulationStep(
    string StepId,
    SensitiveSiteActionKind ActionKind,
    string ExecutionState,
    BrowserVerificationStatus VerificationStatus,
    IReadOnlyList<string> EvidenceRefs,
    IReadOnlyList<string> AuditRefs,
    string FailureReason);

public sealed record BrowserSensitiveReadOnlySimulationEvidence(
    SensitiveSiteCategory Category,
    SensitiveSiteRiskLevel RiskLevel,
    SensitiveSiteHumanApprovalRequirement ApprovalRequirement,
    IReadOnlyList<string> ApprovalRefs,
    IReadOnlyList<string> EvidenceRefs,
    IReadOnlyList<string> WhatRemainsBlocked,
    bool CookiesSecretsBodiesExcluded,
    bool Redacted);

public sealed record BrowserSensitiveReadOnlySimulationResult(
    BrowserSensitiveSimulationStatus Status,
    SensitiveSitePolicyDecision PolicyDecision,
    IReadOnlyList<BrowserSensitiveReadOnlySimulationStep> Steps,
    BrowserSensitiveReadOnlySimulationEvidence Evidence,
    BrowserVerification? Verification,
    BrowserAuditLedgerEvent AuditEvent,
    string Reason,
    bool Redacted)
{
    public bool AllowsDone =>
        Status == BrowserSensitiveSimulationStatus.Verified &&
        PolicyDecision.Allowed &&
        Verification?.AllowsStepDone() == true &&
        Evidence.Redacted &&
        Evidence.CookiesSecretsBodiesExcluded &&
        Redacted;
}

public sealed record BrowserSensitiveDocumentSimulationFixtureServer(
    Uri BaseUri,
    IReadOnlyList<string> SyntheticDocuments,
    IReadOnlyDictionary<string, string> Endpoints)
{
    public static BrowserSensitiveDocumentSimulationFixtureServer FiscalLocal() =>
        new(
            new Uri("http://127.0.0.1/sensitive-document-simulation/"),
            ["synthetic-sensitive-simulation.pdf", "synthetic-tax-summary.json", "synthetic-erp-export.csv"],
            new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["/fiscal/document-page"] = "synthetic document page",
                ["/fiscal/download-synthetic-document"] = "synthetic document download",
                ["/fiscal/upload-page"] = "synthetic upload page",
                ["/fiscal/upload"] = "synthetic upload endpoint",
                ["/fiscal/document-status"] = "synthetic document status"
            });
}

public sealed record BrowserSensitiveDocumentSimulationPolicy(
    SensitiveSitePolicy SensitiveSitePolicy,
    BrowserSafeDownloadPolicy DownloadPolicy,
    BrowserSafeUploadPolicy UploadPolicy,
    bool RequireApprovalForDownload,
    bool RequireApprovalForUpload,
    bool BlockDocumentContentCapture);

public sealed record BrowserSensitiveDocumentSimulationRequest(
    string RunId,
    string ActionId,
    string CorrelationId,
    Uri SiteUri,
    IReadOnlyList<string> DownloadApprovalRefs,
    IReadOnlyList<string> UploadApprovalRefs,
    BrowserRuntimePhaseCloseReport? GateReport,
    BrowserSafeDownloadRequest DownloadRequest,
    string MaterializedDownloadPath,
    BrowserSafeUploadRequest UploadRequest,
    bool DocumentContentCaptureAttempted,
    bool SubmitAfterUploadAttempted,
    bool PaymentAttempted,
    bool SigningAttempted,
    bool DeleteAttempted,
    bool FinalStatusVisible,
    bool SemanticProofPresent);

public sealed record BrowserSensitiveDocumentSimulationEvidence(
    SensitiveSiteCategory Category,
    SensitiveSiteRiskLevel RiskLevel,
    IReadOnlyList<string> ApprovalRefs,
    BrowserSafeDownloadArtifact? DownloadArtifact,
    BrowserSafeUploadArtifact? UploadArtifact,
    IReadOnlyList<string> EvidenceRefs,
    IReadOnlyList<string> ReasonCodes,
    bool DoubleApprovalModeled,
    bool DocumentContentCaptured,
    bool CookiesSecretsBodiesExcluded,
    bool Redacted);

public sealed record BrowserSensitiveDocumentSimulationResult(
    BrowserSensitiveSimulationStatus Status,
    SensitiveSitePolicyDecision DownloadPolicyDecision,
    SensitiveSitePolicyDecision UploadPolicyDecision,
    BrowserSafeDownloadResult? DownloadResult,
    BrowserSafeUploadResult? UploadResult,
    BrowserSensitiveDocumentSimulationEvidence Evidence,
    BrowserVerification? Verification,
    BrowserAuditLedgerEvent AuditEvent,
    string Reason,
    bool Redacted)
{
    public bool AllowsDone =>
        Status == BrowserSensitiveSimulationStatus.Verified &&
        DownloadPolicyDecision.Allowed &&
        UploadPolicyDecision.Allowed &&
        DownloadResult?.AllowsDone == true &&
        UploadResult?.AllowsDone == true &&
        Verification?.AllowsStepDone() == true &&
        Evidence.DoubleApprovalModeled &&
        !Evidence.DocumentContentCaptured &&
        Evidence.CookiesSecretsBodiesExcluded &&
        Redacted;
}
