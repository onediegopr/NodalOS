using System.Security.Cryptography;
using System.Text.Json;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace OneBrain.Safety.Tests;

[TestClass]
[TestCategory("PublicPackageCandidatePrep")]
[TestCategory("M664")]
[TestCategory("M665")]
[TestCategory("M666")]
[TestCategory("M667")]
public sealed class NodalOsPublicPackageCandidatePrepM664M667Tests
{
    private const string M664ReportPath = "docs/reports/m664-public-variant-readiness-fixes.md";
    private const string M665ReportPath = "docs/reports/m665-public-package-candidate-prep.md";
    private const string M666ReportPath = "docs/reports/m666-public-variant-manual-extension-qa-contract.md";
    private const string M667ReportPath = "docs/reports/m667-store-disclosure-prep.md";

    private const string PublicVariantReadinessFixesPath = "artifacts/agent-operations/m664/public-variant-readiness-fixes.json";
    private const string KnownLimitationsPath = "artifacts/agent-operations/m664/public-variant-known-limitations.json";
    private const string MicrocopyContractPath = "artifacts/agent-operations/m664/public-variant-microcopy-contract.json";
    private const string PermissionJustificationPath = "artifacts/agent-operations/m664/public-permission-justification.json";
    private const string MetadataNamingReviewPath = "artifacts/agent-operations/m664/public-metadata-naming-review.json";
    private const string M664GoNoGoPath = "artifacts/agent-operations/m664/m664-go-no-go.json";

    private const string PackageCandidatePrepPath = "artifacts/agent-operations/m665/public-package-candidate-prep.json";
    private const string ManifestSelectionVerificationPath = "artifacts/agent-operations/m665/manifest-selection-verification.json";
    private const string PackageSelectorRunbookPath = "artifacts/agent-operations/m665/package-selector-runbook.json";
    private const string PublicPackageContentsManifestPath = "artifacts/agent-operations/m665/public-package-contents-manifest.json";
    private const string InternalPackageBoundaryProofPath = "artifacts/agent-operations/m665/internal-package-boundary-proof.json";
    private const string M665GoNoGoPath = "artifacts/agent-operations/m665/m665-go-no-go.json";

    private const string ManualQaContractPath = "artifacts/agent-operations/m666/manual-extension-qa-contract.json";
    private const string PublicVariantLoadChecklistPath = "artifacts/agent-operations/m666/public-variant-load-checklist.json";
    private const string RuntimeTabQaChecklistPath = "artifacts/agent-operations/m666/runtime-tab-qa-checklist.json";
    private const string ServiceWorkerDevtoolsQaChecklistPath = "artifacts/agent-operations/m666/service-worker-devtools-qa-checklist.json";
    private const string AllowedDisallowedOriginQaChecklistPath = "artifacts/agent-operations/m666/allowed-disallowed-origin-qa-checklist.json";
    private const string ManualQaEvidenceTemplatePath = "artifacts/agent-operations/m666/manual-qa-evidence-template.json";
    private const string M666GoNoGoPath = "artifacts/agent-operations/m666/m666-go-no-go.json";

    private const string StoreDisclosurePrepPath = "artifacts/agent-operations/m667/store-disclosure-prep.json";
    private const string PermissionDisclosureDraftPath = "artifacts/agent-operations/m667/permission-disclosure-draft.json";
    private const string PrivacyDisclosureDraftPath = "artifacts/agent-operations/m667/privacy-disclosure-draft.json";
    private const string SupportDisclosureDraftPath = "artifacts/agent-operations/m667/support-disclosure-draft.json";
    private const string ListingGapRegisterPath = "artifacts/agent-operations/m667/public-listing-readiness-gap-register.json";
    private const string M667GoNoGoPath = "artifacts/agent-operations/m667/m667-go-no-go.json";

    private const string ConsolidatedGoNoGoPath = "artifacts/agent-operations/m664-m667/public-package-candidate-prep-go-no-go.json";

    private const string InternalManifestPath = "browser-extension/onebrain-chrome-lab/manifest.json";
    private const string PublicManifestPath = "browser-extension/onebrain-chrome-lab/manifest.public.json";
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

    private static string[] StringArray(JsonElement element) =>
        element.EnumerateArray().Select(item => item.GetString() ?? string.Empty).ToArray();

