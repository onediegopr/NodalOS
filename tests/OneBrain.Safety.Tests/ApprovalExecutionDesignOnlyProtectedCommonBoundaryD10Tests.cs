using System.Reflection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using OneBrain.Core.Approval;

namespace OneBrain.Safety.Tests;

[TestClass]
[TestCategory("NodalOsTier1Safety")]
[TestCategory("CommonContracts")]
[TestCategory("SourceCandidate")]
[TestCategory("NoRuntimeWiring")]
[TestCategory("NoAuthority")]
[TestCategory("NoDoubleTruth")]
[TestCategory("PostReplacementAudit")]
[TestCategory("ApprovalExecution")]
[TestCategory("DesignOnly")]
public sealed class ApprovalExecutionDesignOnlyProtectedCommonBoundaryD10Tests
{
    private const string CandidateRelativePath = "src/OneBrain.Core/Approval/NodalOsCommonBoundaryClaimsCandidate.cs";
    private const string D7TargetRelativePath = "src/OneBrain.Core/Approval/ReentryDecisionPacketReadOnly.cs";
    private const string D10TargetRelativePath = "src/OneBrain.Core/Approval/ApprovalExecutionDesignOnlyProtected.cs";

    [TestMethod]
    public void ApprovalExecutionDesignOnlyProtectedPreservesDesignOnlyState()
    {
        var spec = ApprovalExecutionDesignOnlyProtectedPresenter.CreateFixture();

        Assert.AreEqual(ApprovalExecutionDesignStatus.DesignOnly, spec.Status);
        Assert.IsTrue(spec.ReadOnly);
        Assert.IsTrue(spec.DesignOnly);
        Assert.IsTrue(spec.PreviewOnly);
        Assert.IsTrue(spec.AntiCapabilityProof.Passes);
        Assert.IsTrue(spec.Readiness.BlocksRealExecution);
        Assert.IsFalse(spec.HasRealExecution);
        Assert.IsFalse(spec.HasStateMutation);
        Assert.IsFalse(spec.HasRuntimeLive);
        Assert.IsFalse(spec.HasPhysicalExport);
        Assert.IsFalse(spec.HasProductActions);
        CollectionAssert.Contains(
            Enum.GetNames<ApprovalExecutionBlockedReason>(),
            nameof(ApprovalExecutionBlockedReason.ProviderCloudNetworkNotAuthorized));
        CollectionAssert.DoesNotContain(
            Enum.GetNames<ApprovalExecutionBlockedReason>(),
            "ProviderCloudNotAuthorized");
    }

    [TestMethod]
    [TestCategory("PublicProductBlock")]
    public void ApprovalExecutionDesignOnlyProtectedPreservesPublicProductBlockedState() =>
        AssertBoundaryClaim(
            NodalOsCommonBoundaryClaimsCandidate.Claim.PublicProductBlocked,
            NodalOsCommonBoundaryClaimsCandidate.ClaimState.Blocked);

    [TestMethod]
    [TestCategory("ProductionRouteBlock")]
    public void ApprovalExecutionDesignOnlyProtectedPreservesProductionRouteBlockedState() =>
        AssertBoundaryClaim(
            NodalOsCommonBoundaryClaimsCandidate.Claim.ProductionRouteBlocked,
            NodalOsCommonBoundaryClaimsCandidate.ClaimState.Blocked);

    [TestMethod]
    [TestCategory("LatestPointerBlock")]
    public void ApprovalExecutionDesignOnlyProtectedPreservesLatestPointerDisabledState() =>
        AssertBoundaryClaim(
            NodalOsCommonBoundaryClaimsCandidate.Claim.LatestPointerDisabled,
            NodalOsCommonBoundaryClaimsCandidate.ClaimState.Disabled);

    [TestMethod]
    [TestCategory("ReadPrecedenceBlock")]
    public void ApprovalExecutionDesignOnlyProtectedPreservesReadPrecedenceDisabledState() =>
        AssertBoundaryClaim(
            NodalOsCommonBoundaryClaimsCandidate.Claim.ReadPrecedenceDisabled,
            NodalOsCommonBoundaryClaimsCandidate.ClaimState.Disabled);

    [TestMethod]
    [TestCategory("ProductAuthorityBlock")]
    public void ApprovalExecutionDesignOnlyProtectedPreservesProductAuthorityBlockedState() =>
        AssertBoundaryClaim(
            NodalOsCommonBoundaryClaimsCandidate.Claim.ProductAuthorityBlocked,
            NodalOsCommonBoundaryClaimsCandidate.ClaimState.Blocked);

    [TestMethod]
    [TestCategory("CommandExecutionBlock")]
    public void ApprovalExecutionDesignOnlyProtectedPreservesCommandExecutionDeniedState() =>
        AssertBoundaryClaim(
            NodalOsCommonBoundaryClaimsCandidate.Claim.CommandExecutionDenied,
            NodalOsCommonBoundaryClaimsCandidate.ClaimState.Denied);

