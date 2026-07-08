using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace OneBrain.Safety.Tests;

[TestClass]
[TestCategory("ApprovalHumanReview")]
[TestCategory("PhaseEApprovalHumanReview")]
[TestCategory("NodalOsTier1Safety")]
[TestCategory("ProductLedger")]
[TestCategory("DesignOnly")]
[TestCategory("NoRuntimeWiring")]
[TestCategory("NoAuthority")]
[TestCategory("NoDoubleTruth")]
[TestCategory("PublicProductBlock")]
[TestCategory("ProductionRouteBlock")]
[TestCategory("ReleaseCommercialBlock")]
public sealed class ProductLedgerLocalDevCanonGuardTests
{
    private const string CanonPath = "docs/architecture/nodal-os-product-ledger-local-dev-safety-backlog-canon.md";
    private const string NextActionPlanPath = "docs/architecture/nodal-os-product-ledger-local-dev-next-action-plan.md";
    private const string StaleEntrypointIndexPath = "docs/architecture/nodal-os-product-ledger-local-dev-stale-entrypoint-crosslink-index.md";
    private const string ManualGateDecisionTablePath = "docs/audit/product-ledger-local-dev/manual-gate-decision-table.md";

    [TestMethod]
    public void ProductLedgerLocalDevCanonExistsAndIsCurrentAuthority()
    {
        var canon = ReadRepoFile(CanonPath);

        StringAssert.Contains(canon, "# NODAL OS Product Ledger Local/Dev Safety Backlog Canon");
        StringAssert.Contains(canon, "Product Ledger has a real local/dev evidence line");
        StringAssert.Contains(canon, "current Product Ledger local/dev canon");
        StringAssert.Contains(canon, "Current/canonical: this canon");
        StringAssert.Contains(canon, "older QA and handoff artifacts remain traceability");
        StringAssert.Contains(canon, $"E3 next-action plan: `{NextActionPlanPath}`");
    }

    [TestMethod]
    public void ProductLedgerLocalDevCanonPreservesBlockedRuntimeProductPosture()
    {
        var canon = ReadRepoFile(CanonPath);

        StringAssert.Contains(canon, "not public/product");
        StringAssert.Contains(canon, "Product Ledger runtime/product enablement");
        StringAssert.Contains(canon, "Runtime/product enablement remains `0%`");
        StringAssert.Contains(canon, "Productive DI/service registration");
        StringAssert.Contains(canon, "Productive command handlers");
        StringAssert.Contains(canon, "Passing them does not mean public/product readiness");
        AssertDoesNotContainPositiveProductReadinessClaim(canon);
    }

    [TestMethod]
    public void ProductLedgerLocalDevCanonPreservesBlockedRoutePointerPrecedenceAuthorityPosture()
    {
        var canon = ReadRepoFile(CanonPath);

        StringAssert.Contains(canon, "not a Production route");
        StringAssert.Contains(canon, "create a latest pointer");
        StringAssert.Contains(canon, "activate read precedence");
        StringAssert.Contains(canon, "create product authority");
        StringAssert.Contains(canon, "Public/product route or public product UI action");
        StringAssert.Contains(canon, "Production route");
        StringAssert.Contains(canon, "Latest pointer creation or overwrite");
        StringAssert.Contains(canon, "Active read precedence");
        StringAssert.Contains(canon, "Product authority or product read-model authority");
    }

    [TestMethod]
    public void ProductLedgerLocalDevCanonPreservesNoReleaseCommercialAndNoExternalTrustPosture()
    {
        var canon = ReadRepoFile(CanonPath);

        StringAssert.Contains(canon, "DB/migration");
        StringAssert.Contains(canon, "Provider/cloud/network");
        StringAssert.Contains(canon, "KMS/WORM/external trust");
        StringAssert.Contains(canon, "Browser/CDP/WCU/OCR/Recipes live automation");
        StringAssert.Contains(canon, "Release/commercial readiness remains `0% / NO-GO`");
        StringAssert.Contains(canon, "CI enforcement remains `0%`");
        AssertDoesNotContainPositiveProductReadinessClaim(canon);
    }

