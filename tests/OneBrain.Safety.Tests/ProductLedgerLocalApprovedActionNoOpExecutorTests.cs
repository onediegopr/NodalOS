using Microsoft.VisualStudio.TestTools.UnitTesting;
using OneBrain.Core.Approval;

namespace OneBrain.Safety.Tests;

[TestClass]
[TestCategory("ApprovalHumanReview")]
[TestCategory("PhaseEApprovalHumanReview")]
public sealed class ProductLedgerLocalApprovedActionNoOpExecutorTests
{
    [TestMethod]
    public void ApprovedActionNoOpExecutor_CompletesOnlyApprovedLocalDecisionAsNoOp()
    {
        using var fixture = ExecutionFixture.Create();

        var result = fixture.Executor.ExecuteNoOp(ReadyRequest());

        Assert.AreEqual(ProductLedgerLocalApprovedActionExecutionDecision.NoOpExecutionCompletedLocalOnly, result.Decision);
        Assert.AreEqual(ProductLedgerLocalApprovedActionExecutionState.NoOpExecutionCompletedLocalOnly, result.State);
        Assert.AreEqual(0, result.Blockers.Count);
        Assert.IsTrue(result.LocalOnly);
        Assert.IsTrue(result.InternalOnly);
        Assert.IsTrue(result.DevelopmentOnly);
        Assert.IsTrue(result.DefaultOff);
        Assert.IsTrue(result.FailClosed);
        Assert.IsTrue(result.NoOpOnly);
        Assert.IsFalse(result.BoundedActionExecuted);
        Assert.IsFalse(result.ProductCommandExecuted);
        Assert.IsFalse(result.PublicUiActionAvailable);
        Assert.IsFalse(result.ProductCommandHandlerAvailable);
        Assert.IsFalse(result.ProductiveServiceRegistrationAvailable);
        Assert.IsFalse(result.PhysicalExportCreated);
        Assert.IsFalse(result.FileWriteOutsideExecutionStorePerformed);
        Assert.IsFalse(result.ProviderCloudNetworkAvailable);
        Assert.IsFalse(result.DbMigrationAvailable);
        Assert.IsFalse(result.KmsWormExternalTrustAvailable);
        Assert.IsFalse(result.BrowserCdpWcuOcrRecipesLiveAvailable);
        Assert.IsFalse(result.PilotRunAvailable);
        Assert.IsFalse(result.ReleaseCommercialReady);
        StringAssert.Contains(result.StatusText, "NO_OP_ONLY");
        StringAssert.Contains(result.StatusText, "NO_COMMAND_EXECUTION");
    }

    [TestMethod]
    public void ApprovedActionNoOpExecutor_BlocksPendingRejectedRequestChangesTamperAndMismatch()
    {
        var cases = new (ProductLedgerLocalApprovedActionExecutionRequest? Request, ProductLedgerLocalApprovedActionExecutionBlocker Blocker)[]
        {
            (null, ProductLedgerLocalApprovedActionExecutionBlocker.MissingRequest),
            (ReadyRequest() with { ApprovalDecision = ProductLedgerLocalApprovalDecisionSnapshot.PendingPreviewOnly }, ProductLedgerLocalApprovedActionExecutionBlocker.ApprovalDecisionNotApproved),
            (ReadyRequest(ProductLedgerLocalApprovalOperatorDecisionKind.Reject), ProductLedgerLocalApprovedActionExecutionBlocker.ApprovalDecisionNotApproved),
            (ReadyRequest(ProductLedgerLocalApprovalOperatorDecisionKind.RequestChanges), ProductLedgerLocalApprovedActionExecutionBlocker.ApprovalDecisionNotApproved),
            (ReadyRequest() with { CurrentEvidenceHash = new string('b', 64) }, ProductLedgerLocalApprovedActionExecutionBlocker.CandidateEvidenceHashMismatch),
            (ReadyRequest() with { CandidateActionKind = ProductLedgerInternalCommandKind.ViewDiagnostics }, ProductLedgerLocalApprovedActionExecutionBlocker.CandidateActionMismatch),
            (ReadyRequest() with { EvidenceReferences = [] }, ProductLedgerLocalApprovedActionExecutionBlocker.MissingEvidenceReferences)
        };

        foreach (var testCase in cases)
        {
            using var fixture = ExecutionFixture.Create();
            var result = fixture.Executor.ExecuteNoOp(testCase.Request);

            Assert.AreEqual(ProductLedgerLocalApprovedActionExecutionDecision.Rejected, result.Decision, testCase.Blocker.ToString());
            CollectionAssert.Contains(result.Blockers.ToArray(), testCase.Blocker, testCase.Blocker.ToString());
            Assert.IsFalse(result.ProductCommandExecuted);
            Assert.IsFalse(result.PublicUiActionAvailable);
            Assert.IsFalse(result.ProductCommandHandlerAvailable);
            Assert.IsFalse(result.PhysicalExportCreated);
        }
    }

