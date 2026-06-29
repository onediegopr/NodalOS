using Microsoft.VisualStudio.TestTools.UnitTesting;
using OneBrain.Core.Context;

namespace OneBrain.Safety.Tests;

[TestClass]
[TestCategory("WorkspaceContextMemory")]
[TestCategory("PhaseDContextWorkspaceMemory")]
public sealed class WorkspaceContextReadOnlyFoundationSafetyTests
{
    private const string FoundationPath = "src/OneBrain.Core/Context/WorkspaceContextReadOnlyFoundation.cs";

    [TestMethod]
    public void FoundationSource_HasNoFilesystemDatabaseProviderVectorRuntimeOrServiceImplementation()
    {
        var source = ReadRepoText(FoundationPath);
        var forbidden = new[]
        {
            "File.Read",
            "File.Write",
            "FileStream",
            "Directory.",
            "Directory.CreateDirectory",
            "Path.",
            "Microsoft.Data.Sqlite",
            "SQLiteConnection",
            "SqlConnection",
            "DbContext",
            "IDbConnection",
            "HttpClient",
            "WebSocket",
            "Process.Start",
            "ServiceCollection",
            "AddSingleton",
            "AddScoped",
            "AddTransient",
            "OpenAI",
            "EmbeddingClient",
            "VectorStore",
            "KernelMemory"
        };

        foreach (var term in forbidden)
        {
            Assert.IsFalse(source.Contains(term, StringComparison.OrdinalIgnoreCase), term);
        }
    }

    [TestMethod]
    public void FoundationProof_DisablesAllSideEffectsAndRuntimeCapabilities()
    {
        var packet = WorkspaceContextReadOnlyPresenter.CreateFixture();
        var proof = packet.NoSideEffectProof;

        Assert.IsTrue(proof.Passes);
        Assert.IsFalse(proof.WorkspaceFilesystemReadAttempted);
        Assert.IsFalse(proof.FilesystemWriteAttempted);
        Assert.IsFalse(proof.DatabaseTouched);
        Assert.IsFalse(proof.DurablePersistenceActive);
        Assert.IsFalse(proof.DurableMemoryActive);
        Assert.IsFalse(proof.VectorSemanticBackendTouched);
        Assert.IsFalse(proof.LlmProviderTouched);
        Assert.IsFalse(proof.ProviderCloudTouched);
        Assert.IsFalse(proof.MigrationRunnerStarted);
        Assert.IsFalse(proof.MigrationExecuted);
        Assert.IsFalse(proof.RuntimeTouched);
        Assert.IsFalse(proof.BrowserCdpTouched);
        Assert.IsFalse(proof.WcuTouched);
        Assert.IsFalse(proof.OcrTouched);
        Assert.IsFalse(proof.ProductActionExposed);
        Assert.IsFalse(proof.ProductServiceRegistered);
    }

    [TestMethod]
    public void FoundationSources_DoNotReadWorkspaceOrUseProviderCloudOrVectorBackend()
    {
        var packet = WorkspaceContextReadOnlyPresenter.CreateFixture();

        Assert.IsTrue(packet.Sources.All(source => source.FixtureOnly));
        Assert.IsTrue(packet.Sources.All(source => !source.ReadsWorkspaceFilesystem));
        Assert.IsTrue(packet.Sources.All(source => !source.UsesProviderCloud));
        Assert.IsTrue(packet.Sources.All(source => !source.UsesVectorSemanticBackend));
        Assert.IsTrue(packet.Sources.All(source => source.NoSideEffectProof.Passes));
    }

    [TestMethod]
    public void FoundationMemoryCandidates_ArePreviewOnlyAndNonDurable()
    {
        var packet = WorkspaceContextReadOnlyPresenter.CreateFixture();

        Assert.IsFalse(packet.HasDurableMemory);
        Assert.IsTrue(packet.MemoryCandidates.Count >= 5);
        Assert.IsTrue(packet.MemoryCandidates.All(candidate => !candidate.DurableMemoryEnabled));
        Assert.IsTrue(packet.MemoryCandidates.All(candidate => candidate.NoSideEffectProof.Passes));
        Assert.IsTrue(packet.MemoryCandidates.Any(candidate => candidate.Kind == WorkspaceContextItemKind.ContradictionMemoryPreview));
        Assert.IsTrue(packet.MemoryCandidates.Any(candidate => candidate.Kind == WorkspaceContextItemKind.RiskMemoryPreview));
        Assert.IsTrue(packet.MemoryCandidates.Any(candidate => candidate.Kind == WorkspaceContextItemKind.DecisionMemoryPreview));
    }

    [TestMethod]
    public void FoundationDoesNotReuseEilReadWriteStoresAsImplementation()
    {
        var source = ReadRepoText(FoundationPath);

        Assert.IsFalse(source.Contains("DisabledEvidenceIntelligenceReadStore", StringComparison.Ordinal));
        Assert.IsFalse(source.Contains("DisabledEvidenceIntelligenceWriteStore", StringComparison.Ordinal));
        Assert.IsFalse(source.Contains("ProcessMemoryStore", StringComparison.Ordinal));
        Assert.IsFalse(source.Contains("WorkflowRetrievalService", StringComparison.Ordinal));
        StringAssert.Contains(source, "EvidenceIntelligenceAuditDashboardReadOnlyPresenter.CreateFixture");
    }

    [TestMethod]
    public void FoundationText_HasNoContextMemoryOrProductionOverclaim()
    {
        var packet = WorkspaceContextReadOnlyPresenter.CreateFixture();
        var text = string.Join(
            "\n",
            packet.ReadOnlySummary,
            packet.Summary,
            packet.ProviderCloudNotice,
            packet.SemanticVectorNotice,
            packet.SafeNextStep,
            string.Join("\n", packet.Items.Select(item => item.Summary)),
            string.Join("\n", packet.MemoryCandidates.Select(candidate => candidate.Preview)),
            string.Join("\n", packet.Warnings),
            string.Join("\n", packet.Blockers));

        var forbidden = new[]
        {
            "production" + "-ready",
            "memory persisted",
            "durable memory active",
            "semantic search enabled",
            "vector backend enabled",
            "provider call enabled",
            "workspace scan completed",
            "runtime action enabled",
            "live automation enabled",
            "filesystem indexed"
        };

        foreach (var term in forbidden)
        {
            Assert.IsFalse(text.Contains(term, StringComparison.OrdinalIgnoreCase), term);
        }
    }

