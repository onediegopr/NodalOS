using Microsoft.VisualStudio.TestTools.UnitTesting;
using OneBrain.Core.Approval;
using static OneBrain.Safety.Tests.NodalOsCommonBoundaryMappingDesignOnlyAdapter;

namespace OneBrain.Safety.Tests;

[TestClass]
[TestCategory("NodalOsTier1Safety")]
[TestCategory("ProductLedger")]
[TestCategory("CommonContracts")]
[TestCategory("DesignOnly")]
[TestCategory("MappingAdapters")]
[TestCategory("SourceCandidate")]
[TestCategory("NoRuntimeWiring")]
[TestCategory("NoAuthority")]
[TestCategory("NoDoubleTruth")]
public sealed class NodalOsCommonBoundaryClaimsCandidateIsolationHardeningTests
{
    [TestMethod]
    [TestCategory("StaticGuard")]
    public void D5_CandidateIsNotReferencedFromPilotRuntime()
    {
        var references = ReferencesUnderExistingRoots("src/OneBrain.Pilot");

        CollectionAssert.AreEqual(Array.Empty<string>(), references);
    }

    [TestMethod]
    [TestCategory("StaticGuard")]
    [TestCategory("CommandExecutionBlock")]
    public void D5_CandidateIsNotReferencedFromCliOrCommandExecutionPaths()
    {
        var references = ReferencesUnderExistingRoots(
            "src/OneBrain.Cli",
            "src/OneBrain.Actions",
            "src/OneBrain.Core/Execution");

        CollectionAssert.AreEqual(Array.Empty<string>(), references);
    }

    [TestMethod]
    [TestCategory("StaticGuard")]
    [TestCategory("PublicProductBlock")]
    [TestCategory("ProductionRouteBlock")]
    public void D5_CandidateIsNotReferencedFromRoutesDiServicesOrProductLedgerRuntimePaths()
    {
        var root = RepoRoot();
        var references = SourceFilesExceptCandidate(root)
            .Where(path => IsRouteDiServiceOrProductLedgerRuntimePath(path))
            .Where(ContainsCandidate)
            .ToArray();

        CollectionAssert.AreEqual(Array.Empty<string>(), references);
    }

    [TestMethod]
    [TestCategory("StaticGuard")]
    public void D5_CandidateIsNotReferencedFromCiWorkflowOrGateEnforcementFiles()
    {
        var references = CandidateReferencesInExistingCiFiles();

        CollectionAssert.AreEqual(Array.Empty<string>(), references);
    }

    [TestMethod]
    [TestCategory("StaticGuard")]
    public void D5_CandidateAppearsOnlyInAllowedDesignAuditAndSafetyLocations()
    {
        var root = RepoRoot();
        var unexpected = CandidateReferenceFiles(root)
            .Where(path => !IsAllowedCandidateReference(root, path))
            .ToArray();

        CollectionAssert.AreEqual(Array.Empty<string>(), unexpected);
    }

    [TestMethod]
    [TestCategory("NoAuthority")]
    [TestCategory("PublicProductBlock")]
    [TestCategory("ProductionRouteBlock")]
    [TestCategory("CommandExecutionBlock")]
    [TestCategory("ReleaseCommercialBlock")]
    public void D5_DefaultCandidateCannotCreateAnyRuntimeProductOrReleaseAuthority()
    {
        var candidate = NodalOsCommonBoundaryClaimsCandidate.DefaultBlocked();

        Assert.IsFalse(candidate.RuntimeWired);
        Assert.IsFalse(candidate.ServiceRegistered);
        Assert.IsFalse(candidate.RouteRegistered);
        Assert.IsFalse(candidate.CommandHandlerRegistered);
        Assert.IsFalse(candidate.RuntimeProductEnablementAllowed);
        Assert.IsFalse(candidate.ProductAuthorityAllowed);
        Assert.IsFalse(candidate.CiEnforcementClaimed);
        Assert.IsFalse(candidate.ReleaseCommercialReady);
        Assert.IsFalse(candidate.AllowsRuntimeProductOrAuthority());
    }

