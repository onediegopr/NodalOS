using System.Security.Cryptography;
using System.Text.Json;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace OneBrain.Safety.Tests;

[TestClass]
[TestCategory("PublicReleaseBlockersEvidencePrep")]
[TestCategory("M645")]
[TestCategory("M646")]
[TestCategory("M647")]
public sealed class NodalOsPublicReleaseBlockersEvidencePrepM645M647Tests
{
    private const string M645ReportPath = "docs/reports/m645-host-permissions-justification-package.md";
    private const string M646ReportPath = "docs/reports/m646-packaging-signing-store-evidence-prep.md";
    private const string M647ReportPath = "docs/reports/m647-provider-runtime-disabled-state-proof-closure.md";

    private const string HostPermissionsCurrentStatePath = "artifacts/agent-operations/m645/host-permissions-current-state.json";
    private const string HostPermissionsRiskAssessmentPath = "artifacts/agent-operations/m645/host-permissions-risk-assessment.json";
    private const string HostPermissionsReleaseJustificationPath = "artifacts/agent-operations/m645/host-permissions-release-justification.json";
    private const string HostPermissionsFutureNarrowingPlanPath = "artifacts/agent-operations/m645/host-permissions-future-narrowing-plan.json";
    private const string M645GoNoGoPath = "artifacts/agent-operations/m645/m645-go-no-go.json";

    private const string PackagingChecklistPath = "artifacts/agent-operations/m646/packaging-checklist.json";
    private const string SigningRequirementsPath = "artifacts/agent-operations/m646/signing-requirements.json";
    private const string ChromeStoreReadinessPath = "artifacts/agent-operations/m646/chrome-store-readiness-checklist.json";
    private const string InternalDistributionReadinessPath = "artifacts/agent-operations/m646/internal-distribution-readiness-checklist.json";
    private const string PrivacySupportDisclosurePath = "artifacts/agent-operations/m646/privacy-support-disclosure-draft.json";
    private const string RollbackPlanPath = "artifacts/agent-operations/m646/rollback-plan.json";
    private const string ReleaseNotesDraftPath = "artifacts/agent-operations/m646/release-notes-draft.json";
    private const string StoreListingRiskRegisterPath = "artifacts/agent-operations/m646/store-listing-risk-register.json";
    private const string M646GoNoGoPath = "artifacts/agent-operations/m646/m646-go-no-go.json";

    private const string ProviderDisabledProofPath = "artifacts/agent-operations/m647/provider-disabled-state-proof.json";
    private const string RuntimeDisabledProofPath = "artifacts/agent-operations/m647/runtime-disabled-state-proof.json";
    private const string FilesystemDisabledProofPath = "artifacts/agent-operations/m647/filesystem-disabled-state-proof.json";
    private const string BrowserAutomationDisabledProofPath = "artifacts/agent-operations/m647/browser-automation-disabled-state-proof.json";
    private const string CapabilityUnlockDisabledProofPath = "artifacts/agent-operations/m647/capability-unlock-disabled-proof.json";
    private const string OpenAiRiskRegisterPath = "artifacts/agent-operations/m647/openai-agent-client-risk-register.json";
    private const string ProviderPromptNamingDebtPath = "artifacts/agent-operations/m647/provider-prompt-naming-debt.json";
    private const string M647GoNoGoPath = "artifacts/agent-operations/m647/m647-go-no-go.json";

    private const string ConsolidatedGoNoGoPath = "artifacts/agent-operations/m645-m647/public-release-blockers-evidence-prep-go-no-go.json";

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
    public void M645ReportsAndArtifactsExist()
    {
        Assert.IsTrue(File.Exists(FullPath(M645ReportPath)), M645ReportPath);
        Assert.IsTrue(File.Exists(FullPath(HostPermissionsCurrentStatePath)), HostPermissionsCurrentStatePath);
        Assert.IsTrue(File.Exists(FullPath(HostPermissionsRiskAssessmentPath)), HostPermissionsRiskAssessmentPath);
        Assert.IsTrue(File.Exists(FullPath(HostPermissionsReleaseJustificationPath)), HostPermissionsReleaseJustificationPath);
        Assert.IsTrue(File.Exists(FullPath(HostPermissionsFutureNarrowingPlanPath)), HostPermissionsFutureNarrowingPlanPath);
        Assert.IsTrue(File.Exists(FullPath(M645GoNoGoPath)), M645GoNoGoPath);
    }

