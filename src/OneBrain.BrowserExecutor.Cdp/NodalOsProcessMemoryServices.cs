using OneBrain.BrowserExecutor.Contracts;

namespace OneBrain.BrowserExecutor.Cdp;

public sealed class NodalOsProcessMemoryEvaluator
{
    public NodalOsProcessMemoryEntry EvaluateStep(
        NodalOsWorkflowFixture workflow,
        NodalOsSafeActionRunRecord record,
        int index)
    {
        var denied = DetectDeniedReasons(workflow);
        var accepted = denied.Count == 0 &&
            workflow.Scope is NodalOsMemoryScope.LocalFixtureOnly or NodalOsMemoryScope.PrivatePreviewLocal or NodalOsMemoryScope.TargetOwnedRedacted;
        var confidence = accepted
            ? workflow.Scope == NodalOsMemoryScope.LocalFixtureOnly
                ? NodalOsMemoryConfidence.VerifiedFixturePattern
                : NodalOsMemoryConfidence.VerifiedRedactedLocalPattern
            : NodalOsMemoryConfidence.Low;
        var step = new NodalOsWorkflowStepMemory(
            $"step-{workflow.WorkflowId}-{index}",
            record.Action.Category.ToString(),
            record.Action.ActionId,
            $"identity:{record.Action.ActionId}:redacted",
            $"perception:{record.Action.ActionId}:redacted",
            record.Evidence.EvidenceRef,
            record.Decision.ToString(),
            record.DeniedReasons.Count > 0 ? $"issue:{workflow.WorkflowId}:denied:redacted" : null,
            DateTimeOffset.UtcNow,
            confidence,
            record.Evidence.EvidenceRefs);

        return new NodalOsProcessMemoryEntry(
            $"memory-entry-{workflow.WorkflowId}-{index}",
            workflow.Scope,
            step,
            [
                NodalOsMemoryRedactionPolicy.RedactedMetadataOnly,
                NodalOsMemoryRedactionPolicy.RedactedLocalFixtureOnly,
                NodalOsMemoryRedactionPolicy.RejectSensitiveRawValues,
                NodalOsMemoryRedactionPolicy.RejectProductionLearning
            ],
            denied,
            ActionAuthorityGranted: false,
            CoreApprovalStillRequired: true,
            accepted,
            Redacted: true);
    }

    public static IReadOnlyList<NodalOsMemoryDeniedReason> DetectDeniedReasons(NodalOsWorkflowFixture workflow)
    {
        var denied = new List<NodalOsMemoryDeniedReason>();
        if (workflow.ContainsCredential || workflow.Scope == NodalOsMemoryScope.BlockedCredentials)
            denied.Add(NodalOsMemoryDeniedReason.CredentialDetected);
        if (workflow.ContainsCookie)
            denied.Add(NodalOsMemoryDeniedReason.CookieDetected);
        if (workflow.ContainsToken)
            denied.Add(NodalOsMemoryDeniedReason.TokenDetected);
        if (workflow.ContainsPaymentInfo)
            denied.Add(NodalOsMemoryDeniedReason.PaymentInfoDetected);
        if (workflow.ContainsPersonalOrCustomerData)
            denied.Add(NodalOsMemoryDeniedReason.CustomerDataDetected);
        if (workflow.ContainsRawDomOrBody)
            denied.Add(NodalOsMemoryDeniedReason.RawDomOrBodyDetected);
        if (workflow.ContainsRawUiaSensitiveTree)
            denied.Add(NodalOsMemoryDeniedReason.RawUiaSensitiveTreeDetected);
        if (workflow.ContainsUnredactedLogs)
            denied.Add(NodalOsMemoryDeniedReason.UnredactedLogDetected);
        if (workflow.ContainsSubmitPayload)
            denied.Add(NodalOsMemoryDeniedReason.SubmitPayloadDetected);
        if (workflow.ContainsScreenshotWithSecret)
            denied.Add(NodalOsMemoryDeniedReason.ScreenshotWithSecretDetected);
        if (workflow.Scope == NodalOsMemoryScope.BlockedProduction)
            denied.Add(NodalOsMemoryDeniedReason.ProductionScopeBlocked);
        if (workflow.Scope == NodalOsMemoryScope.BlockedExternalGeneral)
            denied.Add(NodalOsMemoryDeniedReason.ExternalGeneralBlocked);
        if (workflow.Scope == NodalOsMemoryScope.BlockedSensitive)
            denied.Add(NodalOsMemoryDeniedReason.SensitiveScopeBlocked);
        if (workflow.RecorderReplayProductiveRequested)
            denied.Add(NodalOsMemoryDeniedReason.RecorderReplayProductiveBlocked);
        return denied.Distinct().ToArray();
    }
}

