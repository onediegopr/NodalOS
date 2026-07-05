using Microsoft.VisualStudio.TestTools.UnitTesting;
using OneBrain.Core.Approval;

namespace OneBrain.Safety.Tests;

[TestClass]
[TestCategory("ApprovalHumanReview")]
[TestCategory("PhaseEApprovalHumanReview")]
public sealed class ProductLedgerLocalBoundedApprovedActionExecutorTests
{
    [TestMethod]
    public void BoundedApprovedActionExecutor_RecordsOnlyBoundedInternalCompletionMarkerAfterApprovedNoOp()
    {
        using var fixture = BoundedFixture.Create();

        var result = fixture.Executor.ExecuteBoundedCompletionMarker(ReadyRequest());

        Assert.AreEqual(ProductLedgerLocalBoundedApprovedActionDecision.BoundedLocalCompletionRecorded, result.Decision);
        Assert.AreEqual(ProductLedgerLocalBoundedApprovedActionState.BoundedExecutionCompletedLocalOnly, result.State);
        Assert.AreEqual(0, result.Blockers.Count);
        Assert.IsTrue(result.LocalOnly);
        Assert.IsTrue(result.InternalOnly);
        Assert.IsTrue(result.DevelopmentOnly);
        Assert.IsTrue(result.DefaultOff);
        Assert.IsTrue(result.FailClosed);
        Assert.IsTrue(result.NonDestructive);
        Assert.IsTrue(result.BoundedInternalCompletionMarker);
        Assert.IsFalse(result.TouchesUserFiles);
        Assert.IsFalse(result.ShellOrSubprocessAllowed);
        Assert.IsFalse(result.ArbitraryCommandExecutionAllowed);
        Assert.IsFalse(result.ProductCommandExecuted);
        Assert.IsFalse(result.PublicUiActionAvailable);
        Assert.IsFalse(result.ProductCommandHandlerAvailable);
        Assert.IsFalse(result.FileWriteOutsideExecutionStorePerformed);
        Assert.IsFalse(result.ProviderCloudNetworkAvailable);
        Assert.IsFalse(result.DbMigrationAvailable);
        Assert.IsFalse(result.KmsWormExternalTrustAvailable);
        Assert.IsFalse(result.BrowserCdpWcuOcrRecipesLiveAvailable);
        Assert.IsFalse(result.PilotRunAvailable);
        Assert.IsFalse(result.ReleaseCommercialReady);
        StringAssert.Contains(result.StatusText, "BOUNDED_INTERNAL_COMPLETION_MARKER");
        StringAssert.Contains(result.StatusText, "NO_USER_FILE_WRITE");
        StringAssert.Contains(result.StatusText, "NO_COMMAND_EXECUTION");
    }

    [TestMethod]
    public void BoundedApprovedActionExecutor_BlocksMissingNoOpPendingRejectedMismatchAndUnknownAction()
    {
        var cases = new (ProductLedgerLocalBoundedApprovedActionRequest? Request, ProductLedgerLocalBoundedApprovedActionBlocker Blocker)[]
        {
            (null, ProductLedgerLocalBoundedApprovedActionBlocker.MissingRequest),
            (ReadyRequest() with { ApprovalDecision = ProductLedgerLocalApprovalDecisionSnapshot.PendingPreviewOnly }, ProductLedgerLocalBoundedApprovedActionBlocker.ApprovalDecisionNotApproved),
            (ReadyRequest(ProductLedgerLocalApprovalOperatorDecisionKind.Reject), ProductLedgerLocalBoundedApprovedActionBlocker.ApprovalDecisionNotApproved),
            (ReadyRequest(ProductLedgerLocalApprovalOperatorDecisionKind.RequestChanges), ProductLedgerLocalBoundedApprovedActionBlocker.ApprovalDecisionNotApproved),
            (ReadyRequest() with { NoOpExecution = ProductLedgerLocalApprovedActionExecutionSnapshot.Pending }, ProductLedgerLocalBoundedApprovedActionBlocker.NoOpExecutionNotCompleted),
            (ReadyRequest() with { CurrentEvidenceHash = new string('b', 64) }, ProductLedgerLocalBoundedApprovedActionBlocker.CandidateEvidenceHashMismatch),
            (ReadyRequest() with { CandidateActionKind = ProductLedgerInternalCommandKind.ViewDiagnostics }, ProductLedgerLocalBoundedApprovedActionBlocker.CandidateActionMismatch),
            (ReadyRequest() with { ActionKind = null }, ProductLedgerLocalBoundedApprovedActionBlocker.UnknownActionKind),
            (ReadyRequest() with { EvidenceReferences = [] }, ProductLedgerLocalBoundedApprovedActionBlocker.MissingEvidenceReferences)
        };

        foreach (var testCase in cases)
        {
            using var fixture = BoundedFixture.Create();
            var result = fixture.Executor.ExecuteBoundedCompletionMarker(testCase.Request);

            Assert.AreEqual(ProductLedgerLocalBoundedApprovedActionDecision.Rejected, result.Decision, testCase.Blocker.ToString());
            CollectionAssert.Contains(result.Blockers.ToArray(), testCase.Blocker, testCase.Blocker.ToString());
            Assert.IsFalse(result.ProductCommandExecuted);
            Assert.IsFalse(result.PublicUiActionAvailable);
            Assert.IsFalse(result.TouchesUserFiles);
        }
    }

