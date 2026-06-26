using System.Text.Json;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using OneBrain.DocumentIntelligence;

namespace OneBrain.Safety.Tests;

[TestClass]
[TestCategory("MistralOcrProviderRouterDesignOnly")]
public sealed class MistralOcrProviderRouterDesignOnlyTests
{
    [TestMethod]
    public void ProviderRegistry_IncludesMistralCandidatesDisabledNoNetworkNoApiKey()
    {
        var registry = new OcrProviderRegistry();

        var mistralOcr = registry.GetRequired("cloud.mistral_ocr_4");
        var mistralDocumentAi = registry.GetRequired("cloud.mistral_document_ai");

        Assert.AreEqual(OcrProviderKind.PaidOcr, mistralOcr.ProviderKind);
        Assert.AreEqual(OcrProviderKind.DocumentAi, mistralDocumentAi.ProviderKind);
        Assert.AreEqual(OcrProviderMode.LiveCandidateBlocked, mistralOcr.Mode);
        Assert.AreEqual(OcrProviderMode.LiveCandidateBlocked, mistralDocumentAi.Mode);
        Assert.IsFalse(mistralOcr.Policy.RequiresApiKey);
        Assert.IsFalse(mistralDocumentAi.Policy.RequiresApiKey);
        Assert.IsFalse(mistralOcr.Policy.NetworkCallsEnabled);
        Assert.IsFalse(mistralDocumentAi.Policy.NetworkCallsEnabled);
        Assert.IsFalse(mistralOcr.Policy.HasLiveClient);
        Assert.IsFalse(mistralDocumentAi.Policy.HasLiveClient);
        CollectionAssert.Contains(mistralOcr.ForbiddenCapabilities.ToList(), OcrForbiddenCapability.ActionAuthorization);
        CollectionAssert.Contains(mistralDocumentAi.ForbiddenCapabilities.ToList(), OcrForbiddenCapability.BrowserControl);
    }

    [TestMethod]
    public void Router_DocumentOcrAllowPaidCandidate_RecommendsMistralOcr4Candidate()
    {
        var decision = Route(OcrDocumentFixtures.TableFixture(), OcrCostMode.AllowPaidCandidate);

        Assert.AreEqual(OcrRoutingDecisionKind.RecommendMistralOcr4Candidate, decision.Decision);
        Assert.AreEqual("cloud.mistral_ocr_4", decision.ProviderId);
        Assert.IsTrue(decision.LiveExecutionBlocked);
        Assert.IsTrue(decision.PaidExecutionBlocked);
        Assert.IsFalse(decision.NetworkCallAllowed);
        Assert.IsFalse(decision.ActionAuthority);
    }

    [TestMethod]
    public void Router_InvoiceExtraction_RecommendsMistralDocumentAiCandidate()
    {
        var decision = Route(OcrDocumentFixtures.SimpleInvoiceFixture(), OcrCostMode.AllowPaidCandidate);

        Assert.AreEqual(OcrRoutingDecisionKind.RecommendMistralDocumentAiCandidate, decision.Decision);
        Assert.AreEqual("cloud.mistral_document_ai", decision.ProviderId);
        Assert.IsTrue(decision.LiveExecutionBlocked);
        Assert.IsFalse(decision.ActionAuthority);
    }

    [TestMethod]
    public void Router_FreeOnly_UsesLocalFixtureProvider()
    {
        var decision = Route(OcrDocumentFixtures.SimpleInvoiceFixture(), OcrCostMode.FreeOnly);

        Assert.AreEqual(OcrRoutingDecisionKind.UseLocalOcrFixture, decision.Decision);
        Assert.AreEqual("local.onnx_ocr_fixture", decision.ProviderId);
        Assert.AreEqual(OcrProviderMode.FixtureOnly, decision.ProviderMode);
        Assert.IsFalse(decision.NetworkCallAllowed);
    }

    [TestMethod]
    public void Router_ScreenPerception_DoesNotJumpToPaidDocumentOcr()
    {
        var decision = Route(OcrDocumentFixtures.ScreenCropFixture(), OcrCostMode.AllowPaidCandidate);

        Assert.AreEqual(OcrRoutingDecisionKind.UseLocalOcrFixture, decision.Decision);
        Assert.AreEqual("local.onnx_ocr_fixture", decision.ProviderId);
        Assert.IsTrue(decision.HumanReviewRequired);
    }