    [TestMethod]
    [TestCategory("NoAuthority")]
    public void D5_UnsafeAuthorityFlagsAreDetectedAsRuntimeProductOrAuthority()
    {
        foreach (var unsafeCandidate in UnsafeFlagVariants())
        {
            Assert.IsTrue(unsafeCandidate.AllowsRuntimeProductOrAuthority(), unsafeCandidate.ToString());
        }
    }

    [TestMethod]
    [TestCategory("NoAuthority")]
    [TestCategory("PublicProductBlock")]
    [TestCategory("ProductionRouteBlock")]
    [TestCategory("LatestPointerBlock")]
    [TestCategory("ReadPrecedenceBlock")]
    [TestCategory("ProductAuthorityBlock")]
    [TestCategory("CommandExecutionBlock")]
    [TestCategory("ReleaseCommercialBlock")]
    public void D5_CandidateClaimStatesDoNotContainAllowedEnabledActiveCreatedPresentOrGo()
    {
        var stateNames = Enum.GetNames<NodalOsCommonBoundaryClaimsCandidate.ClaimState>();
        var expectedClosedStates = new[] { "Blocked", "Disabled", "Denied", "NotClaimed", "NoGo" };
        var forbidden = new[] { "Allowed", "Enabled", "Active", "Created", "Present", "Ready" };

        CollectionAssert.AreEquivalent(expectedClosedStates, stateNames);

        foreach (var name in stateNames)
        {
            foreach (var forbiddenWord in forbidden)
            {
                Assert.IsFalse(name.Contains(forbiddenWord, StringComparison.OrdinalIgnoreCase), name);
            }
        }
    }

    [TestMethod]
    [TestCategory("NoAuthority")]
    public void D5_MissingAndUnsupportedFutureClaimsRemainFailClosed()
    {
        var missingKnownClaim = NodalOsCommonBoundaryClaimsCandidate.DefaultBlocked() with
        {
            Claims = new Dictionary<NodalOsCommonBoundaryClaimsCandidate.Claim, NodalOsCommonBoundaryClaimsCandidate.ClaimState>()
        };
        var unsupportedFutureClaim = (NodalOsCommonBoundaryClaimsCandidate.Claim)999;

        Assert.AreEqual(
            NodalOsCommonBoundaryClaimsCandidate.ClaimState.Denied,
            missingKnownClaim.StateFor(NodalOsCommonBoundaryClaimsCandidate.Claim.PublicProductBlocked));
        Assert.IsTrue(missingKnownClaim.IsFailClosed(NodalOsCommonBoundaryClaimsCandidate.Claim.PublicProductBlocked));
        Assert.IsFalse(missingKnownClaim.IsSupported(unsupportedFutureClaim));
        Assert.AreEqual(NodalOsCommonBoundaryClaimsCandidate.ClaimState.Denied, missingKnownClaim.StateFor(unsupportedFutureClaim));
        Assert.IsTrue(missingKnownClaim.IsFailClosed(unsupportedFutureClaim));
    }

