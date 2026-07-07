using Microsoft.VisualStudio.TestTools.UnitTesting;
using OneBrain.Core.Approval;

namespace OneBrain.Safety.Tests;

[TestClass]
[TestCategory("NodalOsTier1Safety")]
[TestCategory("ProductLedger")]
[TestCategory("CommonContracts")]
[TestCategory("DesignOnly")]
[TestCategory("NoRuntimeWiring")]
[TestCategory("SourceCandidate")]
public sealed class NodalOsCommonBoundaryClaimsCandidateTests
{
    [TestMethod]
    [TestCategory("PublicProductBlock")]
    [TestCategory("ProductionRouteBlock")]
    [TestCategory("LatestPointerBlock")]
    [TestCategory("ReadPrecedenceBlock")]
    [TestCategory("ProductAuthorityBlock")]
    [TestCategory("CommandExecutionBlock")]
    [TestCategory("ReleaseCommercialBlock")]
    public void CandidateDefaultsAreFailClosed()
    {
        var candidate = NodalOsCommonBoundaryClaimsCandidate.DefaultBlocked();

        foreach (var claim in Enum.GetValues<NodalOsCommonBoundaryClaimsCandidate.Claim>())
        {
            Assert.IsTrue(candidate.IsFailClosed(claim), claim.ToString());
        }

        Assert.IsTrue(candidate.ParallelOnly);
        Assert.IsTrue(candidate.NonAuthoritative);
        Assert.IsFalse(candidate.AllowsRuntimeProductOrAuthority());
    }

    [TestMethod]
    [TestCategory("PublicProductBlock")]
    public void CandidateRepresentsPublicProductBlockedWithoutEnablingProduct()
    {
        var candidate = NodalOsCommonBoundaryClaimsCandidate.DefaultBlocked();

        Assert.AreEqual(
            NodalOsCommonBoundaryClaimsCandidate.ClaimState.Blocked,
            candidate.StateFor(NodalOsCommonBoundaryClaimsCandidate.Claim.PublicProductBlocked));
        Assert.IsFalse(candidate.ProductAuthorityAllowed);
        Assert.IsFalse(candidate.RuntimeProductEnablementAllowed);
    }

    [TestMethod]
    [TestCategory("ProductionRouteBlock")]
    public void CandidateRepresentsProductionRouteBlockedWithoutOpeningRoute()
    {
        var candidate = NodalOsCommonBoundaryClaimsCandidate.DefaultBlocked();

        Assert.AreEqual(
            NodalOsCommonBoundaryClaimsCandidate.ClaimState.Blocked,
            candidate.StateFor(NodalOsCommonBoundaryClaimsCandidate.Claim.ProductionRouteBlocked));
        Assert.IsFalse(candidate.RouteRegistered);
    }

    [TestMethod]
    [TestCategory("LatestPointerBlock")]
    public void CandidateRepresentsLatestPointerDisabledWithoutCreatingPointer()
    {
        var candidate = NodalOsCommonBoundaryClaimsCandidate.DefaultBlocked();

        Assert.AreEqual(
            NodalOsCommonBoundaryClaimsCandidate.ClaimState.Disabled,
            candidate.StateFor(NodalOsCommonBoundaryClaimsCandidate.Claim.LatestPointerDisabled));
        Assert.IsFalse(candidate.ExistingHardBlockAuthorityReplaced);
    }

    [TestMethod]
    [TestCategory("ReadPrecedenceBlock")]
    public void CandidateRepresentsReadPrecedenceDisabledWithoutActivatingPrecedence()
    {
        var candidate = NodalOsCommonBoundaryClaimsCandidate.DefaultBlocked();

        Assert.AreEqual(
            NodalOsCommonBoundaryClaimsCandidate.ClaimState.Disabled,
            candidate.StateFor(NodalOsCommonBoundaryClaimsCandidate.Claim.ReadPrecedenceDisabled));
        Assert.IsFalse(candidate.CanOverrideExistingHardBlock(NodalOsCommonBoundaryClaimsCandidate.Claim.ReadPrecedenceDisabled));
    }

    [TestMethod]
    [TestCategory("ProductAuthorityBlock")]
    public void CandidateRepresentsProductAuthorityBlockedWithoutCreatingAuthority()
    {
        var candidate = NodalOsCommonBoundaryClaimsCandidate.DefaultBlocked();

        Assert.AreEqual(
            NodalOsCommonBoundaryClaimsCandidate.ClaimState.Blocked,
            candidate.StateFor(NodalOsCommonBoundaryClaimsCandidate.Claim.ProductAuthorityBlocked));
        Assert.IsFalse(candidate.ProductAuthorityAllowed);
        Assert.IsFalse(candidate.ExistingHardBlockAuthorityReplaced);
    }

