using Microsoft.VisualStudio.TestTools.UnitTesting;
using OneBrain.BrowserExecutor.Cdp;
using OneBrain.BrowserExecutor.Contracts;

namespace OneBrain.Safety.Tests;

[TestClass]
public sealed class NexaPublicApiBoundaryM47Tests
{
    [TestMethod]
    public void NexaPublicApiBoundaryIsDesignOnly()
    {
        var boundary = NexaPublicApiBoundary.DesignOnlyDefault();

        Assert.AreEqual(NexaPublicApiExposureMode.DesignOnly, boundary.ExposureMode);
        Assert.IsTrue(boundary.Validate().IsValid);
    }

    [TestMethod]
    public void NexaPublicApiBoundaryDoesNotOpenNetworkListener()
    {
        var boundary = NexaPublicApiBoundary.DesignOnlyDefault();

        Assert.IsTrue(boundary.NoNetworkListener);
        Assert.IsFalse(boundary.Endpoints.Any(endpoint => endpoint.OpensNetworkListener));
    }

    [TestMethod]
    public void NexaPublicApiDtosDoNotExposeSecrets()
    {
        var (decision, _) = Evaluate();

        Assert.IsTrue(decision.Allowed, string.Join("; ", decision.ReasonCodes));
        Assert.IsFalse(decision.Response.ContainsSecret);
    }

    [TestMethod]
    public void NexaPublicApiDtosDoNotExposeCookies()
    {
        var (decision, _) = Evaluate();

        Assert.IsFalse(decision.Response.ContainsCookie);
    }

    [TestMethod]
    public void NexaPublicApiDtosDoNotExposeBodies()
    {
        var (decision, _) = Evaluate();

        Assert.IsFalse(decision.Response.ContainsBody);
    }

    [TestMethod]
    public void NexaPublicApiBlocksCrossTenantRequest()
    {
        var target = Tenant() with { WorkspaceId = "workspace-two" };
        var (decision, _) = Evaluate(request: Request(targetTenant: target));

        Assert.IsFalse(decision.Allowed);
        CollectionAssert.Contains(decision.ReasonCodes.ToList(), "cross-tenant request blocked");
    }

    [TestMethod]
    public void NexaPublicApiBlocksUnknownTenant()
    {
        var (decision, _) = Evaluate(request: Request(missingTargetTenant: true));

        Assert.IsFalse(decision.Allowed);
        CollectionAssert.Contains(decision.ReasonCodes.ToList(), "unknown tenant");
    }

    [TestMethod]
    public void NexaPublicApiBlocksSupportSecretAccess()
    {
        var (decision, _) = Evaluate(request: Request(role: NexaRole.Support, containsSecret: true));

        Assert.IsFalse(decision.Allowed);
        Assert.IsTrue(decision.ReasonCodes.Any(reason => reason.Contains("support", StringComparison.OrdinalIgnoreCase)));
    }

    [TestMethod]
    public void NexaPublicApiBlocksSensitiveCapabilityWithoutCompliance()
    {
        var (decision, _) = Evaluate(request: Request(feature: NexaFeatureFlag.SensitiveRealPilot, compliance: false));

        Assert.IsFalse(decision.Allowed);
        Assert.IsTrue(decision.ReasonCodes.Any(reason => reason.Contains("compliance", StringComparison.OrdinalIgnoreCase)));
    }

    [TestMethod]
    public void NexaPublicApiAppliesLicenseFeatureDecision()
    {
        var (decision, _) = Evaluate(request: Request(licenseAllowsFeature: false));

        Assert.IsFalse(decision.Allowed);
        CollectionAssert.Contains(decision.ReasonCodes.ToList(), "license feature disabled");
    }

    [TestMethod]
    public void NexaPublicApiRateLimitBlocksWhenExceeded()
    {
        var rateLimit = new NexaPublicApiRateLimitPolicy(new NexaPublicApiRateLimit("api-limit-one", BurstLimit: 1, DailyLimit: 10, MonthlyLimit: 100))
            .Evaluate(new NexaPublicApiUsageCounter("api-limit-one", BurstCount: 1, DailyCount: 0, MonthlyCount: 0));
        var (decision, _) = Evaluate(rateLimit: rateLimit);

        Assert.IsFalse(decision.Allowed);
        Assert.IsTrue(decision.ReasonCodes.Any(reason => reason.Contains("burst", StringComparison.OrdinalIgnoreCase)));
    }

    [TestMethod]
    public void NexaPublicApiAuditEventIsRedacted()
    {
        var (_, audit) = Evaluate();

        Assert.IsTrue(audit.Redacted);
        Assert.IsTrue(audit.Validate().IsValid);
    }

    [TestMethod]
    public void BrowserRuntimePhaseGateFailsWhenPublicApiNetworkListenerEnabled()
    {
        var report = BrowserVaultMinimalM23Tests.GateReport(SafeM47State() with { PublicApiNetworkExposureDisabled = false });

        Assert.AreEqual(BrowserRuntimePhaseCloseStatus.Failed, report.Status);
        CollectionAssert.Contains(report.FailedChecks.ToList(), "public api boundary safe");
    }

    private static (NexaPublicApiDecision Decision, NexaPublicApiAuditEvent Audit) Evaluate(NexaPublicApiRequest? request = null, NexaPublicApiRateLimitDecision? rateLimit = null) =>
        new NexaPublicApiBoundaryEvaluator().Evaluate(NexaPublicApiBoundary.DesignOnlyDefault(), request ?? Request(), rateLimit);

    private static NexaPublicApiRequest Request(
        NexaPublicApiTenantContext? targetTenant = null,
        NexaRole role = NexaRole.Owner,
        NexaFeatureFlag feature = NexaFeatureFlag.AdminConsole,
        bool compliance = true,
        bool licenseAllowsFeature = true,
        bool containsSecret = false,
        bool missingTargetTenant = false) =>
        new(
            NexaPublicApiBoundary.DesignOnlyDefault().Endpoints[0],
            new NexaPublicApiAuthContext("actor-one", role, Authenticated: true),
            Tenant(),
            missingTargetTenant ? null : targetTenant ?? Tenant(),
            feature,
            compliance,
            licenseAllowsFeature,
            RequestContainsSecret: containsSecret,
            RequestContainsCookie: false,
            RequestContainsBody: false);

    private static NexaPublicApiTenantContext Tenant() =>
        new("tenant-one", "account-one", "org-one", "workspace-one", "worker-one");

    private static BrowserRuntimeObservedState SafeM47State() =>
        BrowserVaultMinimalM23Tests.SafeState(vaultMinimal: true) with
        {
            ProductAdminFoundationDefined = true,
            LicensingFoundationDefined = true,
            TenantGovernanceDefined = true,
            CrossTenantIsolationEnabled = true,
            ConfigurationProfilesDefined = true,
            ProductionLockedProfileDefined = true,
            ReleaseChannelsDefined = true,
            UpdateManifestDefined = true,
            AutoUpdateExecutionDisabled = true,
            RollbackModelDefined = true,
            InstallerDryRunDefined = true,
            DeploymentPreflightDefined = true,
            RollbackDryRunDefined = true,
            PublicApiBoundaryDefined = true,
            PublicApiDesignOnly = true,
            PublicApiNetworkExposureDisabled = true
        };
}
