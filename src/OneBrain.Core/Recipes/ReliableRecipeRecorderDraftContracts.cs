namespace OneBrain.Core.Recipes;

public sealed record RecorderToRecipeDraft(
    string DraftId,
    string SourceTrajectoryId,
    ReliableRecipeDefinition Recipe,
    RecorderDraftReviewState ReviewState,
    IReadOnlyList<RecorderDraftReviewChecklistItem> ReviewChecklist,
    IReadOnlyList<RecorderDraftDetectedVariable> DetectedVariables,
    RecorderSensitiveInputSummary SensitiveInputSummary,
    IReadOnlyList<ReliableValidationCheckKind> MissingValidationRequirements,
    IReadOnlyList<EvidenceRequirementKind> MissingEvidenceRequirements,
    IReadOnlyList<ReliableHumanInterventionRequest> HumanInterventionRequirements,
    ReliableRecipeQualityReport QualityReport,
    ReliableRecipePreflightReport PreflightReport,
    ReliableRecipeLabViewModel LabViewModel,
    RecorderDraftConversionDecision ConversionDecision)
{
    public bool FixtureOnly => true;
    public bool ReadOnly => true;
    public bool LiveRuntimeEnabled => false;
    public bool RunReady => false;
    public bool CanStartRecorder => false;
    public bool CanCaptureMouseOrKeyboard => false;
    public bool CanCaptureScreenOrScreenshot => false;
    public bool CanUseBrowserOrDesktopHooks => false;
    public bool CanCallProviderOrNetwork => false;
    public bool CanBypassChallenge => false;
}

public enum RecorderDraftReviewState
{
    DraftOnly,
    NeedsReview,
    NeedsValidationDesign,
    NeedsEvidenceDesign,
    BlockedSensitiveInput,
    BlockedChallenge,
    BlockedAmbiguousTarget,
    DryRunCandidate,
    Rejected
}

public sealed record RecorderDraftReviewChecklistItem(
    string Code,
    string Title,
    string Description,
    ReliableRecipeQualitySeverity Severity,
    bool IsBlocking,
    string RecommendedFix,
    string? RelatedStepId);

public sealed record RecorderDraftDetectedVariable(
    string Name,
    string Source,
    bool IsSensitive,
    string ReplacementToken,
    bool NeedsUserConfirmation);

public sealed record RecorderSensitiveInputSummary(
    bool HasSensitiveInput,
    IReadOnlyList<RecordedInputSensitivityKind> SensitiveKinds,
    bool RedactionApplied,
    bool BlockedRawValues,
    bool UserActionRequired);

public enum RecordedInputSensitivityKind
{
    Password,
    Token,
    ApiKey,
    Cookie,
    AuthorizationHeader,
    Email,
    Phone,
    Payment,
    GovernmentId,
    PersonalData,
    UnknownSensitive
}

public enum RecorderDraftConversionDecision
{
    CreatedDraftOnly,
    CreatedNeedsReview,
    CreatedBlocked,
    RejectedUnsafeTrajectory
}

public sealed record RecorderToRecipeDraftConversionOptions(
    bool IncludeDownloadValidation = false,
    bool IncludeFullValidation = false,
    bool IncludeFullEvidence = false);

public sealed record ReliableRecorderFixture(
    string FixtureId,
    ReliableRecorderTrajectory Trajectory,
    RecorderToRecipeDraft Draft);

public static class ReliableRecipeRecorderFixtureCatalog
{
    public static IReadOnlyList<ReliableRecorderFixture> All() =>
    [
        Create("invoice_download_demonstration", InvoiceDownloadDemonstration()),
        Create("login_password_redacted_demonstration", LoginPasswordRedactedDemonstration()),
        Create("captcha_two_factor_challenge_demonstration", CaptchaTwoFactorChallengeDemonstration()),
        Create("ambiguous_continue_button_demonstration", AmbiguousContinueButtonDemonstration()),
        Create("ocr_only_canvas_button_demonstration", OcrOnlyCanvasButtonDemonstration()),
        Create("government_form_submit_demonstration", GovernmentFormSubmitDemonstration()),
        Create("desktop_future_demonstration", DesktopFutureDemonstration()),
        Create("corrected_user_click_demonstration", CorrectedUserClickDemonstration())
    ];

