using Microsoft.VisualStudio.TestTools.UnitTesting;
using OneBrain.Core.Approval;

namespace OneBrain.Recipes.Tests;

[TestClass]
[TestCategory("ApprovalHumanReview")]
[TestCategory("PhaseEApprovalHumanReview")]
public sealed class DurableAuditTrailAppendOnlyCandidateTests
{
    [TestMethod]
    public void Candidate_FixtureIsDeterministicAndStillBlockedFromEnablement()
    {
        var first = DurableAuditTrailAppendOnlyCandidate.CreateFixtureRequest();
        var second = DurableAuditTrailAppendOnlyCandidate.CreateFixtureRequest();

        Assert.AreEqual(first.CandidateId, second.CandidateId);
        Assert.AreEqual(first.TargetScopeId, second.TargetScopeId);
        Assert.AreEqual(first.EventKind, second.EventKind);
        Assert.AreEqual(first.ApprovalReference, second.ApprovalReference);

        var result = DurableAuditTrailAppendOnlyCandidate.Evaluate(first);

        Assert.AreEqual(DurableAuditTrailAppendOnlyCandidateDecision.CandidateAcceptedNoWrite, result.Decision);
        Assert.IsFalse(result.SafeToEnableNow);
        Assert.IsTrue(result.RequiresPostImplementationExternalAuditBeforeEnablement);
        Assert.AreEqual("POST_IMPLEMENTATION_EXTERNAL_AUDIT_REQUIRED_BEFORE_ENABLEMENT", result.EnablementStatus);
    }

    [TestMethod]
    public void Candidate_EnvelopePreviewContainsReferencesWithoutRawPayloadOrProductIo()
    {
        var result = DurableAuditTrailAppendOnlyCandidate.Evaluate(DurableAuditTrailAppendOnlyCandidate.CreateFixtureRequest());

        Assert.IsNotNull(result.Preview);
        Assert.AreEqual("approval.reviewed", result.Preview!.EventKind);
        Assert.AreEqual("approval-request-durable-audit-candidate-001", result.Preview.ApprovalReference);
        CollectionAssert.Contains(result.Preview.EvidenceReferences.ToArray(), "docs/qa/nodal-os-selected-capability-scope-external-audit-read-only/report.md");
        Assert.IsFalse(result.Preview.ContainsRawPayload);
        Assert.IsFalse(result.Preview.FilesystemOutputAllowed);
        Assert.IsFalse(result.Preview.NetworkAllowed);
        Assert.IsFalse(result.Preview.ProductActionAllowed);
    }

    [TestMethod]
    public void Candidate_ExcludesBrowserWcuOcrRecipesAndProviderPaths()
    {
        var result = DurableAuditTrailAppendOnlyCandidate.Evaluate(DurableAuditTrailAppendOnlyCandidate.CreateFixtureRequest());

        Assert.AreEqual(0, result.Counts.NetworkProviderCallCount);
        Assert.AreEqual(0, result.Counts.BrowserCdpLiveActionCount);
        Assert.AreEqual(0, result.Counts.WcuOcrLiveActionCount);
        Assert.AreEqual(0, result.Counts.RecipesExecutionCount);
        CollectionAssert.Contains(result.ExcludedCapabilities.ToArray(), "BROWSER_CDP_LIVE");
        CollectionAssert.Contains(result.ExcludedCapabilities.ToArray(), "WCU_OCR_LIVE");
        CollectionAssert.Contains(result.ExcludedCapabilities.ToArray(), "RECIPES_EXECUTION_REAL");
        CollectionAssert.Contains(result.ExcludedCapabilities.ToArray(), "PROVIDER_CLOUD_NETWORK");
    }

    [TestMethod]
    public void Candidate_ReleaseCommercialAndRuntimeReadinessRemainNoGo()
    {
        var result = DurableAuditTrailAppendOnlyCandidate.Evaluate(DurableAuditTrailAppendOnlyCandidate.CreateFixtureRequest());

        Assert.AreEqual("NO_GO", result.ReleaseCommercialStatus);
        Assert.AreEqual(0, result.RuntimeLiveReadinessPercent);
        Assert.AreEqual(0, result.DurableAuditTrailRealReadinessPercent);
        Assert.AreEqual(0, result.Counts.ReleaseCommercialReadyCount);
        Assert.AreEqual(0, result.Counts.RuntimeInvocationCount);
        Assert.AreEqual(0, result.Counts.AppendWriteCount);
        Assert.AreEqual(0, result.Counts.PersistedEventCount);
    }

    [TestMethod]
    public void Candidate_FailsClosedForMissingNegativeTestsOrNoSideEffectProof()
    {
        var missingNegativeTests = DurableAuditTrailAppendOnlyCandidate.Evaluate(
            DurableAuditTrailAppendOnlyCandidate.CreateFixtureRequest() with
            {
                Gates = DurableAuditTrailAppendOnlyCandidate.CreateFixtureRequest().Gates with { NegativeTestsReady = false }
            });
        var missingNoSideEffectProof = DurableAuditTrailAppendOnlyCandidate.Evaluate(
            DurableAuditTrailAppendOnlyCandidate.CreateFixtureRequest() with
            {
                Gates = DurableAuditTrailAppendOnlyCandidate.CreateFixtureRequest().Gates with { NoSideEffectProofAccepted = false }
            });

        Assert.AreEqual(DurableAuditTrailAppendOnlyCandidateDecision.Blocked, missingNegativeTests.Decision);
        Assert.AreEqual(DurableAuditTrailAppendOnlyCandidateDecision.Blocked, missingNoSideEffectProof.Decision);
        CollectionAssert.Contains(missingNegativeTests.BlockReasons.ToArray(), DurableAuditTrailAppendOnlyCandidateBlockReason.MissingNegativeTests);
        CollectionAssert.Contains(missingNoSideEffectProof.BlockReasons.ToArray(), DurableAuditTrailAppendOnlyCandidateBlockReason.MissingNoSideEffectProof);
    }
}
