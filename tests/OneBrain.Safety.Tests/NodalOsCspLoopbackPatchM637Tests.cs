using System.Security.Cryptography;
using System.Text.Json;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace OneBrain.Safety.Tests;

[TestClass]
[TestCategory("CspLoopbackPatch")]
[TestCategory("M637")]
public sealed class NodalOsCspLoopbackPatchM637Tests
{
    private const string SummaryPath = "artifacts/agent-operations/m637/csp-loopback-patch-summary.json";
    private const string BeforeAfterPath = "artifacts/agent-operations/m637/csp-before-after.json";
    private const string ManifestBoundaryPath = "artifacts/agent-operations/m637/manifest-boundary-confirmation.json";
    private const string GoNoGoPath = "artifacts/agent-operations/m637/post-csp-loopback-go-no-go.json";
    private const string ReportPath = "docs/reports/csp-loopback-patch-m637.md";
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

    private static string ManifestCsp()
    {
        using var doc = ReadJson(ManifestPath);
        return doc.RootElement.GetProperty("content_security_policy").GetProperty("extension_pages").GetString()!;
    }

    private static void AssertContains(string haystack, string needle) =>
        Assert.IsTrue(haystack.Contains(needle, StringComparison.Ordinal), $"Expected to find '{needle}'.");

    private static void AssertDoesNotContain(string haystack, string needle) =>
        Assert.IsFalse(haystack.Contains(needle, StringComparison.Ordinal), $"Found unexpected '{needle}'.");

    [TestMethod]
    public void SummaryArtifactExists()
    {
        Assert.IsTrue(File.Exists(FullPath(SummaryPath)), SummaryPath);
    }

    [TestMethod]
    public void BeforeAfterArtifactExists()
    {
        Assert.IsTrue(File.Exists(FullPath(BeforeAfterPath)), BeforeAfterPath);
    }

    [TestMethod]
    public void ManifestBoundaryArtifactExists()
    {
        Assert.IsTrue(File.Exists(FullPath(ManifestBoundaryPath)), ManifestBoundaryPath);
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
    public void ManifestConnectSrcNoLongerContainsWildcardHttp()
    {
        AssertDoesNotContain(ManifestCsp(), "http://*:*");
    }

    [TestMethod]
    public void ManifestConnectSrcNoLongerContainsWildcardWs()
    {
        AssertDoesNotContain(ManifestCsp(), "ws://*:*");
    }

    [TestMethod]
    public void ManifestConnectSrcContainsHttp127Loopback()
    {
        AssertContains(ManifestCsp(), "http://127.0.0.1:*");
    }

    [TestMethod]
    public void ManifestConnectSrcContainsWs127Loopback()
    {
        AssertContains(ManifestCsp(), "ws://127.0.0.1:*");
    }

    [TestMethod]
    public void ManifestConnectSrcContainsHttpLocalhost()
    {
        AssertContains(ManifestCsp(), "http://localhost:*");
    }

    [TestMethod]
    public void ManifestConnectSrcContainsWsLocalhost()
    {
        AssertContains(ManifestCsp(), "ws://localhost:*");
    }

    [TestMethod]
    public void SummaryDeclaresManifestOnly()
    {
        using var doc = ReadJson(SummaryPath);
        Assert.IsTrue(doc.RootElement.GetProperty("manifestOnly").GetBoolean());
    }

    [TestMethod]
    public void SummaryDeclaresCspOnly()
    {
        using var doc = ReadJson(SummaryPath);
        Assert.IsTrue(doc.RootElement.GetProperty("cspOnly").GetBoolean());
    }

    [TestMethod]
    public void SummaryDeclaresConnectSrcRestrictedToLoopback()
    {
        using var doc = ReadJson(SummaryPath);
        Assert.IsTrue(doc.RootElement.GetProperty("connectSrcRestrictedToLoopback").GetBoolean());
    }

    [TestMethod]
    public void SummaryDeclaresPermissionsUnchanged()
    {
        using var doc = ReadJson(SummaryPath);
        Assert.IsFalse(doc.RootElement.GetProperty("permissionsChanged").GetBoolean());
    }

    [TestMethod]
    public void SummaryDeclaresHostPermissionsUnchanged()
    {
        using var doc = ReadJson(SummaryPath);
        Assert.IsFalse(doc.RootElement.GetProperty("hostPermissionsChanged").GetBoolean());
    }

    [TestMethod]
    public void SummaryDeclaresJsUnchanged()
    {
        using var doc = ReadJson(SummaryPath);
        Assert.IsFalse(doc.RootElement.GetProperty("jsChanged").GetBoolean());
    }

    [TestMethod]
    public void SummaryDeclaresRuntimeUnchanged()
    {
        using var doc = ReadJson(SummaryPath);
        Assert.IsFalse(doc.RootElement.GetProperty("runtimeChanged").GetBoolean());
    }

    [TestMethod]
    public void BoundaryDeclaresPermissionsUnchanged()
    {
        using var doc = ReadJson(ManifestBoundaryPath);
        Assert.IsFalse(doc.RootElement.GetProperty("permissionsChanged").GetBoolean());
    }

    [TestMethod]
    public void BoundaryDeclaresHostPermissionsUnchanged()
    {
        using var doc = ReadJson(ManifestBoundaryPath);
        Assert.IsFalse(doc.RootElement.GetProperty("hostPermissionsChanged").GetBoolean());
    }

    [TestMethod]
    public void BoundaryDeclaresBackgroundUnchanged()
    {
        using var doc = ReadJson(ManifestBoundaryPath);
        Assert.IsFalse(doc.RootElement.GetProperty("backgroundChanged").GetBoolean());
    }

    [TestMethod]
    public void BoundaryDeclaresSidePanelUnchanged()
    {
        using var doc = ReadJson(ManifestBoundaryPath);
        Assert.IsFalse(doc.RootElement.GetProperty("sidePanelChanged").GetBoolean());
    }

    [TestMethod]
    public void BoundaryDeclaresActionUnchanged()
    {
        using var doc = ReadJson(ManifestBoundaryPath);
        Assert.IsFalse(doc.RootElement.GetProperty("actionChanged").GetBoolean());
    }

    [TestMethod]
    public void BoundaryDeclaresContentScriptsUnchanged()
    {
        using var doc = ReadJson(ManifestBoundaryPath);
        Assert.IsFalse(doc.RootElement.GetProperty("contentScriptsChanged").GetBoolean());
    }

    [TestMethod]
    public void ManifestHasM637Baseline()
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
}