    [TestMethod]
    public void FoundationText_HasNoSyntheticSecretLeakage()
    {
        var packet = WorkspaceContextReadOnlyPresenter.CreateFixture();
        var text = string.Join(
            "\n",
            packet.ReadOnlySummary,
            packet.Summary,
            string.Join("\n", packet.Items.Select(item => item.Summary)),
            string.Join("\n", packet.MemoryCandidates.Select(candidate => candidate.Preview)));

        Assert.IsFalse(text.Contains("sk-", StringComparison.OrdinalIgnoreCase));
        Assert.IsFalse(text.Contains("Bearer" + " ", StringComparison.OrdinalIgnoreCase));
        Assert.IsFalse(text.Contains("PRIVATE KEY", StringComparison.OrdinalIgnoreCase));
        Assert.IsFalse(text.Contains("AKIA", StringComparison.OrdinalIgnoreCase));
        Assert.IsFalse(text.Contains("ghp_", StringComparison.OrdinalIgnoreCase));
    }

    [TestMethod]
    public void AuthorityFreshnessGuard_SourceHasNoFilesystemDatabaseProviderVectorRuntimeOrServiceImplementation()
    {
        var source = ReadRepoText(FoundationPath);
        var forbidden = new[]
        {
            "File.Read",
            "File.Write",
            "FileStream",
            "Directory.",
            "Directory.CreateDirectory",
            "Path.",
            "Microsoft.Data.Sqlite",
            "SQLiteConnection",
            "SqlConnection",
            "DbContext",
            "IDbConnection",
            "HttpClient",
            "WebSocket",
            "Process.Start",
            "ServiceCollection",
            "AddSingleton",
            "AddScoped",
            "AddTransient",
            "OpenAI",
            "EmbeddingClient",
            "VectorStore",
            "KernelMemory"
        };

        foreach (var term in forbidden)
        {
            Assert.IsFalse(source.Contains(term, StringComparison.OrdinalIgnoreCase), term);
        }
    }

    [TestMethod]
    public void AuthorityFreshnessGuard_AllFixturesPreserveNoSideEffectProof()
    {
        foreach (var result in WorkspaceContextAuthorityFreshnessGuard.EvaluateCatalog())
        {
            var proof = result.NoSideEffectProof;

            Assert.IsTrue(proof.Passes, result.FixtureId);
            Assert.IsFalse(proof.WorkspaceFilesystemReadAttempted, result.FixtureId);
            Assert.IsFalse(proof.FilesystemWriteAttempted, result.FixtureId);
            Assert.IsFalse(proof.DatabaseTouched, result.FixtureId);
            Assert.IsFalse(proof.DurablePersistenceActive, result.FixtureId);
            Assert.IsFalse(proof.DurableMemoryActive, result.FixtureId);
            Assert.IsFalse(proof.VectorSemanticBackendTouched, result.FixtureId);
            Assert.IsFalse(proof.LlmProviderTouched, result.FixtureId);
            Assert.IsFalse(proof.ProviderCloudTouched, result.FixtureId);
            Assert.IsFalse(proof.MigrationRunnerStarted, result.FixtureId);
            Assert.IsFalse(proof.MigrationExecuted, result.FixtureId);
            Assert.IsFalse(proof.RuntimeTouched, result.FixtureId);
            Assert.IsFalse(proof.BrowserCdpTouched, result.FixtureId);
            Assert.IsFalse(proof.WcuTouched, result.FixtureId);
            Assert.IsFalse(proof.OcrTouched, result.FixtureId);
            Assert.IsFalse(proof.ProductActionExposed, result.FixtureId);
            Assert.IsFalse(proof.ProductServiceRegistered, result.FixtureId);
        }
    }

    [TestMethod]
    public void AuthorityFreshnessGuard_NoTrustByDefaultForUnsafeFixtures()
    {
        var unsafeResults = WorkspaceContextAuthorityFreshnessGuard.EvaluateCatalog()
            .Where(result => result.Decision != WorkspaceContextAuthorityFreshnessDecision.AllowedReadOnly)
            .ToList();

        Assert.IsTrue(unsafeResults.Count >= 18);
        Assert.IsTrue(unsafeResults.All(result => !result.AllowsDecisionUse), string.Join(", ", unsafeResults.Where(result => result.AllowsDecisionUse).Select(result => result.FixtureId)));
        Assert.IsTrue(unsafeResults.All(result => !result.AllowsSafeNextStepUse), string.Join(", ", unsafeResults.Where(result => result.AllowsSafeNextStepUse).Select(result => result.FixtureId)));
        Assert.IsTrue(unsafeResults.All(result => !result.AllowsMemoryCandidateUse), string.Join(", ", unsafeResults.Where(result => result.AllowsMemoryCandidateUse).Select(result => result.FixtureId)));
    }