    [TestMethod]
    [TestCategory("CommandExecutionBlock")]
    public void CandidateRepresentsCommandExecutionDeniedWithoutExecutionCapability()
    {
        var candidate = NodalOsCommonBoundaryClaimsCandidate.DefaultBlocked();

        Assert.AreEqual(
            NodalOsCommonBoundaryClaimsCandidate.ClaimState.Denied,
            candidate.StateFor(NodalOsCommonBoundaryClaimsCandidate.Claim.CommandExecutionDenied));
        Assert.AreEqual(
            NodalOsCommonBoundaryClaimsCandidate.ClaimState.Denied,
            candidate.StateFor(NodalOsCommonBoundaryClaimsCandidate.Claim.ShellSubprocessDenied));
        Assert.IsFalse(candidate.CommandHandlerRegistered);
    }

    [TestMethod]
    [TestCategory("ReleaseCommercialBlock")]
    public void CandidateRepresentsReleaseCommercialNoGoWithoutReadinessClaim()
    {
        var candidate = NodalOsCommonBoundaryClaimsCandidate.DefaultBlocked();

        Assert.AreEqual(
            NodalOsCommonBoundaryClaimsCandidate.ClaimState.NoGo,
            candidate.StateFor(NodalOsCommonBoundaryClaimsCandidate.Claim.ReleaseCommercialNoGo));
        Assert.IsFalse(candidate.ReleaseCommercialReady);
    }

    [TestMethod]
    public void CandidateRepresentsNoRuntimeProductEnablement()
    {
        var candidate = NodalOsCommonBoundaryClaimsCandidate.DefaultBlocked();

        Assert.AreEqual(
            NodalOsCommonBoundaryClaimsCandidate.ClaimState.NoGo,
            candidate.StateFor(NodalOsCommonBoundaryClaimsCandidate.Claim.RuntimeProductEnablementNoGo));
        Assert.IsFalse(candidate.RuntimeWired);
        Assert.IsFalse(candidate.ServiceRegistered);
        Assert.IsFalse(candidate.RuntimeProductEnablementAllowed);
    }

    [TestMethod]
    public void CandidateRejectsUnknownOrAmbiguousClaimsFailClosed()
    {
        var candidate = NodalOsCommonBoundaryClaimsCandidate.DefaultBlocked();
        var unknown = (NodalOsCommonBoundaryClaimsCandidate.Claim)999;

        Assert.IsFalse(candidate.IsSupported(unknown));
        Assert.AreEqual(NodalOsCommonBoundaryClaimsCandidate.ClaimState.Denied, candidate.StateFor(unknown));
        Assert.IsTrue(candidate.IsFailClosed(unknown));
        Assert.IsFalse(candidate.CanOverrideExistingHardBlock(unknown));
    }

    [TestMethod]
    [TestCategory("StaticGuard")]
    public void SourceCandidateIsNotReferencedByRoutesOrRuntimeRegistration()
    {
        var root = RepoRoot();
        var candidateSource = CandidateSource(root);
        var runtimeReferences = SourceFilesExceptCandidate(root)
            .Where(path => IsRouteRuntimeOrHostPath(path))
            .Where(path => File.ReadAllText(path).Contains(nameof(NodalOsCommonBoundaryClaimsCandidate), StringComparison.Ordinal))
            .ToArray();

        CollectionAssert.AreEqual(Array.Empty<string>(), runtimeReferences);
        AssertCandidateSourceHasNoForbiddenRuntimePatterns(candidateSource);
    }

    [TestMethod]
    [TestCategory("StaticGuard")]
    [TestCategory("CommandExecutionBlock")]
    public void SourceCandidateIsNotReferencedByCommandHandlers()
    {
        var root = RepoRoot();
        var references = SourceFilesExceptCandidate(root)
            .Where(path => !path.EndsWith(
                $"{Path.DirectorySeparatorChar}ReentryDecisionPacketReadOnly.cs",
                StringComparison.Ordinal))
            .Where(path => File.ReadAllText(path).Contains(nameof(NodalOsCommonBoundaryClaimsCandidate), StringComparison.Ordinal)
                && File.ReadAllText(path).Contains("Command", StringComparison.OrdinalIgnoreCase))
            .ToArray();

        CollectionAssert.AreEqual(Array.Empty<string>(), references);
        Assert.IsFalse(NodalOsCommonBoundaryClaimsCandidate.DefaultBlocked().CommandHandlerRegistered);
    }

