using System.Security.Cryptography;
using System.Text.Json;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace OneBrain.Safety.Tests;

[TestClass]
[TestCategory("ManualQaEnvironmentSetup")]
[TestCategory("M671")]
[TestCategory("M672")]
[TestCategory("M673")]
public sealed class NodalOsManualQaEnvironmentSetupM671M673Tests
{
    private const string M671ReportPath = "docs/reports/m671-manual-qa-environment-setup.md";
    private const string M672ReportPath = "docs/reports/m672-public-variant-reload-evidence-retry.md";
    private const string M673ReportPath = "docs/reports/m673-final-qa-retry-closure-human-handoff.md";

    private const string ManualQaEnvironmentSetupPath = "artifacts/agent-operations/m671/manual-qa-environment-setup.json";
    private const string PublicVariantStagingPlanPath = "artifacts/agent-operations/m671/public-variant-staging-plan.json";
    private const string HumanAssistedChromeLoadRunbookPath = "artifacts/agent-operations/m671/human-assisted-chrome-load-runbook.json";
    private const string ChromePolicyBlockerAnalysisPath = "artifacts/agent-operations/m671/chrome-policy-blocker-analysis.json";
    private const string M671GoNoGoPath = "artifacts/agent-operations/m671/m671-go-no-go.json";

    private const string PublicVariantReloadEvidenceRetryPath = "artifacts/agent-operations/m672/public-variant-reload-evidence-retry.json";
    private const string ManualLoadResultTemplatePath = "artifacts/agent-operations/m672/manual-load-result-template.json";
    private const string RuntimeTabResultTemplatePath = "artifacts/agent-operations/m672/runtime-tab-result-template.json";
    private const string ServiceWorkerDevtoolsResultTemplatePath = "artifacts/agent-operations/m672/service-worker-devtools-result-template.json";
    private const string PermissionWarningResultTemplatePath = "artifacts/agent-operations/m672/permission-warning-result-template.json";
    private const string ConsoleCleanlinessResultTemplatePath = "artifacts/agent-operations/m672/console-cleanliness-result-template.json";
    private const string M672GoNoGoPath = "artifacts/agent-operations/m672/m672-go-no-go.json";

    private const string FinalQaRetryClosurePath = "artifacts/agent-operations/m673/final-qa-retry-closure.json";
    private const string HumanEvidenceHandoffPath = "artifacts/agent-operations/m673/human-evidence-handoff.json";
    private const string ManualQaPassFailDecisionTemplatePath = "artifacts/agent-operations/m673/manual-qa-pass-fail-decision-template.json";
    private const string PostRetryRiskRegisterPath = "artifacts/agent-operations/m673/post-retry-risk-register.json";
    private const string PublicReleaseNoGoProofPath = "artifacts/agent-operations/m673/public-release-no-go-proof.json";
    private const string ChromeWebStoreNoGoProofPath = "artifacts/agent-operations/m673/chrome-web-store-no-go-proof.json";
    private const string M673GoNoGoPath = "artifacts/agent-operations/m673/m673-go-no-go.json";

    private const string ConsolidatedGoNoGoPath = "artifacts/agent-operations/m671-m673/manual-qa-environment-setup-go-no-go.json";

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
    public void M671ArtifactsExist()
    {
        Assert.IsTrue(File.Exists(FullPath(M671ReportPath)), M671ReportPath);
        Assert.IsTrue(File.Exists(FullPath(ManualQaEnvironmentSetupPath)), ManualQaEnvironmentSetupPath);
        Assert.IsTrue(File.Exists(FullPath(PublicVariantStagingPlanPath)), PublicVariantStagingPlanPath);
        Assert.IsTrue(File.Exists(FullPath(HumanAssistedChromeLoadRunbookPath)), HumanAssistedChromeLoadRunbookPath);
        Assert.IsTrue(File.Exists(FullPath(ChromePolicyBlockerAnalysisPath)), ChromePolicyBlockerAnalysisPath);
        Assert.IsTrue(File.Exists(FullPath(M671GoNoGoPath)), M671GoNoGoPath);
    }