    [TestMethod]
    [TestCategory("NoDoubleTruth")]
    [TestCategory("PublicProductBlock")]
    [TestCategory("ProductionRouteBlock")]
    [TestCategory("LatestPointerBlock")]
    [TestCategory("ReadPrecedenceBlock")]
    [TestCategory("ProductAuthorityBlock")]
    [TestCategory("CommandExecutionBlock")]
    [TestCategory("ReleaseCommercialBlock")]
    public void D5_D1D2AndD4AgreeThatStaticGuardHardBlocksRemainBlocked()
    {
        var d1 = NodalOsCommonBoundaryClaims.CurrentLocalDesignOnlyNoGo();
        var d2 = NodalOsCommonBoundaryMappingDesignOnlyAdapter.Map(
            new NodalOsCommonBoundaryMappingRequest(NodalOsCommonBoundarySourceConcept.StaticGuardCatalogHardBlock));
        var d4 = NodalOsCommonBoundaryClaimsCandidate.DefaultBlocked();

        foreach (var capability in d2.MappedCapabilities)
        {
            Assert.IsTrue(d1.IsBlocked(capability), capability.ToString());
            Assert.IsTrue(d4.IsFailClosed(ToCandidateClaim(capability)), capability.ToString());
        }

        Assert.IsFalse(d2.OverrideAllowed);
        Assert.IsFalse(d4.AllowsRuntimeProductOrAuthority());
    }

    [TestMethod]
    [TestCategory("NoDoubleTruth")]
    public void D5_D2MapperRemainsTestOnlyDesignOnlyAndD4DoesNotSupersedeIt()
    {
        var root = RepoRoot();
        var d2Path = Path.Combine(root, "tests", "OneBrain.Safety.Tests", "NodalOsCommonBoundaryMappingDesignOnlyAdapter.cs");
        var sourceReferences = SourceFilesExceptCandidate(root)
            .Where(path => File.ReadAllText(path).Contains("NodalOsCommonBoundaryMappingDesignOnlyAdapter", StringComparison.Ordinal))
            .ToArray();
        var d2 = NodalOsCommonBoundaryMappingDesignOnlyAdapter.Map(
            new NodalOsCommonBoundaryMappingRequest(NodalOsCommonBoundarySourceConcept.PublicProductHardBlock));
        var d4 = NodalOsCommonBoundaryClaimsCandidate.DefaultBlocked();

        Assert.IsTrue(File.Exists(d2Path));
        CollectionAssert.AreEqual(Array.Empty<string>(), sourceReferences);
        Assert.IsFalse(d2.OverrideAllowed);
        Assert.IsFalse(d2.Envelope.HasRuntimeOrProductSurface());
        Assert.IsFalse(d4.ExistingHardBlockAuthorityReplaced);
    }

    [TestMethod]
    [TestCategory("NoDoubleTruth")]
    public void D5_D1D2RemainOutsideSourceAndRuntimeUntilFutureAuthorization()
    {
        var root = RepoRoot();
        var source = string.Join(
            Environment.NewLine,
            SourceFilesExceptCandidate(root).OrderBy(path => path, StringComparer.Ordinal).Select(File.ReadAllText));

        Assert.IsFalse(source.Contains("NodalOsCommonBoundaryClaims.CurrentLocalDesignOnlyNoGo", StringComparison.Ordinal));
        Assert.IsFalse(source.Contains("NodalOsCommonSafetyEnvelope", StringComparison.Ordinal));
        Assert.IsFalse(source.Contains("NodalOsCommonBoundaryMappingDesignOnlyAdapter", StringComparison.Ordinal));
        Assert.IsFalse(source.Contains("NodalOsCommonBoundarySourceConcept", StringComparison.Ordinal));
    }

    [TestMethod]
    [TestCategory("NoDoubleTruth")]
    public void D5_NonAuthoritativeOrUnknownD2MappingsFailClosedBeforeD4Comparison()
    {
        var d4 = NodalOsCommonBoundaryClaimsCandidate.DefaultBlocked();
        var results = new[]
        {
            NodalOsCommonBoundaryMappingDesignOnlyAdapter.Map(null),
            NodalOsCommonBoundaryMappingDesignOnlyAdapter.Map(new NodalOsCommonBoundaryMappingRequest(
                NodalOsCommonBoundarySourceConcept.UnsupportedSourceConcept)),
            NodalOsCommonBoundaryMappingDesignOnlyAdapter.Map(new NodalOsCommonBoundaryMappingRequest(
                NodalOsCommonBoundarySourceConcept.PublicProductHardBlock,
                ExistingHardBlockAuthoritative: false))
        };

        foreach (var result in results)
        {
            Assert.IsTrue(result.IsFailClosed, result.SourceConcept.ToString());
            Assert.IsFalse(result.OverrideAllowed, result.SourceConcept.ToString());
            Assert.IsFalse(result.Envelope.HasRuntimeOrProductSurface(), result.SourceConcept.ToString());

            foreach (var capability in result.MappedCapabilities)
            {
                Assert.IsTrue(d4.IsFailClosed(ToCandidateClaim(capability)), capability.ToString());
            }
        }
    }