    [TestMethod]
    public void M646ReportsAndArtifactsExist()
    {
        Assert.IsTrue(File.Exists(FullPath(M646ReportPath)), M646ReportPath);
        Assert.IsTrue(File.Exists(FullPath(PackagingChecklistPath)), PackagingChecklistPath);
        Assert.IsTrue(File.Exists(FullPath(SigningRequirementsPath)), SigningRequirementsPath);
        Assert.IsTrue(File.Exists(FullPath(ChromeStoreReadinessPath)), ChromeStoreReadinessPath);
        Assert.IsTrue(File.Exists(FullPath(InternalDistributionReadinessPath)), InternalDistributionReadinessPath);
        Assert.IsTrue(File.Exists(FullPath(PrivacySupportDisclosurePath)), PrivacySupportDisclosurePath);
        Assert.IsTrue(File.Exists(FullPath(RollbackPlanPath)), RollbackPlanPath);
        Assert.IsTrue(File.Exists(FullPath(ReleaseNotesDraftPath)), ReleaseNotesDraftPath);
        Assert.IsTrue(File.Exists(FullPath(StoreListingRiskRegisterPath)), StoreListingRiskRegisterPath);
        Assert.IsTrue(File.Exists(FullPath(M646GoNoGoPath)), M646GoNoGoPath);
    }

    [TestMethod]
    public void M647ReportsAndArtifactsExist()
    {
        Assert.IsTrue(File.Exists(FullPath(M647ReportPath)), M647ReportPath);
        Assert.IsTrue(File.Exists(FullPath(ProviderDisabledProofPath)), ProviderDisabledProofPath);
        Assert.IsTrue(File.Exists(FullPath(RuntimeDisabledProofPath)), RuntimeDisabledProofPath);
        Assert.IsTrue(File.Exists(FullPath(FilesystemDisabledProofPath)), FilesystemDisabledProofPath);
        Assert.IsTrue(File.Exists(FullPath(BrowserAutomationDisabledProofPath)), BrowserAutomationDisabledProofPath);
        Assert.IsTrue(File.Exists(FullPath(CapabilityUnlockDisabledProofPath)), CapabilityUnlockDisabledProofPath);
        Assert.IsTrue(File.Exists(FullPath(OpenAiRiskRegisterPath)), OpenAiRiskRegisterPath);
        Assert.IsTrue(File.Exists(FullPath(ProviderPromptNamingDebtPath)), ProviderPromptNamingDebtPath);
        Assert.IsTrue(File.Exists(FullPath(M647GoNoGoPath)), M647GoNoGoPath);
    }

    [TestMethod]
    public void ConsolidatedGoNoGoExists() =>
        Assert.IsTrue(File.Exists(FullPath(ConsolidatedGoNoGoPath)), ConsolidatedGoNoGoPath);

    [TestMethod]
    public void HostPermissionsCurrentStateIsDocumented()
    {
        using var doc = ReadJson(HostPermissionsCurrentStatePath);
        Assert.AreEqual("HOST_PERMISSIONS_CURRENT_STATE_DOCUMENTED", doc.RootElement.GetProperty("decision").GetString());
        CollectionAssert.Contains(doc.RootElement.GetProperty("hostPermissions").EnumerateArray().Select(item => item.GetString()).ToArray(), "http://*/*");
        CollectionAssert.Contains(doc.RootElement.GetProperty("hostPermissions").EnumerateArray().Select(item => item.GetString()).ToArray(), "https://*/*");
        Assert.IsFalse(doc.RootElement.GetProperty("manifestModified").GetBoolean());
        Assert.IsFalse(doc.RootElement.GetProperty("permissionsModified").GetBoolean());
        Assert.IsFalse(doc.RootElement.GetProperty("productFilesModified").GetBoolean());
        Assert.IsFalse(doc.RootElement.GetProperty("publicReleaseReady").GetBoolean());
    }

