using System.Text.Json;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace OneBrain.Safety.Tests;

[TestClass]
[TestCategory("ManualQaEvidenceCompletion")]
[TestCategory("M677")]
[TestCategory("M678")]
[TestCategory("M679")]
public sealed class NodalOsManualQaEvidenceCompletionM677M679Tests
{
    private const string M677ReportPath = "docs/reports/m677-manual-qa-evidence-completion.md";
    private const string M678ReportPath = "docs/reports/m678-final-package-audit-retry.md";
    private const string M679ReportPath = "docs/reports/m679-public-package-freeze-decision.md";

    private const string ManualQaEvidenceCompletionPath = "artifacts/agent-operations/m677/manual-qa-evidence-completion.json";
    private const string PublicVariantLoadEvidencePath = "artifacts/agent-operations/m677/public-variant-load-evidence.json";
    private const string ManifestSelectionHumanProofPath = "artifacts/agent-operations/m677/manifest-selection-human-proof.json";
    private const string PermissionWarningHumanProofPath = "artifacts/agent-operations/m677/permission-warning-human-proof.json";
    private const string RuntimeTabHumanProofPath = "artifacts/agent-operations/m677/runtime-tab-human-proof.json";
    private const string ServiceWorkerDevtoolsHumanProofPath = "artifacts/agent-operations/m677/service-worker-devtools-human-proof.json";
    private const string CspConsoleHumanProofPath = "artifacts/agent-operations/m677/csp-console-human-proof.json";
    private const string BridgeLivenessHumanProofPath = "artifacts/agent-operations/m677/bridge-liveness-human-proof.json";
    private const string OriginBehaviorHumanProofPath = "artifacts/agent-operations/m677/origin-behavior-human-proof.json";
    private const string EvidenceRedactionHumanProofPath = "artifacts/agent-operations/m677/evidence-redaction-human-proof.json";
    private const string M677GoNoGoPath = "artifacts/agent-operations/m677/m677-go-no-go.json";

    private const string FinalPackageAuditRetryPath = "artifacts/agent-operations/m678/final-package-audit-retry.json";
    private const string PublicManifestSelectionAuditPath = "artifacts/agent-operations/m678/public-manifest-selection-audit.json";
    private const string PublicPermissionsAuditPath = "artifacts/agent-operations/m678/public-permissions-audit.json";
    private const string ManualQaCompletenessAuditPath = "artifacts/agent-operations/m678/manual-qa-completeness-audit.json";
    private const string StoreDisclosureReadinessAuditPath = "artifacts/agent-operations/m678/store-disclosure-readiness-audit.json";
    private const string PackageFreezeRiskRegisterPath = "artifacts/agent-operations/m678/package-freeze-risk-register.json";
    private const string M678GoNoGoPath = "artifacts/agent-operations/m678/m678-go-no-go.json";

    private const string PublicPackageFreezeDecisionPath = "artifacts/agent-operations/m679/public-package-freeze-decision.json";
    private const string PublicPackageFreezeReadinessPath = "artifacts/agent-operations/m679/public-package-freeze-readiness.json";
    private const string PublicReleaseNoGoProofPath = "artifacts/agent-operations/m679/public-release-no-go-proof.json";
    private const string ChromeWebStoreNoGoProofPath = "artifacts/agent-operations/m679/chrome-web-store-no-go-proof.json";
    private const string NextMilestoneRecommendationPath = "artifacts/agent-operations/m679/next-milestone-recommendation.json";
    private const string M679GoNoGoPath = "artifacts/agent-operations/m679/m679-go-no-go.json";

    private const string ConsolidatedGoNoGoPath = "artifacts/agent-operations/m677-m679/manual-qa-evidence-completion-go-no-go.json";
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
    public void M677ArtifactsExist()
    {
        Assert.IsTrue(File.Exists(FullPath(M677ReportPath)), M677ReportPath);
        Assert.IsTrue(File.Exists(FullPath(ManualQaEvidenceCompletionPath)), ManualQaEvidenceCompletionPath);
        Assert.IsTrue(File.Exists(FullPath(PublicVariantLoadEvidencePath)), PublicVariantLoadEvidencePath);
        Assert.IsTrue(File.Exists(FullPath(ManifestSelectionHumanProofPath)), ManifestSelectionHumanProofPath);
        Assert.IsTrue(File.Exists(FullPath(PermissionWarningHumanProofPath)), PermissionWarningHumanProofPath);
        Assert.IsTrue(File.Exists(FullPath(RuntimeTabHumanProofPath)), RuntimeTabHumanProofPath);
        Assert.IsTrue(File.Exists(FullPath(ServiceWorkerDevtoolsHumanProofPath)), ServiceWorkerDevtoolsHumanProofPath);
        Assert.IsTrue(File.Exists(FullPath(CspConsoleHumanProofPath)), CspConsoleHumanProofPath);
        Assert.IsTrue(File.Exists(FullPath(BridgeLivenessHumanProofPath)), BridgeLivenessHumanProofPath);
        Assert.IsTrue(File.Exists(FullPath(OriginBehaviorHumanProofPath)), OriginBehaviorHumanProofPath);
        Assert.IsTrue(File.Exists(FullPath(EvidenceRedactionHumanProofPath)), EvidenceRedactionHumanProofPath);
        Assert.IsTrue(File.Exists(FullPath(M677GoNoGoPath)), M677GoNoGoPath);
    }

