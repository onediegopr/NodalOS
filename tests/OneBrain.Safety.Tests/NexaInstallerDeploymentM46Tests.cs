using Microsoft.VisualStudio.TestTools.UnitTesting;
using OneBrain.BrowserExecutor.Cdp;
using OneBrain.BrowserExecutor.Contracts;

namespace OneBrain.Safety.Tests;

[TestClass]
public sealed class NexaInstallerDeploymentM46Tests
{
    [TestMethod]
    public void NexaInstallerDryRunDoesNotModifyRealSystem()
    {
        var result = Evaluate();

        Assert.IsTrue(result.Allowed, string.Join("; ", result.Violations));
        Assert.IsFalse(result.ModifiedRealSystem);
    }

    [TestMethod]
    public void NexaInstallerDryRunReportsFileLayout()
    {
        var result = Evaluate();

        Assert.IsTrue(result.FileLayout.SandboxOnly);
        Assert.IsTrue(result.FileLayout.Directories.Count > 0);
        Assert.IsTrue(result.FileLayout.Files.Count > 0);
    }

    [TestMethod]
    public void NexaInstallerDryRunReportsMissingPrerequisites()
    {
        var result = Evaluate(osSupported: false);

        Assert.IsFalse(result.Allowed);
        Assert.IsTrue(result.PreflightChecks.Any(check => check.Status == NexaInstallerPreflightStatus.Missing));
    }

    [TestMethod]
    public void NexaInstallerDryRunBlocksUnknownProfile()
    {
        var plan = Plan(NexaConfigurationProfile.ProductionLocked() with { Kind = NexaConfigurationProfileKind.Unknown });
        var result = Evaluate(plan: plan);

        Assert.IsFalse(result.Allowed);
        Assert.IsTrue(result.Violations.Any(reason => reason.Contains("unknown", StringComparison.OrdinalIgnoreCase)));
    }

    [TestMethod]
    public void NexaInstallerDryRunBlocksSensitiveRealPilotByDefault()
    {
        var profile = NexaConfigurationProfile.ProductionLocked() with
        {
            Features = new NexaConfigurationProfileFeatureSet(new HashSet<NexaFeatureFlag> { NexaFeatureFlag.SensitiveRealPilot }, new HashSet<NexaFeatureFlag>())
        };
        var result = Evaluate(plan: Plan(profile));

        Assert.IsFalse(result.Allowed);
    }

    [TestMethod]
    public void NexaInstallerDryRunBlocksRealBilling()
    {
        var profile = NexaConfigurationProfile.Test() with { BillingMode = NexaProfileExternalMode.FutureReal, Policy = NexaConfigurationProfile.Test().Policy with { AllowRealBilling = true } };
        var result = Evaluate(plan: Plan(profile));

        Assert.IsFalse(result.Allowed);
    }

    [TestMethod]
    public void NexaInstallerDryRunBlocksRealEmail()
    {
        var profile = NexaConfigurationProfile.Test() with { EmailMode = NexaProfileExternalMode.FutureReal, Policy = NexaConfigurationProfile.Test().Policy with { AllowRealEmail = true } };
        var result = Evaluate(plan: Plan(profile));

        Assert.IsFalse(result.Allowed);
    }

    [TestMethod]
    public void NexaInstallerDryRunRequiresReleaseManifestIntegrity()
    {
        var manifest = NexaReleaseUpdateM45Tests.Manifest() with { Integrity = new NexaUpdateIntegrityDescriptor("", "", SignatureRequired: true) };
        var result = Evaluate(plan: Plan(releaseManifest: new NexaReleaseManifest("release-one", NexaReleaseChannel.Defaults().Single(c => c.Kind == NexaReleaseChannelKind.Preview), manifest, Redacted: true)));

        Assert.IsFalse(result.Allowed);
    }

    [TestMethod]
    public void NexaInstallerDryRunProducesRollbackPlan()
    {
        var result = Evaluate();

        Assert.IsFalse(result.RollbackPlan.ExecutesRollback);
        Assert.IsTrue(result.RollbackPlan.RollbackSteps.Count > 0);
    }