    [TestMethod]
    public void HostPermissionsRiskAssessmentAndFutureNarrowingPlanExist()
    {
        using var risk = ReadJson(HostPermissionsRiskAssessmentPath);
        using var narrowing = ReadJson(HostPermissionsFutureNarrowingPlanPath);
        Assert.AreEqual("HOST_PERMISSIONS_RISK_ASSESSMENT_READY", risk.RootElement.GetProperty("decision").GetString());
        Assert.AreEqual("justified_for_internal_candidate_open_for_public_release", risk.RootElement.GetProperty("blockerStatus").GetString());
        Assert.IsGreaterThanOrEqualTo(risk.RootElement.GetProperty("risks").GetArrayLength(), 5);
        Assert.AreEqual("HOST_PERMISSIONS_FUTURE_NARROWING_PLAN_READY", narrowing.RootElement.GetProperty("decision").GetString());
        Assert.IsTrue(narrowing.RootElement.GetProperty("futureNarrowingPlanExists").GetBoolean());
        Assert.IsGreaterThanOrEqualTo(narrowing.RootElement.GetProperty("options").GetArrayLength(), 3);
        Assert.IsFalse(narrowing.RootElement.GetProperty("publicReleaseReady").GetBoolean());
    }

    [TestMethod]
    public void HostPermissionsReleaseJustificationKeepsPublicReleaseBlocked()
    {
        using var doc = ReadJson(HostPermissionsReleaseJustificationPath);
        Assert.AreEqual("HOST_PERMISSIONS_RELEASE_JUSTIFICATION_READY", doc.RootElement.GetProperty("decision").GetString());
        Assert.IsTrue(doc.RootElement.GetProperty("hostPermissionsJustificationReady").GetBoolean());
        Assert.AreEqual("justified_for_internal_candidate", doc.RootElement.GetProperty("blockerStatus").GetString());
        Assert.AreEqual("open_for_public_release", doc.RootElement.GetProperty("publicReleaseBlockerStatus").GetString());
        Assert.IsFalse(doc.RootElement.GetProperty("manifestModified").GetBoolean());
        Assert.IsFalse(doc.RootElement.GetProperty("permissionsModified").GetBoolean());
        Assert.IsFalse(doc.RootElement.GetProperty("publicReleaseReady").GetBoolean());
    }

    [TestMethod]
    public void PackagingChecklistAndSigningRequirementsDoNotPackageOrSign()
    {
        using var packaging = ReadJson(PackagingChecklistPath);
        using var signing = ReadJson(SigningRequirementsPath);
        Assert.AreEqual("PACKAGING_CHECKLIST_READY", packaging.RootElement.GetProperty("decision").GetString());
        Assert.IsFalse(packaging.RootElement.GetProperty("packageFinalCreated").GetBoolean());
        Assert.IsFalse(packaging.RootElement.GetProperty("published").GetBoolean());
        Assert.AreEqual("not-finalized", packaging.RootElement.GetProperty("versionCandidateStatus").GetString());
        Assert.AreEqual("SIGNING_REQUIREMENTS_READY", signing.RootElement.GetProperty("decision").GetString());
        Assert.IsFalse(signing.RootElement.GetProperty("signingPerformed").GetBoolean());
        Assert.IsFalse(signing.RootElement.GetProperty("credentialsStoredInRepo").GetBoolean());
        Assert.IsFalse(signing.RootElement.GetProperty("credentialsAllowedInArtifacts").GetBoolean());
    }

    [TestMethod]
    public void ChromeStorePrivacySupportRollbackAndReleaseNotesRemainPrepOnly()
    {
        using var store = ReadJson(ChromeStoreReadinessPath);
        using var privacy = ReadJson(PrivacySupportDisclosurePath);
        using var rollback = ReadJson(RollbackPlanPath);
        using var notes = ReadJson(ReleaseNotesDraftPath);
        Assert.IsFalse(store.RootElement.GetProperty("chromeStoreUploadPerformed").GetBoolean());
        Assert.IsFalse(store.RootElement.GetProperty("storeReviewComplete").GetBoolean());
        Assert.IsTrue(privacy.RootElement.GetProperty("privacyDisclosureDraftReady").GetBoolean());
        Assert.IsTrue(privacy.RootElement.GetProperty("supportUrlRequired").GetBoolean());
        Assert.IsFalse(privacy.RootElement.GetProperty("supportUrlReady").GetBoolean());
        Assert.IsFalse(rollback.RootElement.GetProperty("rollbackPlanReadyForPublicRelease").GetBoolean());
        Assert.IsTrue(notes.RootElement.GetProperty("releaseNotesDraftReady").GetBoolean());
        Assert.IsFalse(notes.RootElement.GetProperty("releaseNotesFinal").GetBoolean());
    }

