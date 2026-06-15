using Microsoft.VisualStudio.TestTools.UnitTesting;
using OneBrain.Core.Detection.Audit;
using OneBrain.Core.Detection.Contracts;
using OneBrain.Core.Detection.Perception;
using OneBrain.Core.Detection.Policy;
using OneBrain.Core.Detection.Scoring;
using OneBrain.Core.Execution;

namespace OneBrain.Recipes.Tests;

[TestClass]
public sealed class InteractionStatePolicyTests
{
    // ── ISafetyPolicyEngine ───────────────────────────────────────────────

    [TestMethod]
    public async Task Policy_Captcha_RequiresHuman()
    {
        var engine = new StateSafetyPolicyEngine();
        var result = new StateDetectionResult { DetectedState = InteractionState.CaptchaChallenge, ConfidenceScore = 0.95 };
        var decision = await engine.EvaluateStateAsync(result);
        Assert.AreEqual(StateDecisionType.RequiresHuman, decision.Type);
        Assert.AreEqual("P-CAPTCHA-001", decision.ReasonCode);
    }

    [TestMethod]
    public async Task Policy_TwoFactor_RequiresHuman()
    {
        var engine = new StateSafetyPolicyEngine();
        var result = new StateDetectionResult { DetectedState = InteractionState.TwoFactorRequired };
        var decision = await engine.EvaluateStateAsync(result);
        Assert.AreEqual(StateDecisionType.RequiresHuman, decision.Type);
        Assert.AreEqual("P-2FA-001", decision.ReasonCode);
    }

    [TestMethod]
    public async Task Policy_AntiBot_RequiresHuman()
    {
        var engine = new StateSafetyPolicyEngine();
        var result = new StateDetectionResult { DetectedState = InteractionState.AntiBotBlock };
        var decision = await engine.EvaluateStateAsync(result);
        Assert.AreEqual(StateDecisionType.RequiresHuman, decision.Type);
        Assert.AreEqual("P-ANTIBOT-001", decision.ReasonCode);
    }

    [TestMethod]
    public async Task Policy_Loading_Waits()
    {
        var engine = new StateSafetyPolicyEngine();
        var result = new StateDetectionResult { DetectedState = InteractionState.Loading };
        var decision = await engine.EvaluateStateAsync(result);
        Assert.AreEqual(StateDecisionType.Wait, decision.Type);
        Assert.IsTrue(decision.WaitDuration > TimeSpan.Zero);
    }

    [TestMethod]
    public async Task Policy_LayoutChanged_TriggersSelectorRecovery()
    {
        var engine = new StateSafetyPolicyEngine();
        var result = new StateDetectionResult { DetectedState = InteractionState.LayoutChanged };
        var decision = await engine.EvaluateStateAsync(result);
        Assert.AreEqual(StateDecisionType.TriggerSelectorRecovery, decision.Type);
    }

    [TestMethod]
    public async Task Policy_Honeypot_Aborts()
    {
        var engine = new StateSafetyPolicyEngine();
        var result = new StateDetectionResult { DetectedState = InteractionState.HoneypotDetected };
        var decision = await engine.EvaluateStateAsync(result);
        Assert.AreEqual(StateDecisionType.Abort, decision.Type);
        Assert.AreEqual("P-HONEYPOT-001", decision.ReasonCode);
    }

    [TestMethod]
    public async Task Policy_Timeout_Waits()
    {
        var engine = new StateSafetyPolicyEngine();
        var result = new StateDetectionResult { DetectedState = InteractionState.TimeoutOrHang };
        var decision = await engine.EvaluateStateAsync(result);
        Assert.AreEqual(StateDecisionType.Wait, decision.Type);
    }

    [TestMethod]
    public async Task Policy_JsDialog_RequiresHuman()
    {
        var engine = new StateSafetyPolicyEngine();
        var result = new StateDetectionResult { DetectedState = InteractionState.JavaScriptDialog };
        var decision = await engine.EvaluateStateAsync(result);
        Assert.AreEqual(StateDecisionType.RequiresHuman, decision.Type);
    }

    [TestMethod]
    public async Task Policy_FrameNavigated_TriggersSelectorRecovery()
    {
        var engine = new StateSafetyPolicyEngine();
        var result = new StateDetectionResult { DetectedState = InteractionState.FrameNavigated };
        var decision = await engine.EvaluateStateAsync(result);
        Assert.AreEqual(StateDecisionType.TriggerSelectorRecovery, decision.Type);
    }

    [TestMethod]
    public async Task Policy_None_HighConfidence_Proceed()
    {
        var engine = new StateSafetyPolicyEngine();
        var result = new StateDetectionResult { DetectedState = InteractionState.None, ConfidenceScore = 0.80 };
        var decision = await engine.EvaluateStateAsync(result);
        Assert.AreEqual(StateDecisionType.Proceed, decision.Type);
    }

    [TestMethod]
    public async Task Policy_ModalOverlay_Proceed()
    {
        var engine = new StateSafetyPolicyEngine();
        var result = new StateDetectionResult { DetectedState = InteractionState.ModalOverlay };
        var decision = await engine.EvaluateStateAsync(result);
        Assert.AreEqual(StateDecisionType.Proceed, decision.Type);
    }

