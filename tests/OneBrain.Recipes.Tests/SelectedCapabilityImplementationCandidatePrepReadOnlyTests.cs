using Microsoft.VisualStudio.TestTools.UnitTesting;
using OneBrain.Core.Approval;

namespace OneBrain.Recipes.Tests;

[TestClass]
[TestCategory("ApprovalHumanReview")]
[TestCategory("PhaseEApprovalHumanReview")]
public sealed class SelectedCapabilityImplementationCandidatePrepReadOnlyTests
{
    [TestMethod]
    public void PrepPacket_FixtureIsDeterministicAndPrepOnly()
    {
        var first = SelectedCapabilityImplementationCandidatePrepReadOnlyPresenter.CreateFixture();
        var second = SelectedCapabilityImplementationCandidatePrepReadOnlyPresenter.CreateFixture();

        Assert.AreEqual(first.PacketId, second.PacketId);
        Assert.AreEqual(first.GeneratedAtUtc, second.GeneratedAtUtc);
        Assert.AreEqual(first.SelectedCapability, second.SelectedCapability);
        Assert.AreEqual(first.CandidateStatus, second.CandidateStatus);
        Assert.IsTrue(first.PassesPrepOnlySafetyProof);
        Assert.IsTrue(second.PassesPrepOnlySafetyProof);
    }

    [TestMethod]
    public void PrepPacket_PositiveTestsRemainFutureOnlyAndWithoutRealIo()
    {
        var tests = SelectedCapabilityImplementationCandidatePrepReadOnlyPresenter.CreateFixture().FuturePositiveTests;

        Assert.IsTrue(tests.Count >= 7);
        Assert.IsTrue(tests.All(test => !test.RequiresRealIo));
        Assert.IsTrue(tests.All(test => test.Status == CandidatePrepRequirementStatus.BlockedPendingUserGo));
    }

    [TestMethod]
    public void PrepPacket_FailClosedPlanBlocksMissingGatesAndSideEffects()
    {
        var rules = SelectedCapabilityImplementationCandidatePrepReadOnlyPresenter.CreateFixture().FailClosedPlan;
        var triggers = rules.Select(rule => rule.Trigger).ToList();

        CollectionAssert.Contains(triggers, "missing user GO");
        CollectionAssert.Contains(triggers, "missing external audit");
        CollectionAssert.Contains(triggers, "missing scope lock");
        CollectionAssert.Contains(triggers, "unexpected path");
        CollectionAssert.Contains(triggers, "service registration attempted");
        CollectionAssert.Contains(triggers, "command handler attempted");
        CollectionAssert.Contains(triggers, "provider/network call attempted");
        CollectionAssert.Contains(triggers, "product IO outside scope attempted");
        Assert.IsTrue(rules.All(rule => rule.Result == "blocked" && rule.RequiredBeforeImplementation));
    }

    [TestMethod]
    public void PrepPacket_NoSideEffectProofRequiresCountersAndProhibitions()
    {
        var proof = SelectedCapabilityImplementationCandidatePrepReadOnlyPresenter.CreateFixture().NoSideEffectProofPlan;

        CollectionAssert.Contains(proof.RequiredCounters.ToList(), "DurableAuditTrailRealEnabledCount");
        CollectionAssert.Contains(proof.RequiredCounters.ToList(), "AppendWriteEnabledCount");
        CollectionAssert.Contains(proof.RequiredCounters.ToList(), "ServiceRegistrationCount");
        CollectionAssert.Contains(proof.RequiredCounters.ToList(), "CommandHandlerCount");
        CollectionAssert.Contains(proof.RequiredCounters.ToList(), "FilesystemOutputCount");
        CollectionAssert.Contains(proof.RequiredCounters.ToList(), "DbMigrationCount");
        CollectionAssert.Contains(proof.RequiredDryRunSignals.ToList(), "future implementation prompt remains BLOCKED_NOT_EXECUTABLE");
        CollectionAssert.Contains(proof.ProhibitedSideEffects.ToList(), "service registration");
        CollectionAssert.Contains(proof.ProhibitedSideEffects.ToList(), "command handlers");
        CollectionAssert.Contains(proof.ProhibitedSideEffects.ToList(), "filesystem product IO");
    }

    [TestMethod]
    public void PrepPacket_WordingDoesNotClaimImplementationEnablementOrRelease()
    {
        var packet = SelectedCapabilityImplementationCandidatePrepReadOnlyPresenter.CreateFixture();
        var text = string.Join(
            "\n",
            packet.CandidateStatus,
            packet.MaximumDecisionAllowed,
            packet.BlockedFutureImplementationPrompt.Header,
            packet.BlockedFutureImplementationPrompt.Status,
            packet.HumanOperatorRecommendation,
            string.Join("\n", packet.ModuleFileCandidateMap.Select(entry => $"{entry.Path} {entry.Purpose} {entry.InScopeReason}")),
            string.Join("\n", packet.RequiredNegativeTests.Select(test => test.Assertion)));

        var forbidden = new[]
        {
            "APPROVED_FOR_IMPLEMENTATION",
            "READY_TO_IMPLEMENT",
            "READY_TO_RUN",
            "READY_FOR_RUNTIME",
            "SAFE_TO_RUN",
            "RELEASE_READY",
            "COMMERCIAL_READY",
            "safe to implement now",
            "implementation is approved",
            "capability is enabled",
            "runtime ready",
            "release ready",
            "commercial ready"
        };

        foreach (var term in forbidden)
        {
            Assert.IsFalse(text.Contains(term, StringComparison.OrdinalIgnoreCase), term);
        }
    }
}
