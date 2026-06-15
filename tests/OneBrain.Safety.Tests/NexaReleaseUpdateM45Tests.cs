using Microsoft.VisualStudio.TestTools.UnitTesting;
using OneBrain.BrowserExecutor.Cdp;
using OneBrain.BrowserExecutor.Contracts;

namespace OneBrain.Safety.Tests;

[TestClass]
public sealed class NexaReleaseUpdateM45Tests
{
    [TestMethod]
    public void NexaReleaseChannelSupportsDevPreviewStableEnterprisePinned()
    {
        var kinds = NexaReleaseChannel.Defaults().Select(c => c.Kind).ToHashSet();

        Assert.IsTrue(kinds.Contains(NexaReleaseChannelKind.Dev));
        Assert.IsTrue(kinds.Contains(NexaReleaseChannelKind.Preview));
        Assert.IsTrue(kinds.Contains(NexaReleaseChannelKind.Stable));
        Assert.IsTrue(kinds.Contains(NexaReleaseChannelKind.EnterprisePinned));
    }

    [TestMethod]
    public void NexaUpdateManifestRequiresHash()
    {
        var manifest = Manifest() with { Integrity = new NexaUpdateIntegrityDescriptor("", "signature-placeholder", true) };

        Assert.IsFalse(manifest.Validate().IsValid);
    }

    [TestMethod]
    public void NexaUpdateManifestRequiresSignatureMetadata()
    {
        var manifest = Manifest() with { Integrity = new NexaUpdateIntegrityDescriptor("hash-abc", "", true) };

        Assert.IsFalse(manifest.Validate().IsValid);
    }

    [TestMethod]
    public void NexaUpdateEligibilityFailsWhenChannelDisabled()
    {
        var result = Evaluate(NexaReleaseChannelKind.Disabled);

        Assert.AreEqual(NexaUpdateEligibilityDecisionKind.Denied, result.Decision);
    }

    [TestMethod]
    public void NexaUpdateEligibilityFailsForEnterprisePinnedMismatch()
    {
        var result = Evaluate(NexaReleaseChannelKind.EnterprisePinned, pinned: new NexaReleaseVersion(1, 0, 0, "stable"));

        Assert.AreEqual(NexaUpdateEligibilityDecisionKind.Denied, result.Decision);
    }

    [TestMethod]
    public void NexaUpdateEligibilityFailsWhenRuntimeIncompatible()
    {
        var manifest = Manifest() with { Compatibility = new NexaUpdateCompatibilityCheck(new NexaReleaseCompatibility("net11.0", "Windows", "Chrome", RuntimeCompatible: false), Passed: false) };
        var result = Evaluate(NexaReleaseChannelKind.Preview, manifest: manifest);

        Assert.AreEqual(NexaUpdateEligibilityDecisionKind.Denied, result.Decision);
    }

    [TestMethod]
    public void NexaUpdateEligibilityRequiresAdminApprovalWhenPolicyRequires()
    {
        var result = Evaluate(NexaReleaseChannelKind.Preview, adminApproval: false);

        Assert.AreEqual(NexaUpdateEligibilityDecisionKind.RequiresApproval, result.Decision);
    }

    [TestMethod]
    public void NexaUpdateEligibilityDoesNotExecuteUpdate()
    {
        var result = Evaluate(NexaReleaseChannelKind.Preview, adminApproval: true);

        Assert.IsTrue(result.Eligible, result.Reason);
        Assert.IsFalse(result.UpdateExecuted);
    }

    [TestMethod]
    public void NexaRollbackPlanExistsButDoesNotExecute()
    {
        var plan = new NexaRollbackPlan("rollback-plan-one", new NexaReleaseVersion(1, 1, 0, "preview"), new NexaReleaseVersion(1, 0, 0, "preview"), ExecuteAutomatically: false);
        var decision = new NexaRollbackEvaluator().Decide(plan);

        Assert.IsTrue(decision.Allowed, decision.Reason);
        Assert.IsFalse(decision.Executed);
    }

    [TestMethod]
    public void NexaReleaseAuditDoesNotContainSecrets()
    {
        var result = Evaluate(NexaReleaseChannelKind.Preview, adminApproval: true);

        Assert.IsTrue(result.AuditEvent.Validate().IsValid);
        Assert.IsFalse(result.AuditEvent.ToString()!.Contains("password", StringComparison.OrdinalIgnoreCase));
    }

    [TestMethod]
    public void BrowserRuntimePhaseGateFailsWhenAutoUpdateExecutionEnabled()
    {
        var report = BrowserVaultMinimalM23Tests.GateReport(BrowserVaultMinimalM23Tests.SafeState(vaultMinimal: true) with
        {
            ProductAdminFoundationDefined = true,
            LicensingFoundationDefined = true,
            ConfigurationProfilesDefined = true,
            ProductionLockedProfileDefined = true,
            ReleaseChannelsDefined = true,
            UpdateManifestDefined = true,
            AutoUpdateExecutionDisabled = false,
            RollbackModelDefined = true
        });

        Assert.AreEqual(BrowserRuntimePhaseCloseStatus.Failed, report.Status);
        CollectionAssert.Contains(report.FailedChecks.ToList(), "configuration profiles release update safe");
    }

    internal static NexaUpdateManifest Manifest(NexaReleaseChannelKind channel = NexaReleaseChannelKind.Preview) =>
        new(
            "update-manifest-one",
            channel,
            new NexaUpdatePackageDescriptor("package-nexa", new NexaReleaseVersion(1, 1, 0, "preview"), [new NexaReleaseComponent("component-browser-runtime", "1.1.0", "hash-component")]),
            new NexaUpdateIntegrityDescriptor("hash-update", "signature-placeholder", SignatureRequired: true),
            new NexaUpdateCompatibilityCheck(new NexaReleaseCompatibility("net11.0", "Windows", "Chrome", RuntimeCompatible: true), Passed: true),
            new NexaUpdateRollbackPlan(new NexaReleaseVersion(1, 0, 0, "preview"), ExecuteAutomatically: false, "model only"),
            ["compatible with fixture runtime"],
            Redacted: true);

    private static NexaUpdateEligibilityResult Evaluate(NexaReleaseChannelKind channelKind, NexaUpdateManifest? manifest = null, bool adminApproval = true, NexaReleaseVersion? pinned = null)
    {
        var channel = NexaReleaseChannel.Defaults().Single(c => c.Kind == channelKind);
        manifest ??= Manifest(channelKind == NexaReleaseChannelKind.Disabled ? NexaReleaseChannelKind.Disabled : channelKind);
        return new NexaUpdateEligibilityEvaluator().Evaluate(new NexaUpdateEligibilityRequest(new NexaReleaseVersion(1, 0, 0, "preview"), manifest, channel, NexaPlanKind.Pro, TenantPolicyAllows: true, NexaConfigurationProfileKind.InternalPreview, adminApproval, AutoExecuteRequested: false, pinned));
    }
}
