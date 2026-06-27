using Microsoft.VisualStudio.TestTools.UnitTesting;
using OneBrain.Core.Recipes;

namespace OneBrain.Recipes.Tests;

[TestClass]
[TestCategory("ReliableRecipeLabReadOnlySurface")]
public sealed class ReliableRecipeLabReadOnlySurfaceTests
{
    [TestMethod]
    public void MapperConvertsSafeDryRunPreflightToDryRunCandidateViewModel()
    {
        var fixture = ReliableRecipeLabFixtureCatalog.Get("safe_invoice_download_quality_pass");

        Assert.AreEqual("Dry-run candidate", fixture.ViewModel.ReadinessLabel);
        Assert.AreEqual("Dry-run candidate", fixture.ViewModel.ModeAllowedLabel);
        Assert.AreEqual("success", fixture.ViewModel.StatusTone);
        Assert.IsTrue(fixture.ViewModel.OverallScore >= 0.72);
    }

    [TestMethod]
    public void MapperConvertsBlockedPreflightToBlockedViewModel()
    {
        var fixture = ReliableRecipeLabFixtureCatalog.Get("submit_without_validation_quality_blocked");

        Assert.AreEqual("Blocked by policy", fixture.ViewModel.ReadinessLabel);
        Assert.AreEqual("danger", fixture.ViewModel.StatusTone);
        Assert.IsTrue(fixture.ViewModel.BlockingFindings.Count > 0);
    }

    [TestMethod]
    public void ViewModelAlwaysIncludesNoLiveRuntimeNotice()
    {
        foreach (var fixture in ReliableRecipeLabFixtureCatalog.All())
        {
            Assert.AreEqual("Runtime not enabled", fixture.ViewModel.NoLiveRuntimeNotice.Title, fixture.FixtureId);
            CollectionAssert.Contains(fixture.ViewModel.NoLiveRuntimeNotice.BlockedCapabilities.ToList(), "Browser runtime", fixture.FixtureId);
            CollectionAssert.Contains(fixture.ViewModel.NoLiveRuntimeNotice.BlockedCapabilities.ToList(), "CDP runtime", fixture.FixtureId);
            CollectionAssert.Contains(fixture.ViewModel.NoLiveRuntimeNotice.BlockedCapabilities.ToList(), "OCR runtime activation", fixture.FixtureId);
            Assert.IsFalse(fixture.ViewModel.CanEnableLiveRuntime, fixture.FixtureId);
            Assert.IsFalse(fixture.ViewModel.CanStartRecipeRun, fixture.FixtureId);
        }
    }

    [TestMethod]
    public void ViewModelContainsNoForbiddenLiveActionLabels()
    {
        var forbidden = new[] { "Run", "Execute " + "live", "Start " + "automation", "Bypass", "Solver", "Autonomous " + "mode ready", "Production " + "ready" };

        foreach (var fixture in ReliableRecipeLabFixtureCatalog.All())
        {
            foreach (var label in fixture.ViewModel.ReadOnlyActionLabels)
            {
                Assert.IsFalse(forbidden.Any(f => label.Contains(f, StringComparison.OrdinalIgnoreCase)), $"{fixture.FixtureId}:{label}");
            }
        }
    }

    [TestMethod]
    public void MissingValidationAppearsInValidationPanel()
    {
        var view = ReliableRecipeLabFixtureCatalog.Get("submit_without_validation_quality_blocked").ViewModel;

        Assert.AreEqual("danger", view.ValidationPanel.Tone);
        Assert.IsTrue(view.ValidationPanel.Rows.Any(r => r.Value.Contains("Missing", StringComparison.OrdinalIgnoreCase)));
    }

    [TestMethod]
    public void MissingEvidenceAppearsInEvidencePanel()
    {
        var view = ReliableRecipeLabFixtureCatalog.Get("external_side_effect_needs_approval").ViewModel;

        Assert.AreEqual("danger", view.EvidencePanel.Tone);
        Assert.IsTrue(view.EvidencePanel.Rows.Any(r => r.Value.Contains("Missing", StringComparison.OrdinalIgnoreCase)));
    }

