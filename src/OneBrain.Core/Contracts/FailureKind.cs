namespace OneBrain.Core.Contracts;

public enum FailureKind
{
    SourceUnavailable,
    NotFound,
    Stale,
    Ambiguous,
    Blocked,
    Timeout,
    Unverified,
    PolicyDenied,
    HumanInterrupted,
    Cancelled
}
