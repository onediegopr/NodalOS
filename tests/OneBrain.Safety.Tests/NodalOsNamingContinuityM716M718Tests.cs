using System.Text.Json;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace OneBrain.Safety.Tests;

[TestClass]
[TestCategory("NamingContinuity")]
[TestCategory("M716")]
[TestCategory("M717")]
[TestCategory("M718")]
public sealed class NodalOsNamingContinuityM716M718Tests
{
    private const string M716ReportPath = "docs/reports/m716-nodrix-contamination-audit.md";
    private const string M717ReportPath = "docs/reports/m717-nodal-os-naming-cleanup.md";
    private const string M718ReportPath = "docs/reports/m718-release-line-continuity-restore.md";

    private const string ContaminationAuditPath = "artifacts/agent-operations/m716/nodrix-contamination-audit.json";
    private const string OccurrenceInventoryPath = "artifacts/agent-operations/m716/nodrix-occurrence-inventory.json";
    private const string NamingScopePath = "artifacts/agent-operations/m716/naming-scope-classification.json";
    private const string StoreListingNamingAuditPath = "artifacts/agent-operations/m716/store-listing-naming-audit.json";
    private const string PackageFreezeNamingAuditPath = "artifacts/agent-operations/m716/package-freeze-naming-audit.json";
    private const string M716GoNoGoPath = "artifacts/agent-operations/m716/m716-go-no-go.json";

    private const string CleanupPath = "artifacts/agent-operations/m717/nodal-os-naming-cleanup.json";
    private const string PatchSummaryPath = "artifacts/agent-operations/m717/naming-cleanup-patch-summary.json";
    private const string StoreListingCleanupPath = "artifacts/agent-operations/m717/store-listing-cleanup-proof.json";
    private const string PackageFreezeCleanupPath = "artifacts/agent-operations/m717/package-freeze-cleanup-proof.json";
    private const string AllowedLegacyPath = "artifacts/agent-operations/m717/allowed-legacy-reference-register.json";
    private const string M717GoNoGoPath = "artifacts/agent-operations/m717/m717-go-no-go.json";

    private const string ContinuityRestorePath = "artifacts/agent-operations/m718/release-line-continuity-restore.json";
    private const string ActiveScopeProofPath = "artifacts/agent-operations/m718/nodal-os-active-scope-proof.json";
    private const string PublicReleaseNoGoPath = "artifacts/agent-operations/m718/public-release-no-go-proof.json";
    private const string ChromeWebStoreNoGoPath = "artifacts/agent-operations/m718/chrome-web-store-no-go-proof.json";
    private const string NextMilestonePath = "artifacts/agent-operations/m718/next-milestone-recommendation.json";
    private const string M718GoNoGoPath = "artifacts/agent-operations/m718/m718-go-no-go.json";

    private const string ConsolidatedPath = "artifacts/agent-operations/m716-m718/nodal-os-naming-continuity-go-no-go.json";

    private static string RepoRoot()
    {
        var dir = new DirectoryInfo(Environment.CurrentDirectory);
        while (dir != null && !File.Exists(Path.Combine(dir.FullName, "OneBrain.slnx")))
            dir = dir.Parent;
        return dir?.FullName ?? Environment.CurrentDirectory;
    }

    private static string FullPath(string relativePath) =>
        Path.Combine(RepoRoot(), relativePath);

    private static JsonDocument ReadJson(string relativePath) =>
        JsonDocument.Parse(File.ReadAllText(FullPath(relativePath)));

    [TestMethod]
    public void M716ArtifactsExist()
    {
        foreach (var path in new[]
        {
            M716ReportPath, ContaminationAuditPath, OccurrenceInventoryPath, NamingScopePath,
            StoreListingNamingAuditPath, PackageFreezeNamingAuditPath, M716GoNoGoPath
        })
            Assert.IsTrue(File.Exists(FullPath(path)), path);
    }

    [TestMethod]
    public void M717ArtifactsExist()
    {
        foreach (var path in new[]
        {
            M717ReportPath, CleanupPath, PatchSummaryPath, StoreListingCleanupPath,
            PackageFreezeCleanupPath, AllowedLegacyPath, M717GoNoGoPath
        })
            Assert.IsTrue(File.Exists(FullPath(path)), path);
    }

    [TestMethod]
    public void M718ArtifactsExist()
    {
        foreach (var path in new[]
        {
            M718ReportPath, ContinuityRestorePath, ActiveScopeProofPath, PublicReleaseNoGoPath,
            ChromeWebStoreNoGoPath, NextMilestonePath, M718GoNoGoPath, ConsolidatedPath
        })
            Assert.IsTrue(File.Exists(FullPath(path)), path);
    }

