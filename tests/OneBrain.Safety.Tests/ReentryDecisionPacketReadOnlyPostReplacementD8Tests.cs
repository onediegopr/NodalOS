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
public sealed class ReentryDecisionPacketReadOnlyPostReplacementD8Tests
{
    private const string CandidateRelativePath = "src/OneBrain.Core/Approval/NodalOsCommonBoundaryClaimsCandidate.cs";
    private const string D7TargetRelativePath = "src/OneBrain.Core/Approval/ReentryDecisionPacketReadOnly.cs";
    private const string D7FocusedTestsRelativePath = "tests/OneBrain.Safety.Tests/ReentryDecisionPacketReadOnlyCommonBoundaryD7Tests.cs";
    private const string D8FocusedTestsRelativePath = "tests/OneBrain.Safety.Tests/ReentryDecisionPacketReadOnlyPostReplacementD8Tests.cs";

    [TestMethod]
    [TestCategory("CommandExecutionBlock")]
    public void D8_CommandGuardExceptionIsExactToD7TargetAndNotPatternBased()
    {
        var root = RepoRoot();
        var d4GuardSource = File.ReadAllText(Path.Combine(
            root,
            "tests",
            "OneBrain.Safety.Tests",
            "NodalOsCommonBoundaryClaimsCandidateTests.cs"));
        var d5GuardSource = File.ReadAllText(Path.Combine(
            root,
            "tests",
            "OneBrain.Safety.Tests",
            "NodalOsCommonBoundaryClaimsCandidateIsolationHardeningTests.cs"));
        var d7TargetPath = Path.Combine(root, D7TargetRelativePath.Replace('/', Path.DirectorySeparatorChar));
        var hypotheticalSiblingPath = Path.Combine(
            Path.GetDirectoryName(d7TargetPath)!,
            "ReentryDecisionPacketReadOnlyCommandProbe.cs");

        StringAssert.Contains(d4GuardSource, "ReentryDecisionPacketReadOnly.cs");
        StringAssert.Contains(d5GuardSource, "ReentryDecisionPacketReadOnly.cs");
        Assert.IsTrue(IsExactD7Target(root, d7TargetPath));
        Assert.IsFalse(IsExactD7Target(root, hypotheticalSiblingPath));
        Assert.IsFalse(IsExactD7Target(root, Path.Combine(root, "src", "OneBrain.Core", "Approval", "ReentryDecisionPacketReadOnlyExtra.cs")));
        Assert.IsFalse(IsExactD7Target(root, Path.Combine(root, "src", "OneBrain.Pilot", "ReentryDecisionPacketReadOnly.cs")));
    }

    [TestMethod]
    [TestCategory("CommandExecutionBlock")]
    public void D8_CommandLikeWordingInD7TargetDoesNotCreateCommandHandlerShellOrRuntimeExecution()
    {
        var source = ReentrySource();
        var packet = ReentryDecisionPacketReadOnlyPresenter.CreateFixture();

        StringAssert.Contains(source, "CommandExecutionDenied");
        Assert.AreEqual(0, packet.CommandHandlerCount);
        Assert.AreEqual(0, packet.RuntimeInvocationCount);
        Assert.AreEqual(
            NodalOsCommonBoundaryClaimsCandidate.ClaimState.Denied,
            NodalOsCommonBoundaryClaimsCandidate.DefaultBlocked().StateFor(
                NodalOsCommonBoundaryClaimsCandidate.Claim.CommandExecutionDenied));
        AssertSourceLacksRuntimeWiring(source);
    }

    [TestMethod]
    [TestCategory("CommandExecutionBlock")]
    [TestCategory("ProductionRouteBlock")]
    public void D8_D7TargetDoesNotContainRouteDiServiceOrProductCommandRegistration()
    {
        var source = ReentrySource();

        foreach (var forbidden in RuntimeWiringNeedles())
        {
            Assert.IsFalse(source.Contains(forbidden, StringComparison.Ordinal), forbidden);
        }

        foreach (var forbidden in ProductLedgerRuntimeNeedles())
        {
            Assert.IsFalse(source.Contains(forbidden, StringComparison.Ordinal), forbidden);
        }
    }