    [TestMethod]
    public void AuthorityFreshnessGuard_BlocksProviderSemanticRawSensitiveLegacyAndContradictorySources()
    {
        var results = WorkspaceContextAuthorityFreshnessGuard.EvaluateCatalog();

        AssertBlocked(results, "ctx.provider-derived-disabled", WorkspaceContextAuthorityFreshnessIssueKind.ProviderDerivedWhileDisabled);
        AssertBlocked(results, "ctx.semantic-derived-disabled", WorkspaceContextAuthorityFreshnessIssueKind.SemanticDerivedWhileDisabled);
        AssertBlocked(results, "ctx.raw-payload", WorkspaceContextAuthorityFreshnessIssueKind.RawPayloadContext);
        AssertBlocked(results, "ctx.sensitive-without-clearance", WorkspaceContextAuthorityFreshnessIssueKind.SensitiveContextWithoutClearance);
        AssertBlocked(results, "ctx.legacy-no-provenance", WorkspaceContextAuthorityFreshnessIssueKind.LegacyWithoutProvenance);
        AssertBlocked(results, "ctx.contradictory", WorkspaceContextAuthorityFreshnessIssueKind.ContradictoryContext);
    }

    [TestMethod]
    public void AuthorityFreshnessGuard_BlocksStaleMissingUnknownAndDecisionMemoryWithoutHumanReview()
    {
        var results = WorkspaceContextAuthorityFreshnessGuard.EvaluateCatalog();

        AssertBlocked(results, "ctx.stale-context", WorkspaceContextAuthorityFreshnessIssueKind.StaleContext);
        AssertBlocked(results, "ctx.missing-freshness", WorkspaceContextAuthorityFreshnessIssueKind.MissingFreshness);
        AssertBlocked(results, "ctx.unknown-authority", WorkspaceContextAuthorityFreshnessIssueKind.UnknownAuthority);
        AssertBlocked(results, "safe-next-step.stale", WorkspaceContextAuthorityFreshnessIssueKind.SafeNextStepReliesOnStaleContext);
        AssertBlocked(results, "memory.decision-missing-human-review", WorkspaceContextAuthorityFreshnessIssueKind.DecisionMemoryMissingHumanReview);
    }

    [TestMethod]
    public void AuthorityFreshnessGuard_HasNoAuthorityFreshnessContextMemoryOrProductionOverclaim()
    {
        var text = string.Join(
            "\n",
            WorkspaceContextAuthorityFreshnessGuard.EvaluateCatalog()
                .Select(result => $"{result.FixtureId} {result.Decision} {string.Join(" ", result.Warnings)} {string.Join(" ", result.Blockers)}"));

        var forbidden = new[]
        {
            "production" + "-ready",
            "memory persisted",
            "durable memory active",
            "semantic search enabled",
            "vector backend enabled",
            "provider call enabled",
            "workspace scan completed",
            "runtime action enabled",
            "live automation enabled",
            "filesystem indexed",
            "trusted automatically",
            "freshness guaranteed",
            "authority granted"
        };

        foreach (var term in forbidden)
        {
            Assert.IsFalse(text.Contains(term, StringComparison.OrdinalIgnoreCase), term);
        }
    }

    [TestMethod]
    public void SelectionLockExclusionGuard_SourceHasNoFilesystemDatabaseProviderVectorRuntimeOrServiceImplementation()
    {
        var source = ReadRepoText(FoundationPath);
        var forbidden = new[]
        {
            "File.Read",
            "File.Write",
            "FileStream",
            "Directory.",
            "Directory.CreateDirectory",
            "Path.",
            "Microsoft.Data.Sqlite",
            "SQLiteConnection",
            "SqlConnection",
            "DbContext",
            "IDbConnection",
            "HttpClient",
            "WebSocket",
            "Process.Start",
            "ServiceCollection",
            "AddSingleton",
            "AddScoped",
            "AddTransient",
            "OpenAI",
            "EmbeddingClient",
            "VectorStore",
            "KernelMemory"
        };

        foreach (var term in forbidden)
        {
            Assert.IsFalse(source.Contains(term, StringComparison.OrdinalIgnoreCase), term);
        }
    }

    [TestMethod]
    public void SelectionLockExclusionGuard_AllFixturesPreserveNoSideEffectProof()
    {
        foreach (var result in WorkspaceContextSelectionLockExclusionGuard.EvaluateCatalog())
        {
            var proof = result.NoSideEffectProof;

            Assert.IsTrue(proof.Passes, result.FixtureId);
            Assert.IsFalse(proof.WorkspaceFilesystemReadAttempted, result.FixtureId);
            Assert.IsFalse(proof.FilesystemWriteAttempted, result.FixtureId);
            Assert.IsFalse(proof.DatabaseTouched, result.FixtureId);
            Assert.IsFalse(proof.DurablePersistenceActive, result.FixtureId);
            Assert.IsFalse(proof.DurableMemoryActive, result.FixtureId);
            Assert.IsFalse(proof.VectorSemanticBackendTouched, result.FixtureId);
            Assert.IsFalse(proof.LlmProviderTouched, result.FixtureId);
            Assert.IsFalse(proof.ProviderCloudTouched, result.FixtureId);
            Assert.IsFalse(proof.MigrationRunnerStarted, result.FixtureId);
            Assert.IsFalse(proof.MigrationExecuted, result.FixtureId);
            Assert.IsFalse(proof.RuntimeTouched, result.FixtureId);
            Assert.IsFalse(proof.BrowserCdpTouched, result.FixtureId);
            Assert.IsFalse(proof.WcuTouched, result.FixtureId);
            Assert.IsFalse(proof.OcrTouched, result.FixtureId);
            Assert.IsFalse(proof.ProductActionExposed, result.FixtureId);
            Assert.IsFalse(proof.ProductServiceRegistered, result.FixtureId);
        }
    }

    [TestMethod]
    public void SelectionLockExclusionGuard_NoTrustByDefaultForUnsafeFixtures()
    {
        var unsafeResults = WorkspaceContextSelectionLockExclusionGuard.EvaluateCatalog()
            .Where(result => result.Decision != WorkspaceContextSelectionLockExclusionDecision.AllowedReadOnly)
            .ToList();

        Assert.IsTrue(unsafeResults.Count >= 21);
        Assert.IsTrue(unsafeResults.All(result => !result.AllowsDecisionUse), string.Join(", ", unsafeResults.Where(result => result.AllowsDecisionUse).Select(result => result.FixtureId)));
        Assert.IsTrue(unsafeResults.All(result => !result.AllowsSafeNextStepUse), string.Join(", ", unsafeResults.Where(result => result.AllowsSafeNextStepUse).Select(result => result.FixtureId)));
        Assert.IsTrue(unsafeResults.All(result => !result.AllowsMemoryInfluence), string.Join(", ", unsafeResults.Where(result => result.AllowsMemoryInfluence).Select(result => result.FixtureId)));
    }

