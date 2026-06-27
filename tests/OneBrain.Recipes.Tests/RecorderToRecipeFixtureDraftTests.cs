using Microsoft.VisualStudio.TestTools.UnitTesting;
using OneBrain.Core.Recipes;

namespace OneBrain.Recipes.Tests;

[TestClass]
[TestCategory("RecorderToRecipeFixtureDraft")]
public sealed class RecorderToRecipeFixtureDraftTests
{
    [TestMethod]
    public void FixtureCatalogReturnsAllRecorderDemonstrations()
    {
        var fixtures = ReliableRecipeRecorderFixtureCatalog.All();

        Assert.AreEqual(8, fixtures.Count);
        CollectionAssert.Contains(fixtures.Select(f => f.FixtureId).ToList(), "invoice_download_demonstration");
        CollectionAssert.Contains(fixtures.Select(f => f.FixtureId).ToList(), "corrected_user_click_demonstration");
    }

    [TestMethod]
    public void FixtureIdsAreStable()
    {
        var first = ReliableRecipeRecorderFixtureCatalog.All().Select(f => f.FixtureId).ToArray();
        var second = ReliableRecipeRecorderFixtureCatalog.All().Select(f => f.FixtureId).ToArray();

        CollectionAssert.AreEqual(first, second);
    }

    [TestMethod]
    public void FixturesContainNoLiveUrlsOrSecrets()
    {
        foreach (var fixture in ReliableRecipeRecorderFixtureCatalog.All())
        {
            var text = TextFor(fixture);
            Assert.IsFalse(text.Contains("http://", StringComparison.OrdinalIgnoreCase), fixture.FixtureId);
            Assert.IsFalse(text.Contains("https://", StringComparison.OrdinalIgnoreCase), fixture.FixtureId);
            Assert.IsFalse(text.Contains("connection " + "string", StringComparison.OrdinalIgnoreCase), fixture.FixtureId);
            Assert.IsFalse(text.Contains("authorization:", StringComparison.OrdinalIgnoreCase), fixture.FixtureId);
            Assert.IsFalse(text.Contains("password" + "=", StringComparison.OrdinalIgnoreCase), fixture.FixtureId);
            Assert.IsFalse(text.Contains("token" + "=", StringComparison.OrdinalIgnoreCase), fixture.FixtureId);
        }
    }

    [TestMethod]
    public void InvoiceDemonstrationConvertsToDraft()
    {
        var draft = ReliableRecipeRecorderFixtureCatalog.Get("invoice_download_demonstration").Draft;

        Assert.AreEqual("trajectory.invoice-download", draft.SourceTrajectoryId);
        Assert.AreEqual(ReliableRecipeCreatedFrom.RecorderDraft, draft.Recipe.CreatedFrom);
        Assert.AreEqual(RecorderDraftConversionDecision.CreatedNeedsReview, draft.ConversionDecision);
        Assert.IsFalse(draft.RunReady);
    }

    [TestMethod]
    public void InvoiceDraftMissingValidationCreatesChecklist()
    {
        var draft = ReliableRecipeRecorderFixtureCatalog.Get("invoice_download_demonstration").Draft;

        Assert.AreEqual(RecorderDraftReviewState.NeedsValidationDesign, draft.ReviewState);
        Assert.IsTrue(draft.ReviewChecklist.Any(i => i.Code.Contains("missing-validation", StringComparison.OrdinalIgnoreCase)));
        Assert.IsTrue(draft.MissingValidationRequirements.Count > 0);
    }

    [TestMethod]
    public void InvoiceDraftWithValidationCanBecomeFixtureDryRunCandidateReviewState()
    {
        var draft = ReliableRecipeRecorderFixtureCatalog.InvoiceDownloadWithValidation();

        Assert.AreEqual(RecorderDraftReviewState.DryRunCandidate, draft.ReviewState);
        Assert.AreEqual(ReliableRecipeRunMode.DraftOnly, draft.PreflightReport.ModeAllowed);
        Assert.IsFalse(draft.RunReady);
        Assert.IsFalse(draft.LiveRuntimeEnabled);
    }

