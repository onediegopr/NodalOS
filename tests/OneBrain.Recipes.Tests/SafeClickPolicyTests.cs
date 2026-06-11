using Microsoft.VisualStudio.TestTools.UnitTesting;
using OneBrain.Core.Safety;

namespace OneBrain.Recipes.Tests;

[TestClass]
public sealed class SafeClickPolicyTests
{
    [TestMethod]
    public void Controlled_Mode_Allows_Execution_For_SafeReadonly()
    {
        var pr = ClickPreflightEvaluator.Evaluate("Siete");
        var m = ApprovalManifestBuilder.Build(pr, "controlled");
        Assert.AreEqual("allowedForFuture", pr.Decision);
        Assert.IsTrue(m.ExecutionAllowedInThisHito);
    }

    [TestMethod]
    public void NonCommercialWeb_Allows_Execution_For_Nav()
    {
        var pr = ClickPreflightEvaluator.Evaluate("More information...");
        var m = ApprovalManifestBuilder.Build(pr, "nonCommercialWeb");
        Assert.AreEqual("requiresApproval", pr.Decision);
        Assert.IsTrue(m.ExecutionAllowedInThisHito);
    }

    [TestMethod]
    public void ControlledWeb_Allows_Execution_For_Nav()
    {
        var pr = ClickPreflightEvaluator.Evaluate("Ver descripción");
        var m = ApprovalManifestBuilder.Build(pr, "controlled");
        Assert.AreEqual("requiresApproval", pr.Decision);
        Assert.IsTrue(m.ExecutionAllowedInThisHito);
    }

    [TestMethod]
    public void CommercialWeb_Never_Allows_Execution()
    {
        var pr = ClickPreflightEvaluator.Evaluate("Ver descripción");
        var m = ApprovalManifestBuilder.Build(pr, "commercialWeb");
        Assert.IsFalse(m.ExecutionAllowedInThisHito);
    }

    [TestMethod]
    public void RequiresReview_Never_Allows_Execution()
    {
        var pr = ClickPreflightEvaluator.Evaluate("unknown target");
        Assert.IsTrue(pr.RequiresReview);
        Assert.IsFalse(ApprovalManifestBuilder.Build(pr, "controlled").ExecutionAllowedInThisHito);
        Assert.IsFalse(ApprovalManifestBuilder.Build(pr, "nonCommercialWeb").ExecutionAllowedInThisHito);
    }

    [TestMethod]
    public void Comprar_Siempre_Blocked_Controlled()
    {
        var pr = ClickPreflightEvaluator.Evaluate("Comprar ahora");
        Assert.IsTrue(pr.Blocked);
        var m = ApprovalManifestBuilder.Build(pr, "controlled");
        Assert.IsFalse(m.ExecutionAllowedInThisHito);
    }

    [TestMethod]
    public void Pagar_Siempre_Blocked_NonCommercialWeb()
    {
        var pr = ClickPreflightEvaluator.Evaluate("Pagar con Mercado Pago");
        Assert.IsTrue(pr.Blocked);
        var m = ApprovalManifestBuilder.Build(pr, "nonCommercialWeb");
        Assert.IsFalse(m.ExecutionAllowedInThisHito);
    }

    [TestMethod]
    public void Iniciar_Sesion_Siempre_Blocked()
    {
        var pr = ClickPreflightEvaluator.Evaluate("Iniciar sesión");
        Assert.IsTrue(pr.Blocked);
        var m = ApprovalManifestBuilder.Build(pr, "controlled");
        Assert.IsFalse(m.ExecutionAllowedInThisHito);
    }

    [TestMethod]
    public void CommercialWeb_Blocks_Even_Nav_Target()
    {
        var pr = ClickPreflightEvaluator.Evaluate("More information...");
        Assert.IsFalse(pr.Blocked);
        var m = ApprovalManifestBuilder.Build(pr, "commercialWeb");
        Assert.IsFalse(m.ExecutionAllowedInThisHito);
    }

    [TestMethod]
    public void CommercialWeb_Blocks_Even_SafeReadonly_Target()
    {
        var pr = ClickPreflightEvaluator.Evaluate("Siete");
        Assert.IsFalse(pr.Blocked);
        var m = ApprovalManifestBuilder.Build(pr, "commercialWeb");
        Assert.IsFalse(m.ExecutionAllowedInThisHito);
    }

    [TestMethod]
    public void NonCommercialWeb_Blocks_Dangerous_Target()
    {
        var pr = ClickPreflightEvaluator.Evaluate("Agregar al carrito");
        Assert.IsTrue(pr.Blocked);
    }

    [TestMethod]
    public void Target_Login_Siempre_Blocked_All_Modes()
    {
        var pr = ClickPreflightEvaluator.Evaluate("Iniciar sesión");
        Assert.IsTrue(pr.Blocked);

        var m1 = ApprovalManifestBuilder.Build(pr, "controlled");
        var m2 = ApprovalManifestBuilder.Build(pr, "nonCommercialWeb");
        var m3 = ApprovalManifestBuilder.Build(pr, "commercialWeb");

        Assert.IsFalse(m1.ExecutionAllowedInThisHito);
        Assert.IsFalse(m2.ExecutionAllowedInThisHito);
        Assert.IsFalse(m3.ExecutionAllowedInThisHito);
    }
}
