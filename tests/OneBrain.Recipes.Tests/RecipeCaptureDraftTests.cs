using Microsoft.VisualStudio.TestTools.UnitTesting;
using OneBrain.Core.Recipes;

namespace OneBrain.Recipes.Tests;

[TestClass]
[TestCategory("RecipeCaptureDraft")]
public sealed class RecipeCaptureDraftTests
{
    [TestMethod]
    public void CaptureSessionDefaultsToFixtureManualReferenceOnlyMode()
    {
        var session = Session(RecipeCaptureMode.FixtureOnly);

        Assert.AreEqual(RecipeCaptureMode.FixtureOnly, session.SourceMode);
        Assert.IsFalse(session.RecorderEnabled);
        Assert.IsFalse(session.ReplayEnabled);
        Assert.IsFalse(session.LiveRuntimeEnabled);
        Assert.IsFalse(session.CanProduceRunReadyRecipe);
        Assert.IsFalse(session.CanRecordLiveBrowserDesktop);
    }

    [TestMethod]
    public void FutureBrowserDesktopConnectorCaptureModesAreBlocked()
    {
        foreach (var mode in new[] { RecipeCaptureMode.FutureBrowserCaptureBlocked, RecipeCaptureMode.FutureDesktopCaptureBlocked, RecipeCaptureMode.FutureConnectorCaptureBlocked })
        {
            var readiness = RecipeCaptureSafetyPolicy.EvaluateSession(Session(mode), [Step()]);

            Assert.IsFalse(readiness.IsRunReady, mode.ToString());
            Assert.IsTrue(readiness.BlockingIssues.Any(i => i.IssueId == "capture-live-mode-blocked"), mode.ToString());
            Assert.AreEqual(RecipeCaptureSessionStatus.BlockedLiveRuntimeDisabled, readiness.Status, mode.ToString());
            Assert.IsFalse(readiness.LiveRuntimeEnabled, mode.ToString());
        }
    }

    [TestMethod]
    public void CaptureSessionCannotEnableRecorderReplayOrProduceRunReadyRecipe()
    {
        var result = RecipeCaptureSafetyPolicy.GenerateDraft(Session(), [Step()]);

        Assert.IsFalse(result.RecorderReplayEnabled);
        Assert.IsFalse(result.LiveRuntimeEnabled);
        Assert.IsFalse(result.CreatesRunReadyRecipe);
        Assert.IsFalse(result.Readiness.IsRunReady);
        Assert.IsTrue(result.ReviewRequired);
    }

    [TestMethod]
    public void CapturedInputStoresRefSummaryOnlyNoRawValue()
    {
        var input = Input("input.customer", RecipeCapturedInputValueCategory.Text);

        Assert.IsFalse(input.RawValuePresent);
        Assert.IsFalse(input.SecretValueStored);
        Assert.IsTrue(input.SafeForDraft);
        Assert.AreEqual("Customer name", input.OperatorVisibleSummary);
    }

    [TestMethod]
    public void SecretLikeCapturedInputCreatesWarningAndBlocksRunReadyConversion()
    {
        var step = Step(inputs: [Input("input.password", RecipeCapturedInputValueCategory.SecretLike, secretLike: true)]);
        var readiness = RecipeCaptureSafetyPolicy.EvaluateSession(Session(), [step]);

        Assert.IsFalse(readiness.IsRunReady);
        Assert.IsTrue(readiness.Warnings.Any(w => w.Kind == RecipeCaptureWarningKind.SecretLikeInput));
        Assert.IsTrue(readiness.BlockingIssues.Any(i => i.IssueId.StartsWith("capture-warning-blocks-run-ready:", StringComparison.Ordinal)));
    }

    [TestMethod]
    public void PersonalFiscalPaymentInputsCreateRiskWarningsAndRedactionRequirement()
    {
        var step = Step(inputs:
        [
            Input("input.person", RecipeCapturedInputValueCategory.PersonalData, personal: true),
            Input("input.fiscal", RecipeCapturedInputValueCategory.FiscalData, fiscal: true),
            Input("input.payment", RecipeCapturedInputValueCategory.PaymentData, payment: true)
        ]);

        var readiness = RecipeCaptureSafetyPolicy.EvaluateSession(Session(), [step]);

        Assert.IsTrue(readiness.Warnings.Any(w => w.Kind == RecipeCaptureWarningKind.PersonalDataInput));
        Assert.IsTrue(readiness.Warnings.Any(w => w.Kind == RecipeCaptureWarningKind.FiscalDataInput));
        Assert.IsTrue(readiness.Warnings.Any(w => w.Kind == RecipeCaptureWarningKind.PaymentDataInput));
        Assert.IsFalse(readiness.LiveRuntimeEnabled);
    }

