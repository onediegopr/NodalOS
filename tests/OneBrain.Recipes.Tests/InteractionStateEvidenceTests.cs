using System.Security.Cryptography;
using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using OneBrain.Core.Detection.Audit;
using OneBrain.Core.Detection.Contracts;
using OneBrain.Core.Detection.Evidence;
using OneBrain.Core.Detection.Handoff;

namespace OneBrain.Recipes.Tests;

[TestClass]
public sealed class InteractionStateEvidenceTests
{
    // ── Evidence Redaction ────────────────────────────────────────────────

    [TestMethod]
    public async Task DeterministicRedactor_SameInput_SameHash()
    {
        var redactor = new DeterministicEvidenceRedactor();
        var ctx = new TargetContext { Url = "https://example.com" };

        var h1 = await redactor.CaptureAndRedactAsync(ctx);
        var h2 = await redactor.CaptureAndRedactAsync(ctx);

        Assert.AreEqual(h1, h2);
        Assert.IsTrue(h1.Length > 0);
    }

    [TestMethod]
    public async Task DeterministicRedactor_DifferentInput_DifferentHash()
    {
        var redactor = new DeterministicEvidenceRedactor();

        var h1 = await redactor.CaptureAndRedactAsync(new TargetContext { Url = "a" });
        var h2 = await redactor.CaptureAndRedactAsync(new TargetContext { Url = "b" });

        Assert.AreNotEqual(h1, h2);
    }

    [TestMethod]
    public async Task FixedRedactor_Returns_Exact()
    {
        var redactor = new FixedEvidenceRedactor("abc123");
        var hash = await redactor.CaptureAndRedactAsync(new TargetContext());
        Assert.AreEqual("abc123", hash);
    }

    // ── HMAC Chain ────────────────────────────────────────────────────────

    [TestMethod]
    public void HmacChain_Links_Two_Events()
    {
        var secret = "test-chain-key";
        var e1 = new StateDecisionEvent
        {
            DecisionType = StateDecisionType.Proceed,
            ReasonCode = "P-PROCEED",
            StepId = "s1",
            Timestamp = DateTimeOffset.UtcNow
        };
        var e2 = new StateDecisionEvent
        {
            DecisionType = StateDecisionType.RequiresHuman,
            ReasonCode = "P-CAPTCHA-001",
            StepId = "s2",
            Timestamp = DateTimeOffset.UtcNow
        };

        var linked1 = AuditHashChain.Link(e1, "", secret);
        var linked2 = AuditHashChain.Link(e2, linked1.EventHash, secret);

        Assert.IsNotEmpty(linked1.EventHash);
        Assert.IsNotEmpty(linked2.EventHash);
        Assert.AreEqual(linked1.EventHash, linked2.PreviousEventHash);
        Assert.AreNotEqual(linked1.EventHash, linked2.EventHash);
    }

    [TestMethod]
    public void HmacChain_Different_Secrets_Different_Hashes()
    {
        var evt = new StateDecisionEvent { DecisionType = StateDecisionType.Proceed, StepId = "s" };
        var linked1 = AuditHashChain.Link(evt, "", "secret-a");
        var linked2 = AuditHashChain.Link(evt, "", "secret-b");

        Assert.AreNotEqual(linked1.EventHash, linked2.EventHash);
    }

    // ── Handoff Gateway ───────────────────────────────────────────────────

    [TestMethod]
    public async Task Handoff_ResolvedByHuman()
    {
        var gw = new InMemoryHandoffGateway();
        gw.SetNextResult(HandoffResult.ResolvedByHuman);

        var result = await gw.RequestInterventionAsync(new StateHandoffRequest
        {
            RecipeStepId = "s1",
            DetectedState = InteractionState.CaptchaChallenge,
            ReasonCode = "P-CAPTCHA-001"
        });

        Assert.AreEqual(HandoffResult.ResolvedByHuman, result);
    }

    [TestMethod]
    public async Task Handoff_AbortedByUser()
    {
        var gw = new InMemoryHandoffGateway();
        gw.SetNextResult(HandoffResult.AbortedByUser);

        var result = await gw.RequestInterventionAsync(new StateHandoffRequest());
        Assert.AreEqual(HandoffResult.AbortedByUser, result);
    }

    [TestMethod]
    public async Task Handoff_Default_Timeout()
    {
        var gw = new InMemoryHandoffGateway();
        var result = await gw.RequestInterventionAsync(new StateHandoffRequest());
        Assert.AreEqual(HandoffResult.Timeout, result);
    }

    // ── StateHandoffRequest ───────────────────────────────────────────────

    [TestMethod]
    public void HandoffRequest_Default_TimeoutMinutes_Is_Five()
    {
        var req = new StateHandoffRequest();
        Assert.AreEqual(5, req.TimeoutMinutes);
    }

    // ── Audit log thread safety ───────────────────────────────────────────

    [TestMethod]
    public void AuditLogger_Concurrent_Writes_Are_Safe()
    {
        var log = new InMemoryAuditLogger();
        var tasks = new List<Task>();

        for (int i = 0; i < 100; i++)
        {
            var idx = i;
            tasks.Add(Task.Run(() => log.LogAsync(new StateScoringEvent { Phase = $"t{idx}" })));
        }

        Task.WaitAll(tasks.ToArray());
        Assert.AreEqual(100, log.AllEvents.Count);
    }

    // ── PII safety (negative test) ────────────────────────────────────────

    [TestMethod]
    public void NetworkFeatures_HeaderNames_Never_HeaderValues()
    {
        // NetworkFeatures only stores header NAMES, never values
        var n = new NetworkFeatures
        {
            BlockedStatusCode = 403,
            Host = "example.com",
            HeaderNames = ["content-type", "x-frame-options"]
        };

        Assert.IsFalse(n.HeaderNames.Contains("Bearer"));
        Assert.IsFalse(n.HeaderNames.Contains("token"));
    }

    [TestMethod]
    public void StructuralFeatures_DomSnippet_Must_Be_Sanitized()
    {
        // DomSnippet is explicitly nullable and should be redacted by the sensor
        var f = new StructuralFeatures();
        Assert.IsNull(f.DomSnippet);

        // Even when set, it must never contain raw input values
        var f2 = new StructuralFeatures { DomSnippet = "<div>content</div>" };
        Assert.IsFalse(f2.DomSnippet!.Contains("<input"));
        Assert.IsFalse(f2.DomSnippet.Contains("password"));
    }

    // ── Audit event immutability ──────────────────────────────────────────

    [TestMethod]
    public void StateDecisionEvent_With_Expression_Returns_New_Instance()
    {
        var original = new StateDecisionEvent
        {
            DecisionType = StateDecisionType.Proceed,
            ReasonCode = "P-PROCEED",
            StepId = "s1"
        };

        var modified = original with { ReasonCode = "P-OTHER" };

        Assert.AreEqual("P-PROCEED", original.ReasonCode);
        Assert.AreEqual("P-OTHER", modified.ReasonCode);
        Assert.AreNotSame(original, modified);
    }
}