    [TestMethod]
    [TestCategory("CommandExecutionBlock")]
    public void D8_CommandGuardExceptionDoesNotAllowOtherCommandLikeSourceReferences()
    {
        var root = RepoRoot();
        var unexpected = Directory.EnumerateFiles(Path.Combine(root, "src"), "*.cs", SearchOption.AllDirectories)
            .Where(path => !IsCandidate(path) && !IsExactD7Target(root, path))
            .Where(ContainsCandidate)
            .Where(path => File.ReadAllText(path).Contains("Command", StringComparison.OrdinalIgnoreCase))
            .Select(path => Relative(root, path))
            .OrderBy(path => path, StringComparer.Ordinal)
            .ToArray();

        CollectionAssert.AreEqual(Array.Empty<string>(), unexpected);
    }

    [TestMethod]
    [TestCategory("StaticGuard")]
    [TestCategory("PublicProductBlock")]
    [TestCategory("ProductionRouteBlock")]
    [TestCategory("CommandExecutionBlock")]
    public void D8_CandidateReferencesRemainLimitedToExplicitPostD7Allowlist()
    {
        var root = RepoRoot();
        var references = CandidateReferenceFiles(root)
            .Select(path => Relative(root, path))
            .OrderBy(path => path, StringComparer.Ordinal)
            .ToArray();
        var unexpected = references
            .Where(path => !IsAllowedPostD7Reference(path))
            .ToArray();
        var sourceReferences = references
            .Where(path => path.StartsWith("src/", StringComparison.Ordinal))
            .ToArray();

        CollectionAssert.AreEqual(Array.Empty<string>(), unexpected);
        CollectionAssert.AreEqual(
            new[] { CandidateRelativePath, D7TargetRelativePath },
            sourceReferences);
        CollectionAssert.Contains(references, D7FocusedTestsRelativePath);
        CollectionAssert.Contains(references, D8FocusedTestsRelativePath);
    }

    [TestMethod]
    [TestCategory("StaticGuard")]
    public void D8_CandidateIsAbsentFromRuntimeProductCiAndLiveAutomationRoots()
    {
        var root = RepoRoot();
        var forbiddenReferences = ForbiddenReferenceRoots(root)
            .SelectMany(path => Directory.Exists(path)
                ? Directory.EnumerateFiles(path, "*.cs", SearchOption.AllDirectories)
                : File.Exists(path)
                    ? [path]
                    : [])
            .Where(path => !IsIgnoredRepositoryOutput(path))
            .Where(ContainsCandidate)
            .Select(path => Relative(root, path))
            .OrderBy(path => path, StringComparer.Ordinal)
            .ToArray();

        CollectionAssert.AreEqual(Array.Empty<string>(), forbiddenReferences);
    }

    [TestMethod]
    [TestCategory("PublicProductBlock")]
    [TestCategory("ProductionRouteBlock")]
    [TestCategory("CommandExecutionBlock")]
    [TestCategory("ReleaseCommercialBlock")]
    public void D8_ReentryPacketStillPreservesAllBlockedNoGoAndNotClaimedSemantics()
    {
        var packet = ReentryDecisionPacketReadOnlyPresenter.CreateFixture();
        var candidate = NodalOsCommonBoundaryClaimsCandidate.DefaultBlocked();
        var expectedStates = new Dictionary<NodalOsCommonBoundaryClaimsCandidate.Claim, NodalOsCommonBoundaryClaimsCandidate.ClaimState>
        {
            [NodalOsCommonBoundaryClaimsCandidate.Claim.PublicProductBlocked] = NodalOsCommonBoundaryClaimsCandidate.ClaimState.Blocked,
            [NodalOsCommonBoundaryClaimsCandidate.Claim.ProductionRouteBlocked] = NodalOsCommonBoundaryClaimsCandidate.ClaimState.Blocked,
            [NodalOsCommonBoundaryClaimsCandidate.Claim.LatestPointerDisabled] = NodalOsCommonBoundaryClaimsCandidate.ClaimState.Disabled,
            [NodalOsCommonBoundaryClaimsCandidate.Claim.ReadPrecedenceDisabled] = NodalOsCommonBoundaryClaimsCandidate.ClaimState.Disabled,
            [NodalOsCommonBoundaryClaimsCandidate.Claim.ProductAuthorityBlocked] = NodalOsCommonBoundaryClaimsCandidate.ClaimState.Blocked,
            [NodalOsCommonBoundaryClaimsCandidate.Claim.CommandExecutionDenied] = NodalOsCommonBoundaryClaimsCandidate.ClaimState.Denied,
            [NodalOsCommonBoundaryClaimsCandidate.Claim.ShellSubprocessDenied] = NodalOsCommonBoundaryClaimsCandidate.ClaimState.Denied,
            [NodalOsCommonBoundaryClaimsCandidate.Claim.ProviderCloudNetworkNotClaimed] = NodalOsCommonBoundaryClaimsCandidate.ClaimState.NotClaimed,
            [NodalOsCommonBoundaryClaimsCandidate.Claim.DatabaseMigrationNotClaimed] = NodalOsCommonBoundaryClaimsCandidate.ClaimState.NotClaimed,
            [NodalOsCommonBoundaryClaimsCandidate.Claim.ExternalTrustNotClaimed] = NodalOsCommonBoundaryClaimsCandidate.ClaimState.NotClaimed,
            [NodalOsCommonBoundaryClaimsCandidate.Claim.ReleaseCommercialNoGo] = NodalOsCommonBoundaryClaimsCandidate.ClaimState.NoGo,
            [NodalOsCommonBoundaryClaimsCandidate.Claim.RuntimeProductEnablementNoGo] = NodalOsCommonBoundaryClaimsCandidate.ClaimState.NoGo,
            [NodalOsCommonBoundaryClaimsCandidate.Claim.CiEnforcementNotClaimed] = NodalOsCommonBoundaryClaimsCandidate.ClaimState.NotClaimed
        };

        Assert.IsTrue(packet.PassesSafetyProof);
        foreach (var (claim, state) in expectedStates)
        {
            Assert.AreEqual(state, candidate.StateFor(claim), claim.ToString());
            Assert.IsTrue(CommonBoundaryClaimRemainsFailClosed(candidate, claim, state), claim.ToString());
        }
    }

