using Microsoft.VisualStudio.TestTools.UnitTesting;
using OneBrain.BrowserExecutor.Cdp;
using OneBrain.BrowserExecutor.Contracts;

namespace OneBrain.Safety.Tests;

[TestClass]
public sealed class ChromeCdpExternalProofM103M105Tests
{
    [TestMethod]
    public void ChromeCdpExternalPreflightUnavailableBlocksWithoutFailingNormalSuite()
    {
        var result = new ChromeCdpExternalPreflightService().Evaluate(Preflight(browserPath: null, optIn: true));

        Assert.AreEqual(ChromeCdpExternalPreflightStatus.ChromeCdpUnavailable, result.Status);
        Assert.IsFalse(result.CanAttemptLiveProof);
        Assert.IsFalse(result.LaunchesBrowser);
    }

    [TestMethod]
    public void ChromeCdpExternalPreflightPersonalProfileBlocked()
    {
        var result = new ChromeCdpExternalPreflightService().Evaluate(Preflight(usesPersonalProfile: true, profileDirectory: @"C:\Users\diego\AppData\Local\Google\Chrome\User Data"));

        Assert.AreEqual(ChromeCdpExternalPreflightStatus.UnsafeProfileBlocked, result.Status);
        Assert.IsTrue(result.ReasonCodes.Any(r => r.Contains("personal", StringComparison.OrdinalIgnoreCase)));
    }

    [TestMethod]
    public void ChromeCdpExternalPreflightNonAllowlistedHostBlocked()
    {
        var result = new ChromeCdpExternalPreflightService().Evaluate(Preflight(targetHost: "example.com"));

        Assert.AreNotEqual(ChromeCdpExternalPreflightStatus.ReadyForExternalCdpReadOnlyProof, result.Status);
        Assert.IsTrue(result.ReasonCodes.Any(r => r.Contains("allowlisted", StringComparison.OrdinalIgnoreCase)));
    }

    [TestMethod]
    public void ChromeCdpExternalPreflightIsolatedProfileAndAllowlistedTargetReady()
    {
        var result = new ChromeCdpExternalPreflightService().Evaluate(Preflight());

        Assert.AreEqual(ChromeCdpExternalPreflightStatus.ReadyForExternalCdpReadOnlyProof, result.Status);
        Assert.IsTrue(result.CanAttemptLiveProof);
    }

    [TestMethod]
    public void ChromeCdpExternalPreflightNoOptInDoesNotLaunch()
    {
        var result = new ChromeCdpExternalPreflightService().Evaluate(Preflight(optIn: false));

        Assert.AreEqual(ChromeCdpExternalPreflightStatus.ChromeCdpAvailable, result.Status);
        Assert.IsFalse(result.CanAttemptLiveProof);
        Assert.IsFalse(result.LaunchesBrowser);
    }

    [TestMethod]
    public void ChromeCdpExternalPreflightOptInReadyCanAttemptProof()
    {
        var result = new ChromeCdpExternalPreflightService().Evaluate(Preflight(optIn: true));

        Assert.IsTrue(result.CanAttemptLiveProof);
        Assert.AreEqual(ChromeCdpExternalPreflightStatus.ReadyForExternalCdpReadOnlyProof, result.Status);
    }

    [TestMethod]
    public void ChromeCdpExternalProbeHttpProofCannotMarkRealChromeCdp()
    {
        var harness = new NexaExternalProofHarness().Evaluate(HarnessRequest(), DateTimeOffset.UtcNow);
        var pack = new NexaExternalReadOnlyEvidencePackBuilder().Build(harness, HarnessRequest(), runtimeExecuted: true, runtimePassed: true, NexaExternalProofProbeKind.RealHttpClient);

        Assert.AreEqual(NexaExternalProofProbeKind.RealHttpClient, pack.ProbeKind);
        Assert.AreEqual("HttpReadOnlyExternal", pack.Tooling);
        CollectionAssert.DoesNotContain(pack.RuntimeCapabilities.ToArray(), "BrowserNavigationReadOnly");
        CollectionAssert.DoesNotContain(pack.RuntimeCapabilities.ToArray(), "DomSnapshotReadOnly");
    }

