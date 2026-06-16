using System.Text.Json;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using OneBrain.BrowserExecutor.Cdp;
using OneBrain.BrowserExecutor.Contracts;

namespace OneBrain.Safety.Tests;

[TestClass]
public sealed class NodalOsSafeActionExpansionM139M141Tests
{
    [TestMethod]
    public void SafeActionExpansionObserveOnlyLocalAllowed()
    {
        var record = Evaluate("observe-only-local");

        Assert.AreEqual(NodalOsActionCategory.ObserveOnly, record.Action.Category);
        Assert.AreEqual(NodalOsActionDecision.AllowedObserveOnly, record.Decision);
        Assert.IsFalse(record.SensitiveActionAuthorized);
    }

    [TestMethod]
    public void SafeActionBoundaryDiagnosticsAndEvidenceLocalAllowedWithCore()
    {
        var diagnostics = Evaluate("open-local-diagnostics-panel");
        var evidence = Evaluate("review-redacted-evidence");

        Assert.AreEqual(NodalOsActionDecision.AllowedLocalReadOnlyWithCoreApproval, diagnostics.Decision);
        Assert.AreEqual(NodalOsActionDecision.AllowedLocalReadOnlyWithCoreApproval, evidence.Decision);
        Assert.IsTrue(diagnostics.Action.Boundary.CoreAuthorityRequired);
        Assert.IsTrue(evidence.Action.Boundary.CoreApproved);
    }

    [TestMethod]
    public void SafeActionBoundarySubmitPaymentDeleteSignAlwaysBlocked()
    {
        var fixtures = new[] { "blocked-submit", "blocked-payment", "blocked-delete", "blocked-sign" };

        foreach (var fixture in fixtures)
        {
            var record = Evaluate(fixture);
            Assert.AreEqual(NodalOsActionDecision.BlockedAlways, record.Decision, fixture);
            Assert.AreEqual(NodalOsActionApprovalRequirement.AlwaysBlocked, record.Action.ApprovalRequirement, fixture);
            Assert.IsFalse(record.SensitiveActionAuthorized, fixture);
        }
    }

    [TestMethod]
    public void SafeActionBoundaryCredentialsAlwaysBlocked()
    {
        var record = Evaluate("blocked-credential-entry");

        Assert.AreEqual(NodalOsActionDecision.BlockedAlways, record.Decision);
        Assert.IsTrue(record.DeniedReasons.Contains(NodalOsActionDeniedReason.CredentialEntryBlocked));
    }

    [TestMethod]
    public void SafeActionBoundaryExternalGeneralBlocked()
    {
        var record = Evaluate("blocked-external-general");

        Assert.AreEqual(NodalOsActionDecision.BlockedAlways, record.Decision);
        Assert.IsTrue(record.DeniedReasons.Contains(NodalOsActionDeniedReason.ExternalGeneralBlocked));
        Assert.IsTrue(record.Action.Boundary.ExternalGeneralBlocked);
    }

    [TestMethod]
    public void SafeActionBoundaryHighIdentityPerceptionWithoutCoreDenied()
    {
        var record = Evaluate("high-confidence-without-core");

        Assert.AreEqual(NodalOsIdentityConfidence.VerifiedFixture, record.IdentityConfidence);
        Assert.AreEqual(NodalOsPerceptionReadiness.UsableForReadOnlyContext, record.PerceptionReadiness);
        Assert.AreEqual(NodalOsActionDecision.Denied, record.Decision);
        Assert.IsTrue(record.DeniedReasons.Contains(NodalOsActionDeniedReason.MissingCoreApproval));
    }

    [TestMethod]
    public void SafeActionBoundaryCoreApprovalRequiredAndUiAdminCompanionBlocked()
    {
        var boundary = new NodalOsSafeActionFixtureHarness().BuildBoundaryEvidence();

        Assert.IsTrue(boundary.CoreApprovalBoundaryEnforced);
        Assert.IsTrue(boundary.UiAdminCompanionAuthorityBlocked);
        Assert.IsTrue(boundary.IdentityPerceptionNonAuthoritative);
        Assert.IsTrue(boundary.DangerousSurfacesBlocked);
    }

