namespace OneBrain.Core.Recipes;

public sealed record ReliableRecipeLabAuditSurfaceViewModel(
    string SurfaceId,
    ReliableRecipeLabAuditSurfaceHeader Header,
    ReliableRecipeLabAuditSurfaceStatusStrip StatusStrip,
    IReadOnlyList<ReliableRecipeLabAuditSurfaceSection> Sections,
    IReadOnlyList<ReliableRecipeLabAuditMilestone> Timeline,
    ReliableRecipeLabAuditSurfaceExternalAuditHandoff ExternalAuditHandoff,
    ReliableRecipeLabAuditSurfaceDesignSystem DesignSystem,
    IReadOnlyList<string> ReadOnlyActionLabels,
    string NoRuntimeNotice)
{
    public bool ReadOnly => true;
    public bool FixtureOnly => true;
    public bool RuntimeEnabled => false;
    public bool AdapterRuntimeEnabled => false;
    public bool RuntimeActionExposed => false;
    public bool CallsProviderOrNetwork => false;
    public bool UsesLiveBrowser => false;
    public bool UsesLiveDesktop => false;
    public bool ActivatesOcrRuntime => false;
    public bool CapturesScreenshot => false;
    public bool StartsRecorder => false;
    public bool StartsSandbox => false;
}

public sealed record ReliableRecipeLabAuditSurfaceHeader(
    string Title,
    string Subtitle,
    IReadOnlyList<ReliableRecipeLabAuditBadge> Badges,
    string OperatorSummary);

public sealed record ReliableRecipeLabAuditBadge(
    string Label,
    ReliableRecipeLabAuditTone Tone,
    string Description);

public sealed record ReliableRecipeLabAuditSurfaceStatusStrip(
    int OverallUpgradePercent,
    int ProductSurfaceReadinessPercent,
    int AuditReadinessPercent,
    int DryRunAdapterReadinessDesignPercent,
    int RuntimeAutonomyPercent,
    string RuntimeAutonomyLabel,
    string CloseoutDecisionLabel);

public sealed record ReliableRecipeLabAuditSurfaceSection(
    ReliableRecipeLabAuditSectionKind Kind,
    string Title,
    string Eyebrow,
    ReliableRecipeLabAuditTone Tone,
    string Summary,
    IReadOnlyList<ReliableRecipeLabAuditMetric> Metrics,
    IReadOnlyList<string> KeyRows,
    IReadOnlyList<string> RequiredCopy,
    string FooterNote);

public enum ReliableRecipeLabAuditSectionKind
{
    QualityPreflight,
    EvidenceValidation,
    RecorderDraft,
    EvalHarness,
    SandboxReadiness,
    Perception,
    AdapterReadiness,
    StructuredPrerequisites,
    StructuredPrerequisiteAuthoring,
    OperatorReviewPack,
    CloseoutAudit,
    TimelinePreview
}

public enum ReliableRecipeLabAuditTone
{
    Success,
    Info,
    Warning,
    Danger,
    Neutral,
    Audit
}

public sealed record ReliableRecipeLabAuditMetric(
    string Label,
    string Value,
    ReliableRecipeLabAuditTone Tone);

public sealed record ReliableRecipeLabAuditMilestone(
    string BlockId,
    string Decision,
    string Commit,
    string Label,
    string Summary,
    ReliableRecipeLabAuditTone Tone,
    bool ReadOnly,
    bool RuntimeEnabled);

public sealed record ReliableRecipeLabAuditSurfaceExternalAuditHandoff(
    string Title,
    string Summary,
    IReadOnlyList<string> AuditQuestions,
    IReadOnlyList<string> EvidenceReferences,
    string RuntimeProhibitedStatement,
    IReadOnlyList<string> DecisionLabels);

public sealed record ReliableRecipeLabAuditSurfaceDesignSystem(
    string Theme,
    string Layout,
    string Density,
    string Background,
    string Typography,
    IReadOnlyList<string> VisualRules);

