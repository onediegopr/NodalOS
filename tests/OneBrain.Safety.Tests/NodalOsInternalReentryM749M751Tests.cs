using System.Text.Json;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace OneBrain.Safety.Tests;

[TestClass]
[TestCategory("InternalReentry")]
[TestCategory("M749")]
[TestCategory("M750")]
[TestCategory("M751")]
public sealed class NodalOsInternalReentryM749M751Tests
{
    private const string M749ReportPath = "docs/reports/m749-internal-candidate-stabilization-register.md";
    private const string M750ReportPath = "docs/reports/m750-runtime-roadmap-re-entry-gate.md";
    private const string M751ReportPath = "docs/reports/m751-next-internal-roadmap-decision.md";

    private const string StabilizationPath = "artifacts/agent-operations/m749/internal-candidate-stabilization-register.json";
    private const string FreezeLockPath = "artifacts/agent-operations/m749/freeze-lock-preserved-status.json";
    private const string ReleaseStoreDeferredPath = "artifacts/agent-operations/m749/release-store-deferred-status.json";
    private const string CurrentStatePath = "artifacts/agent-operations/m749/internal-candidate-current-state.json";
    private const string NoPublicationPath = "artifacts/agent-operations/m749/no-publication-proof.json";
    private const string M749GoNoGoPath = "artifacts/agent-operations/m749/m749-go-no-go.json";

    private const string RuntimeReentryGatePath = "artifacts/agent-operations/m750/runtime-roadmap-re-entry-gate.json";
    private const string AllowedScopePath = "artifacts/agent-operations/m750/runtime-reentry-allowed-scope.json";
    private const string BlockedScopePath = "artifacts/agent-operations/m750/runtime-reentry-blocked-scope.json";
    private const string ProviderCloudBoundaryPath = "artifacts/agent-operations/m750/provider-cloud-reentry-boundary.json";
    private const string FilesystemBrowserBoundaryPath = "artifacts/agent-operations/m750/filesystem-browser-capability-boundary.json";
    private const string M750GoNoGoPath = "artifacts/agent-operations/m750/m750-go-no-go.json";

    private const string NextRoadmapPath = "artifacts/agent-operations/m751/next-internal-roadmap-decision.json";
    private const string RoadmapOptionsPath = "artifacts/agent-operations/m751/internal-roadmap-options.json";
    private const string RecommendedNextPath = "artifacts/agent-operations/m751/recommended-next-milestone.json";
    private const string NoRuntimeUnlockPath = "artifacts/agent-operations/m751/no-runtime-unlock-proof.json";
    private const string NoBridgeCspProductPath = "artifacts/agent-operations/m751/no-bridge-csp-product-change-proof.json";
    private const string M751GoNoGoPath = "artifacts/agent-operations/m751/m751-go-no-go.json";

    private const string ConsolidatedPath = "artifacts/agent-operations/m749-m751/internal-candidate-runtime-reentry-go-no-go.json";

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
    public void M749ArtifactsExist()
    {
        foreach (var path in new[]
        {
            M749ReportPath, StabilizationPath, FreezeLockPath, ReleaseStoreDeferredPath,
            CurrentStatePath, NoPublicationPath, M749GoNoGoPath
        })
            Assert.IsTrue(File.Exists(FullPath(path)), path);
    }

    [TestMethod]
    public void M750ArtifactsExist()
    {
        foreach (var path in new[]
        {
            M750ReportPath, RuntimeReentryGatePath, AllowedScopePath, BlockedScopePath,
            ProviderCloudBoundaryPath, FilesystemBrowserBoundaryPath, M750GoNoGoPath
        })
            Assert.IsTrue(File.Exists(FullPath(path)), path);
    }

    [TestMethod]
    public void M751ArtifactsExist()
    {
        foreach (var path in new[]
        {
            M751ReportPath, NextRoadmapPath, RoadmapOptionsPath, RecommendedNextPath,
            NoRuntimeUnlockPath, NoBridgeCspProductPath, M751GoNoGoPath, ConsolidatedPath
        })
            Assert.IsTrue(File.Exists(FullPath(path)), path);
    }

    [TestMethod]
    public void FreezeReleaseStoreAndPublicationRemainProtected()
    {
        using var doc = ReadJson(ConsolidatedPath);
        Assert.AreEqual("GO_PRESERVED", doc.RootElement.GetProperty("freezeCandidateLock").GetString());
        Assert.IsTrue(doc.RootElement.GetProperty("freezeLockPreserved").GetBoolean());
        Assert.IsTrue(doc.RootElement.GetProperty("releaseDeferred").GetBoolean());
        Assert.AreEqual("NO-GO", doc.RootElement.GetProperty("publicRelease").GetString());
        Assert.AreEqual("NO-GO", doc.RootElement.GetProperty("chromeWebStore").GetString());
        Assert.IsFalse(doc.RootElement.GetProperty("signedPublicZipCreated").GetBoolean());
        Assert.IsFalse(doc.RootElement.GetProperty("actualPublicReleasePerformed").GetBoolean());
        Assert.IsFalse(doc.RootElement.GetProperty("actualChromeWebStoreSubmissionPerformed").GetBoolean());
    }

