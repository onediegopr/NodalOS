using Microsoft.VisualStudio.TestTools.UnitTesting;
using OneBrain.Core.Extraction;

namespace OneBrain.Recipes.Tests;

[TestClass]
public sealed class SafeNavigationPlannerTests
{
    [TestMethod]
    public void Payment_Always_Blocked()
    {
        var items = new List<ActionItem> { new() { Text = "Pagar", Severity = "payment", Category = "payment" } };
        var plan = SafeNavigationPlanner.Plan(items);
        Assert.AreEqual(1, plan.BlockedCount);
        Assert.IsFalse(plan.HasExecutableActions);
    }

    [TestMethod]
    public void Dangerous_Always_Blocked()
    {
        var items = new List<ActionItem> { new() { Text = "Comprar ahora", Severity = "dangerous", Category = "dangerous" } };
        var plan = SafeNavigationPlanner.Plan(items);
        Assert.AreEqual(1, plan.BlockedCount);
    }

    [TestMethod]
    public void Auth_Always_Blocked()
    {
        var items = new List<ActionItem> { new() { Text = "Iniciar sesión", Severity = "auth", Category = "auth" } };
        var plan = SafeNavigationPlanner.Plan(items);
        Assert.AreEqual(1, plan.BlockedCount);
    }

    [TestMethod]
    public void Nav_Requires_Approval_Not_Executable()
    {
        var items = new List<ActionItem> { new() { Text = "Ver más", Severity = "nav", Category = "nav" } };
        var plan = SafeNavigationPlanner.Plan(items);
        Assert.AreEqual(1, plan.RequiresApprovalCount);
        Assert.AreEqual(0, plan.BlockedCount);
        Assert.IsFalse(plan.HasExecutableActions);
    }

    [TestMethod]
    public void HasExecutableActions_Always_False()
    {
        var items = new List<ActionItem>
        {
            new() { Text = "Categorías", Severity = "safe", Category = "safe" },
            new() { Text = "Ver más", Severity = "nav", Category = "nav" }
        };
        var plan = SafeNavigationPlanner.Plan(items);
        Assert.IsFalse(plan.HasExecutableActions);
    }

    [TestMethod]
    public void Null_Items_No_Exception()
    {
        var plan = SafeNavigationPlanner.Plan(null);
        Assert.AreEqual(0, plan.AllowedCount + plan.BlockedCount + plan.RequiresApprovalCount);
    }
}
