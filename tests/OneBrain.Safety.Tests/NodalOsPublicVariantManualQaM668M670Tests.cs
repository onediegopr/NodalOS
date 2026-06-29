using System.Security.Cryptography;
using System.Text.Json;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace OneBrain.Safety.Tests;

[TestClass]
[TestCategory("PublicVariantManualQa")]
[TestCategory("M668")]
[TestCategory("M669")]
[TestCategory("M670")]
public sealed class NodalOsPublicVariantManualQaM668M670Tests
{
    private const string M668ReportPath = "docs/reports/m668-public-variant-manual-qa-execution.md";
    private const string M669ReportPath = "docs/reports/m669-public-variant-runtime-devtools-evidence.md";
    private const string M670ReportPath = "docs/reports/m670-final-public-package-audit.md";

    private const string ManualQaExecutionPath = "artifacts/agent-operations/m668/public-variant-manual-qa-execution.json";
    private const string PublicVariantLoadProofPath = "artifacts/agent-operations/m668/public-variant-load-proof.json";
    private const string ManifestSelectionLoadProofPath = "artifacts/agent-operations/m668/manifest-selection-load-proof.json";
    private const string ChromeExtensionReloadProofPath = "artifacts/agent-operations/m668/chrome-extension-reload-proof.json";
    private const string PermissionWarningProofPath = "artifacts/agent-operations/m668/public-variant-permission-warning-proof.json";
    private const string M668GoNoGoPath = "artifacts/agent-operations/m668/m668-go-no-go.json";

    private const string RuntimeTabEvidencePath = "artifacts/agent-operations/m669/runtime-tab-evidence.json";
    private const string ServiceWorkerDevtoolsEvidencePath = "artifacts/agent-operations/m669/service-worker-devtools-evidence.json";
    private const string CspConsoleEvidencePath = "artifacts/agent-operations/m669/csp-console-evidence.json";
    private const string BridgeLivenessEvidencePath = "artifacts/agent-operations/m669/bridge-liveness-evidence.json";
    private const string AllowedDisallowedOriginEvidencePath = "artifacts/agent-operations/m669/allowed-disallowed-origin-evidence.json";
    private const string EvidenceRedactionProofPath = "artifacts/agent-operations/m669/evidence-redaction-proof.json";
    private const string M669GoNoGoPath = "artifacts/agent-operations/m669/m669-go-no-go.json";

    private const string FinalPackageAuditPath = "artifacts/agent-operations/m670/final-public-package-audit.json";
    private const string PackageContentsFinalAuditPath = "artifacts/agent-operations/m670/package-contents-final-audit.json";
    private const string ManifestSelectionFinalAuditPath = "artifacts/agent-operations/m670/manifest-selection-final-audit.json";
    private const string PublicReleaseNoGoClosurePath = "artifacts/agent-operations/m670/public-release-no-go-closure.json";
    private const string ChromeWebStoreNoGoClosurePath = "artifacts/agent-operations/m670/chrome-web-store-no-go-closure.json";
    private const string PostQaRiskRegisterPath = "artifacts/agent-operations/m670/post-qa-risk-register.json";
    private const string M670GoNoGoPath = "artifacts/agent-operations/m670/m670-go-no-go.json";

    private const string ConsolidatedGoNoGoPath = "artifacts/agent-operations/m668-m670/public-variant-manual-qa-final-audit-go-no-go.json";

    private const string InternalManifestPath = "browser-extension/onebrain-chrome-lab/manifest.json";
    private const string PublicManifestPath = "browser-extension/onebrain-chrome-lab/manifest.public.json";
    private const string SidepanelHtmlPath = "browser-extension/onebrain-chrome-lab/sidepanel.html";
    private const string SidepanelCssPath = "browser-extension/onebrain-chrome-lab/sidepanel.css";
    private const string SidepanelJsPath = "browser-extension/onebrain-chrome-lab/sidepanel.js";
    private const string ServiceWorkerPath = "browser-extension/onebrain-chrome-lab/service_worker.js";
    private const string ContentScriptPath = "browser-extension/onebrain-chrome-lab/content_script.js";
    private const string RecipeCorePath = "browser-extension/onebrain-chrome-lab/recipe_core.js";

