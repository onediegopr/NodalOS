using System.Security.Cryptography;
using System.Text.Json;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace OneBrain.Safety.Tests;

[TestClass]
[TestCategory("InternalCandidatePackageAudit")]
[TestCategory("M652")]
[TestCategory("M653")]
[TestCategory("M654")]
public sealed class NodalOsInternalCandidatePackageAuditM652M654Tests
{
    private const string M652ReportPath = "docs/reports/m652-packaging-candidate-artifact-prep.md";
    private const string M653ReportPath = "docs/reports/m653-internal-candidate-distribution-evidence.md";
    private const string M654ReportPath = "docs/reports/m654-final-internal-candidate-audit.md";

    private const string PackagingCandidatePlanPath = "artifacts/agent-operations/m652/packaging-candidate-plan.json";
    private const string PackageContentsManifestPath = "artifacts/agent-operations/m652/package-contents-manifest.json";
    private const string PackageRiskRegisterPath = "artifacts/agent-operations/m652/package-risk-register.json";
    private const string PackageChecksumsPlanPath = "artifacts/agent-operations/m652/package-checksums-plan.json";
    private const string M652GoNoGoPath = "artifacts/agent-operations/m652/m652-go-no-go.json";

    private const string InternalDistributionChecklistPath = "artifacts/agent-operations/m653/internal-distribution-checklist.json";
    private const string InternalInstallationRunbookPath = "artifacts/agent-operations/m653/internal-installation-runbook.json";
    private const string KnownIssuesPath = "artifacts/agent-operations/m653/internal-candidate-known-issues.json";
    private const string SupportNotesPath = "artifacts/agent-operations/m653/internal-candidate-support-notes.json";
    private const string RollbackNotesPath = "artifacts/agent-operations/m653/internal-candidate-rollback-notes.json";
    private const string M653GoNoGoPath = "artifacts/agent-operations/m653/m653-go-no-go.json";

    private const string InternalCandidateAuditPath = "artifacts/agent-operations/m654/internal-candidate-final-audit.json";
    private const string InternalCandidateGoNoGoPath = "artifacts/agent-operations/m654/internal-candidate-go-no-go.json";
    private const string PublicReleaseNoGoPath = "artifacts/agent-operations/m654/public-release-no-go-confirmation.json";
    private const string NextPublicReleaseBlockersPath = "artifacts/agent-operations/m654/next-public-release-blockers.json";
    private const string M654GoNoGoPath = "artifacts/agent-operations/m654/m654-go-no-go.json";

    private const string ConsolidatedGoNoGoPath = "artifacts/agent-operations/m652-m654/internal-candidate-package-audit-go-no-go.json";

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
    public void M652ArtifactsExist()
    {
        Assert.IsTrue(File.Exists(FullPath(M652ReportPath)), M652ReportPath);
        Assert.IsTrue(File.Exists(FullPath(PackagingCandidatePlanPath)), PackagingCandidatePlanPath);
        Assert.IsTrue(File.Exists(FullPath(PackageContentsManifestPath)), PackageContentsManifestPath);
        Assert.IsTrue(File.Exists(FullPath(PackageRiskRegisterPath)), PackageRiskRegisterPath);
        Assert.IsTrue(File.Exists(FullPath(PackageChecksumsPlanPath)), PackageChecksumsPlanPath);
        Assert.IsTrue(File.Exists(FullPath(M652GoNoGoPath)), M652GoNoGoPath);
    }

    [TestMethod]
    public void M653ArtifactsExist()
    {
        Assert.IsTrue(File.Exists(FullPath(M653ReportPath)), M653ReportPath);
        Assert.IsTrue(File.Exists(FullPath(InternalDistributionChecklistPath)), InternalDistributionChecklistPath);
        Assert.IsTrue(File.Exists(FullPath(InternalInstallationRunbookPath)), InternalInstallationRunbookPath);
        Assert.IsTrue(File.Exists(FullPath(KnownIssuesPath)), KnownIssuesPath);
        Assert.IsTrue(File.Exists(FullPath(SupportNotesPath)), SupportNotesPath);
        Assert.IsTrue(File.Exists(FullPath(RollbackNotesPath)), RollbackNotesPath);
        Assert.IsTrue(File.Exists(FullPath(M653GoNoGoPath)), M653GoNoGoPath);
    }

