using Microsoft.VisualStudio.TestTools.UnitTesting;
using OneBrain.Core.Safety;

namespace OneBrain.Recipes.Tests;

[TestClass]
public sealed class SafeClickPolicyTests
{
    [TestMethod]
    public void Controlled_Mode_Allows_Execution_For_SafeReadonly()
    {
        var pr = ClickPreflightEvaluator.Evaluate("Mostrar detalles");
        var m = ApprovalManifestBuilder.Build(pr, "controlled");
        Assert.IsTrue(m.ExecutionAllowedInThisHito);
    }

    [TestMethod]
    public void NonCommercialWeb_Allows_Execution_For_Nav()
    {
        var pr = ClickPreflightEvaluator.Evaluate("More information...");
        var m = ApprovalManifestBuilder.Build(pr, "nonCommercialWeb");
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
}