    private const string BridgeOptionsPath = "src/OneBrain.ChromeLab.Bridge/ChromeLabOptions.cs";
    private const string BridgeProtocolPath = "src/OneBrain.ChromeLab.Bridge/ChromeLabProtocol.cs";
    private const string BridgeSecurityPath = "src/OneBrain.ChromeLab.Bridge/ChromeLabSecurity.cs";
    private const string BridgeSelfTestPath = "src/OneBrain.ChromeLab.Bridge/ChromeLabSelfTest.cs";
    private const string BridgeOpenAiPath = "src/OneBrain.ChromeLab.Bridge/OpenAiAgentClient.cs";
    private const string BridgeProgramPath = "src/OneBrain.ChromeLab.Bridge/Program.cs";
    private const string BridgeProjectPath = "src/OneBrain.ChromeLab.Bridge/OneBrain.ChromeLab.Bridge.csproj";

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

    private static string Sha256Hex(string relativePath)
    {
        var bytes = File.ReadAllBytes(FullPath(relativePath));
        return Convert.ToHexString(SHA256.HashData(bytes));
    }

    private static string[] StringArray(JsonElement element) =>
        element.EnumerateArray().Select(item => item.GetString() ?? string.Empty).ToArray();

    [TestMethod]
    public void M668ArtifactsExist()
    {
        Assert.IsTrue(File.Exists(FullPath(M668ReportPath)), M668ReportPath);
        Assert.IsTrue(File.Exists(FullPath(ManualQaExecutionPath)), ManualQaExecutionPath);
        Assert.IsTrue(File.Exists(FullPath(PublicVariantLoadProofPath)), PublicVariantLoadProofPath);
        Assert.IsTrue(File.Exists(FullPath(ManifestSelectionLoadProofPath)), ManifestSelectionLoadProofPath);
        Assert.IsTrue(File.Exists(FullPath(ChromeExtensionReloadProofPath)), ChromeExtensionReloadProofPath);
        Assert.IsTrue(File.Exists(FullPath(PermissionWarningProofPath)), PermissionWarningProofPath);
        Assert.IsTrue(File.Exists(FullPath(M668GoNoGoPath)), M668GoNoGoPath);
    }

    [TestMethod]
    public void M669ArtifactsExist()
    {
        Assert.IsTrue(File.Exists(FullPath(M669ReportPath)), M669ReportPath);
        Assert.IsTrue(File.Exists(FullPath(RuntimeTabEvidencePath)), RuntimeTabEvidencePath);
        Assert.IsTrue(File.Exists(FullPath(ServiceWorkerDevtoolsEvidencePath)), ServiceWorkerDevtoolsEvidencePath);
        Assert.IsTrue(File.Exists(FullPath(CspConsoleEvidencePath)), CspConsoleEvidencePath);
        Assert.IsTrue(File.Exists(FullPath(BridgeLivenessEvidencePath)), BridgeLivenessEvidencePath);
        Assert.IsTrue(File.Exists(FullPath(AllowedDisallowedOriginEvidencePath)), AllowedDisallowedOriginEvidencePath);
        Assert.IsTrue(File.Exists(FullPath(EvidenceRedactionProofPath)), EvidenceRedactionProofPath);
        Assert.IsTrue(File.Exists(FullPath(M669GoNoGoPath)), M669GoNoGoPath);
    }

    [TestMethod]
    public void M670ArtifactsExist()
    {
        Assert.IsTrue(File.Exists(FullPath(M670ReportPath)), M670ReportPath);
        Assert.IsTrue(File.Exists(FullPath(FinalPackageAuditPath)), FinalPackageAuditPath);
        Assert.IsTrue(File.Exists(FullPath(PackageContentsFinalAuditPath)), PackageContentsFinalAuditPath);
        Assert.IsTrue(File.Exists(FullPath(ManifestSelectionFinalAuditPath)), ManifestSelectionFinalAuditPath);
        Assert.IsTrue(File.Exists(FullPath(PublicReleaseNoGoClosurePath)), PublicReleaseNoGoClosurePath);
        Assert.IsTrue(File.Exists(FullPath(ChromeWebStoreNoGoClosurePath)), ChromeWebStoreNoGoClosurePath);
        Assert.IsTrue(File.Exists(FullPath(PostQaRiskRegisterPath)), PostQaRiskRegisterPath);
        Assert.IsTrue(File.Exists(FullPath(M670GoNoGoPath)), M670GoNoGoPath);
    }

