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
[TestCategory("PostReplacementAudit")]
[TestCategory("ApprovalExecution")]
[TestCategory("DesignOnly")]
public sealed class ApprovalExecutionPostSecondReplacementD11Tests
{
    private const string CandidateRelativePath = "src/OneBrain.Core/Approval/NodalOsCommonBoundaryClaimsCandidate.cs";
    private const string D7TargetRelativePath = "src/OneBrain.Core/Approval/ReentryDecisionPacketReadOnly.cs";
    private const string D10TargetRelativePath = "src/OneBrain.Core/Approval/ApprovalExecutionDesignOnlyProtected.cs";
    private const string D11AuditDocRelativePath = "docs/architecture/nodal-os-d11-post-second-replacement-isolation-audit.md";

    [TestMethod]
    [TestCategory("CommandExecutionBlock")]
    public void D11_D10CommandExecutionExceptionIsFileExactAndNotPatternBased()
    {
        var root = RepoRoot();
        var d10Path = Path.Combine(root, D10TargetRelativePath.Replace('/', Path.DirectorySeparatorChar));
        var hypotheticalSibling = Path.Combine(
            Path.GetDirectoryName(d10Path)!,
            "ApprovalExecutionDesignOnlyProtectedCommandHandlerProbe.cs");

        Assert.IsTrue(IsExactD10Target(root, d10Path));
        Assert.IsFalse(IsExactD10Target(root, hypotheticalSibling));
        Assert.IsFalse(IsExactD10Target(root, Path.Combine(root, "src", "OneBrain.Pilot", "ApprovalExecutionDesignOnlyProtected.cs")));
        AssertSourceHasNoRuntimeWiring(D10Source());
    }

    [TestMethod]
    [TestCategory("CommandExecutionBlock")]
    public void D11_D7AndD10CommandExceptionsAreIndependentAndNotBroadAllowlists()
    {
        var root = RepoRoot();
        var d7Path = Path.Combine(root, D7TargetRelativePath.Replace('/', Path.DirectorySeparatorChar));
        var d10Path = Path.Combine(root, D10TargetRelativePath.Replace('/', Path.DirectorySeparatorChar));
        var commandCandidateReferences = SourceFiles(root)
            .Where(ContainsCandidate)
            .Where(path => File.ReadAllText(path).Contains("Command", StringComparison.OrdinalIgnoreCase)
                || File.ReadAllText(path).Contains("Execution", StringComparison.OrdinalIgnoreCase))
            .Select(path => Relative(root, path))
            .OrderBy(path => path, StringComparer.Ordinal)
            .ToArray();

        Assert.IsTrue(IsExactD7Target(root, d7Path));
        Assert.IsTrue(IsExactD10Target(root, d10Path));
        Assert.IsFalse(IsExactD7Target(root, d10Path));
        Assert.IsFalse(IsExactD10Target(root, d7Path));
        CollectionAssert.AreEqual(
            new[] { CandidateRelativePath, D10TargetRelativePath, D7TargetRelativePath }
                .OrderBy(path => path, StringComparer.Ordinal)
                .ToArray(),
            commandCandidateReferences);
    }

    [TestMethod]
    [TestCategory("StaticGuard")]
    public void D11_CandidateReferencesRemainLimitedPostD10()
    {
        var root = RepoRoot();
        var sourceReferences = SourceFiles(root)
            .Where(ContainsCandidate)
            .Select(path => Relative(root, path))
            .OrderBy(path => path, StringComparer.Ordinal)
            .ToArray();
        var allReferences = CandidateReferenceFiles(root)
            .Select(path => Relative(root, path))
            .OrderBy(path => path, StringComparer.Ordinal)
            .ToArray();
        var unexpected = allReferences
            .Where(path => !IsAllowedCandidateReference(path))
            .ToArray();

        CollectionAssert.AreEqual(
            new[] { CandidateRelativePath, D10TargetRelativePath, D7TargetRelativePath }
                .OrderBy(path => path, StringComparer.Ordinal)
                .ToArray(),
            sourceReferences);
        CollectionAssert.AreEqual(Array.Empty<string>(), unexpected);
    }

