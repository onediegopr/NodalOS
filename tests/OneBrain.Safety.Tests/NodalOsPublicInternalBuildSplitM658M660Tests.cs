using System.Security.Cryptography;
using System.Text.Json;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace OneBrain.Safety.Tests;

[TestClass]
[TestCategory("PublicInternalBuildSplit")]
[TestCategory("M658")]
[TestCategory("M659")]
[TestCategory("M660")]
public sealed class NodalOsPublicInternalBuildSplitM658M660Tests
{
    private const string M658ReportPath = "docs/reports/m658-public-internal-build-split-implementation-plan.md";
    private const string M659ReportPath = "docs/reports/m659-manifest-variant-contract.md";
    private const string M660ReportPath = "docs/reports/m660-public-build-patch-readiness-gate.md";

    private const string SplitPlanPath = "artifacts/agent-operations/m658/public-internal-build-split-plan.json";
    private const string BuildChannelTaxonomyPath = "artifacts/agent-operations/m658/build-channel-taxonomy.json";
    private const string InternalBuildPreservationPath = "artifacts/agent-operations/m658/internal-build-preservation-plan.json";
    private const string PublicBuildCreationPath = "artifacts/agent-operations/m658/public-build-creation-plan.json";
    private const string M658GoNoGoPath = "artifacts/agent-operations/m658/m658-go-no-go.json";

    private const string ManifestVariantContractPath = "artifacts/agent-operations/m659/manifest-variant-contract.json";
    private const string PublicManifestTargetShapePath = "artifacts/agent-operations/m659/public-manifest-target-shape.json";
    private const string InternalManifestPreservationPath = "artifacts/agent-operations/m659/internal-manifest-preservation-contract.json";
    private const string ContentScriptVariantContractPath = "artifacts/agent-operations/m659/content-script-variant-contract.json";
    private const string PermissionsVariantContractPath = "artifacts/agent-operations/m659/permissions-variant-contract.json";
    private const string M659GoNoGoPath = "artifacts/agent-operations/m659/m659-go-no-go.json";

    private const string PatchReadinessGatePath = "artifacts/agent-operations/m660/public-build-patch-readiness-gate.json";
    private const string PatchPrerequisitesPath = "artifacts/agent-operations/m660/patch-prerequisites-checklist.json";
    private const string RegressionQaPath = "artifacts/agent-operations/m660/public-build-regression-qa-plan.json";
    private const string StoreDisclosurePath = "artifacts/agent-operations/m660/store-disclosure-readiness-plan.json";
    private const string RollbackPlanPath = "artifacts/agent-operations/m660/rollback-plan-for-manifest-split.json";
    private const string M660GoNoGoPath = "artifacts/agent-operations/m660/m660-go-no-go.json";

    private const string ConsolidatedGoNoGoPath = "artifacts/agent-operations/m658-m660/public-internal-build-split-readiness-go-no-go.json";

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
    public void M658ArtifactsExist()
    {
        Assert.IsTrue(File.Exists(FullPath(M658ReportPath)), M658ReportPath);
        Assert.IsTrue(File.Exists(FullPath(SplitPlanPath)), SplitPlanPath);
        Assert.IsTrue(File.Exists(FullPath(BuildChannelTaxonomyPath)), BuildChannelTaxonomyPath);
        Assert.IsTrue(File.Exists(FullPath(InternalBuildPreservationPath)), InternalBuildPreservationPath);
        Assert.IsTrue(File.Exists(FullPath(PublicBuildCreationPath)), PublicBuildCreationPath);
        Assert.IsTrue(File.Exists(FullPath(M658GoNoGoPath)), M658GoNoGoPath);
    }

