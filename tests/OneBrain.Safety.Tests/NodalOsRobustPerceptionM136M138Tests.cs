using System.Text.Json;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using OneBrain.BrowserExecutor.Cdp;
using OneBrain.BrowserExecutor.Contracts;

namespace OneBrain.Safety.Tests;

[TestClass]
public sealed class NodalOsRobustPerceptionM136M138Tests
{
    [TestMethod]
    public void WindowLivenessAliveStableFixtureProducesUsableReadiness()
    {
        var result = Evaluate("alive-stable");

        Assert.AreEqual(NodalOsWindowLivenessState.Responding, result.Evaluation.LivenessEvidence.LivenessState);
        Assert.AreEqual(NodalOsSurfaceStabilityState.Stable, result.Evaluation.LivenessEvidence.StabilityState);
        Assert.AreEqual(NodalOsPerceptionReadiness.UsableForReadOnlyContext, result.Evaluation.Readiness);
        Assert.IsFalse(result.Evaluation.ActionAuthorityGranted);
    }

    [TestMethod]
    public void WindowLivenessFrozenFixtureIsBlocked()
    {
        var result = Evaluate("frozen-window");

        Assert.AreEqual(NodalOsWindowLivenessState.Frozen, result.Evaluation.LivenessEvidence.LivenessState);
        Assert.AreEqual(NodalOsPerceptionReadiness.Blocked, result.Evaluation.Readiness);
        Assert.IsTrue(result.Evaluation.BlocksSensitiveActions);
    }

    [TestMethod]
    public void WindowLivenessStaleFixtureIsBlockedOrWarning()
    {
        var result = Evaluate("stale-window");

        Assert.IsTrue(result.Evaluation.LivenessEvidence.MismatchReasons.Contains(NodalOsPerceptionMismatchReason.WindowNotResponding));
        Assert.IsTrue(result.Evaluation.LivenessEvidence.MismatchReasons.Contains(NodalOsPerceptionMismatchReason.SurfaceUnstable) ||
            result.Evaluation.IdentityProof.Fingerprint.MismatchReasons.Contains(NodalOsIdentityMismatchReason.StaleFingerprint));
        Assert.AreNotEqual(NodalOsPerceptionReadiness.UsableForReadOnlyContext, result.Evaluation.Readiness);
    }

    [TestMethod]
    public void WindowLivenessMinimizedHiddenOccludedProduceWarningOrBlock()
    {
        foreach (var fixtureId in new[] { "minimized-window", "hidden-window", "occluded-window" })
        {
            var result = Evaluate(fixtureId);

            Assert.IsTrue(result.Evaluation.Readiness is NodalOsPerceptionReadiness.WarningRequiresCoreReview or NodalOsPerceptionReadiness.Blocked, fixtureId);
            Assert.IsFalse(result.Evaluation.ActionAuthorityGranted, fixtureId);
        }
    }

    [TestMethod]
    public void SurfaceStabilityUnsafeSurfaceIsBlocked()
    {
        var result = Evaluate("sensitive-surface");

        Assert.AreEqual(NodalOsPerceptionReadiness.Unsafe, result.Evaluation.Readiness);
        Assert.IsTrue(result.Evaluation.LivenessEvidence.MismatchReasons.Contains(NodalOsPerceptionMismatchReason.SensitiveSurface));
        Assert.IsFalse(result.Evaluation.ActionAuthorityGranted);
    }

    [TestMethod]
    public void RobustPerceptionHighConfidenceDoesNotGrantActionAuthority()
    {
        foreach (var result in new NodalOsRobustPerceptionFixtureHarness().RunDefaultFixtures())
        {
            Assert.IsFalse(result.Evaluation.ActionAuthorityGranted, result.Fixture.FixtureId);
            Assert.IsFalse(result.Evaluation.LivenessEvidence.ActionAuthorityGranted, result.Fixture.FixtureId);
            Assert.IsTrue(result.Evaluation.CoreAuthorityRequired, result.Fixture.FixtureId);
        }
    }

    [TestMethod]
    public void OverlayDetectionSystemOverlayProducesBlockedExplanation()
    {
        var result = Evaluate("system-overlay");

        Assert.IsTrue(result.Overlay.OverlayDetected);
        Assert.AreEqual(NodalOsBlockedSurfaceReason.SystemModalOverlay, result.Overlay.Reason);
        StringAssert.Contains(result.Explanation.Cause, "SystemModalOverlay");
        Assert.IsFalse(result.Explanation.ActionAuthorityGranted);
    }

    [TestMethod]
    public void OverlayDetectionPermissionOverlayRequiresHumanIntervention()
    {
        var result = Evaluate("permission-overlay");

        Assert.AreEqual(NodalOsBlockedSurfaceReason.PermissionOverlay, result.Overlay.Reason);
        StringAssert.Contains(result.Explanation.UserExpectedAction, "Operator must resolve");
        StringAssert.Contains(result.Explanation.RecommendedNextStep, "Do not proceed");
    }

    [TestMethod]
    public void EmptySurfaceDetectionEmptyUiaAndDomProduceInsufficientPerception()
    {
        var uia = Evaluate("empty-uia");
        var dom = Evaluate("empty-dom");

        Assert.AreEqual(NodalOsBlockedSurfaceReason.EmptyUiaTree, uia.Empty.Reason);
        Assert.AreEqual(NodalOsBlockedSurfaceReason.EmptyDomMetadata, dom.Empty.Reason);
        Assert.IsTrue(uia.Empty.BlocksPerception);
        Assert.IsTrue(dom.Empty.BlocksPerception);
    }

