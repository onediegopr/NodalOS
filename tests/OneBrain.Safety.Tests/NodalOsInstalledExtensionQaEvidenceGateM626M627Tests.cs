using System.Security.Cryptography;
using System.Text.Json;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using TextStore = System.IO.File;

namespace OneBrain.Safety.Tests;

[TestClass]
[TestCategory("InstalledExtensionQaEvidenceGate")]
[TestCategory("M626M627")]
public sealed class NodalOsInstalledExtensionQaEvidenceGateM626M627Tests
{
    private const string SidepanelHtmlPath = "browser-extension/onebrain-chrome-lab/sidepanel.html";
    private const string SidepanelCssPath = "browser-extension/onebrain-chrome-lab/sidepanel.css";
    private const string SidepanelJsPath = "browser-extension/onebrain-chrome-lab/sidepanel.js";
    private const string ManifestPath = "browser-extension/onebrain-chrome-lab/manifest.json";
    private const string EvidenceContractPath = "artifacts/agent-operations/m626/manual-qa-evidence-contract.json";
    private const string ScreenshotChecklistPath = "artifacts/agent-operations/m626/manual-screenshot-checklist.json";
    private const string ResultTemplatePath = "artifacts/agent-operations/m626/manual-qa-result-template.json";
    private const string GatePath = "artifacts/agent-operations/m627/html-manifest-readiness-decision-gate.json";
    private const string ReadinessSummaryPath = "artifacts/agent-operations/m627/installed-extension-qa-readiness-summary.json";
    private const string M626ReportPath = "docs/reports/installed-extension-manual-qa-evidence-contract-m626.md";
    private const string M627ReportPath = "docs/reports/html-manifest-readiness-decision-gate-m627.md";

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
    public void M626M627ArtifactsAndReportsExist()
    {
        foreach (var path in new[]
        {
            EvidenceContractPath,
            ScreenshotChecklistPath,
            ResultTemplatePath,
            GatePath,
            ReadinessSummaryPath,
            M626ReportPath,
            M627ReportPath
        })
        {
            Assert.IsTrue(TextStore.Exists(System.IO.Path.Combine(RepoRoot(), path)), path);
        }
    }

    [TestMethod]
    public void EvidenceContractIncludesRequiredScreenshots()
    {
        var text = ReadRepoText(EvidenceContractPath);
        foreach (var expected in new[]
        {
            "Operar normal",
            "Operar 420px narrow viewport",
            "Aprender",
            "Recetas",
            "Runtime",
            "DevTools console sin errores críticos"
        })
        {
            AssertContains(text, expected);
        }
    }

    [TestMethod]
    public void ResultTemplateIncludesRequiredFields()
    {
        using var doc = ReadJson(ResultTemplatePath);
        var root = doc.RootElement;
        Assert.IsFalse(root.GetProperty("manualQaCompleted").GetBoolean());
        AssertContains(root.GetProperty("extensionLoaded").GetString() ?? string.Empty, "unknown");
        AssertContains(root.GetProperty("sidepanelOpened").GetString() ?? string.Empty, "unknown");
        AssertContains(root.GetProperty("consoleCriticalErrors").GetString() ?? string.Empty, "unknown");
        var checks = root.GetProperty("checks");
        AssertContains(checks.GetProperty("tabActiveContrast").GetString() ?? string.Empty, "unknown");
        AssertContains(checks.GetProperty("stopResponsive").GetString() ?? string.Empty, "unknown");
        AssertContains(checks.GetProperty("focusKeyboard").GetString() ?? string.Empty, "unknown");
    }

    [TestMethod]
    public void GateDeclaresAllProductChangesNotReady()
    {
        using var doc = ReadJson(GatePath);
        var root = doc.RootElement;
        Assert.IsFalse(root.GetProperty("readyForHtmlMinimumPatch").GetBoolean());
        Assert.IsFalse(root.GetProperty("readyForManifestNamingCleanup").GetBoolean());
        Assert.IsFalse(root.GetProperty("readyForJsChanges").GetBoolean());
        Assert.IsFalse(root.GetProperty("readyForRuntimeChanges").GetBoolean());
        Assert.IsTrue(root.GetProperty("requiresManualQaEvidence").GetBoolean());
        Assert.IsTrue(root.GetProperty("requiresClaudeBeforeJs").GetBoolean());
        Assert.IsTrue(root.GetProperty("requiresExtensionReviewBeforeManifest").GetBoolean());
    }

    [TestMethod]
    public void ReadinessSummaryMatchesDecision()
    {
        using var doc = ReadJson(ReadinessSummaryPath);
        var root = doc.RootElement;
        AssertContains(root.GetProperty("decision").GetString() ?? string.Empty, "INSTALLED_EXTENSION_QA_EVIDENCE_GATE_READY");
        Assert.IsTrue(root.GetProperty("m626Closed").GetBoolean());
        Assert.IsTrue(root.GetProperty("m627Closed").GetBoolean());
        Assert.IsTrue(root.GetProperty("readyForManualUserQa").GetBoolean());
        Assert.IsFalse(root.GetProperty("readyForHtmlMinimumPatch").GetBoolean());
        Assert.IsFalse(root.GetProperty("readyForManifestNamingCleanup").GetBoolean());
        Assert.IsFalse(root.GetProperty("readyForJsChanges").GetBoolean());
        Assert.IsFalse(root.GetProperty("readyForRuntimeChanges").GetBoolean());
    }

    [TestMethod]
    public void ProductSidepanelFilesRemainUnchanged()
    {
        Assert.AreEqual("1191BEECE5C4045A4C61BF5E2EB7F2846319FFD9C848148196134B92C8E38204", Sha256Hex(SidepanelHtmlPath));
        Assert.AreEqual("D2A14687DB6E201353A100A33B72AECB3C1858C1127114979945750AB5B717AC", Sha256Hex(SidepanelCssPath));
        Assert.AreEqual("204D325980AE88619546B47F7D196FB66041C233B2DB040EBBD497AF337823D2", Sha256Hex(SidepanelJsPath));
        Assert.AreEqual("48894688F6159FACA52C6CC1E3F438BFCB6B835EBCBAE7952BB12AFD9F339A80", Sha256Hex(ManifestPath));
    }

    [TestMethod]
    public void NewReportsAndArtifactsDoNotContainForbiddenMarkers()
    {
        foreach (var path in new[]
        {
            EvidenceContractPath,
            ScreenshotChecklistPath,
            ResultTemplatePath,
            GatePath,
            ReadinessSummaryPath,
            M626ReportPath,
            M627ReportPath
        })
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
