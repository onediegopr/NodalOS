using System.Security.Cryptography;
using System.Text.Json;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace OneBrain.Safety.Tests;

[TestClass]
[TestCategory("CleanDevtoolsEvidenceCapture")]
[TestCategory("M638A-RETRY")]
public sealed class NodalOsCleanDevtoolsEvidenceCaptureM638ARetryTests
{
    private const string StartupPath = "artifacts/agent-operations/m638a-retry/bridge-startup-liveness-recovery.json";
    private const string CapturePath = "artifacts/agent-operations/m638a-retry/clean-devtools-evidence-capture.json";
    private const string RuntimePath = "artifacts/agent-operations/m638a-retry/runtime-tab-evidence.json";
    private const string DevtoolsPath = "artifacts/agent-operations/m638a-retry/devtools-console-evidence.json";
    private const string LivenessPath = "artifacts/agent-operations/m638a-retry/bridge-liveness-evidence.json";
    private const string GoNoGoPath = "artifacts/agent-operations/m638a-retry/post-m638a-retry-go-no-go.json";
    private const string ReportPath = "docs/reports/clean-devtools-evidence-capture-m638a-retry.md";

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
    public void BridgeStartupLivenessRecoveryArtifactExists() =>
        Assert.IsTrue(File.Exists(FullPath(StartupPath)), StartupPath);

    [TestMethod]
    public void CleanDevtoolsEvidenceCaptureArtifactExists() =>
        Assert.IsTrue(File.Exists(FullPath(CapturePath)), CapturePath);

    [TestMethod]
    public void RuntimeTabEvidenceArtifactExists() =>
        Assert.IsTrue(File.Exists(FullPath(RuntimePath)), RuntimePath);

    [TestMethod]
    public void DevtoolsConsoleEvidenceArtifactExists() =>
        Assert.IsTrue(File.Exists(FullPath(DevtoolsPath)), DevtoolsPath);

    [TestMethod]
    public void BridgeLivenessEvidenceArtifactExists() =>
        Assert.IsTrue(File.Exists(FullPath(LivenessPath)), LivenessPath);

    [TestMethod]
    public void GoNoGoArtifactExists() =>
        Assert.IsTrue(File.Exists(FullPath(GoNoGoPath)), GoNoGoPath);

    [TestMethod]
    public void ReportMarkdownExists() =>
        Assert.IsTrue(File.Exists(FullPath(ReportPath)), ReportPath);

    [TestMethod]
    public void StartupIdentifiesM638ARetryBaseMilestoneAndCommit()
    {
        using var doc = ReadJson(StartupPath);
        Assert.AreEqual("M638A-RETRY", doc.RootElement.GetProperty("milestone").GetString());
        Assert.AreEqual("M638A", doc.RootElement.GetProperty("basedOnMilestone").GetString());
        Assert.AreEqual("040615e36b514403726278e93c7d44720c5df9d0", doc.RootElement.GetProperty("basedOnCommit").GetString());
    }

    [TestMethod]
    public void CaptureIsEvidenceOnlyAndKeepsProductFilesUnmodified()
    {
        using var doc = ReadJson(CapturePath);
        Assert.AreEqual("M638A-RETRY", doc.RootElement.GetProperty("milestone").GetString());
        Assert.IsTrue(doc.RootElement.GetProperty("evidenceCaptureOnly").GetBoolean());
        Assert.IsFalse(doc.RootElement.GetProperty("productFilesModified").GetBoolean());
        Assert.IsFalse(doc.RootElement.GetProperty("releasePublicReady").GetBoolean());
    }

    [TestMethod]
    public void RetryRecordsRecoveredBridgeLiveness()
    {
        using var startup = ReadJson(StartupPath);
        using var liveness = ReadJson(LivenessPath);
        Assert.IsTrue(startup.RootElement.GetProperty("bridgeStartupAttempted").GetBoolean());
        Assert.IsTrue(startup.RootElement.GetProperty("runbookUsed").GetBoolean());
        Assert.AreEqual("yes", startup.RootElement.GetProperty("bridgeProcessObserved").GetString());
        Assert.AreEqual("pass", startup.RootElement.GetProperty("tcpListener1270018787").GetString());
        Assert.AreEqual("pass", startup.RootElement.GetProperty("bridgeLivenessAfterStartup").GetString());
        Assert.IsTrue(startup.RootElement.GetProperty("captureAllowed").GetBoolean());

        Assert.IsTrue(liveness.RootElement.GetProperty("scriptRun").GetBoolean());
        Assert.AreEqual("pass", liveness.RootElement.GetProperty("tcp1270018787").GetString());
        Assert.AreEqual("pass", liveness.RootElement.GetProperty("healthEndpoint").GetString());
        Assert.AreEqual("pass", liveness.RootElement.GetProperty("runtimeEndpoint").GetString());
        Assert.AreEqual("pass", liveness.RootElement.GetProperty("debugEndpoint").GetString());
        Assert.AreEqual("pass", liveness.RootElement.GetProperty("configPublicEndpoint").GetString());
        Assert.AreEqual("pass", liveness.RootElement.GetProperty("wsUpgradeEndpoint").GetString());
    }

