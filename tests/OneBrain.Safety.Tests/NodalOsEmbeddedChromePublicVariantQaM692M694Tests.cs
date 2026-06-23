using System.Text.Json;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace OneBrain.Safety.Tests;

[TestClass]
[TestCategory("EmbeddedChromePublicVariantQa")]
[TestCategory("M692")]
[TestCategory("M693")]
[TestCategory("M694")]
public sealed class NodalOsEmbeddedChromePublicVariantQaM692M694Tests
{
    private const string M692ReportPath = "docs/reports/m692-embedded-chrome-public-variant-load.md";
    private const string M693ReportPath = "docs/reports/m693-token-websocket-runtime-devtools-evidence.md";
    private const string M694ReportPath = "docs/reports/m694-package-freeze-decision-after-embedded-chrome-qa.md";

    private const string EmbeddedChromeLoadPath = "artifacts/agent-operations/m692/embedded-chrome-public-variant-load.json";
    private const string ChromeExtensionsAccessPath = "artifacts/agent-operations/m692/chrome-extensions-access-proof.json";
    private const string DeveloperModeLoadPath = "artifacts/agent-operations/m692/developer-mode-load-unpacked-proof.json";
    private const string PublicStagingLoadedPath = "artifacts/agent-operations/m692/public-staging-folder-loaded-proof.json";
    private const string PublicExtensionVisiblePath = "artifacts/agent-operations/m692/public-extension-visible-proof.json";
    private const string BrowserBlockerPath = "artifacts/agent-operations/m692/browser-blocker-proof.json";
    private const string M692GoNoGoPath = "artifacts/agent-operations/m692/m692-go-no-go.json";

    private const string BridgeStartupProofPath = "artifacts/agent-operations/m693/bridge-startup-proof.json";
    private const string TokenPresentUiProofPath = "artifacts/agent-operations/m693/token-present-ui-proof.json";
    private const string WebsocketConnectedProofPath = "artifacts/agent-operations/m693/websocket-connected-proof.json";
    private const string RuntimeTabProofPath = "artifacts/agent-operations/m693/runtime-tab-proof.json";
    private const string ServiceWorkerDevtoolsProofPath = "artifacts/agent-operations/m693/service-worker-devtools-proof.json";
    private const string CspConsoleProofPath = "artifacts/agent-operations/m693/csp-console-proof.json";
    private const string PermissionWarningProofPath = "artifacts/agent-operations/m693/permission-warning-proof.json";
    private const string ReconnectLoopProofPath = "artifacts/agent-operations/m693/reconnect-loop-proof.json";
    private const string EvidenceRedactionProofPath = "artifacts/agent-operations/m693/evidence-redaction-proof.json";
    private const string M693GoNoGoPath = "artifacts/agent-operations/m693/m693-go-no-go.json";

    private const string PackageFreezeDecisionPath = "artifacts/agent-operations/m694/package-freeze-decision-after-embedded-chrome-qa.json";
    private const string ManualQaCompletenessPath = "artifacts/agent-operations/m694/manual-qa-completeness-after-embedded-chrome.json";
    private const string PublicReleaseNoGoPath = "artifacts/agent-operations/m694/public-release-no-go-proof.json";
    private const string ChromeWebStoreNoGoPath = "artifacts/agent-operations/m694/chrome-web-store-no-go-proof.json";
    private const string PostEmbeddedRiskRegisterPath = "artifacts/agent-operations/m694/post-embedded-chrome-risk-register.json";
    private const string M694GoNoGoPath = "artifacts/agent-operations/m694/m694-go-no-go.json";

    private const string ConsolidatedPath = "artifacts/agent-operations/m692-m694/embedded-chrome-public-variant-qa-go-no-go.json";
    private const string ManifestPath = "browser-extension/onebrain-chrome-lab/manifest.json";
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

    private static string ReadText(string relativePath) =>
        File.ReadAllText(FullPath(relativePath));

    private static string[] StringArray(JsonElement element) =>
        element.EnumerateArray().Select(item => item.GetString() ?? string.Empty).ToArray();