    [TestMethod]
    public void SelectionLockExclusionGuard_BlocksExcludedDependenciesAndUnsafeSelection()
    {
        var results = WorkspaceContextSelectionLockExclusionGuard.EvaluateCatalog();

        AssertSelectionBlocked(results, "ctx.selected-excluded", WorkspaceContextSelectionLockExclusionIssueKind.SelectedExcluded);
        AssertSelectionBlocked(results, "memory.excluded-reference", WorkspaceContextSelectionLockExclusionIssueKind.ExcludedReferencedByMemory);
        AssertSelectionBlocked(results, "safe-next-step.excluded-reference", WorkspaceContextSelectionLockExclusionIssueKind.ExcludedReferencedBySafeNextStep);
        AssertSelectionBlocked(results, "claim-action.excluded-reference", WorkspaceContextSelectionLockExclusionIssueKind.ExcludedReferencedByClaimActionPreview);
        AssertSelectionBlocked(results, "graph.excluded-reference", WorkspaceContextSelectionLockExclusionIssueKind.ExcludedReferencedByGraph);
        AssertSelectionBlocked(results, "ctx.selected-raw-sensitive", WorkspaceContextSelectionLockExclusionIssueKind.UnsafeSelectedContent);
        AssertSelectionBlocked(results, "dashboard.excluded-candidate", WorkspaceContextSelectionLockExclusionIssueKind.ExcludedAppearsInExportDashboardCandidate);
    }

    [TestMethod]
    public void SelectionLockExclusionGuard_BlocksLockedStaleUnknownProviderSemanticLegacyAndDuplicateCases()
    {
        var results = WorkspaceContextSelectionLockExclusionGuard.EvaluateCatalog();

        AssertSelectionBlocked(results, "ctx.selected-stale", WorkspaceContextSelectionLockExclusionIssueKind.SelectedStale);
        AssertSelectionBlocked(results, "ctx.selected-unknown-authority", WorkspaceContextSelectionLockExclusionIssueKind.SelectedUnknownAuthority);
        AssertSelectionBlocked(results, "ctx.selected-missing-freshness", WorkspaceContextSelectionLockExclusionIssueKind.SelectedMissingFreshness);
        AssertSelectionBlocked(results, "ctx.selected-contradictory", WorkspaceContextSelectionLockExclusionIssueKind.SelectedContradictory);
        AssertSelectionBlocked(results, "ctx.locked-missing-evidence", WorkspaceContextSelectionLockExclusionIssueKind.LockedMissingEvidence);
        AssertSelectionBlocked(results, "memory.locked-promote", WorkspaceContextSelectionLockExclusionIssueKind.LockedMemoryPromotion);
        AssertSelectionBlocked(results, "ctx.selected-provider-disabled", WorkspaceContextSelectionLockExclusionIssueKind.ProviderDerivedWhileDisabled);
        AssertSelectionBlocked(results, "ctx.selected-semantic-disabled", WorkspaceContextSelectionLockExclusionIssueKind.SemanticDerivedWhileDisabled);
        AssertSelectionBlocked(results, "ctx.selected-legacy-no-provenance", WorkspaceContextSelectionLockExclusionIssueKind.LegacyWithoutProvenance);
        AssertSelectionBlocked(results, "ctx.duplicate-conflicting-lock", WorkspaceContextSelectionLockExclusionIssueKind.DuplicateConflictingLockState);
        AssertSelectionBlocked(results, "ctx.empty-selected-safe-next-step", WorkspaceContextSelectionLockExclusionIssueKind.EmptySelectionWithDependentSafeNextStep);
        AssertSelectionBlocked(results, "ctx.locked-review-missing", WorkspaceContextSelectionLockExclusionIssueKind.LockedMissingHumanReview);
    }

    [TestMethod]
    public void SelectionLockExclusionGuard_HasNoSelectionLockExclusionAuthorityFreshnessContextMemoryOrProductionOverclaim()
    {
        var text = string.Join(
            "\n",
            WorkspaceContextSelectionLockExclusionGuard.EvaluateCatalog()
                .Select(result => $"{result.FixtureId} {result.Decision} {string.Join(" ", result.Warnings)} {string.Join(" ", result.Blockers)}"));

        var forbidden = new[]
        {
            "production" + "-ready",
            "selection is trusted",
            "selected by default",
            "lock bypassed",
            "lock override enabled",
            "exclusion bypassed",
            "excluded context trusted",
            "authority granted",
            "freshness guaranteed",
            "memory persisted",
            "durable memory active",
            "semantic search enabled",
            "vector backend enabled",
            "provider call enabled",
            "workspace scan completed",
            "runtime action enabled",
            "live automation enabled",
            "filesystem indexed"
        };

        foreach (var term in forbidden)
        {
            Assert.IsFalse(text.Contains(term, StringComparison.OrdinalIgnoreCase), term);
        }
    }

    [TestMethod]
    public void MemoryCandidateContradictionRiskGuard_SourceHasNoFilesystemDatabaseProviderVectorRuntimeOrServiceImplementation()
    {
        var source = ReadRepoText(FoundationPath);
        var forbidden = new[]
        {
            "File.Read",
            "File.Write",
            "FileStream",
            "Directory.",
            "Directory.CreateDirectory",
            "Path.",
            "Microsoft.Data.Sqlite",
            "SQLiteConnection",
            "SqlConnection",
            "DbContext",
            "IDbConnection",
            "HttpClient",
            "WebSocket",
            "Process.Start",
            "ServiceCollection",
            "AddSingleton",
            "AddScoped",
            "AddTransient",
            "OpenAI",
            "EmbeddingClient",
            "VectorStore",
            "KernelMemory"
        };

        foreach (var term in forbidden)
        {
            Assert.IsFalse(source.Contains(term, StringComparison.OrdinalIgnoreCase), term);
        }
    }

