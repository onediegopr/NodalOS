using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using OneBrain.Core.Approval;

namespace OneBrain.Safety.Tests;

[TestClass]
[TestCategory("ApprovalHumanReview")]
[TestCategory("PhaseEApprovalHumanReview")]
public sealed class ProductLedgerLocalDurableLatestStateReaderCandidateValidatorTests
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.Never
    };

    [TestMethod]
    public void ReaderCandidate_ValidManifestProducesNotAuthorityCandidate()
    {
        using var fixture = ReaderCandidateFixture.Create();
        var result = fixture.Validator.Validate(fixture.ValidRequest());

        Assert.AreEqual(ProductLedgerLocalDurableLatestStateReaderCandidateDecision.ValidatedCandidateNotAuthority, result.Decision);
        Assert.AreEqual(ProductLedgerLocalDurableLatestStateReaderCandidateState.CandidateValidatedNotAuthority, result.State);
        Assert.AreEqual(0, result.Blockers.Count);
        Assert.IsTrue(result.ReadOnly);
        Assert.IsTrue(result.CandidateEvidenceOnly);
        Assert.IsTrue(result.Validation.ManifestHashValid);
        Assert.IsTrue(result.Validation.SnapshotHashValid);
        Assert.IsTrue(result.Validation.StaleAware);
        Assert.IsTrue(result.Validation.NotAuthority);
        Assert.IsFalse(result.Authority);
        Assert.IsFalse(result.LiveAuthority);
        Assert.IsFalse(result.ProductAuthority);
        Assert.IsFalse(result.ReadPrecedence);
        Assert.IsFalse(result.LatestPointer);
        Assert.IsFalse(result.LatestPointerOverwrite);
        Assert.IsFalse(result.ProductionAllowed);
        Assert.IsFalse(result.PublicProductAllowed);
        Assert.IsFalse(result.CommandExecutionAllowed);
        Assert.IsFalse(result.ProviderCloudNetworkAvailable);
        Assert.IsFalse(result.DbMigrationAvailable);
        Assert.IsFalse(result.KmsWormExternalTrustAvailable);
        Assert.IsFalse(result.BrowserCdpWcuOcrRecipesLiveAvailable);
        Assert.IsFalse(result.ReleaseCommercialReady);
        Assert.IsFalse(result.ComplianceCustody);
        Assert.IsFalse(result.CloudBackedDurability);
        Assert.AreEqual(ProductLedgerLocalDurableLatestStateReaderCandidateValidator.Classification, result.Classification);
        CollectionAssert.Contains(result.SourceSnapshotIds.ToArray(), fixture.SnapshotPayload.SnapshotId);
        CollectionAssert.Contains(result.SourceEvidenceRefs.ToArray(), "product-ledger-durable-latest-state-reader-candidate-validated-not-authority");
        StringAssert.StartsWith(result.SourceManifestRelativePath, ProductLedgerLocalOperatorSurfaceLatestStateManifestWriter.AllowedRelativeOutputBoundary);
        Assert.IsFalse(JsonSerializer.Serialize(result, JsonOptions).Contains(@"C:\Users\", StringComparison.OrdinalIgnoreCase));
        Assert.IsFalse(JsonSerializer.Serialize(result, JsonOptions).Contains("password=", StringComparison.OrdinalIgnoreCase));
    }

    [TestMethod]
    public void ReaderCandidate_BlocksMissingCorruptTamperedAndUnsafeSources()
    {
        var cases = new (Action<ReaderCandidateFixture> Arrange, ProductLedgerLocalDurableLatestStateReaderCandidateBlocker Blocker)[]
        {
            (fixture => fixture.SourceManifest = ProductLedgerLocalOperatorSurfaceLatestStateManifestResult.Pending, ProductLedgerLocalDurableLatestStateReaderCandidateBlocker.SourceManifestNotCreated),
            (fixture => File.WriteAllText(fixture.ManifestFullPath, "{not-json"), ProductLedgerLocalDurableLatestStateReaderCandidateBlocker.SourceManifestCorrupt),
            (fixture => fixture.ReplaceManifest("\"blockerSummaryRedacted\": \"none\"", "\"blockerSummaryRedacted\": \"changed\""), ProductLedgerLocalDurableLatestStateReaderCandidateBlocker.SourceManifestHashMismatch),
            (fixture => fixture.ReplaceManifest("\"authorityLiveProduct\": false", "\"authorityLiveProduct\": true"), ProductLedgerLocalDurableLatestStateReaderCandidateBlocker.SourceManifestClaimsAuthority),
            (fixture => fixture.ReplaceManifest("\"latestPointer\": false", "\"latestPointer\": true"), ProductLedgerLocalDurableLatestStateReaderCandidateBlocker.SourceManifestClaimsLatestPointer),
            (fixture => fixture.ReplaceManifest("\"readPrecedence\": false", "\"readPrecedence\": true"), ProductLedgerLocalDurableLatestStateReaderCandidateBlocker.SourceManifestClaimsReadPrecedence),
            (fixture => fixture.ReplaceManifest("docs/qa/product-ledger-durable-latest-state-reader-candidate/report.md", @"C:\\Users\\diego\\secret.txt"), ProductLedgerLocalDurableLatestStateReaderCandidateBlocker.UnsafeMetadata),
            (fixture => File.WriteAllText(fixture.SnapshotFullPath, "{not-json"), ProductLedgerLocalDurableLatestStateReaderCandidateBlocker.SourceSnapshotCorrupt),
            (fixture => fixture.ReplaceSnapshot("\"warningSummaryRedacted\": \"stale snapshots are historical evidence only; not authority live/product\"", "\"warningSummaryRedacted\": \"stale snapshots changed\""), ProductLedgerLocalDurableLatestStateReaderCandidateBlocker.SourceSnapshotHashMismatch)
        };

        foreach (var testCase in cases)
        {
            using var fixture = ReaderCandidateFixture.Create();
            testCase.Arrange(fixture);
            var result = fixture.Validator.Validate(fixture.ValidRequest());

            Assert.AreEqual(ProductLedgerLocalDurableLatestStateReaderCandidateDecision.Rejected, result.Decision, testCase.Blocker.ToString());
            CollectionAssert.Contains(result.Blockers.ToArray(), testCase.Blocker, string.Join(",", result.Blockers));
            Assert.IsFalse(result.Authority);
            Assert.IsFalse(result.ProductAuthority);
            Assert.IsFalse(result.ReadPrecedence);
            Assert.IsFalse(result.LatestPointer);
        }
    }

    [TestMethod]
    public void ReaderCandidate_BlocksQueryHeaderAndUnsafeOptions()
    {
        using var fixture = ReaderCandidateFixture.Create();
        var query = fixture.Validator.Validate(fixture.ValidRequest() with { QueryOverridePresent = true });
        var header = fixture.Validator.Validate(fixture.ValidRequest() with { HeaderOverridePresent = true });
        var unsafeOptions = new ProductLedgerLocalDurableLatestStateReaderCandidateValidator(
            ReaderCandidateFixture.Options(fixture.WorkspaceRoot) with { AllowsReadPrecedence = true });
        var unsafeResult = unsafeOptions.Validate(fixture.ValidRequest());

        CollectionAssert.Contains(query.Blockers.ToArray(), ProductLedgerLocalDurableLatestStateReaderCandidateBlocker.QueryOverrideRejected);
        CollectionAssert.Contains(header.Blockers.ToArray(), ProductLedgerLocalDurableLatestStateReaderCandidateBlocker.HeaderOverrideRejected);
        CollectionAssert.Contains(unsafeResult.Blockers.ToArray(), ProductLedgerLocalDurableLatestStateReaderCandidateBlocker.BoundaryRejected);
    }

    [TestMethod]
    public void ReaderCandidate_SurfaceDomShowsNotAuthorityCandidateState()
    {
        using var fixture = ReaderCandidateFixture.Create();
        var candidate = fixture.Validator.Validate(fixture.ValidRequest());
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
                null,
                fixture.SourceManifest,
                candidate)
            .HtmlSnapshot;

        StringAssert.Contains(html, "data-testid=\"product-ledger-durable-latest-state-reader-candidate-state\"");
        StringAssert.Contains(html, "data-state=\"CandidateValidatedNotAuthority\"");
        StringAssert.Contains(html, "data-authority=\"false\"");
        StringAssert.Contains(html, "data-product-authority=\"false\"");
        StringAssert.Contains(html, "data-read-precedence=\"false\"");
        StringAssert.Contains(html, "data-latest-pointer=\"false\"");
        StringAssert.Contains(html, "data-stale-aware=\"true\"");
        StringAssert.Contains(html, ProductLedgerLocalDurableLatestStateReaderCandidateValidator.Classification);
        StringAssert.Contains(html, "not authority not live/product candidate evidence only");
        StringAssert.Contains(html, "no latest pointer no read precedence");
        Assert.IsFalse(html.Contains(@"C:\Users\", StringComparison.OrdinalIgnoreCase));
        Assert.IsFalse(html.Contains("password=", StringComparison.OrdinalIgnoreCase));
        Assert.IsFalse(html.Contains("data-executable=\"true\"", StringComparison.OrdinalIgnoreCase));
    }

    [TestMethod]
    public void ReaderCandidate_SourceHasReadOnlyCandidateAndNoForbiddenActivation()
    {
        var source = ReadRepoFile("src", "OneBrain.Core", "Approval", "ProductLedgerLocalDurableLatestStateReaderCandidateValidator.cs");
        foreach (var fragment in new[]
        {
            "File.WriteAllText",
            "File.WriteAllBytes",
            "File.AppendAllText",
            "File.Delete",
            "Directory.GetFiles",
            "Directory.EnumerateFiles",
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
            "ReadPrecedence: true",
            "LatestPointer: true",
            "LatestPointerOverwrite: true",
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

        StringAssert.Contains(source, "File.ReadAllText");
        StringAssert.Contains(source, "LOCAL_INTERNAL_DEV_ONLY_READER_CANDIDATE_NOT_AUTHORITY");
        StringAssert.Contains(source, "NO_LATEST_POINTER");
        StringAssert.Contains(source, "NO_READ_PRECEDENCE");
    }

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

    private sealed class ReaderCandidateFixture : IDisposable
    {
        private ReaderCandidateFixture(string workspaceRoot)
        {
            WorkspaceRoot = workspaceRoot;
            Validator = new ProductLedgerLocalDurableLatestStateReaderCandidateValidator(Options(workspaceRoot));
            SnapshotPayload = CreateSnapshotPayload();
            SnapshotFullPath = Path.Combine(workspaceRoot, SnapshotPayload.SafeRelativeSnapshotPath.Replace('/', Path.DirectorySeparatorChar));
            Directory.CreateDirectory(Path.GetDirectoryName(SnapshotFullPath)!);
            File.WriteAllText(SnapshotFullPath, JsonSerializer.Serialize(SnapshotPayload, JsonOptions));
            ManifestPayload = CreateManifestPayload(SnapshotPayload);
            ManifestFullPath = Path.Combine(workspaceRoot, ManifestPayload.SafeRelativeManifestPath.Replace('/', Path.DirectorySeparatorChar));
            Directory.CreateDirectory(Path.GetDirectoryName(ManifestFullPath)!);
            File.WriteAllText(ManifestFullPath, JsonSerializer.Serialize(ManifestPayload, JsonOptions));
            SourceManifest = ProductLedgerLocalOperatorSurfaceLatestStateManifestResult.Pending with
            {
                Decision = ProductLedgerLocalOperatorSurfaceLatestStateManifestDecision.ManifestCreatedLocalOnly,
                State = ProductLedgerLocalOperatorSurfaceLatestStateManifestState.ManifestCreatedLocalOnly,
                Blockers = [],
                ManifestId = ManifestPayload.ManifestId,
                ActionId = ManifestPayload.ActionId,
                ManifestRelativePath = ManifestPayload.SafeRelativeManifestPath,
                SourceSnapshotId = SnapshotPayload.SnapshotId,
                SourceSnapshotRelativePath = SnapshotPayload.SafeRelativeSnapshotPath,
                SourceSnapshotContentHash = SnapshotPayload.SnapshotContentHash,
                SourceSnapshotContentHashPrefix = Prefix(SnapshotPayload.SnapshotContentHash),
                SourceSnapshotCheckpointHash = SnapshotPayload.CheckpointHash,
                SourceSnapshotCheckpointHashPrefix = Prefix(SnapshotPayload.CheckpointHash),
                ManifestContentHash = ManifestPayload.ManifestContentHash,
                ManifestContentHashPrefix = Prefix(ManifestPayload.ManifestContentHash),
                CheckpointHash = ManifestPayload.CheckpointHash,
                CheckpointHashPrefix = Prefix(ManifestPayload.CheckpointHash),
                Entries = ManifestPayload.Entries,
                EvidenceRefs = ManifestPayload.EvidenceRefs,
                RedactionApplied = true,
                CanonicalizationPassed = true,
                ReparseValidationPassed = true,
                StatusText = ProductLedgerLocalOperatorSurfaceLatestStateManifestWriter.CompletedStatus
            };
        }

        public string WorkspaceRoot { get; }

        public ProductLedgerLocalDurableLatestStateReaderCandidateValidator Validator { get; }

        public ProductLedgerLocalOperatorSurfaceLatestStateSnapshotPayload SnapshotPayload { get; }

        public ProductLedgerLocalOperatorSurfaceLatestStateManifestPayload ManifestPayload { get; }

        public ProductLedgerLocalOperatorSurfaceLatestStateManifestResult SourceManifest { get; set; }

        public string SnapshotFullPath { get; }

        public string ManifestFullPath { get; }

        public static ProductLedgerLocalDurableLatestStateReaderCandidateOptions Options(string workspaceRoot) =>
            new(
                WorkspaceRootPath: workspaceRoot,
                ExplicitReaderCandidateBoundary: true,
                AllowsArbitraryPathInput: false,
                AllowsFilesystemScan: false,
                AllowsLatestPointer: false,
                AllowsLatestPointerOverwrite: false,
                AllowsReadPrecedence: false,
                AllowsAuthority: false,
                AllowsProductAuthority: false,
                AllowsPublicProduct: false,
                AllowsProductionRoute: false,
                AllowsShellOrSubprocess: false,
                AllowsCommandExecution: false,
                AllowsNetwork: false,
                AllowsDb: false,
                AllowsKmsWormExternalTrust: false,
                AllowsReleaseCommercial: false);

        public ProductLedgerLocalDurableLatestStateReaderCandidateRequest ValidRequest() =>
            new(
                ExplicitReaderCandidateScope: true,
                DevelopmentMode: true,
                LocalMode: true,
                InternalMode: true,
                SourceManifest: SourceManifest,
                QueryOverridePresent: false,
                HeaderOverridePresent: false);

        public void ReplaceManifest(string oldValue, string newValue) =>
            File.WriteAllText(ManifestFullPath, File.ReadAllText(ManifestFullPath).Replace(oldValue, newValue, StringComparison.Ordinal));

        public void ReplaceSnapshot(string oldValue, string newValue) =>
            File.WriteAllText(SnapshotFullPath, File.ReadAllText(SnapshotFullPath).Replace(oldValue, newValue, StringComparison.Ordinal));

        public static ReaderCandidateFixture Create()
        {
            var root = Path.Combine(RepoRoot(), ".tmp-product-ledger-reader-candidate-tests", Guid.NewGuid().ToString("N"));
            return new ReaderCandidateFixture(root);
        }

        public void Dispose()
        {
            var tempRoot = Path.Combine(RepoRoot(), ".tmp-product-ledger-reader-candidate-tests");
            if (Directory.Exists(tempRoot))
            {
                Directory.Delete(tempRoot, recursive: true);
            }
        }
    }

    private static ProductLedgerLocalOperatorSurfaceLatestStateSnapshotPayload CreateSnapshotPayload()
    {
        var payload = new ProductLedgerLocalOperatorSurfaceLatestStateSnapshotPayload(
            SchemaVersion: 1,
            SnapshotId: "reader-candidate-snapshot-001",
            ActionId: "reader-candidate-snapshot-action-001",
            CreatedAtUtc: DateTimeOffset.UnixEpoch,
            Classification: ProductLedgerLocalOperatorSurfaceLatestStateSnapshotExecutor.Classification,
            StaleStateClassification: ProductLedgerLocalOperatorSurfaceLatestStateSnapshotExecutor.StaleStateClassification,
            SourceSurfaceModelVersion: ProductLedgerOperatorSurfaceModelFactory.CanonicalSurfaceId,
            SourceChainIds: ["approval", "noop", "bounded", "local-draft", "workspace-draft", "user-workspace-draft"],
            SourceChainContentHashes: [new string('a', 12), new string('b', 12), new string('c', 12)],
            SafeRelativePathsOnly: ["docs/nodal-os/handoffs/user-workspace-allowlisted-handoff-draft-safe.md"],
            EvidenceRefs: ["docs/qa/product-ledger-local-operator-surface-latest-state-snapshot-implementation/report.md"],
            DecisionStateSummaryRedacted: "approval:ApprovedLocalOnly:approval-001",
            NoOpExecutionStateSummaryRedacted: "noop:NoOpExecutionCompletedLocalOnly:noop-001",
            BoundedActionStateSummaryRedacted: "bounded:BoundedActionCompletedLocalOnly:bounded-001",
            LocalHandoffDraftStateSummaryRedacted: "local-handoff:DraftCreatedLocalOnly:local-draft-001",
            WorkspaceTestJailDraftStateSummaryRedacted: "workspace-test-jail:DraftCreatedLocalOnly:workspace-draft-001",
            UserWorkspaceAllowlistedDraftStateSummaryRedacted: "user-workspace-allowlisted:DraftCreatedLocalOnly:user-workspace-draft-001",
            BlockerSummaryRedacted: "none",
            WarningSummaryRedacted: "stale snapshots are historical evidence only; not authority live/product",
            NegativeFlags: ProductLedgerLocalOperatorSurfaceLatestStateSnapshotExecutor.NegativeFlags,
            OperatorSurfaceModelHash: new string('d', 64),
            SnapshotContentHash: string.Empty,
            CheckpointHash: new string('e', 64),
            SafeRelativeSnapshotPath: ProductLedgerLocalOperatorSurfaceLatestStateSnapshotExecutor.AllowedRelativeOutputBoundary + "operator-surface-latest-state-snapshot-reader-candidate-snapshot-001-dddddddddddd.json",
            LocalInternalDevelopmentOnly: true,
            HistoricalEvidenceOnly: true,
            AuthorityLiveProduct: false,
            PublicProduct: false,
            Production: false,
            ReleaseCommercial: false,
            ComplianceCustody: false);
        return payload with { SnapshotContentHash = HashSnapshot(payload) };
    }

    private static ProductLedgerLocalOperatorSurfaceLatestStateManifestPayload CreateManifestPayload(
        ProductLedgerLocalOperatorSurfaceLatestStateSnapshotPayload snapshot)
    {
        var entry = new ProductLedgerLocalOperatorSurfaceLatestStateManifestEntry(
            SourceSnapshotId: snapshot.SnapshotId,
            SourceSnapshotRelativePath: snapshot.SafeRelativeSnapshotPath,
            SourceSnapshotContentHash: snapshot.SnapshotContentHash,
            SourceSnapshotCheckpointHash: snapshot.CheckpointHash,
            SourceSnapshotClassification: snapshot.Classification,
            CandidateFreshness: "unknown-historical-evidence-only",
            HistoricalEvidenceOnly: true,
            NotAuthority: true);
        var payload = new ProductLedgerLocalOperatorSurfaceLatestStateManifestPayload(
            SchemaVersion: 1,
            ManifestId: "reader-candidate-manifest-001",
            ActionId: "reader-candidate-manifest-action-001",
            CreatedAtUtc: DateTimeOffset.UnixEpoch,
            ManifestVersion: "v1",
            Classification: ProductLedgerLocalOperatorSurfaceLatestStateManifestWriter.Classification,
            StalePolicy: ProductLedgerLocalOperatorSurfaceLatestStateManifestWriter.StalePolicy,
            InputSnapshotBoundary: ProductLedgerLocalOperatorSurfaceLatestStateSnapshotExecutor.AllowedRelativeOutputBoundary,
            SafeRelativeManifestPath: ProductLedgerLocalOperatorSurfaceLatestStateManifestWriter.AllowedRelativeOutputBoundary + "operator-surface-latest-state-manifest-reader-candidate-manifest-001-" + Prefix(snapshot.SnapshotContentHash) + ".json",
            Entries: [entry],
            EvidenceRefs:
            [
                "docs/qa/product-ledger-durable-latest-state-reader-candidate/report.md",
                "docs/qa/product-ledger-durable-latest-state-manifest-create-only-implementation/report.md"
            ],
            BlockerSummaryRedacted: "none",
            WarningSummaryRedacted: "manifest is historical index/evidence only; not authority live/product; no read precedence",
            NegativeFlags: ProductLedgerLocalOperatorSurfaceLatestStateManifestWriter.NegativeFlags,
            ManifestContentHash: string.Empty,
            CheckpointHash: HashText(snapshot.SnapshotContentHash + "|" + snapshot.CheckpointHash + "|reader-candidate"),
            LocalInternalDevelopmentOnly: true,
            HistoricalIndexEvidenceOnly: true,
            AuthorityLiveProduct: false,
            ProductAuthority: false,
            PublicProduct: false,
            Production: false,
            ReleaseCommercial: false,
            ComplianceCustody: false,
            CloudBackedDurability: false,
            LatestPointer: false,
            LatestPointerOverwrite: false,
            ReadPrecedence: false);
        return payload with { ManifestContentHash = HashManifest(payload) };
    }

    private static string HashManifest(ProductLedgerLocalOperatorSurfaceLatestStateManifestPayload payload) =>
        HashText(JsonSerializer.Serialize(payload with { ManifestContentHash = string.Empty }, JsonOptions));

    private static string HashSnapshot(ProductLedgerLocalOperatorSurfaceLatestStateSnapshotPayload payload) =>
        HashText(JsonSerializer.Serialize(payload with { SnapshotContentHash = string.Empty }, JsonOptions));

    private static string HashText(string material)
    {
        var hash = System.Security.Cryptography.SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(material));
        return Convert.ToHexString(hash).ToLowerInvariant();
    }

    private static string Prefix(string? value) =>
        string.IsNullOrWhiteSpace(value) ? "none" : value[..Math.Min(12, value.Length)];
}
