using System.Text.Json;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace OneBrain.Safety.Tests;

[TestClass]
[TestCategory("EvidenceAssetsPreSubmission")]
[TestCategory("M701")]
[TestCategory("M702")]
[TestCategory("M703")]
public sealed class NodalOsEvidenceAssetsPreSubmissionM701M703Tests
{
    private const string M701ReportPath = "docs/reports/m701-evidence-completion-manual-screenshot-contract.md";
    private const string M702ReportPath = "docs/reports/m702-store-asset-capture-pack-listing-disclosure.md";
    private const string M703ReportPath = "docs/reports/m703-pre-submission-audit.md";

    private const string EvidenceCompletionPlanPath = "artifacts/agent-operations/m701/evidence-completion-plan.json";
    private const string ManualScreenshotContractPath = "artifacts/agent-operations/m701/manual-screenshot-contract.json";
    private const string HumanChromeFieldsPath = "artifacts/agent-operations/m701/human-chrome-evidence-required-fields.json";
    private const string RuntimeDevtoolsContractPath = "artifacts/agent-operations/m701/runtime-devtools-evidence-contract.json";
    private const string RedactionContractPath = "artifacts/agent-operations/m701/evidence-redaction-contract.json";
    private const string M701GoNoGoPath = "artifacts/agent-operations/m701/m701-go-no-go.json";

    private const string StoreAssetCapturePackPath = "artifacts/agent-operations/m702/store-asset-capture-pack.json";
    private const string StoreListingFinalDraftPath = "artifacts/agent-operations/m702/store-listing-final-draft.json";
    private const string PermissionDisclosureFinalPath = "artifacts/agent-operations/m702/permission-disclosure-final.json";
    private const string PrivacySupportUrlDecisionPath = "artifacts/agent-operations/m702/privacy-support-url-decision.json";
    private const string ScreenshotAssetChecklistPath = "artifacts/agent-operations/m702/screenshot-asset-checklist.json";
    private const string StoreAssetsReadinessPath = "artifacts/agent-operations/m702/store-assets-readiness-register.json";
    private const string M702GoNoGoPath = "artifacts/agent-operations/m702/m702-go-no-go.json";

    private const string PreSubmissionAuditPath = "artifacts/agent-operations/m703/pre-submission-audit.json";
    private const string PreSubmissionChecklistFinalPath = "artifacts/agent-operations/m703/pre-submission-checklist-final.json";
    private const string FreezeReadinessAuditPath = "artifacts/agent-operations/m703/public-package-freeze-readiness-audit.json";
    private const string StoreRiskRegisterPath = "artifacts/agent-operations/m703/store-readiness-risk-register.json";
    private const string PublicReleaseNoGoPath = "artifacts/agent-operations/m703/public-release-no-go-proof.json";
    private const string ChromeWebStoreNoGoPath = "artifacts/agent-operations/m703/chrome-web-store-no-go-proof.json";
    private const string NextMilestonePath = "artifacts/agent-operations/m703/next-milestone-recommendation.json";
    private const string M703GoNoGoPath = "artifacts/agent-operations/m703/m703-go-no-go.json";

    private const string ConsolidatedPath = "artifacts/agent-operations/m701-m703/evidence-assets-pre-submission-go-no-go.json";

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
    public void M701ArtifactsExist()
    {
        foreach (var path in new[]
        {
            M701ReportPath, EvidenceCompletionPlanPath, ManualScreenshotContractPath,
            HumanChromeFieldsPath, RuntimeDevtoolsContractPath, RedactionContractPath, M701GoNoGoPath
        })
            Assert.IsTrue(File.Exists(FullPath(path)), path);
    }

    [TestMethod]
    public void M702ArtifactsExist()
    {
        foreach (var path in new[]
        {
            M702ReportPath, StoreAssetCapturePackPath, StoreListingFinalDraftPath,
            PermissionDisclosureFinalPath, PrivacySupportUrlDecisionPath, ScreenshotAssetChecklistPath,
            StoreAssetsReadinessPath, M702GoNoGoPath
        })
            Assert.IsTrue(File.Exists(FullPath(path)), path);
    }

    [TestMethod]
    public void M703ArtifactsExist()
    {
        foreach (var path in new[]
        {
            M703ReportPath, PreSubmissionAuditPath, PreSubmissionChecklistFinalPath,
            FreezeReadinessAuditPath, StoreRiskRegisterPath, PublicReleaseNoGoPath,
            ChromeWebStoreNoGoPath, NextMilestonePath, M703GoNoGoPath, ConsolidatedPath
        })
            Assert.IsTrue(File.Exists(FullPath(path)), path);
    }

