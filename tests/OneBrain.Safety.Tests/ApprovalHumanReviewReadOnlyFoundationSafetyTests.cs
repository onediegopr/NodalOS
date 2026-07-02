using Microsoft.VisualStudio.TestTools.UnitTesting;
using OneBrain.Core.Approval;

namespace OneBrain.Safety.Tests;

[TestClass]
[TestCategory("ApprovalHumanReview")]
[TestCategory("PhaseEApprovalHumanReview")]
public sealed class ApprovalHumanReviewReadOnlyFoundationSafetyTests
{
    private const string FoundationPath = "src/OneBrain.Core/Approval/ApprovalHumanReviewReadOnlyFoundation.cs";
    private const string RiskDecisionGuardPath = "src/OneBrain.Core/Approval/ApprovalRiskDecisionReadOnlyGuard.cs";
    private const string EvidenceContextLinkGuardPath = "src/OneBrain.Core/Approval/HumanReviewEvidenceContextLinkReadOnlyGuard.cs";
    private const string ApprovalPacketSurfacePath = "src/OneBrain.Core/Approval/ApprovalPacketReadOnlySurface.cs";
    private const string HumanReviewPacketExportPreviewPath = "src/OneBrain.Core/Approval/HumanReviewPacketExportReadOnlyPreview.cs";
    private const string ApprovalExecutionDesignOnlyProtectedPath = "src/OneBrain.Core/Approval/ApprovalExecutionDesignOnlyProtected.cs";

