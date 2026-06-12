using Microsoft.VisualStudio.TestTools.UnitTesting;
using OneBrain.Core.AppProfiles;
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

    [TestMethod]
    public void Siete_Is_Not_Global_Safe_Readonly()
    {
        var r = ClickPreflightEvaluator.Evaluate("Siete");
        Assert.AreEqual("requiresReview", r.Decision);
        Assert.IsTrue(r.RequiresReview);
    }

    [TestMethod]
    public void Mercado_Pago_Is_Not_Global_Generic_Policy_Term()
    {
        var r = ClickPreflightEvaluator.Evaluate("Mercado Pago");
        Assert.AreEqual("requiresReview", r.Decision);
        Assert.IsFalse(r.Blocked);
    }

    [TestMethod]
    public void Sensitive_Actions_Remain_Blocked()
    {
        foreach (var text in new[] { "comprar", "pagar", "enviar", "login", "aceptar cookies", "checkout", "delete" })
        {
            var r = ClickPreflightEvaluator.Evaluate(text);
            Assert.AreEqual("blocked", r.Decision, text);
            Assert.IsTrue(r.Blocked, text);
        }
    }

    [TestMethod]
    public void AppProfile_Can_Extend_Readonly_Without_Global_Hardcode()
    {
        var profile = new AppProfile(
            Id: "controlled-fixture",
            Name: "Controlled fixture",
            Kind: AppProfileKinds.Fixture,
            Status: AppProfileStatuses.Active,
            AppName: null,
            ProcessName: null,
            SiteDomain: "example.invalid",
            SupportedCapabilities: [AppProfileCapabilities.ReadOnly],
            RiskPolicy: new AppProfileRiskPolicy(
                ReadOnlyByDefault: true,
                DiagnosticAllowed: true,
                RequiresApprovalForSubmit: true,
                BlocksLogin: true,
                BlocksCookies: true,
                BlocksPayment: true,
                BlocksPurchase: true,
                AllowsSafeClick: false),
            SelectorAliases: [new AppProfileSelectorAlias("Siete", "text", "Siete", "Profile-specific readonly alias.")],
            LastVerifiedAtUtc: "2026-06-12T00:00:00Z",
            Version: new AppProfileVersion(1, "2026-06-12T00:00:00Z", "test", AppProfileStatuses.Active),
            Notes: []);

        var r = ClickPreflightEvaluator.Evaluate("Siete", ClickPreflightPolicy.ForAppProfile(profile));
        Assert.AreEqual("allowedForFuture", r.Decision);
        Assert.IsTrue(r.Allowed);
    }

    [TestMethod]
    public void ExternalFragile_Profile_Without_Diagnostic_Does_Not_Extend_Readonly()
    {
        var profile = new AppProfile(
            Id: "external-fragile",
            Name: "External fragile",
            Kind: AppProfileKinds.BrowserSite,
            Status: AppProfileStatuses.ExternalFragile,
            AppName: null,
            ProcessName: null,
            SiteDomain: "example.invalid",
            SupportedCapabilities: [AppProfileCapabilities.ReadOnly, AppProfileCapabilities.ExternalFragile],
            RiskPolicy: new AppProfileRiskPolicy(
                ReadOnlyByDefault: true,
                DiagnosticAllowed: false,
                RequiresApprovalForSubmit: true,
                BlocksLogin: true,
                BlocksCookies: true,
                BlocksPayment: true,
                BlocksPurchase: true,
                AllowsSafeClick: false),
            SelectorAliases: [new AppProfileSelectorAlias("Siete", "text", "Siete", "Should not extend without diagnostic.")],
            LastVerifiedAtUtc: "2026-06-12T00:00:00Z",
            Version: new AppProfileVersion(1, "2026-06-12T00:00:00Z", "test", AppProfileStatuses.ExternalFragile),
            Notes: []);

        var r = ClickPreflightEvaluator.Evaluate("Siete", ClickPreflightPolicy.ForAppProfile(profile));
        Assert.AreEqual("requiresReview", r.Decision);
        Assert.IsTrue(r.RequiresReview);
    }
}
