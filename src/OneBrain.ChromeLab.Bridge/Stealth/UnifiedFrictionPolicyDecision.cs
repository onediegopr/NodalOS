namespace OneBrain.ChromeLab.Bridge.Stealth;

public enum UnifiedFrictionDecisionKind
{
    AllowReadOnly,
    RequiresHuman,
    Block,
    FailClosed,
    SolveAndRetry,
    RotateAndRetry,
    SolveThenRotate,
    CooldownAndRetry
}

public sealed record UnifiedFrictionPolicyDecision(
    UnifiedFrictionDecisionKind Decision,
    string Risk,
    string? HandoffReason,
    string Message,
    FrictionSignal TriggerSignal,
    object? Boundary,
    string? SolverProvider,
    int? RetryAttempt,
    int? MaxRetries,
    int? CooldownMs,
    bool RotateProxy,
    bool RotateProfile,
    DateTimeOffset DecidedAtUtc,
    IReadOnlyList<string> EvidenceRefs,
    IReadOnlyList<string> ProofRefs)
{
    public bool BlocksAutomation =>
        Decision is UnifiedFrictionDecisionKind.Block or UnifiedFrictionDecisionKind.FailClosed;

    public bool RequiresHuman =>
        Decision == UnifiedFrictionDecisionKind.RequiresHuman;

    public bool RequiresStealthAction =>
        Decision is UnifiedFrictionDecisionKind.SolveAndRetry
            or UnifiedFrictionDecisionKind.RotateAndRetry
            or UnifiedFrictionDecisionKind.SolveThenRotate
            or UnifiedFrictionDecisionKind.CooldownAndRetry;
}
