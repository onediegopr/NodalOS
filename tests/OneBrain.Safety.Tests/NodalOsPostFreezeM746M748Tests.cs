using System.Text.Json;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace OneBrain.Safety.Tests;

[TestClass]
[TestCategory("PostFreeze")]
[TestCategory("M746")]
[TestCategory("M747")]
[TestCategory("M748")]
public sealed class NodalOsPostFreezeM746M748Tests
{
    private const string M746ReportPath = "docs/reports/m746-post-freeze-stabilization-register.md";
    private const string M747ReportPath = "docs/reports/m747-release-deferred-roadmap.md";
    private const string M748ReportPath = "docs/reports/m748-post-freeze-next-decision.md";

    private const string StabilizationRegisterPath = "artifacts/agent-operations/m746/post-freeze-stabilization-register.json";
    private const string FreezeLockPreservedPath = "artifacts/agent-operations/m746/freeze-lock-preserved-status.json";
    private const string ReleaseDeferredPath = "artifacts/agent-operations/m746/release-deferred-status.json";
    private const string StoreBoundaryStatusPath = "artifacts/agent-operations/m746/store-boundary-stabilized-status.json";
    private const string NoPublicationPath = "artifacts/agent-operations/m746/no-publication-proof.json";
    private const string M746GoNoGoPath = "artifacts/agent-operations/m746/m746-go-no-go.json";

    private const string ReleaseDeferredRoadmapPath = "artifacts/agent-operations/m747/release-deferred-roadmap.json";
    private const string ReleaseReactivationPath = "artifacts/agent-operations/m747/release-reactivation-conditions.json";
    private const string StoreReactivationPath = "artifacts/agent-operations/m747/store-reactivation-conditions.json";
    private const string AssetsUrlRoadmapPath = "artifacts/agent-operations/m747/assets-url-readiness-roadmap.json";
    private const string PublicReleaseDryRunPath = "artifacts/agent-operations/m747/public-release-dry-run-roadmap.json";
    private const string StoreDryRunPath = "artifacts/agent-operations/m747/chrome-web-store-dry-run-roadmap.json";
    private const string M747GoNoGoPath = "artifacts/agent-operations/m747/m747-go-no-go.json";

    private const string PostFreezeDecisionPath = "artifacts/agent-operations/m748/post-freeze-next-decision.json";
    private const string NoBlindReleasePath = "artifacts/agent-operations/m748/no-blind-release-proof.json";
    private const string NoStoreSubmissionPath = "artifacts/agent-operations/m748/no-store-submission-proof.json";
    private const string NoSignedZipPath = "artifacts/agent-operations/m748/no-signed-public-zip-proof.json";
    private const string RuntimeDisabledPath = "artifacts/agent-operations/m748/runtime-provider-filesystem-browser-disabled-proof.json";
    private const string ProductBridgeCspPath = "artifacts/agent-operations/m748/product-bridge-csp-unchanged-proof.json";
    private const string NextMilestonePath = "artifacts/agent-operations/m748/next-milestone-recommendation.json";
    private const string M748GoNoGoPath = "artifacts/agent-operations/m748/m748-go-no-go.json";

    private const string ConsolidatedPath = "artifacts/agent-operations/m746-m748/post-freeze-stabilization-go-no-go.json";

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
    public void M746ArtifactsExist()
    {
        foreach (var path in new[]
        {
            M746ReportPath, StabilizationRegisterPath, FreezeLockPreservedPath,
            ReleaseDeferredPath, StoreBoundaryStatusPath, NoPublicationPath, M746GoNoGoPath
        })
            Assert.IsTrue(File.Exists(FullPath(path)), path);
    }

    [TestMethod]
    public void M747ArtifactsExist()
    {
        foreach (var path in new[]
        {
            M747ReportPath, ReleaseDeferredRoadmapPath, ReleaseReactivationPath,
            StoreReactivationPath, AssetsUrlRoadmapPath, PublicReleaseDryRunPath,
            StoreDryRunPath, M747GoNoGoPath
        })
            Assert.IsTrue(File.Exists(FullPath(path)), path);
    }

    [TestMethod]
    public void M748ArtifactsExist()
    {
        foreach (var path in new[]
        {
            M748ReportPath, PostFreezeDecisionPath, NoBlindReleasePath,
            NoStoreSubmissionPath, NoSignedZipPath, RuntimeDisabledPath,
            ProductBridgeCspPath, NextMilestonePath, M748GoNoGoPath, ConsolidatedPath
        })
            Assert.IsTrue(File.Exists(FullPath(path)), path);
    }

    [TestMethod]
    public void FreezeStoreBoundaryAndReleaseDeferredRemainStable()
    {
        using var doc = ReadJson(ConsolidatedPath);
        Assert.AreEqual("GO_PRESERVED", doc.RootElement.GetProperty("freezeCandidateLock").GetString());
        Assert.IsTrue(doc.RootElement.GetProperty("freezeLockPreserved").GetBoolean());
        Assert.AreEqual("STABILIZED", doc.RootElement.GetProperty("storeAssetsBoundary").GetString());
        Assert.IsTrue(doc.RootElement.GetProperty("releaseDeferred").GetBoolean());
    }