    [TestMethod]
    public void SubmitPaymentFiscalMessageDeletePublicationDraftRequiresApprovalHumanSuggestion()
    {
        var step = Step(RecipeCapturedStepKind.SubmitDraft, approvalRefs: []);
        var result = RecipeCaptureSafetyPolicy.GenerateDraft(Session(), [step]);

        Assert.IsFalse(result.Readiness.IsRunReady);
        Assert.IsTrue(result.Draft.ApprovalSuggestions.Any());
        Assert.IsTrue(result.Readiness.Warnings.Any(w => w.Kind == RecipeCaptureWarningKind.SubmitAction));
        Assert.IsFalse(result.LiveRuntimeEnabled);
    }

    [TestMethod]
    public void CapturedBrowserAndDesktopActionDraftRemainLiveBlocked()
    {
        var browser = RecipeCaptureSafetyPolicy.EvaluateSession(Session(), [Step(RecipeCapturedStepKind.BrowserActionDraft)]);
        var desktop = RecipeCaptureSafetyPolicy.EvaluateSession(Session(), [Step(RecipeCapturedStepKind.DesktopActionDraft)]);

        Assert.IsTrue(browser.Warnings.Any(w => w.Kind == RecipeCaptureWarningKind.BrowserLiveAction));
        Assert.IsTrue(desktop.Warnings.Any(w => w.Kind == RecipeCaptureWarningKind.DesktopLiveAction));
        Assert.IsFalse(browser.LiveRuntimeEnabled);
        Assert.IsFalse(desktop.LiveRuntimeEnabled);
    }

    [TestMethod]
    public void UnknownDraftRemainsBlocked()
    {
        var readiness = RecipeCaptureSafetyPolicy.EvaluateSession(Session(), [Step(RecipeCapturedStepKind.UnknownDraft)]);

        Assert.IsFalse(readiness.IsRunReady);
        Assert.IsTrue(readiness.Warnings.Any(w => w.Kind == RecipeCaptureWarningKind.UnknownUnsafe));
        Assert.IsTrue(readiness.BlockingIssues.Any(i => i.Status == RecipeReadinessStatus.BlockedRiskGate));
    }

    [TestMethod]
    public void CapturedLocatorRelativeCoordinateIsWarningLastResort()
    {
        var locator = Locator("locator.relative", RecipeLocatorStrategy.RelativeCoordinate, RecipeLocatorSafetyStatus.BlockedRelativeCoordinate, RecipeLocatorReplayEligibility.ManualReviewOnly);
        var readiness = RecipeCaptureSafetyPolicy.EvaluateSession(Session(), [Step(locators: [locator])]);

        Assert.IsTrue(readiness.Warnings.Any(w => w.Kind == RecipeCaptureWarningKind.RelativeCoordinateUsed));
        Assert.IsFalse(locator.LiveReplayEnabled);
        Assert.IsFalse(locator.LiveRepairApplyEnabled);
    }

    [TestMethod]
    public void CapturedLocatorAiFallbackForSensitiveActionRequiresHumanPolicy()
    {
        var locator = Locator("locator.ai", RecipeLocatorStrategy.AIFallback, RecipeLocatorSafetyStatus.BlockedAIFallback, RecipeLocatorReplayEligibility.ManualReviewOnly);
        var step = Step(RecipeCapturedStepKind.SubmitDraft, locators: [locator]);
        var readiness = RecipeCaptureSafetyPolicy.EvaluateSession(Session(), [step]);

        Assert.IsTrue(readiness.Warnings.Any(w => w.Kind == RecipeCaptureWarningKind.AIFallbackSuggested));
        Assert.IsTrue(readiness.BlockingIssues.Any(i => i.Status == RecipeReadinessStatus.BlockedRiskGate));
        Assert.IsFalse(locator.ActionAuthorityGranted);
    }

    [TestMethod]
    public void DraftGenerationCreatesDraftOnlyNotRunReady()
    {
        var result = RecipeCaptureSafetyPolicy.GenerateDraft(Session(), [Step()]);

        Assert.AreEqual(RecipeDraftGenerationStatus.NeedsReview, result.Status);
        Assert.IsFalse(result.Draft.RunReady);
        Assert.IsFalse(result.Draft.LiveReady);
        Assert.IsFalse(result.Draft.CanExecute);
        Assert.IsFalse(result.CreatesRunReadyRecipe);
    }