    [TestMethod]
    public void ConsolidatedM668M670GoNoGoExists() =>
        Assert.IsTrue(File.Exists(FullPath(ConsolidatedGoNoGoPath)), ConsolidatedGoNoGoPath);

    [TestMethod]
    public void M668RecordsManualQaEnvironmentBlocker()
    {
        using var execution = ReadJson(ManualQaExecutionPath);
        using var load = ReadJson(PublicVariantLoadProofPath);
        using var manifest = ReadJson(ManifestSelectionLoadProofPath);
        using var reload = ReadJson(ChromeExtensionReloadProofPath);
        using var permissions = ReadJson(PermissionWarningProofPath);
        Assert.IsFalse(execution.RootElement.GetProperty("manualQaEnvironmentAvailable").GetBoolean());
        Assert.IsFalse(execution.RootElement.GetProperty("manualQaExecuted").GetBoolean());
        Assert.IsFalse(execution.RootElement.GetProperty("manualQaPassed").GetBoolean());
        Assert.AreEqual("MANUAL_QA_ENVIRONMENT_REQUIRED", execution.RootElement.GetProperty("manualQaBlocker").GetString());
        Assert.IsFalse(load.RootElement.GetProperty("publicVariantLoaded").GetBoolean());
        Assert.IsTrue(manifest.RootElement.GetProperty("manifestSelectionVerified").GetBoolean());
        Assert.IsTrue(manifest.RootElement.GetProperty("publicManifestUsedForPublicPackageCandidate").GetBoolean());
        Assert.IsTrue(manifest.RootElement.GetProperty("publicHostPermissionsNarrowed").GetBoolean());
        Assert.IsFalse(reload.RootElement.GetProperty("chromeExtensionReloadExecuted").GetBoolean());
        Assert.IsFalse(permissions.RootElement.GetProperty("permissionWarningsCaptured").GetBoolean());
    }

    [TestMethod]
    public void M669RecordsEvidenceAsNotCapturedButRedacted()
    {
        using var runtime = ReadJson(RuntimeTabEvidencePath);
        using var devtools = ReadJson(ServiceWorkerDevtoolsEvidencePath);
        using var csp = ReadJson(CspConsoleEvidencePath);
        using var bridge = ReadJson(BridgeLivenessEvidencePath);
        using var origins = ReadJson(AllowedDisallowedOriginEvidencePath);
        using var redaction = ReadJson(EvidenceRedactionProofPath);
        Assert.IsFalse(runtime.RootElement.GetProperty("runtimeTabEvidenceReady").GetBoolean());
        Assert.IsFalse(devtools.RootElement.GetProperty("serviceWorkerDevtoolsEvidenceReady").GetBoolean());
        Assert.IsFalse(csp.RootElement.GetProperty("cspConsoleEvidenceReady").GetBoolean());
        Assert.IsFalse(bridge.RootElement.GetProperty("bridgeLivenessEvidenceReady").GetBoolean());
        Assert.IsFalse(origins.RootElement.GetProperty("allowedLocalOriginsEvidenceReady").GetBoolean());
        Assert.IsFalse(origins.RootElement.GetProperty("disallowedExternalOriginsEvidenceReady").GetBoolean());
        Assert.IsTrue(redaction.RootElement.GetProperty("evidenceRedacted").GetBoolean());
        Assert.IsFalse(redaction.RootElement.GetProperty("rawSecretsIncluded").GetBoolean());
        Assert.IsFalse(redaction.RootElement.GetProperty("rawApiKeysIncluded").GetBoolean());
        Assert.IsFalse(redaction.RootElement.GetProperty("longRawLogsIncluded").GetBoolean());
        Assert.IsFalse(redaction.RootElement.GetProperty("personalUserDataIncluded").GetBoolean());
    }