    [TestMethod]
    public void ChromeCdpExternalProbeRealChromeCdpRequiresNavigationEvidence()
    {
        var result = PassedProbeResult() with { NavigatedToAllowedTarget = false };
        var proof = new ChromeCdpExternalProofRunner(new FixedChromeProbe(result)).RunAsync(optIn: true).GetAwaiter().GetResult();

        Assert.AreEqual(ChromeCdpExternalProofStatus.FailedRuntime, proof.Status);
        Assert.AreNotEqual(NexaExternalProofProbeKind.RealChromeCdp, proof.EvidencePack.ProbeKind);
    }

    [TestMethod]
    public void ChromeCdpDomReadOnlyRequiresPageMetadata()
    {
        var result = PassedProbeResult() with { DomOrPageMetadataCaptured = false };
        var proof = new ChromeCdpExternalProofRunner(new FixedChromeProbe(result)).RunAsync(optIn: true).GetAwaiter().GetResult();

        Assert.AreEqual(ChromeCdpExternalProofStatus.FailedRuntime, proof.Status);
    }

    [TestMethod]
    public void ChromeCdpDomReadOnlyRejectsSubmitMutationLoginPayment()
    {
        var result = PassedProbeResult() with { SubmittedOrMutated = true };
        var proof = new ChromeCdpExternalProofRunner(new FixedChromeProbe(result)).RunAsync(optIn: true).GetAwaiter().GetResult();

        Assert.AreEqual(ChromeCdpExternalProofStatus.FailedRuntime, proof.Status);
        Assert.AreEqual(M65ClosureReadinessStatus.BlockedByPolicy, proof.M65Review.Status);
    }

    [TestMethod]
    public void ChromeCdpDomReadOnlyDoesNotPersistFullDom()
    {
        using var temp = new TempDirectory();
        var proof = new ChromeCdpExternalProofRunner(new FixedChromeProbe(PassedProbeResult()))
            .RunAsync(optIn: true, TestLedger(temp.Path))
            .GetAwaiter()
            .GetResult();

        Assert.AreEqual(ChromeCdpExternalProofStatus.PassedReadOnlyProof, proof.Status);
        Assert.IsNotNull(proof.ProbeResult?.Snapshot);
        Assert.IsFalse(proof.ProbeResult.Snapshot.FullDomPersisted);
        Assert.IsFalse(System.Text.Json.JsonSerializer.Serialize(proof.EvidencePack).Contains("<html", StringComparison.OrdinalIgnoreCase));
        Assert.IsFalse(System.Text.Json.JsonSerializer.Serialize(proof.EvidencePack).Contains("<body", StringComparison.OrdinalIgnoreCase));
    }

    [TestMethod]
    public void ChromeCdpExternalProbeRequiresTestOwnedAllowedHost()
    {
        var policy = ChromeCdpExternalProofRunner.Policy();

        CollectionAssert.Contains(policy.AllowedHosts.ToArray(), "lab.nodalos.com.ar");
        CollectionAssert.Contains(policy.AllowedPaths.ToArray(), "/document/");
        Assert.IsTrue(policy.RequireRealChromeCdpSession);
        Assert.IsTrue(policy.BlockSubmitMutationLoginPayment);
    }

    [TestMethod]
    public void ExternalChromeCdpDomProofWithoutOptInDoesNotRun()
    {
        var proof = new ChromeCdpExternalProofRunner(new FixedChromeProbe(PassedProbeResult()))
            .RunAsync(optIn: false)
            .GetAwaiter()
            .GetResult();

        Assert.AreEqual(ChromeCdpExternalProofStatus.SkippedNoOptIn, proof.Status);
        Assert.IsFalse(proof.ExecutedLiveCdp);
        Assert.AreEqual(NexaExternalProofProbeKind.ModeledFake, proof.EvidencePack.ProbeKind);
    }

