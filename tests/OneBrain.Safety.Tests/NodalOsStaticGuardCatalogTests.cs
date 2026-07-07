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
    public void StaticGuardCatalog_C2MirrorsRetainedOldSourceAssertions()
    {
        var samples = new[]
        {
            (NodalOsStaticGuardCategory.LatestPointer, "LatestPointer: true"),
            (NodalOsStaticGuardCategory.LatestPointer, "LatestPointerOverwrite: true"),
            (NodalOsStaticGuardCategory.ReadPrecedence, "ReadPrecedence: true"),
            (NodalOsStaticGuardCategory.ReadPrecedence, "AllowsReadPrecedence: true"),
            (NodalOsStaticGuardCategory.ProductAuthority, "ProductAuthority: true"),
            (NodalOsStaticGuardCategory.ProductAuthority, "AuthorityLiveProduct: true"),
            (NodalOsStaticGuardCategory.ShellSubprocess, "Process.Start"),
            (NodalOsStaticGuardCategory.ReleaseCommercial, "ReleaseCommercialReady: true")
        };

        foreach (var (category, source) in samples)
        {
            var matches = NodalOsStaticGuardCatalog.ScanSource(source, category);

            Assert.IsTrue(matches.Count >= 1, $"{category}: {source}");
            Assert.IsTrue(matches.All(match => match.Category == category), $"{category}: {source}");
        }
    }

    [TestMethod]
    public void StaticGuardCatalog_C2KeepsAllowedNegativeWordingSeparateFromPositiveMatches()
    {
        var allowedDocs = string.Join(
            Environment.NewLine,
            "No latest pointer.",
            "No active read precedence.",
            "No product authority.",
            "No shell/subprocess.",
            "No release/commercial.");

        var allowedMatches = NodalOsStaticGuardCatalog.ScanDocs(
            allowedDocs,
            NodalOsStaticGuardCategory.LatestPointer,
            NodalOsStaticGuardCategory.ReadPrecedence,
            NodalOsStaticGuardCategory.ProductAuthority,
            NodalOsStaticGuardCategory.ShellSubprocess,
            NodalOsStaticGuardCategory.ReleaseCommercial);

        Assert.AreEqual(0, allowedMatches.Count, string.Join(", ", allowedMatches.Select(match => match.Fragment)));

        var blockedDocs = string.Join(
            Environment.NewLine,
            "No latest pointer.",
            "latest pointer enabled");

        var blockedMatches = NodalOsStaticGuardCatalog.ScanDocs(
            blockedDocs,
            NodalOsStaticGuardCategory.LatestPointer);

        Assert.AreEqual(1, blockedMatches.Count);
        Assert.AreEqual("latest pointer enabled", blockedMatches[0].Fragment);
    }

    [TestMethod]
    public void StaticGuardCatalog_C2SourceAndDocsScopesUseExplicitEntrypoints()
    {
        const string source = "ProductAuthority: true";

        var sourceMatches = NodalOsStaticGuardCatalog.ScanSource(
            source,
            NodalOsStaticGuardCategory.ProductAuthority);
        var docsMatches = NodalOsStaticGuardCatalog.ScanDocs(
            source,
            NodalOsStaticGuardCategory.ProductAuthority);

        Assert.AreEqual(1, sourceMatches.Count);
        Assert.AreEqual(1, docsMatches.Count);
        Assert.AreEqual(sourceMatches[0], docsMatches[0]);
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
