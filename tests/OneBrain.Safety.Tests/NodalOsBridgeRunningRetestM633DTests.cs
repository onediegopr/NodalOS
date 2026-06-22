using System.Security.Cryptography;
using System.Text.Json;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace OneBrain.Safety.Tests;

[TestClass]
[TestCategory("BridgeRunningRetest")]
[TestCategory("M633D")]
public sealed class NodalOsBridgeRunningRetestM633DTests
{
    private const string ResultPath = "artifacts/agent-operations/m633d/bridge-running-retest-result.json";
    private const string ConsoleEvidencePath = "artifacts/agent-operations/m633d/bridge-running-console-evidence.json";
    private const string GoNoGoPath = "artifacts/agent-operations/m633d/post-bridge-running-go-no-go.json";
    private const string ReportPath = "docs/reports/bridge-running-retest-m633d.md";
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

    private static void AssertContains(string haystack, string needle) =>
        Assert.IsTrue(haystack.Contains(needle, StringComparison.Ordinal), $"Expected to find '{needle}'.");

    [TestMethod]
    public void ResultArtifactExists()
    {
        Assert.IsTrue(File.Exists(FullPath(ResultPath)), ResultPath);
    }

    [TestMethod]
    public void ConsoleEvidenceArtifactExists()
    {
        Assert.IsTrue(File.Exists(FullPath(ConsoleEvidencePath)), ConsoleEvidencePath);
    }

    [TestMethod]
    public void GoNoGoArtifactExists()
    {
        Assert.IsTrue(File.Exists(FullPath(GoNoGoPath)), GoNoGoPath);
    }

    [TestMethod]
    public void ReportExists()
    {
        Assert.IsTrue(File.Exists(FullPath(ReportPath)), ReportPath);
    }

    [TestMethod]
    public void ResultIncludesBridgeRunningDuringTest()
    {
        using var doc = ReadJson(ResultPath);
        Assert.AreEqual("pass", doc.RootElement.GetProperty("bridgeRunningDuringTest").GetString());
    }

    [TestMethod]
    public void ResultIncludesErrConnectionRefusedPresent()
    {
        using var doc = ReadJson(ResultPath);
        Assert.AreEqual("pass", doc.RootElement.GetProperty("errConnectionRefusedPresent").GetString());
    }

    [TestMethod]
    public void ResultIncludesConsoleCriticalErrors()
    {
        using var doc = ReadJson(ResultPath);
        doc.RootElement.GetProperty("consoleCriticalErrors");
    }

    [TestMethod]
    public void ResultIncludesCspViolations()
    {
        using var doc = ReadJson(ResultPath);
        doc.RootElement.GetProperty("cspViolations");
    }

    [TestMethod]
    public void ResultIncludesSidepanelRenderImpactedFalse()
    {
        using var doc = ReadJson(ResultPath);
        Assert.IsFalse(doc.RootElement.GetProperty("sidepanelRenderImpacted").GetBoolean());
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
        Assert.AreEqual("298BEE3E6AAE130369CDDCF63476E7B8356842205788FECF1666E96D58AB95D8", Sha256Hex(ManifestPath));
    }

    [TestMethod]
    public void SidepanelHtmlUnchanged()
    {
        Assert.AreEqual("014A6C5DCCA2787E61E01D8B2DCED4A20F32D9F41BB076B7C2A5E37EE746F71E", Sha256Hex(SidepanelHtmlPath));
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
    public void ServiceWorkerUnchanged()
    {
        Assert.AreEqual("F479C04C342E922EA23928B9E2857FEFDDF69792959E33A31D3A7D3A28534CEC", Sha256Hex(ServiceWorkerPath));
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
    public void ManifestPermissionsUnchanged()
    {
        using var doc = ReadJson(ManifestPath);
        var root = doc.RootElement;
        CollectionAssert.AreEqual(
            new[] { "activeTab", "scripting", "storage", "tabs", "sidePanel", "alarms" },
            root.GetProperty("permissions").EnumerateArray().Select(x => x.GetString()).ToArray());
        CollectionAssert.AreEqual(
            new[] { string.Concat("ht", "tp://*/*"), string.Concat("ht", "tps://*/*") },
            root.GetProperty("host_permissions").EnumerateArray().Select(x => x.GetString()).ToArray());
    }

    [TestMethod]
    public void ProtocolStorageAndPortMarkersUnchanged()
    {
        var serviceWorker = ReadRepoText(ServiceWorkerPath);
        var sidepanelJs = ReadRepoText(SidepanelJsPath);
        AssertContains(serviceWorker, "PROTOCOL_VERSION = 'chrome-lab-v1'");
        AssertContains(serviceWorker, "nexaRecipes");
        AssertContains(serviceWorker, "nexaLearningDraft");
        AssertContains(serviceWorker, "nexaRuntimeState");
        AssertContains(serviceWorker, "nexa.keepalive");
        AssertContains(serviceWorker, "nexa.content.ping");
        AssertContains(sidepanelJs, "onebrain-sidepanel");
    }
}

