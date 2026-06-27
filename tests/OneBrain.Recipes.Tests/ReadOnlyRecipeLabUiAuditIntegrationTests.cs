using Microsoft.VisualStudio.TestTools.UnitTesting;
using OneBrain.Core.Recipes;

namespace OneBrain.Recipes.Tests;

[TestClass]
[TestCategory("ReadOnlyRecipeLabUiAuditIntegration")]
public sealed class ReadOnlyRecipeLabUiAuditIntegrationTests
{
    [TestMethod]
    public void ReadOnlyRecipeLabSurfaceCreated()
    {
        var surface = Surface();

        Assert.AreEqual("reliable-recipe-lab.audit-surface.m13", surface.SurfaceId);
        Assert.IsTrue(surface.ReadOnly);
        Assert.IsTrue(surface.FixtureOnly);
        Assert.IsFalse(surface.RuntimeEnabled);
    }

    [TestMethod]
    public void HeaderIncludesRuntimeNotEnabled()
    {
        var header = Surface().Header;

        Assert.AreEqual("Recipe Lab", header.Title);
        Assert.IsTrue(header.Badges.Any(b => b.Label == "Runtime not enabled"));
        Assert.IsTrue(header.OperatorSummary.Contains("review", StringComparison.OrdinalIgnoreCase));
    }

    [TestMethod]
    public void HeaderIncludesExternalAuditRequired()
    {
        var header = Surface().Header;

        Assert.IsTrue(header.Badges.Any(b => b.Label == "External audit required"));
        Assert.IsTrue(Copy(Surface()).Contains("External audit is required before any runtime or adapter work.", StringComparison.Ordinal));
    }

    [TestMethod]
    public void OverallStatusShowsRuntimeAutonomyZero()
    {
        var status = Surface().StatusStrip;

        Assert.AreEqual(0, status.RuntimeAutonomyPercent);
        Assert.IsTrue(status.RuntimeAutonomyLabel.Contains("0%", StringComparison.Ordinal));
        Assert.AreEqual(100, status.ProductSurfaceReadinessPercent);
    }

    [TestMethod]
    public void QualityPreflightSectionVisible()
    {
        AssertSection(ReliableRecipeLabAuditSectionKind.QualityPreflight);
    }

    [TestMethod]
    public void EvidenceValidationSectionVisible()
    {
        AssertSection(ReliableRecipeLabAuditSectionKind.EvidenceValidation);
    }

    [TestMethod]
    public void RecorderDraftSectionVisible()
    {
        AssertSection(ReliableRecipeLabAuditSectionKind.RecorderDraft);
    }

    [TestMethod]
    public void EvalHarnessSectionVisible()
    {
        AssertSection(ReliableRecipeLabAuditSectionKind.EvalHarness);
    }

    [TestMethod]
    public void SandboxReadinessSectionVisible()
    {
        AssertSection(ReliableRecipeLabAuditSectionKind.SandboxReadiness);
    }

    [TestMethod]
    public void PerceptionSectionVisible()
    {
        AssertSection(ReliableRecipeLabAuditSectionKind.Perception);
    }

    [TestMethod]
    public void AdapterReadinessSectionVisible()
    {
        AssertSection(ReliableRecipeLabAuditSectionKind.AdapterReadiness);
    }

    [TestMethod]
    public void StructuredPrerequisiteAuthoringSectionVisible()
    {
        AssertSection(ReliableRecipeLabAuditSectionKind.StructuredPrerequisiteAuthoring);
    }

    [TestMethod]
    public void OperatorReviewPackSectionVisible()
    {
        AssertSection(ReliableRecipeLabAuditSectionKind.OperatorReviewPack);
    }

    [TestMethod]
    public void CloseoutAuditSectionVisible()
    {
        AssertSection(ReliableRecipeLabAuditSectionKind.CloseoutAudit);
    }

    [TestMethod]
    public void TimelinePreviewIncludesM1M12()
    {
        var surface = Surface();

        Assert.AreEqual(11, surface.Timeline.Count);
        CollectionAssert.AreEqual(Enumerable.Range(1, 11).Select(i => $"M{i}").ToArray(), surface.Timeline.Select(m => m.BlockId).ToArray());
        AssertSection(ReliableRecipeLabAuditSectionKind.TimelinePreview);
    }

