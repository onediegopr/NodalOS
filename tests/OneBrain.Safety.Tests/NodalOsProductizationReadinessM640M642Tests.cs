using System.Security.Cryptography;
using System.Text.Json;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace OneBrain.Safety.Tests;

[TestClass]
[TestCategory("ProductizationReadiness")]
[TestCategory("M640-M642")]
public sealed class NodalOsProductizationReadinessM640M642Tests
{
    private const string M640ReportPath = "docs/reports/m640-productization-readiness-review.md";
    private const string M641ReportPath = "docs/reports/m641-public-release-blockers-consolidation.md";
    private const string M642ReportPath = "docs/reports/m642-future-runtime-enablement-plan.md";
    private const string M640ArtifactPath = "artifacts/agent-operations/m640/productization-readiness-review.json";
    private const string M641ArtifactPath = "artifacts/agent-operations/m641/public-release-blockers-consolidated.json";
    private const string M642ArtifactPath = "artifacts/agent-operations/m642/future-runtime-enablement-plan.json";
    private const string GoNoGoPath = "artifacts/agent-operations/m640-m642/post-m640-m642-go-no-go.json";

    private const string ManifestPath = "browser-extension/onebrain-chrome-lab/manifest.json";
    private const string SidepanelHtmlPath = "browser-extension/onebrain-chrome-lab/sidepanel.html";
    private const string SidepanelCssPath = "browser-extension/onebrain-chrome-lab/sidepanel.css";
    private const string SidepanelJsPath = "browser-extension/onebrain-chrome-lab/sidepanel.js";
    private const string ServiceWorkerPath = "browser-extension/onebrain-chrome-lab/service_worker.js";
    private const string ContentScriptPath = "browser-extension/onebrain-chrome-lab/content_script.js";
    private const string RecipeCorePath = "browser-extension/onebrain-chrome-lab/recipe_core.js";

    private const string BridgeOptionsPath = "src/OneBrain.ChromeLab.Bridge/ChromeLabOptions.cs";
    private const string BridgeProtocolPath = "src/OneBrain.ChromeLab.Bridge/ChromeLabProtocol.cs";
    private const string BridgeSecurityPath = "src/OneBrain.ChromeLab.Bridge/ChromeLabSecurity.cs";
    private const string BridgeSelfTestPath = "src/OneBrain.ChromeLab.Bridge/ChromeLabSelfTest.cs";
    private const string BridgeOpenAiPath = "src/OneBrain.ChromeLab.Bridge/OpenAiAgentClient.cs";
    private const string BridgeProgramPath = "src/OneBrain.ChromeLab.Bridge/Program.cs";
    private const string BridgeProjectPath = "src/OneBrain.ChromeLab.Bridge/OneBrain.ChromeLab.Bridge.csproj";

    private static string RepoRoot()
    {
        var dir = new DirectoryInfo(Environment.CurrentDirectory);
        while (dir != null && !File.Exists(Path.Combine(dir.FullName, "OneBrain.slnx")))
            dir = dir.Parent;
        return dir?.FullName ?? Environment.CurrentDirectory;
    }

    private static string FullPath(string relativePath) =>
        Path.Combine(RepoRoot(), relativePath);

    private static JsonDocument ReadJson(string relativePath) =>
        JsonDocument.Parse(File.ReadAllText(FullPath(relativePath)));

    private static string Sha256Hex(string relativePath)
    {
        var bytes = File.ReadAllBytes(FullPath(relativePath));
        return Convert.ToHexString(SHA256.HashData(bytes));
    }

    [TestMethod]
    public void M640ReportExists() =>
        Assert.IsTrue(File.Exists(FullPath(M640ReportPath)), M640ReportPath);

    [TestMethod]
    public void M641ReportExists() =>
        Assert.IsTrue(File.Exists(FullPath(M641ReportPath)), M641ReportPath);

    [TestMethod]
    public void M642ReportExists() =>
        Assert.IsTrue(File.Exists(FullPath(M642ReportPath)), M642ReportPath);

    [TestMethod]
    public void M640ArtifactExists() =>
        Assert.IsTrue(File.Exists(FullPath(M640ArtifactPath)), M640ArtifactPath);

    [TestMethod]
    public void M641ArtifactExists() =>
        Assert.IsTrue(File.Exists(FullPath(M641ArtifactPath)), M641ArtifactPath);

    [TestMethod]
    public void M642ArtifactExists() =>
        Assert.IsTrue(File.Exists(FullPath(M642ArtifactPath)), M642ArtifactPath);

    [TestMethod]
    public void GoNoGoArtifactExists() =>
        Assert.IsTrue(File.Exists(FullPath(GoNoGoPath)), GoNoGoPath);

    [TestMethod]
    public void ProductizationReadinessKeepsPublicReleaseBlocked()
    {
        using var doc = ReadJson(M640ArtifactPath);
        Assert.AreEqual("M640", doc.RootElement.GetProperty("milestone").GetString());
        Assert.AreEqual("PRODUCTIZATION_READINESS_REVIEW_READY", doc.RootElement.GetProperty("decision").GetString());
        Assert.AreEqual("go", doc.RootElement.GetProperty("installedExtensionEvidenceGate").GetString());
        Assert.AreEqual("no-go", doc.RootElement.GetProperty("publicRelease").GetString());
        Assert.IsFalse(doc.RootElement.GetProperty("productFilesModified").GetBoolean());
    }

