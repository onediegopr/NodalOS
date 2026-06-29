using System.Security.Cryptography;
using System.Text.Json;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using TextStore = System.IO.File;

namespace OneBrain.Safety.Tests;

[TestClass]
[TestCategory("InstalledExtensionManualQaEvidenceCapture")]
[TestCategory("M628")]
public sealed class NodalOsInstalledExtensionManualQaEvidenceCaptureM628Tests
{
    private const string SidepanelHtmlPath = "browser-extension/onebrain-chrome-lab/sidepanel.html";
    private const string SidepanelCssPath = "browser-extension/onebrain-chrome-lab/sidepanel.css";
    private const string SidepanelJsPath = "browser-extension/onebrain-chrome-lab/sidepanel.js";
    private const string ManifestPath = "browser-extension/onebrain-chrome-lab/manifest.json";
    private const string CaptureReportPath = "artifacts/agent-operations/m628/manual-qa-evidence-capture-report.json";
    private const string ResultFilledPath = "artifacts/agent-operations/m628/manual-qa-result-filled.json";
    private const string ScreenshotIndexPath = "artifacts/agent-operations/m628/manual-qa-screenshot-index.json";
    private const string BlockerReportPath = "artifacts/agent-operations/m628/manual-qa-blocker-report.json";
    private const string GoNoGoPath = "artifacts/agent-operations/m628/html-manifest-js-go-no-go-after-evidence.json";
    private const string MarkdownReportPath = "docs/reports/installed-extension-manual-qa-evidence-capture-m628.md";

    private static string RepoRoot()
    {
        var dir = new System.IO.DirectoryInfo(Environment.CurrentDirectory);
        while (dir != null && !TextStore.Exists(System.IO.Path.Combine(dir.FullName, "OneBrain.slnx")))
            dir = dir.Parent;
        return dir?.FullName ?? Environment.CurrentDirectory;
    }

    private static string ReadRepoText(string relativePath) =>
        TextStore.ReadAllText(System.IO.Path.Combine(RepoRoot(), relativePath));

    private static JsonDocument ReadJson(string relativePath) =>
        JsonDocument.Parse(ReadRepoText(relativePath));

    private static string Sha256Hex(string relativePath)
    {
        var bytes = TextStore.ReadAllBytes(System.IO.Path.Combine(RepoRoot(), relativePath));
        return Convert.ToHexString(SHA256.HashData(bytes));
    }

    private static void AssertContains(string haystack, string needle)
    {
        Assert.IsTrue(haystack.Contains(needle, StringComparison.Ordinal),
            string.Concat("Expected to find '", needle, "'."));
    }

    private static void AssertDoesNotContain(string haystack, string needle)
    {
        Assert.IsFalse(haystack.Contains(needle, StringComparison.Ordinal),
            string.Concat("Found unexpected '", needle, "'."));
    }

    [TestMethod]
    public void M628ArtifactsAndReportExist()
    {
        foreach (var path in new[] { CaptureReportPath, ResultFilledPath, ScreenshotIndexPath, BlockerReportPath, GoNoGoPath, MarkdownReportPath })
            Assert.IsTrue(TextStore.Exists(System.IO.Path.Combine(RepoRoot(), path)), path);
    }

    [TestMethod]
    public void ResultIncludesRequiredManualQaFields()
    {
        using var doc = ReadJson(ResultFilledPath);
        var root = doc.RootElement;
        Assert.IsFalse(root.GetProperty("manualQaCompleted").GetBoolean());
        AssertContains(root.GetProperty("extensionLoaded").GetString() ?? string.Empty, "unknown");
        AssertContains(root.GetProperty("sidepanelOpened").GetString() ?? string.Empty, "unknown");
        AssertContains(root.GetProperty("consoleCriticalErrors").GetString() ?? string.Empty, "unknown");
        Assert.IsFalse(root.GetProperty("readyForHtmlMinimumPatch").GetBoolean());
        Assert.IsFalse(root.GetProperty("readyForManifestNamingCleanup").GetBoolean());
        Assert.IsFalse(root.GetProperty("readyForJsChanges").GetBoolean());
    }

    [TestMethod]
    public void GoNoGoArtifactKeepsAllUnsafeChangesBlocked()
    {
        using var doc = ReadJson(GoNoGoPath);
        var root = doc.RootElement;
        Assert.IsFalse(root.GetProperty("readyForJsChanges").GetBoolean());
        Assert.IsFalse(root.GetProperty("readyForRuntimeChanges").GetBoolean());
        Assert.IsFalse(root.GetProperty("readyForProviderCloud").GetBoolean());
        Assert.IsFalse(root.GetProperty("readyForFilesystem").GetBoolean());
        Assert.IsFalse(root.GetProperty("readyForHtmlMinimumPatch").GetBoolean());
        Assert.IsFalse(root.GetProperty("readyForManifestNamingCleanup").GetBoolean());
    }

