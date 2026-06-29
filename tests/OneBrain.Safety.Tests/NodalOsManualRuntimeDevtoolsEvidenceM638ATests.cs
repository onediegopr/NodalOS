using System.Security.Cryptography;
using System.Text.Json;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace OneBrain.Safety.Tests;

[TestClass]
[TestCategory("ManualRuntimeDevtoolsEvidence")]
[TestCategory("M638A-MANUAL")]
public sealed class NodalOsManualRuntimeDevtoolsEvidenceM638ATests
{
    private const string ManualPath = "artifacts/agent-operations/m638a-manual/manual-runtime-devtools-evidence.json";
    private const string RuntimePath = "artifacts/agent-operations/m638a-manual/runtime-tab-evidence.json";
    private const string DevtoolsPath = "artifacts/agent-operations/m638a-manual/service-worker-devtools-evidence.json";
    private const string GoNoGoPath = "artifacts/agent-operations/m638a-manual/post-m638a-manual-go-no-go.json";
    private const string ReportPath = "docs/reports/manual-runtime-devtools-evidence-m638a.md";

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

    private static string ReadRepoText(string relativePath) =>
        File.ReadAllText(FullPath(relativePath));

    private static JsonDocument ReadJson(string relativePath) =>
        JsonDocument.Parse(ReadRepoText(relativePath));

    private static string Sha256Hex(string relativePath)
    {
        var bytes = File.ReadAllBytes(FullPath(relativePath));
        return Convert.ToHexString(SHA256.HashData(bytes));
    }

    [TestMethod]
    public void ManualRuntimeDevtoolsEvidenceArtifactExists() =>
        Assert.IsTrue(File.Exists(FullPath(ManualPath)), ManualPath);

    [TestMethod]
    public void RuntimeTabEvidenceArtifactExists() =>
        Assert.IsTrue(File.Exists(FullPath(RuntimePath)), RuntimePath);

    [TestMethod]
    public void ServiceWorkerDevtoolsEvidenceArtifactExists() =>
        Assert.IsTrue(File.Exists(FullPath(DevtoolsPath)), DevtoolsPath);

    [TestMethod]
    public void GoNoGoArtifactExists() =>
        Assert.IsTrue(File.Exists(FullPath(GoNoGoPath)), GoNoGoPath);

    [TestMethod]
    public void ReportMarkdownExists() =>
        Assert.IsTrue(File.Exists(FullPath(ReportPath)), ReportPath);

    [TestMethod]
    public void ManualEvidenceIdentifiesM638AManualBaseCommit()
    {
        using var doc = ReadJson(ManualPath);
        Assert.AreEqual("M638A-MANUAL", doc.RootElement.GetProperty("milestone").GetString());
        Assert.AreEqual("M638A-RETRY", doc.RootElement.GetProperty("basedOnMilestone").GetString());
        Assert.AreEqual("d156540bc28c0cf1f6b0722872c0093e0e2f6b9f", doc.RootElement.GetProperty("basedOnCommit").GetString());
    }

    [TestMethod]
    public void ManualEvidenceIsEvidenceOnlyAndKeepsReleasePublicBlocked()
    {
        using var doc = ReadJson(ManualPath);
        Assert.IsTrue(doc.RootElement.GetProperty("manualEvidenceOnly").GetBoolean());
        Assert.IsFalse(doc.RootElement.GetProperty("productFilesModified").GetBoolean());
        Assert.IsFalse(doc.RootElement.GetProperty("releasePublicReady").GetBoolean());
    }

