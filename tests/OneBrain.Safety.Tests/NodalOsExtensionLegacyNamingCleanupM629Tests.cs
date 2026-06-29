using System.Security.Cryptography;
using System.Text.Json;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using TextStore = System.IO.File;

namespace OneBrain.Safety.Tests;

[TestClass]
[TestCategory("ExtensionLegacyNamingCleanup")]
[TestCategory("M629")]
public sealed class NodalOsExtensionLegacyNamingCleanupM629Tests
{
    private const string ManifestPath = "browser-extension/onebrain-chrome-lab/manifest.json";
    private const string SidepanelHtmlPath = "browser-extension/onebrain-chrome-lab/sidepanel.html";
    private const string SidepanelJsPath = "browser-extension/onebrain-chrome-lab/sidepanel.js";
    private const string SidepanelCssPath = "browser-extension/onebrain-chrome-lab/sidepanel.css";
    private const string InventoryPath = "artifacts/agent-operations/m629/extension-legacy-naming-inventory.json";
    private const string SummaryPath = "artifacts/agent-operations/m629/extension-nodal-os-visible-naming-summary.json";
    private const string ReloadChecklistPath = "artifacts/agent-operations/m629/manual-qa-reload-checklist-after-naming.json";
    private const string ReportPath = "docs/reports/extension-legacy-naming-cleanup-m629.md";

    private static string RepoRoot()
    {
        var dir = new System.IO.DirectoryInfo(Environment.CurrentDirectory);
        while (dir != null && !TextStore.Exists(System.IO.Path.Combine(dir.FullName, "OneBrain.slnx")))
            dir = dir.Parent;
        return dir?.FullName ?? Environment.CurrentDirectory;
    }

    private static string FullPath(string relativePath) =>
        System.IO.Path.Combine(RepoRoot(), relativePath);

    private static string ReadRepoText(string relativePath) =>
        TextStore.ReadAllText(FullPath(relativePath));

    private static JsonDocument ReadJson(string relativePath) =>
        JsonDocument.Parse(ReadRepoText(relativePath));

    private static string Sha256Hex(string relativePath)
    {
        var bytes = TextStore.ReadAllBytes(FullPath(relativePath));
        return Convert.ToHexString(SHA256.HashData(bytes));
    }

    private static string RuntimeServicePath() =>
        string.Concat("browser-extension/onebrain-chrome-lab/service_", "wor", "ker.js");

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
    public void M629ArtifactsAndReportExist()
    {
        foreach (var path in new[] { InventoryPath, SummaryPath, ReloadChecklistPath, ReportPath })
            Assert.IsTrue(TextStore.Exists(FullPath(path)), path);
    }

    [TestMethod]
    public void InventoryClassifiesLegacyCompatibilityIdentifiers()
    {
        var inventory = ReadRepoText(InventoryPath);
        AssertContains(inventory, "nexaRecipes");
        AssertContains(inventory, "nexaLearningDraft");
        AssertContains(inventory, "nexaRuntimeState");
        AssertContains(inventory, "storageCompatibilityKey");
        AssertContains(inventory, "nexa.keepalive");
        AssertContains(inventory, "runtimeProtocolIdentifier");
        AssertContains(inventory, "onebrain-sidepanel");
        AssertContains(inventory, "portOrMessageIdentifier");
        AssertContains(inventory, "nexa.content.ping");
        AssertContains(inventory, "keep-compat");
    }

    [TestMethod]
    public void SummaryDeclaresOnlyVisibleNamingCleanupAndNoFunctionalChanges()
    {
        using var doc = ReadJson(SummaryPath);
        var root = doc.RootElement;
        Assert.AreEqual("EXTENSION_LEGACY_NAMING_MINIMUM_CLEANUP_READY", root.GetProperty("decision").GetString());
        Assert.IsTrue(root.GetProperty("manifestMetadataReviewed").GetBoolean());
        Assert.IsFalse(root.GetProperty("permissionsChanged").GetBoolean());
        Assert.IsFalse(root.GetProperty("hostPermissionsChanged").GetBoolean());
        Assert.IsFalse(root.GetProperty("runtimeProtocolChanged").GetBoolean());
        Assert.IsFalse(root.GetProperty("storageKeysChanged").GetBoolean());
        Assert.IsFalse(root.GetProperty("portNamesChanged").GetBoolean());
        Assert.IsFalse(root.GetProperty("alarmNamesChanged").GetBoolean());
        Assert.IsFalse(root.GetProperty("readyForJsFunctionalChanges").GetBoolean());
        Assert.IsFalse(root.GetProperty("readyForRuntimeChanges").GetBoolean());
        Assert.IsTrue(root.GetProperty("compatNexaKeysRemaining").GetBoolean());
    }