    [TestMethod]
    public void BoundedApprovedActionExecutor_BlocksAuthorityPayloadAndBoundaryClaims()
    {
        var ready = ReadyRequest();
        var cases = new Dictionary<ProductLedgerLocalBoundedApprovedActionRequest, ProductLedgerLocalBoundedApprovedActionBlocker>
        {
            [ready with { ExplicitLocalBoundedActionScope = false }] = ProductLedgerLocalBoundedApprovedActionBlocker.MissingExplicitLocalBoundedActionScope,
            [ready with { DevelopmentMode = false }] = ProductLedgerLocalBoundedApprovedActionBlocker.NonDevelopmentMode,
            [ready with { LocalMode = false }] = ProductLedgerLocalBoundedApprovedActionBlocker.NonLocalMode,
            [ready with { InternalMode = false }] = ProductLedgerLocalBoundedApprovedActionBlocker.NonInternalMode,
            [ready with { ProposedPath = @"C:\Users\fixture\unsafe.txt" }] = ProductLedgerLocalBoundedApprovedActionBlocker.ContainsPathCommandOrNetworkPayload,
            [ready with { ProposedCommand = "cmd /c whoami" }] = ProductLedgerLocalBoundedApprovedActionBlocker.ContainsPathCommandOrNetworkPayload,
            [ready with { ProposedUrl = "https://local.invalid/action" }] = ProductLedgerLocalBoundedApprovedActionBlocker.ContainsPathCommandOrNetworkPayload,
            [ready with { RequestsPublicUiAction = true }] = ProductLedgerLocalBoundedApprovedActionBlocker.RequestsPublicUiAction,
            [ready with { RequestsProductCommandExecution = true }] = ProductLedgerLocalBoundedApprovedActionBlocker.RequestsProductCommandExecution,
            [ready with { RequestsProductCommandHandler = true }] = ProductLedgerLocalBoundedApprovedActionBlocker.RequestsProductCommandHandler,
            [ready with { RequestsProductiveServiceRegistration = true }] = ProductLedgerLocalBoundedApprovedActionBlocker.RequestsProductiveServiceRegistration,
            [ready with { RequestsPhysicalExport = true }] = ProductLedgerLocalBoundedApprovedActionBlocker.RequestsPhysicalExport,
            [ready with { RequestsFileWriteOutsideExecutionStore = true }] = ProductLedgerLocalBoundedApprovedActionBlocker.RequestsFileWriteOutsideExecutionStore,
            [ready with { RequestsUserFileWrite = true }] = ProductLedgerLocalBoundedApprovedActionBlocker.RequestsUserFileWrite,
            [ready with { RequestsShellOrSubprocess = true }] = ProductLedgerLocalBoundedApprovedActionBlocker.RequestsShellOrSubprocess,
            [ready with { ClaimsArbitraryCommandExecution = true }] = ProductLedgerLocalBoundedApprovedActionBlocker.ClaimsArbitraryCommandExecution,
            [ready with { ClaimsArbitraryPathInput = true }] = ProductLedgerLocalBoundedApprovedActionBlocker.ClaimsArbitraryPathInput,
            [ready with { ClaimsFilesystemScan = true }] = ProductLedgerLocalBoundedApprovedActionBlocker.ClaimsFilesystemScan,
            [ready with { ClaimsProviderCloudNetwork = true }] = ProductLedgerLocalBoundedApprovedActionBlocker.ClaimsProviderCloudNetwork,
            [ready with { ClaimsDbMigration = true }] = ProductLedgerLocalBoundedApprovedActionBlocker.ClaimsDbMigration,
            [ready with { ClaimsKmsWormExternalTrust = true }] = ProductLedgerLocalBoundedApprovedActionBlocker.ClaimsKmsWormExternalTrust,
            [ready with { ClaimsBrowserCdpWcuOcrRecipesLive = true }] = ProductLedgerLocalBoundedApprovedActionBlocker.ClaimsBrowserCdpWcuOcrRecipesLive,
            [ready with { ClaimsPilotRun = true }] = ProductLedgerLocalBoundedApprovedActionBlocker.ClaimsPilotRun,
            [ready with { ClaimsReleaseCommercial = true }] = ProductLedgerLocalBoundedApprovedActionBlocker.ClaimsReleaseCommercial
        };

        foreach (var testCase in cases)
        {
            using var fixture = BoundedFixture.Create();
            var result = fixture.Executor.ExecuteBoundedCompletionMarker(testCase.Key);

            Assert.AreEqual(ProductLedgerLocalBoundedApprovedActionDecision.Rejected, result.Decision, testCase.Value.ToString());
            CollectionAssert.Contains(result.Blockers.ToArray(), testCase.Value, testCase.Value.ToString());
        }
    }

