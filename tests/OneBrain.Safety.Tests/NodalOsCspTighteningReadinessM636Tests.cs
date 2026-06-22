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
        Assert.AreEqual("298BEE3E6AAE130369CDDCF63476E7B8356842205788FECF1666E96D58AB95D8", Sha256Hex(ManifestPath));
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
    public void ServiceWorkerUnchanged()
    {
        Assert.AreEqual("5C98C0B1481ACEAA4EE957CF38C80E5BADA592DF469F077C277D1EA7658EC444", Sha256Hex(ServiceWorkerPath));
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
    public void CurrentManifestStillHasBroadCspBecauseM636IsReadinessOnly()
    {
        using var doc = ReadJson(ManifestPath);
        var csp = doc.RootElement.GetProperty("content_security_policy").GetProperty("extension_pages").GetString();
        AssertContains(csp!, "connect-src 'self' http://*:* ws://*:*;");
    }
}
