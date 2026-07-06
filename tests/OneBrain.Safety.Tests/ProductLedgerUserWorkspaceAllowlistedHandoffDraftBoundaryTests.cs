using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace OneBrain.Safety.Tests;

[TestClass]
[TestCategory("ApprovalHumanReview")]
[TestCategory("PhaseEApprovalHumanReview")]
public sealed class ProductLedgerUserWorkspaceAllowlistedHandoffDraftBoundaryTests
{
    private const string FutureAction = "LocalUserWorkspaceAllowlistedHandoffDraftCreateOnly";
    private const string FutureRoute = "/internal/product-ledger/approval/create-user-workspace-allowlisted-handoff-draft";
    private const string FutureStateRoute = "/internal/product-ledger/approval/user-workspace-allowlisted-handoff-draft-state";
    private const string FutureExecutor = "ProductLedgerLocalUserWorkspaceAllowlistedHandoffDraftExecutor";
    private const string AllowedBoundary = "docs/nodal-os/handoffs/";

    [TestMethod]
    public void UserWorkspaceAllowlistedBoundary_DocsDefineTrustedRootPathJailAndExactNextGo()
    {
        var adr = ReadRepoFile(
            "docs",
            "adr",
            "product-ledger-user-workspace-allowlisted-handoff-draft-boundary-design-only.md");
        var report = ReadRepoFile(
            "docs",
            "qa",
            "product-ledger-user-workspace-allowlisted-handoff-draft-boundary-design-only",
            "report.json");
        var handoff = ReadRepoFile(
            "docs",
            "handoff",
            "nodal-os-user-workspace-allowlisted-handoff-draft-boundary-design-only-handoff.md");

        StringAssert.Contains(adr, "Decision: `GO_WITH_FINDINGS_USER_WORKSPACE_ALLOWLISTED_HANDOFF_DRAFT_BOUNDARY_DESIGN_ONLY_READY`");
        StringAssert.Contains(adr, FutureAction);
        StringAssert.Contains(adr, $"POST {FutureRoute}");
        StringAssert.Contains(adr, $"GET {FutureStateRoute}");
        StringAssert.Contains(adr, FutureExecutor);
        StringAssert.Contains(adr, AllowedBoundary);
        StringAssert.Contains(adr, "USER_WORKSPACE_ALLOWLISTED_BOUNDARY_ONLY");
        StringAssert.Contains(adr, "The future implementation must treat workspace root authority as internal configuration");
        StringAssert.Contains(adr, "Payload.");
        StringAssert.Contains(adr, "Query string.");
        StringAssert.Contains(adr, "Header.");
        StringAssert.Contains(adr, "Any user-selected path.");
        StringAssert.Contains(adr, "NO_TRUSTED_WORKSPACE_ROOT_SOURCE");
        StringAssert.Contains(adr, "Reject payload fields named or shaped like");
        StringAssert.Contains(adr, "Validate the final path remains under the canonical allowlisted boundary by path-segment comparison");
        StringAssert.Contains(adr, "Fail closed on symlink, junction, mount point, reparse point or uncertain metadata.");
        StringAssert.Contains(adr, "Allow only `.md`.");
        StringAssert.Contains(adr, "Redaction-before-persistence is mandatory.");
        StringAssert.Contains(adr, "AUTHORIZE_NODAL_OS_USER_WORKSPACE_ALLOWLISTED_HANDOFF_DRAFT_IMPLEMENTATION_WINDOW");

        StringAssert.Contains(report, "\"scope\": \"design-only-readiness-only-test-only-guard-only\"");
        StringAssert.Contains(report, $"\"futureActionName\": \"{FutureAction}\"");
        StringAssert.Contains(report, $"\"allowedBoundary\": \"{AllowedBoundary}\"");
        StringAssert.Contains(report, "\"workspaceRootFromPayload\": false");
        StringAssert.Contains(report, "\"createOnlyNoOverwriteRequired\": true");
        StringAssert.Contains(report, "\"redactionBeforePersistenceRequired\": true");
        StringAssert.Contains(handoff, "Still Not Implemented");
        StringAssert.Contains(handoff, "No write outside workspace test-jail.");
    }

