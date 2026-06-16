using System.Text.Json;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using OneBrain.BrowserExecutor.Cdp;
using OneBrain.BrowserExecutor.Contracts;

namespace OneBrain.Safety.Tests;

[TestClass]
public sealed class NodalOsProcessMemoryM142M144Tests
{
    [TestMethod]
    public void ProcessMemoryValidFixtureEntryAccepted()
    {
        var memory = new NodalOsProcessMemoryFixtureHarness().RunDefaultFixtures();
        var entry = memory.Entries.Single(e => e.EntryId.StartsWith("memory-entry-readiness-review-workflow", StringComparison.Ordinal));

        Assert.IsTrue(entry.Accepted);
        Assert.AreEqual(NodalOsMemoryScope.LocalFixtureOnly, entry.Scope);
        Assert.AreEqual(NodalOsMemoryConfidence.VerifiedFixturePattern, entry.Step.Confidence);
        Assert.IsFalse(entry.ActionAuthorityGranted);
    }

    [TestMethod]
    public void ProcessMemoryRedactionRejectsCredentialTokenCookieSyntheticEntries()
    {
        var evaluator = new NodalOsProcessMemoryEvaluator();
        var action = new NodalOsSafeActionFixtureHarness().RunDefaultFixtures().First(r => r.Action.ActionId == "observe-only-local");
        var workflow = new NodalOsWorkflowFixture(
            "synthetic-sensitive-memory",
            NodalOsMemoryScope.LocalFixtureOnly,
            [action],
            ContainsCredential: true,
            ContainsCookie: true,
            ContainsToken: true,
            ContainsPaymentInfo: false,
            ContainsPersonalOrCustomerData: false,
            ContainsRawDomOrBody: false,
            ContainsRawUiaSensitiveTree: false,
            ContainsUnredactedLogs: false,
            ContainsSubmitPayload: false,
            ContainsScreenshotWithSecret: false,
            RecorderReplayProductiveRequested: false,
            AmbiguousPerception: false,
            "Reject synthetic credential/token/cookie memory.");

        var entry = evaluator.EvaluateStep(workflow, action, 0);

        Assert.IsFalse(entry.Accepted);
        Assert.IsTrue(entry.DeniedReasons.Contains(NodalOsMemoryDeniedReason.CredentialDetected));
        Assert.IsTrue(entry.DeniedReasons.Contains(NodalOsMemoryDeniedReason.CookieDetected));
        Assert.IsTrue(entry.DeniedReasons.Contains(NodalOsMemoryDeniedReason.TokenDetected));
    }

    [TestMethod]
    public void ProcessMemoryRedactionRejectsRawDomBodyAndUnredactedLogs()
    {
        var denied = NodalOsProcessMemoryEvaluator.DetectDeniedReasons(
            MakeDeniedWorkflow(rawDom: true, rawUia: true, unredactedLogs: true));

        Assert.IsTrue(denied.Contains(NodalOsMemoryDeniedReason.RawDomOrBodyDetected));
        Assert.IsTrue(denied.Contains(NodalOsMemoryDeniedReason.RawUiaSensitiveTreeDetected));
        Assert.IsTrue(denied.Contains(NodalOsMemoryDeniedReason.UnredactedLogDetected));
    }

    [TestMethod]
    public void ProcessMemoryProductionScopeRejected()
    {
        var memory = new NodalOsProcessMemoryFixtureHarness().RunDefaultFixtures();
        var entries = memory.Entries.Where(e => e.Scope == NodalOsMemoryScope.BlockedProduction).ToArray();

        Assert.IsTrue(entries.Length > 0);
        Assert.IsTrue(entries.All(e => !e.Accepted));
        Assert.IsTrue(entries.Any(e => e.DeniedReasons.Contains(NodalOsMemoryDeniedReason.ProductionScopeBlocked)));
    }

    [TestMethod]
    public void ProcessMemoryHighConfidenceDoesNotGrantActionAuthority()
    {
        var memory = new NodalOsProcessMemoryFixtureHarness().RunDefaultFixtures();

        Assert.IsFalse(memory.ActionAuthorityGranted);
        Assert.IsTrue(memory.Entries.All(e => e.ActionAuthorityGranted == false));
        Assert.IsTrue(memory.Entries.All(e => e.CoreApprovalStillRequired));
    }

    [TestMethod]
    public void ProcessMemoryLocalOnlyScopeEnforced()
    {
        var review = new NodalOsProcessMemoryFixtureHarness().BuildEvidenceReview();

        Assert.IsTrue(review.MemorySummary.MemoryLocalOnlyReady);
        Assert.IsTrue(review.WorkflowSummary.LocalOnly);
        Assert.IsTrue(review.MemorySummary.ProductionLearningBlocked);
        Assert.IsTrue(review.MemorySummary.RecorderReplayProductiveBlocked);
    }