    [TestMethod]
    public void MemoryCandidateContradictionRiskGuard_AllFixturesPreserveNoSideEffectProof()
    {
        foreach (var result in WorkspaceMemoryCandidateContradictionRiskGuard.EvaluateCatalog())
        {
            var proof = result.NoSideEffectProof;

            Assert.IsTrue(proof.Passes, result.FixtureId);
            Assert.IsFalse(proof.WorkspaceFilesystemReadAttempted, result.FixtureId);
            Assert.IsFalse(proof.FilesystemWriteAttempted, result.FixtureId);
            Assert.IsFalse(proof.DatabaseTouched, result.FixtureId);
            Assert.IsFalse(proof.DurablePersistenceActive, result.FixtureId);
            Assert.IsFalse(proof.DurableMemoryActive, result.FixtureId);
            Assert.IsFalse(proof.VectorSemanticBackendTouched, result.FixtureId);
            Assert.IsFalse(proof.LlmProviderTouched, result.FixtureId);
            Assert.IsFalse(proof.ProviderCloudTouched, result.FixtureId);
            Assert.IsFalse(proof.MigrationRunnerStarted, result.FixtureId);
            Assert.IsFalse(proof.MigrationExecuted, result.FixtureId);
            Assert.IsFalse(proof.RuntimeTouched, result.FixtureId);
            Assert.IsFalse(proof.BrowserCdpTouched, result.FixtureId);
            Assert.IsFalse(proof.WcuTouched, result.FixtureId);
            Assert.IsFalse(proof.OcrTouched, result.FixtureId);
            Assert.IsFalse(proof.ProductActionExposed, result.FixtureId);
            Assert.IsFalse(proof.ProductServiceRegistered, result.FixtureId);
            Assert.IsFalse(result.DurableMemoryEnabled, result.FixtureId);
        }
    }

    [TestMethod]
    public void MemoryCandidateContradictionRiskGuard_NoCandidatePromotesToDurableMemoryOrDecisionByDefault()
    {
        foreach (var result in WorkspaceMemoryCandidateContradictionRiskGuard.EvaluateCatalog())
        {
            Assert.IsFalse(result.DurableMemoryEnabled, result.FixtureId);
            Assert.IsFalse(result.AllowsDecisionUse, result.FixtureId);
            Assert.IsFalse(result.AllowsSafeNextStepUse, result.FixtureId);
            if (result.Decision != WorkspaceMemoryCandidateInfluenceDecision.AllowedReadOnlyWarning)
            {
                Assert.IsFalse(result.AllowsCandidateInfluence, result.FixtureId);
            }
        }
    }

    [TestMethod]
    public void MemoryCandidateContradictionRiskGuard_BlocksUnsafeSourcesAndDependencies()
    {
        var results = WorkspaceMemoryCandidateContradictionRiskGuard.EvaluateCatalog();

        AssertMemoryBlocked(results, "memory.contradiction.no-evidence", WorkspaceMemoryCandidateContradictionRiskIssueKind.CandidateWithoutEvidence);
        AssertMemoryBlocked(results, "memory.contradiction.stale-context", WorkspaceMemoryCandidateContradictionRiskIssueKind.CandidateUsesStaleContext);
        AssertMemoryBlocked(results, "memory.contradiction.excluded-context", WorkspaceMemoryCandidateContradictionRiskIssueKind.CandidateUsesExcludedContext);
        AssertMemoryBlocked(results, "memory.contradiction.locked-unsafe", WorkspaceMemoryCandidateContradictionRiskIssueKind.CandidateUsesLockedUnsafeContext);
        AssertMemoryBlocked(results, "memory.provider-derived-disabled", WorkspaceMemoryCandidateContradictionRiskIssueKind.ProviderDerivedWhileDisabled);
        AssertMemoryBlocked(results, "memory.semantic-derived-disabled", WorkspaceMemoryCandidateContradictionRiskIssueKind.SemanticDerivedWhileDisabled);
        AssertMemoryBlocked(results, "memory.legacy-no-provenance", WorkspaceMemoryCandidateContradictionRiskIssueKind.LegacyWithoutProvenance);
        AssertMemoryBlocked(results, "memory.raw-sensitive-payload", WorkspaceMemoryCandidateContradictionRiskIssueKind.RawSensitivePayload);
    }

    [TestMethod]
    public void MemoryCandidateContradictionRiskGuard_BlocksContradictionRiskClaimActionSafeNextAndDuplicateOverreach()
    {
        var results = WorkspaceMemoryCandidateContradictionRiskGuard.EvaluateCatalog();

        AssertMemoryBlocked(results, "memory.risk.missing-severity", WorkspaceMemoryCandidateContradictionRiskIssueKind.RiskMissingSeverity);
        AssertMemoryBlocked(results, "memory.risk.promotes-decision", WorkspaceMemoryCandidateContradictionRiskIssueKind.RiskCannotBecomeDecisionMemory);
        AssertMemoryBlocked(results, "memory.decision.no-human-review", WorkspaceMemoryCandidateContradictionRiskIssueKind.DecisionMissingHumanReview);
        AssertMemoryBlocked(results, "memory.decision.contradictory-evidence", WorkspaceMemoryCandidateContradictionRiskIssueKind.DecisionWithContradictoryEvidence);
        AssertMemoryBlocked(results, "memory.claim.missing-confidence", WorkspaceMemoryCandidateContradictionRiskIssueKind.ClaimMissingConfidence);
        AssertMemoryBlocked(results, "memory.action.missing-human-action", WorkspaceMemoryCandidateContradictionRiskIssueKind.ActionMissingRequiredHumanAction);
        AssertMemoryBlocked(results, "memory.safe-next.critical-risk", WorkspaceMemoryCandidateContradictionRiskIssueKind.SafeNextStepReliesOnCriticalRisk);
        AssertMemoryBlocked(results, "memory.safe-next.unresolved-contradiction", WorkspaceMemoryCandidateContradictionRiskIssueKind.SafeNextStepReliesOnUnresolvedContradiction);
        AssertMemoryBlocked(results, "memory.duplicate-conflicting", WorkspaceMemoryCandidateContradictionRiskIssueKind.DuplicateConflictingCandidates);
    }

