using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace OneBrain.Safety.Tests;

[TestClass]
[TestCategory("ApprovalHumanReview")]
[TestCategory("PhaseEApprovalHumanReview")]
[TestCategory("NodalOsTier1Safety")]
[TestCategory("StaticGuard")]
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
    [TestCategory("PublicProductBlock")]
    [TestCategory("ProductionRouteBlock")]
    [TestCategory("LatestPointerBlock")]
    [TestCategory("ReadPrecedenceBlock")]
    [TestCategory("ProductAuthorityBlock")]
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
    [TestCategory("RunClaimCoherence")]
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
    [TestCategory("LatestPointerBlock")]
    [TestCategory("ReadPrecedenceBlock")]
    [TestCategory("ProductAuthorityBlock")]
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
    [TestCategory("LatestPointerBlock")]
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
    [TestCategory("ProductAuthorityBlock")]
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
    [TestCategory("PublicProductBlock")]
    [TestCategory("ProductionRouteBlock")]
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

    [TestMethod]
    public void StaticGuardCatalog_C4MetadataLabelsAreAdditiveAndDiscoverable()
    {
        var classCategories = CategoriesFor(typeof(NodalOsStaticGuardCatalogTests));
        CollectionAssert.Contains(classCategories, "NodalOsTier1Safety");
        CollectionAssert.Contains(classCategories, "StaticGuard");

        var publicProductMethodCategories = CategoriesFor(
            typeof(NodalOsStaticGuardCatalogTests),
            nameof(StaticGuardCatalog_PublicProductAndProductionRouteAssertionsRemainHardFailing));
        CollectionAssert.Contains(publicProductMethodCategories, "PublicProductBlock");
        CollectionAssert.Contains(publicProductMethodCategories, "ProductionRouteBlock");

        var runClaimMethodCategories = CategoriesFor(
            typeof(NodalOsStaticGuardCatalogTests),
            nameof(StaticGuardCatalog_AllowsExpectedNegativeNoGoWording));
        CollectionAssert.Contains(runClaimMethodCategories, "RunClaimCoherence");

        var productLedgerMethodCategories = CategoriesFor(
            typeof(ProductLedgerBroaderWorkspaceOrPublicProductBoundaryTests),
            nameof(ProductLedgerBroaderWorkspaceOrPublicProductBoundaryTests.BroaderWorkspaceOrPublicProductBoundary_PublicProductMutationAndUnsafeFrontiersRemainClosed));
        CollectionAssert.Contains(productLedgerMethodCategories, "NodalOsTier1Safety");
        CollectionAssert.Contains(productLedgerMethodCategories, "ProductLedger");
        CollectionAssert.Contains(productLedgerMethodCategories, "PublicProductBlock");
        CollectionAssert.Contains(productLedgerMethodCategories, "ProductionRouteBlock");
    }

    [TestMethod]
    public void StaticGuardCatalog_C6ExpandedTier1LabelsAreDiscoverable()
    {
        var requiredMetadata = new (Type Type, string MethodName, string[] Categories)[]
        {
            (typeof(ProductLedgerActiveDurableReadPrecedenceDecisionMatrixDesignOnlyTests),
                nameof(ProductLedgerActiveDurableReadPrecedenceDecisionMatrixDesignOnlyTests.ActiveDurableReadPrecedenceDecisionMatrix_SourceStillHasNoPrecedencePointerAuthorityOrPublicProduct),
                ["NodalOsTier1Safety", "ProductLedger", "LatestPointerBlock", "ReadPrecedenceBlock", "ProductAuthorityBlock", "PublicProductBlock", "ProductionRouteBlock"]),
            (typeof(ProductLedgerLocalDurableLatestStateAuxiliaryEvidencePresenterTests),
                nameof(ProductLedgerLocalDurableLatestStateAuxiliaryEvidencePresenterTests.AuxiliaryEvidence_SourceHasNoForbiddenActivation),
                ["NodalOsTier1Safety", "ProductLedger", "StaticGuard", "LatestPointerBlock", "ReadPrecedenceBlock", "ProductAuthorityBlock", "ReleaseCommercialBlock"]),
            (typeof(ProductLedgerPublicProductOrWorkspaceActionAuthorizationReadinessTests),
                nameof(ProductLedgerPublicProductOrWorkspaceActionAuthorizationReadinessTests.PublicProductOrWorkspaceAuthorizationReadiness_StaticScanKeepsBlockedFrontiersClosed),
                ["NodalOsTier1Safety", "ProductLedger", "StaticGuard", "PublicProductBlock", "ProductionRouteBlock", "CommandExecutionBlock", "ReleaseCommercialBlock"]),
            (typeof(ProductLedgerUserWorkspaceOrPublicProductAuthorizationBoundaryTests),
                nameof(ProductLedgerUserWorkspaceOrPublicProductAuthorizationBoundaryTests.UserWorkspaceOrPublicProductBoundary_StaticScanKeepsPublicProductAndUnsafeFrontiersClosed),
                ["NodalOsTier1Safety", "ProductLedger", "StaticGuard", "PublicProductBlock", "ProductionRouteBlock", "CommandExecutionBlock", "ReleaseCommercialBlock"]),
            (typeof(ProductLedgerFirstRealUserFacingLocalActionReadinessTests),
                nameof(ProductLedgerFirstRealUserFacingLocalActionReadinessTests.FirstRealUserFacingLocalActionReadiness_StaticScanKeepsForbiddenRuntimeFrontiersClosed),
                ["NodalOsTier1Safety", "ProductLedger", "StaticGuard", "PublicProductBlock", "ProductionRouteBlock", "CommandExecutionBlock", "ReleaseCommercialBlock"]),
            (typeof(ProductLedgerPublicUiActionSurfaceTests),
                nameof(ProductLedgerPublicUiActionSurfaceTests.PublicUiActionSurface_BlocksDangerousActionCommands),
                ["NodalOsTier1Safety", "ProductLedger", "PublicProductBlock", "CommandExecutionBlock", "ReleaseCommercialBlock"]),
            (typeof(ProductLedgerPublicUiActionSurfaceTests),
                nameof(ProductLedgerPublicUiActionSurfaceTests.PublicUiActionSurface_SourceHasNoNetworkDbKmsLiveReleaseRawOrOverclaim),
                ["NodalOsTier1Safety", "ProductLedger", "StaticGuard", "PublicProductBlock", "CommandExecutionBlock", "ReleaseCommercialBlock"])
        };

        foreach (var (type, methodName, categories) in requiredMetadata)
        {
            var methodCategories = CategoriesFor(type, methodName);

            foreach (var category in categories)
            {
                CollectionAssert.Contains(methodCategories, category, $"{type.Name}.{methodName}: {category}");
            }
        }
    }

    [TestMethod]
    public void StaticGuardCatalog_MetadataConsistencyKeepsTier1PartialAndSemanticLabelsSeparate()
    {
        var catalogClassCategories = CategoriesFor(typeof(NodalOsStaticGuardCatalogTests));
        CollectionAssert.Contains(catalogClassCategories, "NodalOsTier1Safety");
        CollectionAssert.Contains(catalogClassCategories, "StaticGuard");
        Assert.IsFalse(catalogClassCategories.Contains("ProductLedger"));
        AssertDoesNotContainAuthorityOrCiLabels(catalogClassCategories, nameof(NodalOsStaticGuardCatalogTests));

        var productLedgerClassCategories = CategoriesFor(typeof(ProductLedgerLocalDevCanonGuardTests));
        CollectionAssert.Contains(productLedgerClassCategories, "NodalOsTier1Safety");
        CollectionAssert.Contains(productLedgerClassCategories, "ProductLedger");
        CollectionAssert.Contains(productLedgerClassCategories, "NoAuthority");
        CollectionAssert.Contains(productLedgerClassCategories, "NoRuntimeWiring");
        CollectionAssert.Contains(productLedgerClassCategories, "NoDoubleTruth");
        CollectionAssert.Contains(productLedgerClassCategories, "PublicProductBlock");
        CollectionAssert.Contains(productLedgerClassCategories, "ProductionRouteBlock");
        CollectionAssert.Contains(productLedgerClassCategories, "ReleaseCommercialBlock");
        Assert.IsFalse(productLedgerClassCategories.Contains("StaticGuard"));
        AssertDoesNotContainAuthorityOrCiLabels(productLedgerClassCategories, nameof(ProductLedgerLocalDevCanonGuardTests));

        var staticGuardMethodCategories = CategoriesFor(
            typeof(NodalOsStaticGuardCatalogTests),
            nameof(StaticGuardCatalog_PublicProductAndProductionRouteAssertionsRemainHardFailing));
        CollectionAssert.Contains(staticGuardMethodCategories, "PublicProductBlock");
        CollectionAssert.Contains(staticGuardMethodCategories, "ProductionRouteBlock");
        AssertDoesNotContainAuthorityOrCiLabels(staticGuardMethodCategories, nameof(StaticGuardCatalog_PublicProductAndProductionRouteAssertionsRemainHardFailing));
    }

    private static string[] CategoriesFor(Type type) =>
        type.GetCustomAttributes(typeof(TestCategoryAttribute), inherit: false)
            .Cast<TestCategoryAttribute>()
            .SelectMany(attribute => attribute.TestCategories)
            .ToArray();

    private static string[] CategoriesFor(Type type, string methodName)
    {
        var method = type.GetMethod(methodName)
            ?? throw new InvalidOperationException($"Method not found: {type.FullName}.{methodName}");

        return method.GetCustomAttributes(typeof(TestCategoryAttribute), inherit: false)
            .Cast<TestCategoryAttribute>()
            .SelectMany(attribute => attribute.TestCategories)
            .ToArray();
    }

    private static void AssertDoesNotContainAuthorityOrCiLabels(string[] categories, string context)
    {
        foreach (var forbidden in new[] { "CiEnforced", "RuntimeProductAuthority", "ProductAuthorityGranted", "ReleaseCommercialReady" })
        {
            Assert.IsFalse(categories.Contains(forbidden), $"{context}: {forbidden}");
        }
    }
}
