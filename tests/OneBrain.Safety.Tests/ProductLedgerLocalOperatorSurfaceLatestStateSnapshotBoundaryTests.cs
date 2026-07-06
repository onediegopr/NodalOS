using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace OneBrain.Safety.Tests;

[TestClass]
[TestCategory("ApprovalHumanReview")]
[TestCategory("PhaseEApprovalHumanReview")]
public sealed class ProductLedgerLocalOperatorSurfaceLatestStateSnapshotBoundaryTests
{
    private const string FutureAction = "LocalOperatorSurfaceLatestStateSnapshotCreateOnly";
    private const string FutureRoute = "/internal/product-ledger/operator-surface/latest-state-snapshot";
    private const string ImplementationRoute = "/internal/product-ledger/operator-surface/create-latest-state-snapshot";
    private const string FutureStateRoute = "/internal/product-ledger/operator-surface/latest-state-snapshot-state";
    private const string FutureExecutor = "ProductLedgerLocalOperatorSurfaceLatestStateSnapshotExecutor";
    private const string RecommendedBoundary = "docs/test-output/product-ledger/operator-surface-latest-state-snapshots/";
    private const string HistoricalBoundary = "docs/test-output/product-ledger/operator-surface-latest-state/snapshots/";

    [TestMethod]
    public void LatestStateSnapshotBoundary_DocsDefineRecommendedBoundaryAndNextGo()
    {
        var adr = ReadRepoFile(
            "docs",
            "adr",
            "product-ledger-local-operator-surface-latest-state-snapshot-boundary-design-only.md");
        var report = ReadRepoFile(
            "docs",
            "qa",
            "product-ledger-local-operator-surface-latest-state-snapshot-boundary-design-only",
            "report.json");
        var handoff = ReadRepoFile(
            "docs",
            "handoff",
            "nodal-os-local-operator-surface-latest-state-snapshot-boundary-design-only-handoff.md");

        StringAssert.Contains(adr, "Decision: `GO_WITH_FINDINGS_LOCAL_OPERATOR_SURFACE_LATEST_STATE_SNAPSHOT_BOUNDARY_DESIGN_ONLY_READY`");
        StringAssert.Contains(adr, "A. Snapshot create-only under `docs/test-output/product-ledger/operator-surface-latest-state-snapshots/`");
        StringAssert.Contains(adr, "B. Snapshot create-only under `docs/nodal-os/operator-surface-snapshots/`");
        StringAssert.Contains(adr, "C. Snapshot in-memory only hardening, no persistence");
        StringAssert.Contains(adr, "D. More constrained local-temp snapshot boundary");
        StringAssert.Contains(adr, "Recommend option A");
        StringAssert.Contains(adr, FutureAction);
        StringAssert.Contains(adr, FutureRoute);
        StringAssert.Contains(adr, FutureStateRoute);
        StringAssert.Contains(adr, FutureExecutor);
        StringAssert.Contains(adr, RecommendedBoundary);
        StringAssert.Contains(adr, HistoricalBoundary);
        StringAssert.Contains(adr, "No latest pointer overwrite.");
        StringAssert.Contains(adr, "Create-only versioned filename.");
        StringAssert.Contains(adr, "Stale state cannot authorize product/public exposure.");
        StringAssert.Contains(adr, "AUTHORIZE_NODAL_OS_LOCAL_OPERATOR_SURFACE_LATEST_STATE_SNAPSHOT_IMPLEMENTATION_WINDOW");
        StringAssert.Contains(report, "\"scope\": \"design-only-readiness-only-test-only-guard-only\"");
        StringAssert.Contains(report, $"\"futureActionName\": \"{FutureAction}\"");
        StringAssert.Contains(report, $"\"allowedBoundary\": \"{RecommendedBoundary}\"");
        StringAssert.Contains(report, "\"activeSnapshotWriter\": true");
        StringAssert.Contains(report, "\"activeSnapshotRoute\": true");
        StringAssert.Contains(handoff, "Still Not Implemented");
        StringAssert.Contains(handoff, "No active snapshot writer.");
        StringAssert.Contains(handoff, "No active snapshot route.");
    }