    [TestMethod]
    public void M664ArtifactsExist()
    {
        Assert.IsTrue(File.Exists(FullPath(M664ReportPath)), M664ReportPath);
        Assert.IsTrue(File.Exists(FullPath(PublicVariantReadinessFixesPath)), PublicVariantReadinessFixesPath);
        Assert.IsTrue(File.Exists(FullPath(KnownLimitationsPath)), KnownLimitationsPath);
        Assert.IsTrue(File.Exists(FullPath(MicrocopyContractPath)), MicrocopyContractPath);
        Assert.IsTrue(File.Exists(FullPath(PermissionJustificationPath)), PermissionJustificationPath);
        Assert.IsTrue(File.Exists(FullPath(MetadataNamingReviewPath)), MetadataNamingReviewPath);
        Assert.IsTrue(File.Exists(FullPath(M664GoNoGoPath)), M664GoNoGoPath);
    }

    [TestMethod]
    public void M665ArtifactsExist()
    {
        Assert.IsTrue(File.Exists(FullPath(M665ReportPath)), M665ReportPath);
        Assert.IsTrue(File.Exists(FullPath(PackageCandidatePrepPath)), PackageCandidatePrepPath);
        Assert.IsTrue(File.Exists(FullPath(ManifestSelectionVerificationPath)), ManifestSelectionVerificationPath);
        Assert.IsTrue(File.Exists(FullPath(PackageSelectorRunbookPath)), PackageSelectorRunbookPath);
        Assert.IsTrue(File.Exists(FullPath(PublicPackageContentsManifestPath)), PublicPackageContentsManifestPath);
        Assert.IsTrue(File.Exists(FullPath(InternalPackageBoundaryProofPath)), InternalPackageBoundaryProofPath);
        Assert.IsTrue(File.Exists(FullPath(M665GoNoGoPath)), M665GoNoGoPath);
    }

    [TestMethod]
    public void M666ArtifactsExist()
    {
        Assert.IsTrue(File.Exists(FullPath(M666ReportPath)), M666ReportPath);
        Assert.IsTrue(File.Exists(FullPath(ManualQaContractPath)), ManualQaContractPath);
        Assert.IsTrue(File.Exists(FullPath(PublicVariantLoadChecklistPath)), PublicVariantLoadChecklistPath);
        Assert.IsTrue(File.Exists(FullPath(RuntimeTabQaChecklistPath)), RuntimeTabQaChecklistPath);
        Assert.IsTrue(File.Exists(FullPath(ServiceWorkerDevtoolsQaChecklistPath)), ServiceWorkerDevtoolsQaChecklistPath);
        Assert.IsTrue(File.Exists(FullPath(AllowedDisallowedOriginQaChecklistPath)), AllowedDisallowedOriginQaChecklistPath);
        Assert.IsTrue(File.Exists(FullPath(ManualQaEvidenceTemplatePath)), ManualQaEvidenceTemplatePath);
        Assert.IsTrue(File.Exists(FullPath(M666GoNoGoPath)), M666GoNoGoPath);
    }

    [TestMethod]
    public void M667ArtifactsExist()
    {
        Assert.IsTrue(File.Exists(FullPath(M667ReportPath)), M667ReportPath);
        Assert.IsTrue(File.Exists(FullPath(StoreDisclosurePrepPath)), StoreDisclosurePrepPath);
        Assert.IsTrue(File.Exists(FullPath(PermissionDisclosureDraftPath)), PermissionDisclosureDraftPath);
        Assert.IsTrue(File.Exists(FullPath(PrivacyDisclosureDraftPath)), PrivacyDisclosureDraftPath);
        Assert.IsTrue(File.Exists(FullPath(SupportDisclosureDraftPath)), SupportDisclosureDraftPath);
        Assert.IsTrue(File.Exists(FullPath(ListingGapRegisterPath)), ListingGapRegisterPath);
        Assert.IsTrue(File.Exists(FullPath(M667GoNoGoPath)), M667GoNoGoPath);
    }

    [TestMethod]
    public void ConsolidatedM664M667GoNoGoExists() =>
        Assert.IsTrue(File.Exists(FullPath(ConsolidatedGoNoGoPath)), ConsolidatedGoNoGoPath);

