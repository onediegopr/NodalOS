using System.Security.Cryptography;
using System.Text.Json;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace OneBrain.Safety.Tests;

[TestClass]
[TestCategory("BridgeWebSocketFixReloadQa")]
[TestCategory("M637D")]
public sealed class NodalOsBridgeWebSocketFixReloadQaM637DTests
{
    private const string ResultPath = "artifacts/agent-operations/m637d/bridge-websocket-fix-reload-qa-result.json";
    private const string SummaryPath = "artifacts/agent-operations/m637d/bridge-websocket-fix-reload-qa-summary.json";
    private const string GoNoGoPath = "artifacts/agent-operations/m637d/post-m637d-go-no-go.json";
    private const string ReportPath = "docs/reports/bridge-websocket-fix-reload-qa-m637d.md";
    private const string ManifestPath = "browser-extension/onebrain-chrome-lab/manifest.json";
    private const string SidepanelHtmlPath = "browser-extension/onebrain-chrome-lab/sidepanel.html";
    private const string SidepanelCssPath = "browser-extension/onebrain-chrome-lab/sidepanel.css";
    private const string SidepanelJsPath = "browser-extension/onebrain-chrome-lab/sidepanel.js";
    private const string ServiceWorkerPath = "browser-extension/onebrain-chrome-lab/service_worker.js";
    private const string ContentScriptPath = "browser-extension/onebrain-chrome-lab/content_script.js";
    private const string RecipeCorePath = "browser-extension/onebrain-chrome-lab/recipe_core.js";

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
    public void ResultArtifactExists()
    {
        Assert.IsTrue(File.Exists(FullPath(ResultPath)), ResultPath);
    }

    [TestMethod]
    public void SummaryArtifactExists()
    {
        Assert.IsTrue(File.Exists(FullPath(SummaryPath)), SummaryPath);
    }

    [TestMethod]
    public void GoNoGoArtifactExists()
    {
        Assert.IsTrue(File.Exists(FullPath(GoNoGoPath)), GoNoGoPath);
    }

    [TestMethod]
    public void ReportMarkdownExists()
    {
        Assert.IsTrue(File.Exists(FullPath(ReportPath)), ReportPath);
    }

    [TestMethod]
    public void ResultIncludesBridgeRunningDuringTest()
    {
        using var doc = ReadJson(ResultPath);
        doc.RootElement.GetProperty("bridgeRunningDuringTest");
    }

    [TestMethod]
    public void ResultIncludesHealthOk()
    {
        using var doc = ReadJson(ResultPath);
        doc.RootElement.GetProperty("healthOk");
    }

    [TestMethod]
    public void ResultIncludesClientsObserved()
    {
        using var doc = ReadJson(ResultPath);
        doc.RootElement.GetProperty("clientsObserved");
    }

    [TestMethod]
    public void ResultIncludesHeartbeatOk()
    {
        using var doc = ReadJson(ResultPath);
        doc.RootElement.GetProperty("heartbeatOk");
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
    public void ResultIncludesInvalidTokenObserved()
    {
        using var doc = ReadJson(ResultPath);
        doc.RootElement.GetProperty("invalidTokenObserved");
    }

    [TestMethod]
    public void ResultIncludesPolicyViolation1008Observed()
    {
        using var doc = ReadJson(ResultPath);
        doc.RootElement.GetProperty("policyViolation1008Observed");
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
    public void GoNoGoKeepsReadyForReleaseEvidenceGateFalseBecauseQaFailed()
    {
        using var doc = ReadJson(GoNoGoPath);
        Assert.IsFalse(doc.RootElement.GetProperty("readyForReleaseEvidenceGate").GetBoolean());
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
        Assert.AreEqual("204D325980AE88619546B47F7D196FB66041C233B2DB040EBBD497AF337823D2", Sha256Hex(SidepanelJsPath));
    }

    [TestMethod]
    public void ServiceWorkerUnchangedFromM637C()
    {
        Assert.AreEqual("E42D5247C0A9CCAC250EB51300E6F6C1B701CADBA3DBD4B86A62126CC7A1933D", Sha256Hex(ServiceWorkerPath));
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
}
