using OneBrain.BrowserExecutor.Contracts;

namespace OneBrain.BrowserExecutor.Cdp;

public sealed class BrowserExternalReadOnlyTargetEvaluator
{
    private static readonly string[] SensitiveHostTokens = ["afip", "bank", "banco", "fiscal", "tax", "erp", "gov", "gob", "payment", "pay"];

    public BrowserExternalReadOnlyResult Evaluate(BrowserExternalReadOnlyAttempt attempt)
    {
        var decision = Decide(attempt, out var reason);
        var redactedUri = RedactUri(attempt.Config.Target.BaseUri);
        var audit = BrowserPersistentAuditLedger.Create(
            BrowserAuditLedgerEventKind.PolicyBlocked,
            attempt.RunId,
            attempt.ActionId,
            attempt.CorrelationId,
            "profile-disposable",
            "session-external-readonly",
            null,
            null,
            null,
            decision.ToString(),
            reason,
            new Dictionary<string, string>
            {
                ["targetId"] = attempt.Config.Target.TargetId,
                ["host"] = attempt.Config.Target.Host,
                ["risk"] = attempt.Config.Target.RiskProfile.ToString(),
                ["ownership"] = attempt.Config.Target.Ownership.ToString(),
                ["auditKeyProvider"] = attempt.AuditKeyHealth.ProviderKind.ToString(),
                ["auditKeyId"] = attempt.AuditKeyHealth.KeyId
            });

        return new BrowserExternalReadOnlyResult(
            decision,
            BrowserCredentialRedactor.Redact(reason),
            redactedUri,
            NetworkMetadataOnly: attempt.Config.NetworkMetadataPolicy.Mode == BrowserNetworkCaptureMode.MetadataOnly &&
                                 !attempt.Config.NetworkMetadataPolicy.BodiesCaptureSupported,
            OpaqueQueryPersisted: false,
            CookiesPersisted: false,
            BodiesCaptured: false,
            SensitiveHeaderValuesCaptured: false,
            attempt.ExternalReadOnlyGuardActive,
            attempt.SemanticProofAvailable,
            attempt.BrowserCleanupConfirmed,
            new BrowserAuditIntegrityKeyReference(attempt.AuditKeyHealth.ProviderKind, attempt.AuditKeyHealth.KeyId, attempt.AuditKeyHealth.KeyVersion, "HMACSHA256", RawKeyExposed: false),
            audit,
            decision == BrowserExternalReadOnlyDecisionKind.Allowed ? ["external-readonly-semantic-proof", "external-readonly-network-metadata"] : [],
            Redacted: true);
    }

    private static BrowserExternalReadOnlyDecisionKind Decide(BrowserExternalReadOnlyAttempt attempt, out string reason)
    {
        var config = attempt.Config;
        if (!config.IsConfigured)
        {
            reason = "no test-owned external target configured";
            return BrowserExternalReadOnlyDecisionKind.BlockedNoTestOwnedExternalTarget;
        }
        if (!config.Allowlist.Allows(config.Target.BaseUri))
        {
            reason = "external host is not allowlisted";
            return BrowserExternalReadOnlyDecisionKind.Blocked;
        }
        if (SensitiveHostTokens.Any(token => config.Target.Host.Contains(token, StringComparison.OrdinalIgnoreCase)) ||
            config.Target.RiskProfile != BrowserExternalReadOnlyRiskProfile.LowRisk)
        {
            reason = "external target is sensitive or critical";
            return BrowserExternalReadOnlyDecisionKind.Blocked;
        }
        if (!config.Target.IsSafeTestOwned)
        {
            reason = "external target must be test-owned low-risk and non-authenticated";
            return BrowserExternalReadOnlyDecisionKind.Blocked;
        }
        if (config.NetworkMetadataPolicy.Mode != BrowserNetworkCaptureMode.MetadataOnly || config.NetworkMetadataPolicy.BodiesCaptureSupported)
        {
            reason = "external network capture must be metadata-only";
            return BrowserExternalReadOnlyDecisionKind.Blocked;
        }
        if (attempt.GateReport?.Status != BrowserRuntimePhaseCloseStatus.Passed)
        {
            reason = "browser runtime gate must pass";
            return BrowserExternalReadOnlyDecisionKind.RequiresGate;
        }
        if (!attempt.AuditKeyHealth.Healthy || attempt.AuditKeyHealth.ProviderKind == BrowserAuditIntegrityKeyProviderKind.DevFixtureExplicit)
        {
            reason = "audit key custody provider must be healthy and non-dev";
            return BrowserExternalReadOnlyDecisionKind.RequiresAuditKeyCustody;
        }
        if (!attempt.SemanticProofAvailable || !config.VerificationRule.HasSemanticProof)
        {
            reason = "external read-only semantic proof required";
            return BrowserExternalReadOnlyDecisionKind.RequiresSemanticProof;
        }
        if (!attempt.ExternalReadOnlyGuardActive)
        {
            reason = "external read-only guard must be active";
            return BrowserExternalReadOnlyDecisionKind.Blocked;
        }
        if (!attempt.BrowserCleanupConfirmed)
        {
            reason = "browser cleanup must be confirmed";
            return BrowserExternalReadOnlyDecisionKind.Blocked;
        }

        reason = "external read-only proof allowed";
        return BrowserExternalReadOnlyDecisionKind.Allowed;
    }

    public static Uri RedactUri(Uri uri)
    {
        var builder = new UriBuilder(uri) { Query = string.IsNullOrWhiteSpace(uri.Query) ? "" : "[REDACTED_QUERY]" };
        return builder.Uri;
    }
}
