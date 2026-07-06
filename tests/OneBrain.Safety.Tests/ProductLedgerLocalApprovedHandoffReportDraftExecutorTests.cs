using Microsoft.VisualStudio.TestTools.UnitTesting;
using OneBrain.Core.Approval;

namespace OneBrain.Safety.Tests;

[TestClass]
[TestCategory("ApprovalHumanReview")]
[TestCategory("PhaseEApprovalHumanReview")]
public sealed class ProductLedgerLocalApprovedHandoffReportDraftExecutorTests
{
    [TestMethod]
    public void HandoffReportDraftExecutor_CreatesOneRedactedDraftUnderAllowlistedBoundary()
    {
        using var fixture = HandoffDraftFixture.Create();

        var result = fixture.Executor.CreateDraft(ReadyRequest());
        var fullPath = fixture.FullPath(result);
        var content = File.ReadAllText(fullPath);

        Assert.AreEqual(ProductLedgerLocalApprovedHandoffReportDraftDecision.DraftCreatedLocalOnly, result.Decision);
        Assert.AreEqual(ProductLedgerLocalApprovedHandoffReportDraftState.DraftCreatedLocalOnly, result.State);
        Assert.AreEqual(0, result.Blockers.Count);
        Assert.IsTrue(File.Exists(fullPath));
        StringAssert.StartsWith(result.DraftRelativePath, ProductLedgerLocalApprovedHandoffReportDraftExecutor.AllowedRelativeOutputBoundary);
        StringAssert.Contains(result.DraftRelativePath, "local-approved-handoff-draft-handoff-draft-action-001-aaaaaaaaaaaa.md");
        Assert.IsTrue(result.CreateOnly);
        Assert.IsFalse(result.OverwriteAllowed);
        Assert.IsFalse(result.UserFileWrite);
        Assert.IsFalse(result.ShellAllowed);
        Assert.IsFalse(result.NetworkAllowed);
        Assert.IsFalse(result.ProductionAllowed);
        Assert.IsFalse(result.PublicProductAllowed);
        Assert.IsTrue(result.RedactionApplied);
        Assert.IsFalse(result.ProductCommandExecuted);
        Assert.IsFalse(result.PublicUiActionAvailable);
        Assert.IsFalse(result.ProviderCloudNetworkAvailable);
        Assert.IsFalse(result.ReleaseCommercialReady);
        StringAssert.Contains(content, "Local Approved Handoff Report Draft");
        StringAssert.Contains(content, "redacted local handoff summary");
        StringAssert.Contains(content, "No shell/subprocess.");
        StringAssert.Contains(content, "No command execution.");
        StringAssert.Contains(content, "No cloud/network/DB.");
        Assert.IsFalse(content.Contains("password=", StringComparison.OrdinalIgnoreCase));
        Assert.IsFalse(content.Contains(@"C:\Users\", StringComparison.OrdinalIgnoreCase));
    }

    [TestMethod]
    public void HandoffReportDraftExecutor_BlocksMissingOrInvalidApprovedChain()
    {
        var cases = new (ProductLedgerLocalApprovedHandoffReportDraftRequest? Request, ProductLedgerLocalApprovedHandoffReportDraftBlocker Blocker)[]
        {
            (null, ProductLedgerLocalApprovedHandoffReportDraftBlocker.MissingRequest),
            (ReadyRequest() with { ApprovalDecision = ProductLedgerLocalApprovalDecisionSnapshot.PendingPreviewOnly }, ProductLedgerLocalApprovedHandoffReportDraftBlocker.ApprovalDecisionNotApproved),
            (ReadyRequest() with { NoOpExecution = ProductLedgerLocalApprovedActionExecutionSnapshot.Pending }, ProductLedgerLocalApprovedHandoffReportDraftBlocker.NoOpExecutionNotCompleted),
            (ReadyRequest() with { BoundedExecution = ProductLedgerLocalBoundedApprovedActionSnapshot.Pending }, ProductLedgerLocalApprovedHandoffReportDraftBlocker.BoundedExecutionNotCompleted),
            (ReadyRequest() with { ActionKind = null }, ProductLedgerLocalApprovedHandoffReportDraftBlocker.UnknownActionKind),
            (ReadyRequest() with { CandidateActionKind = ProductLedgerInternalCommandKind.ViewDiagnostics }, ProductLedgerLocalApprovedHandoffReportDraftBlocker.CandidateActionMismatch),
            (ReadyRequest() with { CurrentEvidenceHash = new string('b', 64) }, ProductLedgerLocalApprovedHandoffReportDraftBlocker.CandidateEvidenceHashMismatch),
            (ReadyRequest() with { EvidenceReferences = [] }, ProductLedgerLocalApprovedHandoffReportDraftBlocker.MissingEvidenceReferences)
        };

        foreach (var testCase in cases)
        {
            using var fixture = HandoffDraftFixture.Create();
            var result = fixture.Executor.CreateDraft(testCase.Request);

            Assert.AreEqual(ProductLedgerLocalApprovedHandoffReportDraftDecision.Rejected, result.Decision, testCase.Blocker.ToString());
            CollectionAssert.Contains(result.Blockers.ToArray(), testCase.Blocker, testCase.Blocker.ToString());
            Assert.IsFalse(File.Exists(fixture.ExpectedReadyPath));
        }
    }

    [TestMethod]
    public void HandoffReportDraftExecutor_BlocksPathCommandNetworkOverwriteAndUnsafeContent()
    {
        var ready = ReadyRequest();
        var cases = new Dictionary<ProductLedgerLocalApprovedHandoffReportDraftRequest, ProductLedgerLocalApprovedHandoffReportDraftBlocker>
        {
            [ready with { ExplicitLocalApprovedHandoffDraftScope = false }] = ProductLedgerLocalApprovedHandoffReportDraftBlocker.MissingExplicitLocalApprovedHandoffDraftScope,
            [ready with { DevelopmentMode = false }] = ProductLedgerLocalApprovedHandoffReportDraftBlocker.NonDevelopmentMode,
            [ready with { LocalMode = false }] = ProductLedgerLocalApprovedHandoffReportDraftBlocker.NonLocalMode,
            [ready with { InternalMode = false }] = ProductLedgerLocalApprovedHandoffReportDraftBlocker.NonInternalMode,
            [ready with { ActionId = "..\\unsafe" }] = ProductLedgerLocalApprovedHandoffReportDraftBlocker.UnsafeActionId,
            [ready with { ProposedPath = @"C:\Users\fixture\unsafe.txt" }] = ProductLedgerLocalApprovedHandoffReportDraftBlocker.PayloadPathFieldRejected,
            [ready with { ProposedCommand = "cmd /c whoami" }] = ProductLedgerLocalApprovedHandoffReportDraftBlocker.PayloadCommandFieldRejected,
            [ready with { ProposedUrl = "https://local.invalid/action" }] = ProductLedgerLocalApprovedHandoffReportDraftBlocker.PayloadNetworkProviderFieldRejected,
            [ready with { ProposedProvider = "provider-live" }] = ProductLedgerLocalApprovedHandoffReportDraftBlocker.PayloadNetworkProviderFieldRejected,
            [ready with { ProposedDbMigration = "migration" }] = ProductLedgerLocalApprovedHandoffReportDraftBlocker.PayloadNetworkProviderFieldRejected,
            [ready with { ClaimsArbitraryPathInput = true }] = ProductLedgerLocalApprovedHandoffReportDraftBlocker.ClaimsArbitraryPathInput,
            [ready with { ClaimsFilesystemScan = true }] = ProductLedgerLocalApprovedHandoffReportDraftBlocker.ClaimsFilesystemScan,
            [ready with { RequestsOverwrite = true }] = ProductLedgerLocalApprovedHandoffReportDraftBlocker.RequestsOverwrite,
            [ready with { RequestsUserFileWrite = true }] = ProductLedgerLocalApprovedHandoffReportDraftBlocker.RequestsUserFileWrite,
            [ready with { RequestsShellOrSubprocess = true }] = ProductLedgerLocalApprovedHandoffReportDraftBlocker.RequestsShellOrSubprocess,
            [ready with { ClaimsArbitraryCommandExecution = true }] = ProductLedgerLocalApprovedHandoffReportDraftBlocker.ClaimsArbitraryCommandExecution,
            [ready with { ClaimsProviderCloudNetwork = true }] = ProductLedgerLocalApprovedHandoffReportDraftBlocker.ClaimsProviderCloudNetwork,
            [ready with { ClaimsDbMigration = true }] = ProductLedgerLocalApprovedHandoffReportDraftBlocker.ClaimsDbMigration,
            [ready with { ClaimsKmsWormExternalTrust = true }] = ProductLedgerLocalApprovedHandoffReportDraftBlocker.ClaimsKmsWormExternalTrust,
            [ready with { ClaimsBrowserCdpWcuOcrRecipesLive = true }] = ProductLedgerLocalApprovedHandoffReportDraftBlocker.ClaimsBrowserCdpWcuOcrRecipesLive,
            [ready with { ClaimsPilotRun = true }] = ProductLedgerLocalApprovedHandoffReportDraftBlocker.ClaimsPilotRun,
            [ready with { ClaimsReleaseCommercial = true }] = ProductLedgerLocalApprovedHandoffReportDraftBlocker.ClaimsReleaseCommercial,
            [ready with { RedactedDraftSummary = "password=unsafe" }] = ProductLedgerLocalApprovedHandoffReportDraftBlocker.RedactionRejected,
            [ready with { RedactedDraftSummary = @"C:\Users\fixture\unsafe.txt" }] = ProductLedgerLocalApprovedHandoffReportDraftBlocker.RedactionRejected
        };

        foreach (var testCase in cases)
        {
            using var fixture = HandoffDraftFixture.Create();
            var result = fixture.Executor.CreateDraft(testCase.Key);

            Assert.AreEqual(ProductLedgerLocalApprovedHandoffReportDraftDecision.Rejected, result.Decision, testCase.Value.ToString());
            CollectionAssert.Contains(result.Blockers.ToArray(), testCase.Value, testCase.Value.ToString());
            Assert.IsFalse(File.Exists(fixture.ExpectedReadyPath));
        }
    }

    [TestMethod]
    public void HandoffReportDraftExecutor_ReplaysExactDraftAndBlocksConflictingExistingFile()
    {
        using var fixture = HandoffDraftFixture.Create();
        var first = fixture.Executor.CreateDraft(ReadyRequest());
        var replay = fixture.Executor.CreateDraft(ReadyRequest());

        Assert.AreEqual(ProductLedgerLocalApprovedHandoffReportDraftDecision.DraftCreatedLocalOnly, first.Decision);
        Assert.AreEqual(ProductLedgerLocalApprovedHandoffReportDraftDecision.IdempotentReplay, replay.Decision);
        Assert.AreEqual(first.ContentHash, replay.ContentHash);

        using var conflictFixture = HandoffDraftFixture.Create();
        Directory.CreateDirectory(conflictFixture.OutputRoot);
        File.WriteAllText(conflictFixture.ExpectedReadyPath, "conflicting safe content");
        var conflict = conflictFixture.Executor.CreateDraft(ReadyRequest());

        Assert.AreEqual(ProductLedgerLocalApprovedHandoffReportDraftDecision.Rejected, conflict.Decision);
        CollectionAssert.Contains(conflict.Blockers.ToArray(), ProductLedgerLocalApprovedHandoffReportDraftBlocker.ExistingDraftConflict);
    }

    [TestMethod]
    public void HandoffReportDraftExecutor_SourceHasOnlyBoundedCreateOnlyFileWriteAndNoForbiddenRuntime()
    {
        var source = ReadRepoFile("src", "OneBrain.Core", "Approval", "ProductLedgerLocalApprovedHandoffReportDraftExecutor.cs");
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
            "UserFileWrite: true",
            "ProductionAllowed: true",
            "PublicProductAllowed: true",
            "ReleaseCommercialReady: true"
        })
        {
            Assert.IsFalse(source.Contains(fragment, StringComparison.Ordinal), fragment);
        }

        StringAssert.Contains(source, "FileMode.CreateNew");
        StringAssert.Contains(source, ProductLedgerLocalApprovedHandoffReportDraftExecutor.AllowedRelativeOutputBoundary);
        StringAssert.Contains(source, "NO_OVERWRITE");
        StringAssert.Contains(source, "NO_COMMAND_EXECUTION");
        StringAssert.Contains(source, "NO_SHELL_SUBPROCESS");
    }

