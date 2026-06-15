namespace OneBrain.BrowserExecutor.Contracts;

public enum NexaPrivatePreviewFeedbackSeverity
{
    Low,
    Medium,
    High,
    Critical
}

public enum NexaPrivatePreviewFeedbackDecisionKind
{
    Accepted,
    Rejected,
    NeedsMoreInfo,
    SecurityBlocker,
    ProductBlocker,
    Deferred
}

public sealed record NexaPrivatePreviewFeedback(
    string FeedbackId,
    string SessionId,
    string ActorId,
    NexaRole ActorRole,
    string TenantId,
    string WorkspaceId,
    string FeatureArea,
    NexaPrivatePreviewFeedbackSeverity Severity,
    string ReproSummaryRedacted,
    IReadOnlyList<string> DiagnosticsRefs,
    IReadOnlyList<string> AuditRefs,
    bool Redacted);

public sealed record NexaPrivatePreviewIssueReport(
    string IssueId,
    NexaPrivatePreviewFeedback Feedback,
    bool Blocker,
    string RecommendedNextAction,
    bool ContainsSecret,
    bool ContainsCookie,
    bool ContainsBody,
    bool Redacted);

public sealed record NexaPrivatePreviewAuditReview(
    string ReviewId,
    string SessionId,
    IReadOnlyList<string> AuditRefs,
    bool AuditExportRedacted,
    bool LeakDetected,
    bool SecurityBlocker,
    string SummaryRedacted);

public sealed record NexaPrivatePreviewSessionSummary(
    string SessionId,
    string TenantId,
    string WorkspaceId,
    int FeedbackCount,
    int BlockerCount,
    string RecommendedNextAction,
    bool Redacted);

public sealed record NexaPrivatePreviewFeedbackLoop(
    string LoopId,
    string TenantId,
    string WorkspaceId,
    IReadOnlyList<NexaPrivatePreviewFeedback> FeedbackItems,
    IReadOnlyList<NexaPrivatePreviewIssueReport> Issues,
    NexaPrivatePreviewAuditReview AuditReview,
    NexaPrivatePreviewSessionSummary Summary,
    bool Redacted);

public sealed record NexaPrivatePreviewFeedbackDecision(
    NexaPrivatePreviewFeedbackDecisionKind Decision,
    IReadOnlyList<string> ReasonCodes,
    NexaPrivatePreviewIssueReport? IssueReport,
    bool Redacted)
{
    public bool Allowed => Decision is NexaPrivatePreviewFeedbackDecisionKind.Accepted or NexaPrivatePreviewFeedbackDecisionKind.SecurityBlocker or NexaPrivatePreviewFeedbackDecisionKind.ProductBlocker;
}
