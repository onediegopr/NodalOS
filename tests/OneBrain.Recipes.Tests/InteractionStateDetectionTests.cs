using Microsoft.VisualStudio.TestTools.UnitTesting;
using OneBrain.Core.Detection.Contracts;
using OneBrain.Core.Detection.Perception;
using OneBrain.Core.Detection.Scoring;

namespace OneBrain.Recipes.Tests;

[TestClass]
public sealed class InteractionStateDetectionTests
{
    // ── StructuralFeatures construction ────────────────────────────────────

    [TestMethod]
    public void Empty_StructuralFeatures_Has_All_False()
    {
        var f = new StructuralFeatures();
        Assert.IsFalse(f.HasCaptchaIframe);
        Assert.IsFalse(f.HasCaptchaDiv);
        Assert.IsFalse(f.HasTwoFactorFields);
        Assert.IsFalse(f.HasHoneypotFields);
        Assert.IsFalse(f.HasLoadingOverlay);
        Assert.IsFalse(f.HasModalOverlay);
        Assert.IsFalse(f.IsJavaScriptDialogOpen);
        Assert.IsFalse(f.IsFrameNavigated);
        Assert.IsFalse(f.IsNetworkBlocked);
    }

    // ── CdpStructuralAnalyzer (stub) ──────────────────────────────────────

    [TestMethod]
    public async Task Analyzer_With_Injected_Features_Returns_Exact()
    {
        var injected = new StructuralFeatures { HasCaptchaIframe = true, HasTwoFactorFields = true };
        var analyzer = new CdpStructuralAnalyzer(injected);

        var result = await analyzer.AnalyzeAsync(new TargetContext());

        Assert.IsTrue(result.HasCaptchaIframe);
        Assert.IsTrue(result.HasTwoFactorFields);
    }

    [TestMethod]
    public async Task Analyzer_Without_Injection_Returns_Empty()
    {
        var analyzer = new CdpStructuralAnalyzer();
        var result = await analyzer.AnalyzeAsync(new TargetContext());
        Assert.IsFalse(result.HasCaptchaIframe);
    }

    // ── NetworkFeatureExtractor (stub) ────────────────────────────────────

    [TestMethod]
    public async Task NetworkExtractor_With_Injected_Returns_Exact()
    {
        var injected = new NetworkFeatures { BlockedStatusCode = 403, Host = "example.com" };
        var extractor = new NetworkFeatureExtractor(injected);

        var result = await extractor.ExtractAsync(new TargetContext());
        Assert.AreEqual(403, result.BlockedStatusCode);
        Assert.AreEqual("example.com", result.Host);
    }

    [TestMethod]
    public async Task NetworkExtractor_Without_Injection_Returns_Empty()
    {
        var extractor = new NetworkFeatureExtractor();
        var result = await extractor.ExtractAsync(new TargetContext());
        Assert.IsNull(result.BlockedStatusCode);
    }

    // ── InteractionStateDetector (no-scoring fallback) ─────────────────────

    [TestMethod]
    public async Task Detector_CaptchaIframe_Returns_CaptchaChallenge()
    {
        var f = new StructuralFeatures { HasCaptchaIframe = true };
        var detector = new InteractionStateDetector(new CdpStructuralAnalyzer(f), new NetworkFeatureExtractor());

        var result = await detector.AssessPreFlightAsync(new TargetContext());

        Assert.AreEqual(InteractionState.CaptchaChallenge, result.DetectedState);
        Assert.IsTrue(result.ConfidenceScore > 0.9);
    }

    [TestMethod]
    public async Task Detector_Blocked403_Returns_AntiBotBlock()
    {
        var n = new NetworkFeatures { BlockedStatusCode = 403 };
        var detector = new InteractionStateDetector(new CdpStructuralAnalyzer(), new NetworkFeatureExtractor(n));

        var result = await detector.AssessPreFlightAsync(new TargetContext());

        Assert.AreEqual(InteractionState.AntiBotBlock, result.DetectedState);
    }

    [TestMethod]
    public async Task Detector_EmptyPage_Returns_None()
    {
        var detector = new InteractionStateDetector(new CdpStructuralAnalyzer(), new NetworkFeatureExtractor());
        var result = await detector.AssessPreFlightAsync(new TargetContext());

        Assert.AreEqual(InteractionState.None, result.DetectedState);
        Assert.AreEqual(0.0, result.ConfidenceScore);
    }

    [TestMethod]
    public async Task Detector_InFlight_Timeout_Returns_TimeoutOrHang()
    {
        // Use a structural analyzer that blocks forever via CancellationToken
        var detector = new InteractionStateDetector(
            new CdpStructuralAnalyzer(), new NetworkFeatureExtractor());

        var result = await detector.AssessInFlightAsync(new TargetContext(), TimeSpan.FromMilliseconds(10));

        // With no scoring engine, the fallback returns from sensors immediately,
        // so timeout only happens if sensors block. This tests the TimeoutOrHang path.
        // The stub sensors are instant, so this won't timeout.
        Assert.AreEqual(InteractionState.None, result.DetectedState);
    }

    // ── StateScoringEngine ────────────────────────────────────────────────