    [TestMethod]
    public void SafeActionFixtureHarnessAllowsLocalReadinessDiagnosticsDraftOnly()
    {
        var readiness = Evaluate("open-local-readiness-panel");
        var diagnostics = Evaluate("open-local-diagnostics-panel");
        var draft = Evaluate("local-draft-only-note");

        Assert.AreEqual(NodalOsActionDecision.AllowedLocalReadOnlyWithCoreApproval, readiness.Decision);
        Assert.AreEqual(NodalOsActionDecision.AllowedLocalReadOnlyWithCoreApproval, diagnostics.Decision);
        Assert.AreEqual(NodalOsActionDecision.AllowedLocalDraftOnlyWithCoreApproval, draft.Decision);
    }

    [TestMethod]
    public void SafeActionFixtureHarnessCopyToClipboardAllowedOnlyIfRedacted()
    {
        var copy = Evaluate("copy-redacted-log-summary");
        var unredacted = Evaluate("unredacted-evidence-review");

        Assert.AreEqual(NodalOsActionDecision.AllowedLocalReadOnlyWithCoreApproval, copy.Decision);
        Assert.AreEqual(NodalOsActionDecision.Denied, unredacted.Decision);
        Assert.IsTrue(unredacted.DeniedReasons.Contains(NodalOsActionDeniedReason.EvidenceNotRedacted));
    }

    [TestMethod]
    public void SafeActionFixtureHarnessSensitiveOverlayAmbiguousBlocked()
    {
        var sensitive = Evaluate("blocked-sensitive-surface");
        var overlay = Evaluate("overlay-blocked-action");
        var ambiguous = Evaluate("ambiguous-identity-action");

        Assert.AreEqual(NodalOsActionDecision.BlockedAlways, sensitive.Decision);
        Assert.AreEqual(NodalOsActionDecision.Denied, overlay.Decision);
        Assert.AreEqual(NodalOsActionDecision.Denied, ambiguous.Decision);
        Assert.IsTrue(overlay.DeniedReasons.Contains(NodalOsActionDeniedReason.OverlayBlocked));
        Assert.IsTrue(ambiguous.DeniedReasons.Contains(NodalOsActionDeniedReason.IdentityNotVerified) ||
            ambiguous.DeniedReasons.Contains(NodalOsActionDeniedReason.PerceptionNotUsable));
    }

    [TestMethod]
    public void SafeActionEvidenceSummaryDoesNotContainSecrets()
    {
        var record = Evaluate("review-redacted-evidence");
        var json = JsonSerializer.Serialize(record.Evidence);

        Assert.IsTrue(record.Evidence.Redacted);
        StringAssert.Contains(record.Evidence.RedactionSummary, "no credentials, cookies, tokens");
        Assert.IsFalse(json.Contains("synthetic-cookie-session-value", StringComparison.Ordinal));
        Assert.IsFalse(json.Contains("synthetic-api-key-value", StringComparison.Ordinal));
        Assert.IsFalse(json.Contains("synthetic-bearer-token", StringComparison.Ordinal));
    }

    [TestMethod]
    public void SafeActionEvidenceSummaryListsAllowedAndBlockedActions()
    {
        var summary = new NodalOsSafeActionFixtureHarness().BuildSummary();

        Assert.IsTrue(summary.AllowedCategories.Contains(NodalOsActionCategory.LocalPanelOpen));
        Assert.IsTrue(summary.AllowedCategories.Contains(NodalOsActionCategory.LocalDiagnosticsOpen));
        Assert.IsTrue(summary.BlockedCategories.Contains(NodalOsActionCategory.BlockedSubmit));
        Assert.IsTrue(summary.BlockedCategories.Contains(NodalOsActionCategory.BlockedPayment));
        Assert.IsFalse(summary.SensitiveActionsAuthorized);
    }

    [TestMethod]
    public void SafeActionEvidenceBlockedActionGeneratesOperatorExplanation()
    {
        var record = Evaluate("blocked-payment");

        Assert.AreEqual(NodalOsActionDecision.BlockedAlways, record.Decision);
        StringAssert.Contains(record.OperatorExplanation, "Payment is always blocked");
        Assert.IsFalse(record.SensitiveActionAuthorized);
    }

