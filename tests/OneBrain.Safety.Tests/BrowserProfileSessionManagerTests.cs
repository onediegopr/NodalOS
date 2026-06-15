using Microsoft.VisualStudio.TestTools.UnitTesting;
using OneBrain.BrowserExecutor.Cdp;
using OneBrain.BrowserExecutor.Contracts;

namespace OneBrain.Safety.Tests;

[TestClass]
public sealed class BrowserProfileSessionManagerTests
{
    [TestMethod]
    public async Task BrowserProfileDisposableCreatesAndCleansTemporaryControlledDirectory()
    {
        var manager = new BrowserProfileManager();
        var profile = manager.CreateProfile(Policy(BrowserProfileKind.Disposable, BrowserProfileCleanupPolicy.DeleteOnClose));

        Assert.AreEqual(BrowserProfileKind.Disposable, profile.Kind);
        StringAssert.Contains(profile.UserDataDir, "onebrain-cdp-");
        Assert.IsTrue(Directory.Exists(profile.UserDataDir));

        await manager.CleanupProfileAsync(profile);

        Assert.IsFalse(Directory.Exists(profile.UserDataDir));
    }

    [TestMethod]
    public async Task BrowserProfilePersistentControlledUsesOneBrainControlledRootAndIsKept()
    {
        var root = Path.Combine(Path.GetTempPath(), "onebrain-profile-tests", Guid.NewGuid().ToString("N"));
        var manager = new BrowserProfileManager(root);
        var profile = manager.CreateProfile(Policy(BrowserProfileKind.PersistentControlled, BrowserProfileCleanupPolicy.KeepControlled, root));

        try
        {
            Assert.AreEqual(BrowserProfileKind.PersistentControlled, profile.Kind);
            StringAssert.Contains(profile.UserDataDir, root);
            Assert.IsTrue(Directory.Exists(profile.UserDataDir));

            await manager.CleanupProfileAsync(profile);

            Assert.IsTrue(Directory.Exists(profile.UserDataDir));
        }
        finally
        {
            if (Directory.Exists(root))
                Directory.Delete(root, recursive: true);
        }
    }

    [TestMethod]
    public void BrowserProfileRealUserProfileIsBlockedWithoutConsent()
    {
        var manager = new BrowserProfileManager();
        var policy = Policy(BrowserProfileKind.UserProfileWithExplicitConsent, BrowserProfileCleanupPolicy.ManualReviewRequired) with
        {
            AllowRealUserProfile = false,
            ConsentPolicy = BrowserProfileConsentPolicy.ExplicitConsentRequired
        };

        var ex = Assert.ThrowsExactly<InvalidOperationException>(() => manager.CreateProfile(policy));

        StringAssert.Contains(ex.Message, "Real user profile requires explicit consent");
    }

    [TestMethod]
    public void BrowserSessionHasProfileOwnerCorrelationAndState()
    {
        var profileManager = new BrowserProfileManager();
        var profile = profileManager.CreateProfile(Policy(BrowserProfileKind.Disposable, BrowserProfileCleanupPolicy.DeleteOnClose));
        var sessionManager = new BrowserSessionManager();

        try
        {
            var session = sessionManager.CreateSession(profile, new BrowserSessionPolicy("tenant:acme/person:operator", "corr-123", TimeSpan.FromMinutes(5), profile.CleanupPolicy));
            session = sessionManager.MarkState(session.SessionId, BrowserSessionState.Active);

            Assert.AreEqual(profile.ProfileId, session.ProfileId);
            Assert.AreEqual("tenant:acme/person:operator", session.Owner);
            Assert.AreEqual("corr-123", session.CorrelationId);
            Assert.IsTrue(session.IsAlive(DateTimeOffset.UtcNow));
            Assert.IsTrue(session.CanAcceptModifyingAction(DateTimeOffset.UtcNow));
        }
        finally
        {
            profileManager.CleanupProfileAsync(profile).GetAwaiter().GetResult();
        }
    }