    [TestMethod]
    [TestCategory("ReleaseCommercialBlock")]
    public void ApprovalExecutionDesignOnlyProtectedPreservesReleaseCommercialNoGoState() =>
        AssertBoundaryClaim(
            NodalOsCommonBoundaryClaimsCandidate.Claim.ReleaseCommercialNoGo,
            NodalOsCommonBoundaryClaimsCandidate.ClaimState.NoGo);

    [TestMethod]
    [TestCategory("PublicProductBlock")]
    [TestCategory("ProductionRouteBlock")]
    [TestCategory("CommandExecutionBlock")]
    [TestCategory("ReleaseCommercialBlock")]
    public void ApprovalExecutionDesignOnlyProtectedUsesCommonBoundaryCandidateWithoutCreatingAuthority()
    {
        var spec = ApprovalExecutionDesignOnlyProtectedPresenter.CreateFixture();
        var source = D10TargetSource();
        var candidate = NodalOsCommonBoundaryClaimsCandidate.DefaultBlocked();

        Assert.IsTrue(spec.AntiCapabilityProof.Passes);
        Assert.IsTrue(CommonBoundaryClaimsRemainFailClosed(candidate));
        Assert.IsFalse(candidate.AllowsRuntimeProductOrAuthority());
        StringAssert.Contains(source, "NodalOsCommonBoundaryClaimsCandidate.DefaultBlocked()");
        StringAssert.Contains(source, "CommonBoundaryClaimsRemainFailClosed");
    }

    [TestMethod]
    [TestCategory("PublicProductBlock")]
    [TestCategory("ProductionRouteBlock")]
    [TestCategory("CommandExecutionBlock")]
    [TestCategory("ReleaseCommercialBlock")]
    public void ApprovalExecutionDesignOnlyProtectedDoesNotCreateRuntimeProductEnablement()
    {
        var spec = ApprovalExecutionDesignOnlyProtectedPresenter.CreateFixture();
        var proof = spec.AntiCapabilityProof;
        var source = D10TargetSource();

        Assert.IsTrue(proof.Passes);
        Assert.IsFalse(spec.Readiness.ProductiveWriterPolicyPathAvailable);
        Assert.IsFalse(spec.Readiness.CommandHandlerAvailable);
        Assert.IsFalse(spec.Readiness.ProductServiceRegistered);
        Assert.IsFalse(spec.Readiness.ReleaseCommercialReady);
        Assert.IsFalse(spec.HasProductActions);
        Assert.IsFalse(spec.HasRuntimeLive);
        Assert.IsTrue(proof.NoProductiveWriterPolicyPath);
        AssertSourceHasNoForbiddenRuntimePatterns(source);
    }

    [TestMethod]
    [TestCategory("CommandExecutionBlock")]
    public void ApprovalExecutionDesignOnlyProtectedDoesNotCreateShellSubprocessOrCommandCapability()
    {
        var spec = ApprovalExecutionDesignOnlyProtectedPresenter.CreateFixture();
        var proof = spec.AntiCapabilityProof;
        var source = D10TargetSource();

        Assert.IsTrue(proof.Passes);
        Assert.IsTrue(proof.NoCommandHandler);
        Assert.IsFalse(spec.Readiness.CommandHandlerAvailable);
        Assert.IsFalse(spec.Previews.Any(preview => preview.ExecutesApproval));
        Assert.AreEqual(
            NodalOsCommonBoundaryClaimsCandidate.ClaimState.Denied,
            NodalOsCommonBoundaryClaimsCandidate.DefaultBlocked().StateFor(
                NodalOsCommonBoundaryClaimsCandidate.Claim.CommandExecutionDenied));
        Assert.AreEqual(
            NodalOsCommonBoundaryClaimsCandidate.ClaimState.Denied,
            NodalOsCommonBoundaryClaimsCandidate.DefaultBlocked().StateFor(
                NodalOsCommonBoundaryClaimsCandidate.Claim.ShellSubprocessDenied));
        AssertSourceHasNoForbiddenRuntimePatterns(source);
    }

