using System.Text.Json;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace OneBrain.Safety.Tests;

[TestClass]
[TestCategory("HumanChromeQaFreezeReadiness")]
[TestCategory("M695")]
[TestCategory("M696")]
[TestCategory("M697")]
public sealed class NodalOsHumanChromeQaFreezeReadinessM695M697Tests
{
    private const string M695ReportPath = "docs/reports/m695-human-chrome-qa-evidence-intake.md";
    private const string M696ReportPath = "docs/reports/m696-final-package-freeze-audit.md";
    private const string M697ReportPath = "docs/reports/m697-pre-submission-audit-readiness-decision.md";

    private const string HumanIntakePath = "artifacts/agent-operations/m695/human-chrome-qa-evidence-intake.json";
    private const string PublicVariantLoadedPath = "artifacts/agent-operations/m695/public-variant-loaded-human-proof.json";
    private const string ManifestSelectionPath = "artifacts/agent-operations/m695/manifest-selection-human-proof.json";
    private const string TokenPresentPath = "artifacts/agent-operations/m695/token-present-ui-human-proof.json";
    private const string WebsocketConnectedPath = "artifacts/agent-operations/m695/websocket-connected-human-proof.json";
    private const string RuntimeTabPath = "artifacts/agent-operations/m695/runtime-tab-human-proof.json";
    private const string ServiceWorkerDevtoolsPath = "artifacts/agent-operations/m695/service-worker-devtools-human-proof.json";
    private const string CspConsolePath = "artifacts/agent-operations/m695/csp-console-human-proof.json";
    private const string PermissionWarningPath = "artifacts/agent-operations/m695/permission-warning-human-proof.json";
    private const string EvidenceRedactionPath = "artifacts/agent-operations/m695/evidence-redaction-human-proof.json";
    private const string M695GoNoGoPath = "artifacts/agent-operations/m695/m695-go-no-go.json";

    private const string FinalPackageFreezeAuditPath = "artifacts/agent-operations/m696/final-package-freeze-audit.json";
    private const string StagingFolderAuditPath = "artifacts/agent-operations/m696/public-staging-folder-final-audit.json";
    private const string StagingManifestAuditPath = "artifacts/agent-operations/m696/public-staging-manifest-final-audit.json";
    private const string PublicPermissionsAuditPath = "artifacts/agent-operations/m696/public-permissions-final-audit.json";
    private const string PackageContentsAuditPath = "artifacts/agent-operations/m696/package-contents-final-audit.json";
    private const string KnownLimitationsAuditPath = "artifacts/agent-operations/m696/known-limitations-final-audit.json";
    private const string StoreDisclosureGapAuditPath = "artifacts/agent-operations/m696/store-disclosure-gap-final-audit.json";
    private const string M696GoNoGoPath = "artifacts/agent-operations/m696/m696-go-no-go.json";

    private const string PreSubmissionDecisionPath = "artifacts/agent-operations/m697/pre-submission-audit-readiness-decision.json";
    private const string PublicPackageFreezeReadinessPath = "artifacts/agent-operations/m697/public-package-freeze-readiness.json";
    private const string ManualQaCompletenessPath = "artifacts/agent-operations/m697/manual-qa-completeness-decision.json";
    private const string PublicReleaseNoGoPath = "artifacts/agent-operations/m697/public-release-no-go-proof.json";
    private const string ChromeWebStoreNoGoPath = "artifacts/agent-operations/m697/chrome-web-store-no-go-proof.json";
    private const string PostFreezeRiskRegisterPath = "artifacts/agent-operations/m697/post-freeze-risk-register.json";
    private const string NextMilestoneRecommendationPath = "artifacts/agent-operations/m697/next-milestone-recommendation.json";
    private const string M697GoNoGoPath = "artifacts/agent-operations/m697/m697-go-no-go.json";

    private const string ConsolidatedPath = "artifacts/agent-operations/m695-m697/human-chrome-qa-freeze-readiness-go-no-go.json";
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
    public void M695ArtifactsExist()
    {
        foreach (var path in new[]
        {
            M695ReportPath, HumanIntakePath, PublicVariantLoadedPath, ManifestSelectionPath, TokenPresentPath,
            WebsocketConnectedPath, RuntimeTabPath, ServiceWorkerDevtoolsPath, CspConsolePath,
            PermissionWarningPath, EvidenceRedactionPath, M695GoNoGoPath
        })
            Assert.IsTrue(File.Exists(FullPath(path)), path);
    }

