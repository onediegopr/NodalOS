using System.Text.Json;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace OneBrain.Safety.Tests;

[TestClass]
[TestCategory("BridgeTokenConnection")]
[TestCategory("M680")]
[TestCategory("M681")]
[TestCategory("M682")]
public sealed class NodalOsBridgeTokenConnectionM680M682Tests
{
    private const string M680ReportPath = "docs/reports/m680-bridge-token-storage-connection-diagnosis.md";
    private const string M681ReportPath = "docs/reports/m681-bridge-token-connection-loop-bugfix.md";
    private const string M682ReportPath = "docs/reports/m682-bridge-token-connection-qa.md";

    private const string M680DiagnosisPath = "artifacts/agent-operations/m680/bridge-token-storage-diagnosis.json";
    private const string TokenPersistenceFlowPath = "artifacts/agent-operations/m680/token-persistence-flow-map.json";
    private const string WebSocketFlowPath = "artifacts/agent-operations/m680/websocket-connect-flow-map.json";
    private const string PublicPermissionDiagnosisPath = "artifacts/agent-operations/m680/public-variant-permission-diagnosis.json";
    private const string ReconnectRootCausePath = "artifacts/agent-operations/m680/reconnect-loop-root-cause.json";
    private const string M680GoNoGoPath = "artifacts/agent-operations/m680/m680-go-no-go.json";

    private const string BugfixPath = "artifacts/agent-operations/m681/bridge-token-bugfix.json";
    private const string TokenStorageProofPath = "artifacts/agent-operations/m681/token-storage-fix-proof.json";
    private const string WebSocketProofPath = "artifacts/agent-operations/m681/websocket-connect-fix-proof.json";
    private const string ReconnectProofPath = "artifacts/agent-operations/m681/reconnect-loop-fix-proof.json";
    private const string PublicConnectionProofPath = "artifacts/agent-operations/m681/public-variant-connection-fix-proof.json";
    private const string M681GoNoGoPath = "artifacts/agent-operations/m681/m681-go-no-go.json";

    private const string BridgeTokenQaPath = "artifacts/agent-operations/m682/bridge-token-qa.json";
    private const string TokenSaveReadQaPath = "artifacts/agent-operations/m682/token-save-read-qa.json";
    private const string BridgeUnreachableQaPath = "artifacts/agent-operations/m682/bridge-unreachable-qa.json";
    private const string InvalidTokenQaPath = "artifacts/agent-operations/m682/invalid-token-qa.json";
    private const string ValidTokenConnectedQaPath = "artifacts/agent-operations/m682/valid-token-connected-qa.json";
    private const string ReconnectLoopRegressionQaPath = "artifacts/agent-operations/m682/reconnect-loop-regression-qa.json";
    private const string ManualQaUnblockStatusPath = "artifacts/agent-operations/m682/manual-qa-unblock-status.json";
    private const string M682GoNoGoPath = "artifacts/agent-operations/m682/m682-go-no-go.json";

    private const string ConsolidatedGoNoGoPath = "artifacts/agent-operations/m680-m682/bridge-token-connection-bugfix-go-no-go.json";
    private const string PublicManifestPath = "browser-extension/onebrain-chrome-lab/manifest.public.json";
    private const string ServiceWorkerPath = "browser-extension/onebrain-chrome-lab/service_worker.js";
    private const string SidepanelPath = "browser-extension/onebrain-chrome-lab/sidepanel.js";

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

    private static string ReadText(string relativePath) =>
        File.ReadAllText(FullPath(relativePath));

    private static string[] StringArray(JsonElement element) =>
        element.EnumerateArray().Select(item => item.GetString() ?? string.Empty).ToArray();

    [TestMethod]
    public void M680ArtifactsExist()
    {
        foreach (var path in new[]
        {
            M680DiagnosisPath,
            TokenPersistenceFlowPath,
            WebSocketFlowPath,
            PublicPermissionDiagnosisPath,
            ReconnectRootCausePath,
            M680GoNoGoPath
        })
            Assert.IsTrue(File.Exists(FullPath(path)), path);
    }

    [TestMethod]
    public void M681ArtifactsExist()
    {
        foreach (var path in new[]
        {
            BugfixPath,
            TokenStorageProofPath,
            WebSocketProofPath,
            ReconnectProofPath,
            PublicConnectionProofPath,
            M681GoNoGoPath
        })
            Assert.IsTrue(File.Exists(FullPath(path)), path);
    }