    [TestMethod]
    public void ConsolidatedDecisionIsAllowedAndReadyWithTrackedGaps()
    {
        using var doc = ReadJson(ConsolidatedPath);
        var decision = doc.RootElement.GetProperty("decision").GetString();
        CollectionAssert.Contains(new[]
        {
            "PRE_SUBMISSION_AUDIT_READY_ASSET_GAPS_TRACKED",
            "PRE_SUBMISSION_AUDIT_CONDITIONAL_EVIDENCE_REQUIRED",
            "PRE_SUBMISSION_AUDIT_BLOCKED_REMEDIATION_REQUIRED"
        }, decision);

        Assert.AreEqual("PRE_SUBMISSION_AUDIT_READY_ASSET_GAPS_TRACKED", decision);
        Assert.IsTrue(doc.RootElement.GetProperty("internalCandidateReady").GetBoolean());
        Assert.IsTrue(doc.RootElement.GetProperty("publicBuildCandidateReady").GetBoolean());
        Assert.IsTrue(doc.RootElement.GetProperty("publicStagingReady").GetBoolean());
        Assert.IsTrue(doc.RootElement.GetProperty("storeListingFinalDraftReady").GetBoolean());
        Assert.IsTrue(doc.RootElement.GetProperty("permissionDisclosureFinalReady").GetBoolean());
        Assert.IsTrue(doc.RootElement.GetProperty("screenshotAssetChecklistReady").GetBoolean());
        Assert.IsTrue(doc.RootElement.GetProperty("privacySupportUrlDecisionReady").GetBoolean());
        Assert.IsTrue(doc.RootElement.GetProperty("preSubmissionAuditReady").GetBoolean());
        Assert.IsFalse(doc.RootElement.GetProperty("publicPackageFreezeReady").GetBoolean());
        Assert.IsTrue(doc.RootElement.GetProperty("assetCapturePending").GetBoolean());
        Assert.IsTrue(doc.RootElement.GetProperty("humanEvidenceStillPartial").GetBoolean());
        Assert.IsFalse(doc.RootElement.GetProperty("publicReleaseReady").GetBoolean());
        Assert.IsFalse(doc.RootElement.GetProperty("chromeWebStoreReady").GetBoolean());
    }

    [TestMethod]
    public void CapabilitiesRemainDisabled()
    {
        foreach (var path in new[] { M701GoNoGoPath, M702GoNoGoPath, M703GoNoGoPath, ConsolidatedPath })
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
    public void StoreListingFinalDraftDoesNotClaimBroadWebsiteAccess()
    {
        using var doc = ReadJson(StoreListingFinalDraftPath);
        Assert.AreEqual("NODAL OS", doc.RootElement.GetProperty("name").GetString());
        Assert.IsFalse(doc.RootElement.GetProperty("claimsBroadWebsiteAccess").GetBoolean());
        Assert.IsTrue(doc.RootElement.GetProperty("mentionsLocalFirst").GetBoolean());
        Assert.IsTrue(doc.RootElement.GetProperty("mentionsLocalBridge").GetBoolean());
        Assert.IsFalse(doc.RootElement.GetProperty("claimsProductiveRuntimeEnabled").GetBoolean());
        Assert.IsFalse(doc.RootElement.GetProperty("claimsProviderCloudEnabled").GetBoolean());
        Assert.IsFalse(doc.RootElement.GetProperty("claimsBrowserAutomationEnabled").GetBoolean());
    }

    [TestMethod]
    public void PermissionDisclosureFinalMentionsLocalhostAnd127001()
    {
        using var doc = ReadJson(PermissionDisclosureFinalPath);
        Assert.IsTrue(doc.RootElement.GetProperty("mentionsLocalhost127001").GetBoolean());
        Assert.IsFalse(doc.RootElement.GetProperty("wildcardHostPermissions").GetBoolean());
        Assert.IsTrue(doc.RootElement.GetProperty("explainsNoWildcards").GetBoolean());
    }

    [TestMethod]
    public void ScreenshotChecklistIncludesRequiredEvidenceCategories()
    {
        using var doc = ReadJson(ScreenshotAssetChecklistPath);
        var serialized = doc.RootElement.ToString();
        StringAssert.Contains(serialized, "sidepanel");
        StringAssert.Contains(serialized, "extension loaded");
        StringAssert.Contains(serialized, "permissions");
        StringAssert.Contains(serialized, "runtime tab");
        StringAssert.Contains(serialized, "service worker devtools");
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
