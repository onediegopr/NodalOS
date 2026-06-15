namespace OneBrain.BrowserExecutor.Contracts;

public enum BrowserExternalReadOnlyOwnership
{
    Unknown,
    TestOwned,
    Controlled
}

public enum BrowserExternalReadOnlyRiskProfile
{
    LowRisk,
    Fiscal,
    Banking,
    Financial,
    Government,
    Erp,
    SensitivePersonalData,
    Unknown
}

public enum BrowserExternalReadOnlyDecisionKind
{
    Allowed,
    Blocked,
    BlockedNoTestOwnedExternalTarget,
    RequiresGate,
    RequiresAuditKeyCustody,
    RequiresSemanticProof
}

public sealed record BrowserExternalReadOnlyVerificationRule(
    string ExpectedTitleContains,
    string ExpectedTextContains,
    string SemanticProofRef)
{
    public bool HasSemanticProof =>
        !string.IsNullOrWhiteSpace(ExpectedTitleContains) ||
        !string.IsNullOrWhiteSpace(ExpectedTextContains) ||
        !string.IsNullOrWhiteSpace(SemanticProofRef);
}

public sealed record BrowserExternalReadOnlyTarget(
    string TargetId,
    Uri BaseUri,
    BrowserExternalReadOnlyOwnership Ownership,
    BrowserExternalReadOnlyRiskProfile RiskProfile,
    bool AuthRequired,
    bool ContainsRealData,
    bool RequiresTwoFactorOrCaptcha,
    bool HasIrreversibleActions)
{
    public string Host => BaseUri.Host;

    public bool IsSafeTestOwned =>
        Ownership is BrowserExternalReadOnlyOwnership.TestOwned or BrowserExternalReadOnlyOwnership.Controlled &&
        RiskProfile == BrowserExternalReadOnlyRiskProfile.LowRisk &&
        !AuthRequired &&
        !ContainsRealData &&
        !RequiresTwoFactorOrCaptcha &&
        !HasIrreversibleActions;
}

public sealed record BrowserExternalReadOnlyTargetAllowlist(IReadOnlySet<string> Hosts)
{
    public bool Allows(Uri uri) => Hosts.Contains(uri.Host, StringComparer.OrdinalIgnoreCase);
}

public sealed record BrowserExternalReadOnlyTargetConfig(
    BrowserExternalReadOnlyTarget Target,
    BrowserExternalReadOnlyTargetAllowlist Allowlist,
    IReadOnlySet<string> AllowedPaths,
    IReadOnlySet<string> DisallowedActions,
    BrowserExternalReadOnlyVerificationRule VerificationRule,
    BrowserNetworkCapturePolicy NetworkMetadataPolicy)
{
    public bool IsConfigured =>
        !string.IsNullOrWhiteSpace(Target.TargetId) &&
        Allowlist.Hosts.Count > 0 &&
        AllowedPaths.Count > 0 &&
        DisallowedActions.Count > 0;
}

public sealed record BrowserExternalReadOnlyAttempt(
    string RunId,
    string ActionId,
    string CorrelationId,
    BrowserExternalReadOnlyTargetConfig Config,
    BrowserRuntimePhaseCloseReport? GateReport,
    BrowserAuditIntegrityKeyHealthCheck AuditKeyHealth,
    bool SemanticProofAvailable,
    bool BrowserCleanupConfirmed,
    bool ExternalReadOnlyGuardActive);

public sealed record BrowserExternalReadOnlyResult(
    BrowserExternalReadOnlyDecisionKind Decision,
    string Reason,
    Uri RedactedUri,
    bool NetworkMetadataOnly,
    bool OpaqueQueryPersisted,
    bool CookiesPersisted,
    bool BodiesCaptured,
    bool SensitiveHeaderValuesCaptured,
    bool ExternalReadOnlyGuardActive,
    bool SemanticProofAvailable,
    bool BrowserCleanupConfirmed,
    BrowserAuditIntegrityKeyReference AuditKeyReference,
    BrowserAuditLedgerEvent AuditEvent,
    IReadOnlyList<string> EvidenceRefs,
    bool Redacted)
{
    public bool AllowsDone =>
        Decision == BrowserExternalReadOnlyDecisionKind.Allowed &&
        NetworkMetadataOnly &&
        !OpaqueQueryPersisted &&
        !CookiesPersisted &&
        !BodiesCaptured &&
        !SensitiveHeaderValuesCaptured &&
        ExternalReadOnlyGuardActive &&
        SemanticProofAvailable &&
        BrowserCleanupConfirmed &&
        Redacted;
}

public sealed class BrowserExternalReadOnlyGuard
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
        "login",
        "credential-entry",
        "mutation"
    };

    public bool Allows(string operation) =>
        string.Equals(operation, "read-dom", StringComparison.OrdinalIgnoreCase) ||
        string.Equals(operation, "verify-title", StringComparison.OrdinalIgnoreCase) ||
        string.Equals(operation, "verify-text", StringComparison.OrdinalIgnoreCase) ||
        string.Equals(operation, "navigate-readonly", StringComparison.OrdinalIgnoreCase) ||
        string.Equals(operation, "observe-network-metadata", StringComparison.OrdinalIgnoreCase);

    public bool Blocks(string operation) => BlockedActions.Contains(operation);
}
