using Microsoft.VisualStudio.TestTools.UnitTesting;
using OneBrain.BrowserExecutor.Cdp;
using OneBrain.BrowserExecutor.Contracts;

namespace OneBrain.Safety.Tests;

[TestClass]
public sealed class BrowserTargetFrameManagerTests
{
    [TestMethod]
    public void BrowserTargetRegistryRegistersCreatedTarget()
    {
        var registry = new BrowserTargetRegistry();
        var target = registry.UpsertTarget("target-1", new Uri("https://example.com/"), "Example", BrowserTargetState.Alive, tabId: "tab-1");

        Assert.AreEqual("target-1", target.TargetId);
        Assert.AreEqual("tab-1", target.TabId);
        Assert.AreEqual(BrowserTargetState.Alive, target.State);
        Assert.IsTrue(registry.Events.Any(evt => evt.EventType == BrowserTargetEventType.TargetCreated));
    }

    [TestMethod]
    public void BrowserTargetGenerationChangesOnNavigationAndRedirect()
    {
        var registry = new BrowserTargetRegistry();
        var target = registry.UpsertTarget("target-nav", new Uri("https://example.com/start"), "Start", BrowserTargetState.Alive);

        var committed = registry.ApplyNavigation(target.TargetId, new Uri("https://example.com/next"), "Next");
        var redirected = registry.ApplyNavigation(target.TargetId, new Uri("https://example.com/final"), "Final", BrowserTargetEventType.NavigationFinished);

        Assert.AreEqual(target.Generation + 1, committed.Generation);
        Assert.AreEqual(committed.Generation + 1, redirected.Generation);
        Assert.IsTrue(registry.Events.Any(evt => evt.EventType == BrowserTargetEventType.NavigationFinished));
    }

    [TestMethod]
    public void BrowserTargetDetachedDestroyedAndStaleBlockModifyingAction()
    {
        var manager = new BrowserTargetManager();
        var target = manager.Registry.UpsertTarget("target-block", new Uri("https://example.com/"), "Example", BrowserTargetState.Alive);
        var frame = manager.Frames.AttachFrame(target.TargetId, "main", null, target.Url);
        var action = Action(manager.ToTargetContext("run-target", "session-target", target, frame), BrowserActionType.Click);

        Assert.IsTrue(manager.CanExecute(action, target, frame));

        var detachedTarget = manager.Registry.MarkTarget(target.TargetId, BrowserTargetState.Detached, BrowserTargetEventType.TargetDetached);
        Assert.IsFalse(manager.CanExecute(action, detachedTarget, frame));

        var destroyedTarget = manager.Registry.MarkTarget(target.TargetId, BrowserTargetState.Destroyed, BrowserTargetEventType.TargetDestroyed);
        Assert.IsFalse(manager.CanExecute(action, destroyedTarget, frame));
    }

    [TestMethod]
    public void BrowserTargetSelectionDoesNotAssumeActiveTabWithoutExplicitTarget()
    {
        var manager = new BrowserTargetManager();
        manager.Registry.UpsertTarget("target-active", new Uri("https://chat.example/"), "Chat", BrowserTargetState.Active, isActive: true, isUserFacing: true);

        var ex = Assert.ThrowsExactly<InvalidOperationException>(() =>
            manager.SelectTarget(BrowserTargetSelectionPolicy.Explicit("example.com")));

        StringAssert.Contains(ex.Message, "Explicit target is required");
    }

    [TestMethod]
    public void BrowserTargetSelectionPolicyUsesExplicitTargetAndExpectedHost()
    {
        var manager = new BrowserTargetManager();
        manager.Registry.UpsertTarget("target-a", new Uri("https://chat.example/"), "Chat", BrowserTargetState.Active, isActive: true, isUserFacing: true);
        manager.Registry.UpsertTarget("target-b", new Uri("https://portal.example/"), "Portal", BrowserTargetState.Background, isActive: false, isUserFacing: false);

        var selected = manager.SelectTarget(BrowserTargetSelectionPolicy.Explicit("portal.example"), "target-b");

        Assert.AreEqual("target-b", selected.TargetId);
    }