    [TestMethod]
    public void BoundedApprovedActionExecutor_ReplaysSameMarkerIdempotentlyAndBlocksConflict()
    {
        using var fixture = BoundedFixture.Create();
        var first = fixture.Executor.ExecuteBoundedCompletionMarker(ReadyRequest());
        var replay = fixture.Executor.ExecuteBoundedCompletionMarker(ReadyRequest());
        var conflict = fixture.Executor.ExecuteBoundedCompletionMarker(ReadyRequest() with { ExecutionId = "bounded-action-execution-002" });

        Assert.AreEqual(ProductLedgerLocalBoundedApprovedActionDecision.BoundedLocalCompletionRecorded, first.Decision);
        Assert.AreEqual(ProductLedgerLocalBoundedApprovedActionDecision.IdempotentReplay, replay.Decision);
        Assert.AreEqual(ProductLedgerLocalBoundedApprovedActionDecision.Rejected, conflict.Decision);
        CollectionAssert.Contains(conflict.Blockers.ToArray(), ProductLedgerLocalBoundedApprovedActionBlocker.ExistingBoundedActionConflict);
    }

    [TestMethod]
    public void BoundedApprovedActionExecutor_ReadFailsClosedOnTamperedStore()
    {
        using var fixture = BoundedFixture.Create();
        var result = fixture.Executor.ExecuteBoundedCompletionMarker(ReadyRequest());

        Assert.AreEqual(ProductLedgerLocalBoundedApprovedActionDecision.BoundedLocalCompletionRecorded, result.Decision);
        File.WriteAllText(fixture.StateFilePath, "{not-json");

        var loaded = fixture.Executor.Read();

        Assert.AreEqual(ProductLedgerLocalBoundedApprovedActionDecision.Rejected, loaded.Decision);
        Assert.AreEqual(ProductLedgerLocalBoundedApprovedActionState.BoundedExecutionTamperBlocked, loaded.State);
        CollectionAssert.Contains(loaded.Blockers.ToArray(), ProductLedgerLocalBoundedApprovedActionBlocker.StoreTamperedOrCorrupt);
    }

