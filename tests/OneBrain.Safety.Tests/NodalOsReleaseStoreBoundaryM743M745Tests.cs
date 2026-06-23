using System.Text.Json;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace OneBrain.Safety.Tests;

[TestClass]
[TestCategory("ReleaseStoreBoundary")]
[TestCategory("M743")]
[TestCategory("M744")]
[TestCategory("M745")]
public sealed class NodalOsReleaseStoreBoundaryM743M745Tests
{
    private const string M743ReportPath = "docs/reports/m743-release-acceptance-required-register.md";
    private const string M744ReportPath = "docs/reports/m744-store-assets-boundary-stabilization.md";
    private const string M745ReportPath = "docs/reports/m745-release-store-deferred-next-decision.md";

    private const string ReleaseRegisterPath = "artifacts/agent-operations/m743/release-acceptance-required-register.json";
    private const string PublicReleaseAcceptancePath = "artifacts/agent-operations/m743/public-release-owner-acceptance-required.json";
    private const string StoreAcceptancePath = "artifacts/agent-operations/m743/store-owner-acceptance-required.json";
    private const string ReleaseDeferredPath = "artifacts/agent-operations/m743/release-deferred-status.json";
    private const string NoPublicReleasePath = "artifacts/agent-operations/m743/no-public-release-proof.json";
    private const string M743GoNoGoPath = "artifacts/agent-operations/m743/m743-go-no-go.json";

    private const string StoreAssetsBoundaryPath = "artifacts/agent-operations/m744/store-assets-boundary-stabilization.json";
    private const string PrivacyUrlPath = "artifacts/agent-operations/m744/privacy-url-boundary.json";
    private const string SupportUrlPath = "artifacts/agent-operations/m744/support-url-boundary.json";
    private const string StoreListingPath = "artifacts/agent-operations/m744/store-listing-boundary.json";
    private const string IconsAssetsPath = "artifacts/agent-operations/m744/icons-assets-boundary.json";
    private const string ScreenshotsAssetsPath = "artifacts/agent-operations/m744/screenshots-promotional-assets-boundary.json";
    private const string PermissionDisclosurePath = "artifacts/agent-operations/m744/permission-disclosure-boundary.json";
    private const string PackageSigningPath = "artifacts/agent-operations/m744/package-signing-release-procedure-boundary.json";
    private const string NoAssetInventionPath = "artifacts/agent-operations/m744/no-asset-invention-proof.json";
    private const string M744GoNoGoPath = "artifacts/agent-operations/m744/m744-go-no-go.json";

    private const string DeferredNextDecisionPath = "artifacts/agent-operations/m745/release-store-deferred-next-decision.json";
    private const string FreezeLockPreservedPath = "artifacts/agent-operations/m745/freeze-lock-preserved-proof.json";
    private const string NoBlindPublicationPath = "artifacts/agent-operations/m745/no-blind-publication-proof.json";
    private const string NoSignedZipPath = "artifacts/agent-operations/m745/no-signed-public-zip-proof.json";
    private const string RuntimeDisabledPath = "artifacts/agent-operations/m745/runtime-provider-filesystem-browser-disabled-proof.json";
    private const string ProductBridgeCspPath = "artifacts/agent-operations/m745/product-bridge-csp-unchanged-proof.json";
    private const string NextMilestonePath = "artifacts/agent-operations/m745/next-milestone-recommendation.json";
    private const string M745GoNoGoPath = "artifacts/agent-operations/m745/m745-go-no-go.json";

    private const string ConsolidatedPath = "artifacts/agent-operations/m743-m745/release-store-boundary-go-no-go.json";

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
    public void M743ArtifactsExist()
    {
        foreach (var path in new[]
        {
            M743ReportPath, ReleaseRegisterPath, PublicReleaseAcceptancePath,
            StoreAcceptancePath, ReleaseDeferredPath, NoPublicReleasePath, M743GoNoGoPath
        })
            Assert.IsTrue(File.Exists(FullPath(path)), path);
    }

    [TestMethod]
    public void M744ArtifactsExist()
    {
        foreach (var path in new[]
        {
            M744ReportPath, StoreAssetsBoundaryPath, PrivacyUrlPath, SupportUrlPath,
            StoreListingPath, IconsAssetsPath, ScreenshotsAssetsPath, PermissionDisclosurePath,
            PackageSigningPath, NoAssetInventionPath, M744GoNoGoPath
        })
            Assert.IsTrue(File.Exists(FullPath(path)), path);
    }

    [TestMethod]
    public void M745ArtifactsExist()
    {
        foreach (var path in new[]
        {
            M745ReportPath, DeferredNextDecisionPath, FreezeLockPreservedPath,
            NoBlindPublicationPath, NoSignedZipPath, RuntimeDisabledPath,
            ProductBridgeCspPath, NextMilestonePath, M745GoNoGoPath, ConsolidatedPath
        })
            Assert.IsTrue(File.Exists(FullPath(path)), path);
    }

