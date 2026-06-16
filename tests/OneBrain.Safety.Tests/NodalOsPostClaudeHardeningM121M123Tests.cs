using Microsoft.VisualStudio.TestTools.UnitTesting;
using OneBrain.BrowserExecutor.Cdp;
using OneBrain.BrowserExecutor.Contracts;

namespace OneBrain.Safety.Tests;

[TestClass]
public sealed class NodalOsPostClaudeHardeningM121M123Tests
{
    [TestMethod]
    public void ReleaseGateStateProbeDangerousServiceEnabledBlocksSecurity()
    {
        var snapshot = SafeSnapshot() with { RealCredentialsEnabled = true };
        var decision = new NodalOsLocalPrivatePreviewReleaseGate().Evaluate(new NodalOsRuntimeStateProbe(snapshot));

        Assert.AreEqual(NodalOsLocalPrivatePreviewReleaseGateStatus.BlockedBySecurity, decision.Status);
        Assert.IsFalse(decision.ReadyWithRestrictions);
    }

    [TestMethod]
    public void ReleaseGateStateProbeExternalGeneralBlocksExternalGeneral()
    {
        var snapshot = SafeSnapshot() with { ExternalGeneralReady = true };
        var decision = new NodalOsLocalPrivatePreviewReleaseGate().Evaluate(new NodalOsRuntimeStateProbe(snapshot));

        Assert.AreEqual(NodalOsLocalPrivatePreviewReleaseGateStatus.BlockedByExternalGeneral, decision.Status);
    }

    [TestMethod]
    public void ReleaseGateStateProbeWorktreeMismatchBlocksWorktree()
    {
        var snapshot = SafeSnapshot() with { WorktreeCanonical = false };
        var decision = new NodalOsLocalPrivatePreviewReleaseGate().Evaluate(new NodalOsRuntimeStateProbe(snapshot));

        Assert.AreEqual(NodalOsLocalPrivatePreviewReleaseGateStatus.BlockedByWorktree, decision.Status);
    }

    [TestMethod]
    public void ReleaseGateStateProbeMissingEvidenceBlocksMissingEvidence()
    {
        var snapshot = SafeSnapshot() with { M65EvidenceAvailable = false };
        var decision = new NodalOsLocalPrivatePreviewReleaseGate().Evaluate(new NodalOsRuntimeStateProbe(snapshot));

        Assert.AreEqual(NodalOsLocalPrivatePreviewReleaseGateStatus.BlockedByMissingEvidence, decision.Status);
    }

    [TestMethod]
    public void ReleaseGateStateProbeSafeFixtureIsReadyWithRestrictions()
    {
        var decision = new NodalOsLocalPrivatePreviewReleaseGate().Evaluate(NodalOsRuntimeStateProbe.ForCurrentLocalPreview());

        Assert.AreEqual(NodalOsLocalPrivatePreviewReleaseGateStatus.ReadyWithRestrictions, decision.Status);
        Assert.IsTrue(decision.ReadyWithRestrictions);
    }

    [TestMethod]
    public void RuntimeStateProbeSafeFixtureReportsDangerousSurfacesDisabled()
    {
        var snapshot = NodalOsRuntimeStateProbe.ForCurrentLocalPreview().Probe();

        Assert.IsFalse(snapshot.PublicSaasEnabled);
        Assert.IsFalse(snapshot.PublicApiEnabled);
        Assert.IsFalse(snapshot.RealCredentialsEnabled);
        Assert.IsFalse(snapshot.ExternalGeneralReady);
    }

    [TestMethod]
    public void ReleaseGateNoLongerExposesSafeInputAsProductSource()
    {
        var method = typeof(NodalOsLocalPrivatePreviewReleaseGate).GetMethod("SafeInput");

        Assert.IsNull(method);
    }

