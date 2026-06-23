using System.Text.Json;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace OneBrain.Safety.Tests;

[TestClass]
[TestCategory("LiveBridgeQa")]
[TestCategory("M686")]
[TestCategory("M687")]
[TestCategory("M688")]
public sealed class NodalOsLiveBridgeQaM686M688Tests
{
    private const string M686ReportPath = "docs/reports/m686-live-bridge-environment-setup.md";
    private const string M687ReportPath = "docs/reports/m687-connected-token-websocket-devtools-evidence.md";
    private const string M688ReportPath = "docs/reports/m688-package-freeze-decision-after-live-bridge-qa.md";

    private const string LiveBridgeEnvironmentSetupPath = "artifacts/agent-operations/m686/live-bridge-environment-setup.json";
    private const string BridgePortLivenessPath = "artifacts/agent-operations/m686/bridge-port-liveness-check.json";
    private const string ExtensionTokenSourcePath = "artifacts/agent-operations/m686/extension-token-source-check.json";
    private const string PublicVariantLoadReadinessPath = "artifacts/agent-operations/m686/public-variant-load-readiness.json";
    private const string M686GoNoGoPath = "artifacts/agent-operations/m686/m686-go-no-go.json";

    private const string TokenPresentLiveEvidencePath = "artifacts/agent-operations/m687/token-present-live-evidence.json";
    private const string WebsocketConnectedLiveEvidencePath = "artifacts/agent-operations/m687/websocket-connected-live-evidence.json";
    private const string RuntimeTabLiveEvidencePath = "artifacts/agent-operations/m687/runtime-tab-live-evidence.json";
    private const string ServiceWorkerDevtoolsLiveEvidencePath = "artifacts/agent-operations/m687/service-worker-devtools-live-evidence.json";
    private const string CspConsoleLiveEvidencePath = "artifacts/agent-operations/m687/csp-console-live-evidence.json";
    private const string PermissionWarningLiveEvidencePath = "artifacts/agent-operations/m687/permission-warning-live-evidence.json";
    private const string EvidenceRedactionLiveProofPath = "artifacts/agent-operations/m687/evidence-redaction-live-proof.json";
    private const string M687GoNoGoPath = "artifacts/agent-operations/m687/m687-go-no-go.json";

    private const string PackageFreezeDecisionPath = "artifacts/agent-operations/m688/package-freeze-decision-after-live-bridge-qa.json";
    private const string ManualQaCompletenessPath = "artifacts/agent-operations/m688/manual-qa-completeness-after-live-bridge.json";
    private const string PublicReleaseNoGoPath = "artifacts/agent-operations/m688/public-release-no-go-proof.json";
    private const string ChromeWebStoreNoGoPath = "artifacts/agent-operations/m688/chrome-web-store-no-go-proof.json";
    private const string PostLiveQaRiskRegisterPath = "artifacts/agent-operations/m688/post-live-qa-risk-register.json";
    private const string M688GoNoGoPath = "artifacts/agent-operations/m688/m688-go-no-go.json";

    private const string ConsolidatedPath = "artifacts/agent-operations/m686-m688/live-bridge-qa-package-freeze-go-no-go.json";
    private const string PublicManifestPath = "browser-extension/onebrain-chrome-lab/manifest.public.json";

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
    public void M686ArtifactsExist()
    {
        Assert.IsTrue(File.Exists(FullPath(M686ReportPath)), M686ReportPath);
        Assert.IsTrue(File.Exists(FullPath(LiveBridgeEnvironmentSetupPath)), LiveBridgeEnvironmentSetupPath);
        Assert.IsTrue(File.Exists(FullPath(BridgePortLivenessPath)), BridgePortLivenessPath);
        Assert.IsTrue(File.Exists(FullPath(ExtensionTokenSourcePath)), ExtensionTokenSourcePath);
        Assert.IsTrue(File.Exists(FullPath(PublicVariantLoadReadinessPath)), PublicVariantLoadReadinessPath);
        Assert.IsTrue(File.Exists(FullPath(M686GoNoGoPath)), M686GoNoGoPath);
    }

