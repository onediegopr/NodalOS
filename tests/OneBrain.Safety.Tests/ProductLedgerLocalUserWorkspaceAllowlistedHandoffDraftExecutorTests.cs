using Microsoft.VisualStudio.TestTools.UnitTesting;
using OneBrain.Core.Approval;

namespace OneBrain.Safety.Tests;

[TestClass]
[TestCategory("ApprovalHumanReview")]
[TestCategory("PhaseEApprovalHumanReview")]
public sealed class ProductLedgerLocalUserWorkspaceAllowlistedHandoffDraftExecutorTests
{
    [TestMethod]
    public void UserWorkspaceAllowlistedHandoffDraftExecutor_CreatesRedactedDraftUnderAllowedBoundary()
    {
        using var fixture = UserWorkspaceAllowlistedFixture.Create();

        var result = fixture.Executor.CreateDraft(ReadyRequest());
        var fullPath = fixture.FullPath(result);
        var content = File.ReadAllText(fullPath);

        Assert.AreEqual(ProductLedgerLocalUserWorkspaceAllowlistedHandoffDraftDecision.DraftCreatedUserWorkspaceAllowlistedOnly, result.Decision);
        Assert.AreEqual(ProductLedgerLocalUserWorkspaceAllowlistedHandoffDraftState.DraftCreatedUserWorkspaceAllowlistedOnly, result.State);
        Assert.AreEqual(0, result.Blockers.Count);
        Assert.IsTrue(File.Exists(fullPath));
        Assert.IsTrue(Path.GetFullPath(fullPath).StartsWith(Path.GetFullPath(fixture.AllowedBoundaryRoot), StringComparison.OrdinalIgnoreCase));
        StringAssert.StartsWith(result.DraftRelativePath, ProductLedgerLocalUserWorkspaceAllowlistedHandoffDraftExecutor.AllowedRelativeOutputBoundary);
        StringAssert.Contains(result.DraftRelativePath, "user-workspace-allowlisted-handoff-draft-user-workspace-draft-action-001-aaaaaaaaaaaa.md");
        Assert.IsTrue(result.CreateOnly);
        Assert.IsFalse(result.OverwriteAllowed);
        Assert.IsFalse(result.UserSelectedPathAllowed);
        Assert.IsFalse(result.PayloadControlledRootAllowed);
        Assert.IsTrue(result.CanonicalizationPassed);
        Assert.IsTrue(result.ReparseValidationPassed);
        Assert.IsTrue(result.UserWorkspaceAllowlistedBoundaryOnly);
        Assert.IsTrue(result.RedactionApplied);
        Assert.IsFalse(result.ShellAllowed);
        Assert.IsFalse(result.NetworkAllowed);
        Assert.IsFalse(result.ProductionAllowed);
        Assert.IsFalse(result.PublicProductAllowed);
        Assert.IsFalse(result.ProductCommandExecuted);
        Assert.IsFalse(result.PublicUiActionAvailable);
        Assert.IsFalse(result.ProviderCloudNetworkAvailable);
        Assert.IsFalse(result.ReleaseCommercialReady);
        StringAssert.Contains(content, "Local User Workspace Allowlisted Handoff Draft");
        StringAssert.Contains(content, ProductLedgerLocalUserWorkspaceAllowlistedHandoffDraftExecutor.WorkspaceClassification);
        StringAssert.Contains(content, ProductLedgerLocalUserWorkspaceAllowlistedHandoffDraftExecutor.AllowedRelativeOutputBoundary);
        StringAssert.Contains(content, "redacted user workspace allowlisted handoff summary");
        StringAssert.Contains(content, "No user-selected path.");
        StringAssert.Contains(content, "No arbitrary workspace write.");
        StringAssert.Contains(content, "No shell/subprocess.");
        StringAssert.Contains(content, "No command execution.");
        StringAssert.Contains(content, "No cloud/network/DB.");
        Assert.IsFalse(content.Contains("password=", StringComparison.OrdinalIgnoreCase));
        Assert.IsFalse(content.Contains(@"C:\Users\", StringComparison.OrdinalIgnoreCase));
        Assert.IsFalse(content.Contains(fixture.WorkspaceRoot, StringComparison.OrdinalIgnoreCase));
    }

    [TestMethod]
    public void UserWorkspaceAllowlistedHandoffDraftExecutor_BlocksMissingOrInvalidApprovedChain()
    {
        var cases = new (ProductLedgerLocalUserWorkspaceAllowlistedHandoffDraftRequest? Request, ProductLedgerLocalUserWorkspaceAllowlistedHandoffDraftBlocker Blocker)[]
        {
            (null, ProductLedgerLocalUserWorkspaceAllowlistedHandoffDraftBlocker.MissingRequest),
            (ReadyRequest() with { ApprovalDecision = ProductLedgerLocalApprovalDecisionSnapshot.PendingPreviewOnly }, ProductLedgerLocalUserWorkspaceAllowlistedHandoffDraftBlocker.ApprovalDecisionNotApproved),
            (ReadyRequest() with { NoOpExecution = ProductLedgerLocalApprovedActionExecutionSnapshot.Pending }, ProductLedgerLocalUserWorkspaceAllowlistedHandoffDraftBlocker.NoOpExecutionNotCompleted),
            (ReadyRequest() with { BoundedExecution = ProductLedgerLocalBoundedApprovedActionSnapshot.Pending }, ProductLedgerLocalUserWorkspaceAllowlistedHandoffDraftBlocker.BoundedExecutionNotCompleted),
            (ReadyRequest() with { LocalApprovedHandoffDraft = ProductLedgerLocalApprovedHandoffReportDraftSnapshot.Pending }, ProductLedgerLocalUserWorkspaceAllowlistedHandoffDraftBlocker.LocalApprovedHandoffDraftNotCompleted),
            (ReadyRequest() with { LocalApprovedHandoffDraft = null }, ProductLedgerLocalUserWorkspaceAllowlistedHandoffDraftBlocker.MissingLocalApprovedHandoffDraft),
            (ReadyRequest() with { WorkspaceTestJailHandoffDraft = ProductLedgerLocalWorkspaceTestJailHandoffDraftSnapshot.Pending }, ProductLedgerLocalUserWorkspaceAllowlistedHandoffDraftBlocker.WorkspaceTestJailHandoffDraftNotCompleted),
            (ReadyRequest() with { WorkspaceTestJailHandoffDraft = null }, ProductLedgerLocalUserWorkspaceAllowlistedHandoffDraftBlocker.MissingWorkspaceTestJailHandoffDraft),
            (ReadyRequest() with { ActionKind = null }, ProductLedgerLocalUserWorkspaceAllowlistedHandoffDraftBlocker.UnknownActionKind),
            (ReadyRequest() with { CandidateActionKind = ProductLedgerInternalCommandKind.ViewDiagnostics }, ProductLedgerLocalUserWorkspaceAllowlistedHandoffDraftBlocker.CandidateActionMismatch),
            (ReadyRequest() with { CurrentEvidenceHash = new string('b', 64) }, ProductLedgerLocalUserWorkspaceAllowlistedHandoffDraftBlocker.CandidateEvidenceHashMismatch),
            (ReadyRequest() with { LocalApprovedHandoffDraftContentHash = new string('c', 64) }, ProductLedgerLocalUserWorkspaceAllowlistedHandoffDraftBlocker.LocalApprovedHandoffDraftMismatch),
            (ReadyRequest() with { WorkspaceTestJailHandoffDraftContentHash = new string('f', 64) }, ProductLedgerLocalUserWorkspaceAllowlistedHandoffDraftBlocker.WorkspaceTestJailHandoffDraftMismatch),
            (ReadyRequest() with { EvidenceReferences = [] }, ProductLedgerLocalUserWorkspaceAllowlistedHandoffDraftBlocker.MissingEvidenceReferences)
        };

        foreach (var testCase in cases)
        {
            using var fixture = UserWorkspaceAllowlistedFixture.Create();
            var result = fixture.Executor.CreateDraft(testCase.Request);

            Assert.AreEqual(ProductLedgerLocalUserWorkspaceAllowlistedHandoffDraftDecision.Rejected, result.Decision, testCase.Blocker.ToString());
            CollectionAssert.Contains(result.Blockers.ToArray(), testCase.Blocker, testCase.Blocker.ToString());
            Assert.IsFalse(File.Exists(fixture.ExpectedReadyPath));
        }
    }

    [TestMethod]
    public void UserWorkspaceAllowlistedHandoffDraftExecutor_BlocksPathCommandNetworkOverwriteAndUnsafeContent()
    {
        var ready = ReadyRequest();
        var cases = new Dictionary<ProductLedgerLocalUserWorkspaceAllowlistedHandoffDraftRequest, ProductLedgerLocalUserWorkspaceAllowlistedHandoffDraftBlocker>
        {
            [ready with { ExplicitUserWorkspaceAllowlistedScope = false }] = ProductLedgerLocalUserWorkspaceAllowlistedHandoffDraftBlocker.MissingExplicitUserWorkspaceAllowlistedScope,
            [ready with { DevelopmentMode = false }] = ProductLedgerLocalUserWorkspaceAllowlistedHandoffDraftBlocker.NonDevelopmentMode,
            [ready with { LocalMode = false }] = ProductLedgerLocalUserWorkspaceAllowlistedHandoffDraftBlocker.NonLocalMode,
            [ready with { InternalMode = false }] = ProductLedgerLocalUserWorkspaceAllowlistedHandoffDraftBlocker.NonInternalMode,
            [ready with { ActionId = "..\\unsafe" }] = ProductLedgerLocalUserWorkspaceAllowlistedHandoffDraftBlocker.UnsafeActionId,
            [ready with { ProposedPath = @"C:\Users\fixture\unsafe.txt" }] = ProductLedgerLocalUserWorkspaceAllowlistedHandoffDraftBlocker.PayloadPathFieldRejected,
            [ready with { ProposedRoot = @"C:\Users\fixture" }] = ProductLedgerLocalUserWorkspaceAllowlistedHandoffDraftBlocker.PayloadPathFieldRejected,
            [ready with { ProposedFilename = "..\\unsafe.md" }] = ProductLedgerLocalUserWorkspaceAllowlistedHandoffDraftBlocker.PayloadPathFieldRejected,
            [ready with { ProposedCommand = "cmd /c whoami" }] = ProductLedgerLocalUserWorkspaceAllowlistedHandoffDraftBlocker.PayloadCommandFieldRejected,
            [ready with { ProposedUrl = "https://local.invalid/action" }] = ProductLedgerLocalUserWorkspaceAllowlistedHandoffDraftBlocker.PayloadNetworkProviderFieldRejected,
            [ready with { ProposedProvider = "provider-live" }] = ProductLedgerLocalUserWorkspaceAllowlistedHandoffDraftBlocker.PayloadNetworkProviderFieldRejected,
            [ready with { ProposedDbMigration = "migration" }] = ProductLedgerLocalUserWorkspaceAllowlistedHandoffDraftBlocker.PayloadNetworkProviderFieldRejected,
            [ready with { ClaimsArbitraryPathInput = true }] = ProductLedgerLocalUserWorkspaceAllowlistedHandoffDraftBlocker.ClaimsArbitraryPathInput,
            [ready with { ClaimsFilesystemScan = true }] = ProductLedgerLocalUserWorkspaceAllowlistedHandoffDraftBlocker.ClaimsFilesystemScan,
            [ready with { RequestsOverwrite = true }] = ProductLedgerLocalUserWorkspaceAllowlistedHandoffDraftBlocker.RequestsOverwrite,
            [ready with { RequestsUserSelectedPath = true }] = ProductLedgerLocalUserWorkspaceAllowlistedHandoffDraftBlocker.RequestsUserSelectedPath,
            [ready with { RequestsShellOrSubprocess = true }] = ProductLedgerLocalUserWorkspaceAllowlistedHandoffDraftBlocker.RequestsShellOrSubprocess,
            [ready with { ClaimsArbitraryCommandExecution = true }] = ProductLedgerLocalUserWorkspaceAllowlistedHandoffDraftBlocker.ClaimsArbitraryCommandExecution,
            [ready with { ClaimsProviderCloudNetwork = true }] = ProductLedgerLocalUserWorkspaceAllowlistedHandoffDraftBlocker.ClaimsProviderCloudNetwork,
            [ready with { ClaimsDbMigration = true }] = ProductLedgerLocalUserWorkspaceAllowlistedHandoffDraftBlocker.ClaimsDbMigration,
            [ready with { ClaimsKmsWormExternalTrust = true }] = ProductLedgerLocalUserWorkspaceAllowlistedHandoffDraftBlocker.ClaimsKmsWormExternalTrust,
            [ready with { ClaimsBrowserCdpWcuOcrRecipesLive = true }] = ProductLedgerLocalUserWorkspaceAllowlistedHandoffDraftBlocker.ClaimsBrowserCdpWcuOcrRecipesLive,
            [ready with { ClaimsPilotRun = true }] = ProductLedgerLocalUserWorkspaceAllowlistedHandoffDraftBlocker.ClaimsPilotRun,
            [ready with { ClaimsReleaseCommercial = true }] = ProductLedgerLocalUserWorkspaceAllowlistedHandoffDraftBlocker.ClaimsReleaseCommercial,
            [ready with { RedactedDraftSummary = "password=unsafe" }] = ProductLedgerLocalUserWorkspaceAllowlistedHandoffDraftBlocker.RedactionRejected,
            [ready with { RedactedDraftSummary = @"C:\Users\fixture\unsafe.txt" }] = ProductLedgerLocalUserWorkspaceAllowlistedHandoffDraftBlocker.RedactionRejected
        };

        foreach (var testCase in cases)
        {
            using var fixture = UserWorkspaceAllowlistedFixture.Create();
            var result = fixture.Executor.CreateDraft(testCase.Key);

            Assert.AreEqual(ProductLedgerLocalUserWorkspaceAllowlistedHandoffDraftDecision.Rejected, result.Decision, testCase.Value.ToString());
            CollectionAssert.Contains(result.Blockers.ToArray(), testCase.Value, testCase.Value.ToString());
            Assert.IsFalse(File.Exists(fixture.ExpectedReadyPath));
        }
    }

    [TestMethod]
    public void UserWorkspaceAllowlistedHandoffDraftExecutor_ReplaysExactDraftAndBlocksConflictingExistingFile()
    {
        using var fixture = UserWorkspaceAllowlistedFixture.Create();
        var first = fixture.Executor.CreateDraft(ReadyRequest());
        var replay = fixture.Executor.CreateDraft(ReadyRequest());

        Assert.AreEqual(ProductLedgerLocalUserWorkspaceAllowlistedHandoffDraftDecision.DraftCreatedUserWorkspaceAllowlistedOnly, first.Decision);
        Assert.AreEqual(ProductLedgerLocalUserWorkspaceAllowlistedHandoffDraftDecision.IdempotentReplay, replay.Decision);
        Assert.AreEqual(first.ContentHash, replay.ContentHash);

        using var conflictFixture = UserWorkspaceAllowlistedFixture.Create();
        Directory.CreateDirectory(Path.GetDirectoryName(conflictFixture.ExpectedReadyPath)!);
        File.WriteAllText(conflictFixture.ExpectedReadyPath, "conflicting safe content");
        var conflict = conflictFixture.Executor.CreateDraft(ReadyRequest());

        Assert.AreEqual(ProductLedgerLocalUserWorkspaceAllowlistedHandoffDraftDecision.Rejected, conflict.Decision);
        CollectionAssert.Contains(conflict.Blockers.ToArray(), ProductLedgerLocalUserWorkspaceAllowlistedHandoffDraftBlocker.ExistingDraftConflict);
    }

    [TestMethod]
    public void UserWorkspaceAllowlistedHandoffDraftExecutor_SourceHasOnlyBoundedCreateOnlyFileWriteAndNoForbiddenRuntime()
    {
        var source = ReadRepoFile("src", "OneBrain.Core", "Approval", "ProductLedgerLocalUserWorkspaceAllowlistedHandoffDraftExecutor.cs");
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
        StringAssert.Contains(source, ProductLedgerLocalUserWorkspaceAllowlistedHandoffDraftExecutor.AllowedRelativeOutputBoundary);
        StringAssert.Contains(source, ProductLedgerLocalUserWorkspaceAllowlistedHandoffDraftExecutor.WorkspaceClassification);
        StringAssert.Contains(source, "NO_OVERWRITE");
        StringAssert.Contains(source, "NO_COMMAND_EXECUTION");
        StringAssert.Contains(source, "NO_SHELL_SUBPROCESS");
    }

    private static ProductLedgerLocalUserWorkspaceAllowlistedHandoffDraftRequest ReadyRequest()
    {
        var localDraft = ReadyLocalDraft();
        var workspaceDraft = ReadyWorkspaceTestJailDraft();
        return new(
            ExplicitUserWorkspaceAllowlistedScope: true,
            DevelopmentMode: true,
            LocalMode: true,
            InternalMode: true,
            ActionId: "user-workspace-draft-action-001",
            CandidateId: "candidate-user-workspace-allowlisted-001",
            ActionKind: ProductLedgerLocalUserWorkspaceAllowlistedHandoffDraftActionKind.LocalUserWorkspaceAllowlistedHandoffDraftCreateOnly,
            ApprovalDecision: ReadyApproval(),
            NoOpExecution: ReadyNoOpExecution(),
            BoundedExecution: ReadyBoundedExecution(),
            LocalApprovedHandoffDraft: localDraft,
            WorkspaceTestJailHandoffDraft: workspaceDraft,
            CandidateActionKind: ProductLedgerInternalCommandKind.ViewLedgerReadiness,
            CandidateEvidenceHash: new string('a', 64),
            CurrentEvidenceHash: new string('a', 64),
            LocalApprovedHandoffDraftContentHash: localDraft.ContentHash,
            WorkspaceTestJailHandoffDraftContentHash: workspaceDraft.ContentHash,
            DraftTitle: "Local User Workspace Allowlisted Handoff Draft",
            RedactedDraftSummary: "redacted user workspace allowlisted handoff summary",
            EvidenceReferences:
            [
                "docs/qa/product-ledger-user-workspace-allowlisted-handoff-draft-implementation/report.md"
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
            ApprovalId = "approval-user-workspace-allowlisted-draft-001",
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
            ExecutionId = "approved-no-op-user-workspace-001",
            ApprovalId = "approval-user-workspace-allowlisted-draft-001",
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
            ExecutionId = "bounded-user-workspace-001",
            ActionId = "bounded-internal-completion-marker-001",
            ActionKind = ProductLedgerLocalBoundedApprovedActionKind.BoundedInternalCompletionMarker.ToString(),
            ApprovalId = "approval-user-workspace-allowlisted-draft-001",
            NoOpExecutionId = "approved-no-op-user-workspace-001",
            CandidateActionKind = ProductLedgerInternalCommandKind.ViewLedgerReadiness.ToString(),
            CandidateEvidenceHash = new string('a', 64),
            CandidateEvidenceHashPrefix = new string('a', 12),
            ResultHashPrefix = "boundedhash1",
            EvidenceReferences = ["docs/qa/nodal-os-approved-action-execution-local-only-no-op-to-bounded-action/report.md"],
            StatusText = ProductLedgerLocalBoundedApprovedActionExecutor.CompletedStatus
        };

    private static ProductLedgerLocalApprovedHandoffReportDraftSnapshot ReadyLocalDraft() =>
        ProductLedgerLocalApprovedHandoffReportDraftSnapshot.Pending with
        {
            Decision = ProductLedgerLocalApprovedHandoffReportDraftDecision.DraftCreatedLocalOnly,
            State = ProductLedgerLocalApprovedHandoffReportDraftState.DraftCreatedLocalOnly,
            ActionId = "handoff-draft-action-001",
            CandidateId = "candidate-user-workspace-allowlisted-001",
            ApprovalId = "approval-user-workspace-allowlisted-draft-001",
            NoOpExecutionId = "approved-no-op-user-workspace-001",
            BoundedExecutionId = "bounded-user-workspace-001",
            DraftId = "draft-handoff-draft-action-001-abcdef123456",
            DraftRelativePath = ProductLedgerLocalApprovedHandoffReportDraftExecutor.AllowedRelativeOutputBoundary + "local-approved-handoff-draft-handoff-draft-action-001-aaaaaaaaaaaa.md",
            RedactionApplied = true,
            EvidenceRefs = ["docs/qa/product-ledger-local-approved-handoff-report-draft-implementation/report.md"],
            ContentHash = new string('d', 64),
            ContentHashPrefix = new string('d', 12),
            StatusText = ProductLedgerLocalApprovedHandoffReportDraftExecutor.CompletedStatus
        };

    private static ProductLedgerLocalWorkspaceTestJailHandoffDraftSnapshot ReadyWorkspaceTestJailDraft() =>
        ProductLedgerLocalWorkspaceTestJailHandoffDraftSnapshot.Pending with
        {
            Decision = ProductLedgerLocalWorkspaceTestJailHandoffDraftDecision.DraftCreatedWorkspaceTestJailOnly,
            State = ProductLedgerLocalWorkspaceTestJailHandoffDraftState.DraftCreatedWorkspaceTestJailOnly,
            ActionId = "workspace-draft-action-001",
            CandidateId = "candidate-user-workspace-allowlisted-001",
            ApprovalId = "approval-user-workspace-allowlisted-draft-001",
            NoOpExecutionId = "approved-no-op-user-workspace-001",
            BoundedExecutionId = "bounded-user-workspace-001",
            PredecessorDraftId = "draft-handoff-draft-action-001-abcdef123456",
            DraftId = "workspace-test-jail-draft-workspace-draft-action-001-eeeeeeeeeeee",
            DraftRelativePath = ProductLedgerLocalWorkspaceTestJailHandoffDraftExecutor.AllowedRelativeOutputBoundary + "workspace-test-jail-handoff-draft-workspace-draft-action-001-aaaaaaaaaaaa.md",
            WorkspaceTestJailBoundary = ProductLedgerLocalWorkspaceTestJailHandoffDraftExecutor.AllowedRelativeOutputBoundary,
            RedactionApplied = true,
            EvidenceRefs = ["docs/qa/product-ledger-workspace-test-jail-handoff-draft-implementation/report.md"],
            ContentHash = new string('e', 64),
            ContentHashPrefix = new string('e', 12),
            StatusText = ProductLedgerLocalWorkspaceTestJailHandoffDraftExecutor.CompletedStatus
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

    private sealed class UserWorkspaceAllowlistedFixture : IDisposable
    {
        private UserWorkspaceAllowlistedFixture(string workspaceRoot)
        {
            WorkspaceRoot = workspaceRoot;
            Executor = new ProductLedgerLocalUserWorkspaceAllowlistedHandoffDraftExecutor(new ProductLedgerLocalUserWorkspaceAllowlistedHandoffDraftOptions(
                TrustedWorkspaceRootPath: workspaceRoot,
                WorkspaceClassification: ProductLedgerLocalUserWorkspaceAllowlistedHandoffDraftExecutor.WorkspaceClassification,
                ExplicitUserWorkspaceAllowlistedBoundary: true,
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

        public string WorkspaceRoot { get; }

        public string AllowedBoundaryRoot =>
            Path.Combine(WorkspaceRoot, "docs", "nodal-os", "handoffs");

        public string ExpectedReadyPath =>
            Path.Combine(AllowedBoundaryRoot, "user-workspace-allowlisted-handoff-draft-user-workspace-draft-action-001-aaaaaaaaaaaa.md");

        public ProductLedgerLocalUserWorkspaceAllowlistedHandoffDraftExecutor Executor { get; }

        public string FullPath(ProductLedgerLocalUserWorkspaceAllowlistedHandoffDraftSnapshot snapshot) =>
            Path.Combine(WorkspaceRoot, snapshot.DraftRelativePath.Replace('/', Path.DirectorySeparatorChar));

        public static UserWorkspaceAllowlistedFixture Create()
        {
            var root = Path.Combine(RepoRoot(), ".tmp-product-ledger-user-workspace-allowlisted-tests", Guid.NewGuid().ToString("N"));
            return new UserWorkspaceAllowlistedFixture(root);
        }

        public void Dispose()
        {
            var tempRoot = Path.Combine(RepoRoot(), ".tmp-product-ledger-user-workspace-allowlisted-tests");
            if (Directory.Exists(tempRoot))
            {
                Directory.Delete(tempRoot, recursive: true);
            }
        }
    }
}
