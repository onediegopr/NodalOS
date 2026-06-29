using System.Security.Cryptography;
using System.Text.Json;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace OneBrain.Safety.Tests;

[TestClass]
[TestCategory("HostPermissionsFinalDecision")]
[TestCategory("M651")]
public sealed class NodalOsHostPermissionsFinalDecisionM651Tests
{
    private const string ReportPath = "docs/reports/m651-host-permissions-final-decision.md";
    private const string FinalDecisionPath = "artifacts/agent-operations/m651/host-permissions-final-decision.json";
    private const string StrategyOptionsPath = "artifacts/agent-operations/m651/manifest-strategy-options.json";
    private const string ImpactAssessmentPath = "artifacts/agent-operations/m651/host-permissions-impact-assessment.json";
    private const string BuildStrategyPath = "artifacts/agent-operations/m651/public-build-vs-internal-build-strategy.json";
    private const string GoNoGoPath = "artifacts/agent-operations/m651/m651-go-no-go.json";

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
    public void M651ArtifactsExist()
    {
        Assert.IsTrue(File.Exists(FullPath(ReportPath)), ReportPath);
        Assert.IsTrue(File.Exists(FullPath(FinalDecisionPath)), FinalDecisionPath);
        Assert.IsTrue(File.Exists(FullPath(StrategyOptionsPath)), StrategyOptionsPath);
        Assert.IsTrue(File.Exists(FullPath(ImpactAssessmentPath)), ImpactAssessmentPath);
        Assert.IsTrue(File.Exists(FullPath(BuildStrategyPath)), BuildStrategyPath);
        Assert.IsTrue(File.Exists(FullPath(GoNoGoPath)), GoNoGoPath);
    }

    [TestMethod]
    public void FinalDecisionDocumentsCurrentHostPermissionsAndDoesNotModifyManifest()
    {
        using var doc = ReadJson(FinalDecisionPath);
        Assert.AreEqual("HOST_PERMISSIONS_FINAL_DECISION_READY", doc.RootElement.GetProperty("decision").GetString());
        CollectionAssert.Contains(doc.RootElement.GetProperty("currentHostPermissions").EnumerateArray().Select(item => item.GetString()).ToArray(), "http://*/*");
        CollectionAssert.Contains(doc.RootElement.GetProperty("currentHostPermissions").EnumerateArray().Select(item => item.GetString()).ToArray(), "https://*/*");
        Assert.IsFalse(doc.RootElement.GetProperty("manifestModified").GetBoolean());
        Assert.IsFalse(doc.RootElement.GetProperty("permissionsModified").GetBoolean());
        Assert.IsFalse(doc.RootElement.GetProperty("hostPermissionsModified").GetBoolean());
        Assert.IsFalse(doc.RootElement.GetProperty("productFilesModified").GetBoolean());
    }

    [TestMethod]
    public void FinalDecisionRecommendsSplitBuildAndKeepsPublicReleaseBlocked()
    {
        using var doc = ReadJson(FinalDecisionPath);
        Assert.AreEqual("split_internal_public_build", doc.RootElement.GetProperty("recommendedStrategy").GetString());
        Assert.IsTrue(doc.RootElement.GetProperty("internalCandidateAllowed").GetBoolean());
        Assert.IsFalse(doc.RootElement.GetProperty("publicReleaseReady").GetBoolean());
        Assert.IsTrue(doc.RootElement.GetProperty("publicReleaseBlockedByHostPermissions").GetBoolean());
        Assert.IsTrue(doc.RootElement.GetProperty("requiresFutureManifestMilestone").GetBoolean());
        Assert.AreEqual("M651A Host Permissions Narrowing Patch Plan", doc.RootElement.GetProperty("recommendedNextMilestone").GetString());
    }

    [TestMethod]
    public void ManifestStrategyOptionsCompareAllRequiredStrategies()
    {
        using var doc = ReadJson(StrategyOptionsPath);
        Assert.AreEqual("MANIFEST_STRATEGY_OPTIONS_READY", doc.RootElement.GetProperty("decision").GetString());
        var options = doc.RootElement.GetProperty("options").EnumerateArray().ToArray();
        Assert.AreEqual(3, options.Length);
        Assert.IsTrue(options.Any(item => item.GetProperty("id").GetString() == "keep_with_strong_justification"));
        Assert.IsTrue(options.Any(item => item.GetProperty("id").GetString() == "future_narrowing_patch"));
        Assert.IsTrue(options.Any(item => item.GetProperty("id").GetString() == "split_internal_public_build"));
        Assert.IsTrue(options.All(item => item.TryGetProperty("benefit", out _)));
        Assert.IsTrue(options.All(item => item.TryGetProperty("risk", out _)));
        Assert.IsTrue(options.All(item => item.TryGetProperty("implementationCost", out _)));
        Assert.IsTrue(options.All(item => item.TryGetProperty("storeRisk", out _)));
        Assert.AreEqual("split_internal_public_build", doc.RootElement.GetProperty("recommendedStrategy").GetString());
        Assert.IsFalse(doc.RootElement.GetProperty("manifestModified").GetBoolean());
    }

