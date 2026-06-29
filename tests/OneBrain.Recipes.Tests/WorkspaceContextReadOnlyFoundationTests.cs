using Microsoft.VisualStudio.TestTools.UnitTesting;
using OneBrain.Core.Context;

namespace OneBrain.Recipes.Tests;

[TestClass]
[TestCategory("WorkspaceContextMemory")]
[TestCategory("PhaseDContextWorkspaceMemory")]
public sealed class WorkspaceContextReadOnlyFoundationTests
{
    [TestMethod]
    public void WorkspaceContextPacket_IsReadOnlyDeterministicAndFixtureSafe()
    {
        var first = WorkspaceContextReadOnlyPresenter.CreateFixture();
        var second = WorkspaceContextReadOnlyPresenter.CreateFixture();

        Assert.IsTrue(first.ReadOnly);
        Assert.IsTrue(first.Deterministic);
        Assert.IsTrue(first.FixtureSafe);
        Assert.IsTrue(first.NoSideEffectProof.Passes);
        Assert.IsFalse(first.HasDurableMemory);
        Assert.IsFalse(first.HasProductActions);
        Assert.AreEqual(first.ReadOnlySummary, second.ReadOnlySummary);
        Assert.AreEqual("READ_ONLY_FIXTURE_SAFE_NO_MEMORY_RUNTIME", first.Mode);
        Assert.AreEqual("EvidenceIntelligenceAuditDashboardReadOnlyPresenter.CreateFixture", first.SourceLabel);
    }

    [TestMethod]
    public void WorkspaceContextPacket_ContainsMinimumExpectedContextItems()
    {
        var packet = WorkspaceContextReadOnlyPresenter.CreateFixture();
        var kinds = packet.Items.Select(item => item.Kind).ToHashSet();

        Assert.IsTrue(kinds.Contains(WorkspaceContextItemKind.WorkspaceIdentity));
        Assert.IsTrue(kinds.Contains(WorkspaceContextItemKind.WorkspaceBoundary));
        Assert.IsTrue(kinds.Contains(WorkspaceContextItemKind.ContextPacketSummary));
        Assert.IsTrue(kinds.Contains(WorkspaceContextItemKind.SelectedContext));
        Assert.IsTrue(kinds.Contains(WorkspaceContextItemKind.LockedContext));
        Assert.IsTrue(kinds.Contains(WorkspaceContextItemKind.ExcludedContext));
        Assert.IsTrue(kinds.Contains(WorkspaceContextItemKind.EvidenceLinkedContextReference));
        Assert.IsTrue(kinds.Contains(WorkspaceContextItemKind.AuthorityPolicy));
        Assert.IsTrue(kinds.Contains(WorkspaceContextItemKind.FreshnessSignal));
        Assert.IsTrue(kinds.Contains(WorkspaceContextItemKind.MissingContextWarning));
        Assert.IsTrue(kinds.Contains(WorkspaceContextItemKind.StaleContextWarning));
        Assert.IsTrue(kinds.Contains(WorkspaceContextItemKind.SensitiveUnsafeContextBlocker));
        Assert.IsTrue(kinds.Contains(WorkspaceContextItemKind.ProviderCloudDisabledNotice));
        Assert.IsTrue(kinds.Contains(WorkspaceContextItemKind.SemanticVectorDisabledNotice));
        Assert.IsTrue(kinds.Contains(WorkspaceContextItemKind.SafeNextStep));
        Assert.IsTrue(kinds.Contains(WorkspaceContextItemKind.NoSideEffectProof));
        Assert.IsTrue(kinds.Contains(WorkspaceContextItemKind.DeferredCapability));
        Assert.IsTrue(packet.Items.All(item => item.NoSideEffectProof.Passes));
    }

    [TestMethod]
    public void WorkspaceContextPacket_ContainsSelectedLockedAndExcludedContext()
    {
        var packet = WorkspaceContextReadOnlyPresenter.CreateFixture();

        Assert.IsTrue(packet.SelectedContext.Count >= 5);
        Assert.IsTrue(packet.LockedContext.Count >= 5);
        Assert.IsTrue(packet.ExcludedContext.Count >= 2);
        Assert.IsTrue(packet.SelectedContext.All(item => item.Selected));
        Assert.IsTrue(packet.LockedContext.All(item => item.Locked));
        Assert.IsTrue(packet.ExcludedContext.All(item => item.Excluded));
        Assert.IsTrue(packet.Items.Any(item => item.Status == WorkspaceContextItemStatus.Blocked));
        Assert.IsTrue(packet.Items.Any(item => item.Status == WorkspaceContextItemStatus.Disabled));
        Assert.IsTrue(packet.Items.Any(item => item.Status == WorkspaceContextItemStatus.Deferred));
    }

    [TestMethod]
    public void WorkspaceContextPacket_ContainsMemoryCandidatePreviews()
    {
        var packet = WorkspaceContextReadOnlyPresenter.CreateFixture();
        var kinds = packet.MemoryCandidates.Select(candidate => candidate.Kind).ToHashSet();

        Assert.AreEqual(5, packet.MemoryCandidates.Count);
        Assert.IsTrue(kinds.Contains(WorkspaceContextItemKind.ContradictionMemoryPreview));
        Assert.IsTrue(kinds.Contains(WorkspaceContextItemKind.RiskMemoryPreview));
        Assert.IsTrue(kinds.Contains(WorkspaceContextItemKind.DecisionMemoryPreview));
        Assert.IsTrue(kinds.Contains(WorkspaceContextItemKind.ClaimMemoryPreview));
        Assert.IsTrue(kinds.Contains(WorkspaceContextItemKind.ActionMemoryPreview));
        Assert.IsTrue(packet.MemoryCandidates.All(candidate => !candidate.DurableMemoryEnabled));
        Assert.IsTrue(packet.MemoryCandidates.All(candidate => candidate.NoSideEffectProof.Passes));
        Assert.IsTrue(packet.MemoryCandidates.Any(candidate => candidate.Locked));
    }