    [TestMethod]
    [TestCategory("NoAuthority")]
    [TestCategory("NoDoubleTruth")]
    public void D5_NoContractLayerCanIndependentlyCreateAuthority()
    {
        var d1Envelope = NodalOsCommonSafetyEnvelope.D1DescriptiveOnly(
            NodalOsCommonWriterMode.Disabled,
            NodalOsCommonEvidenceRole.Auxiliary);
        var d2Result = NodalOsCommonBoundaryMappingDesignOnlyAdapter.Map(
            new NodalOsCommonBoundaryMappingRequest(NodalOsCommonBoundarySourceConcept.ProductAuthorityDenied));
        var d4Candidate = NodalOsCommonBoundaryClaimsCandidate.DefaultBlocked();

        Assert.IsFalse(d1Envelope.HasRuntimeOrProductSurface());
        Assert.IsFalse(d2Result.Envelope.HasRuntimeOrProductSurface());
        Assert.IsFalse(d2Result.OverrideAllowed);
        Assert.IsFalse(d4Candidate.CanOverrideExistingHardBlock(NodalOsCommonBoundaryClaimsCandidate.Claim.ProductAuthorityBlocked));
        Assert.IsFalse(d4Candidate.AllowsRuntimeProductOrAuthority());
    }

    private static IReadOnlyList<NodalOsCommonBoundaryClaimsCandidate> UnsafeFlagVariants()
    {
        var safe = NodalOsCommonBoundaryClaimsCandidate.DefaultBlocked();

        return
        [
            safe with { ParallelOnly = false },
            safe with { NonAuthoritative = false },
            safe with { ExistingHardBlockAuthorityReplaced = true },
            safe with { RuntimeWired = true },
            safe with { ServiceRegistered = true },
            safe with { RouteRegistered = true },
            safe with { CommandHandlerRegistered = true },
            safe with { RuntimeProductEnablementAllowed = true },
            safe with { ProductAuthorityAllowed = true },
            safe with { CiEnforcementClaimed = true },
            safe with { ReleaseCommercialReady = true }
        ];
    }

    private static string[] ReferencesUnderExistingRoots(params string[] relativeRoots)
    {
        var root = RepoRoot();

        return relativeRoots
            .Select(relativeRoot => Path.Combine(root, relativeRoot.Replace('/', Path.DirectorySeparatorChar)))
            .Where(Directory.Exists)
            .SelectMany(path => Directory.EnumerateFiles(path, "*.cs", SearchOption.AllDirectories))
            .Where(ContainsCandidate)
            .OrderBy(path => path, StringComparer.Ordinal)
            .ToArray();
    }

    private static string[] CandidateReferencesInExistingCiFiles()
    {
        var root = RepoRoot();
        var files = new[]
        {
            Path.Combine(root, "azure-pipelines.yml")
        }
            .Where(File.Exists)
            .Concat(Directory.Exists(Path.Combine(root, ".github"))
                ? Directory.EnumerateFiles(Path.Combine(root, ".github"), "*", SearchOption.AllDirectories)
                : [])
            .Where(path => ContentExtensions.Contains(Path.GetExtension(path)))
            .Where(ContainsCandidate)
            .OrderBy(path => path, StringComparer.Ordinal)
            .ToArray();

        return files;
    }

