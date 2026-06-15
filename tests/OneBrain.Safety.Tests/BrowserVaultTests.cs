using Microsoft.VisualStudio.TestTools.UnitTesting;
using OneBrain.BrowserExecutor.Cdp;
using OneBrain.BrowserExecutor.Contracts;

namespace OneBrain.Safety.Tests;

[TestClass]
public sealed class BrowserVaultTests
{
    [TestMethod]
    public async Task BrowserVaultProductiveDefaultDenyAndUnsupportedFailClosed()
    {
        var reference = Reference();
        var request = Storage(reference, consent: null, provider: BrowserProductiveVaultProviderKind.Null);
        var nullProvider = new NullBrowserProductiveVaultProvider();
        var unsupported = new UnsupportedBrowserProductiveVaultProvider();

        var nullResult = await nullProvider.StoreAsync(request, BrowserProductiveVaultPolicy.DenyAll, BrowserProductiveVaultConfiguration.Null);
        var unsupportedResult = await unsupported.StoreAsync(request with { ProviderKind = BrowserProductiveVaultProviderKind.Unsupported }, BrowserProductiveVaultPolicy.DenyAll, BrowserProductiveVaultConfiguration.Null with { ProviderKind = BrowserProductiveVaultProviderKind.Unsupported });

        Assert.AreNotEqual(BrowserProductiveVaultDecisionKind.Allowed, nullResult.Decision);
        Assert.AreEqual(BrowserProductiveVaultDecisionKind.Unconfigured, nullResult.Decision);
        Assert.AreEqual(BrowserProductiveVaultDecisionKind.Unsupported, unsupportedResult.Decision);
        Assert.IsTrue(nullResult.Validate().IsValid);
        Assert.IsTrue(unsupportedResult.Validate().IsValid);
    }

    [TestMethod]
    public async Task BrowserVaultOsBackedProvidersAreDesignOnlyAndFailClosed()
    {
        var reference = Reference();
        var consent = Consent(BrowserVaultConsentType.SecretStorageConsent, reference);
        var policy = AllowSyntheticPolicy(BrowserSecretKind.ApiKey);
        var dpapi = new WindowsDpapiSecretVaultProvider();
        var credentialManager = new WindowsCredentialManagerSecretVaultProvider();

        var dpapiResult = await dpapi.StoreAsync(Storage(reference, consent, BrowserProductiveVaultProviderKind.WindowsDpapi), policy, Config(BrowserProductiveVaultProviderKind.WindowsDpapi, osBacked: true));
        var cmResult = await credentialManager.StoreAsync(Storage(reference, consent, BrowserProductiveVaultProviderKind.WindowsCredentialManager), policy, Config(BrowserProductiveVaultProviderKind.WindowsCredentialManager, osBacked: true));

        Assert.AreEqual(BrowserProductiveVaultDecisionKind.FailClosed, dpapiResult.Decision);
        Assert.AreEqual(BrowserProductiveVaultDecisionKind.FailClosed, cmResult.Decision);
        Assert.IsNull(dpapiResult.Reference);
        Assert.IsNull(cmResult.Reference);
    }

    [TestMethod]
    public async Task BrowserVaultTestOnlyProviderStoresAndRetrievesSyntheticReferencesOnly()
    {
        var provider = new InMemoryTestOnlyProductiveVaultProvider();
        var reference = Reference();
        var storageConsent = Consent(BrowserVaultConsentType.SecretStorageConsent, reference);
        var retrievalConsent = Consent(BrowserVaultConsentType.SecretRetrievalConsent, reference);
        var policy = AllowSyntheticPolicy(BrowserSecretKind.ApiKey);
        var config = Config(BrowserProductiveVaultProviderKind.InMemoryTestOnly);

        var stored = await provider.StoreAsync(Storage(reference, storageConsent, BrowserProductiveVaultProviderKind.InMemoryTestOnly), policy, config);
        var retrieved = await provider.RetrieveAsync(Retrieval(reference, retrievalConsent, BrowserProductiveVaultProviderKind.InMemoryTestOnly), policy, config);

        Assert.AreEqual(BrowserProductiveVaultDecisionKind.Allowed, stored.Decision);
        Assert.AreEqual(BrowserProductiveVaultDecisionKind.Allowed, retrieved.Decision);
        Assert.AreEqual(reference, retrieved.Reference);
        Assert.IsTrue(provider.AuditEvents.All(audit => audit.Validate().IsValid));
        Assert.IsFalse(provider.AuditEvents.Any(audit => BrowserCredentialRedactor.ContainsSecret(audit.RedactedSummary)));
    }