    [TestMethod]
    public void M672ArtifactsExist()
    {
        Assert.IsTrue(File.Exists(FullPath(M672ReportPath)), M672ReportPath);
        Assert.IsTrue(File.Exists(FullPath(PublicVariantReloadEvidenceRetryPath)), PublicVariantReloadEvidenceRetryPath);
        Assert.IsTrue(File.Exists(FullPath(ManualLoadResultTemplatePath)), ManualLoadResultTemplatePath);
        Assert.IsTrue(File.Exists(FullPath(RuntimeTabResultTemplatePath)), RuntimeTabResultTemplatePath);
        Assert.IsTrue(File.Exists(FullPath(ServiceWorkerDevtoolsResultTemplatePath)), ServiceWorkerDevtoolsResultTemplatePath);
        Assert.IsTrue(File.Exists(FullPath(PermissionWarningResultTemplatePath)), PermissionWarningResultTemplatePath);
        Assert.IsTrue(File.Exists(FullPath(ConsoleCleanlinessResultTemplatePath)), ConsoleCleanlinessResultTemplatePath);
        Assert.IsTrue(File.Exists(FullPath(M672GoNoGoPath)), M672GoNoGoPath);
    }

    [TestMethod]
    public void M673ArtifactsExist()
    {
        Assert.IsTrue(File.Exists(FullPath(M673ReportPath)), M673ReportPath);
        Assert.IsTrue(File.Exists(FullPath(FinalQaRetryClosurePath)), FinalQaRetryClosurePath);
        Assert.IsTrue(File.Exists(FullPath(HumanEvidenceHandoffPath)), HumanEvidenceHandoffPath);
        Assert.IsTrue(File.Exists(FullPath(ManualQaPassFailDecisionTemplatePath)), ManualQaPassFailDecisionTemplatePath);
        Assert.IsTrue(File.Exists(FullPath(PostRetryRiskRegisterPath)), PostRetryRiskRegisterPath);
        Assert.IsTrue(File.Exists(FullPath(PublicReleaseNoGoProofPath)), PublicReleaseNoGoProofPath);
        Assert.IsTrue(File.Exists(FullPath(ChromeWebStoreNoGoProofPath)), ChromeWebStoreNoGoProofPath);
        Assert.IsTrue(File.Exists(FullPath(M673GoNoGoPath)), M673GoNoGoPath);
    }

    [TestMethod]
    public void ConsolidatedM671M673GoNoGoExists() =>
        Assert.IsTrue(File.Exists(FullPath(ConsolidatedGoNoGoPath)), ConsolidatedGoNoGoPath);

    [TestMethod]
    public void M671DocumentsChromePolicyBlockerAndRunbook()
    {
        using var setup = ReadJson(ManualQaEnvironmentSetupPath);
        using var staging = ReadJson(PublicVariantStagingPlanPath);
        using var runbook = ReadJson(HumanAssistedChromeLoadRunbookPath);
        using var blocker = ReadJson(ChromePolicyBlockerAnalysisPath);
        Assert.IsFalse(setup.RootElement.GetProperty("manualQaEnvironmentAvailable").GetBoolean());
        Assert.AreEqual("MANUAL_QA_ENVIRONMENT_REQUIRED", setup.RootElement.GetProperty("manualQaBlocker").GetString());
        Assert.IsFalse(setup.RootElement.GetProperty("bypassAttempted").GetBoolean());
        Assert.IsTrue(staging.RootElement.GetProperty("stagingPlanReady").GetBoolean());
        Assert.IsFalse(staging.RootElement.GetProperty("stagingFolderCreatedByAgent").GetBoolean());
        Assert.IsTrue(staging.RootElement.GetProperty("internalManifestMustNotBeUsedForPublicCandidate").GetBoolean());
        Assert.IsTrue(runbook.RootElement.GetProperty("humanAssistedRunbookReady").GetBoolean());
        Assert.IsTrue(blocker.RootElement.GetProperty("chromePolicyBlockerConfirmed").GetBoolean());
        Assert.IsFalse(blocker.RootElement.GetProperty("unsafeBypassAllowed").GetBoolean());
    }

