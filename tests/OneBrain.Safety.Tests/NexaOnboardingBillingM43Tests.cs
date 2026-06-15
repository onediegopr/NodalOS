using Microsoft.VisualStudio.TestTools.UnitTesting;
using OneBrain.BrowserExecutor.Cdp;
using OneBrain.BrowserExecutor.Contracts;

namespace OneBrain.Safety.Tests;

[TestClass]
public sealed class NexaOnboardingBillingM43Tests
{
    [TestMethod]
    public void NexaOnboardingCreatesFreePlanForNewEmail()
    {
        var result = new NexaOnboardingService().Start(FreeRequest());

        Assert.IsTrue(result.Succeeded, result.Decision.Reason);
        Assert.AreEqual(NexaPlanKind.Free, result.License!.Plan.Kind);
    }

    [TestMethod]
    public void NexaOnboardingBlocksSecondFreeWithinWindow()
    {
        var result = new NexaOnboardingService().Start(FreeRequest(previousFree: DateTimeOffset.UtcNow.AddDays(-1)));

        Assert.AreEqual(NexaOnboardingStatus.Denied, result.Decision.Status);
    }

    [TestMethod]
    public void NexaOnboardingFreePlanExpiresAfterSevenDays()
    {
        var now = DateTimeOffset.UtcNow;
        var result = new NexaOnboardingService().Start(FreeRequest(now: now));

        Assert.AreEqual(now.AddDays(7), result.License!.ExpiresAtUtc);
    }

    [TestMethod]
    public void NexaOnboardingFreePlanDisablesSensitiveFeatures()
    {
        var result = new NexaOnboardingService().Start(FreeRequest());

        Assert.IsFalse(result.License!.Plan.Enables(NexaFeatureFlag.SensitiveRealPilot));
        Assert.IsFalse(result.License.Plan.Enables(NexaFeatureFlag.ProductiveVault));
        Assert.IsFalse(result.License.Plan.Enables(NexaFeatureFlag.ReplayProductive));
    }

    [TestMethod]
    public void NexaOnboardingAdminCanCreateTrial()
    {
        var result = new NexaOnboardingService().Start(TrialRequest(adminApproved: true));

        Assert.IsTrue(result.Succeeded, result.Decision.Reason);
        Assert.AreEqual(NexaPlanKind.Trial, result.License!.Plan.Kind);
    }

    [TestMethod]
    public void NexaOnboardingTrialSupportsCustomLimits()
    {
        var result = new NexaOnboardingService().Start(TrialRequest(adminApproved: true, limits: [new NexaUsageLimit("limit-trial-custom-m43", 3, TimeSpan.FromDays(2))]));

        Assert.AreEqual(3, result.License!.Plan.Limits.Single().MaxCount);
    }

    [TestMethod]
    public void NexaOnboardingTrialCanBeRevoked()
    {
        var service = new NexaOnboardingService();
        var trial = service.Start(TrialRequest(adminApproved: true));
        var revoked = service.RevokeTrial(trial);

        Assert.AreEqual(NexaLicenseStatus.Revoked, revoked.License!.Status);
        Assert.AreEqual(NexaOnboardingStatus.Revoked, revoked.Decision.Status);
    }

    [TestMethod]
    public void NexaBillingProviderIsMockOnly()
    {
        Assert.AreEqual(NexaBillingProviderKind.MockOnly, new NexaBillingMockProvider().Kind);
    }

    [TestMethod]
    public void NexaBillingMockDoesNotCreateRealCharge()
    {
        var result = new NexaBillingMockProvider().CreateCheckout(new NexaBillingCheckoutRequest("checkout-one", "account-company", NexaPlanKind.Pro, 10m, "USD"));

        Assert.IsFalse(result.RealChargeCreated);
    }

    [TestMethod]
    public void NexaBillingMockProducesInvoicePreview()
    {
        var result = new NexaBillingMockProvider().CreateCheckout(new NexaBillingCheckoutRequest("checkout-one", "account-company", NexaPlanKind.Pro, 10m, "USD"));

        Assert.IsNotNull(result.InvoicePreview);
        Assert.IsTrue(result.InvoicePreview.MockOnly);
    }

    [TestMethod]
    public void NexaEmailOutboxDoesNotSendRealEmail()
    {
        var draft = new NexaEmailOutboxMock().Queue("user@example.test", NexaEmailTemplate.FreeLicenseRequested, "Free requested", "Draft only");

        Assert.IsFalse(draft.Sent);
        Assert.AreEqual(NexaEmailDeliveryMode.MockOutboxOnly, draft.DeliveryMode);
    }

    [TestMethod]
    public void NexaEmailOutboxStoresDraftOnly()
    {
        var outbox = new NexaEmailOutboxMock();
        outbox.Queue("user@example.test", NexaEmailTemplate.TrialCreated, "Trial created", "Draft only");

        Assert.AreEqual(1, outbox.Drafts.Count);
        Assert.IsTrue(outbox.Drafts.Single().Validate().IsValid);
    }

    [TestMethod]
    public void NexaOnboardingAuditDoesNotContainSecrets()
    {
        var result = new NexaOnboardingService().Start(FreeRequest());

        Assert.IsTrue(result.AuditEvent.Validate().IsValid);
        Assert.IsFalse(result.AuditEvent.ToString()!.Contains("password", StringComparison.OrdinalIgnoreCase));
        Assert.IsFalse(result.AuditEvent.ToString()!.Contains("cookie", StringComparison.OrdinalIgnoreCase));
    }

    [TestMethod]
    public void BrowserRuntimePhaseGateFailsWhenRealBillingEnabled()
    {
        var report = BrowserVaultMinimalM23Tests.GateReport(BrowserVaultMinimalM23Tests.SafeState(vaultMinimal: true) with
        {
            ProductAdminFoundationDefined = true,
            LicensingFoundationDefined = true,
            PackagingFoundationDefined = true,
            DiagnosticsBundleDefined = true,
            BillingMockDefined = true,
            OnboardingMockDefined = true,
            RealBillingDisabled = false
        });

        Assert.AreEqual(BrowserRuntimePhaseCloseStatus.Failed, report.Status);
        CollectionAssert.Contains(report.FailedChecks.ToList(), "packaging diagnostics billing onboarding safe");
    }

    private static NexaOnboardingRequest FreeRequest(DateTimeOffset? now = null, DateTimeOffset? previousFree = null) =>
        new("onboarding-request-free", "free@example.test", "account-free", NexaPlanKind.Free, AdminApproved: false, now ?? DateTimeOffset.UtcNow, previousFree);

    private static NexaOnboardingRequest TrialRequest(bool adminApproved, IReadOnlyList<NexaUsageLimit>? limits = null) =>
        new("onboarding-request-trial", "trial@example.test", "account-trial", NexaPlanKind.Trial, adminApproved, DateTimeOffset.UtcNow, null, new HashSet<NexaFeatureFlag> { NexaFeatureFlag.BrowserRuntime, NexaFeatureFlag.AdminConsole }, limits);
}