public sealed class NodalOsWorkflowPatternExtractor
{
    public NodalOsWorkflowPattern Extract(NodalOsWorkflowFixture workflow, IReadOnlyList<NodalOsProcessMemoryEntry> entries)
    {
        var denied = entries.SelectMany(e => e.DeniedReasons).Distinct().ToArray();
        var accepted = denied.Length == 0 && entries.All(e => e.Accepted);
        return new NodalOsWorkflowPattern(
            workflow.WorkflowId,
            entries.Select(e => e.Step).ToArray(),
            workflow.Scope,
            accepted ? NodalOsMemoryConfidence.VerifiedRedactedLocalPattern : NodalOsMemoryConfidence.Low,
            entries.Where(e => e.Accepted).Select(e => e.Step.OperatorDecisionRef).Distinct().ToArray(),
            entries.Where(e => !e.Accepted).Select(e => e.Step.OperatorDecisionRef).Distinct().ToArray(),
            denied,
            entries.SelectMany(e => e.Step.EvidenceRefs).Distinct(StringComparer.OrdinalIgnoreCase).ToArray(),
            workflow.AmbiguousPerception ? "Require human review before learning this workflow." : workflow.RecommendedNextStep,
            RecorderReplayProductiveEnabled: false,
            Redacted: true);
    }
}

public sealed class NodalOsWorkflowLearningEvaluator
{
    private readonly NodalOsProcessMemoryEvaluator memoryEvaluator = new();
    private readonly NodalOsWorkflowPatternExtractor extractor = new();

    public NodalOsWorkflowPattern Learn(NodalOsWorkflowFixture workflow)
    {
        var entries = workflow.ActionRecords.Select((record, index) => memoryEvaluator.EvaluateStep(workflow, record, index)).ToArray();
        return extractor.Extract(workflow, entries);
    }
}

public sealed class NodalOsProcessMemoryFixtureHarness
{
    private readonly NodalOsWorkflowLearningEvaluator learning = new();

    public NodalOsProcessMemory RunDefaultFixtures()
    {
        var workflows = CreateDefaultFixtures();
        var patterns = workflows.Select(learning.Learn).ToArray();
        var entries = workflows.SelectMany(w => w.ActionRecords.Select((r, i) => new NodalOsProcessMemoryEvaluator().EvaluateStep(w, r, i))).ToArray();
        return new NodalOsProcessMemory(
            "process-memory-m142-m144",
            entries,
            patterns,
            LocalOnly: true,
            ProductionLearningBlocked: true,
            RecorderReplayProductiveBlocked: true,
            ActionAuthorityGranted: false,
            Redacted: true);
    }

    public NodalOsProcessMemoryEvidenceReview BuildEvidenceReview()
    {
        var memory = RunDefaultFixtures();
        var summary = new NodalOsProcessMemorySummary(
            "process-memory-summary-m142-m144",
            memory.Entries.Count(e => e.Accepted),
            memory.Entries.Count(e => !e.Accepted),
            memory.Entries.Select(e => e.Scope).Distinct().ToArray(),
            memory.Entries.SelectMany(e => e.DeniedReasons).Distinct().ToArray(),
            MemoryLocalOnlyReady: memory.Entries.Any(e => e.Accepted && e.Scope == NodalOsMemoryScope.LocalFixtureOnly),
            ProductionLearningBlocked: memory.ProductionLearningBlocked,
            RecorderReplayProductiveBlocked: memory.RecorderReplayProductiveBlocked,
            SensitiveLearningBlocked: memory.Entries.Any(e => e.DeniedReasons.Contains(NodalOsMemoryDeniedReason.SensitiveScopeBlocked)),
            ActionAuthorityGranted: false,
            Redacted: true);
        var workflowSummary = new NodalOsWorkflowLearningSummary(
            "workflow-learning-summary-m142-m144",
            memory.Patterns,
            WorkflowFixtureLearningReady: memory.Patterns.Any(p => p.Confidence == NodalOsMemoryConfidence.VerifiedRedactedLocalPattern),
            LocalOnly: true,
            RecorderReplayProductiveBlocked: true,
            SensitiveLearningBlocked: summary.SensitiveLearningBlocked,
            ActionAuthorityGranted: false,
            Redacted: true);

        return new NodalOsProcessMemoryEvidenceReview(
            "process-memory-evidence-review-m142-m144",
            summary,
            workflowSummary,
            memory.Entries.SelectMany(e => e.Step.EvidenceRefs).Distinct(StringComparer.OrdinalIgnoreCase).ToArray(),
            ContainsSensitiveRawValues: false,
            ReadyForPrivatePreviewSignal: summary.MemoryLocalOnlyReady && workflowSummary.WorkflowFixtureLearningReady,
            Redacted: true);
    }