    [TestMethod]
    public void M672ProvidesManualEvidenceTemplates()
    {
        using var retry = ReadJson(PublicVariantReloadEvidenceRetryPath);
        using var manual = ReadJson(ManualLoadResultTemplatePath);
        using var runtime = ReadJson(RuntimeTabResultTemplatePath);
        using var devtools = ReadJson(ServiceWorkerDevtoolsResultTemplatePath);
        using var permissions = ReadJson(PermissionWarningResultTemplatePath);
        using var console = ReadJson(ConsoleCleanlinessResultTemplatePath);
        Assert.IsFalse(retry.RootElement.GetProperty("manualQaExecuted").GetBoolean());
        Assert.IsTrue(retry.RootElement.GetProperty("templatesPrepared").GetBoolean());
        Assert.AreEqual("manual-load-result", manual.RootElement.GetProperty("template").GetString());
        Assert.AreEqual("runtime-tab-result", runtime.RootElement.GetProperty("template").GetString());
        Assert.AreEqual("service-worker-devtools-result", devtools.RootElement.GetProperty("template").GetString());
        Assert.AreEqual("permission-warning-result", permissions.RootElement.GetProperty("template").GetString());
        Assert.AreEqual("console-cleanliness-result", console.RootElement.GetProperty("template").GetString());
        Assert.IsFalse(devtools.RootElement.GetProperty("rawConsoleExcerptAllowed").GetBoolean());
        Assert.IsFalse(console.RootElement.GetProperty("rawSecretsIncluded").GetBoolean());
    }

    [TestMethod]
    public void M673ClosesWithHumanEvidenceHandoff()
    {
        using var closure = ReadJson(FinalQaRetryClosurePath);
        using var handoff = ReadJson(HumanEvidenceHandoffPath);
        using var decision = ReadJson(ManualQaPassFailDecisionTemplatePath);
        using var risks = ReadJson(PostRetryRiskRegisterPath);
        using var release = ReadJson(PublicReleaseNoGoProofPath);
        using var store = ReadJson(ChromeWebStoreNoGoProofPath);
        Assert.AreEqual("HUMAN_ASSISTED_MANUAL_QA_HANDOFF_READY", closure.RootElement.GetProperty("decision").GetString());
        Assert.IsTrue(closure.RootElement.GetProperty("humanAssistedHandoffReady").GetBoolean());
        Assert.IsTrue(handoff.RootElement.GetProperty("humanEvidenceTemplateReady").GetBoolean());
        Assert.IsFalse(handoff.RootElement.GetProperty("secretsAllowed").GetBoolean());
        Assert.IsFalse(handoff.RootElement.GetProperty("rawLongLogsAllowed").GetBoolean());
        Assert.AreEqual("manual-qa-pass-fail-decision", decision.RootElement.GetProperty("template").GetString());
        Assert.IsGreaterThanOrEqualTo(3, risks.RootElement.GetProperty("risks").GetArrayLength());
        Assert.IsFalse(release.RootElement.GetProperty("publicReleaseReady").GetBoolean());
        Assert.IsFalse(store.RootElement.GetProperty("chromeWebStoreReady").GetBoolean());
    }

    [TestMethod]
    public void ConsolidatedGoNoGoKeepsExpectedDecisionAndNoGoStatus()
    {
        using var doc = ReadJson(ConsolidatedGoNoGoPath);
        var decision = doc.RootElement.GetProperty("decision").GetString();
        CollectionAssert.Contains(new[]
        {
            "PUBLIC_VARIANT_MANUAL_QA_RETRY_EVIDENCE_READY",
            "HUMAN_ASSISTED_MANUAL_QA_HANDOFF_READY",
            "PUBLIC_VARIANT_QA_RETRY_BLOCKED_REMEDIATION_REQUIRED"
        }, decision);

        Assert.AreEqual("HUMAN_ASSISTED_MANUAL_QA_HANDOFF_READY", decision);
        Assert.IsTrue(doc.RootElement.GetProperty("internalCandidateReady").GetBoolean());
        Assert.IsTrue(doc.RootElement.GetProperty("publicBuildCandidateReady").GetBoolean());
        Assert.IsFalse(doc.RootElement.GetProperty("manualQaExecuted").GetBoolean());
        Assert.IsFalse(doc.RootElement.GetProperty("manualQaPassed").GetBoolean());
        Assert.IsFalse(doc.RootElement.GetProperty("manualQaEnvironmentAvailable").GetBoolean());
        Assert.AreEqual("MANUAL_QA_ENVIRONMENT_REQUIRED", doc.RootElement.GetProperty("manualQaBlocker").GetString());
        Assert.IsTrue(doc.RootElement.GetProperty("humanAssistedRunbookReady").GetBoolean());
        Assert.IsTrue(doc.RootElement.GetProperty("humanEvidenceTemplateReady").GetBoolean());
        Assert.IsTrue(doc.RootElement.GetProperty("manifestSelectionVerifiedStatic").GetBoolean());
        Assert.IsFalse(doc.RootElement.GetProperty("publicReleaseReady").GetBoolean());
        Assert.IsFalse(doc.RootElement.GetProperty("chromeWebStoreReady").GetBoolean());
        Assert.AreEqual("M674-M676 Human Manual QA Evidence Intake + Final Package Audit Retry", doc.RootElement.GetProperty("recommendedNextMilestone").GetString());
    }

