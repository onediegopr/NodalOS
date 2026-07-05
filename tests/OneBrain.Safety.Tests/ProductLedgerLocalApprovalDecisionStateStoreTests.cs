using Microsoft.VisualStudio.TestTools.UnitTesting;
using OneBrain.Core.Approval;

namespace OneBrain.Safety.Tests;

[TestClass]
[TestCategory("ApprovalHumanReview")]
[TestCategory("PhaseEApprovalHumanReview")]
public sealed class ProductLedgerLocalApprovalDecisionStateStoreTests
{
    [TestMethod]
    public void LocalApprovalDecisionStateStore_PersistsApproveRejectAndChangesRequestedLocalOnly()
    {
        foreach (var decision in new[]
        {
            ProductLedgerLocalApprovalOperatorDecisionKind.Approve,
            ProductLedgerLocalApprovalOperatorDecisionKind.Reject,
            ProductLedgerLocalApprovalOperatorDecisionKind.RequestChanges
        })
        {
            using var fixture = StoreFixture.Create();
            var result = fixture.Store.Persist(ReadyRequest(decision));

            Assert.AreEqual(ProductLedgerLocalApprovalDecisionStoreDecision.PersistedLocalOnly, result.Decision);
            Assert.AreEqual(ExpectedState(decision), result.State);
            Assert.AreEqual(0, result.Blockers.Count);
            Assert.IsTrue(result.LocalOnly);
            Assert.IsTrue(result.InternalOnly);
            Assert.IsTrue(result.DefaultOff);
            Assert.IsTrue(result.FailClosed);
            Assert.IsFalse(result.ProductCommandExecuted);
            Assert.IsFalse(result.PublicUiActionAvailable);
            Assert.IsFalse(result.ProductCommandHandlerAvailable);
            Assert.IsFalse(result.ProductiveServiceRegistrationAvailable);
            Assert.IsFalse(result.PhysicalExportCreated);
            Assert.IsFalse(result.FileWriteOutsideApprovalStateStorePerformed);
            Assert.IsFalse(result.ProviderCloudNetworkAvailable);
            Assert.IsFalse(result.DbMigrationAvailable);
            Assert.IsFalse(result.KmsWormExternalTrustAvailable);
            Assert.IsFalse(result.BrowserCdpWcuOcrRecipesLiveAvailable);
            Assert.IsFalse(result.PilotRunAvailable);
            Assert.IsFalse(result.ReleaseCommercialReady);
            StringAssert.Contains(result.StatusText, "NO_COMMAND_EXECUTION");
            StringAssert.Contains(result.StatusText, "NO_PUBLIC_UI_ACTION");
            StringAssert.Contains(result.StatusText, "NO_PRODUCT_COMMAND_HANDLER");
        }
    }

    [TestMethod]
    public void LocalApprovalDecisionStateStore_ReplaysSameDecisionIdempotentlyAndBlocksConflict()
    {
        using var fixture = StoreFixture.Create();
        var first = fixture.Store.Persist(ReadyRequest(ProductLedgerLocalApprovalOperatorDecisionKind.Approve));
        var replay = fixture.Store.Persist(ReadyRequest(ProductLedgerLocalApprovalOperatorDecisionKind.Approve));
        var conflict = fixture.Store.Persist(ReadyRequest(ProductLedgerLocalApprovalOperatorDecisionKind.Reject));

        Assert.AreEqual(ProductLedgerLocalApprovalDecisionStoreDecision.PersistedLocalOnly, first.Decision);
        Assert.AreEqual(ProductLedgerLocalApprovalDecisionStoreDecision.IdempotentReplay, replay.Decision);
        Assert.AreEqual(ProductLedgerLocalApprovalDecisionStoreDecision.Rejected, conflict.Decision);
        CollectionAssert.Contains(
            conflict.Blockers.ToArray(),
            ProductLedgerLocalApprovalDecisionBlocker.ExistingDecisionConflict);
    }