    [TestMethod]
    public void EvidenceLedgerVerifierValidLedgerPasses()
    {
        using var fixture = CreateLedgerFixture();
        var appended = AppendProofEvent(fixture.Ledger, NexaExternalProofProbeKind.RealChromeCdp, "ChromeCdpExternalReadOnly");
        var result = new NodalOsEvidenceLedgerVerifier().Verify(
            Request(appended, NexaExternalProofProbeKind.RealChromeCdp, "target-owned Chrome/CDP/DOM read-only"),
            fixture.Ledger.Events);

        Assert.AreEqual(NodalOsEvidenceLedgerVerificationStatus.Verified, result.Status);
        Assert.IsTrue(result.Verified);
    }

    [TestMethod]
    public void LedgerLiveVerificationValidM65LedgerPasses()
    {
        using var fixture = CreateLedgerFixture();
        var appended = AppendProofEvent(fixture.Ledger, NexaExternalProofProbeKind.RealChromeCdp, "ChromeCdpExternalReadOnly");
        var result = new NodalOsEvidenceLedgerVerifier().Verify(
            Request(appended, NexaExternalProofProbeKind.RealChromeCdp, "target-owned Chrome/CDP/DOM read-only"),
            fixture.Ledger.Events);

        Assert.IsTrue(result.Verified);
    }

    [TestMethod]
    public void EvidenceLedgerVerifierMissingLedgerRefFails()
    {
        using var fixture = CreateLedgerFixture();
        var result = new NodalOsEvidenceLedgerVerifier().Verify(
            new NodalOsEvidenceLedgerVerificationRequest(
                "audit-ledger-missing",
                "missing-hash",
                NexaExternalEvidencePersistenceStatus.PersistedRedactedLedger,
                "target-owned Chrome/CDP/DOM read-only",
                NexaExternalProofProbeKind.RealChromeCdp),
            fixture.Ledger.Events);

        Assert.AreEqual(NodalOsEvidenceLedgerVerificationStatus.MissingLedgerRef, result.Status);
        Assert.IsFalse(result.Verified);
    }

    [TestMethod]
    public void EvidenceLedgerVerifierHashMismatchFails()
    {
        using var fixture = CreateLedgerFixture();
        var appended = AppendProofEvent(fixture.Ledger, NexaExternalProofProbeKind.RealChromeCdp, "ChromeCdpExternalReadOnly");
        var request = Request(appended, NexaExternalProofProbeKind.RealChromeCdp, "target-owned Chrome/CDP/DOM read-only") with
        {
            ExpectedLedgerHash = "bad-hash"
        };
        var result = new NodalOsEvidenceLedgerVerifier().Verify(request, fixture.Ledger.Events);

        Assert.AreEqual(NodalOsEvidenceLedgerVerificationStatus.LedgerHashMismatch, result.Status);
    }

    [TestMethod]
    public void EvidenceLedgerVerifierBlocksSyntheticSecretInLedger()
    {
        using var fixture = CreateLedgerFixture();
        var unsafeEvent = BrowserPersistentAuditLedger.Create(
            BrowserAuditLedgerEventKind.NetworkCaptureRecorded,
            "run",
            "action",
            "corr",
            "profile",
            "session",
            null,
            null,
            null,
            "PassedReadOnlyProofPersisted",
            "redacted external proof",
            new Dictionary<string, string>
            {
                ["probeKind"] = NexaExternalProofProbeKind.RealChromeCdp.ToString(),
                ["tooling"] = "ChromeCdpExternalReadOnly",
                ["scope"] = "target-owned Chrome/CDP/DOM read-only",
                ["body"] = "<html>complete body</html>"
            });
        var appended = fixture.Ledger.Append(unsafeEvent);
        var result = new NodalOsEvidenceLedgerVerifier().Verify(
            Request(appended, NexaExternalProofProbeKind.RealChromeCdp, "target-owned Chrome/CDP/DOM read-only"),
            fixture.Ledger.Events);

        Assert.AreEqual(NodalOsEvidenceLedgerVerificationStatus.UnsafeLedgerContent, result.Status);
    }