    public static ReliableRecorderFixture Get(string fixtureId) =>
        All().Single(f => f.FixtureId == fixtureId);

    public static RecorderToRecipeDraft InvoiceDownloadWithValidation() =>
        RecorderToRecipeDraftConverter.Convert(
            InvoiceDownloadDemonstration(),
            new RecorderToRecipeDraftConversionOptions(IncludeDownloadValidation: true, IncludeFullValidation: true, IncludeFullEvidence: true));

    private static ReliableRecorderFixture Create(string fixtureId, ReliableRecorderTrajectory trajectory) =>
        new(fixtureId, trajectory, RecorderToRecipeDraftConverter.Convert(trajectory));

    private static ReliableRecorderTrajectory InvoiceDownloadDemonstration() =>
        new(
            "trajectory.invoice-download",
            "workspace.fixture",
            [
                Interaction("invoice.nav", ReliableRecordedInputEventKind.Navigate, Target("Invoices", ReliableActionResolutionMode.StableSelector, 0.94), null),
                Interaction("invoice.search", ReliableRecordedInputEventKind.Type, Target("Invoice search", ReliableActionResolutionMode.StableSelector, 0.92), "{INVOICE_ID}"),
                Interaction("invoice.download", ReliableRecordedInputEventKind.DownloadObserved, Target("Download invoice", ReliableActionResolutionMode.StableSelector, 0.95), null)
            ],
            "No sensitive input in fixture demonstration.",
            ["INVOICE_ID"],
            ReliableRecipeRiskProfile.ReadOnly | ReliableRecipeRiskProfile.LocalWrite);

    private static ReliableRecorderTrajectory LoginPasswordRedactedDemonstration() =>
        new(
            "trajectory.login-password-redacted",
            "workspace.fixture",
            [
                Interaction("login.user", ReliableRecordedInputEventKind.Type, Target("Email", ReliableActionResolutionMode.StableSelector, 0.92), "{EMAIL}"),
                Interaction("login.password", ReliableRecordedInputEventKind.Type, Target("Password", ReliableActionResolutionMode.StableSelector, 0.91), "{PASSWORD}", sensitive: true)
            ],
            "Credential input detected; sensitive value redacted by fixture.",
            ["EMAIL", "PASSWORD"],
            ReliableRecipeRiskProfile.Credentialed | ReliableRecipeRiskProfile.SensitiveData);

    private static ReliableRecorderTrajectory CaptchaTwoFactorChallengeDemonstration() =>
        new(
            "trajectory.captcha-two-factor-challenge",
            "workspace.fixture",
            [
                Interaction("auth.challenge", ReliableRecordedInputEventKind.Wait, Target("Two-factor challenge", ReliableActionResolutionMode.VisibleTextExact, 0.88), null)
            ],
            "CAPTCHA/2FA challenge detected; handoff required.",
            [],
            ReliableRecipeRiskProfile.Credentialed);

    private static ReliableRecorderTrajectory AmbiguousContinueButtonDemonstration() =>
        new(
            "trajectory.ambiguous-continue-button",
            "workspace.fixture",
            [
                Interaction("ambiguous.continue", ReliableRecordedInputEventKind.Click, Target("Continue", ReliableActionResolutionMode.VisibleTextApproximate, 0.46, "Multiple matching Continue buttons"), null)
            ],
            "Ambiguous target fixture with multiple Continue buttons.",
            [],
            ReliableRecipeRiskProfile.ReadOnly);

    private static ReliableRecorderTrajectory OcrOnlyCanvasButtonDemonstration() =>
        new(
            "trajectory.ocr-only-canvas-button",
            "workspace.fixture",
            [
                Interaction("canvas.submit", ReliableRecordedInputEventKind.Click, Target("Submit", ReliableActionResolutionMode.OcrRegion, 0.76), null)
            ],
            "OCR-only canvas target for sensitive action; supporting signal only.",
            [],
            ReliableRecipeRiskProfile.ExternalSideEffect | ReliableRecipeRiskProfile.SensitiveData);

