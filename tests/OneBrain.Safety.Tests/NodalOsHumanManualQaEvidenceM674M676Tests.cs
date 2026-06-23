using System.Text.Json;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace OneBrain.Safety.Tests;

[TestClass]
[TestCategory("HumanManualQaEvidence")]
[TestCategory("M674")]
[TestCategory("M675")]
[TestCategory("M676")]
public sealed class NodalOsHumanManualQaEvidenceM674M676Tests
{
    private const string M674ReportPath = "docs/reports/m674-human-manual-qa-evidence-intake.md";
    private const string M675ReportPath = "docs/reports/m675-final-package-audit-retry.md";
    private const string M676ReportPath = "docs/reports/m676-public-package-freeze-go-no-go.md";

    private const string HumanManualQaEvidenceIntakePath = "artifacts/agent-operations/m674/human-manual-qa-evidence-intake.json";
    private const string PublicVariantLoadEvidenceIntakePath = "artifacts/agent-operations/m674/public-variant-load-evidence-intake.json";
    private const string RuntimeTabEvidenceIntakePath = "artifacts/agent-operations/m674/runtime-tab-evidence-intake.json";
    private const string ServiceWorkerDevtoolsEvidenceIntakePath = "artifacts/agent-operations/m674/service-worker-devtools-evidence-intake.json";
    private const string PermissionWarningEvidenceIntakePath = "artifacts/agent-operations/m674/permission-warning-evidence-intake.json";
    private const string ConsoleCleanlinessEvidenceIntakePath = "artifacts/agent-operations/m674/console-cleanliness-evidence-intake.json";
    private const string EvidenceRedactionValidationPath = "artifacts/agent-operations/m674/evidence-redaction-validation.json";
    private const string M674GoNoGoPath = "artifacts/agent-operations/m674/m674-go-no-go.json";

    private const string FinalPackageAuditRetryPath = "artifacts/agent-operations/m675/final-package-audit-retry.json";
    private const string PublicPackageManifestSelectionAuditPath = "artifacts/agent-operations/m675/public-package-manifest-selection-audit.json";
    private const string PublicPackagePermissionAuditPath = "artifacts/agent-operations/m675/public-package-permission-audit.json";
    private const string PublicPackageKnownLimitationsAuditPath = "artifacts/agent-operations/m675/public-package-known-limitations-audit.json";
    private const string StoreDisclosureGapAuditPath = "artifacts/agent-operations/m675/store-disclosure-gap-audit.json";
    private const string ManualQaGapAuditPath = "artifacts/agent-operations/m675/manual-qa-gap-audit.json";
    private const string M675GoNoGoPath = "artifacts/agent-operations/m675/m675-go-no-go.json";

    private const string PublicPackageFreezeGoNoGoPath = "artifacts/agent-operations/m676/public-package-freeze-go-no-go.json";
    private const string PublicPackageFreezeReadinessPath = "artifacts/agent-operations/m676/public-package-freeze-readiness.json";
    private const string PreSubmissionAuditReadinessPath = "artifacts/agent-operations/m676/pre-submission-audit-readiness.json";
    private const string PostManualQaRiskRegisterPath = "artifacts/agent-operations/m676/post-manual-qa-risk-register.json";
    private const string PublicReleaseNoGoProofPath = "artifacts/agent-operations/m676/public-release-no-go-proof.json";
    private const string ChromeWebStoreNoGoProofPath = "artifacts/agent-operations/m676/chrome-web-store-no-go-proof.json";
    private const string M676GoNoGoPath = "artifacts/agent-operations/m676/m676-go-no-go.json";

    private const string ConsolidatedGoNoGoPath = "artifacts/agent-operations/m674-m676/human-manual-qa-evidence-final-audit-go-no-go.json";
    private const string PublicManifestPath = "browser-extension/onebrain-chrome-lab/manifest.public.json";

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

    private static string[] StringArray(JsonElement element) =>
        element.EnumerateArray().Select(item => item.GetString() ?? string.Empty).ToArray();