public static class ReliableRecipeLabAuditSurfacePresenter
{
    private static readonly string[] ReadOnlyActions =
    [
        "Review",
        "Inspect",
        "Open details",
        "Copy summary",
        "Export review pack",
        "View audit handoff"
    ];

    public static ReliableRecipeLabAuditSurfaceViewModel CreateDefault()
    {
        var fixture = ReliableRecipeLabFixtureCatalog.Get("safe_invoice_download_quality_pass");
        var lab = fixture.ViewModel;
        var closeout = ReliableRecipeNoRuntimeCloseoutReportGenerator.Generate();

        return Create(lab, closeout);
    }

    public static ReliableRecipeLabAuditSurfaceViewModel Create(
        ReliableRecipeLabViewModel lab,
        ReliableRecipeNoRuntimeCloseoutReport closeout)
    {
        return new ReliableRecipeLabAuditSurfaceViewModel(
            "reliable-recipe-lab.audit-surface.m13",
            Header(lab, closeout),
            StatusStrip(closeout),
            Sections(lab, closeout),
            Timeline(closeout),
            Handoff(closeout),
            DesignSystem(),
            ReadOnlyActions,
            "Read-only Recipe Lab. Runtime not enabled; external audit required before any runtime or adapter work.");
    }

    private static ReliableRecipeLabAuditSurfaceHeader Header(
        ReliableRecipeLabViewModel lab,
        ReliableRecipeNoRuntimeCloseoutReport closeout) =>
        new(
            "Recipe Lab",
            "Read-only audit surface for reliable recipe readiness",
            [
                new("Read-only", ReliableRecipeLabAuditTone.Info, "Operator can inspect readiness and audit evidence only."),
                new("Runtime not enabled", ReliableRecipeLabAuditTone.Danger, "No live browser, desktop, recorder, OCR or sandbox is enabled."),
                new("External audit required", ReliableRecipeLabAuditTone.Audit, "External audit is required before any runtime or adapter work."),
                new("Fixture-only", ReliableRecipeLabAuditTone.Neutral, "Scores and panels are generated from deterministic fixtures.")
            ],
            $"{lab.RecipeName}: {closeout.OverallDecision}. Operator can review only.");

    private static ReliableRecipeLabAuditSurfaceStatusStrip StatusStrip(ReliableRecipeNoRuntimeCloseoutReport closeout) =>
        new(
            OverallUpgradePercent: 99,
            ProductSurfaceReadinessPercent: 100,
            AuditReadinessPercent: 98,
            DryRunAdapterReadinessDesignPercent: 92,
            RuntimeAutonomyPercent: 0,
            RuntimeAutonomyLabel: "0% intentionally; runtime not enabled",
            CloseoutDecisionLabel: closeout.OverallDecision.ToString());