    [TestMethod]
    public void M664AcceptsAuditAndPreparesLimitationsMicrocopyAndPermissions()
    {
        using var fixes = ReadJson(PublicVariantReadinessFixesPath);
        using var limitations = ReadJson(KnownLimitationsPath);
        using var microcopy = ReadJson(MicrocopyContractPath);
        using var permissions = ReadJson(PermissionJustificationPath);
        using var naming = ReadJson(MetadataNamingReviewPath);
        Assert.AreEqual("AUDIT_CONDITIONAL_GO", fixes.RootElement.GetProperty("auditDecisionAccepted").GetString());
        Assert.IsTrue(limitations.RootElement.GetProperty("publicContentScriptsKnownLimitationDocumented").GetBoolean());
        Assert.IsTrue(microcopy.RootElement.GetProperty("microcopyOrKnownLimitationsPrepared").GetBoolean());
        Assert.IsFalse(microcopy.RootElement.GetProperty("uiModifiedNow").GetBoolean());
        Assert.AreEqual("PUBLIC_PERMISSION_JUSTIFICATION_READY", permissions.RootElement.GetProperty("decision").GetString());
        Assert.IsTrue(permissions.RootElement.GetProperty("permissionJustificationPrepared").GetBoolean());
        Assert.IsFalse(permissions.RootElement.GetProperty("optionalHostPermissionsImplementedNow").GetBoolean());
        Assert.IsTrue(naming.RootElement.GetProperty("currentNameValidForQa").GetBoolean());
        Assert.IsFalse(naming.RootElement.GetProperty("currentNameApprovedForStoreFinal").GetBoolean());
    }

    [TestMethod]
    public void M665VerifiesManifestSelectionAndInternalBoundary()
    {
        using var prep = ReadJson(PackageCandidatePrepPath);
        using var selection = ReadJson(ManifestSelectionVerificationPath);
        using var runbook = ReadJson(PackageSelectorRunbookPath);
        using var contents = ReadJson(PublicPackageContentsManifestPath);
        using var boundary = ReadJson(InternalPackageBoundaryProofPath);
        Assert.IsTrue(prep.RootElement.GetProperty("publicPackageCandidatePrepared").GetBoolean());
        Assert.IsTrue(prep.RootElement.GetProperty("manifestSelectionVerified").GetBoolean());
        Assert.IsFalse(prep.RootElement.GetProperty("finalSignedPackageCreated").GetBoolean());
        Assert.IsFalse(prep.RootElement.GetProperty("chromeWebStoreUploadPerformed").GetBoolean());
        Assert.IsTrue(selection.RootElement.GetProperty("publicManifestUsedForPublicPackageCandidate").GetBoolean());
        Assert.IsTrue(selection.RootElement.GetProperty("internalManifestPreserved").GetBoolean());
        Assert.AreEqual("PACKAGE_SELECTOR_RUNBOOK_READY", runbook.RootElement.GetProperty("decision").GetString());
        Assert.IsTrue(contents.RootElement.GetProperty("checksumsPlanRequired").GetBoolean());
        Assert.IsTrue(contents.RootElement.GetProperty("secretsExclusionRequired").GetBoolean());
        Assert.IsFalse(boundary.RootElement.GetProperty("internalManifestReplacedByPublicManifest").GetBoolean());
    }

    [TestMethod]
    public void M666ManualQaContractRequiresEvidenceAndRedaction()
    {
        using var contract = ReadJson(ManualQaContractPath);
        using var load = ReadJson(PublicVariantLoadChecklistPath);
        using var runtime = ReadJson(RuntimeTabQaChecklistPath);
        using var devtools = ReadJson(ServiceWorkerDevtoolsQaChecklistPath);
        using var origins = ReadJson(AllowedDisallowedOriginQaChecklistPath);
        using var template = ReadJson(ManualQaEvidenceTemplatePath);
        Assert.IsTrue(contract.RootElement.GetProperty("manualQaContractPrepared").GetBoolean());
        Assert.IsFalse(contract.RootElement.GetProperty("manualQaExecutedNow").GetBoolean());
        Assert.IsTrue(contract.RootElement.GetProperty("manualQaRequiredBeforePublicRelease").GetBoolean());
        Assert.AreEqual("PUBLIC_VARIANT_LOAD_CHECKLIST_READY", load.RootElement.GetProperty("decision").GetString());
        Assert.AreEqual("RUNTIME_TAB_QA_CHECKLIST_READY", runtime.RootElement.GetProperty("decision").GetString());
        Assert.IsFalse(runtime.RootElement.GetProperty("runtimeProductiveEnabled").GetBoolean());
        Assert.IsFalse(devtools.RootElement.GetProperty("rawConsoleExcerptAllowed").GetBoolean());
        Assert.AreEqual("ALLOWED_DISALLOWED_ORIGIN_QA_CHECKLIST_READY", origins.RootElement.GetProperty("decision").GetString());
        Assert.IsFalse(template.RootElement.GetProperty("template").GetProperty("rawSecretsIncluded").GetBoolean());
        Assert.IsFalse(template.RootElement.GetProperty("template").GetProperty("rawConsoleExcerptAllowed").GetBoolean());
    }