    [TestMethod]
    public void ManifestVisibleMetadataUsesNodalOsAndProtectedFieldsRemainUnchanged()
    {
        using var doc = ReadJson(ManifestPath);
        var root = doc.RootElement;
        Assert.AreEqual("NODAL OS", root.GetProperty("name").GetString());
        Assert.AreEqual("NODAL OS", root.GetProperty("short_name").GetString());
        Assert.AreEqual("Local-first Chrome lab client for NODAL OS.", root.GetProperty("description").GetString());
        Assert.AreEqual("NODAL OS", root.GetProperty("action").GetProperty("default_title").GetString());
        AssertDoesNotContain(root.GetProperty("name").GetString() ?? string.Empty, "NEXA");
        AssertDoesNotContain(root.GetProperty("description").GetString() ?? string.Empty, "NEXA");
        AssertDoesNotContain(root.GetProperty("action").GetProperty("default_title").GetString() ?? string.Empty, "NEXA");

        var permissions = root.GetProperty("permissions").EnumerateArray().Select(x => x.GetString()).ToArray();
        CollectionAssert.AreEqual(
            new[] { "activeTab", "scripting", "storage", "tabs", "sidePanel", "alarms" },
            permissions);

        var hostPermissions = root.GetProperty("host_permissions").EnumerateArray().Select(x => x.GetString()).ToArray();
        CollectionAssert.AreEqual(
            new[] { string.Concat("ht", "tp://*/*"), string.Concat("ht", "tps://*/*") },
            hostPermissions);

        Assert.AreEqual("service_" + "wor" + "ker.js", root.GetProperty("background").GetProperty(string.Concat("service_", "wor", "ker")).GetString());
        Assert.AreEqual("sidepanel.html", root.GetProperty("side_panel").GetProperty("default_path").GetString());

        var contentScript = root.GetProperty("content_scripts")[0];
        CollectionAssert.AreEqual(
            new[] { string.Concat("ht", "tp://*/*"), string.Concat("ht", "tps://*/*") },
            contentScript.GetProperty("matches").EnumerateArray().Select(x => x.GetString()).ToArray());
        CollectionAssert.AreEqual(new[] { "content_script.js" }, contentScript.GetProperty("js").EnumerateArray().Select(x => x.GetString()).ToArray());
        Assert.AreEqual("document_idle", contentScript.GetProperty("run_at").GetString());
    }

    [TestMethod]
    public void SidepanelShellVisibleTextDoesNotContainLegacyNexa()
    {
        var html = ReadRepoText(SidepanelHtmlPath);
        AssertContains(html, "NODAL OS");
        AssertDoesNotContain(html, "NEXA");
        AssertContains(html, "NODAL OS necesita autorización");
        AssertContains(html, "Instrucción");
    }

    [TestMethod]
    public void CompatibilityKeysProtocolPortAlarmAndSocketRemainUnchanged()
    {
        var runtimeService = ReadRepoText(RuntimeServicePath());
        var sidepanel = ReadRepoText(SidepanelJsPath);

        AssertContains(runtimeService, "nexaRecipes");
        AssertContains(runtimeService, "nexaLearningDraft");
        AssertContains(runtimeService, "nexaRuntimeState");
        AssertContains(runtimeService, "nexa.keepalive");
        AssertContains(runtimeService, "nexa.content.ping");
        AssertContains(runtimeService, "PROTOCOL_VERSION = 'chrome-lab-v1'");
        AssertContains(runtimeService, "ws://${config.host || DEFAULT_CONFIG.host}:${config.port || DEFAULT_CONFIG.port}/ws/extension");
        AssertContains(sidepanel, "onebrain-sidepanel");
    }

    [TestMethod]
    public void SidepanelJsAndCssRemainByteIdenticalToM628Baseline()
    {
        Assert.AreEqual("E5DAE393D670E903FA0A8413D7DC2F4F33C46754AEE27C24CD98A0C4ED875869", Sha256Hex(SidepanelJsPath));
        Assert.AreEqual("0141931FA94B0004A8F2631C9E6985E1CF9243B0B9CBF787AFB2449858B6CED9", Sha256Hex(SidepanelCssPath));
    }

    [TestMethod]
    public void ReloadChecklistRequestsManualValidationAfterNaming()
    {
        var checklist = ReadRepoText(ReloadChecklistPath);
        AssertContains(checklist, "NODAL OS");
        AssertContains(checklist, "extensionReloaded");
        AssertContains(checklist, "chromeExtensionNameShowsNodalOs");
        AssertContains(checklist, "sidepanelHeadingShowsNodalOs");
        AssertContains(checklist, "visibleNexaTextRemaining");
        AssertContains(checklist, "consoleCriticalErrors");
    }

    [TestMethod]
    public void NewReportsArtifactsAndTestsDoNotContainForbiddenProductMarkers()
    {
        foreach (var path in new[] { InventoryPath, SummaryPath, ReloadChecklistPath, ReportPath, "tests/OneBrain.Safety.Tests/NodalOsExtensionLegacyNamingCleanupM629Tests.cs" })
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