    private static IReadOnlyList<ReliableRecipeLabAuditSurfaceSection> Sections(
        ReliableRecipeLabViewModel lab,
        ReliableRecipeNoRuntimeCloseoutReport closeout) =>
    [
        Section(ReliableRecipeLabAuditSectionKind.QualityPreflight, "Quality / preflight", "Readiness", Tone(lab.StatusTone), lab.Summary,
            [Metric("Overall score", lab.OverallScore.ToString("0.00"), Tone(lab.StatusTone)), Metric("Mode allowed", lab.ModeAllowedLabel, ReliableRecipeLabAuditTone.Info)],
            lab.BlockingFindings.Take(3).Select(f => f.Message).DefaultIfEmpty("No blocking findings for fixture review.").ToArray(),
            ["Fixture-ready does not mean runtime-ready."],
            "Quality is a planning signal, not action authority."),
        Section(ReliableRecipeLabAuditSectionKind.EvidenceValidation, "Evidence / validation", "Proof requirements", EvidenceValidationTone(lab),
            "Structured evidence and validation gaps are visible before any future adapter planning.",
            [Metric("Evidence", lab.StructuredPrerequisitesPanel.EvidenceCompletenessScore.ToString("0.00"), ReliableRecipeLabAuditTone.Info), Metric("Validation", lab.StructuredPrerequisitesPanel.ValidationCompletenessScore.ToString("0.00"), ReliableRecipeLabAuditTone.Info)],
            lab.StructuredPrerequisitesPanel.MissingCriticalRequirements.Concat(lab.StructuredPrerequisitesPanel.Warnings).Take(5).DefaultIfEmpty("Structured proof requirements are visible.").ToArray(),
            ["Structured evidence required.", "Structured validation required."],
            "No success is implied without validation."),
        Section(ReliableRecipeLabAuditSectionKind.RecorderDraft, "Recorder draft", "Demonstration review", ReliableRecipeLabAuditTone.Warning,
            lab.RecorderDraftReview.DraftConversionNotice,
            [Metric("Review state", lab.RecorderDraftReview.DraftReviewStateLabel, ReliableRecipeLabAuditTone.Warning), Metric("Sensitive input", lab.RecorderDraftReview.SensitiveInputSummary, ReliableRecipeLabAuditTone.Info)],
            lab.RecorderDraftReview.ReviewChecklist.Select(i => i.Title).DefaultIfEmpty("Recorder output remains draft/review only.").ToArray(),
            ["Sensitive input was redacted.", "Recorder draft cannot approve runtime."],
            "No real recorder, capture or playback is present."),
        Section(ReliableRecipeLabAuditSectionKind.EvalHarness, "Eval harness", "Fixture scenarios", ReliableRecipeLabAuditTone.Info,
            "Fixture evaluation summarizes expected outcomes, blocked scenarios and regression signals.",
            [Metric("Scenario", lab.EvalPanel.ScenarioId, ReliableRecipeLabAuditTone.Info), Metric("Final decision", lab.EvalPanel.FinalDecision, ReliableRecipeLabAuditTone.Info)],
            lab.EvalPanel.TopFailureKinds.DefaultIfEmpty("Fixture-only evaluation. Runtime not enabled.").ToArray(),
            ["Expected block can be a pass.", "Eval confidence is not a runtime guarantee."],
            lab.EvalPanel.EvalNotice.Message),
        Section(ReliableRecipeLabAuditSectionKind.SandboxReadiness, "Sandbox readiness", "Design-only isolation", ReliableRecipeLabAuditTone.Warning,
            "Sandbox readiness explains fixture-ready/design-only state and blocked future capabilities.",
            [Metric("Decision", lab.SandboxReadinessReportPanel.DecisionLabel, ReliableRecipeLabAuditTone.Warning), Metric("Isolation", lab.SandboxReadinessReportPanel.RequiredIsolationModeLabel, ReliableRecipeLabAuditTone.Neutral)],
            lab.SandboxReadinessReportPanel.BlockedCapabilities.Concat(lab.SandboxReadinessReportPanel.FutureUnlockConditions).Take(5).ToArray(),
            ["No live browser, desktop, recorder, OCR or sandbox is enabled."],
            lab.SandboxReadinessReportPanel.FixtureOnlyNotice),
        Section(ReliableRecipeLabAuditSectionKind.Perception, "Perception", "Signal confidence", ReliableRecipeLabAuditTone.Warning,
            "Perception report shows agreement, contradictions and action authority boundaries.",
            [Metric("Decision", lab.PerceptionIntegrationPanel.DecisionLabel, ReliableRecipeLabAuditTone.Warning), Metric("Confidence", lab.PerceptionIntegrationPanel.OverallConfidence.ToString("0.00"), ReliableRecipeLabAuditTone.Info)],
            lab.PerceptionIntegrationPanel.HumanReviewReasons.Concat(lab.PerceptionIntegrationPanel.MissingSignals).Take(5).DefaultIfEmpty("OCR is a supporting signal, not action authority.").ToArray(),
            ["OCR is a supporting signal, not action authority.", "Human review required when perception is weak."],
            lab.PerceptionIntegrationPanel.FixtureOnlyNotice),
        Section(ReliableRecipeLabAuditSectionKind.AdapterReadiness, "Adapter readiness", "Protected design", ReliableRecipeLabAuditTone.Danger,
            "Adapter readiness is design-only and remains blocked by protected-scope and external-audit gates.",
            [Metric("Decision", lab.DryRunAdapterReadinessPanel.DecisionLabel, ReliableRecipeLabAuditTone.Danger), Metric("Score", lab.DryRunAdapterReadinessPanel.ReadinessScore.ToString("0.00"), ReliableRecipeLabAuditTone.Info)],
            lab.DryRunAdapterReadinessPanel.BlockedCapabilities.Concat(lab.DryRunAdapterReadinessPanel.MissingGates).Take(6).ToArray(),
            ["External audit is required before any runtime or adapter work."],
            lab.DryRunAdapterReadinessPanel.NoRuntimeNotice),
        Section(ReliableRecipeLabAuditSectionKind.StructuredPrerequisites, "Structured prerequisites", "Explicit proof", ReliableRecipeLabAuditTone.Info,
            "Explicit, mapped, inferred and missing requirements are visible.",
            [Metric("Explicit ratio", lab.StructuredPrerequisitesPanel.ExplicitRequirementRatio.ToString("0.00"), ReliableRecipeLabAuditTone.Info), Metric("Inferred ratio", lab.StructuredPrerequisitesPanel.InferredRequirementRatio.ToString("0.00"), ReliableRecipeLabAuditTone.Warning)],
            lab.StructuredPrerequisitesPanel.BlockingFindings.Concat(lab.StructuredPrerequisitesPanel.Warnings).Take(5).DefaultIfEmpty("Structured prerequisites available.").ToArray(),
            ["Inferred requirements need review.", "Missing critical requirements block adapter readiness."],
            lab.StructuredPrerequisitesPanel.NoRuntimeNotice),
        Section(ReliableRecipeLabAuditSectionKind.StructuredPrerequisiteAuthoring, "Prerequisite authoring", "Review proposals", ReliableRecipeLabAuditTone.Warning,
            "Authoring panel shows proposals, review state and migration summary without applying live changes.",
            [Metric("Proposals", lab.StructuredPrerequisiteAuthoringPanel.ProposalCount.ToString(), ReliableRecipeLabAuditTone.Warning), Metric("Still blocking", lab.StructuredPrerequisiteAuthoringPanel.StillBlockingCount.ToString(), ReliableRecipeLabAuditTone.Danger)],
            lab.StructuredPrerequisiteAuthoringPanel.TopProposals.Concat(lab.StructuredPrerequisiteAuthoringPanel.ReviewChecklist).Take(5).DefaultIfEmpty("No authoring proposals in this fixture.").ToArray(),
            ["Operator can review only."],
            lab.StructuredPrerequisiteAuthoringPanel.NoRuntimeNotice),
        Section(ReliableRecipeLabAuditSectionKind.OperatorReviewPack, "Operator review pack", "Human review", ReliableRecipeLabAuditTone.Audit,
            lab.OperatorReviewPackPanel.ExecutiveSummary,
            [Metric("Pending decisions", lab.OperatorReviewPackPanel.PendingDecisionCount.ToString(), ReliableRecipeLabAuditTone.Warning), Metric("Critical blockers", lab.OperatorReviewPackPanel.BlockedCriticalCount.ToString(), ReliableRecipeLabAuditTone.Danger)],
            lab.OperatorReviewPackPanel.TopReviewRows.Take(5).DefaultIfEmpty("Operator review pack is available.").ToArray(),
            ["Operator signoff cannot approve runtime."],
            lab.OperatorReviewPackPanel.NoRuntimeNotice),
        Section(ReliableRecipeLabAuditSectionKind.CloseoutAudit, "Closeout / audit", "M1-M12 foundation", ReliableRecipeLabAuditTone.Audit,
            lab.CloseoutPanel.ReadinessSummary,
            [Metric("Decision", lab.CloseoutPanel.DecisionLabel, ReliableRecipeLabAuditTone.Audit), Metric("Next phase", lab.CloseoutPanel.RecommendedNextPhase, ReliableRecipeLabAuditTone.Info)],
            [lab.CloseoutPanel.InvariantSummary, lab.CloseoutPanel.ProtectedScopeSummary, lab.CloseoutPanel.NoRuntimeSummary, lab.CloseoutPanel.ExternalAuditSummary],
            ["External audit required before runtime.", "Protected scope untouched."],
            lab.CloseoutPanel.NoRuntimeNotice),
        Section(ReliableRecipeLabAuditSectionKind.TimelinePreview, "Milestone timeline", "M1-M12", ReliableRecipeLabAuditTone.Neutral,
            "Vertical milestone preview links the closed foundation blocks without exposing runtime state.",
            [Metric("Milestones", closeout.BlockSummaries.Count.ToString(), ReliableRecipeLabAuditTone.Info), Metric("Runtime autonomy", "0%", ReliableRecipeLabAuditTone.Danger)],
            closeout.BlockSummaries.Select(b => $"{b.BlockId}: {b.Purpose}").ToArray(),
            ["Read-only timeline preview."],
            "Milestones are audit references, not action history.")
    ];

