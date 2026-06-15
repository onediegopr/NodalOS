using Microsoft.VisualStudio.TestTools.UnitTesting;
using OneBrain.BrowserExecutor.Cdp;
using OneBrain.BrowserExecutor.Contracts;

namespace OneBrain.Safety.Tests;

[TestClass]
public sealed class NexaPackagingDiagnosticsM42Tests
{
    [TestMethod]
    public void NexaPackageManifestIncludesRuntimeComponents()
    {
        var manifest = Manifest();

        Assert.IsTrue(manifest.Validate().IsValid, string.Join("; ", manifest.Validate().Errors));
        Assert.IsTrue(manifest.Components.Any(c => c.ComponentId == "component-browser-runtime"));
        Assert.IsTrue(manifest.Components.Any(c => c.ComponentId == "component-admin-runtime"));
    }

    [TestMethod]
    public void NexaPackageReadinessReportsMissingBrowserRuntime()
    {
        var manifest = Manifest() with { Components = Manifest().Components.Where(c => c.ComponentId != "component-browser-runtime").ToArray() };
        var report = new NexaPackageReadinessEvaluator().Evaluate(manifest);

        Assert.IsFalse(report.Ready);
        Assert.IsTrue(report.Checks.Any(c => c.CheckId == "browser-runtime-available" && c.Status == NexaHealthCheckStatus.Failed));
    }

    [TestMethod]
    public void NexaDiagnosticsBundleDoesNotContainSecrets()
    {
        var result = Diagnostics(["password=abc"]);

        Assert.IsTrue(result.Bundle!.IsSafe);
        Assert.IsFalse(result.Bundle.ToString()!.Contains("password=abc", StringComparison.OrdinalIgnoreCase));
    }

    [TestMethod]
    public void NexaDiagnosticsBundleDoesNotContainCookies()
    {
        var result = Diagnostics(["cookie=session"]);

        Assert.IsTrue(result.Bundle!.IsSafe);
        Assert.IsFalse(result.Bundle.ToString()!.Contains("cookie=session", StringComparison.OrdinalIgnoreCase));
    }

    [TestMethod]
    public void NexaDiagnosticsBundleDoesNotContainBodies()
    {
        var result = Diagnostics(["request body: hidden"]);

        Assert.IsTrue(result.Bundle!.IsSafe);
        Assert.IsFalse(result.Bundle.ToString()!.Contains("request body:", StringComparison.OrdinalIgnoreCase));
    }

    [TestMethod]
    public void NexaDiagnosticsBundleIncludesHealthReport()
    {
        var result = Diagnostics([]);

        Assert.IsNotNull(result.Bundle!.HealthReport);
        Assert.IsTrue(result.Bundle.HealthReport.Healthy);
    }

    [TestMethod]
    public void NexaDiagnosticsBundleIncludesRedactionManifest()
    {
        var result = Diagnostics([]);

        Assert.AreEqual("diagnostics-redaction-strict", result.Bundle!.Manifest.RedactionPolicyId);
        Assert.IsFalse(string.IsNullOrWhiteSpace(result.Bundle.Manifest.Hash));
    }

    [TestMethod]
    public void NexaSupportModeIsMetadataOnly()
    {
        var tenant = Tenant("tenant-main");
        var decision = new NexaSupportModeService().Evaluate(NexaSupportModePolicy.StrictMetadataOnly(), tenant, tenant);

        Assert.IsTrue(decision.Allowed, decision.Reason);
    }

    [TestMethod]
    public void NexaSupportModeCannotAccessVaultRawValues()
    {
        var tenant = Tenant("tenant-main");
        var policy = NexaSupportModePolicy.StrictMetadataOnly() with { AllowVaultRawAccess = true };
        var decision = new NexaSupportModeService().Evaluate(policy, tenant, tenant);

        Assert.IsFalse(decision.Allowed);
    }

    [TestMethod]
    public void NexaSupportModeCannotCrossTenant()
    {
        var decision = new NexaSupportModeService().Evaluate(NexaSupportModePolicy.StrictMetadataOnly(), Tenant("tenant-a", "account-a"), Tenant("tenant-b", "account-b"));

        Assert.IsFalse(decision.Allowed);
    }

    [TestMethod]
    public void NexaHealthReportFailsClosedForUnknownComponent()
    {
        var report = new NexaHealthReportCollector().UnknownComponentReport();

        Assert.IsFalse(report.Healthy);
    }

    [TestMethod]
    public void BrowserRuntimePhaseGateFailsWhenDiagnosticsLeakSecrets()
    {
        var report = BrowserVaultMinimalM23Tests.GateReport(BrowserVaultMinimalM23Tests.SafeState(vaultMinimal: true) with
        {
            ProductAdminFoundationDefined = true,
            LicensingFoundationDefined = true,
            PackagingFoundationDefined = true,
            DiagnosticsBundleDefined = true,
            DiagnosticsBundleLeaksSecrets = true
        });

        Assert.AreEqual(BrowserRuntimePhaseCloseStatus.Failed, report.Status);
        CollectionAssert.Contains(report.FailedChecks.ToList(), "packaging diagnostics billing onboarding safe");
    }

    internal static NexaPackageManifest Manifest() =>
        new(
            "package-nexa-dev",
            new NexaPackageVersion("1.0.0-test", "net11-preview", "browser-runtime-m43"),
            NexaPackageChannel.Test,
            new NexaPackageEnvironment("Windows", "net11.0", BrowserAvailable: true, CdpAvailable: true, VaultProviderAvailable: true, AdminLicensingAvailable: true),
            [
                new NexaPackageComponent("component-browser-runtime", "Browser Runtime", "m43", Available: true),
                new NexaPackageComponent("component-admin-runtime", "Admin Runtime", "m43", Available: true),
                new NexaPackageComponent("component-tenant-governance", "Tenant Governance", "m43", Available: true),
                new NexaPackageComponent("component-audit-export", "Audit Export", "m43", Available: true)
            ],
            [NexaFeatureFlag.BrowserRuntime, NexaFeatureFlag.AdminConsole],
            Redacted: true);

    private static NexaDiagnosticsBundleResult Diagnostics(IReadOnlyList<string> errors)
    {
        var account = NexaProductAdminM36Tests.CompanyAccount();
        var license = NexaAdminRuntimeM40Tests.License("license-diagnostics", account.AccountId, NexaPlan.Pro());
        var audit = new NexaAdminPolicyEvaluator().Evaluate(account, "actor-owner", NexaRole.Owner, NexaAdminAction.ViewAudit).AuditEvent;
        return new NexaDiagnosticsCollector().Collect(new NexaDiagnosticsBundleRequest("diagnostics-bundle-one", NexaDiagnosticsRedactionPolicy.Strict(), IncludeRecentErrors: true, IncludeAuditSummary: true), Manifest(), license, account, [audit], errors);
    }

    private static NexaTenant Tenant(string tenantId, string accountId = "account-company") =>
        new(tenantId, accountId, "org-main", "workspace-main", "worker-main");
}