    [TestMethod]
    public void PasswordDemonstrationRedactsSensitiveInput()
    {
        var draft = ReliableRecipeRecorderFixtureCatalog.Get("login_password_redacted_demonstration").Draft;

        Assert.AreEqual(RecorderDraftReviewState.BlockedSensitiveInput, draft.ReviewState);
        Assert.IsTrue(draft.SensitiveInputSummary.HasSensitiveInput);
        Assert.IsTrue(draft.SensitiveInputSummary.RedactionApplied);
        CollectionAssert.Contains(draft.SensitiveInputSummary.SensitiveKinds.ToList(), RecordedInputSensitivityKind.Password);
    }

    [TestMethod]
    public void PasswordRawValueIsNotStored()
    {
        var fixture = ReliableRecipeRecorderFixtureCatalog.Get("login_password_redacted_demonstration");
        var text = TextFor(fixture);

        Assert.IsTrue(fixture.Trajectory.Interactions.Any(i => i.TextInputRedacted == "{PASSWORD}"));
        Assert.IsFalse(text.Contains("hunter2", StringComparison.OrdinalIgnoreCase));
        Assert.IsFalse(text.Contains("p@ss", StringComparison.OrdinalIgnoreCase));
        Assert.IsFalse(text.Contains("raw password", StringComparison.OrdinalIgnoreCase));
    }

    [TestMethod]
    public void PasswordDemonstrationCannotBeDryRunCandidate()
    {
        var draft = ReliableRecipeRecorderFixtureCatalog.Get("login_password_redacted_demonstration").Draft;

        Assert.AreNotEqual(RecorderDraftReviewState.DryRunCandidate, draft.ReviewState);
        Assert.IsFalse(draft.PreflightReport.CanProceedToDryRun);
    }

    [TestMethod]
    public void CaptchaTwoFactorDemonstrationCreatesHumanHandoff()
    {
        var draft = ReliableRecipeRecorderFixtureCatalog.Get("captcha_two_factor_challenge_demonstration").Draft;

        Assert.AreEqual(RecorderDraftReviewState.BlockedChallenge, draft.ReviewState);
        Assert.IsTrue(draft.HumanInterventionRequirements.Any(h => h.Reason == ReliableHumanInterventionReason.TwoFactorRequired));
        Assert.IsTrue(draft.LabViewModel.HumanInterventionPanel.Rows.Any(r => r.Value.Contains("Human handoff required", StringComparison.Ordinal)));
    }

    [TestMethod]
    public void CaptchaTwoFactorDoesNotCreateBypassOrSolverAction()
    {
        var draft = ReliableRecipeRecorderFixtureCatalog.Get("captcha_two_factor_challenge_demonstration").Draft;
        var text = ProductFacingText(draft.LabViewModel);

        Assert.IsFalse(draft.CanBypassChallenge);
        Assert.IsFalse(text.Contains("bypass" + " action", StringComparison.OrdinalIgnoreCase));
        Assert.IsFalse(text.Contains("solver action", StringComparison.OrdinalIgnoreCase));
    }

    [TestMethod]
    public void AmbiguousTargetDemonstrationNeedsReview()
    {
        var draft = ReliableRecipeRecorderFixtureCatalog.Get("ambiguous_continue_button_demonstration").Draft;

        Assert.AreEqual(RecorderDraftReviewState.BlockedAmbiguousTarget, draft.ReviewState);
        Assert.IsTrue(draft.ReviewChecklist.Any(i => i.Code.Contains("target-resolution-human-review", StringComparison.OrdinalIgnoreCase)));
    }

    [TestMethod]
    public void OcrOnlySensitiveTargetBlocked()
    {
        var draft = ReliableRecipeRecorderFixtureCatalog.Get("ocr_only_canvas_button_demonstration").Draft;

        Assert.IsTrue(draft.ReviewChecklist.Any(i => i.Code == "ocr-supporting-signal-only"));
        Assert.AreEqual(TargetResolutionQualityDecision.Blocked, draft.QualityReport.TargetResolutionQuality.Decision);
        Assert.IsTrue(draft.LabViewModel.PerceptionPanel.Rows.Any(r => r.Value.Contains("OCR signal is supporting evidence only", StringComparison.Ordinal)));
    }

