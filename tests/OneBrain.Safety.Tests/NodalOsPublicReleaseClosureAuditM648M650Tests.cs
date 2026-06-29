using System.Security.Cryptography;
using System.Text.Json;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace OneBrain.Safety.Tests;

[TestClass]
[TestCategory("PublicReleaseClosureAudit")]
[TestCategory("M648")]
[TestCategory("M649")]
[TestCategory("M650")]
public sealed class NodalOsPublicReleaseClosureAuditM648M650Tests
{
    private const string M648ReportPath = "docs/reports/m648-public-release-closure-audit.md";
    private const string M649ReportPath = "docs/reports/m649-release-scope-decision.md";
    private const string M650ReportPath = "docs/reports/m650-final-remediation-backlog-release-sequence.md";

    private const string PublicReleaseClosureAuditPath = "artifacts/agent-operations/m648/public-release-closure-audit.json";
    private const string FinalPublicReleaseGoNoGoPath = "artifacts/agent-operations/m648/final-public-release-go-no-go.json";
    private const string ReleaseClosureBlockersStatusPath = "artifacts/agent-operations/m648/release-closure-blockers-status.json";
    private const string M648GoNoGoPath = "artifacts/agent-operations/m648/m648-go-no-go.json";

    private const string ReleaseScopeDecisionMatrixPath = "artifacts/agent-operations/m649/release-scope-decision-matrix.json";
    private const string InternalCandidateReadinessPath = "artifacts/agent-operations/m649/internal-candidate-readiness.json";
    private const string PublicCandidateReadinessPath = "artifacts/agent-operations/m649/public-candidate-readiness.json";
    private const string M649GoNoGoPath = "artifacts/agent-operations/m649/m649-go-no-go.json";

    private const string FinalRemediationBacklogPath = "artifacts/agent-operations/m650/final-remediation-backlog.json";
    private const string ReleaseSequencePlanPath = "artifacts/agent-operations/m650/release-sequence-plan.json";
    private const string NextMilestonesRecommendationPath = "artifacts/agent-operations/m650/next-milestones-recommendation.json";
    private const string M650GoNoGoPath = "artifacts/agent-operations/m650/m650-go-no-go.json";

    private const string ConsolidatedSummaryPath = "artifacts/agent-operations/m648-m650/public-release-closure-audit-summary.json";

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
    public void M648ArtifactsExist()
    {
        Assert.IsTrue(File.Exists(FullPath(M648ReportPath)), M648ReportPath);
        Assert.IsTrue(File.Exists(FullPath(PublicReleaseClosureAuditPath)), PublicReleaseClosureAuditPath);
        Assert.IsTrue(File.Exists(FullPath(FinalPublicReleaseGoNoGoPath)), FinalPublicReleaseGoNoGoPath);
        Assert.IsTrue(File.Exists(FullPath(ReleaseClosureBlockersStatusPath)), ReleaseClosureBlockersStatusPath);
        Assert.IsTrue(File.Exists(FullPath(M648GoNoGoPath)), M648GoNoGoPath);
    }

    [TestMethod]
    public void M649ArtifactsExist()
    {
        Assert.IsTrue(File.Exists(FullPath(M649ReportPath)), M649ReportPath);
        Assert.IsTrue(File.Exists(FullPath(ReleaseScopeDecisionMatrixPath)), ReleaseScopeDecisionMatrixPath);
        Assert.IsTrue(File.Exists(FullPath(InternalCandidateReadinessPath)), InternalCandidateReadinessPath);
        Assert.IsTrue(File.Exists(FullPath(PublicCandidateReadinessPath)), PublicCandidateReadinessPath);
        Assert.IsTrue(File.Exists(FullPath(M649GoNoGoPath)), M649GoNoGoPath);
    }

    [TestMethod]
    public void M650ArtifactsExist()
    {
        Assert.IsTrue(File.Exists(FullPath(M650ReportPath)), M650ReportPath);
        Assert.IsTrue(File.Exists(FullPath(FinalRemediationBacklogPath)), FinalRemediationBacklogPath);
        Assert.IsTrue(File.Exists(FullPath(ReleaseSequencePlanPath)), ReleaseSequencePlanPath);
        Assert.IsTrue(File.Exists(FullPath(NextMilestonesRecommendationPath)), NextMilestonesRecommendationPath);
        Assert.IsTrue(File.Exists(FullPath(M650GoNoGoPath)), M650GoNoGoPath);
    }