    [TestMethod]
    public void M670FinalPackageAuditKeepsReleaseAndStoreNoGo()
    {
        using var audit = ReadJson(FinalPackageAuditPath);
        using var package = ReadJson(PackageContentsFinalAuditPath);
        using var manifest = ReadJson(ManifestSelectionFinalAuditPath);
        using var release = ReadJson(PublicReleaseNoGoClosurePath);
        using var store = ReadJson(ChromeWebStoreNoGoClosurePath);
        using var risks = ReadJson(PostQaRiskRegisterPath);
        Assert.IsTrue(audit.RootElement.GetProperty("internalCandidateReady").GetBoolean());
        Assert.IsTrue(audit.RootElement.GetProperty("publicBuildCandidateReady").GetBoolean());
        Assert.IsTrue(audit.RootElement.GetProperty("publicPackageCandidateReady").GetBoolean());
        Assert.IsFalse(audit.RootElement.GetProperty("publicReleaseReady").GetBoolean());
        Assert.IsFalse(audit.RootElement.GetProperty("chromeWebStoreReady").GetBoolean());
        Assert.IsFalse(package.RootElement.GetProperty("finalSignedPackageCreated").GetBoolean());
        Assert.IsTrue(manifest.RootElement.GetProperty("manifestSelectionVerified").GetBoolean());
        Assert.IsFalse(manifest.RootElement.GetProperty("manualLoadVerifiedInChrome").GetBoolean());
        Assert.IsFalse(release.RootElement.GetProperty("publicReleaseReady").GetBoolean());
        Assert.IsFalse(store.RootElement.GetProperty("chromeWebStoreReady").GetBoolean());
        Assert.IsGreaterThanOrEqualTo(4, risks.RootElement.GetProperty("risks").GetArrayLength());
    }

    [TestMethod]
    public void ConsolidatedGoNoGoAllowsExpectedDecisionOnly()
    {
        using var doc = ReadJson(ConsolidatedGoNoGoPath);
        var decision = doc.RootElement.GetProperty("decision").GetString();
        CollectionAssert.Contains(new[]
        {
            "PUBLIC_VARIANT_MANUAL_QA_EVIDENCE_READY",
            "PUBLIC_VARIANT_MANUAL_QA_CONDITIONAL_READY_ENVIRONMENT_REQUIRED",
            "PUBLIC_VARIANT_MANUAL_QA_BLOCKED_REMEDIATION_REQUIRED"
        }, decision);

        Assert.AreEqual("PUBLIC_VARIANT_MANUAL_QA_CONDITIONAL_READY_ENVIRONMENT_REQUIRED", decision);
        Assert.IsTrue(doc.RootElement.GetProperty("internalCandidateReady").GetBoolean());
        Assert.IsTrue(doc.RootElement.GetProperty("publicBuildCandidateReady").GetBoolean());
        Assert.IsTrue(doc.RootElement.GetProperty("publicPackageCandidateReady").GetBoolean());
        Assert.IsFalse(doc.RootElement.GetProperty("manualQaExecuted").GetBoolean());
        Assert.IsFalse(doc.RootElement.GetProperty("manualQaPassed").GetBoolean());
        Assert.IsFalse(doc.RootElement.GetProperty("manualQaEnvironmentAvailable").GetBoolean());
        Assert.AreEqual("MANUAL_QA_ENVIRONMENT_REQUIRED", doc.RootElement.GetProperty("manualQaBlocker").GetString());
        Assert.IsFalse(doc.RootElement.GetProperty("publicReleaseReady").GetBoolean());
        Assert.IsFalse(doc.RootElement.GetProperty("chromeWebStoreReady").GetBoolean());
        Assert.IsTrue(doc.RootElement.GetProperty("manifestSelectionVerified").GetBoolean());
        Assert.IsTrue(doc.RootElement.GetProperty("publicHostPermissionsNarrowed").GetBoolean());
        Assert.IsTrue(doc.RootElement.GetProperty("publicContentScriptsKnownLimitationDocumented").GetBoolean());
        Assert.IsTrue(doc.RootElement.GetProperty("evidenceRedacted").GetBoolean());
        Assert.IsFalse(doc.RootElement.GetProperty("runtimeProductiveEnabled").GetBoolean());
        Assert.IsFalse(doc.RootElement.GetProperty("providerCloudEnabled").GetBoolean());
        Assert.IsFalse(doc.RootElement.GetProperty("filesystemEnabled").GetBoolean());
        Assert.IsFalse(doc.RootElement.GetProperty("browserAutomationEnabled").GetBoolean());
        Assert.IsFalse(doc.RootElement.GetProperty("capabilityUnlockEnabled").GetBoolean());
        Assert.IsFalse(doc.RootElement.GetProperty("bridgeModified").GetBoolean());
        Assert.IsFalse(doc.RootElement.GetProperty("cspModified").GetBoolean());
    }