    [TestMethod]
    public void BoundedApprovedActionExecutor_SourceHasNoShellCommandNetworkDbLiveAutomationOrProductActionPath()
    {
        var source = ReadRepoFile("src", "OneBrain.Core", "Approval", "ProductLedgerLocalBoundedApprovedActionExecutor.cs");
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
            "ProductLedgerLocalReportExportService",
            ".Append(",
            ".Export(",
            "Directory.GetFiles",
            "Directory.EnumerateFiles",
            "PublicUiActionAvailable: true",
            "ProductCommandHandlerAvailable: true",
            "ProductCommandExecuted: true",
            "TouchesUserFiles: true",
            "ReleaseCommercialReady: true"
        })
        {
            Assert.IsFalse(source.Contains(fragment, StringComparison.Ordinal), fragment);
        }

        StringAssert.Contains(source, "File.WriteAllText");
        StringAssert.Contains(source, "BOUNDED_INTERNAL_COMPLETION_MARKER");
        StringAssert.Contains(source, "NO_USER_FILE_WRITE");
    }

    private static ProductLedgerLocalBoundedApprovedActionRequest ReadyRequest(
        ProductLedgerLocalApprovalOperatorDecisionKind decision = ProductLedgerLocalApprovalOperatorDecisionKind.Approve) =>
        new(
            ExplicitLocalBoundedActionScope: true,
            DevelopmentMode: true,
            LocalMode: true,
            InternalMode: true,
            ExecutionId: "bounded-action-execution-001",
            ActionId: "bounded-internal-completion-marker-001",
            ActionKind: ProductLedgerLocalBoundedApprovedActionKind.BoundedInternalCompletionMarker,
            ApprovalDecision: ReadyApproval(decision),
            NoOpExecution: ReadyNoOpExecution(),
            CandidateActionKind: ProductLedgerInternalCommandKind.ViewLedgerReadiness,
            CandidateEvidenceHash: new string('a', 64),
            CurrentEvidenceHash: new string('a', 64),
            EvidenceReferences:
            [
                "docs/qa/nodal-os-approved-action-execution-local-only-no-op-to-bounded-action/report.md"
            ],
            ProposedPath: null,
            ProposedCommand: null,
            ProposedUrl: null,
            RequestsPublicUiAction: false,
            RequestsProductCommandExecution: false,
            RequestsProductCommandHandler: false,
            RequestsProductiveServiceRegistration: false,
            RequestsPhysicalExport: false,
            RequestsFileWriteOutsideExecutionStore: false,
            RequestsUserFileWrite: false,
            RequestsShellOrSubprocess: false,
            ClaimsArbitraryCommandExecution: false,
            ClaimsArbitraryPathInput: false,
            ClaimsFilesystemScan: false,
            ClaimsProviderCloudNetwork: false,
            ClaimsDbMigration: false,
            ClaimsKmsWormExternalTrust: false,
            ClaimsBrowserCdpWcuOcrRecipesLive: false,
            ClaimsPilotRun: false,
            ClaimsReleaseCommercial: false);

    private static ProductLedgerLocalApprovalDecisionSnapshot ReadyApproval(
        ProductLedgerLocalApprovalOperatorDecisionKind decision) =>
        ProductLedgerLocalApprovalDecisionSnapshot.PendingPreviewOnly with
        {
            Decision = ProductLedgerLocalApprovalDecisionStoreDecision.LoadedLocalOnly,
            State = decision switch
            {
                ProductLedgerLocalApprovalOperatorDecisionKind.Approve => ProductLedgerLocalApprovalDecisionState.ApprovedLocalOnly,
                ProductLedgerLocalApprovalOperatorDecisionKind.Reject => ProductLedgerLocalApprovalDecisionState.RejectedLocalOnly,
                _ => ProductLedgerLocalApprovalDecisionState.ChangesRequestedLocalOnly
            },
            ApprovalId = "approval-bounded-test-001",
            CandidateActionKind = ProductLedgerInternalCommandKind.ViewLedgerReadiness.ToString(),
            CandidateEvidenceHash = new string('a', 64),
            CandidateEvidenceHashPrefix = new string('a', 12),
            OperatorDecision = decision.ToString(),
            EvidenceReferences = ["docs/qa/nodal-os-local-approval-real-operator-input-state-persistence/report.md"],
            StatusText = ProductLedgerLocalApprovalDecisionStateStore.PersistedStatus
        };

    private static ProductLedgerLocalApprovedActionExecutionSnapshot ReadyNoOpExecution() =>
        ProductLedgerLocalApprovedActionExecutionSnapshot.Pending with
        {
            Decision = ProductLedgerLocalApprovedActionExecutionDecision.LoadedLocalOnly,
            State = ProductLedgerLocalApprovedActionExecutionState.NoOpExecutionCompletedLocalOnly,
            ExecutionId = "approved-no-op-execution-001",
            ApprovalId = "approval-bounded-test-001",
            CandidateActionKind = ProductLedgerInternalCommandKind.ViewLedgerReadiness.ToString(),
            CandidateEvidenceHash = new string('a', 64),
            CandidateEvidenceHashPrefix = new string('a', 12),
            ExecutionResultHashPrefix = "noophash0001",
            EvidenceReferences = ["docs/qa/nodal-os-approved-action-execution-local-only-no-op-to-bounded-action/report.md"],
            StatusText = ProductLedgerLocalApprovedActionNoOpExecutor.CompletedStatus
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

    private sealed class BoundedFixture : IDisposable
    {
        private const string StateFileName = "product-ledger-local-bounded-approved-action.json";

        private BoundedFixture(string root)
        {
            Root = root;
            Executor = new ProductLedgerLocalBoundedApprovedActionExecutor(new ProductLedgerLocalApprovedActionExecutionStoreOptions(
                StoreRootPath: root,
                ExplicitLocalOnlyExecutionStore: true,
                AllowsArbitraryPathInput: false,
                AllowsFilesystemScan: false,
                AllowsExport: false,
                AllowsNetwork: false,
                AllowsDb: false,
                AllowsReleaseCommercial: false));
        }

        public string Root { get; }

        public string StateFilePath => Path.Combine(Root, StateFileName);

        public ProductLedgerLocalBoundedApprovedActionExecutor Executor { get; }

        public static BoundedFixture Create()
        {
            var root = Path.Combine(RepoRoot(), ".tmp-product-ledger-bounded-approved-action-tests", Guid.NewGuid().ToString("N"));
            return new BoundedFixture(root);
        }

        public void Dispose()
        {
            var tempRoot = Path.Combine(RepoRoot(), ".tmp-product-ledger-bounded-approved-action-tests");
            if (Directory.Exists(tempRoot))
            {
                Directory.Delete(tempRoot, recursive: true);
            }
        }
    }
}