    [TestMethod]
    public void ExternalChromeCdpDomProofUnavailableSkipsWithoutFalseSuccess()
    {
        var proof = new ChromeCdpExternalProofRunner()
            .RunAsync(optIn: true)
            .GetAwaiter()
            .GetResult();

        Assert.AreEqual(ChromeCdpExternalProofStatus.ChromeCdpUnavailable, proof.Status);
        Assert.IsFalse(proof.ExecutedLiveCdp);
        Assert.AreEqual(NexaExternalProofProbeKind.ModeledFake, proof.EvidencePack.ProbeKind);
    }

    [TestMethod]
    public void ExternalChromeCdpDomProofModelProbeCannotProduceRealChromeCdp()
    {
        var modeledResult = PassedProbeResult() with { IsRealChromeCdpSession = false };
        var proof = new ChromeCdpExternalProofRunner(new FixedChromeProbe(modeledResult))
            .RunAsync(optIn: true)
            .GetAwaiter()
            .GetResult();

        Assert.AreEqual(ChromeCdpExternalProofStatus.FailedRuntime, proof.Status);
        Assert.AreEqual(NexaExternalProofProbeKind.ModeledFake, proof.EvidencePack.ProbeKind);
    }

    [TestMethod]
    public void ExternalChromeCdpDomProofRealCdpOptInProducesRealChromeCdpWithLedgerRef()
    {
        using var temp = new TempDirectory();
        var proof = new ChromeCdpExternalProofRunner(new FixedChromeProbe(PassedProbeResult()))
            .RunAsync(optIn: true, TestLedger(temp.Path))
            .GetAwaiter()
            .GetResult();

        Assert.AreEqual(ChromeCdpExternalProofStatus.PassedReadOnlyProof, proof.Status);
        Assert.AreEqual(NexaExternalProofProbeKind.RealChromeCdp, proof.EvidencePack.ProbeKind);
        Assert.AreEqual("ChromeCdpExternalReadOnly", proof.EvidencePack.Tooling);
        CollectionAssert.Contains(proof.EvidencePack.RuntimeCapabilities.ToArray(), "BrowserNavigationReadOnly");
        CollectionAssert.Contains(proof.EvidencePack.RuntimeCapabilities.ToArray(), "DomSnapshotReadOnly");
        Assert.AreEqual(NexaExternalEvidencePersistenceStatus.PersistedRedactedLedger, proof.EvidencePack.PersistenceStatus);
        Assert.IsFalse(string.IsNullOrWhiteSpace(proof.EvidencePack.LedgerRef));
    }

    [TestMethod]
    public void ExternalChromeCdpDomProofBlockedRoutesArePolicyOnlyAndNotMutated()
    {
        var proof = new ChromeCdpExternalProofRunner(new FixedChromeProbe(PassedProbeResult()))
            .RunAsync(optIn: true)
            .GetAwaiter()
            .GetResult();

        CollectionAssert.Contains(proof.ProbeResult!.PolicyBlockedRoutes.ToArray(), "/blocked-login/");
        CollectionAssert.Contains(proof.ProbeResult.PolicyBlockedRoutes.ToArray(), "/blocked-checkout/");
        Assert.IsFalse(proof.ProbeResult.SubmittedOrMutated);
    }

    [TestMethod]
    public void ExternalChromeCdpDomProofSecretsCookiesTokensBlockCandidate()
    {
        var result = PassedProbeResult() with { SecretsCookiesTokensDetected = true };
        var proof = new ChromeCdpExternalProofRunner(new FixedChromeProbe(result))
            .RunAsync(optIn: true)
            .GetAwaiter()
            .GetResult();

        Assert.AreEqual(ChromeCdpExternalProofStatus.FailedRuntime, proof.Status);
        Assert.AreEqual(M65ClosureReadinessStatus.BlockedByPolicy, proof.M65Review.Status);
    }