    [TestMethod]
    public void LatestStateSnapshotBoundary_ImplementationNamesAppearOnlyInApprovedLocalInternalSource()
    {
        var source = SourceText();

        StringAssert.Contains(source, FutureAction);
        StringAssert.Contains(source, ImplementationRoute);
        StringAssert.Contains(source, FutureStateRoute);
        StringAssert.Contains(source, FutureExecutor);
        StringAssert.Contains(source, "ProductLedgerLocalOperatorSurfaceLatestStateSnapshot");
        StringAssert.Contains(source, "operator-surface-latest-state-snapshots");
        StringAssert.Contains(source, "LOCAL_INTERNAL_DEV_ONLY_HISTORICAL_SNAPSHOT");
        StringAssert.Contains(source, "HISTORICAL_EVIDENCE_ONLY");
        StringAssert.Contains(source, "NO_LATEST_POINTER_OVERWRITE");
        StringAssert.Contains(source, "ProductLedgerLocalUserWorkspaceAllowlistedHandoffDraftExecutor");
        StringAssert.Contains(source, "/internal/product-ledger/approval/create-user-workspace-allowlisted-handoff-draft");
    }

    [TestMethod]
    public void LatestStateSnapshotBoundary_ActiveRouteWriterAndUnsafeFrontiersRemainClosed()
    {
        var source = ProductLedgerBoundarySourceText();

        foreach (var forbidden in new[]
        {
            "MapPost(\"/internal/product-ledger/operator-surface/latest-state-snapshot\"",
            "/public/product-ledger",
            "PublicProductAllowed: true",
            "ProductionAllowed: true",
            "AuthorityLiveProduct: true",
            "ReleaseCommercialReady: true",
            "UserSelectedPathAllowed: true",
            "OverwriteAllowed: true",
            "LatestPointerOverwriteAllowed: true",
            "FileMode.OpenOrCreate",
            "FileMode.Create,",
            "File.Delete(",
            "Process.Start",
            "ShellExecute",
            "System.Diagnostics.Process",
            "HttpClient",
            "DbContext",
            "MigrationBuilder",
            "Directory.GetFiles",
            "Directory.EnumerateFiles"
        })
        {
            Assert.IsFalse(source.Contains(forbidden, StringComparison.Ordinal), forbidden);
        }

        StringAssert.Contains(source, "environment.IsDevelopment()");
        StringAssert.Contains(source, "FileMode.CreateNew");
        StringAssert.Contains(source, ImplementationRoute);
        StringAssert.Contains(source, FutureStateRoute);
        StringAssert.Contains(source, "NO_OVERWRITE");
        StringAssert.Contains(source, "NO_LATEST_POINTER_OVERWRITE");
        StringAssert.Contains(source, "NO_COMMAND_EXECUTION");
        StringAssert.Contains(source, "NO_SHELL_SUBPROCESS");
    }

    private static string SourceText()
    {
        var root = RepoRoot();
        var sourceFiles = Directory.EnumerateFiles(Path.Combine(root, "src"), "*.cs", SearchOption.AllDirectories)
            .Where(path => path.Contains(Path.Combine("OneBrain.Core", "Approval"), StringComparison.Ordinal)
                || path.Contains("OneBrain.Pilot", StringComparison.Ordinal))
            .OrderBy(path => path, StringComparer.Ordinal)
            .Select(File.ReadAllText);
        return string.Join(Environment.NewLine, sourceFiles);
    }

    private static string ProductLedgerBoundarySourceText() =>
        string.Join(
            Environment.NewLine,
            ReadRepoFile("src", "OneBrain.Pilot", "ProductLedgerLocalDevRouteEndpointMapper.cs"),
            ReadRepoFile("src", "OneBrain.Core", "Approval", "ProductLedgerLocalApprovedHandoffReportDraftExecutor.cs"),
            ReadRepoFile("src", "OneBrain.Core", "Approval", "ProductLedgerLocalWorkspaceTestJailHandoffDraftExecutor.cs"),
            ReadRepoFile("src", "OneBrain.Core", "Approval", "ProductLedgerLocalUserWorkspaceAllowlistedHandoffDraftExecutor.cs"),
            ReadRepoFile("src", "OneBrain.Core", "Approval", "ProductLedgerLocalOperatorSurfaceLatestStateSnapshotExecutor.cs"));

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