    [TestMethod]
    public void M667StoreDisclosureDraftsRemainNoGoForStore()
    {
        using var prep = ReadJson(StoreDisclosurePrepPath);
        using var permission = ReadJson(PermissionDisclosureDraftPath);
        using var privacy = ReadJson(PrivacyDisclosureDraftPath);
        using var support = ReadJson(SupportDisclosureDraftPath);
        using var gaps = ReadJson(ListingGapRegisterPath);
        Assert.IsTrue(prep.RootElement.GetProperty("storeDisclosurePrepared").GetBoolean());
        Assert.IsFalse(prep.RootElement.GetProperty("storeDisclosureFinalReady").GetBoolean());
        Assert.IsFalse(prep.RootElement.GetProperty("publicReleaseReady").GetBoolean());
        Assert.IsFalse(prep.RootElement.GetProperty("chromeWebStoreReady").GetBoolean());
        Assert.AreEqual("PERMISSION_DISCLOSURE_DRAFT_READY", permission.RootElement.GetProperty("decision").GetString());
        Assert.IsTrue(privacy.RootElement.GetProperty("draft").GetProperty("localFirst").GetBoolean());
        Assert.IsFalse(privacy.RootElement.GetProperty("draft").GetProperty("providerCloudEnabled").GetBoolean());
        Assert.IsFalse(support.RootElement.GetProperty("supportUrlFinalReady").GetBoolean());
        Assert.IsGreaterThanOrEqualTo(5, gaps.RootElement.GetProperty("gaps").GetArrayLength());
    }

    [TestMethod]
    public void ConsolidatedGoNoGoMatchesExpectedM664M667Decision()
    {
        using var doc = ReadJson(ConsolidatedGoNoGoPath);
        Assert.AreEqual("PUBLIC_PACKAGE_CANDIDATE_PREP_READY", doc.RootElement.GetProperty("decision").GetString());
        Assert.AreEqual("AUDIT_CONDITIONAL_GO", doc.RootElement.GetProperty("auditDecisionAccepted").GetString());
        Assert.IsTrue(doc.RootElement.GetProperty("internalCandidateReady").GetBoolean());
        Assert.IsTrue(doc.RootElement.GetProperty("publicBuildCandidateReady").GetBoolean());
        Assert.IsTrue(doc.RootElement.GetProperty("publicPackageCandidatePrepared").GetBoolean());
        Assert.IsFalse(doc.RootElement.GetProperty("publicReleaseReady").GetBoolean());
        Assert.IsFalse(doc.RootElement.GetProperty("chromeWebStoreReady").GetBoolean());
        Assert.IsTrue(doc.RootElement.GetProperty("manifestSelectionVerified").GetBoolean());
        Assert.IsTrue(doc.RootElement.GetProperty("publicManifestUsedForPublicPackageCandidate").GetBoolean());
        Assert.IsTrue(doc.RootElement.GetProperty("internalManifestPreserved").GetBoolean());
        Assert.IsTrue(doc.RootElement.GetProperty("publicHostPermissionsNarrowed").GetBoolean());
        Assert.IsTrue(doc.RootElement.GetProperty("publicContentScriptsKnownLimitationDocumented").GetBoolean());
        Assert.IsTrue(doc.RootElement.GetProperty("permissionJustificationPrepared").GetBoolean());
        Assert.IsTrue(doc.RootElement.GetProperty("microcopyOrKnownLimitationsPrepared").GetBoolean());
        Assert.IsTrue(doc.RootElement.GetProperty("manualQaContractPrepared").GetBoolean());
        Assert.IsTrue(doc.RootElement.GetProperty("manualQaRequiredBeforePublicRelease").GetBoolean());
        Assert.IsTrue(doc.RootElement.GetProperty("storeDisclosurePrepared").GetBoolean());
        Assert.IsFalse(doc.RootElement.GetProperty("runtimeProductiveEnabled").GetBoolean());
        Assert.IsFalse(doc.RootElement.GetProperty("providerCloudEnabled").GetBoolean());
        Assert.IsFalse(doc.RootElement.GetProperty("filesystemEnabled").GetBoolean());
        Assert.IsFalse(doc.RootElement.GetProperty("browserAutomationEnabled").GetBoolean());
        Assert.IsFalse(doc.RootElement.GetProperty("capabilityUnlockEnabled").GetBoolean());
        Assert.IsFalse(doc.RootElement.GetProperty("bridgeModified").GetBoolean());
        Assert.IsFalse(doc.RootElement.GetProperty("cspModified").GetBoolean());
        Assert.AreEqual("M668-M670 Public Variant Manual QA Execution + Evidence Capture + Final Package Audit", doc.RootElement.GetProperty("recommendedNextMilestone").GetString());
    }