    [TestMethod]
    public void CaptureHonestlyRecordsMissingDevtoolsAndRuntimeEvidence()
    {
        using var doc = ReadJson(CapturePath);
        Assert.AreEqual("pass", doc.RootElement.GetProperty("bridgeLivenessEvidence").GetString());
        Assert.IsFalse(doc.RootElement.GetProperty("runtimeTabEvidenceProvided").GetBoolean());
        Assert.IsFalse(doc.RootElement.GetProperty("devtoolsConsoleEvidenceProvided").GetBoolean());
        Assert.AreEqual("unknown", doc.RootElement.GetProperty("cspViolationCheck").GetString());
        Assert.AreEqual("unknown", doc.RootElement.GetProperty("errConnectionRefusedCheck").GetString());
        Assert.AreEqual("unknown", doc.RootElement.GetProperty("invalidTokenCheck").GetString());
        Assert.AreEqual("unknown", doc.RootElement.GetProperty("policyViolation1008Check").GetString());
        Assert.AreEqual("unknown", doc.RootElement.GetProperty("bridgeWebSocketErrorCheck").GetString());
        Assert.AreEqual("unknown", doc.RootElement.GetProperty("webSocketReconnectingCheck").GetString());
    }

    [TestMethod]
    public void DevtoolsEvidenceDoesNotAllowRawConsoleExcerpt()
    {
        using var doc = ReadJson(DevtoolsPath);
        Assert.IsFalse(doc.RootElement.GetProperty("devtoolsConsoleProvided").GetBoolean());
        Assert.IsFalse(doc.RootElement.GetProperty("serviceWorkerConsoleInspected").GetBoolean());
        Assert.IsFalse(doc.RootElement.GetProperty("afterExtensionReloadWithBridgeLive").GetBoolean());
        Assert.IsFalse(doc.RootElement.GetProperty("rawConsoleExcerptAllowed").GetBoolean());
    }

    [TestMethod]
    public void RuntimeEvidenceRemainsUnknownWithoutVisualInspection()
    {
        using var doc = ReadJson(RuntimePath);
        Assert.IsFalse(doc.RootElement.GetProperty("runtimeTabOpened").GetBoolean());
        Assert.AreEqual("unknown", doc.RootElement.GetProperty("healthOk").GetString());
        Assert.AreEqual("unknown", doc.RootElement.GetProperty("clientsObserved").GetString());
        Assert.AreEqual("unknown", doc.RootElement.GetProperty("heartbeatOk").GetString());
        Assert.IsFalse(doc.RootElement.GetProperty("screenshotProvided").GetBoolean());
        Assert.IsTrue(doc.RootElement.GetProperty("bridgeDebugExtensionHelloObserved").GetBoolean());
    }

    [TestMethod]
    public void IfBridgeLivenessAfterStartupFailsThenCaptureIsBlocked()
    {
        using var startup = ReadJson(StartupPath);
        if (startup.RootElement.GetProperty("bridgeLivenessAfterStartup").GetString() == "fail")
        {
            Assert.IsFalse(startup.RootElement.GetProperty("captureAllowed").GetBoolean());
        }
    }

    [TestMethod]
    public void IfBridgeLivenessAfterStartupFailsThenRecommendedNextMilestoneMentionsBridgeStartupRequired()
    {
        using var startup = ReadJson(StartupPath);
        using var go = ReadJson(GoNoGoPath);
        if (startup.RootElement.GetProperty("bridgeLivenessAfterStartup").GetString() == "fail")
        {
            StringAssert.Contains(go.RootElement.GetProperty("recommendedNextMilestone").GetString(), "Bridge Startup Required");
        }
    }

    [TestMethod]
    public void IfBridgeLivenessPassAndCleanDevtoolsCapturedThenRecommendedNextMilestoneMayBeM639()
    {
        using var capture = ReadJson(CapturePath);
        using var go = ReadJson(GoNoGoPath);
        if (capture.RootElement.GetProperty("bridgeLivenessEvidence").GetString() == "pass" &&
            go.RootElement.GetProperty("cleanDevtoolsEvidenceCaptured").GetBoolean())
        {
            StringAssert.Contains(go.RootElement.GetProperty("recommendedNextMilestone").GetString(), "M639");
        }
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
        Assert.IsTrue(doc.RootElement.GetProperty("bridgeLivenessRecovered").GetBoolean());
        Assert.IsFalse(doc.RootElement.GetProperty("cleanDevtoolsEvidenceCaptured").GetBoolean());
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