    [TestMethod]
    public void WorkspaceContextPacket_ModelsAuthorityFreshnessWarningsBlockersAndSafeNextStep()
    {
        var packet = WorkspaceContextReadOnlyPresenter.CreateFixture();

        Assert.IsTrue(packet.Items.Any(item => item.Authority == WorkspaceContextAuthorityLevel.EvidenceLinked));
        Assert.IsTrue(packet.Items.Any(item => item.Authority == WorkspaceContextAuthorityLevel.HumanReviewRequired));
        Assert.IsTrue(packet.Items.Any(item => item.Authority == WorkspaceContextAuthorityLevel.LockedBySafety));
        Assert.IsTrue(packet.Items.Any(item => item.Freshness == WorkspaceContextFreshnessStatus.Stale));
        Assert.IsTrue(packet.Items.Any(item => item.Freshness == WorkspaceContextFreshnessStatus.Missing));
        Assert.IsTrue(packet.Warnings.Count >= 3);
        Assert.IsTrue(packet.Blockers.Count >= 5);
        StringAssert.Contains(packet.SafeNextStep, "authority and freshness guards");
    }

    [TestMethod]
    public void WorkspaceContextPacket_DisablesProviderCloudSemanticVectorFilesystemDbPersistenceRuntime()
    {
        var packet = WorkspaceContextReadOnlyPresenter.CreateFixture();
        var proof = packet.NoSideEffectProof;

        StringAssert.Contains(packet.ProviderCloudNotice, "disabled");
        StringAssert.Contains(packet.SemanticVectorNotice, "disabled");
        Assert.IsFalse(proof.WorkspaceFilesystemReadAttempted);
        Assert.IsFalse(proof.FilesystemWriteAttempted);
        Assert.IsFalse(proof.DatabaseTouched);
        Assert.IsFalse(proof.DurablePersistenceActive);
        Assert.IsFalse(proof.DurableMemoryActive);
        Assert.IsFalse(proof.VectorSemanticBackendTouched);
        Assert.IsFalse(proof.LlmProviderTouched);
        Assert.IsFalse(proof.ProviderCloudTouched);
        Assert.IsFalse(proof.RuntimeTouched);
        Assert.IsFalse(proof.BrowserCdpTouched);
        Assert.IsFalse(proof.WcuTouched);
        Assert.IsFalse(proof.OcrTouched);
        Assert.IsTrue(packet.Sources.All(source => !source.ReadsWorkspaceFilesystem));
        Assert.IsTrue(packet.Sources.All(source => !source.UsesProviderCloud));
        Assert.IsTrue(packet.Sources.All(source => !source.UsesVectorSemanticBackend));
    }

    [TestMethod]
    public void WorkspaceContextPacket_HasNoSecretLeakageOrProductionClaim()
    {
        var packet = WorkspaceContextReadOnlyPresenter.CreateFixture();
        var text = string.Join("\n", packet.ReadOnlySummary, packet.Summary, string.Join("\n", packet.Items.Select(item => item.Summary)), string.Join("\n", packet.MemoryCandidates.Select(candidate => candidate.Preview)));

        Assert.IsFalse(text.Contains("sk-", StringComparison.OrdinalIgnoreCase));
        Assert.IsFalse(text.Contains("Bearer" + " ", StringComparison.OrdinalIgnoreCase));
        Assert.IsFalse(text.Contains("PRIVATE KEY", StringComparison.OrdinalIgnoreCase));
        Assert.IsFalse(text.Contains("AKIA", StringComparison.OrdinalIgnoreCase));
        Assert.IsFalse(text.Contains("ghp_", StringComparison.OrdinalIgnoreCase));
        Assert.IsFalse(text.Contains("production" + "-ready", StringComparison.OrdinalIgnoreCase));
        Assert.IsFalse(text.Contains("memory persisted", StringComparison.OrdinalIgnoreCase));
        Assert.IsFalse(text.Contains("semantic search enabled", StringComparison.OrdinalIgnoreCase));
    }

    [TestMethod]
    public void AuthorityFreshnessGuard_CatalogCoversExpectedAdversarialFixtures()
    {
        var fixtures = WorkspaceContextAuthorityFreshnessGuard.CreateFixtureCatalog();

        Assert.AreEqual(20, fixtures.Count);
        Assert.IsTrue(fixtures.All(fixture => fixture.NoSideEffectProof.Passes));
        CollectionAssert.AreEquivalent(
            new[]
            {
                "ctx.evidence-linked-current",
                "ctx.human-reviewed-current",
                "ctx.fixture-only-warning",
                "ctx.stale-context",
                "ctx.missing-freshness",
                "ctx.unknown-authority",
                "ctx.low-authority-source",
                "ctx.contradictory",
                "memory.no-evidence",
                "memory.stale-evidence",
                "ctx.selected-unknown-authority",
                "ctx.locked-stale",
                "ctx.excluded-selected",
                "ctx.sensitive-without-clearance",
                "ctx.raw-payload",
                "ctx.provider-derived-disabled",
                "ctx.semantic-derived-disabled",
                "ctx.legacy-no-provenance",
                "safe-next-step.stale",
                "memory.decision-missing-human-review"
            },
            fixtures.Select(fixture => fixture.FixtureId).ToArray());
    }

    [TestMethod]
    public void AuthorityFreshnessGuard_ExpectedDecisionsAndIssuesMatchFixtures()
    {
        foreach (var fixture in WorkspaceContextAuthorityFreshnessGuard.CreateFixtureCatalog())
        {
            var result = WorkspaceContextAuthorityFreshnessGuard.Evaluate(fixture);

            Assert.AreEqual(fixture.ExpectedDecision, result.Decision, fixture.FixtureId);
            if (fixture.ExpectedIssue == WorkspaceContextAuthorityFreshnessIssueKind.None)
            {
                Assert.AreEqual(0, result.Issues.Count, fixture.FixtureId);
            }
            else
            {
                Assert.IsTrue(result.HasIssue(fixture.ExpectedIssue), fixture.FixtureId);
                Assert.IsTrue(result.Issues.Any(issue => !string.IsNullOrWhiteSpace(issue.Message)), fixture.FixtureId);
            }

            Assert.IsTrue(result.NoSideEffectProof.Passes, fixture.FixtureId);
        }
    }

