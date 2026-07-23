using Microsoft.VisualStudio.TestTools.UnitTesting;
using OneBrain.Core.Models;
using OneBrain.Core.Perception;
using OneBrain.Core.Skills;
using OneBrain.Pilot;

namespace OneBrain.Recipes.Tests;

[TestClass]
[TestCategory("NodalOsTier1Safety")]
[TestCategory("TeachNodalProduct")]
public sealed class NodalOsTeachNodalProductTests
{
    private static readonly IntPtr Hwnd = new(4242);

    [TestMethod]
    public async Task GuidedSemanticCaptureProducesEditableReviewOnlyDraft()
    {
        var root = NewRoot();
        try
        {
            var service = Service(root, SaveSnapshots());

            var bound = await service.BindAsync(
                new NodalOsTeachNodalBindRequest("Save one fixture document", "fixture-editor"),
                CancellationToken.None);
            Assert.AreEqual(NodalOsTeachNodalProductState.Bound, bound.State);
            Assert.IsTrue(bound.ExplicitOptInRecorded);
            Assert.IsTrue(bound.ApplicationScopeBound);

            var captured = await CaptureSaveStep(service, "Save the fixture document.");
            Assert.AreEqual(1, captured.ObservationCount);
            Assert.AreEqual(NodalOsTeachNodalProductState.Bound, captured.State);

            var reviewed = await service.FinishAsync(CancellationToken.None);
            Assert.AreEqual(NodalOsTeachNodalProductState.ReviewReady, reviewed.State);
            Assert.IsNotNull(reviewed.Proposal);
            Assert.AreEqual(NodalOsTeachNodalProposalKind.NewSkill, reviewed.Proposal.Kind);
            Assert.IsTrue(reviewed.Proposal.SaveAllowed);
            Assert.IsTrue(reviewed.Proposal.ReviewRequired);
            Assert.IsTrue(reviewed.Proposal.Steps.Single().Verified);
            Assert.IsFalse(reviewed.Proposal.ScriptsIncluded);
            Assert.IsFalse(reviewed.ReplayEnabled);
            Assert.IsFalse(reviewed.RawInputStored);
            Assert.IsFalse(reviewed.GlobalHooksUsed);
            Assert.IsFalse(reviewed.ExecutionAuthorityGranted);
            Assert.IsFalse(reviewed.ProductAuthorityGranted);

            var edited = service.UpdateProposal(new NodalOsTeachNodalProposalEditRequest(
                "Save the reviewed fixture document",
                "One bounded semantic action, reviewed before reuse.",
                new Dictionary<string, string>(StringComparer.Ordinal)
                {
                    [reviewed.Proposal.Steps.Single().StepId] = "Save only inside the already selected application."
                },
                new Dictionary<string, string>(StringComparer.Ordinal)
                {
                    [reviewed.Proposal.Steps.Single().StepId] = "Save"
                }));
            Assert.AreEqual("Save the reviewed fixture document", edited.Proposal?.Title);
            Assert.AreEqual(TeachNodalCompilationDecision.DraftNeedsReview.ToString(), edited.Proposal?.CompilationDecision);
            Assert.AreEqual("TEACH_NODAL_REVIEW_EDIT_REQUIRES_REVERIFICATION", edited.Proposal?.CompilationCode);
            Assert.AreEqual(string.Empty, edited.Proposal?.SkillFingerprint);
            Assert.IsFalse(edited.Proposal?.Steps.Single().Verified ?? true);

            var saved = await service.SaveAsync(CancellationToken.None);
            Assert.AreEqual(NodalOsTeachNodalProductState.Saved, saved.State);
            Assert.AreEqual(1, saved.Proposal?.Version);
            Assert.AreEqual(1, saved.SavedDrafts.Count);
            Assert.AreEqual(1, Directory.GetFiles(root, "*.json").Length);
            Assert.IsFalse(saved.Proposal?.ExecutionAuthorityGranted ?? true);
        }
        finally
        {
            DeleteRoot(root);
        }
    }

    [TestMethod]
    public async Task MatchingSavedWorkflowIsProposedAsUpdateWithoutReplayAuthority()
    {
        var root = NewRoot();
        try
        {
            var first = Service(root, SaveSnapshots());
            await CaptureAndSave(first, "Prepare monthly report");

            var second = Service(root, SaveSnapshots());
            await second.BindAsync(
                new NodalOsTeachNodalBindRequest("Prepare monthly report", "fixture-editor"),
                CancellationToken.None);
            await CaptureSaveStep(second, "Save the monthly report.");
            var proposal = await second.FinishAsync(CancellationToken.None);

            Assert.AreEqual(NodalOsTeachNodalProposalKind.UpdateCandidate, proposal.Proposal?.Kind);
            Assert.AreEqual(1, proposal.Proposal?.Version);
            Assert.IsFalse(proposal.ReplayEnabled);
            Assert.IsFalse(proposal.ExecutionAuthorityGranted);
            Assert.IsFalse(proposal.ProductAuthorityGranted);
        }
        finally
        {
            DeleteRoot(root);
        }
    }