    [TestMethod]
    public async Task BrowserVaultTestOnlyProviderRejectsNonSyntheticReferences()
    {
        var provider = new InMemoryTestOnlyProductiveVaultProvider();
        var reference = Reference() with { SecretId = $"secret-ref-{Guid.NewGuid():N}" };
        var consent = Consent(BrowserVaultConsentType.SecretStorageConsent, reference);

        var result = await provider.StoreAsync(Storage(reference, consent, BrowserProductiveVaultProviderKind.InMemoryTestOnly), AllowSyntheticPolicy(BrowserSecretKind.ApiKey), Config(BrowserProductiveVaultProviderKind.InMemoryTestOnly));

        Assert.AreEqual(BrowserProductiveVaultDecisionKind.FailClosed, result.Decision);
        Assert.IsNull(result.Reference);
    }

    [TestMethod]
    public async Task BrowserVaultOperationsRequireSeparateScopedConsent()
    {
        var provider = new InMemoryTestOnlyProductiveVaultProvider();
        var reference = Reference();
        var storageConsent = Consent(BrowserVaultConsentType.SecretStorageConsent, reference);
        var useConsent = Consent(BrowserVaultConsentType.SecretUseConsent, reference);
        var exportConsent = Consent(BrowserVaultConsentType.SecretExportConsent, reference);
        var policy = AllowSyntheticPolicy(BrowserSecretKind.ApiKey);
        var config = Config(BrowserProductiveVaultProviderKind.InMemoryTestOnly);

        var storageWithoutConsent = await provider.StoreAsync(Storage(reference, null, BrowserProductiveVaultProviderKind.InMemoryTestOnly), policy, config);
        var stored = await provider.StoreAsync(Storage(reference, storageConsent, BrowserProductiveVaultProviderKind.InMemoryTestOnly), policy, config);
        var retrievalWithStorageConsent = await provider.RetrieveAsync(Retrieval(reference, storageConsent, BrowserProductiveVaultProviderKind.InMemoryTestOnly), policy, config);
        var useWithRetrievalConsent = await provider.UseAsync(Use(reference, Consent(BrowserVaultConsentType.SecretRetrievalConsent, reference), BrowserProductiveVaultProviderKind.InMemoryTestOnly), policy, config);
        var useWithUseConsent = await provider.UseAsync(Use(reference, useConsent, BrowserProductiveVaultProviderKind.InMemoryTestOnly), policy, config);
        var exportWithExportConsent = await provider.ExportAsync(Retrieval(reference, exportConsent, BrowserProductiveVaultProviderKind.InMemoryTestOnly), policy, config);

        Assert.AreEqual(BrowserProductiveVaultDecisionKind.RequiresConsent, storageWithoutConsent.Decision);
        Assert.AreEqual(BrowserProductiveVaultDecisionKind.Allowed, stored.Decision);
        Assert.AreEqual(BrowserProductiveVaultDecisionKind.RequiresConsent, retrievalWithStorageConsent.Decision);
        Assert.AreEqual(BrowserProductiveVaultDecisionKind.RequiresConsent, useWithRetrievalConsent.Decision);
        Assert.AreEqual(BrowserProductiveVaultDecisionKind.Allowed, useWithUseConsent.Decision);
        Assert.AreEqual(BrowserProductiveVaultDecisionKind.Denied, exportWithExportConsent.Decision);
    }