    [TestMethod]
    public void OcrSupportingSignalCopyPresent()
    {
        var copy = Copy(Surface());

        Assert.IsTrue(copy.Contains("OCR is a supporting signal, not action authority.", StringComparison.Ordinal));
    }

    [TestMethod]
    public void OperatorSignoffCannotApproveRuntimeCopyPresent()
    {
        var copy = Copy(Surface());

        Assert.IsTrue(copy.Contains("Operator signoff cannot approve runtime.", StringComparison.Ordinal));
    }

    [TestMethod]
    public void NoForbiddenLiveActionLabels()
    {
        var surface = Surface();
        var forbidden = new[]
        {
            "Run",
            "Execute",
            "Start adapter",
            "Launch browser",
            "Connect CDP",
            "Replay",
            "Record live",
            "Capture screen",
            "Enable runtime",
            "Approve runtime",
            "Production ready",
            "Automation ready",
            "Live validated"
        };

        foreach (var label in surface.ReadOnlyActionLabels)
        {
            foreach (var forbiddenLabel in forbidden)
                Assert.IsFalse(label.Contains(forbiddenLabel, StringComparison.OrdinalIgnoreCase), label);
        }
    }

    [TestMethod]
    public void NoRuntimeActionHandlersExposed()
    {
        var surface = Surface();

        Assert.IsFalse(surface.RuntimeActionExposed);
        Assert.IsFalse(surface.AdapterRuntimeEnabled);
        Assert.IsFalse(surface.CallsProviderOrNetwork);
        Assert.IsFalse(surface.UsesLiveBrowser);
        Assert.IsFalse(surface.UsesLiveDesktop);
        Assert.IsFalse(surface.ActivatesOcrRuntime);
        Assert.IsFalse(surface.CapturesScreenshot);
        Assert.IsFalse(surface.StartsRecorder);
        Assert.IsFalse(surface.StartsSandbox);
    }

    [TestMethod]
    public void PresenterDeterministic()
    {
        var first = Surface();
        var second = Surface();

        CollectionAssert.AreEqual(first.Sections.Select(s => s.Kind).ToArray(), second.Sections.Select(s => s.Kind).ToArray());
        CollectionAssert.AreEqual(first.Timeline.Select(t => t.BlockId).ToArray(), second.Timeline.Select(t => t.BlockId).ToArray());
        CollectionAssert.AreEqual(first.ReadOnlyActionLabels.ToArray(), second.ReadOnlyActionLabels.ToArray());
        Assert.AreEqual(first.NoRuntimeNotice, second.NoRuntimeNotice);
    }

    [TestMethod]
    public void RequiredProductCopyPresent()
    {
        var copy = Copy(Surface());

        Assert.IsTrue(copy.Contains("Fixture-ready does not mean runtime-ready.", StringComparison.Ordinal));
        Assert.IsTrue(copy.Contains("External audit is required before any runtime or adapter work.", StringComparison.Ordinal));
        Assert.IsTrue(copy.Contains("OCR is a supporting signal, not action authority.", StringComparison.Ordinal));
        Assert.IsTrue(copy.Contains("Operator signoff cannot approve runtime.", StringComparison.Ordinal));
        Assert.IsTrue(copy.Contains("No live browser, desktop, recorder, OCR or sandbox is enabled.", StringComparison.Ordinal));
    }

    [TestMethod]
    public void SuccessCopyDoesNotImplyRuntime()
    {
        var successSections = Surface().Sections.Where(s => s.Tone == ReliableRecipeLabAuditTone.Success).ToArray();

        foreach (var section in successSections)
        {
            var copy = SectionCopy(section);
            Assert.IsFalse(copy.Contains("ready for runtime", StringComparison.OrdinalIgnoreCase), section.Title);
            Assert.IsFalse(copy.Contains("can execute", StringComparison.OrdinalIgnoreCase), section.Title);
            Assert.IsFalse(copy.Contains("live validated", StringComparison.OrdinalIgnoreCase), section.Title);
        }
    }

    [TestMethod]
    public void NoProviderBrowserDesktopRuntimeDependencyAdded()
    {
        var surface = Surface();

        Assert.IsFalse(surface.CallsProviderOrNetwork);
        Assert.IsFalse(surface.UsesLiveBrowser);
        Assert.IsFalse(surface.UsesLiveDesktop);
        Assert.IsFalse(surface.RuntimeEnabled);
    }

