using Microsoft.VisualStudio.TestTools.UnitTesting;
using OneBrain.Core.Approval;

namespace OneBrain.Recipes.Tests;

[TestClass]
[TestCategory("ApprovalHumanReview")]
[TestCategory("PhaseEApprovalHumanReview")]
public sealed class ApprovalHumanReviewReadOnlyFoundationTests
{
    [TestMethod]
    public void ApprovalHumanReviewPacket_IsReadOnlyDeterministicAndFixtureSafe()
    {
        var first = ApprovalHumanReviewReadOnlyPresenter.CreateFixture();
        var second = ApprovalHumanReviewReadOnlyPresenter.CreateFixture();

        Assert.IsTrue(first.ReadOnly);
        Assert.IsTrue(first.Deterministic);
        Assert.IsTrue(first.FixtureSafe);
        Assert.IsTrue(first.NoSideEffectProof.Passes);
        Assert.AreEqual("READ_ONLY_FIXTURE_SAFE_NO_APPROVAL_EXECUTION_NO_STATE_MUTATION", first.Mode);
        Assert.AreEqual(first.ReadOnlySummary, second.ReadOnlySummary);
        Assert.AreEqual(0, first.ProductActionCount);
        Assert.AreEqual(0, first.StateMutationCount);
        Assert.IsFalse(first.HasApprovalExecution);
        Assert.IsFalse(first.HasApprovalStateMutation);
        Assert.IsFalse(first.HasDurableMemory);
    }

    [TestMethod]
    public void ApprovalHumanReviewPacket_ContainsMinimumExpectedItems()
    {
        var packet = ApprovalHumanReviewReadOnlyPresenter.CreateFixture();
        var ids = packet.Items.Select(item => item.ItemId).ToHashSet(StringComparer.Ordinal);
        var kinds = packet.Items.Select(item => item.Kind).ToHashSet();

        Assert.AreEqual(27, packet.Items.Count);
        CollectionAssert.IsSubsetOf(
            new[]
            {
                "approval.packet.identity.fixture",
                "human.review.summary",
                "candidate.action.preview",
                "candidate.action.kind",
                "risk.level",
                "risk.rationale",
                "phase.c.evidence.links",
                "phase.d.context.links",
                "authority.freshness.summary",
                "selection.lock.exclusion.summary",
                "memory.candidate.risk.contradiction.summary",
                "required.human.decision",
                "decision.options.preview",
                "approval.blockers",
                "approval.warnings",
                "missing.evidence.blocker",
                "missing.context.blocker",
                "stale.context.blocker",
                "unresolved.contradiction.blocker",
                "critical.risk.blocker",
                "runtime.live.disabled",
                "filesystem.db.disabled",
                "provider.cloud.disabled",
                "durable.memory.disabled",
                "safe.next.step",
                "no.side.effect.proof",
                "deferred.capabilities.debt"
            },
            ids.ToArray());
        Assert.IsTrue(kinds.Contains(ApprovalHumanReviewItemKind.EvidenceLink));
        Assert.IsTrue(kinds.Contains(ApprovalHumanReviewItemKind.ContextLink));
        Assert.IsTrue(kinds.Contains(ApprovalHumanReviewItemKind.DecisionOptionPreview));
        Assert.IsTrue(packet.Items.All(item => item.NoSideEffectProof.Passes));
        Assert.IsTrue(packet.Items.All(item => item.ProductActionCount == 0));
        Assert.IsTrue(packet.Items.All(item => item.StateMutationCount == 0));
    }

