using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace OneBrain.Safety.Tests;

[TestClass]
[TestCategory("ApprovalHumanReview")]
[TestCategory("PhaseEApprovalHumanReview")]
public sealed class ProductLedgerPublicProductOrWorkspaceActionAuthorizationReadinessTests
{
    private const string FutureWorkspaceAction = "LocalWorkspaceTestJailHandoffDraftCreateOnly";
    private const string FutureWorkspaceRoute = "/internal/product-ledger/approval/create-workspace-test-jail-handoff-draft";

    [TestMethod]
    public void PublicProductOrWorkspaceAuthorizationReadiness_DocsSelectWorkspaceTestJailDesignOnly()
    {
        var adr = ReadRepoFile(
            "docs",
            "adr",
            "product-ledger-public-product-or-user-workspace-action-authorization-readiness-design-only.md");
        var report = ReadRepoFile(
            "docs",
            "qa",
            "product-ledger-public-product-or-user-workspace-action-authorization-readiness-design-only",
            "report.json");
        var handoff = ReadRepoFile(
            "docs",
            "handoff",
            "nodal-os-public-product-or-user-workspace-action-authorization-readiness-design-only-handoff.md");

        StringAssert.Contains(adr, "Decision: `GO_WITH_FINDINGS_PUBLIC_PRODUCT_OR_USER_WORKSPACE_ACTION_AUTHORIZATION_READINESS_DESIGN_ONLY_READY`");
        StringAssert.Contains(adr, "design-only, readiness-only, audit-only and test-only");
        StringAssert.Contains(adr, "Recommend option D as the immediate next macro-block and option B as the next real frontier");
        StringAssert.Contains(adr, "`NODAL_OS_WORKSPACE_TEST_JAIL_HANDOFF_DRAFT_BOUNDARY_DESIGN_ONLY`");
        StringAssert.Contains(adr, FutureWorkspaceAction);
        StringAssert.Contains(adr, $"POST {FutureWorkspaceRoute}");
        StringAssert.Contains(adr, "No public/product exposure");
        StringAssert.Contains(adr, "No user-workspace action");
        StringAssert.Contains(report, "\"recommendedImmediateNextMacroBlock\": \"NODAL_OS_WORKSPACE_TEST_JAIL_HANDOFF_DRAFT_BOUNDARY_DESIGN_ONLY\"");
        StringAssert.Contains(report, $"\"actionName\": \"{FutureWorkspaceAction}\"");
        StringAssert.Contains(report, "\"publicProductImplemented\": false");
        StringAssert.Contains(report, "\"userWorkspaceActionImplemented\": false");
        StringAssert.Contains(handoff, "A later separate implementation GO would still be required before any user-workspace write exists.");
    }

    [TestMethod]
    public void PublicProductOrWorkspaceAuthorizationReadiness_WorkspaceTestJailActionIsActiveButNotPublicProductOrUserWorkspace()
    {
        var mapper = ReadRepoFile("src", "OneBrain.Pilot", "ProductLedgerLocalDevRouteEndpointMapper.cs");
        var approvalSources = string.Join(
            Environment.NewLine,
            Directory.EnumerateFiles(Path.Combine(RepoRoot(), "src", "OneBrain.Core", "Approval"), "*.cs")
                .Select(File.ReadAllText));
        var implementationSource = string.Join(
            Environment.NewLine,
            mapper,
            ReadRepoFile("src", "OneBrain.Core", "Approval", "ProductLedgerLocalWorkspaceTestJailHandoffDraftExecutor.cs"));

        StringAssert.Contains(mapper, FutureWorkspaceRoute);
        StringAssert.Contains(mapper, "environment.IsDevelopment()");
        StringAssert.Contains(mapper, "LocalWorkspaceTestJailHandoffDraftStateRoute");
        StringAssert.Contains(approvalSources, FutureWorkspaceAction);
        StringAssert.Contains(approvalSources, "ProductLedgerLocalWorkspaceTestJailHandoffDraftExecutor");
        StringAssert.Contains(approvalSources, "WORKSPACE_TEST_JAIL_ONLY");
        StringAssert.Contains(approvalSources, "FileMode.CreateNew");
        StringAssert.Contains(approvalSources, "FileAttributes.ReparsePoint");

        StringAssert.Contains(mapper, "/internal/product-ledger/approval/create-local-handoff-draft");
        StringAssert.Contains(approvalSources, "ProductLedgerLocalApprovedHandoffReportDraftExecutor");

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
            "FileMode.OpenOrCreate",
            "OverwriteAllowed: true",
            "UserSelectedPathAllowed: true",
            "ProductionAllowed: true",
            "PublicProductAllowed: true",
            "ReleaseCommercialReady: true"
        })
        {
            Assert.IsFalse(implementationSource.Contains(forbiddenSource, StringComparison.Ordinal), forbiddenSource);
        }
    }

    [TestMethod]
    public void PublicProductOrWorkspaceAuthorizationReadiness_StaticScanKeepsBlockedFrontiersClosed()
    {
        var source = string.Join(
            Environment.NewLine,
            ReadRepoFile(
                "docs",
                "adr",
                "product-ledger-public-product-or-user-workspace-action-authorization-readiness-design-only.md"),
            ReadRepoFile(
                "docs",
                "handoff",
                "nodal-os-public-product-or-user-workspace-action-authorization-readiness-design-only-handoff.md"));

        foreach (var requiredNegative in new[]
        {
            "No public/product",
            "No Production route",
            "No user-workspace action",
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
            StringAssert.Contains(source, requiredNegative);
        }

        foreach (var forbiddenClaim in new[]
        {
            "public/product exposure implemented",
            "user-workspace action implemented",
            "Production route enabled",
            "workspace writer implemented",
            "shell enabled",
            "subprocess enabled",
            "command execution enabled",
            "provider live",
            "cloud backed",
            "DB migration enabled",
            "KMS enabled",
            "WORM enabled",
            "release ready",
            "commercial ready"
        })
        {
            Assert.IsFalse(source.Contains(forbiddenClaim, StringComparison.OrdinalIgnoreCase), forbiddenClaim);
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