    public static IReadOnlyList<NodalOsWorkflowFixture> CreateDefaultFixtures()
    {
        var actions = new NodalOsSafeActionFixtureHarness().RunDefaultFixtures().ToDictionary(r => r.Action.ActionId, StringComparer.Ordinal);
        return
        [
            Workflow("readiness-review-workflow", NodalOsMemoryScope.LocalFixtureOnly, [actions["open-local-readiness-panel"]], "Reuse readiness review checklist locally."),
            Workflow("diagnostics-review-workflow", NodalOsMemoryScope.LocalFixtureOnly, [actions["open-local-diagnostics-panel"]], "Reuse diagnostics review checklist locally."),
            Workflow("evidence-review-workflow", NodalOsMemoryScope.PrivatePreviewLocal, [actions["review-redacted-evidence"], actions["copy-redacted-log-summary"]], "Reuse redacted evidence review only."),
            Workflow("issue-triage-workflow", NodalOsMemoryScope.PrivatePreviewLocal, [actions["observe-only-local"]], "Reuse local issue triage steps."),
            Workflow("operator-blocker-explanation-workflow", NodalOsMemoryScope.LocalFixtureOnly, [actions["overlay-blocked-action"]], "Keep blocker explanation pattern for local review."),
            Workflow("safe-draft-only-workflow", NodalOsMemoryScope.LocalFixtureOnly, [actions["local-draft-only-note"]], "Reuse draft-only local note pattern."),
            Workflow("blocked-credential-workflow", NodalOsMemoryScope.BlockedCredentials, [actions["blocked-credential-entry"]], "Do not learn credential entry.", credential: true),
            Workflow("blocked-submit-workflow", NodalOsMemoryScope.LocalFixtureOnly, [actions["blocked-submit"]], "Do not learn submit payload.", submit: true),
            Workflow("blocked-payment-workflow", NodalOsMemoryScope.LocalFixtureOnly, [actions["blocked-payment"]], "Do not learn payment flow.", payment: true),
            Workflow("blocked-delete-sign-workflow", NodalOsMemoryScope.LocalFixtureOnly, [actions["blocked-delete"], actions["blocked-sign"]], "Do not learn delete/sign flow.", submit: true),
            Workflow("blocked-sensitive-workflow", NodalOsMemoryScope.BlockedSensitive, [actions["blocked-sensitive-surface"]], "Do not learn sensitive surface."),
            Workflow("ambiguous-perception-workflow", NodalOsMemoryScope.LocalFixtureOnly, [actions["ambiguous-identity-action"]], "Require human review.", ambiguous: true),
            Workflow("blocked-production-learning", NodalOsMemoryScope.BlockedProduction, [actions["observe-only-local"]], "Production learning is blocked."),
            Workflow("blocked-recorder-replay", NodalOsMemoryScope.LocalFixtureOnly, [actions["observe-only-local"]], "Productive recorder/replay is blocked.", recorderReplay: true)
        ];
    }

    private static NodalOsWorkflowFixture Workflow(
        string id,
        NodalOsMemoryScope scope,
        IReadOnlyList<NodalOsSafeActionRunRecord> records,
        string nextStep,
        bool credential = false,
        bool cookie = false,
        bool token = false,
        bool payment = false,
        bool personal = false,
        bool rawDom = false,
        bool rawUia = false,
        bool unredactedLogs = false,
        bool submit = false,
        bool screenshotSecret = false,
        bool recorderReplay = false,
        bool ambiguous = false) =>
        new(
            id,
            scope,
            records,
            credential,
            cookie,
            token,
            payment,
            personal,
            rawDom,
            rawUia,
            unredactedLogs,
            submit,
            screenshotSecret,
            recorderReplay,
            ambiguous,
            nextStep);
}
