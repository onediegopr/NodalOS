using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace OneBrain.Safety.Tests;

[TestClass]
[TestCategory("ApprovalHumanReview")]
[TestCategory("PhaseEApprovalHumanReview")]
public sealed class ProductLedgerBroaderWorkspaceOrPublicProductBoundaryTests
{
    private const string FutureAction = "LocalOperatorSurfaceLatestStateSnapshotCreateOnly";
    private const string FutureRoute = "/internal/product-ledger/operator-surface/latest-state-snapshot";
    private const string FutureStateRoute = "/internal/product-ledger/operator-surface/latest-state-snapshot-state";
    private const string FutureExecutor = "ProductLedgerLocalOperatorSurfaceLatestStateSnapshotExecutor";
    private const string FutureBoundary = "docs/test-output/product-ledger/operator-surface-latest-state/snapshots/";

    [TestMethod]
    public void BroaderWorkspaceOrPublicProductBoundary_DocsDefineMatrixRecommendationAndNextGo()
    {
        var adr = ReadRepoFile(
            "docs",
            "adr",
            "product-ledger-broader-user-workspace-action-or-public-product-exposure-boundary-design-only.md");
        var report = ReadRepoFile(
            "docs",
            "qa",
            "product-ledger-broader-user-workspace-action-or-public-product-exposure-boundary-design-only",
            "report.json");
        var handoff = ReadRepoFile(
            "docs",
            "handoff",
            "nodal-os-broader-user-workspace-action-or-public-product-exposure-boundary-design-only-handoff.md");

        StringAssert.Contains(adr, "Decision: `GO_WITH_FINDINGS_BROADER_USER_WORKSPACE_OR_PUBLIC_PRODUCT_EXPOSURE_BOUNDARY_DESIGN_ONLY_READY`");
        StringAssert.Contains(adr, "A. Broader user-workspace create-only action in another allowlisted boundary");
        StringAssert.Contains(adr, "B. Controlled edit/update action under `docs/nodal-os/handoffs/`");
        StringAssert.Contains(adr, "C. Public/product exposure of local operator surface/action path");
        StringAssert.Contains(adr, "D. Durable/latest-state persistence hardening before public/product");
        StringAssert.Contains(adr, "Recommend option D");
        StringAssert.Contains(adr, FutureAction);
        StringAssert.Contains(adr, FutureRoute);
        StringAssert.Contains(adr, FutureStateRoute);
        StringAssert.Contains(adr, FutureExecutor);
        StringAssert.Contains(adr, FutureBoundary);
        StringAssert.Contains(adr, "No latest pointer overwrite.");
        StringAssert.Contains(adr, "Future names may appear only in docs/tests until an implementation GO is granted.");
        StringAssert.Contains(adr, "AUTHORIZE_NODAL_OS_LOCAL_OPERATOR_SURFACE_LATEST_STATE_SNAPSHOT_BOUNDARY_DESIGN_ONLY");
        StringAssert.Contains(report, "\"scope\": \"design-only-readiness-only-audit-only-test-only-guard-only\"");
        StringAssert.Contains(report, $"\"futureActionName\": \"{FutureAction}\"");
        StringAssert.Contains(report, "\"publicProductExposureRecommendedNow\": false");
        StringAssert.Contains(report, "\"editUpdateDeleteRecommendedNow\": false");
        StringAssert.Contains(handoff, "Still Not Implemented");
        StringAssert.Contains(handoff, "No public/product exposure.");
    }

    [TestMethod]
    public void BroaderWorkspaceOrPublicProductBoundary_LatestStateSnapshotImplementationIsAuthorizedButOtherFrontiersRemainClosed()
    {
        var source = SourceText();

        StringAssert.Contains(source, FutureAction);
        StringAssert.Contains(source, FutureStateRoute);
        StringAssert.Contains(source, FutureExecutor);
        StringAssert.Contains(source, "ProductLedgerLocalOperatorSurfaceLatestStateSnapshot");
        StringAssert.Contains(source, "operator-surface-latest-state-snapshots");
        StringAssert.Contains(source, "HISTORICAL_EVIDENCE_ONLY");
        StringAssert.Contains(source, "ProductLedgerLocalUserWorkspaceAllowlistedHandoffDraftExecutor");
        StringAssert.Contains(source, "/internal/product-ledger/approval/create-user-workspace-allowlisted-handoff-draft");
        NodalOsStaticGuardCatalog.AssertNoForbiddenMatches(
            source,
            NodalOsStaticGuardCategory.PublicProductExposure,
            NodalOsStaticGuardCategory.ProductionRoutes);
        Assert.IsFalse(source.Contains("/public/product-ledger", StringComparison.Ordinal), "/public/product-ledger");
        Assert.IsFalse(source.Contains("PublicProductAllowed: true", StringComparison.Ordinal), "PublicProductAllowed");
        Assert.IsFalse(source.Contains("ProductionAllowed: true", StringComparison.Ordinal), "ProductionAllowed");
        Assert.IsFalse(source.Contains("ReleaseCommercialReady: true,", StringComparison.Ordinal), "ReleaseCommercialReady");
    }

    [TestMethod]
    public void BroaderWorkspaceOrPublicProductBoundary_PublicProductMutationAndUnsafeFrontiersRemainClosed()
    {
        var source = ProductLedgerBoundarySourceText();

        NodalOsStaticGuardCatalog.AssertNoForbiddenMatches(
            source,
            NodalOsStaticGuardCategory.PublicProductExposure,
            NodalOsStaticGuardCategory.ProductionRoutes);

        foreach (var forbidden in new[]
        {
            "/public/product-ledger",
            "PublicProductAllowed: true",
            "ProductionAllowed: true",
            "ReleaseCommercialReady: true",
            "ProductCommandExecuted: true",
            "UserSelectedPathAllowed: true",
            "PayloadControlledRootAllowed: true",
            "OverwriteAllowed: true",
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
        StringAssert.Contains(source, "NO_OVERWRITE");
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
            ReadRepoFile("src", "OneBrain.Core", "Approval", "ProductLedgerLocalUserWorkspaceAllowlistedHandoffDraftExecutor.cs"));

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
