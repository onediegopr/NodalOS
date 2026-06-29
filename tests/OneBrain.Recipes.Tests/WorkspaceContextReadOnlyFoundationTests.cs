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
}
