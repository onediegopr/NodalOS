using System.Text.Json;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using OneBrain.WindowsComputerUse;

namespace OneBrain.Safety.Tests;

[TestClass]
[TestCategory("WindowsComputerUseFixtureSafe")]
public sealed class WindowsComputerUseFixtureSafeFoundationTests
{
    [TestMethod]
    public void FixtureBuilder_ConstructsNotepadSnapshotWithoutLiveFlags()
    {
        var snapshot = FixtureComputerUseSnapshotBuilder.NotepadLikeUiaRichApp();

        Assert.AreEqual(ComputerUseSnapshotSource.Fixture, snapshot.Source);
        Assert.IsFalse(snapshot.RealMouseUsed);
        Assert.IsFalse(snapshot.RealKeyboardUsed);
        Assert.IsFalse(snapshot.LiveUiaActionUsed);
        Assert.IsFalse(snapshot.ClipboardCaptured);
        Assert.IsFalse(snapshot.ScreenshotPersisted);
        Assert.IsTrue(snapshot.Windows.Any());
    }

    [TestMethod]
    public void Classifier_DetectsUiaRichCalculator()
    {
        var snapshot = FixtureComputerUseSnapshotBuilder.CalculatorLikeUiaRichApp();
        var classification = new ComputerUseCapabilityClassifier().Classify(snapshot);

        Assert.AreEqual(WindowTechnologyKind.UiaRich, classification.TechnologyKind);
        Assert.IsTrue(classification.Confidence > 0.8);
        Assert.IsFalse(classification.RequiresHumanHandoff);
    }

    [TestMethod]
    public void Classifier_DetectsVisualOnlyCanvasAsHandoff()
    {
        var snapshot = FixtureComputerUseSnapshotBuilder.CustomCanvasVisualOnlyApp();
        var classification = new ComputerUseCapabilityClassifier().Classify(snapshot);

        Assert.AreEqual(WindowTechnologyKind.VisualOnly, classification.TechnologyKind);
        Assert.IsTrue(classification.RequiresVisualFallback);
        Assert.IsTrue(classification.RequiresHumanHandoff);
    }

    [TestMethod]
    public void BlockageDetector_BlocksUacAdminAndModal()
    {
        var uac = FixtureComputerUseSnapshotBuilder.UacAdminBlocker();
        var modal = FixtureComputerUseSnapshotBuilder.ModalDialogBlocker();
        var detector = new ComputerUseBlockageDetector();

        var uacBlockages = detector.Detect(uac);
        var modalBlockages = detector.Detect(modal);

        Assert.IsTrue(uacBlockages.Any(b => b.Kind == ComputerUseBlockageKind.UacAdmin && b.RequiresHumanHandoff));
        Assert.IsTrue(modalBlockages.Any(b => b.Kind == ComputerUseBlockageKind.HiddenWindowOrModal && b.RequiresHumanHandoff));
        Assert.IsTrue(modalBlockages.Any(b => b.Kind == ComputerUseBlockageKind.DestructiveAction && b.RequiresHumanHandoff));
    }

    [TestMethod]
    public void SensitiveSurfaceDetector_FindsPasswordCredentialField()
    {
        var snapshot = FixtureComputerUseSnapshotBuilder.PasswordLoginForm();
        var surfaces = new ComputerUseSensitiveSurfaceDetector().Detect(snapshot);

        Assert.IsTrue(surfaces.Any(s => s.RequiresHumanHandoff));
        Assert.IsTrue(surfaces.Any(s => s.Category.Contains("password", StringComparison.OrdinalIgnoreCase) || s.Category.Contains("credential", StringComparison.OrdinalIgnoreCase)));
    }

    [TestMethod]
    public void LocatorEngine_PrefersAutomationIdAndFlagsLowConfidenceFallbacks()
    {
        var calculator = FixtureComputerUseSnapshotBuilder.CalculatorLikeUiaRichApp();
        var canvas = FixtureComputerUseSnapshotBuilder.CustomCanvasVisualOnlyApp();
        var engine = new ComputerUseLocatorEngine();

        var calculatorBest = engine.GenerateCandidates(calculator, "One").First();
        var canvasBest = engine.GenerateCandidates(canvas, "Draw Surface").First();

        Assert.AreEqual("AutomationId+Process+ControlType", calculatorBest.SelectorKind);
        Assert.IsTrue(calculatorBest.Confidence > 0.6);
        Assert.IsFalse(calculatorBest.RequiresHumanHandoff);
        Assert.IsTrue(canvasBest.RequiresVisualFallback);
        Assert.IsTrue(canvasBest.RequiresHumanHandoff);
    }

    [TestMethod]
    public void SafeActionPlanner_DryRunOnlyForAllowlistedUiaRichFixture()
    {
        var snapshot = FixtureComputerUseSnapshotBuilder.CalculatorLikeUiaRichApp();
        var decision = new ComputerUseSafeActionPlanner().Plan(snapshot, "One", ComputerUseActionKind.Invoke);
        var action = decision.Candidates.Single();

        Assert.IsTrue(decision.AllowedToPlan);
        Assert.IsFalse(decision.AllowedToExecuteLive);
        Assert.IsTrue(decision.FixtureOnly);
        Assert.AreEqual(ComputerUseActionKind.Invoke, action.ActionKind);
        Assert.IsTrue(action.DryRunOnly);
        Assert.IsFalse(action.RequiresHumanHandoff);
    }

