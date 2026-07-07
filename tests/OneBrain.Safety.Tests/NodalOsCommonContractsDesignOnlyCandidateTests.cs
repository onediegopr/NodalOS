using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace OneBrain.Safety.Tests;

[TestClass]
[TestCategory("NodalOsTier1Safety")]
[TestCategory("ProductLedger")]
[TestCategory("CommonContracts")]
[TestCategory("DesignOnly")]
[TestCategory("NoRuntimeWiring")]
public sealed class NodalOsCommonContractsDesignOnlyCandidateTests
{
    [TestMethod]
    [TestCategory("PublicProductBlock")]
    [TestCategory("ProductionRouteBlock")]
    [TestCategory("LatestPointerBlock")]
    [TestCategory("ReadPrecedenceBlock")]
    [TestCategory("ProductAuthorityBlock")]
    [TestCategory("CommandExecutionBlock")]
    [TestCategory("ReleaseCommercialBlock")]
    public void CommonContracts_CurrentBoundaryClaimsRemainBlocked()
    {
        var claims = NodalOsCommonBoundaryClaims.CurrentLocalDesignOnlyNoGo();

        foreach (var capability in Enum.GetValues<NodalOsCommonBoundaryCapability>())
        {
            Assert.IsTrue(claims.IsBlocked(capability), capability.ToString());
        }

        Assert.IsFalse(claims.RuntimeActivationAllowed);
        Assert.IsFalse(claims.ProductAuthorityAllowed);
        Assert.IsFalse(claims.CiEnforcementAllowed);
        Assert.IsFalse(claims.AllowsAnyRuntimeOrProductAuthority());
        Assert.AreEqual(NodalOsCommonEvidenceBoundary.DesignOnly, claims.EvidenceBoundary);
    }

    [TestMethod]
    [TestCategory("ProductAuthorityBlock")]
    [TestCategory("LatestPointerBlock")]
    [TestCategory("ReadPrecedenceBlock")]
    public void CommonContracts_EvidenceRolesAndWriterModesRemainDescriptiveOnly()
    {
        var roleModes = new[]
        {
            (NodalOsCommonEvidenceRole.Snapshot, NodalOsCommonWriterMode.CreateOnly),
            (NodalOsCommonEvidenceRole.Manifest, NodalOsCommonWriterMode.VersionedCreateOnly),
            (NodalOsCommonEvidenceRole.ReaderCandidate, NodalOsCommonWriterMode.ReadOnly),
            (NodalOsCommonEvidenceRole.Auxiliary, NodalOsCommonWriterMode.ReadOnly),
            (NodalOsCommonEvidenceRole.HandoffDraft, NodalOsCommonWriterMode.CreateOnly),
            (NodalOsCommonEvidenceRole.WorkspaceDraft, NodalOsCommonWriterMode.CreateOnly),
            (NodalOsCommonEvidenceRole.LedgerEntry, NodalOsCommonWriterMode.AppendOnly),
            (NodalOsCommonEvidenceRole.Checkpoint, NodalOsCommonWriterMode.Disabled)
        };

        foreach (var (role, mode) in roleModes)
        {
            var envelope = NodalOsCommonSafetyEnvelope.D1DescriptiveOnly(mode, role);

            Assert.AreEqual(NodalOsCommonSafetyDecision.DescriptiveOnlyNoRuntimeAuthority, envelope.Decision);
            Assert.AreEqual(role, envelope.EvidenceRole);
            Assert.AreEqual(mode, envelope.WriterMode);
            Assert.IsFalse(envelope.HasRuntimeOrProductSurface(), role.ToString());
            Assert.IsTrue(envelope.EvidenceRefs.All(reference => !string.IsNullOrWhiteSpace(reference)));
        }
    }

