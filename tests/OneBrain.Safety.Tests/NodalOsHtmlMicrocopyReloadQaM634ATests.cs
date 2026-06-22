using System.Security.Cryptography;
using System.Text.Json;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace OneBrain.Safety.Tests;

[TestClass]
[TestCategory("HtmlMicrocopyReloadQa")]
[TestCategory("M634A")]
public sealed class NodalOsHtmlMicrocopyReloadQaM634ATests
{
    private const string ResultPath = "artifacts/agent-operations/m634a/html-microcopy-reload-qa-result.json";
    private const string SummaryPath = "artifacts/agent-operations/m634a/html-microcopy-reload-qa-summary.json";
    private const string GoNoGoPath = "artifacts/agent-operations/m634a/post-m634a-go-no-go.json";
    private const string ReportPath = "docs/reports/html-microcopy-reload-qa-m634a.md";
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
    public void ResultIncludesOperateTabRendered()
    {
        using var doc = ReadJson(ResultPath);
        doc.RootElement.GetProperty("operateTabRendered");
    }

    [TestMethod]
    public void ResultIncludesRuntimeTabRendered()
    {
        using var doc = ReadJson(ResultPath);
        doc.RootElement.GetProperty("runtimeTabRendered");
    }

    [TestMethod]
    public void ResultIncludesTargetResolutionTranslated()
    {
        using var doc = ReadJson(ResultPath);
        doc.RootElement.GetProperty("targetResolutionTranslated");
    }

    [TestMethod]
    public void ResultIncludesVerificationTranslated()
    {
        using var doc = ReadJson(ResultPath);
        doc.RootElement.GetProperty("verificationTranslated");
    }

    [TestMethod]
    public void ResultIncludesMojibakeVisible()
    {
        using var doc = ReadJson(ResultPath);
        doc.RootElement.GetProperty("mojibakeVisible");
    }

    [TestMethod]
    public void ResultIncludesChromeExtensionNameShowsNodalOs()
    {
        using var doc = ReadJson(ResultPath);
        doc.RootElement.GetProperty("chromeExtensionNameShowsNodalOs");
    }

    [TestMethod]
    public void ResultIncludesVisibleNexaTextRemaining()
    {
        using var doc = ReadJson(ResultPath);
        doc.RootElement.GetProperty("visibleNexaTextRemaining");
    }

    [TestMethod]
    public void ResultDeclaresProductFilesModifiedFalse()
    {
        using var doc = ReadJson(ResultPath);
        Assert.IsFalse(doc.RootElement.GetProperty("productFilesModified").GetBoolean());
    }

    [TestMethod]
    public void GoNoGoReadyForSwVisibleStringsCleanupTrue()
    {
        using var doc = ReadJson(GoNoGoPath);
        Assert.IsTrue(doc.RootElement.GetProperty("readyForSwVisibleStringsCleanup").GetBoolean());
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
    public void SidepanelHtmlUnchangedFromM634()
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
        Assert.AreEqual("546AAF381024B5F784F28A94A57F05948AECA92F6BFF174F577D22B4120A655F", Sha256Hex(ServiceWorkerPath));
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
