using System.Text.Json;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace OneBrain.Safety.Tests;

[TestClass]
[TestCategory("P0ManualCompletion")]
[TestCategory("M725")]
[TestCategory("M726")]
[TestCategory("M727")]
public sealed class NodalOsP0ManualCompletionM725M727Tests
{
    private const string M725ReportPath = "docs/reports/m725-manual-url-evidence-intake.md";
    private const string M726ReportPath = "docs/reports/m726-screenshot-asset-intake.md";
    private const string M727ReportPath = "docs/reports/m727-freeze-gate-final-retry.md";

    private const string ManualUrlEvidencePath = "artifacts/agent-operations/m725/manual-url-evidence-intake.json";
    private const string PrivacyUrlPath = "artifacts/agent-operations/m725/privacy-url-intake.json";
    private const string SupportUrlPath = "artifacts/agent-operations/m725/support-url-intake.json";
    private const string DocsUrlPath = "artifacts/agent-operations/m725/docs-url-intake.json";
    private const string HumanChromeEvidencePath = "artifacts/agent-operations/m725/human-chrome-evidence-intake.json";
    private const string EvidenceRedactionPath = "artifacts/agent-operations/m725/evidence-redaction-intake.json";
    private const string M725GoNoGoPath = "artifacts/agent-operations/m725/m725-go-no-go.json";

    private const string ScreenshotAssetPath = "artifacts/agent-operations/m726/screenshot-asset-intake.json";
    private const string ExtensionLoadedScreenshotPath = "artifacts/agent-operations/m726/extension-loaded-screenshot-intake.json";
    private const string SidepanelConnectedScreenshotPath = "artifacts/agent-operations/m726/sidepanel-connected-screenshot-intake.json";
    private const string RuntimeTabScreenshotPath = "artifacts/agent-operations/m726/runtime-tab-screenshot-intake.json";
    private const string ServiceWorkerDevtoolsScreenshotPath = "artifacts/agent-operations/m726/service-worker-devtools-screenshot-intake.json";
    private const string PermissionWarningScreenshotPath = "artifacts/agent-operations/m726/permission-warning-screenshot-intake.json";
    private const string StoreAssetFinalStatusPath = "artifacts/agent-operations/m726/store-asset-final-status.json";
    private const string M726GoNoGoPath = "artifacts/agent-operations/m726/m726-go-no-go.json";

    private const string FreezeGateRetryPath = "artifacts/agent-operations/m727/freeze-gate-final-retry.json";
    private const string P0FinalRetryStatusPath = "artifacts/agent-operations/m727/p0-blocker-final-retry-status.json";
    private const string PublicPackageFreezePath = "artifacts/agent-operations/m727/public-package-freeze-final-go-no-go.json";
    private const string PublicReleaseNoGoPath = "artifacts/agent-operations/m727/public-release-no-go-proof.json";
    private const string ChromeWebStoreNoGoPath = "artifacts/agent-operations/m727/chrome-web-store-no-go-proof.json";
    private const string FinalMustProvidePath = "artifacts/agent-operations/m727/final-must-provide-before-freeze.json";
    private const string NextMilestonePath = "artifacts/agent-operations/m727/next-milestone-recommendation.json";
    private const string M727GoNoGoPath = "artifacts/agent-operations/m727/m727-go-no-go.json";

    private const string ConsolidatedPath = "artifacts/agent-operations/m725-m727/p0-manual-completion-freeze-go-no-go.json";

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
    public void M725ArtifactsExist()
    {
        foreach (var path in new[]
        {
            M725ReportPath, ManualUrlEvidencePath, PrivacyUrlPath, SupportUrlPath,
            DocsUrlPath, HumanChromeEvidencePath, EvidenceRedactionPath, M725GoNoGoPath
        })
            Assert.IsTrue(File.Exists(FullPath(path)), path);
    }

    [TestMethod]
    public void M726ArtifactsExist()
    {
        foreach (var path in new[]
        {
            M726ReportPath, ScreenshotAssetPath, ExtensionLoadedScreenshotPath,
            SidepanelConnectedScreenshotPath, RuntimeTabScreenshotPath,
            ServiceWorkerDevtoolsScreenshotPath, PermissionWarningScreenshotPath,
            StoreAssetFinalStatusPath, M726GoNoGoPath
        })
            Assert.IsTrue(File.Exists(FullPath(path)), path);
    }