    [TestMethod]
    public void M654ArtifactsExist()
    {
        Assert.IsTrue(File.Exists(FullPath(M654ReportPath)), M654ReportPath);
        Assert.IsTrue(File.Exists(FullPath(InternalCandidateAuditPath)), InternalCandidateAuditPath);
        Assert.IsTrue(File.Exists(FullPath(InternalCandidateGoNoGoPath)), InternalCandidateGoNoGoPath);
        Assert.IsTrue(File.Exists(FullPath(PublicReleaseNoGoPath)), PublicReleaseNoGoPath);
        Assert.IsTrue(File.Exists(FullPath(NextPublicReleaseBlockersPath)), NextPublicReleaseBlockersPath);
        Assert.IsTrue(File.Exists(FullPath(M654GoNoGoPath)), M654GoNoGoPath);
    }

    [TestMethod]
    public void ConsolidatedGoNoGoExists() =>
        Assert.IsTrue(File.Exists(FullPath(ConsolidatedGoNoGoPath)), ConsolidatedGoNoGoPath);

    [TestMethod]
    public void PackagingCandidatePlanIsPrepOnly()
    {
        using var doc = ReadJson(PackagingCandidatePlanPath);
        Assert.AreEqual("PACKAGING_CANDIDATE_ARTIFACT_PREP_READY", doc.RootElement.GetProperty("decision").GetString());
        Assert.AreEqual("local-first controlled internal evidence candidate", doc.RootElement.GetProperty("packageCandidateScope").GetString());
        Assert.IsFalse(doc.RootElement.GetProperty("packageRealZipCreated").GetBoolean());
        Assert.IsFalse(doc.RootElement.GetProperty("publicStoreUpload").GetBoolean());
        Assert.IsFalse(doc.RootElement.GetProperty("signingPerformed").GetBoolean());
        Assert.IsFalse(doc.RootElement.GetProperty("manifestModified").GetBoolean());
        Assert.IsFalse(doc.RootElement.GetProperty("hostPermissionsModified").GetBoolean());
        Assert.IsFalse(doc.RootElement.GetProperty("productFilesModified").GetBoolean());
        Assert.IsFalse(doc.RootElement.GetProperty("publicReleaseReady").GetBoolean());
    }

    [TestMethod]
    public void PackageContentsManifestDocumentsIncludesAndExcludes()
    {
        using var doc = ReadJson(PackageContentsManifestPath);
        Assert.AreEqual("PACKAGE_CONTENTS_MANIFEST_READY", doc.RootElement.GetProperty("decision").GetString());
        Assert.IsGreaterThanOrEqualTo(7, doc.RootElement.GetProperty("include").GetArrayLength());
        Assert.IsGreaterThanOrEqualTo(8, doc.RootElement.GetProperty("exclude").GetArrayLength());
        Assert.IsFalse(doc.RootElement.GetProperty("packageRealZipCreated").GetBoolean());
        Assert.IsFalse(doc.RootElement.GetProperty("productFilesModified").GetBoolean());
    }