    [TestMethod]
    [TestCategory("StaticGuard")]
    [TestCategory("PublicProductBlock")]
    [TestCategory("ProductionRouteBlock")]
    public void D11_CandidateIsAbsentFromRuntimeProductCiAndLiveAutomationRoots()
    {
        var root = RepoRoot();
        var forbiddenReferences = ForbiddenCandidateReferenceRoots(root)
            .SelectMany(path => Directory.Exists(path)
                ? Directory.EnumerateFiles(path, "*", SearchOption.AllDirectories)
                : File.Exists(path)
                    ? [path]
                    : [])
            .Where(path => !IsIgnoredRepositoryOutput(path))
            .Where(path => ContentExtensions.Contains(Path.GetExtension(path)))
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
    public void D11_ApprovalExecutionBehaviorPreservesBlockedNoGoAndDesignOnlyState()
    {
        var spec = ApprovalExecutionDesignOnlyProtectedPresenter.CreateFixture();
        var proof = spec.AntiCapabilityProof;

        Assert.AreEqual(ApprovalExecutionDesignStatus.DesignOnly, spec.Status);
        Assert.IsTrue(spec.ReadOnly);
        Assert.IsTrue(spec.DesignOnly);
        Assert.IsTrue(spec.PreviewOnly);
        Assert.IsTrue(proof.Passes);
        Assert.IsTrue(spec.Readiness.BlocksRealExecution);
        Assert.IsFalse(spec.Readiness.CommandHandlerAvailable);
        Assert.IsFalse(spec.Readiness.ProductServiceRegistered);
        Assert.IsFalse(spec.Readiness.ReleaseCommercialReady);
        Assert.IsFalse(spec.HasRealExecution);
        Assert.IsFalse(spec.HasStateMutation);
        Assert.IsFalse(spec.HasRuntimeLive);
        Assert.IsFalse(spec.HasProductActions);
        Assert.IsTrue(spec.Gates.All(gate => gate.Status == ApprovalExecutionDesignStatus.Blocked));
        Assert.IsTrue(spec.Previews.All(preview => preview.PreviewOnly
            && !preview.ExecutesApproval
            && !preview.MutatesState
            && !preview.ExposesProductAction
            && !preview.StartsRuntime));
    }

    [TestMethod]
    [TestCategory("PublicProductBlock")]
    [TestCategory("ProductionRouteBlock")]
    [TestCategory("CommandExecutionBlock")]
    [TestCategory("ReleaseCommercialBlock")]
    public void D11_ApprovalExecutionCommonClaimStatesRemainEquivalentToCandidate()
    {
        var candidate = NodalOsCommonBoundaryClaimsCandidate.DefaultBlocked();

        Assert.IsTrue(D10CommonBoundaryClaimsRemainFailClosed(candidate));
        foreach (var (claim, expectedState) in ExpectedCandidateStates())
        {
            Assert.AreEqual(expectedState, candidate.StateFor(claim), claim.ToString());
            Assert.IsTrue(candidate.IsFailClosed(claim), claim.ToString());
            Assert.IsFalse(candidate.CanOverrideExistingHardBlock(claim), claim.ToString());
        }
    }

    [TestMethod]
    [TestCategory("NoAuthority")]
    [TestCategory("NoDoubleTruth")]
    public void D11_UnsafeCandidateVariantsCannotPassD10ApprovalProof()
    {
        foreach (var unsafeCandidate in UnsafeCandidateVariants())
        {
            Assert.IsFalse(D10CommonBoundaryClaimsRemainFailClosed(unsafeCandidate), unsafeCandidate.ToString());
        }
    }

    [TestMethod]
    [TestCategory("NoAuthority")]
    [TestCategory("NoDoubleTruth")]
    public void D11_D7AndD10TogetherDoNotCreateCommonAuthorityByAccumulation()
    {
        var d1 = NodalOsCommonBoundaryClaims.CurrentLocalDesignOnlyNoGo();
        var d2 = NodalOsCommonBoundaryMappingDesignOnlyAdapter.Map(
            new NodalOsCommonBoundaryMappingRequest(NodalOsCommonBoundarySourceConcept.StaticGuardCatalogHardBlock));
        var d4 = NodalOsCommonBoundaryClaimsCandidate.DefaultBlocked();
        var d7 = ReentryDecisionPacketReadOnlyPresenter.CreateFixture();
        var d10 = ApprovalExecutionDesignOnlyProtectedPresenter.CreateFixture();
        var sourceConsumers = SourceConsumerReferences(RepoRoot());

        Assert.IsTrue(d7.PassesSafetyProof);
        Assert.IsTrue(d10.AntiCapabilityProof.Passes);
        Assert.IsTrue(D7CommonBoundaryClaimsRemainFailClosed(d4));
        Assert.IsTrue(D10CommonBoundaryClaimsRemainFailClosed(d4));
        Assert.IsFalse(d4.AllowsRuntimeProductOrAuthority());
        Assert.IsFalse(d4.ExistingHardBlockAuthorityReplaced);
        Assert.IsFalse(d2.OverrideAllowed);
        Assert.IsFalse(d2.Envelope.HasRuntimeOrProductSurface());
        CollectionAssert.AreEqual(
            new[] { D10TargetRelativePath, D7TargetRelativePath }
                .OrderBy(path => path, StringComparer.Ordinal)
                .ToArray(),
            sourceConsumers);

        foreach (var capability in d2.MappedCapabilities)
        {
            Assert.IsTrue(d1.IsBlocked(capability), capability.ToString());
            Assert.IsFalse(d4.CanOverrideExistingHardBlock(ToCandidateClaim(capability)), capability.ToString());
        }
    }

    [TestMethod]
    [TestCategory("NoAuthority")]
    [TestCategory("NoDoubleTruth")]
    public void D11_D1D2RemainTestOnlyAndHardBlocksRemainAuthoritative()
    {
        var root = RepoRoot();
        var productionSource = string.Join(
            Environment.NewLine,
            SourceFiles(root).OrderBy(path => path, StringComparer.Ordinal).Select(File.ReadAllText));
        var d1Envelope = NodalOsCommonSafetyEnvelope.D1DescriptiveOnly(
            NodalOsCommonWriterMode.Disabled,
            NodalOsCommonEvidenceRole.Auxiliary);
        var d2Result = NodalOsCommonBoundaryMappingDesignOnlyAdapter.Map(
            new NodalOsCommonBoundaryMappingRequest(NodalOsCommonBoundarySourceConcept.CommandExecutionDenied));

        Assert.IsFalse(productionSource.Contains("NodalOsCommonBoundaryClaims.CurrentLocalDesignOnlyNoGo", StringComparison.Ordinal));
        Assert.IsFalse(productionSource.Contains("NodalOsCommonBoundaryMappingDesignOnlyAdapter", StringComparison.Ordinal));
        Assert.IsFalse(productionSource.Contains("NodalOsCommonSafetyEnvelope", StringComparison.Ordinal));
        Assert.IsFalse(d1Envelope.HasRuntimeOrProductSurface());
        Assert.IsFalse(d2Result.OverrideAllowed);
        Assert.IsTrue(d2Result.MappedCapabilities.All(d2Result.Envelope.Claims.IsBlocked));
    }

    [TestMethod]
    [TestCategory("PublicProductBlock")]
    [TestCategory("ProductionRouteBlock")]
    [TestCategory("CommandExecutionBlock")]
    [TestCategory("ReleaseCommercialBlock")]
    public void D11_PostSecondReplacementDoesNotActivateProductLedgerLatestStateWriterHandoffOrExternalTrust()
    {
        var source = string.Join(Environment.NewLine, D7Source(), D10Source());
        var candidate = NodalOsCommonBoundaryClaimsCandidate.DefaultBlocked();

        foreach (var forbidden in ProductLedgerRuntimeNeedles())
        {
            Assert.IsFalse(source.Contains(forbidden, StringComparison.Ordinal), forbidden);
        }

        Assert.AreEqual(
            NodalOsCommonBoundaryClaimsCandidate.ClaimState.NotClaimed,
            candidate.StateFor(NodalOsCommonBoundaryClaimsCandidate.Claim.ProviderCloudNetworkNotClaimed));
        Assert.AreEqual(
            NodalOsCommonBoundaryClaimsCandidate.ClaimState.NotClaimed,
            candidate.StateFor(NodalOsCommonBoundaryClaimsCandidate.Claim.DatabaseMigrationNotClaimed));
        Assert.AreEqual(
            NodalOsCommonBoundaryClaimsCandidate.ClaimState.NotClaimed,
            candidate.StateFor(NodalOsCommonBoundaryClaimsCandidate.Claim.ExternalTrustNotClaimed));
    }

    [TestMethod]
    [TestCategory("StaticGuard")]
    public void D11_SourceBloatTrajectoryIsExplicitAndNoReductionIsClaimed()
    {
        var root = RepoRoot();
        var docs = string.Join(
            Environment.NewLine,
            File.ReadAllText(Path.Combine(root, "docs", "architecture", "nodal-os-d9-second-minimal-replacement-plan-audit.md")),
            File.ReadAllText(Path.Combine(root, "docs", "architecture", "nodal-os-simplification-backlog.md")),
            File.Exists(Path.Combine(root, D11AuditDocRelativePath.Replace('/', Path.DirectorySeparatorChar)))
                ? File.ReadAllText(Path.Combine(root, D11AuditDocRelativePath.Replace('/', Path.DirectorySeparatorChar)))
                : string.Empty);

        StringAssert.Contains(docs, "source bloat reduction remains `0%`");
        StringAssert.Contains(docs, "D10 adds net `+70`");
        StringAssert.Contains(docs, "cumulative D7+D10 source impact is net `+140`");
        StringAssert.Contains(docs, "proven equivalence/isolation, not reduced source bloat");
    }

    [TestMethod]
    [TestCategory("NoAuthority")]
    public void D11_NoPublicCommonBoundaryProofSurfaceWasAddedToD7OrD10()
    {
        AssertNoPublicCommonBoundaryMethods(typeof(ReentryDecisionPacketReadOnly));
        AssertNoPublicCommonBoundaryMethods(typeof(ApprovalExecutionAntiCapabilityProof));
    }

    private static IReadOnlyDictionary<NodalOsCommonBoundaryClaimsCandidate.Claim, NodalOsCommonBoundaryClaimsCandidate.ClaimState> ExpectedCandidateStates() =>
        new Dictionary<NodalOsCommonBoundaryClaimsCandidate.Claim, NodalOsCommonBoundaryClaimsCandidate.ClaimState>
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
                Claims = new Dictionary<NodalOsCommonBoundaryClaimsCandidate.Claim, NodalOsCommonBoundaryClaimsCandidate.ClaimState>()
            },
            safe with
            {
                Claims = new Dictionary<NodalOsCommonBoundaryClaimsCandidate.Claim, NodalOsCommonBoundaryClaimsCandidate.ClaimState>
                {
                    [NodalOsCommonBoundaryClaimsCandidate.Claim.PublicProductBlocked] = NodalOsCommonBoundaryClaimsCandidate.ClaimState.Denied
                }
            }
        ];
    }

    private static bool D7CommonBoundaryClaimsRemainFailClosed(NodalOsCommonBoundaryClaimsCandidate candidate) =>
        InvokePrivateBoundaryProof(typeof(ReentryDecisionPacketReadOnly), candidate);

    private static bool D10CommonBoundaryClaimsRemainFailClosed(NodalOsCommonBoundaryClaimsCandidate candidate) =>
        InvokePrivateBoundaryProof(typeof(ApprovalExecutionAntiCapabilityProof), candidate);

    private static bool InvokePrivateBoundaryProof(Type type, NodalOsCommonBoundaryClaimsCandidate candidate)
    {
        var method = type.GetMethod(
            "CommonBoundaryClaimsRemainFailClosed",
            BindingFlags.NonPublic | BindingFlags.Static,
            binder: null,
            types: [typeof(NodalOsCommonBoundaryClaimsCandidate)],
            modifiers: null);

        Assert.IsNotNull(method);
        return (bool)method!.Invoke(null, [candidate])!;
    }

    private static void AssertNoPublicCommonBoundaryMethods(Type type)
    {
        var publicBoundaryMethods = type.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static)
            .Where(method => method.Name.Contains("CommonBoundary", StringComparison.Ordinal))
            .Select(method => method.Name)
            .ToArray();

        CollectionAssert.AreEqual(Array.Empty<string>(), publicBoundaryMethods);
    }

    private static void AssertSourceHasNoRuntimeWiring(string source)
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
        "Endpoint" + "RouteBuilder",
        "Map" + "Get",
        "Map" + "Post",
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
    ];

    private static string[] ProductLedgerRuntimeNeedles() =>
    [
        "ProductLedgerLatestState",
        "ProductLedgerWriter",
        "ProductLedgerHandoff",
        "ProductLedgerRuntime",
        "LatestStateSnapshot",
        "ReadPrecedence:" + " true",
        "ReleaseCommercialReady:" + " true",
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

    private static IEnumerable<string> ForbiddenCandidateReferenceRoots(string root) =>
    [
        Path.Combine(root, "src", "OneBrain.Pilot"),
        Path.Combine(root, "src", "OneBrain.Cli"),
        Path.Combine(root, "src", "OneBrain.Actions"),
        Path.Combine(root, "src", "OneBrain.Core", "Execution"),
        Path.Combine(root, "src", "OneBrain.AgentOperations.Core"),
        Path.Combine(root, "src", "OneBrain.AgentOperations.Adapters.Browser"),
        Path.Combine(root, "src", "OneBrain.BrowserRuntime"),
        Path.Combine(root, "src", "OneBrain.BrowserExecutor.Cdp"),
        Path.Combine(root, "src", "OneBrain.BrowserPerception"),
        Path.Combine(root, ".github"),
        Path.Combine(root, "azure-pipelines.yml")
    ];

    private static IEnumerable<string> SourceFiles(string root) =>
        Directory.EnumerateFiles(Path.Combine(root, "src"), "*.cs", SearchOption.AllDirectories);

    private static string[] SourceConsumerReferences(string root) =>
        SourceFiles(root)
            .Where(ContainsCandidate)
            .Select(path => Relative(root, path))
            .Where(path => path != CandidateRelativePath)
            .OrderBy(path => path, StringComparer.Ordinal)
            .ToArray();

    private static bool IsAllowedCandidateReference(string relativePath) =>
        relativePath == CandidateRelativePath
        || relativePath == D7TargetRelativePath
        || relativePath == D10TargetRelativePath
        || relativePath.StartsWith("tests/OneBrain.Safety.Tests/", StringComparison.Ordinal)
        || relativePath.StartsWith("docs/", StringComparison.Ordinal);

    private static bool IsExactD7Target(string root, string path) =>
        Relative(root, path) == D7TargetRelativePath;

    private static bool IsExactD10Target(string root, string path) =>
        Relative(root, path) == D10TargetRelativePath;

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

    private static string D7Source() =>
        File.ReadAllText(Path.Combine(RepoRoot(), D7TargetRelativePath.Replace('/', Path.DirectorySeparatorChar)));

    private static string D10Source() =>
        File.ReadAllText(Path.Combine(RepoRoot(), D10TargetRelativePath.Replace('/', Path.DirectorySeparatorChar)));

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