    [TestMethod]
    public void M678ArtifactsExist()
    {
        Assert.IsTrue(File.Exists(FullPath(M678ReportPath)), M678ReportPath);
        Assert.IsTrue(File.Exists(FullPath(FinalPackageAuditRetryPath)), FinalPackageAuditRetryPath);
        Assert.IsTrue(File.Exists(FullPath(PublicManifestSelectionAuditPath)), PublicManifestSelectionAuditPath);
        Assert.IsTrue(File.Exists(FullPath(PublicPermissionsAuditPath)), PublicPermissionsAuditPath);
        Assert.IsTrue(File.Exists(FullPath(ManualQaCompletenessAuditPath)), ManualQaCompletenessAuditPath);
        Assert.IsTrue(File.Exists(FullPath(StoreDisclosureReadinessAuditPath)), StoreDisclosureReadinessAuditPath);
        Assert.IsTrue(File.Exists(FullPath(PackageFreezeRiskRegisterPath)), PackageFreezeRiskRegisterPath);
        Assert.IsTrue(File.Exists(FullPath(M678GoNoGoPath)), M678GoNoGoPath);
    }

    [TestMethod]
    public void M679ArtifactsExist()
    {
        Assert.IsTrue(File.Exists(FullPath(M679ReportPath)), M679ReportPath);
        Assert.IsTrue(File.Exists(FullPath(PublicPackageFreezeDecisionPath)), PublicPackageFreezeDecisionPath);
        Assert.IsTrue(File.Exists(FullPath(PublicPackageFreezeReadinessPath)), PublicPackageFreezeReadinessPath);
        Assert.IsTrue(File.Exists(FullPath(PublicReleaseNoGoProofPath)), PublicReleaseNoGoProofPath);
        Assert.IsTrue(File.Exists(FullPath(ChromeWebStoreNoGoProofPath)), ChromeWebStoreNoGoProofPath);
        Assert.IsTrue(File.Exists(FullPath(NextMilestoneRecommendationPath)), NextMilestoneRecommendationPath);
        Assert.IsTrue(File.Exists(FullPath(M679GoNoGoPath)), M679GoNoGoPath);
    }

    [TestMethod]
    public void ConsolidatedM677M679GoNoGoExists() =>
        Assert.IsTrue(File.Exists(FullPath(ConsolidatedGoNoGoPath)), ConsolidatedGoNoGoPath);

    [TestMethod]
    public void M677ClassifiesBlankHumanTemplateAsInsufficient()
    {
        using var completion = ReadJson(ManualQaEvidenceCompletionPath);
        using var load = ReadJson(PublicVariantLoadEvidencePath);
        using var manifest = ReadJson(ManifestSelectionHumanProofPath);
        using var runtime = ReadJson(RuntimeTabHumanProofPath);
        using var devtools = ReadJson(ServiceWorkerDevtoolsHumanProofPath);
        using var redaction = ReadJson(EvidenceRedactionHumanProofPath);
        Assert.IsFalse(completion.RootElement.GetProperty("manualQaEvidenceReceived").GetBoolean());
        Assert.IsTrue(completion.RootElement.GetProperty("inputWasBlankTemplate").GetBoolean());
        Assert.AreEqual("INSUFFICIENT", load.RootElement.GetProperty("classification").GetString());
        Assert.AreEqual("INSUFFICIENT", manifest.RootElement.GetProperty("classification").GetString());
        Assert.AreEqual("INSUFFICIENT", runtime.RootElement.GetProperty("runtimeTabEvidence").GetString());
        Assert.AreEqual("INSUFFICIENT", devtools.RootElement.GetProperty("serviceWorkerDevtoolsEvidence").GetString());
        Assert.IsTrue(redaction.RootElement.GetProperty("evidenceRedacted").GetBoolean());
    }

