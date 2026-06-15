using OneBrain.BrowserExecutor.Contracts;

namespace OneBrain.BrowserExecutor.Cdp;

public sealed class NexaConfigurationProfileEvaluator
{
    public NexaConfigurationProfileEvaluationResult Evaluate(NexaConfigurationProfileRequest request)
    {
        var violations = new List<string>();
        var profile = request.Profile;
        if (profile.Kind == NexaConfigurationProfileKind.Unknown)
            violations.Add("unknown configuration profile fails closed");
        if (request.ProfileRawRequested || profile.Policy.ExposesProfileRaw)
            violations.Add("profile raw is not exposed");
        if (request.RealBillingRequested || profile.Policy.AllowRealBilling)
            violations.Add("real billing is blocked before billing-real milestone");
        if (request.RealEmailRequested || profile.Policy.AllowRealEmail)
            violations.Add("real email is blocked before email-real milestone");
        if (request.PublicSaasActivationRequested || profile.Policy.AllowPublicSaasActivation)
            violations.Add("public SaaS activation is blocked");
        if (!profile.Policy.SupportMetadataOnly)
            violations.Add("support mode must remain metadata-only");
        if (!profile.Policy.DiagnosticsRedacted)
            violations.Add("diagnostics must remain redacted");
        if (profile.RecorderReplayMode == NexaProfileRecorderReplayMode.Productive || request.RequestedFeatures.Contains(NexaFeatureFlag.RecorderProductive) || request.RequestedFeatures.Contains(NexaFeatureFlag.ReplayProductive))
            violations.Add("productive recorder/replay are blocked");
        if (request.RequestedFeatures.Contains(NexaFeatureFlag.SensitiveRealPilot) && !request.ComplianceDecisionApproved)
            violations.Add("sensitive real pilot requires compliance decision");
        if (request.RequestedFeatures.Contains(NexaFeatureFlag.ProductiveVault) && !(request.ProductiveVaultEntitlement && request.AdminOverride && request.ComplianceDecisionApproved))
            violations.Add("productive vault requires entitlement, admin override, and compliance decision");
        if (profile.Kind == NexaConfigurationProfileKind.ProductionLocked && (profile.Features.EnabledFeatures.Contains(NexaFeatureFlag.SensitiveRealPilot) || profile.Features.EnabledFeatures.Contains(NexaFeatureFlag.ProductiveVault)))
            violations.Add("production locked profile cannot enable sensitive features by default");

        return new NexaConfigurationProfileEvaluationResult(
            new NexaConfigurationProfileDecision(violations.Count == 0, violations.Count == 0 ? "configuration profile allowed" : "configuration profile blocked", violations.Select(BrowserCredentialRedactor.Redact).ToArray(), Redacted: true),
            profile);
    }
}
