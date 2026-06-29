using System.Security.Cryptography;
using System.Text.Json;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace OneBrain.Safety.Tests;

[TestClass]
[TestCategory("BridgeLivenessReloadQa")]
[TestCategory("M637H")]
public sealed class NodalOsBridgeLivenessReloadQaM637HTests
{
    private const string ResultPath = "artifacts/agent-operations/m637h/bridge-liveness-reload-qa-result.json";
    private const string SummaryPath = "artifacts/agent-operations/m637h/bridge-liveness-reload-qa-summary.json";
    private const string GoNoGoPath = "artifacts/agent-operations/m637h/post-m637h-go-no-go.json";
    private const string ReportPath = "docs/reports/bridge-liveness-reload-qa-m637h.md";
    private const string ManifestPath = "browser-extension/onebrain-chrome-lab/manifest.json";
    private const string SidepanelHtmlPath = "browser-extension/onebrain-chrome-lab/sidepanel.html";
    private const string SidepanelCssPath = "browser-extension/onebrain-chrome-lab/sidepanel.css";
    private const string SidepanelJsPath = "browser-extension/onebrain-chrome-lab/sidepanel.js";
    private const string ServiceWorkerPath = "browser-extension/onebrain-chrome-lab/service_worker.js";
    private const string ContentScriptPath = "browser-extension/onebrain-chrome-lab/content_script.js";
    private const string RecipeCorePath = "browser-extension/onebrain-chrome-lab/recipe_core.js";
    private const string BridgeProgramPath = "src/OneBrain.ChromeLab.Bridge/Program.cs";

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
    public void ResultArtifactExists() => Assert.IsTrue(File.Exists(FullPath(ResultPath)), ResultPath);

    [TestMethod]
    public void SummaryArtifactExists() => Assert.IsTrue(File.Exists(FullPath(SummaryPath)), SummaryPath);

    [TestMethod]
    public void GoNoGoArtifactExists() => Assert.IsTrue(File.Exists(FullPath(GoNoGoPath)), GoNoGoPath);

    [TestMethod]
    public void ReportMarkdownExists() => Assert.IsTrue(File.Exists(FullPath(ReportPath)), ReportPath);

    [TestMethod]
    public void ResultIncludesBridgeLivenessScriptRun()
    {
        using var doc = ReadJson(ResultPath);
        doc.RootElement.GetProperty("bridgeLivenessScriptRun");
    }

    [TestMethod]
    public void ResultIncludesTcp1270018787()
    {
        using var doc = ReadJson(ResultPath);
        doc.RootElement.GetProperty("tcp1270018787");
    }

    [TestMethod]
    public void ResultIncludesHealthEndpoint()
    {
        using var doc = ReadJson(ResultPath);
        doc.RootElement.GetProperty("healthEndpoint");
    }

    [TestMethod]
    public void ResultIncludesRuntimeEndpoint()
    {
        using var doc = ReadJson(ResultPath);
        doc.RootElement.GetProperty("runtimeEndpoint");
    }

    [TestMethod]
    public void ResultIncludesDebugEndpoint()
    {
        using var doc = ReadJson(ResultPath);
        doc.RootElement.GetProperty("debugEndpoint");
    }

    [TestMethod]
    public void ResultIncludesConfigPublicEndpoint()
    {
        using var doc = ReadJson(ResultPath);
        doc.RootElement.GetProperty("configPublicEndpoint");
    }

    [TestMethod]
    public void ResultIncludesWsUpgradeEndpoint()
    {
        using var doc = ReadJson(ResultPath);
        doc.RootElement.GetProperty("wsUpgradeEndpoint");
    }

    [TestMethod]
    public void ResultIncludesExtensionReloaded()
    {
        using var doc = ReadJson(ResultPath);
        doc.RootElement.GetProperty("extensionReloaded");
    }

    [TestMethod]
    public void ResultIncludesSidepanelOpened()
    {
        using var doc = ReadJson(ResultPath);
        doc.RootElement.GetProperty("sidepanelOpened");
    }

    [TestMethod]
    public void ResultIncludesRuntimeTabRendered()
    {
        using var doc = ReadJson(ResultPath);
        doc.RootElement.GetProperty("runtimeTabRendered");
    }

    [TestMethod]
    public void ResultIncludesWebSocketReconnecting()
    {
        using var doc = ReadJson(ResultPath);
        doc.RootElement.GetProperty("webSocketReconnecting");
    }

    [TestMethod]
    public void ResultIncludesBridgeWebSocketErrorVisible()
    {
        using var doc = ReadJson(ResultPath);
        doc.RootElement.GetProperty("bridgeWebSocketErrorVisible");
    }

    [TestMethod]
    public void ResultIncludesErrConnectionRefusedObserved()
    {
        using var doc = ReadJson(ResultPath);
        doc.RootElement.GetProperty("errConnectionRefusedObserved");
    }

    [TestMethod]
    public void ResultIncludesCspViolations()
    {
        using var doc = ReadJson(ResultPath);
        doc.RootElement.GetProperty("cspViolations");
    }

    [TestMethod]
    public void ResultDeclaresProductFilesModifiedFalse()
    {
        using var doc = ReadJson(ResultPath);
        Assert.IsFalse(doc.RootElement.GetProperty("productFilesModified").GetBoolean());
    }

    [TestMethod]
    public void GoNoGoKeepsReadyForReleasePublicFalse()
    {
        using var doc = ReadJson(GoNoGoPath);
        Assert.IsFalse(doc.RootElement.GetProperty("readyForReleasePublic").GetBoolean());
    }

    [TestMethod]
    public void GoNoGoKeepsReadyForJsChangesFalse()
    {
        using var doc = ReadJson(GoNoGoPath);
        Assert.IsFalse(doc.RootElement.GetProperty("readyForJsChanges").GetBoolean());
    }

    [TestMethod]
    public void GoNoGoKeepsReadyForRuntimeChangesFalse()
    {
        using var doc = ReadJson(GoNoGoPath);
        Assert.IsFalse(doc.RootElement.GetProperty("readyForRuntimeChanges").GetBoolean());
    }

    [TestMethod]
    public void ManifestJsonUnchanged()
    {
        Assert.AreEqual("76859A171D0FBC585E96253D0F269AEF4A54DCFD5F704DE9DB92D26EB1AEDDFD", Sha256Hex(ManifestPath));
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
    public void BridgeProgramSourceUnchanged()
    {
        Assert.AreEqual("B9FB617E6B2FD1393EC4E1DD7B90AF91E44B26EBD4D70DA87416A5B46531E21E", Sha256Hex(BridgeProgramPath));
    }
}
