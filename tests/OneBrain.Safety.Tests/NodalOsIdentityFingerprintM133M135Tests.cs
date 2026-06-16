using System.Text.Json;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using OneBrain.BrowserExecutor.Cdp;
using OneBrain.BrowserExecutor.Contracts;

namespace OneBrain.Safety.Tests;

[TestClass]
public sealed class NodalOsIdentityFingerprintM133M135Tests
{
    [TestMethod]
    public void IdentityFingerprintV2FixtureValidProducesVerifiedFixture()
    {
        var proof = Evaluate("local-admin-surface");

        Assert.AreEqual(NodalOsIdentityConfidence.VerifiedFixture, proof.Fingerprint.Confidence);
        Assert.AreEqual(NodalOsIdentityRecommendedDecision.AllowReadOnlyLocalObservation, proof.RecommendedDecision);
        Assert.IsTrue(proof.Fingerprint.CoreAuthorityRequired);
        Assert.IsFalse(proof.ActionAuthorityGranted);
    }

    [TestMethod]
    public void SurfaceIdentityTargetOwnedLabProducesVerifiedTargetOwned()
    {
        var proof = Evaluate("target-owned-lab-metadata");

        Assert.AreEqual(NodalOsIdentityConfidence.VerifiedTargetOwned, proof.Fingerprint.Confidence);
        Assert.AreEqual("lab.nodalos.com.ar", proof.Fingerprint.Surface.Host);
        Assert.IsFalse(proof.ActionAuthorityGranted);
    }

    [TestMethod]
    public void IdentityFingerprintV2HostMismatchBlocksAsAmbiguousIdentity()
    {
        var proof = Evaluate("mismatched-host");

        Assert.IsTrue(proof.Fingerprint.MismatchReasons.Contains(NodalOsIdentityMismatchReason.HostMismatch));
        Assert.AreEqual(NodalOsIdentityRecommendedDecision.BlockUntilIdentityVerified, proof.RecommendedDecision);
        Assert.IsFalse(proof.ActionAuthorityGranted);
    }

    [TestMethod]
    public void IdentityFingerprintV2MissingSignalYieldsInsufficientEvidence()
    {
        var proof = Evaluate("missing-evidence");

        Assert.IsTrue(proof.Fingerprint.MismatchReasons.Contains(NodalOsIdentityMismatchReason.MissingRequiredSignal));
        Assert.IsTrue(proof.Fingerprint.MismatchReasons.Contains(NodalOsIdentityMismatchReason.InsufficientEvidence));
        Assert.AreEqual(NodalOsIdentityConfidence.Low, proof.Fingerprint.Confidence);
    }

    [TestMethod]
    public void IdentityFingerprintV2SensitiveSurfaceIsBlocked()
    {
        var proof = Evaluate("sensitive-blocked-surface");

        Assert.IsTrue(proof.Fingerprint.MismatchReasons.Contains(NodalOsIdentityMismatchReason.SensitiveSurface));
        Assert.AreEqual(NodalOsIdentityRecommendedDecision.BlockSensitiveSurface, proof.RecommendedDecision);
    }

    [TestMethod]
    public void IdentityFingerprintV2NeverGrantsActionAuthority()
    {
        foreach (var fixture in NodalOsIdentityFixtureHarness.CreateDefaultFixtures())
        {
            var proof = new NodalOsIdentityFingerprintEvaluator().Evaluate(fixture);
            Assert.IsFalse(proof.ActionAuthorityGranted, fixture.FixtureId);
            Assert.IsFalse(proof.Fingerprint.GrantsActionAuthority, fixture.FixtureId);
            Assert.IsTrue(proof.Fingerprint.CoreAuthorityRequired, fixture.FixtureId);
        }
    }

    [TestMethod]
    public void IdentityFixtureHarnessValidatesMatchMismatchStaleAmbiguousAndSensitive()
    {
        var report = new NodalOsIdentityFixtureHarness().RunDefaultFixtures();

        Assert.IsTrue(report.IdentityFixtureReadiness);
        Assert.IsTrue(report.IdentityBlocked);
        Assert.IsTrue(report.CoreAuthorityRequired);
        Assert.IsFalse(report.ActionAuthorityGranted);
        Assert.IsTrue(report.Proofs.Any(p => p.Fingerprint.MismatchReasons.Contains(NodalOsIdentityMismatchReason.StaleFingerprint)));
        Assert.IsTrue(report.Proofs.Any(p => p.Fingerprint.MismatchReasons.Contains(NodalOsIdentityMismatchReason.AmbiguousIdentity)));
        Assert.IsTrue(report.Proofs.Any(p => p.Fingerprint.MismatchReasons.Contains(NodalOsIdentityMismatchReason.SensitiveSurface)));
    }

    [TestMethod]
    public void WindowIdentityAndSurfaceIdentityContractsCarryRedactedEvidenceRefs()
    {
        var window = new NodalOsWindowIdentity(
            "window-local-admin",
            "nodal os admin",
            "local-fixture",
            "nodal-os-local-admin",
            ["identity:window-local-admin:redacted"]);

        Assert.AreEqual("local-fixture", window.RuntimeProvider);
        Assert.IsTrue(window.EvidenceRefs.All(r => r.Contains("redacted", StringComparison.Ordinal)));
    }