    [TestMethod]
    public void AuthorityFreshnessGuard_AllowsOnlyEvidenceLinkedOrHumanReviewedCurrentContext()
    {
        var results = WorkspaceContextAuthorityFreshnessGuard.EvaluateCatalog();
        var allowed = results.Where(result => result.Decision == WorkspaceContextAuthorityFreshnessDecision.AllowedReadOnly).ToList();

        CollectionAssert.AreEquivalent(
            new[] { "ctx.evidence-linked-current", "ctx.human-reviewed-current" },
            allowed.Select(result => result.FixtureId).ToArray());
        Assert.IsTrue(allowed.All(result => result.AllowsReadOnlySummary));
        Assert.IsTrue(allowed.All(result => result.AllowsDecisionUse));
        Assert.IsTrue(allowed.All(result => result.AllowsSafeNextStepUse));
    }

    [TestMethod]
    public void AuthorityFreshnessGuard_FixtureOnlyAndLowAuthorityStayWarningReadOnlyOnly()
    {
        var results = WorkspaceContextAuthorityFreshnessGuard.EvaluateCatalog()
            .Where(result => result.FixtureId is "ctx.fixture-only-warning" or "ctx.low-authority-source")
            .ToList();

        Assert.AreEqual(2, results.Count);
        Assert.IsTrue(results.All(result => result.Decision == WorkspaceContextAuthorityFreshnessDecision.WarningReadOnlyOnly));
        Assert.IsTrue(results.All(result => result.AllowsReadOnlySummary));
        Assert.IsTrue(results.All(result => !result.AllowsDecisionUse));
        Assert.IsTrue(results.All(result => !result.AllowsSafeNextStepUse));
        Assert.IsTrue(results.All(result => result.RequiresHumanReview));
    }

    [TestMethod]
    public void AuthorityFreshnessGuard_StaleMissingUnknownContradictoryAndExcludedContextBlock()
    {
        var blockedIds = new[]
        {
            "ctx.stale-context",
            "ctx.missing-freshness",
            "ctx.unknown-authority",
            "ctx.contradictory",
            "ctx.selected-unknown-authority",
            "ctx.excluded-selected",
            "safe-next-step.stale"
        };
        var results = WorkspaceContextAuthorityFreshnessGuard.EvaluateCatalog()
            .Where(result => blockedIds.Contains(result.FixtureId, StringComparer.Ordinal))
            .ToList();

        Assert.AreEqual(blockedIds.Length, results.Count);
        Assert.IsTrue(results.All(result => result.Blocked), string.Join(", ", results.Where(result => !result.Blocked).Select(result => result.FixtureId)));
        Assert.IsTrue(results.All(result => !result.AllowsDecisionUse));
        Assert.IsTrue(results.All(result => !result.AllowsSafeNextStepUse));
        Assert.IsTrue(results.All(result => result.RequiresHumanReview));
    }

    [TestMethod]
    public void AuthorityFreshnessGuard_SensitiveRawProviderSemanticLegacyAndMemoryUnsafeCasesBlock()
    {
        var blockedIds = new[]
        {
            "ctx.sensitive-without-clearance",
            "ctx.raw-payload",
            "ctx.provider-derived-disabled",
            "ctx.semantic-derived-disabled",
            "ctx.legacy-no-provenance",
            "memory.no-evidence",
            "memory.stale-evidence",
            "memory.decision-missing-human-review"
        };
        var results = WorkspaceContextAuthorityFreshnessGuard.EvaluateCatalog()
            .Where(result => blockedIds.Contains(result.FixtureId, StringComparer.Ordinal))
            .ToList();

        Assert.AreEqual(blockedIds.Length, results.Count);
        Assert.IsTrue(results.All(result => result.Blocked), string.Join(", ", results.Where(result => !result.Blocked).Select(result => result.FixtureId)));
        Assert.IsTrue(results.All(result => !result.AllowsDecisionUse));
        Assert.IsTrue(results.All(result => !result.AllowsSafeNextStepUse));
        Assert.IsTrue(results.All(result => !result.AllowsMemoryCandidateUse));
        Assert.IsTrue(results.All(result => result.ProviderCloudDisabled));
        Assert.IsTrue(results.All(result => result.SemanticVectorDisabled));
    }

    [TestMethod]
    public void AuthorityFreshnessGuard_OutputIsDeterministicAndHasNoProductionClaim()
    {
        var first = WorkspaceContextAuthorityFreshnessGuard.EvaluateCatalog();
        var second = WorkspaceContextAuthorityFreshnessGuard.EvaluateCatalog();
        var firstText = string.Join("\n", first.Select(result => $"{result.FixtureId}:{result.Decision}:{string.Join(",", result.Issues.Select(issue => issue.IssueKind))}"));
        var secondText = string.Join("\n", second.Select(result => $"{result.FixtureId}:{result.Decision}:{string.Join(",", result.Issues.Select(issue => issue.IssueKind))}"));

        Assert.AreEqual(firstText, secondText);
        Assert.IsFalse(firstText.Contains("production" + "-ready", StringComparison.OrdinalIgnoreCase));
        Assert.IsFalse(firstText.Contains("semantic search enabled", StringComparison.OrdinalIgnoreCase));
        Assert.IsFalse(firstText.Contains("memory persisted", StringComparison.OrdinalIgnoreCase));
    }

