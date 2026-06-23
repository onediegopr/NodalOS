using System.Text.Json;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace OneBrain.Safety.Tests;

[TestClass]
[TestCategory("ExternalAuditPrep")]
[TestCategory("M707")]
[TestCategory("M708")]
[TestCategory("M709")]
public sealed class NodalOsExternalAuditPrepM707M709Tests
{
    private const string M707ReportPath = "docs/reports/m707-evidence-screenshot-asset-completion.md";
    private const string M708ReportPath = "docs/reports/m708-privacy-support-docs-url-closure.md";
    private const string M709ReportPath = "docs/reports/m709-external-audit-prep-freeze-candidate-review-pack.md";

    private const string EvidenceAssetCompletionPath = "artifacts/agent-operations/m707/evidence-screenshot-asset-completion.json";
    private const string RequiredScreenshotStatusPath = "artifacts/agent-operations/m707/required-screenshot-status.json";
    private const string ChromeExtensionScreenshotPath = "artifacts/agent-operations/m707/chrome-extension-loaded-screenshot-status.json";
    private const string SidepanelScreenshotPath = "artifacts/agent-operations/m707/sidepanel-connected-screenshot-status.json";
    private const string RuntimeDevtoolsScreenshotPath = "artifacts/agent-operations/m707/runtime-devtools-screenshot-status.json";
    private const string PermissionWarningScreenshotPath = "artifacts/agent-operations/m707/permission-warning-screenshot-status.json";
    private const string RedactionStatusPath = "artifacts/agent-operations/m707/sensitive-data-redaction-status.json";
    private const string M707GoNoGoPath = "artifacts/agent-operations/m707/m707-go-no-go.json";

    private const string UrlClosurePath = "artifacts/agent-operations/m708/privacy-support-docs-url-closure.json";
    private const string PrivacyUrlPath = "artifacts/agent-operations/m708/privacy-url-status.json";
    private const string SupportUrlPath = "artifacts/agent-operations/m708/support-url-status.json";
    private const string DocsUrlPath = "artifacts/agent-operations/m708/docs-url-status.json";
    private const string UrlGapRegisterPath = "artifacts/agent-operations/m708/store-url-gap-register.json";
    private const string UrlRiskRegisterPath = "artifacts/agent-operations/m708/url-readiness-risk-register.json";
    private const string M708GoNoGoPath = "artifacts/agent-operations/m708/m708-go-no-go.json";

    private const string ExternalAuditPrepPath = "artifacts/agent-operations/m709/external-audit-prep.json";
    private const string FreezeCandidateReviewPath = "artifacts/agent-operations/m709/freeze-candidate-review-pack.json";
    private const string ClaudeAuditPromptPath = "artifacts/agent-operations/m709/claude-audit-prompt-pack.json";
    private const string FreezeReadinessReviewPath = "artifacts/agent-operations/m709/public-package-freeze-readiness-review.json";
    private const string StoreBlockerRegisterPath = "artifacts/agent-operations/m709/store-submission-blocker-register.json";
    private const string PublicReleaseNoGoPath = "artifacts/agent-operations/m709/public-release-no-go-proof.json";
    private const string ChromeWebStoreNoGoPath = "artifacts/agent-operations/m709/chrome-web-store-no-go-proof.json";
    private const string NextMilestonePath = "artifacts/agent-operations/m709/next-milestone-recommendation.json";
    private const string M709GoNoGoPath = "artifacts/agent-operations/m709/m709-go-no-go.json";

    private const string ConsolidatedPath = "artifacts/agent-operations/m707-m709/external-audit-prep-go-no-go.json";

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
    public void M707ArtifactsExist()
    {
        foreach (var path in new[]
        {
            M707ReportPath, EvidenceAssetCompletionPath, RequiredScreenshotStatusPath,
            ChromeExtensionScreenshotPath, SidepanelScreenshotPath, RuntimeDevtoolsScreenshotPath,
            PermissionWarningScreenshotPath, RedactionStatusPath, M707GoNoGoPath
        })
            Assert.IsTrue(File.Exists(FullPath(path)), path);
    }

    [TestMethod]
    public void M708ArtifactsExist()
    {
        foreach (var path in new[]
        {
            M708ReportPath, UrlClosurePath, PrivacyUrlPath, SupportUrlPath, DocsUrlPath,
            UrlGapRegisterPath, UrlRiskRegisterPath, M708GoNoGoPath
        })
            Assert.IsTrue(File.Exists(FullPath(path)), path);
    }

