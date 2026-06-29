using System.Security.Cryptography;
using System.Text.Json;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace OneBrain.Safety.Tests;

[TestClass]
[TestCategory("PublicManifestVariantPatch")]
[TestCategory("M661")]
[TestCategory("M662")]
[TestCategory("M663")]
public sealed class NodalOsPublicManifestVariantPatchM661M663Tests
{
    private const string M661ReportPath = "docs/reports/m661-public-manifest-variant-patch.md";
    private const string M662ReportPath = "docs/reports/m662-host-permissions-narrowing.md";
    private const string M663ReportPath = "docs/reports/m663-public-manifest-regression-qa.md";

    private const string PublicManifestPath = "browser-extension/onebrain-chrome-lab/manifest.public.json";
    private const string InternalManifestPath = "browser-extension/onebrain-chrome-lab/manifest.json";

    private const string PublicManifestVariantPatchPath = "artifacts/agent-operations/m661/public-manifest-variant-patch.json";
    private const string InternalManifestBaselineProofPath = "artifacts/agent-operations/m661/internal-manifest-baseline-proof.json";
    private const string PublicManifestVariantProofPath = "artifacts/agent-operations/m661/public-manifest-variant-proof.json";
    private const string ManifestVariantDiffSummaryPath = "artifacts/agent-operations/m661/manifest-variant-diff-summary.json";
    private const string M661GoNoGoPath = "artifacts/agent-operations/m661/m661-go-no-go.json";

    private const string HostPermissionsNarrowingProofPath = "artifacts/agent-operations/m662/host-permissions-narrowing-proof.json";
    private const string ContentScriptMatchNarrowingProofPath = "artifacts/agent-operations/m662/content-script-match-narrowing-proof.json";
    private const string OptionalPermissionsEvaluationPath = "artifacts/agent-operations/m662/optional-permissions-evaluation.json";
    private const string PublicBuildFeatureImpactPath = "artifacts/agent-operations/m662/public-build-feature-impact-after-narrowing.json";
    private const string M662GoNoGoPath = "artifacts/agent-operations/m662/m662-go-no-go.json";

    private const string InternalBuildRegressionProofPath = "artifacts/agent-operations/m663/internal-build-regression-proof.json";
    private const string PublicVariantStaticValidationPath = "artifacts/agent-operations/m663/public-variant-static-validation.json";
    private const string PublicVariantRuntimeQaPlanPath = "artifacts/agent-operations/m663/public-variant-runtime-qa-plan.json";
    private const string PublicReleaseNoGoProofPath = "artifacts/agent-operations/m663/public-release-no-go-proof.json";
    private const string RollbackReadinessProofPath = "artifacts/agent-operations/m663/rollback-readiness-proof.json";
    private const string M663GoNoGoPath = "artifacts/agent-operations/m663/m663-go-no-go.json";

    private const string ConsolidatedGoNoGoPath = "artifacts/agent-operations/m661-m663/public-manifest-variant-patch-go-no-go.json";

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
    public void M661ArtifactsExist()
    {
        Assert.IsTrue(File.Exists(FullPath(M661ReportPath)), M661ReportPath);
        Assert.IsTrue(File.Exists(FullPath(PublicManifestVariantPatchPath)), PublicManifestVariantPatchPath);
        Assert.IsTrue(File.Exists(FullPath(InternalManifestBaselineProofPath)), InternalManifestBaselineProofPath);
        Assert.IsTrue(File.Exists(FullPath(PublicManifestVariantProofPath)), PublicManifestVariantProofPath);
        Assert.IsTrue(File.Exists(FullPath(ManifestVariantDiffSummaryPath)), ManifestVariantDiffSummaryPath);
        Assert.IsTrue(File.Exists(FullPath(M661GoNoGoPath)), M661GoNoGoPath);
    }

    [TestMethod]
    public void M662ArtifactsExist()
    {
        Assert.IsTrue(File.Exists(FullPath(M662ReportPath)), M662ReportPath);
        Assert.IsTrue(File.Exists(FullPath(HostPermissionsNarrowingProofPath)), HostPermissionsNarrowingProofPath);
        Assert.IsTrue(File.Exists(FullPath(ContentScriptMatchNarrowingProofPath)), ContentScriptMatchNarrowingProofPath);
        Assert.IsTrue(File.Exists(FullPath(OptionalPermissionsEvaluationPath)), OptionalPermissionsEvaluationPath);
        Assert.IsTrue(File.Exists(FullPath(PublicBuildFeatureImpactPath)), PublicBuildFeatureImpactPath);
        Assert.IsTrue(File.Exists(FullPath(M662GoNoGoPath)), M662GoNoGoPath);
    }

