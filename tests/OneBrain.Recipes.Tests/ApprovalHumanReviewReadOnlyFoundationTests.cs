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

    [TestMethod]
    public void ApprovalRiskDecisionGuard_CoversExpectedFixtureCatalog()
    {
        var fixtures = ApprovalRiskDecisionReadOnlyGuard.CreateFixtureCatalog();
        var results = ApprovalRiskDecisionReadOnlyGuard.EvaluateCatalog();

        Assert.AreEqual(26, fixtures.Count);
        Assert.AreEqual(fixtures.Count, results.Count);

        foreach (var fixture in fixtures)
        {
            var result = results.Single(item => item.FixtureId == fixture.FixtureId);

            Assert.AreEqual(fixture.ExpectedDecision, result.Decision, fixture.FixtureId);
            if (fixture.ExpectedIssue == ApprovalRiskDecisionReadOnlyIssueKind.None)
            {
                Assert.AreEqual(0, result.Issues.Count, fixture.FixtureId);
            }
            else
            {
                Assert.IsTrue(result.HasIssue(fixture.ExpectedIssue), fixture.FixtureId);
            }

            Assert.IsTrue(result.PreviewOnly, fixture.FixtureId);
            Assert.IsFalse(result.ApprovalExecutionAllowed, fixture.FixtureId);
            Assert.IsFalse(result.StateMutationAllowed, fixture.FixtureId);
            Assert.IsFalse(result.ProductActionAllowed, fixture.FixtureId);
            Assert.IsTrue(result.NoSideEffectProof.Passes, fixture.FixtureId);
        }
    }

    [TestMethod]
    public void ApprovalRiskDecisionGuard_AllowsOnlySafePreviewLabels()
    {
        var results = ApprovalRiskDecisionReadOnlyGuard.EvaluateCatalog();

        AssertRiskDecision(results, "approval.low-risk.evidence-context", ApprovalRiskDecisionReadOnlyDecision.AllowedPreviewOnly, ApprovalRiskDecisionReadOnlyIssueKind.None);
        AssertRiskDecision(results, "approval.reject-preview", ApprovalRiskDecisionReadOnlyDecision.AllowedPreviewOnly, ApprovalRiskDecisionReadOnlyIssueKind.None);
        AssertRiskDecision(results, "approval.request-more-evidence", ApprovalRiskDecisionReadOnlyDecision.AllowedPreviewOnly, ApprovalRiskDecisionReadOnlyIssueKind.None);
        AssertRiskDecision(results, "approval.request-context-refresh", ApprovalRiskDecisionReadOnlyDecision.AllowedPreviewOnly, ApprovalRiskDecisionReadOnlyIssueKind.None);
        AssertRiskDecision(results, "approval.defer-decision", ApprovalRiskDecisionReadOnlyDecision.AllowedPreviewOnly, ApprovalRiskDecisionReadOnlyIssueKind.None);
        AssertRiskDecision(results, "approval.medium-risk.human-review", ApprovalRiskDecisionReadOnlyDecision.NeedsHumanReview, ApprovalRiskDecisionReadOnlyIssueKind.HumanReviewRequired);
        AssertRiskDecision(results, "approval.fixture-only", ApprovalRiskDecisionReadOnlyDecision.WarningPreviewOnly, ApprovalRiskDecisionReadOnlyIssueKind.FixtureOnlyNotProductionTrusted);
    }

    [TestMethod]
    public void ApprovalRiskDecisionGuard_BlocksUnsafeRiskEvidenceContextAndContradictions()
    {
        var results = ApprovalRiskDecisionReadOnlyGuard.EvaluateCatalog();

        AssertRiskDecision(results, "approval.critical-risk", ApprovalRiskDecisionReadOnlyDecision.Blocked, ApprovalRiskDecisionReadOnlyIssueKind.CriticalRisk);
        AssertRiskDecision(results, "approval.missing-evidence", ApprovalRiskDecisionReadOnlyDecision.Blocked, ApprovalRiskDecisionReadOnlyIssueKind.MissingEvidence);
        AssertRiskDecision(results, "approval.missing-context", ApprovalRiskDecisionReadOnlyDecision.Blocked, ApprovalRiskDecisionReadOnlyIssueKind.MissingContext);
        AssertRiskDecision(results, "approval.stale-context", ApprovalRiskDecisionReadOnlyDecision.Blocked, ApprovalRiskDecisionReadOnlyIssueKind.StaleContext);
        AssertRiskDecision(results, "approval.excluded-context", ApprovalRiskDecisionReadOnlyDecision.Blocked, ApprovalRiskDecisionReadOnlyIssueKind.ExcludedContext);
        AssertRiskDecision(results, "approval.unresolved-contradiction", ApprovalRiskDecisionReadOnlyDecision.Blocked, ApprovalRiskDecisionReadOnlyIssueKind.UnresolvedContradiction);
        AssertRiskDecision(results, "approval.approve-critical-risk", ApprovalRiskDecisionReadOnlyDecision.Blocked, ApprovalRiskDecisionReadOnlyIssueKind.ApproveWithCriticalRisk);
        AssertRiskDecision(results, "approval.approve-without-evidence", ApprovalRiskDecisionReadOnlyDecision.Blocked, ApprovalRiskDecisionReadOnlyIssueKind.ApproveWithoutEvidence);
        AssertRiskDecision(results, "approval.conflicting-risk-summaries", ApprovalRiskDecisionReadOnlyDecision.Blocked, ApprovalRiskDecisionReadOnlyIssueKind.ConflictingRiskSummaries);
        AssertRiskDecision(results, "approval.human-review-missing", ApprovalRiskDecisionReadOnlyDecision.Blocked, ApprovalRiskDecisionReadOnlyIssueKind.HumanReviewMissing);
    }

    [TestMethod]
    public void ApprovalRiskDecisionGuard_BlocksActionRuntimeProviderSemanticLlmAndMutationImplications()
    {
        var results = ApprovalRiskDecisionReadOnlyGuard.EvaluateCatalog();

        AssertRiskDecision(results, "approval.provider-derived-disabled", ApprovalRiskDecisionReadOnlyDecision.Blocked, ApprovalRiskDecisionReadOnlyIssueKind.ProviderCloudDerivedWhileDisabled);
        AssertRiskDecision(results, "approval.semantic-derived-disabled", ApprovalRiskDecisionReadOnlyDecision.Blocked, ApprovalRiskDecisionReadOnlyIssueKind.SemanticVectorDerivedWhileDisabled);
        AssertRiskDecision(results, "approval.llm-derived-disabled", ApprovalRiskDecisionReadOnlyDecision.Blocked, ApprovalRiskDecisionReadOnlyIssueKind.LlmDerivedWhileDisabled);
        AssertRiskDecision(results, "approval.action-filesystem-write", ApprovalRiskDecisionReadOnlyDecision.Blocked, ApprovalRiskDecisionReadOnlyIssueKind.FilesystemWriteImplication);
        AssertRiskDecision(results, "approval.action-runtime-live", ApprovalRiskDecisionReadOnlyDecision.Blocked, ApprovalRiskDecisionReadOnlyIssueKind.RuntimeLiveImplication);
        AssertRiskDecision(results, "approval.action-state-mutation", ApprovalRiskDecisionReadOnlyDecision.Blocked, ApprovalRiskDecisionReadOnlyIssueKind.ApprovalStateMutationImplication);
        AssertRiskDecision(results, "approval.option-product-action-count", ApprovalRiskDecisionReadOnlyDecision.Blocked, ApprovalRiskDecisionReadOnlyIssueKind.ProductActionCountNonZero);
        AssertRiskDecision(results, "approval.option-state-mutation-count", ApprovalRiskDecisionReadOnlyDecision.Blocked, ApprovalRiskDecisionReadOnlyIssueKind.StateMutationCountNonZero);
        AssertRiskDecision(results, "approval.raw-sensitive-payload", ApprovalRiskDecisionReadOnlyDecision.Excluded, ApprovalRiskDecisionReadOnlyIssueKind.RawSensitivePayload);
    }

    [TestMethod]
    public void ApprovalRiskDecisionGuard_HasNoApprovalHumanReviewOrProductionOverclaim()
    {
        var text = string.Join(
            "\n",
            ApprovalRiskDecisionReadOnlyGuard.EvaluateCatalog()
                .Select(result => $"{result.FixtureId} {result.Decision} {string.Join(" ", result.Warnings)} {string.Join(" ", result.Blockers)}"));

        var forbidden = new[]
        {
            "production" + "-ready",
            "approval executed",
            "approval execution completed",
            "approval state mutated",
            "state mutation completed",
            "approved and applied",
            "recipe executed",
            "runtime action enabled",
            "live automation enabled",
            "provider call enabled",
            "semantic search enabled",
            "vector backend enabled",
            "durable memory active",
            "memory persisted"
        };

        foreach (var term in forbidden)
        {
            Assert.IsFalse(text.Contains(term, StringComparison.OrdinalIgnoreCase), term);
        }
    }

    [TestMethod]
    public void HumanReviewEvidenceContextLinkGuard_CoversExpectedFixtureCatalog()
    {
        var fixtures = HumanReviewEvidenceContextLinkReadOnlyGuard.CreateFixtureCatalog();
        var results = HumanReviewEvidenceContextLinkReadOnlyGuard.EvaluateCatalog();

        Assert.AreEqual(27, fixtures.Count);
        Assert.AreEqual(fixtures.Count, results.Count);

        foreach (var fixture in fixtures)
        {
            var result = results.Single(item => item.FixtureId == fixture.FixtureId);

            Assert.AreEqual(fixture.ExpectedDecision, result.Decision, fixture.FixtureId);
            if (fixture.ExpectedIssue == HumanReviewEvidenceContextLinkIssueKind.None)
            {
                Assert.AreEqual(0, result.Issues.Count, fixture.FixtureId);
            }
            else
            {
                Assert.IsTrue(result.HasIssue(fixture.ExpectedIssue), fixture.FixtureId);
            }

            Assert.IsTrue(result.PreviewOnly, fixture.FixtureId);
            Assert.IsFalse(result.EvidenceLinkIsDurableEvidence, fixture.FixtureId);
            Assert.IsFalse(result.ContextLinkTrustedByDefault, fixture.FixtureId);
            Assert.IsFalse(result.ApprovalExecutionAllowed, fixture.FixtureId);
            Assert.IsFalse(result.StateMutationAllowed, fixture.FixtureId);
            Assert.IsFalse(result.ProductActionAllowed, fixture.FixtureId);
            Assert.IsFalse(result.ServiceRegistrationAllowed, fixture.FixtureId);
            Assert.IsTrue(result.NoSideEffectProof.Passes, fixture.FixtureId);
        }
    }

    [TestMethod]
    public void HumanReviewEvidenceContextLinkGuard_AllowsOnlyPreviewForValidAndWarningLinks()
    {
        var results = HumanReviewEvidenceContextLinkReadOnlyGuard.EvaluateCatalog();

        AssertLinkDecision(results, "link.valid-evidence-context", HumanReviewEvidenceContextLinkDecision.AllowedPreviewOnly, HumanReviewEvidenceContextLinkIssueKind.None);
        AssertLinkDecision(results, "link.fixture-only-evidence", HumanReviewEvidenceContextLinkDecision.WarningPreviewOnly, HumanReviewEvidenceContextLinkIssueKind.FixtureOnlyEvidenceNotProductionTrusted);
        AssertLinkDecision(results, "link.context-requires-human-review", HumanReviewEvidenceContextLinkDecision.NeedsHumanReview, HumanReviewEvidenceContextLinkIssueKind.ContextRequiresHumanReview);
        Assert.IsTrue(results.Single(result => result.FixtureId == "link.valid-evidence-context").AllowsApprovalPreview);
        Assert.IsTrue(results.Single(result => result.FixtureId == "link.fixture-only-evidence").AllowsApprovalPreview);
        Assert.IsTrue(results.Single(result => result.FixtureId == "link.context-requires-human-review").AllowsApprovalPreview);
    }

    [TestMethod]
    public void HumanReviewEvidenceContextLinkGuard_BlocksMissingStaleExcludedUnknownAndMismatchLinks()
    {
        var results = HumanReviewEvidenceContextLinkReadOnlyGuard.EvaluateCatalog();

        AssertLinkDecision(results, "link.missing-evidence", HumanReviewEvidenceContextLinkDecision.Blocked, HumanReviewEvidenceContextLinkIssueKind.MissingEvidenceLink);
        AssertLinkDecision(results, "link.missing-context", HumanReviewEvidenceContextLinkDecision.Blocked, HumanReviewEvidenceContextLinkIssueKind.MissingContextLink);
        AssertLinkDecision(results, "link.stale-context", HumanReviewEvidenceContextLinkDecision.Blocked, HumanReviewEvidenceContextLinkIssueKind.StaleContextLink);
        AssertLinkDecision(results, "link.excluded-context", HumanReviewEvidenceContextLinkDecision.Excluded, HumanReviewEvidenceContextLinkIssueKind.ExcludedContextLink);
        AssertLinkDecision(results, "link.locked-context-no-review", HumanReviewEvidenceContextLinkDecision.Blocked, HumanReviewEvidenceContextLinkIssueKind.LockedContextWithoutReview);
        AssertLinkDecision(results, "link.evidence-context-mismatch", HumanReviewEvidenceContextLinkDecision.Blocked, HumanReviewEvidenceContextLinkIssueKind.EvidenceContextMismatch);
        AssertLinkDecision(results, "link.missing-confidence", HumanReviewEvidenceContextLinkDecision.Blocked, HumanReviewEvidenceContextLinkIssueKind.MissingEvidenceConfidence);
        AssertLinkDecision(results, "link.unknown-context-authority", HumanReviewEvidenceContextLinkDecision.Blocked, HumanReviewEvidenceContextLinkIssueKind.UnknownContextAuthority);
        AssertLinkDecision(results, "link.missing-context-freshness", HumanReviewEvidenceContextLinkDecision.Blocked, HumanReviewEvidenceContextLinkIssueKind.MissingContextFreshness);
    }

    [TestMethod]
    public void HumanReviewEvidenceContextLinkGuard_BlocksRiskContradictionRawSecretAndDisabledSources()
    {
        var results = HumanReviewEvidenceContextLinkReadOnlyGuard.EvaluateCatalog();

        AssertLinkDecision(results, "link.unresolved-contradiction", HumanReviewEvidenceContextLinkDecision.Blocked, HumanReviewEvidenceContextLinkIssueKind.UnresolvedContradictionLink);
        AssertLinkDecision(results, "link.critical-risk", HumanReviewEvidenceContextLinkDecision.Blocked, HumanReviewEvidenceContextLinkIssueKind.CriticalRiskLink);
        AssertLinkDecision(results, "link.raw-payload-evidence", HumanReviewEvidenceContextLinkDecision.Excluded, HumanReviewEvidenceContextLinkIssueKind.RawPayloadLink);
        AssertLinkDecision(results, "link.secret-like", HumanReviewEvidenceContextLinkDecision.Excluded, HumanReviewEvidenceContextLinkIssueKind.SecretLikeLink);
        AssertLinkDecision(results, "link.provider-cloud-evidence-disabled", HumanReviewEvidenceContextLinkDecision.Blocked, HumanReviewEvidenceContextLinkIssueKind.ProviderCloudDerivedWhileDisabled);
        AssertLinkDecision(results, "link.semantic-context-disabled", HumanReviewEvidenceContextLinkDecision.Blocked, HumanReviewEvidenceContextLinkIssueKind.SemanticVectorDerivedWhileDisabled);
        AssertLinkDecision(results, "link.llm-rationale-disabled", HumanReviewEvidenceContextLinkDecision.Blocked, HumanReviewEvidenceContextLinkIssueKind.LlmDerivedWhileDisabled);
        AssertLinkDecision(results, "link.disabled-persistence-store", HumanReviewEvidenceContextLinkDecision.Blocked, HumanReviewEvidenceContextLinkIssueKind.DisabledPersistenceStoreLink);
        AssertLinkDecision(results, "link.durable-memory-disabled", HumanReviewEvidenceContextLinkDecision.Blocked, HumanReviewEvidenceContextLinkIssueKind.DurableMemoryWhileDisabled);
    }

    [TestMethod]
    public void HumanReviewEvidenceContextLinkGuard_BlocksInvalidUsageAndActionCounts()
    {
        var results = HumanReviewEvidenceContextLinkReadOnlyGuard.EvaluateCatalog();

        AssertLinkDecision(results, "link.invalid-decision-option", HumanReviewEvidenceContextLinkDecision.Blocked, HumanReviewEvidenceContextLinkIssueKind.InvalidDecisionOptionLink);
        AssertLinkDecision(results, "link.invalid-candidate-action", HumanReviewEvidenceContextLinkDecision.Blocked, HumanReviewEvidenceContextLinkIssueKind.InvalidCandidateActionLink);
        AssertLinkDecision(results, "link.invalid-safe-next-step", HumanReviewEvidenceContextLinkDecision.Blocked, HumanReviewEvidenceContextLinkIssueKind.InvalidSafeNextStepLink);
        AssertLinkDecision(results, "link.duplicate-conflicting-source", HumanReviewEvidenceContextLinkDecision.Blocked, HumanReviewEvidenceContextLinkIssueKind.DuplicateConflictingSourceKind);
        AssertLinkDecision(results, "link.product-action-count", HumanReviewEvidenceContextLinkDecision.Blocked, HumanReviewEvidenceContextLinkIssueKind.ProductActionCountNonZero);
        AssertLinkDecision(results, "link.state-mutation-count", HumanReviewEvidenceContextLinkDecision.Blocked, HumanReviewEvidenceContextLinkIssueKind.StateMutationCountNonZero);
    }

    [TestMethod]
    public void HumanReviewEvidenceContextLinkGuard_HasNoEvidenceContextApprovalOrProductionOverclaim()
    {
        var text = string.Join(
            "\n",
            HumanReviewEvidenceContextLinkReadOnlyGuard.EvaluateCatalog()
                .Select(result => $"{result.FixtureId} {result.Decision} {string.Join(" ", result.Warnings)} {string.Join(" ", result.Blockers)}"));

        var forbidden = new[]
        {
            "production" + "-ready",
            "durable evidence created",
            "context trusted by default",
            "approval executed",
            "approval state mutated",
            "state mutation completed",
            "approved and applied",
            "provider call enabled",
            "semantic search enabled",
            "vector backend enabled",
            "llm live enabled",
            "durable memory active",
            "memory persisted"
        };

        foreach (var term in forbidden)
        {
            Assert.IsFalse(text.Contains(term, StringComparison.OrdinalIgnoreCase), term);
        }
    }

    [TestMethod]
    public void ApprovalPacketSurface_IsReadOnlyDeterministicFixtureSafeAndActionless()
    {
        var first = ApprovalPacketReadOnlySurfacePresenter.CreateFixture();
        var second = ApprovalPacketReadOnlySurfacePresenter.CreateFixture();

        Assert.IsTrue(first.ReadOnly);
        Assert.IsTrue(first.Deterministic);
        Assert.IsTrue(first.FixtureSafe);
        Assert.IsTrue(first.NoSideEffectProof.Passes);
        Assert.AreEqual("READ_ONLY_FIXTURE_SAFE_NO_APPROVAL_EXECUTION_NO_ACTIONS_NO_EXPORT", first.Mode);
        Assert.AreEqual(first.ReadOnlySummary, second.ReadOnlySummary);
        Assert.AreEqual(0, first.ProductActionsCount);
        Assert.AreEqual(0, first.StateMutationsCount);
        Assert.AreEqual(0, first.ExportActionsCount);
        Assert.IsFalse(first.HasApprovalExecution);
        Assert.IsFalse(first.HasApprovalStateMutation);
        Assert.IsFalse(first.HasProductActions);
        Assert.IsFalse(first.HasExportActions);
        Assert.IsFalse(first.HasDurableMemory);
        Assert.IsTrue(first.Sections.All(section => section.NoSideEffectProof.Passes));
        Assert.IsTrue(first.Sections.All(section => section.ProductActionsCount == 0));
        Assert.IsTrue(first.Sections.All(section => section.StateMutationsCount == 0));
        Assert.IsTrue(first.Sections.All(section => section.ExportActionsCount == 0));
    }

    [TestMethod]
    public void ApprovalPacketSurface_ContainsMinimumSections()
    {
        var surface = ApprovalPacketReadOnlySurfacePresenter.CreateFixture();
        var ids = surface.Sections.Select(section => section.SectionId).ToHashSet(StringComparer.Ordinal);

        Assert.AreEqual(30, surface.Sections.Count);
        CollectionAssert.IsSubsetOf(
            new[]
            {
                "approval.packet.executive-summary",
                "human.review.packet.identity",
                "candidate.action.previews",
                "candidate.action.risk.summary",
                "risk.decision.guard.summary",
                "evidence.context.link.guard.summary",
                "evidence.links",
                "context.links",
                "missing.evidence.blockers",
                "missing.stale.excluded.context.blockers",
                "unresolved.contradiction.blockers",
                "critical.risk.blockers",
                "decision.options.preview",
                "approve.preview.label",
                "reject.preview.label",
                "request.evidence.preview.label",
                "request.context.refresh.preview.label",
                "defer.decision.preview.label",
                "human.review.requirements",
                "runtime.live.disabled",
                "filesystem.db.disabled",
                "provider.cloud.disabled",
                "semantic.vector.disabled",
                "llm.live.disabled",
                "durable.memory.disabled",
                "approval.execution.disabled",
                "approval.state.mutation.disabled",
                "no.side.effect.proof",
                "documented.debt",
                "next.recommended.block"
            },
            ids.ToArray());
    }

    [TestMethod]
    public void ApprovalPacketSurface_ShowsFoundationRiskAndEvidenceContextSummaries()
    {
        var surface = ApprovalPacketReadOnlySurfacePresenter.CreateFixture();

        Assert.IsTrue(surface.Packet.EvidenceLinks.Count > 0);
        Assert.IsTrue(surface.Packet.ContextLinks.Count > 0);
        Assert.IsTrue(surface.CandidateActionPreviews.Count >= 3);
        Assert.IsTrue(surface.RiskDecisionSummaries.Any(summary => summary.Contains("Risk/decision fixtures: 26", StringComparison.Ordinal)));
        Assert.IsTrue(surface.EvidenceContextLinkSummaries.Any(summary => summary.Contains("Evidence/context link fixtures: 27", StringComparison.Ordinal)));
        Assert.IsTrue(surface.DecisionOptionPreviews.Count == 5);
        Assert.IsTrue(surface.HumanReviewRequirements.Count > 0);
        Assert.IsTrue(surface.Warnings.Count > 0);
        Assert.IsTrue(surface.Blockers.Count > 0);
    }

    [TestMethod]
    public void ApprovalPacketSurface_DecisionOptionsArePreviewLabelsOnly()
    {
        var surface = ApprovalPacketReadOnlySurfacePresenter.CreateFixture();
        var decisionSectionIds = new[]
        {
            "approve.preview.label",
            "reject.preview.label",
            "request.evidence.preview.label",
            "request.context.refresh.preview.label",
            "defer.decision.preview.label"
        };

        Assert.IsTrue(surface.Packet.DecisionOptions.All(option => option.PreviewOnly));
        Assert.IsTrue(surface.Packet.DecisionOptions.All(option => !option.ExecutesAction));
        Assert.IsTrue(surface.Packet.DecisionOptions.All(option => !option.MutatesState));

        foreach (var sectionId in decisionSectionIds)
        {
            var section = surface.Sections.Single(item => item.SectionId == sectionId);

            Assert.AreEqual(ApprovalHumanReviewItemStatus.PreviewOnly, section.Status);
            Assert.AreEqual(0, section.ProductActionsCount);
            Assert.AreEqual(0, section.StateMutationsCount);
            Assert.AreEqual(0, section.ExportActionsCount);
            Assert.IsTrue(section.Warnings.Any(warning => warning.Contains("preview label", StringComparison.OrdinalIgnoreCase)));
        }
    }

    [TestMethod]
    public void ApprovalPacketSurface_IncludesDisabledCapabilityNotices()
    {
        var surface = ApprovalPacketReadOnlySurfacePresenter.CreateFixture();
        var text = string.Join("\n", surface.DisabledNotices.Concat(surface.Sections.SelectMany(section => section.Blockers)));

        StringAssert.Contains(text, "Runtime/live");
        StringAssert.Contains(text, "Filesystem");
        StringAssert.Contains(text, "Provider/cloud");
        StringAssert.Contains(text, "Semantic/vector");
        StringAssert.Contains(text, "LLM live");
        StringAssert.Contains(text, "Durable memory");
        StringAssert.Contains(text, "Approval execution");
        StringAssert.Contains(text, "Approval state mutation");
        Assert.IsTrue(surface.Sections.Any(section => section.SectionId == "approval.execution.disabled" && section.Status == ApprovalHumanReviewItemStatus.Disabled));
        Assert.IsTrue(surface.Sections.Any(section => section.SectionId == "approval.state.mutation.disabled" && section.Status == ApprovalHumanReviewItemStatus.Disabled));
    }

    [TestMethod]
    public void ApprovalPacketSurface_HasNoActionButtonExportOrProductionOverclaim()
    {
        var surface = ApprovalPacketReadOnlySurfacePresenter.CreateFixture();
        var text = string.Join(
            "\n",
            surface.ReadOnlySummary,
            string.Join("\n", surface.CandidateActionPreviews),
            string.Join("\n", surface.RiskDecisionSummaries),
            string.Join("\n", surface.EvidenceContextLinkSummaries),
            string.Join("\n", surface.DecisionOptionPreviews),
            string.Join("\n", surface.Sections.Select(section => $"{section.Title} {string.Join(" ", section.Warnings)} {string.Join(" ", section.Blockers)}")));

        Assert.IsFalse(text.Contains("production" + "-ready", StringComparison.OrdinalIgnoreCase));
        Assert.IsFalse(text.Contains("approval executed", StringComparison.OrdinalIgnoreCase));
        Assert.IsFalse(text.Contains("approval state mutated", StringComparison.OrdinalIgnoreCase));
        Assert.IsFalse(text.Contains("state mutation completed", StringComparison.OrdinalIgnoreCase));
        Assert.IsFalse(text.Contains("approved and applied", StringComparison.OrdinalIgnoreCase));
        Assert.IsFalse(text.Contains("action button", StringComparison.OrdinalIgnoreCase));
        Assert.IsFalse(text.Contains("action command", StringComparison.OrdinalIgnoreCase));
        Assert.IsFalse(text.Contains("export file created", StringComparison.OrdinalIgnoreCase));
        Assert.IsFalse(text.Contains("clipboard used", StringComparison.OrdinalIgnoreCase));
        Assert.IsFalse(text.Contains("durable memory active", StringComparison.OrdinalIgnoreCase));
        Assert.IsFalse(text.Contains("provider call enabled", StringComparison.OrdinalIgnoreCase));
    }

    private static void AssertRiskDecision(
        IReadOnlyList<ApprovalRiskDecisionReadOnlyResult> results,
        string fixtureId,
        ApprovalRiskDecisionReadOnlyDecision expectedDecision,
        ApprovalRiskDecisionReadOnlyIssueKind expectedIssue)
    {
        var result = results.Single(item => item.FixtureId == fixtureId);

        Assert.AreEqual(expectedDecision, result.Decision, fixtureId);
        if (expectedIssue == ApprovalRiskDecisionReadOnlyIssueKind.None)
        {
            Assert.AreEqual(0, result.Issues.Count, fixtureId);
        }
        else
        {
            Assert.IsTrue(result.HasIssue(expectedIssue), fixtureId);
        }
    }

    private static void AssertLinkDecision(
        IReadOnlyList<HumanReviewEvidenceContextLinkResult> results,
        string fixtureId,
        HumanReviewEvidenceContextLinkDecision expectedDecision,
        HumanReviewEvidenceContextLinkIssueKind expectedIssue)
    {
        var result = results.Single(item => item.FixtureId == fixtureId);

        Assert.AreEqual(expectedDecision, result.Decision, fixtureId);
        if (expectedIssue == HumanReviewEvidenceContextLinkIssueKind.None)
        {
            Assert.AreEqual(0, result.Issues.Count, fixtureId);
        }
        else
        {
            Assert.IsTrue(result.HasIssue(expectedIssue), fixtureId);
        }
    }
}
