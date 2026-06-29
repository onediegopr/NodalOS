using System.Security.Cryptography;
using System.Text.Json;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace OneBrain.Safety.Tests;

[TestClass]
[TestCategory("PublicBuildManifestStrategy")]
[TestCategory("M655")]
[TestCategory("M656")]
[TestCategory("M657")]
public sealed class NodalOsPublicBuildManifestStrategyM655M657Tests
{
    private const string M655ReportPath = "docs/reports/m655-public-build-manifest-strategy.md";
    private const string M656ReportPath = "docs/reports/m656-public-build-permission-impact-matrix.md";
    private const string M657ReportPath = "docs/reports/m657-public-build-strategy-final-go-no-go.md";

    private const string PublicBuildManifestStrategyPath = "artifacts/agent-operations/m655/public-build-manifest-strategy.json";
    private const string HostPermissionsNarrowingPlanPath = "artifacts/agent-operations/m655/host-permissions-narrowing-plan.json";
    private const string ContentScriptMatchStrategyPath = "artifacts/agent-operations/m655/content-script-match-strategy.json";
    private const string M655GoNoGoPath = "artifacts/agent-operations/m655/m655-go-no-go.json";

    private const string PermissionImpactMatrixPath = "artifacts/agent-operations/m656/permission-impact-matrix.json";
    private const string RuntimeFeatureImpactPath = "artifacts/agent-operations/m656/runtime-feature-impact.json";
    private const string ExtensionBehaviorImpactPath = "artifacts/agent-operations/m656/extension-behavior-impact.json";
    private const string RegressionRiskRegisterPath = "artifacts/agent-operations/m656/public-build-regression-risk-register.json";
    private const string M656GoNoGoPath = "artifacts/agent-operations/m656/m656-go-no-go.json";

    private const string FinalDecisionPath = "artifacts/agent-operations/m657/public-build-strategy-final-decision.json";
    private const string NextMilestonePath = "artifacts/agent-operations/m657/public-build-next-milestone-recommendation.json";
    private const string InternalVsPublicDecisionPath = "artifacts/agent-operations/m657/internal-vs-public-build-decision.json";
    private const string M657GoNoGoPath = "artifacts/agent-operations/m657/m657-go-no-go.json";

    private const string ConsolidatedGoNoGoPath = "artifacts/agent-operations/m655-m657/public-build-manifest-strategy-go-no-go.json";

    private const string ManifestPath = "browser-extension/onebrain-chrome-lab/manifest.json";
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

    [TestMethod]
    public void M655ArtifactsExist()
    {
        Assert.IsTrue(File.Exists(FullPath(M655ReportPath)), M655ReportPath);
        Assert.IsTrue(File.Exists(FullPath(PublicBuildManifestStrategyPath)), PublicBuildManifestStrategyPath);
        Assert.IsTrue(File.Exists(FullPath(HostPermissionsNarrowingPlanPath)), HostPermissionsNarrowingPlanPath);
        Assert.IsTrue(File.Exists(FullPath(ContentScriptMatchStrategyPath)), ContentScriptMatchStrategyPath);
        Assert.IsTrue(File.Exists(FullPath(M655GoNoGoPath)), M655GoNoGoPath);
    }

    [TestMethod]
    public void M656ArtifactsExist()
    {
        Assert.IsTrue(File.Exists(FullPath(M656ReportPath)), M656ReportPath);
        Assert.IsTrue(File.Exists(FullPath(PermissionImpactMatrixPath)), PermissionImpactMatrixPath);
        Assert.IsTrue(File.Exists(FullPath(RuntimeFeatureImpactPath)), RuntimeFeatureImpactPath);
        Assert.IsTrue(File.Exists(FullPath(ExtensionBehaviorImpactPath)), ExtensionBehaviorImpactPath);
        Assert.IsTrue(File.Exists(FullPath(RegressionRiskRegisterPath)), RegressionRiskRegisterPath);
        Assert.IsTrue(File.Exists(FullPath(M656GoNoGoPath)), M656GoNoGoPath);
    }

    [TestMethod]
    public void M657ArtifactsExist()
    {
        Assert.IsTrue(File.Exists(FullPath(M657ReportPath)), M657ReportPath);
        Assert.IsTrue(File.Exists(FullPath(FinalDecisionPath)), FinalDecisionPath);
        Assert.IsTrue(File.Exists(FullPath(NextMilestonePath)), NextMilestonePath);
        Assert.IsTrue(File.Exists(FullPath(InternalVsPublicDecisionPath)), InternalVsPublicDecisionPath);
        Assert.IsTrue(File.Exists(FullPath(M657GoNoGoPath)), M657GoNoGoPath);
    }