    [TestMethod]
    public void LocalApprovalDecisionStateStore_FailsClosedOnMissingMalformedUnsafeOrMismatchedInput()
    {
        using var fixture = StoreFixture.Create();
        var cases = new (ProductLedgerLocalApprovalDecisionStateRequest? Request, ProductLedgerLocalApprovalDecisionBlocker Blocker)[]
        {
            (null, ProductLedgerLocalApprovalDecisionBlocker.MissingRequest),
            (ReadyRequest(ProductLedgerLocalApprovalOperatorDecisionKind.Approve) with { ExplicitLocalOnlyStatePersistenceScope = false }, ProductLedgerLocalApprovalDecisionBlocker.MissingExplicitLocalOnlyStatePersistenceScope),
            (ReadyRequest(ProductLedgerLocalApprovalOperatorDecisionKind.Approve) with { ApprovalId = " " }, ProductLedgerLocalApprovalDecisionBlocker.MissingApprovalId),
            (ReadyRequest(ProductLedgerLocalApprovalOperatorDecisionKind.Approve) with { CandidateResult = null }, ProductLedgerLocalApprovalDecisionBlocker.MissingCandidate),
            (ReadyRequest(ProductLedgerLocalApprovalOperatorDecisionKind.Approve) with { CurrentEvidenceHash = new string('b', 64) }, ProductLedgerLocalApprovalDecisionBlocker.CandidateEvidenceHashMismatch),
            (ReadyRequest(ProductLedgerLocalApprovalOperatorDecisionKind.Approve) with { OperatorDecision = null }, ProductLedgerLocalApprovalDecisionBlocker.MissingOperatorDecision),
            (ReadyRequest(ProductLedgerLocalApprovalOperatorDecisionKind.Approve) with { EvidenceReferences = [] }, ProductLedgerLocalApprovalDecisionBlocker.MissingEvidenceReferences),
            (ReadyRequest(ProductLedgerLocalApprovalOperatorDecisionKind.Approve) with { OperatorNote = "password=super-secret" }, ProductLedgerLocalApprovalDecisionBlocker.UnsafeOperatorNote),
            (ReadyRequest(ProductLedgerLocalApprovalOperatorDecisionKind.Approve) with { RequestsProductCommandExecution = true }, ProductLedgerLocalApprovalDecisionBlocker.ClaimsProductCommandExecution),
            (ReadyRequest(ProductLedgerLocalApprovalOperatorDecisionKind.Approve) with { ClaimsArbitraryPathInput = true }, ProductLedgerLocalApprovalDecisionBlocker.ClaimsArbitraryPathInput)
        };

        foreach (var testCase in cases)
        {
            var result = fixture.Store.Persist(testCase.Request);

            Assert.AreEqual(ProductLedgerLocalApprovalDecisionStoreDecision.Rejected, result.Decision, testCase.Blocker.ToString());
            CollectionAssert.Contains(result.Blockers.ToArray(), testCase.Blocker, testCase.Blocker.ToString());
            Assert.IsFalse(result.ProductCommandExecuted);
            Assert.IsFalse(result.PublicUiActionAvailable);
            Assert.IsFalse(result.ProductCommandHandlerAvailable);
            Assert.IsFalse(result.PhysicalExportCreated);
        }
    }

    [TestMethod]
    public void LocalApprovalDecisionStateStore_RedactsEmailAndWindowsPathBeforePersistence()
    {
        using var fixture = StoreFixture.Create();
        var result = fixture.Store.Persist(
            ReadyRequest(ProductLedgerLocalApprovalOperatorDecisionKind.Approve) with
            {
                OperatorNote = "Approved by user@example.com after checking C:\\Users\\Diego\\secret.txt"
            });
        var loaded = fixture.Store.Read();
        var persisted = File.ReadAllText(fixture.StateFilePath);

        Assert.AreEqual(ProductLedgerLocalApprovalDecisionStoreDecision.PersistedLocalOnly, result.Decision);
        StringAssert.Contains(loaded.RedactedOperatorNote, "[redacted-email]");
        StringAssert.Contains(loaded.RedactedOperatorNote, "[redacted-path]");
        Assert.IsFalse(persisted.Contains("user@example.com", StringComparison.OrdinalIgnoreCase));
        Assert.IsFalse(persisted.Contains("C:\\Users\\Diego", StringComparison.OrdinalIgnoreCase));
    }