    [TestMethod]
    public void M682ArtifactsExist()
    {
        foreach (var path in new[]
        {
            BridgeTokenQaPath,
            TokenSaveReadQaPath,
            BridgeUnreachableQaPath,
            InvalidTokenQaPath,
            ValidTokenConnectedQaPath,
            ReconnectLoopRegressionQaPath,
            ManualQaUnblockStatusPath,
            M682GoNoGoPath
        })
            Assert.IsTrue(File.Exists(FullPath(path)), path);
    }

    [TestMethod]
    public void ConsolidatedM680M682GoNoGoExists() =>
        Assert.IsTrue(File.Exists(FullPath(ConsolidatedGoNoGoPath)), ConsolidatedGoNoGoPath);

    [TestMethod]
    public void ReportsAndFlowMapsExist()
    {
        foreach (var path in new[]
        {
            M680ReportPath,
            M681ReportPath,
            M682ReportPath,
            TokenPersistenceFlowPath,
            WebSocketFlowPath,
            PublicPermissionDiagnosisPath,
            ReconnectRootCausePath
        })
            Assert.IsTrue(File.Exists(FullPath(path)), path);
    }

    [TestMethod]
    public void BugfixProofArtifactsAssertCoreFixes()
    {
        using var bugfix = ReadJson(BugfixPath);
        using var token = ReadJson(TokenStorageProofPath);
        using var socket = ReadJson(WebSocketProofPath);
        using var reconnect = ReadJson(ReconnectProofPath);
        using var publicVariant = ReadJson(PublicConnectionProofPath);

        Assert.IsTrue(bugfix.RootElement.GetProperty("tokenPersistenceFixed").GetBoolean());
        Assert.IsTrue(bugfix.RootElement.GetProperty("tokenStatusAccurate").GetBoolean());
        Assert.IsTrue(bugfix.RootElement.GetProperty("websocketConnectRaceFixed").GetBoolean());
        Assert.IsTrue(bugfix.RootElement.GetProperty("reconnectLoopHardened").GetBoolean());
        Assert.IsTrue(token.RootElement.GetProperty("runtimeSnapshotHasTokenBoolean").GetBoolean());
        Assert.IsFalse(token.RootElement.GetProperty("runtimeSnapshotContainsTokenValue").GetBoolean());
        Assert.IsTrue(socket.RootElement.GetProperty("missingTokenGateBeforeWebSocket").GetBoolean());
        Assert.AreEqual("token_required", socket.RootElement.GetProperty("tokenRequiredDiagnostic").GetString());
        Assert.AreEqual("invalid_token", socket.RootElement.GetProperty("invalidTokenDiagnostic").GetString());
        Assert.AreEqual("bridge_unreachable", socket.RootElement.GetProperty("bridgeUnreachableDiagnostic").GetString());
        Assert.IsTrue(reconnect.RootElement.GetProperty("aggressiveLoopPrevented").GetBoolean());
        Assert.AreEqual(5, reconnect.RootElement.GetProperty("maxReconnectAttempts").GetInt32());
        Assert.IsTrue(publicVariant.RootElement.GetProperty("publicVariantConnectionReady").GetBoolean());
    }

    [TestMethod]
    public void QaArtifactsCoverRequiredCases()
    {
        using var qa = ReadJson(BridgeTokenQaPath);
        using var saveRead = ReadJson(TokenSaveReadQaPath);
        using var unreachable = ReadJson(BridgeUnreachableQaPath);
        using var invalid = ReadJson(InvalidTokenQaPath);
        using var valid = ReadJson(ValidTokenConnectedQaPath);
        using var loop = ReadJson(ReconnectLoopRegressionQaPath);
        using var unblock = ReadJson(ManualQaUnblockStatusPath);

        Assert.AreEqual("PASS_STATIC", qa.RootElement.GetProperty("caseAWithoutToken").GetString());
        Assert.AreEqual("PASS_STATIC", qa.RootElement.GetProperty("caseBTokenSaved").GetString());
        Assert.AreEqual("PASS_STATIC", qa.RootElement.GetProperty("caseCBridgeOff").GetString());
        Assert.AreEqual("PASS_STATIC", qa.RootElement.GetProperty("caseDInvalidToken").GetString());
        StringAssert.StartsWith(valid.RootElement.GetProperty("status").GetString(), "CONDITIONAL");
        Assert.IsFalse(valid.RootElement.GetProperty("healthOkVerified").GetBoolean());
        Assert.IsFalse(valid.RootElement.GetProperty("webSocketConnectedVerified").GetBoolean());
        Assert.IsTrue(saveRead.RootElement.GetProperty("serviceWorkerCanReadToken").GetBoolean());
        Assert.IsTrue(unreachable.RootElement.GetProperty("doesNotReportTokenMissingWhenTokenPresent").GetBoolean());
        Assert.AreEqual("replace token", invalid.RootElement.GetProperty("recommendedAction").GetString());
        Assert.IsTrue(loop.RootElement.GetProperty("missingTokenNoLoop").GetBoolean());
        Assert.IsTrue(unblock.RootElement.GetProperty("manualQaUnblocked").GetBoolean());
    }

