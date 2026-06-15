using Microsoft.VisualStudio.TestTools.UnitTesting;
using OneBrain.BrowserExecutor.Cdp;
using OneBrain.BrowserExecutor.Contracts;

namespace OneBrain.Safety.Tests;

[TestClass]
public sealed class NexaPrivateTrialSimulationM64Tests
{
    [TestMethod]
    public void NexaPrivateTrialSimulationCreatesTrialWithAdminApproval()
    {
        var result = Run();

        Assert.IsTrue(result.Succeeded);
        CollectionAssert.Contains(result.States.ToList(), NexaPrivateTrialLifecycleState.AdminApproved);
        CollectionAssert.Contains(result.States.ToList(), NexaPrivateTrialLifecycleState.LicenseCreated);
    }

    [TestMethod]
    public void NexaPrivateTrialSimulationCreatesBillingPreviewWithoutCharge()
    {
        var result = Run();

        Assert.IsTrue(result.Billing.InvoicePreviewCreated);
        Assert.IsFalse(result.Billing.RealChargeCreated);
    }

    [TestMethod]
    public void NexaPrivateTrialSimulationQueuesEmailDraftOnly()
    {
        var result = Run();

        Assert.IsTrue(result.Email.EmailDraftQueued);
        Assert.IsFalse(result.Email.RealEmailSent);
    }

    [TestMethod]
    public void NexaPrivateTrialSimulationActivatesSafeFeatureSet()
    {
        var result = Run();

        Assert.IsNotNull(result.License);
        Assert.IsTrue(result.License.Plan.EnabledFeatures.Contains(NexaFeatureFlag.AdminConsole));
        Assert.IsFalse(result.License.Plan.EnabledFeatures.Contains(NexaFeatureFlag.SensitiveRealPilot));
    }

    [TestMethod]
    public void NexaPrivateTrialSimulationBlocksSensitiveRealPilot()
    {
        var result = Run(Request(features: new HashSet<NexaFeatureFlag> { NexaFeatureFlag.SensitiveRealPilot }));

        Assert.IsFalse(result.Succeeded);
        CollectionAssert.Contains(result.Violations.ToList(), "SensitiveRealPilot blocked");
    }

    [TestMethod]
    public void NexaPrivateTrialSimulationBlocksProductiveVaultByDefault()
    {
        var result = Run(Request(features: new HashSet<NexaFeatureFlag> { NexaFeatureFlag.ProductiveVault }));

        Assert.IsFalse(result.Succeeded);
        CollectionAssert.Contains(result.Violations.ToList(), "ProductiveVault blocked by default");
    }

    [TestMethod]
    public void NexaPrivateTrialSimulationExpiresTrial()
    {
        var result = new NexaPrivateTrialSimulationService().Expire(Run());

        CollectionAssert.Contains(result.States.ToList(), NexaPrivateTrialLifecycleState.Expired);
        Assert.AreEqual(NexaLicenseStatus.Expired, result.License!.Status);
    }

    [TestMethod]
    public void NexaPrivateTrialSimulationRevokesTrial()
    {
        var result = new NexaPrivateTrialSimulationService().Revoke(Run());

        CollectionAssert.Contains(result.States.ToList(), NexaPrivateTrialLifecycleState.Revoked);
        Assert.AreEqual(NexaLicenseStatus.Revoked, result.License!.Status);
    }

    [TestMethod]
    public void NexaPrivateTrialSimulationAuditIsRedacted()
    {
        var result = Run();

        Assert.IsTrue(result.Audit.Redacted);
        Assert.IsFalse(BrowserCredentialRedactor.ContainsSecret(NexaLeakHardeningSerialization.ToSafeJson(result.Audit)));
    }

    [TestMethod]
    public void NexaPrivateTrialSimulationFailsClosedForRealBillingProvider()
    {
        var result = Run(Request(billing: new NexaPaymentProviderConfig(NexaPaymentProviderKind.RealProviderFuture, true, RealBillingEnabled: false, SandboxAllowed: false, StoresPaymentCardData: false)));

        Assert.IsFalse(result.Succeeded);
        CollectionAssert.Contains(result.Violations.ToList(), "real billing provider blocked");
    }

    [TestMethod]
    public void NexaPrivateTrialSimulationFailsClosedForRealEmailProvider()
    {
        var result = Run(Request(email: new NexaEmailProviderConfig(NexaEmailProviderKind.RealProviderFuture, true, RealEmailDeliveryEnabled: false, SandboxAllowed: false)));

        Assert.IsFalse(result.Succeeded);
        CollectionAssert.Contains(result.Violations.ToList(), "real email provider blocked");
    }

    private static NexaPrivateTrialSimulationResult Run(NexaPrivateTrialSimulationRequest? request = null) =>
        new NexaPrivateTrialSimulationService().Run(request ?? Request());

    private static NexaPrivateTrialSimulationRequest Request(
        IReadOnlySet<NexaFeatureFlag>? features = null,
        NexaPaymentProviderConfig? billing = null,
        NexaEmailProviderConfig? email = null) =>
        new(
            "private-trial-request",
            "account-local",
            "trial@example.invalid",
            NexaRole.Admin,
            AdminApproved: true,
            features ?? new HashSet<NexaFeatureFlag> { NexaFeatureFlag.AdminConsole, NexaFeatureFlag.BrowserRuntime },
            [new NexaUsageLimit("trial-api-calls", 100, TimeSpan.FromDays(14))],
            billing ?? new NexaPaymentProviderConfig(NexaPaymentProviderKind.SandboxProvider, PrivatePreviewLocal: true, RealBillingEnabled: false, SandboxAllowed: true, StoresPaymentCardData: false),
            email ?? new NexaEmailProviderConfig(NexaEmailProviderKind.SandboxProvider, PrivatePreviewLocal: true, RealEmailDeliveryEnabled: false, SandboxAllowed: true),
            DateTimeOffset.UtcNow);
}
