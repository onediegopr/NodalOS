using Microsoft.VisualStudio.TestTools.UnitTesting;
using OneBrain.Core.Execution;

namespace OneBrain.Safety.Tests;

[TestClass]
public sealed class TypeSurfacePolicyTests
{
    [TestMethod]
    public void TypeSurfaceAllowsEditWithValuePattern()
    {
        var decision = TypeSurfacePolicy.Decide("Edit", valueSupported: true);

        Assert.IsTrue(decision.Allowed);
        Assert.AreEqual("ValuePattern.SetValue", decision.PatternUsed);
    }

    [TestMethod]
    public void TypeSurfaceAllowsDocumentWithValuePatternIfExplicitlyAllowed()
    {
        var decision = TypeSurfacePolicy.Decide("Document", valueSupported: true);

        Assert.IsTrue(decision.Allowed);
        Assert.AreEqual("Document", decision.Role);
    }

    [TestMethod]
    public void TypeSurfaceDeniesNoValuePattern()
    {
        var decision = TypeSurfacePolicy.Decide("Edit", valueSupported: false);

        Assert.IsFalse(decision.Allowed);
        Assert.AreEqual("ValuePattern required for safe.type", decision.Reason);
    }

    [TestMethod]
    public void TypeSurfaceDeniesInvokeOnly()
    {
        var decision = TypeSurfacePolicy.Decide("Edit", valueSupported: true, invokeSupported: true);

        Assert.IsFalse(decision.Allowed);
        Assert.AreEqual("InvokePattern is not allowed for safe.type", decision.Reason);
    }

    public void TypeSurfaceDeniesNonTypeRoles()
    {
        foreach (var deniedRole in new[] { "Button", "Hyperlink", "MenuItem", "Custom", "" })
        {
            var decision = TypeSurfacePolicy.Decide(deniedRole, valueSupported: true);

            Assert.IsFalse(decision.Allowed, deniedRole);
        }
    }

    [TestMethod]
    public void TypeSurfaceDeniesPasswordField()
    {
        var decision = TypeSurfacePolicy.Decide("Edit", valueSupported: true, isPassword: true);

        Assert.IsFalse(decision.Allowed);
        Assert.IsTrue(decision.IsPasswordBlocked);
        Assert.AreEqual("password fields are blocked for safe.type", decision.Reason);
    }

    [TestMethod]
    public void TypeSurfaceDeniesMutationPatterns()
    {
        Assert.IsFalse(TypeSurfacePolicy.Decide("Edit", valueSupported: true, toggleSupported: true).Allowed);
        Assert.IsFalse(TypeSurfacePolicy.Decide("Edit", valueSupported: true, selectionItemSupported: true).Allowed);
        Assert.IsFalse(TypeSurfacePolicy.Decide("Edit", valueSupported: true, expandCollapseSupported: true).Allowed);
        Assert.IsFalse(TypeSurfacePolicy.Decide("Edit", valueSupported: true, scrollSupported: true).Allowed);
    }

    [TestMethod]
    public void TypeSurfaceDoesNotUseReadOrClickSurfacePolicy()
    {
        Assert.AreNotSame(typeof(TypeSurfacePolicy), typeof(ReadSurfacePolicy));
        Assert.AreNotSame(typeof(TypeSurfacePolicy), typeof(ExecutorSurfacePolicy));
    }
}