    [TestMethod]
    public void ApprovalHumanReviewPacket_LinksPhaseCEvidenceAndPhaseDContextReadOnlySources()
    {
        var packet = ApprovalHumanReviewReadOnlyPresenter.CreateFixture();

        Assert.AreEqual(3, packet.EvidenceLinks.Count);
        Assert.AreEqual(4, packet.ContextLinks.Count);
        Assert.IsTrue(packet.EvidenceLinks.All(link => link.FixtureOnly));
        Assert.IsTrue(packet.EvidenceLinks.All(link => link.PayloadValuesExcluded));
        Assert.IsTrue(packet.EvidenceLinks.All(link => link.NoSideEffectProof.Passes));
        Assert.IsTrue(packet.ContextLinks.All(link => link.FixtureOnly));
        Assert.IsTrue(packet.ContextLinks.All(link => link.NoSideEffectProof.Passes));
        Assert.IsTrue(packet.ContextLinks.Any(link => link.Stale));
        Assert.IsTrue(packet.ContextLinks.Any(link => link.Missing));
        Assert.IsTrue(packet.ContextLinks.Any(link => link.Excluded));
        Assert.IsTrue(packet.SourceLabel.Contains("EvidenceIntelligenceAuditDashboardReadOnlyPresenter.CreateFixture", StringComparison.Ordinal));
        Assert.IsTrue(packet.SourceLabel.Contains("WorkspaceContextPacketExportReadOnlyPresenter.CreateFixture", StringComparison.Ordinal));
    }

    [TestMethod]
    public void ApprovalHumanReviewPacket_ContainsCandidateActionsRiskRationaleAndDecisionOptionsPreview()
    {
        var packet = ApprovalHumanReviewReadOnlyPresenter.CreateFixture();

        Assert.AreEqual(3, packet.CandidateActions.Count);
        Assert.AreEqual(5, packet.DecisionOptions.Count);
        Assert.IsTrue(packet.RiskSummaries.Count >= 4);
        Assert.IsTrue(packet.CandidateActions.All(action => action.HumanReviewRequired));
        Assert.IsTrue(packet.CandidateActions.All(action => action.DecisionAllowedOnlyAsPreview));
        Assert.IsTrue(packet.CandidateActions.All(action => action.ProductActionCount == 0));
        Assert.IsTrue(packet.CandidateActions.All(action => action.StateMutationCount == 0));
        Assert.IsTrue(packet.DecisionOptions.All(option => option.PreviewOnly));
        Assert.IsTrue(packet.DecisionOptions.All(option => !option.ExecutesAction));
        Assert.IsTrue(packet.DecisionOptions.All(option => !option.MutatesState));
        Assert.IsTrue(packet.RiskSummaries.Any(risk => risk.RiskLevel == ApprovalRiskLevel.Critical && risk.BlocksDecision));
        Assert.IsTrue(packet.RiskSummaries.Any(risk => risk.BlocksSafeNextStep));
    }

    [TestMethod]
    public void ApprovalHumanReviewPacket_BlocksMissingEvidenceMissingContextStaleContextContradictionAndCriticalRisk()
    {
        var packet = ApprovalHumanReviewReadOnlyPresenter.CreateFixture();
        var blockers = string.Join("\n", packet.Blockers.Concat(packet.Items.SelectMany(item => item.Blockers)).Concat(packet.RiskSummaries.SelectMany(risk => risk.Blockers)));

        StringAssert.Contains(blockers, "Missing evidence blocks approval decision use.");
        StringAssert.Contains(blockers, "Missing context blocks approval decision use.");
        StringAssert.Contains(blockers, "Stale context blocks approval decision use.");
        StringAssert.Contains(blockers, "Unresolved contradiction blocks safe next step approval.");
        StringAssert.Contains(blockers, "Critical risk blocks approval decision use.");
        Assert.IsTrue(packet.Items.Any(item => item.Kind == ApprovalHumanReviewItemKind.MissingEvidenceBlocker && item.Status == ApprovalHumanReviewItemStatus.Blocked));
        Assert.IsTrue(packet.Items.Any(item => item.Kind == ApprovalHumanReviewItemKind.MissingContextBlocker && item.Status == ApprovalHumanReviewItemStatus.Blocked));
        Assert.IsTrue(packet.Items.Any(item => item.Kind == ApprovalHumanReviewItemKind.StaleContextBlocker && item.Status == ApprovalHumanReviewItemStatus.Blocked));
        Assert.IsTrue(packet.Items.Any(item => item.Kind == ApprovalHumanReviewItemKind.UnresolvedContradictionBlocker && item.Status == ApprovalHumanReviewItemStatus.Blocked));
        Assert.IsTrue(packet.Items.Any(item => item.Kind == ApprovalHumanReviewItemKind.CriticalRiskBlocker && item.Status == ApprovalHumanReviewItemStatus.Blocked));
    }