    private static IEnumerable<string> CandidateReferenceFiles(string root) =>
        CandidateReferenceRoots(root)
            .SelectMany(path => Directory.Exists(path)
                ? Directory.EnumerateFiles(path, "*", SearchOption.AllDirectories)
                : File.Exists(path)
                    ? [path]
                    : [])
            .Where(path => !IsIgnoredRepositoryOutput(path))
            .Where(path => ContentExtensions.Contains(Path.GetExtension(path)))
            .Where(ContainsCandidate)
            .OrderBy(path => path, StringComparer.Ordinal);

    private static IEnumerable<string> CandidateReferenceRoots(string root) =>
    [
        Path.Combine(root, "src"),
        Path.Combine(root, "tests"),
        Path.Combine(root, "docs"),
        Path.Combine(root, ".github"),
        Path.Combine(root, "azure-pipelines.yml")
    ];

    private static bool IsAllowedCandidateReference(string root, string path)
    {
        var relative = Path.GetRelativePath(root, path).Replace('\\', '/');

        return relative == "src/OneBrain.Core/Approval/NodalOsCommonBoundaryClaimsCandidate.cs"
            || relative == "src/OneBrain.Core/Approval/ReentryDecisionPacketReadOnly.cs"
            || relative == "src/OneBrain.Core/Approval/ApprovalExecutionDesignOnlyProtected.cs"
            || relative.StartsWith("tests/OneBrain.Safety.Tests/", StringComparison.Ordinal)
            || relative.StartsWith("docs/", StringComparison.Ordinal);
    }

    private static bool IsRouteDiServiceOrProductLedgerRuntimePath(string path)
    {
        var normalized = path.Replace('\\', '/');

        return normalized.Contains("/OneBrain.Pilot/", StringComparison.Ordinal)
            || normalized.Contains("/OneBrain.Cli/", StringComparison.Ordinal)
            || normalized.Contains("/ProductLedger", StringComparison.Ordinal)
            || normalized.EndsWith("/Program.cs", StringComparison.OrdinalIgnoreCase)
            || File.ReadAllText(path).Contains("IService" + "Collection", StringComparison.Ordinal)
            || File.ReadAllText(path).Contains("Add" + "Singleton", StringComparison.Ordinal)
            || File.ReadAllText(path).Contains("Add" + "Scoped", StringComparison.Ordinal)
            || File.ReadAllText(path).Contains("Add" + "Transient", StringComparison.Ordinal)
            || File.ReadAllText(path).Contains("Map" + "Get", StringComparison.Ordinal)
            || File.ReadAllText(path).Contains("Map" + "Post", StringComparison.Ordinal);
    }

    private static IEnumerable<string> SourceFilesExceptCandidate(string root) =>
        Directory.EnumerateFiles(Path.Combine(root, "src"), "*.cs", SearchOption.AllDirectories)
            .Where(path => !path.EndsWith(
                $"{Path.DirectorySeparatorChar}NodalOsCommonBoundaryClaimsCandidate.cs",
                StringComparison.Ordinal));

    private static bool ContainsCandidate(string path) =>
        File.ReadAllText(path).Contains(nameof(NodalOsCommonBoundaryClaimsCandidate), StringComparison.Ordinal);

    private static bool IsIgnoredRepositoryOutput(string path)
    {
        var normalized = path.Replace('\\', '/');

        return normalized.Contains("/.git/", StringComparison.Ordinal)
            || normalized.Contains("/bin/", StringComparison.Ordinal)
            || normalized.Contains("/obj/", StringComparison.Ordinal)
            || normalized.Contains("/TestResults/", StringComparison.Ordinal);
    }

    private static readonly string[] ContentExtensions =
    [
        ".cs",
        ".md",
        ".json",
        ".yml",
        ".yaml",
        ".csproj",
        ".slnx",
        ".props",
        ".targets"
    ];

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