    [TestMethod]
    public void M727ArtifactsExist()
    {
        foreach (var path in new[]
        {
            M727ReportPath, FreezeGateRetryPath, P0FinalRetryStatusPath,
            PublicPackageFreezePath, PublicReleaseNoGoPath, ChromeWebStoreNoGoPath,
            FinalMustProvidePath, NextMilestonePath, M727GoNoGoPath, ConsolidatedPath
        })
            Assert.IsTrue(File.Exists(FullPath(path)), path);
    }

    [TestMethod]
    public void ConsolidatedDecisionIsAllowedAndPending()
    {
        using var doc = ReadJson(ConsolidatedPath);
        var decision = doc.RootElement.GetProperty("decision").GetString();
        CollectionAssert.Contains(new[]
        {
            "P0_BLOCKERS_CLOSED_FREEZE_READY",
            "P0_BLOCKERS_STILL_PENDING_FREEZE_NO_GO",
            "P0_BLOCKER_REMEDIATION_REQUIRED"
        }, decision);

        Assert.AreEqual("P0_BLOCKERS_STILL_PENDING_FREEZE_NO_GO", decision);
        Assert.AreEqual("NODAL OS", doc.RootElement.GetProperty("activeProject").GetString());
        Assert.IsTrue(doc.RootElement.GetProperty("nodrixOutOfScope").GetBoolean());
        Assert.IsTrue(doc.RootElement.GetProperty("internalCandidateReady").GetBoolean());
        Assert.IsTrue(doc.RootElement.GetProperty("publicBuildCandidateReady").GetBoolean());
        Assert.IsTrue(doc.RootElement.GetProperty("publicStagingReady").GetBoolean());
    }

    [TestMethod]
    public void MissingUrlsKeepChromeWebStoreNoGo()
    {
        using var doc = ReadJson(ConsolidatedPath);
        using var privacy = ReadJson(PrivacyUrlPath);
        using var support = ReadJson(SupportUrlPath);

        Assert.IsFalse(doc.RootElement.GetProperty("privacyUrlReady").GetBoolean());
        Assert.IsFalse(doc.RootElement.GetProperty("supportUrlReady").GetBoolean());
        Assert.IsFalse(doc.RootElement.GetProperty("chromeWebStoreReady").GetBoolean());
        Assert.IsFalse(privacy.RootElement.GetProperty("privacyUrlReady").GetBoolean());
        Assert.IsFalse(support.RootElement.GetProperty("supportUrlReady").GetBoolean());
    }

    [TestMethod]
    public void MissingScreenshotsKeepPublicPackageFreezeNoGo()
    {
        using var doc = ReadJson(ConsolidatedPath);
        using var screenshots = ReadJson(ScreenshotAssetPath);
        using var package = ReadJson(PublicPackageFreezePath);

        Assert.IsFalse(doc.RootElement.GetProperty("screenshotsReady").GetBoolean());
        Assert.IsFalse(screenshots.RootElement.GetProperty("screenshotsReady").GetBoolean());
        Assert.IsFalse(package.RootElement.GetProperty("publicPackageFreezeReady").GetBoolean());
    }

    [TestMethod]
    public void PartialHumanEvidenceKeepsPublicPackageFreezeNoGo()
    {
        using var doc = ReadJson(ConsolidatedPath);
        using var evidence = ReadJson(HumanChromeEvidencePath);
        using var package = ReadJson(PublicPackageFreezePath);

        Assert.IsFalse(doc.RootElement.GetProperty("humanEvidenceReady").GetBoolean());
        Assert.AreEqual("partial", evidence.RootElement.GetProperty("humanChromeEvidence").GetString());
        Assert.IsFalse(package.RootElement.GetProperty("publicPackageFreezeReady").GetBoolean());
    }

    [TestMethod]
    public void PublicReleaseAndCapabilitiesRemainNoGoAndDisabled()
    {
        foreach (var path in new[] { M725GoNoGoPath, M726GoNoGoPath, M727GoNoGoPath, ConsolidatedPath })
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
}