    [TestMethod]
    public void M692ArtifactsExist()
    {
        foreach (var path in new[]
        {
            M692ReportPath,
            EmbeddedChromeLoadPath,
            ChromeExtensionsAccessPath,
            DeveloperModeLoadPath,
            PublicStagingLoadedPath,
            PublicExtensionVisiblePath,
            BrowserBlockerPath,
            M692GoNoGoPath
        })
            Assert.IsTrue(File.Exists(FullPath(path)), path);
    }

    [TestMethod]
    public void M693ArtifactsExist()
    {
        foreach (var path in new[]
        {
            M693ReportPath,
            BridgeStartupProofPath,
            TokenPresentUiProofPath,
            WebsocketConnectedProofPath,
            RuntimeTabProofPath,
            ServiceWorkerDevtoolsProofPath,
            CspConsoleProofPath,
            PermissionWarningProofPath,
            ReconnectLoopProofPath,
            EvidenceRedactionProofPath,
            M693GoNoGoPath
        })
            Assert.IsTrue(File.Exists(FullPath(path)), path);
    }

    [TestMethod]
    public void M694ArtifactsExist()
    {
        foreach (var path in new[]
        {
            M694ReportPath,
            PackageFreezeDecisionPath,
            ManualQaCompletenessPath,
            PublicReleaseNoGoPath,
            ChromeWebStoreNoGoPath,
            PostEmbeddedRiskRegisterPath,
            M694GoNoGoPath
        })
            Assert.IsTrue(File.Exists(FullPath(path)), path);
    }

    [TestMethod]
    public void ConsolidatedExists() =>
        Assert.IsTrue(File.Exists(FullPath(ConsolidatedPath)), ConsolidatedPath);

    [TestMethod]
    public void M692ProofsRecordEmbeddedChromeBlockerWithoutInventedPass()
    {
        using var access = ReadJson(ChromeExtensionsAccessPath);
        using var developer = ReadJson(DeveloperModeLoadPath);
        using var staging = ReadJson(PublicStagingLoadedPath);
        using var visible = ReadJson(PublicExtensionVisiblePath);
        using var blocker = ReadJson(BrowserBlockerPath);

        Assert.IsFalse(access.RootElement.GetProperty("accessible").GetBoolean());
        Assert.IsTrue(access.RootElement.GetProperty("blocked").GetBoolean());
        Assert.AreEqual("BROWSER_USE_URL_POLICY", access.RootElement.GetProperty("blockerType").GetString());
        Assert.IsFalse(developer.RootElement.GetProperty("developerModeAvailable").GetBoolean());
        Assert.IsFalse(developer.RootElement.GetProperty("loadUnpackedAvailable").GetBoolean());
        Assert.IsTrue(staging.RootElement.GetProperty("stagingFolderExists").GetBoolean());
        Assert.IsTrue(staging.RootElement.GetProperty("stagingManifestValidJson").GetBoolean());
        Assert.IsFalse(staging.RootElement.GetProperty("loadedInEmbeddedChrome").GetBoolean());
        Assert.IsFalse(visible.RootElement.GetProperty("publicExtensionVisible").GetBoolean());
        Assert.IsTrue(blocker.RootElement.GetProperty("blocked").GetBoolean());
        Assert.IsFalse(blocker.RootElement.GetProperty("workaroundAttempted").GetBoolean());
    }

    [TestMethod]
    public void M693ProofsAreNotExecutedDueToBrowserBlocker()
    {
        foreach (var path in new[]
        {
            BridgeStartupProofPath,
            TokenPresentUiProofPath,
            WebsocketConnectedProofPath,
            RuntimeTabProofPath,
            ServiceWorkerDevtoolsProofPath,
            CspConsoleProofPath,
            PermissionWarningProofPath,
            ReconnectLoopProofPath
        })
        {
            using var doc = ReadJson(path);
            Assert.AreEqual("NOT_EXECUTED_DUE_BROWSER_BLOCKER", doc.RootElement.GetProperty("status").GetString(), path);
        }

        using var redaction = ReadJson(EvidenceRedactionProofPath);
        Assert.IsTrue(redaction.RootElement.GetProperty("evidenceRedacted").GetBoolean());
        Assert.IsFalse(redaction.RootElement.GetProperty("tokenLoggedInFull").GetBoolean());
        Assert.IsFalse(redaction.RootElement.GetProperty("bridgeSecretsLeaked").GetBoolean());
    }