    [TestMethod]
    public void MemoryCandidateContradictionRiskGuard_HasNoCandidateContradictionRiskContextMemoryOrProductionOverclaim()
    {
        var text = string.Join(
            "\n",
            WorkspaceMemoryCandidateContradictionRiskGuard.EvaluateCatalog()
                .Select(result => $"{result.FixtureId} {result.Decision} {string.Join(" ", result.Warnings)} {string.Join(" ", result.Blockers)}"));

        var forbidden = new[]
        {
            "production" + "-ready",
            "candidate promoted",
            "candidate persisted",
            "durable memory enabled",
            "durable memory active",
            "memory persisted",
            "risk is decision",
            "risk promoted to decision",
            "contradiction resolved automatically",
            "contradiction hidden",
            "safe next step approved",
            "semantic search enabled",
            "vector backend enabled",
            "provider call enabled",
            "workspace scan completed",
            "runtime action enabled",
            "live automation enabled",
            "filesystem indexed"
        };

        foreach (var term in forbidden)
        {
            Assert.IsFalse(text.Contains(term, StringComparison.OrdinalIgnoreCase), term);
        }
    }

    [TestMethod]
    public void WorkspaceContextPacketSurface_SourceHasNoFilesystemDatabaseProviderVectorRuntimeOrServiceImplementation()
    {
        var source = ReadRepoText(FoundationPath);
        var forbidden = new[]
        {
            "File.Read",
            "File.Write",
            "FileStream",
            "Directory.",
            "Directory.CreateDirectory",
            "Path.",
            "Microsoft.Data.Sqlite",
            "SQLiteConnection",
            "SqlConnection",
            "DbContext",
            "IDbConnection",
            "HttpClient",
            "WebSocket",
            "Process.Start",
            "ServiceCollection",
            "AddSingleton",
            "AddScoped",
            "AddTransient",
            "OpenAI",
            "EmbeddingClient",
            "VectorStore",
            "KernelMemory"
        };

        foreach (var term in forbidden)
        {
            Assert.IsFalse(source.Contains(term, StringComparison.OrdinalIgnoreCase), term);
        }
    }

    [TestMethod]
    public void WorkspaceContextPacketSurface_AllSectionsPreserveNoSideEffectProofAndNoActions()
    {
        var surface = WorkspaceContextPacketReadOnlySurfacePresenter.CreateFixture();

        Assert.IsTrue(surface.NoSideEffectProof.Passes);
        Assert.AreEqual(0, surface.ProductActionsCount);
        Assert.AreEqual(0, surface.ExportActionsCount);
        foreach (var section in surface.Sections)
        {
            var proof = section.NoSideEffectProof;

            Assert.IsTrue(proof.Passes, section.SectionId);
            Assert.AreEqual(0, section.ProductActionsCount, section.SectionId);
            Assert.AreEqual(0, section.ExportActionsCount, section.SectionId);
            Assert.IsFalse(proof.WorkspaceFilesystemReadAttempted, section.SectionId);
            Assert.IsFalse(proof.FilesystemWriteAttempted, section.SectionId);
            Assert.IsFalse(proof.DatabaseTouched, section.SectionId);
            Assert.IsFalse(proof.DurablePersistenceActive, section.SectionId);
            Assert.IsFalse(proof.DurableMemoryActive, section.SectionId);
            Assert.IsFalse(proof.VectorSemanticBackendTouched, section.SectionId);
            Assert.IsFalse(proof.LlmProviderTouched, section.SectionId);
            Assert.IsFalse(proof.ProviderCloudTouched, section.SectionId);
            Assert.IsFalse(proof.MigrationRunnerStarted, section.SectionId);
            Assert.IsFalse(proof.MigrationExecuted, section.SectionId);
            Assert.IsFalse(proof.RuntimeTouched, section.SectionId);
            Assert.IsFalse(proof.BrowserCdpTouched, section.SectionId);
            Assert.IsFalse(proof.WcuTouched, section.SectionId);
            Assert.IsFalse(proof.OcrTouched, section.SectionId);
            Assert.IsFalse(proof.ProductActionExposed, section.SectionId);
            Assert.IsFalse(proof.ProductServiceRegistered, section.SectionId);
        }
    }

    [TestMethod]
    public void WorkspaceContextPacketSurface_DisablesRuntimeProviderSemanticDurableMemoryAndExport()
    {
        var surface = WorkspaceContextPacketReadOnlySurfacePresenter.CreateFixture();
        var proof = surface.NoSideEffectProof;

        Assert.IsFalse(surface.HasDurableMemory);
        Assert.IsFalse(proof.WorkspaceFilesystemReadAttempted);
        Assert.IsFalse(proof.FilesystemWriteAttempted);
        Assert.IsFalse(proof.DatabaseTouched);
        Assert.IsFalse(proof.DurablePersistenceActive);
        Assert.IsFalse(proof.DurableMemoryActive);
        Assert.IsFalse(proof.VectorSemanticBackendTouched);
        Assert.IsFalse(proof.LlmProviderTouched);
        Assert.IsFalse(proof.ProviderCloudTouched);
        Assert.IsFalse(proof.MigrationRunnerStarted);
        Assert.IsFalse(proof.MigrationExecuted);
        Assert.IsFalse(proof.RuntimeTouched);
        Assert.IsFalse(proof.BrowserCdpTouched);
        Assert.IsFalse(proof.WcuTouched);
        Assert.IsFalse(proof.OcrTouched);
        Assert.IsTrue(surface.DisabledNotices.Any(notice => notice.Contains("Provider/cloud", StringComparison.Ordinal)));
        Assert.IsTrue(surface.DisabledNotices.Any(notice => notice.Contains("Semantic/vector", StringComparison.Ordinal)));
        Assert.IsTrue(surface.DisabledNotices.Any(notice => notice.Contains("Durable memory", StringComparison.Ordinal)));
        Assert.IsTrue(surface.DisabledNotices.Any(notice => notice.Contains("Export actions disabled", StringComparison.Ordinal)));
    }