    [TestMethod]
    public void M65FormalClosureRequiresVerifiedLedgerNotJustChromeFlag()
    {
        var candidate = new M65DedicatedEvidenceReviewer().Review(new M65DedicatedEvidenceReviewInput(
            M51Closed: true,
            ScenarioPlanReady: true,
            NexaExternalProofProbeKind.RealChromeCdp,
            "ChromeCdpExternalReadOnly",
            LedgerRefPresent: true,
            TargetVerified: true,
            ReadOnlyProofPassed: true,
            ChromeCdpDomProofPassed: true,
            SecretsCookiesTokensDetected: false,
            SubmitMutationPaymentLoginDetected: false,
            PolicyViolationDetected: false,
            ScopeRequiresChromeCdpDomProof: true,
            PublicSaasEnabled: false,
            RealBillingEnabled: false,
            RealEmailEnabled: false,
            RealCredentialsEnabled: false,
            SensitiveSurfaceEnabled: false));

        var review = new M65FormalClosureReviewService().Review(new M65FormalClosureReviewInput(
            candidate,
            "https://lab.nodalos.com.ar",
            NexaExternalProofProbeKind.RealChromeCdp,
            "ChromeCdpExternalReadOnly",
            ["BrowserNavigationReadOnly", "DomSnapshotReadOnly", "PageMetadataReadOnly", "NetworkMetadataRedacted", "CoreGoverned"],
            LedgerPersisted: true,
            "audit-ledger-present-but-unverified",
            "hash-present-but-unverified",
            IsolatedProfile: true,
            NoSecretsCookiesTokens: true,
            NoFullDomOrBodyPersisted: true,
            NoSubmitMutationPaymentLogin: true,
            BlockedRoutesPolicyVerified: true,
            PublicSaasEnabled: false,
            PublicApiEnabled: false,
            RealBillingEnabled: false,
            RealEmailEnabled: false,
            RealCredentialsEnabled: false,
            SensitiveSitesEnabled: false,
            GeneralExternalCdpRequested: false,
            LedgerVerified: false));

        Assert.AreEqual(M65FormalClosureDecision.NeedsAdditionalEvidence, review.Decision);
        Assert.IsTrue(review.ReasonCodes.Any(r => r.Contains("verified", StringComparison.OrdinalIgnoreCase)));
    }

    [TestMethod]
    public void EvidenceFreezeFailsWhenLedgerVerificationMissing()
    {
        var snapshot = NodalOsPrivatePreviewEvidenceFreezeService.DefaultSnapshot("1e38a4a84ab6c4d58a9f7b7896a019980fa71b14") with
        {
            EvidenceLedgerVerified = false
        };
        var result = new NodalOsPrivatePreviewEvidenceFreezeService().Freeze(snapshot);

        Assert.AreEqual(NodalOsPrivatePreviewEvidenceFreezeStatus.EvidenceMissing, result.Status);
    }

    [TestMethod]
    public void SkippedTestsCategoryAuditPassesExpectedCategories()
    {
        var result = new NexaSkippedTestsCategoryAuditor().Audit(new NexaSkippedTestsAuditReporter().CreateReport());

        Assert.IsTrue(result.Passed);
        Assert.AreEqual(29, result.ActualCount);
        CollectionAssert.Contains(result.ActualCategories.ToArray(), NexaSkippedTestCategory.ExternalTargetBlocked);
        CollectionAssert.Contains(result.ActualCategories.ToArray(), NexaSkippedTestCategory.CdpLiveOptIn);
    }

    [TestMethod]
    public void SkippedTestsCategoryAuditFailsLocalPrivatePreviewSkip()
    {
        var report = new NexaSkippedTestsAuditReporter().CreateReport();
        var mutated = report with
        {
            Items = report.Items.Select((item, index) => index == 0 ? item with { BlocksLocalPrivatePreview = true } : item).ToArray()
        };
        var result = new NexaSkippedTestsCategoryAuditor().Audit(mutated);

        Assert.IsFalse(result.Passed);
        Assert.IsTrue(result.ReasonCodes.Any(r => r.Contains("local/private preview", StringComparison.OrdinalIgnoreCase)));
    }