    [TestMethod]
    public void ConsolidatedGoNoGoExists() =>
        Assert.IsTrue(File.Exists(FullPath(ConsolidatedGoNoGoPath)), ConsolidatedGoNoGoPath);

    [TestMethod]
    public void PublicBuildManifestStrategyKeepsManifestUnchanged()
    {
        using var doc = ReadJson(PublicBuildManifestStrategyPath);
        Assert.AreEqual("PUBLIC_BUILD_MANIFEST_STRATEGY_READY", doc.RootElement.GetProperty("decision").GetString());
        CollectionAssert.Contains(doc.RootElement.GetProperty("currentHostPermissions").EnumerateArray().Select(item => item.GetString()).ToArray(), "http://*/*");
        CollectionAssert.Contains(doc.RootElement.GetProperty("currentHostPermissions").EnumerateArray().Select(item => item.GetString()).ToArray(), "https://*/*");
        Assert.AreEqual("split_internal_public_build", doc.RootElement.GetProperty("recommendedPublicBuildStrategy").GetString());
        Assert.IsTrue(doc.RootElement.GetProperty("internalCandidateReady").GetBoolean());
        Assert.IsFalse(doc.RootElement.GetProperty("publicReleaseReady").GetBoolean());
        Assert.IsFalse(doc.RootElement.GetProperty("manifestModified").GetBoolean());
        Assert.IsFalse(doc.RootElement.GetProperty("hostPermissionsModified").GetBoolean());
        Assert.IsFalse(doc.RootElement.GetProperty("contentScriptsMatchesModified").GetBoolean());
        Assert.IsFalse(doc.RootElement.GetProperty("permissionsModified").GetBoolean());
        Assert.IsFalse(doc.RootElement.GetProperty("productFilesModified").GetBoolean());
    }

    [TestMethod]
    public void HostPermissionsNarrowingPlanExists()
    {
        using var doc = ReadJson(HostPermissionsNarrowingPlanPath);
        Assert.AreEqual("HOST_PERMISSIONS_NARROWING_PLAN_READY", doc.RootElement.GetProperty("decision").GetString());
        Assert.IsGreaterThanOrEqualTo(5, doc.RootElement.GetProperty("narrowingOptions").GetArrayLength());
        Assert.AreEqual("M658 Public/Internal Build Split Implementation Plan", doc.RootElement.GetProperty("recommendedNextMilestone").GetString());
        Assert.IsFalse(doc.RootElement.GetProperty("manifestModified").GetBoolean());
        Assert.IsFalse(doc.RootElement.GetProperty("hostPermissionsModified").GetBoolean());
        Assert.IsFalse(doc.RootElement.GetProperty("contentScriptsMatchesModified").GetBoolean());
        Assert.IsFalse(doc.RootElement.GetProperty("publicReleaseReady").GetBoolean());
    }

    [TestMethod]
    public void ContentScriptMatchStrategyExists()
    {
        using var doc = ReadJson(ContentScriptMatchStrategyPath);
        Assert.AreEqual("CONTENT_SCRIPT_MATCH_STRATEGY_READY", doc.RootElement.GetProperty("decision").GetString());
        Assert.AreEqual("separate_content_scripts_by_build_or_optional_permissions", doc.RootElement.GetProperty("strategy").GetString());
        Assert.IsTrue(doc.RootElement.GetProperty("requiresFutureManifestMilestone").GetBoolean());
        Assert.IsTrue(doc.RootElement.GetProperty("requiresManualQa").GetBoolean());
        Assert.IsTrue(doc.RootElement.GetProperty("requiresStoreDisclosure").GetBoolean());
        Assert.IsFalse(doc.RootElement.GetProperty("manifestModified").GetBoolean());
        Assert.IsFalse(doc.RootElement.GetProperty("contentScriptsMatchesModified").GetBoolean());
        Assert.IsFalse(doc.RootElement.GetProperty("publicReleaseReady").GetBoolean());
    }