    [TestMethod]
    public void M65ChromeCdpEvidenceReviewDeferredWithoutCdpProof()
    {
        var review = new M65DedicatedEvidenceReviewer().Review(new M65DedicatedEvidenceReviewInput(
            M51Closed: true,
            ScenarioPlanReady: true,
            NexaExternalProofProbeKind.RealHttpClient,
            "HttpReadOnlyExternal",
            LedgerRefPresent: true,
            TargetVerified: true,
            ReadOnlyProofPassed: true,
            ChromeCdpDomProofPassed: false,
            SecretsCookiesTokensDetected: false,
            SubmitMutationPaymentLoginDetected: false,
            PolicyViolationDetected: false,
            ScopeRequiresChromeCdpDomProof: true,
            PublicSaasEnabled: false,
            RealBillingEnabled: false,
            RealEmailEnabled: false,
            RealCredentialsEnabled: false,
            SensitiveSurfaceEnabled: false));

        Assert.AreEqual(M65ClosureReadinessStatus.RequiresChromeCdpDomProof, review.Status);
    }

    [TestMethod]
    public void M65ChromeCdpEvidenceReviewCandidateWithRealChromeCdpAndLedger()
    {
        using var temp = new TempDirectory();
        var proof = new ChromeCdpExternalProofRunner(new FixedChromeProbe(PassedProbeResult()))
            .RunAsync(optIn: true, TestLedger(temp.Path))
            .GetAwaiter()
            .GetResult();

        Assert.AreEqual(M65ClosureReadinessStatus.CandidateCloseM65, proof.M65Review.Status);
        Assert.IsTrue(proof.M65Review.CandidateCloseM65);
        Assert.IsTrue(proof.M65Review.PublicSaasStillDisabled);
        Assert.IsTrue(proof.M65Review.RealBillingStillDisabled);
        Assert.IsTrue(proof.M65Review.RealEmailStillDisabled);
        Assert.IsTrue(proof.M65Review.RealCredentialsStillBlocked);
        Assert.IsTrue(proof.M65Review.SensitiveSurfacesStillBlocked);
    }

    [TestMethod]
    public void M65ChromeCdpEvidenceReviewRejectsCdpProofWithoutLedger()
    {
        var proof = new ChromeCdpExternalProofRunner(new FixedChromeProbe(PassedProbeResult()))
            .RunAsync(optIn: true)
            .GetAwaiter()
            .GetResult();

        Assert.AreEqual(ChromeCdpExternalProofStatus.PassedReadOnlyProof, proof.Status);
        Assert.AreEqual(M65ClosureReadinessStatus.DeferredNeedsDedicatedEvidence, proof.M65Review.Status);
        Assert.IsTrue(proof.M65Review.ReasonCodes.Any(r => r.Contains("ledger", StringComparison.OrdinalIgnoreCase)));
    }

    [TestMethod]
    public void M65ChromeCdpEvidenceReviewReadinessDistinguishesHttpAndCdpProof()
    {
        var http = new NexaExternalReadOnlyEvidencePackBuilder().Build(
            new NexaExternalProofHarness().Evaluate(HarnessRequest(), DateTimeOffset.UtcNow),
            HarnessRequest(),
            runtimeExecuted: true,
            runtimePassed: true,
            NexaExternalProofProbeKind.RealHttpClient);
        var cdp = new NexaExternalReadOnlyEvidencePackBuilder().Build(
            new NexaExternalProofHarness().Evaluate(HarnessRequest(), DateTimeOffset.UtcNow),
            HarnessRequest(),
            runtimeExecuted: true,
            runtimePassed: true,
            NexaExternalProofProbeKind.RealChromeCdp);

        Assert.AreEqual("HttpReadOnlyExternal", http.Tooling);
        Assert.AreEqual("ChromeCdpExternalReadOnly", cdp.Tooling);
        CollectionAssert.Contains(cdp.RuntimeCapabilities.ToArray(), "PageMetadataReadOnly");
        CollectionAssert.DoesNotContain(http.RuntimeCapabilities.ToArray(), "PageMetadataReadOnly");
    }

    private static NexaExternalProofHarnessRequest HarnessRequest() =>
        new(
            OptInEnabled: true,
            NexaFirstReadOnlyLiveProofRunner.CreateLiveTarget(),
            "lab.nodalos.com.ar",
            "/",
            "GET",
            WouldCaptureBodies: false,
            WouldCaptureSensitiveHeaderValues: false,
            WouldPersistCookies: false,
            WouldSubmit: false,
            "operator-test");

