using Microsoft.VisualStudio.TestTools.UnitTesting;
using OneBrain.BrowserExecutor.Cdp;
using OneBrain.BrowserExecutor.Contracts;

namespace OneBrain.Safety.Tests;

[TestClass]
public sealed class BrowserRuntimeArchitectureM68Tests
{
    [TestMethod]
    public void BrowserRuntimeArchitectureDecisionKeepsChromeCdpPrimary()
    {
        var decision = Decision();

        Assert.AreEqual(BrowserRuntimeArchitectureRecommendation.KeepChromeCdpPrimary, decision.Recommendation);
        Assert.AreEqual(BrowserEmbeddedRuntimeKind.ChromeCdpExternal, decision.PrimaryRuntime);
        Assert.IsTrue(decision.ChromeCdpRemainsPrimary);
    }

    [TestMethod]
    public void BrowserRuntimeArchitectureDecisionDisablesEmbeddedProduction()
    {
        var decision = Decision();

        Assert.IsTrue(decision.EmbeddedRuntimeProductionDisabled);
        Assert.IsTrue(decision.Options.Where(option => option.Kind is BrowserEmbeddedRuntimeKind.WebView2Embedded or BrowserEmbeddedRuntimeKind.CefEmbedded).All(option => !option.ProductionAllowed));
    }

    [TestMethod]
    public void BrowserRuntimeArchitectureDecisionIncludesWebView2AndCefTradeoffs()
    {
        var decision = Decision();

        Assert.IsTrue(decision.Tradeoffs.Any(tradeoff => tradeoff.RuntimeKind == BrowserEmbeddedRuntimeKind.WebView2Embedded && tradeoff.Criterion == "Safety"));
        Assert.IsTrue(decision.Tradeoffs.Any(tradeoff => tradeoff.RuntimeKind == BrowserEmbeddedRuntimeKind.CefEmbedded && tradeoff.Criterion == "Packaging"));
    }

    [TestMethod]
    public void BrowserRuntimeArchitectureDecisionModelsAuthorityLeakRisk()
    {
        var decision = Decision();

        Assert.IsTrue(decision.Risks.Any(risk => risk.Description.Contains("authority", StringComparison.OrdinalIgnoreCase)));
        Assert.IsTrue(decision.Risks.All(risk => !string.IsNullOrWhiteSpace(risk.Mitigation)));
    }

    [TestMethod]
    public void BrowserRuntimeArchitectureDecisionIsRedacted()
    {
        var serialized = System.Text.Json.JsonSerializer.Serialize(Decision());
        var forbiddenValues = new[]
        {
            "synthetic-password-value",
            "synthetic-cookie-session-value",
            "synthetic-api-key-value",
            "opaque-token-value-123456789"
        };

        Assert.IsTrue(Decision().Redacted);
        Assert.IsFalse(forbiddenValues.Any(value => serialized.Contains(value, StringComparison.Ordinal)));
    }

    private static BrowserRuntimeArchitectureDecision Decision() =>
        new BrowserRuntimeArchitectureDecisionService().CreateDecision();
}