    [TestMethod]
    public void SafeActionPlanner_BlocksCredentialsUacDestructiveAndVisualOnly()
    {
        var planner = new ComputerUseSafeActionPlanner();

        var login = planner.Plan(FixtureComputerUseSnapshotBuilder.PasswordLoginForm(), "type password", ComputerUseActionKind.SetValue);
        var uac = planner.Plan(FixtureComputerUseSnapshotBuilder.UacAdminBlocker(), "click Yes", ComputerUseActionKind.Click);
        var modal = planner.Plan(FixtureComputerUseSnapshotBuilder.ModalDialogBlocker(), "overwrite file", ComputerUseActionKind.Click);
        var visual = planner.Plan(FixtureComputerUseSnapshotBuilder.CustomCanvasVisualOnlyApp(), "click draw surface", ComputerUseActionKind.Click);

        AssertHandoff(login, ComputerUseHandoffReason.SensitiveSurface);
        AssertHandoff(uac, ComputerUseHandoffReason.UacAdmin);
        AssertHandoff(modal, ComputerUseHandoffReason.DestructiveAction);
        AssertHandoff(visual, ComputerUseHandoffReason.VisualOnlyTarget);
    }

    [TestMethod]
    public void SafeActionPlanner_RejectsNonFixtureSnapshot()
    {
        var snapshot = FixtureComputerUseSnapshotBuilder.CalculatorLikeUiaRichApp() with
        {
            Source = ComputerUseSnapshotSource.LiveDisabled
        };

        var decision = new ComputerUseSafeActionPlanner().Plan(snapshot, "One", ComputerUseActionKind.Invoke);

        Assert.IsFalse(decision.AllowedToPlan);
        Assert.IsFalse(decision.AllowedToExecuteLive);
        AssertHandoff(decision, ComputerUseHandoffReason.LiveExecutionDisabled);
    }

    [TestMethod]
    public void EvidencePackBuilder_CreatesObserveAndHandoffEvidenceWithoutLiveFlags()
    {
        var snapshot = FixtureComputerUseSnapshotBuilder.PasswordLoginForm();
        var decision = new ComputerUseSafeActionPlanner().Plan(snapshot, "type password", ComputerUseActionKind.SetValue);
        var pack = new ComputerUseEvidencePackBuilder().Build(ComputerUseEvidenceKind.HumanHandoff, snapshot, decision, decision.Candidates.Single());

        Assert.AreEqual(ComputerUseEvidenceKind.HumanHandoff, pack.EvidenceKind);
        Assert.IsFalse(pack.LiveExecutionEnabled);
        Assert.IsFalse(pack.RealMouseUsed);
        Assert.IsFalse(pack.RealKeyboardUsed);
        Assert.IsFalse(pack.LiveUiaActionUsed);
        Assert.IsTrue(pack.SensitiveSurfaces.Any());
        Assert.IsTrue(pack.EvidenceRefs.All(r => r.Redacted));
    }

    [TestMethod]
    public void EvidenceRedactor_RedactsAdversarialSecrets()
    {
        const string secretSummary = "password=hunter2 token=sk-testSecret999 email=user@example.com card=4111 1111 1111 1111 ssn=123-45-6789 jwt=eyJabc.eyJdef.sig";

        var result = new ComputerUseEvidenceRedactor().Redact(secretSummary);

        Assert.AreEqual(ComputerUseRedactionStatus.Partial, result.Status);
        Assert.IsFalse(result.Value.Contains("hunter2", StringComparison.Ordinal));
        Assert.IsFalse(result.Value.Contains("sk-testSecret999", StringComparison.Ordinal));
        Assert.IsFalse(result.Value.Contains("user@example.com", StringComparison.Ordinal));
        Assert.IsFalse(result.Value.Contains("4111 1111 1111 1111", StringComparison.Ordinal));
        Assert.IsFalse(result.Value.Contains("123-45-6789", StringComparison.Ordinal));
        Assert.IsFalse(result.Value.Contains("eyJabc.eyJdef.sig", StringComparison.Ordinal));
        Assert.IsTrue(result.SensitiveFieldsRedacted.Count >= 5);
    }

    [TestMethod]
    public void EvidencePack_SerializesWithoutReintroducingSecrets()
    {
        var snapshot = FixtureComputerUseSnapshotBuilder.NotepadLikeUiaRichApp();
        const string summary = "clipboard token=ghp_fakeSecretToken999 password=hidden";
        var builder = new ComputerUseEvidencePackBuilder();

        var pack = builder.Build(ComputerUseEvidenceKind.ObserveOnlySnapshot, snapshot, summary: summary);
        var json = builder.Serialize(pack);
        var roundTrip = JsonSerializer.Deserialize<ComputerUseEvidencePack>(json);

        Assert.IsNotNull(roundTrip);
        Assert.AreEqual(ComputerUseRedactionStatus.Partial, roundTrip.RedactionStatus);
        Assert.IsFalse(json.Contains("ghp_fakeSecretToken999", StringComparison.Ordinal));
        Assert.IsFalse(json.Contains("hidden", StringComparison.Ordinal));
        Assert.IsTrue(json.Contains("[REDACTED]", StringComparison.Ordinal));
    }

    private static void AssertHandoff(ComputerUsePolicyDecision decision, ComputerUseHandoffReason expectedReason)
    {
        Assert.IsFalse(decision.AllowedToPlan);
        Assert.IsFalse(decision.AllowedToExecuteLive);
        Assert.IsTrue(decision.Candidates.Single().RequiresHumanHandoff);
        CollectionAssert.Contains(decision.Candidates.Single().HandoffReasons.ToList(), expectedReason);
    }
}