    [TestMethod]
    public async Task DuplicateSaveFailsClosedWithoutCreatingNoOpVersion()
    {
        var root = NewRoot();
        try
        {
            var service = Service(root, SaveSnapshots());
            var firstSave = await CaptureAndSave(service, "Save once");
            Assert.AreEqual(1, firstSave.Proposal?.Version);

            var duplicate = await service.SaveAsync(CancellationToken.None);

            Assert.AreEqual(NodalOsTeachNodalProductState.FailedClosed, duplicate.State);
            Assert.IsFalse(duplicate.Bound);
            Assert.IsNull(duplicate.Proposal);
            Assert.AreEqual(1, duplicate.SavedDrafts.Single().Version);
            Assert.AreEqual(1, Directory.GetFiles(root, "*.json").Length);
        }
        finally
        {
            DeleteRoot(root);
        }
    }

    [TestMethod]
    public async Task FailedClosedSessionClearsActiveStateAndRequiresDiscard()
    {
        var root = NewRoot();
        try
        {
            var service = Service(root,
            [
                Snapshot("Fixture Editor", "empty"),
                Snapshot("Fixture Editor", "empty")
            ]);
            await service.BindAsync(
                new NodalOsTeachNodalBindRequest("Missing target", "fixture-editor"),
                CancellationToken.None);

            var failed = await service.CaptureStepAsync(
                new NodalOsTeachNodalCaptureStepRequest(
                    TeachNodalActionKind.Click,
                    "Click a target that is not present.",
                    "Does not exist",
                    "Button",
                    null,
                    null,
                    false),
                CancellationToken.None);

            Assert.AreEqual(NodalOsTeachNodalProductState.FailedClosed, failed.State);
            Assert.IsFalse(failed.Bound);
            Assert.IsNull(failed.Proposal);
            Assert.IsFalse(failed.ApplicationScopeBound);

            var stillBlocked = await service.FinishAsync(CancellationToken.None);
            Assert.AreEqual(NodalOsTeachNodalProductState.FailedClosed, stillBlocked.State);
            Assert.IsFalse(stillBlocked.Bound);

            var reset = service.Discard();
            Assert.AreEqual(NodalOsTeachNodalProductState.Empty, reset.State);
        }
        finally
        {
            DeleteRoot(root);
        }
    }

    [TestMethod]
    public async Task InvalidReviewCanBeRejectedWithoutLeavingCaptureOrProposalActive()
    {
        var root = NewRoot();
        try
        {
            var service = Service(root, SaveSnapshots());
            await service.BindAsync(
                new NodalOsTeachNodalBindRequest("Review invalid edit", "fixture-editor"),
                CancellationToken.None);
            await CaptureSaveStep(service, "Save the fixture document.");
            await service.FinishAsync(CancellationToken.None);

            var exception = Assert.ThrowsExactly<ArgumentException>(() => service.UpdateProposal(
                new NodalOsTeachNodalProposalEditRequest(
                    string.Empty,
                    "Still a summary",
                    new Dictionary<string, string>(),
                    new Dictionary<string, string>())));
            var failed = service.RejectReview(exception.Message);

            Assert.AreEqual(NodalOsTeachNodalProductState.FailedClosed, failed.State);
            Assert.IsFalse(failed.Bound);
            Assert.IsNull(failed.Proposal);
            Assert.IsFalse(failed.ExecutionAuthorityGranted);
            Assert.IsFalse(failed.ProductAuthorityGranted);
        }
        finally
        {
            DeleteRoot(root);
        }
    }

