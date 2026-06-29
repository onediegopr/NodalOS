using System.Security.Cryptography;
using System.Text.Json;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace OneBrain.Safety.Tests;

[TestClass]
[TestCategory("CspTighteningReadiness")]
[TestCategory("M636")]
public sealed class NodalOsCspTighteningReadinessM636Tests
{
    private const string InventoryPath = "artifacts/agent-operations/m636/csp-connect-src-inventory.json";
    private const string DecisionPath = "artifacts/agent-operations/m636/csp-tightening-readiness-decision.json";
    private const string CandidatePath = "artifacts/agent-operations/m636/csp-loopback-candidate.json";
    private const string GoNoGoPath = "artifacts/agent-operations/m636/post-csp-readiness-go-no-go.json";
    private const string ReportPath = "docs/reports/csp-tightening-readiness-m636.md";
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
    public void CspConnectSrcInventoryExists()
    {
        Assert.IsTrue(File.Exists(FullPath(InventoryPath)), InventoryPath);
    }

    [TestMethod]
    public void CspTighteningReadinessDecisionExists()
    {
        Assert.IsTrue(File.Exists(FullPath(DecisionPath)), DecisionPath);
    }

    [TestMethod]
    public void CspLoopbackCandidateExists()
    {
        Assert.IsTrue(File.Exists(FullPath(CandidatePath)), CandidatePath);
    }

    [TestMethod]
    public void PostCspReadinessGoNoGoExists()
    {
        Assert.IsTrue(File.Exists(FullPath(GoNoGoPath)), GoNoGoPath);
    }

    [TestMethod]
    public void ReportMarkdownExists()
    {
        Assert.IsTrue(File.Exists(FullPath(ReportPath)), ReportPath);
    }

    [TestMethod]
    public void InventoryIncludesCurrentConnectSrc()
    {
        using var doc = ReadJson(InventoryPath);
        Assert.AreEqual("'self' http://*:* ws://*:*", doc.RootElement.GetProperty("currentConnectSrc").GetString());
    }

    [TestMethod]
    public void InventoryDeclaresRequiredHttpLoopback()
    {
        using var doc = ReadJson(InventoryPath);
        Assert.IsTrue(doc.RootElement.GetProperty("requiredHttpLoopback").GetBoolean());
    }

    [TestMethod]
    public void InventoryDeclaresRequiredWsLoopback()
    {
        using var doc = ReadJson(InventoryPath);
        Assert.IsTrue(doc.RootElement.GetProperty("requiredWsLoopback").GetBoolean());
    }

    [TestMethod]
    public void InventoryDeclaresWildcardHttpNotRequired()
    {
        using var doc = ReadJson(InventoryPath);
        Assert.IsFalse(doc.RootElement.GetProperty("requiresWildcardHttp").GetBoolean());
    }

    [TestMethod]
    public void InventoryDeclaresWildcardWsNotRequired()
    {
        using var doc = ReadJson(InventoryPath);
        Assert.IsFalse(doc.RootElement.GetProperty("requiresWildcardWs").GetBoolean());
    }

    [TestMethod]
    public void InventoryDeclaresExternalDomainsNotRequired()
    {
        using var doc = ReadJson(InventoryPath);
        Assert.IsFalse(doc.RootElement.GetProperty("externalDomainsRequired").GetBoolean());
    }

    [TestMethod]
    public void CandidateAllowsLoopbackOnly()
    {
        using var doc = ReadJson(CandidatePath);
        Assert.IsTrue(doc.RootElement.GetProperty("allowsLoopbackOnly").GetBoolean());
    }

    [TestMethod]
    public void CandidateDisallowsWildcardRemoteHosts()
    {
        using var doc = ReadJson(CandidatePath);
        Assert.IsFalse(doc.RootElement.GetProperty("allowsWildcardRemoteHosts").GetBoolean());
    }

    [TestMethod]
    public void CandidateDisallowsProviderCloud()
    {
        using var doc = ReadJson(CandidatePath);
        Assert.IsFalse(doc.RootElement.GetProperty("allowsProviderCloud").GetBoolean());
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
    public void GoNoGoKeepsReadyForReleasePublicFalse()
    {
        using var doc = ReadJson(GoNoGoPath);
        Assert.IsFalse(doc.RootElement.GetProperty("readyForReleasePublic").GetBoolean());
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
    public void ManifestPermissionsUnchanged()
    {
        using var doc = ReadJson(ManifestPath);
        var root = doc.RootElement;
        CollectionAssert.AreEqual(
            new[] { "activeTab", "scripting", "storage", "tabs", "sidePanel", "alarms" },
            root.GetProperty("permissions").EnumerateArray().Select(x => x.GetString()).ToArray());
    }

    [TestMethod]
    public void ManifestHostPermissionsUnchanged()
    {
        using var doc = ReadJson(ManifestPath);
        var root = doc.RootElement;
        CollectionAssert.AreEqual(
            new[] { string.Concat("ht", "tp://*/*"), string.Concat("ht", "tps://*/*") },
            root.GetProperty("host_permissions").EnumerateArray().Select(x => x.GetString()).ToArray());
    }

    [TestMethod]
    public void ProtocolUnchanged()
    {
        AssertContains(ReadRepoText(ServiceWorkerPath), "PROTOCOL_VERSION = 'chrome-lab-v1'");
    }

    [TestMethod]
    public void StorageKeysUnchanged()
    {
        var serviceWorker = ReadRepoText(ServiceWorkerPath);
        AssertContains(serviceWorker, "nexaRecipes");
        AssertContains(serviceWorker, "nexaLearningDraft");
        AssertContains(serviceWorker, "nexaRuntimeState");
    }

    [TestMethod]
    public void PortAndAlarmNamesUnchanged()
    {
        var serviceWorker = ReadRepoText(ServiceWorkerPath);
        var sidepanelJs = ReadRepoText(SidepanelJsPath);
        AssertContains(serviceWorker, "nexa.keepalive");
        AssertContains(sidepanelJs, "onebrain-sidepanel");
    }

    [TestMethod]
    public void M636InventoryCapturedBroadCspBeforeM637Patch()
    {
        using var doc = ReadJson(InventoryPath);
        Assert.AreEqual("'self' http://*:* ws://*:*", doc.RootElement.GetProperty("currentConnectSrc").GetString());
    }
}
