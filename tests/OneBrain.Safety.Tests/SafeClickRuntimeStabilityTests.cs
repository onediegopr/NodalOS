using Microsoft.VisualStudio.TestTools.UnitTesting;
using OneBrain.Core.Approval;
using OneBrain.Core.Contracts;
using OneBrain.Core.Execution;
using OneBrain.Core.Identity;
using OneBrain.Core.Models;
using OneBrain.Core.Safety;
using OneBrain.Core.Selectors;

namespace OneBrain.Safety.Tests;

[TestClass]
public sealed class SafeClickRuntimeStabilityTests
{
    [TestMethod]
    public void StableRuntimeIdProducesReobservedStable()
    {
        var approved = StrongIdentity("runtime-1");
        var manifest = StrongManifest(approved);

        var stability = SafeClickRuntimeStabilityEvaluator.Evaluate(
            manifest,
            StrongIdentity("runtime-1"),
            reobserveAttempted: true,
            reobserveSucceeded: true);

        Assert.AreEqual(SafeClickRuntimeStabilityVerdict.ReobservedStable, stability.StabilityVerdict);
        Assert.AreEqual(RuntimeIdentityMatch.Same, stability.ReobserveMatch);
        Assert.IsTrue(stability.AllowsDefaultDispatch);
    }

    [TestMethod]
    public void ChangedRuntimeIdProducesReobservedChanged()
    {
        var manifest = StrongManifest(StrongIdentity("runtime-1"));

        var stability = SafeClickRuntimeStabilityEvaluator.Evaluate(
            manifest,
            StrongIdentity("runtime-2"),
            reobserveAttempted: true,
            reobserveSucceeded: true);

        Assert.AreEqual(SafeClickRuntimeStabilityVerdict.ReobservedChanged, stability.StabilityVerdict);
        Assert.AreEqual(RuntimeIdentityMatch.Different, stability.ReobserveMatch);
        Assert.IsTrue(stability.RuntimeIdChanged);
        Assert.IsFalse(stability.AllowsDefaultDispatch);
    }

    [TestMethod]
    public void MissingRuntimeIdProducesMissing()
    {
        var manifest = StrongManifest(StrongIdentity("runtime-1"));

        var stability = SafeClickRuntimeStabilityEvaluator.Evaluate(
            manifest,
            StrongIdentity(""),
            reobserveAttempted: true,
            reobserveSucceeded: true);

        Assert.AreEqual(SafeClickRuntimeStabilityVerdict.Missing, stability.StabilityVerdict);
        Assert.AreEqual(RuntimeIdentityMatch.Missing, stability.ReobserveMatch);
        Assert.IsFalse(stability.AllowsDefaultDispatch);
    }

    [TestMethod]
    public void LikelySameDoesNotAllowDefaultDispatch()
    {
        var manifest = StrongManifest(StrongIdentity("runtime-1"));
        var likelySame = StrongIdentity("") with { AutomationId = "target", Name = "Target", ControlType = "Hyperlink", Role = "Hyperlink" };

        var stability = SafeClickRuntimeStabilityEvaluator.Evaluate(
            manifest,
            likelySame,
            reobserveAttempted: true,
            reobserveSucceeded: true);

        Assert.AreNotEqual(RuntimeIdentityMatch.Same, stability.ReobserveMatch);
        Assert.IsFalse(stability.AllowsDefaultDispatch);
    }

    private static ApprovalManifest StrongManifest(ElementIdentity identity)
    {
        return new ApprovalManifest
        {
            IdentitySchemaVersion = ApprovalManifestBuilder.IdentitySchemaVersion,
            IdentityStrength = IdentityStrength.Strong,
            ApprovedIdentityDigest = ElementFingerprintBuilder.Build(identity),
            ApprovedSelector = new SelectorDefinition
            {
                Provenance = Provenance.Uia,
                Role = identity.EffectiveControlType,
                Name = identity.Name,
                AutomationId = identity.AutomationId,
                ExpectedIdentity = identity
            },
            IdentitySource = "web-uia",
            IdentityBindingHash = "identity-binding"
        };
    }

    private static ElementIdentity StrongIdentity(string runtimeId)
    {
        return new ElementIdentity
        {
            RuntimeId = runtimeId,
            AutomationId = "target",
            Name = "Target",
            Role = "Hyperlink",
            ControlType = "Hyperlink",
            ClassName = "Chrome_RenderWidgetHostHWND",
            FrameworkId = "UIA",
            AncestorPath = "Window:ONE Brain > Document:Main",
            ProcessName = "msedge",
            WindowTitle = "ONE Brain",
            Provenance = string.IsNullOrWhiteSpace(runtimeId) ? Provenance.Inferred : Provenance.Uia
        };
    }
}