    [TestMethod]
    public void ConsolidatedSummaryExists() =>
        Assert.IsTrue(File.Exists(FullPath(ConsolidatedSummaryPath)), ConsolidatedSummaryPath);

    [TestMethod]
    public void ConsolidatedSummaryKeepsPublicReleaseNoGo()
    {
        using var doc = ReadJson(ConsolidatedSummaryPath);
        Assert.AreEqual("PUBLIC_RELEASE_CLOSURE_AUDIT_READY", doc.RootElement.GetProperty("decision").GetString());
        Assert.AreEqual("INTERNAL_CANDIDATE_GO_PUBLIC_RELEASE_NO_GO", doc.RootElement.GetProperty("releaseScopeDecision").GetString());
        Assert.IsTrue(doc.RootElement.GetProperty("releaseEvidenceGateReady").GetBoolean());
        Assert.IsTrue(doc.RootElement.GetProperty("cleanRuntimeDevtoolsEvidenceReady").GetBoolean());
        Assert.IsTrue(doc.RootElement.GetProperty("releaseCandidateEvidencePackReady").GetBoolean());
        Assert.IsTrue(doc.RootElement.GetProperty("hostPermissionsJustificationReady").GetBoolean());
        Assert.IsTrue(doc.RootElement.GetProperty("hostPermissionsOpenForPublicRelease").GetBoolean());
        Assert.IsTrue(doc.RootElement.GetProperty("packagingSigningStorePrepReady").GetBoolean());
        Assert.IsFalse(doc.RootElement.GetProperty("packagingSigningStoreFinalReady").GetBoolean());
        Assert.IsTrue(doc.RootElement.GetProperty("providerRuntimeDisabledProofReady").GetBoolean());
        Assert.IsFalse(doc.RootElement.GetProperty("publicReleaseReady").GetBoolean());
        Assert.IsTrue(doc.RootElement.GetProperty("internalCandidateReady").GetBoolean());
    }

    [TestMethod]
    public void ConsolidatedSummaryKeepsProductAndRuntimeBoundariesClosed()
    {
        using var doc = ReadJson(ConsolidatedSummaryPath);
        Assert.IsFalse(doc.RootElement.GetProperty("productFilesModified").GetBoolean());
        Assert.IsFalse(doc.RootElement.GetProperty("manifestModified").GetBoolean());
        Assert.IsFalse(doc.RootElement.GetProperty("permissionsModified").GetBoolean());
        Assert.IsFalse(doc.RootElement.GetProperty("jsModified").GetBoolean());
        Assert.IsFalse(doc.RootElement.GetProperty("bridgeModified").GetBoolean());
        Assert.IsFalse(doc.RootElement.GetProperty("runtimeProductiveEnabled").GetBoolean());
        Assert.IsFalse(doc.RootElement.GetProperty("providerCloudEnabled").GetBoolean());
        Assert.IsFalse(doc.RootElement.GetProperty("filesystemEnabled").GetBoolean());
        Assert.IsFalse(doc.RootElement.GetProperty("browserAutomationEnabled").GetBoolean());
        Assert.IsFalse(doc.RootElement.GetProperty("capabilityUnlockEnabled").GetBoolean());
        Assert.AreEqual("M651 Host Permissions Final Decision / Manifest Strategy Gate", doc.RootElement.GetProperty("recommendedNextMilestone").GetString());
    }

    [TestMethod]
    public void PublicReleaseClosureAuditReviewsRequiredInputs()
    {
        using var doc = ReadJson(PublicReleaseClosureAuditPath);
        Assert.AreEqual("PUBLIC_RELEASE_CLOSURE_AUDIT_READY", doc.RootElement.GetProperty("decision").GetString());
        Assert.IsTrue(doc.RootElement.GetProperty("installedExtensionEvidenceGateReady").GetBoolean());
        Assert.IsTrue(doc.RootElement.GetProperty("cleanRuntimeDevtoolsEvidenceReady").GetBoolean());
        Assert.IsTrue(doc.RootElement.GetProperty("releaseCandidateEvidencePackReady").GetBoolean());
        Assert.IsTrue(doc.RootElement.GetProperty("hostPermissionsJustificationReady").GetBoolean());
        Assert.IsTrue(doc.RootElement.GetProperty("hostPermissionsOpenForPublicRelease").GetBoolean());
        Assert.IsTrue(doc.RootElement.GetProperty("packagingSigningStorePrepReady").GetBoolean());
        Assert.IsFalse(doc.RootElement.GetProperty("packagingSigningStoreFinalReady").GetBoolean());
        Assert.IsTrue(doc.RootElement.GetProperty("providerRuntimeDisabledProofReady").GetBoolean());
        Assert.IsTrue(doc.RootElement.GetProperty("providerPathPresentCaveat").GetBoolean());
        Assert.IsFalse(doc.RootElement.GetProperty("publicReleaseReady").GetBoolean());
    }