    private static ReliableRecorderTrajectory GovernmentFormSubmitDemonstration() =>
        new(
            "trajectory.government-form-submit",
            "workspace.fixture",
            [
                Interaction("gov.fill", ReliableRecordedInputEventKind.Type, Target("Government form field", ReliableActionResolutionMode.StableSelector, 0.9), "{FORM_VALUE}"),
                Interaction("gov.submit", ReliableRecordedInputEventKind.Click, Target("Submit official form", ReliableActionResolutionMode.StableSelector, 0.9), null)
            ],
            "Government form submit fixture; external side effect and legal review required.",
            ["FORM_VALUE"],
            ReliableRecipeRiskProfile.ExternalSideEffect | ReliableRecipeRiskProfile.SensitiveData | ReliableRecipeRiskProfile.Irreversible);

    private static ReliableRecorderTrajectory DesktopFutureDemonstration() =>
        new(
            "trajectory.desktop-future",
            "workspace.fixture",
            [
                Interaction("desktop.export", ReliableRecordedInputEventKind.Click, Target("Desktop export", ReliableActionResolutionMode.VisualBoundingBox, 0.62), null)
            ],
            "Desktop surface requested; sandbox is design-only.",
            [],
            ReliableRecipeRiskProfile.ExternalSideEffect);

    private static ReliableRecorderTrajectory CorrectedUserClickDemonstration() =>
        new(
            "trajectory.corrected-user-click",
            "workspace.fixture",
            [
                Interaction("repair.initial", ReliableRecordedInputEventKind.Click, Target("Wrong row", ReliableActionResolutionMode.VisibleTextApproximate, 0.55), null),
                Interaction("repair.corrected", ReliableRecordedInputEventKind.HumanCorrection, Target("Correct row", ReliableActionResolutionMode.StableSelector, 0.86), null, correction: true)
            ],
            "User correction marker captured as fixture review signal.",
            [],
            ReliableRecipeRiskProfile.ReadOnly);

    private static ReliableRecordedInteraction Interaction(
        string observationRef,
        ReliableRecordedInputEventKind eventKind,
        ReliableTargetDescriptor target,
        string? redactedInput,
        bool sensitive = false,
        bool correction = false) =>
        new(DateTimeOffset.UnixEpoch, observationRef, eventKind, target, redactedInput, "tab.fixture", correction, sensitive);

    private static ReliableTargetDescriptor Target(string text, ReliableActionResolutionMode mode, double confidence, string? ambiguity = null) =>
        new(
            text,
            mode == ReliableActionResolutionMode.OcrRegion ? "canvas-region" : "button",
            mode == ReliableActionResolutionMode.StableSelector ? $"#{text.ToLowerInvariant().Replace(' ', '-')}" : null,
            new ReliableBoundingBox(10, 10, 100, 32),
            null,
            "element.fixture",
            "tab.fixture",
            mode,
            new ReliableTargetResolutionConfidence(confidence, [mode], ambiguity, ReliableRecipePolicyDecision.AllowDryRunOnly));
}

public static class RecorderToRecipeDraftConverter
{
    public static RecorderToRecipeDraft Convert(ReliableRecorderTrajectory trajectory, RecorderToRecipeDraftConversionOptions? options = null)
    {
        options ??= new RecorderToRecipeDraftConversionOptions();

        var recipe = BuildRecipe(trajectory, options);
        var sensitive = SensitiveSummary(trajectory);
        var humanInterventions = HumanInterventions(trajectory, sensitive).ToArray();
        var context = QualityContext(trajectory, humanInterventions);
        var preflight = ReliableRecipePreflightComposer.Compose(recipe, ReliableRecipeRunMode.DryRun, context: context);
        var checklist = Checklist(trajectory, preflight, sensitive).ToArray();
        var state = DetermineReviewState(trajectory, preflight, checklist, sensitive);
        var decision = state switch
        {
            RecorderDraftReviewState.DryRunCandidate => RecorderDraftConversionDecision.CreatedDraftOnly,
            RecorderDraftReviewState.BlockedSensitiveInput or RecorderDraftReviewState.BlockedChallenge or RecorderDraftReviewState.BlockedAmbiguousTarget or RecorderDraftReviewState.Rejected => RecorderDraftConversionDecision.CreatedBlocked,
            _ => RecorderDraftConversionDecision.CreatedNeedsReview
        };
        var variables = DetectedVariables(trajectory, sensitive).ToArray();
        var lab = ReliableRecipeLabViewModelMapper.Map(recipe, preflight) with
        {
            RecorderDraftReview = RecorderDraftReviewPanel(trajectory, state, checklist, variables, sensitive)
        };

        return new RecorderToRecipeDraft(
            $"draft.{trajectory.Id}",
            trajectory.Id,
            recipe,
            state,
            checklist,
            variables,
            sensitive,
            preflight.QualityReport.ValidationCompleteness.MissingValidations,
            preflight.QualityReport.EvidenceCompleteness.MissingEvidenceKinds,
            humanInterventions,
            preflight.QualityReport,
            preflight,
            lab,
            decision);
    }