    [TestMethod]
    public void OcrOnlySensitiveTargetAppearsInPerceptionPanelAsBlockedSupportingOnly()
    {
        var view = ReliableRecipeLabFixtureCatalog.Get("ocr_only_sensitive_submit_blocked").ViewModel;

        Assert.AreEqual("danger", view.PerceptionPanel.Tone);
        Assert.IsTrue(view.PerceptionPanel.Rows.Any(r => r.Value.Contains("OCR signal is supporting evidence only and cannot authorize sensitive actions.", StringComparison.Ordinal)));
        Assert.IsFalse(view.CanActivateOcrRuntime);
    }

    [TestMethod]
    public void CaptchaTwoFactorAppearsAsHumanHandoff()
    {
        var view = ReliableRecipeLabFixtureCatalog.Get("captcha_handoff_quality_review").ViewModel;

        Assert.IsTrue(view.HumanInterventionPanel.Rows.Any(r => r.Value.Contains("Human handoff required", StringComparison.Ordinal)));
        Assert.IsTrue(view.HumanInterventionPanel.Rows.Any(r => r.Value.Contains("No bypass or solver", StringComparison.Ordinal)));
    }

    [TestMethod]
    public void RecorderDraftAppearsAsDraftOnlyNeedsReview()
    {
        var view = ReliableRecipeLabFixtureCatalog.Get("recorder_draft_quality_draft_only").ViewModel;

        Assert.AreEqual("Draft only", view.ModeAllowedLabel);
        Assert.AreEqual("warning", view.RecorderDraftPanel.Tone);
        Assert.IsTrue(view.RecorderDraftPanel.Rows.Any(r => r.Value.Contains("Draft only until reviewed and validated.", StringComparison.Ordinal)));
    }

    [TestMethod]
    public void HighQualityHighRiskStillBlockedOrWarned()
    {
        var view = ReliableRecipeLabFixtureCatalog.Get("high_quality_high_risk_blocked").ViewModel;

        Assert.AreEqual("danger", view.RiskPanel.Tone);
        Assert.IsTrue(view.RiskPanel.Rows.Any(r => r.Value.Contains("financial-action", StringComparison.OrdinalIgnoreCase)));
    }

    [TestMethod]
    public void SandboxDesktopRuntimeBlockedAppearsInSandboxPanel()
    {
        var view = ReliableRecipeLabFixtureCatalog.Get("desktop_live_sandbox_blocked").ViewModel;

        Assert.AreEqual("danger", view.SandboxPanel.Tone);
        Assert.IsTrue(view.SandboxPanel.Rows.Any(r => r.Value.Contains("desktop-live-surface", StringComparison.Ordinal)));
    }

    [TestMethod]
    public void FixtureCatalogReturnsDeterministicStableIds()
    {
        var first = ReliableRecipeLabFixtureCatalog.All();
        var second = ReliableRecipeLabFixtureCatalog.All();

        CollectionAssert.AreEqual(first.Select(f => f.FixtureId).ToArray(), second.Select(f => f.FixtureId).ToArray());
        CollectionAssert.Contains(first.Select(f => f.FixtureId).ToList(), "safe_invoice_download_quality_pass");
        CollectionAssert.Contains(first.Select(f => f.FixtureId).ToList(), "high_quality_high_risk_blocked");
    }

    [TestMethod]
    public void FixtureCatalogContainsNoSecretsOrLiveUrls()
    {
        foreach (var fixture in ReliableRecipeLabFixtureCatalog.All())
        {
            var text = string.Join(" ", fixture.Recipe.Id, fixture.Recipe.Name, string.Join(" ", fixture.Recipe.Variables.Select(v => v.ValueRef)), string.Join(" ", fixture.Recipe.Blocks.SelectMany(b => b.Config.Values)), string.Join(" ", fixture.ViewModel.TimelinePreview.Select(t => t.EvidenceExpectationSummary)));
            Assert.IsFalse(text.Contains("http://", StringComparison.OrdinalIgnoreCase), fixture.FixtureId);
            Assert.IsFalse(text.Contains("https://", StringComparison.OrdinalIgnoreCase), fixture.FixtureId);
            Assert.IsFalse(text.Contains("password" + "=", StringComparison.OrdinalIgnoreCase), fixture.FixtureId);
            Assert.IsFalse(text.Contains("authorization:", StringComparison.OrdinalIgnoreCase), fixture.FixtureId);
        }
    }

    [TestMethod]
    public void CategoryScoreCardsMatchQualityCategories()
    {
        var view = ReliableRecipeLabFixtureCatalog.Get("safe_invoice_download_quality_pass").ViewModel;

        foreach (var category in Enum.GetValues<ReliableRecipeQualityCategory>())
        {
            Assert.IsTrue(view.CategoryCards.Any(c => c.Category == category), category.ToString());
        }
    }

