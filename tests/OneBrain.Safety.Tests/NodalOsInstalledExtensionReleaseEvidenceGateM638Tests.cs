using System.Security.Cryptography;
using System.Text.Json;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace OneBrain.Safety.Tests;

[TestClass]
[TestCategory("InstalledExtensionReleaseEvidenceGate")]
[TestCategory("M638")]
public sealed class NodalOsInstalledExtensionReleaseEvidenceGateM638Tests
{
    private const string GatePath = "artifacts/agent-operations/m638/installed-extension-release-evidence-gate.json";
    private const string ChecklistPath = "artifacts/agent-operations/m638/release-evidence-checklist.json";
    private const string BlockersPath = "artifacts/agent-operations/m638/release-blockers-register.json";
    private const string ProviderRiskPath = "artifacts/agent-operations/m638/provider-runtime-release-risk-register.json";
    private const string GoNoGoPath = "artifacts/agent-operations/m638/post-m638-go-no-go.json";
    private const string ReportPath = "docs/reports/installed-extension-release-evidence-gate-m638.md";

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

    private static void AssertArrayContainsId(JsonElement array, string id)
    {
        Assert.IsTrue(
            array.EnumerateArray().Any(item =>
                item.TryGetProperty("id", out var itemId) &&
                string.Equals(itemId.GetString(), id, StringComparison.Ordinal)),
            $"Expected blockers/checklist to include {id}.");
    }

    [TestMethod]
    public void InstalledExtensionReleaseEvidenceGateArtifactExists() =>
        Assert.IsTrue(File.Exists(FullPath(GatePath)), GatePath);

    [TestMethod]
    public void ReleaseEvidenceChecklistArtifactExists() =>
        Assert.IsTrue(File.Exists(FullPath(ChecklistPath)), ChecklistPath);

    [TestMethod]
    public void ReleaseBlockersRegisterArtifactExists() =>
        Assert.IsTrue(File.Exists(FullPath(BlockersPath)), BlockersPath);

    [TestMethod]
    public void ProviderRuntimeReleaseRiskRegisterArtifactExists() =>
        Assert.IsTrue(File.Exists(FullPath(ProviderRiskPath)), ProviderRiskPath);

    [TestMethod]
    public void PostM638GoNoGoArtifactExists() =>
        Assert.IsTrue(File.Exists(FullPath(GoNoGoPath)), GoNoGoPath);

    [TestMethod]
    public void ReportMarkdownExists() =>
        Assert.IsTrue(File.Exists(FullPath(ReportPath)), ReportPath);

    [TestMethod]
    public void GateRecordsAuditConditionalGoAndClosedQaLine()
    {
        using var doc = ReadJson(GatePath);
        Assert.AreEqual("AUDIT_CONDITIONAL_GO", doc.RootElement.GetProperty("auditVerdict").GetString());
        Assert.IsTrue(doc.RootElement.GetProperty("installedExtensionQaLineClosed").GetBoolean());
        Assert.IsTrue(doc.RootElement.GetProperty("releaseEvidenceGateCanStart").GetBoolean());
    }

    [TestMethod]
    public void GateKeepsPublicReleaseBlockedAndProductFilesUnmodified()
    {
        using var doc = ReadJson(GatePath);
        Assert.IsFalse(doc.RootElement.GetProperty("releasePublicReady").GetBoolean());
        Assert.IsFalse(doc.RootElement.GetProperty("runtimeScreenshotProvided").GetBoolean());
        Assert.IsFalse(doc.RootElement.GetProperty("devtoolsConsoleProvided").GetBoolean());
        Assert.AreEqual("unknown", doc.RootElement.GetProperty("cspViolationCheck").GetString());
        Assert.IsFalse(doc.RootElement.GetProperty("productFilesModified").GetBoolean());
    }

    [TestMethod]
    public void ProviderRuntimeRiskRegisterKeepsProviderReleaseBlocked()
    {
        using var doc = ReadJson(ProviderRiskPath);
        Assert.IsTrue(doc.RootElement.GetProperty("openAiAgentClientPresent").GetBoolean());
        Assert.IsTrue(doc.RootElement.GetProperty("providerCanCallExternalApiIfKeyPresent").GetBoolean());
        Assert.IsTrue(doc.RootElement.GetProperty("providerReleaseGateRequired").GetBoolean());
        Assert.IsFalse(doc.RootElement.GetProperty("providerRuntimeReady").GetBoolean());
        Assert.IsFalse(doc.RootElement.GetProperty("providerCloudEnabledForRelease").GetBoolean());
        Assert.AreEqual("ONE BRAIN Chrome Lab Agent", doc.RootElement.GetProperty("providerPromptNamingDebt").GetString());
    }

    [TestMethod]
    public void PostM638GoNoGoKeepsReleasePublicJsRuntimeProviderAndFilesystemBlocked()
    {
        using var doc = ReadJson(GoNoGoPath);
        Assert.IsTrue(doc.RootElement.GetProperty("readyForReleaseEvidenceGate").GetBoolean());
        Assert.IsFalse(doc.RootElement.GetProperty("readyForReleasePublic").GetBoolean());
        Assert.IsFalse(doc.RootElement.GetProperty("readyForJsChanges").GetBoolean());
        Assert.IsFalse(doc.RootElement.GetProperty("readyForRuntimeChanges").GetBoolean());
        Assert.IsFalse(doc.RootElement.GetProperty("readyForProviderCloud").GetBoolean());
        Assert.IsFalse(doc.RootElement.GetProperty("readyForFilesystem").GetBoolean());
    }

    [TestMethod]
    public void ReleaseBlockersIncludeCleanDevToolsHostPermissionsProviderGateAndIpv6Caveat()
    {
        using var doc = ReadJson(BlockersPath);
        AssertArrayContainsId(doc.RootElement, "RELEASE-BLOCKER-DEVTOOLS-CLEAN-EVIDENCE");
        AssertArrayContainsId(doc.RootElement, "RELEASE-BLOCKER-HOST-PERMISSIONS-REVIEW");
        AssertArrayContainsId(doc.RootElement, "RELEASE-BLOCKER-PROVIDER-RUNTIME-GATE");
        AssertArrayContainsId(doc.RootElement, "RELEASE-CAVEAT-IPV6-LOOPBACK");
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

    [TestMethod]
    public void HostPermissionsRemainBroadAndReviewedForReleaseGate()
    {
        using var manifest = ReadJson(ManifestPath);
        var hostPermissions = manifest.RootElement.GetProperty("host_permissions").EnumerateArray().Select(x => x.GetString()).ToArray();
        CollectionAssert.AreEqual(new[] { "http://*/*", "https://*/*" }, hostPermissions);

        using var gate = ReadJson(GatePath);
        Assert.IsTrue(gate.RootElement.GetProperty("hostPermissionsReviewed").GetBoolean());
    }

    [TestMethod]
    public void ReleaseEvidenceChecklistIncludesCleanConsoleAsUnknown()
    {
        using var doc = ReadJson(ChecklistPath);
        AssertArrayContainsId(doc.RootElement.GetProperty("checklist"), "DEVTOOLS-CLEAN-CONSOLE");
        Assert.IsFalse(doc.RootElement.GetProperty("publicReleaseEvidenceComplete").GetBoolean());
    }
}