    [TestMethod]
    [TestCategory("StaticGuard")]
    public void SourceCandidateIsNotReferencedByCiOrGateEnforcement()
    {
        var root = RepoRoot();
        var ciFiles = Directory.EnumerateFiles(root, "*", SearchOption.AllDirectories)
            .Where(path => path.Contains($"{Path.DirectorySeparatorChar}.github{Path.DirectorySeparatorChar}", StringComparison.Ordinal)
                || path.EndsWith("azure-pipelines.yml", StringComparison.OrdinalIgnoreCase))
            .Where(path => File.ReadAllText(path).Contains(nameof(NodalOsCommonBoundaryClaimsCandidate), StringComparison.Ordinal)
                || File.ReadAllText(path).Contains("SourceCandidate", StringComparison.Ordinal))
            .ToArray();

        CollectionAssert.AreEqual(Array.Empty<string>(), ciFiles);
        Assert.IsFalse(NodalOsCommonBoundaryClaimsCandidate.DefaultBlocked().CiEnforcementClaimed);
    }

    [TestMethod]
    [TestCategory("ProductAuthorityBlock")]
    public void SourceCandidateDoesNotReplaceExistingHardBlockAuthorities()
    {
        var d1Claims = NodalOsCommonBoundaryClaims.CurrentLocalDesignOnlyNoGo();
        var d2Mapping = NodalOsCommonBoundaryMappingDesignOnlyAdapter.Map(
            new NodalOsCommonBoundaryMappingRequest(NodalOsCommonBoundarySourceConcept.StaticGuardCatalogHardBlock));
        var candidate = NodalOsCommonBoundaryClaimsCandidate.DefaultBlocked();

        Assert.IsTrue(d1Claims.IsBlocked(NodalOsCommonBoundaryCapability.ProductAuthority));
        Assert.IsTrue(d2Mapping.ExistingHardBlockAuthoritative);
        Assert.IsFalse(d2Mapping.OverrideAllowed);
        Assert.IsFalse(candidate.ExistingHardBlockAuthorityReplaced);
    }

    [TestMethod]
    [TestCategory("ProductAuthorityBlock")]
    public void SourceCandidateCannotOverrideExistingBlockDecisions()
    {
        var candidate = NodalOsCommonBoundaryClaimsCandidate.DefaultBlocked();

        foreach (var claim in Enum.GetValues<NodalOsCommonBoundaryClaimsCandidate.Claim>())
        {
            Assert.IsFalse(candidate.CanOverrideExistingHardBlock(claim), claim.ToString());
        }
    }

    [TestMethod]
    [TestCategory("StaticGuard")]
    public void SourceCandidateIsDocumentedAsParallelOnlyNonAuthoritative()
    {
        var source = CandidateSource(RepoRoot());

        StringAssert.Contains(source, "Parallel-only source candidate");
        StringAssert.Contains(source, "descriptive and non-authoritative");
        StringAssert.Contains(source, "not runtime-wired");
        StringAssert.Contains(source, "cannot override existing hard-block decisions");
    }

    [TestMethod]
    [TestCategory("MappingAdapters")]
    public void D2MappingConceptsAlignWithSourceCandidateFailClosedDefaults()
    {
        var candidate = NodalOsCommonBoundaryClaimsCandidate.DefaultBlocked();

        foreach (var concept in SupportedD2Concepts())
        {
            var result = NodalOsCommonBoundaryMappingDesignOnlyAdapter.Map(
                new NodalOsCommonBoundaryMappingRequest(concept));

            foreach (var capability in result.MappedCapabilities)
            {
                Assert.IsTrue(candidate.IsFailClosed(ToCandidateClaim(capability)), $"{concept}:{capability}");
            }
        }
    }

    [TestMethod]
    [TestCategory("MappingAdapters")]
    public void ExistingD2MapperRemainsNonAuthoritative()
    {
        foreach (var concept in SupportedD2Concepts())
        {
            var result = NodalOsCommonBoundaryMappingDesignOnlyAdapter.Map(
                new NodalOsCommonBoundaryMappingRequest(concept));

            Assert.IsFalse(result.OverrideAllowed, concept.ToString());
            Assert.IsFalse(result.Envelope.HasRuntimeOrProductSurface(), concept.ToString());
        }
    }

    [TestMethod]
    [TestCategory("MappingAdapters")]
    public void SourceCandidateDoesNotSupersedeD2Mapper()
    {
        var candidate = NodalOsCommonBoundaryClaimsCandidate.DefaultBlocked();
        var result = NodalOsCommonBoundaryMappingDesignOnlyAdapter.Map(
            new NodalOsCommonBoundaryMappingRequest(NodalOsCommonBoundarySourceConcept.PublicProductHardBlock));

        Assert.IsTrue(result.ExistingHardBlockAuthoritative);
        Assert.IsFalse(result.OverrideAllowed);
        Assert.IsFalse(candidate.ExistingHardBlockAuthorityReplaced);
        Assert.IsFalse(candidate.CanOverrideExistingHardBlock(NodalOsCommonBoundaryClaimsCandidate.Claim.PublicProductBlocked));
    }

