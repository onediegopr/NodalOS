using System.Text.Json;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace OneBrain.Safety.Tests;

[TestClass]
[TestCategory("PackageFreezeFinalReadiness")]
[TestCategory("M713")]
[TestCategory("M714")]
[TestCategory("M715")]
public sealed class NodalOsPackageFreezeFinalReadinessM713M715Tests
{
    private const string M713ReportPath = "docs/reports/m713-evidence-screenshot-asset-completion.md";
    private const string M714ReportPath = "docs/reports/m714-privacy-support-docs-url-closure.md";
    private const string M715ReportPath = "docs/reports/m715-package-freeze-final-readiness.md";

    private const string EvidenceCompletionPath = "artifacts/agent-operations/m713/evidence-screenshot-asset-completion.json";
    private const string HumanEvidencePath = "artifacts/agent-operations/m713/human-evidence-final-status.json";
    private const string RequiredScreenshotsPath = "artifacts/agent-operations/m713/required-screenshots-final-status.json";
    private const string ExtensionScreenshotPath = "artifacts/agent-operations/m713/extension-loaded-screenshot-final-status.json";
    private const string SidepanelScreenshotPath = "artifacts/agent-operations/m713/sidepanel-connected-screenshot-final-status.json";
    private const string RuntimeDevtoolsScreenshotPath = "artifacts/agent-operations/m713/runtime-devtools-screenshot-final-status.json";
    private const string PermissionWarningScreenshotPath = "artifacts/agent-operations/m713/permission-warning-screenshot-final-status.json";
    private const string RedactionProofPath = "artifacts/agent-operations/m713/evidence-redaction-final-proof.json";
    private const string M713GoNoGoPath = "artifacts/agent-operations/m713/m713-go-no-go.json";

    private const string UrlClosurePath = "artifacts/agent-operations/m714/privacy-support-docs-url-closure.json";
    private const string PrivacyUrlPath = "artifacts/agent-operations/m714/privacy-url-final-status.json";
    private const string SupportUrlPath = "artifacts/agent-operations/m714/support-url-final-status.json";
    private const string DocsUrlPath = "artifacts/agent-operations/m714/docs-url-final-status.json";
    private const string UrlBlockerClosurePath = "artifacts/agent-operations/m714/url-blocker-closure.json";
    private const string StoreUrlRiskPath = "artifacts/agent-operations/m714/store-url-final-risk-register.json";
    private const string M714GoNoGoPath = "artifacts/agent-operations/m714/m714-go-no-go.json";

    private const string FreezeReadinessPath = "artifacts/agent-operations/m715/package-freeze-final-readiness.json";
    private const string StoreBlockerClosurePath = "artifacts/agent-operations/m715/store-submission-blocker-closure.json";
    private const string FinalFreezeChecklistPath = "artifacts/agent-operations/m715/final-freeze-checklist.json";
    private const string ChecksumReadinessPath = "artifacts/agent-operations/m715/checksum-final-readiness.json";
    private const string PublicPackageFreezePath = "artifacts/agent-operations/m715/public-package-freeze-go-no-go.json";
    private const string PublicReleaseNoGoPath = "artifacts/agent-operations/m715/public-release-no-go-proof.json";
    private const string ChromeWebStoreNoGoPath = "artifacts/agent-operations/m715/chrome-web-store-no-go-proof.json";
    private const string NextMilestonePath = "artifacts/agent-operations/m715/next-milestone-recommendation.json";
    private const string M715GoNoGoPath = "artifacts/agent-operations/m715/m715-go-no-go.json";

    private const string ConsolidatedPath = "artifacts/agent-operations/m713-m715/package-freeze-final-readiness-go-no-go.json";

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
    public void M713ArtifactsExist()
    {
        foreach (var path in new[]
        {
            M713ReportPath, EvidenceCompletionPath, HumanEvidencePath, RequiredScreenshotsPath,
            ExtensionScreenshotPath, SidepanelScreenshotPath, RuntimeDevtoolsScreenshotPath,
            PermissionWarningScreenshotPath, RedactionProofPath, M713GoNoGoPath
        })
            Assert.IsTrue(File.Exists(FullPath(path)), path);
    }

