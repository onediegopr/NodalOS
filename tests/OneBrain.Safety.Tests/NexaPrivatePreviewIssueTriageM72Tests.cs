using Microsoft.VisualStudio.TestTools.UnitTesting;
using OneBrain.BrowserExecutor.Cdp;
using OneBrain.BrowserExecutor.Contracts;

namespace OneBrain.Safety.Tests;

[TestClass]
public sealed class NexaPrivatePreviewIssueTriageM72Tests
{
    [TestMethod]
    public void NexaPrivatePreviewIssueTriageClassifiesSecurityLeakAsBlocker() =>
        AssertDecision(Issue(secretLeak: true), NexaPrivatePreviewIssueDecisionKind.SecurityBlocker);

    [TestMethod]
    public void NexaPrivatePreviewIssueTriageClassifiesCrossTenantAsSecurityBlocker() =>
        AssertDecision(Issue(crossTenant: true), NexaPrivatePreviewIssueDecisionKind.SecurityBlocker);

    [TestMethod]
    public void NexaPrivatePreviewIssueTriageClassifiesVaultRawExposureAsSecurityBlocker() =>
        AssertDecision(Issue(vaultRaw: true), NexaPrivatePreviewIssueDecisionKind.SecurityBlocker);

    [TestMethod]
    public void NexaPrivatePreviewIssueTriageClassifiesPublicApiExposureAsSecurityBlocker() =>
        AssertDecision(Issue(publicApi: true), NexaPrivatePreviewIssueDecisionKind.SecurityBlocker);

    [TestMethod]
    public void NexaPrivatePreviewIssueTriageClassifiesBuildFailureAsReleaseBlocker() =>
        AssertDecision(Issue(buildFailed: true), NexaPrivatePreviewIssueDecisionKind.ReleaseBlocker);

    [TestMethod]
    public void NexaPrivatePreviewIssueTriageClassifiesGateFailureAsReleaseBlocker() =>
        AssertDecision(Issue(gateFailed: true), NexaPrivatePreviewIssueDecisionKind.ReleaseBlocker);

    [TestMethod]
    public void NexaPrivatePreviewIssueTriageAllowsLowUxIssue()
    {
        var decision = Triage(Issue(category: NexaPrivatePreviewIssueCategory.Ux, severity: NexaPrivatePreviewIssueSeverity.Low));

        Assert.AreEqual(NexaPrivatePreviewIssueDecisionKind.Accept, decision.Decision);
        Assert.AreEqual(NexaPrivatePreviewIssueDisposition.Accepted, decision.Disposition);
    }

    [TestMethod]
    public void NexaPrivatePreviewIssueTriageRequiresActionForHighSeverity()
    {
        var decision = Triage(Issue(severity: NexaPrivatePreviewIssueSeverity.High));

        Assert.AreEqual(NexaPrivatePreviewIssueDecisionKind.FixBeforeNextPreview, decision.Decision);
        Assert.IsTrue(decision.Actions.Any(action => action.RequiredBeforeNextPreview));
    }

    [TestMethod]
    public void NexaPrivatePreviewIssueTriageDoesNotExposeSecretsInReport()
    {
        var decision = Triage(Issue(summary: "synthetic-password-value synthetic-cookie-session-value", containsSensitive: true));
        var serialized = System.Text.Json.JsonSerializer.Serialize(decision);

        Assert.IsTrue(decision.Redacted);
        Assert.IsFalse(serialized.Contains("synthetic-password-value", StringComparison.Ordinal));
        Assert.IsFalse(serialized.Contains("synthetic-cookie-session-value", StringComparison.Ordinal));
    }

    private static void AssertDecision(NexaPrivatePreviewIssueTriage issue, NexaPrivatePreviewIssueDecisionKind expected) =>
        Assert.AreEqual(expected, Triage(issue).Decision);

    private static NexaPrivatePreviewIssueDecision Triage(NexaPrivatePreviewIssueTriage issue) =>
        new NexaPrivatePreviewIssueTriageService().Triage(issue);

    private static NexaPrivatePreviewIssueTriage Issue(
        NexaPrivatePreviewIssueCategory category = NexaPrivatePreviewIssueCategory.Product,
        NexaPrivatePreviewIssueSeverity severity = NexaPrivatePreviewIssueSeverity.Medium,
        string summary = "redacted issue summary",
        bool secretLeak = false,
        bool crossTenant = false,
        bool vaultRaw = false,
        bool publicApi = false,
        bool buildFailed = false,
        bool gateFailed = false,
        bool containsSensitive = false) =>
        new(
            "triage-local",
            category,
            severity,
            BrowserCredentialRedactor.Redact(summary),
            SecretCookieBodyLeak: secretLeak,
            CrossTenantAccess: crossTenant,
            VaultRawExposure: vaultRaw,
            SupportCanSeeSecret: false,
            PublicApiExposure: publicApi,
            RealBillingEmailEnabled: false,
            SensitiveRealPilotEnabled: false,
            BuildOrTestFailed: buildFailed,
            GateFailed: gateFailed,
            DiagnosticsUnavailable: false,
            AuditIntegrityUnavailable: false,
            RunbookMissing: false,
            ApiRoleEnforcementBroken: false,
            ContainsSecretsCookiesBodies: containsSensitive);
}