    [TestMethod]
    public async Task BrowserVaultDeletionRotationAndCookieConsentAreScoped()
    {
        var provider = new InMemoryTestOnlyProductiveVaultProvider();
        var reference = Reference(BrowserSecretKind.Cookie);
        var storageConsent = Consent(BrowserVaultConsentType.SecretStorageConsent, reference);
        var cookieConsent = Consent(BrowserVaultConsentType.CookieAccessConsent, reference);
        var deletionConsent = Consent(BrowserVaultConsentType.SecretDeletionConsent, reference);
        var rotationConsent = Consent(BrowserVaultConsentType.SecretRotationConsent, reference);
        var policy = AllowSyntheticPolicy(BrowserSecretKind.Cookie);
        var config = Config(BrowserProductiveVaultProviderKind.InMemoryTestOnly);

        await provider.StoreAsync(Storage(reference, storageConsent, BrowserProductiveVaultProviderKind.InMemoryTestOnly), policy, config);
        var passwordUseWithCookieConsent = await provider.UseAsync(Use(Reference(BrowserSecretKind.Password), cookieConsent, BrowserProductiveVaultProviderKind.InMemoryTestOnly), AllowSyntheticPolicy(BrowserSecretKind.Password), config);
        var rotation = await provider.RotateAsync(Rotation(reference, rotationConsent, BrowserProductiveVaultProviderKind.InMemoryTestOnly), policy, config);
        var deletion = await provider.DeleteAsync(Deletion(reference, deletionConsent, BrowserProductiveVaultProviderKind.InMemoryTestOnly), policy, config);
        var retrievalAfterDelete = await provider.RetrieveAsync(Retrieval(reference, Consent(BrowserVaultConsentType.SecretRetrievalConsent, reference), BrowserProductiveVaultProviderKind.InMemoryTestOnly), policy, config);

        Assert.AreEqual(BrowserProductiveVaultDecisionKind.RequiresConsent, passwordUseWithCookieConsent.Decision);
        Assert.AreEqual(BrowserProductiveVaultDecisionKind.Allowed, rotation.Decision);
        Assert.AreEqual(BrowserProductiveVaultDecisionKind.Allowed, deletion.Decision);
        Assert.AreEqual(BrowserProductiveVaultDecisionKind.Denied, retrievalAfterDelete.Decision);
    }

    [TestMethod]
    public void BrowserVaultConsentCompanionIntentIsNonAuthoritativeAndCannotGrant()
    {
        var service = new BrowserVaultConsentService();
        var request = ConsentRequest(BrowserVaultConsentType.SecretUseConsent, Reference());

        var intent = service.CompanionIntent("vaultConsent.userApproved", request, "user clicked approve");
        var decision = service.Decide(
            request,
            BrowserVaultConsentStatus.Granted,
            DateTimeOffset.UtcNow,
            BrowserVaultConsentAuthorityKind.UserViaCompanionIntent,
            approvingActor: "chrome-companion",
            approvalSource: "sidepanel",
            consentProofRef: "proof-companion",
            consentChallengeId: request.ConsentChallengeId,
            companionAuthoritative: true);

        Assert.IsFalse(intent.Authoritative);
        Assert.IsFalse(intent.CanGrantConsent);
        Assert.AreEqual(BrowserVerificationStatus.Uncertain, intent.VerificationStatus);
        Assert.AreEqual(BrowserVaultConsentStatus.Invalid, decision.Status);
        Assert.IsFalse(decision.Allows(DateTimeOffset.UtcNow, BrowserVaultConsentType.SecretUseConsent, request.SecretReference));
        Assert.IsTrue(intent.Validate().IsValid);
    }