    [TestMethod]
    public void BrowserFrameTreeRepresentsMainAndChildFrame()
    {
        var fixture = SourcePath("tests", "fixtures", "browser-executor", "frames.html");
        Assert.IsTrue(File.Exists(fixture));
        var manager = new BrowserTargetManager();
        var target = manager.Registry.UpsertTarget("target-frames", new Uri(fixture), "Frames", BrowserTargetState.Alive);

        var main = manager.Frames.AttachFrame(target.TargetId, "main", null, target.Url);
        var child = manager.Frames.AttachFrame(target.TargetId, "child-1", "main", new Uri("about:srcdoc"), "Child fixture");
        var tree = manager.Frames.GetTree(target.TargetId);

        Assert.AreEqual(main, tree.MainFrame);
        Assert.AreEqual(2, tree.Frames.Count);
        Assert.AreEqual("main", child.ParentFrameId);
    }

    [TestMethod]
    public void BrowserDetachedFrameBlocksActionAndVerificationContext()
    {
        var manager = new BrowserTargetManager();
        var target = manager.Registry.UpsertTarget("target-frame-detach", new Uri("https://example.com/"), "Example", BrowserTargetState.Alive);
        var frame = manager.Frames.AttachFrame(target.TargetId, "child", "main", target.Url);
        var detached = manager.Frames.DetachFrame(target.TargetId, frame.FrameId);

        Assert.IsFalse(detached.CanUseForVerification);
        Assert.ThrowsExactly<InvalidOperationException>(() =>
            manager.ToTargetContext("run-frame", "session-frame", target, detached));
    }

    [TestMethod]
    public void BrowserPopupAndWindowEventsAreRecorded()
    {
        var registry = new BrowserTargetRegistry();
        var popup = registry.UpsertTarget("target-popup", new Uri("https://popup.example/"), "Popup", BrowserTargetState.Popup);
        registry.MarkTarget(popup.TargetId, BrowserTargetState.Popup, BrowserTargetEventType.PopupOpened);
        registry.MarkTarget(popup.TargetId, BrowserTargetState.Popup, BrowserTargetEventType.WindowOpened);
        registry.MarkTarget(popup.TargetId, BrowserTargetState.Background, BrowserTargetEventType.DownloadStarted);

        Assert.IsTrue(registry.Events.Any(evt => evt.EventType == BrowserTargetEventType.PopupOpened));
        Assert.IsTrue(registry.Events.Any(evt => evt.EventType == BrowserTargetEventType.WindowOpened));
        Assert.IsTrue(registry.Events.Any(evt => evt.EventType == BrowserTargetEventType.DownloadStarted));
    }

    [TestMethod]
    public void BrowserTargetContextEvidenceAndVerificationCarryTargetAndFrame()
    {
        var manager = new BrowserTargetManager();
        var target = manager.Registry.UpsertTarget("target-evidence", new Uri("https://example.com/"), "Example", BrowserTargetState.Alive, tabId: "tab-evidence", isActive: false, isVisible: null, isUserFacing: null);
        var frame = manager.Frames.AttachFrame(target.TargetId, "main", null, target.Url);
        var context = manager.ToTargetContext("run-evidence", "session-evidence", target, frame);

        var evidence = new BrowserEvidence(
            EvidenceId: "evidence-target",
            RunId: "run-evidence",
            StepId: "step",
            ActionId: null,
            VerificationId: null,
            TargetContext: context,
            EvidenceType: BrowserEvidenceType.DomSnapshot,
            CreatedAtUtc: DateTimeOffset.UtcNow,
            Summary: "target/frame evidence",
            PayloadRef: null,
            InlinePayload: null,
            RedactionApplied: true,
            SensitivityLevel: BrowserSensitivityLevel.Low);
        var verification = new BrowserVerification(
            VerificationId: "verification-target",
            RunId: "run-evidence",
            StepId: "step",
            ActionId: null,
            TargetContext: context,
            ExpectedOutcome: new BrowserExpectedOutcome("target frame verified", "example.com", null, null),
            PreObservationId: null,
            PostObservationId: "observation-target",
            Status: BrowserVerificationStatus.Verified,
            Confidence: 0.95,
            EvidenceRefs: [evidence.EvidenceId],
            FailureReason: null,
            VerifiedAtUtc: DateTimeOffset.UtcNow,
            ProofRefs: ["proof-target-frame"]);

        Assert.AreEqual("target-evidence", evidence.TargetContext!.TargetId);
        Assert.AreEqual("main", evidence.TargetContext.FrameId);
        Assert.AreEqual("target-evidence", verification.TargetContext.TargetId);
        Assert.AreEqual("main", verification.TargetContext.FrameId);
        Assert.IsTrue(evidence.Validate().IsValid);
        Assert.IsTrue(verification.Validate().IsValid);
    }

