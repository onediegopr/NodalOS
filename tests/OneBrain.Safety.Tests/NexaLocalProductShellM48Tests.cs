using Microsoft.VisualStudio.TestTools.UnitTesting;
using OneBrain.BrowserExecutor.Cdp;
using OneBrain.BrowserExecutor.Contracts;

namespace OneBrain.Safety.Tests;

[TestClass]
public sealed class NexaLocalProductShellM48Tests
{
    [TestMethod]
    public void NexaLocalProductShellDefinesExpectedRoutes()
    {
        var shell = Shell();
        var paths = shell.RenderModel.Navigation.Routes.Select(route => route.Path).ToHashSet();

        CollectionAssert.IsSubsetOf(new[] { "/dashboard", "/accounts", "/organization", "/workers", "/licenses", "/features", "/usage", "/diagnostics", "/support", "/audit", "/release", "/configuration", "/readiness" }, paths.ToList());
    }

    [TestMethod]
    public void NexaLocalProductShellDashboardIsRedacted()
    {
        var page = Shell().RenderModel.Pages.Single(page => page.Route == NexaLocalProductShellRouteKind.Dashboard);

        Assert.IsTrue(page.Redacted);
    }

    [TestMethod]
    public void NexaLocalProductShellIsTenantScoped()
    {
        Assert.IsTrue(Shell().RenderModel.TenantScoped);
    }

    [TestMethod]
    public void NexaLocalProductShellIsRoleFiltered()
    {
        Assert.IsTrue(Shell().RenderModel.RoleFiltered);
    }

    [TestMethod]
    public void NexaLocalProductShellShowsLicenseAndFeatureStatus()
    {
        var shell = Shell();

        Assert.IsTrue(shell.RenderModel.Pages.Any(page => page.Route == NexaLocalProductShellRouteKind.Licenses));
        Assert.IsTrue(shell.RenderModel.Pages.Any(page => page.Route == NexaLocalProductShellRouteKind.Features));
    }

    [TestMethod]
    public void NexaLocalProductShellShowsDiagnosticsStatus()
    {
        Assert.IsTrue(Shell().RenderModel.Pages.Any(page => page.Route == NexaLocalProductShellRouteKind.Diagnostics));
    }

    [TestMethod]
    public void NexaLocalProductShellShowsAuditExportStatus()
    {
        Assert.IsTrue(Shell().RenderModel.Pages.Any(page => page.Route == NexaLocalProductShellRouteKind.Audit));
    }

    [TestMethod]
    public void NexaLocalProductShellBlocksSensitiveRealPilotAction()
    {
        var decision = Decide(NexaLocalProductShellActionKind.EnableSensitiveRealPilot);

        Assert.IsFalse(decision.Allowed);
    }

    [TestMethod]
    public void NexaLocalProductShellBlocksRealBillingAction()
    {
        var decision = Decide(NexaLocalProductShellActionKind.EnableRealBilling);

        Assert.IsFalse(decision.Allowed);
    }

    [TestMethod]
    public void NexaLocalProductShellBlocksPublicSaasActivation()
    {
        var decision = Decide(NexaLocalProductShellActionKind.EnablePublicSaasActivation);

        Assert.IsFalse(decision.Allowed);
    }

    [TestMethod]
    public void NexaLocalProductShellDoesNotExposeSecretsCookiesBodies()
    {
        var shell = Shell();

        Assert.IsTrue(shell.Validate().IsValid);
        Assert.IsFalse(shell.RenderModel.ContainsSecretsCookiesBodies);
    }

    [TestMethod]
    public void BrowserRuntimePhaseGateFailsWhenLocalShellExposesSecrets()
    {
        var report = BrowserVaultMinimalM23Tests.GateReport(SafeState() with { LocalProductShellExposesSecrets = true });

        Assert.AreEqual(BrowserRuntimePhaseCloseStatus.Failed, report.Status);
        CollectionAssert.Contains(report.FailedChecks.ToList(), "local product shell pre-production checkpoint safe");
    }

    private static NexaLocalProductShell Shell() => new NexaLocalProductShellService().CreateShell();

    private static NexaLocalProductShellDecision Decide(NexaLocalProductShellActionKind kind) =>
        new NexaLocalProductShellService().Decide(new NexaLocalProductShellAction(kind, NexaRole.Owner, NexaPlanKind.Trial, ProductiveVaultEntitlement: false, AdminOverride: false, GatePassed: false));

    internal static BrowserRuntimeObservedState SafeState() =>
        BrowserVaultMinimalM23Tests.SafeState(vaultMinimal: true) with
        {
            ProductAdminFoundationDefined = true,
            LicensingFoundationDefined = true,
            TenantGovernanceDefined = true,
            CrossTenantIsolationEnabled = true,
            PackagingFoundationDefined = true,
            DiagnosticsBundleDefined = true,
            BillingMockDefined = true,
            OnboardingMockDefined = true,
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
            PublicApiNetworkExposureDisabled = true,
            LocalProductShellDefined = true,
            PreProductionCheckpointDefined = true,
            ProductAdminPrivatePreviewAllowed = true,
            PublicSaasStillDisabled = true,
            RealBillingStillDisabled = true,
            SensitiveRealPilotStillDisabled = true,
            ExternalAuditRecommended = true
        };
}