    [TestMethod]
    public void M664M667GoNoGosKeepReleaseRuntimeAndStoreBlocked()
    {
        foreach (var path in new[] { M664GoNoGoPath, M665GoNoGoPath, M666GoNoGoPath, M667GoNoGoPath })
        {
            using var doc = ReadJson(path);
            Assert.IsFalse(doc.RootElement.GetProperty("publicReleaseReady").GetBoolean(), path);
            Assert.IsFalse(doc.RootElement.GetProperty("chromeWebStoreReady").GetBoolean(), path);
            Assert.IsFalse(doc.RootElement.GetProperty("runtimeProductiveEnabled").GetBoolean(), path);
            Assert.IsFalse(doc.RootElement.GetProperty("providerCloudEnabled").GetBoolean(), path);
            Assert.IsFalse(doc.RootElement.GetProperty("filesystemEnabled").GetBoolean(), path);
            Assert.IsFalse(doc.RootElement.GetProperty("browserAutomationEnabled").GetBoolean(), path);
            Assert.IsFalse(doc.RootElement.GetProperty("capabilityUnlockEnabled").GetBoolean(), path);
        }
    }

    [TestMethod]
    public void ManifestPublicJsonRemainsValidAndNarrowed()
    {
        using var doc = ReadJson(PublicManifestPath);
        Assert.AreEqual(3, doc.RootElement.GetProperty("manifest_version").GetInt32());
        var hostPermissions = StringArray(doc.RootElement.GetProperty("host_permissions"));
        CollectionAssert.DoesNotContain(hostPermissions, "http://*/*");
        CollectionAssert.DoesNotContain(hostPermissions, "https://*/*");
        CollectionAssert.Contains(hostPermissions, "http://127.0.0.1/*");
        CollectionAssert.Contains(hostPermissions, "http://localhost/*");

        if (!doc.RootElement.TryGetProperty("content_scripts", out var contentScripts))
            return;

        foreach (var contentScript in contentScripts.EnumerateArray())
        {
            if (!contentScript.TryGetProperty("matches", out var matches))
                continue;

            var matchValues = StringArray(matches);
            CollectionAssert.DoesNotContain(matchValues, "http://*/*");
            CollectionAssert.DoesNotContain(matchValues, "https://*/*");
        }
    }

    [TestMethod]
    public void InternalManifestJsonUnchanged()
    {
        Assert.AreEqual("76859A171D0FBC585E96253D0F269AEF4A54DCFD5F704DE9DB92D26EB1AEDDFD", Sha256Hex(InternalManifestPath));
    }

    [TestMethod]
    public void PublicManifestJsonUnchangedFromM661()
    {
        Assert.AreEqual("CB89F7EE9E39BE0BB5B04E062D3D0A059BEB236951CBAA16DA0C325933965A40", Sha256Hex(PublicManifestPath));
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
        Assert.AreEqual("FED938DE2C42EC56F9061E2587A57338DAD1A770BBFAD2B710937BBD97D9D329", Sha256Hex(SidepanelJsPath));
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
        Assert.AreEqual("D4E043EC6EC181D780FD2A18AAC48BB39FED4F9078E03229BF5F718635031712", Sha256Hex(BridgeOptionsPath));
        Assert.AreEqual("A22DAE8449A297DC77E5E0E28D404DA253846BEC541033F2E0EF6AC779562624", Sha256Hex(BridgeProtocolPath));
        Assert.AreEqual("5E933A21C501487523C60197F02E0D68CE6D0F5865DC9401F9337EBA41B58B37", Sha256Hex(BridgeSecurityPath));
        Assert.AreEqual("8CF05765736DEF46584022F1E33E22CD4E85A3C08A952048D425FBBC1EFE5309", Sha256Hex(BridgeSelfTestPath));
        Assert.AreEqual("CAACC05544B9E41F7606BD17ECAD81FB36842869BC34E46F8596D4D62EB5B5C4", Sha256Hex(BridgeOpenAiPath));
        Assert.AreEqual("061CED72CF27BC31C0BAD240BC698B58D0D5966D6D107CE8A315C98D52DB1305", Sha256Hex(BridgeProgramPath));
        Assert.AreEqual("E00407FA4BC6DCB8ADBB677E5FE4AF41FE7BDB967C5827EBF54569E6DF32E85D", Sha256Hex(BridgeProjectPath));
    }
}