    [TestMethod]
    public void Router_SensitiveUnredactedDocument_BlocksCloudCandidate()
    {
        var decision = Route(OcrDocumentFixtures.SensitiveDocumentFixture(), OcrCostMode.AllowPaidCandidate);

        Assert.AreEqual(OcrRoutingDecisionKind.BlockDueToSensitiveUnredactedInput, decision.Decision);
        Assert.IsNull(decision.ProviderId);
        Assert.IsTrue(decision.HumanReviewRequired);
        Assert.IsFalse(decision.NetworkCallAllowed);
    }

    [TestMethod]
    public void Router_LiveAndPaidExecution_RemainBlocked()
    {
        var live = Route(OcrDocumentFixtures.TableFixture(), OcrCostMode.AllowPaidCandidate, OcrExecutionMode.LiveBlocked);
        var paid = Route(OcrDocumentFixtures.TableFixture(), OcrCostMode.PaidLiveBlocked);

        Assert.AreEqual(OcrRoutingDecisionKind.BlockLiveProvider, live.Decision);
        Assert.AreEqual(OcrRoutingDecisionKind.BlockDueToPaidProviderNotEnabled, paid.Decision);
        Assert.IsFalse(live.NetworkCallAllowed);
        Assert.IsFalse(paid.NetworkCallAllowed);
    }

    [TestMethod]
    public void Policy_OcrCannotAuthorizeActionsOrUnlockExecution()
    {
        var highConfidence = Route(OcrDocumentFixtures.SimpleInvoiceFixture(), OcrCostMode.AllowPaidCandidate);
        var actionRequest = Request(OcrDocumentFixtures.SimpleInvoiceFixture(), OcrCostMode.AllowPaidCandidate) with
        {
            RequestsActionAuthority = true
        };

        var blocked = new OcrProviderRouter().Route(actionRequest);

        Assert.AreEqual(OcrConfidenceBand.High, highConfidence.Confidence.Band);
        Assert.IsFalse(highConfidence.ActionAuthority);
        Assert.AreEqual(OcrRoutingDecisionKind.BlockDueToActionAuthorityRequest, blocked.Decision);
        Assert.IsFalse(blocked.ActionAuthority);
    }

    [TestMethod]
    public void Policy_BlocksBrowserDesktopClickSubmitCaptchaLoginPaymentFiscalAutomation()
    {
        var router = new OcrProviderRouter();
        var baseRequest = Request(OcrDocumentFixtures.ScreenCropFixture(), OcrCostMode.AllowPaidCandidate);
        var requests = new[]
        {
            baseRequest with { RequestsBrowserControl = true },
            baseRequest with { RequestsDesktopControl = true },
            baseRequest with { ContainsCaptchaLikeChallenge = true },
            baseRequest with { ContainsLoginLikeFlow = true },
            baseRequest with { ContainsPaymentLikeFlow = true },
            baseRequest with { ContainsFiscalSubmissionLikeFlow = true }
        };

        foreach (var request in requests)
        {
            var decision = router.Route(request);
            Assert.AreEqual(OcrRoutingDecisionKind.BlockDueToActionAuthorityRequest, decision.Decision);
            Assert.IsTrue(decision.HumanReviewRequired);
            Assert.IsFalse(decision.ActionAuthority);
            Assert.IsFalse(decision.NetworkCallAllowed);
        }
    }

    [TestMethod]
    public void ConfidencePolicy_LowMissingAndMediumRequireHumanReview()
    {
        var policy = new OcrConfidencePolicy();

        Assert.IsTrue(policy.Evaluate(null).HumanReviewRequired);
        Assert.IsTrue(policy.Evaluate(0.42).HumanReviewRequired);
        Assert.IsTrue(policy.Evaluate(0.75).HumanReviewRequired);
        Assert.IsFalse(policy.Evaluate(0.95).HumanReviewRequired);
    }

    [TestMethod]
    public void Router_ConflictingFieldsRequireHumanReview()
    {
        var decision = Route(OcrDocumentFixtures.ConflictingFieldsFixture(), OcrCostMode.AllowPaidCandidate);

        Assert.AreEqual(OcrRoutingDecisionKind.RecommendMistralDocumentAiCandidate, decision.Decision);
        Assert.IsTrue(decision.HumanReviewRequired);
        Assert.IsFalse(decision.ActionAuthority);
    }

