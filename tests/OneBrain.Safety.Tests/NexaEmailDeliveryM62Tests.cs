using Microsoft.VisualStudio.TestTools.UnitTesting;
using OneBrain.BrowserExecutor.Cdp;
using OneBrain.BrowserExecutor.Contracts;

namespace OneBrain.Safety.Tests;

[TestClass]
public sealed class NexaEmailDeliveryM62Tests
{
    [TestMethod]
    public void NexaEmailDeliveryRealProviderDisabledByDefault()
    {
        var result = Provider().Deliver(new NexaEmailProviderConfig(NexaEmailProviderKind.RealProviderFuture, true, RealEmailDeliveryEnabled: false, SandboxAllowed: false), Request());

        Assert.AreEqual(NexaEmailDeliveryDecision.DesignOnly, result.Decision);
        Assert.IsFalse(result.RealEmailSent);
    }

    [TestMethod]
    public void NexaEmailDeliverySandboxProviderDoesNotSendRealEmail()
    {
        var result = Provider().Deliver(SandboxConfig(), Request());

        Assert.AreEqual(NexaEmailDeliveryDecision.QueuedSandbox, result.Decision);
        Assert.IsFalse(result.RealEmailSent);
    }

    [TestMethod]
    public void NexaEmailTemplateRendersFreeLicenseRequested()
    {
        var render = Provider().Render(NexaEmailTemplateKind.FreeLicenseRequested, new Dictionary<string, string> { ["account"] = "account-local" });

        Assert.AreEqual(NexaEmailTemplateKind.FreeLicenseRequested, render.Template);
        Assert.IsTrue(render.Redacted);
    }

    [TestMethod]
    public void NexaEmailTemplateDoesNotRenderSecrets()
    {
        var result = Provider().Deliver(SandboxConfig(), Request(parameters: new Dictionary<string, string> { ["account"] = "token=synthetic-api-key-value" }));

        Assert.AreNotEqual(NexaEmailDeliveryDecision.QueuedSandbox, result.Decision);
        Assert.IsFalse(result.RealEmailSent);
    }

    [TestMethod]
    public void NexaEmailTemplateDoesNotRenderCookies()
    {
        var result = Provider().Deliver(SandboxConfig(), Request(containsCookie: true));

        Assert.AreEqual(NexaEmailDeliveryDecision.Blocked, result.Decision);
    }

    [TestMethod]
    public void NexaEmailOutboxStoresDraftOnly()
    {
        var provider = Provider();
        provider.Deliver(SandboxConfig(), Request());

        Assert.AreEqual(1, provider.Snapshot().Messages.Count);
        Assert.IsFalse(provider.Snapshot().SendsRealEmail);
    }

    [TestMethod]
    public void NexaEmailDeliveryAuditIsRedacted()
    {
        var result = Provider().Deliver(SandboxConfig(), Request());

        Assert.IsTrue(result.AuditEvent.Redacted);
        Assert.IsFalse(BrowserCredentialRedactor.ContainsSecret(NexaLeakHardeningSerialization.ToSafeJson(result.AuditEvent)));
    }

    [TestMethod]
    public void NexaEmailDeliveryFailsClosedForUnknownProvider()
    {
        var result = Provider().Deliver(new NexaEmailProviderConfig(NexaEmailProviderKind.Unknown, true, false, false), Request());

        Assert.AreEqual(NexaEmailDeliveryDecision.FailClosed, result.Decision);
    }

    [TestMethod]
    public void NexaEmailDeliveryBlocksRealProviderWithoutFutureHito()
    {
        var result = Provider().Deliver(new NexaEmailProviderConfig(NexaEmailProviderKind.SandboxProvider, true, RealEmailDeliveryEnabled: true, SandboxAllowed: true), Request());

        Assert.AreEqual(NexaEmailDeliveryDecision.DesignOnly, result.Decision);
        Assert.IsFalse(result.RealEmailSent);
    }

    private static NexaEmailDeliveryProvider Provider() => new();
    private static NexaEmailProviderConfig SandboxConfig() => new(NexaEmailProviderKind.SandboxProvider, PrivatePreviewLocal: true, RealEmailDeliveryEnabled: false, SandboxAllowed: true);
    private static NexaEmailDeliveryRequest Request(IReadOnlyDictionary<string, string>? parameters = null, bool containsCookie = false) =>
        new("email-request", "preview@example.invalid", NexaEmailTemplateKind.FreeLicenseRequested, parameters ?? new Dictionary<string, string> { ["account"] = "account-local" }, ContainsSecret: false, ContainsCookie: containsCookie);
}