    [TestMethod]
    public void InternalDistributionChecklistAndRunbookExist()
    {
        using var checklist = ReadJson(InternalDistributionChecklistPath);
        using var runbook = ReadJson(InternalInstallationRunbookPath);
        Assert.AreEqual("INTERNAL_DISTRIBUTION_CHECKLIST_READY", checklist.RootElement.GetProperty("decision").GetString());
        Assert.IsGreaterThanOrEqualTo(7, checklist.RootElement.GetProperty("requirements").GetArrayLength());
        Assert.IsFalse(checklist.RootElement.GetProperty("publicReleaseReady").GetBoolean());
        Assert.AreEqual("INTERNAL_INSTALLATION_RUNBOOK_READY", runbook.RootElement.GetProperty("decision").GetString());
        Assert.IsTrue(runbook.RootElement.GetProperty("bridgeLivenessRequired").GetBoolean());
        Assert.IsGreaterThanOrEqualTo(10, runbook.RootElement.GetProperty("steps").GetArrayLength());
        Assert.IsFalse(runbook.RootElement.GetProperty("runtimeProductiveEnabled").GetBoolean());
        Assert.IsFalse(runbook.RootElement.GetProperty("providerCloudEnabled").GetBoolean());
        Assert.IsFalse(runbook.RootElement.GetProperty("filesystemEnabled").GetBoolean());
        Assert.IsFalse(runbook.RootElement.GetProperty("browserAutomationEnabled").GetBoolean());
    }

    [TestMethod]
    public void KnownIssuesSupportAndRollbackNotesExist()
    {
        using var known = ReadJson(KnownIssuesPath);
        using var support = ReadJson(SupportNotesPath);
        using var rollback = ReadJson(RollbackNotesPath);
        Assert.AreEqual("INTERNAL_CANDIDATE_KNOWN_ISSUES_READY", known.RootElement.GetProperty("decision").GetString());
        Assert.IsGreaterThanOrEqualTo(4, known.RootElement.GetProperty("knownIssues").GetArrayLength());
        Assert.AreEqual("INTERNAL_CANDIDATE_SUPPORT_NOTES_READY", support.RootElement.GetProperty("decision").GetString());
        Assert.IsGreaterThanOrEqualTo(5, support.RootElement.GetProperty("requiredSupportEvidence").GetArrayLength());
        Assert.AreEqual("INTERNAL_CANDIDATE_ROLLBACK_NOTES_READY", rollback.RootElement.GetProperty("decision").GetString());
        Assert.IsFalse(rollback.RootElement.GetProperty("storeRollbackRequired").GetBoolean());
        Assert.IsFalse(rollback.RootElement.GetProperty("publicReleaseReady").GetBoolean());
    }

    [TestMethod]
    public void FinalInternalCandidateAuditEvaluatesReadiness()
    {
        using var doc = ReadJson(InternalCandidateAuditPath);
        Assert.AreEqual("FINAL_INTERNAL_CANDIDATE_AUDIT_READY", doc.RootElement.GetProperty("decision").GetString());
        Assert.IsTrue(doc.RootElement.GetProperty("releaseEvidenceGateReady").GetBoolean());
        Assert.IsTrue(doc.RootElement.GetProperty("cleanRuntimeDevtoolsEvidenceReady").GetBoolean());
        Assert.IsTrue(doc.RootElement.GetProperty("packagingCandidateEvidenceReady").GetBoolean());
        Assert.IsTrue(doc.RootElement.GetProperty("distributionRunbookReady").GetBoolean());
        Assert.IsTrue(doc.RootElement.GetProperty("disabledStateProofReady").GetBoolean());
        Assert.IsTrue(doc.RootElement.GetProperty("internalCandidateReady").GetBoolean());
        Assert.IsFalse(doc.RootElement.GetProperty("publicReleaseReady").GetBoolean());
        Assert.IsFalse(doc.RootElement.GetProperty("productFilesModified").GetBoolean());
    }

