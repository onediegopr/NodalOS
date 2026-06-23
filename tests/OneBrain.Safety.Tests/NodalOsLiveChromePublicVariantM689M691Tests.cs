using System.Text.Json;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace OneBrain.Safety.Tests;

[TestClass]
[TestCategory("LiveChromePublicVariant")]
[TestCategory("M689")]
[TestCategory("M690")]
[TestCategory("M691")]
public sealed class NodalOsLiveChromePublicVariantM689M691Tests
{
    private const string M689ReportPath = "docs/reports/m689-public-variant-staging-remediation.md";
    private const string M690ReportPath = "docs/reports/m690-human-assisted-chrome-load-connected-evidence.md";
    private const string M691ReportPath = "docs/reports/m691-package-freeze-decision-after-chrome-load-remediation.md";

    private const string StagingRemediationPath = "artifacts/agent-operations/m689/public-variant-staging-remediation.json";
    private const string StagingFolderProofPath = "artifacts/agent-operations/m689/public-staging-folder-proof.json";
    private const string StagingManifestProofPath = "artifacts/agent-operations/m689/public-staging-manifest-proof.json";
    private const string StagingFileListPath = "artifacts/agent-operations/m689/public-staging-file-list.json";
    private const string StagingCleanupPlanPath = "artifacts/agent-operations/m689/public-staging-cleanup-plan.json";
    private const string M689GoNoGoPath = "artifacts/agent-operations/m689/m689-go-no-go.json";

    private const string HumanChromeLoadEvidencePath = "artifacts/agent-operations/m690/human-assisted-chrome-load-evidence.json";
    private const string PublicVariantLoadedEvidencePath = "artifacts/agent-operations/m690/public-variant-loaded-evidence.json";
    private const string TokenPresentUiEvidencePath = "artifacts/agent-operations/m690/token-present-ui-evidence.json";
    private const string WebsocketConnectedUiEvidencePath = "artifacts/agent-operations/m690/websocket-connected-ui-evidence.json";
    private const string RuntimeTabEvidencePath = "artifacts/agent-operations/m690/runtime-tab-evidence.json";
    private const string ServiceWorkerDevtoolsEvidencePath = "artifacts/agent-operations/m690/service-worker-devtools-evidence.json";
    private const string CspConsoleEvidencePath = "artifacts/agent-operations/m690/csp-console-evidence.json";
    private const string PermissionWarningEvidencePath = "artifacts/agent-operations/m690/permission-warning-evidence.json";
    private const string EvidenceRedactionProofPath = "artifacts/agent-operations/m690/evidence-redaction-proof.json";
    private const string M690GoNoGoPath = "artifacts/agent-operations/m690/m690-go-no-go.json";

    private const string PackageFreezeDecisionPath = "artifacts/agent-operations/m691/package-freeze-decision-after-chrome-load-remediation.json";
    private const string ManualQaCompletenessPath = "artifacts/agent-operations/m691/manual-qa-completeness-after-chrome-load.json";
    private const string PublicPackageFreezeReadinessPath = "artifacts/agent-operations/m691/public-package-freeze-readiness.json";
    private const string PublicReleaseNoGoPath = "artifacts/agent-operations/m691/public-release-no-go-proof.json";
    private const string ChromeWebStoreNoGoPath = "artifacts/agent-operations/m691/chrome-web-store-no-go-proof.json";
    private const string PostRemediationRiskRegisterPath = "artifacts/agent-operations/m691/post-remediation-risk-register.json";
    private const string M691GoNoGoPath = "artifacts/agent-operations/m691/m691-go-no-go.json";

    private const string ConsolidatedPath = "artifacts/agent-operations/m689-m691/live-chrome-public-variant-remediation-go-no-go.json";
    private const string PublicManifestPath = "browser-extension/onebrain-chrome-lab/manifest.public.json";
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
    public void M689ArtifactsExist()
    {
        Assert.IsTrue(File.Exists(FullPath(M689ReportPath)), M689ReportPath);
        Assert.IsTrue(File.Exists(FullPath(StagingRemediationPath)), StagingRemediationPath);
        Assert.IsTrue(File.Exists(FullPath(StagingFolderProofPath)), StagingFolderProofPath);
        Assert.IsTrue(File.Exists(FullPath(StagingManifestProofPath)), StagingManifestProofPath);
        Assert.IsTrue(File.Exists(FullPath(StagingFileListPath)), StagingFileListPath);
        Assert.IsTrue(File.Exists(FullPath(StagingCleanupPlanPath)), StagingCleanupPlanPath);
        Assert.IsTrue(File.Exists(FullPath(M689GoNoGoPath)), M689GoNoGoPath);
    }