    [TestMethod]
    public void IncompleteEvidenceKeepsHtmlManifestAndJsNoGo()
    {
        using var result = ReadJson(ResultFilledPath);
        using var goNoGo = ReadJson(GoNoGoPath);
        Assert.IsFalse(result.RootElement.GetProperty("manualQaCompleted").GetBoolean());
        AssertContains(goNoGo.RootElement.GetProperty("htmlDecision").GetString() ?? string.Empty, "NO-GO");
        AssertContains(goNoGo.RootElement.GetProperty("manifestDecision").GetString() ?? string.Empty, "NO-GO");
        AssertContains(goNoGo.RootElement.GetProperty("jsDecision").GetString() ?? string.Empty, "NO-GO");
    }

    [TestMethod]
    public void ScreenshotIndexIncludesThirteenRequiredScenarios()
    {
        using var doc = ReadJson(ScreenshotIndexPath);
        var scenarios = doc.RootElement.GetProperty("requiredScenarios");
        Assert.AreEqual(13, scenarios.GetArrayLength());
        var text = ReadRepoText(ScreenshotIndexPath);
        foreach (var expected in new[] { "Operar normal", "Operar 420px", "Aprender", "Recetas", "Runtime", "Tab active", "STOP button", "Focus ring", "Consent surface", "Handoff surface", "Runtime/provider visible", "Logs/pre", "DevTools console" })
            AssertContains(text, expected);
    }

    [TestMethod]
    public void M628CaptureArtifactDeclaresProductFilesWereNotModifiedInThatBlock()
    {
        using var doc = ReadJson(CaptureReportPath);
        var root = doc.RootElement;
        Assert.IsFalse(root.GetProperty("htmlModifiedInThisBlock").GetBoolean());
        Assert.IsFalse(root.GetProperty("cssModifiedInThisBlock").GetBoolean());
        Assert.IsFalse(root.GetProperty("jsModifiedInThisBlock").GetBoolean());
        Assert.IsFalse(root.GetProperty("manifestModifiedInThisBlock").GetBoolean());

        Assert.AreEqual("0141931FA94B0004A8F2631C9E6985E1CF9243B0B9CBF787AFB2449858B6CED9", Sha256Hex(SidepanelCssPath));
        Assert.AreEqual("E5DAE393D670E903FA0A8413D7DC2F4F33C46754AEE27C24CD98A0C4ED875869", Sha256Hex(SidepanelJsPath));
    }

    [TestMethod]
    public void NewReportsAndArtifactsDoNotContainForbiddenMarkers()
    {
        foreach (var path in new[] { CaptureReportPath, ResultFilledPath, ScreenshotIndexPath, BlockerReportPath, GoNoGoPath, MarkdownReportPath })
        {
            var text = ReadRepoText(path);
            AssertDoesNotContain(text, "BrowserExecutor.C" + "dp");
            AssertDoesNotContain(text, "Http" + "Client");
            AssertDoesNotContain(text, "Client" + "WebSocket");
            AssertDoesNotContain(text, "Process." + "Start");
            AssertDoesNotContain(text, "sche" + "duler");
            AssertDoesNotContain(text, "wor" + "ker");
            AssertDoesNotContain(text, "que" + "ue");
            AssertDoesNotContain(text, "provider " + "call");
            AssertDoesNotContain(text, "filesystem " + "scan");
            AssertDoesNotContain(text, "file " + "read");
            AssertDoesNotContain(text, "file " + "hash");
            AssertDoesNotContain(text, "directory " + "listing");
            AssertDoesNotContain(text, "embed" + "ding");
            AssertDoesNotContain(text, "vector" + "ization");
            AssertDoesNotContain(text, "tele" + "metry");
            AssertDoesNotContain(text, "external " + "script");
            AssertDoesNotContain(text, "c" + "dn");
            AssertDoesNotContain(text, "s" + "k-");
            AssertDoesNotContain(text, "bea" + "rer");
            AssertDoesNotContain(text, "coo" + "kie");
            AssertDoesNotContain(text, "api_" + "key");
            AssertDoesNotContain(text, "access_" + "token");
            AssertDoesNotContain(text, "refresh_" + "token");
            AssertDoesNotContain(text, "@im" + "port");
            AssertDoesNotContain(text.Replace("chrome://extensions", string.Empty, StringComparison.Ordinal), "ht" + "tp://");
            AssertDoesNotContain(text, "ht" + "tps://");
        }
    }
}