    [TestMethod]
    public void M674ArtifactsExist()
    {
        Assert.IsTrue(File.Exists(FullPath(M674ReportPath)), M674ReportPath);
        Assert.IsTrue(File.Exists(FullPath(HumanManualQaEvidenceIntakePath)), HumanManualQaEvidenceIntakePath);
        Assert.IsTrue(File.Exists(FullPath(PublicVariantLoadEvidenceIntakePath)), PublicVariantLoadEvidenceIntakePath);
        Assert.IsTrue(File.Exists(FullPath(RuntimeTabEvidenceIntakePath)), RuntimeTabEvidenceIntakePath);
        Assert.IsTrue(File.Exists(FullPath(ServiceWorkerDevtoolsEvidenceIntakePath)), ServiceWorkerDevtoolsEvidenceIntakePath);
        Assert.IsTrue(File.Exists(FullPath(PermissionWarningEvidenceIntakePath)), PermissionWarningEvidenceIntakePath);
        Assert.IsTrue(File.Exists(FullPath(ConsoleCleanlinessEvidenceIntakePath)), ConsoleCleanlinessEvidenceIntakePath);
        Assert.IsTrue(File.Exists(FullPath(EvidenceRedactionValidationPath)), EvidenceRedactionValidationPath);
        Assert.IsTrue(File.Exists(FullPath(M674GoNoGoPath)), M674GoNoGoPath);
    }

    [TestMethod]
    public void M675ArtifactsExist()
    {
        Assert.IsTrue(File.Exists(FullPath(M675ReportPath)), M675ReportPath);
        Assert.IsTrue(File.Exists(FullPath(FinalPackageAuditRetryPath)), FinalPackageAuditRetryPath);
        Assert.IsTrue(File.Exists(FullPath(PublicPackageManifestSelectionAuditPath)), PublicPackageManifestSelectionAuditPath);
        Assert.IsTrue(File.Exists(FullPath(PublicPackagePermissionAuditPath)), PublicPackagePermissionAuditPath);
        Assert.IsTrue(File.Exists(FullPath(PublicPackageKnownLimitationsAuditPath)), PublicPackageKnownLimitationsAuditPath);
        Assert.IsTrue(File.Exists(FullPath(StoreDisclosureGapAuditPath)), StoreDisclosureGapAuditPath);
        Assert.IsTrue(File.Exists(FullPath(ManualQaGapAuditPath)), ManualQaGapAuditPath);
        Assert.IsTrue(File.Exists(FullPath(M675GoNoGoPath)), M675GoNoGoPath);
    }

    [TestMethod]
    public void M676ArtifactsExist()
    {
        Assert.IsTrue(File.Exists(FullPath(M676ReportPath)), M676ReportPath);
        Assert.IsTrue(File.Exists(FullPath(PublicPackageFreezeGoNoGoPath)), PublicPackageFreezeGoNoGoPath);
        Assert.IsTrue(File.Exists(FullPath(PublicPackageFreezeReadinessPath)), PublicPackageFreezeReadinessPath);
        Assert.IsTrue(File.Exists(FullPath(PreSubmissionAuditReadinessPath)), PreSubmissionAuditReadinessPath);
        Assert.IsTrue(File.Exists(FullPath(PostManualQaRiskRegisterPath)), PostManualQaRiskRegisterPath);
        Assert.IsTrue(File.Exists(FullPath(PublicReleaseNoGoProofPath)), PublicReleaseNoGoProofPath);
        Assert.IsTrue(File.Exists(FullPath(ChromeWebStoreNoGoProofPath)), ChromeWebStoreNoGoProofPath);
        Assert.IsTrue(File.Exists(FullPath(M676GoNoGoPath)), M676GoNoGoPath);
    }

    [TestMethod]
    public void ConsolidatedM674M676GoNoGoExists() =>
        Assert.IsTrue(File.Exists(FullPath(ConsolidatedGoNoGoPath)), ConsolidatedGoNoGoPath);

