using Microsoft.VisualStudio.TestTools.UnitTesting;
using OneBrain.Core.Approval;

namespace OneBrain.Safety.Tests;

[TestClass]
[TestCategory("ApprovalHumanReview")]
[TestCategory("PhaseEApprovalHumanReview")]
public sealed class ApprovalHumanReviewReadOnlyFoundationSafetyTests
{
    private const string FoundationPath = "src/OneBrain.Core/Approval/ApprovalHumanReviewReadOnlyFoundation.cs";

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
        var source = ReadRepoText(FoundationPath);
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
        var text = string.Join(
            "\n",
            packet.ReadOnlySummary,
            packet.Summary,
            string.Join("\n", packet.Items.Select(item => $"{item.ItemId} {item.Title} {item.Summary} {string.Join(" ", item.Warnings)} {string.Join(" ", item.Blockers)}")),
            string.Join("\n", packet.CandidateActions.Select(action => $"{action.Title} {action.Summary}")),
            string.Join("\n", packet.DecisionOptions.Select(option => $"{option.Label} {option.Summary}")));

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
            string.Join("\n", packet.DecisionOptions.Select(option => option.Summary)));

        Assert.IsFalse(text.Contains("sk-", StringComparison.OrdinalIgnoreCase));
        Assert.IsFalse(text.Contains("Bearer ", StringComparison.OrdinalIgnoreCase));
        Assert.IsFalse(text.Contains("PRIVATE KEY", StringComparison.OrdinalIgnoreCase));
        Assert.IsFalse(text.Contains("AKIA", StringComparison.OrdinalIgnoreCase));
        Assert.IsFalse(text.Contains("ghp_", StringComparison.OrdinalIgnoreCase));
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