    [TestMethod]
    public void DraftGenerationSuggestsLimitsValidationEvidenceApprovalWhereMissing()
    {
        var step = Step(RecipeCapturedStepKind.SubmitDraft, validationRefs: ["validation.ref"], evidenceRefs: [Evidence("evidence.ref")]);
        var result = RecipeCaptureSafetyPolicy.GenerateDraft(Session(), [step]);

        Assert.IsTrue(result.Draft.BlockSuggestions.Any());
        Assert.IsTrue(result.Draft.ValidationSuggestions.Any());
        Assert.IsTrue(result.Draft.EvidenceSuggestions.Any());
        Assert.IsTrue(result.Draft.ApprovalSuggestions.Any());
        Assert.IsTrue(result.Draft.ReviewRequired);
    }

    [TestMethod]
    public void MissingValidationAndEvidenceBlockRunReadyConversion()
    {
        var result = RecipeCaptureSafetyPolicy.GenerateDraft(Session(), [Step(validationRefs: [], evidenceRefs: [])]);

        Assert.IsFalse(result.Readiness.IsRunReady);
        Assert.IsTrue(result.Readiness.BlockingIssues.Any(i => i.Status == RecipeReadinessStatus.BlockedMissingValidation));
        Assert.IsTrue(result.Readiness.BlockingIssues.Any(i => i.Status == RecipeReadinessStatus.BlockedMissingEvidencePolicy));
    }

    [TestMethod]
    public void MissingToolTrustAndSecretRefsBlockCredentialedDraftReadiness()
    {
        var step = Step(RecipeCapturedStepKind.ConnectorDraft, toolTrustRefs: [], secretRefs: []);
        var result = RecipeCaptureSafetyPolicy.GenerateDraft(Session(), [step]);

        Assert.IsFalse(result.Readiness.IsRunReady);
        Assert.IsTrue(result.Readiness.Warnings.Any(w => w.Kind == RecipeCaptureWarningKind.MissingToolTrust || w.Kind == RecipeCaptureWarningKind.MissingSecretRef) || result.Draft.ApprovalSuggestions.Any());
        Assert.IsFalse(result.LiveRuntimeEnabled);
    }

    [TestMethod]
    public void TwoFactorCaptchaChallengeWarningMapsToHumanBlockAndNoBypass()
    {
        var warning = new RecipeCaptureWarning("warning.2fa", RecipeCaptureWarningKind.TwoFactorOrCaptcha, RecipeReadinessIssueSeverity.Warning, "Human checkpoint required; no bypass.", "step.2fa");
        var session = Session(riskWarnings: [warning]);
        var readiness = RecipeCaptureSafetyPolicy.EvaluateSession(session, [Step()]);

        Assert.IsTrue(readiness.Warnings.Any(w => w.Kind == RecipeCaptureWarningKind.TwoFactorOrCaptcha));
        Assert.IsTrue(readiness.BlockingIssues.Any(i => i.Status == RecipeReadinessStatus.BlockedRiskGate));
        Assert.IsFalse(readiness.OperatorSummary.Contains("bypass", StringComparison.OrdinalIgnoreCase));
    }

    [TestMethod]
    public void TemplateMappingCannotOverrideCompositeTemplateReadiness()
    {
        var mapping = new RecipeDraftTemplateMapping(
            "mapping.bad",
            "browser.table_extract_preview",
            RecipeTemplateCategory.GenericBrowserPortal,
            RecipeTemplateSystem.GenericBrowserPortal,
            RecipeTemplateRuntimeEligibility.LiveBlocked,
            TemplateReadiness: null,
            "bad mapping",
            RequiresCompositeReadiness: false);

        var draft = new RecipeDraft("draft.bad", "Bad draft", "capture.fixture", [], [], [ValidationSuggestion()], [EvidenceSuggestion()], [], [mapping], RecipeRiskLevel.Low, [], [], SafeAction());
        var readiness = RecipeCaptureSafetyPolicy.EvaluateDraft(draft);

        Assert.IsFalse(readiness.IsRunReady);
        Assert.IsTrue(readiness.BlockingIssues.Any(i => i.IssueId == "capture-template-readiness-override-blocked"));
    }

    [TestMethod]
    public void CaptureDraftCanMapToExcelTemplatePackSafely()
    {
        var session = Session(targetCategory: RecipeTemplateCategory.ExcelMicrosoft365, targetSystem: RecipeTemplateSystem.Excel);
        var mapping = RecipeCaptureTemplateMapper.MapToTemplate(session, RecipeTemplateCatalogFactory.CreateGlobalLatamV1(), TemplateContext());

        Assert.AreEqual(RecipeTemplateCategory.ExcelMicrosoft365, mapping.Category);
        Assert.AreEqual(RecipeTemplateSystem.Excel, mapping.System);
        Assert.IsNotNull(mapping.TemplateReadiness);
        Assert.IsFalse(mapping.LiveRuntimeEnabled);
        Assert.IsFalse(mapping.CanOverrideCompositeReadiness);
    }

