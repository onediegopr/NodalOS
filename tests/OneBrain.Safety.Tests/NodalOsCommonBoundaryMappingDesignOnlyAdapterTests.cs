using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace OneBrain.Safety.Tests;

[TestClass]
[TestCategory("NodalOsTier1Safety")]
[TestCategory("ProductLedger")]
[TestCategory("CommonContracts")]
[TestCategory("MappingAdapters")]
[TestCategory("DesignOnly")]
[TestCategory("NoRuntimeWiring")]
public sealed class NodalOsCommonBoundaryMappingDesignOnlyAdapterTests
{
    [TestMethod]
    [TestCategory("PublicProductBlock")]
    public void PublicProductBlockMapsToCommonBlockedExposure() =>
        AssertMapsBlocked(
            NodalOsCommonBoundarySourceConcept.PublicProductHardBlock,
            NodalOsCommonBoundaryCapability.PublicProductExposure);

    [TestMethod]
    [TestCategory("ProductionRouteBlock")]
    public void ProductionRouteBlockMapsToCommonBlockedExposure() =>
        AssertMapsBlocked(
            NodalOsCommonBoundarySourceConcept.ProductionRouteHardBlock,
            NodalOsCommonBoundaryCapability.ProductionRoute);

    [TestMethod]
    [TestCategory("LatestPointerBlock")]
    public void LatestPointerDisabledMapsToCommonDisabledPointer() =>
        AssertMapsBlocked(
            NodalOsCommonBoundarySourceConcept.LatestPointerDenied,
            NodalOsCommonBoundaryCapability.LatestPointer);

    [TestMethod]
    [TestCategory("ReadPrecedenceBlock")]
    public void ReadPrecedenceDisabledMapsToCommonDisabledPrecedence() =>
        AssertMapsBlocked(
            NodalOsCommonBoundarySourceConcept.ReadPrecedenceDenied,
            NodalOsCommonBoundaryCapability.ReadPrecedence);

    [TestMethod]
    [TestCategory("ProductAuthorityBlock")]
    public void ProductAuthorityDeniedMapsToCommonAbsentAuthority() =>
        AssertMapsBlocked(
            NodalOsCommonBoundarySourceConcept.ProductAuthorityDenied,
            NodalOsCommonBoundaryCapability.ProductAuthority);

    [TestMethod]
    [TestCategory("CommandExecutionBlock")]
    public void CommandExecutionDeniedMapsToCommonDeniedExecutionPermission()
    {
        var result = Map(NodalOsCommonBoundarySourceConcept.CommandExecutionDenied);

        CollectionAssert.Contains(result.MappedCapabilities.ToArray(), NodalOsCommonBoundaryCapability.CommandExecution);
        CollectionAssert.Contains(result.MappedCapabilities.ToArray(), NodalOsCommonBoundaryCapability.ShellSubprocess);
        AssertBlockedAndNonAuthoritative(result);
    }

    [TestMethod]
    [TestCategory("ReleaseCommercialBlock")]
    public void ReleaseCommercialDeniedMapsToCommonNoGoReadiness() =>
        AssertMapsBlocked(
            NodalOsCommonBoundarySourceConcept.ReleaseCommercialDenied,
            NodalOsCommonBoundaryCapability.ReleaseCommercialReadiness);

    [TestMethod]
    [TestCategory("PublicProductBlock")]
    [TestCategory("ProductionRouteBlock")]
    [TestCategory("ReleaseCommercialBlock")]
    public void ProductLedgerSafetyBoundaryMapsToCommonLocalDesignOnlyEvidenceBoundary()
    {
        var result = Map(NodalOsCommonBoundarySourceConcept.ProductLedgerLocalDesignOnlySafetyBoundary);

        CollectionAssert.Contains(result.MappedCapabilities.ToArray(), NodalOsCommonBoundaryCapability.PublicProductExposure);
        CollectionAssert.Contains(result.MappedCapabilities.ToArray(), NodalOsCommonBoundaryCapability.ProductionRoute);
        CollectionAssert.Contains(result.MappedCapabilities.ToArray(), NodalOsCommonBoundaryCapability.ProviderCloudNetwork);
        CollectionAssert.Contains(result.MappedCapabilities.ToArray(), NodalOsCommonBoundaryCapability.DatabaseMigration);
        CollectionAssert.Contains(result.MappedCapabilities.ToArray(), NodalOsCommonBoundaryCapability.KmsWormExternalTrust);
        CollectionAssert.Contains(result.MappedCapabilities.ToArray(), NodalOsCommonBoundaryCapability.ReleaseCommercialReadiness);
        Assert.AreEqual(NodalOsCommonEvidenceBoundary.DesignOnly, result.Envelope.Claims.EvidenceBoundary);
        AssertBlockedAndNonAuthoritative(result);
    }