    [TestMethod]
    public void WorkflowLearningReadinessWorkflowCreatesRedactedMemory()
    {
        var pattern = Pattern("readiness-review-workflow");

        Assert.AreEqual(NodalOsMemoryConfidence.VerifiedRedactedLocalPattern, pattern.Confidence);
        Assert.IsTrue(pattern.Redacted);
        Assert.IsFalse(pattern.RecorderReplayProductiveEnabled);
    }

    [TestMethod]
    public void WorkflowLearningFixtureHarnessCreatesLocalOnlyPatterns()
    {
        var memory = new NodalOsProcessMemoryFixtureHarness().RunDefaultFixtures();

        Assert.IsTrue(memory.LocalOnly);
        Assert.IsTrue(memory.Patterns.Any(p => p.WorkflowId == "readiness-review-workflow"));
        Assert.IsTrue(memory.Patterns.Any(p => p.WorkflowId == "diagnostics-review-workflow"));
        Assert.IsTrue(memory.Patterns.Any(p => p.WorkflowId == "issue-triage-workflow"));
        Assert.IsFalse(memory.ActionAuthorityGranted);
    }

    [TestMethod]
    public void WorkflowLearningDiagnosticsAndIssueTriageCreateRedactedMemory()
    {
        var diagnostics = Pattern("diagnostics-review-workflow");
        var issue = Pattern("issue-triage-workflow");

        Assert.AreEqual(NodalOsMemoryConfidence.VerifiedRedactedLocalPattern, diagnostics.Confidence);
        Assert.AreEqual(NodalOsMemoryConfidence.VerifiedRedactedLocalPattern, issue.Confidence);
        Assert.IsTrue(diagnostics.EvidenceRefs.All(r => r.Contains("redacted", StringComparison.Ordinal)));
    }

    [TestMethod]
    public void WorkflowLearningBlockedCredentialWorkflowCreatesDeniedMemoryNoSensitiveContent()
    {
        var pattern = Pattern("blocked-credential-workflow");
        var json = JsonSerializer.Serialize(pattern);

        Assert.IsTrue(pattern.DeniedReasons.Contains(NodalOsMemoryDeniedReason.CredentialDetected));
        Assert.IsFalse(json.Contains("synthetic-cookie-session-value", StringComparison.Ordinal));
        Assert.IsFalse(json.Contains("synthetic-api-key-value", StringComparison.Ordinal));
        Assert.IsFalse(json.Contains("synthetic-bearer-token", StringComparison.Ordinal));
    }

    [TestMethod]
    public void WorkflowLearningBlockedSubmitPaymentDeleteWorkflowBlocked()
    {
        Assert.IsTrue(Pattern("blocked-submit-workflow").DeniedReasons.Contains(NodalOsMemoryDeniedReason.SubmitPayloadDetected));
        Assert.IsTrue(Pattern("blocked-payment-workflow").DeniedReasons.Contains(NodalOsMemoryDeniedReason.PaymentInfoDetected));
        Assert.IsTrue(Pattern("blocked-delete-sign-workflow").DeniedReasons.Contains(NodalOsMemoryDeniedReason.SubmitPayloadDetected));
    }

    [TestMethod]
    public void WorkflowLearningAmbiguousPerceptionRequiresHumanReview()
    {
        var pattern = Pattern("ambiguous-perception-workflow");

        StringAssert.Contains(pattern.RecommendedNextStep, "Require human review");
        Assert.IsFalse(pattern.RecorderReplayProductiveEnabled);
    }

    [TestMethod]
    public void WorkflowLearningDoesNotEnableProductiveRecorderReplay()
    {
        var pattern = Pattern("blocked-recorder-replay");

        Assert.IsTrue(pattern.DeniedReasons.Contains(NodalOsMemoryDeniedReason.RecorderReplayProductiveBlocked));
        Assert.IsFalse(pattern.RecorderReplayProductiveEnabled);
    }

    [TestMethod]
    public void ProcessMemoryEvidenceSummaryDoesNotContainSecrets()
    {
        var review = new NodalOsProcessMemoryFixtureHarness().BuildEvidenceReview();
        var json = JsonSerializer.Serialize(review);

        Assert.IsTrue(review.Redacted);
        Assert.IsFalse(json.Contains("synthetic-cookie-session-value", StringComparison.Ordinal));
        Assert.IsFalse(json.Contains("synthetic-api-key-value", StringComparison.Ordinal));
        Assert.IsFalse(json.Contains("<html", StringComparison.OrdinalIgnoreCase));
        Assert.IsFalse(review.ContainsSensitiveRawValues);
    }