    [TestMethod]
    public void CaptureDraftMapsToMercadoLibreAndArcaOnlyAsPreviewHumanGatedLiveBlocked()
    {
        var catalog = RecipeTemplateCatalogFactory.CreateGlobalLatamV1();
        var meli = RecipeCaptureTemplateMapper.MapToTemplate(Session(targetCategory: RecipeTemplateCategory.MercadoLibreMercadoPago, targetSystem: RecipeTemplateSystem.MercadoLibre), catalog, TemplateContext());
        var arca = RecipeCaptureTemplateMapper.MapToTemplate(Session(targetCategory: RecipeTemplateCategory.ARCAFiscal, targetSystem: RecipeTemplateSystem.ARCA), catalog, TemplateContext());

        Assert.AreEqual(RecipeTemplateRuntimeEligibility.FutureGated, meli.RuntimeEligibility);
        Assert.AreEqual(RecipeTemplateRuntimeEligibility.FutureGated, arca.RuntimeEligibility);
        Assert.IsFalse(meli.TemplateReadiness!.IsReady);
        Assert.IsFalse(arca.TemplateReadiness!.IsReady);
        Assert.IsFalse(meli.LiveRuntimeEnabled);
        Assert.IsFalse(arca.LiveRuntimeEnabled);
    }

    [TestMethod]
    public void CaptureDraftLabSummaryExcludesRawSecretsPayloads()
    {
        var cell = new RecipeLabCell(
            "cell.capture",
            RecipeLabCellKind.Overview,
            RecipeLabCellStatus.ReferenceOnly,
            "Capture draft",
            "Redacted capture draft summary.",
            new RecipeLabCellEvidenceSummary(["evidence.ref"], "redacted", ReferenceOnly: true, RawPayloadExposed: false));

        Assert.IsTrue(cell.InspectionOnly);
        Assert.IsFalse(cell.RawSecretValuesShown);
        Assert.IsFalse(cell.EvidenceSummary!.RawPayloadExposed);
    }

    [TestMethod]
    public void CaptureTimelineAndEvidenceRefsAreReferenceOnly()
    {
        var evidence = Evidence("evidence.timeline", RecipeEvidenceSourceKind.TimelineEventRef);
        var session = Session(evidenceRefs: [evidence], timelineRefs: ["timeline.capture"]);

        Assert.IsTrue(evidence.ReferenceOnly);
        Assert.IsTrue(evidence.SafeForDraft);
        Assert.AreEqual("timeline.capture", session.TimelineRefs.Single());
    }

    [TestMethod]
    public void NoRealRecorderReplayCaptureOrLiveRuntimeIsIntroduced()
    {
        var session = Session();
        var result = RecipeCaptureSafetyPolicy.GenerateDraft(session, [Step()]);

        Assert.IsFalse(session.RecorderEnabled);
        Assert.IsFalse(session.ReplayEnabled);
        Assert.IsFalse(session.LiveRuntimeEnabled);
        Assert.IsFalse(result.RecorderReplayEnabled);
        Assert.IsFalse(result.LiveRuntimeEnabled);
        Assert.IsFalse(result.Draft.RunReady);
        Assert.IsFalse(result.Draft.LiveReady);
    }

    [TestMethod]
    public void DisabledCaptureModeIsBlockedByPolicy()
    {
        var readiness = RecipeCaptureSafetyPolicy.EvaluateSession(Session(RecipeCaptureMode.Disabled), [Step()]);

        Assert.AreEqual(RecipeCaptureSessionStatus.BlockedByPolicy, readiness.Status);
        Assert.IsTrue(readiness.BlockingIssues.Any(i => i.IssueId == "capture-disabled"));
    }

    [TestMethod]
    public void ImportedTraceRefOnlyRemainsReferenceOnlyAndNotLiveCapture()
    {
        var session = Session(RecipeCaptureMode.ImportedTraceRefOnly);

        Assert.IsFalse(session.CanRecordLiveBrowserDesktop);
        Assert.IsFalse(session.LiveRuntimeEnabled);
        Assert.AreEqual(RecipeCaptureMode.ImportedTraceRefOnly, session.SourceMode);
    }