    [TestMethod]
    [TestCategory("RunClaimCoherence")]
    public void RunClaimCoherenceMapsToCommonNoRuntimeOverclaimDecision() =>
        AssertMapsBlocked(
            NodalOsCommonBoundarySourceConcept.RunClaimCoherenceNoRuntimeOverclaim,
            NodalOsCommonBoundaryCapability.PilotRunCoupling);

    [TestMethod]
    [TestCategory("StaticGuard")]
    [TestCategory("PublicProductBlock")]
    [TestCategory("ProductionRouteBlock")]
    [TestCategory("LatestPointerBlock")]
    [TestCategory("ReadPrecedenceBlock")]
    [TestCategory("ProductAuthorityBlock")]
    [TestCategory("CommandExecutionBlock")]
    [TestCategory("ReleaseCommercialBlock")]
    public void StaticGuardCatalogHardBlocksMapToCommonBlockedBoundaryClaims()
    {
        var result = Map(NodalOsCommonBoundarySourceConcept.StaticGuardCatalogHardBlock);

        foreach (var capability in new[]
        {
            NodalOsCommonBoundaryCapability.PublicProductExposure,
            NodalOsCommonBoundaryCapability.ProductionRoute,
            NodalOsCommonBoundaryCapability.LatestPointer,
            NodalOsCommonBoundaryCapability.ReadPrecedence,
            NodalOsCommonBoundaryCapability.ProductAuthority,
            NodalOsCommonBoundaryCapability.CommandExecution,
            NodalOsCommonBoundaryCapability.ShellSubprocess,
            NodalOsCommonBoundaryCapability.ProviderCloudNetwork,
            NodalOsCommonBoundaryCapability.DatabaseMigration,
            NodalOsCommonBoundaryCapability.KmsWormExternalTrust,
            NodalOsCommonBoundaryCapability.ReleaseCommercialReadiness
        })
        {
            CollectionAssert.Contains(result.MappedCapabilities.ToArray(), capability);
            Assert.IsTrue(result.Envelope.Claims.IsBlocked(capability), capability.ToString());
        }

        AssertBlockedAndNonAuthoritative(result);
    }

    [TestMethod]
    [TestCategory("PublicProductBlock")]
    [TestCategory("ProductionRouteBlock")]
    [TestCategory("CommandExecutionBlock")]
    public void MappingAdaptersDoNotCreateRuntimeAuthority()
    {
        foreach (var concept in SupportedConcepts())
        {
            var result = Map(concept);

            Assert.IsFalse(result.Envelope.RuntimeWired, concept.ToString());
            Assert.IsFalse(result.Envelope.ServiceRegistered, concept.ToString());
            Assert.IsFalse(result.Envelope.RouteRegistered, concept.ToString());
            Assert.IsFalse(result.Envelope.CommandHandlerRegistered, concept.ToString());
            Assert.IsFalse(result.Envelope.HasRuntimeOrProductSurface(), concept.ToString());
            Assert.IsFalse(result.OverrideAllowed, concept.ToString());
        }
    }

    [TestMethod]
    public void MappingAdaptersDoNotCreateCiEnforcement()
    {
        foreach (var concept in SupportedConcepts())
        {
            var result = Map(concept);

            Assert.IsFalse(result.Envelope.Claims.CiEnforcementAllowed, concept.ToString());
            Assert.IsTrue(result.Envelope.Claims.IsBlocked(NodalOsCommonBoundaryCapability.CiEnforcement), concept.ToString());
        }
    }