    [TestMethod]
    public void M674ClassifiesMissingManualEvidenceAsIncomplete()
    {
        using var intake = ReadJson(HumanManualQaEvidenceIntakePath);
        using var load = ReadJson(PublicVariantLoadEvidenceIntakePath);
        using var runtime = ReadJson(RuntimeTabEvidenceIntakePath);
        using var devtools = ReadJson(ServiceWorkerDevtoolsEvidenceIntakePath);
        using var permissions = ReadJson(PermissionWarningEvidenceIntakePath);
        using var console = ReadJson(ConsoleCleanlinessEvidenceIntakePath);
        using var redaction = ReadJson(EvidenceRedactionValidationPath);
        Assert.IsFalse(intake.RootElement.GetProperty("manualQaEvidenceReceived").GetBoolean());
        Assert.IsFalse(intake.RootElement.GetProperty("manualQaEvidenceSufficient").GetBoolean());
        Assert.IsFalse(intake.RootElement.GetProperty("intakeComplete").GetBoolean());
        Assert.AreEqual("unknown", load.RootElement.GetProperty("publicVariantLoaded").GetString());
        Assert.AreEqual("unknown", runtime.RootElement.GetProperty("runtimeTabEvidence").GetString());
        Assert.AreEqual("unknown", devtools.RootElement.GetProperty("serviceWorkerDevtoolsEvidence").GetString());
        Assert.AreEqual("unknown", permissions.RootElement.GetProperty("permissionWarningsCaptured").GetString());
        Assert.AreEqual("unknown", console.RootElement.GetProperty("consoleCleanlinessEvidence").GetString());
        Assert.IsTrue(redaction.RootElement.GetProperty("evidenceRedacted").GetBoolean());
        Assert.IsFalse(redaction.RootElement.GetProperty("rawEvidencePayloadStored").GetBoolean());
    }

    [TestMethod]
    public void M675AuditsPackageButBlocksFreezeOnManualQaGap()
    {
        using var audit = ReadJson(FinalPackageAuditRetryPath);
        using var manifest = ReadJson(PublicPackageManifestSelectionAuditPath);
        using var permissions = ReadJson(PublicPackagePermissionAuditPath);
        using var limitations = ReadJson(PublicPackageKnownLimitationsAuditPath);
        using var store = ReadJson(StoreDisclosureGapAuditPath);
        using var gap = ReadJson(ManualQaGapAuditPath);
        Assert.IsTrue(audit.RootElement.GetProperty("publicPackageCandidatePrepared").GetBoolean());
        Assert.IsFalse(audit.RootElement.GetProperty("publicPackageFreezeReady").GetBoolean());
        Assert.IsTrue(manifest.RootElement.GetProperty("manifestSelectionVerifiedStatic").GetBoolean());
        Assert.IsTrue(permissions.RootElement.GetProperty("publicHostPermissionsNarrowed").GetBoolean());
        Assert.IsFalse(permissions.RootElement.GetProperty("containsHttpWildcard").GetBoolean());
        Assert.IsFalse(permissions.RootElement.GetProperty("containsHttpsWildcard").GetBoolean());
        Assert.IsTrue(limitations.RootElement.GetProperty("publicContentScriptsKnownLimitationDocumented").GetBoolean());
        Assert.IsFalse(store.RootElement.GetProperty("chromeWebStoreReady").GetBoolean());
        Assert.IsTrue(gap.RootElement.GetProperty("publicPackageFreezeBlocked").GetBoolean());
    }