    [TestMethod]
    public void BrowserVaultConsentExpiredRevokedUnknownAndMissingProofBlock()
    {
        var service = new BrowserVaultConsentService();
        var expiredRequest = ConsentRequest(BrowserVaultConsentType.SecretRetrievalConsent, Reference(), ttl: TimeSpan.FromMilliseconds(-1));
        var unknownRequest = ConsentRequest(BrowserVaultConsentType.SecretRetrievalConsent, Reference());
        var missingProofRequest = ConsentRequest(BrowserVaultConsentType.SecretRetrievalConsent, Reference());
        var revokedRequest = ConsentRequest(BrowserVaultConsentType.SecretRetrievalConsent, Reference());

        var expired = Grant(service, expiredRequest);
        var unknown = service.Decide(unknownRequest, BrowserVaultConsentStatus.Granted, DateTimeOffset.UtcNow, BrowserVaultConsentAuthorityKind.Unknown, "core", "policy", "proof", unknownRequest.ConsentChallengeId);
        var missingProof = service.Decide(missingProofRequest, BrowserVaultConsentStatus.Granted, DateTimeOffset.UtcNow, BrowserVaultConsentAuthorityKind.CorePolicy, "core", "policy", "", missingProofRequest.ConsentChallengeId);
        var revoked = service.Decide(revokedRequest, BrowserVaultConsentStatus.Revoked, DateTimeOffset.UtcNow, BrowserVaultConsentAuthorityKind.CorePolicy, "core", "policy", "proof", revokedRequest.ConsentChallengeId);

        Assert.AreEqual(BrowserVaultConsentStatus.Expired, expired.Status);
        Assert.AreEqual(BrowserVaultConsentStatus.Invalid, unknown.Status);
        Assert.AreEqual(BrowserVaultConsentStatus.Invalid, missingProof.Status);
        Assert.AreEqual(BrowserVaultConsentStatus.Revoked, revoked.Status);
    }

    [TestMethod]
    public void BrowserVaultConsentPresentationIsRedactedAndNonAuthoritative()
    {
        var service = new BrowserVaultConsentService();
        var request = ConsentRequest(BrowserVaultConsentType.SecretStorageConsent, Reference(), purpose: BrowserCredentialRedactor.Redact("store token=synthetic-value"));

        var presentation = service.CreatePresentation(request);

        Assert.IsFalse(presentation.Authoritative);
        Assert.IsTrue(presentation.Redacted);
        Assert.IsTrue(presentation.Validate().IsValid);
        Assert.IsFalse(BrowserCredentialRedactor.ContainsSecret(presentation.Instruction));
    }

    [TestMethod]
    public async Task BrowserVaultRejectsSecretLikeIdsInProductiveRequests()
    {
        var provider = new InMemoryTestOnlyProductiveVaultProvider();
        var reference = Reference() with { SecretId = "token=raw-secret-value" };
        var consent = Consent(BrowserVaultConsentType.SecretStorageConsent, reference);

        var result = await provider.StoreAsync(Storage(reference, consent, BrowserProductiveVaultProviderKind.InMemoryTestOnly), AllowSyntheticPolicy(BrowserSecretKind.ApiKey), Config(BrowserProductiveVaultProviderKind.InMemoryTestOnly));

        Assert.AreEqual(BrowserProductiveVaultDecisionKind.FailClosed, result.Decision);
        Assert.IsFalse(result.AuditEvent.SecretId.Contains("raw-secret-value", StringComparison.OrdinalIgnoreCase));
        Assert.IsTrue(result.Validate().IsValid);
    }

    [TestMethod]
    public void BrowserVaultProfileRealConsentDoesNotAuthorizeSecretRetrieval()
    {
        var reference = Reference();
        var profileConsent = Consent(BrowserVaultConsentType.ProfileRealConsent, reference);

        Assert.IsFalse(profileConsent.Allows(DateTimeOffset.UtcNow, BrowserVaultConsentType.SecretRetrievalConsent, reference));
    }

    private static BrowserSecretReference Reference(BrowserSecretKind kind = BrowserSecretKind.ApiKey) =>
        new(
            SecretId: $"synthetic-secret-{Guid.NewGuid():N}",
            Kind: kind,
            Scope: BrowserSecretScope.Temporary,
            Owner: "test-owner",
            Portal: "fixture",
            CreatedAtUtc: DateTimeOffset.UtcNow,
            RedactedLabel: $"{kind} Temporary [REDACTED]");