    [TestMethod]
    public void PermissionImpactMatrixAndRiskRegisterExist()
    {
        using var matrix = ReadJson(PermissionImpactMatrixPath);
        using var risks = ReadJson(RegressionRiskRegisterPath);
        Assert.AreEqual("PERMISSION_IMPACT_MATRIX_READY", matrix.RootElement.GetProperty("decision").GetString());
        Assert.IsGreaterThanOrEqualTo(3, matrix.RootElement.GetProperty("strategies").GetArrayLength());
        Assert.IsFalse(matrix.RootElement.GetProperty("publicReleaseReady").GetBoolean());
        Assert.AreEqual("PUBLIC_BUILD_REGRESSION_RISK_REGISTER_READY", risks.RootElement.GetProperty("decision").GetString());
        Assert.IsGreaterThanOrEqualTo(4, risks.RootElement.GetProperty("risks").GetArrayLength());
        Assert.IsFalse(risks.RootElement.GetProperty("publicReleaseReady").GetBoolean());
    }

    [TestMethod]
    public void RuntimeAndExtensionBehaviorImpactKeepRuntimeDisabled()
    {
        using var runtime = ReadJson(RuntimeFeatureImpactPath);
        using var behavior = ReadJson(ExtensionBehaviorImpactPath);
        Assert.AreEqual("RUNTIME_FEATURE_IMPACT_READY", runtime.RootElement.GetProperty("decision").GetString());
        Assert.IsFalse(runtime.RootElement.GetProperty("runtimeProductiveEnabled").GetBoolean());
        Assert.IsFalse(runtime.RootElement.GetProperty("providerCloudEnabled").GetBoolean());
        Assert.IsFalse(runtime.RootElement.GetProperty("filesystemEnabled").GetBoolean());
        Assert.IsFalse(runtime.RootElement.GetProperty("browserAutomationEnabled").GetBoolean());
        Assert.AreEqual("EXTENSION_BEHAVIOR_IMPACT_READY", behavior.RootElement.GetProperty("decision").GetString());
        Assert.IsTrue(behavior.RootElement.GetProperty("requiresManualQa").GetBoolean());
        Assert.IsTrue(behavior.RootElement.GetProperty("requiresStoreDisclosure").GetBoolean());
        Assert.IsFalse(behavior.RootElement.GetProperty("publicReleaseReady").GetBoolean());
    }

    [TestMethod]
    public void FinalPublicBuildStrategyDecisionIsConditionalForFutureImplementation()
    {
        using var doc = ReadJson(FinalDecisionPath);
        Assert.AreEqual("PUBLIC_BUILD_STRATEGY_FINAL_DECISION_READY", doc.RootElement.GetProperty("decision").GetString());
        Assert.AreEqual("CONDITIONAL_GO_FOR_FUTURE_IMPLEMENTATION", doc.RootElement.GetProperty("publicBuildStrategyDecision").GetString());
        Assert.AreEqual("split_internal_public_build", doc.RootElement.GetProperty("recommendedPublicBuildStrategy").GetString());
        Assert.IsTrue(doc.RootElement.GetProperty("internalCandidateReady").GetBoolean());
        Assert.IsFalse(doc.RootElement.GetProperty("publicReleaseReady").GetBoolean());
        Assert.IsFalse(doc.RootElement.GetProperty("nextBlockShouldTouchManifest").GetBoolean());
        Assert.IsTrue(doc.RootElement.GetProperty("nextBlockShouldPlanManifestImplementation").GetBoolean());
        Assert.IsFalse(doc.RootElement.GetProperty("manifestModified").GetBoolean());
        Assert.IsFalse(doc.RootElement.GetProperty("hostPermissionsModified").GetBoolean());
        Assert.IsFalse(doc.RootElement.GetProperty("contentScriptsMatchesModified").GetBoolean());
    }

    [TestMethod]
    public void InternalVsPublicBuildDecisionKeepsTwoLines()
    {
        using var doc = ReadJson(InternalVsPublicDecisionPath);
        Assert.AreEqual("INTERNAL_VS_PUBLIC_BUILD_DECISION_READY", doc.RootElement.GetProperty("decision").GetString());
        Assert.IsTrue(doc.RootElement.GetProperty("keepTwoLines").GetBoolean());
        Assert.IsTrue(doc.RootElement.GetProperty("internalCandidateReady").GetBoolean());
        Assert.IsFalse(doc.RootElement.GetProperty("publicCandidateReady").GetBoolean());
        Assert.IsFalse(doc.RootElement.GetProperty("publicReleaseReady").GetBoolean());
        Assert.AreEqual("split_internal_public_build", doc.RootElement.GetProperty("hostPermissionsStrategy").GetString());
        Assert.IsFalse(doc.RootElement.GetProperty("productFilesModified").GetBoolean());
    }