    [TestMethod]
    public void IdentityEvidenceSummaryDoesNotExposeSecretsCookiesTokensOrBodies()
    {
        var proof = Evaluate("target-owned-lab-metadata");
        var summary = new NodalOsIdentityPrivatePreviewIntegrationService().BuildEvidenceSummary(proof);
        var json = JsonSerializer.Serialize(summary);

        Assert.IsTrue(summary.Redacted);
        StringAssert.Contains(summary.RedactionSummary, "no credentials, cookies, tokens, bodies, full DOM");
        Assert.IsFalse(json.Contains("synthetic-cookie-session-value", StringComparison.Ordinal));
        Assert.IsFalse(json.Contains("synthetic-api-key-value", StringComparison.Ordinal));
        Assert.IsFalse(json.Contains("synthetic-bearer-token", StringComparison.Ordinal));
        Assert.IsFalse(json.Contains("<html", StringComparison.OrdinalIgnoreCase));
    }

    [TestMethod]
    public void OperatorUxReadinessShowsIdentityConfidenceAndWarnings()
    {
        var summary = new NodalOsOperatorUxReadinessService().BuildDefaultSummary();

        Assert.IsTrue(summary.EvidenceSummary.Any(e => e.Contains("Identity/Fingerprint v2", StringComparison.Ordinal)));
        Assert.IsTrue(summary.EvidenceSummary.Any(e => e.Contains("Core authority still required", StringComparison.Ordinal)));
    }

    [TestMethod]
    public void ProductAdminPrivatePreviewIncludesIdentityFixtureReadinessSignal()
    {
        var report = new NodalOsProductAdminPrivatePreviewHardeningService().BuildDefaultReport();

        Assert.IsTrue(report.EvidenceRefs.Any(e => e.Contains("identity:fingerprint-v2:fixture-ready", StringComparison.Ordinal)));
        Assert.IsTrue(report.CoreAuthorityRequired);
        Assert.IsTrue(report.UiAdminAuthorityBlocked);
    }

    [TestMethod]
    public void LocalPrivatePreviewReleaseGateDoesNotBecomePermissiveFromHighIdentityConfidence()
    {
        var decision = new NodalOsLocalPrivatePreviewReleaseGate().Evaluate(
            NodalOsRuntimeStateProbe.ForCurrentLocalPreview());

        Assert.AreEqual(NodalOsLocalPrivatePreviewReleaseGateStatus.ReadyWithRestrictions, decision.Status);
        Assert.IsFalse(decision.ExternalGeneralReady);
        Assert.IsTrue(decision.SubmitPaySignDeleteStillBlocked);
        Assert.IsTrue(decision.RealCredentialsStillBlocked);
    }

    [TestMethod]
    public void IdentityMismatchCanGenerateOperatorWarningIssue()
    {
        var proof = Evaluate("ambiguous-window");
        var issue = new NodalOsPrivatePreviewIssue(
            "pp-identity-001",
            NodalOsPrivatePreviewIssueCategory.ReadinessMismatch,
            NodalOsPrivatePreviewIssueSeverity.Medium,
            NodalOsPrivatePreviewIssueDecision.ShouldFixSoon,
            "Identity/Fingerprint v2 detected ambiguous local fixture identity",
            BlocksPostRunGo: false,
            Redacted: true);

        Assert.IsTrue(proof.Fingerprint.MismatchReasons.Contains(NodalOsIdentityMismatchReason.AmbiguousIdentity));
        Assert.AreEqual(NodalOsPrivatePreviewIssueCategory.ReadinessMismatch, issue.Category);
        Assert.IsFalse(issue.BlocksPostRunGo);
    }

    [TestMethod]
    public void IdentityFingerprintV2AdrExists()
    {
        var text = File.ReadAllText(SourcePath("docs", "adr", "identity-fingerprint-v2-m133-m135.md"));

        StringAssert.Contains(text, "Identity/Fingerprint v2");
        StringAssert.Contains(text, "does not authorize actions");
        StringAssert.Contains(text, "M136-M138");
    }

    [TestMethod]
    public void Hito162ReplacementSequenceShowsM133M135ImplementedWithoutScopeExpansion()
    {
        var text = File.ReadAllText(SourcePath("docs", "roadmap", "nodal-os-hito-162-replacement-sequence.md"));

        StringAssert.Contains(text, "Status after M133-M135: implemented");
        StringAssert.Contains(text, "does not authorize actions");
        StringAssert.Contains(text, "No SaaS public");
        StringAssert.Contains(text, "No external CDP general-ready claim");
    }

    private static NodalOsIdentityFingerprintV2 Evaluate(string fixtureId)
    {
        var fixture = NodalOsIdentityFixtureHarness.CreateDefaultFixtures()
            .Single(f => f.FixtureId == fixtureId);
        return new NodalOsIdentityFingerprintEvaluator().Evaluate(fixture);
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
