using Microsoft.VisualStudio.TestTools.UnitTesting;
using OneBrain.Safety.Policies;

namespace OneBrain.Safety.Tests;

[TestClass]
public sealed class MinimalSafetyGuardTests
{
    private readonly MinimalSafetyGuard _guard = new();

    [TestMethod]
    public void Blocks_Close_AutomationId()
    {
        var decision = _guard.Evaluate("invoke", "Button", "Cerrar ventana", "Close", "ButtonClass");
        Assert.IsFalse(decision.Allowed);
    }

    [TestMethod]
    public void Blocks_Close_AutomationId_Only()
    {
        var decision = _guard.Evaluate("invoke", "Button", "Regular Button", "Close", "");
        Assert.IsFalse(decision.Allowed);
        StringAssert.Contains(decision.Reason, "Close");
    }

    [TestMethod]
    public void Blocks_CloseButton_AutomationId()
    {
        var decision = _guard.Evaluate("invoke", "Button", "X", "CloseButton", "");
        Assert.IsFalse(decision.Allowed);
    }

    [TestMethod]
    public void Blocks_Dangerous_Name_Cerrar()
    {
        var decision = _guard.Evaluate("invoke", "Button", "Cerrar pestaña", "", "");
        Assert.IsFalse(decision.Allowed);
        StringAssert.Contains(decision.Reason, "dangerous target name");
    }

    [TestMethod]
    public void Blocks_Dangerous_Name_Delete()
    {
        var decision = _guard.Evaluate("invoke", "Button", "Delete item", "", "");
        Assert.IsFalse(decision.Allowed);
    }

    [TestMethod]
    public void Blocks_Dangerous_Name_Pay()
    {
        var decision = _guard.Evaluate("invoke", "Button", "Pagar ahora", "", "");
        Assert.IsFalse(decision.Allowed);
    }

    [TestMethod]
    public void Blocks_Dangerous_Name_Checkout()
    {
        var decision = _guard.Evaluate("invoke", "Button", "Confirmar checkout", "", "");
        Assert.IsFalse(decision.Allowed);
    }

    [TestMethod]
    public void Allows_Safe_Name()
    {
        var decision = _guard.Evaluate("invoke", "Button", "Run ONE Brain Search", "", "");
        Assert.IsTrue(decision.Allowed);
    }

    [TestMethod]
    public void Blocks_RunAsAdministrator()
    {
        var decision = _guard.Evaluate("invoke", "Button", "Run as administrator", "", "");
        Assert.IsFalse(decision.Allowed);
    }

    [TestMethod]
    public void Blocks_RunScript()
    {
        var decision = _guard.Evaluate("invoke", "Button", "Run script", "", "");
        Assert.IsFalse(decision.Allowed);
    }

    [TestMethod]
    public void Blocks_Click_Kind()
    {
        var decision = _guard.Evaluate("click", "Button", "Cerrar", "Close", "");
        Assert.IsFalse(decision.Allowed);
    }

    [TestMethod]
    public void Blocks_Press_Kind()
    {
        var decision = _guard.Evaluate("press", "Button", "Cerrar", "Close", "");
        Assert.IsFalse(decision.Allowed);
    }

    [TestMethod]
    public void Allows_Type_Kind_On_Dangerous_Name()
    {
        var decision = _guard.Evaluate("type", "Edit", "Close", "", "");
        Assert.IsTrue(decision.Allowed);
    }

    [TestMethod]
    public void Allows_Focus_Kind()
    {
        var decision = _guard.Evaluate("focus", "Button", "Cerrar", "Close", "");
        Assert.IsTrue(decision.Allowed);
    }

    [TestMethod]
    public void Allows_Key_Kind()
    {
        var decision = _guard.Evaluate("key", "Window", "Cerrar", "Close", "");
        Assert.IsTrue(decision.Allowed);
    }

    [TestMethod]
    public void Unknown_Kind_Not_Dangerous()
    {
        var decision = _guard.Evaluate("foobar", "Button", "Cerrar", "Close", "");
        Assert.IsTrue(decision.Allowed);
    }
}