    [TestMethod]
    [TestCategory("NoAuthority")]
    [TestCategory("NoDoubleTruth")]
    public void D8_UnsafeCandidateVariantsCannotPassReentryCommonBoundaryProof()
    {
        foreach (var unsafeCandidate in UnsafeCandidateVariants())
        {
            Assert.IsFalse(CommonBoundaryClaimsRemainFailClosed(unsafeCandidate), unsafeCandidate.ToString());
        }
    }

    [TestMethod]
    [TestCategory("NoAuthority")]
    [TestCategory("NoDoubleTruth")]
    public void D8_ReentryTargetDoesNotBecomeGlobalAuthorityOrOverrideHardBlocks()
    {
        var d1 = NodalOsCommonBoundaryClaims.CurrentLocalDesignOnlyNoGo();
        var d2 = NodalOsCommonBoundaryMappingDesignOnlyAdapter.Map(
            new NodalOsCommonBoundaryMappingRequest(NodalOsCommonBoundarySourceConcept.StaticGuardCatalogHardBlock));
        var d4 = NodalOsCommonBoundaryClaimsCandidate.DefaultBlocked();
        var packet = ReentryDecisionPacketReadOnlyPresenter.CreateFixture();

        Assert.IsTrue(packet.PassesSafetyProof);
        Assert.IsTrue(d2.ExistingHardBlockAuthoritative);
        Assert.IsFalse(d2.OverrideAllowed);
        Assert.IsFalse(d4.ExistingHardBlockAuthorityReplaced);
        Assert.IsFalse(d4.AllowsRuntimeProductOrAuthority());

        foreach (var capability in d2.MappedCapabilities)
        {
            Assert.IsTrue(d1.IsBlocked(capability), capability.ToString());
            Assert.IsFalse(d4.CanOverrideExistingHardBlock(ToCandidateClaim(capability)), capability.ToString());
        }
    }

    [TestMethod]
    [TestCategory("PublicProductBlock")]
    [TestCategory("ProductionRouteBlock")]
    [TestCategory("CommandExecutionBlock")]
    [TestCategory("ReleaseCommercialBlock")]
    public void D8_ReentryPacketDoesNotActivateProductLedgerLatestStateWriterHandoffOrReleasePaths()
    {
        var packet = ReentryDecisionPacketReadOnlyPresenter.CreateFixture();
        var source = ReentrySource();

        Assert.IsTrue(packet.PassesSafetyProof);
        Assert.AreEqual(0, packet.ProductActionCount);
        Assert.AreEqual(0, packet.StateMutationCount);
        Assert.AreEqual(0, packet.FileOutputCount);
        Assert.AreEqual(0, packet.ProviderNetworkCallCount);
        Assert.AreEqual("NO-GO", packet.ReleaseCommercialStatus);

        foreach (var forbidden in ProductLedgerRuntimeNeedles())
        {
            Assert.IsFalse(source.Contains(forbidden, StringComparison.Ordinal), forbidden);
        }
    }