    private static ReliableRecipeLabAuditSurfaceSection Section(
        ReliableRecipeLabAuditSectionKind kind,
        string title,
        string eyebrow,
        ReliableRecipeLabAuditTone tone,
        string summary,
        IReadOnlyList<ReliableRecipeLabAuditMetric> metrics,
        IReadOnlyList<string> keyRows,
        IReadOnlyList<string> requiredCopy,
        string footer) =>
        new(kind, title, eyebrow, tone, summary, metrics, keyRows, requiredCopy, footer);

    private static ReliableRecipeLabAuditMetric Metric(string label, string value, ReliableRecipeLabAuditTone tone) =>
        new(label, value, tone);

    private static ReliableRecipeLabAuditTone Tone(string statusTone) =>
        statusTone switch
        {
            "success" => ReliableRecipeLabAuditTone.Success,
            "warning" => ReliableRecipeLabAuditTone.Warning,
            "danger" => ReliableRecipeLabAuditTone.Danger,
            _ => ReliableRecipeLabAuditTone.Info
        };

    private static ReliableRecipeLabAuditTone EvidenceValidationTone(ReliableRecipeLabViewModel lab) =>
        lab.StructuredPrerequisitesPanel.MissingCriticalRequirements.Count > 0
            ? ReliableRecipeLabAuditTone.Danger
            : ReliableRecipeLabAuditTone.Info;