    [TestMethod]
    public void FoundationSource_HasNoFilesystemDatabaseProviderVectorRuntimeOrServiceImplementation()
    {
        var source = string.Join("\n", ReadOnlyPhaseEPaths().Select(ReadRepoText));
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
    public void FoundationDoesNotReuseExistingApprovalExecutionOrArtifactWriter()
    {
        var source = string.Join("\n", ReadOnlyPhaseEPaths().Select(ReadRepoText));
        var forbidden = new[]
        {
            "ApprovalArtifactWriter",
            "ApprovalPolicy",
            "ApprovalBindingValidator",
            "WriteArtifact",
            "WriteApproval",
            "ApprovedInputBinding",
            "ApprovedIdentityInput",
            "BusinessFlowDemoFixture",
            "Pilot",
            "AgentOperations"
        };

        foreach (var term in forbidden)
        {
            Assert.IsFalse(source.Contains(term, StringComparison.Ordinal), term);
        }

        StringAssert.Contains(source, "EvidenceIntelligenceAuditDashboardReadOnlyPresenter.CreateFixture");
        StringAssert.Contains(source, "WorkspaceContextPacketExportReadOnlyPresenter.CreateFixture");
    }

    [TestMethod]
    public void FoundationProof_DisablesAllSideEffectsRuntimeAndApprovalMutation()
    {
        var packet = ApprovalHumanReviewReadOnlyPresenter.CreateFixture();
        var proof = packet.NoSideEffectProof;

        Assert.IsTrue(proof.Passes);
        Assert.IsFalse(proof.FilesystemReadAttempted);
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
        Assert.IsFalse(proof.RecipeExecutionStarted);
        Assert.IsFalse(proof.ApprovalExecutionStarted);
        Assert.IsFalse(proof.ApprovalStateMutationAttempted);
        Assert.IsFalse(proof.ProductActionExposed);
        Assert.IsFalse(proof.ProductServiceRegistered);
    }

    [TestMethod]
    public void RiskDecisionGuardProof_DisablesAllSideEffectsRuntimeApprovalMutationAndProductActions()
    {
        foreach (var result in ApprovalRiskDecisionReadOnlyGuard.EvaluateCatalog())
        {
            var proof = result.NoSideEffectProof;

            Assert.IsTrue(proof.Passes, result.FixtureId);
            Assert.IsTrue(result.PreviewOnly, result.FixtureId);
            Assert.IsFalse(result.ApprovalExecutionAllowed, result.FixtureId);
            Assert.IsFalse(result.StateMutationAllowed, result.FixtureId);
            Assert.IsFalse(result.ProductActionAllowed, result.FixtureId);
            Assert.IsFalse(proof.FilesystemReadAttempted, result.FixtureId);
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
            Assert.IsFalse(proof.RecipeExecutionStarted, result.FixtureId);
            Assert.IsFalse(proof.ApprovalExecutionStarted, result.FixtureId);
            Assert.IsFalse(proof.ApprovalStateMutationAttempted, result.FixtureId);
            Assert.IsFalse(proof.ProductActionExposed, result.FixtureId);
            Assert.IsFalse(proof.ProductServiceRegistered, result.FixtureId);
        }
    }

    [TestMethod]
    public void EvidenceContextLinkGuardProof_DisablesAllSideEffectsRuntimeApprovalMutationAndProductActions()
    {
        foreach (var result in HumanReviewEvidenceContextLinkReadOnlyGuard.EvaluateCatalog())
        {
            var proof = result.NoSideEffectProof;

            Assert.IsTrue(proof.Passes, result.FixtureId);
            Assert.IsTrue(result.PreviewOnly, result.FixtureId);
            Assert.IsFalse(result.EvidenceLinkIsDurableEvidence, result.FixtureId);
            Assert.IsFalse(result.ContextLinkTrustedByDefault, result.FixtureId);
            Assert.IsFalse(result.ApprovalExecutionAllowed, result.FixtureId);
            Assert.IsFalse(result.StateMutationAllowed, result.FixtureId);
            Assert.IsFalse(result.ProductActionAllowed, result.FixtureId);
            Assert.IsFalse(result.ServiceRegistrationAllowed, result.FixtureId);
            Assert.IsFalse(proof.FilesystemReadAttempted, result.FixtureId);
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
            Assert.IsFalse(proof.RecipeExecutionStarted, result.FixtureId);
            Assert.IsFalse(proof.ApprovalExecutionStarted, result.FixtureId);
            Assert.IsFalse(proof.ApprovalStateMutationAttempted, result.FixtureId);
            Assert.IsFalse(proof.ProductActionExposed, result.FixtureId);
            Assert.IsFalse(proof.ProductServiceRegistered, result.FixtureId);
        }
    }

    [TestMethod]
    public void ApprovalPacketSurfaceProof_DisablesAllSideEffectsRuntimeApprovalMutationProductAndExportActions()
    {
        var surface = ApprovalPacketReadOnlySurfacePresenter.CreateFixture();
        var proof = surface.NoSideEffectProof;

        Assert.IsTrue(proof.Passes);
        Assert.IsTrue(surface.ReadOnly);
        Assert.IsTrue(surface.Deterministic);
        Assert.IsTrue(surface.FixtureSafe);
        Assert.AreEqual(0, surface.ProductActionsCount);
        Assert.AreEqual(0, surface.StateMutationsCount);
        Assert.AreEqual(0, surface.ExportActionsCount);
        Assert.IsFalse(surface.HasApprovalExecution);
        Assert.IsFalse(surface.HasApprovalStateMutation);
        Assert.IsFalse(surface.HasProductActions);
        Assert.IsFalse(surface.HasExportActions);
        Assert.IsFalse(surface.HasDurableMemory);
        Assert.IsTrue(surface.Sections.All(section => section.NoSideEffectProof.Passes));
        Assert.IsTrue(surface.Sections.All(section => section.ProductActionsCount == 0));
        Assert.IsTrue(surface.Sections.All(section => section.StateMutationsCount == 0));
        Assert.IsTrue(surface.Sections.All(section => section.ExportActionsCount == 0));
        Assert.IsFalse(proof.FilesystemReadAttempted);
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
        Assert.IsFalse(proof.RecipeExecutionStarted);
        Assert.IsFalse(proof.ApprovalExecutionStarted);
        Assert.IsFalse(proof.ApprovalStateMutationAttempted);
        Assert.IsFalse(proof.ProductActionExposed);
        Assert.IsFalse(proof.ProductServiceRegistered);
    }

    [TestMethod]
    public void HumanReviewPacketExportPreviewProof_DisablesAllSideEffectsRuntimeApprovalMutationProductAndExportActions()
    {
        var preview = HumanReviewPacketExportReadOnlyPresenter.CreateFixture();
        var proof = preview.NoSideEffectProof;

        Assert.IsTrue(proof.Passes);
        Assert.IsTrue(preview.ReadOnly);
        Assert.IsTrue(preview.Deterministic);
        Assert.IsTrue(preview.FixtureSafe);
        Assert.IsFalse(preview.Manifest.PhysicalFileCreated);
        Assert.IsFalse(preview.Manifest.ClipboardUsed);
        Assert.IsFalse(preview.Manifest.DownloadStarted);
        Assert.AreEqual(0, preview.Manifest.ProductActionsCount);
        Assert.AreEqual(0, preview.Manifest.StateMutationCount);
        Assert.AreEqual(0, preview.Manifest.ExportActionsCount);
        Assert.IsFalse(preview.Manifest.ApprovalExecutionOccurred);
        Assert.IsFalse(preview.Manifest.ApprovalStateMutationOccurred);
        Assert.IsFalse(preview.Manifest.ContainsRawPayload);
        Assert.IsFalse(preview.Manifest.ContainsSecretLikeContent);
        Assert.IsFalse(preview.Manifest.ContainsDurableMemory);
        Assert.IsFalse(preview.HasRealExport);
        Assert.IsFalse(preview.HasApprovalExecution);
        Assert.IsFalse(preview.HasApprovalStateMutation);
        Assert.IsFalse(preview.HasProductActions);
        Assert.IsFalse(preview.HasStateMutations);
        Assert.IsFalse(preview.HasExportActions);
        Assert.IsFalse(preview.HasDurableMemory);
        Assert.IsTrue(preview.Sections.All(section => section.NoSideEffectProof.Passes));
        Assert.IsTrue(preview.Sections.All(section => section.PhysicalExportOccurred == false));
        Assert.IsFalse(proof.FilesystemReadAttempted);
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
        Assert.IsFalse(proof.RecipeExecutionStarted);
        Assert.IsFalse(proof.ApprovalExecutionStarted);
        Assert.IsFalse(proof.ApprovalStateMutationAttempted);
        Assert.IsFalse(proof.ProductActionExposed);
        Assert.IsFalse(proof.ProductServiceRegistered);
    }

    [TestMethod]
    public void ApprovalExecutionDesignOnlyProtectedProof_DisablesExecutionMutationRuntimeExportAndServiceRegistration()
    {
        var spec = ApprovalExecutionDesignOnlyProtectedPresenter.CreateFixture();
        var proof = spec.AntiCapabilityProof;

        Assert.IsTrue(proof.Passes);
        Assert.IsTrue(spec.ReadOnly);
        Assert.IsTrue(spec.DesignOnly);
        Assert.IsTrue(spec.PreviewOnly);
        Assert.AreEqual(0, spec.Readiness.ApprovalExecutionReadinessPercent);
        Assert.AreEqual(0, spec.Readiness.ApprovalStateMutationReadinessPercent);
        Assert.AreEqual(0, spec.Readiness.RuntimeLiveReadinessPercent);
        Assert.AreEqual(0, spec.Readiness.PhysicalExportReadinessPercent);
        Assert.IsTrue(spec.Readiness.BlocksRealExecution);
        Assert.IsFalse(spec.HasRealExecution);
        Assert.IsFalse(spec.HasStateMutation);
        Assert.IsFalse(spec.HasRuntimeLive);
        Assert.IsFalse(spec.HasPhysicalExport);
        Assert.IsFalse(spec.HasProductActions);
        Assert.IsTrue(spec.Gates.All(gate => gate.Status == ApprovalExecutionDesignStatus.Blocked));
        Assert.IsTrue(spec.Gates.All(gate => !gate.AllowsRealExecution));
        Assert.IsTrue(spec.Gates.All(gate => !gate.AllowsStateMutation));
        Assert.IsTrue(spec.Gates.All(gate => !gate.AllowsRuntimeLive));
        Assert.IsTrue(spec.Gates.All(gate => !gate.AllowsPhysicalExport));
        Assert.IsTrue(spec.Gates.All(gate => !gate.AllowsProductAction));
        Assert.IsTrue(spec.Previews.All(preview => preview.PreviewOnly));
        Assert.IsTrue(spec.Previews.All(preview => !preview.ExecutesApproval));
        Assert.IsTrue(spec.Previews.All(preview => !preview.MutatesState));
        Assert.IsTrue(spec.Previews.All(preview => !preview.ExposesProductAction));
        Assert.IsTrue(spec.Previews.All(preview => !preview.StartsRuntime));
        Assert.IsTrue(spec.Previews.All(preview => !preview.CreatesPhysicalExport));
        Assert.IsFalse(proof.NoSideEffectProof.ApprovalExecutionStarted);
        Assert.IsFalse(proof.NoSideEffectProof.ApprovalStateMutationAttempted);
        Assert.IsFalse(proof.NoSideEffectProof.RuntimeTouched);
        Assert.IsFalse(proof.NoSideEffectProof.ProductActionExposed);
        Assert.IsFalse(proof.NoSideEffectProof.ProductServiceRegistered);
    }

    [TestMethod]
    public void ReadOnlyPresenterAndRiskDecisionGuard_DoNotCreateApprovalArtifactFiles()
    {
        var root = FindRepoRoot();
        var approvalArtifacts = System.IO.Path.Combine(root, "artifacts", "approvals");
        var before = SnapshotFiles(approvalArtifacts);

        _ = ApprovalHumanReviewReadOnlyPresenter.CreateFixture();
        _ = ApprovalRiskDecisionReadOnlyGuard.EvaluateCatalog();
        _ = HumanReviewEvidenceContextLinkReadOnlyGuard.EvaluateCatalog();
        _ = ApprovalPacketReadOnlySurfacePresenter.CreateFixture();
        _ = HumanReviewPacketExportReadOnlyPresenter.CreateFixture();

        var after = SnapshotFiles(approvalArtifacts);

        CollectionAssert.AreEqual(before, after);
    }

    [TestMethod]
    public void CandidateActionsAndDecisionOptions_DoNotExecuteOrMutateState()
    {
        var packet = ApprovalHumanReviewReadOnlyPresenter.CreateFixture();

        Assert.AreEqual(0, packet.ProductActionCount);
        Assert.AreEqual(0, packet.StateMutationCount);
        Assert.IsFalse(packet.HasApprovalExecution);
        Assert.IsFalse(packet.HasApprovalStateMutation);
        Assert.IsTrue(packet.CandidateActions.All(action => action.DecisionAllowedOnlyAsPreview));
        Assert.IsTrue(packet.CandidateActions.All(action => action.ProductActionCount == 0));
        Assert.IsTrue(packet.CandidateActions.All(action => action.StateMutationCount == 0));
        Assert.IsTrue(packet.DecisionOptions.All(option => option.PreviewOnly));
        Assert.IsTrue(packet.DecisionOptions.All(option => !option.ExecutesAction));
        Assert.IsTrue(packet.DecisionOptions.All(option => !option.MutatesState));
    }

    [TestMethod]
    public void PacketKeepsEvidenceAndContextLinksFixtureOnlyWithNoSideEffects()
    {
        var packet = ApprovalHumanReviewReadOnlyPresenter.CreateFixture();

        Assert.IsTrue(packet.EvidenceLinks.All(link => link.FixtureOnly));
        Assert.IsTrue(packet.EvidenceLinks.All(link => link.PayloadValuesExcluded));
        Assert.IsTrue(packet.EvidenceLinks.All(link => link.NoSideEffectProof.Passes));
        Assert.IsTrue(packet.ContextLinks.All(link => link.FixtureOnly));
        Assert.IsTrue(packet.ContextLinks.All(link => link.NoSideEffectProof.Passes));
        Assert.IsTrue(packet.RiskSummaries.All(risk => risk.NoSideEffectProof.Passes));
        Assert.IsTrue(packet.CandidateActions.All(action => action.NoSideEffectProof.Passes));
        Assert.IsTrue(packet.DecisionOptions.All(option => option.NoSideEffectProof.Passes));
    }

    [TestMethod]
    public void MissingEvidenceContextStaleContradictionAndCriticalRiskStayBlocked()
    {
        var packet = ApprovalHumanReviewReadOnlyPresenter.CreateFixture();
        var blockedItems = packet.Items.Where(item => item.Status == ApprovalHumanReviewItemStatus.Blocked).ToList();

        Assert.IsTrue(blockedItems.Any(item => item.Kind == ApprovalHumanReviewItemKind.MissingEvidenceBlocker));
        Assert.IsTrue(blockedItems.Any(item => item.Kind == ApprovalHumanReviewItemKind.MissingContextBlocker));
        Assert.IsTrue(blockedItems.Any(item => item.Kind == ApprovalHumanReviewItemKind.StaleContextBlocker));
        Assert.IsTrue(blockedItems.Any(item => item.Kind == ApprovalHumanReviewItemKind.UnresolvedContradictionBlocker));
        Assert.IsTrue(blockedItems.Any(item => item.Kind == ApprovalHumanReviewItemKind.CriticalRiskBlocker));
        Assert.IsTrue(packet.RiskSummaries.Where(risk => risk.RiskLevel == ApprovalRiskLevel.Critical).All(risk => risk.BlocksDecision));
        Assert.IsTrue(packet.RiskSummaries.Where(risk => risk.RiskLevel == ApprovalRiskLevel.Critical).All(risk => risk.BlocksSafeNextStep));
    }

    [TestMethod]
    public void FoundationText_HasNoApprovalHumanReviewOrProductionOverclaim()
    {
        var packet = ApprovalHumanReviewReadOnlyPresenter.CreateFixture();
        var guardText = string.Join(
            "\n",
            ApprovalRiskDecisionReadOnlyGuard.EvaluateCatalog()
                .Select(result => $"{result.FixtureId} {result.Decision} {string.Join(" ", result.Warnings)} {string.Join(" ", result.Blockers)}"));
        var linkGuardText = string.Join(
            "\n",
            HumanReviewEvidenceContextLinkReadOnlyGuard.EvaluateCatalog()
                .Select(result => $"{result.FixtureId} {result.Decision} {string.Join(" ", result.Warnings)} {string.Join(" ", result.Blockers)}"));
        var surface = ApprovalPacketReadOnlySurfacePresenter.CreateFixture();
        var preview = HumanReviewPacketExportReadOnlyPresenter.CreateFixture();
        var text = string.Join(
            "\n",
            packet.ReadOnlySummary,
            packet.Summary,
            string.Join("\n", packet.Items.Select(item => $"{item.ItemId} {item.Title} {item.Summary} {string.Join(" ", item.Warnings)} {string.Join(" ", item.Blockers)}")),
            string.Join("\n", packet.CandidateActions.Select(action => $"{action.Title} {action.Summary}")),
            string.Join("\n", packet.DecisionOptions.Select(option => $"{option.Label} {option.Summary}")),
            guardText,
            linkGuardText,
            surface.ReadOnlySummary,
            string.Join("\n", surface.Sections.Select(section => $"{section.SectionId} {section.Title} {string.Join(" ", section.Warnings)} {string.Join(" ", section.Blockers)}")),
            preview.PreviewText,
            string.Join("\n", preview.Sections.Select(section => $"{section.SectionId} {section.Title} {string.Join(" ", section.Warnings)} {string.Join(" ", section.Blockers)}")));

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
            "browser cdp live",
            "wcu live",
            "ocr live",
            "provider call enabled",
            "semantic search enabled",
            "vector backend enabled",
            "durable memory active",
            "memory persisted",
            "workspace scan completed",
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
        var packet = ApprovalHumanReviewReadOnlyPresenter.CreateFixture();
        var text = string.Join(
            "\n",
            packet.ReadOnlySummary,
            packet.Summary,
            string.Join("\n", packet.Items.Select(item => item.Summary)),
            string.Join("\n", packet.CandidateActions.Select(action => action.Summary)),
            string.Join("\n", packet.DecisionOptions.Select(option => option.Summary)),
            string.Join("\n", ApprovalRiskDecisionReadOnlyGuard.EvaluateCatalog().Select(result => $"{string.Join(" ", result.Warnings)} {string.Join(" ", result.Blockers)}")),
            string.Join("\n", HumanReviewEvidenceContextLinkReadOnlyGuard.EvaluateCatalog().Select(result => $"{string.Join(" ", result.Warnings)} {string.Join(" ", result.Blockers)}")),
            string.Join("\n", ApprovalPacketReadOnlySurfacePresenter.CreateFixture().Sections.Select(section => $"{string.Join(" ", section.Warnings)} {string.Join(" ", section.Blockers)}")),
            HumanReviewPacketExportReadOnlyPresenter.CreateFixture().PreviewText);

        Assert.IsFalse(text.Contains("sk-", StringComparison.OrdinalIgnoreCase));
        Assert.IsFalse(text.Contains("Bearer ", StringComparison.OrdinalIgnoreCase));
        Assert.IsFalse(text.Contains("PRIVATE KEY", StringComparison.OrdinalIgnoreCase));
        Assert.IsFalse(text.Contains("AKIA", StringComparison.OrdinalIgnoreCase));
        Assert.IsFalse(text.Contains("ghp_", StringComparison.OrdinalIgnoreCase));
    }

    private static IReadOnlyList<string> ReadOnlyPhaseEPaths() =>
    [
        FoundationPath,
        RiskDecisionGuardPath,
        EvidenceContextLinkGuardPath,
        ApprovalPacketSurfacePath,
        HumanReviewPacketExportPreviewPath,
        ApprovalExecutionDesignOnlyProtectedPath
    ];

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

    private static string FindRepoRoot()
    {
        var current = AppContext.BaseDirectory;
        while (!string.IsNullOrWhiteSpace(current))
        {
            if (System.IO.File.Exists(System.IO.Path.Combine(current, "OneBrain.slnx")))
                return current;

            current = System.IO.Directory.GetParent(current)?.FullName;
        }

        Assert.Fail("Could not locate repo root.");
        return "";
    }

    private static string[] SnapshotFiles(string directory)
    {
        if (!System.IO.Directory.Exists(directory))
            return [];

        return System.IO.Directory.GetFiles(directory, "*", System.IO.SearchOption.AllDirectories)
            .Select(path => System.IO.Path.GetRelativePath(directory, path))
            .OrderBy(path => path, StringComparer.Ordinal)
            .ToArray();
    }
}
