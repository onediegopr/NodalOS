namespace OneBrain.Core.Approval;

public static class ApprovalRiskLevels
{
    public const string Low = "low";
    public const string Medium = "medium";
    public const string High = "high";
    public const string Critical = "critical";
}

public static class ApprovalActionKinds
{
    public const string Send = "send";
    public const string Submit = "submit";
    public const string Delete = "delete";
    public const string Publish = "publish";
    public const string Pay = "pay";
    public const string Purchase = "purchase";
    public const string Login = "login";
    public const string AcceptTerms = "accept_terms";
    public const string AcceptCookies = "accept_cookies";
    public const string ModifyFinancialData = "modify_financial_data";
    public const string ModifyLegalData = "modify_legal_data";
    public const string RunScript = "run_script";
    public const string InstallSoftware = "install_software";
    public const string PrepareMessage = "prepare_message";
    public const string ViewReport = "view_report";
}

public static class ApprovalDecisionKinds
{
    public const string Approved = "approved";
    public const string Rejected = "rejected";
}

public static class HumanInTheLoopModes
{
    public const string Conservative = "conservative";
    public const string ConfidenceBased = "confidence_based";
    public const string ManualOnlyForSensitive = "manual_only_for_sensitive";
    public const string AlwaysRequired = "always_required";
}

public sealed record PlatformApprovalPolicy(
    string HumanInTheLoopMode,
    int MinConfidenceForLowRiskAutoProceed,
    IReadOnlyList<string> SensitiveActionKinds,
    IReadOnlyList<string> CriticalEnvironments,
    bool FailClosedWhenMissingInformation,
    bool FailClosedWithoutSafeExecutor);

public sealed record ApprovalRequest(
    string ApprovalRequestId,
    string CreatedAtUtc,
    string Source,
    string? CandidateFlowId,
    string ActionKind,
    string RiskLevel,
    string Title,
    string Description,
    string Preview,
    bool RequiresApproval,
    bool FailClosed,
    string Status,
    IReadOnlyList<string> MissingInformation,
    IReadOnlyList<string> Notes);

public sealed record ApprovalDecision(
    string ApprovalDecisionId,
    string ApprovalRequestId,
    string DecidedAtUtc,
    string Decision,
    string Reason,
    string DecidedBy,
    bool ExecutionAllowed,
    IReadOnlyList<string> Notes);

public sealed record ApprovalArtifactWriteResult
{
    public bool Success { get; init; }
    public string Path { get; init; } = "";
    public string RelativePath { get; init; } = "";
    public string Error { get; init; } = "";
}
