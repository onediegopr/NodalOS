using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace OneBrain.Safety.Tests;

[TestClass]
[TestCategory("ApprovalHumanReview")]
[TestCategory("PhaseEApprovalHumanReview")]
public sealed class ProductLedgerUserWorkspaceOrPublicProductAuthorizationBoundaryTests
{
    private const string FutureAction = "LocalUserWorkspaceAllowlistedHandoffDraftCreateOnly";
    private const string FutureRoute = "/internal/product-ledger/approval/create-user-workspace-allowlisted-handoff-draft";
    private const string FutureStateRoute = "/internal/product-ledger/approval/user-workspace-allowlisted-handoff-draft-state";
    private const string FutureExecutor = "ProductLedgerLocalUserWorkspaceAllowlistedHandoffDraftExecutor";
    private const string RecommendedBoundary = "docs/nodal-os/handoffs/";

    [TestMethod]
    public void UserWorkspaceOrPublicProductBoundary_DocsDefineMatrixRecommendationAndExactNextGo()
    {
        var adr = ReadRepoFile(
            "docs",
            "adr",
            "product-ledger-user-workspace-action-outside-test-jail-or-public-product-exposure-authorization-boundary-design-only.md");
        var report = ReadRepoFile(
            "docs",
            "qa",
            "product-ledger-user-workspace-action-outside-test-jail-or-public-product-exposure-authorization-boundary-design-only",
            "report.json");
        var handoff = ReadRepoFile(
            "docs",
            "handoff",
            "nodal-os-user-workspace-action-outside-test-jail-or-public-product-exposure-authorization-boundary-design-only-handoff.md");

        StringAssert.Contains(adr, "Decision: `GO_WITH_FINDINGS_USER_WORKSPACE_OR_PUBLIC_PRODUCT_AUTHORIZATION_BOUNDARY_DESIGN_ONLY_READY`");
        StringAssert.Contains(adr, "design-only, readiness-only, audit-only, test-only and guard-only");
        StringAssert.Contains(adr, "A. Controlled user-workspace allowlisted write outside test-jail");
        StringAssert.Contains(adr, "B. Public/product exposure of local operator surface/action path");
        StringAssert.Contains(adr, "Recommend option A");
        StringAssert.Contains(adr, FutureAction);
        StringAssert.Contains(adr, FutureRoute);
        StringAssert.Contains(adr, FutureStateRoute);
        StringAssert.Contains(adr, FutureExecutor);
        StringAssert.Contains(adr, RecommendedBoundary);
        StringAssert.Contains(adr, "No user-selected arbitrary path");
        StringAssert.Contains(adr, "No payload-controlled root");
        StringAssert.Contains(adr, "No filesystem scan");
        StringAssert.Contains(adr, "Redaction-before-persistence required");
        StringAssert.Contains(adr, "Production blocking");
        StringAssert.Contains(adr, "Public/product blocking");
        StringAssert.Contains(adr, "AUTHORIZE_NODAL_OS_USER_WORKSPACE_ALLOWLISTED_HANDOFF_DRAFT_BOUNDARY_DESIGN_ONLY");
        StringAssert.Contains(report, "\"scope\": \"design-only-readiness-only-audit-only-test-only-guard-only\"");
        StringAssert.Contains(report, $"\"futureActionName\": \"{FutureAction}\"");
        StringAssert.Contains(report, $"\"recommendedAllowedBoundary\": \"{RecommendedBoundary}\"");
        StringAssert.Contains(report, "\"publicProductExposureRecommendedNow\": false");
        StringAssert.Contains(handoff, "Still Not Implemented");
        StringAssert.Contains(handoff, "No action outside workspace test-jail.");
        StringAssert.Contains(handoff, "No public/product exposure.");
    }

    [TestMethod]
    public void UserWorkspaceOrPublicProductBoundary_FutureNamesRemainDocsAndTestsOnly()
    {
        var source = SourceText();

        foreach (var futureFragment in new[]
        {
            FutureAction,
            FutureRoute,
            FutureStateRoute,
            FutureExecutor,
            "ProductLedgerLocalUserWorkspaceAllowlisted",
            "create-user-workspace-allowlisted-handoff-draft",
            RecommendedBoundary
        })
        {
            Assert.IsFalse(source.Contains(futureFragment, StringComparison.Ordinal), futureFragment);
        }

        StringAssert.Contains(source, "ProductLedgerLocalWorkspaceTestJailHandoffDraftExecutor");
        StringAssert.Contains(source, "/internal/product-ledger/approval/create-workspace-test-jail-handoff-draft");
    }

    [TestMethod]
    public void UserWorkspaceOrPublicProductBoundary_StaticScanKeepsForbiddenFrontiersClosed()
    {
        var source = ProductLedgerBoundarySourceText();

        foreach (var forbidden in new[]
        {
            "ProductLedgerLocalUserWorkspaceAllowlistedHandoffDraftExecutor",
            "LocalUserWorkspaceAllowlistedHandoffDraftRoute",
            "/internal/product-ledger/approval/create-user-workspace-allowlisted-handoff-draft",
            "/internal/product-ledger/approval/user-workspace-allowlisted-handoff-draft-state",
            "docs/nodal-os/handoffs/",
            "UserSelectedPathAllowed: true",
            "PayloadControlledRootAllowed: true",
            "ProductionAllowed: true",
            "PublicProductAllowed: true",
            "ReleaseCommercialReady: true",
            "ProductCommandExecuted: true"
        })
        {
            Assert.IsFalse(source.Contains(forbidden, StringComparison.Ordinal), forbidden);
        }

        var mapper = ReadRepoFile("src", "OneBrain.Pilot", "ProductLedgerLocalDevRouteEndpointMapper.cs");
        StringAssert.Contains(mapper, "environment.IsDevelopment()");
        Assert.IsFalse(mapper.Contains(FutureRoute, StringComparison.Ordinal), FutureRoute);
        Assert.IsFalse(mapper.Contains(FutureStateRoute, StringComparison.Ordinal), FutureStateRoute);
    }

    [TestMethod]
    public void UserWorkspaceOrPublicProductBoundary_CurrentWorkspaceTestJailRealityIsStillVisible()
    {
        var boundaryReport = ReadRepoFile(
            "docs",
            "qa",
            "product-ledger-user-workspace-action-outside-test-jail-or-public-product-exposure-authorization-boundary-design-only",
            "report.json");
        var auditReport = ReadRepoFile(
            "docs",
            "qa",
            "product-ledger-workspace-test-jail-handoff-draft-external-audit-read-only",
            "report.json");

        StringAssert.Contains(boundaryReport, "\"localWorkspaceTestJailHandoffDraftCreateOnlyImplemented\": true");
        StringAssert.Contains(boundaryReport, "\"writeRealOnlyInsideWorkspaceTestJail\": true");
        StringAssert.Contains(boundaryReport, "\"productionRoute\": true");
        StringAssert.Contains(auditReport, "\"userWorkspaceOutsideTestJailAllowed\": false");
        StringAssert.Contains(auditReport, "\"publicProductPathAllowed\": false");
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
            ReadRepoFile("src", "OneBrain.Core", "Approval", "ProductLedgerOperatorSurfaceModel.cs"),
            ReadRepoFile("src", "OneBrain.Core", "Approval", "ProductLedgerLocalDevRoutePreview.cs"),
            ReadRepoFile("src", "OneBrain.Core", "Approval", "ProductLedgerLocalWorkspaceTestJailHandoffDraftExecutor.cs"));

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