    [TestMethod]
    public void ProviderDisabledProofExistsAndKeepsProviderCloudBlocked()
    {
        using var doc = ReadJson(ProviderDisabledProofPath);
        Assert.AreEqual("PROVIDER_DISABLED_STATE_PROOF_READY", doc.RootElement.GetProperty("decision").GetString());
        Assert.IsFalse(doc.RootElement.GetProperty("providerCloudEnabled").GetBoolean());
        Assert.IsFalse(doc.RootElement.GetProperty("byokEnabled").GetBoolean());
        Assert.IsFalse(doc.RootElement.GetProperty("apiKeyInputEnabled").GetBoolean());
        Assert.IsFalse(doc.RootElement.GetProperty("providerCallEnabled").GetBoolean());
        Assert.IsFalse(doc.RootElement.GetProperty("promptExecutionEnabled").GetBoolean());
        Assert.IsTrue(doc.RootElement.GetProperty("openAiAgentClientPresent").GetBoolean());
    }

    [TestMethod]
    public void RuntimeDisabledProofExistsAndKeepsRuntimeBlocked()
    {
        using var doc = ReadJson(RuntimeDisabledProofPath);
        Assert.AreEqual("RUNTIME_DISABLED_STATE_PROOF_READY", doc.RootElement.GetProperty("decision").GetString());
        Assert.IsFalse(doc.RootElement.GetProperty("runtimeProductiveEnabled").GetBoolean());
        Assert.IsFalse(doc.RootElement.GetProperty("runtimeExecutionEnabled").GetBoolean());
        Assert.IsFalse(doc.RootElement.GetProperty("runtimeUnlockEnabled").GetBoolean());
        Assert.IsFalse(doc.RootElement.GetProperty("approvalExecutionEnabled").GetBoolean());
        Assert.IsFalse(doc.RootElement.GetProperty("capabilityExecutionEnabled").GetBoolean());
        Assert.IsTrue(doc.RootElement.GetProperty("evidenceGateDoesNotUnlockRuntime").GetBoolean());
    }

    [TestMethod]
    public void FilesystemBrowserAndCapabilityDisabledProofsExist()
    {
        using var fs = ReadJson(FilesystemDisabledProofPath);
        using var browser = ReadJson(BrowserAutomationDisabledProofPath);
        using var capability = ReadJson(CapabilityUnlockDisabledProofPath);
        Assert.IsFalse(fs.RootElement.GetProperty("filesystemEnabled").GetBoolean());
        Assert.IsFalse(fs.RootElement.GetProperty("filePickerEnabled").GetBoolean());
        Assert.IsFalse(fs.RootElement.GetProperty("workspaceScanEnabled").GetBoolean());
        Assert.IsFalse(fs.RootElement.GetProperty("filesystemReadEnabled").GetBoolean());
        Assert.IsFalse(fs.RootElement.GetProperty("filesystemWriteEnabled").GetBoolean());
        Assert.IsFalse(browser.RootElement.GetProperty("browserAutomationEnabled").GetBoolean());
        Assert.IsFalse(browser.RootElement.GetProperty("connectorActionsEnabled").GetBoolean());
        Assert.IsFalse(capability.RootElement.GetProperty("capabilityUnlockEnabled").GetBoolean());
        Assert.IsTrue(capability.RootElement.GetProperty("releaseEvidenceGateDoesNotUnlockCapabilities").GetBoolean());
    }

    [TestMethod]
    public void OpenAiAgentClientRiskAndPromptNamingDebtRemainDocumented()
    {
        using var risk = ReadJson(OpenAiRiskRegisterPath);
        using var debt = ReadJson(ProviderPromptNamingDebtPath);
        Assert.AreEqual("OPENAI_AGENT_CLIENT_RISK_REGISTER_READY", risk.RootElement.GetProperty("decision").GetString());
        Assert.IsTrue(risk.RootElement.GetProperty("openAiAgentClientPresent").GetBoolean());
        Assert.IsTrue(risk.RootElement.GetProperty("providerReleaseGateRequired").GetBoolean());
        Assert.IsFalse(risk.RootElement.GetProperty("providerRuntimeReady").GetBoolean());
        Assert.AreEqual("PROVIDER_PROMPT_NAMING_DEBT_DOCUMENTED", debt.RootElement.GetProperty("decision").GetString());
        Assert.AreEqual("open", debt.RootElement.GetProperty("providerPromptNamingDebt").GetString());
        Assert.IsTrue(debt.RootElement.GetProperty("requiresFutureProviderGate").GetBoolean());
        Assert.IsFalse(debt.RootElement.GetProperty("publicReleaseReady").GetBoolean());
    }