    [TestMethod]
    public void SelectionLockExclusionGuard_CatalogCoversExpectedAdversarialFixtures()
    {
        var fixtures = WorkspaceContextSelectionLockExclusionGuard.CreateFixtureCatalog();

        Assert.AreEqual(22, fixtures.Count);
        Assert.IsTrue(fixtures.All(fixture => fixture.NoSideEffectProof.Passes));
        CollectionAssert.AreEquivalent(
            new[]
            {
                "ctx.selected-evidence-fresh",
                "ctx.selected-excluded",
                "ctx.selected-locked-by-safety",
                "ctx.selected-stale",
                "ctx.selected-unknown-authority",
                "ctx.selected-missing-freshness",
                "ctx.selected-contradictory",
                "ctx.locked-stale",
                "ctx.locked-missing-evidence",
                "memory.locked-promote",
                "memory.excluded-reference",
                "safe-next-step.excluded-reference",
                "claim-action.excluded-reference",
                "graph.excluded-reference",
                "ctx.selected-raw-sensitive",
                "ctx.selected-provider-disabled",
                "ctx.selected-semantic-disabled",
                "ctx.selected-legacy-no-provenance",
                "ctx.duplicate-conflicting-lock",
                "ctx.empty-selected-safe-next-step",
                "ctx.locked-review-missing",
                "dashboard.excluded-candidate"
            },
            fixtures.Select(fixture => fixture.FixtureId).ToArray());
    }

    [TestMethod]
    public void SelectionLockExclusionGuard_ExpectedDecisionsAndIssuesMatchFixtures()
    {
        foreach (var fixture in WorkspaceContextSelectionLockExclusionGuard.CreateFixtureCatalog())
        {
            var result = WorkspaceContextSelectionLockExclusionGuard.Evaluate(fixture);

            Assert.AreEqual(fixture.ExpectedDecision, result.Decision, fixture.FixtureId);
            if (fixture.ExpectedIssue == WorkspaceContextSelectionLockExclusionIssueKind.None)
            {
                Assert.AreEqual(0, result.Issues.Count, fixture.FixtureId);
            }
            else
            {
                Assert.IsTrue(result.HasIssue(fixture.ExpectedIssue), fixture.FixtureId);
                Assert.IsTrue(result.Issues.Any(issue => !string.IsNullOrWhiteSpace(issue.Message)), fixture.FixtureId);
            }

            Assert.IsTrue(result.NoSideEffectProof.Passes, fixture.FixtureId);
        }
    }

    [TestMethod]
    public void SelectionLockExclusionGuard_AllowsOnlySelectedEvidenceLinkedFreshContext()
    {
        var results = WorkspaceContextSelectionLockExclusionGuard.EvaluateCatalog();
        var allowed = results.Where(result => result.Decision == WorkspaceContextSelectionLockExclusionDecision.AllowedReadOnly).ToList();

        CollectionAssert.AreEquivalent(
            new[] { "ctx.selected-evidence-fresh" },
            allowed.Select(result => result.FixtureId).ToArray());
        Assert.IsTrue(allowed.All(result => result.AllowsReadOnlySummary));
        Assert.IsTrue(allowed.All(result => result.AllowsDecisionUse));
        Assert.IsTrue(allowed.All(result => result.AllowsSafeNextStepUse));
    }

    [TestMethod]
    public void SelectionLockExclusionGuard_ExcludedWinsOverSelectedAndDependentRefs()
    {
        var excludedIds = new[]
        {
            "ctx.selected-excluded",
            "memory.excluded-reference",
            "safe-next-step.excluded-reference",
            "claim-action.excluded-reference",
            "graph.excluded-reference",
            "ctx.selected-raw-sensitive",
            "dashboard.excluded-candidate"
        };
        var results = WorkspaceContextSelectionLockExclusionGuard.EvaluateCatalog()
            .Where(result => excludedIds.Contains(result.FixtureId, StringComparer.Ordinal))
            .ToList();

        Assert.AreEqual(excludedIds.Length, results.Count);
        Assert.IsTrue(results.All(result => result.Decision == WorkspaceContextSelectionLockExclusionDecision.Excluded), string.Join(", ", results.Where(result => result.Decision != WorkspaceContextSelectionLockExclusionDecision.Excluded).Select(result => result.FixtureId)));
        Assert.IsTrue(results.All(result => !result.AllowsDecisionUse));
        Assert.IsTrue(results.All(result => !result.AllowsSafeNextStepUse));
        Assert.IsTrue(results.All(result => !result.AllowsMemoryInfluence));
    }

    [TestMethod]
    public void SelectionLockExclusionGuard_LockedContextRequiresHumanReviewBeforeInfluence()
    {
        var results = WorkspaceContextSelectionLockExclusionGuard.EvaluateCatalog()
            .Where(result => result.FixtureId is "ctx.selected-locked-by-safety" or "ctx.locked-stale" or "ctx.locked-missing-evidence" or "memory.locked-promote" or "ctx.locked-review-missing")
            .ToList();

        Assert.AreEqual(5, results.Count);
        Assert.IsTrue(results.All(result => result.RequiresHumanReview), string.Join(", ", results.Where(result => !result.RequiresHumanReview).Select(result => result.FixtureId)));
        Assert.IsTrue(results.All(result => !result.AllowsDecisionUse));
        Assert.IsTrue(results.All(result => !result.AllowsSafeNextStepUse));
        Assert.IsTrue(results.All(result => !result.AllowsMemoryInfluence));
    }

    [TestMethod]
    public void SelectionLockExclusionGuard_StaleUnknownMissingContradictoryProviderSemanticLegacyAndDuplicateCasesBlock()
    {
        var blockedIds = new[]
        {
            "ctx.selected-stale",
            "ctx.selected-unknown-authority",
            "ctx.selected-missing-freshness",
            "ctx.selected-contradictory",
            "ctx.selected-provider-disabled",
            "ctx.selected-semantic-disabled",
            "ctx.selected-legacy-no-provenance",
            "ctx.duplicate-conflicting-lock",
            "ctx.empty-selected-safe-next-step"
        };
        var results = WorkspaceContextSelectionLockExclusionGuard.EvaluateCatalog()
            .Where(result => blockedIds.Contains(result.FixtureId, StringComparer.Ordinal))
            .ToList();

        Assert.AreEqual(blockedIds.Length, results.Count);
        Assert.IsTrue(results.All(result => result.Blocked), string.Join(", ", results.Where(result => !result.Blocked).Select(result => result.FixtureId)));
        Assert.IsTrue(results.All(result => !result.AllowsDecisionUse));
        Assert.IsTrue(results.All(result => !result.AllowsSafeNextStepUse));
    }

