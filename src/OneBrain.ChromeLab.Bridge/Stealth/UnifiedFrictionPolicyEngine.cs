namespace OneBrain.ChromeLab.Bridge.Stealth;

public sealed class UnifiedFrictionPolicyEngine : IUnifiedFrictionPolicyEngine
{
    private readonly ChromeLabOptions _options;

    public UnifiedFrictionPolicyEngine(ChromeLabOptions options)
    {
        _options = options;
    }

    public Task<UnifiedFrictionPolicyDecision> EvaluateAsync(
        FrictionSignal signal,
        string mode,
        int currentRetryCount,
        CancellationToken ct)
    {
        var maxRetries = _options.GetStealthMaxRetries();
        var baseKind = EvaluateSignal(signal);

        var kind = string.Equals(mode, "stealth", StringComparison.OrdinalIgnoreCase)
            ? ApplyStealthOverrides(baseKind, signal, currentRetryCount, maxRetries)
            : baseKind;

        var solverProvider = kind == UnifiedFrictionDecisionKind.SolveAndRetry
            ? signal.SolverRecommendation : null;
        var cooldownMs = kind == UnifiedFrictionDecisionKind.CooldownAndRetry
            ? (int?)_options.GetStealthCooldownMs()
            : null;

        var decide = new UnifiedFrictionPolicyDecision(
            Decision: kind,
            Risk: SeverityToRisk(signal.Severity),
            HandoffReason: MapHandoffReason(signal.Kind),
            Message: kind switch
            {
                UnifiedFrictionDecisionKind.SolveAndRetry =>
                    $"CAPTCHA detected. Attempting auto-solve via {solverProvider ?? "external service"}. Attempt {currentRetryCount + 1}/{maxRetries}.",
                UnifiedFrictionDecisionKind.RotateAndRetry =>
                    "Bot block or access denial detected. Rotating proxy and retrying.",
                UnifiedFrictionDecisionKind.CooldownAndRetry =>
                    $"Rate limit detected. Cooling down for {cooldownMs}ms before retry.",
                UnifiedFrictionDecisionKind.RequiresHuman =>
                    $"Human intervention required: {signal.Reason}",
                _ => signal.Reason
            },
            TriggerSignal: signal,
            Boundary: null!,
            SolverProvider: solverProvider,
            RetryAttempt: currentRetryCount,
            MaxRetries: maxRetries,
            CooldownMs: cooldownMs,
            RotateProxy: kind == UnifiedFrictionDecisionKind.RotateAndRetry,
            RotateProfile: kind == UnifiedFrictionDecisionKind.RotateAndRetry,
            DecidedAtUtc: DateTimeOffset.UtcNow,
            EvidenceRefs: [$"policy:{kind}:{signal.SignalId}"],
            ProofRefs: [$"proof:policy:{kind}:{signal.SignalId}"]);

        return Task.FromResult(decide);
    }

    private static UnifiedFrictionDecisionKind EvaluateSignal(FrictionSignal signal)
    {
        return signal.Kind switch
        {
            FrictionSignalKind.CaptchaDetected
                when signal.Severity >= FrictionSignalSeverity.Critical
                => UnifiedFrictionDecisionKind.RequiresHuman,

            FrictionSignalKind.TwoFactorDetected
            or FrictionSignalKind.PasswordFieldDetected
                => UnifiedFrictionDecisionKind.RequiresHuman,

            FrictionSignalKind.LoginFormDetected
                => UnifiedFrictionDecisionKind.AllowReadOnly,

            FrictionSignalKind.BotBlockDetected
            or FrictionSignalKind.AccessDeniedDetected
                => UnifiedFrictionDecisionKind.RotateAndRetry,

            FrictionSignalKind.RateLimitDetected
                => UnifiedFrictionDecisionKind.CooldownAndRetry,

            FrictionSignalKind.ServiceUnavailable
                => UnifiedFrictionDecisionKind.CooldownAndRetry,

            FrictionSignalKind.SuspiciousRedirect
                => UnifiedFrictionDecisionKind.RotateAndRetry,

            FrictionSignalKind.UnknownFriction
                when signal.Severity == FrictionSignalSeverity.Fatal
                => UnifiedFrictionDecisionKind.FailClosed,

            _ => UnifiedFrictionDecisionKind.AllowReadOnly
        };
    }

    private static UnifiedFrictionDecisionKind ApplyStealthOverrides(
        UnifiedFrictionDecisionKind baseKind,
        FrictionSignal signal,
        int currentRetryCount,
        int maxRetries)
    {
        var exhausted = currentRetryCount >= maxRetries;

        if (baseKind == UnifiedFrictionDecisionKind.RequiresHuman
            && signal.Kind == FrictionSignalKind.CaptchaDetected
            && signal.AutoSolvable
            && !exhausted)
        {
            return UnifiedFrictionDecisionKind.SolveAndRetry;
        }

        if (baseKind == UnifiedFrictionDecisionKind.RotateAndRetry && exhausted)
            return UnifiedFrictionDecisionKind.RequiresHuman;

        if (baseKind == UnifiedFrictionDecisionKind.CooldownAndRetry && exhausted)
            return UnifiedFrictionDecisionKind.RequiresHuman;

        return baseKind;
    }

    private static string SeverityToRisk(FrictionSignalSeverity severity) => severity switch
    {
        FrictionSignalSeverity.Fatal => "Critical",
        FrictionSignalSeverity.Critical => "High",
        FrictionSignalSeverity.Warning => "Medium",
        _ => "None"
    };

    private static string MapHandoffReason(FrictionSignalKind kind) => kind switch
    {
        FrictionSignalKind.CaptchaDetected => "CaptchaRequired",
        FrictionSignalKind.TwoFactorDetected => "TwoFactorRequired",
        FrictionSignalKind.PasswordFieldDetected => "PasswordRequired",
        FrictionSignalKind.LoginFormDetected => "LoginRequired",
        FrictionSignalKind.BotBlockDetected => "AutomationBlocked",
        FrictionSignalKind.AccessDeniedDetected => "AutomationBlocked",
        FrictionSignalKind.RateLimitDetected => "AutomationBlocked",
        _ => "UserConfirmationRequired"
    };
}