    [TestMethod]
    public void M663ArtifactsExist()
    {
        Assert.IsTrue(File.Exists(FullPath(M663ReportPath)), M663ReportPath);
        Assert.IsTrue(File.Exists(FullPath(InternalBuildRegressionProofPath)), InternalBuildRegressionProofPath);
        Assert.IsTrue(File.Exists(FullPath(PublicVariantStaticValidationPath)), PublicVariantStaticValidationPath);
        Assert.IsTrue(File.Exists(FullPath(PublicVariantRuntimeQaPlanPath)), PublicVariantRuntimeQaPlanPath);
        Assert.IsTrue(File.Exists(FullPath(PublicReleaseNoGoProofPath)), PublicReleaseNoGoProofPath);
        Assert.IsTrue(File.Exists(FullPath(RollbackReadinessProofPath)), RollbackReadinessProofPath);
        Assert.IsTrue(File.Exists(FullPath(M663GoNoGoPath)), M663GoNoGoPath);
    }

    [TestMethod]
    public void ConsolidatedM661M663GoNoGoExists() =>
        Assert.IsTrue(File.Exists(FullPath(ConsolidatedGoNoGoPath)), ConsolidatedGoNoGoPath);

    [TestMethod]
    public void PublicManifestVariantExistsAndIsValidJson()
    {
        Assert.IsTrue(File.Exists(FullPath(PublicManifestPath)), PublicManifestPath);
        using var doc = ReadJson(PublicManifestPath);
        Assert.AreEqual(3, doc.RootElement.GetProperty("manifest_version").GetInt32());
        Assert.AreEqual("NODAL OS Public Candidate", doc.RootElement.GetProperty("name").GetString());
    }

    [TestMethod]
    public void PublicManifestVariantHasNoWildcardHostPermissions()
    {
        using var doc = ReadJson(PublicManifestPath);
        var hostPermissions = StringArray(doc.RootElement.GetProperty("host_permissions"));
        CollectionAssert.DoesNotContain(hostPermissions, "http://*/*");
        CollectionAssert.DoesNotContain(hostPermissions, "https://*/*");
        CollectionAssert.Contains(hostPermissions, "http://127.0.0.1/*");
        CollectionAssert.Contains(hostPermissions, "http://localhost/*");
    }