    [TestMethod]
    public void M687ArtifactsExist()
    {
        Assert.IsTrue(File.Exists(FullPath(M687ReportPath)), M687ReportPath);
        Assert.IsTrue(File.Exists(FullPath(TokenPresentLiveEvidencePath)), TokenPresentLiveEvidencePath);
        Assert.IsTrue(File.Exists(FullPath(WebsocketConnectedLiveEvidencePath)), WebsocketConnectedLiveEvidencePath);
        Assert.IsTrue(File.Exists(FullPath(RuntimeTabLiveEvidencePath)), RuntimeTabLiveEvidencePath);
        Assert.IsTrue(File.Exists(FullPath(ServiceWorkerDevtoolsLiveEvidencePath)), ServiceWorkerDevtoolsLiveEvidencePath);
        Assert.IsTrue(File.Exists(FullPath(CspConsoleLiveEvidencePath)), CspConsoleLiveEvidencePath);
        Assert.IsTrue(File.Exists(FullPath(PermissionWarningLiveEvidencePath)), PermissionWarningLiveEvidencePath);
        Assert.IsTrue(File.Exists(FullPath(EvidenceRedactionLiveProofPath)), EvidenceRedactionLiveProofPath);
        Assert.IsTrue(File.Exists(FullPath(M687GoNoGoPath)), M687GoNoGoPath);
    }

    [TestMethod]
    public void M688ArtifactsExist()
    {
        Assert.IsTrue(File.Exists(FullPath(M688ReportPath)), M688ReportPath);
        Assert.IsTrue(File.Exists(FullPath(PackageFreezeDecisionPath)), PackageFreezeDecisionPath);
        Assert.IsTrue(File.Exists(FullPath(ManualQaCompletenessPath)), ManualQaCompletenessPath);
        Assert.IsTrue(File.Exists(FullPath(PublicReleaseNoGoPath)), PublicReleaseNoGoPath);
        Assert.IsTrue(File.Exists(FullPath(ChromeWebStoreNoGoPath)), ChromeWebStoreNoGoPath);
        Assert.IsTrue(File.Exists(FullPath(PostLiveQaRiskRegisterPath)), PostLiveQaRiskRegisterPath);
        Assert.IsTrue(File.Exists(FullPath(M688GoNoGoPath)), M688GoNoGoPath);
    }

    [TestMethod]
    public void ConsolidatedGoNoGoExists() =>
        Assert.IsTrue(File.Exists(FullPath(ConsolidatedPath)), ConsolidatedPath);

    [TestMethod]
    public void BridgeEnvironmentAndTokenSourceAreRecordedWithoutSecretExposure()
    {
        using var setup = ReadJson(LiveBridgeEnvironmentSetupPath);
        using var liveness = ReadJson(BridgePortLivenessPath);
        using var token = ReadJson(ExtensionTokenSourcePath);

        Assert.IsTrue(setup.RootElement.GetProperty("bridgeConfigExists").GetBoolean());
        Assert.IsTrue(setup.RootElement.GetProperty("extensionTokenPresent").GetBoolean());
        Assert.IsFalse(setup.RootElement.GetProperty("extensionTokenValueStored").GetBoolean());
        Assert.IsFalse(setup.RootElement.GetProperty("extensionTokenValueLogged").GetBoolean());
        Assert.AreEqual("pass", setup.RootElement.GetProperty("bridgeLivenessAfterStartup").GetString());
        Assert.IsTrue(setup.RootElement.GetProperty("temporaryBridgeProcessStopped").GetBoolean());

        Assert.IsFalse(liveness.RootElement.GetProperty("initialTcpTestSucceeded").GetBoolean());
        Assert.AreEqual("pass", liveness.RootElement.GetProperty("postStartupTcp1270018787").GetString());
        Assert.AreEqual("pass", liveness.RootElement.GetProperty("wsExtensionUpgrade").GetString());

        Assert.IsTrue(token.RootElement.GetProperty("extensionTokenPresent").GetBoolean());
        Assert.IsFalse(token.RootElement.GetProperty("tokenLoggedInFull").GetBoolean());
        Assert.IsFalse(token.RootElement.GetProperty("bridgeSecretsLeaked").GetBoolean());
    }

    [TestMethod]
    public void LiveChromeEvidenceRemainsConditionalAndRedacted()
    {
        using var tokenLive = ReadJson(TokenPresentLiveEvidencePath);
        using var websocket = ReadJson(WebsocketConnectedLiveEvidencePath);
        using var runtime = ReadJson(RuntimeTabLiveEvidencePath);
        using var devtools = ReadJson(ServiceWorkerDevtoolsLiveEvidencePath);
        using var redaction = ReadJson(EvidenceRedactionLiveProofPath);

        Assert.IsFalse(tokenLive.RootElement.GetProperty("tokenPresentLive").GetBoolean());
        Assert.IsFalse(tokenLive.RootElement.GetProperty("tokenPresentLiveEvidenceReady").GetBoolean());
        Assert.IsFalse(websocket.RootElement.GetProperty("websocketConnectedLive").GetBoolean());
        Assert.IsFalse(websocket.RootElement.GetProperty("websocketConnectedLiveEvidenceReady").GetBoolean());
        Assert.IsFalse(runtime.RootElement.GetProperty("runtimeTabEvidenceReady").GetBoolean());
        Assert.IsFalse(devtools.RootElement.GetProperty("serviceWorkerDevtoolsEvidenceReady").GetBoolean());
        Assert.IsTrue(redaction.RootElement.GetProperty("evidenceRedacted").GetBoolean());
        Assert.IsFalse(redaction.RootElement.GetProperty("tokenLoggedInFull").GetBoolean());
        Assert.IsFalse(redaction.RootElement.GetProperty("bridgeSecretsLeaked").GetBoolean());
    }