    [TestMethod]
    public void FinalPublicReleaseGoNoGoBlocksPublicReleaseUnlessBlockersClose()
    {
        using var doc = ReadJson(FinalPublicReleaseGoNoGoPath);
        Assert.AreEqual("FINAL_PUBLIC_RELEASE_GO_NO_GO_REVIEW_READY", doc.RootElement.GetProperty("decision").GetString());
        Assert.AreEqual("PUBLIC_RELEASE_NO_GO_REMEDIATION_REQUIRED", doc.RootElement.GetProperty("publicReleaseDecision").GetString());
        Assert.IsFalse(doc.RootElement.GetProperty("publicReleaseReady").GetBoolean());
        Assert.IsTrue(doc.RootElement.GetProperty("internalCandidateReady").GetBoolean());
        Assert.IsGreaterThanOrEqualTo(doc.RootElement.GetProperty("reasons").GetArrayLength(), 5);
    }

    [TestMethod]
    public void ReleaseClosureBlockersRemainOpenForPublicRelease()
    {
        using var doc = ReadJson(ReleaseClosureBlockersStatusPath);
        var blockers = doc.RootElement.GetProperty("blockers").EnumerateArray().ToArray();
        Assert.IsGreaterThanOrEqualTo(blockers.Length, 4);
        Assert.IsTrue(blockers.Any(item => item.GetProperty("id").GetString() == "HOST-PERMISSIONS-PUBLIC-RELEASE" && item.GetProperty("status").GetString() == "open"));
        Assert.IsTrue(blockers.Any(item => item.GetProperty("id").GetString() == "PACKAGING-SIGNING-STORE-FINAL" && item.GetProperty("status").GetString() == "open"));
        Assert.IsFalse(doc.RootElement.GetProperty("publicReleaseReady").GetBoolean());
    }

    [TestMethod]
    public void ReleaseScopeDecisionMatrixEvaluatesInternalAndPublicScopes()
    {
        using var doc = ReadJson(ReleaseScopeDecisionMatrixPath);
        Assert.AreEqual("RELEASE_SCOPE_DECISION_MATRIX_READY", doc.RootElement.GetProperty("decision").GetString());
        Assert.AreEqual("INTERNAL_CANDIDATE_GO_PUBLIC_RELEASE_NO_GO", doc.RootElement.GetProperty("recommendedDecision").GetString());
        var scopes = doc.RootElement.GetProperty("scopes").EnumerateArray().ToArray();
        Assert.IsTrue(scopes.Any(item => item.GetProperty("scope").GetString() == "Internal Evidence Candidate" && item.GetProperty("readiness").GetString() == "GO"));
        Assert.IsTrue(scopes.Any(item => item.GetProperty("scope").GetString() == "Public Release" && item.GetProperty("readiness").GetString() == "NO_GO"));
    }

    [TestMethod]
    public void InternalCandidateIsReadyAndPublicCandidateIsNotReady()
    {
        using var internalDoc = ReadJson(InternalCandidateReadinessPath);
        using var publicDoc = ReadJson(PublicCandidateReadinessPath);
        Assert.IsTrue(internalDoc.RootElement.GetProperty("internalCandidateReady").GetBoolean());
        Assert.IsFalse(internalDoc.RootElement.GetProperty("publicReleaseReady").GetBoolean());
        Assert.IsFalse(publicDoc.RootElement.GetProperty("publicChromeStoreCandidateReady").GetBoolean());
        Assert.IsFalse(publicDoc.RootElement.GetProperty("publicReleaseReady").GetBoolean());
        Assert.IsGreaterThanOrEqualTo(publicDoc.RootElement.GetProperty("blockingReasons").GetArrayLength(), 4);
    }