    private static IReadOnlyList<NodalOsCommonBoundaryClaimsCandidate> UnsafeCandidateVariants()
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
            safe with { ReleaseCommercialReady = true },
            safe with
            {
                Claims = new Dictionary<NodalOsCommonBoundaryClaimsCandidate.Claim, NodalOsCommonBoundaryClaimsCandidate.ClaimState>
                {
                    [NodalOsCommonBoundaryClaimsCandidate.Claim.PublicProductBlocked] = NodalOsCommonBoundaryClaimsCandidate.ClaimState.Denied
                }
            }
        ];
    }

    private static bool CommonBoundaryClaimsRemainFailClosed(NodalOsCommonBoundaryClaimsCandidate candidate)
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

    private static void AssertSourceLacksRuntimeWiring(string source)
    {
        foreach (var forbidden in RuntimeWiringNeedles())
        {
            Assert.IsFalse(source.Contains(forbidden, StringComparison.Ordinal), forbidden);
        }
    }

    private static string[] RuntimeWiringNeedles() =>
    [
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
        "File" + ".Write",
        "File" + ".Append",
        "Directory" + ".CreateDirectory"
    ];

    private static string[] ProductLedgerRuntimeNeedles() =>
    [
        "ProductLedgerLatestState",
        "ProductLedgerWriter",
        "ProductLedgerHandoff",
        "ProductLedgerRuntime",
        "LatestStateSnapshot",
        "ReadPrecedence: true",
        "ReleaseCommercialReady: true",
        "Kms" + "Client",
        "Worm" + "Store"
    ];

    private static IEnumerable<string> CandidateReferenceFiles(string root) =>
        CandidateReferenceRoots(root)
            .SelectMany(path => Directory.Exists(path)
                ? Directory.EnumerateFiles(path, "*", SearchOption.AllDirectories)
                : File.Exists(path)
                    ? [path]
                    : [])
            .Where(path => !IsIgnoredRepositoryOutput(path))
            .Where(path => ContentExtensions.Contains(Path.GetExtension(path)))
            .Where(ContainsCandidate);

    private static IEnumerable<string> CandidateReferenceRoots(string root) =>
    [
        Path.Combine(root, "src"),
        Path.Combine(root, "tests"),
        Path.Combine(root, "docs"),
        Path.Combine(root, ".github"),
        Path.Combine(root, "azure-pipelines.yml")
    ];

    private static IEnumerable<string> ForbiddenReferenceRoots(string root) =>
    [
        Path.Combine(root, "src", "OneBrain.Pilot"),
        Path.Combine(root, "src", "OneBrain.Cli"),
        Path.Combine(root, "src", "OneBrain.Actions"),
        Path.Combine(root, "src", "OneBrain.AgentOperations.Core"),
        Path.Combine(root, "src", "OneBrain.AgentOperations.Adapters.Browser"),
        Path.Combine(root, "src", "OneBrain.BrowserRuntime"),
        Path.Combine(root, "src", "OneBrain.BrowserExecutor.Cdp"),
        Path.Combine(root, "src", "OneBrain.BrowserPerception"),
        Path.Combine(root, ".github"),
        Path.Combine(root, "azure-pipelines.yml")
    ];

    private static bool IsAllowedPostD7Reference(string relativePath) =>
        relativePath == CandidateRelativePath
        || relativePath == D7TargetRelativePath
        || relativePath.StartsWith("tests/OneBrain.Safety.Tests/", StringComparison.Ordinal)
        || relativePath.StartsWith("docs/", StringComparison.Ordinal);

    private static bool IsCandidate(string path) =>
        path.EndsWith(
            $"src{Path.DirectorySeparatorChar}OneBrain.Core{Path.DirectorySeparatorChar}Approval{Path.DirectorySeparatorChar}NodalOsCommonBoundaryClaimsCandidate.cs",
            StringComparison.Ordinal);

    private static bool IsExactD7Target(string root, string path) =>
        Relative(root, path) == D7TargetRelativePath;

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

    private static string ReentrySource() =>
        File.ReadAllText(Path.Combine(RepoRoot(), D7TargetRelativePath.Replace('/', Path.DirectorySeparatorChar)));

    private static string Relative(string root, string path) =>
        Path.GetRelativePath(root, path).Replace('\\', '/');

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
