using System.Reflection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using OneBrain.Core.Approval;
using static OneBrain.Safety.Tests.NodalOsCommonBoundaryMappingDesignOnlyAdapter;

namespace OneBrain.Safety.Tests;

[TestClass]
[TestCategory("NodalOsTier1Safety")]
[TestCategory("CommonContracts")]
[TestCategory("SourceCandidate")]
[TestCategory("NoRuntimeWiring")]
[TestCategory("NoAuthority")]
[TestCategory("NoDoubleTruth")]
public sealed class ReentryDecisionPacketReadOnlyCommonBoundaryD7Tests
{
    [TestMethod]
    [TestCategory("PublicProductBlock")]
    public void ReentryDecisionPacketReadOnlyPreservesPublicProductBlockedState() =>
        AssertBoundaryClaim(
            NodalOsCommonBoundaryClaimsCandidate.Claim.PublicProductBlocked,
            NodalOsCommonBoundaryClaimsCandidate.ClaimState.Blocked);

    [TestMethod]
    [TestCategory("ProductionRouteBlock")]
    public void ReentryDecisionPacketReadOnlyPreservesProductionRouteBlockedState() =>
        AssertBoundaryClaim(
            NodalOsCommonBoundaryClaimsCandidate.Claim.ProductionRouteBlocked,
            NodalOsCommonBoundaryClaimsCandidate.ClaimState.Blocked);

    [TestMethod]
    [TestCategory("LatestPointerBlock")]
    public void ReentryDecisionPacketReadOnlyPreservesLatestPointerDisabledState() =>
        AssertBoundaryClaim(
            NodalOsCommonBoundaryClaimsCandidate.Claim.LatestPointerDisabled,
            NodalOsCommonBoundaryClaimsCandidate.ClaimState.Disabled);

    [TestMethod]
    [TestCategory("ReadPrecedenceBlock")]
    public void ReentryDecisionPacketReadOnlyPreservesReadPrecedenceDisabledState() =>
        AssertBoundaryClaim(
            NodalOsCommonBoundaryClaimsCandidate.Claim.ReadPrecedenceDisabled,
            NodalOsCommonBoundaryClaimsCandidate.ClaimState.Disabled);

    [TestMethod]
    [TestCategory("ProductAuthorityBlock")]
    public void ReentryDecisionPacketReadOnlyPreservesProductAuthorityBlockedState() =>
        AssertBoundaryClaim(
            NodalOsCommonBoundaryClaimsCandidate.Claim.ProductAuthorityBlocked,
            NodalOsCommonBoundaryClaimsCandidate.ClaimState.Blocked);

    [TestMethod]
    [TestCategory("CommandExecutionBlock")]
    public void ReentryDecisionPacketReadOnlyPreservesCommandExecutionDeniedState() =>
        AssertBoundaryClaim(
            NodalOsCommonBoundaryClaimsCandidate.Claim.CommandExecutionDenied,
            NodalOsCommonBoundaryClaimsCandidate.ClaimState.Denied);

    [TestMethod]
    [TestCategory("ReleaseCommercialBlock")]
    public void ReentryDecisionPacketReadOnlyPreservesReleaseCommercialNoGoState() =>
        AssertBoundaryClaim(
            NodalOsCommonBoundaryClaimsCandidate.Claim.ReleaseCommercialNoGo,
            NodalOsCommonBoundaryClaimsCandidate.ClaimState.NoGo);

    [TestMethod]
    [TestCategory("PublicProductBlock")]
    [TestCategory("ProductionRouteBlock")]
    [TestCategory("CommandExecutionBlock")]
    [TestCategory("ReleaseCommercialBlock")]
    public void ReentryDecisionPacketReadOnlyUsesCommonBoundaryCandidateWithoutCreatingAuthority()
    {
        var packet = ReentryDecisionPacketReadOnlyPresenter.CreateFixture();
        var source = ReentrySource();
        var candidate = NodalOsCommonBoundaryClaimsCandidate.DefaultBlocked();

        Assert.IsTrue(packet.PassesSafetyProof);
        Assert.IsTrue(CommonBoundaryClaimsRemainFailClosed(candidate));
        Assert.IsFalse(candidate.AllowsRuntimeProductOrAuthority());
        StringAssert.Contains(source, "NodalOsCommonBoundaryClaimsCandidate.DefaultBlocked()");
        StringAssert.Contains(source, "CommonBoundaryClaimsRemainFailClosed");
    }

    [TestMethod]
    [TestCategory("NoDoubleTruth")]
    public void ReentryDecisionPacketReadOnlyUsesCandidateExpectedClosedStatesWithoutLocalDuplicateTable()
    {
        var source = ReentrySource();

        StringAssert.Contains(source, "NodalOsCommonBoundaryClaimsCandidate.ExpectedClosedStates");
        Assert.IsFalse(source.Contains("ExpectedFailClosedClaims", StringComparison.Ordinal));
        Assert.IsTrue(CommonBoundaryClaimsRemainFailClosed(NodalOsCommonBoundaryClaimsCandidate.DefaultBlocked()));
    }