    [TestMethod]
    public void RawCapturedInputBlocksDraftReadiness()
    {
        var step = Step(inputs: [Input("input.raw", RecipeCapturedInputValueCategory.Text, rawValue: true)]);
        var readiness = RecipeCaptureSafetyPolicy.EvaluateSession(Session(), [step]);

        Assert.IsTrue(readiness.BlockingIssues.Any(i => i.IssueId == "capture-raw-input-detected"));
        Assert.AreEqual(RecipeCaptureSessionStatus.BlockedRawSecretDetected, readiness.Status);
    }

    [TestMethod]
    public void EvidenceRequiringRealCaptureBlocksCaptureReadiness()
    {
        var evidence = new RecipeCapturedEvidenceRef("evidence.live", RecipeEvidenceSourceKind.ScreenshotBeforeRef, ReferenceOnly: false, RawPayloadPresent: false, RequiresRealCapture: true);
        var readiness = RecipeCaptureSafetyPolicy.EvaluateSession(Session(evidenceRefs: [evidence]), [Step(evidenceRefs: [evidence])]);

        Assert.IsTrue(readiness.BlockingIssues.Any(i => i.IssueId is "capture-evidence-not-reference-only" or "capture-step-evidence-not-reference-only"));
        Assert.IsFalse(evidence.SafeForDraft);
    }

    [TestMethod]
    public void DraftVariableSuggestionStoresNoRawValue()
    {
        var result = RecipeCaptureSafetyPolicy.GenerateDraft(Session(), [Step(inputs: [Input("input.secret", RecipeCapturedInputValueCategory.SecretLike, secretLike: true)])]);
        var variable = result.Draft.VariableSuggestions.Single();

        Assert.IsTrue(variable.SecretRefOnly);
        Assert.IsFalse(variable.RawValueStored);
    }

    [TestMethod]
    public void DraftBlockSuggestionsCannotExecute()
    {
        var result = RecipeCaptureSafetyPolicy.GenerateDraft(Session(), [Step(RecipeCapturedStepKind.ClickDraft)]);
        var block = result.Draft.BlockSuggestions.Single();

        Assert.IsFalse(block.CanExecute);
        Assert.IsFalse(block.LiveRuntimeEnabled);
    }

    [TestMethod]
    public void UnknownTemplateMappingDefaultsFutureGatedAndBlockedForManualReview()
    {
        var mapping = RecipeCaptureTemplateMapper.MapToTemplate(
            Session(targetCategory: RecipeTemplateCategory.Unknown, targetSystem: RecipeTemplateSystem.Unknown),
            RecipeTemplateCatalogFactory.CreateGlobalLatamV1(),
            TemplateContext());

        Assert.IsNull(mapping.TemplateId);
        Assert.AreEqual(RecipeTemplateRuntimeEligibility.FutureGated, mapping.RuntimeEligibility);
        Assert.IsFalse(mapping.LiveRuntimeEnabled);
    }

    [TestMethod]
    public void TemplateMappingPreservesCompositeReadinessResult()
    {
        var mapping = RecipeCaptureTemplateMapper.MapToTemplate(Session(), RecipeTemplateCatalogFactory.CreateGlobalLatamV1(), TemplateContext());

        Assert.IsNotNull(mapping.TemplateReadiness);
        Assert.AreEqual(mapping.TemplateReadiness!.LiveRuntimeEnabled, mapping.LiveRuntimeEnabled);
        Assert.IsFalse(mapping.CanOverrideCompositeReadiness);
    }

    [TestMethod]
    public void CaptureWarningMissingToolTrustMapsToToolTrustBlock()
    {
        var warning = new RecipeCaptureWarning("warning.tool", RecipeCaptureWarningKind.MissingToolTrust, RecipeReadinessIssueSeverity.Warning, "Missing tool trust ref.", "step.tool");
        var readiness = RecipeCaptureSafetyPolicy.EvaluateSession(Session(riskWarnings: [warning]), [Step()]);

        Assert.IsTrue(readiness.BlockingIssues.Any(i => i.Status == RecipeReadinessStatus.BlockedMissingToolTrust));
    }

    [TestMethod]
    public void CaptureWarningMissingSecretRefMapsToSecretReferenceBlock()
    {
        var warning = new RecipeCaptureWarning("warning.secret", RecipeCaptureWarningKind.MissingSecretRef, RecipeReadinessIssueSeverity.Warning, "Missing secret ref.", "step.secret");
        var readiness = RecipeCaptureSafetyPolicy.EvaluateSession(Session(secretWarnings: [warning]), [Step()]);

        Assert.IsTrue(readiness.BlockingIssues.Any(i => i.Status == RecipeReadinessStatus.BlockedMissingSecretReference));
    }