    private static ReliableRecipeDefinition BuildRecipe(ReliableRecorderTrajectory trajectory, RecorderToRecipeDraftConversionOptions options)
    {
        var blocks = trajectory.Interactions.Select((interaction, index) => BlockFrom(trajectory, interaction, index, options)).ToArray();
        return new ReliableRecipeDefinition(
            $"recipe.{trajectory.Id}",
            trajectory.Id.Replace("trajectory.", string.Empty).Replace('-', ' '),
            "1.0.0",
            trajectory.WorkspaceScope,
            trajectory.DetectedVariables.Select(v => new ReliableRecipeVariable(v, $"var.{v.ToLowerInvariant()}", SecretByReference: IsSensitiveVariable(v))).ToArray(),
            blocks,
            new ReliableRecipeRunLimits(
                MaxSteps: Math.Max(6, blocks.Length + 3),
                MaxRetries: 1,
                MaxLoopIterations: 1,
                MaxRuntimeSeconds: 60,
                AllowedDomains: ["fixture.local"],
                AllowedActions: blocks.Select(b => b.Kind).Distinct().ToArray(),
                CompleteCriteria: new ReliableCompleteCriteria([new ReliableValidationCheck("complete", ReliableValidationCheckKind.TimelineEventCreated, "timeline.complete", Passed: true)]),
                TerminateCriteria: new ReliableTerminateCriteria([new ReliableValidationCheck("terminate", ReliableValidationCheckKind.ManualConfirmationRequired, "human.stop", Passed: true)])),
            trajectory.RiskProfile,
            ReliableRecipeReadiness.DesignOnly,
            ReliableRecipeCreatedFrom.RecorderDraft);
    }

    private static ReliableRecipeBlock BlockFrom(ReliableRecorderTrajectory trajectory, ReliableRecordedInteraction interaction, int index, RecorderToRecipeDraftConversionOptions options)
    {
        var kind = KindFor(trajectory, interaction);
        var risk = RiskFor(trajectory, interaction);
        var evidence = EvidenceFor(interaction, options).ToArray();
        var validation = ValidationFor(trajectory, interaction, options).ToArray();
        return new ReliableRecipeBlock(
            $"step.{index + 1}",
            kind,
            LabelFor(interaction),
            new Dictionary<string, string>
            {
                ["observationRef"] = interaction.ObservationRef,
                ["targetSummaryRef"] = $"target.{index + 1}"
            },
            interaction.TextInputRedacted is null ? [] : [interaction.TextInputRedacted],
            [],
            risk,
            evidence,
            validation);
    }

    private static ReliableRecipeBlockKind KindFor(ReliableRecorderTrajectory trajectory, ReliableRecordedInteraction interaction)
    {
        if (trajectory.Id.Contains("desktop", StringComparison.OrdinalIgnoreCase))
            return ReliableRecipeBlockKind.DesktopFuture;
        if (interaction.InputEventKind == ReliableRecordedInputEventKind.DownloadObserved)
            return ReliableRecipeBlockKind.FileDownloadEvidence;
        if (interaction.InputEventKind == ReliableRecordedInputEventKind.Wait && IsChallenge(trajectory))
            return ReliableRecipeBlockKind.HumanIntervention;
        if (interaction.InputEventKind == ReliableRecordedInputEventKind.HumanCorrection)
            return ReliableRecipeBlockKind.HumanIntervention;
        return interaction.InputEventKind == ReliableRecordedInputEventKind.Wait ? ReliableRecipeBlockKind.Wait : ReliableRecipeBlockKind.BrowserAction;
    }