    private static ProductLedgerLocalApprovedHandoffReportDraftRequest ReadyRequest() =>
        new(
            ExplicitLocalApprovedHandoffDraftScope: true,
            DevelopmentMode: true,
            LocalMode: true,
            InternalMode: true,
            ActionId: "handoff-draft-action-001",
            CandidateId: "candidate-local-handoff-001",
            ActionKind: ProductLedgerLocalApprovedHandoffReportDraftActionKind.LocalApprovedHandoffReportDraft,
            ApprovalDecision: ReadyApproval(),
            NoOpExecution: ReadyNoOpExecution(),
            BoundedExecution: ReadyBoundedExecution(),
            CandidateActionKind: ProductLedgerInternalCommandKind.ViewLedgerReadiness,
            CandidateEvidenceHash: new string('a', 64),
            CurrentEvidenceHash: new string('a', 64),
            DraftTitle: "Local Approved Handoff Report Draft",
            RedactedDraftSummary: "redacted local handoff summary",
            EvidenceReferences:
            [
                "docs/qa/product-ledger-local-approved-handoff-report-draft-implementation/report.md"
            ],
            ProposedPath: null,
            ProposedCommand: null,
            ProposedUrl: null,
            ProposedProvider: null,
            ProposedDbMigration: null,
            ClaimsArbitraryPathInput: false,
            ClaimsFilesystemScan: false,
            RequestsOverwrite: false,
            RequestsUserFileWrite: false,
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

    private static ProductLedgerLocalApprovalDecisionSnapshot ReadyApproval() =>
        ProductLedgerLocalApprovalDecisionSnapshot.PendingPreviewOnly with
        {
            Decision = ProductLedgerLocalApprovalDecisionStoreDecision.LoadedLocalOnly,
            State = ProductLedgerLocalApprovalDecisionState.ApprovedLocalOnly,
            ApprovalId = "approval-handoff-draft-test-001",
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
            ExecutionId = "approved-no-op-execution-001",
            ApprovalId = "approval-handoff-draft-test-001",
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
            ExecutionId = "bounded-action-execution-001",
            ActionId = "bounded-internal-completion-marker-001",
            ActionKind = ProductLedgerLocalBoundedApprovedActionKind.BoundedInternalCompletionMarker.ToString(),
            ApprovalId = "approval-handoff-draft-test-001",
            NoOpExecutionId = "approved-no-op-execution-001",
            CandidateActionKind = ProductLedgerInternalCommandKind.ViewLedgerReadiness.ToString(),
            CandidateEvidenceHash = new string('a', 64),
            CandidateEvidenceHashPrefix = new string('a', 12),
            ResultHashPrefix = "boundedhash1",
            EvidenceReferences = ["docs/qa/nodal-os-approved-action-execution-local-only-no-op-to-bounded-action/report.md"],
            StatusText = ProductLedgerLocalBoundedApprovedActionExecutor.CompletedStatus
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

    private sealed class HandoffDraftFixture : IDisposable
    {
        private HandoffDraftFixture(string root, string outputRoot)
        {
            Root = root;
            OutputRoot = outputRoot;
            Executor = new ProductLedgerLocalApprovedHandoffReportDraftExecutor(new ProductLedgerLocalApprovedHandoffReportDraftOptions(
                OutputRootPath: outputRoot,
                ExplicitLocalApprovedHandoffDraftBoundary: true,
                AllowsArbitraryPathInput: false,
                AllowsFilesystemScan: false,
                AllowsOverwrite: false,
                AllowsUserFileWrite: false,
                AllowsShellOrSubprocess: false,
                AllowsCommandExecution: false,
                AllowsNetwork: false,
                AllowsDb: false,
                AllowsKmsWormExternalTrust: false,
                AllowsReleaseCommercial: false));
        }

        public string Root { get; }

        public string OutputRoot { get; }

        public string ExpectedReadyPath =>
            Path.Combine(OutputRoot, "local-approved-handoff-draft-handoff-draft-action-001-aaaaaaaaaaaa.md");

        public ProductLedgerLocalApprovedHandoffReportDraftExecutor Executor { get; }

        public string FullPath(ProductLedgerLocalApprovedHandoffReportDraftSnapshot snapshot) =>
            Path.Combine(OutputRoot, Path.GetFileName(snapshot.DraftRelativePath));

        public static HandoffDraftFixture Create()
        {
            var root = Path.Combine(RepoRoot(), ".tmp-product-ledger-handoff-draft-tests", Guid.NewGuid().ToString("N"));
            var outputRoot = Path.Combine(root, "docs", "test-output", "product-ledger", "approved-local-handoff-drafts");
            return new HandoffDraftFixture(root, outputRoot);
        }

        public void Dispose()
        {
            var tempRoot = Path.Combine(RepoRoot(), ".tmp-product-ledger-handoff-draft-tests");
            if (Directory.Exists(tempRoot))
            {
                Directory.Delete(tempRoot, recursive: true);
            }
        }
    }
}