    [TestMethod]
    public void M659ArtifactsExist()
    {
        Assert.IsTrue(File.Exists(FullPath(M659ReportPath)), M659ReportPath);
        Assert.IsTrue(File.Exists(FullPath(ManifestVariantContractPath)), ManifestVariantContractPath);
        Assert.IsTrue(File.Exists(FullPath(PublicManifestTargetShapePath)), PublicManifestTargetShapePath);
        Assert.IsTrue(File.Exists(FullPath(InternalManifestPreservationPath)), InternalManifestPreservationPath);
        Assert.IsTrue(File.Exists(FullPath(ContentScriptVariantContractPath)), ContentScriptVariantContractPath);
        Assert.IsTrue(File.Exists(FullPath(PermissionsVariantContractPath)), PermissionsVariantContractPath);
        Assert.IsTrue(File.Exists(FullPath(M659GoNoGoPath)), M659GoNoGoPath);
    }

    [TestMethod]
    public void M660ArtifactsExist()
    {
        Assert.IsTrue(File.Exists(FullPath(M660ReportPath)), M660ReportPath);
        Assert.IsTrue(File.Exists(FullPath(PatchReadinessGatePath)), PatchReadinessGatePath);
        Assert.IsTrue(File.Exists(FullPath(PatchPrerequisitesPath)), PatchPrerequisitesPath);
        Assert.IsTrue(File.Exists(FullPath(RegressionQaPath)), RegressionQaPath);
        Assert.IsTrue(File.Exists(FullPath(StoreDisclosurePath)), StoreDisclosurePath);
        Assert.IsTrue(File.Exists(FullPath(RollbackPlanPath)), RollbackPlanPath);
        Assert.IsTrue(File.Exists(FullPath(M660GoNoGoPath)), M660GoNoGoPath);
    }

    [TestMethod]
    public void ConsolidatedM658M660GoNoGoExists() =>
        Assert.IsTrue(File.Exists(FullPath(ConsolidatedGoNoGoPath)), ConsolidatedGoNoGoPath);

    [TestMethod]
    public void PublicInternalBuildSplitPlanKeepsProductFilesUnchanged()
    {
        using var doc = ReadJson(SplitPlanPath);
        Assert.AreEqual("PUBLIC_INTERNAL_BUILD_SPLIT_IMPLEMENTATION_PLAN_READY", doc.RootElement.GetProperty("decision").GetString());
        Assert.AreEqual("manifest_variant_contract_with_build_channel_boundary", doc.RootElement.GetProperty("recommendedStrategy").GetString());
        Assert.IsTrue(doc.RootElement.GetProperty("internalCandidateReady").GetBoolean());
        Assert.IsFalse(doc.RootElement.GetProperty("publicReleaseReady").GetBoolean());
        Assert.IsFalse(doc.RootElement.GetProperty("manifestModified").GetBoolean());
        Assert.IsFalse(doc.RootElement.GetProperty("hostPermissionsModified").GetBoolean());
        Assert.IsFalse(doc.RootElement.GetProperty("contentScriptsMatchesModified").GetBoolean());
        Assert.IsFalse(doc.RootElement.GetProperty("permissionsModified").GetBoolean());
        Assert.IsFalse(doc.RootElement.GetProperty("jsModified").GetBoolean());
        Assert.IsFalse(doc.RootElement.GetProperty("bridgeModified").GetBoolean());
        Assert.IsFalse(doc.RootElement.GetProperty("productFilesModified").GetBoolean());
    }

    [TestMethod]
    public void BuildChannelTaxonomyAndPreservationPlanExist()
    {
        using var taxonomy = ReadJson(BuildChannelTaxonomyPath);
        using var preservation = ReadJson(InternalBuildPreservationPath);
        Assert.AreEqual("BUILD_CHANNEL_TAXONOMY_READY", taxonomy.RootElement.GetProperty("decision").GetString());
        Assert.IsGreaterThanOrEqualTo(3, taxonomy.RootElement.GetProperty("channels").GetArrayLength());
        Assert.AreEqual("internal_candidate_and_public_candidate_must_remain_distinct", taxonomy.RootElement.GetProperty("recommendedChannelBoundary").GetString());
        Assert.AreEqual("INTERNAL_BUILD_PRESERVATION_PLAN_READY", preservation.RootElement.GetProperty("decision").GetString());
        Assert.IsTrue(preservation.RootElement.GetProperty("preserveCurrentInternalCandidate").GetBoolean());
        Assert.IsTrue(preservation.RootElement.GetProperty("internalCandidateReady").GetBoolean());
        Assert.IsFalse(preservation.RootElement.GetProperty("publicReleaseReady").GetBoolean());
    }