    [TestMethod]
    public async Task CorruptOrAuthorityBearingDraftsAreIgnored()
    {
        var root = NewRoot();
        try
        {
            Directory.CreateDirectory(root);
            await File.WriteAllTextAsync(Path.Combine(root, "corrupt.json"), "{}", CancellationToken.None);
            await File.WriteAllTextAsync(
                Path.Combine(root, "authority.json"),
                """
                {
                  "draftId":"bad",
                  "version":1,
                  "kind":"NewSkill",
                  "title":"Bad",
                  "summary":"Bad",
                  "appProfileId":"fixture-editor",
                  "applicationRef":"app.fixture-editor",
                  "processNameRedacted":"fixture-editor",
                  "compilationDecision":"DraftNeedsReview",
                  "compilationCode":"BAD",
                  "skillFingerprint":"",
                  "steps":[],
                  "findings":[],
                  "createdAtUtc":"2026-07-23T00:00:00Z",
                  "updatedAtUtc":"2026-07-23T00:00:00Z",
                  "reviewRequired":true,
                  "saveAllowed":true,
                  "scriptsIncluded":false,
                  "rawInputStored":false,
                  "rawScreenshotStored":false,
                  "rawDomStored":false,
                  "globalHooksUsed":false,
                  "executionAuthorityGranted":true,
                  "productAuthorityGranted":true
                }
                """,
                CancellationToken.None);

            var service = Service(root, SaveSnapshots());
            var snapshot = service.GetSnapshot();
            Assert.AreEqual(0, snapshot.SavedDrafts.Count);

            await service.BindAsync(
                new NodalOsTeachNodalBindRequest("Prepare monthly report", "fixture-editor"),
                CancellationToken.None);
            await CaptureSaveStep(service, "Save the monthly report.");
            var review = await service.FinishAsync(CancellationToken.None);
            Assert.AreEqual(NodalOsTeachNodalProposalKind.NewSkill, review.Proposal?.Kind);
        }
        finally
        {
            DeleteRoot(root);
        }
    }

    [TestMethod]
    public async Task SensitiveTypeUsesOpaqueSecretReferenceWithoutRawInput()
    {
        var root = NewRoot();
        try
        {
            var service = Service(root,
            [
                InputSnapshot("Fixture Editor", "empty"),
                InputSnapshot("Fixture Editor", "empty"),
                InputSnapshot("Fixture Editor — Updated", "updated")
            ]);
            await service.BindAsync(
                new NodalOsTeachNodalBindRequest("Enter protected value", "fixture-editor"),
                CancellationToken.None);
            var captured = await service.CaptureStepAsync(
                new NodalOsTeachNodalCaptureStepRequest(
                    TeachNodalActionKind.Type,
                    "Enter the already configured protected value.",
                    "Account value",
                    "Edit",
                    "ACCOUNT_VALUE",
                    "secret-ref:account-value",
                    true),
                CancellationToken.None);
            var reviewed = await service.FinishAsync(CancellationToken.None);

            Assert.AreEqual(NodalOsTeachNodalProductState.Bound, captured.State);
            Assert.AreEqual(NodalOsTeachNodalProductState.ReviewReady, reviewed.State);
            StringAssert.Contains(reviewed.Proposal?.Steps.Single().ParameterRefs.Single(), "secret-ref:account-value");
            Assert.IsFalse(reviewed.RawInputStored);
            Assert.IsFalse(reviewed.Proposal?.RawInputStored ?? true);
        }
        finally
        {
            DeleteRoot(root);
        }
    }

    [TestMethod]
    public void ProductSurfaceIsPackagedButLegacyTeachSurfaceRemainsExcluded()
    {
        foreach (var route in new[]
        {
            "/teach",
            "/api/teach",
            "/teach/bind",
            "/teach/capture",
            "/teach/finish",
            "/teach/review",
            "/teach/save",
            "/teach/discard"
        })
        {
            Assert.IsTrue(NodalOsDesktopLaunchRuntime.IsPackagedProductPath(route), route);
        }

        Assert.IsFalse(NodalOsDesktopLaunchRuntime.IsPackagedProductPath("/runtime/teach-nodal"));
        Assert.IsFalse(NodalOsDesktopLaunchRuntime.IsPackagedProductPath("/api/runtime/teach-nodal"));
    }

    [TestMethod]
    public void RendererMakesPrivacyAuthorityAndTerminalStateBoundariesExplicit()
    {
        var snapshot = EmptySnapshot(NodalOsTeachNodalProductState.Empty, []);
        var html = NodalOsTeachNodalProductHtmlRenderer.Render(snapshot, new string('a', 64));

        StringAssert.Contains(html, "data-nodal-os=\"teach-nodal-product-surface\"");
        StringAssert.Contains(html, "data-video-stored=\"false\"");
        StringAssert.Contains(html, "data-audio-stored=\"false\"");
        StringAssert.Contains(html, "data-raw-input-stored=\"false\"");
        StringAssert.Contains(html, "data-global-hooks=\"false\"");
        StringAssert.Contains(html, "data-replay-enabled=\"false\"");
        StringAssert.Contains(html, "data-execution-authority=\"false\"");
        StringAssert.Contains(html, "data-product-authority=\"false\"");
        StringAssert.Contains(html, "Win + H");
        Assert.IsFalse(html.Contains("secret://", StringComparison.Ordinal));

        var failedHtml = NodalOsTeachNodalProductHtmlRenderer.Render(
            EmptySnapshot(NodalOsTeachNodalProductState.FailedClosed, ["Stopped safely."]),
            new string('b', 64));
        StringAssert.Contains(failedHtml, "failed closed");
        StringAssert.Contains(failedHtml, "Descartar y volver al inicio");
        Assert.IsFalse(failedHtml.Contains("Vincular aplicación foreground", StringComparison.Ordinal));
    }