    [TestMethod]
    public void SelectionLockExclusionGuard_OutputIsDeterministicAndHasNoProductionClaim()
    {
        var first = WorkspaceContextSelectionLockExclusionGuard.EvaluateCatalog();
        var second = WorkspaceContextSelectionLockExclusionGuard.EvaluateCatalog();
        var firstText = string.Join("\n", first.Select(result => $"{result.FixtureId}:{result.Decision}:{string.Join(",", result.Issues.Select(issue => issue.IssueKind))}"));
        var secondText = string.Join("\n", second.Select(result => $"{result.FixtureId}:{result.Decision}:{string.Join(",", result.Issues.Select(issue => issue.IssueKind))}"));

        Assert.AreEqual(firstText, secondText);
        Assert.IsFalse(firstText.Contains("production" + "-ready", StringComparison.OrdinalIgnoreCase));
        Assert.IsFalse(firstText.Contains("selection is trusted", StringComparison.OrdinalIgnoreCase));
        Assert.IsFalse(firstText.Contains("lock bypassed", StringComparison.OrdinalIgnoreCase));
        Assert.IsFalse(firstText.Contains("exclusion bypassed", StringComparison.OrdinalIgnoreCase));
        Assert.IsFalse(firstText.Contains("memory persisted", StringComparison.OrdinalIgnoreCase));
    }

    [TestMethod]
    public void MemoryCandidateContradictionRiskGuard_CatalogCoversExpectedAdversarialFixtures()
    {
        var fixtures = WorkspaceMemoryCandidateContradictionRiskGuard.CreateFixtureCatalog();

        Assert.AreEqual(24, fixtures.Count);
        Assert.IsTrue(fixtures.All(fixture => fixture.NoSideEffectProof.Passes));
        CollectionAssert.AreEquivalent(
            new[]
            {
                "memory.contradiction.evidence-linked",
                "memory.contradiction.no-evidence",
                "memory.contradiction.stale-context",
                "memory.contradiction.excluded-context",
                "memory.contradiction.locked-unsafe",
                "memory.risk.evidence-fresh",
                "memory.risk.missing-severity",
                "memory.risk.promotes-decision",
                "memory.decision.no-human-review",
                "memory.decision.contradictory-evidence",
                "memory.claim.missing-confidence",
                "memory.claim.stale-evidence",
                "memory.action.missing-human-action",
                "memory.action.excluded-context",
                "memory.safe-next.critical-risk",
                "memory.safe-next.unresolved-contradiction",
                "memory.provider-derived-disabled",
                "memory.semantic-derived-disabled",
                "memory.legacy-no-provenance",
                "memory.fixture-only",
                "memory.duplicate-conflicting",
                "memory.raw-sensitive-payload",
                "memory.unknown-authority",
                "memory.missing-freshness"
            },
            fixtures.Select(fixture => fixture.FixtureId).ToArray());
    }

    [TestMethod]
    public void MemoryCandidateContradictionRiskGuard_ExpectedDecisionsAndIssuesMatchFixtures()
    {
        foreach (var fixture in WorkspaceMemoryCandidateContradictionRiskGuard.CreateFixtureCatalog())
        {
            var result = WorkspaceMemoryCandidateContradictionRiskGuard.Evaluate(fixture);

            Assert.AreEqual(fixture.ExpectedDecision, result.Decision, fixture.FixtureId);
            if (fixture.ExpectedIssue == WorkspaceMemoryCandidateContradictionRiskIssueKind.None)
            {
                Assert.AreEqual(0, result.Issues.Count, fixture.FixtureId);
            }
            else
            {
                Assert.IsTrue(result.HasIssue(fixture.ExpectedIssue), fixture.FixtureId);
                Assert.IsTrue(result.Issues.Any(issue => !string.IsNullOrWhiteSpace(issue.Message)), fixture.FixtureId);
            }

            Assert.IsTrue(result.NoSideEffectProof.Passes, fixture.FixtureId);
            Assert.IsFalse(result.DurableMemoryEnabled, fixture.FixtureId);
        }
    }

    [TestMethod]
    public void MemoryCandidateContradictionRiskGuard_KeepsContradictionAndRiskReadOnly()
    {
        var results = WorkspaceMemoryCandidateContradictionRiskGuard.EvaluateCatalog();
        var contradiction = results.Single(result => result.FixtureId == "memory.contradiction.evidence-linked");
        var risk = results.Single(result => result.FixtureId == "memory.risk.evidence-fresh");
        var fixtureOnly = results.Single(result => result.FixtureId == "memory.fixture-only");

        Assert.AreEqual(WorkspaceMemoryCandidateInfluenceDecision.NeedsHumanReview, contradiction.Decision);
        Assert.IsTrue(contradiction.RequiresHumanReview);
        Assert.IsTrue(contradiction.AllowsReadOnlyPreview);
        Assert.IsFalse(contradiction.AllowsDecisionUse);
        Assert.IsFalse(contradiction.AllowsSafeNextStepUse);
        Assert.IsFalse(contradiction.DurableMemoryEnabled);

        Assert.AreEqual(WorkspaceMemoryCandidateInfluenceDecision.AllowedReadOnlyWarning, risk.Decision);
        Assert.IsTrue(risk.AllowsReadOnlyPreview);
        Assert.IsFalse(risk.AllowsDecisionUse);
        Assert.IsFalse(risk.AllowsSafeNextStepUse);
        Assert.IsFalse(risk.DurableMemoryEnabled);

        Assert.AreEqual(WorkspaceMemoryCandidateInfluenceDecision.WarningReadOnlyOnly, fixtureOnly.Decision);
        Assert.IsTrue(fixtureOnly.AllowsReadOnlyPreview);
        Assert.IsFalse(fixtureOnly.AllowsCandidateInfluence);
        Assert.IsFalse(fixtureOnly.DurableMemoryEnabled);
    }

