using System.Text.Json;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace OneBrain.Safety.Tests;

[TestClass]
[TestCategory("PackageFreezeReadinessRetry")]
[TestCategory("M719")]
[TestCategory("M720")]
[TestCategory("M721")]
public sealed class NodalOsPackageFreezeReadinessRetryM719M721Tests
{
    private const string M719ReportPath = "docs/reports/m719-human-evidence-intake-final-attempt.md";
    private const string M720ReportPath = "docs/reports/m720-privacy-support-docs-url-finalization-retry.md";
    private const string M721ReportPath = "docs/reports/m721-package-freeze-readiness-recheck.md";

    private const string HumanEvidenceAttemptPath = "artifacts/agent-operations/m719/human-evidence-intake-final-attempt.json";
    private const string HumanEvidenceFieldStatusPath = "artifacts/agent-operations/m719/human-evidence-field-status.json";
    private const string ScreenshotAssetStatusPath = "artifacts/agent-operations/m719/screenshot-asset-final-input-status.json";
    private const string RuntimeDevtoolsStatusPath = "artifacts/agent-operations/m719/runtime-devtools-final-input-status.json";
    private const string PermissionWarningStatusPath = "artifacts/agent-operations/m719/permission-warning-final-input-status.json";
    private const string EvidenceRedactionPath = "artifacts/agent-operations/m719/evidence-redaction-final-input-proof.json";
    private const string M719GoNoGoPath = "artifacts/agent-operations/m719/m719-go-no-go.json";

    private const string UrlFinalizationPath = "artifacts/agent-operations/m720/privacy-support-docs-url-finalization-retry.json";
    private const string PrivacyUrlPath = "artifacts/agent-operations/m720/privacy-url-readiness.json";
    private const string SupportUrlPath = "artifacts/agent-operations/m720/support-url-readiness.json";
    private const string DocsUrlPath = "artifacts/agent-operations/m720/docs-url-readiness.json";
    private const string UrlBlockerPath = "artifacts/agent-operations/m720/url-blocker-final-status.json";
    private const string StoreUrlRiskPath = "artifacts/agent-operations/m720/store-url-final-risk-register.json";
    private const string M720GoNoGoPath = "artifacts/agent-operations/m720/m720-go-no-go.json";

    private const string FreezeRecheckPath = "artifacts/agent-operations/m721/package-freeze-readiness-recheck.json";
    private const string P0RecheckPath = "artifacts/agent-operations/m721/p0-blocker-recheck.json";
    private const string P1P2RecheckPath = "artifacts/agent-operations/m721/p1-p2-blocker-recheck.json";
    private const string PublicPackageFreezePath = "artifacts/agent-operations/m721/public-package-freeze-go-no-go.json";
    private const string PublicReleaseNoGoPath = "artifacts/agent-operations/m721/public-release-no-go-proof.json";
    private const string ChromeWebStoreNoGoPath = "artifacts/agent-operations/m721/chrome-web-store-no-go-proof.json";
    private const string NextMilestonePath = "artifacts/agent-operations/m721/next-milestone-recommendation.json";
    private const string M721GoNoGoPath = "artifacts/agent-operations/m721/m721-go-no-go.json";

    private const string ConsolidatedPath = "artifacts/agent-operations/m719-m721/package-freeze-readiness-retry-go-no-go.json";

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
    public void M719ArtifactsExist()
    {
        foreach (var path in new[]
        {
            M719ReportPath, HumanEvidenceAttemptPath, HumanEvidenceFieldStatusPath,
            ScreenshotAssetStatusPath, RuntimeDevtoolsStatusPath, PermissionWarningStatusPath,
            EvidenceRedactionPath, M719GoNoGoPath
        })
            Assert.IsTrue(File.Exists(FullPath(path)), path);
    }

    [TestMethod]
    public void M720ArtifactsExist()
    {
        foreach (var path in new[]
        {
            M720ReportPath, UrlFinalizationPath, PrivacyUrlPath, SupportUrlPath,
            DocsUrlPath, UrlBlockerPath, StoreUrlRiskPath, M720GoNoGoPath
        })
            Assert.IsTrue(File.Exists(FullPath(path)), path);
    }