    [TestMethod]
    public void TimelinePreviewIncludesValidationAndEvidenceExpectationSummaries()
    {
        var view = ReliableRecipeLabFixtureCatalog.Get("safe_invoice_download_quality_pass").ViewModel;
        var item = view.TimelinePreview.Single(i => i.StepId == "download");

        StringAssert.Contains(item.EvidenceExpectationSummary, "evidence.before");
        StringAssert.Contains(item.ValidationExpectationSummary, "validation.download");
        Assert.AreEqual("Fixture preview", item.Status);
    }

    [TestMethod]
    public void ProductFacingLabelsDoNotClaimLiveReadiness()
    {
        var forbidden = new[] { "Autonomous " + "execution ready", "Safe to run " + "live", "Guaranteed", "Fully automated", "Bypasses CAPTCHA", "Solves login", "Executes for you", "Production-ready " + "automation" };

        foreach (var fixture in ReliableRecipeLabFixtureCatalog.All())
        {
            var labels = ProductFacingText(fixture.ViewModel);
            foreach (var forbiddenText in forbidden)
            {
                Assert.IsFalse(labels.Contains(forbiddenText, StringComparison.OrdinalIgnoreCase), $"{fixture.FixtureId}:{forbiddenText}");
            }
        }
    }

    [TestMethod]
    public void CopyAndReportSummaryIsDeterministic()
    {
        var first = ReliableRecipeLabFixtureCatalog.Get("safe_invoice_download_quality_pass").ViewModel;
        var second = ReliableRecipeLabFixtureCatalog.Get("safe_invoice_download_quality_pass").ViewModel;

        Assert.AreEqual(first.Summary, second.Summary);
        Assert.AreEqual(first.DecisionLabel, second.DecisionLabel);
        Assert.AreEqual(first.NoLiveRuntimeNotice.Message, second.NoLiveRuntimeNotice.Message);
    }

    [TestMethod]
    public void NoProviderBrowserDesktopRuntimeDependencyAdded()
    {
        foreach (var fixture in ReliableRecipeLabFixtureCatalog.All())
        {
            var view = fixture.ViewModel;
            Assert.IsFalse(view.CanCallProviderOrNetwork, fixture.FixtureId);
            Assert.IsFalse(view.CanEnableBrowserRuntime, fixture.FixtureId);
            Assert.IsFalse(view.CanEnableDesktopRuntime, fixture.FixtureId);
            Assert.IsFalse(view.CanReadSecrets, fixture.FixtureId);
            Assert.IsFalse(view.CanCaptureScreenshot, fixture.FixtureId);
        }
    }

    private static string ProductFacingText(ReliableRecipeLabViewModel view) =>
        string.Join(" ",
            view.ReadinessLabel,
            view.ModeRequestedLabel,
            view.ModeAllowedLabel,
            view.DecisionLabel,
            view.Summary,
            string.Join(" ", view.CategoryCards.SelectMany(c => new[] { c.Label, c.Summary })),
            string.Join(" ", view.BlockingFindings.Select(f => $"{f.Title} {f.Message} {f.RecommendedFix}")),
            string.Join(" ", view.Warnings.Select(f => $"{f.Title} {f.Message} {f.RecommendedFix}")),
            string.Join(" ", view.RecommendedFixes),
            string.Join(" ", view.ReadOnlyActionLabels),
            view.NoLiveRuntimeNotice.Title,
            view.NoLiveRuntimeNotice.Message,
            view.NoLiveRuntimeNotice.AllowedMode,
            string.Join(" ", view.NoLiveRuntimeNotice.BlockedCapabilities),
            PanelText(view.EvidencePanel),
            PanelText(view.ValidationPanel),
            PanelText(view.TargetResolutionPanel),
            PanelText(view.RiskPanel),
            PanelText(view.SandboxPanel),
            PanelText(view.HumanInterventionPanel),
            PanelText(view.PerceptionPanel),
            PanelText(view.RecorderDraftPanel));

    private static string PanelText(ReliableRecipeLabPanelViewModel panel) =>
        string.Join(" ", panel.Title, panel.EmptyState, panel.FooterNote, string.Join(" ", panel.Rows.Select(r => $"{r.Label} {r.Value}")));
}