    private static BrowserProductiveVaultPolicy AllowSyntheticPolicy(BrowserSecretKind kind) =>
        new(
            DenyByDefault: true,
            RequireConsent: true,
            AllowSyntheticTestOnly: true,
            AllowOsBackedStorage: false,
            AllowSecretExport: false,
            AllowedKinds: new HashSet<BrowserSecretKind> { kind },
            AllowedScopes: new HashSet<BrowserSecretScope> { BrowserSecretScope.Temporary });

    private static BrowserProductiveVaultConfiguration Config(BrowserProductiveVaultProviderKind kind, bool osBacked = false) =>
        new(
            ProviderKind: kind,
            IsConfigured: true,
            EnableOsBackedStorage: osBacked,
            AllowSyntheticTestMode: true,
            ConfigurationId: $"vault-config-{kind}",
            ProviderInstanceId: $"vault-provider-{kind}");

    private static BrowserVaultConsentRequest ConsentRequest(BrowserVaultConsentType type, BrowserSecretReference reference, TimeSpan? ttl = null, string purpose = "synthetic consent") =>
        new BrowserVaultConsentService().CreateRequest(
            type,
            BrowserVaultConsentScope.Secret,
            reference,
            "run-1",
            "action-1",
            $"corr-{Guid.NewGuid():N}",
            "profile-1",
            "session-1",
            "core-test",
            purpose,
            ttl);

    private static BrowserVaultConsentDecision Consent(BrowserVaultConsentType type, BrowserSecretReference reference) =>
        Grant(new BrowserVaultConsentService(), ConsentRequest(type, reference));

    private static BrowserVaultConsentDecision Grant(BrowserVaultConsentService service, BrowserVaultConsentRequest request) =>
        service.Decide(
            request,
            BrowserVaultConsentStatus.Granted,
            DateTimeOffset.UtcNow,
            BrowserVaultConsentAuthorityKind.TestHarness,
            approvingActor: "test-harness",
            approvalSource: "unit-test",
            consentProofRef: $"vault-consent-proof-{Guid.NewGuid():N}",
            consentChallengeId: request.ConsentChallengeId);

    private static BrowserSecretStorageRequest Storage(BrowserSecretReference reference, BrowserVaultConsentDecision? consent, BrowserProductiveVaultProviderKind provider) =>
        new($"vault-request-{Guid.NewGuid():N}", "run-1", "action-1", $"corr-{Guid.NewGuid():N}", "profile-1", "session-1", reference, provider, consent, DateTimeOffset.UtcNow, "synthetic storage");

    private static BrowserSecretRetrievalRequest Retrieval(BrowserSecretReference reference, BrowserVaultConsentDecision? consent, BrowserProductiveVaultProviderKind provider) =>
        new($"vault-request-{Guid.NewGuid():N}", "run-1", "action-1", $"corr-{Guid.NewGuid():N}", "profile-1", "session-1", reference, provider, consent, DateTimeOffset.UtcNow, "synthetic retrieval");

    private static BrowserSecretUseRequest Use(BrowserSecretReference reference, BrowserVaultConsentDecision? consent, BrowserProductiveVaultProviderKind provider, bool autofill = false) =>
        new($"vault-request-{Guid.NewGuid():N}", "run-1", "action-1", $"corr-{Guid.NewGuid():N}", "profile-1", "session-1", reference, provider, consent, DateTimeOffset.UtcNow, "synthetic use", autofill);

    private static BrowserSecretRotationRequest Rotation(BrowserSecretReference reference, BrowserVaultConsentDecision? consent, BrowserProductiveVaultProviderKind provider) =>
        new($"vault-request-{Guid.NewGuid():N}", "run-1", "action-1", $"corr-{Guid.NewGuid():N}", "profile-1", "session-1", reference, provider, consent, DateTimeOffset.UtcNow, "synthetic rotation");

    private static BrowserSecretDeletionRequest Deletion(BrowserSecretReference reference, BrowserVaultConsentDecision? consent, BrowserProductiveVaultProviderKind provider) =>
        new($"vault-request-{Guid.NewGuid():N}", "run-1", "action-1", $"corr-{Guid.NewGuid():N}", "profile-1", "session-1", reference, provider, consent, DateTimeOffset.UtcNow, "synthetic deletion");
}
