using System.Text.Json;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace OneBrain.Safety.Tests;

[TestClass]
[TestCategory("P0BlockerClosure")]
[TestCategory("M722")]
[TestCategory("M723")]
[TestCategory("M724")]
public sealed class NodalOsP0BlockerClosureM722M724Tests
{
    private const string M722ReportPath = "docs/reports/m722-evidence-screenshot-asset-intake.md";
    private const string M723ReportPath = "docs/reports/m723-privacy-support-docs-url-closure.md";
    private const string M724ReportPath = "docs/reports/m724-freeze-final-decision.md";

    private const string EvidenceIntakePath = "artifacts/agent-operations/m722/evidence-screenshot-asset-intake.json";
    private const string HumanEvidenceStatusPath = "artifacts/agent-operations/m722/human-evidence-intake-status.json";
    private const string ScreenshotAssetStatusPath = "artifacts/agent-operations/m722/screenshot-asset-intake-status.json";
    private const string RuntimeDevtoolsStatusPath = "artifacts/agent-operations/m722/runtime-devtools-intake-status.json";
    private const string PermissionWarningStatusPath = "artifacts/agent-operations/m722/permission-warning-intake-status.json";
    private const string EvidenceRedactionPath = "artifacts/agent-operations/m722/evidence-redaction-intake-proof.json";
    private const string M722GoNoGoPath = "artifacts/agent-operations/m722/m722-go-no-go.json";

    private const string UrlClosurePath = "artifacts/agent-operations/m723/privacy-support-docs-url-closure.json";
    private const string PrivacyUrlPath = "artifacts/agent-operations/m723/privacy-url-closure-status.json";
    private const string SupportUrlPath = "artifacts/agent-operations/m723/support-url-closure-status.json";
    private const string DocsUrlPath = "artifacts/agent-operations/m723/docs-url-closure-status.json";
    private const string UrlBlockerPath = "artifacts/agent-operations/m723/url-blocker-closure-status.json";
    private const string UrlChecklistPath = "artifacts/agent-operations/m723/url-must-provide-checklist.json";
    private const string M723GoNoGoPath = "artifacts/agent-operations/m723/m723-go-no-go.json";

    private const string FreezeDecisionPath = "artifacts/agent-operations/m724/freeze-final-decision.json";
    private const string P0FinalStatusPath = "artifacts/agent-operations/m724/p0-blocker-final-status.json";
    private const string P1P2FinalStatusPath = "artifacts/agent-operations/m724/p1-p2-blocker-final-status.json";
    private const string PublicPackageFreezePath = "artifacts/agent-operations/m724/public-package-freeze-go-no-go.json";
    private const string PublicReleaseNoGoPath = "artifacts/agent-operations/m724/public-release-no-go-proof.json";
    private const string ChromeWebStoreNoGoPath = "artifacts/agent-operations/m724/chrome-web-store-no-go-proof.json";
    private const string MustProvidePath = "artifacts/agent-operations/m724/must-provide-before-freeze.json";
    private const string NextMilestonePath = "artifacts/agent-operations/m724/next-milestone-recommendation.json";
    private const string M724GoNoGoPath = "artifacts/agent-operations/m724/m724-go-no-go.json";

    private const string ConsolidatedPath = "artifacts/agent-operations/m722-m724/p0-blocker-closure-freeze-go-no-go.json";

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
    public void M722ArtifactsExist()
    {
        foreach (var path in new[]
        {
            M722ReportPath, EvidenceIntakePath, HumanEvidenceStatusPath,
            ScreenshotAssetStatusPath, RuntimeDevtoolsStatusPath, PermissionWarningStatusPath,
            EvidenceRedactionPath, M722GoNoGoPath
        })
            Assert.IsTrue(File.Exists(FullPath(path)), path);
    }

    [TestMethod]
    public void M723ArtifactsExist()
    {
        foreach (var path in new[]
        {
            M723ReportPath, UrlClosurePath, PrivacyUrlPath, SupportUrlPath,
            DocsUrlPath, UrlBlockerPath, UrlChecklistPath, M723GoNoGoPath
        })
            Assert.IsTrue(File.Exists(FullPath(path)), path);
    }

    [TestMethod]
    public void M724ArtifactsExist()
    {
        foreach (var path in new[]
        {
            M724ReportPath, FreezeDecisionPath, P0FinalStatusPath, P1P2FinalStatusPath,
            PublicPackageFreezePath, PublicReleaseNoGoPath, ChromeWebStoreNoGoPath,
            MustProvidePath, NextMilestonePath, M724GoNoGoPath, ConsolidatedPath
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
    public void OpenP0BlockersKeepFreezeNoGo()
    {
        using var doc = ReadJson(ConsolidatedPath);
        Assert.IsFalse(doc.RootElement.GetProperty("humanEvidenceReady").GetBoolean());
        Assert.IsFalse(doc.RootElement.GetProperty("screenshotsReady").GetBoolean());
        Assert.IsFalse(doc.RootElement.GetProperty("privacyUrlReady").GetBoolean());
        Assert.IsFalse(doc.RootElement.GetProperty("supportUrlReady").GetBoolean());
        Assert.IsFalse(doc.RootElement.GetProperty("p0BlockersClosed").GetBoolean());
        Assert.IsFalse(doc.RootElement.GetProperty("publicPackageFreezeReady").GetBoolean());
    }

    [TestMethod]
    public void MissingEvidenceAndScreenshotsKeepFreezeNoGo()
    {
        using var human = ReadJson(HumanEvidenceStatusPath);
        using var screenshots = ReadJson(ScreenshotAssetStatusPath);
        using var package = ReadJson(PublicPackageFreezePath);

        Assert.AreEqual("partial", human.RootElement.GetProperty("humanEvidence").GetString());
        Assert.IsFalse(human.RootElement.GetProperty("humanEvidenceReady").GetBoolean());
        Assert.IsFalse(screenshots.RootElement.GetProperty("screenshotsReady").GetBoolean());
        Assert.IsFalse(package.RootElement.GetProperty("publicPackageFreezeReady").GetBoolean());
    }

    [TestMethod]
    public void MissingUrlsKeepStoreNoGo()
    {
        using var privacy = ReadJson(PrivacyUrlPath);
        using var support = ReadJson(SupportUrlPath);
        using var store = ReadJson(ChromeWebStoreNoGoPath);

        Assert.IsFalse(privacy.RootElement.GetProperty("privacyUrlReady").GetBoolean());
        Assert.IsFalse(support.RootElement.GetProperty("supportUrlReady").GetBoolean());
        Assert.IsFalse(store.RootElement.GetProperty("chromeWebStoreReady").GetBoolean());
    }

    [TestMethod]
    public void PublicReleaseAndCapabilitiesRemainNoGoAndDisabled()
    {
        foreach (var path in new[] { M722GoNoGoPath, M723GoNoGoPath, M724GoNoGoPath, ConsolidatedPath })
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
