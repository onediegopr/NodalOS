using Microsoft.VisualStudio.TestTools.UnitTesting;
using OneBrain.BrowserExecutor.Cdp;
using OneBrain.BrowserExecutor.Contracts;

namespace OneBrain.Safety.Tests;

[TestClass]
public sealed class NexaSyntheticExternalScenarioM81Tests
{
    [TestMethod]
    public void SyntheticExternalScenarioCatalogReadOnlyPagesAllowOnlyReadActions()
    {
        var readOnly = Catalog().Scenarios
            .Where(s => s.Kind is NexaSyntheticExternalScenarioKind.LandingReadOnly or NexaSyntheticExternalScenarioKind.ProductListReadOnly or NexaSyntheticExternalScenarioKind.DocumentReadOnly or NexaSyntheticExternalScenarioKind.TableReportReadOnly)
            .ToArray();

        Assert.AreEqual(4, readOnly.Length);
        Assert.IsTrue(readOnly.All(s => s.ExpectedAllowedActions.Any(action => action.StartsWith("read-", StringComparison.Ordinal))));
        Assert.IsTrue(readOnly.All(s => s.ExpectedDeniedActions.Count == 0));
        Assert.IsTrue(readOnly.All(s => !s.UsesRealContent));
    }

    [TestMethod]
    public void SyntheticExternalScenarioDisabledFormDoesNotAllowSubmit()
    {
        var scenario = Scenario(NexaSyntheticExternalScenarioKind.DisabledFormBlocked);

        CollectionAssert.Contains(scenario.ExpectedDeniedActions.ToList(), "submit");
        Assert.IsNotNull(scenario.ExpectedBlockerExplanation);
        Assert.AreEqual(NexaOperatorBlockerScenario.IrreversibleActionBlocked, scenario.ExpectedBlockerExplanation.Value);
    }

    [TestMethod]
    public void SyntheticExternalScenarioLoginFixtureBlocksCredentialFlow()
    {
        var scenario = Scenario(NexaSyntheticExternalScenarioKind.LoginBlocked);

        CollectionAssert.Contains(scenario.ExpectedDeniedActions.ToList(), "enter-credentials");
        Assert.IsNotNull(scenario.ExpectedBlockerExplanation);
        Assert.AreEqual(NexaOperatorBlockerScenario.RealCredentialsBlocked, scenario.ExpectedBlockerExplanation.Value);
    }

    [TestMethod]
    public void SyntheticExternalScenarioCheckoutPaymentBlocksPaymentSubmit()
    {
        var scenario = Scenario(NexaSyntheticExternalScenarioKind.CheckoutPaymentBlocked);

        CollectionAssert.Contains(scenario.ExpectedDeniedActions.ToList(), "pay");
        Assert.IsNotNull(scenario.ExpectedBlockerExplanation);
        Assert.AreEqual(NexaOperatorBlockerScenario.RealBillingBlocked, scenario.ExpectedBlockerExplanation.Value);
    }

    [TestMethod]
    public void SyntheticExternalScenarioDestructiveActionBlocksDeleteSignMutate()
    {
        var scenario = Scenario(NexaSyntheticExternalScenarioKind.DestructiveActionBlocked);

        CollectionAssert.Contains(scenario.ExpectedDeniedActions.ToList(), "delete");
        CollectionAssert.Contains(scenario.ExpectedDeniedActions.ToList(), "sign");
        CollectionAssert.Contains(scenario.ExpectedDeniedActions.ToList(), "mutate");
        Assert.IsNotNull(scenario.ExpectedBlockerExplanation);
        Assert.AreEqual(NexaOperatorBlockerScenario.IrreversibleActionBlocked, scenario.ExpectedBlockerExplanation.Value);
    }

    [TestMethod]
    public void SyntheticExternalScenarioBlockedCasesProduceOperatorExplanations()
    {
        var service = new NexaOperatorBlockerExplanationService();
        foreach (var scenario in Catalog().Scenarios.Where(s => s.ExpectedBlockerExplanation is not null))
        {
            var explanation = service.Explain(scenario.ExpectedBlockerExplanation!.Value, ["synthetic-scenario:redacted"]);

            Assert.IsFalse(string.IsNullOrWhiteSpace(explanation.Cause));
            Assert.IsTrue(explanation.SafeOptions.Count > 0);
            Assert.IsTrue(explanation.BlockedOptions.Count > 0);
            Assert.IsTrue(explanation.Redacted);
        }
    }

    [TestMethod]
    public void SyntheticExternalScenarioCatalogDoesNotUseInternetOrRealData()
    {
        var catalog = Catalog();

        Assert.IsFalse(catalog.UsesInternet);
        Assert.IsFalse(catalog.UsesRealCustomerData);
        Assert.IsTrue(catalog.Redacted);
    }

    private static NexaSyntheticExternalScenarioCatalog Catalog() => new NexaSyntheticExternalScenarioCatalogService().CreateDefault();

    private static NexaSyntheticExternalScenario Scenario(NexaSyntheticExternalScenarioKind kind) =>
        Catalog().Scenarios.Single(s => s.Kind == kind);
}