    [TestMethod]
    public void NoOcrInternalsTouched()
    {
        var surface = Surface();

        Assert.IsFalse(surface.ActivatesOcrRuntime);
        Assert.IsTrue(Copy(surface).Contains("OCR", StringComparison.Ordinal));
    }

    [TestMethod]
    public void NoPerceptionRuntimeAdded()
    {
        var surface = Surface();

        Assert.IsFalse(Copy(surface).Contains("live perception active", StringComparison.OrdinalIgnoreCase));
        Assert.IsTrue(surface.Sections.Any(s => s.Kind == ReliableRecipeLabAuditSectionKind.Perception));
    }

    [TestMethod]
    public void NoRecorderRuntimeAdded()
    {
        var surface = Surface();

        Assert.IsFalse(surface.StartsRecorder);
        Assert.IsFalse(Copy(surface).Contains("record now", StringComparison.OrdinalIgnoreCase));
    }

    [TestMethod]
    public void NoSandboxRuntimeAdded()
    {
        var surface = Surface();

        Assert.IsFalse(surface.StartsSandbox);
        Assert.IsFalse(Copy(surface).Contains("launch sandbox", StringComparison.OrdinalIgnoreCase));
    }

    [TestMethod]
    public void NoAdapterRuntimeAdded()
    {
        var surface = Surface();

        Assert.IsFalse(surface.AdapterRuntimeEnabled);
        Assert.IsFalse(Copy(surface).Contains("adapter enabled", StringComparison.OrdinalIgnoreCase));
    }

    [TestMethod]
    public void NoLiveNoActionInvariants()
    {
        var copy = Copy(Surface());

        Assert.IsFalse(copy.Contains("Production ready", StringComparison.OrdinalIgnoreCase));
        Assert.IsFalse(copy.Contains("Automation ready", StringComparison.OrdinalIgnoreCase));
        Assert.IsFalse(copy.Contains("Live validated", StringComparison.OrdinalIgnoreCase));
        Assert.IsFalse(copy.Contains("Approved to run", StringComparison.OrdinalIgnoreCase));
    }

    private static ReliableRecipeLabAuditSurfaceViewModel Surface() =>
        ReliableRecipeLabAuditSurfacePresenter.CreateDefault();

    private static void AssertSection(ReliableRecipeLabAuditSectionKind kind)
    {
        var section = Surface().Sections.SingleOrDefault(s => s.Kind == kind);
        Assert.IsNotNull(section, kind.ToString());
        Assert.IsFalse(string.IsNullOrWhiteSpace(section.Title), kind.ToString());
        Assert.IsFalse(string.IsNullOrWhiteSpace(section.Summary), kind.ToString());
    }

    private static string Copy(ReliableRecipeLabAuditSurfaceViewModel surface) =>
        string.Join(" ",
            surface.SurfaceId,
            surface.NoRuntimeNotice,
            surface.Header.Title,
            surface.Header.Subtitle,
            surface.Header.OperatorSummary,
            string.Join(" ", surface.Header.Badges.Select(b => $"{b.Label} {b.Description}")),
            surface.StatusStrip.RuntimeAutonomyLabel,
            surface.StatusStrip.CloseoutDecisionLabel,
            string.Join(" ", surface.ReadOnlyActionLabels),
            string.Join(" ", surface.Sections.Select(SectionCopy)),
            string.Join(" ", surface.Timeline.Select(t => $"{t.BlockId} {t.Decision} {t.Label} {t.Summary}")),
            surface.ExternalAuditHandoff.Title,
            surface.ExternalAuditHandoff.Summary,
            surface.ExternalAuditHandoff.RuntimeProhibitedStatement,
            string.Join(" ", surface.ExternalAuditHandoff.AuditQuestions),
            string.Join(" ", surface.ExternalAuditHandoff.DecisionLabels),
            surface.DesignSystem.Theme,
            surface.DesignSystem.Layout,
            string.Join(" ", surface.DesignSystem.VisualRules));

    private static string SectionCopy(ReliableRecipeLabAuditSurfaceSection section) =>
        string.Join(" ",
            section.Title,
            section.Eyebrow,
            section.Summary,
            section.FooterNote,
            string.Join(" ", section.Metrics.Select(m => $"{m.Label} {m.Value}")),
            string.Join(" ", section.KeyRows),
            string.Join(" ", section.RequiredCopy));
}
