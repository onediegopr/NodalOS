using System.Text.Json;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace OneBrain.Safety.Tests;

[TestClass]
[TestCategory("NativeBrowserSkillsProduct")]
[TestCategory("M1269B")]
[TestCategory("M1270B")]
[TestCategory("M1271B")]
[TestCategory("M1272B")]
[TestCategory("M1273B")]
[TestCategory("M1274B")]
[TestCategory("M1275B")]
[TestCategory("M1276B")]
[TestCategory("M1277B")]
[TestCategory("M1278B")]
[TestCategory("M1279B")]
[TestCategory("M1280B")]
[TestCategory("M1281B")]
[TestCategory("M1282B")]
[TestCategory("M1283B")]
[TestCategory("M1284B")]
[TestCategory("M1285B")]
[TestCategory("M1286B")]
[TestCategory("M1287B")]
[TestCategory("M1288B")]
public sealed class NativeBrowserSkillsProductM1269BTests
{
    private const string SidepanelHtmlPath = "browser-extension/onebrain-chrome-lab/sidepanel.html";
    private const string SidepanelJsPath = "browser-extension/onebrain-chrome-lab/sidepanel.js";
    private const string ManifestPath = "browser-extension/onebrain-chrome-lab/manifest.json";

    [TestMethod]
    public void BrowserSkillsSurfaceIsVisibleAndUsableFromMissionControl()
    {
        var html = ReadRepoText(SidepanelHtmlPath);
        var section = ExtractBrowserSkillsSection(html);

        StringAssert.Contains(section, "Browser Skills");
        StringAssert.Contains(section, "Capturar pestaña");
        StringAssert.Contains(section, "Indexar página");
        StringAssert.Contains(section, "Copiar resumen Browser Skill");
        StringAssert.Contains(section, "browserIndexedElements");
        StringAssert.Contains(section, "browserEvidencePanel");
        StringAssert.Contains(section, "browserSnapshotHistory");
        StringAssert.Contains(section, "Captcha:");
        StringAssert.Contains(section, "referencia externa no usada en runtime");

        foreach (var forbidden in new[] { "planned", "future", "reference only", "NO-GO", "blocked by policy", "claim guard", "gate" })
        {
            Assert.IsFalse(section.Contains(forbidden, StringComparison.OrdinalIgnoreCase), forbidden);
        }
    }

    [TestMethod]
    public void BrowserSkillsJavascriptImplementsCaptureIndexEvidenceHistoryAndCopy()
    {
        var js = ReadRepoText(SidepanelJsPath);

        foreach (var expected in new[]
        {
            "BROWSER_SKILLS_SNAPSHOT_KEY",
            "nodal-os.browserSkills.snapshots.v1",
            "captureBrowserActiveTab",
            "indexBrowserActivePage",
            "readActiveBrowserTab",
            "executeBrowserPageIndex",
            "collectBrowserSkillPageState",
            "renderBrowserSkills",
            "copyBrowserSkillSummary",
            "clearBrowserSnapshotHistory",
            "captcha_visible",
            "login_required",
            "access_restricted",
            "empty_or_error",
            "session_expired"
        })
        {
            StringAssert.Contains(js, expected);
        }
    }

    [TestMethod]
    public void BrowserSkillsDoesNotInventCaptureWhenExtensionApisAreUnavailable()
    {
        var js = ReadRepoText(SidepanelJsPath);

        StringAssert.Contains(js, "NOT_IMPLEMENTED_BLOCKED_BY_CURRENT_EXTENSION_CAPABILITIES");
        StringAssert.Contains(js, "No pude leer la pestaña desde este contexto.");
        StringAssert.Contains(js, "chrome.tabs.query no está disponible fuera del sidepanel instalado.");
        StringAssert.Contains(js, "chrome.scripting.executeScript no está disponible en este contexto.");
    }

