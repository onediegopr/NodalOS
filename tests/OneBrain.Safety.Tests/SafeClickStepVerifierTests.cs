using Microsoft.VisualStudio.TestTools.UnitTesting;
using OneBrain.Core.Contracts;
using OneBrain.Core.Execution;
using OneBrain.Core.Models;
using OneBrain.Core.Safety;
using OneBrain.Core.Selectors;

namespace OneBrain.Safety.Tests;

[TestClass]
public sealed class SafeClickStepVerifierTests
{
    [TestMethod]
    public void VerifyReturnsSuccessForStrongSameObservedIdentity()
    {
        var expected = CreateIdentity("42.1.9");
        var contract = CreateContract(expected);
        var dispatch = new PatternExecutionResult(
            Success: true,
            FailureKind: null,
            Reasons: ["invoke ok"],
            ObservedIdentity: expected,
            WindowFound: true,
            TargetVisible: true,
            TargetName: expected.Name,
            ObservedActions: 1,
            Signals: ["postAction.windowFound=true", "postAction.targetVisible=true"]);

        var result = new SafeClickStepVerifier().Verify(contract, dispatch);

        Assert.IsTrue(result.Success);
        Assert.IsNull(result.FailureKind);
        Assert.AreEqual("Same", result.MatchVerdict);
    }

    [TestMethod]
    public void VerifyReturnsUnverifiedForObservedIdentityMismatch()
    {
        var expected = CreateIdentity("42.1.9");
        var observed = CreateIdentity("42.1.10");
        var contract = CreateContract(expected);
        var dispatch = new PatternExecutionResult(
            Success: true,
            FailureKind: null,
            Reasons: ["invoke ok"],
            ObservedIdentity: observed,
            WindowFound: true,
            TargetVisible: true,
            TargetName: observed.Name,
            ObservedActions: 1);

        var result = new SafeClickStepVerifier().Verify(contract, dispatch);

        Assert.IsFalse(result.Success);
        Assert.AreEqual(FailureKind.Unverified, result.FailureKind);
    }

    [TestMethod]
    public void VerifyReturnsUnverifiedWhenDispatchDoesNotReportObservedIdentity()
    {
        var expected = CreateIdentity("42.1.9");
        var contract = CreateContract(expected);
        var dispatch = new PatternExecutionResult(
            Success: true,
            FailureKind: null,
            Reasons: ["invoke ok"],
            ObservedIdentity: null,
            WindowFound: true,
            TargetVisible: true,
            TargetName: expected.Name,
            ObservedActions: 1);

        var result = new SafeClickStepVerifier().Verify(contract, dispatch);

        Assert.IsFalse(result.Success);
        Assert.AreEqual(FailureKind.Unverified, result.FailureKind);
    }

    [TestMethod]
    public void VerifyPropagatesInvokeTimeIdentityMismatchVerdict()
    {
        var expected = CreateIdentity("42.1.9");
        var observed = CreateIdentity("42.1.10");
        var contract = CreateContract(expected);
        var dispatch = new PatternExecutionResult(
            Success: false,
            FailureKind: FailureKind.Stale,
            Reasons: ["InvokeTimeIdentityMismatch"],
            ObservedIdentity: observed,
            InvokeTimeIdentityChecked: true,
            InvokeTimeIdentityVerdict: "Different",
            InvokeTimeIdentityReason: "InvokeTimeIdentityMismatch");

        var result = new SafeClickStepVerifier().Verify(contract, dispatch);

        Assert.IsFalse(result.Success);
        Assert.AreEqual(FailureKind.Stale, result.FailureKind);
        Assert.AreEqual("Different", result.MatchVerdict);
        CollectionAssert.Contains(result.Reasons.ToList(), "InvokeTimeIdentityMismatch");
    }

    private static RecipeSafetyContract CreateContract(ElementIdentity expected)
    {
        var selector = new SelectorDefinition
        {
            ExpectedIdentity = expected
        };

        return new RecipeSafetyContract(
            SchemaVersion: 1,
            ContractId: "safe-click-test",
            ActionKind: "click",
            ExpectedIdentity: expected,
            Selector: selector,
            WindowConstraints: new ExecutionWindowConstraints(false, true),
            Reversible: false,
            MaxActions: 1,
            ActionCeiling: ActionCeiling.FullActionWithPreflight,
            Provenance: Provenance.Uia,
            TrustLevel: TrustLevel.ProfileVerified,
            ApprovalRef: null);
    }

    private static ElementIdentity CreateIdentity(string runtimeId)
    {
        return new ElementIdentity(runtimeId, "Hyperlink", "More information...", "more-information-link")
        {
            Role = "Hyperlink",
            ControlType = "Hyperlink",
            ClassName = "Chrome_RenderWidgetHostHWND",
            AncestorPath = "Window:ONE Brain > Pane:Catalog > Document:Main",
            Provenance = Provenance.Uia
        };
    }
}
