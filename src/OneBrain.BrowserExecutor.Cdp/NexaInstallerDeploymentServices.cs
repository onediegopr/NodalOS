using OneBrain.BrowserExecutor.Contracts;

namespace OneBrain.BrowserExecutor.Cdp;

public sealed class NexaInstallerDryRunEvaluator
{
    public NexaInstallerDryRunResult Evaluate(NexaInstallerDryRunRequest request)
    {
        var violations = new List<string>();
        violations.AddRange(request.Plan.Validate().Errors);

        var preflight = new List<NexaInstallerPreflightCheck>
        {
            Check("preflight-os-supported", request.OsSupported, "OS support declared"),
            Check("preflight-dotnet-runtime", request.DotNetRuntimeCompatible, ".NET runtime compatible"),
            Check("preflight-browser-availability", request.BrowserAvailabilityDeclared, "browser availability declared"),
            Check("preflight-cdp-capability", request.CdpCapabilityDeclared, "CDP capability declared"),
            Check("preflight-vault-provider", request.VaultProviderDeclared, "vault provider declared"),
            Check("preflight-diagnostics-redaction", request.DiagnosticsRedactionActive, "diagnostics redaction active"),
            Check("preflight-tenant-governance", request.TenantGovernanceAvailable, "tenant governance available"),
            Check("preflight-admin-runtime", request.AdminRuntimeAvailable, "admin runtime available"),
            Check("preflight-license-evaluator", request.LicenseEvaluatorAvailable, "license evaluator available"),
            Check("preflight-release-integrity", request.Plan.ReleaseManifest.UpdateManifest.Validate().IsValid, "release manifest hash and signature metadata valid")
        };

        foreach (var check in preflight)
        {
            violations.AddRange(check.Validate().Errors);
            if (!check.Passed)
                violations.Add(check.Reason);
        }

        var profileResult = new NexaConfigurationProfileEvaluator().Evaluate(new NexaConfigurationProfileRequest(
            request.Plan.Profile,
            request.Plan.Profile.Features.EnabledFeatures,
            ProductiveVaultEntitlement: false,
            ComplianceDecisionApproved: false,
            AdminOverride: false,
            RealBillingRequested: request.Plan.Profile.Policy.AllowRealBilling,
            RealEmailRequested: request.Plan.Profile.Policy.AllowRealEmail,
            PublicSaasActivationRequested: request.Plan.Profile.Policy.AllowPublicSaasActivation,
            ProfileRawRequested: request.Plan.Profile.Policy.ExposesProfileRaw));
        violations.AddRange(profileResult.Decision.Violations);

        var modified = request.Plan.Steps.Any(step => step.WouldModifyRealSystem) ||
                       request.Plan.RegistersRealService ||
                       request.Plan.CreatesRealScheduledTask ||
                       request.Plan.TouchesRegistry ||
                       request.Plan.OpensPublicPort ||
                       request.Plan.AutoUpdateRealEnabled ||
                       request.Plan.RollbackPlan.ExecutesRollback;

        return new NexaInstallerDryRunResult(
            Allowed: violations.Count == 0 && !modified,
            ModifiedRealSystem: modified,
            PreflightChecks: preflight,
            Violations: violations,
            FileLayout: request.Plan.FileLayout,
            RollbackPlan: request.Plan.RollbackPlan,
            Decision: violations.Count == 0 && !modified ? "dry-run allowed" : "dry-run blocked",
            Redacted: true);
    }

    private static NexaInstallerPreflightCheck Check(string id, bool passed, string reason) =>
        new(id, passed ? NexaInstallerPreflightStatus.Passed : NexaInstallerPreflightStatus.Missing, reason, Required: true);
}

public sealed class NexaDeploymentRollbackDryRunEvaluator
{
    public NexaDeploymentRollbackDecision Evaluate(NexaDeploymentRollbackDryRun plan)
    {
        var executes = !plan.ModelOnly || plan.Steps.Any(step => step.ExecutesRealRollback);
        return new NexaDeploymentRollbackDecision(
            Allowed: !executes,
            Executed: false,
            Reason: executes ? "rollback dry-run attempted executable rollback" : "rollback dry-run model only",
            Plan: plan);
    }
}
