using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace OneBrain.Safety.Tests;

[TestClass]
[TestCategory("ApprovalHumanReview")]
[TestCategory("PhaseEApprovalHumanReview")]
public sealed class ProductLedgerWorkspaceTestJailHandoffDraftBoundaryTests
{
    private const string FutureAction = "LocalWorkspaceTestJailHandoffDraftCreateOnly";
    private const string FutureRoute = "/internal/product-ledger/approval/create-workspace-test-jail-handoff-draft";
    private const string FutureExecutor = "ProductLedgerLocalWorkspaceTestJailHandoffDraftExecutor";

    [TestMethod]
    public void WorkspaceTestJailBoundary_DocsDefineCanonicalizationReparseAndCreateOnlyRules()
    {
        var adr = ReadRepoFile(
            "docs",
            "adr",
            "product-ledger-workspace-test-jail-handoff-draft-boundary-design-only.md");
        var report = ReadRepoFile(
            "docs",
            "qa",
            "product-ledger-workspace-test-jail-handoff-draft-boundary-design-only",
            "report.json");
        var handoff = ReadRepoFile(
            "docs",
            "handoff",
            "nodal-os-workspace-test-jail-handoff-draft-boundary-design-only-handoff.md");

        StringAssert.Contains(adr, "Decision: `GO_WITH_FINDINGS_WORKSPACE_TEST_JAIL_HANDOFF_DRAFT_BOUNDARY_DESIGN_ONLY_READY`");
        StringAssert.Contains(adr, FutureAction);
        StringAssert.Contains(adr, $"POST {FutureRoute}");
        StringAssert.Contains(adr, FutureExecutor);
        StringAssert.Contains(adr, "Canonicalize the registered root before any write.");
        StringAssert.Contains(adr, "Re-read/reparse the parent directory after creation.");
        StringAssert.Contains(adr, "path-segment comparison, not string prefix alone");
        StringAssert.Contains(adr, "fail closed when platform APIs report a symlink, junction, mount point, reparse point or uncertain metadata");
        StringAssert.Contains(adr, "The future writer must use create-new semantics.");
        StringAssert.Contains(adr, "Use create-only semantics.");
        StringAssert.Contains(adr, "Redaction-before-persistence is mandatory.");
        StringAssert.Contains(report, "\"status\": \"NOT_IMPLEMENTED_DESIGN_ONLY\"");
        StringAssert.Contains(report, "\"workspaceWriteImplemented\": false");
        StringAssert.Contains(report, "\"activeRouteImplementedInSrc\": false");
        StringAssert.Contains(report, "\"activeExecutorImplementedInSrc\": false");
        StringAssert.Contains(handoff, "No workspace write.");
        StringAssert.Contains(handoff, "That future GO would be the first window allowed to implement the workspace test-jail write.");
    }

    [TestMethod]
    public void WorkspaceTestJailBoundary_ImplementedActionRemainsDevelopmentOnlyWorkspaceTestJailSource()
    {
        var mapper = ReadRepoFile("src", "OneBrain.Pilot", "ProductLedgerLocalDevRouteEndpointMapper.cs");
        var approvalSources = string.Join(
            Environment.NewLine,
            Directory.EnumerateFiles(Path.Combine(RepoRoot(), "src", "OneBrain.Core", "Approval"), "*.cs")
                .Select(File.ReadAllText));

        StringAssert.Contains(mapper, FutureRoute);
        StringAssert.Contains(mapper, "LocalWorkspaceTestJailHandoffDraftStateRoute");
        StringAssert.Contains(mapper, "environment.IsDevelopment()");
        StringAssert.Contains(mapper, FutureExecutor);
        StringAssert.Contains(approvalSources, FutureAction);
        StringAssert.Contains(approvalSources, FutureExecutor);
        StringAssert.Contains(approvalSources, "FileMode.CreateNew");
        StringAssert.Contains(approvalSources, "FileAttributes.ReparsePoint");
        StringAssert.Contains(approvalSources, "WORKSPACE_TEST_JAIL_ONLY");

        StringAssert.Contains(mapper, "/internal/product-ledger/approval/create-local-handoff-draft");
        StringAssert.Contains(approvalSources, "ProductLedgerLocalApprovedHandoffReportDraftExecutor");
    }

    [TestMethod]
    public void WorkspaceTestJailBoundary_StaticScanKeepsImplementedFrontierClosedOutsideTestJail()
    {
        var docs = string.Join(
            Environment.NewLine,
            ReadRepoFile(
                "docs",
                "adr",
                "product-ledger-workspace-test-jail-handoff-draft-boundary-design-only.md"),
            ReadRepoFile(
                "docs",
                "handoff",
                "nodal-os-workspace-test-jail-handoff-draft-boundary-design-only-handoff.md"));
        var source = string.Join(
            Environment.NewLine,
            ReadRepoFile("src", "OneBrain.Pilot", "ProductLedgerLocalDevRouteEndpointMapper.cs"),
            ReadRepoFile("src", "OneBrain.Core", "Approval", "ProductLedgerLocalWorkspaceTestJailHandoffDraftExecutor.cs"));

        foreach (var requiredNegative in new[]
        {
            "No workspace write",
            "No active route",
            "No public/product path",
            "No Production route",
            "No shell/subprocess",
            "No command execution",
            "No Browser/CDP/WCU/OCR/Recipes live",
            "No Pilot `/run`",
            "No provider/cloud/network",
            "No DB/migration",
            "No KMS/WORM/external trust",
            "No release/commercial"
        })
        {
            StringAssert.Contains(docs, requiredNegative);
        }

        foreach (var requiredSource in new[]
        {
            FutureRoute,
            FutureAction,
            FutureExecutor,
            "WorkspaceTestJailHandoffDraftExecutor",
            "create-workspace-test-jail-handoff-draft",
            "FileMode.CreateNew",
            "FileAttributes.ReparsePoint",
            "NO_USER_SELECTED_PATH",
            "NO_COMMAND_EXECUTION",
            "NO_SHELL_SUBPROCESS"
        })
        {
            StringAssert.Contains(source, requiredSource);
        }

        foreach (var forbiddenSource in new[]
        {
            "Process.Start",
            "ShellExecute",
            "System.Diagnostics.Process",
            "HttpClient",
            "DbContext",
            "MigrationBuilder",
            "KmsClient",
            "WormStore",
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
            Assert.IsFalse(source.Contains(forbiddenSource, StringComparison.Ordinal), forbiddenSource);
        }
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
