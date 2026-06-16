using System.Text.Json;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using OneBrain.BrowserExecutor.Cdp;
using OneBrain.BrowserExecutor.Contracts;

namespace OneBrain.Safety.Tests;

[TestClass]
[TestCategory("BrowserGroundingSnapshot")]
[TestCategory("GroundedElement")]
[TestCategory("GroundingSnapshotRedaction")]
[TestCategory("GroundingSnapshotFixtureHarness")]
[TestCategory("GroundingStagnationIntegration")]
[TestCategory("VisualGroundingSurface")]
[TestCategory("GroundingTimelineBinding")]
[TestCategory("RuntimeStagnationDetector")]
[TestCategory("RecoveryUX")]
[TestCategory("TimelineStepper")]
[TestCategory("SidePanelTimeline")]
[TestCategory("OperatorTimeline")]
[TestCategory("EvidenceTimeline")]
public sealed class NodalOsBrowserGroundingM169M171Tests
{
    private readonly NodalOsGroundingSnapshotBuilder builder = new();
    private readonly NodalOsGroundingSnapshotFixtureHarness harness = new();
    private readonly NodalOsGroundingSnapshotToStagnationAdapter groundingToStagnation = new();
    private readonly NodalOsRuntimeStagnationDetector stagnation = new();
    private readonly NodalOsRecoveryUxService recovery = new();
    private readonly NodalOsGroundingTimelineAdapter groundingTimeline = new();

    [TestMethod]
    public void BrowserGroundingSnapshotCanCreateRedactedSnapshot()
    {
        var snapshot = builder.Create("snapshot-ready");

        Assert.AreEqual(NodalOsGroundingRedactionStatus.RedactedSafe, snapshot.RedactionStatus);
        Assert.IsTrue(snapshot.PersistenceAllowed);
        Assert.IsTrue(snapshot.ScreenshotPersisted);
        Assert.IsTrue(snapshot.Redacted);
        Assert.IsFalse(snapshot.GrantsAuthority);
        Assert.IsFalse(string.IsNullOrWhiteSpace(snapshot.DomHash));
        Assert.IsFalse(string.IsNullOrWhiteSpace(snapshot.ScreenshotHash));
    }

    [TestMethod]
    public void FocusedElementAndVisibleInteractablesAreRedacted()
    {
        var snapshot = builder.Create(
            "snapshot-elements",
            focusedElement: NodalOsGroundingSnapshotBuilder.Element("focus", "textbox", "token=synthetic-secret", "#token", "password=abc123"),
            visibleInteractables: [
                NodalOsGroundingSnapshotBuilder.Element("safe", "button", "Open readiness", "#readiness"),
                NodalOsGroundingSnapshotBuilder.Element("sensitive", "input", "password=secret", "#password", sensitive: true)
            ]);

        Assert.IsNotNull(snapshot.FocusedElement);
        Assert.IsTrue(snapshot.FocusedElement.IsSensitive);
        Assert.AreEqual("[REDACTED]", snapshot.FocusedElement.Label);
        Assert.IsTrue(snapshot.VisibleInteractables.All(element => element.Redacted));
        Assert.IsTrue(snapshot.VisibleInteractables.Any(element => element.IsSensitive));
        Assert.AreEqual(NodalOsGroundingRedactionStatus.RedactionFailed, snapshot.RedactionStatus);
        Assert.IsFalse(snapshot.PersistenceAllowed);
    }

    [TestMethod]
    public void ScreenshotHashAndRefAreAllowedOnlyWhenRedactionPasses()
    {
        var ready = builder.Create("snapshot-shot-ready", screenshotRef: "shot-ref:redacted");
        var failed = builder.Create("snapshot-shot-failed", screenshotRef: "shot-ref-sensitive", redactionStatus: NodalOsGroundingRedactionStatus.RedactionFailed);

        Assert.IsTrue(ready.ScreenshotPersisted);
        Assert.AreEqual("shot-ref:redacted", ready.ScreenshotRef);
        Assert.IsFalse(failed.ScreenshotPersisted);
        Assert.IsNull(failed.ScreenshotRef);
        Assert.IsFalse(failed.PersistenceAllowed);
    }