    [TestMethod]
    public void ConsolidatedRestoresNodalOsContinuity()
    {
        using var doc = ReadJson(ConsolidatedPath);
        var decision = doc.RootElement.GetProperty("decision").GetString();
        CollectionAssert.Contains(new[]
        {
            "NODAL_OS_NAMING_CONTINUITY_RESTORED",
            "NODAL_OS_NAMING_CONTAMINATION_REMEDIATION_REQUIRED"
        }, decision);

        Assert.AreEqual("NODAL_OS_NAMING_CONTINUITY_RESTORED", decision);
        Assert.AreEqual("NODAL OS", doc.RootElement.GetProperty("activeProject").GetString());
        Assert.IsTrue(doc.RootElement.GetProperty("nodrixOutOfScope").GetBoolean());
        Assert.IsTrue(doc.RootElement.GetProperty("contaminationAuditCompleted").GetBoolean());
        Assert.IsTrue(doc.RootElement.GetProperty("activeProductNamingErrorsFixed").GetBoolean());
        Assert.IsTrue(doc.RootElement.GetProperty("storeListingClean").GetBoolean());
        Assert.IsTrue(doc.RootElement.GetProperty("packageFreezeDocsClean").GetBoolean());
        Assert.IsTrue(doc.RootElement.GetProperty("externalAuditPackClean").GetBoolean());
    }

    [TestMethod]
    public void ReleaseAndCapabilitiesRemainNoGoDisabled()
    {
        foreach (var path in new[] { M716GoNoGoPath, M717GoNoGoPath, M718GoNoGoPath, ConsolidatedPath })
        {
            using var doc = ReadJson(path);
            Assert.IsFalse(doc.RootElement.GetProperty("publicReleaseReady").GetBoolean(), path);
            Assert.IsFalse(doc.RootElement.GetProperty("chromeWebStoreReady").GetBoolean(), path);
            Assert.IsFalse(doc.RootElement.GetProperty("publicPackageFreezeReady").GetBoolean(), path);
            Assert.IsFalse(doc.RootElement.GetProperty("runtimeProductiveEnabled").GetBoolean(), path);
            Assert.IsFalse(doc.RootElement.GetProperty("providerCloudEnabled").GetBoolean(), path);
            Assert.IsFalse(doc.RootElement.GetProperty("filesystemEnabled").GetBoolean(), path);
            Assert.IsFalse(doc.RootElement.GetProperty("browserAutomationEnabled").GetBoolean(), path);
            Assert.IsFalse(doc.RootElement.GetProperty("capabilityUnlockEnabled").GetBoolean(), path);
            Assert.IsFalse(doc.RootElement.GetProperty("bridgeModified").GetBoolean(), path);
            Assert.IsFalse(doc.RootElement.GetProperty("cspModified").GetBoolean(), path);
        }
    }

    [TestMethod]
    public void ActiveSurfacesAreNotNodrix()
    {
        using var store = ReadJson(StoreListingCleanupPath);
        using var package = ReadJson(PackageFreezeCleanupPath);
        using var scope = ReadJson(ActiveScopeProofPath);

        Assert.AreEqual("NODAL OS", store.RootElement.GetProperty("storeListingActiveProduct").GetString());
        Assert.IsFalse(store.RootElement.GetProperty("activeProductIsNodrix").GetBoolean());
        Assert.AreEqual("NODAL OS", package.RootElement.GetProperty("packageFreezeActiveProduct").GetString());
        Assert.IsFalse(package.RootElement.GetProperty("activeProductIsNodrix").GetBoolean());
        Assert.AreEqual("NODAL OS", scope.RootElement.GetProperty("externalAuditPackActiveProduct").GetString());
        Assert.IsFalse(scope.RootElement.GetProperty("nodrixUsedAsSourceOfTruth").GetBoolean());
    }

    [TestMethod]
    public void PatchSummaryListsDocumentOnlyCorrections()
    {
        using var doc = ReadJson(PatchSummaryPath);
        Assert.AreEqual(2, doc.RootElement.GetProperty("filesCorrectedCount").GetInt32());
        Assert.IsFalse(doc.RootElement.GetProperty("behaviorChanged").GetBoolean());
        var serialized = doc.RootElement.ToString();
        StringAssert.Contains(serialized, "out-of-scope external project files");
    }
}