    [TestMethod]
    public void WorkspaceContextPacketSurface_HasNoSurfaceContextMemoryExportOrProductionOverclaim()
    {
        var surface = WorkspaceContextPacketReadOnlySurfacePresenter.CreateFixture();
        var text = string.Join(
            "\n",
            surface.ReadOnlySummary,
            string.Join("\n", surface.GuardSummaries),
            string.Join("\n", surface.CandidateSummaries),
            string.Join("\n", surface.DisabledNotices),
            string.Join("\n", surface.Sections.Select(section => $"{section.SectionId} {section.Title} {string.Join(" ", section.Warnings)} {string.Join(" ", section.Blockers)}")));

        var forbidden = new[]
        {
            "production" + "-ready",
            "candidate promoted",
            "candidate persisted",
            "durable memory enabled",
            "durable memory active",
            "memory persisted",
            "risk is decision",
            "semantic search enabled",
            "vector backend enabled",
            "provider call enabled",
            "workspace scan completed",
            "runtime action enabled",
            "live automation enabled",
            "filesystem indexed",
            "action command exposed",
            "product action exposed",
            "export file created",
            "export written"
        };

        foreach (var term in forbidden)
        {
            Assert.IsFalse(text.Contains(term, StringComparison.OrdinalIgnoreCase), term);
        }
    }

    [TestMethod]
    public void WorkspaceContextPacketExportPreview_SourceHasNoFilesystemClipboardDownloadDatabaseProviderVectorRuntimeOrServiceImplementation()
    {
        var source = ReadRepoText(FoundationPath);
        var forbidden = new[]
        {
            "File.Read",
            "File.Write",
            "FileStream",
            "Directory.",
            "Directory.CreateDirectory",
            "Path.",
            "Microsoft.Data.Sqlite",
            "SQLiteConnection",
            "SqlConnection",
            "DbContext",
            "IDbConnection",
            "HttpClient",
            "WebSocket",
            "Process.Start",
            "ServiceCollection",
            "AddSingleton",
            "AddScoped",
            "AddTransient",
            "OpenAI",
            "EmbeddingClient",
            "VectorStore",
            "KernelMemory",
            "System.Windows.Clipboard",
            "navigator.clipboard",
            "DownloadFile"
        };

        foreach (var term in forbidden)
        {
            Assert.IsFalse(source.Contains(term, StringComparison.OrdinalIgnoreCase), term);
        }
    }

    [TestMethod]
    public void WorkspaceContextPacketExportPreview_AllSectionsPreserveNoSideEffectProofAndNoExport()
    {
        var preview = WorkspaceContextPacketExportReadOnlyPresenter.CreateFixture();

        Assert.IsTrue(preview.NoSideEffectProof.Passes);
        Assert.IsFalse(preview.Manifest.PhysicalFileCreated);
        Assert.IsFalse(preview.Manifest.ClipboardUsed);
        Assert.IsFalse(preview.Manifest.DownloadStarted);
        Assert.AreEqual(0, preview.Manifest.ProductActionsCount);
        Assert.AreEqual(0, preview.Manifest.ExportActionsCount);
        Assert.IsFalse(preview.Manifest.ContainsRawPayload);
        Assert.IsFalse(preview.Manifest.ContainsSecretLikeContent);
        Assert.IsFalse(preview.Manifest.ContainsDurableMemory);
        Assert.IsFalse(preview.HasRealExport);
        Assert.IsFalse(preview.HasProductActions);
        Assert.IsFalse(preview.HasExportActions);
        Assert.IsFalse(preview.HasDurableMemory);

        foreach (var section in preview.Sections)
        {
            var proof = section.NoSideEffectProof;

            Assert.IsTrue(proof.Passes, section.SectionId);
            Assert.IsTrue(section.IncludedInPreview, section.SectionId);
            Assert.IsFalse(section.PhysicalExportOccurred, section.SectionId);
            Assert.IsFalse(proof.WorkspaceFilesystemReadAttempted, section.SectionId);
            Assert.IsFalse(proof.FilesystemWriteAttempted, section.SectionId);
            Assert.IsFalse(proof.DatabaseTouched, section.SectionId);
            Assert.IsFalse(proof.DurablePersistenceActive, section.SectionId);
            Assert.IsFalse(proof.DurableMemoryActive, section.SectionId);
            Assert.IsFalse(proof.VectorSemanticBackendTouched, section.SectionId);
            Assert.IsFalse(proof.LlmProviderTouched, section.SectionId);
            Assert.IsFalse(proof.ProviderCloudTouched, section.SectionId);
            Assert.IsFalse(proof.MigrationRunnerStarted, section.SectionId);
            Assert.IsFalse(proof.MigrationExecuted, section.SectionId);
            Assert.IsFalse(proof.RuntimeTouched, section.SectionId);
            Assert.IsFalse(proof.BrowserCdpTouched, section.SectionId);
            Assert.IsFalse(proof.WcuTouched, section.SectionId);
            Assert.IsFalse(proof.OcrTouched, section.SectionId);
            Assert.IsFalse(proof.ProductActionExposed, section.SectionId);
            Assert.IsFalse(proof.ProductServiceRegistered, section.SectionId);
        }
    }

