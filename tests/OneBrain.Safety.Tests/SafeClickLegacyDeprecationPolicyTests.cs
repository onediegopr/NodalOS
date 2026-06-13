using Microsoft.VisualStudio.TestTools.UnitTesting;
using OneBrain.Core.Execution;

namespace OneBrain.Safety.Tests;

[TestClass]
public sealed class SafeClickLegacyDeprecationPolicyTests
{
    [TestMethod]
    public void LegacyDispatchWithMetadataIsCompliantWarning()
    {
        var policy = SafeClickLegacyDeprecationPolicyEvaluator.Evaluate(
            isLegacyDispatch: true,
            owner: "migration",
            reason: "Target lacks InvokePattern",
            reviewBy: "2026-07-31");

        Assert.IsTrue(policy.IsDeprecated);
        Assert.IsTrue(policy.IsCompliant);
        Assert.AreEqual(SafeClickLegacyDeprecationSeverity.Warning, policy.DeprecationSeverity);
        Assert.AreEqual("", policy.ViolationReason);
    }

    [TestMethod]
    public void LegacyDispatchMissingMetadataIsNonCompliantButWarning()
    {
        var policy = SafeClickLegacyDeprecationPolicyEvaluator.Evaluate(
            isLegacyDispatch: true,
            owner: "",
            reason: "",
            reviewBy: "");

        Assert.IsFalse(policy.IsCompliant);
        Assert.AreEqual(SafeClickLegacyDeprecationSeverity.Warning, policy.DeprecationSeverity);
        StringAssert.Contains(policy.ViolationReason, "MissingOwner");
        StringAssert.Contains(policy.ViolationReason, "MissingReason");
        StringAssert.Contains(policy.ViolationReason, "MissingReviewBy");
    }

    [TestMethod]
    public void NonLegacyDispatchIsNotDeprecated()
    {
        var policy = SafeClickLegacyDeprecationPolicyEvaluator.Evaluate(false, null, null, null);

        Assert.IsFalse(policy.IsLegacyDispatch);
        Assert.IsFalse(policy.IsDeprecated);
        Assert.IsTrue(policy.IsCompliant);
        Assert.AreEqual(SafeClickLegacyDeprecationSeverity.None, policy.DeprecationSeverity);
    }
}