    [TestMethod]
    public void ConsolidatedGoNoGoKeepsAllSensitivePathsBlocked()
    {
        using var doc = ReadJson(ConsolidatedGoNoGoPath);
        Assert.AreEqual("PUBLIC_RELEASE_BLOCKERS_EVIDENCE_PREP_READY", doc.RootElement.GetProperty("decision").GetString());
        Assert.IsTrue(doc.RootElement.GetProperty("hostPermissionsJustificationReady").GetBoolean());
        Assert.IsTrue(doc.RootElement.GetProperty("packagingStoreEvidencePrepReady").GetBoolean());
        Assert.IsTrue(doc.RootElement.GetProperty("providerRuntimeDisabledStateProofReady").GetBoolean());
        Assert.IsFalse(doc.RootElement.GetProperty("productFilesModified").GetBoolean());
        Assert.IsFalse(doc.RootElement.GetProperty("manifestModified").GetBoolean());
        Assert.IsFalse(doc.RootElement.GetProperty("permissionsModified").GetBoolean());
        Assert.IsFalse(doc.RootElement.GetProperty("hostPermissionsModified").GetBoolean());
        Assert.IsFalse(doc.RootElement.GetProperty("jsModified").GetBoolean());
        Assert.IsFalse(doc.RootElement.GetProperty("bridgeModified").GetBoolean());
        Assert.IsFalse(doc.RootElement.GetProperty("runtimeProductiveEnabled").GetBoolean());
        Assert.IsFalse(doc.RootElement.GetProperty("providerCloudEnabled").GetBoolean());
        Assert.IsFalse(doc.RootElement.GetProperty("filesystemEnabled").GetBoolean());
        Assert.IsFalse(doc.RootElement.GetProperty("browserAutomationEnabled").GetBoolean());
        Assert.IsFalse(doc.RootElement.GetProperty("capabilityUnlockEnabled").GetBoolean());
        Assert.IsTrue(doc.RootElement.GetProperty("releaseEvidenceGateReady").GetBoolean());
        Assert.IsFalse(doc.RootElement.GetProperty("publicReleaseReady").GetBoolean());
        Assert.AreEqual("M648 Public Release Closure Audit / Final Go-No-Go Review", doc.RootElement.GetProperty("recommendedNextMilestone").GetString());
    }

    [TestMethod]
    public void M645M647GoNoGosRemainReleaseBlocked()
    {
        using var m645 = ReadJson(M645GoNoGoPath);
        using var m646 = ReadJson(M646GoNoGoPath);
        using var m647 = ReadJson(M647GoNoGoPath);
        Assert.IsFalse(m645.RootElement.GetProperty("publicReleaseReady").GetBoolean());
        Assert.IsFalse(m645.RootElement.GetProperty("manifestModified").GetBoolean());
        Assert.IsFalse(m645.RootElement.GetProperty("permissionsModified").GetBoolean());
        Assert.IsFalse(m646.RootElement.GetProperty("packageFinalCreated").GetBoolean());
        Assert.IsFalse(m646.RootElement.GetProperty("signingPerformed").GetBoolean());
        Assert.IsFalse(m646.RootElement.GetProperty("chromeStoreUploadPerformed").GetBoolean());
        Assert.IsFalse(m646.RootElement.GetProperty("published").GetBoolean());
        Assert.IsFalse(m647.RootElement.GetProperty("providerCloudEnabled").GetBoolean());
        Assert.IsFalse(m647.RootElement.GetProperty("runtimeProductiveEnabled").GetBoolean());
        Assert.IsFalse(m647.RootElement.GetProperty("filesystemEnabled").GetBoolean());
        Assert.IsFalse(m647.RootElement.GetProperty("browserAutomationEnabled").GetBoolean());
        Assert.IsFalse(m647.RootElement.GetProperty("capabilityUnlockEnabled").GetBoolean());
    }

    [TestMethod]
    public void ManifestJsonUnchanged()
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