    [TestMethod]
    public void SkippedTestsCategoryAuditFailsCountCorrectButCategoriesWrong()
    {
        var report = new NexaSkippedTestsAuditReporter().CreateReport();
        var mutated = report with
        {
            Items = report.Items.Select((item, index) => index == 0 ? item with { Category = NexaSkippedTestCategory.Other } : item).ToArray()
        };
        var result = new NexaSkippedTestsCategoryAuditor().Audit(mutated);

        Assert.IsFalse(result.Passed);
        Assert.AreEqual(29, result.ActualCount);
        Assert.IsTrue(result.ReasonCodes.Any(r => r.Contains("unexpected skipped categories", StringComparison.OrdinalIgnoreCase)));
    }

    [TestMethod]
    public void PostClaudeAdrExistsAndKeepsReadyWithRestrictions()
    {
        var text = File.ReadAllText(SourcePath("docs", "adr", "post-claude-private-preview-hardening-m121-m123.md"));

        StringAssert.Contains(text, "Claude");
        StringAssert.Contains(text, "ReadyWithRestrictions");
        StringAssert.Contains(text, "No scope expansion");
        StringAssert.Contains(text, "ledger");
        StringAssert.Contains(text, "skipped");
    }

    private static NodalOsReleaseGateStateSnapshot SafeSnapshot() =>
        NodalOsRuntimeStateProbe.ForCurrentLocalPreview().Probe();

    private static BrowserAuditLedgerEvent AppendProofEvent(
        BrowserPersistentAuditLedger ledger,
        NexaExternalProofProbeKind probeKind,
        string tooling) =>
        ledger.Append(BrowserPersistentAuditLedger.Create(
            BrowserAuditLedgerEventKind.NetworkCaptureRecorded,
            "run",
            "action",
            "corr",
            "profile",
            "session",
            null,
            null,
            null,
            "PassedReadOnlyProofPersisted",
            "redacted external proof; response body not persisted",
            new Dictionary<string, string>
            {
                ["proofId"] = "proof-redacted",
                ["probeKind"] = probeKind.ToString(),
                ["tooling"] = tooling,
                ["scope"] = "target-owned Chrome/CDP/DOM read-only",
                ["bodyPolicy"] = "body transiently scanned and not persisted"
            }));

    private static NodalOsEvidenceLedgerVerificationRequest Request(
        BrowserAuditLedgerEvent ledgerEvent,
        NexaExternalProofProbeKind probeKind,
        string scope) =>
        new(
            ledgerEvent.EventId,
            ledgerEvent.Integrity.EventHash,
            NexaExternalEvidencePersistenceStatus.PersistedRedactedLedger,
            scope,
            probeKind);

    private static LedgerFixture CreateLedgerFixture() =>
        new();

    private sealed class LedgerFixture : IDisposable
    {
        private readonly string _directory = Path.Combine(Path.GetTempPath(), $"nodal-os-ledger-{Guid.NewGuid():N}");

        public LedgerFixture()
        {
            Ledger = new BrowserPersistentAuditLedger(
                new BrowserAuditLedgerPolicy(_directory, AllowFilePersistence: false, RedactBeforePersist: true, new BrowserAuditLedgerRetentionPolicy(null, null, DeleteOnCleanup: true)),
                BrowserAuditLedgerHmacIntegrityProvider.CreateDevFixtureProvider("0123456789abcdef0123456789abcdef"));
        }

        public BrowserPersistentAuditLedger Ledger { get; }

        public void Dispose()
        {
            if (Directory.Exists(_directory))
                Directory.Delete(_directory, recursive: true);
        }
    }

    private static string SourcePath(params string[] relativePath)
    {
        var current = new DirectoryInfo(AppContext.BaseDirectory);
        while (current is not null)
        {
            if (File.Exists(Path.Combine(current.FullName, "OneBrain.slnx")))
                return Path.Combine(new[] { current.FullName }.Concat(relativePath).ToArray());
            current = current.Parent;
        }

        Assert.Fail("Could not locate repository root.");
        return "";
    }
}