    [TestMethod]
    public void ApprovalHumanReviewPacket_DisablesRuntimeProviderFilesystemDbAndDurableMemory()
    {
        var packet = ApprovalHumanReviewReadOnlyPresenter.CreateFixture();
        var proof = packet.NoSideEffectProof;
        var text = string.Join("\n", packet.Items.Select(item => $"{item.Title} {item.Summary} {string.Join(" ", item.Blockers)}"));

        StringAssert.Contains(text, "Runtime/live");
        StringAssert.Contains(text, "Filesystem/DB");
        StringAssert.Contains(text, "Provider/cloud");
        StringAssert.Contains(text, "Durable memory");
        Assert.IsFalse(proof.FilesystemReadAttempted);
        Assert.IsFalse(proof.FilesystemWriteAttempted);
        Assert.IsFalse(proof.DatabaseTouched);
        Assert.IsFalse(proof.DurablePersistenceActive);
        Assert.IsFalse(proof.DurableMemoryActive);
        Assert.IsFalse(proof.VectorSemanticBackendTouched);
        Assert.IsFalse(proof.LlmProviderTouched);
        Assert.IsFalse(proof.ProviderCloudTouched);
        Assert.IsFalse(proof.RuntimeTouched);
        Assert.IsFalse(proof.RecipeExecutionStarted);
        Assert.IsFalse(proof.ApprovalExecutionStarted);
        Assert.IsFalse(proof.ApprovalStateMutationAttempted);
    }

    [TestMethod]
    public void ApprovalHumanReviewPacket_SafeNextStepIsReadOnlyGuardHardening()
    {
        var packet = ApprovalHumanReviewReadOnlyPresenter.CreateFixture();

        Assert.AreEqual("PHASE_E_APPROVAL_RISK_DECISION_GUARDS", packet.SafeNextStep);
        StringAssert.Contains(packet.ReadOnlySummary, "PHASE_E_APPROVAL_RISK_DECISION_GUARDS");
        StringAssert.Contains(packet.ReadOnlySummary, "Approval preview is not approval execution.");
        StringAssert.Contains(packet.ReadOnlySummary, "Human review packet is not state mutation.");
    }

    [TestMethod]
    public void ApprovalHumanReviewPacket_HasNoSecretLeakageOrProductionClaim()
    {
        var packet = ApprovalHumanReviewReadOnlyPresenter.CreateFixture();
        var text = string.Join(
            "\n",
            packet.ReadOnlySummary,
            packet.Summary,
            string.Join("\n", packet.Items.Select(item => $"{item.Title} {item.Summary} {string.Join(" ", item.Warnings)} {string.Join(" ", item.Blockers)}")),
            string.Join("\n", packet.CandidateActions.Select(action => action.Summary)),
            string.Join("\n", packet.DecisionOptions.Select(option => option.Summary)));

        Assert.IsFalse(text.Contains("sk-", StringComparison.OrdinalIgnoreCase));
        Assert.IsFalse(text.Contains("Bearer ", StringComparison.OrdinalIgnoreCase));
        Assert.IsFalse(text.Contains("PRIVATE KEY", StringComparison.OrdinalIgnoreCase));
        Assert.IsFalse(text.Contains("AKIA", StringComparison.OrdinalIgnoreCase));
        Assert.IsFalse(text.Contains("ghp_", StringComparison.OrdinalIgnoreCase));
        Assert.IsFalse(text.Contains("production" + "-ready", StringComparison.OrdinalIgnoreCase));
        Assert.IsFalse(text.Contains("approval executed", StringComparison.OrdinalIgnoreCase));
        Assert.IsFalse(text.Contains("state mutation completed", StringComparison.OrdinalIgnoreCase));
        Assert.IsFalse(text.Contains("durable memory active", StringComparison.OrdinalIgnoreCase));
        Assert.IsFalse(text.Contains("provider call enabled", StringComparison.OrdinalIgnoreCase));
    }
}
