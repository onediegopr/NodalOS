using Microsoft.VisualStudio.TestTools.UnitTesting;
using OneBrain.BrowserExecutor.Cdp;
using OneBrain.BrowserExecutor.Contracts;

namespace OneBrain.Safety.Tests;

[TestClass]
public sealed class BrowserProductiveVaultOsBackedM39Tests
{
    private const string SyntheticValue = "synthetic-os-backed-password";

    [TestMethod]
    public async Task BrowserProductiveVaultOsBackedStoresSyntheticSecret()
    {
        var provider = Provider();
        var reference = Reference();

        var result = await provider.StoreSyntheticAsync(Store(reference), SyntheticValue);

        Assert.IsTrue(result.Allowed, result.Reason);
        Assert.AreEqual(BrowserProductiveVaultProviderKind.OsBackedDpapiCurrentUser, result.ProviderKind);
        Assert.IsFalse(result.SecretValueReturned);
        Assert.IsFalse(result.ToString()!.Contains(SyntheticValue, StringComparison.Ordinal));
    }

    [TestMethod]
    public async Task BrowserProductiveVaultOsBackedRetrievesOnlyThroughCoreBoundary()
    {
        var provider = await StoredProvider();

        var publicResult = await provider.Provider.RetrieveAsync(Retrieve(provider.Reference));
        using var handle = await provider.Provider.RetrieveForCoreBoundaryAsync(Retrieve(provider.Reference));

        Assert.IsTrue(publicResult.AllowsCoreBoundaryUse, publicResult.Reason);
        Assert.IsFalse(publicResult.SecretValueReturned);
        Assert.IsFalse(publicResult.ToString()!.Contains(SyntheticValue, StringComparison.Ordinal));
        Assert.IsNotNull(handle);
        Assert.AreEqual(SyntheticValue, handle.RequireValue());
    }

    [TestMethod]
    public async Task BrowserProductiveVaultOsBackedPublicDtosDoNotExposeValue()
    {
        var provider = await StoredProvider();
        var result = await provider.Provider.RetrieveAsync(Retrieve(provider.Reference));

        Assert.IsFalse(result.ToString()!.Contains(SyntheticValue, StringComparison.Ordinal));
        Assert.IsFalse(result.Reference!.ToString()!.Contains(SyntheticValue, StringComparison.Ordinal));
    }

    [TestMethod]
    public async Task BrowserProductiveVaultOsBackedAuditDoesNotContainValue()
    {
        var provider = await StoredProvider();
        var result = await provider.Provider.RetrieveAsync(Retrieve(provider.Reference));

        Assert.IsTrue(result.AuditEvent.Validate().IsValid, string.Join("; ", result.AuditEvent.Validate().Errors));
        Assert.IsFalse(result.AuditEvent.ToString()!.Contains(SyntheticValue, StringComparison.Ordinal));
    }

    [TestMethod]
    public async Task BrowserProductiveVaultOsBackedExportDoesNotContainValue()
    {
        var provider = await StoredProvider();
        var retrieve = await provider.Provider.RetrieveAsync(Retrieve(provider.Reference));
        var export = string.Join("\n", retrieve.AuditRefs.Append(retrieve.AuditEvent.ToString()!));

        Assert.IsFalse(export.Contains(SyntheticValue, StringComparison.Ordinal));
    }

    [TestMethod]
    public async Task BrowserProductiveVaultOsBackedFailsWithoutConsent()
    {
        var provider = await StoredProvider();
        var result = await provider.Provider.RetrieveAsync(Retrieve(provider.Reference, includeConsent: false));

        Assert.AreEqual(BrowserProductiveVaultDecisionKind.RequiresConsent, result.Decision);
    }

    [TestMethod]
    public async Task BrowserProductiveVaultOsBackedFailsWithoutPolicy()
    {
        var provider = await StoredProvider();
        var result = await provider.Provider.RetrieveAsync(Retrieve(provider.Reference, policyAllowed: false));

        Assert.AreEqual(BrowserProductiveVaultDecisionKind.Denied, result.Decision);
    }

    [TestMethod]
    public async Task BrowserProductiveVaultOsBackedFailsWhenGateFails()
    {
        var provider = await StoredProvider();
        var result = await provider.Provider.RetrieveAsync(Retrieve(provider.Reference, gate: GateReport(OsBackedState() with { CompanionAuthoritative = true })));

        Assert.AreEqual(BrowserProductiveVaultDecisionKind.FailClosed, result.Decision);
    }