    [TestMethod]
    public void LocalApprovalDecisionStateStore_ReadFailsClosedOnTamperedStateFile()
    {
        using var fixture = StoreFixture.Create();
        var result = fixture.Store.Persist(ReadyRequest(ProductLedgerLocalApprovalOperatorDecisionKind.Approve));

        Assert.AreEqual(ProductLedgerLocalApprovalDecisionStoreDecision.PersistedLocalOnly, result.Decision);
        File.WriteAllText(fixture.StateFilePath, "{not-json");

        var loaded = fixture.Store.Read();

        Assert.AreEqual(ProductLedgerLocalApprovalDecisionStoreDecision.Rejected, loaded.Decision);
        CollectionAssert.Contains(
            loaded.Blockers.ToArray(),
            ProductLedgerLocalApprovalDecisionBlocker.StoreTamperedOrCorrupt);
    }

    private static ProductLedgerLocalApprovalDecisionState ExpectedState(
        ProductLedgerLocalApprovalOperatorDecisionKind decision) =>
        decision switch
        {
            ProductLedgerLocalApprovalOperatorDecisionKind.Approve => ProductLedgerLocalApprovalDecisionState.ApprovedLocalOnly,
            ProductLedgerLocalApprovalOperatorDecisionKind.Reject => ProductLedgerLocalApprovalDecisionState.RejectedLocalOnly,
            _ => ProductLedgerLocalApprovalDecisionState.ChangesRequestedLocalOnly
        };

    private static ProductLedgerLocalApprovalDecisionStateRequest ReadyRequest(
        ProductLedgerLocalApprovalOperatorDecisionKind decision) =>
        new(
            ExplicitLocalOnlyStatePersistenceScope: true,
            ApprovalId: "approval-state-store-test-001",
            CandidateResult: ReadyCandidate(),
            CandidateEvidenceHash: new string('a', 64),
            CurrentEvidenceHash: new string('a', 64),
            OperatorDecision: decision,
            DecidedAtUtc: new DateTimeOffset(2026, 7, 5, 12, 30, 0, TimeSpan.Zero),
            OperatorClassification: "local-internal-operator",
            OperatorNote: "safe local approval note",
            EvidenceReferences:
            [
                "docs/qa/nodal-os-local-approval-execution-final-local-only-readiness-packet/report.md",
                "docs/qa/nodal-os-local-route-live-ledger-read-model-test-safe/report.md"
            ],
            RequestsPublicUiAction: false,
            RequestsProductCommandExecution: false,
            RequestsProductCommandHandler: false,
            RequestsProductiveServiceRegistration: false,
            RequestsPhysicalExport: false,
            RequestsFileWriteOutsideApprovalStateStore: false,
            ClaimsArbitraryPathInput: false,
            ClaimsProviderCloudNetwork: false,
            ClaimsDbMigration: false,
            ClaimsKmsWormExternalTrust: false,
            ClaimsBrowserCdpWcuOcrRecipesLive: false,
            ClaimsPilotRun: false,
            ClaimsReleaseCommercial: false);

    private static ProductLedgerLocalApprovalExecutionResult ReadyCandidate() =>
        new ProductLedgerLocalDevRoutePreview()
            .Render(ProductLedgerLocalDevRoutePreview.CreateDefaultLocalDevRequest())
            .CanonicalSurface
            .ApprovalExecutionCandidatePreview;

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

    private sealed class StoreFixture : IDisposable
    {
        private const string StateFileName = "product-ledger-local-approval-state.json";

        private StoreFixture(string root)
        {
            Root = root;
            Store = new ProductLedgerLocalApprovalDecisionStateStore(new ProductLedgerLocalApprovalDecisionStateStoreOptions(
                StoreRootPath: root,
                ExplicitLocalOnlyStateStore: true,
                AllowsArbitraryPathInput: false,
                AllowsExport: false,
                AllowsNetwork: false,
                AllowsDb: false,
                AllowsReleaseCommercial: false));
        }

        public string Root { get; }

        public string StateFilePath => Path.Combine(Root, StateFileName);

        public ProductLedgerLocalApprovalDecisionStateStore Store { get; }

        public static StoreFixture Create()
        {
            var root = Path.Combine(RepoRoot(), ".tmp-product-ledger-approval-state-tests", Guid.NewGuid().ToString("N"));
            return new StoreFixture(root);
        }

        public void Dispose()
        {
            var tempRoot = Path.Combine(RepoRoot(), ".tmp-product-ledger-approval-state-tests");
            if (Directory.Exists(tempRoot))
            {
                Directory.Delete(tempRoot, recursive: true);
            }
        }
    }
}
