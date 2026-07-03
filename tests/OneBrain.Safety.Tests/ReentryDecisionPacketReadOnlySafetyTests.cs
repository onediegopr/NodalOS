using Microsoft.VisualStudio.TestTools.UnitTesting;
using OneBrain.Core.Approval;

namespace OneBrain.Safety.Tests;

[TestClass]
[TestCategory("ApprovalHumanReview")]
[TestCategory("PhaseEApprovalHumanReview")]
public sealed class ReentryDecisionPacketReadOnlySafetyTests
{
    [TestMethod]
    public void ReentryDecisionPacket_KeepsCanonicalPausedStateAndRealReadinessAtZero()
    {
        var packet = ReentryDecisionPacketReadOnlyPresenter.CreateFixture();

        Assert.AreEqual("PAUSED_READ_ONLY_NO_RUNTIME_NO_EXECUTION_NO_MUTATION_NO_PHYSICAL_EXPORT_NO_REDACTION_RUNTIME", packet.CanonicalState);
        Assert.AreEqual(0, packet.CapabilityReadinessSummary.RuntimeLiveRealPercent);
        Assert.AreEqual(0, packet.CapabilityReadinessSummary.ExecutionRealPercent);
        Assert.AreEqual(0, packet.CapabilityReadinessSummary.MutationRealPercent);
        Assert.AreEqual(0, packet.CapabilityReadinessSummary.PhysicalExportRealPercent);
        Assert.AreEqual(0, packet.CapabilityReadinessSummary.RedactionRuntimeRealPercent);
        Assert.AreEqual(0, packet.CapabilityReadinessSummary.SecretPiiScanRealPercent);
        Assert.AreEqual(0, packet.CapabilityReadinessSummary.RetentionDeletionRuntimeRealPercent);
        Assert.AreEqual("NO-GO", packet.CapabilityReadinessSummary.ReleaseCommercialReadiness);
        Assert.IsTrue(packet.CapabilityReadinessSummary.KeepsRealReadinessAtZero);
    }

    [TestMethod]
    public void ReentryDecisionPacket_HasNoRealCapabilityFlagsOrActionCounts()
    {
        var packet = ReentryDecisionPacketReadOnlyPresenter.CreateFixture();

        Assert.IsTrue(packet.CapabilityStatus.NoRealCapabilities);
        Assert.IsTrue(packet.NoSideEffectProof.Counts.AllZero);
        Assert.AreEqual(0, packet.ProductActionCount);
        Assert.AreEqual(0, packet.StateMutationCount);
        Assert.AreEqual(0, packet.ExportActionCount);
        Assert.AreEqual(0, packet.FileOutputCount);
        Assert.AreEqual(0, packet.RedactionActionCount);
        Assert.AreEqual(0, packet.RetentionActionCount);
        Assert.AreEqual(0, packet.DeletionActionCount);
        Assert.AreEqual(0, packet.ServiceRegistrationCount);
        Assert.AreEqual(0, packet.CommandHandlerCount);
        Assert.AreEqual(0, packet.RuntimeInvocationCount);
        Assert.AreEqual(0, packet.ProviderNetworkCallCount);
        Assert.AreEqual(0, packet.BrowserCdpLiveActionCount);
        Assert.AreEqual(0, packet.WcuOcrLiveActionCount);
    }

    [TestMethod]
    public void ReentryDecisionPacket_NoSideEffectProofPassesAndUsesDocEvidenceOnly()
    {
        var packet = ReentryDecisionPacketReadOnlyPresenter.CreateFixture();

        Assert.IsTrue(packet.NoSideEffectProof.Passes);
        Assert.IsTrue(packet.NoSideEffectProof.ReadOnly);
        Assert.IsTrue(packet.NoSideEffectProof.Deterministic);
        Assert.IsTrue(packet.NoSideEffectProof.FixtureSafe);
        Assert.IsTrue(packet.NoSideEffectProof.UsesOnlyDocEvidenceRefs);
        Assert.IsTrue(packet.EvidenceLinks.All(link => link.StartsWith("docs/", StringComparison.Ordinal)));
        Assert.IsFalse(packet.NoSideEffectProof.ApprovalProof.RuntimeTouched);
        Assert.IsFalse(packet.NoSideEffectProof.ApprovalProof.ApprovalExecutionStarted);
        Assert.IsFalse(packet.NoSideEffectProof.ApprovalProof.ApprovalStateMutationAttempted);
        Assert.IsFalse(packet.NoSideEffectProof.ApprovalProof.ProductActionExposed);
        Assert.IsFalse(packet.NoSideEffectProof.ApprovalProof.ProductServiceRegistered);
    }