    [TestMethod]
    public void ApprovalExecutionDesignOnlyProtectedUnknownOrAmbiguousStatesRemainFailClosed()
    {
        var missingKnownClaim = NodalOsCommonBoundaryClaimsCandidate.DefaultBlocked() with
        {
            Claims = new Dictionary<NodalOsCommonBoundaryClaimsCandidate.Claim, NodalOsCommonBoundaryClaimsCandidate.ClaimState>()
        };
        var unknown = (NodalOsCommonBoundaryClaimsCandidate.Claim)999;

        Assert.IsFalse(CommonBoundaryClaimsRemainFailClosed(missingKnownClaim));
        Assert.IsTrue(CommonBoundaryClaimRemainsFailClosed(
            missingKnownClaim,
            NodalOsCommonBoundaryClaimsCandidate.Claim.PublicProductBlocked,
            NodalOsCommonBoundaryClaimsCandidate.ClaimState.Denied));
        Assert.IsTrue(CommonBoundaryClaimRemainsFailClosed(
            NodalOsCommonBoundaryClaimsCandidate.DefaultBlocked(),
            unknown,
            NodalOsCommonBoundaryClaimsCandidate.ClaimState.Denied));
    }

    [TestMethod]
    [TestCategory("StaticGuard")]
    public void D10DoesNotBroadenCandidateRuntimeReferences()
    {
        var root = RepoRoot();
        var sourceReferences = Directory.EnumerateFiles(Path.Combine(root, "src"), "*.cs", SearchOption.AllDirectories)
            .Where(path => File.ReadAllText(path).Contains(nameof(NodalOsCommonBoundaryClaimsCandidate), StringComparison.Ordinal))
            .Select(path => Relative(root, path))
            .OrderBy(path => path, StringComparer.Ordinal)
            .ToArray();
        var expected = new[]
        {
            CandidateRelativePath,
            D10TargetRelativePath,
            D7TargetRelativePath
        }.OrderBy(path => path, StringComparer.Ordinal).ToArray();
        var runtimeReferences = ExistingRuntimeRoots(root)
            .SelectMany(path => Directory.EnumerateFiles(path, "*.cs", SearchOption.AllDirectories))
            .Where(path => File.ReadAllText(path).Contains(nameof(NodalOsCommonBoundaryClaimsCandidate), StringComparison.Ordinal))
            .ToArray();

        CollectionAssert.AreEqual(expected, sourceReferences);
        CollectionAssert.AreEqual(Array.Empty<string>(), runtimeReferences);
    }

    [TestMethod]
    [TestCategory("NoAuthority")]
    [TestCategory("NoDoubleTruth")]
    public void D10DoesNotWeakenNoAuthorityNoDoubleTruthOrPostReplacementGuards()
    {
        var d1 = NodalOsCommonBoundaryClaims.CurrentLocalDesignOnlyNoGo();
        var d2 = NodalOsCommonBoundaryMappingDesignOnlyAdapter.Map(
            new NodalOsCommonBoundaryMappingRequest(NodalOsCommonBoundarySourceConcept.StaticGuardCatalogHardBlock));
        var d4 = NodalOsCommonBoundaryClaimsCandidate.DefaultBlocked();
        var d7 = ReentryDecisionPacketReadOnlyPresenter.CreateFixture();
        var d10 = ApprovalExecutionDesignOnlyProtectedPresenter.CreateFixture();

        Assert.IsTrue(d7.PassesSafetyProof);
        Assert.IsTrue(d10.AntiCapabilityProof.Passes);
        Assert.IsTrue(d2.ExistingHardBlockAuthoritative);
        Assert.IsFalse(d2.OverrideAllowed);
        Assert.IsFalse(d4.ExistingHardBlockAuthorityReplaced);
        Assert.IsFalse(d4.AllowsRuntimeProductOrAuthority());
        Assert.IsTrue(CommonBoundaryClaimsRemainFailClosed(d4));

        foreach (var capability in d2.MappedCapabilities)
        {
            Assert.IsTrue(d1.IsBlocked(capability), capability.ToString());
            Assert.IsFalse(d4.CanOverrideExistingHardBlock(ToCandidateClaim(capability)), capability.ToString());
        }
    }

    [TestMethod]
    [TestCategory("CommandExecutionBlock")]
    public void D10CommandVocabularyExceptionIsExactToApprovalExecutionDesignOnlyProtected()
    {
        var root = RepoRoot();
        var d10TargetPath = Path.Combine(root, D10TargetRelativePath.Replace('/', Path.DirectorySeparatorChar));
        var hypotheticalSiblingPath = Path.Combine(
            Path.GetDirectoryName(d10TargetPath)!,
            "ApprovalExecutionDesignOnlyProtectedCommandProbe.cs");
        var unexpected = Directory.EnumerateFiles(Path.Combine(root, "src"), "*.cs", SearchOption.AllDirectories)
            .Where(path => !IsCandidate(root, path) && !IsExactD7Target(root, path) && !IsExactD10Target(root, path))
            .Where(path => File.ReadAllText(path).Contains(nameof(NodalOsCommonBoundaryClaimsCandidate), StringComparison.Ordinal))
            .Where(path => File.ReadAllText(path).Contains("Command", StringComparison.OrdinalIgnoreCase))
            .Select(path => Relative(root, path))
            .OrderBy(path => path, StringComparer.Ordinal)
            .ToArray();

        Assert.IsTrue(IsExactD10Target(root, d10TargetPath));
        Assert.IsFalse(IsExactD10Target(root, hypotheticalSiblingPath));
        Assert.IsFalse(IsExactD10Target(root, Path.Combine(root, "src", "OneBrain.Pilot", "ApprovalExecutionDesignOnlyProtected.cs")));
        CollectionAssert.AreEqual(Array.Empty<string>(), unexpected);
        AssertSourceHasNoForbiddenRuntimePatterns(D10TargetSource());
    }

