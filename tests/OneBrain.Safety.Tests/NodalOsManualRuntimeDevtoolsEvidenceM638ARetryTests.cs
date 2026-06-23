using System.Security.Cryptography;
using System.Text.Json;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace OneBrain.Safety.Tests;

[TestClass]
[TestCategory("ManualRuntimeDevtoolsEvidence")]
[TestCategory("M638A-MANUAL-RETRY")]
public sealed class NodalOsManualRuntimeDevtoolsEvidenceM638ARetryTests
{
    private const string ManualPath = "artifacts/agent-operations/m638a-manual-retry/manual-runtime-devtools-evidence.json";
    private const string RuntimePath = "artifacts/agent-operations/m638a-manual-retry/runtime-tab-evidence.json";
    private const string DevtoolsPath = "artifacts/agent-operations/m638a-manual-retry/service-worker-devtools-evidence.json";
    private const string GoNoGoPath = "artifacts/agent-operations/m638a-manual-retry/post-m638a-manual-retry-go-no-go.json";
    private const string ReportPath = "docs/reports/manual-runtime-devtools-evidence-m638a-retry.md";

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
    public void ManualEvidenceIdentifiesM638AManualRetryBaseCommit()
    {
        using var doc = ReadJson(ManualPath);
        Assert.AreEqual("M638A-MANUAL-RETRY", doc.RootElement.GetProperty("milestone").GetString());
        Assert.AreEqual("M638A-MANUAL", doc.RootElement.GetProperty("basedOnMilestone").GetString());
        Assert.AreEqual("8abf08caa40f690506f69fe3175fa5c05bcc97a8", doc.RootElement.GetProperty("basedOnCommit").GetString());
    }

    [TestMethod]
    public void ManualEvidenceRecordsCleanUserReportedRuntimeAndDevtools()
    {
        using var doc = ReadJson(ManualPath);
        Assert.AreEqual("CLEAN_RUNTIME_DEVTOOLS_EVIDENCE_READY", doc.RootElement.GetProperty("decision").GetString());
        Assert.IsTrue(doc.RootElement.GetProperty("manualEvidenceOnly").GetBoolean());
        Assert.AreEqual("user-reported", doc.RootElement.GetProperty("manualEvidenceSource").GetString());
        Assert.AreEqual("pass", doc.RootElement.GetProperty("bridgeLiveness").GetString());
        Assert.IsTrue(doc.RootElement.GetProperty("runtimeTabEvidenceProvided").GetBoolean());
        Assert.IsTrue(doc.RootElement.GetProperty("serviceWorkerDevtoolsEvidenceProvided").GetBoolean());
        Assert.IsFalse(doc.RootElement.GetProperty("productFilesModified").GetBoolean());
        Assert.IsFalse(doc.RootElement.GetProperty("releasePublicReady").GetBoolean());
    }

    [TestMethod]
    public void RuntimeTabEvidenceIsClean()
    {
        using var doc = ReadJson(RuntimePath);
        Assert.IsTrue(doc.RootElement.GetProperty("runtimeTabOpened").GetBoolean());
        Assert.AreEqual("pass", doc.RootElement.GetProperty("healthOk").GetString());
        Assert.AreEqual("pass", doc.RootElement.GetProperty("clientsObserved").GetString());
        Assert.AreEqual("pass", doc.RootElement.GetProperty("heartbeatOk").GetString());
        Assert.AreEqual("no", doc.RootElement.GetProperty("webSocketReconnectingVisible").GetString());
        Assert.AreEqual("no", doc.RootElement.GetProperty("bridgeWebSocketErrorVisible").GetString());
        Assert.IsFalse(doc.RootElement.GetProperty("screenshotProvided").GetBoolean());
        Assert.AreEqual("user-reported", doc.RootElement.GetProperty("manualEvidenceSource").GetString());
    }