    [TestMethod]
    public void EnvironmentUnavailableDecisionRequiresManualQaBlocker()
    {
        using var doc = ReadJson(ConsolidatedGoNoGoPath);
        if (!doc.RootElement.GetProperty("manualQaEnvironmentAvailable").GetBoolean())
            Assert.AreEqual("MANUAL_QA_ENVIRONMENT_REQUIRED", doc.RootElement.GetProperty("manualQaBlocker").GetString());
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

    [TestMethod]
    public void M668M670GoNoGosKeepSensitiveCapabilitiesDisabled()
    {
        foreach (var path in new[] { M668GoNoGoPath, M669GoNoGoPath, M670GoNoGoPath, ConsolidatedGoNoGoPath })
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
    public void InternalManifestJsonUnchanged()
    {
        Assert.AreEqual("76859A171D0FBC585E96253D0F269AEF4A54DCFD5F704DE9DB92D26EB1AEDDFD", Sha256Hex(InternalManifestPath));
    }

    [TestMethod]
    public void PublicManifestJsonUnchanged()
    {
        Assert.AreEqual("CB89F7EE9E39BE0BB5B04E062D3D0A059BEB236951CBAA16DA0C325933965A40", Sha256Hex(PublicManifestPath));
    }

    [TestMethod]
    public void SidepanelHtmlUnchanged()
    {
        Assert.AreEqual("96421123D2EC9BADDEA52AB7063E3D01E4B2AD0CA208EBF68FF16450990B1CFC", Sha256Hex(SidepanelHtmlPath));
    }

    [TestMethod]
    public void SidepanelCssUnchanged()
    {
        Assert.AreEqual("0141931FA94B0004A8F2631C9E6985E1CF9243B0B9CBF787AFB2449858B6CED9", Sha256Hex(SidepanelCssPath));
    }

    [TestMethod]
    public void SidepanelJsUnchanged()
    {
        Assert.AreEqual("E5DAE393D670E903FA0A8413D7DC2F4F33C46754AEE27C24CD98A0C4ED875869", Sha256Hex(SidepanelJsPath));
    }

    [TestMethod]
    public void ServiceWorkerUnchanged()
    {
        Assert.AreEqual("B65E0385EC96F0E96DCB3493311372A3B307C53E732235C7B3093AFE2DC39859", Sha256Hex(ServiceWorkerPath));
    }

    [TestMethod]
    public void ContentScriptUnchanged()
    {
        Assert.AreEqual("E1042E37DC884BA8B088DC7CB4D805BC5BFC72820C78DB632D520B6AD4477186", Sha256Hex(ContentScriptPath));
    }

    [TestMethod]
    public void RecipeCoreUnchanged()
    {
        Assert.AreEqual("DEA70FD162CE2F94ED29D35CD2C919AD2D62DA1810D46F49DD0CEBF63399C5F8", Sha256Hex(RecipeCorePath));
    }

    [TestMethod]
    public void BridgeSourceUnchanged()
    {
        Assert.AreEqual("F61164907EE4FB35AA8C0E3CA4A13A98986258B9AB4EDD1469A9D146AF81646A", Sha256Hex(BridgeOptionsPath));
        Assert.AreEqual("C62BBBEA97ACA6B15D72F25415F9E94E20FE9D460903AEB9292A107DCF2AFE68", Sha256Hex(BridgeProtocolPath));
        Assert.AreEqual("5E933A21C501487523C60197F02E0D68CE6D0F5865DC9401F9337EBA41B58B37", Sha256Hex(BridgeSecurityPath));
        Assert.AreEqual("8CF05765736DEF46584022F1E33E22CD4E85A3C08A952048D425FBBC1EFE5309", Sha256Hex(BridgeSelfTestPath));
        Assert.AreEqual("CAACC05544B9E41F7606BD17ECAD81FB36842869BC34E46F8596D4D62EB5B5C4", Sha256Hex(BridgeOpenAiPath));
        Assert.AreEqual("B9FB617E6B2FD1393EC4E1DD7B90AF91E44B26EBD4D70DA87416A5B46531E21E", Sha256Hex(BridgeProgramPath));
        Assert.AreEqual("E00407FA4BC6DCB8ADBB677E5FE4AF41FE7BDB967C5827EBF54569E6DF32E85D", Sha256Hex(BridgeProjectPath));
    }
}