    private static IReadOnlyList<ReliableRecipeLabAuditMilestone> Timeline(ReliableRecipeNoRuntimeCloseoutReport closeout) =>
        closeout.BlockSummaries.Select(b => new ReliableRecipeLabAuditMilestone(
            b.BlockId,
            b.Decision,
            b.Commit,
            b.FocusedTestCategory,
            b.Purpose,
            ReliableRecipeLabAuditTone.Neutral,
            ReadOnly: true,
            RuntimeEnabled: false)).ToArray();

    private static ReliableRecipeLabAuditSurfaceExternalAuditHandoff Handoff(ReliableRecipeNoRuntimeCloseoutReport closeout) =>
        new(
            closeout.ExternalAuditHandoff.Title,
            closeout.ExternalAuditHandoff.Scope,
            closeout.ExternalAuditHandoff.AuditQuestions,
            closeout.ExternalAuditHandoff.EvidenceReferences,
            closeout.ExternalAuditHandoff.RuntimeProhibitedStatement,
            closeout.ExternalAuditHandoff.RecommendedAuditDecisionLabels);

    private static ReliableRecipeLabAuditSurfaceDesignSystem DesignSystem() =>
        new(
            "dark-first mission-control",
            "asymmetric control-room panels with vertical milestone rail",
            "dense but scannable",
            "calm technical graphite with restrained status accents",
            "compact command typography with numeric score badges",
            [
                "Cards stay shallow and individual, not nested.",
                "Warnings and audit sections stay visible above fold.",
                "Success tones mean fixture/read-only readiness only.",
                "No action button implies runtime or live automation."
            ]);
}