    [TestMethod]
    public void MappingAdaptersAreNotDoubleTruthAndUnknownFailClosed()
    {
        var unsupported = Map(NodalOsCommonBoundarySourceConcept.UnsupportedSourceConcept);
        var unknown = Map(NodalOsCommonBoundarySourceConcept.UnknownAmbiguousState);
        var notAuthoritative = NodalOsCommonBoundaryMappingDesignOnlyAdapter.Map(
            new NodalOsCommonBoundaryMappingRequest(
                NodalOsCommonBoundarySourceConcept.PublicProductHardBlock,
                ExistingHardBlockAuthoritative: false));

        foreach (var result in new[] { unsupported, unknown, notAuthoritative })
        {
            Assert.IsTrue(result.IsFailClosed, result.SourceConcept.ToString());
            Assert.AreEqual(NodalOsCommonSafetyDecision.Rejected, result.Envelope.Decision);
            Assert.IsFalse(result.OverrideAllowed);
            Assert.IsFalse(result.Envelope.HasRuntimeOrProductSurface());
        }

        Assert.IsFalse(notAuthoritative.ExistingHardBlockAuthoritative);
    }

    [TestMethod]
    [TestCategory("StaticGuard")]
    public void MappingAdaptersAreTestOnlyAndNotRuntimeWired()
    {
        var root = RepoRoot();
        var d2Source = File.ReadAllText(Path.Combine(
            root,
            "tests",
            "OneBrain.Safety.Tests",
            "NodalOsCommonBoundaryMappingDesignOnlyAdapter.cs"));
        var productionSource = string.Join(
            Environment.NewLine,
            Directory.EnumerateFiles(Path.Combine(root, "src"), "*.cs", SearchOption.AllDirectories)
                .OrderBy(path => path, StringComparer.Ordinal)
                .Select(File.ReadAllText));

        StringAssert.Contains(d2Source, "D2 design/test-only mapper");
        StringAssert.Contains(d2Source, "not runtime authority");
        StringAssert.Contains(d2Source, "RuntimeWired: false");
        StringAssert.Contains(d2Source, "ServiceRegistered: false");
        StringAssert.Contains(d2Source, "RouteRegistered: false");
        StringAssert.Contains(d2Source, "CommandHandlerRegistered: false");

        foreach (var forbidden in new[]
        {
            "IService" + "Collection",
            "Add" + "Singleton",
            "Add" + "Scoped",
            "Add" + "Transient",
            "Map" + "Get",
            "Map" + "Post",
            "ICommand" + "Handler",
            "Process" + ".Start",
            "Shell" + "Execute",
            "Http" + "Client",
            "Db" + "Context",
            "Migration" + "Builder",
            "Kms" + "Client",
            "Worm" + "Store",
            "ReleaseCommercialReady:" + " true"
        })
        {
            Assert.IsFalse(d2Source.Contains(forbidden, StringComparison.Ordinal), forbidden);
        }

        Assert.IsFalse(productionSource.Contains("NodalOsCommonBoundaryMappingDesignOnlyAdapter", StringComparison.Ordinal));
        Assert.IsFalse(productionSource.Contains("NodalOsCommonBoundarySourceConcept", StringComparison.Ordinal));
    }

    private static NodalOsCommonBoundaryMappingResult Map(NodalOsCommonBoundarySourceConcept concept) =>
        NodalOsCommonBoundaryMappingDesignOnlyAdapter.Map(new NodalOsCommonBoundaryMappingRequest(concept));

    private static void AssertMapsBlocked(
        NodalOsCommonBoundarySourceConcept concept,
        NodalOsCommonBoundaryCapability capability)
    {
        var result = Map(concept);

        CollectionAssert.Contains(result.MappedCapabilities.ToArray(), capability);
        Assert.IsTrue(result.Envelope.Claims.IsBlocked(capability), capability.ToString());
        AssertBlockedAndNonAuthoritative(result);
    }

    private static void AssertBlockedAndNonAuthoritative(NodalOsCommonBoundaryMappingResult result)
    {
        Assert.IsTrue(result.ExistingHardBlockAuthoritative);
        Assert.IsFalse(result.UnsupportedSourceConcept);
        Assert.IsFalse(result.OverrideAllowed);
        Assert.IsTrue(result.IsFailClosed);
        Assert.IsFalse(result.Envelope.HasRuntimeOrProductSurface());
        Assert.AreEqual(NodalOsCommonSafetyDecision.DescriptiveOnlyNoRuntimeAuthority, result.Envelope.Decision);
    }

    private static IEnumerable<NodalOsCommonBoundarySourceConcept> SupportedConcepts() =>
        Enum.GetValues<NodalOsCommonBoundarySourceConcept>()
            .Where(concept => concept is not NodalOsCommonBoundarySourceConcept.UnsupportedSourceConcept
                and not NodalOsCommonBoundarySourceConcept.UnknownAmbiguousState);

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