    [TestMethod]
    public void MemoryCandidateContradictionRiskGuard_BlocksMissingEvidenceStaleExcludedLockedAndUnsafeDependencies()
    {
        var blockedIds = new[]
        {
            "memory.contradiction.no-evidence",
            "memory.contradiction.stale-context",
            "memory.contradiction.excluded-context",
            "memory.contradiction.locked-unsafe",
            "memory.action.excluded-context",
            "memory.raw-sensitive-payload",
            "memory.unknown-authority",
            "memory.missing-freshness"
        };
        var results = WorkspaceMemoryCandidateContradictionRiskGuard.EvaluateCatalog()
            .Where(result => blockedIds.Contains(result.FixtureId, StringComparer.Ordinal))
            .ToList();

        Assert.AreEqual(blockedIds.Length, results.Count);
        Assert.IsTrue(results.All(result => result.Blocked), string.Join(", ", results.Where(result => !result.Blocked).Select(result => result.FixtureId)));
        Assert.IsTrue(results.All(result => !result.AllowsDecisionUse));
        Assert.IsTrue(results.All(result => !result.AllowsSafeNextStepUse));
        Assert.IsTrue(results.All(result => !result.AllowsCandidateInfluence));
    }

    [TestMethod]
    public void MemoryCandidateContradictionRiskGuard_BlocksRiskDecisionClaimActionSafeNextProviderSemanticLegacyAndDuplicates()
    {
        var blockedIds = new[]
        {
            "memory.risk.missing-severity",
            "memory.risk.promotes-decision",
            "memory.decision.no-human-review",
            "memory.decision.contradictory-evidence",
            "memory.claim.missing-confidence",
            "memory.claim.stale-evidence",
            "memory.action.missing-human-action",
            "memory.safe-next.critical-risk",
            "memory.safe-next.unresolved-contradiction",
            "memory.provider-derived-disabled",
            "memory.semantic-derived-disabled",
            "memory.legacy-no-provenance",
            "memory.duplicate-conflicting"
        };
        var results = WorkspaceMemoryCandidateContradictionRiskGuard.EvaluateCatalog()
            .Where(result => blockedIds.Contains(result.FixtureId, StringComparer.Ordinal))
            .ToList();

        Assert.AreEqual(blockedIds.Length, results.Count);
        Assert.IsTrue(results.All(result => result.Blocked), string.Join(", ", results.Where(result => !result.Blocked).Select(result => result.FixtureId)));
        Assert.IsTrue(results.All(result => !result.AllowsDecisionUse));
        Assert.IsTrue(results.All(result => !result.AllowsSafeNextStepUse));
        Assert.IsTrue(results.All(result => !result.AllowsCandidateInfluence));
    }

    [TestMethod]
    public void MemoryCandidateContradictionRiskGuard_OutputIsDeterministicAndHasNoProductionClaim()
    {
        var first = WorkspaceMemoryCandidateContradictionRiskGuard.EvaluateCatalog();
        var second = WorkspaceMemoryCandidateContradictionRiskGuard.EvaluateCatalog();
        var firstText = string.Join("\n", first.Select(result => $"{result.FixtureId}:{result.Decision}:{string.Join(",", result.Issues.Select(issue => issue.IssueKind))}"));
        var secondText = string.Join("\n", second.Select(result => $"{result.FixtureId}:{result.Decision}:{string.Join(",", result.Issues.Select(issue => issue.IssueKind))}"));

        Assert.AreEqual(firstText, secondText);
        Assert.IsFalse(firstText.Contains("production" + "-ready", StringComparison.OrdinalIgnoreCase));
        Assert.IsFalse(firstText.Contains("candidate promoted", StringComparison.OrdinalIgnoreCase));
        Assert.IsFalse(firstText.Contains("durable memory", StringComparison.OrdinalIgnoreCase));
        Assert.IsFalse(firstText.Contains("risk is decision", StringComparison.OrdinalIgnoreCase));
        Assert.IsFalse(firstText.Contains("contradiction resolved automatically", StringComparison.OrdinalIgnoreCase));
    }

    [TestMethod]
    public void WorkspaceContextPacketSurface_IsReadOnlyDeterministicFixtureSafeAndActionless()
    {
        var first = WorkspaceContextPacketReadOnlySurfacePresenter.CreateFixture();
        var second = WorkspaceContextPacketReadOnlySurfacePresenter.CreateFixture();

        Assert.IsTrue(first.ReadOnly);
        Assert.IsTrue(first.Deterministic);
        Assert.IsTrue(first.FixtureSafe);
        Assert.IsTrue(first.NoSideEffectProof.Passes);
        Assert.AreEqual("READ_ONLY_FIXTURE_SAFE_NO_ACTIONS_NO_EXPORT", first.Mode);
        Assert.AreEqual(first.ReadOnlySummary, second.ReadOnlySummary);
        Assert.AreEqual(0, first.ProductActionsCount);
        Assert.AreEqual(0, first.ExportActionsCount);
        Assert.IsFalse(first.HasDurableMemory);
        Assert.IsTrue(first.Sections.All(section => section.NoSideEffectProof.Passes));
        Assert.IsTrue(first.Sections.All(section => section.ProductActionsCount == 0));
        Assert.IsTrue(first.Sections.All(section => section.ExportActionsCount == 0));
    }