    [TestMethod]
    public void ConsolidatedGoNoGoKeepsAllSensitivePathsBlocked()
    {
        using var doc = ReadJson(ConsolidatedGoNoGoPath);
        Assert.AreEqual("PUBLIC_BUILD_MANIFEST_STRATEGY_READY", doc.RootElement.GetProperty("decision").GetString());
        Assert.IsTrue(doc.RootElement.GetProperty("internalCandidateReady").GetBoolean());
        Assert.IsFalse(doc.RootElement.GetProperty("publicReleaseReady").GetBoolean());
        Assert.IsFalse(doc.RootElement.GetProperty("manifestModified").GetBoolean());
        Assert.IsFalse(doc.RootElement.GetProperty("hostPermissionsModified").GetBoolean());
        Assert.IsFalse(doc.RootElement.GetProperty("contentScriptsMatchesModified").GetBoolean());
        Assert.IsFalse(doc.RootElement.GetProperty("permissionsModified").GetBoolean());
        Assert.IsFalse(doc.RootElement.GetProperty("jsModified").GetBoolean());
        Assert.IsFalse(doc.RootElement.GetProperty("bridgeModified").GetBoolean());
        Assert.IsFalse(doc.RootElement.GetProperty("runtimeProductiveEnabled").GetBoolean());
        Assert.IsFalse(doc.RootElement.GetProperty("providerCloudEnabled").GetBoolean());
        Assert.IsFalse(doc.RootElement.GetProperty("filesystemEnabled").GetBoolean());
        Assert.IsFalse(doc.RootElement.GetProperty("browserAutomationEnabled").GetBoolean());
        Assert.IsFalse(doc.RootElement.GetProperty("capabilityUnlockEnabled").GetBoolean());
        Assert.AreEqual("split_internal_public_build", doc.RootElement.GetProperty("recommendedPublicBuildStrategy").GetString());
    }

    [TestMethod]
    public void M655M657GoNoGosRemainPlanningOnly()
    {
        using var m655 = ReadJson(M655GoNoGoPath);
        using var m656 = ReadJson(M656GoNoGoPath);
        using var m657 = ReadJson(M657GoNoGoPath);
        Assert.IsFalse(m655.RootElement.GetProperty("manifestModified").GetBoolean());
        Assert.IsFalse(m656.RootElement.GetProperty("hostPermissionsModified").GetBoolean());
        Assert.IsTrue(m657.RootElement.GetProperty("publicBuildStrategyConditionalGo").GetBoolean());
        Assert.IsTrue(m657.RootElement.GetProperty("internalCandidateReady").GetBoolean());
        Assert.IsFalse(m657.RootElement.GetProperty("publicReleaseReady").GetBoolean());
        Assert.IsFalse(m657.RootElement.GetProperty("runtimeProductiveEnabled").GetBoolean());
        Assert.IsFalse(m657.RootElement.GetProperty("providerCloudEnabled").GetBoolean());
        Assert.IsFalse(m657.RootElement.GetProperty("filesystemEnabled").GetBoolean());
        Assert.IsFalse(m657.RootElement.GetProperty("browserAutomationEnabled").GetBoolean());
        Assert.IsFalse(m657.RootElement.GetProperty("capabilityUnlockEnabled").GetBoolean());
    }

    [TestMethod]
    public void ManifestJsonUnchanged()
    {
        Assert.AreEqual("76859A171D0FBC585E96253D0F269AEF4A54DCFD5F704DE9DB92D26EB1AEDDFD", Sha256Hex(ManifestPath));
    }

    [TestMethod]
    public void SidepanelHtmlUnchanged()
    {
        Assert.AreEqual("8A2123D2DE578C8A026B3CB15D71C1E47A015FB66B68397DFB9DDBADF35877EB", Sha256Hex(SidepanelHtmlPath));
    }

    [TestMethod]
    public void SidepanelCssUnchanged()
    {
        Assert.AreEqual("69E508F153A2D58DC6824BFFF041636C9D94181C97CA2CD929DF091BD434F61B", Sha256Hex(SidepanelCssPath));
    }

    [TestMethod]
    public void SidepanelJsUnchanged()
    {
        Assert.AreEqual("5936C1B95AEC7745A76EA32CE1ED0FFE10309FA8B9879FD685F75F4FBC77F8D6", Sha256Hex(SidepanelJsPath));
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