    [TestMethod]
    public void M709ArtifactsExist()
    {
        foreach (var path in new[]
        {
            M709ReportPath, ExternalAuditPrepPath, FreezeCandidateReviewPath, ClaudeAuditPromptPath,
            FreezeReadinessReviewPath, StoreBlockerRegisterPath, PublicReleaseNoGoPath,
            ChromeWebStoreNoGoPath, NextMilestonePath, M709GoNoGoPath, ConsolidatedPath
        })
            Assert.IsTrue(File.Exists(FullPath(path)), path);
    }

    [TestMethod]
    public void ConsolidatedDecisionIsExternalAuditReadyWithTrackedAssets()
    {
        using var doc = ReadJson(ConsolidatedPath);
        var decision = doc.RootElement.GetProperty("decision").GetString();
        CollectionAssert.Contains(new[]
        {
            "EXTERNAL_AUDIT_PREP_READY_ASSETS_TRACKED",
            "EXTERNAL_AUDIT_PREP_CONDITIONAL_URLS_ASSETS_PENDING",
            "EXTERNAL_AUDIT_PREP_BLOCKED_REMEDIATION_REQUIRED"
        }, decision);

        Assert.AreEqual("EXTERNAL_AUDIT_PREP_READY_ASSETS_TRACKED", decision);
        Assert.IsTrue(doc.RootElement.GetProperty("internalCandidateReady").GetBoolean());
        Assert.IsTrue(doc.RootElement.GetProperty("publicBuildCandidateReady").GetBoolean());
        Assert.IsTrue(doc.RootElement.GetProperty("publicStagingReady").GetBoolean());
        Assert.IsTrue(doc.RootElement.GetProperty("externalAuditPackReady").GetBoolean());
        Assert.IsTrue(doc.RootElement.GetProperty("claudeAuditPromptPackReady").GetBoolean());
        Assert.IsFalse(doc.RootElement.GetProperty("publicPackageFreezeReady").GetBoolean());
        Assert.IsFalse(doc.RootElement.GetProperty("publicReleaseReady").GetBoolean());
        Assert.IsFalse(doc.RootElement.GetProperty("chromeWebStoreReady").GetBoolean());
    }

    [TestMethod]
    public void CapabilitiesRemainDisabled()
    {
        foreach (var path in new[] { M707GoNoGoPath, M708GoNoGoPath, M709GoNoGoPath, ConsolidatedPath })
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
    public void PendingPrivacySupportUrlsKeepStoreNoGo()
    {
        using var urls = ReadJson(UrlClosurePath);
        using var consolidated = ReadJson(ConsolidatedPath);
        Assert.AreEqual("pending", urls.RootElement.GetProperty("privacyUrlStatus").GetString());
        Assert.AreEqual("pending", urls.RootElement.GetProperty("supportUrlStatus").GetString());
        Assert.IsFalse(urls.RootElement.GetProperty("privacySupportUrlsReady").GetBoolean());
        Assert.IsFalse(consolidated.RootElement.GetProperty("chromeWebStoreReady").GetBoolean());
    }

    [TestMethod]
    public void ExternalAuditAndClaudePromptPacksAreReady()
    {
        using var external = ReadJson(ExternalAuditPrepPath);
        using var prompt = ReadJson(ClaudeAuditPromptPath);
        Assert.IsTrue(external.RootElement.GetProperty("externalAuditPackReady").GetBoolean());
        Assert.IsTrue(prompt.RootElement.GetProperty("claudeAuditPromptPackReady").GetBoolean());
        StringAssert.Contains(prompt.RootElement.GetProperty("auditorPrompt").GetString(), "Public Release and Chrome Web Store remain NO-GO");
    }

    [TestMethod]
    public void StoreSubmissionBlockersKeepReleaseNoGo()
    {
        using var blockers = ReadJson(StoreBlockerRegisterPath);
        using var release = ReadJson(PublicReleaseNoGoPath);
        using var store = ReadJson(ChromeWebStoreNoGoPath);
        Assert.IsFalse(blockers.RootElement.GetProperty("publicReleaseReady").GetBoolean());
        Assert.IsFalse(blockers.RootElement.GetProperty("chromeWebStoreReady").GetBoolean());
        Assert.IsFalse(release.RootElement.GetProperty("publicReleaseReady").GetBoolean());
        Assert.IsFalse(store.RootElement.GetProperty("chromeWebStoreReady").GetBoolean());
    }
}