    [TestMethod]
    public void SensitiveScreenshotIsNotPersistedAndBlocksEvidence()
    {
        var fixtures = harness.CreateFixtures();
        var sensitive = fixtures["sensitive-screenshot"];

        Assert.AreEqual(NodalOsGroundingRedactionStatus.BlockedSensitive, sensitive.RedactionStatus);
        Assert.IsFalse(sensitive.PersistenceAllowed);
        Assert.IsFalse(sensitive.ScreenshotPersisted);
        Assert.IsNull(sensitive.ScreenshotRef);
        Assert.AreEqual(0, sensitive.EvidenceRefs.Count);
    }

    [TestMethod]
    public void RedactionFailedBlocksPersistenceAndDoesNotLeakSecrets()
    {
        var snapshot = builder.Create(
            "snapshot-secret",
            url: "http://local/?token=synthetic-secret",
            title: "password=abc123",
            screenshotRef: "shot-ref",
            redactionStatus: NodalOsGroundingRedactionStatus.RedactedSafe);
        var json = JsonSerializer.Serialize(snapshot);

        Assert.AreEqual(NodalOsGroundingRedactionStatus.RedactionFailed, snapshot.RedactionStatus);
        Assert.IsFalse(snapshot.PersistenceAllowed);
        Assert.IsFalse(snapshot.ScreenshotPersisted);
        Assert.IsFalse(json.Contains("synthetic-secret", StringComparison.Ordinal));
        Assert.IsFalse(json.Contains("password=abc123", StringComparison.Ordinal));
    }

    [TestMethod]
    public void FixtureHarnessCreatesReadyLoadingBlockedRepeatedAndFailedSnapshots()
    {
        var fixtures = harness.CreateFixtures();

        Assert.IsTrue(fixtures.ContainsKey("ready-dom"));
        Assert.IsTrue(fixtures.ContainsKey("ready-screenshot"));
        Assert.AreEqual(NodalOsPageHealth.Loading, fixtures["loading-page"].PageHealth);
        Assert.AreEqual(NodalOsPageHealth.Blocked, fixtures["blocked-page"].PageHealth);
        Assert.AreEqual(NodalOsGroundingRedactionStatus.RedactionFailed, fixtures["redaction-failed"].RedactionStatus);
    }

    [TestMethod]
    public void RepeatedDomHashCreatesStagnationSignal()
    {
        var fixtures = harness.CreateFixtures();
        var snapshots = new[] { fixtures["repeated-dom-1"], fixtures["repeated-dom-2"], fixtures["repeated-dom-3"] };
        var signals = stagnation.Detect(groundingToStagnation.Map(snapshots), threshold: 3);

        Assert.IsTrue(signals.Any(signal => signal.Kind == NodalOsStagnationKind.RepeatedDomHash));
        Assert.IsTrue(signals.All(signal => signal.Redacted));
        Assert.IsTrue(signals.All(signal => !signal.GrantsAuthority));
    }

    [TestMethod]
    public void RepeatedScreenshotHashCreatesStagnationSignal()
    {
        var fixtures = harness.CreateFixtures();
        var snapshots = new[] { fixtures["repeated-shot-1"], fixtures["repeated-shot-2"], fixtures["repeated-shot-3"] };
        var signals = stagnation.Detect(groundingToStagnation.Map(snapshots), threshold: 3);

        Assert.IsTrue(signals.Any(signal => signal.Kind == NodalOsStagnationKind.RepeatedScreenshotHash));
    }