    [TestMethod]
    public void M690ArtifactsExist()
    {
        Assert.IsTrue(File.Exists(FullPath(M690ReportPath)), M690ReportPath);
        Assert.IsTrue(File.Exists(FullPath(HumanChromeLoadEvidencePath)), HumanChromeLoadEvidencePath);
        Assert.IsTrue(File.Exists(FullPath(PublicVariantLoadedEvidencePath)), PublicVariantLoadedEvidencePath);
        Assert.IsTrue(File.Exists(FullPath(TokenPresentUiEvidencePath)), TokenPresentUiEvidencePath);
        Assert.IsTrue(File.Exists(FullPath(WebsocketConnectedUiEvidencePath)), WebsocketConnectedUiEvidencePath);
        Assert.IsTrue(File.Exists(FullPath(RuntimeTabEvidencePath)), RuntimeTabEvidencePath);
        Assert.IsTrue(File.Exists(FullPath(ServiceWorkerDevtoolsEvidencePath)), ServiceWorkerDevtoolsEvidencePath);
        Assert.IsTrue(File.Exists(FullPath(CspConsoleEvidencePath)), CspConsoleEvidencePath);
        Assert.IsTrue(File.Exists(FullPath(PermissionWarningEvidencePath)), PermissionWarningEvidencePath);
        Assert.IsTrue(File.Exists(FullPath(EvidenceRedactionProofPath)), EvidenceRedactionProofPath);
        Assert.IsTrue(File.Exists(FullPath(M690GoNoGoPath)), M690GoNoGoPath);
    }

    [TestMethod]
    public void M691ArtifactsExist()
    {
        Assert.IsTrue(File.Exists(FullPath(M691ReportPath)), M691ReportPath);
        Assert.IsTrue(File.Exists(FullPath(PackageFreezeDecisionPath)), PackageFreezeDecisionPath);
        Assert.IsTrue(File.Exists(FullPath(ManualQaCompletenessPath)), ManualQaCompletenessPath);
        Assert.IsTrue(File.Exists(FullPath(PublicPackageFreezeReadinessPath)), PublicPackageFreezeReadinessPath);
        Assert.IsTrue(File.Exists(FullPath(PublicReleaseNoGoPath)), PublicReleaseNoGoPath);
        Assert.IsTrue(File.Exists(FullPath(ChromeWebStoreNoGoPath)), ChromeWebStoreNoGoPath);
        Assert.IsTrue(File.Exists(FullPath(PostRemediationRiskRegisterPath)), PostRemediationRiskRegisterPath);
        Assert.IsTrue(File.Exists(FullPath(M691GoNoGoPath)), M691GoNoGoPath);
    }

    [TestMethod]
    public void ConsolidatedGoNoGoExists() =>
        Assert.IsTrue(File.Exists(FullPath(ConsolidatedPath)), ConsolidatedPath);

    [TestMethod]
    public void StagingFolderAndManifestAreReady()
    {
        using var folder = ReadJson(StagingFolderProofPath);
        using var manifest = ReadJson(StagingManifestProofPath);
        using var remediation = ReadJson(StagingRemediationPath);

        Assert.IsTrue(folder.RootElement.GetProperty("stagingFolderReady").GetBoolean());
        Assert.IsTrue(folder.RootElement.GetProperty("safeForManualChromeLoad").GetBoolean());
        Assert.IsFalse(folder.RootElement.GetProperty("releasePackage").GetBoolean());
        Assert.IsTrue(manifest.RootElement.GetProperty("publicStagingUsesPublicManifest").GetBoolean());
        Assert.IsTrue(manifest.RootElement.GetProperty("stagingManifestValidJson").GetBoolean());
        Assert.IsFalse(manifest.RootElement.GetProperty("containsWildcardHttpHostPermission").GetBoolean());
        Assert.IsFalse(manifest.RootElement.GetProperty("containsWildcardHttpsHostPermission").GetBoolean());
        Assert.IsFalse(manifest.RootElement.GetProperty("containsContentScriptsWildcard").GetBoolean());
        Assert.IsTrue(remediation.RootElement.GetProperty("publicStagingHasNoWildcards").GetBoolean());
        Assert.IsFalse(remediation.RootElement.GetProperty("tokenLoggedInFull").GetBoolean());
        Assert.IsFalse(remediation.RootElement.GetProperty("bridgeSecretsLeaked").GetBoolean());
    }

