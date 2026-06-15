using OneBrain.BrowserExecutor.Contracts;

namespace OneBrain.BrowserExecutor.Cdp;

public sealed class NexaExternalLowRiskTargetSetupEvaluator
{
    private static readonly string[] SensitiveHostTokens = ["afip", "bank", "banco", "fiscal", "tax", "erp", "gov", "gob", "payment", "pay"];

    public NexaExternalLowRiskTargetSetup Evaluate(NexaExternalLowRiskTargetConfig? config, NexaExternalLowRiskTargetOwnershipProof? proof)
    {
        var reasons = new List<string>();
        if (config is null || string.IsNullOrWhiteSpace(config.BaseUrl))
        {
            var readiness = new NexaExternalLowRiskTargetReadiness(false, false, false, true, false, false, true, Redacted: true);
            return new NexaExternalLowRiskTargetSetup(config, proof, new NexaExternalLowRiskTargetDecision(NexaExternalLowRiskTargetDecisionKind.BlockedNoTestOwnedExternalTarget, config, readiness, ["external low-risk target base URL missing"], Redacted: true));
        }

        if (!config.HostAllowlisted)
            reasons.Add("host not allowlisted");
        if (SensitiveHostTokens.Any(token => config.Host.Contains(token, StringComparison.OrdinalIgnoreCase)) || config.SensitiveCategory)
            reasons.Add("sensitive host/category blocked");
        if (!config.TestOwned || proof?.TestOwned != true)
            reasons.Add("test-owned proof required");
        if (config.ContainsRealCustomerData)
            reasons.Add("real customer data blocked");
        if (config.HasPayment || config.HasIrreversibleActions)
            reasons.Add("payment or irreversible actions blocked");
        if (config.RequiresTwoFactorOrCaptcha)
            reasons.Add("2FA/CAPTCHA target blocked");
        if (!config.SemanticProofAvailable)
            reasons.Add("semantic proof required");
        if (config.ReadOnlyPaths.Count == 0)
            reasons.Add("read-only paths required");

        var readinessOk = reasons.Count == 0;
        var readinessResult = new NexaExternalLowRiskTargetReadiness(
            Configured: true,
            Reachable: readinessOk,
            SemanticProofVerified: readinessOk,
            MetadataOnly: true,
            CookiesPersisted: false,
            SensitiveHeaderValuesCaptured: false,
            BrowserCleanupConfirmed: true,
            Redacted: true);
        return new NexaExternalLowRiskTargetSetup(
            config,
            proof,
            new NexaExternalLowRiskTargetDecision(readinessOk ? NexaExternalLowRiskTargetDecisionKind.Allowed : NexaExternalLowRiskTargetDecisionKind.Blocked, config, readinessResult, reasons, Redacted: true));
    }

    public static NexaExternalLowRiskTargetConfig? FromEnvironment()
    {
        var url = Environment.GetEnvironmentVariable("ONEBRAIN_EXTERNAL_LOW_RISK_TARGET_BASE_URL");
        if (string.IsNullOrWhiteSpace(url) || !Uri.TryCreate(url, UriKind.Absolute, out var uri))
            return null;
        return new NexaExternalLowRiskTargetConfig(
            "external-low-risk-env-target",
            url,
            uri.Host,
            HostAllowlisted: true,
            TestOwned: true,
            SensitiveCategory: false,
            ContainsRealCustomerData: false,
            HasPayment: false,
            HasIrreversibleActions: false,
            RequiresTwoFactorOrCaptcha: false,
            SemanticProofAvailable: true,
            ReadOnlyPaths: ["/", "/status", "/readonly"]);
    }
}

public sealed class NexaExternalLowRiskDocumentWorkflowEvaluator
{
    public NexaExternalLowRiskDocumentWorkflowDecision Evaluate(NexaExternalLowRiskDocumentWorkflowPolicy policy, NexaExternalLowRiskDocumentWorkflowRequest request)
    {
        var reasons = new List<string>();
        if (policy.RequireTargetReadiness && !request.TargetReadiness.SemanticProofVerified)
            reasons.Add("external low-risk target readiness required");
        if (policy.RequireSafeDownload && !request.SafeDownloadConfigured)
            reasons.Add("safe download required");
        if (policy.RequireSafeUpload && !request.SafeUploadConfigured)
            reasons.Add("safe upload required");
        if (policy.RequireApproval && !request.ApprovalPresent)
            reasons.Add("approval required");
        if (!policy.AllowSensitiveDocuments && request.UsesSensitiveDocuments)
            reasons.Add("sensitive external documents blocked");
        if (policy.RequireAudit && !request.AuditRedacted)
            reasons.Add("redacted audit required");

        var readiness = new NexaExternalLowRiskDocumentWorkflowReadiness(
            request.TargetReadiness.SemanticProofVerified,
            request.SafeDownloadConfigured,
            request.SafeUploadConfigured,
            request.ApprovalPresent,
            !request.UsesSensitiveDocuments,
            request.AuditRedacted);
        var decision = !request.TargetReadiness.SemanticProofVerified
            ? NexaExternalLowRiskDocumentWorkflowDecisionKind.PreparedButBlockedByM65
            : reasons.Count == 0 ? NexaExternalLowRiskDocumentWorkflowDecisionKind.Prepared : NexaExternalLowRiskDocumentWorkflowDecisionKind.Blocked;
        return new NexaExternalLowRiskDocumentWorkflowDecision(decision, readiness, reasons, Redacted: true);
    }
}
