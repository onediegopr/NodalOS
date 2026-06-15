namespace OneBrain.BrowserExecutor.Contracts;

public enum NexaExternalLowRiskDocumentWorkflowDecisionKind
{
    Prepared,
    PreparedButBlockedByM65,
    Blocked,
    LiveAllowed
}

public sealed record NexaExternalLowRiskDocumentWorkflowPolicy(
    bool RequireTargetReadiness,
    bool RequireSafeDownload,
    bool RequireSafeUpload,
    bool RequireApproval,
    bool AllowSensitiveDocuments,
    bool RequireAudit);

public sealed record NexaExternalLowRiskDocumentWorkflowRequest(
    string RequestId,
    NexaExternalLowRiskTargetReadiness TargetReadiness,
    bool SafeDownloadConfigured,
    bool SafeUploadConfigured,
    bool ApprovalPresent,
    bool UsesSensitiveDocuments,
    bool AuditRedacted);

public sealed record NexaExternalLowRiskDocumentWorkflowReadiness(
    bool TargetReady,
    bool SafeDownloadReady,
    bool SafeUploadReady,
    bool ApprovalReady,
    bool SyntheticDocumentsOnly,
    bool AuditRedacted);

public sealed record NexaExternalLowRiskDocumentWorkflowDecision(
    NexaExternalLowRiskDocumentWorkflowDecisionKind Decision,
    NexaExternalLowRiskDocumentWorkflowReadiness Readiness,
    IReadOnlyList<string> ReasonCodes,
    bool Redacted);