    // ── Integration ───────────────────────────────────────────────────────

    [TestMethod]
    public async Task Integration_Captcha_Stops_With_RequiresHuman()
    {
        var features = new StructuralFeatures { HasCaptchaDiv = true };
        var detector = new InteractionStateDetector(
            new CdpStructuralAnalyzer(features), new NetworkFeatureExtractor());
        var audit = new InMemoryAuditLogger();

        var integration = new InteractionStateDecisionIntegration(detector, audit: audit);

        var decision = await integration.EvaluateAsync(new TargetContext(), "step-1");

        Assert.AreEqual(StateDecisionType.RequiresHuman, decision.Type);
        Assert.AreEqual("P-CAPTCHA-001", decision.ReasonCode);
    }

    [TestMethod]
    public async Task Integration_NormalPage_Proceeds()
    {
        var detector = new InteractionStateDetector(
            new CdpStructuralAnalyzer(), new NetworkFeatureExtractor());
        var audit = new InMemoryAuditLogger();

        var integration = new InteractionStateDecisionIntegration(detector, audit: audit);

        var decision = await integration.EvaluateAsync(new TargetContext(), "step-2");

        Assert.AreEqual(StateDecisionType.Proceed, decision.Type);
    }

    [TestMethod]
    public async Task Integration_Honeypot_Aborts()
    {
        var features = new StructuralFeatures { HasHoneypotFields = true };
        var detector = new InteractionStateDetector(
            new CdpStructuralAnalyzer(features), new NetworkFeatureExtractor());
        var audit = new InMemoryAuditLogger();

        var integration = new InteractionStateDecisionIntegration(detector, audit: audit);

        var decision = await integration.EvaluateAsync(new TargetContext(), "step-3");

        Assert.AreEqual(StateDecisionType.Abort, decision.Type);
    }

    [TestMethod]
    public void Integration_AuditEvents_Emitted()
    {
        var features = new StructuralFeatures { HasCaptchaIframe = true };
        var detector = new InteractionStateDetector(
            new CdpStructuralAnalyzer(features), new NetworkFeatureExtractor());
        var audit = new InMemoryAuditLogger();

        var integration = new InteractionStateDecisionIntegration(detector, audit: audit);

        var task = integration.EvaluateAsync(new TargetContext(), "step-audit");
        task.Wait();

        var scoringEvents = audit.GetEventsOfType<StateScoringEvent>();
        var decisionEvents = audit.GetEventsOfType<StateDecisionEvent>();

        Assert.IsTrue(scoringEvents.Count > 0, "Expected scoring events in audit log");
        Assert.IsTrue(decisionEvents.Count > 0, "Expected decision events in audit log");
        Assert.AreEqual("PRE_FLIGHT", scoringEvents[0].Phase);
    }

    // ── Scoring engine determinism ────────────────────────────────────────

    [TestMethod]
    public void Scoring_Honeypot_Dominates_Loading()
    {
        var engine = new StateScoringEngine([new HoneypotRule(), new LoadingRule()]);
        var f = new StructuralFeatures { HasHoneypotFields = true, HasLoadingOverlay = true };

        var result = engine.Score(f);

        Assert.AreEqual(InteractionState.HoneypotDetected, result.DetectedState);
    }

    [TestMethod]
    public void Scoring_Captcha_Dominates_TwoFactor()
    {
        var engine = new StateScoringEngine([new CaptchaSignatureRule(), new TwoFactorRule()]);
        var f = new StructuralFeatures { HasCaptchaDiv = true, HasTwoFactorFields = true };

        var result = engine.Score(f);

        // Captcha weight (0.40 * 1.0 = 0.40) vs 2FA weight (0.35 * 1.0 = 0.35)
        Assert.AreEqual(InteractionState.CaptchaChallenge, result.DetectedState);
    }

    [TestMethod]
    public void Scoring_NetworkBlock_Overrides()
    {
        var engine = new StateScoringEngine([new CaptchaSignatureRule(), new AntiBotRule()]);
        var f = new StructuralFeatures();
        var n = new NetworkFeatures { BlockedStatusCode = 403 };

        var result = engine.Score(f, n);

        Assert.AreEqual(InteractionState.AntiBotBlock, result.DetectedState);
        Assert.IsTrue(result.Vector.AntiBotScore >= 0.90);
    }

    [TestMethod]
    public void Scoring_Stability_Across_Multiple_Calls()
    {
        var engine = new StateScoringEngine([new CaptchaSignatureRule(), new ModalOverlayRule(), new HoneypotRule()]);
        var f = new StructuralFeatures { HasCaptchaIframe = true, HasModalOverlay = true };

        for (int i = 0; i < 10; i++)
        {
            var r = engine.Score(f);
            Assert.AreEqual(InteractionState.CaptchaChallenge, r.DetectedState);
            Assert.IsTrue(r.ConfidenceScore > 0);
        }
    }

    // ── StateVector updates ───────────────────────────────────────────────

    [TestMethod]
    public void StateVector_WithExpression_UpdatesSingleDimension()
    {
        var v = new StateVector();
        var updated = v with { CaptchaScore = 0.95 };

        Assert.AreEqual(0.95, updated.CaptchaScore);
        Assert.AreEqual(0.0, updated.TwoFactorScore);
    }
}
