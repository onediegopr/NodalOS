using System.Text.Json;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace OneBrain.Safety.Tests;

[TestClass]
[TestCategory("PreSubmissionAuditReadiness")]
[TestCategory("M698")]
[TestCategory("M699")]
[TestCategory("M700")]
public sealed class NodalOsPreSubmissionAuditReadinessM698M700Tests
{
    private const string M698ReportPath = "docs/reports/m698-human-chrome-evidence-completion.md";
    private const string M699ReportPath = "docs/reports/m699-store-asset-listing-disclosure-gap-closure.md";
    private const string M700ReportPath = "docs/reports/m700-pre-submission-audit-readiness.md";

    private const string HumanEvidenceCompletionPath = "artifacts/agent-operations/m698/human-chrome-evidence-completion.json";
    private const string EvidenceGapMatrixPath = "artifacts/agent-operations/m698/evidence-gap-completion-matrix.json";
    private const string VisibleNameStatusPath = "artifacts/agent-operations/m698/public-variant-visible-name-status.json";
    private const string ManifestSelectionStatusPath = "artifacts/agent-operations/m698/manifest-selection-human-status.json";
    private const string TokenWebsocketStatusPath = "artifacts/agent-operations/m698/token-websocket-human-status.json";
    private const string RuntimeDevtoolsStatusPath = "artifacts/agent-operations/m698/runtime-devtools-human-status.json";
    private const string CspConsoleStatusPath = "artifacts/agent-operations/m698/csp-console-human-status.json";
    private const string PermissionWarningStatusPath = "artifacts/agent-operations/m698/permission-warning-human-status.json";
    private const string EvidenceRedactionStatusPath = "artifacts/agent-operations/m698/evidence-redaction-status.json";
    private const string M698GoNoGoPath = "artifacts/agent-operations/m698/m698-go-no-go.json";

    private const string StoreAssetGapClosurePath = "artifacts/agent-operations/m699/store-asset-gap-closure.json";
    private const string StoreListingDraftPath = "artifacts/agent-operations/m699/store-listing-draft.json";
    private const string FinalNamingRecommendationPath = "artifacts/agent-operations/m699/final-naming-recommendation.json";
    private const string PermissionDisclosureDraftPath = "artifacts/agent-operations/m699/permission-disclosure-final-draft.json";
    private const string PrivacySupportGapPath = "artifacts/agent-operations/m699/privacy-support-url-gap-register.json";
    private const string ScreenshotAssetGapPath = "artifacts/agent-operations/m699/screenshot-asset-gap-register.json";
    private const string StoreRiskRegisterPath = "artifacts/agent-operations/m699/store-submission-risk-register.json";
    private const string M699GoNoGoPath = "artifacts/agent-operations/m699/m699-go-no-go.json";

    private const string PreSubmissionReadinessPath = "artifacts/agent-operations/m700/pre-submission-audit-readiness.json";
    private const string PreSubmissionChecklistPath = "artifacts/agent-operations/m700/pre-submission-checklist.json";
    private const string FreezeReadinessAfterGapsPath = "artifacts/agent-operations/m700/public-package-freeze-readiness-after-gaps.json";
    private const string PublicReleaseNoGoPath = "artifacts/agent-operations/m700/public-release-no-go-proof.json";
    private const string ChromeWebStoreNoGoPath = "artifacts/agent-operations/m700/chrome-web-store-no-go-proof.json";
    private const string NextMilestonePath = "artifacts/agent-operations/m700/next-milestone-recommendation.json";
    private const string M700GoNoGoPath = "artifacts/agent-operations/m700/m700-go-no-go.json";

    private const string ConsolidatedPath = "artifacts/agent-operations/m698-m700/evidence-store-gaps-pre-submission-go-no-go.json";
    private const string StagingManifestPath = "artifacts/manual-qa/public-variant-staging/manifest.json";

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

    private static string[] StringArray(JsonElement element) =>
        element.EnumerateArray().Select(item => item.GetString() ?? string.Empty).ToArray();

    [TestMethod]
    public void M698ArtifactsExist()
    {
        foreach (var path in new[]
        {
            M698ReportPath, HumanEvidenceCompletionPath, EvidenceGapMatrixPath, VisibleNameStatusPath,
            ManifestSelectionStatusPath, TokenWebsocketStatusPath, RuntimeDevtoolsStatusPath,
            CspConsoleStatusPath, PermissionWarningStatusPath, EvidenceRedactionStatusPath, M698GoNoGoPath
        })
            Assert.IsTrue(File.Exists(FullPath(path)), path);
    }

    [TestMethod]
    public void M699ArtifactsExist()
    {
        foreach (var path in new[]
        {
            M699ReportPath, StoreAssetGapClosurePath, StoreListingDraftPath, FinalNamingRecommendationPath,
            PermissionDisclosureDraftPath, PrivacySupportGapPath, ScreenshotAssetGapPath,
            StoreRiskRegisterPath, M699GoNoGoPath
        })
            Assert.IsTrue(File.Exists(FullPath(path)), path);
    }

