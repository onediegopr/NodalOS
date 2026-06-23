using System.Security.Cryptography;
using System.Text.Json;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace OneBrain.Safety.Tests;

[TestClass]
[TestCategory("PublicReleaseBlockerRemediation")]
[TestCategory("M644")]
public sealed class NodalOsPublicReleaseBlockerRemediationM644Tests
{
    private const string ReportPath = "docs/reports/m644-public-release-blocker-remediation-plan.md";
    private const string HostPermissionsPlanPath = "artifacts/agent-operations/m644/host-permissions-remediation-plan.json";
    private const string PackagingPlanPath = "artifacts/agent-operations/m644/packaging-signing-store-review-plan.json";
    private const string ProviderRuntimePlanPath = "artifacts/agent-operations/m644/provider-runtime-public-release-gate-plan.json";
    private const string GoNoGoPath = "artifacts/agent-operations/m644/public-release-blocker-remediation-go-no-go.json";

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
    public void ReportExists() =>
        Assert.IsTrue(File.Exists(FullPath(ReportPath)), ReportPath);

    [TestMethod]
    public void HostPermissionsPlanExists() =>
        Assert.IsTrue(File.Exists(FullPath(HostPermissionsPlanPath)), HostPermissionsPlanPath);

    [TestMethod]
    public void PackagingSigningStorePlanExists() =>
        Assert.IsTrue(File.Exists(FullPath(PackagingPlanPath)), PackagingPlanPath);

    [TestMethod]
    public void ProviderRuntimePublicReleaseGatePlanExists() =>
        Assert.IsTrue(File.Exists(FullPath(ProviderRuntimePlanPath)), ProviderRuntimePlanPath);

    [TestMethod]
    public void GoNoGoExists() =>
        Assert.IsTrue(File.Exists(FullPath(GoNoGoPath)), GoNoGoPath);

    [TestMethod]
    public void HostPermissionsPlanDoesNotModifyManifestOrPermissions()
    {
        using var doc = ReadJson(HostPermissionsPlanPath);
        Assert.AreEqual("HOST_PERMISSIONS_REMEDIATION_PLAN_READY", doc.RootElement.GetProperty("decision").GetString());
        Assert.IsFalse(doc.RootElement.GetProperty("manifestModified").GetBoolean());
        Assert.IsFalse(doc.RootElement.GetProperty("hostPermissionsModified").GetBoolean());
        Assert.IsFalse(doc.RootElement.GetProperty("productFilesModified").GetBoolean());
        Assert.IsFalse(doc.RootElement.GetProperty("publicReleaseReady").GetBoolean());
    }

    [TestMethod]
    public void HostPermissionsPlanIncludesRiskOptionsRecommendationAndClosureCriteria()
    {
        using var doc = ReadJson(HostPermissionsPlanPath);
        Assert.IsGreaterThanOrEqualTo(doc.RootElement.GetProperty("risk").GetArrayLength(), 4);
        Assert.AreEqual(3, doc.RootElement.GetProperty("remediationOptions").GetArrayLength());
        Assert.AreEqual("local_only_limited_public_scope_with_possible_future_narrowing_patch", doc.RootElement.GetProperty("recommendedOption").GetString());
        Assert.IsGreaterThanOrEqualTo(doc.RootElement.GetProperty("closureCriteria").GetArrayLength(), 4);
    }

    [TestMethod]
    public void PackagingPlanKeepsStoreReviewIncompleteAndPublicReleaseBlocked()
    {
        using var doc = ReadJson(PackagingPlanPath);
        Assert.AreEqual("PACKAGING_SIGNING_STORE_REVIEW_PLAN_READY", doc.RootElement.GetProperty("decision").GetString());
        Assert.IsFalse(doc.RootElement.GetProperty("packagingReviewComplete").GetBoolean());
        Assert.IsFalse(doc.RootElement.GetProperty("signingReviewComplete").GetBoolean());
        Assert.IsFalse(doc.RootElement.GetProperty("storeReviewComplete").GetBoolean());
        Assert.IsFalse(doc.RootElement.GetProperty("publicReleaseReady").GetBoolean());
        Assert.AreEqual("required before public release", doc.RootElement.GetProperty("privacyDisclosure").GetString());
        Assert.AreEqual("required before public release", doc.RootElement.GetProperty("supportUrl").GetString());
        Assert.AreEqual("required before public release", doc.RootElement.GetProperty("rollbackPlan").GetString());
        Assert.AreEqual("not-finalized", doc.RootElement.GetProperty("versionCandidate").GetString());
    }

