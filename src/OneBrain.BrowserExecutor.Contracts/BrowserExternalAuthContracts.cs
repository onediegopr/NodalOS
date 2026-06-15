namespace OneBrain.BrowserExecutor.Contracts;

public enum BrowserExternalAuthRiskProfile
{
    LowRisk,
    Financial,
    Fiscal,
    Erp,
    SensitivePersonalData,
    Unknown
}

public enum BrowserExternalAuthDecisionKind
{
    Allowed,
    Blocked,
    RequiresConsent,
    RequiresVault,
    RequiresGate,
    BlockedNoSafeExternalTarget
}

public sealed record BrowserExternalAuthTarget(
    string TargetId,
    Uri LoginUri,
    Uri AuthenticatedUri,
    string DisplayName,
    BrowserExternalAuthRiskProfile RiskProfile,
    bool NonFinancial,
    bool NonFiscal,
    bool NonErp,
    bool NoIrreversibleActions,
    bool NoSensitivePersonalData,
    bool ReadOnlyAfterLogin,
    bool RequiresTwoFactorOrCaptcha)
{
    public string Host => LoginUri.Host;

    public bool IsLowRisk =>
        RiskProfile == BrowserExternalAuthRiskProfile.LowRisk &&
        NonFinancial &&
        NonFiscal &&
        NonErp &&
        NoIrreversibleActions &&
        NoSensitivePersonalData &&
        ReadOnlyAfterLogin &&
        !RequiresTwoFactorOrCaptcha;
}

public sealed record BrowserExternalAuthPolicy(
    IReadOnlySet<string> AllowlistedHosts,
    bool RequireConsent,
    bool RequireVaultTestCredential,
    bool RequireControlledProfile,
    bool RequireGate,
    bool EnforceReadOnlyAfterLogin);

public sealed record BrowserExternalAuthAttempt(
    string RunId,
    string ActionId,
    string CorrelationId,
    BrowserExternalAuthTarget Target,
    BrowserConsentGrant? Consent,
    BrowserRuntimePhaseCloseReport? GateReport,
    bool HasVaultTestCredential,
    bool HasControlledProfile);

public sealed record BrowserExternalAuthResult(
    BrowserExternalAuthDecisionKind Decision,
    BrowserExternalAuthTarget Target,
    string Reason,
    BrowserVerification? Verification,
    bool CredentialValuesExposed,
    bool CookiesExposed,
    bool ReadOnlyGuardActive,
    BrowserAuditLedgerEvent AuditEvent,
    IReadOnlyList<string> EvidenceRefs,
    bool Redacted)
{
    public bool AllowsStepDone =>
        Decision == BrowserExternalAuthDecisionKind.Allowed &&
        Verification?.AllowsStepDone() == true &&
        !CredentialValuesExposed &&
        !CookiesExposed &&
        ReadOnlyGuardActive &&
        Redacted;
}

public sealed class BrowserExternalAuthReadOnlyGuard
{
    private static readonly HashSet<string> BlockedActions = new(StringComparer.OrdinalIgnoreCase)
    {
        "submit",
        "save",
        "delete",
        "publish",
        "pay",
        "upload",
        "confirm",
        "mutate"
    };

    public bool Allows(string operation) =>
        string.Equals(operation, "read", StringComparison.OrdinalIgnoreCase) ||
        string.Equals(operation, "navigate-readonly", StringComparison.OrdinalIgnoreCase) ||
        string.Equals(operation, "download-allowed-document", StringComparison.OrdinalIgnoreCase);

    public bool Blocks(string operation) => BlockedActions.Contains(operation);
}

public sealed class BrowserExternalAuthPolicyEvaluator
{
    public BrowserExternalAuthResult Evaluate(BrowserExternalAuthAttempt attempt, BrowserExternalAuthPolicy policy, DateTimeOffset now)
    {
        var decision = Decide(attempt, policy, now, out var reason);
        var audit = new BrowserAuditLedgerEvent(
            EventId: $"audit-ledger-{Guid.NewGuid():N}",
            Kind: BrowserAuditLedgerEventKind.PolicyBlocked,
            CreatedAtUtc: DateTimeOffset.UtcNow,
            RunId: attempt.RunId,
            ActionId: attempt.ActionId,
            CorrelationId: attempt.CorrelationId,
            ProfileId: "profile-controlled",
            SessionId: "session-controlled",
            ConsentId: attempt.Consent?.Request.ConsentId,
            SecretId: null,
            ProviderKind: null,
            Decision: decision.ToString(),
            Reason: BrowserCredentialRedactor.Redact(reason),
            Metadata: new Dictionary<string, string>
            {
                ["host"] = attempt.Target.Host,
                ["risk"] = attempt.Target.RiskProfile.ToString(),
                ["readOnlyGuard"] = policy.EnforceReadOnlyAfterLogin.ToString()
            },
            Redacted: true,
            Integrity: new BrowserAuditLedgerIntegrityProof(0, "0", "0")).WithIntegrity(1, "0");
        return new BrowserExternalAuthResult(decision, attempt.Target, BrowserCredentialRedactor.Redact(reason), null, false, false, policy.EnforceReadOnlyAfterLogin, audit, decision == BrowserExternalAuthDecisionKind.Allowed ? ["external-auth-policy"] : [], true);
    }

    private static BrowserExternalAuthDecisionKind Decide(BrowserExternalAuthAttempt attempt, BrowserExternalAuthPolicy policy, DateTimeOffset now, out string reason)
    {
        if (policy.AllowlistedHosts.Count == 0)
        {
            reason = "no safe external target configured";
            return BrowserExternalAuthDecisionKind.BlockedNoSafeExternalTarget;
        }
        if (!policy.AllowlistedHosts.Contains(attempt.Target.Host, StringComparer.OrdinalIgnoreCase))
        {
            reason = "external auth host is not allowlisted";
            return BrowserExternalAuthDecisionKind.Blocked;
        }
        if (!attempt.Target.IsLowRisk)
        {
            reason = "external auth target is not low risk";
            return BrowserExternalAuthDecisionKind.Blocked;
        }
        if (policy.RequireConsent && (attempt.Consent is null || !attempt.Consent.AllowsCapability(BrowserConsentCapability.SecretRetrieval, BrowserConsentScope.Profile, now)))
        {
            reason = "external auth requires scoped consent";
            return BrowserExternalAuthDecisionKind.RequiresConsent;
        }
        if (policy.RequireGate && attempt.GateReport?.Passed != true)
        {
            reason = "external auth requires passing phase gate";
            return BrowserExternalAuthDecisionKind.RequiresGate;
        }
        if (policy.RequireVaultTestCredential && !attempt.HasVaultTestCredential)
        {
            reason = "external auth requires test vault reference";
            return BrowserExternalAuthDecisionKind.RequiresVault;
        }
        if (policy.RequireControlledProfile && !attempt.HasControlledProfile)
        {
            reason = "external auth requires controlled profile";
            return BrowserExternalAuthDecisionKind.Blocked;
        }
        if (!policy.EnforceReadOnlyAfterLogin)
        {
            reason = "external auth requires post-login read-only guard";
            return BrowserExternalAuthDecisionKind.Blocked;
        }
        reason = "external auth target accepted by policy";
        return BrowserExternalAuthDecisionKind.Allowed;
    }
}
