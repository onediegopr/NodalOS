using Microsoft.VisualStudio.TestTools.UnitTesting;
using OneBrain.BrowserExecutor.Cdp;
using OneBrain.BrowserExecutor.Contracts;

namespace OneBrain.Safety.Tests;

[TestClass]
public sealed class BrowserProfileConsentTests
{
    [TestMethod]
    public void BrowserProfileConsentBlocksRealProfileWithoutConsent()
    {
        var profileManager = new BrowserProfileManager();
        var policy = RealProfilePolicy(consentGranted: false);

        var result = profileManager.ValidateRealUserProfileConsent(policy, consent: null, DateTimeOffset.UtcNow);

        Assert.IsFalse(result.IsValid);
        Assert.IsTrue(result.Errors.Any(error => error.Contains("consent", StringComparison.OrdinalIgnoreCase)));
    }

    [TestMethod]
    public void BrowserProfileConsentGrantedAllowsOnlyScopedValidationButDoesNotLaunchRealProfileInM12()
    {
        var manager = new BrowserProfileConsentManager();
        var profileManager = new BrowserProfileManager();
        var request = manager.CreateRequest("profile-real", "session-1", "corr-1", BrowserProfileConsentScope.Profile, "person:test", "launch real profile with explicit consent");
        var decision = manager.Decide(request, BrowserProfileConsentStatus.Granted, DateTimeOffset.UtcNow);
        var policy = RealProfilePolicy(consentGranted: true);

        var validation = profileManager.ValidateRealUserProfileConsent(policy, decision, DateTimeOffset.UtcNow);
        var launch = Assert.ThrowsExactly<NotSupportedException>(() => profileManager.CreateRealUserProfileDescriptorWithConsent(policy, decision, DateTimeOffset.UtcNow));

        Assert.IsTrue(validation.IsValid);
        Assert.IsTrue(decision.AllowsRealProfile(DateTimeOffset.UtcNow));
        StringAssert.Contains(launch.Message, "not implemented in M12");
        Assert.AreEqual(1, manager.AuditEvents.Count);
    }

    [TestMethod]
    public void BrowserProfileConsentDeniedExpiredAndRevokedBlock()
    {
        var manager = new BrowserProfileConsentManager();
        var profileManager = new BrowserProfileManager();
        var policy = RealProfilePolicy(consentGranted: true);
        var deniedRequest = manager.CreateRequest("profile-real", "session-1", "corr-denied", BrowserProfileConsentScope.Profile, "person:test", "real profile");
        var expiredRequest = manager.CreateRequest("profile-real", "session-1", "corr-expired", BrowserProfileConsentScope.Profile, "person:test", "real profile", TimeSpan.FromMilliseconds(-1));
        var revokedRequest = manager.CreateRequest("profile-real", "session-1", "corr-revoked", BrowserProfileConsentScope.Profile, "person:test", "real profile");

        var denied = manager.Decide(deniedRequest, BrowserProfileConsentStatus.Denied, DateTimeOffset.UtcNow);
        var expired = manager.Decide(expiredRequest, BrowserProfileConsentStatus.Granted, DateTimeOffset.UtcNow);
        var revoked = manager.Revoke(revokedRequest);

        Assert.IsFalse(profileManager.ValidateRealUserProfileConsent(policy, denied, DateTimeOffset.UtcNow).IsValid);
        Assert.IsFalse(profileManager.ValidateRealUserProfileConsent(policy, expired, DateTimeOffset.UtcNow).IsValid);
        Assert.IsFalse(profileManager.ValidateRealUserProfileConsent(policy, revoked, DateTimeOffset.UtcNow).IsValid);
    }

    [TestMethod]
    public void BrowserProfileConsentScopeMustMatchProfileUse()
    {
        var manager = new BrowserProfileConsentManager();
        var profileManager = new BrowserProfileManager();
        var request = manager.CreateRequest("profile-real", "session-1", "corr-runtime", BrowserProfileConsentScope.Runtime, "person:test", "runtime-only consent");
        var decision = manager.Decide(request, BrowserProfileConsentStatus.Granted, DateTimeOffset.UtcNow);

        var validation = profileManager.ValidateRealUserProfileConsent(RealProfilePolicy(consentGranted: true), decision, DateTimeOffset.UtcNow);

        Assert.IsFalse(validation.IsValid);
        Assert.IsTrue(validation.Errors.Any(error => error.Contains("scope", StringComparison.OrdinalIgnoreCase)));
    }

    [TestMethod]
    public void BrowserProfileConsentDoesNotAuthorizeSecretAccess()
    {
        var consentManager = new BrowserProfileConsentManager();
        var request = consentManager.CreateRequest("profile-real", "session-1", "corr-1", BrowserProfileConsentScope.Profile, "person:test", "real profile");
        var consent = consentManager.Decide(request, BrowserProfileConsentStatus.Granted, DateTimeOffset.UtcNow);
        var secret = new BrowserSecretReference("secret-1", BrowserSecretKind.Password, BrowserSecretScope.Person, "person:test", "fixture", DateTimeOffset.UtcNow, "Password:[REDACTED]");
        var secretRequest = new BrowserSecretAccessRequest("secret-request-1", "run-1", "action-1", "corr-1", "profile-real", "session-1", secret, BrowserSecretUsageIntent.FillCredential, DateTimeOffset.UtcNow, "synthetic password fill");

        var secretDecision = new BrowserSecretAccessPolicyEvaluator().Decide(secretRequest, BrowserSecretAccessPolicy.DenyAll);

        Assert.IsTrue(consent.AllowsRealProfile(DateTimeOffset.UtcNow));
        Assert.AreNotEqual(BrowserSecretAccessDecisionKind.Allowed, secretDecision.Decision);
    }

    [TestMethod]
    public void BrowserProfileConsentAuditIsRedactedAndAuditable()
    {
        var manager = new BrowserProfileConsentManager();
        var request = manager.CreateRequest("profile-real", "session-1", "corr-1", BrowserProfileConsentScope.Profile, "person:test", BrowserCredentialRedactor.Redact("purpose token=synthetic"));
        var decision = manager.Decide(request, BrowserProfileConsentStatus.Granted, DateTimeOffset.UtcNow);

        Assert.IsTrue(decision.AuditEvent.RedactionApplied);
        Assert.IsFalse(BrowserCredentialRedactor.ContainsSecret(decision.AuditEvent.RedactedSummary));
        Assert.IsFalse(BrowserCredentialRedactor.ContainsSecret(decision.Request.Purpose));
        Assert.AreEqual(BrowserProfileConsentStatus.Granted, decision.AuditEvent.Status);
    }

    private static BrowserProfilePolicy RealProfilePolicy(bool consentGranted) =>
        new(
            Kind: BrowserProfileKind.UserProfileWithExplicitConsent,
            Scope: BrowserStorageScope.Person,
            CleanupPolicy: BrowserProfileCleanupPolicy.ManualReviewRequired,
            ConsentPolicy: consentGranted ? BrowserProfileConsentPolicy.Granted : BrowserProfileConsentPolicy.ExplicitConsentRequired,
            AllowRealUserProfile: consentGranted,
            ControlledRootDirectory: Path.Combine(Path.GetTempPath(), "onebrain-profile-consent-tests"));
}