    [TestMethod]
    public void BrowserSkillsMissionIntegrationKeepsProductDemoHooks()
    {
        var html = ReadRepoText(SidepanelHtmlPath);
        var js = ReadRepoText(SidepanelJsPath);

        StringAssert.Contains(html, "runSafeDemoBtn");
        StringAssert.Contains(html, "demoTimeline");
        StringAssert.Contains(html, "demoEvidencePanel");
        StringAssert.Contains(js, "attachBrowserSkillSnapshotToMission");
        StringAssert.Contains(js, "browserSkillSnapshots");
        StringAssert.Contains(js, "Browser Skill capturado");
        StringAssert.Contains(js, "browser-skill:");
        StringAssert.Contains(js, "saveDemoStore()");
    }

    [TestMethod]
    public void ManifestExposesRequiredNativeBrowserCapabilities()
    {
        using var manifest = JsonDocument.Parse(ReadRepoText(ManifestPath));
        var root = manifest.RootElement;
        var permissions = root.GetProperty("permissions").EnumerateArray().Select(item => item.GetString()).ToArray();

        CollectionAssert.Contains(permissions, "activeTab");
        CollectionAssert.Contains(permissions, "tabs");
        CollectionAssert.Contains(permissions, "scripting");
        CollectionAssert.Contains(permissions, "sidePanel");
        Assert.IsTrue(root.TryGetProperty("host_permissions", out var hostPermissions));
        Assert.IsTrue(hostPermissions.EnumerateArray().Any(item => item.GetString() == "http://*/*"));
        Assert.IsTrue(hostPermissions.EnumerateArray().Any(item => item.GetString() == "https://*/*"));
        Assert.IsTrue(root.TryGetProperty("content_scripts", out _));
    }

    [TestMethod]
    public void BrowserActDependencyIsNotAdded()
    {
        var repo = RepoRoot();
        var projectAndPackageFiles = Directory
            .EnumerateFiles(repo, "*.*", SearchOption.AllDirectories)
            .Where(path =>
                path.EndsWith(".csproj", StringComparison.OrdinalIgnoreCase) ||
                Path.GetFileName(path).Equals("package.json", StringComparison.OrdinalIgnoreCase) ||
                Path.GetFileName(path).Equals("package-lock.json", StringComparison.OrdinalIgnoreCase) ||
                Path.GetFileName(path).Equals("pnpm-lock.yaml", StringComparison.OrdinalIgnoreCase) ||
                Path.GetFileName(path).Equals("yarn.lock", StringComparison.OrdinalIgnoreCase))
            .Where(path => !path.Contains($"{Path.DirectorySeparatorChar}.git{Path.DirectorySeparatorChar}", StringComparison.OrdinalIgnoreCase))
            .ToArray();

        foreach (var path in projectAndPackageFiles)
        {
            var text = File.ReadAllText(path);
            Assert.IsFalse(text.Contains("BrowserAct", StringComparison.OrdinalIgnoreCase), path);
            Assert.IsFalse(text.Contains("browser-act", StringComparison.OrdinalIgnoreCase), path);
        }
    }

    private static string ExtractBrowserSkillsSection(string html)
    {
        const string startMarker = "id=\"browserSkillsWorkspace\"";
        const string endMarker = "<section id=\"recipeLabSurface\"";
        var start = html.IndexOf(startMarker, StringComparison.Ordinal);
        var end = html.IndexOf(endMarker, start >= 0 ? start : 0, StringComparison.Ordinal);
        Assert.IsTrue(start >= 0, "Browser Skills workspace is missing.");
        Assert.IsTrue(end > start, "Browser Skills section end marker is missing.");
        return html[start..end];
    }

    private static string ReadRepoText(string relativePath) =>
        File.ReadAllText(Path.Combine(RepoRoot(), relativePath));

    private static string RepoRoot()
    {
        var dir = new DirectoryInfo(Environment.CurrentDirectory);
        while (dir != null && !File.Exists(Path.Combine(dir.FullName, "OneBrain.slnx")))
        {
            dir = dir.Parent;
        }

        return dir?.FullName ?? Environment.CurrentDirectory;
    }
}