    private static ChromeCdpExternalPreflightRequest Preflight(
        string? browserPath = "C:\\Windows\\System32\\cmd.exe",
        bool optIn = true,
        bool usesPersonalProfile = false,
        string profileDirectory = "C:\\Temp\\nodal-os-isolated-cdp-profile",
        string targetHost = "lab.nodalos.com.ar") =>
        new(
            optIn,
            browserPath,
            profileDirectory,
            usesPersonalProfile,
            UsesDefaultUserDataDir: false,
            CookiesPersisted: false,
            CredentialsAvailable: false,
            PersonalExtensionsEnabled: false,
            SavedPasswordsAvailable: false,
            CdpSessionControlled: true,
            targetHost,
            ReadOnlyOnly: true);

    private static ChromeCdpExternalProbeResult PassedProbeResult() =>
        new(
            ChromeCdpExternalProbeStatus.PassedReadOnlyDomProof,
            IsRealChromeCdpSession: true,
            NavigatedToAllowedTarget: true,
            DomOrPageMetadataCaptured: true,
            BrowserVersion: "Chrome/fixture-cdp",
            TargetUrl: "https://lab.nodalos.com.ar/",
            RoutesVisited: ["/", "/health/", "/ownership/", "/products/", "/document/", "/report/"],
            PolicyBlockedRoutes: ["/disabled-form/", "/blocked-login/", "/blocked-checkout/", "/blocked-destructive-action/"],
            new ChromeCdpDomReadOnlySnapshot(
                "https://lab.nodalos.com.ar/document/",
                "NODAL OS Test-Owned Target",
                ["project", "purpose", "owner", "environment"],
                ElementCount: 42,
                FullDomPersisted: false,
                ContainsSensitiveMaterial: false,
                Redacted: true),
            SubmittedOrMutated: false,
            UsedCredentials: false,
            UsedLoginOrPayment: false,
            PersistedCookies: false,
            CapturedSensitiveHeaderValues: false,
            PersistedFullDomOrBody: false,
            SecretsCookiesTokensDetected: false,
            EvidenceRefs: ["cdp:navigation:redacted", "cdp:dom-metadata:redacted"],
            ReasonCodes: ["Chrome/CDP read-only DOM proof passed"],
            Redacted: true);

    private static BrowserPersistentAuditLedger TestLedger(string path) =>
        new(
            new BrowserAuditLedgerPolicy(path, AllowFilePersistence: true, RedactBeforePersist: true, new BrowserAuditLedgerRetentionPolicy(null, null, DeleteOnCleanup: true)),
            BrowserAuditLedgerHmacIntegrityProvider.CreateDevFixtureProvider("nodal-m103-explicit-test-fixture-hmac-key"));

    private sealed class FixedChromeProbe(ChromeCdpExternalProbeResult result) : INexaChromeCdpExternalProbe
    {
        public bool IsAvailable => true;

        public Task<ChromeCdpExternalProbeResult> ProbeReadOnlyAsync(
            ChromeCdpReadOnlyEvidencePolicy policy,
            NexaExternalTestOwnedTarget target,
            IReadOnlyList<string> allowedRoutes,
            IReadOnlyList<string> blockedRoutes,
            CancellationToken cancellationToken) =>
            Task.FromResult(result with
            {
                RoutesVisited = result.RoutesVisited.Count == 0 ? allowedRoutes : result.RoutesVisited,
                PolicyBlockedRoutes = result.PolicyBlockedRoutes.Count == 0 ? blockedRoutes : result.PolicyBlockedRoutes
            });
    }

    private sealed class TempDirectory : IDisposable
    {
        public string Path { get; } = System.IO.Path.Combine(System.IO.Path.GetTempPath(), $"nodal-m103-{Guid.NewGuid():N}");

        public TempDirectory() => Directory.CreateDirectory(Path);

        public void Dispose()
        {
            if (Directory.Exists(Path))
                Directory.Delete(Path, recursive: true);
        }
    }
}