    private static async Task<NodalOsTeachNodalProductSnapshot> CaptureAndSave(
        NodalOsTeachNodalProductService service,
        string title)
    {
        await service.BindAsync(new NodalOsTeachNodalBindRequest(title, "fixture-editor"), CancellationToken.None);
        await CaptureSaveStep(service, "Save the monthly report.");
        await service.FinishAsync(CancellationToken.None);
        return await service.SaveAsync(CancellationToken.None);
    }

    private static Task<NodalOsTeachNodalProductSnapshot> CaptureSaveStep(
        NodalOsTeachNodalProductService service,
        string intent) =>
        service.CaptureStepAsync(
            new NodalOsTeachNodalCaptureStepRequest(
                TeachNodalActionKind.Click,
                intent,
                "Save",
                "Button",
                null,
                null,
                false),
            CancellationToken.None);

    private static NodalOsTeachNodalProductService Service(
        string root,
        IEnumerable<CognitiveSnapshot> snapshots)
    {
        var queue = new Queue<CognitiveSnapshot>(snapshots);
        return new NodalOsTeachNodalProductService(
            root,
            (_, _) => queue.Count == 0 ? null : queue.Dequeue(),
            () => Hwnd,
            (_, _) => Task.CompletedTask);
    }

    private static string NewRoot() => Path.Combine(
        Path.GetTempPath(),
        "nodal-teach-product-tests",
        Guid.NewGuid().ToString("N"));

    private static void DeleteRoot(string root)
    {
        if (Directory.Exists(root))
            Directory.Delete(root, recursive: true);
    }

    private static CognitiveSnapshot[] SaveSnapshots() =>
    [
        Snapshot("Fixture Editor", "empty"),
        Snapshot("Fixture Editor", "empty"),
        Snapshot("Fixture Editor — Saved", "saved")
    ];

    private static CognitiveSnapshot Snapshot(string title, string documentState) => new(
        new WindowSnapshot(
            Title: title,
            ProcessName: "fixture-editor",
            ProcessId: 101,
            Bounds: new WindowBounds(0, 0, 1280, 720),
            IsForeground: true),
        [
            Element("document-state", "Document", documentState, "document-runtime"),
            Element("save-button", "Button", "Save", "save-runtime")
        ],
        TreeTruncated: false);

    private static CognitiveSnapshot InputSnapshot(string title, string documentState) => new(
        new WindowSnapshot(
            Title: title,
            ProcessName: "fixture-editor",
            ProcessId: 101,
            Bounds: new WindowBounds(0, 0, 1280, 720),
            IsForeground: true),
        [
            Element("document-state", "Document", documentState, "document-runtime"),
            Element("account-value", "Edit", "Account value", "account-runtime")
        ],
        TreeTruncated: false);

    private static UiElementSnapshot Element(
        string automationId,
        string role,
        string name,
        string runtimeId) => new(
        Ref: "@" + automationId,
        Role: role,
        Name: name,
        AutomationId: automationId,
        ClassName: role,
        Bounds: new WindowBounds(10, 10, 200, 50),
        IsEnabled: true,
        IsOffscreen: false,
        IsKeyboardFocusable: true,
        Patterns: role == "Button" ? ["Invoke"] : ["Value"],
        Actions: role == "Button" ? ["invoke"] : ["read_value"],
        RuntimeId: runtimeId);

    private static NodalOsTeachNodalProductSnapshot EmptySnapshot(
        NodalOsTeachNodalProductState state,
        IReadOnlyList<string> findings) =>
        new(
            state == NodalOsTeachNodalProductState.FailedClosed
                ? "TEACH_NODAL_FAILED_CLOSED"
                : "TEACH_NODAL_READY_TO_BIND",
            state,
            false,
            "not bound",
            "not bound",
            "not bound",
            0,
            null,
            [],
            findings,
            false,
            false,
            false,
            false,
            false,
            false,
            false,
            false,
            false,
            false);
}
