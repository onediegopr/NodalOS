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
            var service = Service(root,
            [
                Snapshot("Fixture Editor", "empty"),
                Snapshot("Fixture Editor", "empty"),
                Snapshot("Fixture Editor — Saved", "saved")
            ]);

            var bound = await service.BindAsync(
                new NodalOsTeachNodalBindRequest("Save one fixture document", "fixture-editor"),
                CancellationToken.None);
            Assert.AreEqual(NodalOsTeachNodalProductState.Bound, bound.State);
            Assert.IsTrue(bound.ExplicitOptInRecorded);
            Assert.IsTrue(bound.ApplicationScopeBound);

            var captured = await service.CaptureStepAsync(
                new NodalOsTeachNodalCaptureStepRequest(
                    TeachNodalActionKind.Click,
                    "Save the fixture document.",
                    "Save",
                    "Button",
                    null,
                    null,
                    false),
                CancellationToken.None);
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

            var saved = await service.SaveAsync(CancellationToken.None);
            Assert.AreEqual(NodalOsTeachNodalProductState.Saved, saved.State);
            Assert.AreEqual(1, saved.Proposal?.Version);
            Assert.AreEqual(1, saved.SavedDrafts.Count);
            Assert.AreEqual(1, Directory.GetFiles(root, "*.json").Length);
            Assert.IsFalse(saved.Proposal?.ExecutionAuthorityGranted ?? true);
        }
        finally
        {
            if (Directory.Exists(root))
                Directory.Delete(root, recursive: true);
        }
    }

    [TestMethod]
    public async Task MatchingSavedWorkflowIsProposedAsUpdateWithoutReplayAuthority()
    {
        var root = NewRoot();
        try
        {
            var first = Service(root,
            [
                Snapshot("Fixture Editor", "empty"),
                Snapshot("Fixture Editor", "empty"),
                Snapshot("Fixture Editor — Saved", "saved")
            ]);
            await CaptureAndSave(first, "Prepare monthly report");

            var second = Service(root,
            [
                Snapshot("Fixture Editor", "empty"),
                Snapshot("Fixture Editor", "empty"),
                Snapshot("Fixture Editor — Saved", "saved")
            ]);
            await second.BindAsync(
                new NodalOsTeachNodalBindRequest("Prepare monthly report", "fixture-editor"),
                CancellationToken.None);
            await second.CaptureStepAsync(
                new NodalOsTeachNodalCaptureStepRequest(
                    TeachNodalActionKind.Click,
                    "Save the monthly report.",
                    "Save",
                    "Button",
                    null,
                    null,
                    false),
                CancellationToken.None);
            var proposal = await second.FinishAsync(CancellationToken.None);

            Assert.AreEqual(NodalOsTeachNodalProposalKind.UpdateCandidate, proposal.Proposal?.Kind);
            Assert.AreEqual(1, proposal.Proposal?.Version);
            Assert.IsFalse(proposal.ReplayEnabled);
            Assert.IsFalse(proposal.ExecutionAuthorityGranted);
            Assert.IsFalse(proposal.ProductAuthorityGranted);
        }
        finally
        {
            if (Directory.Exists(root))
                Directory.Delete(root, recursive: true);
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
    public void RendererMakesPrivacyAndAuthorityBoundariesExplicit()
    {
        var snapshot = new NodalOsTeachNodalProductSnapshot(
            "TEACH_NODAL_READY_TO_BIND",
            NodalOsTeachNodalProductState.Empty,
            false,
            "not bound",
            "not bound",
            "not bound",
            0,
            null,
            [],
            [],
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
    }

    private static async Task CaptureAndSave(NodalOsTeachNodalProductService service, string title)
    {
        await service.BindAsync(new NodalOsTeachNodalBindRequest(title, "fixture-editor"), CancellationToken.None);
        await service.CaptureStepAsync(
            new NodalOsTeachNodalCaptureStepRequest(
                TeachNodalActionKind.Click,
                "Save the monthly report.",
                "Save",
                "Button",
                null,
                null,
                false),
            CancellationToken.None);
        await service.FinishAsync(CancellationToken.None);
        await service.SaveAsync(CancellationToken.None);
    }

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
}