    [TestMethod]
    public void GovernmentFormSubmitHighRiskBlocked()
    {
        var draft = ReliableRecipeRecorderFixtureCatalog.Get("government_form_submit_demonstration").Draft;

        Assert.AreEqual("danger", draft.LabViewModel.RiskPanel.Tone);
        Assert.IsTrue(draft.QualityReport.RiskPosture.BlockedReasons.Any(r => r.Contains("government", StringComparison.OrdinalIgnoreCase) || r.Contains("irreversible", StringComparison.OrdinalIgnoreCase)));
        Assert.IsFalse(draft.PreflightReport.CanProceedToDryRun);
    }

    [TestMethod]
    public void DesktopFutureDemonstrationSandboxBlocked()
    {
        var draft = ReliableRecipeRecorderFixtureCatalog.Get("desktop_future_demonstration").Draft;

        Assert.AreEqual("danger", draft.LabViewModel.SandboxPanel.Tone);
        Assert.IsTrue(draft.QualityReport.SandboxReadiness.BlockedCapabilities.Contains("desktop-live-surface"));
        Assert.IsFalse(draft.CanUseBrowserOrDesktopHooks);
    }

    [TestMethod]
    public void UserCorrectionMarkerCreatesReviewChecklist()
    {
        var draft = ReliableRecipeRecorderFixtureCatalog.Get("corrected_user_click_demonstration").Draft;

        Assert.IsTrue(draft.ReviewChecklist.Any(i => i.Code == "user-correction-marker"));
        Assert.IsTrue(draft.TrajectoryHasUserCorrectionMarkerForTest());
    }

    [TestMethod]
    public void ConvertedDraftIncludesM2QualityReport()
    {
        var draft = ReliableRecipeRecorderFixtureCatalog.Get("invoice_download_demonstration").Draft;

        Assert.AreEqual(draft.Recipe.Id, draft.QualityReport.RecipeId);
        Assert.IsTrue(draft.QualityReport.CategoryScores.Count > 0);
    }

    [TestMethod]
    public void ConvertedDraftIncludesM2PreflightReport()
    {
        var draft = ReliableRecipeRecorderFixtureCatalog.Get("invoice_download_demonstration").Draft;

        Assert.AreEqual(draft.Recipe.Id, draft.PreflightReport.RecipeId);
        Assert.IsTrue(draft.PreflightReport.CanProceedToDraftOnly);
        Assert.IsFalse(draft.PreflightReport.LiveRuntimeEnabled);
    }

    [TestMethod]
    public void ConvertedDraftIncludesM3LabViewModel()
    {
        var draft = ReliableRecipeRecorderFixtureCatalog.Get("invoice_download_demonstration").Draft;

        Assert.AreEqual(draft.Recipe.Id, draft.LabViewModel.RecipeId);
        Assert.AreEqual("Runtime not enabled", draft.LabViewModel.NoLiveRuntimeNotice.Title);
        Assert.IsTrue(draft.LabViewModel.ReadOnly);
    }

    [TestMethod]
    public void LabViewModelIncludesRecorderDraftPanel()
    {
        var draft = ReliableRecipeRecorderFixtureCatalog.Get("invoice_download_demonstration").Draft;

        Assert.AreEqual("trajectory.invoice-download", draft.LabViewModel.RecorderDraftReview.SourceTrajectoryLabel);
        Assert.IsTrue(draft.LabViewModel.RecorderDraftReview.DraftReviewStateLabel.Contains("Recorder draft:", StringComparison.Ordinal));
        Assert.IsTrue(draft.LabViewModel.RecorderDraftReview.ReviewChecklist.Count > 0);
    }