    [TestMethod]
    public void WorkspaceContextPacketSurface_ContainsMinimumSections()
    {
        var surface = WorkspaceContextPacketReadOnlySurfacePresenter.CreateFixture();
        var ids = surface.Sections.Select(section => section.SectionId).ToHashSet(StringComparer.Ordinal);

        Assert.AreEqual(24, surface.Sections.Count);
        CollectionAssert.IsSubsetOf(
            new[]
            {
                "context.packet.executive-summary",
                "workspace.identity.fixture",
                "selected.context",
                "locked.context",
                "excluded.context",
                "authority.freshness.guard.summary",
                "selection.lock.exclusion.guard.summary",
                "memory.candidate.guard.summary",
                "contradiction.candidates",
                "risk.candidates",
                "decision.candidates",
                "claim.candidates",
                "action.candidates",
                "safe.next.step.status",
                "human.review.requirements",
                "missing.stale.context.warnings",
                "blocked.context.candidate.list",
                "provider.cloud.disabled",
                "semantic.vector.disabled",
                "durable.memory.disabled",
                "runtime.live.disabled",
                "no.side.effect.proof",
                "documented.debt",
                "next.recommended.block"
            },
            ids.ToArray());
    }

    [TestMethod]
    public void WorkspaceContextPacketSurface_ShowsContextGuardAndCandidateSummaries()
    {
        var surface = WorkspaceContextPacketReadOnlySurfacePresenter.CreateFixture();

        Assert.IsTrue(surface.Packet.SelectedContext.Count > 0);
        Assert.IsTrue(surface.Packet.LockedContext.Count > 0);
        Assert.IsTrue(surface.Packet.ExcludedContext.Count > 0);
        Assert.IsTrue(surface.GuardSummaries.Any(summary => summary.Contains("Authority/freshness fixtures: 20", StringComparison.Ordinal)));
        Assert.IsTrue(surface.GuardSummaries.Any(summary => summary.Contains("Selection/lock/exclusion fixtures: 22", StringComparison.Ordinal)));
        Assert.IsTrue(surface.GuardSummaries.Any(summary => summary.Contains("Memory candidate fixtures: 24", StringComparison.Ordinal)));
        Assert.IsTrue(surface.CandidateSummaries.Any(summary => summary.Contains("Candidate is not memory", StringComparison.Ordinal)));
        Assert.IsTrue(surface.HumanReviewRequirements.Count > 0);
        Assert.IsTrue(surface.Blockers.Count > 0);
        Assert.IsTrue(surface.Warnings.Count > 0);
    }

    [TestMethod]
    public void WorkspaceContextPacketSurface_IncludesDisabledCapabilityNotices()
    {
        var surface = WorkspaceContextPacketReadOnlySurfacePresenter.CreateFixture();
        var text = string.Join("\n", surface.DisabledNotices.Concat(surface.Sections.SelectMany(section => section.Blockers)));

        StringAssert.Contains(text, "Provider/cloud");
        StringAssert.Contains(text, "Semantic/vector");
        StringAssert.Contains(text, "Durable memory");
        StringAssert.Contains(text, "Runtime/live");
        Assert.IsTrue(surface.Sections.Any(section => section.SectionId == "provider.cloud.disabled" && section.Status == WorkspaceContextItemStatus.Disabled));
        Assert.IsTrue(surface.Sections.Any(section => section.SectionId == "semantic.vector.disabled" && section.Status == WorkspaceContextItemStatus.Disabled));
        Assert.IsTrue(surface.Sections.Any(section => section.SectionId == "durable.memory.disabled" && section.Status == WorkspaceContextItemStatus.Disabled));
        Assert.IsTrue(surface.Sections.Any(section => section.SectionId == "runtime.live.disabled" && section.Status == WorkspaceContextItemStatus.Disabled));
    }

    [TestMethod]
    public void WorkspaceContextPacketSurface_DoesNotPromoteCandidatesOrClaimProductionReadiness()
    {
        var surface = WorkspaceContextPacketReadOnlySurfacePresenter.CreateFixture();
        var text = string.Join(
            "\n",
            surface.ReadOnlySummary,
            string.Join("\n", surface.GuardSummaries),
            string.Join("\n", surface.CandidateSummaries),
            string.Join("\n", surface.Sections.Select(section => $"{section.Title} {string.Join(" ", section.Warnings)} {string.Join(" ", section.Blockers)}")));

        Assert.IsFalse(text.Contains("production" + "-ready", StringComparison.OrdinalIgnoreCase));
        Assert.IsFalse(text.Contains("candidate promoted", StringComparison.OrdinalIgnoreCase));
        Assert.IsFalse(text.Contains("memory persisted", StringComparison.OrdinalIgnoreCase));
        Assert.IsFalse(text.Contains("risk is decision", StringComparison.OrdinalIgnoreCase));
        Assert.IsFalse(text.Contains("export file created", StringComparison.OrdinalIgnoreCase));
        Assert.IsFalse(text.Contains("action command", StringComparison.OrdinalIgnoreCase));
    }

    [TestMethod]
    public void WorkspaceContextPacketExportPreview_IsReadOnlyDeterministicInMemoryAndActionless()
    {
        var first = WorkspaceContextPacketExportReadOnlyPresenter.CreateFixture();
        var second = WorkspaceContextPacketExportReadOnlyPresenter.CreateFixture();

        Assert.IsTrue(first.ReadOnly);
        Assert.IsTrue(first.Deterministic);
        Assert.IsTrue(first.FixtureSafe);
        Assert.IsTrue(first.NoSideEffectProof.Passes);
        Assert.AreEqual("READ_ONLY_IN_MEMORY_EXPORT_PREVIEW_NO_FILE_NO_CLIPBOARD_NO_DOWNLOAD", first.Mode);
        Assert.AreEqual(first.PreviewText, second.PreviewText);
        Assert.AreEqual(0, first.Manifest.ProductActionsCount);
        Assert.AreEqual(0, first.Manifest.ExportActionsCount);
        Assert.IsFalse(first.HasRealExport);
        Assert.IsFalse(first.HasProductActions);
        Assert.IsFalse(first.HasExportActions);
        Assert.IsFalse(first.HasDurableMemory);
        Assert.IsTrue(first.Sections.All(section => section.NoSideEffectProof.Passes));
        Assert.IsTrue(first.Sections.All(section => section.IncludedInPreview));
        Assert.IsTrue(first.Sections.All(section => !section.PhysicalExportOccurred));
    }