    [TestMethod]
    public void BrowserTargetRuntimeEventHandlerMarksDetachedAndDestroyed()
    {
        var manager = new BrowserTargetManager();
        manager.ApplyRuntimeEvent(new BrowserCdpRuntimeEvent(BrowserTargetEventType.TargetCreated, "target-live", Url: new Uri("https://example.com/"), Title: "Example"));

        manager.ApplyRuntimeEvent(new BrowserCdpRuntimeEvent(BrowserTargetEventType.TargetDetached, "target-live", Reason: "cdp detached"));
        var detached = manager.Registry.TryGet("target-live");
        manager.ApplyRuntimeEvent(new BrowserCdpRuntimeEvent(BrowserTargetEventType.TargetDestroyed, "target-live", Reason: "cdp destroyed"));
        var destroyed = manager.Registry.TryGet("target-live");

        Assert.AreEqual(BrowserTargetState.Detached, detached!.State);
        Assert.AreEqual(BrowserTargetState.Destroyed, destroyed!.State);
    }

    [TestMethod]
    public void BrowserTargetRuntimeEventHandlerUpdatesNavigationGenerationAndFrameContext()
    {
        var manager = new BrowserTargetManager();
        manager.ApplyRuntimeEvent(new BrowserCdpRuntimeEvent(BrowserTargetEventType.TargetCreated, "target-nav-live", Url: new Uri("https://example.com/start"), Title: "Start"));
        manager.ApplyRuntimeEvent(new BrowserCdpRuntimeEvent(BrowserTargetEventType.FrameAttached, "target-nav-live", FrameId: "main", Url: new Uri("https://example.com/start")));

        var before = manager.Registry.TryGet("target-nav-live")!;
        manager.ApplyRuntimeEvent(new BrowserCdpRuntimeEvent(BrowserTargetEventType.NavigationCommitted, "target-nav-live", FrameId: "main", Url: new Uri("https://example.com/final"), Title: "Final"));
        var after = manager.Registry.TryGet("target-nav-live")!;
        var tree = manager.Frames.GetTree("target-nav-live");

        Assert.IsTrue(after.Generation > before.Generation);
        Assert.AreEqual(new Uri("https://example.com/final"), after.Url);
        Assert.AreEqual(new Uri("https://example.com/final"), tree.MainFrame.Url);
    }

    [TestMethod]
    public void BrowserTargetRuntimeEventHandlerDetachesFrame()
    {
        var manager = new BrowserTargetManager();
        manager.ApplyRuntimeEvent(new BrowserCdpRuntimeEvent(BrowserTargetEventType.TargetCreated, "target-frame-live", Url: new Uri("https://example.com/"), Title: "Example"));
        manager.ApplyRuntimeEvent(new BrowserCdpRuntimeEvent(BrowserTargetEventType.FrameAttached, "target-frame-live", FrameId: "child", ParentFrameId: "main", Url: new Uri("https://example.com/frame")));

        manager.ApplyRuntimeEvent(new BrowserCdpRuntimeEvent(BrowserTargetEventType.FrameDetached, "target-frame-live", FrameId: "child"));
        var tree = manager.Frames.GetTree("target-frame-live");

        Assert.AreEqual(BrowserTargetState.Detached, tree.Frames["child"].State);
        Assert.IsFalse(tree.Frames["child"].CanUseForVerification);
    }

    private static BrowserAction Action(BrowserTargetContext context, BrowserActionType type) =>
        new(
            ActionId: "action-" + Guid.NewGuid().ToString("N"),
            IdempotencyKey: "idem-" + Guid.NewGuid().ToString("N"),
            RunId: context.RunId,
            StepId: "step",
            TargetContext: context,
            FrameId: context.FrameId,
            ActionType: type,
            Target: new BrowserActionTarget("candidate", "#button", "button", null),
            Input: null,
            ExpectedOutcome: new BrowserExpectedOutcome("expected", null, "expected", null),
            RiskClass: BrowserRiskClass.Low,
            TimeoutMs: 8000,
            RequiresApproval: false,
            CreatedAtUtc: DateTimeOffset.UtcNow);

    private static string SourcePath(params string[] segments)
    {
        var directory = new DirectoryInfo(Directory.GetCurrentDirectory());
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "OneBrain.slnx")))
            directory = directory.Parent;

        Assert.IsNotNull(directory, "Repository root was not found.");
        return Path.Combine(new[] { directory!.FullName }.Concat(segments).ToArray());
    }
}