    private static ReliableRecipeRiskProfile RiskFor(ReliableRecorderTrajectory trajectory, ReliableRecordedInteraction interaction)
    {
        var risk = trajectory.RiskProfile;
        if (interaction.SensitiveInputDetected)
            risk |= ReliableRecipeRiskProfile.Credentialed | ReliableRecipeRiskProfile.SensitiveData;
        if (trajectory.Id.Contains("government", StringComparison.OrdinalIgnoreCase))
            risk |= ReliableRecipeRiskProfile.ExternalSideEffect | ReliableRecipeRiskProfile.Irreversible | ReliableRecipeRiskProfile.SensitiveData;
        return risk;
    }

    private static IEnumerable<string> EvidenceFor(ReliableRecordedInteraction interaction, RecorderToRecipeDraftConversionOptions options)
    {
        yield return $"evidence.before.{interaction.ObservationRef}";
        yield return $"evidence.proposal.{interaction.ObservationRef}";
        yield return $"evidence.result.{interaction.ObservationRef}";
        yield return $"evidence.timeline.{interaction.ObservationRef}";
        yield return $"evidence.perception.{interaction.ObservationRef}";
        if (options.IncludeFullEvidence)
        {
            yield return $"evidence.after.{interaction.ObservationRef}";
            yield return $"evidence.validation.{interaction.ObservationRef}";
        }
        if (interaction.InputEventKind == ReliableRecordedInputEventKind.DownloadObserved)
            yield return $"evidence.download-artifact.{interaction.ObservationRef}";
        if (interaction.InputEventKind == ReliableRecordedInputEventKind.HumanCorrection || interaction.SensitiveInputDetected)
            yield return $"evidence.human.{interaction.ObservationRef}";
    }

    private static IEnumerable<string> ValidationFor(ReliableRecorderTrajectory trajectory, ReliableRecordedInteraction interaction, RecorderToRecipeDraftConversionOptions options)
    {
        if (options.IncludeFullValidation)
        {
            yield return "validation.visible";
            yield return "validation.timeline";
        }
        if (interaction.InputEventKind == ReliableRecordedInputEventKind.DownloadObserved && options.IncludeDownloadValidation)
            yield return "validation.download";
        if (interaction.InputEventKind == ReliableRecordedInputEventKind.HumanCorrection || IsChallenge(trajectory))
            yield return "validation.manual";
    }

    private static string LabelFor(ReliableRecordedInteraction interaction) =>
        interaction.InputEventKind switch
        {
            ReliableRecordedInputEventKind.DownloadObserved => "Download artifact fixture step",
            ReliableRecordedInputEventKind.Type => "Type redacted fixture input",
            ReliableRecordedInputEventKind.HumanCorrection => "Review user correction marker",
            ReliableRecordedInputEventKind.Wait => "Wait or handoff fixture step",
            _ => "Recorded fixture step"
        };

    private static ReliableRecipeQualityContext QualityContext(ReliableRecorderTrajectory trajectory, IReadOnlyList<ReliableHumanInterventionRequest> interventions)
    {
        var first = trajectory.Interactions.FirstOrDefault();
        var target = first?.TargetDescriptor;
        var ocrOnly = target?.Confidence.SignalsUsed.SequenceEqual([ReliableActionResolutionMode.OcrRegion]) == true;
        var perception = ocrOnly
            ? new ReliablePerceptionStackSnapshot(
                $"snapshot.{trajectory.Id}",
                ReliablePerceptionSourceSurface.FixtureBrowser,
                [],
                [],
                [new ReliablePerceptionSignal(ReliablePerceptionSignalKind.OcrText, $"ocr.{trajectory.Id}", target?.Text ?? "OCR fixture target", target?.Confidence.Score ?? 0.5, trajectory.RiskProfile.HasFlag(ReliableRecipeRiskProfile.SensitiveData))],
                [],
                [],
                true,
                new ReliablePerceptionConfidence(0, 0, [], trajectory.RiskProfile.HasFlag(ReliableRecipeRiskProfile.SensitiveData)))
            : new ReliablePerceptionStackSnapshot(
                $"snapshot.{trajectory.Id}",
                ReliablePerceptionSourceSurface.FixtureBrowser,
                [new ReliablePerceptionSignal(ReliablePerceptionSignalKind.DomElement, $"dom.{trajectory.Id}", target?.Text ?? "Fixture target", 0.9)],
                [new ReliablePerceptionSignal(ReliablePerceptionSignalKind.AccessibilityNode, $"a11y.{trajectory.Id}", target?.Text ?? "Fixture target", 0.88)],
                [],
                [],
                [],
                true,
                new ReliablePerceptionConfidence(0, 0, [], false));

        var sandbox = trajectory.Id.Contains("desktop", StringComparison.OrdinalIgnoreCase)
            ? new ComputerUseSandboxProfile(ReliableSandboxIsolationMode.VmFuture, [ReliableComputerUseSurface.Desktop], "blocked", "fixture", "secret-refs-only", "rollback", "reference-only", 60)
            : new ComputerUseSandboxProfile(ReliableSandboxIsolationMode.DryRunFixture, [ReliableComputerUseSurface.Browser], "blocked-by-default", "fixture-only", "secret-refs-only", "rollback-fixture-state", "reference-only", 60);

        return new ReliableRecipeQualityContext(
            TargetDescriptor: target,
            PerceptionSnapshot: perception,
            SandboxProfile: sandbox,
            HumanInterventionRequests: interventions);
    }

