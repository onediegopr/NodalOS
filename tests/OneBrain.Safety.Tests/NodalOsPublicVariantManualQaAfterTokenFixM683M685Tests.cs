using System.Text.Json;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace OneBrain.Safety.Tests;

[TestClass]
[TestCategory("PublicVariantManualQaAfterTokenFix")]
[TestCategory("M683")]
[TestCategory("M684")]
[TestCategory("M685")]
public sealed class NodalOsPublicVariantManualQaAfterTokenFixM683M685Tests
{
    private const string M683ReportPath = "docs/reports/m683-public-variant-manual-qa-retry.md";
    private const string M684ReportPath = "docs/reports/m684-live-bridge-runtime-devtools-evidence.md";
    private const string M685ReportPath = "docs/reports/m685-public-package-freeze-decision-after-token-fix.md";

    private const string M683RetryPath = "artifacts/agent-operations/m683/public-variant-manual-qa-retry.json";
    private const string TokenSaveStatusProofPath = "artifacts/agent-operations/m683/token-save-status-proof.json";
    private const string NoTokenRequiredStateProofPath = "artifacts/agent-operations/m683/no-token-required-state-proof.json";
    private const string BridgeUnreachableStateProofPath = "artifacts/agent-operations/m683/bridge-unreachable-state-proof.json";
    private const string ReconnectLoopRegressionProofPath = "artifacts/agent-operations/m683/reconnect-loop-regression-proof.json";
    private const string M683GoNoGoPath = "artifacts/agent-operations/m683/m683-go-no-go.json";

    private const string LiveBridgeTokenEvidencePath = "artifacts/agent-operations/m684/live-bridge-token-evidence.json";
    private const string RuntimeTabEvidencePath = "artifacts/agent-operations/m684/runtime-tab-evidence.json";
    private const string ServiceWorkerDevtoolsEvidencePath = "artifacts/agent-operations/m684/service-worker-devtools-evidence.json";
    private const string CspConsoleEvidencePath = "artifacts/agent-operations/m684/csp-console-evidence.json";
    private const string PermissionWarningEvidencePath = "artifacts/agent-operations/m684/permission-warning-evidence.json";
    private const string EvidenceRedactionProofPath = "artifacts/agent-operations/m684/evidence-redaction-proof.json";
    private const string M684GoNoGoPath = "artifacts/agent-operations/m684/m684-go-no-go.json";

    private const string PublicPackageFreezeDecisionPath = "artifacts/agent-operations/m685/public-package-freeze-decision-after-token-fix.json";
    private const string ManualQaReadinessPath = "artifacts/agent-operations/m685/manual-qa-readiness-after-token-fix.json";
    private const string PublicReleaseNoGoPath = "artifacts/agent-operations/m685/public-release-no-go-proof.json";
    private const string ChromeWebStoreNoGoPath = "artifacts/agent-operations/m685/chrome-web-store-no-go-proof.json";
    private const string PostFixRiskRegisterPath = "artifacts/agent-operations/m685/post-fix-risk-register.json";
    private const string M685GoNoGoPath = "artifacts/agent-operations/m685/m685-go-no-go.json";

    private const string ConsolidatedPath = "artifacts/agent-operations/m683-m685/public-variant-manual-qa-after-token-fix-go-no-go.json";
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
    public void M683ArtifactsExist()
    {
        Assert.IsTrue(File.Exists(FullPath(M683ReportPath)), M683ReportPath);
        Assert.IsTrue(File.Exists(FullPath(M683RetryPath)), M683RetryPath);
        Assert.IsTrue(File.Exists(FullPath(TokenSaveStatusProofPath)), TokenSaveStatusProofPath);
        Assert.IsTrue(File.Exists(FullPath(NoTokenRequiredStateProofPath)), NoTokenRequiredStateProofPath);
        Assert.IsTrue(File.Exists(FullPath(BridgeUnreachableStateProofPath)), BridgeUnreachableStateProofPath);
        Assert.IsTrue(File.Exists(FullPath(ReconnectLoopRegressionProofPath)), ReconnectLoopRegressionProofPath);
        Assert.IsTrue(File.Exists(FullPath(M683GoNoGoPath)), M683GoNoGoPath);
    }

