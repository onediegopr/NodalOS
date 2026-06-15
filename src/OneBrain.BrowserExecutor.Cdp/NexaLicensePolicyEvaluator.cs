using OneBrain.BrowserExecutor.Contracts;

namespace OneBrain.BrowserExecutor.Cdp;

public sealed class NexaLicensePolicyEvaluator
{
    public NexaLicensePolicyDecision Evaluate(NexaLicensePolicyRequest request)
    {
        var validation = request.License.Validate(request.NowUtc);
        if (!validation.IsValid)
            return Decision(request, NexaLicenseDecisionKind.Denied, string.Join("; ", validation.Errors));
        if (request.Account.Validate().IsValid == false)
            return Decision(request, NexaLicenseDecisionKind.FailClosed, "account validation failed");
        if (request.Account.Status is NexaAccountStatus.Suspended or NexaAccountStatus.Expired)
            return Decision(request, NexaLicenseDecisionKind.Denied, "account is suspended or expired");
        if (request.License.Status != NexaLicenseStatus.Active)
            return Decision(request, NexaLicenseDecisionKind.Denied, "license is not active");
        if (!request.License.IsActive(request.NowUtc))
            return Decision(request, NexaLicenseDecisionKind.Denied, "license expired");
        if (!FeatureEnabled(request.License, request.Feature))
            return Decision(request, NexaLicenseDecisionKind.Denied, "feature not included in license");
        if (request.Feature == NexaFeatureFlag.SensitiveRealPilot && (request.License.Plan.Kind != NexaPlanKind.Enterprise || !request.SensitiveCompliancePolicyApproved))
            return Decision(request, NexaLicenseDecisionKind.Denied, "sensitive feature requires enterprise compliance policy");
        if (request.Feature is NexaFeatureFlag.ProductiveVault or NexaFeatureFlag.RecorderProductive or NexaFeatureFlag.ReplayProductive)
            return Decision(request, NexaLicenseDecisionKind.Denied, "productive sensitive feature disabled by default");
        if (request.Worker is not null && (!request.Worker.Active || request.Worker.Validate().IsValid == false))
            return Decision(request, NexaLicenseDecisionKind.Denied, "worker is not authorized");
        if (request.UsageCounter is not null && LimitExceeded(request.License.Plan, request.UsageCounter))
            return Decision(request, NexaLicenseDecisionKind.Denied, "usage limit exceeded");

        return Decision(request, NexaLicenseDecisionKind.Allowed, "license policy allowed feature");
    }

    private static bool FeatureEnabled(NexaLicense license, NexaFeatureFlag feature)
    {
        var entitlement = license.Entitlements.FirstOrDefault(e => e.Feature == feature);
        if (entitlement is not null)
            return entitlement.Enabled;
        return license.Plan.Enables(feature);
    }

    private static bool LimitExceeded(NexaPlan plan, NexaUsageCounter counter)
    {
        var limit = plan.Limits.FirstOrDefault(l => l.LimitId.Equals(counter.LimitId, StringComparison.OrdinalIgnoreCase));
        return limit is not null && counter.Count >= limit.MaxCount;
    }

    private static NexaLicensePolicyDecision Decision(NexaLicensePolicyRequest request, NexaLicenseDecisionKind decision, string reason)
    {
        var audit = new NexaLicenseAuditEvent(
            $"license-audit-{Guid.NewGuid():N}",
            request.Account.AccountId,
            request.License.LicenseId,
            request.License.Plan.Kind,
            request.Feature,
            decision,
            BrowserCredentialRedactor.Redact(reason),
            request.NowUtc,
            Redacted: true);
        return new NexaLicensePolicyDecision(decision, request.Feature, BrowserCredentialRedactor.Redact(reason), audit);
    }
}