    private static IEnumerable<ReliableHumanInterventionRequest> HumanInterventions(ReliableRecorderTrajectory trajectory, RecorderSensitiveInputSummary sensitive)
    {
        if (IsChallenge(trajectory))
            yield return ReliableHumanInterventionFactory.ForChallenge(ReliableHumanInterventionReason.TwoFactorRequired, ["evidence.challenge.fixture"]);
        if (sensitive.HasSensitiveInput)
            yield return ReliableHumanInterventionFactory.ForChallenge(ReliableHumanInterventionReason.CredentialRequired, ["evidence.credential-redacted.fixture"]);
        if (trajectory.Interactions.Any(i => !string.IsNullOrWhiteSpace(i.TargetDescriptor.Confidence.AmbiguityReason)))
            yield return new ReliableHumanInterventionRequest(
                ReliableHumanInterventionReason.AmbiguousTarget,
                "A recorded target was ambiguous in the fixture.",
                "NODAL OS preserved the ambiguous target as a review item.",
                "Choose the intended target in review or keep the draft blocked.",
                ["Keep blocked", "Confirm target by reference"],
                "No automatic retry.",
                false,
                ["evidence.ambiguous-target.fixture"]);
    }

    private static IEnumerable<RecorderDraftReviewChecklistItem> Checklist(ReliableRecorderTrajectory trajectory, ReliableRecipePreflightReport preflight, RecorderSensitiveInputSummary sensitive)
    {
        if (sensitive.HasSensitiveInput)
            yield return Item("sensitive-input-redacted", "Sensitive input redacted", "Sensitive recorder input is represented by reference/token only.", ReliableRecipeQualitySeverity.Blocking, true, "Confirm secret reference and keep the raw value out of the draft.", null);
        if (IsChallenge(trajectory))
            yield return Item("challenge-handoff-required", "Human handoff required", "CAPTCHA/2FA/login challenge cannot be bypassed or solved by the draft.", ReliableRecipeQualitySeverity.Blocking, true, "Keep blocked until a human handoff path is designed.", null);
        foreach (var finding in preflight.QualityReport.BlockingFindings)
            yield return Item(finding.Code, TitleFor(finding.Code), finding.Message, finding.Severity, finding.IsBlocking, finding.RecommendedFix, finding.AffectedBlockId);
        foreach (var interaction in trajectory.Interactions.Where(i => i.UserCorrectionMarker))
            yield return Item("user-correction-marker", "User correction needs review", "A user correction marker was preserved as target repair context.", ReliableRecipeQualitySeverity.Warning, false, "Review locator/target choice before future dry-run eligibility.", interaction.ObservationRef);
        if (trajectory.Interactions.Any(i => i.TargetDescriptor.Confidence.SignalsUsed.SequenceEqual([ReliableActionResolutionMode.OcrRegion])))
            yield return Item("ocr-supporting-signal-only", "OCR supporting signal only", "OCR-only target evidence cannot authorize sensitive actions.", ReliableRecipeQualitySeverity.Blocking, trajectory.RiskProfile.HasFlag(ReliableRecipeRiskProfile.SensitiveData), "Add deterministic DOM/accessibility/known-target signals or keep blocked.", null);
    }

