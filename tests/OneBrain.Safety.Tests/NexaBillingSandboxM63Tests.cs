using Microsoft.VisualStudio.TestTools.UnitTesting;
using OneBrain.BrowserExecutor.Cdp;
using OneBrain.BrowserExecutor.Contracts;

namespace OneBrain.Safety.Tests;

[TestClass]
public sealed class NexaBillingSandboxM63Tests
{
    [TestMethod]
    public void NexaBillingRealProviderDisabledByDefault()
    {
        var result = Provider().CreateCheckout(new NexaPaymentProviderConfig(NexaPaymentProviderKind.RealProviderFuture, true, RealBillingEnabled: false, SandboxAllowed: false, StoresPaymentCardData: false), Checkout());

        Assert.AreEqual(NexaPaymentDecisionKind.Blocked, result.Decision);
        Assert.IsFalse(result.RealChargeCreated);
    }

    [TestMethod]
    public void NexaBillingSandboxDoesNotChargeRealMoney()
    {
        var provider = Provider();
        provider.CreateCheckout(SandboxConfig(), Checkout());

        Assert.IsFalse(provider.Ledger.RealMoneyCharged);
    }

    [TestMethod]
    public void NexaBillingSandboxCreatesCheckoutSession()
    {
        var result = Provider().CreateCheckout(SandboxConfig(), Checkout());

        Assert.AreEqual(NexaPaymentDecisionKind.SandboxCreated, result.Decision);
        Assert.IsFalse(result.RealChargeCreated);
    }

    [TestMethod]
    public void NexaBillingSandboxCreatesPaymentIntentWithoutRealCharge()
    {
        var result = Provider().CreatePaymentIntent(SandboxConfig(), new NexaPaymentIntentRequest("intent-request", "account-local", 12m, "USD", ContainsPaymentCardData: false));

        Assert.AreEqual(NexaPaymentDecisionKind.SandboxCreated, result.Decision);
        Assert.IsFalse(result.RealChargeCreated);
    }

    [TestMethod]
    public void NexaBillingSandboxWebhookDoesNotEnableSensitiveFeatures()
    {
        var decision = Provider().HandleWebhook(new NexaBillingWebhookEvent("webhook-one", "invoice.paid", "account-local", SandboxOnly: true, ContainsPaymentCardData: false), [new NexaSubscriptionPlanBinding(NexaPlanKind.Pro, "price-sandbox", EnablesSensitiveRealPilot: false, EnablesProductiveVaultByDefault: false)]);

        Assert.IsFalse(decision.EnablesSensitiveFeatures);
        Assert.AreEqual(NexaPaymentDecisionKind.SandboxCreated, decision.Decision);
    }

    [TestMethod]
    public void NexaBillingSandboxDoesNotStorePaymentCardData()
    {
        var provider = Provider();
        provider.CreatePaymentIntent(SandboxConfig(), new NexaPaymentIntentRequest("intent-request", "account-local", 12m, "USD", ContainsPaymentCardData: false));

        Assert.IsFalse(provider.Ledger.StoresPaymentCardData);
    }

    [TestMethod]
    public void NexaBillingSandboxAuditIsRedacted()
    {
        var provider = Provider();
        provider.CreateCheckout(SandboxConfig(), Checkout());

        Assert.IsTrue(provider.Ledger.Events.All(audit => audit.Redacted));
    }

    [TestMethod]
    public void NexaBillingFailsClosedForUnknownProvider()
    {
        var result = Provider().CreateCheckout(new NexaPaymentProviderConfig(NexaPaymentProviderKind.Unknown, true, false, false, false), Checkout());

        Assert.AreEqual(NexaPaymentDecisionKind.Blocked, result.Decision);
    }

    [TestMethod]
    public void NexaBillingBlocksRealProviderWithoutFutureHito()
    {
        var result = Provider().CreatePaymentIntent(new NexaPaymentProviderConfig(NexaPaymentProviderKind.SandboxProvider, true, RealBillingEnabled: true, SandboxAllowed: true, StoresPaymentCardData: false), new NexaPaymentIntentRequest("intent-request", "account-local", 12m, "USD", ContainsPaymentCardData: false));

        Assert.AreEqual(NexaPaymentDecisionKind.Blocked, result.Decision);
        Assert.IsFalse(result.RealChargeCreated);
    }

    [TestMethod]
    public void NexaBillingDoesNotEnableProductiveVaultByDefault()
    {
        var decision = Provider().HandleWebhook(new NexaBillingWebhookEvent("webhook-one", "subscription.created", "account-local", SandboxOnly: true, ContainsPaymentCardData: false), [new NexaSubscriptionPlanBinding(NexaPlanKind.Enterprise, "price-sandbox", EnablesSensitiveRealPilot: false, EnablesProductiveVaultByDefault: false)]);

        Assert.IsFalse(decision.EnablesSensitiveFeatures);
    }

    [TestMethod]
    public void BrowserRuntimePhaseGatePassesWithApiEmailBillingSandbox()
    {
        var report = BrowserVaultMinimalM23Tests.GateReport(State());

        Assert.AreEqual(BrowserRuntimePhaseCloseStatus.Passed, report.Status);
        CollectionAssert.Contains(report.PassedChecks.ToList(), "api diagnostics email billing sandbox safe");
    }

    [TestMethod]
    public void BrowserRuntimePhaseGateFailsWhenRealBillingEnabled()
    {
        var report = BrowserVaultMinimalM23Tests.GateReport(State() with { RealBillingEnabled = true });

        Assert.AreEqual(BrowserRuntimePhaseCloseStatus.Failed, report.Status);
        CollectionAssert.Contains(report.FailedChecks.ToList(), "api diagnostics email billing sandbox safe");
    }

    private static NexaPaymentProvider Provider() => new();
    private static NexaPaymentProviderConfig SandboxConfig() => new(NexaPaymentProviderKind.SandboxProvider, PrivatePreviewLocal: true, RealBillingEnabled: false, SandboxAllowed: true, StoresPaymentCardData: false);
    private static NexaCheckoutSessionRequest Checkout() => new("checkout-request", "account-local", NexaPlanKind.Pro, 12m, "USD", ContainsPaymentCardData: false);

    private static BrowserRuntimeObservedState State() =>
        BrowserVaultThreatLifecycleM56M57Tests.StateForM60() with
        {
            PrivateLocalApiDiagnosticsDefined = true,
            PrivateLocalApiDiagnosticsLeaksSecretsCookiesBodies = false,
            EmailSandboxProviderDefined = true,
            RealEmailDeliveryEnabled = false,
            EmailTemplateLeaksSecrets = false,
            BillingSandboxProviderDefined = true,
            RealBillingEnabled = false,
            PaymentCardDataStored = false,
            BillingEnablesSensitiveFeaturesByDefault = false
        };
}