    [TestMethod]
    public void PublicBuildCreationPlanDoesNotCreatePublicBuild()
    {
        using var doc = ReadJson(PublicBuildCreationPath);
        Assert.AreEqual("PUBLIC_BUILD_CREATION_PLAN_READY", doc.RootElement.GetProperty("decision").GetString());
        Assert.IsFalse(doc.RootElement.GetProperty("publicBuildCreatedNow").GetBoolean());
        Assert.AreEqual("future_manifest_variant_patch_with_separate_public_candidate_channel", doc.RootElement.GetProperty("recommendedCreationModel").GetString());
        Assert.IsFalse(doc.RootElement.GetProperty("publicReleaseReady").GetBoolean());
        Assert.IsFalse(doc.RootElement.GetProperty("publicManifestPatchReady").GetBoolean());
        Assert.IsFalse(doc.RootElement.GetProperty("manifestModified").GetBoolean());
    }

    [TestMethod]
    public void ManifestVariantContractAndPublicTargetShapeAreContractOnly()
    {
        using var contract = ReadJson(ManifestVariantContractPath);
        using var target = ReadJson(PublicManifestTargetShapePath);
        Assert.AreEqual("MANIFEST_VARIANT_CONTRACT_READY", contract.RootElement.GetProperty("decision").GetString());
        Assert.AreEqual("split_internal_public_build_with_public_optional_permissions_and_narrow_default_matches", contract.RootElement.GetProperty("recommendedTarget").GetString());
        Assert.IsTrue(contract.RootElement.GetProperty("requiresManualQa").GetBoolean());
        Assert.IsTrue(contract.RootElement.GetProperty("requiresStoreDisclosure").GetBoolean());
        Assert.IsFalse(contract.RootElement.GetProperty("manifestModified").GetBoolean());
        Assert.AreEqual("PUBLIC_MANIFEST_TARGET_SHAPE_READY", target.RootElement.GetProperty("decision").GetString());
        Assert.IsFalse(target.RootElement.GetProperty("publicBuildCreatedNow").GetBoolean());
        CollectionAssert.Contains(target.RootElement.GetProperty("targetHostPermissions").EnumerateArray().Select(item => item.GetString()).ToArray(), "http://127.0.0.1/*");
        Assert.IsTrue(target.RootElement.GetProperty("optionalHostPermissionsConsidered").GetBoolean());
        Assert.IsFalse(target.RootElement.GetProperty("publicReleaseReady").GetBoolean());
    }

    [TestMethod]
    public void ContentScriptAndPermissionsVariantContractsRemainUnapplied()
    {
        using var content = ReadJson(ContentScriptVariantContractPath);
        using var permissions = ReadJson(PermissionsVariantContractPath);
        Assert.AreEqual("CONTENT_SCRIPT_VARIANT_CONTRACT_READY", content.RootElement.GetProperty("decision").GetString());
        Assert.AreEqual("narrow_default_matches_or_user_granted_origins", content.RootElement.GetProperty("futurePublicMatchesStrategy").GetString());
        Assert.IsFalse(content.RootElement.GetProperty("contentScriptsMatchesModified").GetBoolean());
        Assert.IsFalse(content.RootElement.GetProperty("manifestModified").GetBoolean());
        Assert.IsFalse(content.RootElement.GetProperty("jsModified").GetBoolean());
        Assert.AreEqual("PERMISSIONS_VARIANT_CONTRACT_READY", permissions.RootElement.GetProperty("decision").GetString());
        Assert.IsTrue(permissions.RootElement.GetProperty("requiresUserApprovalForExpandedSiteAccess").GetBoolean());
        Assert.IsFalse(permissions.RootElement.GetProperty("permissionsChangedNow").GetBoolean());
        Assert.IsFalse(permissions.RootElement.GetProperty("hostPermissionsChangedNow").GetBoolean());
        Assert.IsFalse(permissions.RootElement.GetProperty("runtimeProductiveEnabled").GetBoolean());
        Assert.IsFalse(permissions.RootElement.GetProperty("providerCloudEnabled").GetBoolean());
        Assert.IsFalse(permissions.RootElement.GetProperty("filesystemEnabled").GetBoolean());
        Assert.IsFalse(permissions.RootElement.GetProperty("browserAutomationEnabled").GetBoolean());
        Assert.IsFalse(permissions.RootElement.GetProperty("capabilityUnlockEnabled").GetBoolean());
    }