    [TestMethod]
    public void FreezeCandidateLockAndReleaseAcceptanceStateArePreserved()
    {
        using var doc = ReadJson(ConsolidatedPath);
        Assert.AreEqual("GO", doc.RootElement.GetProperty("freezeCandidateLock").GetString());
        Assert.IsTrue(doc.RootElement.GetProperty("freezeLockPreserved").GetBoolean());
        Assert.AreEqual("REQUIRED", doc.RootElement.GetProperty("publicReleaseOwnerAcceptance").GetString());
        Assert.AreEqual("REQUIRED", doc.RootElement.GetProperty("chromeWebStoreOwnerAcceptance").GetString());
    }

    [TestMethod]
    public void PublicReleaseAndStoreRemainNoGoWithNoPublicationArtifacts()
    {
        using var doc = ReadJson(ConsolidatedPath);
        Assert.AreEqual("NO-GO", doc.RootElement.GetProperty("publicRelease").GetString());
        Assert.AreEqual("NO-GO", doc.RootElement.GetProperty("chromeWebStore").GetString());
        Assert.IsFalse(doc.RootElement.GetProperty("signedPublicZipCreated").GetBoolean());
        Assert.IsFalse(doc.RootElement.GetProperty("actualPublicReleasePerformed").GetBoolean());
        Assert.IsFalse(doc.RootElement.GetProperty("actualChromeWebStoreSubmissionPerformed").GetBoolean());
    }

    [TestMethod]
    public void StoreAssetsBoundaryRecordsMissingValuesWithoutInvention()
    {
        using var boundary = ReadJson(StoreAssetsBoundaryPath);
        using var privacy = ReadJson(PrivacyUrlPath);
        using var support = ReadJson(SupportUrlPath);
        using var noInvention = ReadJson(NoAssetInventionPath);
        Assert.AreEqual("STABILIZED", boundary.RootElement.GetProperty("storeAssetsBoundary").GetString());
        Assert.IsFalse(boundary.RootElement.GetProperty("assetValuesInvented").GetBoolean());
        Assert.AreEqual("pending", privacy.RootElement.GetProperty("status").GetString());
        Assert.AreEqual("pending", support.RootElement.GetProperty("status").GetString());
        Assert.IsFalse(noInvention.RootElement.GetProperty("privacyUrlInvented").GetBoolean());
        Assert.IsFalse(noInvention.RootElement.GetProperty("supportUrlInvented").GetBoolean());
        Assert.IsFalse(noInvention.RootElement.GetProperty("screenshotsInvented").GetBoolean());
    }

    [TestMethod]
    public void EvidenceClassAndRuntimeBoundariesRemainProtected()
    {
        using var doc = ReadJson(ConsolidatedPath);
        Assert.AreEqual("OWNER_ATTESTATION_NON_AUDITABLE", doc.RootElement.GetProperty("evidenceClass").GetString());
        Assert.IsFalse(doc.RootElement.GetProperty("evidenceInvented").GetBoolean());
        Assert.IsFalse(doc.RootElement.GetProperty("runtimeProductiveEnabled").GetBoolean());
        Assert.IsFalse(doc.RootElement.GetProperty("providerCloudEnabled").GetBoolean());
        Assert.IsFalse(doc.RootElement.GetProperty("filesystemEnabled").GetBoolean());
        Assert.IsFalse(doc.RootElement.GetProperty("browserAutomationEnabled").GetBoolean());
        Assert.IsFalse(doc.RootElement.GetProperty("capabilityUnlockEnabled").GetBoolean());
    }

    [TestMethod]
    public void ProductBridgeCspAndBlindPublicationRemainProtected()
    {
        using var product = ReadJson(ProductBridgeCspPath);
        using var noBlind = ReadJson(NoBlindPublicationPath);
        Assert.IsFalse(product.RootElement.GetProperty("productFilesModified").GetBoolean());
        Assert.IsFalse(product.RootElement.GetProperty("bridgeModified").GetBoolean());
        Assert.IsFalse(product.RootElement.GetProperty("cspModified").GetBoolean());
        Assert.IsFalse(product.RootElement.GetProperty("manifestsModified").GetBoolean());
        Assert.IsTrue(noBlind.RootElement.GetProperty("noBlindPublication").GetBoolean());
    }

    [TestMethod]
    public void FinalDecisionAndNextMilestoneAreReleaseDeferred()
    {
        using var doc = ReadJson(ConsolidatedPath);
        using var next = ReadJson(NextMilestonePath);
        Assert.AreEqual("RELEASE_ACCEPTANCE_REQUIRED_STORE_ASSETS_BOUNDARY_STABILIZED", doc.RootElement.GetProperty("decision").GetString());
        Assert.AreEqual(
            "M746-M748 Post-Freeze Stabilization + Release Deferred Roadmap",
            next.RootElement.GetProperty("recommendedNextMilestone").GetString());
    }
}