    [TestMethod]
    public async Task BrowserProductiveVaultOsBackedFailsWhenLicenseFeatureDisabled()
    {
        var provider = await StoredProvider();
        var result = await provider.Provider.RetrieveAsync(Retrieve(provider.Reference, license: DisabledLicenseDecision()));

        Assert.AreEqual(BrowserProductiveVaultDecisionKind.Denied, result.Decision);
    }

    [TestMethod]
    public async Task BrowserProductiveVaultOsBackedFailsForUnknownProvider()
    {
        var provider = Provider();
        var reference = Reference() with { ProviderKind = BrowserProductiveVaultProviderKind.Disabled };
        var result = await provider.StoreSyntheticAsync(Store(reference), SyntheticValue);

        Assert.AreEqual(BrowserProductiveVaultDecisionKind.FailClosed, result.Decision);
    }

    [TestMethod]
    public async Task BrowserProductiveVaultOsBackedRevokeBlocksRetrieval()
    {
        var provider = await StoredProvider();
        var deleted = await provider.Provider.DeleteAsync(Delete(provider.Reference));
        var retrieve = await provider.Provider.RetrieveAsync(Retrieve(provider.Reference));

        Assert.IsTrue(deleted.Allowed, deleted.Reason);
        Assert.AreEqual(BrowserProductiveVaultDecisionKind.Denied, retrieve.Decision);
    }

    [TestMethod]
    public void BrowserProductiveVaultOsBackedHealthCheckReportsProvider()
    {
        var health = Provider().HealthCheck();

        Assert.AreEqual(BrowserProductiveVaultProviderKind.OsBackedDpapiCurrentUser, health.ProviderKind);
        Assert.IsTrue(health.OsBacked);
        Assert.IsTrue(health.SyntheticOnly);
        Assert.IsTrue(health.ProviderAvailable, health.Status);
    }

    [TestMethod]
    public void BrowserRuntimePhaseGateAllowsVaultOsBackedMinimal()
    {
        var report = GateReport(OsBackedState());

        Assert.IsTrue(report.Passed, string.Join("; ", report.FailedChecks));
        Assert.AreEqual(BrowserRuntimeVaultState.OsBackedMinimalActive, report.ObservedState!.VaultState);
    }

    private static async Task<(BrowserProductiveVaultDpapiCurrentUserProvider Provider, BrowserProductiveVaultSecretReference Reference)> StoredProvider()
    {
        var provider = Provider();
        var reference = Reference();
        var stored = await provider.StoreSyntheticAsync(Store(reference), SyntheticValue);
        Assert.IsTrue(stored.Allowed, stored.Reason);
        return (provider, reference);
    }

    private static BrowserProductiveVaultDpapiCurrentUserProvider Provider()
    {
        var provider = new BrowserProductiveVaultDpapiCurrentUserProvider();
        var health = provider.HealthCheck();
        Assert.IsTrue(health.ProviderAvailable, health.Status);
        return provider;
    }

    private static BrowserProductiveVaultSecretReference Reference() =>
        new(
            $"productive-vault-ref-{Guid.NewGuid():N}",
            BrowserSecretKind.Password,
            BrowserSecretScope.Profile,
            BrowserProductiveVaultProviderKind.OsBackedDpapiCurrentUser,
            "synthetic-owner",
            "local-fixture",
            DateTimeOffset.UtcNow,
            null,
            BrowserProductiveVaultSecretLifecycleState.Active,
            "sandbox item");

    private static BrowserProductiveVaultStoreRequest Store(BrowserProductiveVaultSecretReference reference, BrowserVaultConsentDecision? consent = null, bool includeConsent = true, bool policyAllowed = true, BrowserRuntimePhaseCloseReport? gate = null, NexaLicensePolicyDecision? license = null) =>
        new(
            $"productive-vault-request-{Guid.NewGuid():N}",
            "run-vault",
            "action-vault",
            "corr-vault",
            "profile-controlled",
            "session-controlled",
            reference,
            Context(reference, BrowserVaultConsentType.SecretStorageConsent, consent, includeConsent, policyAllowed, gate, license, coreBoundary: false),
            "controlled access");

    private static BrowserProductiveVaultRetrieveRequest Retrieve(BrowserProductiveVaultSecretReference reference, BrowserVaultConsentDecision? consent = null, bool includeConsent = true, bool policyAllowed = true, BrowserRuntimePhaseCloseReport? gate = null, NexaLicensePolicyDecision? license = null) =>
        new(
            $"productive-vault-request-{Guid.NewGuid():N}",
            "run-vault",
            "action-vault",
            "corr-vault",
            "profile-controlled",
            "session-controlled",
            reference,
            Context(reference, BrowserVaultConsentType.SecretRetrievalConsent, consent, includeConsent, policyAllowed, gate, license, coreBoundary: true),
            "controlled access");