    private static void AssertBoundaryClaim(
        NodalOsCommonBoundaryClaimsCandidate.Claim claim,
        NodalOsCommonBoundaryClaimsCandidate.ClaimState expectedState)
    {
        var spec = ApprovalExecutionDesignOnlyProtectedPresenter.CreateFixture();
        var candidate = NodalOsCommonBoundaryClaimsCandidate.DefaultBlocked();

        Assert.IsTrue(spec.AntiCapabilityProof.Passes);
        Assert.AreEqual(expectedState, candidate.StateFor(claim));
        Assert.IsTrue(CommonBoundaryClaimRemainsFailClosed(candidate, claim, expectedState));
        Assert.IsFalse(candidate.CanOverrideExistingHardBlock(claim));
    }

    private static bool CommonBoundaryClaimsRemainFailClosed(NodalOsCommonBoundaryClaimsCandidate candidate)
    {
        var method = typeof(ApprovalExecutionAntiCapabilityProof).GetMethod(
            "CommonBoundaryClaimsRemainFailClosed",
            BindingFlags.NonPublic | BindingFlags.Static,
            binder: null,
            types: [typeof(NodalOsCommonBoundaryClaimsCandidate)],
            modifiers: null);

        Assert.IsNotNull(method);
        return (bool)method!.Invoke(null, [candidate])!;
    }

    private static bool CommonBoundaryClaimRemainsFailClosed(
        NodalOsCommonBoundaryClaimsCandidate candidate,
        NodalOsCommonBoundaryClaimsCandidate.Claim claim,
        NodalOsCommonBoundaryClaimsCandidate.ClaimState expectedState)
    {
        var method = typeof(ApprovalExecutionAntiCapabilityProof).GetMethod(
            "CommonBoundaryClaimRemainsFailClosed",
            BindingFlags.NonPublic | BindingFlags.Static,
            binder: null,
            types:
            [
                typeof(NodalOsCommonBoundaryClaimsCandidate),
                typeof(NodalOsCommonBoundaryClaimsCandidate.Claim),
                typeof(NodalOsCommonBoundaryClaimsCandidate.ClaimState)
            ],
            modifiers: null);

        Assert.IsNotNull(method);
        return (bool)method!.Invoke(null, [candidate, claim, expectedState])!;
    }

    private static void AssertSourceHasNoForbiddenRuntimePatterns(string source)
    {
        foreach (var forbidden in new[]
        {
            "IService" + "Collection",
            "Add" + "Singleton",
            "Add" + "Scoped",
            "Add" + "Transient",
            "ICommand" + "Handler",
            "Map" + "Get",
            "Map" + "Post",
            "Endpoint" + "RouteBuilder",
            "Process" + ".Start",
            "Shell" + "Execute",
            "Http" + "Client",
            "Db" + "Context",
            "Migration" + "Builder",
            "Kms" + "Client",
            "Worm" + "Store",
            "File" + ".Write",
            "File" + ".Append",
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

    private static IEnumerable<string> ExistingRuntimeRoots(string root) =>
        new[]
        {
            "src/OneBrain.Pilot",
            "src/OneBrain.Cli",
            "src/OneBrain.Actions",
            "src/OneBrain.Core/Execution",
            "src/OneBrain.AgentOperations.Core",
            "src/OneBrain.AgentOperations.Adapters.Browser",
            "src/OneBrain.BrowserRuntime",
            "src/OneBrain.BrowserExecutor.Cdp",
            "src/OneBrain.BrowserPerception"
        }
            .Select(relative => Path.Combine(root, relative.Replace('/', Path.DirectorySeparatorChar)))
            .Where(Directory.Exists);

    private static bool IsCandidate(string root, string path) =>
        Relative(root, path) == CandidateRelativePath;

    private static bool IsExactD7Target(string root, string path) =>
        Relative(root, path) == D7TargetRelativePath;

    private static bool IsExactD10Target(string root, string path) =>
        Relative(root, path) == D10TargetRelativePath;

    private static string D10TargetSource() =>
        File.ReadAllText(Path.Combine(RepoRoot(), D10TargetRelativePath.Replace('/', Path.DirectorySeparatorChar)));

    private static string Relative(string root, string path) =>
        Path.GetRelativePath(root, path).Replace('\\', '/');

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