    [TestMethod]
    public void BrowserSessionClosedExpiredDisposedCannotAcceptModifyingAction()
    {
        var profileManager = new BrowserProfileManager();
        var profile = profileManager.CreateProfile(Policy(BrowserProfileKind.Disposable, BrowserProfileCleanupPolicy.DeleteOnClose));
        var sessionManager = new BrowserSessionManager();

        try
        {
            var session = sessionManager.CreateSession(profile, new BrowserSessionPolicy("worker:m7", "corr-expired", TimeSpan.FromMilliseconds(-1), profile.CleanupPolicy));
            var activeExpired = sessionManager.MarkState(session.SessionId, BrowserSessionState.Active);
            Assert.IsFalse(activeExpired.IsAlive(DateTimeOffset.UtcNow));
            Assert.IsFalse(activeExpired.CanAcceptModifyingAction(DateTimeOffset.UtcNow));

            var closed = sessionManager.MarkState(session.SessionId, BrowserSessionState.Closed);
            Assert.IsFalse(closed.CanAcceptModifyingAction(DateTimeOffset.UtcNow));

            var disposed = sessionManager.MarkState(session.SessionId, BrowserSessionState.Disposed);
            Assert.IsFalse(disposed.CanAcceptModifyingAction(DateTimeOffset.UtcNow));
        }
        finally
        {
            profileManager.CleanupProfileAsync(profile).GetAwaiter().GetResult();
        }
    }

    [TestMethod]
    public void BrowserProfileDiagnosticsRedactSensitivePathsAndAvoidStorageNames()
    {
        var manager = new BrowserProfileManager();
        var profile = manager.CreateProfile(Policy(BrowserProfileKind.Disposable, BrowserProfileCleanupPolicy.DeleteOnClose));
        var sessionManager = new BrowserSessionManager();

        try
        {
            var session = sessionManager.CreateSession(profile, new BrowserSessionPolicy("portal:test", "corr-redact", null, profile.CleanupPolicy));
            var diagnostic = sessionManager.Diagnostic(profile, session);
            var text = $"{diagnostic.RedactedUserDataDir} {diagnostic.Owner} {diagnostic.CorrelationId}";

            StringAssert.Contains(diagnostic.RedactedUserDataDir, "[REDACTED-PATH:");
            Assert.IsFalse(text.Contains(profile.UserDataDir, StringComparison.OrdinalIgnoreCase));
            Assert.IsFalse(text.Contains("cookie", StringComparison.OrdinalIgnoreCase));
            Assert.IsFalse(text.Contains("token", StringComparison.OrdinalIgnoreCase));
            Assert.IsFalse(text.Contains("localStorage", StringComparison.OrdinalIgnoreCase));
            Assert.IsFalse(text.Contains("IndexedDB", StringComparison.OrdinalIgnoreCase));
        }
        finally
        {
            manager.CleanupProfileAsync(profile).GetAwaiter().GetResult();
        }
    }

    [TestMethod]
    public async Task BrowserLauncherUsesProfileSessionManagerForDefaultDisposableProfile()
    {
        var browserPath = ChromeCdpBrowserLauncher.FindBrowserExecutable();
        if (browserPath is null)
            Assert.Inconclusive("Chrome/Edge executable is not available in this environment.");

        int pid;
        string userDataDir;

        await using (var session = await new ChromeCdpBrowserLauncher().LaunchAsync(new ChromeCdpOptions(browserPath)))
        {
            pid = session.ProcessId;
            userDataDir = session.UserDataDir;
            Assert.AreEqual(BrowserProfileKind.Disposable, session.Profile.Kind);
            Assert.AreEqual(BrowserSessionState.Active, session.ManagedSession.State);
            Assert.AreEqual(session.BrowserSessionId, session.ManagedSession.SessionId.Value);
            StringAssert.Contains(userDataDir, "onebrain-cdp-");
            Assert.IsTrue(Directory.Exists(userDataDir));
        }

        await Task.Delay(500);
        Assert.IsFalse(IsProcessAlive(pid));
        Assert.IsFalse(Directory.Exists(userDataDir));
    }

    private static BrowserProfilePolicy Policy(BrowserProfileKind kind, BrowserProfileCleanupPolicy cleanup, string? root = null) =>
        new(
            Kind: kind,
            Scope: kind == BrowserProfileKind.Disposable ? BrowserStorageScope.Temporary : BrowserStorageScope.Tenant,
            CleanupPolicy: cleanup,
            ConsentPolicy: kind == BrowserProfileKind.UserProfileWithExplicitConsent ? BrowserProfileConsentPolicy.ExplicitConsentRequired : BrowserProfileConsentPolicy.NotRequired,
            AllowRealUserProfile: false,
            ControlledRootDirectory: root ?? Path.Combine(Path.GetTempPath(), "onebrain-profile-tests"));

    private static bool IsProcessAlive(int pid)
    {
        try
        {
            using var process = System.Diagnostics.Process.GetProcessById(pid);
            return !process.HasExited;
        }
        catch
        {
            return false;
        }
    }
}