    [TestMethod]
    public void LoadingPageMapsToPageNotLoadedAndBlockedPageCreatesRecovery()
    {
        var fixtures = harness.CreateFixtures();
        var loadingSnapshots = Enumerable.Repeat(fixtures["loading-page"], 3).ToArray();
        var loadingSignal = stagnation.Detect(groundingToStagnation.Map(loadingSnapshots), threshold: 3)
            .Single(signal => signal.Kind == NodalOsStagnationKind.PageNotLoaded);
        var blockedSignal = stagnation.Detect(groundingToStagnation.Map(Enumerable.Repeat(fixtures["blocked-page"], 3).ToArray()), threshold: 3)
            .First(signal => signal.Kind == NodalOsStagnationKind.PageNotLoaded);
        var decision = recovery.CreateDecision(blockedSignal);

        Assert.AreEqual(NodalOsRecoveryRecommendation.Retry, loadingSignal.Recommendation);
        Assert.AreEqual(NodalOsRecoveryState.RetrySuggested, decision.State);
        Assert.IsFalse(decision.GrantsAuthority);
    }

    [TestMethod]
    public void GroundingTimelineShowsCardEvidenceAndDoesNotGrantAuthority()
    {
        var snapshot = builder.Create("snapshot-timeline");
        var timeline = groundingTimeline.Map(snapshot);
        var step = timeline.Steps.Single();

        Assert.AreEqual(NodalOsTimelineStepStatus.EvidenceReady, step.Status);
        Assert.IsTrue(step.EvidenceRefs.Any());
        Assert.IsFalse(step.GrantsAuthority);
        Assert.IsTrue(step.CoreAuthorityRequired);
        Assert.IsFalse(timeline.ProductionReady);
    }

    [TestMethod]
    public void GroundingTimelineBlocksRedactionFailure()
    {
        var snapshot = builder.Create("snapshot-blocked", redactionStatus: NodalOsGroundingRedactionStatus.RedactionFailed);
        var timeline = groundingTimeline.Map(snapshot);
        var step = timeline.Steps.Single();

        Assert.AreEqual(NodalOsTimelineStepStatus.Warning, step.Status);
        Assert.IsTrue(step.Blockers.Any());
        Assert.IsTrue(step.HumanInterventionRequired);
        Assert.IsFalse(step.GrantsAuthority);
    }

    [TestMethod]
    public void SidepanelUsesExistingTimelineForGroundingWithoutDuplicateTimeline()
    {
        var js = File.ReadAllText(SourcePath("browser-extension", "onebrain-chrome-lab", "sidepanel.js"));
        var css = File.ReadAllText(SourcePath("browser-extension", "onebrain-chrome-lab", "sidepanel.css"));
        var html = File.ReadAllText(SourcePath("browser-extension", "onebrain-chrome-lab", "sidepanel.html"));

        StringAssert.Contains(js, "case 'groundingSnapshot'");
        StringAssert.Contains(js, "buildGroundingTimeline");
        StringAssert.Contains(js, "renderGroundingCard");
        StringAssert.Contains(js, "Screenshot is never authority");
        StringAssert.Contains(css, ".timeline-grounding-card");
        Assert.AreEqual(3, CountOccurrences(html, "class=\"timeline\""));
        Assert.AreEqual(0, CountOccurrences(html, "groundingTimeline"));
    }

    [TestMethod]
    public void GroundingAdrAndRunbookExist()
    {
        var adr = File.ReadAllText(SourcePath("docs", "adr", "dom-screenshot-grounding-snapshot-m169-m171.md"));
        var runbook = File.ReadAllText(SourcePath("docs", "runbooks", "nodal-os-internal-preview-operator-ux-guide-m148-m150.md"));

        StringAssert.Contains(adr, "screenshot is evidence/debug");
        StringAssert.Contains(adr, "Core remains authoritative");
        StringAssert.Contains(adr, "No scope expansion");
        StringAssert.Contains(runbook, "Reading Grounding Snapshot");
        StringAssert.Contains(runbook, "redaction failed");
    }

    private static int CountOccurrences(string text, string value)
    {
        var count = 0;
        var index = 0;
        while ((index = text.IndexOf(value, index, StringComparison.Ordinal)) >= 0)
        {
            count++;
            index += value.Length;
        }

        return count;
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