    [TestMethod]
    public void PublicBuildPatchReadinessGateIsConditionalForFutureMilestone()
    {
        using var gate = ReadJson(PatchReadinessGatePath);
        Assert.AreEqual("PUBLIC_BUILD_PATCH_READINESS_GATE_READY", gate.RootElement.GetProperty("decision").GetString());
        Assert.AreEqual("CONDITIONAL_GO_FOR_M661_M663", gate.RootElement.GetProperty("publicManifestPatchDecision").GetString());
        Assert.IsTrue(gate.RootElement.GetProperty("nextMilestoneCanTouchManifest").GetBoolean());
        Assert.IsFalse(gate.RootElement.GetProperty("currentMilestoneTouchesManifest").GetBoolean());
        Assert.IsFalse(gate.RootElement.GetProperty("publicReleaseReady").GetBoolean());
        Assert.IsTrue(gate.RootElement.GetProperty("internalCandidateReady").GetBoolean());
        Assert.IsFalse(gate.RootElement.GetProperty("manifestModified").GetBoolean());
        Assert.IsFalse(gate.RootElement.GetProperty("hostPermissionsModified").GetBoolean());
        Assert.IsFalse(gate.RootElement.GetProperty("contentScriptsMatchesModified").GetBoolean());
        Assert.AreEqual("M661-M663 Public Manifest Variant Patch + Host Permissions Narrowing + Regression QA", gate.RootElement.GetProperty("recommendedNextMilestone").GetString());
    }

    [TestMethod]
    public void PatchPrerequisitesQaStoreAndRollbackPlansExist()
    {
        using var prereqs = ReadJson(PatchPrerequisitesPath);
        using var qa = ReadJson(RegressionQaPath);
        using var store = ReadJson(StoreDisclosurePath);
        using var rollback = ReadJson(RollbackPlanPath);
        Assert.AreEqual("PATCH_PREREQUISITES_CHECKLIST_READY", prereqs.RootElement.GetProperty("decision").GetString());
        Assert.IsGreaterThanOrEqualTo(5, prereqs.RootElement.GetProperty("items").GetArrayLength());
        Assert.AreEqual("PUBLIC_BUILD_REGRESSION_QA_PLAN_READY", qa.RootElement.GetProperty("decision").GetString());
        Assert.IsGreaterThanOrEqualTo(10, qa.RootElement.GetProperty("qaRequired").GetArrayLength());
        Assert.AreEqual("STORE_DISCLOSURE_READINESS_PLAN_READY", store.RootElement.GetProperty("decision").GetString());
        Assert.IsFalse(store.RootElement.GetProperty("storeDisclosureFinalReady").GetBoolean());
        Assert.IsFalse(store.RootElement.GetProperty("publicReleaseReady").GetBoolean());
        Assert.AreEqual("ROLLBACK_PLAN_FOR_MANIFEST_SPLIT_READY", rollback.RootElement.GetProperty("decision").GetString());
        Assert.AreEqual("current_internal_candidate_manifest_baseline", rollback.RootElement.GetProperty("rollbackTarget").GetString());
        Assert.IsTrue(rollback.RootElement.GetProperty("rollbackRequiresPublicReleaseStop").GetBoolean());
    }