    [TestMethod]
    public void ProviderRuntimePlanKeepsRuntimeProviderCloudFilesystemDisabled()
    {
        using var doc = ReadJson(ProviderRuntimePlanPath);
        Assert.IsTrue(doc.RootElement.GetProperty("openAiAgentClientPresent").GetBoolean());
        Assert.AreEqual("open", doc.RootElement.GetProperty("providerPromptNamingDebt").GetString());
        Assert.IsFalse(doc.RootElement.GetProperty("providerCloudEnabled").GetBoolean());
        Assert.IsFalse(doc.RootElement.GetProperty("runtimeProductiveEnabled").GetBoolean());
        Assert.IsFalse(doc.RootElement.GetProperty("filesystemEnabled").GetBoolean());
        Assert.IsFalse(doc.RootElement.GetProperty("capabilityUnlockEnabled").GetBoolean());
        Assert.IsFalse(doc.RootElement.GetProperty("publicReleaseReady").GetBoolean());
    }

    [TestMethod]
    public void GoNoGoKeepsAllSensitivePathsBlocked()
    {
        using var doc = ReadJson(GoNoGoPath);
        Assert.AreEqual("PUBLIC_RELEASE_BLOCKER_REMEDIATION_PLAN_READY", doc.RootElement.GetProperty("decision").GetString());
        Assert.IsTrue(doc.RootElement.GetProperty("readyForReleaseEvidenceGate").GetBoolean());
        Assert.IsFalse(doc.RootElement.GetProperty("readyForReleasePublic").GetBoolean());
        Assert.IsFalse(doc.RootElement.GetProperty("readyForJsChanges").GetBoolean());
        Assert.IsFalse(doc.RootElement.GetProperty("readyForRuntimeChanges").GetBoolean());
        Assert.IsFalse(doc.RootElement.GetProperty("readyForProviderCloud").GetBoolean());
        Assert.IsFalse(doc.RootElement.GetProperty("readyForFilesystem").GetBoolean());
        Assert.IsFalse(doc.RootElement.GetProperty("readyForBrowserAutomation").GetBoolean());
        Assert.IsFalse(doc.RootElement.GetProperty("manifestModified").GetBoolean());
        Assert.IsFalse(doc.RootElement.GetProperty("hostPermissionsModified").GetBoolean());
        Assert.IsFalse(doc.RootElement.GetProperty("runtimeEnabled").GetBoolean());
        Assert.IsFalse(doc.RootElement.GetProperty("providerCloudEnabled").GetBoolean());
        Assert.IsFalse(doc.RootElement.GetProperty("filesystemEnabled").GetBoolean());
        Assert.IsFalse(doc.RootElement.GetProperty("publicReleaseEnabled").GetBoolean());
        Assert.IsFalse(doc.RootElement.GetProperty("productFilesModified").GetBoolean());
    }

    [TestMethod]
    public void M644ClosesOnlyPlanningAndLeavesReleaseBlockersOpen()
    {
        using var doc = ReadJson(GoNoGoPath);
        var closes = doc.RootElement.GetProperty("m644Closes").EnumerateArray()
            .Select(item => item.GetString())
            .ToArray();
        var open = doc.RootElement.GetProperty("m644LeavesOpen").EnumerateArray()
            .Select(item => item.GetString())
            .ToArray();

        CollectionAssert.Contains(closes, "remediation plan creation");
        CollectionAssert.Contains(open, "host permissions formal justification");
        CollectionAssert.Contains(open, "packaging signing store review");
        CollectionAssert.Contains(open, "provider runtime final disabled-state release proof");
        CollectionAssert.Contains(open, "public release approval");
    }

    [TestMethod]
    public void ManifestJsonUnchanged()
    {
        Assert.AreEqual("76859A171D0FBC585E96253D0F269AEF4A54DCFD5F704DE9DB92D26EB1AEDDFD", Sha256Hex(ManifestPath));
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
        Assert.AreEqual("E42D5247C0A9CCAC250EB51300E6F6C1B701CADBA3DBD4B86A62126CC7A1933D", Sha256Hex(ServiceWorkerPath));
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
        Assert.AreEqual("D4E043EC6EC181D780FD2A18AAC48BB39FED4F9078E03229BF5F718635031712", Sha256Hex(BridgeOptionsPath));
        Assert.AreEqual("A22DAE8449A297DC77E5E0E28D404DA253846BEC541033F2E0EF6AC779562624", Sha256Hex(BridgeProtocolPath));
        Assert.AreEqual("5E933A21C501487523C60197F02E0D68CE6D0F5865DC9401F9337EBA41B58B37", Sha256Hex(BridgeSecurityPath));
        Assert.AreEqual("8CF05765736DEF46584022F1E33E22CD4E85A3C08A952048D425FBBC1EFE5309", Sha256Hex(BridgeSelfTestPath));
        Assert.AreEqual("CAACC05544B9E41F7606BD17ECAD81FB36842869BC34E46F8596D4D62EB5B5C4", Sha256Hex(BridgeOpenAiPath));
        Assert.AreEqual("061CED72CF27BC31C0BAD240BC698B58D0D5966D6D107CE8A315C98D52DB1305", Sha256Hex(BridgeProgramPath));
        Assert.AreEqual("E00407FA4BC6DCB8ADBB677E5FE4AF41FE7BDB967C5827EBF54569E6DF32E85D", Sha256Hex(BridgeProjectPath));
    }
}
