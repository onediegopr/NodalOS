namespace OneBrain.AgentOperations.Contracts;

public enum NexaFailureKind
{
    SelectorNotFound,
    SelectorAmbiguous,
    NavigationTimeout,
    PageLoadFailed,
    LoginRequired,
    CaptchaDetected,
    TwoFactorRequired,
    PolicyBlocked,
    ApprovalRequired,
    NoProgressDetected,
    RepeatedActionDetected,
    RuntimeDisconnected,
    ContentScriptUnreachable,
    TabCrashed,
    NetworkUnavailable,
    DownloadBlocked,
    UploadBlocked,
    SensitiveDataRisk,
    HumanInputRequired,
    ExternalServiceUnavailable,
    Unknown
}

public enum NexaFailureSeverity
{
    Info,
    Warning,
    Recoverable,
    Blocking,
    Critical
}

public sealed record NexaTroubleshootingRecommendation
{
    public required NexaFailureKind FailureKind { get; init; }
    public required string HumanReadableCause { get; init; }
    public required string SuggestedAction { get; init; }
    public required bool RequiresHumanInput { get; init; }
    public required bool CanRetryAutomatically { get; init; }
    public required bool CanReplan { get; init; }
    public required bool MustStop { get; init; }
}