    [TestMethod]
    public void GoNoGosKeepSensitiveCapabilitiesDisabled()
    {
        foreach (var path in new[] { M671GoNoGoPath, M672GoNoGoPath, M673GoNoGoPath, ConsolidatedGoNoGoPath })
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

    [TestMethod]
    public void ProductAndBridgeFilesRemainUnchanged()
    {
        Assert.AreEqual("76859A171D0FBC585E96253D0F269AEF4A54DCFD5F704DE9DB92D26EB1AEDDFD", Sha256Hex(InternalManifestPath));
        Assert.AreEqual("CB89F7EE9E39BE0BB5B04E062D3D0A059BEB236951CBAA16DA0C325933965A40", Sha256Hex(PublicManifestPath));
        Assert.AreEqual("4A9642242F742B641B60430EB16647DD4A989EBCCCB072D0296B8CDCDE6E88C2", Sha256Hex(SidepanelHtmlPath));
        Assert.AreEqual("D2A14687DB6E201353A100A33B72AECB3C1858C1127114979945750AB5B717AC", Sha256Hex(SidepanelCssPath));
        Assert.AreEqual("FED938DE2C42EC56F9061E2587A57338DAD1A770BBFAD2B710937BBD97D9D329", Sha256Hex(SidepanelJsPath));
        Assert.AreEqual("B65E0385EC96F0E96DCB3493311372A3B307C53E732235C7B3093AFE2DC39859", Sha256Hex(ServiceWorkerPath));
        Assert.AreEqual("E1042E37DC884BA8B088DC7CB4D805BC5BFC72820C78DB632D520B6AD4477186", Sha256Hex(ContentScriptPath));
        Assert.AreEqual("DEA70FD162CE2F94ED29D35CD2C919AD2D62DA1810D46F49DD0CEBF63399C5F8", Sha256Hex(RecipeCorePath));
        Assert.AreEqual("D4E043EC6EC181D780FD2A18AAC48BB39FED4F9078E03229BF5F718635031712", Sha256Hex(BridgeOptionsPath));
        Assert.AreEqual("A22DAE8449A297DC77E5E0E28D404DA253846BEC541033F2E0EF6AC779562624", Sha256Hex(BridgeProtocolPath));
        Assert.AreEqual("5E933A21C501487523C60197F02E0D68CE6D0F5865DC9401F9337EBA41B58B37", Sha256Hex(BridgeSecurityPath));
        Assert.AreEqual("8CF05765736DEF46584022F1E33E22CD4E85A3C08A952048D425FBBC1EFE5309", Sha256Hex(BridgeSelfTestPath));
        Assert.AreEqual("CAACC05544B9E41F7606BD17ECAD81FB36842869BC34E46F8596D4D62EB5B5C4", Sha256Hex(BridgeOpenAiPath));
        Assert.AreEqual("061CED72CF27BC31C0BAD240BC698B58D0D5966D6D107CE8A315C98D52DB1305", Sha256Hex(BridgeProgramPath));
        Assert.AreEqual("E00407FA4BC6DCB8ADBB677E5FE4AF41FE7BDB967C5827EBF54569E6DF32E85D", Sha256Hex(BridgeProjectPath));
    }
}