    [TestMethod]
    public void PublicManifestVariantHasNoWildcardContentScriptMatches()
    {
        using var doc = ReadJson(PublicManifestPath);
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
    public void InternalManifestBaselineProofPreservesInternalCandidate()
    {
        using var doc = ReadJson(InternalManifestBaselineProofPath);
        Assert.AreEqual("INTERNAL_MANIFEST_BASELINE_PROOF_READY", doc.RootElement.GetProperty("decision").GetString());
        Assert.IsTrue(doc.RootElement.GetProperty("internalManifestPreserved").GetBoolean());
        Assert.IsTrue(doc.RootElement.GetProperty("internalCandidateReady").GetBoolean());
        Assert.IsTrue(doc.RootElement.GetProperty("internalWildcardPermissionsPreservedForInternalCandidate").GetBoolean());
        Assert.IsFalse(doc.RootElement.GetProperty("publicReleaseReady").GetBoolean());
    }

    [TestMethod]
    public void PublicManifestVariantProofShowsNarrowing()
    {
        using var doc = ReadJson(PublicManifestVariantProofPath);
        Assert.AreEqual("PUBLIC_MANIFEST_VARIANT_PROOF_READY", doc.RootElement.GetProperty("decision").GetString());
        Assert.IsTrue(doc.RootElement.GetProperty("publicManifestVariantCreatedOrUpdated").GetBoolean());
        Assert.IsTrue(doc.RootElement.GetProperty("publicHostPermissionsNarrowed").GetBoolean());
        Assert.IsTrue(doc.RootElement.GetProperty("publicContentScriptMatchesNarrowed").GetBoolean());
        Assert.IsFalse(doc.RootElement.GetProperty("publicReleaseReady").GetBoolean());
        Assert.IsFalse(doc.RootElement.GetProperty("chromeWebStoreReady").GetBoolean());
        Assert.IsTrue(doc.RootElement.GetProperty("manualQaRequiredBeforePublicRelease").GetBoolean());
    }

    [TestMethod]
    public void ManifestVariantDiffSummaryDocumentsExactPermissionDiff()
    {
        using var doc = ReadJson(ManifestVariantDiffSummaryPath);
        Assert.AreEqual("MANIFEST_VARIANT_DIFF_SUMMARY_READY", doc.RootElement.GetProperty("decision").GetString());
        Assert.IsGreaterThanOrEqualTo(4, doc.RootElement.GetProperty("differences").GetArrayLength());
        Assert.IsTrue(doc.RootElement.GetProperty("publicHostPermissionsNarrowed").GetBoolean());
        Assert.IsTrue(doc.RootElement.GetProperty("publicContentScriptMatchesNarrowed").GetBoolean());
        Assert.IsFalse(doc.RootElement.GetProperty("cspModified").GetBoolean());
        Assert.IsFalse(doc.RootElement.GetProperty("bridgeModified").GetBoolean());
    }

    [TestMethod]
    public void HostPermissionsAndContentScriptNarrowingProofsAreReady()
    {
        using var host = ReadJson(HostPermissionsNarrowingProofPath);
        using var content = ReadJson(ContentScriptMatchNarrowingProofPath);
        Assert.AreEqual("HOST_PERMISSIONS_NARROWING_PROOF_READY", host.RootElement.GetProperty("decision").GetString());
        Assert.IsTrue(host.RootElement.GetProperty("publicHostPermissionsNarrowed").GetBoolean());
        Assert.IsTrue(host.RootElement.GetProperty("internalWildcardPermissionsPreservedForInternalCandidate").GetBoolean());
        Assert.AreEqual("CONTENT_SCRIPT_MATCH_NARROWING_PROOF_READY", content.RootElement.GetProperty("decision").GetString());
        Assert.AreEqual("omit_automatic_content_scripts_for_public_variant", content.RootElement.GetProperty("publicContentScriptsStrategy").GetString());
        Assert.IsTrue(content.RootElement.GetProperty("publicContentScriptMatchesNarrowed").GetBoolean());
        Assert.IsTrue(content.RootElement.GetProperty("manualQaRequiredBeforePublicRelease").GetBoolean());
    }

    [TestMethod]
    public void OptionalPermissionsAndFeatureImpactKeepUnlocksDisabled()
    {
        using var optional = ReadJson(OptionalPermissionsEvaluationPath);
        using var impact = ReadJson(PublicBuildFeatureImpactPath);
        Assert.AreEqual("OPTIONAL_PERMISSIONS_EVALUATION_READY", optional.RootElement.GetProperty("decision").GetString());
        Assert.IsFalse(optional.RootElement.GetProperty("optionalHostPermissionsImplementedNow").GetBoolean());
        Assert.IsTrue(optional.RootElement.GetProperty("activeTabAlreadyPresent").GetBoolean());
        Assert.IsFalse(optional.RootElement.GetProperty("providerCloudEnabled").GetBoolean());
        Assert.IsFalse(optional.RootElement.GetProperty("filesystemEnabled").GetBoolean());
        Assert.IsFalse(optional.RootElement.GetProperty("browserAutomationEnabled").GetBoolean());
        Assert.IsFalse(optional.RootElement.GetProperty("capabilityUnlockEnabled").GetBoolean());
        Assert.AreEqual("PUBLIC_BUILD_FEATURE_IMPACT_AFTER_NARROWING_READY", impact.RootElement.GetProperty("decision").GetString());
        Assert.IsFalse(impact.RootElement.GetProperty("runtimeProductiveEnabled").GetBoolean());
        Assert.IsFalse(impact.RootElement.GetProperty("providerCloudEnabled").GetBoolean());
        Assert.IsFalse(impact.RootElement.GetProperty("filesystemEnabled").GetBoolean());
        Assert.IsFalse(impact.RootElement.GetProperty("browserAutomationEnabled").GetBoolean());
        Assert.IsFalse(impact.RootElement.GetProperty("capabilityUnlockEnabled").GetBoolean());
    }

    [TestMethod]
    public void RegressionQaNoGoAndRollbackProofsAreReady()
    {
        using var regression = ReadJson(InternalBuildRegressionProofPath);
        using var validation = ReadJson(PublicVariantStaticValidationPath);
        using var qa = ReadJson(PublicVariantRuntimeQaPlanPath);
        using var noGo = ReadJson(PublicReleaseNoGoProofPath);
        using var rollback = ReadJson(RollbackReadinessProofPath);
        Assert.IsTrue(regression.RootElement.GetProperty("internalCandidateReady").GetBoolean());
        Assert.IsTrue(regression.RootElement.GetProperty("internalManifestPreserved").GetBoolean());
        Assert.IsTrue(validation.RootElement.GetProperty("manifestJsonValid").GetBoolean());
        Assert.IsFalse(validation.RootElement.GetProperty("wildcardHostPermissionsPresent").GetBoolean());
        Assert.IsFalse(validation.RootElement.GetProperty("wildcardContentScriptMatchesPresent").GetBoolean());
        Assert.IsTrue(qa.RootElement.GetProperty("manualQaRequiredBeforePublicRelease").GetBoolean());
        Assert.IsFalse(noGo.RootElement.GetProperty("publicReleaseReady").GetBoolean());
        Assert.IsFalse(noGo.RootElement.GetProperty("chromeWebStoreReady").GetBoolean());
        Assert.IsTrue(rollback.RootElement.GetProperty("rollbackReady").GetBoolean());
    }

    [TestMethod]
    public void ConsolidatedGoNoGoMatchesExpectedM661M663Decision()
    {
        using var doc = ReadJson(ConsolidatedGoNoGoPath);
        Assert.AreEqual("PUBLIC_MANIFEST_VARIANT_PATCH_READY", doc.RootElement.GetProperty("decision").GetString());
        Assert.IsTrue(doc.RootElement.GetProperty("internalCandidateReady").GetBoolean());
        Assert.IsTrue(doc.RootElement.GetProperty("publicBuildCandidateReady").GetBoolean());
        Assert.IsFalse(doc.RootElement.GetProperty("publicReleaseReady").GetBoolean());
        Assert.IsFalse(doc.RootElement.GetProperty("chromeWebStoreReady").GetBoolean());
        Assert.IsTrue(doc.RootElement.GetProperty("internalManifestPreserved").GetBoolean());
        Assert.IsTrue(doc.RootElement.GetProperty("publicManifestVariantCreatedOrUpdated").GetBoolean());
        Assert.IsTrue(doc.RootElement.GetProperty("publicHostPermissionsNarrowed").GetBoolean());
        Assert.IsTrue(doc.RootElement.GetProperty("publicContentScriptMatchesNarrowed").GetBoolean());
        Assert.IsTrue(doc.RootElement.GetProperty("internalWildcardPermissionsPreservedForInternalCandidate").GetBoolean());
        Assert.IsFalse(doc.RootElement.GetProperty("runtimeProductiveEnabled").GetBoolean());
        Assert.IsFalse(doc.RootElement.GetProperty("providerCloudEnabled").GetBoolean());
        Assert.IsFalse(doc.RootElement.GetProperty("filesystemEnabled").GetBoolean());
        Assert.IsFalse(doc.RootElement.GetProperty("browserAutomationEnabled").GetBoolean());
        Assert.IsFalse(doc.RootElement.GetProperty("capabilityUnlockEnabled").GetBoolean());
        Assert.IsFalse(doc.RootElement.GetProperty("bridgeModified").GetBoolean());
        Assert.IsFalse(doc.RootElement.GetProperty("cspModified").GetBoolean());
        Assert.IsTrue(doc.RootElement.GetProperty("manualQaRequiredBeforePublicRelease").GetBoolean());
        Assert.AreEqual("M664-M666 Public Build Package Candidate + Manual Extension QA + Store Disclosure Prep", doc.RootElement.GetProperty("recommendedNextMilestone").GetString());
    }

    [TestMethod]
    public void M661M663GoNoGosRemainReleaseBlocked()
    {
        using var m661 = ReadJson(M661GoNoGoPath);
        using var m662 = ReadJson(M662GoNoGoPath);
        using var m663 = ReadJson(M663GoNoGoPath);
        Assert.IsTrue(m661.RootElement.GetProperty("publicManifestVariantCreatedOrUpdated").GetBoolean());
        Assert.IsTrue(m662.RootElement.GetProperty("publicHostPermissionsNarrowed").GetBoolean());
        Assert.IsTrue(m663.RootElement.GetProperty("rollbackReadinessProofReady").GetBoolean());
        Assert.IsFalse(m661.RootElement.GetProperty("publicReleaseReady").GetBoolean());
        Assert.IsFalse(m662.RootElement.GetProperty("chromeWebStoreReady").GetBoolean());
        Assert.IsFalse(m663.RootElement.GetProperty("runtimeProductiveEnabled").GetBoolean());
        Assert.IsFalse(m663.RootElement.GetProperty("providerCloudEnabled").GetBoolean());
        Assert.IsFalse(m663.RootElement.GetProperty("filesystemEnabled").GetBoolean());
        Assert.IsFalse(m663.RootElement.GetProperty("browserAutomationEnabled").GetBoolean());
        Assert.IsFalse(m663.RootElement.GetProperty("capabilityUnlockEnabled").GetBoolean());
        Assert.IsFalse(m663.RootElement.GetProperty("bridgeModified").GetBoolean());
        Assert.IsFalse(m663.RootElement.GetProperty("cspModified").GetBoolean());
    }

    [TestMethod]
    public void InternalManifestJsonUnchanged()
    {
        Assert.AreEqual("76859A171D0FBC585E96253D0F269AEF4A54DCFD5F704DE9DB92D26EB1AEDDFD", Sha256Hex(InternalManifestPath));
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