    [TestMethod]
    public void SensitiveDraftWithoutApprovalSuggestionBlocksApprovalPath()
    {
        var warning = new RecipeCaptureWarning("warning.public", RecipeCaptureWarningKind.PublicPosting, RecipeReadinessIssueSeverity.Warning, "Public posting needs review.", "step.public");
        var draft = new RecipeDraft("draft.public", "Public posting draft", "capture.fixture", [], [], [ValidationSuggestion()], [EvidenceSuggestion()], [], [], RecipeRiskLevel.High, [warning], [], SafeAction());
        var readiness = RecipeCaptureSafetyPolicy.EvaluateDraft(draft);

        Assert.IsTrue(readiness.BlockingIssues.Any(i => i.Status == RecipeReadinessStatus.BlockedMissingApprovalPolicy));
        Assert.IsFalse(readiness.IsRunReady);
    }

    [TestMethod]
    public void MissingValidationWarningMapsToValidationBlock()
    {
        var readiness = RecipeCaptureSafetyPolicy.EvaluateSession(Session(), [Step(validationRefs: [])]);

        Assert.IsTrue(readiness.Warnings.Any(w => w.Kind == RecipeCaptureWarningKind.MissingValidation));
        Assert.IsTrue(readiness.BlockingIssues.Any(i => i.Status == RecipeReadinessStatus.BlockedMissingValidation));
    }

    [TestMethod]
    public void MissingEvidenceWarningMapsToEvidenceBlock()
    {
        var readiness = RecipeCaptureSafetyPolicy.EvaluateSession(Session(), [Step(evidenceRefs: [])]);

        Assert.IsTrue(readiness.Warnings.Any(w => w.Kind == RecipeCaptureWarningKind.MissingEvidence));
        Assert.IsTrue(readiness.BlockingIssues.Any(i => i.Status == RecipeReadinessStatus.BlockedMissingEvidencePolicy));
    }

    [TestMethod]
    public void GeneratedDraftDoesNotEmitSuggestedRunReadyRecipeDefinition()
    {
        var result = RecipeCaptureSafetyPolicy.GenerateDraft(Session(), [Step()]);

        Assert.IsNull(result.SuggestedRecipeDefinition);
        Assert.IsFalse(result.CreatesRunReadyRecipe);
    }

    private static RecipeCaptureSession Session(
        RecipeCaptureMode mode = RecipeCaptureMode.FixtureOnly,
        RecipeTemplateCategory? targetCategory = RecipeTemplateCategory.ExcelMicrosoft365,
        RecipeTemplateSystem? targetSystem = RecipeTemplateSystem.Excel,
        IReadOnlyList<RecipeCapturedEvidenceRef>? evidenceRefs = null,
        IReadOnlyList<RecipeCaptureWarning>? secretWarnings = null,
        IReadOnlyList<RecipeCaptureWarning>? riskWarnings = null,
        IReadOnlyList<string>? timelineRefs = null) =>
        new(
            "capture.fixture",
            "Fixture capture",
            "Manual fixture capture description.",
            targetCategory,
            targetSystem,
            RecipeTemplateRegion.Global,
            [RecipeTemplateCountry.Global],
            mode,
            RecipeCaptureSafetyStatus.SafeForDraft,
            new RecipeCaptureReadiness(true, false, RecipeCaptureSessionStatus.Draft, [], [], "draft"),
            "Describe the observed workflow without recording it.",
            [new RecipeCapturedStepRef("step.fixture")],
            evidenceRefs ?? [Evidence("evidence.capture")],
            secretWarnings ?? [],
            riskWarnings ?? [],
            new RecipeDraftRef("draft.capture"),
            [],
            "redaction.summary",
            timelineRefs ?? ["timeline.capture"],
            [new RecipeLabSnapshotRef("lab.capture")]);

    private static RecipeCapturedStep Step(
        RecipeCapturedStepKind kind = RecipeCapturedStepKind.ExtractDraft,
        IReadOnlyList<RecipeCapturedInput>? inputs = null,
        IReadOnlyList<RecipeCapturedLocator>? locators = null,
        IReadOnlyList<RecipeCapturedEvidenceRef>? evidenceRefs = null,
        IReadOnlyList<string>? validationRefs = null,
        IReadOnlyList<string>? approvalRefs = null,
        IReadOnlyList<string>? toolTrustRefs = null,
        IReadOnlyList<string>? secretRefs = null) =>
        new(
            "step.fixture",
            1,
            kind,
            "Extract fixture data by reference.",
            "target summary",
            inputs ?? [],
            locators ?? [Locator()],
            evidenceRefs ?? [Evidence("evidence.step")],
            validationRefs ?? ["validation.step"],
            RiskWarningRefs: [],
            SecretWarningRefs: [],
            approvalRefs ?? [],
            toolTrustRefs ?? ["tool.excel.fixture"],
            secretRefs ?? [],
            TemplateMappingRefs: [],
            RecipeCaptureSafetyStatus.SafeForDraft,
            BlockedReasons: [],
            SafeAction());