    [TestMethod]
    public void ApprovedActionNoOpExecutor_BlocksAuthorityBoundaryClaims()
    {
        var ready = ReadyRequest();
        var cases = new Dictionary<ProductLedgerLocalApprovedActionExecutionRequest, ProductLedgerLocalApprovedActionExecutionBlocker>
        {
            [ready with { ExplicitLocalOnlyNoOpExecutionScope = false }] = ProductLedgerLocalApprovedActionExecutionBlocker.MissingExplicitLocalOnlyNoOpExecutionScope,
            [ready with { DevelopmentMode = false }] = ProductLedgerLocalApprovedActionExecutionBlocker.NonDevelopmentMode,
            [ready with { LocalMode = false }] = ProductLedgerLocalApprovedActionExecutionBlocker.NonLocalMode,
            [ready with { InternalMode = false }] = ProductLedgerLocalApprovedActionExecutionBlocker.NonInternalMode,
            [ready with { RequestsBoundedAction = true }] = ProductLedgerLocalApprovedActionExecutionBlocker.RequestsBoundedActionWithoutSeparateGate,
            [ready with { RequestsPublicUiAction = true }] = ProductLedgerLocalApprovedActionExecutionBlocker.RequestsPublicUiAction,
            [ready with { RequestsProductCommandExecution = true }] = ProductLedgerLocalApprovedActionExecutionBlocker.RequestsProductCommandExecution,
            [ready with { RequestsProductCommandHandler = true }] = ProductLedgerLocalApprovedActionExecutionBlocker.RequestsProductCommandHandler,
            [ready with { RequestsProductiveServiceRegistration = true }] = ProductLedgerLocalApprovedActionExecutionBlocker.RequestsProductiveServiceRegistration,
            [ready with { RequestsPhysicalExport = true }] = ProductLedgerLocalApprovedActionExecutionBlocker.RequestsPhysicalExport,
            [ready with { RequestsFileWriteOutsideExecutionStore = true }] = ProductLedgerLocalApprovedActionExecutionBlocker.RequestsFileWriteOutsideExecutionStore,
            [ready with { ClaimsArbitraryPathInput = true }] = ProductLedgerLocalApprovedActionExecutionBlocker.ClaimsArbitraryPathInput,
            [ready with { ClaimsFilesystemScan = true }] = ProductLedgerLocalApprovedActionExecutionBlocker.ClaimsFilesystemScan,
            [ready with { ClaimsProviderCloudNetwork = true }] = ProductLedgerLocalApprovedActionExecutionBlocker.ClaimsProviderCloudNetwork,
            [ready with { ClaimsDbMigration = true }] = ProductLedgerLocalApprovedActionExecutionBlocker.ClaimsDbMigration,
            [ready with { ClaimsKmsWormExternalTrust = true }] = ProductLedgerLocalApprovedActionExecutionBlocker.ClaimsKmsWormExternalTrust,
            [ready with { ClaimsBrowserCdpWcuOcrRecipesLive = true }] = ProductLedgerLocalApprovedActionExecutionBlocker.ClaimsBrowserCdpWcuOcrRecipesLive,
            [ready with { ClaimsPilotRun = true }] = ProductLedgerLocalApprovedActionExecutionBlocker.ClaimsPilotRun,
            [ready with { ClaimsReleaseCommercial = true }] = ProductLedgerLocalApprovedActionExecutionBlocker.ClaimsReleaseCommercial
        };

        foreach (var testCase in cases)
        {
            using var fixture = ExecutionFixture.Create();
            var result = fixture.Executor.ExecuteNoOp(testCase.Key);

            Assert.AreEqual(ProductLedgerLocalApprovedActionExecutionDecision.Rejected, result.Decision, testCase.Value.ToString());
            CollectionAssert.Contains(result.Blockers.ToArray(), testCase.Value, testCase.Value.ToString());
        }
    }

    [TestMethod]
    public void ApprovedActionNoOpExecutor_ReplaysSameExecutionIdempotentlyAndBlocksConflict()
    {
        using var fixture = ExecutionFixture.Create();
        var first = fixture.Executor.ExecuteNoOp(ReadyRequest());
        var replay = fixture.Executor.ExecuteNoOp(ReadyRequest());
        var conflict = fixture.Executor.ExecuteNoOp(ReadyRequest() with { ExecutionId = "approved-no-op-execution-002" });

        Assert.AreEqual(ProductLedgerLocalApprovedActionExecutionDecision.NoOpExecutionCompletedLocalOnly, first.Decision);
        Assert.AreEqual(ProductLedgerLocalApprovedActionExecutionDecision.IdempotentReplay, replay.Decision);
        Assert.AreEqual(ProductLedgerLocalApprovedActionExecutionDecision.Rejected, conflict.Decision);
        CollectionAssert.Contains(conflict.Blockers.ToArray(), ProductLedgerLocalApprovedActionExecutionBlocker.ExistingExecutionConflict);
    }

