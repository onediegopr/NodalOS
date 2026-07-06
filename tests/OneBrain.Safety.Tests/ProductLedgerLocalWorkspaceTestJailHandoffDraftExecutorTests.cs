using Microsoft.VisualStudio.TestTools.UnitTesting;
using OneBrain.Core.Approval;

namespace OneBrain.Safety.Tests;

[TestClass]
[TestCategory("ApprovalHumanReview")]
[TestCategory("PhaseEApprovalHumanReview")]
public sealed class ProductLedgerLocalWorkspaceTestJailHandoffDraftExecutorTests
{
    [TestMethod]
    public void WorkspaceTestJailHandoffDraftExecutor_CreatesOneRedactedDraftUnderJailBoundary()
    {
        using var fixture = WorkspaceTestJailFixture.Create();

        var result = fixture.Executor.CreateDraft(ReadyRequest());
        var fullPath = fixture.FullPath(result);
        var content = File.ReadAllText(fullPath);

        Assert.AreEqual(ProductLedgerLocalWorkspaceTestJailHandoffDraftDecision.DraftCreatedWorkspaceTestJailOnly, result.Decision);
        Assert.AreEqual(ProductLedgerLocalWorkspaceTestJailHandoffDraftState.DraftCreatedWorkspaceTestJailOnly, result.State);
        Assert.AreEqual(0, result.Blockers.Count);
        Assert.IsTrue(File.Exists(fullPath));
        Assert.IsTrue(Path.GetFullPath(fullPath).StartsWith(Path.GetFullPath(fixture.JailRoot), StringComparison.OrdinalIgnoreCase));
        StringAssert.StartsWith(result.DraftRelativePath, ProductLedgerLocalWorkspaceTestJailHandoffDraftExecutor.AllowedRelativeOutputBoundary);
        StringAssert.Contains(result.DraftRelativePath, "workspace-test-jail-handoff-draft-workspace-draft-action-001-aaaaaaaaaaaa.md");
        Assert.IsTrue(result.CreateOnly);
        Assert.IsFalse(result.OverwriteAllowed);
        Assert.IsFalse(result.UserSelectedPathAllowed);
        Assert.IsFalse(result.PayloadControlledRootAllowed);
        Assert.IsTrue(result.CanonicalizationPassed);
        Assert.IsTrue(result.ReparseValidationPassed);
        Assert.IsTrue(result.WorkspaceTestJailOnly);
        Assert.IsTrue(result.RedactionApplied);
        Assert.IsFalse(result.ShellAllowed);
        Assert.IsFalse(result.NetworkAllowed);
        Assert.IsFalse(result.ProductionAllowed);
        Assert.IsFalse(result.PublicProductAllowed);
        Assert.IsFalse(result.ProductCommandExecuted);
        Assert.IsFalse(result.PublicUiActionAvailable);
        Assert.IsFalse(result.ProviderCloudNetworkAvailable);
        Assert.IsFalse(result.ReleaseCommercialReady);
        StringAssert.Contains(content, "Local Workspace Test-Jail Handoff Draft");
        StringAssert.Contains(content, "workspace test-jail only");
        StringAssert.Contains(content, "redacted workspace test-jail handoff summary");
        StringAssert.Contains(content, "No user-selected path.");
        StringAssert.Contains(content, "No shell/subprocess.");
        StringAssert.Contains(content, "No command execution.");
        StringAssert.Contains(content, "No cloud/network/DB.");
        Assert.IsFalse(content.Contains("password=", StringComparison.OrdinalIgnoreCase));
        Assert.IsFalse(content.Contains(@"C:\Users\", StringComparison.OrdinalIgnoreCase));
    }

    [TestMethod]
    public void WorkspaceTestJailHandoffDraftExecutor_BlocksMissingOrInvalidApprovedChain()
    {
        var cases = new (ProductLedgerLocalWorkspaceTestJailHandoffDraftRequest? Request, ProductLedgerLocalWorkspaceTestJailHandoffDraftBlocker Blocker)[]
        {
            (null, ProductLedgerLocalWorkspaceTestJailHandoffDraftBlocker.MissingRequest),
            (ReadyRequest() with { ApprovalDecision = ProductLedgerLocalApprovalDecisionSnapshot.PendingPreviewOnly }, ProductLedgerLocalWorkspaceTestJailHandoffDraftBlocker.ApprovalDecisionNotApproved),
            (ReadyRequest() with { NoOpExecution = ProductLedgerLocalApprovedActionExecutionSnapshot.Pending }, ProductLedgerLocalWorkspaceTestJailHandoffDraftBlocker.NoOpExecutionNotCompleted),
            (ReadyRequest() with { BoundedExecution = ProductLedgerLocalBoundedApprovedActionSnapshot.Pending }, ProductLedgerLocalWorkspaceTestJailHandoffDraftBlocker.BoundedExecutionNotCompleted),
            (ReadyRequest() with { PredecessorDraft = ProductLedgerLocalApprovedHandoffReportDraftSnapshot.Pending }, ProductLedgerLocalWorkspaceTestJailHandoffDraftBlocker.PredecessorDraftNotCompleted),
            (ReadyRequest() with { PredecessorDraft = null }, ProductLedgerLocalWorkspaceTestJailHandoffDraftBlocker.MissingPredecessorDraft),
            (ReadyRequest() with { ActionKind = null }, ProductLedgerLocalWorkspaceTestJailHandoffDraftBlocker.UnknownActionKind),
            (ReadyRequest() with { CandidateActionKind = ProductLedgerInternalCommandKind.ViewDiagnostics }, ProductLedgerLocalWorkspaceTestJailHandoffDraftBlocker.CandidateActionMismatch),
            (ReadyRequest() with { CurrentEvidenceHash = new string('b', 64) }, ProductLedgerLocalWorkspaceTestJailHandoffDraftBlocker.CandidateEvidenceHashMismatch),
            (ReadyRequest() with { PredecessorDraftContentHash = new string('c', 64) }, ProductLedgerLocalWorkspaceTestJailHandoffDraftBlocker.PredecessorDraftMismatch),
            (ReadyRequest() with { EvidenceReferences = [] }, ProductLedgerLocalWorkspaceTestJailHandoffDraftBlocker.MissingEvidenceReferences)
        };

        foreach (var testCase in cases)
        {
            using var fixture = WorkspaceTestJailFixture.Create();
            var result = fixture.Executor.CreateDraft(testCase.Request);

            Assert.AreEqual(ProductLedgerLocalWorkspaceTestJailHandoffDraftDecision.Rejected, result.Decision, testCase.Blocker.ToString());
            CollectionAssert.Contains(result.Blockers.ToArray(), testCase.Blocker, testCase.Blocker.ToString());
            Assert.IsFalse(File.Exists(fixture.ExpectedReadyPath));
        }
    }

    [TestMethod]
    public void WorkspaceTestJailHandoffDraftExecutor_BlocksPathCommandNetworkOverwriteAndUnsafeContent()
    {
        var ready = ReadyRequest();
        var cases = new Dictionary<ProductLedgerLocalWorkspaceTestJailHandoffDraftRequest, ProductLedgerLocalWorkspaceTestJailHandoffDraftBlocker>
        {
            [ready with { ExplicitWorkspaceTestJailScope = false }] = ProductLedgerLocalWorkspaceTestJailHandoffDraftBlocker.MissingExplicitWorkspaceTestJailScope,
            [ready with { DevelopmentMode = false }] = ProductLedgerLocalWorkspaceTestJailHandoffDraftBlocker.NonDevelopmentMode,
            [ready with { LocalMode = false }] = ProductLedgerLocalWorkspaceTestJailHandoffDraftBlocker.NonLocalMode,
            [ready with { InternalMode = false }] = ProductLedgerLocalWorkspaceTestJailHandoffDraftBlocker.NonInternalMode,
            [ready with { ActionId = "..\\unsafe" }] = ProductLedgerLocalWorkspaceTestJailHandoffDraftBlocker.UnsafeActionId,
            [ready with { ProposedPath = @"C:\Users\fixture\unsafe.txt" }] = ProductLedgerLocalWorkspaceTestJailHandoffDraftBlocker.PayloadPathFieldRejected,
            [ready with { ProposedRoot = @"C:\Users\fixture" }] = ProductLedgerLocalWorkspaceTestJailHandoffDraftBlocker.PayloadPathFieldRejected,
            [ready with { ProposedFilename = "..\\unsafe.md" }] = ProductLedgerLocalWorkspaceTestJailHandoffDraftBlocker.PayloadPathFieldRejected,
            [ready with { ProposedCommand = "cmd /c whoami" }] = ProductLedgerLocalWorkspaceTestJailHandoffDraftBlocker.PayloadCommandFieldRejected,
            [ready with { ProposedUrl = "https://local.invalid/action" }] = ProductLedgerLocalWorkspaceTestJailHandoffDraftBlocker.PayloadNetworkProviderFieldRejected,
            [ready with { ProposedProvider = "provider-live" }] = ProductLedgerLocalWorkspaceTestJailHandoffDraftBlocker.PayloadNetworkProviderFieldRejected,
            [ready with { ProposedDbMigration = "migration" }] = ProductLedgerLocalWorkspaceTestJailHandoffDraftBlocker.PayloadNetworkProviderFieldRejected,
            [ready with { ClaimsArbitraryPathInput = true }] = ProductLedgerLocalWorkspaceTestJailHandoffDraftBlocker.ClaimsArbitraryPathInput,
            [ready with { ClaimsFilesystemScan = true }] = ProductLedgerLocalWorkspaceTestJailHandoffDraftBlocker.ClaimsFilesystemScan,
            [ready with { RequestsOverwrite = true }] = ProductLedgerLocalWorkspaceTestJailHandoffDraftBlocker.RequestsOverwrite,
            [ready with { RequestsUserSelectedPath = true }] = ProductLedgerLocalWorkspaceTestJailHandoffDraftBlocker.RequestsUserSelectedPath,
            [ready with { RequestsShellOrSubprocess = true }] = ProductLedgerLocalWorkspaceTestJailHandoffDraftBlocker.RequestsShellOrSubprocess,
            [ready with { ClaimsArbitraryCommandExecution = true }] = ProductLedgerLocalWorkspaceTestJailHandoffDraftBlocker.ClaimsArbitraryCommandExecution,
            [ready with { ClaimsProviderCloudNetwork = true }] = ProductLedgerLocalWorkspaceTestJailHandoffDraftBlocker.ClaimsProviderCloudNetwork,
            [ready with { ClaimsDbMigration = true }] = ProductLedgerLocalWorkspaceTestJailHandoffDraftBlocker.ClaimsDbMigration,
            [ready with { ClaimsKmsWormExternalTrust = true }] = ProductLedgerLocalWorkspaceTestJailHandoffDraftBlocker.ClaimsKmsWormExternalTrust,
            [ready with { ClaimsBrowserCdpWcuOcrRecipesLive = true }] = ProductLedgerLocalWorkspaceTestJailHandoffDraftBlocker.ClaimsBrowserCdpWcuOcrRecipesLive,
            [ready with { ClaimsPilotRun = true }] = ProductLedgerLocalWorkspaceTestJailHandoffDraftBlocker.ClaimsPilotRun,
            [ready with { ClaimsReleaseCommercial = true }] = ProductLedgerLocalWorkspaceTestJailHandoffDraftBlocker.ClaimsReleaseCommercial,
            [ready with { RedactedDraftSummary = "password=unsafe" }] = ProductLedgerLocalWorkspaceTestJailHandoffDraftBlocker.RedactionRejected,
            [ready with { RedactedDraftSummary = @"C:\Users\fixture\unsafe.txt" }] = ProductLedgerLocalWorkspaceTestJailHandoffDraftBlocker.RedactionRejected
        };

        foreach (var testCase in cases)
        {
            using var fixture = WorkspaceTestJailFixture.Create();
            var result = fixture.Executor.CreateDraft(testCase.Key);

            Assert.AreEqual(ProductLedgerLocalWorkspaceTestJailHandoffDraftDecision.Rejected, result.Decision, testCase.Value.ToString());
            CollectionAssert.Contains(result.Blockers.ToArray(), testCase.Value, testCase.Value.ToString());
            Assert.IsFalse(File.Exists(fixture.ExpectedReadyPath));
        }
    }

    [TestMethod]
    public void WorkspaceTestJailHandoffDraftExecutor_ReplaysExactDraftAndBlocksConflictingExistingFile()
    {
        using var fixture = WorkspaceTestJailFixture.Create();
        var first = fixture.Executor.CreateDraft(ReadyRequest());
        var replay = fixture.Executor.CreateDraft(ReadyRequest());

        Assert.AreEqual(ProductLedgerLocalWorkspaceTestJailHandoffDraftDecision.DraftCreatedWorkspaceTestJailOnly, first.Decision);
        Assert.AreEqual(ProductLedgerLocalWorkspaceTestJailHandoffDraftDecision.IdempotentReplay, replay.Decision);
        Assert.AreEqual(first.ContentHash, replay.ContentHash);

        using var conflictFixture = WorkspaceTestJailFixture.Create();
        Directory.CreateDirectory(Path.GetDirectoryName(conflictFixture.ExpectedReadyPath)!);
        File.WriteAllText(conflictFixture.ExpectedReadyPath, "conflicting safe content");
        var conflict = conflictFixture.Executor.CreateDraft(ReadyRequest());

        Assert.AreEqual(ProductLedgerLocalWorkspaceTestJailHandoffDraftDecision.Rejected, conflict.Decision);
        CollectionAssert.Contains(conflict.Blockers.ToArray(), ProductLedgerLocalWorkspaceTestJailHandoffDraftBlocker.ExistingDraftConflict);
    }

    [TestMethod]
    public void WorkspaceTestJailHandoffDraftExecutor_SourceHasOnlyBoundedCreateOnlyFileWriteAndNoForbiddenRuntime()
    {
        var source = ReadRepoFile("src", "OneBrain.Core", "Approval", "ProductLedgerLocalWorkspaceTestJailHandoffDraftExecutor.cs");
        foreach (var fragment in new[]
        {
            "Process.Start",
            "ShellExecute",
            "System.Diagnostics.Process",
            "HttpClient",
            "WebSocket",
            "DbContext",
            "MigrationBuilder",
            "KmsClient",
            "WormStore",
            "PilotRecipeExecutor",
            "PilotRecipeExecutionGate.Evaluate(",
            "NODAL_OS_ENABLE_PILOT_RECIPE_EXECUTION",
            "ProductLedgerInternalCommandHandler",
            "Directory.GetFiles",
            "Directory.EnumerateFiles",
            "FileMode.OpenOrCreate",
            "FileMode.Create,",
            "OverwriteAllowed: true",
            "UserSelectedPathAllowed: true",
            "PayloadControlledRootAllowed: true",
            "ProductionAllowed: true",
            "PublicProductAllowed: true",
            "ReleaseCommercialReady: true"
        })
        {
            Assert.IsFalse(source.Contains(fragment, StringComparison.Ordinal), fragment);
        }

        StringAssert.Contains(source, "FileMode.CreateNew");
        StringAssert.Contains(source, "FileAttributes.ReparsePoint");
        StringAssert.Contains(source, ProductLedgerLocalWorkspaceTestJailHandoffDraftExecutor.AllowedRelativeOutputBoundary);
        StringAssert.Contains(source, "WORKSPACE_TEST_JAIL_ONLY");
        StringAssert.Contains(source, "NO_OVERWRITE");
        StringAssert.Contains(source, "NO_COMMAND_EXECUTION");
        StringAssert.Contains(source, "NO_SHELL_SUBPROCESS");
    }

    private static ProductLedgerLocalWorkspaceTestJailHandoffDraftRequest ReadyRequest()
    {
        var predecessor = ReadyPredecessor();
        return new(
            ExplicitWorkspaceTestJailScope: true,
            DevelopmentMode: true,
            LocalMode: true,
            InternalMode: true,
            ActionId: "workspace-draft-action-001",
            CandidateId: "candidate-workspace-test-jail-001",
            ActionKind: ProductLedgerLocalWorkspaceTestJailHandoffDraftActionKind.LocalWorkspaceTestJailHandoffDraftCreateOnly,
            ApprovalDecision: ReadyApproval(),
            NoOpExecution: ReadyNoOpExecution(),
            BoundedExecution: ReadyBoundedExecution(),
            PredecessorDraft: predecessor,
            CandidateActionKind: ProductLedgerInternalCommandKind.ViewLedgerReadiness,
            CandidateEvidenceHash: new string('a', 64),
            CurrentEvidenceHash: new string('a', 64),
            PredecessorDraftContentHash: predecessor.ContentHash,
            DraftTitle: "Local Workspace Test-Jail Handoff Draft",
            RedactedDraftSummary: "redacted workspace test-jail handoff summary",
            EvidenceReferences:
            [
                "docs/qa/product-ledger-workspace-test-jail-handoff-draft-implementation/report.md"
            ],
            ProposedPath: null,
            ProposedRoot: null,
            ProposedFilename: null,
            ProposedCommand: null,
            ProposedUrl: null,
            ProposedProvider: null,
            ProposedDbMigration: null,
            ClaimsArbitraryPathInput: false,
            ClaimsFilesystemScan: false,
            RequestsOverwrite: false,
            RequestsUserSelectedPath: false,
            RequestsPublicUiAction: false,
            RequestsProductCommandExecution: false,
            RequestsProductCommandHandler: false,
            RequestsProductiveServiceRegistration: false,
            RequestsShellOrSubprocess: false,
            ClaimsArbitraryCommandExecution: false,
            ClaimsProviderCloudNetwork: false,
            ClaimsDbMigration: false,
            ClaimsKmsWormExternalTrust: false,
            ClaimsBrowserCdpWcuOcrRecipesLive: false,
            ClaimsPilotRun: false,
            ClaimsReleaseCommercial: false);
    }

    private static ProductLedgerLocalApprovalDecisionSnapshot ReadyApproval() =>
        ProductLedgerLocalApprovalDecisionSnapshot.PendingPreviewOnly with
        {
            Decision = ProductLedgerLocalApprovalDecisionStoreDecision.LoadedLocalOnly,
            State = ProductLedgerLocalApprovalDecisionState.ApprovedLocalOnly,
            ApprovalId = "approval-workspace-test-jail-draft-001",
            CandidateActionKind = ProductLedgerInternalCommandKind.ViewLedgerReadiness.ToString(),
            CandidateEvidenceHash = new string('a', 64),
            CandidateEvidenceHashPrefix = new string('a', 12),
            OperatorDecision = ProductLedgerLocalApprovalOperatorDecisionKind.Approve.ToString(),
            EvidenceReferences = ["docs/qa/nodal-os-local-approval-real-operator-input-state-persistence/report.md"],
            StatusText = ProductLedgerLocalApprovalDecisionStateStore.PersistedStatus
        };

    private static ProductLedgerLocalApprovedActionExecutionSnapshot ReadyNoOpExecution() =>
        ProductLedgerLocalApprovedActionExecutionSnapshot.Pending with
        {
            Decision = ProductLedgerLocalApprovedActionExecutionDecision.LoadedLocalOnly,
            State = ProductLedgerLocalApprovedActionExecutionState.NoOpExecutionCompletedLocalOnly,
            ExecutionId = "approved-no-op-workspace-test-jail-001",
            ApprovalId = "approval-workspace-test-jail-draft-001",
            CandidateActionKind = ProductLedgerInternalCommandKind.ViewLedgerReadiness.ToString(),
            CandidateEvidenceHash = new string('a', 64),
            CandidateEvidenceHashPrefix = new string('a', 12),
            ExecutionResultHashPrefix = "noophash0001",
            EvidenceReferences = ["docs/qa/nodal-os-approved-action-execution-local-only-no-op-to-bounded-action/report.md"],
            StatusText = ProductLedgerLocalApprovedActionNoOpExecutor.CompletedStatus
        };

    private static ProductLedgerLocalBoundedApprovedActionSnapshot ReadyBoundedExecution() =>
        ProductLedgerLocalBoundedApprovedActionSnapshot.Pending with
        {
            Decision = ProductLedgerLocalBoundedApprovedActionDecision.LoadedLocalOnly,
            State = ProductLedgerLocalBoundedApprovedActionState.BoundedExecutionCompletedLocalOnly,
            ExecutionId = "bounded-workspace-test-jail-001",
            ActionId = "bounded-internal-completion-marker-001",
            ActionKind = ProductLedgerLocalBoundedApprovedActionKind.BoundedInternalCompletionMarker.ToString(),
            ApprovalId = "approval-workspace-test-jail-draft-001",
            NoOpExecutionId = "approved-no-op-workspace-test-jail-001",
            CandidateActionKind = ProductLedgerInternalCommandKind.ViewLedgerReadiness.ToString(),
            CandidateEvidenceHash = new string('a', 64),
            CandidateEvidenceHashPrefix = new string('a', 12),
            ResultHashPrefix = "boundedhash1",
            EvidenceReferences = ["docs/qa/nodal-os-approved-action-execution-local-only-no-op-to-bounded-action/report.md"],
            StatusText = ProductLedgerLocalBoundedApprovedActionExecutor.CompletedStatus
        };

    private static ProductLedgerLocalApprovedHandoffReportDraftSnapshot ReadyPredecessor() =>
        ProductLedgerLocalApprovedHandoffReportDraftSnapshot.Pending with
        {
            Decision = ProductLedgerLocalApprovedHandoffReportDraftDecision.DraftCreatedLocalOnly,
            State = ProductLedgerLocalApprovedHandoffReportDraftState.DraftCreatedLocalOnly,
            ActionId = "handoff-draft-action-001",
            CandidateId = "candidate-workspace-test-jail-001",
            ApprovalId = "approval-workspace-test-jail-draft-001",
            NoOpExecutionId = "approved-no-op-workspace-test-jail-001",
            BoundedExecutionId = "bounded-workspace-test-jail-001",
            DraftId = "draft-handoff-draft-action-001-abcdef123456",
            DraftRelativePath = ProductLedgerLocalApprovedHandoffReportDraftExecutor.AllowedRelativeOutputBoundary + "local-approved-handoff-draft-handoff-draft-action-001-aaaaaaaaaaaa.md",
            RedactionApplied = true,
            EvidenceRefs = ["docs/qa/product-ledger-local-approved-handoff-report-draft-implementation/report.md"],
            ContentHash = new string('d', 64),
            ContentHashPrefix = new string('d', 12),
            StatusText = ProductLedgerLocalApprovedHandoffReportDraftExecutor.CompletedStatus
        };

    private static string ReadRepoFile(params string[] segments) =>
        File.ReadAllText(Path.Combine(new[] { RepoRoot() }.Concat(segments).ToArray()));

    private static string RepoRoot()
    {
        var directory = new DirectoryInfo(Directory.GetCurrentDirectory());
        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "OneBrain.slnx")))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        Assert.Fail("repo root not found");
        return string.Empty;
    }

    private sealed class WorkspaceTestJailFixture : IDisposable
    {
        private WorkspaceTestJailFixture(string root, string jailRoot)
        {
            Root = root;
            JailRoot = jailRoot;
            Executor = new ProductLedgerLocalWorkspaceTestJailHandoffDraftExecutor(new ProductLedgerLocalWorkspaceTestJailHandoffDraftOptions(
                WorkspaceTestJailRootPath: jailRoot,
                ExplicitWorkspaceTestJailBoundary: true,
                AllowsArbitraryPathInput: false,
                AllowsFilesystemScan: false,
                AllowsOverwrite: false,
                AllowsUserSelectedPath: false,
                AllowsShellOrSubprocess: false,
                AllowsCommandExecution: false,
                AllowsNetwork: false,
                AllowsDb: false,
                AllowsKmsWormExternalTrust: false,
                AllowsReleaseCommercial: false));
        }

        public string Root { get; }

        public string JailRoot { get; }

        public string ExpectedReadyPath =>
            Path.Combine(JailRoot, ".nodal", "product-ledger", "handoff-drafts", "workspace-test-jail-handoff-draft-workspace-draft-action-001-aaaaaaaaaaaa.md");

        public ProductLedgerLocalWorkspaceTestJailHandoffDraftExecutor Executor { get; }

        public string FullPath(ProductLedgerLocalWorkspaceTestJailHandoffDraftSnapshot snapshot) =>
            Path.Combine(JailRoot, snapshot.DraftRelativePath.Replace('/', Path.DirectorySeparatorChar));

        public static WorkspaceTestJailFixture Create()
        {
            var root = Path.Combine(RepoRoot(), ".tmp-product-ledger-workspace-test-jail-tests", Guid.NewGuid().ToString("N"));
            var jailRoot = Path.Combine(root, "docs", "test-output", "product-ledger", "workspace-test-jail");
            return new WorkspaceTestJailFixture(root, jailRoot);
        }

        public void Dispose()
        {
            var tempRoot = Path.Combine(RepoRoot(), ".tmp-product-ledger-workspace-test-jail-tests");
            if (Directory.Exists(tempRoot))
            {
                Directory.Delete(tempRoot, recursive: true);
            }
        }
    }
}
