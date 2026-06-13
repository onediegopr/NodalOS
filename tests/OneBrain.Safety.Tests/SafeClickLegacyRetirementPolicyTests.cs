using Microsoft.VisualStudio.TestTools.UnitTesting;
using OneBrain.Core.Execution;

namespace OneBrain.Safety.Tests;

[TestClass]
public sealed class SafeClickLegacyRetirementPolicyTests
{
    [TestMethod]
    public void LegacyDispatchIsRejected()
    {
        var policy = SafeClickLegacyRetirementPolicyEvaluator.Evaluate("legacy", ineligibleAfterRetirement: false);

        Assert.IsTrue(policy.Enabled);
        Assert.IsTrue(policy.Retired);
        Assert.IsTrue(policy.Blocked);
        Assert.IsTrue(policy.LegacyDispatchRejected);
        Assert.AreEqual("LegacyDispatchRetired", policy.Reason);
        Assert.AreEqual(SafeClickLegacyRetirementPolicy.SafeExecutorRequiredAction, policy.RequiredAction);
    }

    [TestMethod]
    public void MissingDispatchBlocksSafeClickLegacyRetired()
    {
        var policy = SafeClickLegacyRetirementPolicyEvaluator.Evaluate("", ineligibleAfterRetirement: true);

        Assert.IsTrue(policy.Blocked);
        Assert.IsTrue(policy.IneligibleAfterRetirement);
        Assert.IsFalse(policy.LegacyDispatchRejected);
        Assert.AreEqual("SafeClickLegacyRetired", policy.Reason);
    }
}