    [TestMethod]
    public void M696ArtifactsExist()
    {
        foreach (var path in new[]
        {
            M696ReportPath, FinalPackageFreezeAuditPath, StagingFolderAuditPath, StagingManifestAuditPath,
            PublicPermissionsAuditPath, PackageContentsAuditPath, KnownLimitationsAuditPath,
            StoreDisclosureGapAuditPath, M696GoNoGoPath
        })
            Assert.IsTrue(File.Exists(FullPath(path)), path);
    }

    [TestMethod]
    public void M697ArtifactsExist()
    {
        foreach (var path in new[]
        {
            M697ReportPath, PreSubmissionDecisionPath, PublicPackageFreezeReadinessPath,
            ManualQaCompletenessPath, PublicReleaseNoGoPath, ChromeWebStoreNoGoPath,
            PostFreezeRiskRegisterPath, NextMilestoneRecommendationPath, M697GoNoGoPath, ConsolidatedPath
        })
            Assert.IsTrue(File.Exists(FullPath(path)), path);
    }

    [TestMethod]
    public void ConsolidatedDecisionIsAllowedAndConditional()
    {
        using var doc = ReadJson(ConsolidatedPath);
        var decision = doc.RootElement.GetProperty("decision").GetString();
        CollectionAssert.Contains(new[]
        {
            "HUMAN_CHROME_QA_EVIDENCE_READY",
            "HUMAN_CHROME_QA_CONDITIONAL_READY_EVIDENCE_PARTIAL",
            "HUMAN_CHROME_QA_REMEDIATION_REQUIRED"
        }, decision);

        Assert.AreEqual("HUMAN_CHROME_QA_CONDITIONAL_READY_EVIDENCE_PARTIAL", decision);
        Assert.IsTrue(doc.RootElement.GetProperty("internalCandidateReady").GetBoolean());
        Assert.IsTrue(doc.RootElement.GetProperty("publicBuildCandidateReady").GetBoolean());
        Assert.IsTrue(doc.RootElement.GetProperty("publicStagingReady").GetBoolean());
        Assert.IsTrue(doc.RootElement.GetProperty("humanChromeEvidenceReceived").GetBoolean());
        Assert.IsFalse(doc.RootElement.GetProperty("humanChromeEvidenceSufficient").GetBoolean());
        Assert.IsTrue(doc.RootElement.GetProperty("userReportedWorking").GetBoolean());
        Assert.IsFalse(doc.RootElement.GetProperty("publicPackageFreezeReady").GetBoolean());
        Assert.IsTrue(doc.RootElement.GetProperty("preSubmissionAuditReady").GetBoolean());
        Assert.IsFalse(doc.RootElement.GetProperty("publicReleaseReady").GetBoolean());
        Assert.IsFalse(doc.RootElement.GetProperty("chromeWebStoreReady").GetBoolean());
    }

    [TestMethod]
    public void CapabilitiesRemainDisabled()
    {
        foreach (var path in new[] { M695GoNoGoPath, M696GoNoGoPath, M697GoNoGoPath, ConsolidatedPath })
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
    public void EvidenceRedactionBlocksSecretPersistence()
    {
        using var redaction = ReadJson(EvidenceRedactionPath);
        Assert.IsTrue(redaction.RootElement.GetProperty("evidenceRedacted").GetBoolean());
        Assert.IsFalse(redaction.RootElement.GetProperty("secretsIncluded").GetBoolean());
        Assert.IsFalse(redaction.RootElement.GetProperty("tokenLoggedInFull").GetBoolean());
        Assert.IsFalse(redaction.RootElement.GetProperty("bridgeSecretsLeaked").GetBoolean());
        Assert.IsFalse(redaction.RootElement.GetProperty("rawConsoleExcerptStored").GetBoolean());
    }

    [TestMethod]
    public void ConditionalDecisionDoesNotClaimFullEvidenceReady()
    {
        using var intake = ReadJson(HumanIntakePath);
        Assert.IsTrue(intake.RootElement.GetProperty("humanChromeEvidenceReceived").GetBoolean());
        Assert.IsTrue(intake.RootElement.GetProperty("userReportedWorking").GetBoolean());
        Assert.AreEqual("partial", intake.RootElement.GetProperty("evidenceCompleteness").GetString());
        Assert.AreEqual("PARTIAL", intake.RootElement.GetProperty("evidenceClassification").GetString());
        Assert.IsTrue(intake.RootElement.GetProperty("fullPassNotClaimed").GetBoolean());
    }

    [TestMethod]
    public void StagingManifestRemainsValidAndNarrowed()
    {
        Assert.IsTrue(File.Exists(FullPath(StagingManifestPath)), StagingManifestPath);
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