    [TestMethod]
    public void ApprovedActionNoOpExecutor_ReadFailsClosedOnTamperedExecutionStore()
    {
        using var fixture = ExecutionFixture.Create();
        var result = fixture.Executor.ExecuteNoOp(ReadyRequest());

        Assert.AreEqual(ProductLedgerLocalApprovedActionExecutionDecision.NoOpExecutionCompletedLocalOnly, result.Decision);
        File.WriteAllText(fixture.StateFilePath, "{not-json");

        var loaded = fixture.Executor.Read();

        Assert.AreEqual(ProductLedgerLocalApprovedActionExecutionDecision.Rejected, loaded.Decision);
        Assert.AreEqual(ProductLedgerLocalApprovedActionExecutionState.ExecutionTamperBlocked, loaded.State);
        CollectionAssert.Contains(loaded.Blockers.ToArray(), ProductLedgerLocalApprovedActionExecutionBlocker.StoreTamperedOrCorrupt);
    }

    [TestMethod]
    public void ApprovedActionNoOpExecutor_SourceHasNoShellCommandNetworkDbLiveAutomationOrProductActionPath()
    {
        var source = ReadRepoFile("src", "OneBrain.Core", "Approval", "ProductLedgerLocalApprovedActionNoOpExecutor.cs");
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
            "ReleaseCommercialReady: true"
        })
        {
            Assert.IsFalse(source.Contains(fragment, StringComparison.Ordinal), fragment);
        }

        StringAssert.Contains(source, "File.WriteAllText");
        StringAssert.Contains(source, "NO_OP_ONLY");
    }

    private static ProductLedgerLocalApprovedActionExecutionRequest ReadyRequest(
        ProductLedgerLocalApprovalOperatorDecisionKind decision = ProductLedgerLocalApprovalOperatorDecisionKind.Approve) =>
        new(
            ExplicitLocalOnlyNoOpExecutionScope: true,
            DevelopmentMode: true,
            LocalMode: true,
            InternalMode: true,
            ExecutionId: "approved-no-op-execution-001",
            ApprovalDecision: ReadyApproval(decision),
            CandidateActionKind: ProductLedgerInternalCommandKind.ViewLedgerReadiness,
            CandidateEvidenceHash: new string('a', 64),
            CurrentEvidenceHash: new string('a', 64),
            EvidenceReferences:
            [
                "docs/qa/nodal-os-local-approval-real-operator-input-state-persistence/report.md",
                "docs/qa/nodal-os-local-approval-decision-state-external-audit-read-only/report.md"
            ],
            RequestsBoundedAction: false,
            RequestsPublicUiAction: false,
            RequestsProductCommandExecution: false,
            RequestsProductCommandHandler: false,
            RequestsProductiveServiceRegistration: false,
            RequestsPhysicalExport: false,
            RequestsFileWriteOutsideExecutionStore: false,
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
            ApprovalId = "approval-no-op-test-001",
            CandidateActionKind = ProductLedgerInternalCommandKind.ViewLedgerReadiness.ToString(),
            CandidateEvidenceHash = new string('a', 64),
            CandidateEvidenceHashPrefix = new string('a', 12),
            OperatorDecision = decision.ToString(),
            EvidenceReferences = [
                "docs/qa/nodal-os-local-approval-real-operator-input-state-persistence/report.md"
            ],
            StatusText = ProductLedgerLocalApprovalDecisionStateStore.PersistedStatus
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

    private sealed class ExecutionFixture : IDisposable
    {
        private const string StateFileName = "product-ledger-local-approved-no-op-execution.json";

        private ExecutionFixture(string root)
        {
            Root = root;
            Executor = new ProductLedgerLocalApprovedActionNoOpExecutor(new ProductLedgerLocalApprovedActionExecutionStoreOptions(
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

        public ProductLedgerLocalApprovedActionNoOpExecutor Executor { get; }

        public static ExecutionFixture Create()
        {
            var root = Path.Combine(RepoRoot(), ".tmp-product-ledger-approved-no-op-execution-tests", Guid.NewGuid().ToString("N"));
            return new ExecutionFixture(root);
        }

        public void Dispose()
        {
            var tempRoot = Path.Combine(RepoRoot(), ".tmp-product-ledger-approved-no-op-execution-tests");
            if (Directory.Exists(tempRoot))
            {
                Directory.Delete(tempRoot, recursive: true);
            }
        }
    }
}