    private static BrowserProductiveVaultDeleteRequest Delete(BrowserProductiveVaultSecretReference reference) =>
        new(
            $"productive-vault-request-{Guid.NewGuid():N}",
            "run-vault",
            "action-vault",
            "corr-vault",
            "profile-controlled",
            "session-controlled",
            reference,
            Context(reference, BrowserVaultConsentType.SecretDeletionConsent, consent: null, includeConsent: true, policyAllowed: true, gate: null, license: null, coreBoundary: true),
            "controlled access");

    private static BrowserProductiveVaultAccessContext Context(BrowserProductiveVaultSecretReference reference, BrowserVaultConsentType consentType, BrowserVaultConsentDecision? consent, bool includeConsent, bool policyAllowed, BrowserRuntimePhaseCloseReport? gate, NexaLicensePolicyDecision? license, bool coreBoundary) =>
        new(
            includeConsent ? consent ?? Consent(consentType, reference) : null,
            gate ?? GateReport(OsBackedState()),
            license ?? ProductiveVaultAllowedDecision(),
            policyAllowed,
            coreBoundary,
            DateTimeOffset.UtcNow);

    private static BrowserVaultConsentDecision Consent(BrowserVaultConsentType type, BrowserProductiveVaultSecretReference reference, TimeSpan? ttl = null)
    {
        var service = new BrowserVaultConsentService();
        var request = service.CreateRequest(
            type,
            BrowserVaultConsentScope.Secret,
            ToSecretReference(reference),
            "run-vault",
            "action-vault",
            "corr-vault",
            "profile-controlled",
            "session-controlled",
            "core-test",
            "controlled access",
            ttl ?? TimeSpan.FromMinutes(5));
        return service.Decide(request, BrowserVaultConsentStatus.Granted, DateTimeOffset.UtcNow, BrowserVaultConsentAuthorityKind.TestHarness, "core-test", "test-harness", $"proof-{Guid.NewGuid():N}", request.ConsentChallengeId);
    }

    private static BrowserSecretReference ToSecretReference(BrowserProductiveVaultSecretReference reference) =>
        new(reference.ReferenceId, reference.Kind, reference.Scope, reference.Owner, reference.Portal, reference.CreatedAtUtc, reference.RedactedLabel);

    private static NexaLicensePolicyDecision ProductiveVaultAllowedDecision()
    {
        var account = NexaProductAdminM36Tests.CompanyAccount();
        var license = new NexaLicense(
            "license-productive-vault",
            account.AccountId,
            NexaPlan.Enterprise(),
            NexaLicenseStatus.Active,
            DateTimeOffset.UtcNow.AddMinutes(-1),
            DateTimeOffset.UtcNow.AddDays(30),
            [new NexaLicenseEntitlement(NexaFeatureFlag.ProductiveVault, Enabled: true, null)],
            ManualAdminOverride: true);
        return new NexaLicensePolicyEvaluator().Evaluate(new NexaLicensePolicyRequest(account, license, NexaFeatureFlag.ProductiveVault, account.Workers.First(), null, DateTimeOffset.UtcNow, SensitiveCompliancePolicyApproved: false));
    }

    private static NexaLicensePolicyDecision DisabledLicenseDecision()
    {
        var account = NexaProductAdminM36Tests.CompanyAccount();
        var license = new NexaLicense(
            "license-productive-vault-disabled",
            account.AccountId,
            NexaPlan.Pro(),
            NexaLicenseStatus.Active,
            DateTimeOffset.UtcNow.AddMinutes(-1),
            DateTimeOffset.UtcNow.AddDays(30),
            [],
            ManualAdminOverride: false);
        return new NexaLicensePolicyEvaluator().Evaluate(new NexaLicensePolicyRequest(account, license, NexaFeatureFlag.ProductiveVault, account.Workers.First(), null, DateTimeOffset.UtcNow, SensitiveCompliancePolicyApproved: false));
    }

    private static BrowserRuntimeObservedState OsBackedState() =>
        BrowserVaultMinimalM23Tests.SafeState(vaultMinimal: true) with
        {
            VaultState = BrowserRuntimeVaultState.OsBackedMinimalActive,
            ProductAdminFoundationDefined = true,
            LicensingFoundationDefined = true,
            FeatureFlagProductiveVaultEnabled = true,
            ProductiveVaultFeatureControlledTestContext = true,
            OsBackedVaultProviderHealthy = true,
            OsBackedVaultPublicDtosReferenceOnly = true
        };

    private static BrowserRuntimePhaseCloseReport GateReport(BrowserRuntimeObservedState state) =>
        BrowserVaultMinimalM23Tests.GateReport(state);
}
