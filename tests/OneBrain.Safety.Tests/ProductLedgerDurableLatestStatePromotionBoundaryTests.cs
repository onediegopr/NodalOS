using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace OneBrain.Safety.Tests;

[TestClass]
[TestCategory("ApprovalHumanReview")]
[TestCategory("PhaseEApprovalHumanReview")]
public sealed class ProductLedgerDurableLatestStatePromotionBoundaryTests
{
    private const string Decision = "GO_WITH_FINDINGS_DURABLE_LATEST_STATE_PROMOTION_BOUNDARY_DESIGN_ONLY_READY";
    private const string FutureActionKind = "LocalDurableLatestStateManifestCreateOnly";
    private const string FutureReadKind = "LocalDurableLatestStateReadCandidateNotAuthority";
    private const string FutureWriter = "ProductLedgerLocalDurableLatestStateManifestWriter";
    private const string FutureValidator = "ProductLedgerLocalDurableLatestStateManifestValidator";
    private const string FutureReader = "ProductLedgerLocalDurableLatestStateCandidateReader";
    private const string ImplementedManifestWriter = "ProductLedgerLocalOperatorSurfaceLatestStateManifestWriter";
    private const string ImplementedManifestCreateRoute = "/internal/product-ledger/operator-surface/create-latest-state-manifest";
    private const string InputSnapshotBoundary = "docs/test-output/product-ledger/operator-surface-latest-state-snapshots/";
    private const string OutputManifestBoundary = "docs/test-output/product-ledger/operator-surface-latest-state-manifests/";
    private const string Classification = "LOCAL_INTERNAL_DEV_ONLY_VERSIONED_MANIFEST_NOT_AUTHORITY";
    private const string FutureCreateRoute = "/internal/product-ledger/operator-surface/create-durable-latest-state-manifest";
    private const string FutureStateRoute = "/internal/product-ledger/operator-surface/durable-latest-state-candidate-state";
    private const string NextGo = "AUTHORIZE_NODAL_OS_DURABLE_LATEST_STATE_MANIFEST_CREATE_ONLY_IMPLEMENTATION_WINDOW";

    [TestMethod]
    public void DurableLatestStatePromotionBoundary_DocsDefineRecommendationAndNextGo()
    {
        var adr = ReadRepoFile("docs", "adr", "product-ledger-durable-latest-state-promotion-boundary-design-only.md");
        var report = ReadRepoFile("docs", "qa", "product-ledger-durable-latest-state-promotion-boundary-design-only", "report.json");
        var handoff = ReadRepoFile("docs", "handoff", "nodal-os-durable-latest-state-promotion-boundary-design-only-handoff.md");
        var roadmap = ReadRepoFile("docs", "roadmap", "product-ledger-durable-latest-state-promotion-boundary-design-only.md");

        StringAssert.Contains(adr, $"Decision: `{Decision}`");
        StringAssert.Contains(adr, "A. Keep snapshots as historical evidence only");
        StringAssert.Contains(adr, "B. Promote latest snapshot to local durable read-model candidate, but not authority");
        StringAssert.Contains(adr, "C. Promote latest snapshot to local durable read source for Development-only operator surface");
        StringAssert.Contains(adr, "D. Create explicit latest-state manifest/index create-only, no overwrite, versioned, but not product authority");
        StringAssert.Contains(adr, "E. Two-phase D then B: manifest candidate first, reader later");
        StringAssert.Contains(adr, "Recommend option D");
        StringAssert.Contains(adr, FutureActionKind);
        StringAssert.Contains(adr, FutureReadKind);
        StringAssert.Contains(adr, FutureWriter);
        StringAssert.Contains(adr, FutureValidator);
        StringAssert.Contains(adr, FutureReader);
        StringAssert.Contains(adr, InputSnapshotBoundary);
        StringAssert.Contains(adr, OutputManifestBoundary);
        StringAssert.Contains(adr, Classification);
        StringAssert.Contains(adr, FutureCreateRoute);
        StringAssert.Contains(adr, FutureStateRoute);
        StringAssert.Contains(adr, "No mutable `latest.json`.");
        StringAssert.Contains(adr, "No latest pointer overwrite.");
        StringAssert.Contains(adr, NextGo);

        StringAssert.Contains(report, "\"scope\": \"design-only-readiness-only-audit-only-test-only-guard-only\"");
        StringAssert.Contains(report, $"\"futureActionKind\": \"{FutureActionKind}\"");
        StringAssert.Contains(report, $"\"outputManifestBoundary\": \"{OutputManifestBoundary}\"");
        StringAssert.Contains(report, $"\"authorityClassification\": \"{Classification}\"");
        StringAssert.Contains(report, "\"durableLatestStatePromotionImplemented\": false");
        StringAssert.Contains(report, "\"activeDurableReaderEnabled\": false");
        StringAssert.Contains(report, "\"latestPointerOverwriteEnabled\": false");
        StringAssert.Contains(report, $"\"exactNextGoRequired\": \"{NextGo}\"");

        StringAssert.Contains(handoff, "Still Not Implemented");
        StringAssert.Contains(handoff, "No durable latest-state promotion.");
        StringAssert.Contains(handoff, "No active durable reader.");
        StringAssert.Contains(handoff, NextGo);
        StringAssert.Contains(roadmap, "Evidence/Timeline/Audit Trail: 97-99% -> 98-99%.");
    }

