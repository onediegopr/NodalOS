using Microsoft.VisualStudio.TestTools.UnitTesting;
using OneBrain.Core.Approval;

namespace OneBrain.Recipes.Tests;

[TestClass]
[TestCategory("ApprovalHumanReview")]
[TestCategory("PhaseEApprovalHumanReview")]
public sealed class ReentryDecisionPacketReadOnlyTests
{
    [TestMethod]
    public void ReentryDecisionPacket_IsDeterministicReadOnlySurface()
    {
        var first = ReentryDecisionPacketReadOnlyPresenter.CreateFixture();
        var second = ReentryDecisionPacketReadOnlyPresenter.CreateFixture();

        Assert.AreEqual("nodal-os.reentry.decision-packet.read-only.fixture.v1", first.PacketId);
        Assert.AreEqual(ReentryDecisionPacketStatus.ReadOnly, first.Status);
        Assert.AreEqual(first.PacketId, second.PacketId);
        Assert.AreEqual(first.GeneratedAtUtc, second.GeneratedAtUtc);
        Assert.AreEqual(first.Mode, second.Mode);
        CollectionAssert.AreEqual(first.EvidenceLinks.ToArray(), second.EvidenceLinks.ToArray());
        Assert.IsTrue(first.PassesSafetyProof);
    }

    [TestMethod]
    public void ReentryDecisionPacket_CoversClosedTracksAndEvidenceLinks()
    {
        var packet = ReentryDecisionPacketReadOnlyPresenter.CreateFixture();

        Assert.IsTrue(packet.TrackClosureSummary.Count >= 7);
        Assert.IsTrue(packet.TrackClosureSummary.All(track => track.Closed));
        Assert.IsTrue(packet.TrackClosureSummary.All(track => track.Audited));
        Assert.IsTrue(packet.TrackClosureSummary.All(track => track.ReadOnly));
        Assert.IsTrue(packet.TrackClosureSummary.All(track => !track.OpensRealCapability));
        CollectionAssert.Contains(packet.EvidenceLinks.ToList(), "docs/qa/nodal-os-canonical-status-docs-hardening/report.md");
        CollectionAssert.Contains(packet.EvidenceLinks.ToList(), "docs/decision-log.md");
    }

    [TestMethod]
    public void ReentryDecisionPacket_SeparatesDocsReadinessFromImplementationReadiness()
    {
        var readiness = ReentryDecisionPacketReadOnlyPresenter.CreateFixture().CapabilityReadinessSummary;

        Assert.AreEqual("92-95%", readiness.DocsCanonicalConsistency);
        Assert.AreEqual("100% closed/audited", readiness.PrivacyExportControlledExecutionDesign);
        Assert.AreEqual("20-30%", readiness.ProductUsableEndToEndEstimate);
        Assert.AreEqual(0, readiness.RuntimeLiveRealPercent);
        Assert.AreEqual(0, readiness.PhysicalExportRealPercent);
        Assert.AreEqual(0, readiness.RedactionRuntimeRealPercent);
        Assert.AreEqual(0, readiness.RetentionDeletionRuntimeRealPercent);
        Assert.AreEqual("NO-GO", readiness.ReleaseCommercialReadiness);
    }

    [TestMethod]
    public void ReentryDecisionPacket_RecommendsPauseOrDesignOnlyPlanningNotRealCapability()
    {
        var packet = ReentryDecisionPacketReadOnlyPresenter.CreateFixture();
        var optionIds = packet.NextSafeOptions.Select(option => option.OptionId).ToList();

        CollectionAssert.Contains(optionIds, "PAUSE_AFTER_REENTRY_PACKET_NO_CHANGES");
        CollectionAssert.Contains(optionIds, "NODAL_OS_IMPLEMENTATION_PLANNING_GATE_DESIGN_ONLY");
        CollectionAssert.Contains(optionIds, "NODAL_OS_READ_ONLY_PRODUCT_STATUS_SURFACE_EXPANSION");
        CollectionAssert.Contains(optionIds, "NODAL_OS_EXTERNAL_AUDIT_PRE_RUNTIME_GATE_READ_ONLY");
        StringAssert.Contains(packet.HumanOperatorRecommendation, "design-only implementation planning");
        StringAssert.Contains(packet.HumanOperatorRecommendation, "do not open runtime/live");
        Assert.IsFalse(packet.NextSafeOptions.Any(option => option.OpensRealCapability));
    }
}