    [TestMethod]
    public void WorkspaceContextPacketExportPreview_ManifestDeclaresNoFileClipboardDownload()
    {
        var preview = WorkspaceContextPacketExportReadOnlyPresenter.CreateFixture();
        var manifest = preview.Manifest;

        Assert.AreEqual(WorkspaceContextPacketExportPreviewKind.MarkdownLikeText, manifest.FormatPreviewKind);
        Assert.AreEqual("WorkspaceContextPacketReadOnlySurfacePresenter.CreateFixture", manifest.SourceFixture);
        Assert.IsFalse(manifest.PhysicalFileCreated);
        Assert.IsFalse(manifest.ClipboardUsed);
        Assert.IsFalse(manifest.DownloadStarted);
        Assert.AreEqual(0, manifest.ProductActionsCount);
        Assert.AreEqual(0, manifest.ExportActionsCount);
        Assert.IsFalse(manifest.ContainsRawPayload);
        Assert.IsFalse(manifest.ContainsSecretLikeContent);
        Assert.IsFalse(manifest.ContainsDurableMemory);
        Assert.IsTrue(manifest.NoSideEffectProof.Passes);
    }

    [TestMethod]
    public void WorkspaceContextPacketExportPreview_ContainsMinimumSections()
    {
        var preview = WorkspaceContextPacketExportReadOnlyPresenter.CreateFixture();
        var ids = preview.Sections.Select(section => section.SectionId).ToHashSet(StringComparer.Ordinal);

        Assert.AreEqual(26, preview.Sections.Count);
        CollectionAssert.IsSubsetOf(
            new[]
            {
                "export.manifest",
                "executive.summary",
                "workspace.identity.fixture",
                "context.packet.summary",
                "selected.context",
                "locked.context",
                "excluded.context",
                "authority.freshness.guard.summary",
                "selection.lock.exclusion.guard.summary",
                "memory.candidate.guard.summary",
                "contradiction.candidates",
                "risk.candidates",
                "decision.candidates",
                "claim.candidates",
                "action.candidates",
                "safe.next.step.status",
                "human.review.requirements",
                "missing.stale.context.warnings",
                "blocked.context.candidate.list",
                "provider.cloud.disabled",
                "semantic.vector.disabled",
                "durable.memory.disabled",
                "runtime.live.disabled",
                "no.side.effect.proof",
                "documented.debt",
                "next.recommended.block"
            },
            ids.ToArray());
    }

    [TestMethod]
    public void WorkspaceContextPacketExportPreview_IncludesGuardSummariesHumanReviewDisabledNoticesAndNextStep()
    {
        var preview = WorkspaceContextPacketExportReadOnlyPresenter.CreateFixture();
        var text = string.Join(
            "\n",
            preview.PreviewText,
            string.Join("\n", preview.DisabledNotices),
            string.Join("\n", preview.SourceSurface.GuardSummaries),
            string.Join("\n", preview.SourceSurface.CandidateSummaries),
            string.Join("\n", preview.Sections.Select(section => section.SectionId)));

        StringAssert.Contains(text, "Authority/freshness fixtures: 20");
        StringAssert.Contains(text, "Selection/lock/exclusion fixtures: 22");
        StringAssert.Contains(text, "Memory candidate fixtures: 24");
        StringAssert.Contains(text, "HumanReviewRequirements:");
        StringAssert.Contains(text, "Provider/cloud");
        StringAssert.Contains(text, "Semantic/vector");
        StringAssert.Contains(text, "Durable memory");
        StringAssert.Contains(text, "Runtime/live");
        Assert.AreEqual("PHASE_D_CONTEXT_MEMORY_CLOSEOUT_AUDIT_PREP", preview.NextSafeStep);
        Assert.IsTrue(preview.Blockers.Count > 0);
        Assert.IsTrue(preview.Warnings.Count > 0);
        Assert.IsTrue(preview.Exclusions.Count >= 4);
    }

    [TestMethod]
    public void WorkspaceContextPacketExportPreview_DoesNotLeakRawSecretDurableMemoryOrProductionClaim()
    {
        var preview = WorkspaceContextPacketExportReadOnlyPresenter.CreateFixture();
        var text = string.Join(
            "\n",
            preview.PreviewText,
            string.Join("\n", preview.Warnings),
            string.Join("\n", preview.Blockers),
            string.Join("\n", preview.Exclusions),
            string.Join("\n", preview.Sections.Select(section => $"{section.Title} {string.Join(" ", section.Warnings)} {string.Join(" ", section.Blockers)}")));

        Assert.IsFalse(text.Contains("sk-", StringComparison.OrdinalIgnoreCase));
        Assert.IsFalse(text.Contains("Bearer ", StringComparison.OrdinalIgnoreCase));
        Assert.IsFalse(text.Contains("PRIVATE KEY", StringComparison.OrdinalIgnoreCase));
        Assert.IsFalse(text.Contains("AKIA", StringComparison.OrdinalIgnoreCase));
        Assert.IsFalse(text.Contains("ghp_", StringComparison.OrdinalIgnoreCase));
        Assert.IsFalse(text.Contains("production" + "-ready", StringComparison.OrdinalIgnoreCase));
        Assert.IsFalse(text.Contains("memory persisted", StringComparison.OrdinalIgnoreCase));
        Assert.IsFalse(text.Contains("export file created", StringComparison.OrdinalIgnoreCase));
        Assert.IsFalse(text.Contains("clipboard used", StringComparison.OrdinalIgnoreCase));
        Assert.IsFalse(text.Contains("download started", StringComparison.OrdinalIgnoreCase));
        Assert.IsFalse(text.Contains("candidate promoted", StringComparison.OrdinalIgnoreCase));
    }
}