    [TestMethod]
    public void ProductLedgerLocalDevCanonKeepsSafetyRecipesManualGateAuthority()
    {
        var canon = ReadRepoFile(CanonPath);

        StringAssert.Contains(canon, "Safety/Recipes focused test evidence");
        StringAssert.Contains(canon, "Safety/Recipes remain the authoritative evidence surface for Product Ledger local/dev");
        StringAssert.Contains(canon, "These tests are evidence, not CI enforcement");
        StringAssert.Contains(canon, "These commands are manual/discovery-only");
        StringAssert.Contains(canon, "Tier 1 remains manual/discovery-only");
        StringAssert.Contains(canon, "Gate evidence remains manual/discovery-only and not CI-enforced");
    }

    [TestMethod]
    public void ProductLedgerLocalDevCanonLinksNextActionPlanAndHistoricalEntryPoints()
    {
        var canon = ReadRepoFile(CanonPath);
        var plan = ReadRepoFile(NextActionPlanPath);
        var crossLinkIndex = ReadRepoFile(StaleEntrypointIndexPath);

        StringAssert.Contains(canon, NextActionPlanPath);
        StringAssert.Contains(canon, StaleEntrypointIndexPath);
        StringAssert.Contains(plan, CanonPath);
        StringAssert.Contains(plan, StaleEntrypointIndexPath);
        StringAssert.Contains(crossLinkIndex, CanonPath);
        StringAssert.Contains(crossLinkIndex, NextActionPlanPath);
        StringAssert.Contains(crossLinkIndex, "Current interpretation: this document is historical/block-specific evidence");
        StringAssert.Contains(crossLinkIndex, "E4 recommends but does not authorize E5");
    }

    [TestMethod]
    public void ProductLedgerLocalDevManualGateDecisionTablePreservesNoProductAuthority()
    {
        var table = ReadRepoFile(ManualGateDecisionTablePath);

        StringAssert.Contains(table, "PRODUCT_LEDGER_LOCAL_DEV_MANUAL_GATE_DECISION_TABLE_READY_NO_PRODUCT_AUTHORITY");
        StringAssert.Contains(table, "Every current E-series gate has Product/runtime authority = `NO`");
        StringAssert.Contains(table, "`APPROVE_PACKET_FOR_EXTERNAL_REVIEW`");
        StringAssert.Contains(table, "`CLOSE_EXTERNAL_REVIEW_WAIT_WITHOUT_EXTERNAL_RESPONSE_AND_CONTINUE_INTERNAL_ONLY`");
        StringAssert.Contains(table, "`PRODUCT_LEDGER_LOCAL_DEV_MANUAL_GATE_DECISION_TABLE_DOCS_TEST_ONLY`");
        StringAssert.Contains(table, "`FUTURE_RUNTIME_PRODUCT_AUTHORIZATION_GATE`");
        StringAssert.Contains(table, "`FUTURE_CI_ENFORCEMENT_GATE`");
        StringAssert.Contains(table, "`FUTURE_RELEASE_COMMERCIAL_GATE`");
        StringAssert.Contains(table, "`NOT_AUTHORIZED_NOW`");
        StringAssert.Contains(table, "`REQUIRES_SEPARATE_EXPLICIT_OPERATOR_AUTHORIZATION`");
        StringAssert.Contains(table, "No gate may claim external review response, external reviewer approval or external audit pass unless actual response content is provided and recorded.");
        StringAssert.Contains(table, "Manual/operator-run gates are not CI enforcement.");
        StringAssert.Contains(table, "E14 clarifies manual gates only. It does not authorize runtime/product, CI enforcement, release/commercial or external audit approval.");
        AssertDoesNotContainPositiveProductReadinessClaim(table);
    }

    private static void AssertDoesNotContainPositiveProductReadinessClaim(string canon)
    {
        foreach (var forbidden in new[]
        {
            "Product Ledger is product-ready",
            "Product Ledger is public/product ready",
            "Product Ledger is public/product-ready",
            "public/product route enabled",
            "Production route enabled",
            "latest pointer enabled",
            "active read precedence enabled",
            "product authority enabled",
            "runtime/product enablement: `1",
            "CI enforcement: `1",
            "Release/commercial readiness: `1",
            "Tier1 is CI-enforced",
            "Tier 1 is CI-enforced",
            "E5 authorizes E6"
        })
        {
            Assert.IsFalse(canon.Contains(forbidden, StringComparison.OrdinalIgnoreCase), forbidden);
        }
    }

    private static string ReadRepoFile(string relativePath) =>
        File.ReadAllText(Path.Combine(RepoRoot(), relativePath.Replace('/', Path.DirectorySeparatorChar)));

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