    [TestMethod]
    public void ConsolidatedDecisionIsAllowedAndCapabilitiesStayDisabled()
    {
        using var doc = ReadJson(ConsolidatedPath);
        var decision = doc.RootElement.GetProperty("decision").GetString();
        CollectionAssert.Contains(new[]
        {
            "LIVE_BRIDGE_QA_EVIDENCE_READY",
            "LIVE_BRIDGE_QA_CONDITIONAL_ENVIRONMENT",
            "LIVE_BRIDGE_QA_REMEDIATION_REQUIRED"
        }, decision);

        Assert.AreEqual("LIVE_BRIDGE_QA_CONDITIONAL_ENVIRONMENT", decision);
        Assert.IsTrue(doc.RootElement.GetProperty("internalCandidateReady").GetBoolean());
        Assert.IsTrue(doc.RootElement.GetProperty("publicBuildCandidateReady").GetBoolean());
        Assert.IsTrue(doc.RootElement.GetProperty("bridgeTokenFixReady").GetBoolean());
        Assert.IsFalse(doc.RootElement.GetProperty("liveBridgeEnvironmentAvailable").GetBoolean());
        Assert.IsFalse(doc.RootElement.GetProperty("publicVariantLoaded").GetBoolean());
        Assert.IsFalse(doc.RootElement.GetProperty("tokenPresentLive").GetBoolean());
        Assert.IsFalse(doc.RootElement.GetProperty("websocketConnectedLive").GetBoolean());
        Assert.IsFalse(doc.RootElement.GetProperty("publicPackageFreezeReady").GetBoolean());
        Assert.IsFalse(doc.RootElement.GetProperty("publicReleaseReady").GetBoolean());
        Assert.IsFalse(doc.RootElement.GetProperty("chromeWebStoreReady").GetBoolean());
        Assert.IsTrue(doc.RootElement.GetProperty("evidenceRedacted").GetBoolean());
        Assert.IsFalse(doc.RootElement.GetProperty("tokenLoggedInFull").GetBoolean());
        Assert.IsFalse(doc.RootElement.GetProperty("bridgeSecretsLeaked").GetBoolean());
        Assert.IsFalse(doc.RootElement.GetProperty("runtimeProductiveEnabled").GetBoolean());
        Assert.IsFalse(doc.RootElement.GetProperty("providerCloudEnabled").GetBoolean());
        Assert.IsFalse(doc.RootElement.GetProperty("filesystemEnabled").GetBoolean());
        Assert.IsFalse(doc.RootElement.GetProperty("browserAutomationEnabled").GetBoolean());
        Assert.IsFalse(doc.RootElement.GetProperty("capabilityUnlockEnabled").GetBoolean());
        Assert.IsFalse(doc.RootElement.GetProperty("bridgeModified").GetBoolean());
        Assert.IsFalse(doc.RootElement.GetProperty("cspModified").GetBoolean());
    }

    [TestMethod]
    public void ConditionalDecisionKeepsPackageFreezeBlocked()
    {
        using var doc = ReadJson(ConsolidatedPath);
        if (doc.RootElement.GetProperty("decision").GetString() == "LIVE_BRIDGE_QA_CONDITIONAL_ENVIRONMENT")
            Assert.IsFalse(doc.RootElement.GetProperty("publicPackageFreezeReady").GetBoolean());
    }

    [TestMethod]
    public void PerMilestoneGoNoGosKeepCapabilitiesDisabled()
    {
        foreach (var path in new[] { M686GoNoGoPath, M687GoNoGoPath, M688GoNoGoPath, ConsolidatedPath })
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
    public void ManifestPublicJsonRemainsValidAndNarrowed()
    {
        using var doc = ReadJson(PublicManifestPath);
        Assert.AreEqual(3, doc.RootElement.GetProperty("manifest_version").GetInt32());
        var hostPermissions = StringArray(doc.RootElement.GetProperty("host_permissions"));
        CollectionAssert.DoesNotContain(hostPermissions, "http://*/*");
        CollectionAssert.DoesNotContain(hostPermissions, "https://*/*");
        CollectionAssert.Contains(hostPermissions, "http://127.0.0.1/*");
        CollectionAssert.Contains(hostPermissions, "http://localhost/*");

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
