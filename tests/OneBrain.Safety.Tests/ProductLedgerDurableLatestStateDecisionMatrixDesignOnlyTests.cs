using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace OneBrain.Safety.Tests;

[TestClass]
[TestCategory("ApprovalHumanReview")]
[TestCategory("PhaseEApprovalHumanReview")]
public sealed class ProductLedgerDurableLatestStateDecisionMatrixDesignOnlyTests
{
    private const string Decision = "GO_WITH_FINDINGS_DURABLE_LATEST_STATE_AUTHORITY_READ_PRECEDENCE_PUBLIC_PRODUCT_DECISION_MATRIX_DESIGN_ONLY_READY";
    private const string RecommendedCapability = "LocalDurableLatestStateAuxiliaryEvidenceNotPrecedenceNotAuthority";
    private const string RecommendedClassification = "LOCAL_INTERNAL_DEV_ONLY_AUXILIARY_EVIDENCE_NOT_PRECEDENCE_NOT_AUTHORITY";
    private const string NextGo = "AUTHORIZE_NODAL_OS_DURABLE_LATEST_STATE_AUXILIARY_EVIDENCE_NOT_PRECEDENCE_NOT_AUTHORITY_IMPLEMENTATION_WINDOW";

    [TestMethod]
    public void DurableLatestStateDecisionMatrix_DocsDefineOptionARecommendationAndExactNextGo()
    {
        var adr = ReadRepoFile("docs", "adr", "product-ledger-durable-latest-state-authority-read-precedence-public-product-decision-matrix-design-only.md");
        var report = ReadRepoFile("docs", "qa", "product-ledger-durable-latest-state-authority-read-precedence-public-product-decision-matrix-design-only", "report.json");
        var handoff = ReadRepoFile("docs", "handoff", "nodal-os-durable-latest-state-authority-read-precedence-public-product-decision-matrix-design-only-handoff.md");
        var roadmap = ReadRepoFile("docs", "roadmap", "product-ledger-durable-latest-state-authority-read-precedence-public-product-decision-matrix-design-only.md");

        StringAssert.Contains(adr, $"Decision: `{Decision}`");
        StringAssert.Contains(adr, "| A. Auxiliary evidence integration |");
        StringAssert.Contains(adr, "| B. Development-only read precedence candidate |");
        StringAssert.Contains(adr, "| C. Versioned/latest manifest-index pointer |");
        StringAssert.Contains(adr, "| D. Durable local authority Development-only |");
        StringAssert.Contains(adr, "| E. Public/product exposure |");
        StringAssert.Contains(adr, "| F. More local/internal hardening |");
        StringAssert.Contains(adr, "Recommend option A");
        StringAssert.Contains(adr, RecommendedCapability);
        StringAssert.Contains(adr, RecommendedClassification);
        StringAssert.Contains(adr, "no read precedence");
        StringAssert.Contains(adr, "no latest pointer");
        StringAssert.Contains(adr, "no public/product exposure");
        StringAssert.Contains(adr, NextGo);

        StringAssert.Contains(report, "\"scope\": \"design-only-readiness-only-audit-only-test-only-guard-only\"");
        StringAssert.Contains(report, "\"recommendedOption\": \"A\"");
        StringAssert.Contains(report, $"\"recommendedFutureCapability\": \"{RecommendedCapability}\"");
        StringAssert.Contains(report, $"\"recommendedClassification\": \"{RecommendedClassification}\"");
        StringAssert.Contains(report, "\"activeReadPrecedence\": false");
        StringAssert.Contains(report, "\"latestPointer\": false");
        StringAssert.Contains(report, "\"publicProductExposure\": false");
        StringAssert.Contains(report, $"\"exactNextGoRequired\": \"{NextGo}\"");

        StringAssert.Contains(handoff, RecommendedCapability);
        StringAssert.Contains(handoff, NextGo);
        StringAssert.Contains(roadmap, "No implementation capability percentages changed");
    }

    [TestMethod]
    public void DurableLatestStateDecisionMatrix_SourceStillHasNoAuthorityPrecedencePointerOrPublicProduct()
    {
        var source = SourceText();

        foreach (var forbidden in new[]
        {
            RecommendedCapability,
            "ProductLedgerLocalDurableLatestStateAuxiliaryEvidencePresenter",
            RecommendedClassification,
            "LOCAL_INTERNAL_DEV_ONLY_READ_PRECEDENCE_CANDIDATE_NOT_PRODUCT_AUTHORITY",
            "LOCAL_INTERNAL_DEV_ONLY_DURABLE_AUTHORITY",
            "ReadPrecedence: true",
            "ReadPrecedenceAllowed: true",
            "RequestsReadPrecedence: true",
            "LatestPointer: true",
            "LatestPointerAvailable: true",
            "LatestPointerOverwrite: true",
            "LatestPointerOverwriteAllowed: true",
            "AuthorityLiveProduct: true",
            "ProductAuthority: true",
            "ProductionAllowed: true",
            "PublicProductAllowed: true",
            "/public/product-ledger",
            "MapProductLedgerPublic",
            "FileMode.OpenOrCreate",
            "File.Delete(",
            "Process.Start",
            "ShellExecute",
            "System.Diagnostics.Process",
            "DbContext",
            "MigrationBuilder",
            "KmsClient",
            "WormStore"
        })
        {
            Assert.IsFalse(source.Contains(forbidden, StringComparison.Ordinal), forbidden);
        }

        StringAssert.Contains(source, "LOCAL_INTERNAL_DEV_ONLY_READER_CANDIDATE_NOT_AUTHORITY");
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