    [TestMethod]
    public void M676KeepsFreezeReleaseAndStoreNoGo()
    {
        using var freeze = ReadJson(PublicPackageFreezeGoNoGoPath);
        using var readiness = ReadJson(PublicPackageFreezeReadinessPath);
        using var preSubmission = ReadJson(PreSubmissionAuditReadinessPath);
        using var risks = ReadJson(PostManualQaRiskRegisterPath);
        using var release = ReadJson(PublicReleaseNoGoProofPath);
        using var store = ReadJson(ChromeWebStoreNoGoProofPath);
        Assert.IsFalse(freeze.RootElement.GetProperty("publicPackageFreezeReady").GetBoolean());
        Assert.IsTrue(freeze.RootElement.GetProperty("internalCandidateReady").GetBoolean());
        Assert.IsTrue(freeze.RootElement.GetProperty("publicBuildCandidateReady").GetBoolean());
        Assert.IsFalse(readiness.RootElement.GetProperty("manualQaEvidenceReady").GetBoolean());
        Assert.IsFalse(preSubmission.RootElement.GetProperty("preSubmissionAuditReady").GetBoolean());
        Assert.IsGreaterThanOrEqualTo(3, risks.RootElement.GetProperty("risks").GetArrayLength());
        Assert.IsFalse(release.RootElement.GetProperty("publicReleaseReady").GetBoolean());
        Assert.IsFalse(store.RootElement.GetProperty("chromeWebStoreReady").GetBoolean());
    }

    [TestMethod]
    public void ConsolidatedGoNoGoUsesAllowedDecisionAndIncompleteBranch()
    {
        using var doc = ReadJson(ConsolidatedGoNoGoPath);
        var decision = doc.RootElement.GetProperty("decision").GetString();
        CollectionAssert.Contains(new[]
        {
            "HUMAN_MANUAL_QA_EVIDENCE_READY",
            "HUMAN_MANUAL_QA_EVIDENCE_INTAKE_INCOMPLETE",
            "PUBLIC_VARIANT_QA_REMEDIATION_REQUIRED"
        }, decision);

        Assert.AreEqual("HUMAN_MANUAL_QA_EVIDENCE_INTAKE_INCOMPLETE", decision);
        Assert.IsTrue(doc.RootElement.GetProperty("internalCandidateReady").GetBoolean());
        Assert.IsTrue(doc.RootElement.GetProperty("publicBuildCandidateReady").GetBoolean());
        Assert.IsFalse(doc.RootElement.GetProperty("manualQaEvidenceReceived").GetBoolean());
        Assert.IsFalse(doc.RootElement.GetProperty("manualQaEvidenceSufficient").GetBoolean());
        Assert.IsFalse(doc.RootElement.GetProperty("publicPackageFreezeReady").GetBoolean());
        Assert.AreEqual("M677-M679 Manual QA Evidence Completion", doc.RootElement.GetProperty("recommendedNextMilestone").GetString());
    }

    [TestMethod]
    public void GoNoGosKeepReleaseStoreAndSensitiveCapabilitiesDisabled()
    {
        foreach (var path in new[] { M674GoNoGoPath, M675GoNoGoPath, M676GoNoGoPath, ConsolidatedGoNoGoPath })
        {
            using var doc = ReadJson(path);
            Assert.IsFalse(doc.RootElement.GetProperty("publicReleaseReady").GetBoolean(), path);
            Assert.IsFalse(doc.RootElement.GetProperty("chromeWebStoreReady").GetBoolean(), path);
            Assert.IsFalse(doc.RootElement.GetProperty("runtimeProductiveEnabled").GetBoolean(), path);
            Assert.IsFalse(doc.RootElement.GetProperty("providerCloudEnabled").GetBoolean(), path);
            Assert.IsFalse(doc.RootElement.GetProperty("filesystemEnabled").GetBoolean(), path);
            Assert.IsFalse(doc.RootElement.GetProperty("browserAutomationEnabled").GetBoolean(), path);
            Assert.IsFalse(doc.RootElement.GetProperty("capabilityUnlockEnabled").GetBoolean(), path);
            Assert.IsFalse(doc.RootElement.GetProperty("bridgeModified").GetBoolean(), path);
            Assert.IsFalse(doc.RootElement.GetProperty("cspModified").GetBoolean(), path);
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
}