    [TestMethod]
    public void M694PackageFreezeAndReleaseGatesRemainNoGo()
    {
        using var freeze = ReadJson(PackageFreezeDecisionPath);
        using var completeness = ReadJson(ManualQaCompletenessPath);
        using var publicRelease = ReadJson(PublicReleaseNoGoPath);
        using var store = ReadJson(ChromeWebStoreNoGoPath);

        Assert.AreEqual("PUBLIC_PACKAGE_FREEZE_NO_GO", freeze.RootElement.GetProperty("decision").GetString());
        Assert.IsFalse(freeze.RootElement.GetProperty("publicPackageFreezeReady").GetBoolean());
        Assert.IsFalse(completeness.RootElement.GetProperty("manualQaEvidenceSufficient").GetBoolean());
        Assert.IsTrue(completeness.RootElement.GetProperty("humanInputRequired").GetBoolean());
        Assert.IsFalse(publicRelease.RootElement.GetProperty("publicReleaseReady").GetBoolean());
        Assert.IsFalse(store.RootElement.GetProperty("chromeWebStoreReady").GetBoolean());
        Assert.IsFalse(store.RootElement.GetProperty("packageFreezeReady").GetBoolean());
    }

    [TestMethod]
    public void ConsolidatedDecisionIsAllowedAndBlockedBranchRequiresHumanInput()
    {
        using var doc = ReadJson(ConsolidatedPath);
        var decision = doc.RootElement.GetProperty("decision").GetString();
        CollectionAssert.Contains(new[]
        {
            "EMBEDDED_CHROME_PUBLIC_VARIANT_EVIDENCE_READY",
            "BROWSER_EMBEDDED_CHROME_BLOCKED",
            "EMBEDDED_CHROME_PUBLIC_VARIANT_REMEDIATION_REQUIRED"
        }, decision);

        Assert.AreEqual("BROWSER_EMBEDDED_CHROME_BLOCKED", decision);
        Assert.IsTrue(doc.RootElement.GetProperty("humanInputRequired").GetBoolean());
        Assert.IsFalse(doc.RootElement.GetProperty("publicPackageFreezeReady").GetBoolean());
        Assert.IsFalse(doc.RootElement.GetProperty("embeddedChromeEvidenceReady").GetBoolean());
        Assert.IsFalse(doc.RootElement.GetProperty("manualQaEvidenceSufficient").GetBoolean());
    }

    [TestMethod]
    public void ConsolidatedEvidenceReadyBranchWouldRequireLiveProofs()
    {
        using var doc = ReadJson(ConsolidatedPath);
        if (doc.RootElement.GetProperty("decision").GetString() != "EMBEDDED_CHROME_PUBLIC_VARIANT_EVIDENCE_READY")
            return;

        Assert.IsTrue(doc.RootElement.GetProperty("chromeExtensionsAccessible").GetBoolean());
        Assert.IsTrue(doc.RootElement.GetProperty("developerModeAvailable").GetBoolean());
        Assert.IsTrue(doc.RootElement.GetProperty("loadUnpackedAvailable").GetBoolean());
        Assert.IsTrue(doc.RootElement.GetProperty("publicVariantLoaded").GetBoolean());
        Assert.IsTrue(doc.RootElement.GetProperty("tokenPresentUi").GetBoolean());
        Assert.IsTrue(doc.RootElement.GetProperty("websocketConnectedLive").GetBoolean());
        Assert.IsTrue(doc.RootElement.GetProperty("runtimeTabEvidenceReady").GetBoolean());
        Assert.IsTrue(doc.RootElement.GetProperty("serviceWorkerDevtoolsEvidenceReady").GetBoolean());
        Assert.IsTrue(doc.RootElement.GetProperty("manualQaEvidenceSufficient").GetBoolean());
    }

