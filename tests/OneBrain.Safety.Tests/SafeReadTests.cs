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
public sealed class SafeReadTests
{
    [TestMethod]
    public void ReadSurfaceAllowsValuePattern()
    {
        var decision = ReadSurfacePolicy.Decide("Edit", valueSupported: true, textSupported: false);

        Assert.IsTrue(decision.Allowed);
        Assert.AreEqual("ValuePattern", decision.PatternUsed);
    }

    [TestMethod]
    public void ReadSurfaceAllowsTextPattern()
    {
        var decision = ReadSurfacePolicy.Decide("Document", valueSupported: false, textSupported: true);

        Assert.IsTrue(decision.Allowed);
        Assert.AreEqual("TextPattern", decision.PatternUsed);
    }

    [TestMethod]
    public void ReadSurfaceDeniesInvokeOnly()
    {
        var decision = ReadSurfacePolicy.Decide("Button", valueSupported: false, textSupported: false, invokeSupported: true);

        Assert.IsFalse(decision.Allowed);
        Assert.AreEqual(FailureKind.PolicyDenied, decision.FailureKind);
        Assert.AreEqual("", decision.PatternUsed);
    }

    [TestMethod]
    public void ReadSurfaceDeniesMutationPatterns()
    {
        var decision = ReadSurfacePolicy.Decide("CheckBox", valueSupported: true, textSupported: false, mutationPatternSupported: true);

        Assert.IsFalse(decision.Allowed);
        StringAssert.Contains(decision.Reason, "mutation");
    }

    [TestMethod]
    public void ReadSurfaceFailClosedUnknownRole()
    {
        var decision = ReadSurfacePolicy.Decide(null, valueSupported: false, textSupported: false);

        Assert.IsFalse(decision.Allowed);
    }

    [TestMethod]
    public void SafeReadVerifierRequiresSameIdentity()
    {
        var expected = CreateIdentity("42.1.9");
        var observed = CreateIdentity("42.1.10");
        var result = new SafeReadStepVerifier().Verify(
            CreateReadContract(expected),
            new PatternReadResult(
                Success: true,
                FailureKind: null,
                Reasons: ["read ok"],
                Value: "value",
                PatternUsed: "ValuePattern",
                ObservedIdentity: observed,
                WindowFound: true,
                TargetVisible: true,
                MutationObserved: false));

        Assert.IsFalse(result.Success);
    }

    [TestMethod]
    public void SafeReadVerifierPassesForSameIdentityWithoutMutation()
    {
        var expected = CreateIdentity("42.1.9");
        var result = new SafeReadStepVerifier().Verify(
            CreateReadContract(expected),
            new PatternReadResult(
                Success: true,
                FailureKind: null,
                Reasons: ["read ok"],
                Value: "value",
                PatternUsed: "ValuePattern",
                ObservedIdentity: expected,
                WindowFound: true,
                TargetVisible: true,
                MutationObserved: false));

        Assert.IsTrue(result.Success);
        Assert.AreEqual("Same", result.MatchVerdict);
    }

    private static RecipeSafetyContract CreateReadContract(ElementIdentity identity)
    {
        var selector = SelectorEngine.GenerateSelector(identity);
        return new RecipeSafetyContract(
            SchemaVersion: 1,
            ContractId: "safe-read-test",
            ActionKind: "read",
            ExpectedIdentity: identity,
            Selector: selector,
            WindowConstraints: new ExecutionWindowConstraints(false, true),
            Reversible: true,
            MaxActions: 1,
            ActionCeiling: ActionCeiling.ReadOnly,
            Provenance: Provenance.Uia,
            TrustLevel: TrustLevel.ProfileVerified,
            ApprovalRef: new ApprovalBinding(
                "approval-read",
                ElementFingerprintBuilder.Build(identity),
                selector,
                "read",
                "controlled",
                ApprovalManifestBuilder.PolicyVersion,
                "evidence"));
    }

    private static ElementIdentity CreateIdentity(string runtimeId)
    {
        return new ElementIdentity(runtimeId, "Edit", "Account number", "account-number")
        {
            Role = "Edit",
            ControlType = "Edit",
            ClassName = "TextBox",
            AncestorPath = "Window:ONE Brain > Pane:Form",
            Provenance = Provenance.Uia
        };
    }
}