    [TestMethod]
    public void UserWorkspaceAllowlistedBoundary_AuthorizedImplementationIsBoundedInSource()
    {
        var source = SourceText();

        foreach (var requiredFragment in new[]
        {
            FutureAction,
            FutureRoute,
            FutureStateRoute,
            FutureExecutor,
            "ProductLedgerLocalUserWorkspaceAllowlisted",
            "create-user-workspace-allowlisted-handoff-draft",
            AllowedBoundary,
            "USER_WORKSPACE_ALLOWLISTED_BOUNDARY_ONLY"
        })
        {
            StringAssert.Contains(source, requiredFragment);
        }

        StringAssert.Contains(source, "ProductLedgerLocalWorkspaceTestJailHandoffDraftExecutor");
        StringAssert.Contains(source, "/internal/product-ledger/approval/create-workspace-test-jail-handoff-draft");
        StringAssert.Contains(source, "WORKSPACE_TEST_JAIL_ONLY");
        StringAssert.Contains(source, "FileMode.CreateNew");
        StringAssert.Contains(source, "FileAttributes.ReparsePoint");
        StringAssert.Contains(source, "environment.IsDevelopment()");

        var implementationSource = string.Join(
            Environment.NewLine,
            ReadRepoFile("src", "OneBrain.Core", "Approval", "ProductLedgerLocalUserWorkspaceAllowlistedHandoffDraftExecutor.cs"),
            ReadRepoFile("src", "OneBrain.Pilot", "ProductLedgerLocalDevRouteEndpointMapper.cs"));

        foreach (var forbidden in new[]
        {
            "Process.Start",
            "ShellExecute",
            "System.Diagnostics.Process",
            "HttpClient",
            "DbContext",
            "MigrationBuilder",
            "Directory.GetFiles",
            "Directory.EnumerateFiles",
            "FileMode.OpenOrCreate",
            "FileMode.Create,",
            "OverwriteAllowed: true",
            "UserSelectedPathAllowed: true",
            "PayloadControlledRootAllowed: true",
            "ProductionAllowed: true",
            "PublicProductAllowed: true",
            "ReleaseCommercialReady: true"
        })
        {
            Assert.IsFalse(implementationSource.Contains(forbidden, StringComparison.Ordinal), forbidden);
        }
    }

    [TestMethod]
    public void UserWorkspaceAllowlistedBoundary_CurrentChainAndNegativeFrontiersRemainExplicit()
    {
        var adr = ReadRepoFile(
            "docs",
            "adr",
            "product-ledger-user-workspace-allowlisted-handoff-draft-boundary-design-only.md");
        var previousReport = ReadRepoFile(
            "docs",
            "qa",
            "product-ledger-user-workspace-action-outside-test-jail-or-public-product-exposure-authorization-boundary-design-only",
            "report.json");
        var source = ProductLedgerBoundarySourceText();

        foreach (var required in new[]
        {
            "candidate -> approval persisted -> approved no-op execution -> bounded internal marker -> LocalApprovedHandoffReportDraft -> LocalWorkspaceTestJailHandoffDraftCreateOnly",
            "Write outside the workspace test-jail.",
            "Public/product exposure.",
            "Production route.",
            "User-selected path or arbitrary workspace path.",
            "Payload-controlled root, filename or path.",
            "Shell/subprocess or command execution.",
            "Browser/CDP/WCU/OCR/Recipes live authority.",
            "Provider/cloud/network, DB/migration or KMS/WORM/external trust.",
            "Release/commercial readiness or compliance custody."
        })
        {
            StringAssert.Contains(adr, required);
        }

        StringAssert.Contains(previousReport, "\"localWorkspaceTestJailHandoffDraftCreateOnlyImplemented\": true");
        StringAssert.Contains(previousReport, "\"writeRealOnlyInsideWorkspaceTestJail\": true");
        StringAssert.Contains(source, "ProductLedgerLocalApprovedHandoffReportDraftExecutor");
        StringAssert.Contains(source, "ProductLedgerLocalWorkspaceTestJailHandoffDraftExecutor");
        Assert.IsFalse(source.Contains("Process.Start", StringComparison.Ordinal), "Process.Start");
        Assert.IsFalse(source.Contains("System.Diagnostics.Process", StringComparison.Ordinal), "System.Diagnostics.Process");
        Assert.IsFalse(source.Contains("HttpClient", StringComparison.Ordinal), "HttpClient");
        Assert.IsFalse(source.Contains("DbContext", StringComparison.Ordinal), "DbContext");
        Assert.IsFalse(source.Contains("MigrationBuilder", StringComparison.Ordinal), "MigrationBuilder");
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
            ReadRepoFile("src", "OneBrain.Core", "Approval", "ProductLedgerLocalApprovedHandoffReportDraftExecutor.cs"),
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