    [TestMethod]
    public void LabViewModelContainsNoRunPlaybackOrRecordLiveLabels()
    {
        var forbidden = new[] { "Run now", "Run recipe", "playback", "record live", "Live recorder " + "ready", "Autonomous " + "playback", "Replay " + "automatically", "Records your " + "screen" };

        foreach (var fixture in ReliableRecipeRecorderFixtureCatalog.All())
        {
            var text = ProductFacingText(fixture.Draft.LabViewModel);
            foreach (var forbiddenText in forbidden)
            {
                Assert.IsFalse(text.Contains(forbiddenText, StringComparison.OrdinalIgnoreCase), $"{fixture.FixtureId}:{forbiddenText}");
            }
        }
    }

    [TestMethod]
    public void DetectedVariablesUseReplacementTokens()
    {
        var draft = ReliableRecipeRecorderFixtureCatalog.Get("login_password_redacted_demonstration").Draft;

        Assert.IsTrue(draft.DetectedVariables.All(v => v.ReplacementToken.StartsWith("{", StringComparison.Ordinal) && v.ReplacementToken.EndsWith("}", StringComparison.Ordinal)));
        Assert.IsTrue(draft.LabViewModel.RecorderDraftReview.DetectedVariables.All(v => v.ReplacementToken.StartsWith("{", StringComparison.Ordinal)));
    }

    [TestMethod]
    public void SensitiveInputSummaryRedacted()
    {
        var draft = ReliableRecipeRecorderFixtureCatalog.Get("login_password_redacted_demonstration").Draft;

        Assert.IsTrue(draft.SensitiveInputSummary.RedactionApplied);
        Assert.IsTrue(draft.SensitiveInputSummary.BlockedRawValues);
        Assert.IsTrue(draft.LabViewModel.RecorderDraftReview.SensitiveInputSummary.Contains("Sensitive input was redacted", StringComparison.Ordinal));
    }

    [TestMethod]
    public void HumanInterventionPlanQualityReflectedInLab()
    {
        var draft = ReliableRecipeRecorderFixtureCatalog.Get("captcha_two_factor_challenge_demonstration").Draft;

        Assert.IsTrue(draft.QualityReport.HumanInterventionPlanQuality.Score >= 0.75);
        Assert.AreEqual("success", draft.LabViewModel.HumanInterventionPanel.Tone);
        Assert.IsTrue(draft.LabViewModel.RecorderDraftReview.ReviewChecklist.Any(i => i.Title.Contains("Human handoff", StringComparison.Ordinal)));
    }

    [TestMethod]
    public void DeterministicConversionAcrossRepeatedRuns()
    {
        var first = ReliableRecipeRecorderFixtureCatalog.Get("invoice_download_demonstration").Draft;
        var second = ReliableRecipeRecorderFixtureCatalog.Get("invoice_download_demonstration").Draft;

        Assert.AreEqual(first.ReviewState, second.ReviewState);
        Assert.AreEqual(first.QualityReport.OverallScore, second.QualityReport.OverallScore);
        CollectionAssert.AreEqual(first.ReviewChecklist.Select(i => i.Code).ToArray(), second.ReviewChecklist.Select(i => i.Code).ToArray());
    }

    [TestMethod]
    public void NoProviderBrowserDesktopRuntimeDependencyAdded()
    {
        foreach (var fixture in ReliableRecipeRecorderFixtureCatalog.All())
        {
            var draft = fixture.Draft;
            Assert.IsFalse(draft.LiveRuntimeEnabled, fixture.FixtureId);
            Assert.IsFalse(draft.CanStartRecorder, fixture.FixtureId);
            Assert.IsFalse(draft.CanCaptureMouseOrKeyboard, fixture.FixtureId);
            Assert.IsFalse(draft.CanCaptureScreenOrScreenshot, fixture.FixtureId);
            Assert.IsFalse(draft.CanUseBrowserOrDesktopHooks, fixture.FixtureId);
            Assert.IsFalse(draft.CanCallProviderOrNetwork, fixture.FixtureId);
        }
    }