    [TestMethod]
    public void GoNoGosKeepProductReleaseNoGoAndSensitiveCapabilitiesDisabled()
    {
        foreach (var path in new[] { M680GoNoGoPath, M681GoNoGoPath, M682GoNoGoPath, ConsolidatedGoNoGoPath })
        {
            using var doc = ReadJson(path);
            Assert.IsFalse(doc.RootElement.GetProperty("publicReleaseReady").GetBoolean(), path);
            Assert.IsFalse(doc.RootElement.GetProperty("chromeWebStoreReady").GetBoolean(), path);
            Assert.IsFalse(doc.RootElement.GetProperty("runtimeProductiveEnabled").GetBoolean(), path);
            Assert.IsFalse(doc.RootElement.GetProperty("providerCloudEnabled").GetBoolean(), path);
            Assert.IsFalse(doc.RootElement.GetProperty("filesystemEnabled").GetBoolean(), path);
            Assert.IsFalse(doc.RootElement.GetProperty("browserAutomationEnabled").GetBoolean(), path);
            Assert.IsFalse(doc.RootElement.GetProperty("capabilityUnlockEnabled").GetBoolean(), path);
            Assert.IsFalse(doc.RootElement.GetProperty("bridgeSecretsLeaked").GetBoolean(), path);
            Assert.IsFalse(doc.RootElement.GetProperty("tokenLoggedInFull").GetBoolean(), path);
        }
    }

    [TestMethod]
    public void ConsolidatedDecisionIsConditionalReady()
    {
        using var doc = ReadJson(ConsolidatedGoNoGoPath);
        Assert.AreEqual("BRIDGE_TOKEN_CONNECTION_LOOP_CONDITIONAL_READY", doc.RootElement.GetProperty("decision").GetString());
        Assert.IsTrue(doc.RootElement.GetProperty("rootCauseIdentified").GetBoolean());
        Assert.IsTrue(doc.RootElement.GetProperty("tokenPersistenceFixed").GetBoolean());
        Assert.IsTrue(doc.RootElement.GetProperty("tokenStatusAccurate").GetBoolean());
        Assert.IsTrue(doc.RootElement.GetProperty("websocketConnectRaceFixed").GetBoolean());
        Assert.IsTrue(doc.RootElement.GetProperty("reconnectLoopHardened").GetBoolean());
        Assert.IsTrue(doc.RootElement.GetProperty("publicVariantConnectionReady").GetBoolean());
        Assert.IsTrue(doc.RootElement.GetProperty("manualQaUnblocked").GetBoolean());
        Assert.IsFalse(doc.RootElement.GetProperty("validTokenConnectedLiveEvidence").GetBoolean());
        Assert.IsFalse(doc.RootElement.GetProperty("manifestPublicHasWildcards").GetBoolean());
        Assert.IsFalse(doc.RootElement.GetProperty("contentScriptsWildcardInPublicManifest").GetBoolean());
        Assert.IsFalse(doc.RootElement.GetProperty("bridgeModified").GetBoolean());
        Assert.IsFalse(doc.RootElement.GetProperty("cspModified").GetBoolean());
    }