    private static RecorderDraftReviewState DetermineReviewState(
        ReliableRecorderTrajectory trajectory,
        ReliableRecipePreflightReport preflight,
        IReadOnlyList<RecorderDraftReviewChecklistItem> checklist,
        RecorderSensitiveInputSummary sensitive)
    {
        if (sensitive.HasSensitiveInput)
            return RecorderDraftReviewState.BlockedSensitiveInput;
        if (IsChallenge(trajectory))
            return RecorderDraftReviewState.BlockedChallenge;
        if (trajectory.Interactions.Any(i => !string.IsNullOrWhiteSpace(i.TargetDescriptor.Confidence.AmbiguityReason)))
            return RecorderDraftReviewState.BlockedAmbiguousTarget;
        var nonRecorderBlocks = checklist
            .Where(i => i.IsBlocking)
            .Where(i => !i.Code.Contains("recorder-draft", StringComparison.OrdinalIgnoreCase))
            .ToArray();
        if (nonRecorderBlocks.Length == 0 &&
            preflight.QualityReport.ValidationCompleteness.MissingValidations.Count == 0 &&
            preflight.QualityReport.EvidenceCompleteness.MissingEvidenceKinds.Count == 0)
            return RecorderDraftReviewState.DryRunCandidate;
        if (preflight.QualityReport.ValidationCompleteness.MissingValidations.Count > 0)
            return RecorderDraftReviewState.NeedsValidationDesign;
        if (preflight.QualityReport.EvidenceCompleteness.MissingEvidenceKinds.Count > 0)
            return RecorderDraftReviewState.NeedsEvidenceDesign;
        return RecorderDraftReviewState.NeedsReview;
    }

    private static IEnumerable<RecorderDraftDetectedVariable> DetectedVariables(ReliableRecorderTrajectory trajectory, RecorderSensitiveInputSummary sensitive) =>
        trajectory.DetectedVariables.Select(v => new RecorderDraftDetectedVariable(
            v,
            "Recorded fixture input",
            IsSensitiveVariable(v),
            $"{{{v}}}",
            sensitive.HasSensitiveInput || IsSensitiveVariable(v)));

    private static RecorderSensitiveInputSummary SensitiveSummary(ReliableRecorderTrajectory trajectory)
    {
        var kinds = trajectory.DetectedVariables.Select(KindForVariable)
            .Concat(trajectory.Interactions.Where(i => i.SensitiveInputDetected).Select(_ => RecordedInputSensitivityKind.UnknownSensitive))
            .Where(k => k != RecordedInputSensitivityKind.PersonalData || trajectory.RiskProfile.HasFlag(ReliableRecipeRiskProfile.SensitiveData))
            .Distinct()
            .ToArray();
        var hasSensitive = kinds.Length > 0 || trajectory.Interactions.Any(i => i.SensitiveInputDetected);
        return new RecorderSensitiveInputSummary(hasSensitive, kinds, RedactionApplied: hasSensitive, BlockedRawValues: hasSensitive, UserActionRequired: hasSensitive);
    }

    private static ReliableRecipeLabRecorderDraftReviewPanel RecorderDraftReviewPanel(
        ReliableRecorderTrajectory trajectory,
        RecorderDraftReviewState state,
        IReadOnlyList<RecorderDraftReviewChecklistItem> checklist,
        IReadOnlyList<RecorderDraftDetectedVariable> variables,
        RecorderSensitiveInputSummary sensitive) =>
        new(
            trajectory.Id,
            state switch
            {
                RecorderDraftReviewState.DryRunCandidate => "Recorder draft: dry-run candidate after review",
                RecorderDraftReviewState.BlockedChallenge => "Recorder draft: human handoff required for challenge",
                RecorderDraftReviewState.BlockedSensitiveInput => "Recorder draft: sensitive input redacted",
                RecorderDraftReviewState.BlockedAmbiguousTarget => "Recorder draft: ambiguous target needs confirmation",
                RecorderDraftReviewState.NeedsValidationDesign => "Recorder draft: add validation before dry-run",
                RecorderDraftReviewState.NeedsEvidenceDesign => "Recorder draft: add evidence before dry-run",
                _ => "Recorder draft: needs review"
            },
            checklist.Select(i => new ReliableRecipeLabRecorderChecklistItem(i.Code, i.Title, i.Description, i.Severity.ToString(), i.IsBlocking, i.RecommendedFix)).ToArray(),
            variables.Select(v => new ReliableRecipeLabDetectedVariableRow(v.Name, v.Source, v.ReplacementToken, v.IsSensitive, v.NeedsUserConfirmation)).ToArray(),
            sensitive.HasSensitiveInput ? "Sensitive input was redacted; raw values are blocked." : "No sensitive raw input is stored in this fixture draft.",
            "This draft cannot run live. Review validation, evidence, risk and handoff requirements before any future fixture dry-run.");