    [TestMethod]
    public void PublicReleaseBlockersIncludeExpectedRisks()
    {
        using var doc = ReadJson(M641ArtifactPath);
        var blockers = doc.RootElement.GetProperty("blockers").EnumerateArray()
            .Select(item => item.GetProperty("id").GetString())
            .ToArray();

        CollectionAssert.Contains(blockers, "HOST_PERMISSIONS_BROAD_REVIEW");
        CollectionAssert.Contains(blockers, "PROVIDER_RUNTIME_RELEASE_GATE");
        CollectionAssert.Contains(blockers, "IPV6_LOOPBACK_CAVEAT");
        CollectionAssert.Contains(blockers, "CORS_LAN_CAVEAT");
        CollectionAssert.Contains(blockers, "PROVIDER_PROMPT_NAMING_DEBT");
        CollectionAssert.Contains(blockers, "MICROCOPY_MIXED_SURFACE");
        CollectionAssert.Contains(blockers, "RELEASE_EVIDENCE_REQUIREMENTS");
        CollectionAssert.Contains(blockers, "PACKAGING_SIGNING_STORE_PUBLICATION");
    }

    [TestMethod]
    public void FutureRuntimePlanDoesNotEnableRuntimeOrSensitiveCapabilities()
    {
        using var doc = ReadJson(M642ArtifactPath);
        Assert.AreEqual("M642", doc.RootElement.GetProperty("milestone").GetString());
        Assert.AreEqual("FUTURE_RUNTIME_ENABLEMENT_PLAN_READY", doc.RootElement.GetProperty("decision").GetString());
        Assert.IsFalse(doc.RootElement.GetProperty("runtimeEnabled").GetBoolean());
        Assert.IsFalse(doc.RootElement.GetProperty("runtimeProductiveEnabled").GetBoolean());
        Assert.IsFalse(doc.RootElement.GetProperty("providerCloudEnabled").GetBoolean());
        Assert.IsFalse(doc.RootElement.GetProperty("filesystemEnabled").GetBoolean());
        Assert.IsFalse(doc.RootElement.GetProperty("browserAutomationEnabled").GetBoolean());
        Assert.IsFalse(doc.RootElement.GetProperty("connectorActionsEnabled").GetBoolean());
        Assert.IsFalse(doc.RootElement.GetProperty("capabilityUnlockEnabled").GetBoolean());
        Assert.IsFalse(doc.RootElement.GetProperty("productFilesModified").GetBoolean());
    }

    [TestMethod]
    public void ConsolidatedGoNoGoKeepsAllSensitiveReleasePathsBlocked()
    {
        using var doc = ReadJson(GoNoGoPath);
        Assert.AreEqual("M640-M642", doc.RootElement.GetProperty("milestone").GetString());
        Assert.AreEqual("PRODUCTIZATION_RELEASE_RUNTIME_READINESS_PLAN_READY", doc.RootElement.GetProperty("decision").GetString());
        Assert.IsTrue(doc.RootElement.GetProperty("readyForReleaseEvidenceGate").GetBoolean());
        Assert.IsFalse(doc.RootElement.GetProperty("readyForReleasePublic").GetBoolean());
        Assert.IsFalse(doc.RootElement.GetProperty("readyForJsChanges").GetBoolean());
        Assert.IsFalse(doc.RootElement.GetProperty("readyForRuntimeChanges").GetBoolean());
        Assert.IsFalse(doc.RootElement.GetProperty("readyForProviderCloud").GetBoolean());
        Assert.IsFalse(doc.RootElement.GetProperty("readyForFilesystem").GetBoolean());
        Assert.IsFalse(doc.RootElement.GetProperty("readyForBrowserAutomation").GetBoolean());
        Assert.IsFalse(doc.RootElement.GetProperty("runtimeEnabled").GetBoolean());
        Assert.IsFalse(doc.RootElement.GetProperty("providerCloudEnabled").GetBoolean());
        Assert.IsFalse(doc.RootElement.GetProperty("filesystemEnabled").GetBoolean());
        Assert.IsFalse(doc.RootElement.GetProperty("publicReleaseEnabled").GetBoolean());
        Assert.IsFalse(doc.RootElement.GetProperty("productFilesModified").GetBoolean());
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
    public void BridgeSourceUnchanged()
    {
        Assert.AreEqual("F61164907EE4FB35AA8C0E3CA4A13A98986258B9AB4EDD1469A9D146AF81646A", Sha256Hex(BridgeOptionsPath));
        Assert.AreEqual("C62BBBEA97ACA6B15D72F25415F9E94E20FE9D460903AEB9292A107DCF2AFE68", Sha256Hex(BridgeProtocolPath));
        Assert.AreEqual("5E933A21C501487523C60197F02E0D68CE6D0F5865DC9401F9337EBA41B58B37", Sha256Hex(BridgeSecurityPath));
        Assert.AreEqual("8CF05765736DEF46584022F1E33E22CD4E85A3C08A952048D425FBBC1EFE5309", Sha256Hex(BridgeSelfTestPath));
        Assert.AreEqual("CAACC05544B9E41F7606BD17ECAD81FB36842869BC34E46F8596D4D62EB5B5C4", Sha256Hex(BridgeOpenAiPath));
        Assert.AreEqual("B9FB617E6B2FD1393EC4E1DD7B90AF91E44B26EBD4D70DA87416A5B46531E21E", Sha256Hex(BridgeProgramPath));
        Assert.AreEqual("E00407FA4BC6DCB8ADBB677E5FE4AF41FE7BDB967C5827EBF54569E6DF32E85D", Sha256Hex(BridgeProjectPath));
    }
}