    private static RecipeCapturedInput Input(
        string id,
        RecipeCapturedInputValueCategory category,
        bool secretLike = false,
        bool personal = false,
        bool fiscal = false,
        bool payment = false,
        bool rawValue = false) =>
        new(
            id,
            "Customer name",
            category,
            RecipeEvidenceRedactionStatus.Applied,
            $"variable:{id}",
            RawValuePresent: rawValue,
            SecretLike: secretLike,
            PersonalData: personal,
            FiscalData: fiscal,
            PaymentData: payment,
            OperatorVisibleSummary: "Customer name");

    private static RecipeCapturedLocator Locator(
        string id = "locator.fixture",
        RecipeLocatorStrategy strategy = RecipeLocatorStrategy.KnownTarget,
        RecipeLocatorSafetyStatus safety = RecipeLocatorSafetyStatus.SafeForPreview,
        RecipeLocatorReplayEligibility replay = RecipeLocatorReplayEligibility.FixtureOnly) =>
        new(
            id,
            strategy,
            RecipeLocatorConfidence.High,
            RecipeLocatorDriftStatus.Stable,
            "redacted target summary",
            ["locator.evidence"],
            FallbackOrder: strategy == RecipeLocatorStrategy.RelativeCoordinate ? 7 : 1,
            safety,
            replay,
            RepairSuggestionRef: null);

    private static RecipeCapturedEvidenceRef Evidence(string id, RecipeEvidenceSourceKind source = RecipeEvidenceSourceKind.ExtractedDataRef) =>
        new(id, source);

    private static RecipeDraftValidationSuggestion ValidationSuggestion() =>
        new("validation.suggestion", RecipeValidationKind.EvidenceRefExists, RecipeValidationSeverity.Blocking, "step.fixture");

    private static RecipeDraftEvidenceSuggestion EvidenceSuggestion() =>
        new("evidence.suggestion", RecipeEvidenceSourceKind.ExtractedDataRef, "step.fixture");

    private static RecipeSafeNextAction SafeAction() =>
        new(RecipeSafeNextActionKind.RequestMoreEvidence, "Keep draft in review and add missing refs.");

    private static RecipeTemplateReadinessContext TemplateContext()
    {
        var catalog = RecipeTemplateCatalogFactory.CreateGlobalLatamV1();
        return new(
            Registry(),
            Secrets(),
            ConnectorEligibilities(catalog),
            TriggerBindings: [],
            EvidencePack(),
            [new RecipeStepEvidenceResult(true, RecipeStepEvidenceStatus.Satisfied, [], [])],
            [new RecipeValidationEvidence("validation.evidence", RecipeValidationKind.EvidenceRefExists, "expected", "redacted actual", ["evidence.ref"], RecipeValidationEvidenceStatus.Passed, RecipeValidationSeverity.Blocking, RecipeEvidenceRedactionStatus.Applied)],
            RecipeApprovalNarrativeFactory.Create("narrative.template", "recipe.template", "9.0.0", "run.template", RecipeHumanInterventionKind.PaymentConfirmationRequired),
            LabSnapshot: null,
            RawSecretDetected: false);
    }

    private static RecipeToolTrustRegistry Registry() =>
        new([
            TrustedTool("tool.excel.fixture", RecipeToolCategory.Microsoft365),
            TrustedTool("tool.google.fixture", RecipeToolCategory.Microsoft365),
            TrustedTool("tool.sap.future", RecipeToolCategory.SAP) with { RuntimeStatus = RecipeToolRuntimeStatus.FutureGated },
            TrustedTool("tool.meli.future", RecipeToolCategory.Marketplace) with { RuntimeStatus = RecipeToolRuntimeStatus.FutureGated },
            TrustedTool("tool.mercadopago.future", RecipeToolCategory.Payment) with { RuntimeStatus = RecipeToolRuntimeStatus.FutureGated },
            TrustedTool("tool.arca.future", RecipeToolCategory.Fiscal) with { RuntimeStatus = RecipeToolRuntimeStatus.FutureGated },
            TrustedTool("tool.erp.future", RecipeToolCategory.ERP) with { RuntimeStatus = RecipeToolRuntimeStatus.FutureGated },
            TrustedTool("tool.browser.runtime", RecipeToolCategory.BrowserRuntime) with { RuntimeStatus = RecipeToolRuntimeStatus.LiveBlocked },
            TrustedTool("tool.desktop.runtime", RecipeToolCategory.DesktopRuntime) with { RuntimeStatus = RecipeToolRuntimeStatus.LiveBlocked }
        ]);