    [TestMethod]
    public void RuntimeRoadmapReentryIsPlanningOnly()
    {
        using var doc = ReadJson(RuntimeReentryGatePath);
        Assert.AreEqual("RUNTIME_ROADMAP_REENTRY_PLANNING_ONLY_READY", doc.RootElement.GetProperty("runtimeRoadmapReentryStatus").GetString());
        Assert.IsTrue(doc.RootElement.GetProperty("planningOnly").GetBoolean());
        Assert.IsFalse(doc.RootElement.GetProperty("productiveRuntimeEnabled").GetBoolean());
        Assert.IsFalse(doc.RootElement.GetProperty("providerCloudEnabled").GetBoolean());
        Assert.IsFalse(doc.RootElement.GetProperty("filesystemEnabled").GetBoolean());
        Assert.IsFalse(doc.RootElement.GetProperty("browserAutomationEnabled").GetBoolean());
        Assert.IsFalse(doc.RootElement.GetProperty("capabilityUnlockEnabled").GetBoolean());
    }

    [TestMethod]
    public void AllowedScopeIncludesDocsArtifactsTestsAndPlanning()
    {
        using var doc = ReadJson(AllowedScopePath);
        var values = doc.RootElement.GetProperty("allowedScope").EnumerateArray().Select(e => e.GetString()).ToArray();
        CollectionAssert.Contains(values, "docs");
        CollectionAssert.Contains(values, "reports");
        CollectionAssert.Contains(values, "artifacts");
        CollectionAssert.Contains(values, "tests");
        CollectionAssert.Contains(values, "roadmap planning");
        CollectionAssert.Contains(values, "runtime capability inventory");
    }

    [TestMethod]
    public void BlockedScopeIncludesRuntimeProviderFilesystemBrowserCapabilityReleaseAndStore()
    {
        using var doc = ReadJson(BlockedScopePath);
        var values = doc.RootElement.GetProperty("blockedScope").EnumerateArray().Select(e => e.GetString()).ToArray();
        CollectionAssert.Contains(values, "runtime productive execution");
        CollectionAssert.Contains(values, "provider/cloud live calls");
        CollectionAssert.Contains(values, "filesystem real unlock");
        CollectionAssert.Contains(values, "browser automation unlock");
        CollectionAssert.Contains(values, "capability unlock");
        CollectionAssert.Contains(values, "Store submission");
        CollectionAssert.Contains(values, "public release");
        CollectionAssert.Contains(values, "signed public ZIP");
    }

    [TestMethod]
    public void EvidenceRuntimeProductBridgeCspRemainProtected()
    {
        using var doc = ReadJson(ConsolidatedPath);
        Assert.AreEqual("OWNER_ATTESTATION_NON_AUDITABLE", doc.RootElement.GetProperty("evidenceClass").GetString());
        Assert.IsFalse(doc.RootElement.GetProperty("evidenceInvented").GetBoolean());
        Assert.IsFalse(doc.RootElement.GetProperty("runtimeProductiveEnabled").GetBoolean());
        Assert.IsFalse(doc.RootElement.GetProperty("providerCloudEnabled").GetBoolean());
        Assert.IsFalse(doc.RootElement.GetProperty("filesystemEnabled").GetBoolean());
        Assert.IsFalse(doc.RootElement.GetProperty("browserAutomationEnabled").GetBoolean());
        Assert.IsFalse(doc.RootElement.GetProperty("capabilityUnlockEnabled").GetBoolean());
        Assert.IsFalse(doc.RootElement.GetProperty("productFilesModified").GetBoolean());
        Assert.IsFalse(doc.RootElement.GetProperty("bridgeModified").GetBoolean());
        Assert.IsFalse(doc.RootElement.GetProperty("cspModified").GetBoolean());
    }

    [TestMethod]
    public void RecommendedNextMilestoneIsRuntimeCapabilityInventoryPlanning()
    {
        using var doc = ReadJson(RecommendedNextPath);
        Assert.AreEqual(
            "M752-M754 Runtime Capability Inventory + Policy/Evidence Re-Entry Plan",
            doc.RootElement.GetProperty("recommendedNextMilestone").GetString());
        Assert.AreEqual("planning/tests/docs only", doc.RootElement.GetProperty("scope").GetString());
        Assert.IsFalse(doc.RootElement.GetProperty("runtimeUnlockAllowed").GetBoolean());
        Assert.IsFalse(doc.RootElement.GetProperty("publicReleaseAllowed").GetBoolean());
        Assert.IsFalse(doc.RootElement.GetProperty("storeSubmissionAllowed").GetBoolean());
    }

    [TestMethod]
    public void FinalDecisionIsInternalCandidateRuntimeReentryPlanningReady()
    {
        using var doc = ReadJson(ConsolidatedPath);
        Assert.AreEqual("INTERNAL_CANDIDATE_STABILIZED_RUNTIME_REENTRY_PLANNING_READY", doc.RootElement.GetProperty("decision").GetString());
        Assert.IsTrue(doc.RootElement.GetProperty("noBlindRelease").GetBoolean());
        Assert.IsTrue(doc.RootElement.GetProperty("noStoreSubmission").GetBoolean());
    }
}
