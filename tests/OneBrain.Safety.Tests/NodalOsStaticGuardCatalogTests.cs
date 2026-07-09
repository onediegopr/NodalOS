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
                NodalOsStaticGuardCategory.TrustedContextDurableEvidence,
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
            "release ready",
            "trusted context enabled");

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
        CollectionAssert.Contains(matches.Select(match => match.Category).ToArray(), NodalOsStaticGuardCategory.TrustedContextDurableEvidence);
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
            "Evidence link is not durable evidence.",
            "Context link is not trusted context.",
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
            (NodalOsStaticGuardCategory.ReleaseCommercial, "ReleaseCommercialReady: true"),
            (NodalOsStaticGuardCategory.TrustedContextDurableEvidence, "EvidenceLinkIsDurableEvidence: true"),
            (NodalOsStaticGuardCategory.TrustedContextDurableEvidence, "ContextLinkTrustedByDefault: true")
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
    [TestCategory("StaticGuard")]
    [TestCategory("NoAuthority")]
    public void StaticGuardCatalog_DetectsTrustedContextAndDurableEvidenceOverclaims()
    {
        var allowedNegativeSamples = string.Join(
            Environment.NewLine,
            "Evidence link is not durable evidence.",
            "Context link is not trusted context.",
            "durable evidence remains blocked.",
            "trusted context remains blocked.");
        var positiveSamples = string.Join(
            Environment.NewLine,
            "EvidenceLinkIsDurableEvidence: true",
            "ContextLinkTrustedByDefault: true",
            "review link promoted to durable evidence",
            "review link promoted to trusted context");

        Assert.AreEqual(
            0,
            NodalOsStaticGuardCatalog.Scan(
                allowedNegativeSamples,
                NodalOsStaticGuardCategory.TrustedContextDurableEvidence).Count);

        var matches = NodalOsStaticGuardCatalog.Scan(
            positiveSamples,
            NodalOsStaticGuardCategory.TrustedContextDurableEvidence);

        Assert.AreEqual(4, matches.Count, string.Join(", ", matches.Select(match => match.Fragment)));
        Assert.IsTrue(matches.All(match => match.Category == NodalOsStaticGuardCategory.TrustedContextDurableEvidence));
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

        var deferredForbiddenPhraseMethodCategories = CategoriesFor(
            typeof(NodalOsStaticGuardCatalogTests),
            nameof(StaticGuardCatalog_DeferredForbiddenPhraseFamiliesNarrowCorpusRespectsNegativeAllowlist));
        CollectionAssert.Contains(deferredForbiddenPhraseMethodCategories, "NoAuthority");
        CollectionAssert.Contains(deferredForbiddenPhraseMethodCategories, "NoRuntimeWiring");
        CollectionAssert.Contains(deferredForbiddenPhraseMethodCategories, "ReleaseCommercialBlock");
        CollectionAssert.Contains(deferredForbiddenPhraseMethodCategories, "StaticGuard");
        AssertDoesNotContainAuthorityOrCiLabels(
            deferredForbiddenPhraseMethodCategories,
            nameof(StaticGuardCatalog_DeferredForbiddenPhraseFamiliesNarrowCorpusRespectsNegativeAllowlist));
    }

    [TestMethod]
    [TestCategory("RunClaimCoherence")]
    [TestCategory("NoRuntimeWiring")]
    [TestCategory("PublicProductBlock")]
    [TestCategory("ProductionRouteBlock")]
    [TestCategory("LatestPointerBlock")]
    [TestCategory("ReadPrecedenceBlock")]
    [TestCategory("ReleaseCommercialBlock")]
    public void StaticGuardCatalog_ForbiddenPhraseExpansionNarrowCorpusRespectsNegativeAllowlist()
    {
        var allowedNegativeSamples = string.Join(
            Environment.NewLine,
            "no runtime/product enabled",
            "does not authorize runtime/product",
            "runtime/product remains 0%",
            "NOT_AUTHORIZED_NOW",
            "No public/product",
            "public/product remains blocked",
            "No Production route",
            "Production route remains blocked",
            "No latest pointer",
            "No active read precedence",
            "CI changed: none",
            "no CI enforcement",
            "Release/commercial: 0% / NO-GO",
            "not enabled and not release/commercial ready",
            "Non-goals: runtime enabled by default");

        AssertNoNarrowForbiddenPhraseMatches(
            allowedNegativeSamples,
            context: nameof(allowedNegativeSamples));

        var positiveSamples = string.Join(
            Environment.NewLine,
            "runtime/product ready",
            "product enabled",
            "public route live",
            "Production route enabled",
            "latest pointer promoted",
            "read precedence changed",
            "CI enforcement active",
            "release approved",
            "commercial ready");

        var positiveMatches = ScanNarrowForbiddenPhraseLines(
            positiveSamples,
            context: nameof(positiveSamples));

        CollectionAssert.Contains(positiveMatches.Select(match => match.Category).ToArray(), "RuntimeProduct");
        CollectionAssert.Contains(positiveMatches.Select(match => match.Category).ToArray(), "PublicProduct");
        CollectionAssert.Contains(positiveMatches.Select(match => match.Category).ToArray(), "ProductionRoute");
        CollectionAssert.Contains(positiveMatches.Select(match => match.Category).ToArray(), "LatestPointerReadPrecedence");
        CollectionAssert.Contains(positiveMatches.Select(match => match.Category).ToArray(), "CiEnforcement");
        CollectionAssert.Contains(positiveMatches.Select(match => match.Category).ToArray(), "ReleaseCommercial");

        var separatePositiveClaim = string.Join(
            Environment.NewLine,
            "runtime/product remains 0%.",
            "release approved");

        var separateMatches = ScanNarrowForbiddenPhraseLines(
            separatePositiveClaim,
            context: nameof(separatePositiveClaim));

        Assert.AreEqual(1, separateMatches.Count, string.Join(", ", separateMatches.Select(match => match.Fragment)));
        Assert.AreEqual("ReleaseCommercial", separateMatches[0].Category);

        foreach (var corpusFile in NarrowForbiddenPhraseCorpusFiles())
        {
            var matches = ScanNarrowForbiddenPhraseLines(
                System.IO.File.ReadAllText(corpusFile),
                context: corpusFile);

            Assert.AreEqual(
                0,
                matches.Count,
                string.Join(Environment.NewLine, matches.Select(match => match.ToString())));
        }
    }

    [TestMethod]
    [TestCategory("RunClaimCoherence")]
    [TestCategory("NoAuthority")]
    [TestCategory("NoRuntimeWiring")]
    [TestCategory("ReleaseCommercialBlock")]
    [TestCategory("StaticGuard")]
    public void StaticGuardCatalog_DeferredForbiddenPhraseFamiliesNarrowCorpusRespectsNegativeAllowlist()
    {
        var allowedNegativeSamples = string.Join(
            Environment.NewLine,
            "no external response recorded",
            "no external approval claimed",
            "no external audit pass",
            "external review wait closed without response",
            "internal/operator-attested only",
            "operator-run external review packet",
            "operator submission only",
            "external review is not submitted",
            "external audit approval remains blocked",
            "external approval is NOT_AUTHORIZED_NOW",
            "design-only external approval planning",
            "future external review requires explicit response content",
            "requires separate explicit operator authorization",
            "does not grant external approval",
            "no DB/cloud/network/provider",
            "DB/cloud/network/provider remains blocked",
            "no KMS/WORM guarantee",
            "KMS/WORM not claimed");

        AssertNoDeferredForbiddenPhraseMatches(
            allowedNegativeSamples,
            context: nameof(allowedNegativeSamples));

        var positiveSamples = string.Join(
            Environment.NewLine,
            "external audit passed",
            "external reviewer approved",
            "external approval granted",
            "reviewer approval recorded",
            "external audit completed as approval",
            "external review approved product readiness",
            "external review approved",
            "external approval claimed",
            "DB enabled",
            "database migration active",
            "cloud backed",
            "provider enabled",
            "network enabled",
            "KMS guaranteed",
            "WORM guaranteed",
            "durable WORM active",
            "external trust guaranteed",
            "DB/cloud/network enabled",
            "KMS/WORM guarantee",
            "cloud/provider capability enabled");

        var positiveMatches = ScanDeferredForbiddenPhraseLines(
            positiveSamples,
            context: nameof(positiveSamples));

        CollectionAssert.Contains(positiveMatches.Select(match => match.Category).ToArray(), "ExternalAuditApproval");
        CollectionAssert.Contains(positiveMatches.Select(match => match.Category).ToArray(), "DbCloudNetworkKmsWorm");

        var separatePositiveClaim = string.Join(
            Environment.NewLine,
            "no external approval claimed",
            "external audit passed");

        var separateMatches = ScanDeferredForbiddenPhraseLines(
            separatePositiveClaim,
            context: nameof(separatePositiveClaim));

        Assert.AreEqual(1, separateMatches.Count, string.Join(", ", separateMatches.Select(match => match.Fragment)));
        Assert.AreEqual("ExternalAuditApproval", separateMatches[0].Category);

        var corpusFiles = DeferredForbiddenPhraseCorpusFiles();
        foreach (var corpusFile in corpusFiles)
        {
            var matches = ScanDeferredForbiddenPhraseLines(
                System.IO.File.ReadAllText(corpusFile),
                context: corpusFile);

            Assert.AreEqual(
                0,
                matches.Count,
                string.Join(Environment.NewLine, matches.Select(match => match.ToString())));
        }

        foreach (var excludedCorpusFile in DeferredForbiddenPhraseExcludedCorpusFiles())
        {
            Assert.IsFalse(
                corpusFiles.Contains(excludedCorpusFile, StringComparer.OrdinalIgnoreCase),
                excludedCorpusFile);
        }
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

    private static string[] NarrowForbiddenPhraseCorpusFiles()
    {
        var root = RepositoryRoot();

        return
        [
            System.IO.Path.Combine(root, "docs/architecture/nodal-os-global-roadmap-current-index.md"),
            System.IO.Path.Combine(root, "docs/architecture/nodal-os-static-guard-catalog-coverage-map.md"),
            System.IO.Path.Combine(root, "docs/architecture/nodal-os-static-guard-catalog-metadata-consistency-check.md"),
            System.IO.Path.Combine(root, "docs/architecture/nodal-os-simplification-backlog.md"),
            System.IO.Path.Combine(root, "docs/decision-log.md")
        ];
    }

    private static string[] DeferredForbiddenPhraseCorpusFiles()
    {
        var root = RepositoryRoot();

        return
        [
            System.IO.Path.Combine(root, "docs/architecture/nodal-os-global-roadmap-current-index.md"),
            System.IO.Path.Combine(root, "docs/architecture/nodal-os-forbidden-phrase-expansion-corpus-selection.md"),
            System.IO.Path.Combine(root, "docs/architecture/nodal-os-static-guard-catalog-coverage-map.md"),
            System.IO.Path.Combine(root, "docs/architecture/nodal-os-simplification-backlog.md"),
            System.IO.Path.Combine(root, "docs/decision-log.md"),
            System.IO.Path.Combine(root, "docs/audit/product-ledger-local-dev/current-authority-map.md"),
            System.IO.Path.Combine(root, "docs/audit/product-ledger-local-dev/external-review-response-intake.md"),
            System.IO.Path.Combine(root, "docs/audit/product-ledger-local-dev/internal-packet-closeout-e2-e15.md"),
            System.IO.Path.Combine(root, "docs/audit/product-ledger-local-dev/no-authority-static-scan-contract.md")
        ];
    }

    private static string[] DeferredForbiddenPhraseExcludedCorpusFiles()
    {
        var root = RepositoryRoot();

        return
        [
            System.IO.Path.Combine(root, "docs/audit/product-ledger-local-dev/operator-submission-packet.md"),
            System.IO.Path.Combine(root, "docs/audit/product-ledger-local-dev/external-review-handoff.md"),
            System.IO.Path.Combine(root, "docs/audit/product-ledger-local-dev/operator-review-handoff.md"),
            System.IO.Path.Combine(root, "docs/architecture/nodal-os-runner-filter-safe-commands-guidance.md")
        ];
    }

    private static string RepositoryRoot()
    {
        var current = AppContext.BaseDirectory;

        while (!string.IsNullOrWhiteSpace(current))
        {
            var marker = System.IO.Path.Combine(
                current,
                "docs/architecture/nodal-os-global-roadmap-current-index.md");

            if (System.IO.File.Exists(marker))
            {
                return current;
            }

            current = System.IO.Directory.GetParent(current)?.FullName;
        }

        throw new InvalidOperationException("Repository root not found for narrow forbidden phrase corpus.");
    }

    private static void AssertNoNarrowForbiddenPhraseMatches(string source, string context)
    {
        var matches = ScanNarrowForbiddenPhraseLines(source, context);

        Assert.AreEqual(
            0,
            matches.Count,
            string.Join(Environment.NewLine, matches.Select(match => match.ToString())));
    }

    private static void AssertNoDeferredForbiddenPhraseMatches(string source, string context)
    {
        var matches = ScanDeferredForbiddenPhraseLines(source, context);

        Assert.AreEqual(
            0,
            matches.Count,
            string.Join(Environment.NewLine, matches.Select(match => match.ToString())));
    }

    private static IReadOnlyList<NarrowForbiddenPhraseMatch> ScanNarrowForbiddenPhraseLines(
        string source,
        string context)
    {
        var matches = new List<NarrowForbiddenPhraseMatch>();
        var lines = source.Replace("\r\n", "\n").Split('\n');

        for (var lineIndex = 0; lineIndex < lines.Length; lineIndex++)
        {
            foreach (var segment in SplitClaimSegments(lines[lineIndex]))
            {
                foreach (var phrase in NarrowForbiddenPositivePhrases())
                {
                    if (segment.Contains(phrase.Fragment, StringComparison.OrdinalIgnoreCase)
                        && !IsAllowedNegativeForbiddenPhraseContext(segment))
                    {
                        matches.Add(new NarrowForbiddenPhraseMatch(
                            context,
                            lineIndex + 1,
                            phrase.Category,
                            phrase.Fragment,
                            segment.Trim()));
                    }
                }
            }
        }

        return matches;
    }

    private static IReadOnlyList<NarrowForbiddenPhraseMatch> ScanDeferredForbiddenPhraseLines(
        string source,
        string context)
    {
        var matches = new List<NarrowForbiddenPhraseMatch>();
        var lines = source.Replace("\r\n", "\n").Split('\n');

        for (var lineIndex = 0; lineIndex < lines.Length; lineIndex++)
        {
            foreach (var segment in SplitClaimSegments(lines[lineIndex]))
            {
                foreach (var phrase in DeferredForbiddenPositivePhrases())
                {
                    if (segment.Contains(phrase.Fragment, StringComparison.OrdinalIgnoreCase)
                        && !IsAllowedDeferredForbiddenPhraseContext(segment)
                        && !IsAllowedDeferredForbiddenPhraseCatalogExample(context, lines, lineIndex))
                    {
                        matches.Add(new NarrowForbiddenPhraseMatch(
                            context,
                            lineIndex + 1,
                            phrase.Category,
                            phrase.Fragment,
                            segment.Trim()));
                    }
                }
            }
        }

        return matches;
    }

    private static IEnumerable<string> SplitClaimSegments(string line) =>
        line.Split(['.', ';'], StringSplitOptions.RemoveEmptyEntries);

    private static bool IsAllowedNegativeForbiddenPhraseContext(string segment)
    {
        var value = segment.Trim().ToLowerInvariant();

        return value.StartsWith("- non-goals:", StringComparison.Ordinal)
            || value.Contains("non-goals:", StringComparison.Ordinal)
            || value.Contains(" no ", StringComparison.Ordinal)
            || value.StartsWith("no ", StringComparison.Ordinal)
            || value.Contains(": no", StringComparison.Ordinal)
            || value.Contains(" not ", StringComparison.Ordinal)
            || value.StartsWith("not ", StringComparison.Ordinal)
            || value.Contains("does not authorize", StringComparison.Ordinal)
            || value.Contains("does not enable", StringComparison.Ordinal)
            || value.Contains("remains blocked", StringComparison.Ordinal)
            || value.Contains("remains 0%", StringComparison.Ordinal)
            || value.Contains("no-go", StringComparison.Ordinal)
            || value.Contains("not_authorized_now", StringComparison.Ordinal)
            || value.Contains("blocked", StringComparison.Ordinal)
            || value.Contains("denied", StringComparison.Ordinal)
            || value.Contains("not claimed", StringComparison.Ordinal)
            || value.Contains("historical", StringComparison.Ordinal)
            || value.Contains("superseded", StringComparison.Ordinal)
            || value.Contains("future", StringComparison.Ordinal)
            || value.Contains("requires separate explicit operator authorization", StringComparison.Ordinal)
            || value.Contains("requires explicit operator authorization", StringComparison.Ordinal)
            || value.Contains("ci changed: none", StringComparison.Ordinal)
            || value.Contains("runtime/product changed: none", StringComparison.Ordinal);
    }

    private static bool IsAllowedDeferredForbiddenPhraseContext(string segment)
    {
        var value = segment.Trim().ToLowerInvariant();

        return value.Contains(" no ", StringComparison.Ordinal)
            || value.StartsWith("no ", StringComparison.Ordinal)
            || value.Contains(": no", StringComparison.Ordinal)
            || value.Contains(" not ", StringComparison.Ordinal)
            || value.StartsWith("not ", StringComparison.Ordinal)
            || value.Contains("not claimed", StringComparison.Ordinal)
            || value.Contains("no external response recorded", StringComparison.Ordinal)
            || value.Contains("no external approval claimed", StringComparison.Ordinal)
            || value.Contains("no external audit pass", StringComparison.Ordinal)
            || value.Contains("wait closed without response", StringComparison.Ordinal)
            || value.Contains("without response", StringComparison.Ordinal)
            || value.Contains("internal/operator-attested only", StringComparison.Ordinal)
            || value.Contains("operator-run", StringComparison.Ordinal)
            || value.Contains("operator submission", StringComparison.Ordinal)
            || value.Contains("not submitted", StringComparison.Ordinal)
            || value.Contains("blocked", StringComparison.Ordinal)
            || value.Contains("remains blocked", StringComparison.Ordinal)
            || value.Contains("no-go", StringComparison.Ordinal)
            || value.Contains("not_authorized_now", StringComparison.Ordinal)
            || value.Contains("design-only", StringComparison.Ordinal)
            || value.Contains("future", StringComparison.Ordinal)
            || value.Contains("explicit authorization", StringComparison.Ordinal)
            || value.Contains("explicit response content", StringComparison.Ordinal)
            || value.Contains("requires separate explicit operator authorization", StringComparison.Ordinal)
            || value.Contains("does not authorize", StringComparison.Ordinal)
            || value.Contains("does not grant", StringComparison.Ordinal)
            || value.Contains("no db/cloud/network/provider", StringComparison.Ordinal)
            || value.Contains("no kms/worm guarantee", StringComparison.Ordinal)
            || value.Contains("kms/worm not claimed", StringComparison.Ordinal);
    }

    private static bool IsAllowedDeferredForbiddenPhraseCatalogExample(
        string context,
        string[] lines,
        int lineIndex)
    {
        var normalizedContext = context.Replace('\\', '/');
        if (normalizedContext.EndsWith(
            "docs/audit/product-ledger-local-dev/no-authority-static-scan-contract.md",
            StringComparison.OrdinalIgnoreCase))
        {
            return HasRecentHeading(lines, lineIndex, "## Blocked Claim Families");
        }

        if (normalizedContext.EndsWith(
            "docs/architecture/nodal-os-forbidden-phrase-expansion-corpus-selection.md",
            StringComparison.OrdinalIgnoreCase))
        {
            return HasRecentHeading(lines, lineIndex, "## Phrase Families");
        }

        return false;
    }

    private static bool HasRecentHeading(string[] lines, int lineIndex, string heading)
    {
        var start = Math.Max(0, lineIndex - 24);
        for (var index = start; index <= lineIndex; index++)
        {
            if (lines[index].Contains(heading, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }

    private static IReadOnlyList<(string Category, string Fragment)> NarrowForbiddenPositivePhrases() =>
    [
        ("RuntimeProduct", "runtime enabled"),
        ("RuntimeProduct", "product enabled"),
        ("RuntimeProduct", "runtime/product ready"),
        ("RuntimeProduct", "product authority granted"),
        ("PublicProduct", "public product enabled"),
        ("PublicProduct", "public route live"),
        ("PublicProduct", "product surface live"),
        ("PublicProduct", "public/product exposure implemented"),
        ("PublicProduct", "Product Ledger public product route is live"),
        ("ProductionRoute", "production route enabled"),
        ("ProductionRoute", "production route active"),
        ("ProductionRoute", "production route ready"),
        ("ProductionRoute", "Production route is live"),
        ("LatestPointerReadPrecedence", "latest pointer promoted"),
        ("LatestPointerReadPrecedence", "latest pointer enabled"),
        ("LatestPointerReadPrecedence", "read precedence changed"),
        ("LatestPointerReadPrecedence", "read precedence enabled"),
        ("LatestPointerReadPrecedence", "authoritative read path changed"),
        ("CiEnforcement", "CI enforced"),
        ("CiEnforcement", "CI gate active"),
        ("CiEnforcement", "CI blocks release"),
        ("CiEnforcement", "CI enforcement active"),
        ("ReleaseCommercial", "release approved"),
        ("ReleaseCommercial", "commercial ready"),
        ("ReleaseCommercial", "launch ready"),
        ("ReleaseCommercial", "production-ready"),
        ("ReleaseCommercial", "release ready")
    ];

    private static IReadOnlyList<(string Category, string Fragment)> DeferredForbiddenPositivePhrases() =>
    [
        ("ExternalAuditApproval", "external audit passed"),
        ("ExternalAuditApproval", "external reviewer approved"),
        ("ExternalAuditApproval", "external approval granted"),
        ("ExternalAuditApproval", "reviewer approval recorded"),
        ("ExternalAuditApproval", "external audit completed as approval"),
        ("ExternalAuditApproval", "external review approved product readiness"),
        ("ExternalAuditApproval", "external review approved"),
        ("ExternalAuditApproval", "external approval claimed"),
        ("DbCloudNetworkKmsWorm", "DB enabled"),
        ("DbCloudNetworkKmsWorm", "database migration active"),
        ("DbCloudNetworkKmsWorm", "cloud backed"),
        ("DbCloudNetworkKmsWorm", "provider enabled"),
        ("DbCloudNetworkKmsWorm", "network enabled"),
        ("DbCloudNetworkKmsWorm", "KMS guaranteed"),
        ("DbCloudNetworkKmsWorm", "WORM guaranteed"),
        ("DbCloudNetworkKmsWorm", "durable WORM active"),
        ("DbCloudNetworkKmsWorm", "external trust guaranteed"),
        ("DbCloudNetworkKmsWorm", "DB/cloud/network enabled"),
        ("DbCloudNetworkKmsWorm", "KMS/WORM guarantee"),
        ("DbCloudNetworkKmsWorm", "cloud/provider capability enabled")
    ];

    private sealed record NarrowForbiddenPhraseMatch(
        string Context,
        int Line,
        string Category,
        string Fragment,
        string Text);
}