    [TestMethod]
    public void ServiceWorkerDevtoolsEvidenceIsCleanAndDoesNotStoreRawConsole()
    {
        using var doc = ReadJson(DevtoolsPath);
        Assert.IsTrue(doc.RootElement.GetProperty("devtoolsConsoleProvided").GetBoolean());
        Assert.IsTrue(doc.RootElement.GetProperty("serviceWorkerConsoleInspected").GetBoolean());
        Assert.IsTrue(doc.RootElement.GetProperty("afterExtensionReloadWithBridgeLive").GetBoolean());
        Assert.AreEqual("no", doc.RootElement.GetProperty("cspViolationsObserved").GetString());
        Assert.AreEqual("no", doc.RootElement.GetProperty("errConnectionRefusedObserved").GetString());
        Assert.AreEqual("no", doc.RootElement.GetProperty("invalidTokenObserved").GetString());
        Assert.AreEqual("no", doc.RootElement.GetProperty("policyViolation1008Observed").GetString());
        Assert.AreEqual("no", doc.RootElement.GetProperty("bridgeWebSocketErrorRepeatedObserved").GetString());
        Assert.AreEqual("no", doc.RootElement.GetProperty("webSocketReconnectingObserved").GetString());
        Assert.AreEqual("no", doc.RootElement.GetProperty("criticalErrorsObserved").GetString());
        Assert.AreEqual(0, doc.RootElement.GetProperty("warningsObserved").GetArrayLength());
        Assert.IsFalse(doc.RootElement.GetProperty("rawConsoleExcerptAllowed").GetBoolean());
    }

    [TestMethod]
    public void GoNoGoKeepsPublicReleaseAndSensitiveChangesBlocked()
    {
        using var doc = ReadJson(GoNoGoPath);
        Assert.IsTrue(doc.RootElement.GetProperty("readyForReleaseEvidenceGate").GetBoolean());
        Assert.IsFalse(doc.RootElement.GetProperty("readyForReleasePublic").GetBoolean());
        Assert.IsFalse(doc.RootElement.GetProperty("readyForJsChanges").GetBoolean());
        Assert.IsFalse(doc.RootElement.GetProperty("readyForRuntimeChanges").GetBoolean());
        Assert.IsFalse(doc.RootElement.GetProperty("readyForProviderCloud").GetBoolean());
        Assert.IsFalse(doc.RootElement.GetProperty("readyForFilesystem").GetBoolean());
        Assert.IsTrue(doc.RootElement.GetProperty("cleanManualEvidenceCaptured").GetBoolean());
        Assert.AreEqual("M639 Roadmap Consolidation Update / Extended Architecture Pack", doc.RootElement.GetProperty("recommendedNextMilestone").GetString());
    }

    [TestMethod]
    public void ManifestJsonUnchanged()
    {
        Assert.AreEqual("76859A171D0FBC585E96253D0F269AEF4A54DCFD5F704DE9DB92D26EB1AEDDFD", Sha256Hex(ManifestPath));
    }

    [TestMethod]
    public void SidepanelHtmlUnchanged()
    {
        Assert.AreEqual("4A9642242F742B641B60430EB16647DD4A989EBCCCB072D0296B8CDCDE6E88C2", Sha256Hex(SidepanelHtmlPath));
    }

    [TestMethod]
    public void SidepanelCssUnchanged()
    {
        Assert.AreEqual("D2A14687DB6E201353A100A33B72AECB3C1858C1127114979945750AB5B717AC", Sha256Hex(SidepanelCssPath));
    }

    [TestMethod]
    public void SidepanelJsUnchanged()
    {
        Assert.AreEqual("FED938DE2C42EC56F9061E2587A57338DAD1A770BBFAD2B710937BBD97D9D329", Sha256Hex(SidepanelJsPath));
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
        Assert.AreEqual("D4E043EC6EC181D780FD2A18AAC48BB39FED4F9078E03229BF5F718635031712", Sha256Hex(BridgeOptionsPath));
        Assert.AreEqual("A22DAE8449A297DC77E5E0E28D404DA253846BEC541033F2E0EF6AC779562624", Sha256Hex(BridgeProtocolPath));
        Assert.AreEqual("5E933A21C501487523C60197F02E0D68CE6D0F5865DC9401F9337EBA41B58B37", Sha256Hex(BridgeSecurityPath));
        Assert.AreEqual("8CF05765736DEF46584022F1E33E22CD4E85A3C08A952048D425FBBC1EFE5309", Sha256Hex(BridgeSelfTestPath));
        Assert.AreEqual("CAACC05544B9E41F7606BD17ECAD81FB36842869BC34E46F8596D4D62EB5B5C4", Sha256Hex(BridgeOpenAiPath));
        Assert.AreEqual("061CED72CF27BC31C0BAD240BC698B58D0D5966D6D107CE8A315C98D52DB1305", Sha256Hex(BridgeProgramPath));
        Assert.AreEqual("E00407FA4BC6DCB8ADBB677E5FE4AF41FE7BDB967C5827EBF54569E6DF32E85D", Sha256Hex(BridgeProjectPath));
    }
}
