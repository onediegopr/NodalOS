using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace OneBrain.Safety.Tests;

[TestClass]
[TestCategory("ApprovalHumanReview")]
[TestCategory("PhaseEApprovalHumanReview")]
public sealed class NodalOsStaticGuardCatalogTests
{
    [TestMethod]
    public void StaticGuardCatalog_ContainsExpectedC1Categories()
    {
        var categories = NodalOsStaticGuardCatalog.All
            .Select(definition => definition.Category)
            .ToHashSet();

        CollectionAssert.IsSubsetOf(
            new[]
            {
                NodalOsStaticGuardCategory.PublicProductExposure,
                NodalOsStaticGuardCategory.ProductionRoutes,
                NodalOsStaticGuardCategory.RuntimeExecutionClaims,
                NodalOsStaticGuardCategory.LatestPointer,
                NodalOsStaticGuardCategory.ReadPrecedence,
                NodalOsStaticGuardCategory.ProductAuthority,
                NodalOsStaticGuardCategory.CommandExecution,
                NodalOsStaticGuardCategory.ShellSubprocess,
                NodalOsStaticGuardCategory.CloudNetworkDb,
                NodalOsStaticGuardCategory.KmsWormCompliance,
                NodalOsStaticGuardCategory.ReleaseCommercial,
                NodalOsStaticGuardCategory.RunClaimCoherence
            },
            categories.ToArray());
    }

    [TestMethod]
    public void StaticGuardCatalog_DetectsForbiddenPositiveSamples()
    {
        var source = string.Join(
            Environment.NewLine,
            "/public/product-ledger",
            "ProductionAllowed: true",
            "latest pointer enabled",
            "active read precedence enabled",
            "product authority enabled",
            "command execution enabled",
            "Process.Start",
            "HttpClient",
            "KMS enabled",
            "release ready");

        var matches = NodalOsStaticGuardCatalog.Scan(source);

        CollectionAssert.Contains(matches.Select(match => match.Category).ToArray(), NodalOsStaticGuardCategory.PublicProductExposure);
        CollectionAssert.Contains(matches.Select(match => match.Category).ToArray(), NodalOsStaticGuardCategory.ProductionRoutes);
        CollectionAssert.Contains(matches.Select(match => match.Category).ToArray(), NodalOsStaticGuardCategory.LatestPointer);
        CollectionAssert.Contains(matches.Select(match => match.Category).ToArray(), NodalOsStaticGuardCategory.ReadPrecedence);
        CollectionAssert.Contains(matches.Select(match => match.Category).ToArray(), NodalOsStaticGuardCategory.ProductAuthority);
        CollectionAssert.Contains(matches.Select(match => match.Category).ToArray(), NodalOsStaticGuardCategory.CommandExecution);
        CollectionAssert.Contains(matches.Select(match => match.Category).ToArray(), NodalOsStaticGuardCategory.ShellSubprocess);
        CollectionAssert.Contains(matches.Select(match => match.Category).ToArray(), NodalOsStaticGuardCategory.CloudNetworkDb);
        CollectionAssert.Contains(matches.Select(match => match.Category).ToArray(), NodalOsStaticGuardCategory.KmsWormCompliance);
        CollectionAssert.Contains(matches.Select(match => match.Category).ToArray(), NodalOsStaticGuardCategory.ReleaseCommercial);
    }

    [TestMethod]
    public void StaticGuardCatalog_AllowsExpectedNegativeNoGoWording()
    {
        var source = string.Join(
            Environment.NewLine,
            "No public/product exposure.",
            "No Production route.",
            "No active read precedence.",
            "No latest pointer.",
            "No product authority.",
            "No command execution.",
            "No shell/subprocess.",
            "No provider/cloud/network.",
            "No DB/migration.",
            "No KMS/WORM/external trust.",
            "No release/commercial.",
            "Pilot `/run` is separate: `/run` is a gated allowlisted local execution path.");

        var matches = NodalOsStaticGuardCatalog.Scan(source);

        Assert.AreEqual(0, matches.Count, string.Join(", ", matches.Select(match => match.Fragment)));
    }

    [TestMethod]
    public void StaticGuardCatalog_PublicProductAndProductionRouteAssertionsRemainHardFailing()
    {
        var source = string.Join(
            Environment.NewLine,
            "public/product exposure implemented",
            "Production route enabled");

        var matches = NodalOsStaticGuardCatalog.Scan(
            source,
            NodalOsStaticGuardCategory.PublicProductExposure,
            NodalOsStaticGuardCategory.ProductionRoutes);

        Assert.AreEqual(2, matches.Count);
        CollectionAssert.Contains(matches.Select(match => match.Category).ToArray(), NodalOsStaticGuardCategory.PublicProductExposure);
        CollectionAssert.Contains(matches.Select(match => match.Category).ToArray(), NodalOsStaticGuardCategory.ProductionRoutes);
    }
}