    [TestMethod]
    public void PublicReleaseNoGoConfirmationExists()
    {
        using var doc = ReadJson(PublicReleaseNoGoPath);
        Assert.AreEqual("PUBLIC_RELEASE_NO_GO_CONFIRMED", doc.RootElement.GetProperty("decision").GetString());
        Assert.IsFalse(doc.RootElement.GetProperty("publicReleaseReady").GetBoolean());
        Assert.IsFalse(doc.RootElement.GetProperty("chromeWebStoreReady").GetBoolean());
        Assert.IsFalse(doc.RootElement.GetProperty("publicReleasePublished").GetBoolean());
        Assert.IsFalse(doc.RootElement.GetProperty("publicStoreUpload").GetBoolean());
        Assert.IsGreaterThanOrEqualTo(4, doc.RootElement.GetProperty("reasons").GetArrayLength());
    }

    [TestMethod]
    public void ConsolidatedGoNoGoKeepsPublicReleaseAndSensitivePathsBlocked()
    {
        using var doc = ReadJson(ConsolidatedGoNoGoPath);
        Assert.AreEqual("INTERNAL_CANDIDATE_PACKAGE_AUDIT_READY", doc.RootElement.GetProperty("decision").GetString());
        Assert.IsTrue(doc.RootElement.GetProperty("internalCandidateReady").GetBoolean());
        Assert.AreEqual("local-first controlled internal evidence candidate", doc.RootElement.GetProperty("internalCandidateScope").GetString());
        Assert.IsFalse(doc.RootElement.GetProperty("publicReleaseReady").GetBoolean());
        Assert.IsFalse(doc.RootElement.GetProperty("chromeWebStoreReady").GetBoolean());
        Assert.AreEqual("split_internal_public_build", doc.RootElement.GetProperty("hostPermissionsStrategy").GetString());
        Assert.IsFalse(doc.RootElement.GetProperty("hostPermissionsModified").GetBoolean());
        Assert.IsFalse(doc.RootElement.GetProperty("manifestModified").GetBoolean());
        Assert.IsFalse(doc.RootElement.GetProperty("permissionsModified").GetBoolean());
        Assert.IsFalse(doc.RootElement.GetProperty("jsModified").GetBoolean());
        Assert.IsFalse(doc.RootElement.GetProperty("bridgeModified").GetBoolean());
        Assert.IsFalse(doc.RootElement.GetProperty("runtimeProductiveEnabled").GetBoolean());
        Assert.IsFalse(doc.RootElement.GetProperty("providerCloudEnabled").GetBoolean());
        Assert.IsFalse(doc.RootElement.GetProperty("filesystemEnabled").GetBoolean());
        Assert.IsFalse(doc.RootElement.GetProperty("browserAutomationEnabled").GetBoolean());
        Assert.IsFalse(doc.RootElement.GetProperty("capabilityUnlockEnabled").GetBoolean());
        Assert.IsFalse(doc.RootElement.GetProperty("productFilesModified").GetBoolean());
    }

    [TestMethod]
    public void M652M654GoNoGosRemainInternalCandidateOnly()
    {
        using var m652 = ReadJson(M652GoNoGoPath);
        using var m653 = ReadJson(M653GoNoGoPath);
        using var m654 = ReadJson(M654GoNoGoPath);
        Assert.IsFalse(m652.RootElement.GetProperty("packageRealZipCreated").GetBoolean());
        Assert.IsFalse(m652.RootElement.GetProperty("publicStoreUpload").GetBoolean());
        Assert.IsFalse(m652.RootElement.GetProperty("signingPerformed").GetBoolean());
        Assert.IsFalse(m653.RootElement.GetProperty("publicReleaseReady").GetBoolean());
        Assert.IsFalse(m653.RootElement.GetProperty("chromeWebStoreReady").GetBoolean());
        Assert.IsTrue(m654.RootElement.GetProperty("internalCandidateReady").GetBoolean());
        Assert.IsFalse(m654.RootElement.GetProperty("publicReleaseReady").GetBoolean());
        Assert.IsFalse(m654.RootElement.GetProperty("chromeWebStoreReady").GetBoolean());
        Assert.IsFalse(m654.RootElement.GetProperty("productFilesModified").GetBoolean());
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