    [TestMethod]
    public void ProcessMemoryEvidenceProductAdminAndOperatorSummariesIncludeLocalOnly()
    {
        var report = new NodalOsProductAdminPrivatePreviewHardeningService().BuildDefaultReport();
        var summary = new NodalOsOperatorUxReadinessService().BuildDefaultSummary();

        Assert.IsTrue(report.EvidenceRefs.Any(e => e.Contains("process-memory:local-fixture-only-ready", StringComparison.Ordinal)));
        Assert.IsTrue(summary.EvidenceSummary.Any(e => e.Contains("Process memory/workflow learning", StringComparison.Ordinal)));
        Assert.IsTrue(summary.EvidenceSummary.Any(e => e.Contains("productive recorder/replay remains blocked", StringComparison.Ordinal)));
    }

    [TestMethod]
    public void ProcessMemoryEvidenceReleaseGateDoesNotBecomePermissive()
    {
        var decision = new NodalOsLocalPrivatePreviewReleaseGate().Evaluate(NodalOsRuntimeStateProbe.ForCurrentLocalPreview());

        Assert.AreEqual(NodalOsLocalPrivatePreviewReleaseGateStatus.ReadyWithRestrictions, decision.Status);
        Assert.IsTrue(decision.RecorderReplayProductiveStillBlocked);
        Assert.IsTrue(decision.SubmitPaySignDeleteStillBlocked);
        Assert.IsFalse(decision.ExternalGeneralReady);
    }

    [TestMethod]
    public void ProcessMemoryEvidenceIssueCaptureCanReceiveMemoryWarning()
    {
        var issue = new NodalOsPrivatePreviewIssue(
            "pp-memory-001",
            NodalOsPrivatePreviewIssueCategory.ReadinessMismatch,
            NodalOsPrivatePreviewIssueSeverity.Medium,
            NodalOsPrivatePreviewIssueDecision.ShouldFixSoon,
            "Process memory requires human review for ambiguous fixture workflow.",
            BlocksPostRunGo: false,
            Redacted: true);

        Assert.AreEqual(NodalOsPrivatePreviewIssueCategory.ReadinessMismatch, issue.Category);
        Assert.IsFalse(issue.BlocksPostRunGo);
    }

    [TestMethod]
    public void ProcessMemoryEvidenceAdrExists()
    {
        var text = File.ReadAllText(SourcePath("docs", "adr", "process-memory-workflow-learning-local-only-m142-m144.md"));

        StringAssert.Contains(text, "Process Memory");
        StringAssert.Contains(text, "local-only");
        StringAssert.Contains(text, "does not authorize actions");
    }

    [TestMethod]
    public void Hito162ReplacementSequenceShowsM142M144ImplementedWithoutScopeExpansion()
    {
        var text = File.ReadAllText(SourcePath("docs", "roadmap", "nodal-os-hito-162-replacement-sequence.md"));

        StringAssert.Contains(text, "Status after M142-M144: implemented");
        StringAssert.Contains(text, "no productive recorder/replay");
        StringAssert.Contains(text, "No SaaS public");
        StringAssert.Contains(text, "No external CDP general-ready claim");
    }

    private static NodalOsWorkflowPattern Pattern(string workflowId) =>
        new NodalOsProcessMemoryFixtureHarness().RunDefaultFixtures().Patterns.Single(p => p.WorkflowId == workflowId);

    private static NodalOsWorkflowFixture MakeDeniedWorkflow(
        bool rawDom = false,
        bool rawUia = false,
        bool unredactedLogs = false)
    {
        var action = new NodalOsSafeActionFixtureHarness().RunDefaultFixtures().First(r => r.Action.ActionId == "observe-only-local");
        return new NodalOsWorkflowFixture(
            "denied",
            NodalOsMemoryScope.LocalFixtureOnly,
            [action],
            ContainsCredential: false,
            ContainsCookie: false,
            ContainsToken: false,
            ContainsPaymentInfo: false,
            ContainsPersonalOrCustomerData: false,
            ContainsRawDomOrBody: rawDom,
            ContainsRawUiaSensitiveTree: rawUia,
            ContainsUnredactedLogs: unredactedLogs,
            ContainsSubmitPayload: false,
            ContainsScreenshotWithSecret: false,
            RecorderReplayProductiveRequested: false,
            AmbiguousPerception: false,
            "denied");
    }

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