    [TestMethod]
    public void HumanEvidenceRemainsRequiredWithoutInventedPasses()
    {
        using var human = ReadJson(HumanChromeLoadEvidencePath);
        using var token = ReadJson(TokenPresentUiEvidencePath);
        using var websocket = ReadJson(WebsocketConnectedUiEvidencePath);
        using var runtime = ReadJson(RuntimeTabEvidencePath);
        using var devtools = ReadJson(ServiceWorkerDevtoolsEvidencePath);

        Assert.AreEqual("HUMAN_INPUT_REQUIRED", human.RootElement.GetProperty("decision").GetString());
        Assert.IsFalse(human.RootElement.GetProperty("publicVariantLoaded").GetBoolean());
        Assert.IsFalse(token.RootElement.GetProperty("tokenPresentUi").GetBoolean());
        Assert.IsFalse(websocket.RootElement.GetProperty("websocketConnectedLive").GetBoolean());
        Assert.IsFalse(runtime.RootElement.GetProperty("runtimeTabEvidenceReady").GetBoolean());
        Assert.IsFalse(devtools.RootElement.GetProperty("serviceWorkerDevtoolsEvidenceReady").GetBoolean());
    }

    [TestMethod]
    public void ConsolidatedDecisionIsAllowedAndCapabilitiesStayDisabled()
    {
        using var doc = ReadJson(ConsolidatedPath);
        var decision = doc.RootElement.GetProperty("decision").GetString();
        CollectionAssert.Contains(new[]
        {
            "LIVE_CHROME_PUBLIC_VARIANT_EVIDENCE_READY",
            "HUMAN_CHROME_LOAD_INPUT_REQUIRED",
            "LIVE_CHROME_PUBLIC_VARIANT_REMEDIATION_REQUIRED"
        }, decision);

        Assert.AreEqual("HUMAN_CHROME_LOAD_INPUT_REQUIRED", decision);
        Assert.IsTrue(doc.RootElement.GetProperty("internalCandidateReady").GetBoolean());
        Assert.IsTrue(doc.RootElement.GetProperty("publicBuildCandidateReady").GetBoolean());
        Assert.IsTrue(doc.RootElement.GetProperty("bridgeLivenessReady").GetBoolean());
        Assert.IsTrue(doc.RootElement.GetProperty("publicStagingFolderReady").GetBoolean());
        Assert.IsTrue(doc.RootElement.GetProperty("publicStagingUsesPublicManifest").GetBoolean());
        Assert.IsTrue(doc.RootElement.GetProperty("publicStagingHasNoWildcards").GetBoolean());
        Assert.IsFalse(doc.RootElement.GetProperty("publicVariantLoaded").GetBoolean());
        Assert.IsFalse(doc.RootElement.GetProperty("tokenPresentUi").GetBoolean());
        Assert.IsFalse(doc.RootElement.GetProperty("websocketConnectedLive").GetBoolean());
        Assert.IsFalse(doc.RootElement.GetProperty("manualQaEvidenceSufficient").GetBoolean());
        Assert.IsFalse(doc.RootElement.GetProperty("publicPackageFreezeReady").GetBoolean());
        Assert.IsFalse(doc.RootElement.GetProperty("publicReleaseReady").GetBoolean());
        Assert.IsFalse(doc.RootElement.GetProperty("chromeWebStoreReady").GetBoolean());
        Assert.IsFalse(doc.RootElement.GetProperty("runtimeProductiveEnabled").GetBoolean());
        Assert.IsFalse(doc.RootElement.GetProperty("providerCloudEnabled").GetBoolean());
        Assert.IsFalse(doc.RootElement.GetProperty("filesystemEnabled").GetBoolean());
        Assert.IsFalse(doc.RootElement.GetProperty("browserAutomationEnabled").GetBoolean());
        Assert.IsFalse(doc.RootElement.GetProperty("capabilityUnlockEnabled").GetBoolean());
        Assert.IsFalse(doc.RootElement.GetProperty("bridgeModified").GetBoolean());
        Assert.IsFalse(doc.RootElement.GetProperty("cspModified").GetBoolean());
        Assert.IsFalse(doc.RootElement.GetProperty("tokenLoggedInFull").GetBoolean());
        Assert.IsFalse(doc.RootElement.GetProperty("bridgeSecretsLeaked").GetBoolean());
    }

    [TestMethod]
    public void PerMilestoneGoNoGosKeepReleaseAndCapabilitiesDisabled()
    {
        foreach (var path in new[] { M689GoNoGoPath, M690GoNoGoPath, M691GoNoGoPath, ConsolidatedPath })
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
            Assert.IsFalse(doc.RootElement.GetProperty("tokenLoggedInFull").GetBoolean(), path);
            Assert.IsFalse(doc.RootElement.GetProperty("bridgeSecretsLeaked").GetBoolean(), path);
        }
    }

    [TestMethod]
    public void ManifestPublicJsonRemainsValidAndNarrowed() =>
        AssertManifestIsNarrowed(PublicManifestPath);

    [TestMethod]
    public void StagingManifestJsonExistsAndIsNarrowed()
    {
        Assert.IsTrue(File.Exists(FullPath(StagingManifestPath)), StagingManifestPath);
        AssertManifestIsNarrowed(StagingManifestPath);
    }

    private static void AssertManifestIsNarrowed(string relativePath)
    {
        using var doc = ReadJson(relativePath);
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