    [TestMethod]
    public void M684ArtifactsExist()
    {
        Assert.IsTrue(File.Exists(FullPath(M684ReportPath)), M684ReportPath);
        Assert.IsTrue(File.Exists(FullPath(LiveBridgeTokenEvidencePath)), LiveBridgeTokenEvidencePath);
        Assert.IsTrue(File.Exists(FullPath(RuntimeTabEvidencePath)), RuntimeTabEvidencePath);
        Assert.IsTrue(File.Exists(FullPath(ServiceWorkerDevtoolsEvidencePath)), ServiceWorkerDevtoolsEvidencePath);
        Assert.IsTrue(File.Exists(FullPath(CspConsoleEvidencePath)), CspConsoleEvidencePath);
        Assert.IsTrue(File.Exists(FullPath(PermissionWarningEvidencePath)), PermissionWarningEvidencePath);
        Assert.IsTrue(File.Exists(FullPath(EvidenceRedactionProofPath)), EvidenceRedactionProofPath);
        Assert.IsTrue(File.Exists(FullPath(M684GoNoGoPath)), M684GoNoGoPath);
    }

    [TestMethod]
    public void M685ArtifactsExist()
    {
        Assert.IsTrue(File.Exists(FullPath(M685ReportPath)), M685ReportPath);
        Assert.IsTrue(File.Exists(FullPath(PublicPackageFreezeDecisionPath)), PublicPackageFreezeDecisionPath);
        Assert.IsTrue(File.Exists(FullPath(ManualQaReadinessPath)), ManualQaReadinessPath);
        Assert.IsTrue(File.Exists(FullPath(PublicReleaseNoGoPath)), PublicReleaseNoGoPath);
        Assert.IsTrue(File.Exists(FullPath(ChromeWebStoreNoGoPath)), ChromeWebStoreNoGoPath);
        Assert.IsTrue(File.Exists(FullPath(PostFixRiskRegisterPath)), PostFixRiskRegisterPath);
        Assert.IsTrue(File.Exists(FullPath(M685GoNoGoPath)), M685GoNoGoPath);
    }

    [TestMethod]
    public void ConsolidatedGoNoGoExists() =>
        Assert.IsTrue(File.Exists(FullPath(ConsolidatedPath)), ConsolidatedPath);

    [TestMethod]
    public void TokenFixStatesRemainReadyWithoutSecretExposure()
    {
        using var retry = ReadJson(M683RetryPath);
        using var saved = ReadJson(TokenSaveStatusProofPath);
        using var noToken = ReadJson(NoTokenRequiredStateProofPath);
        using var unreachable = ReadJson(BridgeUnreachableStateProofPath);
        using var reconnect = ReadJson(ReconnectLoopRegressionProofPath);
        Assert.IsTrue(retry.RootElement.GetProperty("bridgeTokenFixReady").GetBoolean());
        Assert.IsTrue(saved.RootElement.GetProperty("tokenPresentStateWorks").GetBoolean());
        Assert.IsTrue(noToken.RootElement.GetProperty("tokenRequiredStateWorks").GetBoolean());
        Assert.IsFalse(noToken.RootElement.GetProperty("webSocketOpenedWithoutToken").GetBoolean());
        Assert.IsTrue(unreachable.RootElement.GetProperty("bridgeUnreachableStateWorks").GetBoolean());
        Assert.IsFalse(unreachable.RootElement.GetProperty("falseInvalidTokenWhenBridgeOff").GetBoolean());
        Assert.IsTrue(reconnect.RootElement.GetProperty("reconnectLoopHardened").GetBoolean());
        Assert.AreEqual(5, reconnect.RootElement.GetProperty("maxReconnectAttempts").GetInt32());
        Assert.IsFalse(saved.RootElement.GetProperty("fullTokenValueStored").GetBoolean());
        Assert.IsFalse(saved.RootElement.GetProperty("fullTokenValueLogged").GetBoolean());
    }

    [TestMethod]
    public void LiveEvidenceIsConditionalAndRedacted()
    {
        using var live = ReadJson(LiveBridgeTokenEvidencePath);
        using var runtime = ReadJson(RuntimeTabEvidencePath);
        using var devtools = ReadJson(ServiceWorkerDevtoolsEvidencePath);
        using var csp = ReadJson(CspConsoleEvidencePath);
        using var redaction = ReadJson(EvidenceRedactionProofPath);
        Assert.IsFalse(live.RootElement.GetProperty("validTokenConnectedLiveEvidence").GetBoolean());
        Assert.IsFalse(runtime.RootElement.GetProperty("runtimeTabEvidenceReady").GetBoolean());
        Assert.IsFalse(devtools.RootElement.GetProperty("serviceWorkerDevtoolsEvidenceReady").GetBoolean());
        Assert.IsFalse(csp.RootElement.GetProperty("cspConsoleEvidenceReady").GetBoolean());
        Assert.IsTrue(redaction.RootElement.GetProperty("evidenceRedacted").GetBoolean());
        Assert.IsFalse(redaction.RootElement.GetProperty("fullTokenValueStored").GetBoolean());
        Assert.IsFalse(redaction.RootElement.GetProperty("bridgeSecretsLeaked").GetBoolean());
    }

