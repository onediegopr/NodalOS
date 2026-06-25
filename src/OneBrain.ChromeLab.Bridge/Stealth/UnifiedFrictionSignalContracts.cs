using System.Text.Json;

namespace OneBrain.ChromeLab.Bridge.Stealth;

public enum FrictionSignalKind
{
    CaptchaDetected,
    TwoFactorDetected,
    PasswordFieldDetected,
    LoginFormDetected,
    BotBlockDetected,
    RateLimitDetected,
    AccessDeniedDetected,
    ServiceUnavailable,
    SuspiciousRedirect,
    UnknownFriction
}

public enum FrictionSignalSeverity
{
    Info,
    Warning,
    Critical,
    Fatal
}

public sealed record FrictionSignal(
    string SignalId,
    string TaskId,
    FrictionSignalKind Kind,
    FrictionSignalSeverity Severity,
    string Source,
    string FrameId,
    string? ElementId,
    string? Sitekey,
    string? BlockHttpCode,
    string? BlockPattern,
    string RedactedEvidence,
    string Reason,
    DateTimeOffset DetectedAtUtc,
    bool AutoSolvable,
    string? SolverRecommendation,
    IReadOnlyList<string> EvidenceRefs,
    IReadOnlyList<string> ProofRefs);