    [TestMethod]
    public void ConsolidatedGoNoGoMatchesM658M660Contract()
    {
        using var doc = ReadJson(ConsolidatedGoNoGoPath);
        Assert.AreEqual("PUBLIC_INTERNAL_BUILD_SPLIT_PLAN_READY", doc.RootElement.GetProperty("decision").GetString());
        Assert.IsTrue(doc.RootElement.GetProperty("internalCandidateReady").GetBoolean());
        Assert.IsFalse(doc.RootElement.GetProperty("publicReleaseReady").GetBoolean());
        Assert.IsTrue(doc.RootElement.GetProperty("publicBuildStrategyReady").GetBoolean());
        Assert.IsTrue(doc.RootElement.GetProperty("publicManifestPatchReady").GetBoolean());
        Assert.IsFalse(doc.RootElement.GetProperty("manifestModified").GetBoolean());
        Assert.IsFalse(doc.RootElement.GetProperty("hostPermissionsModified").GetBoolean());
        Assert.IsFalse(doc.RootElement.GetProperty("contentScriptsMatchesModified").GetBoolean());
        Assert.IsFalse(doc.RootElement.GetProperty("permissionsModified").GetBoolean());
        Assert.IsFalse(doc.RootElement.GetProperty("jsModified").GetBoolean());
        Assert.IsFalse(doc.RootElement.GetProperty("bridgeModified").GetBoolean());
        Assert.IsFalse(doc.RootElement.GetProperty("runtimeProductiveEnabled").GetBoolean());
        Assert.IsFalse(doc.RootElement.GetProperty("providerCloudEnabled").GetBoolean());
        Assert.IsFalse(doc.RootElement.GetProperty("filesystemEnabled").GetBoolean());
        Assert.IsFalse(doc.RootElement.GetProperty("browserAutomationEnabled").GetBoolean());
        Assert.IsFalse(doc.RootElement.GetProperty("capabilityUnlockEnabled").GetBoolean());
        Assert.AreEqual("M661-M663 Public Manifest Variant Patch + Host Permissions Narrowing + Regression QA", doc.RootElement.GetProperty("recommendedNextMilestone").GetString());
    }

    [TestMethod]
    public void M658M660GoNoGosRemainPlanningOnly()
    {
        using var m658 = ReadJson(M658GoNoGoPath);
        using var m659 = ReadJson(M659GoNoGoPath);
        using var m660 = ReadJson(M660GoNoGoPath);
        Assert.IsTrue(m658.RootElement.GetProperty("publicInternalBuildSplitPlanReady").GetBoolean());
        Assert.IsTrue(m659.RootElement.GetProperty("manifestVariantContractReady").GetBoolean());
        Assert.IsTrue(m660.RootElement.GetProperty("publicManifestPatchConditionalGo").GetBoolean());
        Assert.IsTrue(m660.RootElement.GetProperty("internalCandidateReady").GetBoolean());
        Assert.IsFalse(m660.RootElement.GetProperty("publicReleaseReady").GetBoolean());
        Assert.IsFalse(m658.RootElement.GetProperty("manifestModified").GetBoolean());
        Assert.IsFalse(m659.RootElement.GetProperty("permissionsModified").GetBoolean());
        Assert.IsFalse(m660.RootElement.GetProperty("hostPermissionsModified").GetBoolean());
        Assert.IsFalse(m660.RootElement.GetProperty("runtimeProductiveEnabled").GetBoolean());
        Assert.IsFalse(m660.RootElement.GetProperty("providerCloudEnabled").GetBoolean());
        Assert.IsFalse(m660.RootElement.GetProperty("filesystemEnabled").GetBoolean());
        Assert.IsFalse(m660.RootElement.GetProperty("browserAutomationEnabled").GetBoolean());
        Assert.IsFalse(m660.RootElement.GetProperty("capabilityUnlockEnabled").GetBoolean());
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