    [TestMethod]
    public void ReentryDecisionPacket_NextSafeOptionsDoNotOpenRuntimeExportMutationOrExecution()
    {
        var packet = ReentryDecisionPacketReadOnlyPresenter.CreateFixture();

        Assert.IsTrue(packet.NextSafeOptions.Count >= 4);
        Assert.IsTrue(packet.NextSafeOptions.All(option => !option.OpensRealCapability));
        Assert.IsTrue(packet.NextSafeOptions.All(option => option.ReadOnly));
        Assert.IsFalse(packet.NextSafeOptions.Any(option => option.OptionId.Contains("RUNTIME_LIVE_REAL", StringComparison.OrdinalIgnoreCase)));
        Assert.IsFalse(packet.NextSafeOptions.Any(option => option.OptionId.Contains("PHYSICAL_EXPORT_REAL", StringComparison.OrdinalIgnoreCase)));
        Assert.IsFalse(packet.NextSafeOptions.Any(option => option.OptionId.Contains("APPROVAL_EXECUTION_REAL", StringComparison.OrdinalIgnoreCase)));
        Assert.IsFalse(packet.NextSafeOptions.Any(option => option.OptionId.Contains("APPROVAL_MUTATION_REAL", StringComparison.OrdinalIgnoreCase)));
        Assert.IsTrue(packet.BlockedRealCapabilityOptions.All(option => option.Status == ReentryNextSafeOptionStatus.BlockedNoGo));
        Assert.IsTrue(packet.BlockedRealCapabilityOptions.All(option => option.OpensRealCapability));
    }

    [TestMethod]
    public void ReentryDecisionPacket_RequiresExternalAuditBeforeSensitiveCapabilities()
    {
        var packet = ReentryDecisionPacketReadOnlyPresenter.CreateFixture();
        var capabilities = packet.RequiredExternalAuditGates.Select(gate => gate.Capability).ToList();

        CollectionAssert.Contains(capabilities, "runtime/live");
        CollectionAssert.Contains(capabilities, "approval execution");
        CollectionAssert.Contains(capabilities, "approval mutation");
        CollectionAssert.Contains(capabilities, "physical export");
        CollectionAssert.Contains(capabilities, "redaction runtime");
        CollectionAssert.Contains(capabilities, "secret/PII scan");
        CollectionAssert.Contains(capabilities, "retention/deletion runtime");
        Assert.IsTrue(packet.RequiredExternalAuditGates.All(gate => gate.RequiredBeforeImplementation));
        Assert.IsTrue(packet.RequiredExternalAuditGates.All(gate => !gate.SatisfiedNow));
        Assert.IsTrue(packet.RequiredExternalAuditGates.All(gate => gate.Status == "REQUIRED_BEFORE_IMPLEMENTATION"));
    }

    [TestMethod]
    public void ReentryDecisionPacket_TextHasNoActiveReadinessOverclaim()
    {
        var packet = ReentryDecisionPacketReadOnlyPresenter.CreateFixture();
        var text = string.Join(
            "\n",
            packet.Mode,
            packet.ReleaseCommercialStatus,
            packet.HumanOperatorRecommendation,
            string.Join("\n", packet.Warnings),
            string.Join("\n", packet.Blockers),
            string.Join("\n", packet.NextSafeOptions.Select(option => option.Title)),
            string.Join("\n", packet.BlockedRealCapabilityOptions.Select(option => $"{option.OptionId} {option.Title}")));

        var forbidden = new[]
        {
            "production" + "-ready",
            "release" + "-ready",
            "runtime enabled",
            "export enabled",
            "approval executed",
            "state mutation completed",
            "physical export enabled",
            "redaction implemented",
            "retention implemented",
            "deletion implemented"
        };

        foreach (var term in forbidden)
        {
            Assert.IsFalse(text.Contains(term, StringComparison.OrdinalIgnoreCase), term);
        }
    }
}