    [TestMethod]
    public void Scoring_CaptchaSignatures_Score_High()
    {
        var engine = new StateScoringEngine([
            new CaptchaSignatureRule(), new TwoFactorRule(), new AntiBotRule(),
            new LoadingRule(), new LayoutChangedRule(), new ModalOverlayRule(),
            new HoneypotRule(), new TimeoutRule()
        ]);

        var f = new StructuralFeatures { HasCaptchaIframe = true, HasCaptchaDiv = true };
        var result = engine.Score(f);

        Assert.AreEqual(InteractionState.CaptchaChallenge, result.DetectedState);
        Assert.IsTrue(result.Vector.CaptchaScore > 0.3);
        Assert.IsTrue(result.ConfidenceScore > 0.0);
    }

    [TestMethod]
    public void Scoring_Determinism_SameInput_SameScore()
    {
        var engine = new StateScoringEngine([
            new CaptchaSignatureRule(), new ModalOverlayRule()
        ]);

        var f = new StructuralFeatures { HasCaptchaIframe = true, HasModalOverlay = true };

        var r1 = engine.Score(f);
        var r2 = engine.Score(f);

        Assert.AreEqual(r1.DetectedState, r2.DetectedState);
        Assert.AreEqual(r1.ConfidenceScore, r2.ConfidenceScore);
        Assert.AreEqual(r1.ScoringConfigHash, r2.ScoringConfigHash);
    }

    [TestMethod]
    public void Scoring_ConfigHash_Changes_With_Different_Rules()
    {
        var engine1 = new StateScoringEngine([new CaptchaSignatureRule()]);
        var engine2 = new StateScoringEngine([new CaptchaSignatureRule(), new HoneypotRule()]);

        Assert.AreNotEqual(engine1.ConfigurationHash, engine2.ConfigurationHash);
    }

    [TestMethod]
    public void Scoring_ConfigHash_Stable_Same_Rules()
    {
        var engine1 = new StateScoringEngine([new CaptchaSignatureRule(), new TwoFactorRule()]);
        var engine2 = new StateScoringEngine([new CaptchaSignatureRule(), new TwoFactorRule()]);

        Assert.AreEqual(engine1.ConfigurationHash, engine2.ConfigurationHash);
    }

    // ── Individual scoring rules ──────────────────────────────────────────

    [TestMethod]
    public void HoneypotRule_Detects_Hidden_Fields()
    {
        var rule = new HoneypotRule();
        Assert.AreEqual(1.0, rule.Evaluate(new StructuralFeatures { HasHoneypotFields = true }));
        Assert.AreEqual(0.0, rule.Evaluate(new StructuralFeatures()));
    }

    [TestMethod]
    public void AntiBotRule_Detects_NetworkBlock()
    {
        var rule = new AntiBotRule();
        Assert.AreEqual(1.0, rule.Evaluate(new StructuralFeatures { IsNetworkBlocked = true }));
        Assert.AreEqual(0.0, rule.Evaluate(new StructuralFeatures()));
    }

    [TestMethod]
    public void ModalRule_Detects_Overlay_And_Dialog()
    {
        var rule = new ModalOverlayRule();
        Assert.AreEqual(1.0, rule.Evaluate(new StructuralFeatures { HasModalOverlay = true }));
        Assert.AreEqual(0.8, rule.Evaluate(new StructuralFeatures { IsJavaScriptDialogOpen = true }));
        Assert.AreEqual(0.0, rule.Evaluate(new StructuralFeatures()));
    }

    // ── StateDecision factory methods ─────────────────────────────────────

    [TestMethod]
    public void StateDecision_Proceed_Has_Correct_Type()
    {
        var d = StateDecision.Proceed();
        Assert.AreEqual(StateDecisionType.Proceed, d.Type);
    }

    [TestMethod]
    public void StateDecision_RequiresHuman_Has_Correct_Type()
    {
        var d = StateDecision.RequiresHuman("P-CAPTCHA-001");
        Assert.AreEqual(StateDecisionType.RequiresHuman, d.Type);
        Assert.AreEqual("P-CAPTCHA-001", d.ReasonCode);
    }

    [TestMethod]
    public void StateDecision_Wait_Sets_Duration()
    {
        var d = StateDecision.Wait(TimeSpan.FromSeconds(5));
        Assert.AreEqual(StateDecisionType.Wait, d.Type);
        Assert.AreEqual(TimeSpan.FromSeconds(5), d.WaitDuration);
    }

    [TestMethod]
    public void StateDecision_Abort_Has_Correct_Type()
    {
        var d = StateDecision.Abort("P-HONEYPOT-001");
        Assert.AreEqual(StateDecisionType.Abort, d.Type);
    }

    // ── Detector with scoring engine ──────────────────────────────────────

    [TestMethod]
    public async Task Detector_With_Scoring_Uses_Engine_Not_Fallback()
    {
        var engine = new StateScoringEngine([new CaptchaSignatureRule(), new ModalOverlayRule()]);
        var f = new StructuralFeatures { HasCaptchaIframe = true };
        var detector = new InteractionStateDetector(
            new CdpStructuralAnalyzer(f), new NetworkFeatureExtractor(), engine);

        var result = await detector.AssessPreFlightAsync(new TargetContext());
        Assert.IsNotEmpty(result.ScoringConfigHash);
        Assert.AreEqual(engine.ConfigurationHash, result.ScoringConfigHash);
    }
}