    [TestMethod]
    public void DurableLatestStatePromotionBoundary_SourceHasManifestCreateOnlyButNoReaderPromotionOrProductRoute()
    {
        var source = SourceText();

        foreach (var forbidden in new[]
        {
            FutureReadKind,
            FutureWriter,
            FutureValidator,
            FutureReader,
            FutureCreateRoute,
            FutureStateRoute,
            "LOCAL_INTERNAL_DEV_ONLY_DURABLE_READ_CANDIDATE",
            "latest.json",
            "LatestPointerOverwriteAllowed: true",
            "LatestPointerAvailable: true",
            "ReadPrecedenceAllowed: true",
            "AuthorityLiveProduct: true",
            "ProductAuthority: true",
            "PublicProductAllowed: true",
            "ProductionAllowed: true",
            "/public/product-ledger",
            "FileMode.OpenOrCreate",
            "FileMode.Create,",
            "File.Delete(",
            "Process.Start",
            "ShellExecute",
            "System.Diagnostics.Process",
            "HttpClient",
            "DbContext",
            "MigrationBuilder"
        })
        {
            Assert.IsFalse(source.Contains(forbidden, StringComparison.Ordinal), forbidden);
        }

        StringAssert.Contains(source, "LocalOperatorSurfaceLatestStateSnapshotCreateOnly");
        StringAssert.Contains(source, InputSnapshotBoundary);
        StringAssert.Contains(source, "LOCAL_INTERNAL_DEV_ONLY_HISTORICAL_SNAPSHOT");
        StringAssert.Contains(source, "HISTORICAL_EVIDENCE_ONLY");
        StringAssert.Contains(source, "NO_LATEST_POINTER_OVERWRITE");
        StringAssert.Contains(source, ImplementedManifestWriter);
        StringAssert.Contains(source, ImplementedManifestCreateRoute);
        StringAssert.Contains(source, OutputManifestBoundary);
        StringAssert.Contains(source, Classification);
        StringAssert.Contains(source, "NO_LATEST_POINTER");
        StringAssert.Contains(source, "NO_READ_PRECEDENCE");
    }

    private static string SourceText()
    {
        var root = RepoRoot();
        var sourceFiles = Directory.EnumerateFiles(Path.Combine(root, "src"), "*.cs", SearchOption.AllDirectories)
            .Where(path => path.Contains(Path.Combine("OneBrain.Core", "Approval", "ProductLedger"), StringComparison.Ordinal)
                || path.EndsWith(Path.Combine("OneBrain.Pilot", "ProductLedgerLocalDevRouteEndpointMapper.cs"), StringComparison.Ordinal))
            .OrderBy(path => path, StringComparer.Ordinal)
            .Select(File.ReadAllText);
        return string.Join(Environment.NewLine, sourceFiles);
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
}
