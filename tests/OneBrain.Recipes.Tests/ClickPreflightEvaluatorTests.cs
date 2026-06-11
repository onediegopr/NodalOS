using Microsoft.VisualStudio.TestTools.UnitTesting;
using OneBrain.Core.Safety;

namespace OneBrain.Recipes.Tests;

[TestClass]
public sealed class ClickPreflightEvaluatorTests
{
    [TestMethod]
    public void Comprar_Ahora_Blocked()
    {
        var r = ClickPreflightEvaluator.Evaluate("Comprar ahora");
        Assert.AreEqual("blocked", r.Decision);
        Assert.IsTrue(r.Blocked);
    }

    [TestMethod]
    public void Pagar_Blocked_Payment_Critical()
    {
        var r = ClickPreflightEvaluator.Evaluate("Pagar con Mercado Pago");
        Assert.AreEqual("blocked", r.Decision);
        Assert.AreEqual("payment-related", r.RiskCategory);
        Assert.AreEqual("critical", r.RiskLevel);
    }

    [TestMethod]
    public void Tarjeta_De_Credito_Blocked_Payment()
    {
        var r = ClickPreflightEvaluator.Evaluate("tarjeta de crédito");
        Assert.AreEqual("blocked", r.Decision);
        Assert.AreEqual("payment-related", r.RiskCategory);
    }

    [TestMethod]
    public void Debito_Blocked_Payment()
    {
        var r = ClickPreflightEvaluator.Evaluate("débito");
        Assert.AreEqual("blocked", r.Decision);
        Assert.AreEqual("payment-related", r.RiskCategory);
    }

    [TestMethod]
    public void Iniciar_Sesion_Blocked_Auth()
    {
        var r = ClickPreflightEvaluator.Evaluate("iniciar sesión");
        Assert.AreEqual("blocked", r.Decision);
        Assert.AreEqual("auth-related", r.RiskCategory);
    }

    [TestMethod]
    public void Contrasena_Blocked_Auth()
    {
        var r = ClickPreflightEvaluator.Evaluate("contraseña");
        Assert.AreEqual("blocked", r.Decision);
        Assert.AreEqual("auth-related", r.RiskCategory);
    }

    [TestMethod]
    public void Ver_Descripcion_RequiresApproval()
    {
        var r = ClickPreflightEvaluator.Evaluate("Ver descripción");
        Assert.AreEqual("requiresApproval", r.Decision);
        Assert.IsTrue(r.RequiresApproval);
    }

    [TestMethod]
    public void Categorias_AllowedForFuture_But_Not_Blocked()
    {
        var r = ClickPreflightEvaluator.Evaluate("Categorías");
        Assert.AreEqual("allowedForFuture", r.Decision);
        Assert.IsFalse(r.Blocked);
    }

    [TestMethod]
    public void Unknown_Target_RequiresReview()
    {
        var r = ClickPreflightEvaluator.Evaluate("foo-bar-unknown-action");
        Assert.AreEqual("requiresReview", r.Decision);
        Assert.IsTrue(r.RequiresReview);
    }
}