    [TestMethod]
    public void Evidence_RedactsSensitiveTextAndPreservesMetadata()
    {
        var fixture = OcrDocumentFixtures.SensitiveDocumentFixture() with { RedactionApplied = true };
        var decision = Route(fixture, OcrCostMode.FreeOnly);

        var pack = new OcrEvidencePackBuilder().Build(fixture, decision);
        var json = JsonSerializer.Serialize(pack);

        Assert.IsFalse(pack.RawDocumentStored);
        Assert.IsFalse(pack.RawScreenshotStored);
        Assert.IsFalse(pack.ActionAuthority);
        Assert.IsTrue(pack.SourceHash.StartsWith("fixture-sha256-", StringComparison.Ordinal));
        Assert.AreEqual(fixture.FixtureId, pack.FixtureId);
        Assert.IsTrue(pack.RedactionCandidates.Count >= 2);
        Assert.IsTrue(pack.Blocks.All(block => !block.RawTextPresent));
        Assert.IsTrue(pack.Blocks.All(block => block.BoundingBox is not null));
        Assert.IsFalse(json.Contains("123-45-6789", StringComparison.Ordinal));
        Assert.IsFalse(json.Contains("sk-fixture-token-0000", StringComparison.Ordinal));
    }

    [TestMethod]
    public void Evidence_HumanReviewFlagPreservedForLowConfidence()
    {
        var fixture = OcrDocumentFixtures.LowConfidenceFixture();
        var decision = Route(fixture, OcrCostMode.AllowPaidCandidate);
        var pack = new OcrEvidencePackBuilder().Build(fixture, decision);

        Assert.IsTrue(decision.HumanReviewRequired);
        Assert.IsTrue(pack.HumanReviewRequired);
        Assert.AreEqual(OcrConfidenceBand.Low, decision.Confidence.Band);
    }

    [TestMethod]
    public void NoLiveProviderClientEnvironmentOrFakeSuccessExistsInDesignOnlyProject()
    {
        var assembly = typeof(OcrProviderRouter).Assembly;
        var typeNames = assembly.GetTypes().Select(type => type.FullName ?? type.Name).ToList();

        Assert.IsFalse(typeNames.Any(name => name.Contains("HttpClient", StringComparison.OrdinalIgnoreCase)));
        Assert.IsFalse(typeNames.Any(name => name.Contains("WebSocket", StringComparison.OrdinalIgnoreCase)));
        Assert.IsFalse(typeNames.Any(name => name.Contains("MistralClient", StringComparison.OrdinalIgnoreCase)));
        Assert.IsFalse(typeNames.Any(name => name.Contains("LiveSuccess", StringComparison.OrdinalIgnoreCase)));

        var registry = new OcrProviderRegistry();
        Assert.IsTrue(registry.Providers.All(provider => !provider.Policy.NetworkCallsEnabled));
        Assert.IsTrue(registry.Providers.All(provider => !provider.Policy.LiveExecutionEnabled));
        Assert.IsTrue(registry.Providers.All(provider => !provider.Policy.HasLiveClient));
        Assert.IsTrue(registry.Providers.All(provider => !provider.Policy.RequiresApiKey));
    }

    private static OcrProviderRoutingDecision Route(
        OcrFixtureDocument fixture,
        OcrCostMode costMode,
        OcrExecutionMode executionMode = OcrExecutionMode.FixtureOnly) =>
        new OcrProviderRouter().Route(Request(fixture, costMode, executionMode));

    private static OcrProviderRoutingRequest Request(
        OcrFixtureDocument fixture,
        OcrCostMode costMode,
        OcrExecutionMode executionMode = OcrExecutionMode.FixtureOnly) =>
        new(
            fixture.TaskType,
            fixture.RiskLevel,
            fixture.PrivacyLevel,
            costMode,
            executionMode,
            fixture.InputKind,
            fixture.RedactionApplied,
            RequestsActionAuthority: false,
            RequestsBrowserControl: false,
            RequestsDesktopControl: false,
            fixture.ContainsCaptchaLikeChallenge,
            fixture.ContainsLoginLikeFlow,
            fixture.ContainsPaymentLikeFlow,
            fixture.ContainsFiscalSubmissionLikeFlow,
            fixture.ContainsConflictingFields,
            fixture.Confidence);
}