    [TestMethod]
    public void EmptySurfaceDetectionTruncatedSurfaceProducesWarningOrBlock()
    {
        var result = Evaluate("truncated-surface");

        Assert.AreEqual(NodalOsBlockedSurfaceReason.TruncatedSurface, result.Empty.Reason);
        Assert.IsTrue(result.Evaluation.LivenessEvidence.MismatchReasons.Contains(NodalOsPerceptionMismatchReason.SurfaceTruncated));
        Assert.AreNotEqual(NodalOsPerceptionReadiness.UsableForReadOnlyContext, result.Evaluation.Readiness);
    }

    [TestMethod]
    public void PerceptionBlockerExplanationDoesNotExposeSecretsCookiesTokens()
    {
        var result = Evaluate("system-overlay");
        var json = JsonSerializer.Serialize(result.Explanation);

        Assert.IsTrue(result.Explanation.Redacted);
        Assert.IsFalse(json.Contains("synthetic-cookie-session-value", StringComparison.Ordinal));
        Assert.IsFalse(json.Contains("synthetic-api-key-value", StringComparison.Ordinal));
        Assert.IsFalse(json.Contains("synthetic-bearer-token", StringComparison.Ordinal));
    }

    [TestMethod]
    public void SemanticAccessFallbackWorksForLocalFixtureDescriptor()
    {
        var result = Evaluate("truncated-surface");

        Assert.AreEqual(NodalOsSemanticFallbackDecision.AvailableForReadOnlyContext, result.Semantic.Decision);
        Assert.IsFalse(result.Semantic.ActionAuthorityGranted);
        Assert.IsFalse(result.Semantic.Evidence.UsesOcrVisionProductive);
    }

    [TestMethod]
    public void SemanticAccessFallbackInsufficientEvidenceRequiresHumanReview()
    {
        var result = Evaluate("ambiguous-interactive");

        Assert.AreEqual(NodalOsSemanticFallbackDecision.RequiresHumanReview, result.Semantic.Decision);
        Assert.IsFalse(result.Semantic.ActionAuthorityGranted);
    }

    [TestMethod]
    public void SemanticAccessFallbackOverlayAndSensitiveAreBlocked()
    {
        var overlay = Evaluate("system-overlay");
        var sensitive = Evaluate("sensitive-surface");

        Assert.AreEqual(NodalOsSemanticFallbackDecision.BlockedByOverlay, overlay.Semantic.Decision);
        Assert.AreEqual(NodalOsSemanticFallbackDecision.BlockedSensitiveSurface, sensitive.Semantic.Decision);
    }

    [TestMethod]
    public void SemanticAccessFallbackCanDeferFutureVisionOcr()
    {
        var result = Evaluate("future-vision-ocr-required");

        Assert.AreEqual(NodalOsSemanticFallbackDecision.RequiresFutureVisionOcr, result.Semantic.Decision);
        Assert.IsFalse(result.Semantic.Evidence.UsesOcrVisionProductive);
    }

    [TestMethod]
    public void RobustPerceptionIntegratesIdentityFingerprintV2()
    {
        var result = Evaluate("alive-stable");

        Assert.AreEqual(NodalOsIdentityConfidence.VerifiedFixture, result.Evaluation.IdentityProof.Fingerprint.Confidence);
        Assert.IsTrue(result.Evaluation.LivenessEvidence.Signals.Any(s => s.Name.Contains("identity/fingerprint v2", StringComparison.Ordinal)));
    }

    [TestMethod]
    public void OperatorUxReadinessShowsRobustPerceptionWarning()
    {
        var summary = new NodalOsOperatorUxReadinessService().BuildDefaultSummary();

        Assert.IsTrue(summary.EvidenceSummary.Any(e => e.Contains("Robust perception", StringComparison.Ordinal)));
        Assert.IsTrue(summary.EvidenceSummary.Any(e => e.Contains("non-authoritative", StringComparison.Ordinal)));
    }

    [TestMethod]
    public void ProductAdminPrivatePreviewIncludesRobustPerceptionReadinessSignal()
    {
        var report = new NodalOsProductAdminPrivatePreviewHardeningService().BuildDefaultReport();

        Assert.IsTrue(report.EvidenceRefs.Any(e => e.Contains("perception:robust-fixture-ready", StringComparison.Ordinal)));
        Assert.IsTrue(report.CoreAuthorityRequired);
        Assert.IsTrue(report.UiAdminAuthorityBlocked);
    }

    [TestMethod]
    public void RobustPerceptionAdrExists()
    {
        var text = File.ReadAllText(SourcePath("docs", "adr", "robust-perception-stabilization-m136-m138.md"));

        StringAssert.Contains(text, "Robust Perception Stabilization");
        StringAssert.Contains(text, "does not authorize actions");
        StringAssert.Contains(text, "M139-M141");
    }

    [TestMethod]
    public void Hito162ReplacementSequenceShowsM136M138ImplementedWithoutScopeExpansion()
    {
        var text = File.ReadAllText(SourcePath("docs", "roadmap", "nodal-os-hito-162-replacement-sequence.md"));

        StringAssert.Contains(text, "Status after M136-M138: implemented");
        StringAssert.Contains(text, "does not authorize actions");
        StringAssert.Contains(text, "No SaaS public");
        StringAssert.Contains(text, "No external CDP general-ready claim");
    }

    private static (NodalOsRobustPerceptionFixture Fixture, NodalOsPerceptionEvaluation Evaluation, NodalOsOverlayDetectionResult Overlay, NodalOsEmptySurfaceDetectionResult Empty, NodalOsSemanticAccessFallback Semantic, NodalOsPerceptionBlockerExplanation Explanation) Evaluate(string fixtureId) =>
        new NodalOsRobustPerceptionFixtureHarness().RunDefaultFixtures().Single(r => r.Fixture.FixtureId == fixtureId);

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