    [TestMethod]
    public void GoNoGosKeepReleaseAndSensitiveCapabilitiesDisabled()
    {
        foreach (var path in new[] { M692GoNoGoPath, M693GoNoGoPath, M694GoNoGoPath, ConsolidatedPath })
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
            Assert.IsTrue(doc.RootElement.GetProperty("evidenceRedacted").GetBoolean(), path);
        }
    }

    [TestMethod]
    public void ManifestsRemainValidJsonAndHostPermissionsAreNarrow()
    {
        AssertManifestIsValidJson(ManifestPath);
        AssertManifestHasNoHostWildcards(PublicManifestPath);
        AssertManifestHasNoHostWildcards(StagingManifestPath);
    }

    [TestMethod]
    public void PublicAndStagingManifestsDoNotDeclareContentScriptWildcards()
    {
        AssertNoContentScriptWildcards(PublicManifestPath);
        AssertNoContentScriptWildcards(StagingManifestPath);
    }

    [TestMethod]
    public void ReportsAndArtifactsDoNotContainForbiddenSecretMarkers()
    {
        foreach (var path in new[]
        {
            M692ReportPath,
            M693ReportPath,
            M694ReportPath,
            EmbeddedChromeLoadPath,
            ChromeExtensionsAccessPath,
            DeveloperModeLoadPath,
            PublicStagingLoadedPath,
            PublicExtensionVisiblePath,
            BrowserBlockerPath,
            BridgeStartupProofPath,
            TokenPresentUiProofPath,
            WebsocketConnectedProofPath,
            RuntimeTabProofPath,
            ServiceWorkerDevtoolsProofPath,
            CspConsoleProofPath,
            PermissionWarningProofPath,
            ReconnectLoopProofPath,
            EvidenceRedactionProofPath,
            PackageFreezeDecisionPath,
            ManualQaCompletenessPath,
            PublicReleaseNoGoPath,
            ChromeWebStoreNoGoPath,
            PostEmbeddedRiskRegisterPath,
            ConsolidatedPath
        })
        {
            var text = ReadText(path);
            Assert.IsFalse(text.Contains("Bearer ", StringComparison.Ordinal), path);
            Assert.IsFalse(text.Contains("api_key", StringComparison.OrdinalIgnoreCase), path);
            Assert.IsFalse(text.Contains("access_token", StringComparison.OrdinalIgnoreCase), path);
            Assert.IsFalse(text.Contains("refresh_token", StringComparison.OrdinalIgnoreCase), path);
            Assert.IsFalse(text.Contains("id_token", StringComparison.OrdinalIgnoreCase), path);
            Assert.IsFalse(text.Contains("cookie", StringComparison.OrdinalIgnoreCase), path);
            Assert.IsFalse(text.Contains("set-cookie", StringComparison.OrdinalIgnoreCase), path);
        }
    }

    private static void AssertManifestHasNoHostWildcards(string relativePath)
    {
        using var doc = AssertManifestIsValidJson(relativePath);
        var hostPermissions = StringArray(doc.RootElement.GetProperty("host_permissions"));
        CollectionAssert.DoesNotContain(hostPermissions, "http://*/*", relativePath);
        CollectionAssert.DoesNotContain(hostPermissions, "https://*/*", relativePath);
        CollectionAssert.DoesNotContain(hostPermissions, "<all_urls>", relativePath);
    }

    private static JsonDocument AssertManifestIsValidJson(string relativePath)
    {
        var doc = ReadJson(relativePath);
        Assert.AreEqual(3, doc.RootElement.GetProperty("manifest_version").GetInt32(), relativePath);
        return doc;
    }

    private static void AssertNoContentScriptWildcards(string relativePath)
    {
        using var doc = ReadJson(relativePath);
        if (!doc.RootElement.TryGetProperty("content_scripts", out var contentScripts))
            return;

        foreach (var contentScript in contentScripts.EnumerateArray())
        {
            if (!contentScript.TryGetProperty("matches", out var matches))
                continue;

            var matchValues = StringArray(matches);
            CollectionAssert.DoesNotContain(matchValues, "http://*/*", relativePath);
            CollectionAssert.DoesNotContain(matchValues, "https://*/*", relativePath);
            CollectionAssert.DoesNotContain(matchValues, "<all_urls>", relativePath);
        }
    }
}