    [TestMethod]
    public void ReleaseStoreAndSignedZipRemainNoGo()
    {
        using var doc = ReadJson(ConsolidatedPath);
        Assert.AreEqual("NO-GO", doc.RootElement.GetProperty("publicRelease").GetString());
        Assert.AreEqual("NO-GO", doc.RootElement.GetProperty("chromeWebStore").GetString());
        Assert.IsFalse(doc.RootElement.GetProperty("signedPublicZipCreated").GetBoolean());
        Assert.IsFalse(doc.RootElement.GetProperty("actualPublicReleasePerformed").GetBoolean());
        Assert.IsFalse(doc.RootElement.GetProperty("actualChromeWebStoreSubmissionPerformed").GetBoolean());
    }

    [TestMethod]
    public void ReactivationRequiresExplicitOwnerAcceptance()
    {
        using var release = ReadJson(ReleaseReactivationPath);
        using var store = ReadJson(StoreReactivationPath);
        Assert.IsTrue(release.RootElement.GetProperty("publicReleaseReactivationRequiresExplicitOwnerAcceptance").GetBoolean());
        Assert.AreEqual("ACEPTO PUBLIC RELEASE BAJO MI RESPONSABILIDAD", release.RootElement.GetProperty("requiredPhrase").GetString());
        Assert.IsFalse(release.RootElement.GetProperty("acceptanceReceived").GetBoolean());
        Assert.IsTrue(store.RootElement.GetProperty("storeReactivationRequiresExplicitOwnerAcceptance").GetBoolean());
        Assert.AreEqual("ACEPTO CHROME WEB STORE SUBMISSION BAJO MI RESPONSABILIDAD", store.RootElement.GetProperty("requiredPhrase").GetString());
        Assert.IsFalse(store.RootElement.GetProperty("acceptanceReceived").GetBoolean());
    }

    [TestMethod]
    public void AssetsAreTrackedButNotInvented()
    {
        using var roadmap = ReadJson(AssetsUrlRoadmapPath);
        using var status = ReadJson(StoreBoundaryStatusPath);
        Assert.AreEqual("READY", roadmap.RootElement.GetProperty("assetsUrlReadinessRoadmap").GetString());
        Assert.IsFalse(roadmap.RootElement.GetProperty("valuesInvented").GetBoolean());
        Assert.IsFalse(status.RootElement.GetProperty("privacyUrlInvented").GetBoolean());
        Assert.IsFalse(status.RootElement.GetProperty("supportUrlInvented").GetBoolean());
        Assert.IsFalse(status.RootElement.GetProperty("assetsInvented").GetBoolean());
    }

    [TestMethod]
    public void EvidenceRuntimeProductAndBridgeBoundariesRemainProtected()
    {
        using var doc = ReadJson(ConsolidatedPath);
        using var product = ReadJson(ProductBridgeCspPath);
        Assert.AreEqual("OWNER_ATTESTATION_NON_AUDITABLE", doc.RootElement.GetProperty("evidenceClass").GetString());
        Assert.IsFalse(doc.RootElement.GetProperty("evidenceInvented").GetBoolean());
        Assert.IsFalse(doc.RootElement.GetProperty("runtimeProductiveEnabled").GetBoolean());
        Assert.IsFalse(doc.RootElement.GetProperty("providerCloudEnabled").GetBoolean());
        Assert.IsFalse(doc.RootElement.GetProperty("filesystemEnabled").GetBoolean());
        Assert.IsFalse(doc.RootElement.GetProperty("browserAutomationEnabled").GetBoolean());
        Assert.IsFalse(doc.RootElement.GetProperty("capabilityUnlockEnabled").GetBoolean());
        Assert.IsFalse(product.RootElement.GetProperty("productFilesModified").GetBoolean());
        Assert.IsFalse(product.RootElement.GetProperty("bridgeModified").GetBoolean());
        Assert.IsFalse(product.RootElement.GetProperty("cspModified").GetBoolean());
    }

    [TestMethod]
    public void NextMilestoneRecommendationsDoNotPublishAutomatically()
    {
        using var next = ReadJson(NextMilestonePath);
        Assert.AreEqual(
            "M749-M751 Internal Candidate Stabilization + Runtime Roadmap Re-Entry Gate",
            next.RootElement.GetProperty("recommendedNextMilestone").GetString());
        Assert.IsFalse(next.RootElement.GetProperty("publicationAllowedInNextMilestone").GetBoolean());
    }

    [TestMethod]
    public void FinalDecisionIsPostFreezeStabilizationReady()
    {
        using var doc = ReadJson(ConsolidatedPath);
        Assert.AreEqual("POST_FREEZE_STABILIZATION_READY_RELEASE_DEFERRED", doc.RootElement.GetProperty("decision").GetString());
    }
}
