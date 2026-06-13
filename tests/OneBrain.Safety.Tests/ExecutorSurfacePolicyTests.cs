using Microsoft.VisualStudio.TestTools.UnitTesting;
using OneBrain.Core.Contracts;
using OneBrain.Core.Execution;

namespace OneBrain.Safety.Tests;

[TestClass]
public sealed class ExecutorSurfacePolicyTests
{
    [TestMethod]
    public void ButtonWithInvokeAllowed()
    {
        AssertAllowed("Button");
    }

    [TestMethod]
    public void HyperlinkWithInvokeAllowed()
    {
        AssertAllowed("Hyperlink");
    }

    [TestMethod]
    public void MenuItemWithInvokeAllowed()
    {
        AssertAllowed("MenuItem");
    }

    [TestMethod]
    public void ButtonWithoutInvokeDenied()
    {
        AssertDenied("Button", "role allowlisted but does not support InvokePattern");
    }

    [TestMethod]
    public void HyperlinkWithoutInvokeDenied()
    {
        AssertDenied("Hyperlink", "role allowlisted but does not support InvokePattern");
    }

    [TestMethod]
    public void MenuItemWithoutInvokeDenied()
    {
        AssertDenied("MenuItem", "role allowlisted but does not support InvokePattern");
    }

    [TestMethod]
    public void CheckBoxDenied()
    {
        AssertDenied("CheckBox", "role not in executor surface allowlist");
    }

    [TestMethod]
    public void RadioButtonDenied()
    {
        AssertDenied("RadioButton", "role not in executor surface allowlist");
    }

    [TestMethod]
    public void ComboBoxDenied()
    {
        AssertDenied("ComboBox", "role not in executor surface allowlist");
    }

    [TestMethod]
    public void EditDenied()
    {
        AssertDenied("Edit", "role not in executor surface allowlist");
    }

    [TestMethod]
    public void DocumentDenied()
    {
        AssertDenied("Document", "role not in executor surface allowlist");
    }

    [TestMethod]
    public void WindowDenied()
    {
        AssertDenied("Window", "role not in executor surface allowlist");
    }

    [TestMethod]
    public void PaneDenied()
    {
        AssertDenied("Pane", "role not in executor surface allowlist");
    }

    [TestMethod]
    public void CustomDenied()
    {
        AssertDenied("Custom", "role not in executor surface allowlist");
    }

    [TestMethod]
    public void SplitButtonDeniedFuture()
    {
        AssertDenied("SplitButton", "role not in executor surface allowlist");
    }

    [TestMethod]
    public void ListItemDeniedFuture()
    {
        AssertDenied("ListItem", "role not in executor surface allowlist");
    }

    [TestMethod]
    public void TabItemDeniedFuture()
    {
        AssertDenied("TabItem", "role not in executor surface allowlist");
    }

    [TestMethod]
    public void EmptyRoleDeniedFailClosed()
    {
        AssertDenied("", "empty role denied fail-closed");
        AssertDenied("   ", "empty role denied fail-closed");
        AssertDenied(null, "empty role denied fail-closed");
    }

    [TestMethod]
    public void UnknownRoleDeniedFailClosed()
    {
        AssertDenied("UnknownFutureRole", "role not in executor surface allowlist");
    }

    [TestMethod]
    public void RoleMatchingIsCaseInsensitive()
    {
        AssertAllowed("button");
        AssertAllowed(" HYPERLINK ");
        AssertAllowed("menuitem");
    }

    private static void AssertAllowed(string role)
    {
        var decision = ExecutorSurfacePolicy.Decide(role, invokeSupported: true);

        Assert.IsTrue(decision.Allowed);
        Assert.IsNull(decision.FailureKind);
        Assert.AreEqual("executor surface allowlisted", decision.Reason);
        Assert.AreEqual("InvokePattern", decision.RequiredPattern);
    }

    private static void AssertDenied(string? role, string reason)
    {
        var decision = ExecutorSurfacePolicy.Decide(role, invokeSupported: false);

        Assert.IsFalse(decision.Allowed);
        Assert.AreEqual(FailureKind.PolicyDenied, decision.FailureKind);
        Assert.AreEqual(reason, decision.Reason);
        Assert.AreEqual("InvokePattern", decision.RequiredPattern);
    }
}