    [TestMethod]
    public void SafeActionEvidenceCanFeedIssueCaptureWithDeniedReason()
    {
        var record = Evaluate("blocked-submit");
        var issue = new NodalOsPrivatePreviewIssue(
            "pp-action-001",
            NodalOsPrivatePreviewIssueCategory.ScopeInflationRisk,
            NodalOsPrivatePreviewIssueSeverity.High,
            NodalOsPrivatePreviewIssueDecision.MustFixBeforeNextRun,
            $"Safe action denied: {record.DeniedReasons[0]}",
            BlocksPostRunGo: true,
            Redacted: true);

        Assert.IsTrue(record.DeniedReasons.Contains(NodalOsActionDeniedReason.SubmitBlocked));
        Assert.IsTrue(issue.BlocksPostRunGo);
    }

    [TestMethod]
    public void SafeActionExpansionOperatorSummaryShowsAllowedAndBlockedActions()
    {
        var summary = new NodalOsOperatorUxReadinessService().BuildDefaultSummary();

        Assert.IsTrue(summary.EvidenceSummary.Any(e => e.Contains("Safe action expansion", StringComparison.Ordinal)));
        Assert.IsTrue(summary.BlockedExternalSensitiveActions.Any(e => e.Contains("submit/pay/sign/delete", StringComparison.OrdinalIgnoreCase)));
    }

    [TestMethod]
    public void SafeActionExpansionProductAdminIncludesBoundaryReady()
    {
        var report = new NodalOsProductAdminPrivatePreviewHardeningService().BuildDefaultReport();

        Assert.IsTrue(report.EvidenceRefs.Any(e => e.Contains("safe-action:local-fixture-boundary-ready", StringComparison.Ordinal)));
        Assert.IsTrue(report.CoreAuthorityRequired);
        Assert.IsTrue(report.UiAdminAuthorityBlocked);
    }

    [TestMethod]
    public void SafeActionExpansionReleaseGateDoesNotBecomePermissive()
    {
        var decision = new NodalOsLocalPrivatePreviewReleaseGate().Evaluate(NodalOsRuntimeStateProbe.ForCurrentLocalPreview());

        Assert.AreEqual(NodalOsLocalPrivatePreviewReleaseGateStatus.ReadyWithRestrictions, decision.Status);
        Assert.IsTrue(decision.SubmitPaySignDeleteStillBlocked);
        Assert.IsTrue(decision.RealCredentialsStillBlocked);
        Assert.IsFalse(decision.ExternalGeneralReady);
    }

    [TestMethod]
    public void SafeActionExpansionAdrExists()
    {
        var text = File.ReadAllText(SourcePath("docs", "adr", "safe-action-expansion-local-fixtures-m139-m141.md"));

        StringAssert.Contains(text, "Safe Action Expansion");
        StringAssert.Contains(text, "Core Boundary");
        StringAssert.Contains(text, "M142-M144");
    }

    [TestMethod]
    public void Hito162ReplacementSequenceShowsM139M141ImplementedWithoutScopeExpansion()
    {
        var text = File.ReadAllText(SourcePath("docs", "roadmap", "nodal-os-hito-162-replacement-sequence.md"));

        StringAssert.Contains(text, "Status after M139-M141: implemented");
        StringAssert.Contains(text, "does not execute or authorize sensitive real actions");
        StringAssert.Contains(text, "No SaaS public");
        StringAssert.Contains(text, "No external CDP general-ready claim");
    }

    private static NodalOsSafeActionRunRecord Evaluate(string fixtureId) =>
        new NodalOsSafeActionFixtureHarness().RunDefaultFixtures()
            .Single(r => r.RunId == $"safe-action-run-{fixtureId}");

    private static string SourcePath(params string[] relativePath)
    {
        var current = new DirectoryInfo(AppContext.BaseDirectory);
        while (current is not null)
        {
            if (File.Exists(Path.Combine(current.FullName, "OneBrain.slnx")))
                return Path.Combine(new[] { current.FullName }.Concat(relativePath).ToArray());
            current = current.Parent;
        }

        Assert.Fail("Could not locate repository root.");
        return "";
    }
}
