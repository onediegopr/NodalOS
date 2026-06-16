using Microsoft.VisualStudio.TestTools.UnitTesting;
using OneBrain.Core.Detection.Contracts;
using OneBrain.Core.Detection.Perception;
using OneBrain.Core.Detection.Policy;
using OneBrain.Core.Detection.Scoring;

namespace OneBrain.Recipes.Tests;

[TestClass]
public sealed class InteractionStateEdgeFixTests
{
    private static StateDetectionResult Result(InteractionState state, double confidence) =>
        new() { DetectedState = state, ConfidenceScore = confidence };

    // ── C-1: gate de confianza ────────────────────────────────────────────

    [TestMethod]
    public async Task C1_LowConfidenceModal_DegradesToRequiresHuman()
    {
        var decision = await new StateSafetyPolicyEngine().EvaluateStateAsync(Result(InteractionState.ModalOverlay, 0.10));
        Assert.AreEqual(StateDecisionType.RequiresHuman, decision.Type);
        Assert.AreEqual("P-LOW-CONFIDENCE-001", decision.ReasonCode);
    }

    [TestMethod]
    public async Task C1_LowConfidenceLoading_DegradesToRequiresHuman()
    {
        var decision = await new StateSafetyPolicyEngine().EvaluateStateAsync(Result(InteractionState.Loading, 0.59));
        Assert.AreEqual(StateDecisionType.RequiresHuman, decision.Type);
    }

    [TestMethod]
    public async Task C1_BoundaryExactly060Modal_Proceeds()
    {
        var decision = await new StateSafetyPolicyEngine().EvaluateStateAsync(Result(InteractionState.ModalOverlay, StateSafetyPolicyEngine.MINIMUM_CONFIDENCE));
        Assert.AreEqual(StateDecisionType.Proceed, decision.Type);
        Assert.AreEqual("P-MODAL-001", decision.ReasonCode);
    }

    [TestMethod]
    public async Task C1_LowConfidenceNone_StillProceeds()
    {
        // None es el baseline limpio (confianza 0 por construcción): no debe forzar humano.
        var decision = await new StateSafetyPolicyEngine().EvaluateStateAsync(Result(InteractionState.None, 0.0));
        Assert.AreEqual(StateDecisionType.Proceed, decision.Type);
    }

    [TestMethod]
    public async Task C1_LowConfidenceCaptcha_StaysRequiresHuman()
    {
        var decision = await new StateSafetyPolicyEngine().EvaluateStateAsync(Result(InteractionState.CaptchaChallenge, 0.05));
        Assert.AreEqual(StateDecisionType.RequiresHuman, decision.Type);
        Assert.AreEqual("P-CAPTCHA-001", decision.ReasonCode);
    }

    [TestMethod]
    public async Task C1_LowConfidenceHoneypot_StaysAbort()
    {
        var decision = await new StateSafetyPolicyEngine().EvaluateStateAsync(Result(InteractionState.HoneypotDetected, 0.05));
        Assert.AreEqual(StateDecisionType.Abort, decision.Type);
    }

    // ── C-2: vector en cero ───────────────────────────────────────────────

    [TestMethod]
    public void C2_ZeroVector_ReturnsNoneNotCaptcha()
    {
        var engine = new StateScoringEngine(new List<IScoringRule>());
        var result = engine.Score(new StructuralFeatures(), null);
        Assert.AreEqual(InteractionState.None, result.DetectedState);
        Assert.AreEqual(0.0, result.ConfidenceScore);
    }

    [TestMethod]
    public void C2_NonZeroVector_ReturnsDominantState()
    {
        var engine = new StateScoringEngine(new List<IScoringRule> { new FakeRule("R-MODAL", 1.0, InteractionState.ModalOverlay, 0.80) });
        var result = engine.Score(new StructuralFeatures { HasModalOverlay = true }, null);
        Assert.AreEqual(InteractionState.ModalOverlay, result.DetectedState);
        Assert.AreEqual(0.80, result.ConfidenceScore, 0.0001);
    }

    // ── M-3: thread-safe + idempotente ────────────────────────────────────

    [TestMethod]
    public async Task M3_SecondRead_DoesNotReturnEmpty()
    {
        var extractor = new NetworkFeatureExtractor(new NetworkFeatures { BlockedStatusCode = 403 });
        var first = await extractor.ExtractAsync(new TargetContext());
        var second = await extractor.ExtractAsync(new TargetContext());
        Assert.AreEqual(403, first.BlockedStatusCode);
        Assert.AreEqual(403, second.BlockedStatusCode);
    }

    [TestMethod]
    public async Task M3_ConcurrentReads_DoNotCorruptOrThrow()
    {
        var extractor = new NetworkFeatureExtractor(new NetworkFeatures { BlockedStatusCode = 429 });
        var tasks = Enumerable.Range(0, 200).Select(_ => extractor.ExtractAsync(new TargetContext())).ToArray();
        var results = await Task.WhenAll(tasks);
        Assert.IsTrue(results.All(r => r is not null));
        Assert.IsTrue(results.All(r => r.BlockedStatusCode == 429));
    }

    [TestMethod]
    public async Task M3_QueuedFeatures_DrainInOrderThenStick()
    {
        var extractor = new NetworkFeatureExtractor();
        extractor.Enqueue(new NetworkFeatures { BlockedStatusCode = 401 });
        extractor.Enqueue(new NetworkFeatures { BlockedStatusCode = 403 });
        Assert.AreEqual(401, (await extractor.ExtractAsync(new TargetContext())).BlockedStatusCode);
        Assert.AreEqual(403, (await extractor.ExtractAsync(new TargetContext())).BlockedStatusCode);
        Assert.AreEqual(403, (await extractor.ExtractAsync(new TargetContext())).BlockedStatusCode);
    }

    private sealed class FakeRule : IScoringRule
    {
        private readonly double _score;
        public FakeRule(string ruleId, double weight, InteractionState target, double score)
        {
            RuleId = ruleId;
            Weight = weight;
            TargetState = target;
            _score = score;
        }

        public string RuleId { get; }
        public double Weight { get; }
        public InteractionState TargetState { get; }
        public double Evaluate(StructuralFeatures features) => _score;
    }
}