    [TestMethod]
    public void M721ArtifactsExist()
    {
        foreach (var path in new[]
        {
            M721ReportPath, FreezeRecheckPath, P0RecheckPath, P1P2RecheckPath,
            PublicPackageFreezePath, PublicReleaseNoGoPath, ChromeWebStoreNoGoPath,
            NextMilestonePath, M721GoNoGoPath, ConsolidatedPath
        })
            Assert.IsTrue(File.Exists(FullPath(path)), path);
    }

    [TestMethod]
    public void ConsolidatedDecisionIsAllowedAndConditionalP0Pending()
    {
        using var doc = ReadJson(ConsolidatedPath);
        var decision = doc.RootElement.GetProperty("decision").GetString();
        CollectionAssert.Contains(new[]
        {
            "PACKAGE_FREEZE_READINESS_READY",
            "PACKAGE_FREEZE_READINESS_CONDITIONAL_P0_PENDING",
            "PACKAGE_FREEZE_READINESS_BLOCKED_REMEDIATION_REQUIRED"
        }, decision);

        Assert.AreEqual("PACKAGE_FREEZE_READINESS_CONDITIONAL_P0_PENDING", decision);
        Assert.AreEqual("NODAL OS", doc.RootElement.GetProperty("activeProject").GetString());
        Assert.IsTrue(doc.RootElement.GetProperty("nodrixOutOfScope").GetBoolean());
        Assert.IsTrue(doc.RootElement.GetProperty("internalCandidateReady").GetBoolean());
        Assert.IsTrue(doc.RootElement.GetProperty("publicBuildCandidateReady").GetBoolean());
        Assert.IsTrue(doc.RootElement.GetProperty("publicStagingReady").GetBoolean());
    }

    [TestMethod]
    public void OpenP0BlockersKeepFreezeAndStoreNoGo()
    {
        using var doc = ReadJson(ConsolidatedPath);
        Assert.IsFalse(doc.RootElement.GetProperty("p0BlockersClosed").GetBoolean());
        Assert.IsFalse(doc.RootElement.GetProperty("humanEvidenceReady").GetBoolean());
        Assert.IsFalse(doc.RootElement.GetProperty("screenshotsReady").GetBoolean());
        Assert.IsFalse(doc.RootElement.GetProperty("privacyUrlReady").GetBoolean());
        Assert.IsFalse(doc.RootElement.GetProperty("supportUrlReady").GetBoolean());
        Assert.IsFalse(doc.RootElement.GetProperty("publicPackageFreezeReady").GetBoolean());
        Assert.IsFalse(doc.RootElement.GetProperty("publicReleaseReady").GetBoolean());
        Assert.IsFalse(doc.RootElement.GetProperty("chromeWebStoreReady").GetBoolean());
    }

    [TestMethod]
    public void MissingUrlsKeepChromeWebStoreNoGo()
    {
        using var privacy = ReadJson(PrivacyUrlPath);
        using var support = ReadJson(SupportUrlPath);
        using var store = ReadJson(ChromeWebStoreNoGoPath);
        Assert.IsFalse(privacy.RootElement.GetProperty("privacyUrlReady").GetBoolean());
        Assert.IsFalse(support.RootElement.GetProperty("supportUrlReady").GetBoolean());
        Assert.IsFalse(store.RootElement.GetProperty("chromeWebStoreReady").GetBoolean());
    }

    [TestMethod]
    public void CapabilitiesRemainDisabled()
    {
        foreach (var path in new[] { M719GoNoGoPath, M720GoNoGoPath, M721GoNoGoPath, ConsolidatedPath })
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
    public void PublicReleaseRemainsNoGo()
    {
        using var release = ReadJson(PublicReleaseNoGoPath);
        using var package = ReadJson(PublicPackageFreezePath);
        Assert.IsFalse(release.RootElement.GetProperty("publicReleaseReady").GetBoolean());
        Assert.IsFalse(package.RootElement.GetProperty("publicPackageFreezeReady").GetBoolean());
    }
}