    private static bool IsChallenge(ReliableRecorderTrajectory trajectory) =>
        trajectory.Id.Contains("captcha", StringComparison.OrdinalIgnoreCase) ||
        trajectory.Id.Contains("two-factor", StringComparison.OrdinalIgnoreCase) ||
        trajectory.SensitiveDataSummary.Contains("CAPTCHA", StringComparison.OrdinalIgnoreCase) ||
        trajectory.SensitiveDataSummary.Contains("2FA", StringComparison.OrdinalIgnoreCase);

    private static bool IsSensitiveVariable(string value) =>
        value.Contains("PASSWORD", StringComparison.OrdinalIgnoreCase) ||
        value.Contains("TOKEN", StringComparison.OrdinalIgnoreCase) ||
        value.Contains("API", StringComparison.OrdinalIgnoreCase) ||
        value.Contains("COOKIE", StringComparison.OrdinalIgnoreCase) ||
        value.Contains("AUTH", StringComparison.OrdinalIgnoreCase);

    private static RecordedInputSensitivityKind KindForVariable(string value)
    {
        if (value.Contains("PASSWORD", StringComparison.OrdinalIgnoreCase)) return RecordedInputSensitivityKind.Password;
        if (value.Contains("TOKEN", StringComparison.OrdinalIgnoreCase)) return RecordedInputSensitivityKind.Token;
        if (value.Contains("API", StringComparison.OrdinalIgnoreCase)) return RecordedInputSensitivityKind.ApiKey;
        if (value.Contains("COOKIE", StringComparison.OrdinalIgnoreCase)) return RecordedInputSensitivityKind.Cookie;
        if (value.Contains("AUTH", StringComparison.OrdinalIgnoreCase)) return RecordedInputSensitivityKind.AuthorizationHeader;
        if (value.Contains("EMAIL", StringComparison.OrdinalIgnoreCase)) return RecordedInputSensitivityKind.Email;
        if (value.Contains("PHONE", StringComparison.OrdinalIgnoreCase)) return RecordedInputSensitivityKind.Phone;
        if (value.Contains("PAYMENT", StringComparison.OrdinalIgnoreCase)) return RecordedInputSensitivityKind.Payment;
        if (value.Contains("GOV", StringComparison.OrdinalIgnoreCase)) return RecordedInputSensitivityKind.GovernmentId;
        return RecordedInputSensitivityKind.PersonalData;
    }

    private static string TitleFor(string code)
    {
        if (code.Contains("validation", StringComparison.OrdinalIgnoreCase)) return "Validation missing";
        if (code.Contains("evidence", StringComparison.OrdinalIgnoreCase)) return "Evidence missing";
        if (code.Contains("target", StringComparison.OrdinalIgnoreCase)) return "Target needs review";
        if (code.Contains("sandbox", StringComparison.OrdinalIgnoreCase)) return "Sandbox blocked";
        if (code.Contains("risk", StringComparison.OrdinalIgnoreCase)) return "Risk review required";
        if (code.Contains("human", StringComparison.OrdinalIgnoreCase)) return "Human handoff required";
        return "Recorder draft review item";
    }

    private static RecorderDraftReviewChecklistItem Item(
        string code,
        string title,
        string description,
        ReliableRecipeQualitySeverity severity,
        bool isBlocking,
        string recommendedFix,
        string? relatedStepId) =>
        new(code, title, description, severity, isBlocking, recommendedFix, relatedStepId);
}
