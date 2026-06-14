using Microsoft.VisualStudio.TestTools.UnitTesting;
using OneBrain.Core.Approval;

namespace OneBrain.Safety.Tests;

[TestClass]
public sealed class ApprovedInputBindingTests
{
    [TestMethod]
    public void ApprovedInputBindingHashStable()
    {
        var first = ApprovedInputBindingHashBuilder.Build("type", "approval-1", "identity-1", "value-1");
        var second = ApprovedInputBindingHashBuilder.Build("type", "approval-1", "identity-1", "value-1");

        Assert.AreEqual(first.ApprovedInputBindingHash, second.ApprovedInputBindingHash);
        Assert.AreEqual("approved-input-v1", first.BindingVersion);
        Assert.AreEqual("SHA256", first.ApprovedValueDigestAlgorithm);
    }

    [TestMethod]
    public void ApprovedInputBindingHashChangesWhenTextDigestChanges()
    {
        var first = ApprovedInputBindingHashBuilder.Build("type", "approval-1", "identity-1", "value-1");
        var second = ApprovedInputBindingHashBuilder.Build("type", "approval-1", "identity-1", "value-2");

        Assert.AreNotEqual(first.ApprovedInputBindingHash, second.ApprovedInputBindingHash);
    }

    [TestMethod]
    public void ApprovedInputBindingHashChangesWhenIdentityBindingChanges()
    {
        var first = ApprovedInputBindingHashBuilder.Build("type", "approval-1", "identity-1", "value-1");
        var second = ApprovedInputBindingHashBuilder.Build("type", "approval-1", "identity-2", "value-1");

        Assert.AreNotEqual(first.ApprovedInputBindingHash, second.ApprovedInputBindingHash);
    }

    [TestMethod]
    public void ApprovedInputBindingHashChangesWhenActionKindChanges()
    {
        var first = ApprovedInputBindingHashBuilder.Build("type", "approval-1", "identity-1", "value-1");
        var second = ApprovedInputBindingHashBuilder.Build("read", "approval-1", "identity-1", "value-1");

        Assert.AreNotEqual(first.ApprovedInputBindingHash, second.ApprovedInputBindingHash);
    }

    [TestMethod]
    public void ApprovedInputBindingHashRequiresApprovalRef()
    {
        AssertArgumentException(() =>
            ApprovedInputBindingHashBuilder.Build("type", "", "identity-1", "value-1"));
    }

    [TestMethod]
    public void ApprovedInputBindingHashRequiresApprovedValueDigest()
    {
        AssertArgumentException(() =>
            ApprovedInputBindingHashBuilder.Build("type", "approval-1", "identity-1", ""));
    }

    [TestMethod]
    public void ApprovedInputBindingHashRequiresIdentityBinding()
    {
        AssertArgumentException(() =>
            ApprovedInputBindingHashBuilder.Build("type", "approval-1", "", "value-1"));
    }

    [TestMethod]
    public void ApprovedInputBindingValidatorAllowsMatchingBinding()
    {
        var binding = ApprovedInputBindingHashBuilder.Build("type", "approval-1", "identity-1", "value-1");
        var result = ApprovedInputBindingValidator.Validate(binding, "type", "approval-1", "identity-1", "value-1");

        Assert.IsTrue(result.Success, result.Reason);
    }

    [TestMethod]
    public void ApprovedInputBindingValidatorRejectsMismatchedTextDigest()
    {
        var binding = ApprovedInputBindingHashBuilder.Build("type", "approval-1", "identity-1", "value-1");
        var result = ApprovedInputBindingValidator.Validate(binding, "type", "approval-1", "identity-1", "value-2");

        Assert.IsFalse(result.Success);
        Assert.AreEqual("ApprovedInputDigestMismatch", result.Reason);
    }

    [TestMethod]
    public void ApprovedInputBindingValidatorRejectsMismatchedIdentityBinding()
    {
        var binding = ApprovedInputBindingHashBuilder.Build("type", "approval-1", "identity-1", "value-1");
        var result = ApprovedInputBindingValidator.Validate(binding, "type", "approval-1", "identity-2", "value-1");

        Assert.IsFalse(result.Success);
        Assert.AreEqual("ApprovedInputIdentityBindingMismatch", result.Reason);
    }

    [TestMethod]
    public void ApprovedInputBindingValidatorRejectsMissingBinding()
    {
        var result = ApprovedInputBindingValidator.Validate(null, "type", "approval-1", "identity-1", "value-1");

        Assert.IsFalse(result.Success);
        Assert.AreEqual("ApprovedInputBindingRequired", result.Reason);
    }

    private static void AssertArgumentException(Action action)
    {
        try
        {
            action();
        }
        catch (ArgumentException)
        {
            return;
        }

        Assert.Fail("Expected ArgumentException");
    }
}