    [TestMethod]
    public void NoOcrInternalsTouchedByContracts()
    {
        var draft = ReliableRecipeRecorderFixtureCatalog.Get("ocr_only_canvas_button_demonstration").Draft;

        Assert.IsFalse(draft.LabViewModel.CanActivateOcrRuntime);
        Assert.IsTrue(draft.LabViewModel.PerceptionPanel.FooterNote.Contains("OCR is read-only supporting evidence", StringComparison.Ordinal));
    }

    [TestMethod]
    public void NoLiveNoActionInvariants()
    {
        foreach (var fixture in ReliableRecipeRecorderFixtureCatalog.All())
        {
            var draft = fixture.Draft;
            Assert.IsTrue(draft.FixtureOnly, fixture.FixtureId);
            Assert.IsTrue(draft.ReadOnly, fixture.FixtureId);
            Assert.IsFalse(draft.RunReady, fixture.FixtureId);
            Assert.IsFalse(draft.LabViewModel.CanStartRecipeRun, fixture.FixtureId);
            Assert.IsFalse(draft.LabViewModel.CanExecuteRecipe, fixture.FixtureId);
            Assert.IsFalse(draft.LabViewModel.CanEnableLiveRuntime, fixture.FixtureId);
            Assert.IsFalse(draft.LabViewModel.RecorderDraftReview.CanStartRecorder, fixture.FixtureId);
            Assert.IsFalse(draft.LabViewModel.RecorderDraftReview.CanPlaybackRecording, fixture.FixtureId);
        }
    }

    private static string TextFor(ReliableRecorderFixture fixture) =>
        string.Join(" ",
            fixture.FixtureId,
            fixture.Trajectory.Id,
            fixture.Trajectory.WorkspaceScope,
            fixture.Trajectory.SensitiveDataSummary,
            string.Join(" ", fixture.Trajectory.DetectedVariables),
            string.Join(" ", fixture.Trajectory.Interactions.Select(i => $"{i.ObservationRef} {i.InputEventKind} {i.TextInputRedacted} {i.TargetDescriptor.Text} {i.TargetDescriptor.Role} {i.TargetDescriptor.Selector}")),
            ProductFacingText(fixture.Draft.LabViewModel));

    private static string ProductFacingText(ReliableRecipeLabViewModel view) =>
        string.Join(" ",
            view.ReadinessLabel,
            view.ModeAllowedLabel,
            view.DecisionLabel,
            view.Summary,
            string.Join(" ", view.ReadOnlyActionLabels),
            view.NoLiveRuntimeNotice.Title,
            view.NoLiveRuntimeNotice.Message,
            string.Join(" ", view.NoLiveRuntimeNotice.BlockedCapabilities),
            view.RecorderDraftReview.SourceTrajectoryLabel,
            view.RecorderDraftReview.DraftReviewStateLabel,
            view.RecorderDraftReview.SensitiveInputSummary,
            view.RecorderDraftReview.DraftConversionNotice,
            string.Join(" ", view.RecorderDraftReview.ReviewChecklist.Select(i => $"{i.Code} {i.Title} {i.Description} {i.RecommendedFix}")),
            string.Join(" ", view.RecorderDraftReview.DetectedVariables.Select(v => $"{v.Name} {v.Source} {v.ReplacementToken}")),
            PanelText(view.RecorderDraftPanel),
            PanelText(view.PerceptionPanel),
            PanelText(view.HumanInterventionPanel));

    private static string PanelText(ReliableRecipeLabPanelViewModel panel) =>
        string.Join(" ", panel.Title, panel.EmptyState, panel.FooterNote, string.Join(" ", panel.Rows.Select(r => $"{r.Label} {r.Value}")));
}

file static class RecorderDraftTestExtensions
{
    public static bool TrajectoryHasUserCorrectionMarkerForTest(this RecorderToRecipeDraft draft) =>
        ReliableRecipeRecorderFixtureCatalog.Get("corrected_user_click_demonstration").Trajectory.Interactions.Any(i => i.UserCorrectionMarker);
}
