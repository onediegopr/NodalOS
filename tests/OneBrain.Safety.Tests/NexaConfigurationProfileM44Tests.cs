using Microsoft.VisualStudio.TestTools.UnitTesting;
using OneBrain.BrowserExecutor.Cdp;
using OneBrain.BrowserExecutor.Contracts;

namespace OneBrain.Safety.Tests;

[TestClass]
public sealed class NexaConfigurationProfileM44Tests
{
    [TestMethod]
    public void NexaConfigurationProfileDevelopmentAllowsSyntheticFixtures()
    {
        var profile = NexaConfigurationProfile.Development();

        Assert.AreEqual(NexaProfileVaultMode.OsBackedSynthetic, profile.VaultMode);
        Assert.AreEqual(NexaProfileBrowserRuntimeMode.LocalCdpReadOnly, profile.BrowserRuntimeMode);
        Assert.IsFalse(profile.Policy.AllowRealBilling);
    }

    [TestMethod]
    public void NexaConfigurationProfileTestBlocksRealBilling()
    {
        var result = Evaluate(NexaConfigurationProfile.Test(), realBilling: true);

        Assert.IsFalse(result.Decision.Allowed);
        Assert.IsTrue(result.Decision.Violations.Any(v => v.Contains("real billing", StringComparison.OrdinalIgnoreCase)));
    }

    [TestMethod]
    public void NexaConfigurationProfileTestBlocksRealEmail()
    {
        var result = Evaluate(NexaConfigurationProfile.Test(), realEmail: true);

        Assert.IsFalse(result.Decision.Allowed);
        Assert.IsTrue(result.Decision.Violations.Any(v => v.Contains("real email", StringComparison.OrdinalIgnoreCase)));
    }

    [TestMethod]
    public void NexaConfigurationProfileProductionLockedDisablesSensitiveRealPilot()
    {
        var profile = NexaConfigurationProfile.ProductionLocked();

        Assert.IsFalse(profile.Features.EnabledFeatures.Contains(NexaFeatureFlag.SensitiveRealPilot));
        Assert.AreEqual(NexaProfileSensitiveSiteMode.Disabled, profile.SensitiveSiteMode);
    }

    [TestMethod]
    public void NexaConfigurationProfileProductionLockedDisablesRecorderReplayProductive()
    {
        var profile = NexaConfigurationProfile.ProductionLocked();

        Assert.AreEqual(NexaProfileRecorderReplayMode.Disabled, profile.RecorderReplayMode);
        Assert.IsTrue(profile.Features.DisabledFeatures.Contains(NexaFeatureFlag.RecorderProductive));
        Assert.IsTrue(profile.Features.DisabledFeatures.Contains(NexaFeatureFlag.ReplayProductive));
    }

    [TestMethod]
    public void NexaConfigurationProfileProductionLockedKeepsSupportMetadataOnly()
    {
        Assert.IsTrue(NexaConfigurationProfile.ProductionLocked().Policy.SupportMetadataOnly);
    }

    [TestMethod]
    public void NexaConfigurationProfileBlocksProfileRaw()
    {
        var result = Evaluate(NexaConfigurationProfile.Development(), profileRaw: true);

        Assert.IsFalse(result.Decision.Allowed);
    }

    [TestMethod]
    public void NexaConfigurationProfileBlocksProductiveVaultWithoutOverride()
    {
        var result = Evaluate(NexaConfigurationProfile.EnterpriseControlled(), requested: new HashSet<NexaFeatureFlag> { NexaFeatureFlag.ProductiveVault }, productiveVaultEntitlement: true, compliance: true, adminOverride: false);

        Assert.IsFalse(result.Decision.Allowed);
    }

    [TestMethod]
    public void NexaConfigurationProfileEvaluatorFailsClosedForUnknownProfile()
    {
        var unknown = NexaConfigurationProfile.ProductionLocked() with { Kind = NexaConfigurationProfileKind.Unknown };
        var result = Evaluate(unknown);

        Assert.IsFalse(result.Decision.Allowed);
    }

    [TestMethod]
    public void NexaConfigurationProfileDiagnosticsRemainRedacted()
    {
        Assert.IsTrue(NexaConfigurationProfile.ProductionLocked().Policy.DiagnosticsRedacted);
    }

    [TestMethod]
    public void BrowserRuntimePhaseGateFailsUnknownConfigurationProfile()
    {
        var report = BrowserVaultMinimalM23Tests.GateReport(BrowserVaultMinimalM23Tests.SafeState(vaultMinimal: true) with
        {
            ProductAdminFoundationDefined = true,
            LicensingFoundationDefined = true,
            ConfigurationProfilesDefined = true,
            ProductionLockedProfileDefined = true,
            UnknownConfigurationProfileActive = true
        });

        Assert.AreEqual(BrowserRuntimePhaseCloseStatus.Failed, report.Status);
        CollectionAssert.Contains(report.FailedChecks.ToList(), "configuration profiles release update safe");
    }

    private static NexaConfigurationProfileEvaluationResult Evaluate(
        NexaConfigurationProfile profile,
        IReadOnlySet<NexaFeatureFlag>? requested = null,
        bool productiveVaultEntitlement = false,
        bool compliance = false,
        bool adminOverride = false,
        bool realBilling = false,
        bool realEmail = false,
        bool publicSaas = false,
        bool profileRaw = false) =>
        new NexaConfigurationProfileEvaluator().Evaluate(new NexaConfigurationProfileRequest(profile, requested ?? new HashSet<NexaFeatureFlag>(), productiveVaultEntitlement, compliance, adminOverride, realBilling, realEmail, publicSaas, profileRaw));
}
