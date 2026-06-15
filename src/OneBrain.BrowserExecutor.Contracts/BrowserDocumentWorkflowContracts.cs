namespace OneBrain.BrowserExecutor.Contracts;

public enum BrowserDocumentWorkflowStatus
{
    Created,
    Running,
    Completed,
    Failed,
    Blocked
}

public enum BrowserDocumentWorkflowStepKind
{
    Consent,
    Gate,
    ProfileControlled,
    VaultRetrieval,
    AuthenticatedSandboxLogin,
    SafeDownload,
    LocalTransform,
    SafeUpload,
    FinalStatusVerification,
    Cleanup
}

public sealed record BrowserDocumentWorkflowPolicy(
    bool RequireConsent,
    bool RequireGate,
    bool RequireProfileControlled,
    bool RequireVaultReference,
    BrowserSafeDownloadPolicy DownloadPolicy,
    BrowserSafeUploadPolicy UploadPolicy,
    bool RequireFinalSemanticProof,
    bool FailOnAuditLeak);

public sealed record BrowserDocumentWorkflowRequest(
    string RunId,
    string ActionId,
    string CorrelationId,
    string SessionId,
    BrowserConsentGrant? Consent,
    BrowserRuntimePhaseCloseReport? GateReport,
    BrowserVaultSecretReference? VaultReference,
    bool VaultReferenceRevoked,
    bool ProfileControlled,
    BrowserSafeUploadApproval? UploadApproval);

public sealed record BrowserDocumentWorkflowStep(
    string StepId,
    BrowserDocumentWorkflowStepKind Kind,
    BrowserDocumentWorkflowStatus Status,
    string VerificationStatus,
    IReadOnlyList<string> EvidenceRefs,
    IReadOnlyList<string> AuditRefs,
    string FailureReason = "")
{
    public bool Verified => Status == BrowserDocumentWorkflowStatus.Completed && string.Equals(VerificationStatus, "Verified", StringComparison.OrdinalIgnoreCase) && EvidenceRefs.Count > 0;
}

public sealed record BrowserDocumentWorkflowEvidence(
    IReadOnlyList<string> ProofRefs,
    IReadOnlyList<string> EvidenceRefs,
    bool SemanticProofPresent,
    bool Redacted);

public sealed record BrowserDocumentWorkflowAuditSummary(
    IReadOnlyList<BrowserAuditLedgerEvent> Events,
    bool HmacHeadVerified,
    bool ContainsLeak,
    bool Redacted)
{
    public bool IsSafe => HmacHeadVerified && !ContainsLeak && Redacted && Events.All(e => e.Validate().IsValid);
}

public sealed record BrowserDocumentWorkflowResult(
    BrowserDocumentWorkflowStatus Status,
    IReadOnlyList<BrowserDocumentWorkflowStep> Steps,
    BrowserDocumentWorkflowEvidence Evidence,
    BrowserDocumentWorkflowAuditSummary AuditSummary,
    bool DownloadVerified,
    bool UploadVerified,
    bool FinalStatusVerified,
    bool CleanupCompleted,
    bool Redacted,
    string Reason)
{
    public bool AllowsDone =>
        Status == BrowserDocumentWorkflowStatus.Completed &&
        Steps.All(s => s.Verified) &&
        Evidence.SemanticProofPresent &&
        Evidence.ProofRefs.Count > 0 &&
        AuditSummary.IsSafe &&
        DownloadVerified &&
        UploadVerified &&
        FinalStatusVerified &&
        CleanupCompleted &&
        Redacted;
}