    [TestMethod]
    public void WorkspaceContextPacketExportPreview_DisablesFileClipboardDownloadProviderSemanticRuntimeAndMemory()
    {
        var preview = WorkspaceContextPacketExportReadOnlyPresenter.CreateFixture();
        var proof = preview.NoSideEffectProof;
        var text = string.Join("\n", preview.DisabledNotices);

        Assert.IsFalse(proof.WorkspaceFilesystemReadAttempted);
        Assert.IsFalse(proof.FilesystemWriteAttempted);
        Assert.IsFalse(proof.DatabaseTouched);
        Assert.IsFalse(proof.DurablePersistenceActive);
        Assert.IsFalse(proof.DurableMemoryActive);
        Assert.IsFalse(proof.VectorSemanticBackendTouched);
        Assert.IsFalse(proof.LlmProviderTouched);
        Assert.IsFalse(proof.ProviderCloudTouched);
        Assert.IsFalse(proof.MigrationRunnerStarted);
        Assert.IsFalse(proof.MigrationExecuted);
        Assert.IsFalse(proof.RuntimeTouched);
        Assert.IsFalse(proof.BrowserCdpTouched);
        Assert.IsFalse(proof.WcuTouched);
        Assert.IsFalse(proof.OcrTouched);
        Assert.IsTrue(text.Contains("Physical export disabled", StringComparison.Ordinal));
        Assert.IsTrue(text.Contains("Clipboard disabled", StringComparison.Ordinal));
        Assert.IsTrue(text.Contains("Browser download disabled", StringComparison.Ordinal));
        Assert.IsTrue(text.Contains("Provider/cloud", StringComparison.Ordinal));
        Assert.IsTrue(text.Contains("Semantic/vector", StringComparison.Ordinal));
        Assert.IsTrue(text.Contains("Durable memory", StringComparison.Ordinal));
        Assert.IsTrue(text.Contains("Runtime/live", StringComparison.Ordinal));
    }

    [TestMethod]
    public void WorkspaceContextPacketExportPreview_HasNoExportContextMemoryOrProductionOverclaim()
    {
        var preview = WorkspaceContextPacketExportReadOnlyPresenter.CreateFixture();
        var text = string.Join(
            "\n",
            preview.PreviewText,
            string.Join("\n", preview.DisabledNotices),
            string.Join("\n", preview.Warnings),
            string.Join("\n", preview.Blockers),
            string.Join("\n", preview.Exclusions),
            string.Join("\n", preview.Sections.Select(section => $"{section.SectionId} {section.Title} {string.Join(" ", section.Warnings)} {string.Join(" ", section.Blockers)}")));

        var forbidden = new[]
        {
            "production" + "-ready",
            "candidate promoted",
            "candidate persisted",
            "durable memory enabled",
            "durable memory active",
            "memory persisted",
            "risk is decision",
            "semantic search enabled",
            "vector backend enabled",
            "provider call enabled",
            "workspace scan completed",
            "runtime action enabled",
            "live automation enabled",
            "filesystem indexed",
            "action command exposed",
            "product action exposed",
            "export file created",
            "export written",
            "clipboard write completed",
            "download started"
        };

        foreach (var term in forbidden)
        {
            Assert.IsFalse(text.Contains(term, StringComparison.OrdinalIgnoreCase), term);
        }
    }

    private static void AssertBlocked(
        IReadOnlyList<WorkspaceContextAuthorityFreshnessResult> results,
        string fixtureId,
        WorkspaceContextAuthorityFreshnessIssueKind issueKind)
    {
        var result = results.Single(item => item.FixtureId == fixtureId);

        Assert.IsTrue(result.Blocked, fixtureId);
        Assert.IsTrue(result.HasIssue(issueKind), fixtureId);
        Assert.IsFalse(result.AllowsDecisionUse, fixtureId);
        Assert.IsFalse(result.AllowsSafeNextStepUse, fixtureId);
    }

    private static void AssertSelectionBlocked(
        IReadOnlyList<WorkspaceContextSelectionLockExclusionResult> results,
        string fixtureId,
        WorkspaceContextSelectionLockExclusionIssueKind issueKind)
    {
        var result = results.Single(item => item.FixtureId == fixtureId);

        Assert.IsTrue(result.Blocked, fixtureId);
        Assert.IsTrue(result.HasIssue(issueKind), fixtureId);
        Assert.IsFalse(result.AllowsDecisionUse, fixtureId);
        Assert.IsFalse(result.AllowsSafeNextStepUse, fixtureId);
        Assert.IsFalse(result.AllowsMemoryInfluence, fixtureId);
    }

    private static void AssertMemoryBlocked(
        IReadOnlyList<WorkspaceMemoryCandidateContradictionRiskResult> results,
        string fixtureId,
        WorkspaceMemoryCandidateContradictionRiskIssueKind issueKind)
    {
        var result = results.Single(item => item.FixtureId == fixtureId);

        Assert.IsTrue(result.Blocked, fixtureId);
        Assert.IsTrue(result.HasIssue(issueKind), fixtureId);
        Assert.IsFalse(result.DurableMemoryEnabled, fixtureId);
        Assert.IsFalse(result.AllowsDecisionUse, fixtureId);
        Assert.IsFalse(result.AllowsSafeNextStepUse, fixtureId);
        Assert.IsFalse(result.AllowsCandidateInfluence, fixtureId);
    }

    private static string ReadRepoText(string relativePath)
    {
        var current = AppContext.BaseDirectory;
        while (!string.IsNullOrWhiteSpace(current))
        {
            var candidate = System.IO.Path.Combine(current, relativePath.Replace('/', System.IO.Path.DirectorySeparatorChar));
            if (System.IO.File.Exists(candidate))
                return System.IO.File.ReadAllText(candidate);

            current = System.IO.Directory.GetParent(current)?.FullName;
        }

        Assert.Fail($"Could not locate {relativePath}");
        return "";
    }
}