    [TestMethod]
    public void FreezeReleaseAndStoreRemainNoGo()
    {
        using var freeze = ReadJson(PublicPackageFreezeDecisionPath);
        using var readiness = ReadJson(ManualQaReadinessPath);
        using var release = ReadJson(PublicReleaseNoGoPath);
        using var store = ReadJson(ChromeWebStoreNoGoPath);
        using var risks = ReadJson(PostFixRiskRegisterPath);
        Assert.IsFalse(freeze.RootElement.GetProperty("packageFreezeReady").GetBoolean());
        Assert.IsFalse(freeze.RootElement.GetProperty("publicReleaseReady").GetBoolean());
        Assert.IsFalse(freeze.RootElement.GetProperty("chromeWebStoreReady").GetBoolean());
        Assert.IsTrue(readiness.RootElement.GetProperty("manualQaUnblocked").GetBoolean());
        Assert.IsFalse(readiness.RootElement.GetProperty("validTokenConnectedLiveEvidence").GetBoolean());
        Assert.IsFalse(release.RootElement.GetProperty("publicReleaseReady").GetBoolean());
        Assert.IsFalse(store.RootElement.GetProperty("chromeWebStoreReady").GetBoolean());
        Assert.IsGreaterThanOrEqualTo(3, risks.RootElement.GetProperty("risks").GetArrayLength());
    }

    [TestMethod]
    public void ConsolidatedDecisionIsAllowedAndCapabilitiesStayDisabled()
    {
        using var doc = ReadJson(ConsolidatedPath);
        var decision = doc.RootElement.GetProperty("decision").GetString();
        CollectionAssert.Contains(new[]
        {
            "PUBLIC_VARIANT_MANUAL_QA_AFTER_TOKEN_FIX_READY",
            "PUBLIC_VARIANT_MANUAL_QA_AFTER_TOKEN_FIX_CONDITIONAL_ENVIRONMENT",
            "BRIDGE_TOKEN_QA_REMEDIATION_REQUIRED"
        }, decision);

        Assert.AreEqual("PUBLIC_VARIANT_MANUAL_QA_AFTER_TOKEN_FIX_CONDITIONAL_ENVIRONMENT", decision);
        Assert.IsTrue(doc.RootElement.GetProperty("internalCandidateReady").GetBoolean());
        Assert.IsTrue(doc.RootElement.GetProperty("publicBuildCandidateReady").GetBoolean());
        Assert.IsTrue(doc.RootElement.GetProperty("bridgeTokenFixReady").GetBoolean());
        Assert.IsTrue(doc.RootElement.GetProperty("tokenRequiredStateWorks").GetBoolean());
        Assert.IsTrue(doc.RootElement.GetProperty("tokenPresentStateWorks").GetBoolean());
        Assert.IsTrue(doc.RootElement.GetProperty("reconnectLoopHardened").GetBoolean());
        Assert.IsFalse(doc.RootElement.GetProperty("tokenLoggedInFull").GetBoolean());
        Assert.IsFalse(doc.RootElement.GetProperty("bridgeSecretsLeaked").GetBoolean());
        Assert.IsFalse(doc.RootElement.GetProperty("manualQaPassed").GetBoolean());
        Assert.IsFalse(doc.RootElement.GetProperty("packageFreezeReady").GetBoolean());
        Assert.IsFalse(doc.RootElement.GetProperty("publicReleaseReady").GetBoolean());
        Assert.IsFalse(doc.RootElement.GetProperty("chromeWebStoreReady").GetBoolean());
        Assert.IsFalse(doc.RootElement.GetProperty("runtimeProductiveEnabled").GetBoolean());
        Assert.IsFalse(doc.RootElement.GetProperty("providerCloudEnabled").GetBoolean());
        Assert.IsFalse(doc.RootElement.GetProperty("filesystemEnabled").GetBoolean());
        Assert.IsFalse(doc.RootElement.GetProperty("browserAutomationEnabled").GetBoolean());
        Assert.IsFalse(doc.RootElement.GetProperty("capabilityUnlockEnabled").GetBoolean());
        Assert.IsFalse(doc.RootElement.GetProperty("bridgeModified").GetBoolean());
        Assert.IsFalse(doc.RootElement.GetProperty("cspModified").GetBoolean());
    }

    [TestMethod]
    public void PerMilestoneGoNoGosKeepCapabilitiesDisabled()
    {
        foreach (var path in new[] { M683GoNoGoPath, M684GoNoGoPath, M685GoNoGoPath, ConsolidatedPath })
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