    [TestMethod]
    public void NexaDeploymentRollbackDryRunDoesNotExecute()
    {
        var plan = new NexaDeploymentRollbackDryRun("rollback-dry-run-one", [new NexaDeploymentRollbackStep("rollback-step-one", "restore previous manifest model", ExecutesRealRollback: false)], ModelOnly: true);
        var decision = new NexaDeploymentRollbackDryRunEvaluator().Evaluate(plan);

        Assert.IsTrue(decision.Allowed, decision.Reason);
        Assert.IsFalse(decision.Executed);
    }

    [TestMethod]
    public void BrowserRuntimePhaseGateFailsWhenInstallerModifiesRealSystem()
    {
        var report = BrowserVaultMinimalM23Tests.GateReport(SafeM46State() with { InstallerModifiesRealSystem = true });

        Assert.AreEqual(BrowserRuntimePhaseCloseStatus.Failed, report.Status);
        CollectionAssert.Contains(report.FailedChecks.ToList(), "installer deployment dry run safe");
    }

    private static NexaInstallerDryRunResult Evaluate(NexaInstallerPlan? plan = null, bool osSupported = true) =>
        new NexaInstallerDryRunEvaluator().Evaluate(new NexaInstallerDryRunRequest(
            plan ?? Plan(),
            osSupported,
            DotNetRuntimeCompatible: true,
            BrowserAvailabilityDeclared: true,
            CdpCapabilityDeclared: true,
            VaultProviderDeclared: true,
            DiagnosticsRedactionActive: true,
            TenantGovernanceAvailable: true,
            AdminRuntimeAvailable: true,
            LicenseEvaluatorAvailable: true));

    private static NexaInstallerPlan Plan(NexaConfigurationProfile? profile = null, NexaReleaseManifest? releaseManifest = null) =>
        new(
            "installer-plan-one",
            profile ?? NexaConfigurationProfile.LocalSandbox(),
            releaseManifest ?? new NexaReleaseManifest("release-one", NexaReleaseChannel.Defaults().Single(c => c.Kind == NexaReleaseChannelKind.Preview), NexaReleaseUpdateM45Tests.Manifest(), Redacted: true),
            new NexaInstallerFileLayout("[SANDBOX]/nexa", ["bin", "logs", "diagnostics"], ["nexa.exe.model", "manifest.json.model"], SandboxOnly: true),
            [new NexaInstallerPermissionRequirement("permission-local-user", "local sandbox directory", Elevated: false, ModelOnly: true)],
            [
                new NexaInstallerPlanStep("step-preflight", NexaInstallerPlanStepKind.Preflight, "evaluate prerequisites", WouldModifyRealSystem: false),
                new NexaInstallerPlanStep("step-file-layout", NexaInstallerPlanStepKind.FileLayout, "report file layout", WouldModifyRealSystem: false)
            ],
            new NexaInstallerRollbackDryRunPlan("rollback-plan-one", ["remove model layout", "restore previous manifest model"], ExecutesRollback: false),
            RegistersRealService: false,
            CreatesRealScheduledTask: false,
            TouchesRegistry: false,
            OpensPublicPort: false,
            AutoUpdateRealEnabled: false);

    private static BrowserRuntimeObservedState SafeM46State() =>
        BrowserVaultMinimalM23Tests.SafeState(vaultMinimal: true) with
        {
            ProductAdminFoundationDefined = true,
            LicensingFoundationDefined = true,
            ConfigurationProfilesDefined = true,
            ProductionLockedProfileDefined = true,
            ReleaseChannelsDefined = true,
            UpdateManifestDefined = true,
            AutoUpdateExecutionDisabled = true,
            RollbackModelDefined = true,
            InstallerDryRunDefined = true,
            DeploymentPreflightDefined = true,
            RollbackDryRunDefined = true
        };
}