    [TestMethod]
    public void ManifestPublicJsonRemainsValidAndNarrowed()
    {
        using var doc = ReadJson(PublicManifestPath);
        Assert.AreEqual(3, doc.RootElement.GetProperty("manifest_version").GetInt32());
        var permissions = StringArray(doc.RootElement.GetProperty("permissions"));
        CollectionAssert.Contains(permissions, "storage");

        var hostPermissions = StringArray(doc.RootElement.GetProperty("host_permissions"));
        CollectionAssert.Contains(hostPermissions, "http://127.0.0.1/*");
        CollectionAssert.Contains(hostPermissions, "http://localhost/*");
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

    [TestMethod]
    public void MissingTokenConnectLoopIsNotAggressiveOrInfinite()
    {
        var serviceWorker = ReadText(ServiceWorkerPath);
        StringAssert.Contains(serviceWorker, "blockReconnect('tokenRequired', 'token_required')");
        StringAssert.Contains(serviceWorker, "throw new Error('token_required')");
        StringAssert.Contains(serviceWorker, "MAX_RECONNECT_ATTEMPTS = 5");

        using var flow = ReadJson(WebSocketFlowPath);
        using var reconnect = ReadJson(ReconnectProofPath);
        Assert.IsFalse(flow.RootElement.GetProperty("withoutToken").GetProperty("opensWebSocket").GetBoolean());
        Assert.IsFalse(flow.RootElement.GetProperty("withoutToken").GetProperty("aggressiveLoop").GetBoolean());
        Assert.IsFalse(reconnect.RootElement.GetProperty("missingTokenSchedulesReconnect").GetBoolean());
    }

    [TestMethod]
    public void TokenPresentUiStateIsRepresentable()
    {
        var serviceWorker = ReadText(ServiceWorkerPath);
        var sidepanel = ReadText(SidepanelPath);
        StringAssert.Contains(serviceWorker, "hasToken: hasSavedToken(config)");
        StringAssert.Contains(serviceWorker, "tokenStatus: connectionState === 'tokenError' ? 'invalid' : hasSavedToken(config) ? 'present' : 'missing'");
        StringAssert.Contains(sidepanel, "connection.tokenStatus === 'present' || connection.hasToken");

        using var flow = ReadJson(TokenPersistenceFlowPath);
        Assert.IsTrue(flow.RootElement.GetProperty("uiRepresentableAsTokenPresent").GetBoolean());
    }

    [TestMethod]
    public void InvalidTokenAndBridgeUnreachableDiagnosticsAreDistinct()
    {
        var serviceWorker = ReadText(ServiceWorkerPath);
        var sidepanel = ReadText(SidepanelPath);
        StringAssert.Contains(serviceWorker, "blockReconnect('tokenError', 'invalid_token')");
        StringAssert.Contains(serviceWorker, "setConnectionState('error', 'bridge_unreachable'");
        StringAssert.Contains(sidepanel, "return 'invalid_token'");
        StringAssert.Contains(sidepanel, "return 'bridge_unreachable'");

        using var invalid = ReadJson(InvalidTokenQaPath);
        using var unreachable = ReadJson(BridgeUnreachableQaPath);
        Assert.AreEqual("invalid_token", invalid.RootElement.GetProperty("diagnostic").GetString());
        Assert.AreEqual("bridge_unreachable", unreachable.RootElement.GetProperty("diagnostic").GetString());
    }

    [TestMethod]
    public void FixDoesNotModifyBridgeOrCspByDecision()
    {
        using var bugfix = ReadJson(BugfixPath);
        using var consolidated = ReadJson(ConsolidatedGoNoGoPath);
        using var permissions = ReadJson(PublicPermissionDiagnosisPath);
        Assert.IsFalse(bugfix.RootElement.GetProperty("bridgeModified").GetBoolean());
        Assert.IsFalse(bugfix.RootElement.GetProperty("cspModified").GetBoolean());
        Assert.IsFalse(consolidated.RootElement.GetProperty("bridgeModified").GetBoolean());
        Assert.IsFalse(consolidated.RootElement.GetProperty("cspModified").GetBoolean());
        Assert.IsFalse(permissions.RootElement.GetProperty("cspModified").GetBoolean());
    }

    [TestMethod]
    public void ReportsAndArtifactsDoNotContainForbiddenSecretMarkers()
    {
        var paths = new[]
        {
            M680ReportPath,
            M681ReportPath,
            M682ReportPath,
            M680DiagnosisPath,
            TokenPersistenceFlowPath,
            WebSocketFlowPath,
            PublicPermissionDiagnosisPath,
            ReconnectRootCausePath,
            BugfixPath,
            TokenStorageProofPath,
            WebSocketProofPath,
            ReconnectProofPath,
            PublicConnectionProofPath,
            BridgeTokenQaPath,
            TokenSaveReadQaPath,
            BridgeUnreachableQaPath,
            InvalidTokenQaPath,
            ValidTokenConnectedQaPath,
            ReconnectLoopRegressionQaPath,
            ManualQaUnblockStatusPath,
            ConsolidatedGoNoGoPath
        };

        foreach (var path in paths)
        {
            var text = ReadText(path);
            var leakText = text.Replace("invalid_token", "invalid-token-diagnostic", StringComparison.OrdinalIgnoreCase);
            Assert.IsFalse(text.Contains("Bearer ", StringComparison.Ordinal), path);
            Assert.IsFalse(leakText.Contains("api_key", StringComparison.OrdinalIgnoreCase), path);
            Assert.IsFalse(leakText.Contains("access_token", StringComparison.OrdinalIgnoreCase), path);
            Assert.IsFalse(leakText.Contains("refresh_token", StringComparison.OrdinalIgnoreCase), path);
            Assert.IsFalse(leakText.Contains("id_token", StringComparison.OrdinalIgnoreCase), path);
            Assert.IsFalse(leakText.Contains("set-cookie", StringComparison.OrdinalIgnoreCase), path);
        }
    }
}