    [TestMethod]
    [TestCategory("PublicProductBlock")]
    [TestCategory("ProductionRouteBlock")]
    [TestCategory("CommandExecutionBlock")]
    [TestCategory("ReleaseCommercialBlock")]
    public void ReentryDecisionPacketReadOnlyDoesNotCreateRuntimeProductEnablement()
    {
        var packet = ReentryDecisionPacketReadOnlyPresenter.CreateFixture();
        var source = ReentrySource();

        Assert.IsTrue(packet.PassesSafetyProof);
        Assert.IsTrue(packet.CapabilityStatus.NoRealCapabilities);
        Assert.IsTrue(packet.NoSideEffectProof.Counts.AllZero);
        Assert.AreEqual(0, packet.ProductActionCount);
        Assert.AreEqual(0, packet.StateMutationCount);
        Assert.AreEqual(0, packet.ServiceRegistrationCount);
        Assert.AreEqual(0, packet.CommandHandlerCount);
        Assert.AreEqual(0, packet.RuntimeInvocationCount);
        Assert.AreEqual("NO-GO", packet.ReleaseCommercialStatus);
        AssertSourceHasNoForbiddenRuntimePatterns(source);
    }

    [TestMethod]
    public void ReentryDecisionPacketReadOnlyUnknownOrAmbiguousStatesRemainFailClosed()
    {
        var missingKnownClaim = NodalOsCommonBoundaryClaimsCandidate.DefaultBlocked() with
        {
            Claims = new Dictionary<NodalOsCommonBoundaryClaimsCandidate.Claim, NodalOsCommonBoundaryClaimsCandidate.ClaimState>()
        };
        var unknown = (NodalOsCommonBoundaryClaimsCandidate.Claim)999;

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
    [TestCategory("NoAuthority")]
    [TestCategory("NoDoubleTruth")]
    public void ReentryDecisionPacketReadOnlyRejectsNullCommonBoundaryCandidateFailClosed()
    {
        Assert.IsFalse(CommonBoundaryClaimsRemainFailClosed(null));
    }

    [TestMethod]
    [TestCategory("StaticGuard")]
    public void D7DoesNotBroadenCandidateRuntimeReferences()
    {
        var root = RepoRoot();
        var sourceReferences = Directory.EnumerateFiles(Path.Combine(root, "src"), "*.cs", SearchOption.AllDirectories)
            .Where(path => File.ReadAllText(path).Contains(nameof(NodalOsCommonBoundaryClaimsCandidate), StringComparison.Ordinal))
            .Select(path => Path.GetRelativePath(root, path).Replace('\\', '/'))
            .OrderBy(path => path, StringComparer.Ordinal)
            .ToArray();
        var expected = new[]
        {
            "src/OneBrain.Core/Approval/NodalOsCommonBoundaryClaimsCandidate.cs",
            "src/OneBrain.Core/Approval/ApprovalExecutionDesignOnlyProtected.cs",
            "src/OneBrain.Core/Approval/ReentryDecisionPacketReadOnly.cs"
        }.OrderBy(path => path, StringComparer.Ordinal).ToArray();
        var runtimeReferences = ExistingRuntimeRoots(root)
            .SelectMany(path => Directory.EnumerateFiles(path, "*.cs", SearchOption.AllDirectories))
            .Where(path => File.ReadAllText(path).Contains(nameof(NodalOsCommonBoundaryClaimsCandidate), StringComparison.Ordinal))
            .ToArray();

        CollectionAssert.AreEqual(expected, sourceReferences);
        CollectionAssert.AreEqual(Array.Empty<string>(), runtimeReferences);
    }

    [TestMethod]
    [TestCategory("PublicProductBlock")]
    [TestCategory("ProductionRouteBlock")]
    [TestCategory("CommandExecutionBlock")]
    [TestCategory("ReleaseCommercialBlock")]
    public void D7DoesNotWeakenNoAuthorityOrNoDoubleTruthGuards()
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
        Assert.IsFalse(d2.Envelope.HasRuntimeOrProductSurface());
        Assert.IsFalse(d4.AllowsRuntimeProductOrAuthority());
        Assert.IsTrue(CommonBoundaryClaimsRemainFailClosed(d4));
    }

    private static void AssertBoundaryClaim(
        NodalOsCommonBoundaryClaimsCandidate.Claim claim,
        NodalOsCommonBoundaryClaimsCandidate.ClaimState expectedState)
    {
        var packet = ReentryDecisionPacketReadOnlyPresenter.CreateFixture();
        var candidate = NodalOsCommonBoundaryClaimsCandidate.DefaultBlocked();

        Assert.IsTrue(packet.PassesSafetyProof);
        Assert.AreEqual(expectedState, candidate.StateFor(claim));
        Assert.IsTrue(CommonBoundaryClaimRemainsFailClosed(candidate, claim, expectedState));
        Assert.IsFalse(candidate.CanOverrideExistingHardBlock(claim));
    }

    private static bool CommonBoundaryClaimsRemainFailClosed(NodalOsCommonBoundaryClaimsCandidate? candidate)
    {
        var method = typeof(ReentryDecisionPacketReadOnly).GetMethod(
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
        var method = typeof(ReentryDecisionPacketReadOnly).GetMethod(
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

    private static string ReentrySource() =>
        File.ReadAllText(Path.Combine(
            RepoRoot(),
            "src",
            "OneBrain.Core",
            "Approval",
            "ReentryDecisionPacketReadOnly.cs"));

    private static IEnumerable<string> ExistingRuntimeRoots(string root) =>
        new[]
        {
            "src/OneBrain.Pilot",
            "src/OneBrain.Cli",
            "src/OneBrain.Actions",
            "src/OneBrain.Core/Execution"
        }
            .Select(relative => Path.Combine(root, relative.Replace('/', Path.DirectorySeparatorChar)))
            .Where(Directory.Exists);

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