    [TestMethod]
    [TestCategory("PublicProductBlock")]
    [TestCategory("ProductionRouteBlock")]
    [TestCategory("CommandExecutionBlock")]
    public void CommonContracts_DoNotImplyRuntimeRouteServiceCommandOrCiEnforcement()
    {
        var envelope = NodalOsCommonSafetyEnvelope.D1DescriptiveOnly(
            NodalOsCommonWriterMode.Disabled,
            NodalOsCommonEvidenceRole.Auxiliary);

        Assert.IsFalse(envelope.RuntimeWired);
        Assert.IsFalse(envelope.ServiceRegistered);
        Assert.IsFalse(envelope.RouteRegistered);
        Assert.IsFalse(envelope.CommandHandlerRegistered);
        Assert.IsFalse(envelope.Claims.CiEnforcementAllowed);
        Assert.IsFalse(envelope.HasRuntimeOrProductSurface());
    }

    [TestMethod]
    [TestCategory("StaticGuard")]
    [TestCategory("PublicProductBlock")]
    [TestCategory("ProductionRouteBlock")]
    [TestCategory("CommandExecutionBlock")]
    [TestCategory("ReleaseCommercialBlock")]
    public void CommonContracts_D1ArtifactsAreTestOnlyAndNotRuntimeWired()
    {
        var root = RepoRoot();
        var d1Source = string.Join(
            Environment.NewLine,
            File.ReadAllText(Path.Combine(root, "tests", "OneBrain.Safety.Tests", "NodalOsCommonContractsDesignOnlyCandidate.cs")),
            File.ReadAllText(Path.Combine(root, "tests", "OneBrain.Safety.Tests", "NodalOsCommonContractsDesignOnlyCandidateTests.cs")));
        var productionSource = string.Join(
            Environment.NewLine,
            Directory.EnumerateFiles(Path.Combine(root, "src"), "*.cs", SearchOption.AllDirectories)
                .OrderBy(path => path, StringComparer.Ordinal)
                .Select(File.ReadAllText));

        StringAssert.Contains(d1Source, "design/test-only candidate");
        StringAssert.Contains(d1Source, "not production authority");
        StringAssert.Contains(d1Source, "RuntimeWired: false");
        StringAssert.Contains(d1Source, "ServiceRegistered: false");
        StringAssert.Contains(d1Source, "RouteRegistered: false");
        StringAssert.Contains(d1Source, "CommandHandlerRegistered: false");

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
            Assert.IsFalse(d1Source.Contains(forbidden, StringComparison.Ordinal), forbidden);
        }

        Assert.IsFalse(
            productionSource.Contains("NodalOsCommonBoundaryClaims.CurrentLocalDesignOnlyNoGo", StringComparison.Ordinal),
            "src references D1 test-only candidate factory");
        Assert.IsFalse(productionSource.Contains("NodalOsCommonSafetyEnvelope", StringComparison.Ordinal), "src references D1 candidate");
    }

    [TestMethod]
    [TestCategory("StaticGuard")]
    public void CommonContracts_MetadataRemainsManualDiscoveryOnly()
    {
        var categories = typeof(NodalOsCommonContractsDesignOnlyCandidateTests)
            .GetCustomAttributes(typeof(TestCategoryAttribute), inherit: false)
            .Cast<TestCategoryAttribute>()
            .SelectMany(attribute => attribute.TestCategories)
            .ToArray();

        CollectionAssert.Contains(categories, "CommonContracts");
        CollectionAssert.Contains(categories, "DesignOnly");
        CollectionAssert.Contains(categories, "NoRuntimeWiring");
        CollectionAssert.Contains(categories, "NodalOsTier1Safety");

        var ciFiles = Directory.EnumerateFiles(RepoRoot(), "*", SearchOption.AllDirectories)
            .Where(path => path.Contains($"{Path.DirectorySeparatorChar}.github{Path.DirectorySeparatorChar}", StringComparison.Ordinal)
                || path.EndsWith("azure-pipelines.yml", StringComparison.OrdinalIgnoreCase))
            .ToArray();

        foreach (var file in ciFiles)
        {
            var text = File.ReadAllText(file);
            Assert.IsFalse(text.Contains("CommonContracts", StringComparison.Ordinal), file);
            Assert.IsFalse(text.Contains("NodalOsTier1Safety", StringComparison.Ordinal), file);
        }
    }

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
