using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace OneBrain.Safety.Tests;

[TestClass]
[TestCategory("ApprovalHumanReview")]
[TestCategory("PhaseEApprovalHumanReview")]
public sealed class ProductLedgerFirstRealUserFacingLocalActionReadinessTests
{
    [TestMethod]
    public void FirstRealUserFacingLocalActionReadiness_DocsSelectSingleCandidateAndRemainDesignOnly()
    {
        var adr = ReadRepoFile("docs", "adr", "product-ledger-first-real-user-facing-local-action-path-readiness-design-only.md");
        var report = ReadRepoFile("docs", "qa", "product-ledger-first-real-user-facing-local-action-path-readiness-design-only", "report.json");
        var handoff = ReadRepoFile("docs", "handoff", "nodal-os-first-real-user-facing-local-action-path-readiness-design-only-handoff.md");

        StringAssert.Contains(adr, "Decision: `GO_WITH_FINDINGS_FIRST_REAL_USER_FACING_LOCAL_ACTION_READINESS_DESIGN_ONLY_READY`");
        StringAssert.Contains(adr, "Recommended first real action candidate: `LocalApprovedHandoffReportDraft`.");
        StringAssert.Contains(adr, "Future route: `POST /internal/product-ledger/approval/create-local-handoff-draft`.");
        StringAssert.Contains(adr, "Allowed root: `docs/test-output/product-ledger/approved-local-handoff-drafts/`.");
        StringAssert.Contains(adr, "No real user-facing action is implemented in this block.");
        StringAssert.Contains(adr, "No user file write");
        StringAssert.Contains(adr, "No shell/subprocess");
        StringAssert.Contains(adr, "No public/product path");
        StringAssert.Contains(adr, "No release/commercial");
        StringAssert.Contains(report, "\"actionKind\": \"LocalApprovedHandoffReportDraft\"");
        StringAssert.Contains(report, "\"realUserFacingActionImplemented\": false");
        StringAssert.Contains(report, "\"userFileWriteImplemented\": false");
        StringAssert.Contains(handoff, "Required Next GO");
    }

    [TestMethod]
    public void FirstRealUserFacingLocalActionReadiness_ImplementedCandidateRemainsLocalDevelopmentOnly()
    {
        var implementationAdr = ReadRepoFile("docs", "adr", "product-ledger-local-approved-handoff-report-draft-implementation.md");
        var mapper = ReadRepoFile("src", "OneBrain.Pilot", "ProductLedgerLocalDevRouteEndpointMapper.cs");
        var handoffDraftExecutor = ReadRepoFile("src", "OneBrain.Core", "Approval", "ProductLedgerLocalApprovedHandoffReportDraftExecutor.cs");
        var approvalRoot = Path.Combine(RepoRoot(), "src", "OneBrain.Core", "Approval");
        var approvalSources = string.Join(Environment.NewLine, Directory.EnumerateFiles(approvalRoot, "*.cs").Select(File.ReadAllText));

        StringAssert.Contains(implementationAdr, "Decision: `GO_WITH_FINDINGS_LOCAL_APPROVED_HANDOFF_REPORT_DRAFT_IMPLEMENTATION_READY`");
        StringAssert.Contains(implementationAdr, "`docs/test-output/product-ledger/approved-local-handoff-drafts/`");
        StringAssert.Contains(mapper, "environment.IsDevelopment()");
        StringAssert.Contains(mapper, "LocalApprovedHandoffReportDraftRoute");
        StringAssert.Contains(mapper, "LocalApprovedHandoffReportDraftStateRoute");
        StringAssert.Contains(mapper, "/internal/product-ledger/approval/create-local-handoff-draft");
        StringAssert.Contains(mapper, "/internal/product-ledger/approval/local-handoff-draft-state");
        StringAssert.Contains(mapper, "ProductLedgerLocalApprovedHandoffReportDraftExecutor");
        StringAssert.Contains(approvalSources, "ProductLedgerLocalApprovedHandoffReportDraftExecutor");
        StringAssert.Contains(handoffDraftExecutor, "FileMode.CreateNew");

        foreach (var forbidden in new[]
        {
            "Process.Start",
            "HttpClient",
            "WebSocket",
            "DbContext",
            "MigrationBuilder",
            "KmsClient",
            "WormStore",
            "PilotRecipeExecutor",
            "PilotRecipeExecutionGate.Evaluate(",
            "NODAL_OS_ENABLE_PILOT_RECIPE_EXECUTION",
            "ReleaseCommercialReady: true",
            "PublicProductAllowed: true",
            "ProductionAllowed: true",
            "UserFileWrite: true",
            "OverwriteAllowed: true"
        })
        {
            Assert.IsFalse(mapper.Contains(forbidden, StringComparison.Ordinal), forbidden);
            Assert.IsFalse(handoffDraftExecutor.Contains(forbidden, StringComparison.Ordinal), forbidden);
        }
    }

    [TestMethod]
    public void FirstRealUserFacingLocalActionReadiness_StaticScanKeepsForbiddenRuntimeFrontiersClosed()
    {
        var source = string.Join(
            Environment.NewLine,
            ReadRepoFile("docs", "adr", "product-ledger-first-real-user-facing-local-action-path-readiness-design-only.md"),
            ReadRepoFile("docs", "handoff", "nodal-os-first-real-user-facing-local-action-path-readiness-design-only-handoff.md"));

        foreach (var requiredNegative in new[]
        {
            "No real user-facing action",
            "No user file write",
            "No shell/subprocess",
            "No public/product path",
            "No Production execution",
            "No Pilot `/run`",
            "No Browser/CDP/WCU/OCR/Recipes live",
            "No release/commercial"
        })
        {
            StringAssert.Contains(source, requiredNegative);
        }

        foreach (var forbiddenClaim in new[]
        {
            "release ready",
            "commercial ready",
            "public path enabled",
            "product path enabled",
            "Production route enabled",
            "shell enabled",
            "subprocess enabled",
            "Pilot run enabled",
            "cloud backed",
            "provider live",
            "DB migration enabled",
            "KMS enabled",
            "WORM enabled",
            "compliance custody ready"
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
