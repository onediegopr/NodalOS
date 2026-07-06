using System.Text.Json;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using OneBrain.Core.Approval;

namespace OneBrain.Safety.Tests;

[TestClass]
[TestCategory("ApprovalHumanReview")]
[TestCategory("PhaseEApprovalHumanReview")]
public sealed class ProductLedgerLocalOperatorSurfaceLatestStateManifestWriterTests
{
    [TestMethod]
    public void LatestStateManifestWriter_CreatesJsonManifestUnderAllowedBoundary()
    {
        using var fixture = LatestStateManifestFixture.Create();
        var request = ReadyRequest();

        var result = fixture.Writer.CreateManifest(request);
        var fullPath = fixture.FullPath(result);
        var json = File.ReadAllText(fullPath);

        Assert.AreEqual(ProductLedgerLocalOperatorSurfaceLatestStateManifestDecision.ManifestCreatedLocalOnly, result.Decision);
        Assert.AreEqual(ProductLedgerLocalOperatorSurfaceLatestStateManifestState.ManifestCreatedLocalOnly, result.State);
        Assert.AreEqual(0, result.Blockers.Count);
        Assert.IsTrue(File.Exists(fullPath));
        Assert.IsTrue(Path.GetFullPath(fullPath).StartsWith(Path.GetFullPath(fixture.AllowedBoundaryRoot), StringComparison.OrdinalIgnoreCase));
        Assert.IsTrue(result.ManifestRelativePath.EndsWith(".json", StringComparison.Ordinal));
        StringAssert.StartsWith(result.ManifestRelativePath, ProductLedgerLocalOperatorSurfaceLatestStateManifestWriter.AllowedRelativeOutputBoundary);
        StringAssert.Contains(result.ManifestRelativePath, "operator-surface-latest-state-manifest-latest-state-manifest-001-");
        Assert.IsTrue(result.CreateOnly);
        Assert.IsTrue(result.Versioned);
        Assert.IsTrue(result.JsonOnly);
        Assert.IsFalse(result.OverwriteAllowed);
        Assert.IsFalse(result.LatestPointerAvailable);
        Assert.IsFalse(result.LatestPointerOverwriteAllowed);
        Assert.IsFalse(result.ReadPrecedenceAllowed);
        Assert.IsFalse(result.UserSelectedPathAllowed);
        Assert.IsTrue(result.CanonicalizationPassed);
        Assert.IsTrue(result.ReparseValidationPassed);
        Assert.IsTrue(result.RedactionApplied);
        Assert.IsTrue(result.HistoricalIndexEvidenceOnly);
        Assert.IsFalse(result.AuthorityLiveProduct);
        Assert.IsFalse(result.ProductAuthority);
        Assert.IsFalse(result.ProductionAllowed);
        Assert.IsFalse(result.PublicProductAllowed);
        Assert.IsFalse(result.ShellAllowed);
        Assert.IsFalse(result.CommandExecutionAllowed);
        Assert.IsFalse(result.ProviderCloudNetworkAvailable);
        Assert.IsFalse(result.DbMigrationAvailable);
        Assert.IsFalse(result.KmsWormExternalTrustAvailable);
        Assert.IsFalse(result.BrowserCdpWcuOcrRecipesLiveAvailable);
        Assert.IsFalse(result.PilotRunAvailable);
        Assert.IsFalse(result.ReleaseCommercialReady);
        Assert.IsFalse(result.ComplianceCustody);
        Assert.IsFalse(result.CloudBackedDurability);
        Assert.AreEqual(ProductLedgerLocalOperatorSurfaceLatestStateManifestWriter.Classification, result.Classification);
        Assert.AreEqual(ProductLedgerLocalOperatorSurfaceLatestStateManifestWriter.StalePolicy, result.StalePolicy);
        Assert.AreEqual(request.SourceSnapshot!.SnapshotContentHash, result.SourceSnapshotContentHash);
        Assert.AreEqual(64, result.ManifestContentHash.Length);
        Assert.AreEqual(64, result.CheckpointHash.Length);
        CollectionAssert.Contains(result.NegativeFlags.ToArray(), "not live authority");
        CollectionAssert.Contains(result.NegativeFlags.ToArray(), "no latest pointer");
        CollectionAssert.Contains(result.NegativeFlags.ToArray(), "no read precedence");
        StringAssert.Contains(json, "\"classification\": \"LOCAL_INTERNAL_DEV_ONLY_VERSIONED_MANIFEST_NOT_AUTHORITY\"");
        StringAssert.Contains(json, "\"historicalIndexEvidenceOnly\": true");
        StringAssert.Contains(json, "\"authorityLiveProduct\": false");
        StringAssert.Contains(json, "\"productAuthority\": false");
        StringAssert.Contains(json, "\"latestPointer\": false");
        StringAssert.Contains(json, "\"latestPointerOverwrite\": false");
        StringAssert.Contains(json, "\"readPrecedence\": false");
        StringAssert.Contains(json, "\"releaseCommercial\": false");
        StringAssert.Contains(json, "\"complianceCustody\": false");
        StringAssert.Contains(json, ProductLedgerLocalOperatorSurfaceLatestStateSnapshotExecutor.AllowedRelativeOutputBoundary);
        _ = JsonSerializer.Deserialize<JsonElement>(json);
        Assert.IsFalse(json.Contains("password=", StringComparison.OrdinalIgnoreCase));
        Assert.IsFalse(json.Contains(@"C:\Users\", StringComparison.OrdinalIgnoreCase));
        Assert.IsFalse(json.Contains(fixture.WorkspaceRoot, StringComparison.OrdinalIgnoreCase));
        Assert.IsFalse(File.Exists(Path.Combine(fixture.AllowedBoundaryRoot, "latest.json")));
    }

    [TestMethod]
    public void LatestStateManifestWriter_ReplaysExactManifestAndBlocksCorruptExistingFile()
    {
        using var fixture = LatestStateManifestFixture.Create();
        var first = fixture.Writer.CreateManifest(ReadyRequest());
        var replay = fixture.Writer.CreateManifest(ReadyRequest());

        Assert.AreEqual(ProductLedgerLocalOperatorSurfaceLatestStateManifestDecision.ManifestCreatedLocalOnly, first.Decision);
        Assert.AreEqual(ProductLedgerLocalOperatorSurfaceLatestStateManifestDecision.IdempotentReplay, replay.Decision);
        Assert.AreEqual(first.ManifestContentHash, replay.ManifestContentHash);

        using var conflictFixture = LatestStateManifestFixture.Create();
        var expected = conflictFixture.FullPath(conflictFixture.Writer.CreateManifest(ReadyRequest()));
        File.WriteAllText(expected, "{\"safe\":\"different\"}");
        var conflict = conflictFixture.Writer.CreateManifest(ReadyRequest());

        Assert.AreEqual(ProductLedgerLocalOperatorSurfaceLatestStateManifestDecision.Rejected, conflict.Decision);
        CollectionAssert.Contains(
            conflict.Blockers.ToArray(),
            ProductLedgerLocalOperatorSurfaceLatestStateManifestBlocker.ExistingManifestCorrupt,
            string.Join(",", conflict.Blockers));
    }

    [TestMethod]
    public void LatestStateManifestWriter_BlocksUnsafeClaimsAndInputs()
    {
        var ready = ReadyRequest();
        var cases = new (ProductLedgerLocalOperatorSurfaceLatestStateManifestRequest? Request, ProductLedgerLocalOperatorSurfaceLatestStateManifestBlocker Blocker)[]
        {
            (null, ProductLedgerLocalOperatorSurfaceLatestStateManifestBlocker.MissingRequest),
            (ready with { ExplicitLatestStateManifestScope = false }, ProductLedgerLocalOperatorSurfaceLatestStateManifestBlocker.MissingExplicitManifestScope),
            (ready with { DevelopmentMode = false }, ProductLedgerLocalOperatorSurfaceLatestStateManifestBlocker.NonDevelopmentMode),
            (ready with { ManifestId = "../unsafe" }, ProductLedgerLocalOperatorSurfaceLatestStateManifestBlocker.UnsafeManifestId),
            (ready with { ManifestId = "%2e%2e%2funsafe" }, ProductLedgerLocalOperatorSurfaceLatestStateManifestBlocker.UnsafeManifestId),
            (ready with { ActionKind = null }, ProductLedgerLocalOperatorSurfaceLatestStateManifestBlocker.UnknownActionKind),
            (ready with { SourceSnapshot = ProductLedgerLocalOperatorSurfaceLatestStateSnapshotResult.Pending }, ProductLedgerLocalOperatorSurfaceLatestStateManifestBlocker.SourceSnapshotNotCreated),
            (ready with { ExpectedSourceSnapshotContentHash = new string('1', 64) }, ProductLedgerLocalOperatorSurfaceLatestStateManifestBlocker.SourceSnapshotHashMismatch),
            (ready with { ExpectedSourceSnapshotCheckpointHash = new string('2', 64) }, ProductLedgerLocalOperatorSurfaceLatestStateManifestBlocker.SourceSnapshotCheckpointMismatch),
            (ready with { EvidenceReferences = [] }, ProductLedgerLocalOperatorSurfaceLatestStateManifestBlocker.MissingEvidenceReferences),
            (ready with { ProposedPath = @"C:\Users\fixture\unsafe.json" }, ProductLedgerLocalOperatorSurfaceLatestStateManifestBlocker.PayloadPathFieldRejected),
            (ready with { ProposedRoot = @"C:\Users\fixture" }, ProductLedgerLocalOperatorSurfaceLatestStateManifestBlocker.PayloadPathFieldRejected),
            (ready with { ProposedFilename = "latest.json" }, ProductLedgerLocalOperatorSurfaceLatestStateManifestBlocker.PayloadPathFieldRejected),
            (ready with { ProposedCommand = "cmd /c whoami" }, ProductLedgerLocalOperatorSurfaceLatestStateManifestBlocker.PayloadCommandFieldRejected),
            (ready with { ProposedUrl = "https://local.invalid/action" }, ProductLedgerLocalOperatorSurfaceLatestStateManifestBlocker.PayloadNetworkProviderFieldRejected),
            (ready with { ProposedProvider = "provider-live" }, ProductLedgerLocalOperatorSurfaceLatestStateManifestBlocker.PayloadNetworkProviderFieldRejected),
            (ready with { ProposedDbMigration = "migration" }, ProductLedgerLocalOperatorSurfaceLatestStateManifestBlocker.PayloadNetworkProviderFieldRejected),
            (ready with { ClaimsFilesystemScan = true }, ProductLedgerLocalOperatorSurfaceLatestStateManifestBlocker.ClaimsFilesystemScan),
            (ready with { RequestsOverwrite = true }, ProductLedgerLocalOperatorSurfaceLatestStateManifestBlocker.RequestsOverwrite),
            (ready with { RequestsLatestPointer = true }, ProductLedgerLocalOperatorSurfaceLatestStateManifestBlocker.RequestsLatestPointer),
            (ready with { RequestsLatestPointerOverwrite = true }, ProductLedgerLocalOperatorSurfaceLatestStateManifestBlocker.RequestsLatestPointerOverwrite),
            (ready with { RequestsReadPrecedence = true }, ProductLedgerLocalOperatorSurfaceLatestStateManifestBlocker.RequestsReadPrecedence),
            (ready with { RequestsShellOrSubprocess = true }, ProductLedgerLocalOperatorSurfaceLatestStateManifestBlocker.RequestsShellOrSubprocess),
            (ready with { ClaimsArbitraryCommandExecution = true }, ProductLedgerLocalOperatorSurfaceLatestStateManifestBlocker.ClaimsArbitraryCommandExecution),
            (ready with { ClaimsProviderCloudNetwork = true }, ProductLedgerLocalOperatorSurfaceLatestStateManifestBlocker.ClaimsProviderCloudNetwork),
            (ready with { ClaimsDbMigration = true }, ProductLedgerLocalOperatorSurfaceLatestStateManifestBlocker.ClaimsDbMigration),
            (ready with { ClaimsKmsWormExternalTrust = true }, ProductLedgerLocalOperatorSurfaceLatestStateManifestBlocker.ClaimsKmsWormExternalTrust),
            (ready with { ClaimsBrowserCdpWcuOcrRecipesLive = true }, ProductLedgerLocalOperatorSurfaceLatestStateManifestBlocker.ClaimsBrowserCdpWcuOcrRecipesLive),
            (ready with { ClaimsPilotRun = true }, ProductLedgerLocalOperatorSurfaceLatestStateManifestBlocker.ClaimsPilotRun),
            (ready with { ClaimsReleaseCommercial = true }, ProductLedgerLocalOperatorSurfaceLatestStateManifestBlocker.ClaimsReleaseCommercial),
            (ready with { ClaimsLiveAuthority = true }, ProductLedgerLocalOperatorSurfaceLatestStateManifestBlocker.ClaimsLiveAuthority),
            (ready with { ClaimsProductAuthority = true }, ProductLedgerLocalOperatorSurfaceLatestStateManifestBlocker.ClaimsProductAuthority),
            (ready with { ClaimsComplianceCustody = true }, ProductLedgerLocalOperatorSurfaceLatestStateManifestBlocker.ClaimsComplianceCustody),
            (ready with { ClaimsCloudBackedDurability = true }, ProductLedgerLocalOperatorSurfaceLatestStateManifestBlocker.ClaimsCloudBackedDurability)
        };

        foreach (var testCase in cases)
        {
            using var fixture = LatestStateManifestFixture.Create();
            var result = fixture.Writer.CreateManifest(testCase.Request);

            Assert.AreEqual(ProductLedgerLocalOperatorSurfaceLatestStateManifestDecision.Rejected, result.Decision, testCase.Blocker.ToString());
            CollectionAssert.Contains(result.Blockers.ToArray(), testCase.Blocker, testCase.Blocker.ToString());
            Assert.IsFalse(Directory.Exists(fixture.AllowedBoundaryRoot) && Directory.GetFiles(fixture.AllowedBoundaryRoot, "*.json").Length > 0);
        }
    }

    [TestMethod]
    public void LatestStateManifestWriter_OptionCorpusFailsClosedForUnsafeBoundaryCapabilities()
    {
        var unsafeOptions = new Func<string, ProductLedgerLocalOperatorSurfaceLatestStateManifestOptions>[]
        {
            root => LatestStateManifestFixture.Options(root) with { ExplicitLatestStateManifestBoundary = false },
            root => LatestStateManifestFixture.Options(root) with { AllowsArbitraryPathInput = true },
            root => LatestStateManifestFixture.Options(root) with { AllowsFilesystemScan = true },
            root => LatestStateManifestFixture.Options(root) with { AllowsOverwrite = true },
            root => LatestStateManifestFixture.Options(root) with { AllowsLatestPointer = true },
            root => LatestStateManifestFixture.Options(root) with { AllowsLatestPointerOverwrite = true },
            root => LatestStateManifestFixture.Options(root) with { AllowsReadPrecedence = true },
            root => LatestStateManifestFixture.Options(root) with { AllowsUserSelectedPath = true },
            root => LatestStateManifestFixture.Options(root) with { AllowsShellOrSubprocess = true },
            root => LatestStateManifestFixture.Options(root) with { AllowsCommandExecution = true },
            root => LatestStateManifestFixture.Options(root) with { AllowsNetwork = true },
            root => LatestStateManifestFixture.Options(root) with { AllowsDb = true },
            root => LatestStateManifestFixture.Options(root) with { AllowsKmsWormExternalTrust = true },
            root => LatestStateManifestFixture.Options(root) with { AllowsReleaseCommercial = true },
            _ => LatestStateManifestFixture.Options(string.Empty)
        };

        foreach (var optionsFactory in unsafeOptions)
        {
            using var fixture = LatestStateManifestFixture.Create();
            var writer = new ProductLedgerLocalOperatorSurfaceLatestStateManifestWriter(optionsFactory(fixture.WorkspaceRoot));
            var result = writer.CreateManifest(ReadyRequest());

            Assert.AreEqual(ProductLedgerLocalOperatorSurfaceLatestStateManifestDecision.Rejected, result.Decision);
            CollectionAssert.Contains(result.Blockers.ToArray(), ProductLedgerLocalOperatorSurfaceLatestStateManifestBlocker.OutputBoundaryRejected);
            Assert.IsFalse(Directory.Exists(fixture.AllowedBoundaryRoot) && Directory.GetFiles(fixture.AllowedBoundaryRoot, "*.json").Length > 0);
        }
    }

    [TestMethod]
    public void LatestStateManifestWriter_SurfaceDomShowsManifestState()
    {
        using var fixture = LatestStateManifestFixture.Create();
        var result = fixture.Writer.CreateManifest(ReadyRequest());
        var html = new ProductLedgerLocalDevRoutePreview()
            .Render(
                ProductLedgerLocalDevRoutePreview.CreateDefaultLocalDevRequest(),
                ProductLedgerOperatorSurfaceReadModelSource.FixtureSafe,
                null,
                null,
                null,
                null,
                null,
                null,
                ReadySnapshot(),
                result)
            .HtmlSnapshot;

        StringAssert.Contains(html, "data-testid=\"product-ledger-latest-state-manifest-state\"");
        StringAssert.Contains(html, "data-state=\"ManifestCreatedLocalOnly\"");
        StringAssert.Contains(html, "data-historical-index-evidence-only=\"true\"");
        StringAssert.Contains(html, "data-authority-live-product=\"false\"");
        StringAssert.Contains(html, "data-product-authority=\"false\"");
        StringAssert.Contains(html, "data-latest-pointer=\"false\"");
        StringAssert.Contains(html, "data-read-precedence=\"false\"");
        StringAssert.Contains(html, ProductLedgerLocalOperatorSurfaceLatestStateManifestWriter.AllowedRelativeOutputBoundary);
        StringAssert.Contains(html, "json-only immutable versioned create-only no-overwrite no latest pointer no read precedence");
        StringAssert.Contains(html, ProductLedgerLocalOperatorSurfaceLatestStateManifestWriter.StalePolicy);
        Assert.IsFalse(html.Contains("password=", StringComparison.OrdinalIgnoreCase));
        Assert.IsFalse(html.Contains(@"C:\Users\", StringComparison.OrdinalIgnoreCase));
        Assert.IsFalse(html.Contains("data-executable=\"true\"", StringComparison.OrdinalIgnoreCase));
    }

    [TestMethod]
    public void LatestStateManifestWriter_SourceHasOnlyBoundedCreateOnlyJsonWriteAndNoForbiddenRuntime()
    {
        var source = ReadRepoFile("src", "OneBrain.Core", "Approval", "ProductLedgerLocalOperatorSurfaceLatestStateManifestWriter.cs");
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
            "Directory.GetFiles",
            "Directory.EnumerateFiles",
            "FileMode.OpenOrCreate",
            "FileMode.Create,",
            "OverwriteAllowed: true",
            "LatestPointerAvailable: true",
            "LatestPointerOverwriteAllowed: true",
            "ReadPrecedenceAllowed: true",
            "AuthorityLiveProduct: true",
            "ProductAuthority: true",
            "ProductionAllowed: true",
            "PublicProductAllowed: true",
            "ReleaseCommercialReady: true",
            "ComplianceCustody: true",
            "CloudBackedDurability: true"
        })
        {
            Assert.IsFalse(source.Contains(fragment, StringComparison.Ordinal), fragment);
        }

        StringAssert.Contains(source, "FileMode.CreateNew");
        StringAssert.Contains(source, "FileAttributes.ReparsePoint");
        StringAssert.Contains(source, ProductLedgerLocalOperatorSurfaceLatestStateManifestWriter.AllowedRelativeOutputBoundary);
        StringAssert.Contains(source, "NO_OVERWRITE");
        StringAssert.Contains(source, "NO_LATEST_POINTER");
        StringAssert.Contains(source, "NO_READ_PRECEDENCE");
        StringAssert.Contains(source, "VERSIONED_MANIFEST_NOT_AUTHORITY");
    }

    private static ProductLedgerLocalOperatorSurfaceLatestStateManifestRequest ReadyRequest()
    {
        var snapshot = ReadySnapshot();
        return new(
            ExplicitLatestStateManifestScope: true,
            DevelopmentMode: true,
            LocalMode: true,
            InternalMode: true,
            ManifestId: "latest-state-manifest-001",
            ActionId: "latest-state-manifest-action-001",
            ActionKind: ProductLedgerLocalOperatorSurfaceLatestStateManifestActionKind.LocalOperatorSurfaceLatestStateManifestCreateOnly,
            SourceSnapshot: snapshot,
            ExpectedSourceSnapshotContentHash: snapshot.SnapshotContentHash,
            ExpectedSourceSnapshotCheckpointHash: snapshot.CheckpointHash,
            EvidenceReferences: ["docs/qa/product-ledger-durable-latest-state-manifest-create-only-implementation/report.md"],
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
            RequestsLatestPointer: false,
            RequestsLatestPointerOverwrite: false,
            RequestsReadPrecedence: false,
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
            ClaimsReleaseCommercial: false,
            ClaimsLiveAuthority: false,
            ClaimsProductAuthority: false,
            ClaimsComplianceCustody: false,
            ClaimsCloudBackedDurability: false);
    }

    private static ProductLedgerLocalOperatorSurfaceLatestStateSnapshotResult ReadySnapshot() =>
        ProductLedgerLocalOperatorSurfaceLatestStateSnapshotResult.Pending with
        {
            Decision = ProductLedgerLocalOperatorSurfaceLatestStateSnapshotDecision.SnapshotCreatedLocalOnly,
            State = ProductLedgerLocalOperatorSurfaceLatestStateSnapshotState.SnapshotCreatedLocalOnly,
            Blockers = [],
            SnapshotId = "latest-state-snapshot-001",
            ActionId = "latest-state-snapshot-action-001",
            SnapshotRelativePath = ProductLedgerLocalOperatorSurfaceLatestStateSnapshotExecutor.AllowedRelativeOutputBoundary + "operator-surface-latest-state-snapshot-latest-state-snapshot-001-aaaaaaaaaaaa.json",
            OperatorSurfaceModelHash = new string('a', 64),
            OperatorSurfaceModelHashPrefix = new string('a', 12),
            SnapshotContentHash = new string('b', 64),
            SnapshotContentHashPrefix = new string('b', 12),
            CheckpointHash = new string('c', 64),
            CheckpointHashPrefix = new string('c', 12),
            SourceChainIds = ["approval", "noop", "bounded", "local-draft", "workspace-draft", "user-workspace-draft"],
            SourceChainContentHashes = [new string('d', 12), new string('e', 12), new string('f', 12)],
            SafeRelativePathsOnly = ["docs/nodal-os/handoffs/user-workspace-allowlisted-handoff-draft-safe.md"],
            EvidenceRefs = ["docs/qa/product-ledger-local-operator-surface-latest-state-snapshot-implementation/report.md"],
            CanonicalizationPassed = true,
            ReparseValidationPassed = true,
            RedactionApplied = true,
            HistoricalEvidenceOnly = true,
            AuthorityLiveProduct = false,
            ProductionAllowed = false,
            PublicProductAllowed = false,
            ReleaseCommercialReady = false,
            StatusText = ProductLedgerLocalOperatorSurfaceLatestStateSnapshotExecutor.CompletedStatus
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

    private sealed class LatestStateManifestFixture : IDisposable
    {
        private LatestStateManifestFixture(string workspaceRoot)
        {
            WorkspaceRoot = workspaceRoot;
            Writer = new ProductLedgerLocalOperatorSurfaceLatestStateManifestWriter(Options(workspaceRoot));
        }

        public static ProductLedgerLocalOperatorSurfaceLatestStateManifestOptions Options(string workspaceRoot) =>
            new(
                WorkspaceRootPath: workspaceRoot,
                ExplicitLatestStateManifestBoundary: true,
                AllowsArbitraryPathInput: false,
                AllowsFilesystemScan: false,
                AllowsOverwrite: false,
                AllowsLatestPointer: false,
                AllowsLatestPointerOverwrite: false,
                AllowsReadPrecedence: false,
                AllowsUserSelectedPath: false,
                AllowsShellOrSubprocess: false,
                AllowsCommandExecution: false,
                AllowsNetwork: false,
                AllowsDb: false,
                AllowsKmsWormExternalTrust: false,
                AllowsReleaseCommercial: false);

        public string WorkspaceRoot { get; }

        public string AllowedBoundaryRoot =>
            Path.Combine(WorkspaceRoot, "docs", "test-output", "product-ledger", "operator-surface-latest-state-manifests");

        public ProductLedgerLocalOperatorSurfaceLatestStateManifestWriter Writer { get; }

        public string FullPath(ProductLedgerLocalOperatorSurfaceLatestStateManifestResult manifest) =>
            Path.Combine(WorkspaceRoot, manifest.ManifestRelativePath.Replace('/', Path.DirectorySeparatorChar));

        public static LatestStateManifestFixture Create()
        {
            var root = Path.Combine(RepoRoot(), ".tmp-product-ledger-latest-state-manifest-tests", Guid.NewGuid().ToString("N"));
            return new LatestStateManifestFixture(root);
        }

        public void Dispose()
        {
            var tempRoot = Path.Combine(RepoRoot(), ".tmp-product-ledger-latest-state-manifest-tests");
            if (Directory.Exists(tempRoot))
            {
                Directory.Delete(tempRoot, recursive: true);
            }
        }
    }
}
