using Microsoft.VisualStudio.TestTools.UnitTesting;
using OneBrain.Core.Extraction;

namespace OneBrain.Recipes.Tests;

[TestClass]
public sealed class ActionDiscoveryTests
{
    [TestMethod]
    public void Detects_Dangerous_Commercial()
    {
        var r = CommercialActionDiscovery.Discover("Comprar ahora Notebook | Agregar al carrito");
        Assert.IsTrue(r.DangerousCount >= 2);
        Assert.IsTrue(r.HasDangerous);
    }

    [TestMethod]
    public void Detects_Payment_Related()
    {
        var r = CommercialActionDiscovery.Discover("Pagar con Mercado Pago | Tarjeta de crédito | Cuotas");
        Assert.IsTrue(r.PaymentCount >= 1);
        Assert.IsTrue(r.HasPayment);
        Assert.AreEqual("payment", r.HighestRisk);
    }

    [TestMethod]
    public void Detects_Auth_Related()
    {
        var r = CommercialActionDiscovery.Discover("Iniciar sesión | Crear cuenta | Ingresar");
        Assert.IsTrue(r.AuthCount >= 2);
        Assert.IsTrue(r.HasAuth);
    }

    [TestMethod]
    public void Detects_Navigation_Candidate()
    {
        var r = CommercialActionDiscovery.Discover("Ver más | Descripción | Opiniones | Preguntas");
        Assert.IsTrue(r.NavCount >= 3);
    }

    [TestMethod]
    public void Highest_Severity_Wins()
    {
        var r = CommercialActionDiscovery.Discover("Pagar ahora carrito");
        Assert.AreEqual("payment", r.HighestRisk);
    }

    [TestMethod]
    public void Empty_Text_Returns_Zero()
    {
        var r = CommercialActionDiscovery.Discover("");
        Assert.AreEqual(0, r.Count);
    }
}