    private static RecipeToolTrustEntry TrustedTool(string toolId, RecipeToolCategory category) =>
        RecipeToolTrustEntry.CandidateConnector(toolId, $"{toolId} fixture") with
        {
            Category = category,
            TrustLevel = RecipeToolTrustLevel.ApprovedForFixture,
            RuntimeStatus = RecipeToolRuntimeStatus.FixtureOnly,
            RequiredSecretRefs = [],
            RequiredApprovalPolicyRefs = ["approval.fixture"],
            EvidencePolicyRefs = ["evidence.fixture"],
            RedactionPolicyRefs = ["redaction.fixture"]
        };

    private static IReadOnlyList<RecipeSecretRequirement> Secrets() =>
        [
            Secret("secret.sap.ref", "tool.sap.future"),
            Secret("secret.meli.ref", "tool.meli.future"),
            Secret("secret.mp.ref", "tool.mercadopago.future"),
            Secret("secret.arca.ref", "tool.arca.future", RecipeSecretKind.FiscalCertificate),
            Secret("secret.erp.ref", "tool.erp.future"),
            Secret("secret.browser.ref", "tool.browser.runtime")
        ];

    private static RecipeSecretRequirement Secret(string id, string toolRef, RecipeSecretKind kind = RecipeSecretKind.ApiKey) =>
        new($"requirement:{id}", id, kind, RecipeSecretScope.Tool, toolRef, Required: true, RecipeSecretPresenceStatus.PresentByReference, RawValuePresent: false, "redaction.fixture");

    private static IReadOnlyList<RecipeConnectorEligibility> ConnectorEligibilities(RecipeTemplateCatalog catalog) =>
        catalog.Templates
            .Where(t => t.ConnectorEligibilityRefs.Count > 0)
            .SelectMany(t => t.ConnectorEligibilityRefs.Select(r => ConnectorEligibility(t, r)))
            .ToArray();

    private static RecipeConnectorEligibility ConnectorEligibility(RecipeTemplateDefinition template, string refId)
    {
        var action = Enum.Parse<RecipeConnectorActionCategory>(refId.Split(':').Last());
        var mode = template.RuntimeEligibility switch
        {
            RecipeTemplateRuntimeEligibility.LiveBlocked => RecipeConnectorRuntimeMode.LiveBlocked,
            RecipeTemplateRuntimeEligibility.FutureGated => RecipeConnectorRuntimeMode.FutureGated,
            RecipeTemplateRuntimeEligibility.FixtureOnly => RecipeConnectorRuntimeMode.FixtureOnly,
            _ => RecipeConnectorRuntimeMode.ReferenceOnly
        };

        return new(
            refId,
            template.RequiredToolTrustRefs.First(),
            mode,
            action,
            new RecipeConnectorTrustRequirement(template.RequiredToolTrustRefs.First(), template.RequiredSecretRefs, ApprovalRequired: RecipeToolTrustSecretsPolicy.RequiresApproval(action) || template.SafetyProfile.RequiresHumanApproval, EvidencePolicyRequired: true),
            ApprovalPolicyPresent: true,
            EvidencePolicyPresent: true);
    }

    private static RecipeEvidencePack EvidencePack() =>
        new(
            "pack.template",
            "recipe.template",
            "9.0.0",
            "run.template",
            MissionIdRef: null,
            WorkitemRefs: [],
            StepEvidenceRefs: ["step.evidence"],
            ValidationEvidenceRefs: ["validation.evidence"],
            ApprovalRefs: ["approval.ref"],
            TimelineEventRefs: ["timeline.ref"],
            ArtifactRefs: [],
            RedactionReportRef: "redaction.report",
            RecipeEvidenceSensitivity.Confidential,
            RecipeEvidenceCompleteness.Complete,
            RecipeEvidenceCaptureMode.ReferenceOnly,
            CreatedAt: null,
            RecipeRunMode.FixtureRun,
            FailureSummary: null,
            new RecipeEvidenceRedactionSummary(true, "redaction.policy", [], [], RecipeEvidenceSecretHandlingStatus.SecretRefsOnly));
}