    [TestMethod]
    public void M714ArtifactsExist()
    {
        foreach (var path in new[]
        {
            M714ReportPath, UrlClosurePath, PrivacyUrlPath, SupportUrlPath, DocsUrlPath,
            UrlBlockerClosurePath, StoreUrlRiskPath, M714GoNoGoPath
        })
            Assert.IsTrue(File.Exists(FullPath(path)), path);
    }

    [TestMethod]
    public void M715ArtifactsExist()
    {
        foreach (var path in new[]
        {
            M715ReportPath, FreezeReadinessPath, StoreBlockerClosurePath, FinalFreezeChecklistPath,
            ChecksumReadinessPath, PublicPackageFreezePath, PublicReleaseNoGoPath,
            ChromeWebStoreNoGoPath, NextMilestonePath, M715GoNoGoPath, ConsolidatedPath
        })
            Assert.IsTrue(File.Exists(FullPath(path)), path);
    }

    [TestMethod]
    public void ConsolidatedDecisionIsConditionalBlockersPending()
    {
        using var doc = ReadJson(ConsolidatedPath);
        var decision = doc.RootElement.GetProperty("decision").GetString();
        CollectionAssert.Contains(new[]
        {
            "PACKAGE_FREEZE_FINAL_READINESS_READY",
            "PACKAGE_FREEZE_FINAL_READINESS_CONDITIONAL_BLOCKERS_PENDING",
            "PACKAGE_FREEZE_FINAL_READINESS_BLOCKED_REMEDIATION_REQUIRED"
        }, decision);

        Assert.AreEqual("PACKAGE_FREEZE_FINAL_READINESS_CONDITIONAL_BLOCKERS_PENDING", decision);
        Assert.IsTrue(doc.RootElement.GetProperty("internalCandidateReady").GetBoolean());
        Assert.IsTrue(doc.RootElement.GetProperty("publicBuildCandidateReady").GetBoolean());
        Assert.IsTrue(doc.RootElement.GetProperty("publicStagingReady").GetBoolean());
        Assert.IsFalse(doc.RootElement.GetProperty("p0BlockersClosed").GetBoolean());
        Assert.IsFalse(doc.RootElement.GetProperty("publicPackageFreezeReady").GetBoolean());
        Assert.IsFalse(doc.RootElement.GetProperty("publicReleaseReady").GetBoolean());
        Assert.IsFalse(doc.RootElement.GetProperty("chromeWebStoreReady").GetBoolean());
    }

    [TestMethod]
    public void CapabilitiesRemainDisabled()
    {
        foreach (var path in new[] { M713GoNoGoPath, M714GoNoGoPath, M715GoNoGoPath, ConsolidatedPath })
        {
            using var doc = ReadJson(path);
            Assert.IsFalse(doc.RootElement.GetProperty("publicReleaseReady").GetBoolean(), path);
            Assert.IsFalse(doc.RootElement.GetProperty("chromeWebStoreReady").GetBoolean(), path);
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
    public void OpenP0BlockersKeepPublicPackageFreezeNoGo()
    {
        using var doc = ReadJson(ConsolidatedPath);
        Assert.IsFalse(doc.RootElement.GetProperty("p0BlockersClosed").GetBoolean());
        Assert.IsFalse(doc.RootElement.GetProperty("publicPackageFreezeReady").GetBoolean());
    }

    [TestMethod]
    public void MissingPrivacyAndSupportUrlsKeepStoreNoGo()
    {
        using var privacy = ReadJson(PrivacyUrlPath);
        using var support = ReadJson(SupportUrlPath);
        using var consolidated = ReadJson(ConsolidatedPath);
        Assert.IsFalse(privacy.RootElement.GetProperty("privacyUrlReady").GetBoolean());
        Assert.IsFalse(support.RootElement.GetProperty("supportUrlReady").GetBoolean());
        Assert.IsFalse(consolidated.RootElement.GetProperty("chromeWebStoreReady").GetBoolean());
    }

    [TestMethod]
    public void PublicReleaseRemainsNoGo()
    {
        using var release = ReadJson(PublicReleaseNoGoPath);
        using var store = ReadJson(ChromeWebStoreNoGoPath);
        Assert.IsFalse(release.RootElement.GetProperty("publicReleaseReady").GetBoolean());
        Assert.IsFalse(store.RootElement.GetProperty("chromeWebStoreReady").GetBoolean());
    }
}