    private static void AssertCandidateSourceHasNoForbiddenRuntimePatterns(string source)
    {
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
            "File" + ".Write",
            "Directory" + ".CreateDirectory"
        })
        {
            Assert.IsFalse(source.Contains(forbidden, StringComparison.Ordinal), forbidden);
        }
    }

    private static NodalOsCommonBoundaryClaimsCandidate.Claim ToCandidateClaim(
        NodalOsCommonBoundaryCapability capability) =>
        capability switch
        {
            NodalOsCommonBoundaryCapability.PublicProductExposure => NodalOsCommonBoundaryClaimsCandidate.Claim.PublicProductBlocked,
            NodalOsCommonBoundaryCapability.ProductionRoute => NodalOsCommonBoundaryClaimsCandidate.Claim.ProductionRouteBlocked,
            NodalOsCommonBoundaryCapability.LatestPointer => NodalOsCommonBoundaryClaimsCandidate.Claim.LatestPointerDisabled,
            NodalOsCommonBoundaryCapability.ReadPrecedence => NodalOsCommonBoundaryClaimsCandidate.Claim.ReadPrecedenceDisabled,
            NodalOsCommonBoundaryCapability.ProductAuthority => NodalOsCommonBoundaryClaimsCandidate.Claim.ProductAuthorityBlocked,
            NodalOsCommonBoundaryCapability.CommandExecution => NodalOsCommonBoundaryClaimsCandidate.Claim.CommandExecutionDenied,
            NodalOsCommonBoundaryCapability.ShellSubprocess => NodalOsCommonBoundaryClaimsCandidate.Claim.ShellSubprocessDenied,
            NodalOsCommonBoundaryCapability.ProviderCloudNetwork => NodalOsCommonBoundaryClaimsCandidate.Claim.ProviderCloudNetworkNotClaimed,
            NodalOsCommonBoundaryCapability.DatabaseMigration => NodalOsCommonBoundaryClaimsCandidate.Claim.DatabaseMigrationNotClaimed,
            NodalOsCommonBoundaryCapability.KmsWormExternalTrust => NodalOsCommonBoundaryClaimsCandidate.Claim.ExternalTrustNotClaimed,
            NodalOsCommonBoundaryCapability.ReleaseCommercialReadiness => NodalOsCommonBoundaryClaimsCandidate.Claim.ReleaseCommercialNoGo,
            NodalOsCommonBoundaryCapability.PilotRunCoupling => NodalOsCommonBoundaryClaimsCandidate.Claim.RuntimeProductEnablementNoGo,
            NodalOsCommonBoundaryCapability.CiEnforcement => NodalOsCommonBoundaryClaimsCandidate.Claim.CiEnforcementNotClaimed,
            _ => (NodalOsCommonBoundaryClaimsCandidate.Claim)999
        };

    private static IEnumerable<NodalOsCommonBoundarySourceConcept> SupportedD2Concepts() =>
        Enum.GetValues<NodalOsCommonBoundarySourceConcept>()
            .Where(concept => concept is not NodalOsCommonBoundarySourceConcept.UnsupportedSourceConcept
                and not NodalOsCommonBoundarySourceConcept.UnknownAmbiguousState);

    private static bool IsRouteRuntimeOrHostPath(string path) =>
        path.Contains($"{Path.DirectorySeparatorChar}OneBrain.Pilot{Path.DirectorySeparatorChar}", StringComparison.Ordinal)
        || path.Contains($"{Path.DirectorySeparatorChar}OneBrain.Cli{Path.DirectorySeparatorChar}", StringComparison.Ordinal)
        || path.EndsWith($"{Path.DirectorySeparatorChar}Program.cs", StringComparison.OrdinalIgnoreCase);

    private static IEnumerable<string> SourceFilesExceptCandidate(string root) =>
        Directory.EnumerateFiles(Path.Combine(root, "src"), "*.cs", SearchOption.AllDirectories)
            .Where(path => !path.EndsWith(
                $"{Path.DirectorySeparatorChar}NodalOsCommonBoundaryClaimsCandidate.cs",
                StringComparison.Ordinal));

    private static string CandidateSource(string root) =>
        File.ReadAllText(Path.Combine(
            root,
            "src",
            "OneBrain.Core",
            "Approval",
            "NodalOsCommonBoundaryClaimsCandidate.cs"));

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