    [TestMethod]
    public void ImpactAssessmentEvaluatesInternalAndPublicRelease()
    {
        using var doc = ReadJson(ImpactAssessmentPath);
        Assert.AreEqual("HOST_PERMISSIONS_IMPACT_ASSESSMENT_READY", doc.RootElement.GetProperty("decision").GetString());
        Assert.AreEqual("GO", doc.RootElement.GetProperty("internalCandidateImpact").GetProperty("status").GetString());
        Assert.AreEqual("NO-GO", doc.RootElement.GetProperty("publicReleaseImpact").GetProperty("status").GetString());
        Assert.AreEqual("high-until-public-strategy-closes", doc.RootElement.GetProperty("chromeWebStoreImpact").GetProperty("risk").GetString());
        Assert.IsGreaterThanOrEqualTo(doc.RootElement.GetProperty("functionalityImpactIfNarrowed").GetArrayLength(), 3);
        Assert.IsFalse(doc.RootElement.GetProperty("manifestModified").GetBoolean());
        Assert.IsFalse(doc.RootElement.GetProperty("permissionsModified").GetBoolean());
    }

    [TestMethod]
    public void PublicVsInternalBuildStrategyKeepsPublicBuildBlocked()
    {
        using var doc = ReadJson(BuildStrategyPath);
        Assert.AreEqual("PUBLIC_BUILD_VS_INTERNAL_BUILD_STRATEGY_READY", doc.RootElement.GetProperty("decision").GetString());
        Assert.IsTrue(doc.RootElement.GetProperty("internalCandidateBuildScope").GetProperty("allowed").GetBoolean());
        Assert.IsFalse(doc.RootElement.GetProperty("publicCandidateBuildScope").GetProperty("allowed").GetBoolean());
        Assert.AreEqual("split_internal_public_build", doc.RootElement.GetProperty("recommendedPolicy").GetString());
        Assert.IsFalse(doc.RootElement.GetProperty("manifestModified").GetBoolean());
        Assert.IsFalse(doc.RootElement.GetProperty("permissionsModified").GetBoolean());
        Assert.IsFalse(doc.RootElement.GetProperty("publicReleaseReady").GetBoolean());
    }

    [TestMethod]
    public void GoNoGoKeepsAllSensitivePathsClosed()
    {
        using var doc = ReadJson(GoNoGoPath);
        Assert.AreEqual("HOST_PERMISSIONS_FINAL_DECISION_READY", doc.RootElement.GetProperty("decision").GetString());
        Assert.IsTrue(doc.RootElement.GetProperty("hostPermissionsFinalDecisionReady").GetBoolean());
        Assert.AreEqual("split_internal_public_build", doc.RootElement.GetProperty("recommendedStrategy").GetString());
        Assert.IsTrue(doc.RootElement.GetProperty("internalCandidateReady").GetBoolean());
        Assert.IsFalse(doc.RootElement.GetProperty("publicReleaseReady").GetBoolean());
        Assert.IsTrue(doc.RootElement.GetProperty("publicReleaseBlockedByHostPermissions").GetBoolean());
        Assert.IsTrue(doc.RootElement.GetProperty("requiresFutureManifestMilestone").GetBoolean());
        Assert.IsFalse(doc.RootElement.GetProperty("manifestModified").GetBoolean());
        Assert.IsFalse(doc.RootElement.GetProperty("permissionsModified").GetBoolean());
        Assert.IsFalse(doc.RootElement.GetProperty("hostPermissionsModified").GetBoolean());
        Assert.IsFalse(doc.RootElement.GetProperty("jsModified").GetBoolean());
        Assert.IsFalse(doc.RootElement.GetProperty("bridgeModified").GetBoolean());
        Assert.IsFalse(doc.RootElement.GetProperty("runtimeProductiveEnabled").GetBoolean());
        Assert.IsFalse(doc.RootElement.GetProperty("providerCloudEnabled").GetBoolean());
        Assert.IsFalse(doc.RootElement.GetProperty("filesystemEnabled").GetBoolean());
        Assert.IsFalse(doc.RootElement.GetProperty("browserAutomationEnabled").GetBoolean());
        Assert.IsFalse(doc.RootElement.GetProperty("capabilityUnlockEnabled").GetBoolean());
        Assert.IsFalse(doc.RootElement.GetProperty("productFilesModified").GetBoolean());
    }

    [TestMethod]
    public void ManifestJsonUnchanged()
    {
        Assert.AreEqual("76859A171D0FBC585E96253D0F269AEF4A54DCFD5F704DE9DB92D26EB1AEDDFD", Sha256Hex(ManifestPath));
    }

    [TestMethod]
    public void SidepanelHtmlUnchanged()
    {
        Assert.AreEqual("A8E95DC5772C5B55EFE29A35D57A038B44F11432D2FEA23739553CB5C7C835A9", Sha256Hex(SidepanelHtmlPath));
    }

    [TestMethod]
    public void SidepanelCssUnchanged()
    {
        Assert.AreEqual("C6DA9402E2A859DB8C598F417A6F362B6B819E734F11F7B95DBA5957DE620182", Sha256Hex(SidepanelCssPath));
    }

    [TestMethod]
    public void SidepanelJsUnchanged()
    {
        Assert.AreEqual("9063CDDD2FBE020FB3EDD8EEC9591356DA8B1B54774F3666D0B9E2E76217E6A2", Sha256Hex(SidepanelJsPath));
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