    [TestMethod]
    public void M678AuditKeepsFreezeBlocked()
    {
        using var audit = ReadJson(FinalPackageAuditRetryPath);
        using var manifest = ReadJson(PublicManifestSelectionAuditPath);
        using var permissions = ReadJson(PublicPermissionsAuditPath);
        using var completeness = ReadJson(ManualQaCompletenessAuditPath);
        using var store = ReadJson(StoreDisclosureReadinessAuditPath);
        using var risks = ReadJson(PackageFreezeRiskRegisterPath);
        Assert.IsTrue(audit.RootElement.GetProperty("publicPackageCandidatePrepared").GetBoolean());
        Assert.IsFalse(audit.RootElement.GetProperty("packageFreezeReady").GetBoolean());
        Assert.IsTrue(manifest.RootElement.GetProperty("manifestSelectionVerifiedStatic").GetBoolean());
        Assert.IsFalse(manifest.RootElement.GetProperty("manifestSelectionVerifiedHuman").GetBoolean());
        Assert.IsTrue(permissions.RootElement.GetProperty("publicHostPermissionsNarrowed").GetBoolean());
        Assert.IsFalse(permissions.RootElement.GetProperty("containsHttpWildcard").GetBoolean());
        Assert.IsFalse(permissions.RootElement.GetProperty("containsHttpsWildcard").GetBoolean());
        Assert.IsFalse(completeness.RootElement.GetProperty("manualQaEvidenceSufficient").GetBoolean());
        Assert.IsFalse(store.RootElement.GetProperty("storeSubmissionReady").GetBoolean());
        Assert.IsGreaterThanOrEqualTo(3, risks.RootElement.GetProperty("risks").GetArrayLength());
    }

    [TestMethod]
    public void M679DecisionKeepsPackageFreezeNoGo()
    {
        using var decision = ReadJson(PublicPackageFreezeDecisionPath);
        using var readiness = ReadJson(PublicPackageFreezeReadinessPath);
        using var release = ReadJson(PublicReleaseNoGoProofPath);
        using var store = ReadJson(ChromeWebStoreNoGoProofPath);
        using var next = ReadJson(NextMilestoneRecommendationPath);
        Assert.IsFalse(decision.RootElement.GetProperty("packageFreezeReady").GetBoolean());
        Assert.IsFalse(decision.RootElement.GetProperty("manualQaSufficient").GetBoolean());
        Assert.IsFalse(decision.RootElement.GetProperty("remediationRequired").GetBoolean());
        Assert.IsFalse(readiness.RootElement.GetProperty("publicPackageFreezeReady").GetBoolean());
        Assert.IsFalse(release.RootElement.GetProperty("publicReleaseReady").GetBoolean());
        Assert.IsFalse(store.RootElement.GetProperty("chromeWebStoreReady").GetBoolean());
        Assert.AreEqual("M680-M682 Manual QA Evidence Completion Retry", next.RootElement.GetProperty("recommendedNextMilestone").GetString());
    }

    [TestMethod]
    public void ConsolidatedGoNoGoUsesAllowedDecisionAndNoGoStatus()
    {
        using var doc = ReadJson(ConsolidatedGoNoGoPath);
        var decision = doc.RootElement.GetProperty("decision").GetString();
        CollectionAssert.Contains(new[]
        {
            "HUMAN_MANUAL_QA_EVIDENCE_READY",
            "MANUAL_QA_EVIDENCE_STILL_INCOMPLETE",
            "PUBLIC_VARIANT_QA_REMEDIATION_REQUIRED"
        }, decision);

        Assert.AreEqual("MANUAL_QA_EVIDENCE_STILL_INCOMPLETE", decision);
        Assert.IsTrue(doc.RootElement.GetProperty("internalCandidateReady").GetBoolean());
        Assert.IsTrue(doc.RootElement.GetProperty("publicBuildCandidateReady").GetBoolean());
        Assert.IsFalse(doc.RootElement.GetProperty("manualQaEvidenceSufficient").GetBoolean());
        Assert.IsFalse(doc.RootElement.GetProperty("publicPackageFreezeReady").GetBoolean());
        Assert.IsFalse(doc.RootElement.GetProperty("publicReleaseReady").GetBoolean());
        Assert.IsFalse(doc.RootElement.GetProperty("chromeWebStoreReady").GetBoolean());
    }

    [TestMethod]
    public void GoNoGosKeepSensitiveCapabilitiesDisabled()
    {
        foreach (var path in new[] { M677GoNoGoPath, M678GoNoGoPath, M679GoNoGoPath, ConsolidatedGoNoGoPath })
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