    [TestMethod]
    public void M700ArtifactsExist()
    {
        foreach (var path in new[]
        {
            M700ReportPath, PreSubmissionReadinessPath, PreSubmissionChecklistPath,
            FreezeReadinessAfterGapsPath, PublicReleaseNoGoPath, ChromeWebStoreNoGoPath,
            NextMilestonePath, M700GoNoGoPath, ConsolidatedPath
        })
            Assert.IsTrue(File.Exists(FullPath(path)), path);
    }

    [TestMethod]
    public void ConsolidatedDecisionIsAllowedAndPartial()
    {
        using var doc = ReadJson(ConsolidatedPath);
        var decision = doc.RootElement.GetProperty("decision").GetString();
        CollectionAssert.Contains(new[]
        {
            "PRE_SUBMISSION_AUDIT_READY_EVIDENCE_COMPLETE",
            "PRE_SUBMISSION_AUDIT_READY_EVIDENCE_PARTIAL",
            "PRE_SUBMISSION_AUDIT_BLOCKED_REMEDIATION_REQUIRED"
        }, decision);

        Assert.AreEqual("PRE_SUBMISSION_AUDIT_READY_EVIDENCE_PARTIAL", decision);
        Assert.IsTrue(doc.RootElement.GetProperty("internalCandidateReady").GetBoolean());
        Assert.IsTrue(doc.RootElement.GetProperty("publicBuildCandidateReady").GetBoolean());
        Assert.IsTrue(doc.RootElement.GetProperty("publicStagingReady").GetBoolean());
        Assert.IsTrue(doc.RootElement.GetProperty("humanChromeEvidenceReceived").GetBoolean());
        Assert.IsFalse(doc.RootElement.GetProperty("humanChromeEvidenceSufficient").GetBoolean());
        Assert.IsTrue(doc.RootElement.GetProperty("userReportedWorking").GetBoolean());
        Assert.IsTrue(doc.RootElement.GetProperty("storeListingDraftReady").GetBoolean());
        Assert.IsTrue(doc.RootElement.GetProperty("permissionDisclosureDraftReady").GetBoolean());
        Assert.IsTrue(doc.RootElement.GetProperty("privacySupportUrlGapsRegistered").GetBoolean());
        Assert.IsTrue(doc.RootElement.GetProperty("screenshotAssetGapsRegistered").GetBoolean());
        Assert.IsTrue(doc.RootElement.GetProperty("preSubmissionAuditReady").GetBoolean());
        Assert.IsFalse(doc.RootElement.GetProperty("publicPackageFreezeReady").GetBoolean());
        Assert.IsFalse(doc.RootElement.GetProperty("publicReleaseReady").GetBoolean());
        Assert.IsFalse(doc.RootElement.GetProperty("chromeWebStoreReady").GetBoolean());
    }

    [TestMethod]
    public void CapabilitiesRemainDisabled()
    {
        foreach (var path in new[] { M698GoNoGoPath, M699GoNoGoPath, M700GoNoGoPath, ConsolidatedPath })
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
    public void StoreListingDoesNotClaimBroadWebsiteAccessAndMentionsLocalBridge()
    {
        using var doc = ReadJson(StoreListingDraftPath);
        Assert.IsFalse(doc.RootElement.GetProperty("claimsBroadWebsiteAccess").GetBoolean());
        Assert.IsTrue(doc.RootElement.GetProperty("mentionsLocalFirst").GetBoolean());
        Assert.IsTrue(doc.RootElement.GetProperty("mentionsLocalBridge").GetBoolean());
    }

    [TestMethod]
    public void FinalNamingDoesNotUsePublicCandidateAsStoreName()
    {
        using var doc = ReadJson(FinalNamingRecommendationPath);
        Assert.AreEqual("NODAL OS", doc.RootElement.GetProperty("recommendedStoreFinalName").GetString());
        Assert.IsFalse(doc.RootElement.GetProperty("publicCandidateNameAllowedForStoreFinal").GetBoolean());
    }

    [TestMethod]
    public void PermissionDisclosureMentionsLocalhostAnd127001()
    {
        using var doc = ReadJson(PermissionDisclosureDraftPath);
        Assert.IsTrue(doc.RootElement.GetProperty("mentionsLocalhost127001").GetBoolean());
        Assert.IsFalse(doc.RootElement.GetProperty("wildcardHostPermissions").GetBoolean());
    }

    [TestMethod]
    public void StagingManifestRemainsValidAndNarrowed()
    {
        using var doc = ReadJson(StagingManifestPath);
        Assert.AreEqual(3, doc.RootElement.GetProperty("manifest_version").GetInt32());
        var hostPermissions = StringArray(doc.RootElement.GetProperty("host_permissions"));
        CollectionAssert.DoesNotContain(hostPermissions, "http://*/*");
        CollectionAssert.DoesNotContain(hostPermissions, "https://*/*");

        if (!doc.RootElement.TryGetProperty("content_scripts", out var contentScripts))
            return;

        foreach (var contentScript in contentScripts.EnumerateArray())
        {
            if (!contentScript.TryGetProperty("matches", out var matches))
                continue;

            var matchValues = StringArray(matches);
            CollectionAssert.DoesNotContain(matchValues, "http://*/*");
            CollectionAssert.DoesNotContain(matchValues, "https://*/*");
        }
    }
}