    [TestMethod]
    public void ManualEvidenceRecordsBridgeLivenessPassWithoutInventingRuntimeOrDevtools()
    {
        using var doc = ReadJson(ManualPath);
        Assert.AreEqual("pass", doc.RootElement.GetProperty("bridgeLivenessEvidence").GetString());
        Assert.IsFalse(doc.RootElement.GetProperty("runtimeTabEvidenceProvided").GetBoolean());
        Assert.IsFalse(doc.RootElement.GetProperty("serviceWorkerDevtoolsEvidenceProvided").GetBoolean());
        Assert.AreEqual("unknown", doc.RootElement.GetProperty("cspViolationCheck").GetString());
        Assert.AreEqual("unknown", doc.RootElement.GetProperty("errConnectionRefusedCheck").GetString());
        Assert.AreEqual("unknown", doc.RootElement.GetProperty("invalidTokenCheck").GetString());
        Assert.AreEqual("unknown", doc.RootElement.GetProperty("policyViolation1008Check").GetString());
        Assert.AreEqual("unknown", doc.RootElement.GetProperty("bridgeWebSocketErrorCheck").GetString());
        Assert.AreEqual("unknown", doc.RootElement.GetProperty("webSocketReconnectingCheck").GetString());
    }

    [TestMethod]
    public void RuntimeEvidenceRecordsUnknownWhenManualRuntimeTabEvidenceIsNotProvided()
    {
        using var doc = ReadJson(RuntimePath);
        Assert.AreEqual("M638A-MANUAL", doc.RootElement.GetProperty("milestone").GetString());
        Assert.IsFalse(doc.RootElement.GetProperty("runtimeTabOpened").GetBoolean());
        Assert.AreEqual("unknown", doc.RootElement.GetProperty("healthOk").GetString());
        Assert.AreEqual("unknown", doc.RootElement.GetProperty("clientsObserved").GetString());
        Assert.AreEqual("unknown", doc.RootElement.GetProperty("heartbeatOk").GetString());
        Assert.IsFalse(doc.RootElement.GetProperty("screenshotProvided").GetBoolean());
        Assert.IsTrue(doc.RootElement.GetProperty("bridgeRuntimeEndpointOk").GetBoolean());
        Assert.IsTrue(doc.RootElement.GetProperty("bridgeDebugExtensionHelloObserved").GetBoolean());
    }

    [TestMethod]
    public void ServiceWorkerDevtoolsEvidenceDoesNotAllowRawConsoleExcerpt()
    {
        using var doc = ReadJson(DevtoolsPath);
        Assert.AreEqual("M638A-MANUAL", doc.RootElement.GetProperty("milestone").GetString());
        Assert.IsFalse(doc.RootElement.GetProperty("devtoolsConsoleProvided").GetBoolean());
        Assert.IsFalse(doc.RootElement.GetProperty("serviceWorkerConsoleInspected").GetBoolean());
        Assert.IsFalse(doc.RootElement.GetProperty("afterExtensionReloadWithBridgeLive").GetBoolean());
        Assert.IsFalse(doc.RootElement.GetProperty("rawConsoleExcerptAllowed").GetBoolean());
    }

    [TestMethod]
    public void GoNoGoKeepsReleasePublicAndSensitiveChangesBlocked()
    {
        using var doc = ReadJson(GoNoGoPath);
        Assert.IsTrue(doc.RootElement.GetProperty("readyForReleaseEvidenceGate").GetBoolean());
        Assert.IsFalse(doc.RootElement.GetProperty("readyForReleasePublic").GetBoolean());
        Assert.IsFalse(doc.RootElement.GetProperty("readyForJsChanges").GetBoolean());
        Assert.IsFalse(doc.RootElement.GetProperty("readyForRuntimeChanges").GetBoolean());
        Assert.IsFalse(doc.RootElement.GetProperty("readyForProviderCloud").GetBoolean());
        Assert.IsFalse(doc.RootElement.GetProperty("readyForFilesystem").GetBoolean());
        Assert.IsTrue(doc.RootElement.GetProperty("bridgeLivenessConfirmed").GetBoolean());
        Assert.IsFalse(doc.RootElement.GetProperty("manualRuntimeEvidenceCaptured").GetBoolean());
        Assert.IsFalse(doc.RootElement.GetProperty("manualServiceWorkerDevtoolsEvidenceCaptured").GetBoolean());
        Assert.AreEqual("M638A-MANUAL-RETRY", doc.RootElement.GetProperty("recommendedNextMilestone").GetString());
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