    [TestMethod]
    public void FinalRemediationBacklogAndReleaseSequencePlanExist()
    {
        using var backlog = ReadJson(FinalRemediationBacklogPath);
        using var sequence = ReadJson(ReleaseSequencePlanPath);
        Assert.AreEqual("FINAL_REMEDIATION_BACKLOG_READY", backlog.RootElement.GetProperty("decision").GetString());
        Assert.IsGreaterThanOrEqualTo(backlog.RootElement.GetProperty("backlog").GetArrayLength(), 5);
        Assert.IsFalse(backlog.RootElement.GetProperty("publicReleaseReady").GetBoolean());
        Assert.AreEqual("RELEASE_SEQUENCE_PLAN_READY", sequence.RootElement.GetProperty("decision").GetString());
        Assert.IsGreaterThanOrEqualTo(sequence.RootElement.GetProperty("sequence").GetArrayLength(), 4);
        Assert.IsFalse(sequence.RootElement.GetProperty("publicReleaseCanStartNow").GetBoolean());
        Assert.IsFalse(sequence.RootElement.GetProperty("packageFinalCreated").GetBoolean());
        Assert.IsFalse(sequence.RootElement.GetProperty("signingPerformed").GetBoolean());
        Assert.IsFalse(sequence.RootElement.GetProperty("published").GetBoolean());
    }

    [TestMethod]
    public void M648M650GoNoGosRemainPlanningOnly()
    {
        using var m648 = ReadJson(M648GoNoGoPath);
        using var m649 = ReadJson(M649GoNoGoPath);
        using var m650 = ReadJson(M650GoNoGoPath);
        Assert.IsFalse(m648.RootElement.GetProperty("publicReleaseReady").GetBoolean());
        Assert.IsTrue(m648.RootElement.GetProperty("internalCandidateReady").GetBoolean());
        Assert.AreEqual("INTERNAL_CANDIDATE_GO_PUBLIC_RELEASE_NO_GO", m649.RootElement.GetProperty("releaseScopeDecision").GetString());
        Assert.IsTrue(m649.RootElement.GetProperty("internalCandidateReady").GetBoolean());
        Assert.IsFalse(m649.RootElement.GetProperty("publicReleaseReady").GetBoolean());
        Assert.IsTrue(m650.RootElement.GetProperty("finalRemediationBacklogReady").GetBoolean());
        Assert.IsTrue(m650.RootElement.GetProperty("releaseSequencePlanReady").GetBoolean());
        Assert.IsFalse(m650.RootElement.GetProperty("packageFinalCreated").GetBoolean());
        Assert.IsFalse(m650.RootElement.GetProperty("signingPerformed").GetBoolean());
        Assert.IsFalse(m650.RootElement.GetProperty("published").GetBoolean());
    }

    [TestMethod]
    public void NextMilestonesRecommendationPointsToM651()
    {
        using var doc = ReadJson(NextMilestonesRecommendationPath);
        Assert.AreEqual("NEXT_MILESTONES_RECOMMENDATION_READY", doc.RootElement.GetProperty("decision").GetString());
        Assert.AreEqual("M651 Host Permissions Final Decision / Manifest Strategy Gate", doc.RootElement.GetProperty("recommendedNextMilestone").GetString());
        Assert.IsFalse(doc.RootElement.GetProperty("publicReleaseReady").GetBoolean());
        Assert.IsTrue(doc.RootElement.GetProperty("internalCandidateReady").GetBoolean());
    }

    [TestMethod]
    public void ManifestJsonUnchanged()
    {
        Assert.AreEqual("76859A171D0FBC585E96253D0F269AEF4A54DCFD5F704DE9DB92D26EB1AEDDFD", Sha256Hex(ManifestPath));
    }

    [TestMethod]
    public void SidepanelHtmlUnchanged()
    {
        Assert.AreEqual("8A2123D2DE578C8A026B3CB15D71C1E47A015FB66B68397DFB9DDBADF35877EB", Sha256Hex(SidepanelHtmlPath));
    }

    [TestMethod]
    public void SidepanelCssUnchanged()
    {
        Assert.AreEqual("69E508F153A2D58DC6824BFFF041636C9D94181C97CA2CD929DF091BD434F61B", Sha256Hex(SidepanelCssPath));
    }

    [TestMethod]
    public void SidepanelJsUnchanged()
    {
        Assert.AreEqual("5936C1B95AEC7745A76EA32CE1ED0FFE10309FA8B9879FD685F75F4FBC77F8D6", Sha256Hex(SidepanelJsPath));
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
