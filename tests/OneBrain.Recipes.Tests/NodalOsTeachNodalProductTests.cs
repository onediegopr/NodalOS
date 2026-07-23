using Microsoft.VisualStudio.TestTools.UnitTesting;
using OneBrain.Core.Models;
using OneBrain.Core.Perception;
using OneBrain.Core.Skills;
using OneBrain.Pilot;
using System.Text.Json;
using System.Text.Json.Serialization;

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

            var edited = await service.UpdateProposalAsync(
                new NodalOsTeachNodalProposalEditRequest(
                    "Save the reviewed fixture document",
                    "One bounded semantic action, reviewed before reuse.",
                    reviewed.Proposal.Version,
                    reviewed.Proposal.UpdatedAtUtc,
                    new Dictionary<string, string>(StringComparer.Ordinal)
                    {
                        [reviewed.Proposal.Steps.Single().StepId] = "Save only inside the already selected application."
                    },
                    new Dictionary<string, string>(StringComparer.Ordinal)
                    {
                        [reviewed.Proposal.Steps.Single().StepId] = "Save"
                    }),
                CancellationToken.None);
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
    public async Task StaleSaveAcrossServiceInstancesFailsClosedWithoutOverwritingNewerDraft()
    {
        var root = NewRoot();
        try
        {
            var initial = Service(root, SaveSnapshots());
            var first = await CaptureAndSave(initial, "Concurrent draft");
            Assert.AreEqual(1, first.Proposal?.Version);

            var staleA = Service(root, SaveSnapshots());
            await staleA.BindAsync(new NodalOsTeachNodalBindRequest("Concurrent draft", "fixture-editor"), CancellationToken.None);
            await CaptureSaveStep(staleA, "Save with stale session A.");
            var reviewA = await staleA.FinishAsync(CancellationToken.None);

            var freshB = Service(root, SaveSnapshots());
            await freshB.BindAsync(new NodalOsTeachNodalBindRequest("Concurrent draft", "fixture-editor"), CancellationToken.None);
            await CaptureSaveStep(freshB, "Save with fresh session B.");
            var reviewB = await freshB.FinishAsync(CancellationToken.None);
            var editedB = await freshB.UpdateProposalAsync(
                new NodalOsTeachNodalProposalEditRequest(
                    "Concurrent draft from B",
                    reviewB.Proposal!.Summary,
                    reviewB.Proposal.Version,
                    reviewB.Proposal.UpdatedAtUtc,
                    new Dictionary<string, string>(),
                    new Dictionary<string, string>()),
                CancellationToken.None);

            var savedB = await freshB.SaveAsync(CancellationToken.None);
            var failedA = await staleA.SaveAsync(CancellationToken.None);
            var persisted = ReadDraft(root);

            Assert.AreEqual(NodalOsTeachNodalProductState.Saved, savedB.State);
            Assert.AreEqual(NodalOsTeachNodalProductState.FailedClosed, failedA.State);
            Assert.AreEqual(2, persisted.Version);
            Assert.AreEqual("Concurrent draft from B", persisted.Title);
            Assert.AreEqual(2, savedB.Proposal?.Version);
            Assert.AreEqual(1, reviewA.Proposal?.Version);
            Assert.IsFalse(File.Exists(Path.Combine(root, persisted.DraftId + ".json.tmp")));
            Assert.IsFalse(Directory.EnumerateFiles(root, "*.tmp").Any());
            Assert.AreEqual("Draft changed after this review started. Refresh and review the current version before saving.", failedA.Findings.Single());
            Assert.AreEqual("Concurrent draft from B", editedB.Proposal?.Title);
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

            var reset = await service.DiscardAsync(CancellationToken.None);
            Assert.AreEqual(NodalOsTeachNodalProductState.Empty, reset.State);
        }
        finally
        {
            DeleteRoot(root);
        }
    }

    [TestMethod]
    public async Task InvalidReviewFailsClosedWithoutLeavingCaptureOrProposalActive()
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

            var failed = await service.UpdateProposalAsync(
                new NodalOsTeachNodalProposalEditRequest(
                    string.Empty,
                    "Still a summary",
                    service.GetSnapshot().Proposal!.Version,
                    service.GetSnapshot().Proposal!.UpdatedAtUtc,
                    new Dictionary<string, string>(),
                    new Dictionary<string, string>()),
                CancellationToken.None);

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
    public async Task StaleReviewMetadataFailsClosedWithoutSavingPartialEdits()
    {
        var root = NewRoot();
        try
        {
            var service = Service(root, SaveSnapshots());
            await service.BindAsync(
                new NodalOsTeachNodalBindRequest("Review conflict", "fixture-editor"),
                CancellationToken.None);
            await CaptureSaveStep(service, "Save the fixture document.");
            var review = await service.FinishAsync(CancellationToken.None);
            var expectedVersion = review.Proposal!.Version;
            var expectedUpdatedAtUtc = review.Proposal.UpdatedAtUtc;

            var first = await service.UpdateProposalAsync(
                new NodalOsTeachNodalProposalEditRequest(
                    "Review conflict accepted",
                    review.Proposal.Summary,
                    expectedVersion,
                    expectedUpdatedAtUtc,
                    new Dictionary<string, string>(),
                    new Dictionary<string, string>()),
                CancellationToken.None);
            Assert.AreEqual("Review conflict accepted", first.Proposal?.Title);

            var failed = await service.UpdateProposalAsync(
                new NodalOsTeachNodalProposalEditRequest(
                    "Stale overwrite attempt",
                    first.Proposal!.Summary,
                    expectedVersion,
                    expectedUpdatedAtUtc,
                    new Dictionary<string, string>(),
                    new Dictionary<string, string>()),
                CancellationToken.None);

            Assert.AreEqual(NodalOsTeachNodalProductState.FailedClosed, failed.State);
            Assert.IsNull(failed.Proposal);
            Assert.AreEqual(0, Directory.Exists(root) ? Directory.GetFiles(root, "*.json").Length : 0);
            Assert.AreEqual("Draft changed after this review started. Refresh and review the current version before saving.", failed.Findings.Single());
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
            var service = Service(root, InputSnapshots());
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
    public async Task TargetMatchingRequiresExactVisibleOrCanonicalLabel()
    {
        var root = NewRoot();
        try
        {
            var service = Service(root,
            [
                SnapshotWithSaveLabel("Fixture Editor", "empty", "Auto Save Settings"),
                SnapshotWithSaveLabel("Fixture Editor", "empty", "Auto Save Settings")
            ]);
            await service.BindAsync(
                new NodalOsTeachNodalBindRequest("Exact target only", "fixture-editor"),
                CancellationToken.None);

            var failed = await CaptureSaveStep(service, "Save must not match Auto Save Settings.");

            Assert.AreEqual(NodalOsTeachNodalProductState.FailedClosed, failed.State);
            Assert.IsNull(failed.Proposal);
            Assert.AreEqual(0, Directory.Exists(root) ? Directory.GetFiles(root, "*.json").Length : 0);
        }
        finally
        {
            DeleteRoot(root);
        }
    }

    [TestMethod]
    public async Task TargetMatchingAcceptsExactLabelCaseInsensitive()
    {
        var root = NewRoot();
        try
        {
            var service = Service(root,
            [
                SnapshotWithSaveLabel("Fixture Editor", "empty", "save"),
                SnapshotWithSaveLabel("Fixture Editor", "empty", "save"),
                SnapshotWithSaveLabel("Fixture Editor - Saved", "saved", "save")
            ]);
            await service.BindAsync(
                new NodalOsTeachNodalBindRequest("Exact target positive", "fixture-editor"),
                CancellationToken.None);

            var captured = await service.CaptureStepAsync(
                new NodalOsTeachNodalCaptureStepRequest(
                    TeachNodalActionKind.Click,
                    "Click the exact save target.",
                    "SAVE",
                    "Button",
                    null,
                    null,
                    false),
                CancellationToken.None);
            var review = await service.FinishAsync(CancellationToken.None);

            Assert.AreEqual(NodalOsTeachNodalProductState.Bound, captured.State);
            Assert.AreEqual(NodalOsTeachNodalProductState.ReviewReady, review.State);
            Assert.AreEqual("save", review.Proposal?.Steps.Single().TargetLabel);
        }
        finally
        {
            DeleteRoot(root);
        }
    }

    [TestMethod]
    public async Task RawLookingSecretInsideReferenceFailsClosedAndIsNotPersisted()
    {
        var root = NewRoot();
        try
        {
            var service = Service(root,
            [
                InputSnapshot("Fixture Editor", "empty"),
                InputSnapshot("Fixture Editor", "empty")
            ]);
            await service.BindAsync(
                new NodalOsTeachNodalBindRequest("Reject raw protected value", "fixture-editor"),
                CancellationToken.None);

            var failed = await service.CaptureStepAsync(
                new NodalOsTeachNodalCaptureStepRequest(
                    TeachNodalActionKind.Type,
                    "Enter the configured protected value.",
                    "Account value",
                    "Edit",
                    "ACCOUNT_VALUE",
                    "secret-ref:sk-12345678",
                    true),
                CancellationToken.None);

            Assert.AreEqual(NodalOsTeachNodalProductState.FailedClosed, failed.State);
            Assert.IsFalse(failed.Bound);
            Assert.IsNull(failed.Proposal);
            Assert.AreEqual(0, Directory.Exists(root) ? Directory.GetFiles(root, "*.json").Length : 0);
        }
        finally
        {
            DeleteRoot(root);
        }
    }

    [TestMethod]
    [DataRow("secret-ref:sk-123456789012")]
    [DataRow("secret-ref:api_key=rawvalue")]
    [DataRow("secret-ref:Bearer abcdef123456")]
    [DataRow("secret-ref:token=rawvalue")]
    [DataRow("secret-ref:password=rawvalue")]
    public async Task RawLookingSecretReferenceVariantsFailClosedBeforeSanitize(string reference)
    {
        var root = NewRoot();
        try
        {
            var service = Service(root,
            [
                InputSnapshot("Fixture Editor", "empty"),
                InputSnapshot("Fixture Editor", "empty")
            ]);
            await service.BindAsync(
                new NodalOsTeachNodalBindRequest("Reject raw protected value", "fixture-editor"),
                CancellationToken.None);

            var failed = await service.CaptureStepAsync(
                new NodalOsTeachNodalCaptureStepRequest(
                    TeachNodalActionKind.Type,
                    "Enter the configured protected value.",
                    "Account value",
                    "Edit",
                    "ACCOUNT_VALUE",
                    reference,
                    true),
                CancellationToken.None);

            Assert.AreEqual(NodalOsTeachNodalProductState.FailedClosed, failed.State);
            Assert.IsFalse(string.Join(" ", failed.Findings).Contains("sk-", StringComparison.OrdinalIgnoreCase));
            Assert.IsFalse(string.Join(" ", failed.Findings).Contains("[REDACTED", StringComparison.OrdinalIgnoreCase));
            Assert.AreEqual(0, Directory.Exists(root) ? Directory.GetFiles(root, "*.json").Length : 0);
        }
        finally
        {
            DeleteRoot(root);
        }
    }

    [TestMethod]
    public async Task AcceptedReferencesAndReviewSavePreserveReviewOnlyAuthority()
    {
        var root = NewRoot();
        try
        {
            var secret = Service(root, InputSnapshots());
            await secret.BindAsync(new NodalOsTeachNodalBindRequest("Secret reference accepted", "fixture-editor"), CancellationToken.None);
            await secret.CaptureStepAsync(
                new NodalOsTeachNodalCaptureStepRequest(
                    TeachNodalActionKind.Type,
                    "Enter the already configured protected value.",
                    "Account value",
                    "Edit",
                    "ACCOUNT_VALUE",
                    "secret-ref:account-value",
                    true),
                CancellationToken.None);
            var secretReview = await secret.FinishAsync(CancellationToken.None);
            StringAssert.Contains(secretReview.Proposal!.Steps.Single().ParameterRefs.Single(), "secret-ref:account-value");

            var variable = Service(root, InputSnapshots());
            await variable.BindAsync(new NodalOsTeachNodalBindRequest("Variable reference accepted", "fixture-editor"), CancellationToken.None);
            await variable.CaptureStepAsync(
                new NodalOsTeachNodalCaptureStepRequest(
                    TeachNodalActionKind.Type,
                    "Enter the approved report name.",
                    "Account value",
                    "Edit",
                    "REPORT_NAME",
                    "variable-ref:REPORT_NAME",
                    false),
                CancellationToken.None);
            var variableReview = await variable.FinishAsync(CancellationToken.None);
            StringAssert.Contains(variableReview.Proposal!.Steps.Single().ParameterRefs.Single(), "variable-ref:REPORT_NAME");

            var literal = Service(root, InputSnapshots());
            await literal.BindAsync(new NodalOsTeachNodalBindRequest("Literal reference accepted", "fixture-editor"), CancellationToken.None);
            await literal.CaptureStepAsync(
                new NodalOsTeachNodalCaptureStepRequest(
                    TeachNodalActionKind.Type,
                    "Enter the approved template.",
                    "Account value",
                    "Edit",
                    "TEMPLATE",
                    "literal-ref:approved-template",
                    false),
                CancellationToken.None);
            var literalReview = await literal.FinishAsync(CancellationToken.None);
            var edited = await literal.UpdateProposalAsync(
                new NodalOsTeachNodalProposalEditRequest(
                    "Literal reference accepted",
                    "Reviewed literal reference remains review-only.",
                    literalReview.Proposal!.Version,
                    literalReview.Proposal.UpdatedAtUtc,
                    new Dictionary<string, string>
                    {
                        [literalReview.Proposal.Steps.Single().StepId] = "Enter the approved template after human review."
                    },
                    new Dictionary<string, string>()),
                CancellationToken.None);
            var saved = await literal.SaveAsync(CancellationToken.None);
            var html = NodalOsTeachNodalProductHtmlRenderer.Render(saved, new string('c', 64));

            StringAssert.Contains(literalReview.Proposal.Steps.Single().ParameterRefs.Single(), "literal-ref:approved-template");
            Assert.AreEqual(TeachNodalCompilationDecision.DraftNeedsReview.ToString(), edited.Proposal?.CompilationDecision);
            Assert.AreEqual("TEACH_NODAL_REVIEW_EDIT_REQUIRES_REVERIFICATION", edited.Proposal?.CompilationCode);
            Assert.AreEqual(string.Empty, edited.Proposal?.SkillFingerprint);
            Assert.IsFalse(edited.Proposal?.Steps.Single().Verified ?? true);
            Assert.AreEqual(1, saved.Proposal?.Version);
            StringAssert.Contains(html, "data-execution-authority=\"false\"");
            StringAssert.Contains(html, "data-product-authority=\"false\"");
            StringAssert.Contains(html, "data-replay-enabled=\"false\"");
        }
        finally
        {
            DeleteRoot(root);
        }
    }

    [TestMethod]
    public async Task CaptureCancellationFailsClosedImmediately()
    {
        var root = NewRoot();
        var delayCalls = 0;
        try
        {
            Task Delay(TimeSpan _, CancellationToken __)
            {
                var call = Interlocked.Increment(ref delayCalls);
                return call == 1
                    ? Task.CompletedTask
                    : Task.FromCanceled(new CancellationToken(canceled: true));
            }

            var service = Service(root, SaveSnapshots(), Delay);
            await service.BindAsync(
                new NodalOsTeachNodalBindRequest("Cancelled capture", "fixture-editor"),
                CancellationToken.None);

            await Assert.ThrowsExactlyAsync<TaskCanceledException>(() => CaptureSaveStep(service, "Save the fixture document."));
            var snapshot = service.GetSnapshot();

            Assert.AreEqual(NodalOsTeachNodalProductState.FailedClosed, snapshot.State);
            Assert.IsFalse(snapshot.Bound);
            Assert.IsNull(snapshot.Proposal);
        }
        finally
        {
            DeleteRoot(root);
        }
    }

    [TestMethod]
    public async Task DiscardWaitsForInFlightCaptureAndCannotBeResurrected()
    {
        var root = NewRoot();
        var delayCalls = 0;
        var captureDelayStarted = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var releaseCapture = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        try
        {
            async Task Delay(TimeSpan _, CancellationToken cancellationToken)
            {
                var call = Interlocked.Increment(ref delayCalls);
                if (call == 2)
                {
                    captureDelayStarted.TrySetResult();
                    await releaseCapture.Task.WaitAsync(cancellationToken);
                }
            }

            var service = Service(root, SaveSnapshots(), Delay);
            await service.BindAsync(
                new NodalOsTeachNodalBindRequest("Serialized discard", "fixture-editor"),
                CancellationToken.None);

            var captureTask = CaptureSaveStep(service, "Save the fixture document.");
            await captureDelayStarted.Task;
            var discardTask = service.DiscardAsync(CancellationToken.None);
            Assert.IsFalse(discardTask.IsCompleted);

            releaseCapture.TrySetResult();
            var captured = await captureTask;
            var discarded = await discardTask;

            Assert.AreEqual(NodalOsTeachNodalProductState.Bound, captured.State);
            Assert.AreEqual(NodalOsTeachNodalProductState.Empty, discarded.State);
            Assert.AreEqual(NodalOsTeachNodalProductState.Empty, service.GetSnapshot().State);
            Assert.IsFalse(service.GetSnapshot().Bound);
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

    [TestMethod]
    public void RendererIncludesStableReviewConcurrencyMetadata()
    {
        var proposal = Proposal("metadata-draft", 3, DateTimeOffset.Parse("2026-07-23T10:11:12.1234567+00:00"));
        var snapshot = EmptySnapshot(NodalOsTeachNodalProductState.ReviewReady, []) with { Proposal = proposal };
        var html = NodalOsTeachNodalProductHtmlRenderer.Render(snapshot, new string('d', 64));

        StringAssert.Contains(html, "name=\"proposalVersion\" value=\"3\"");
        StringAssert.Contains(html, "name=\"proposalUpdatedAtUtc\" value=\"2026-07-23T10:11:12.1234567+00:00\"");
    }

    [TestMethod]
    public void ApiSerializationEmitsStateAsString()
    {
        var options = new JsonSerializerOptions(JsonSerializerDefaults.Web);
        options.Converters.Add(new JsonStringEnumConverter());
        var json = JsonSerializer.Serialize(EmptySnapshot(NodalOsTeachNodalProductState.Empty, []), options);

        StringAssert.Contains(json, "\"state\":\"Empty\"");
        Assert.IsFalse(json.Contains("\"state\":0", StringComparison.Ordinal));
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
        IEnumerable<CognitiveSnapshot> snapshots,
        Func<TimeSpan, CancellationToken, Task>? delay = null)
    {
        var queue = new Queue<CognitiveSnapshot>(snapshots);
        return new NodalOsTeachNodalProductService(
            root,
            (_, _) => queue.Count == 0 ? null : queue.Dequeue(),
            () => Hwnd,
            delay ?? ((_, _) => Task.CompletedTask));
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

    private static CognitiveSnapshot[] InputSnapshots() =>
    [
        InputSnapshot("Fixture Editor", "empty"),
        InputSnapshot("Fixture Editor", "empty"),
        InputSnapshot("Fixture Editor — Updated", "updated")
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

    private static CognitiveSnapshot SnapshotWithSaveLabel(
        string title,
        string documentState,
        string saveLabel) => new(
        new WindowSnapshot(
            Title: title,
            ProcessName: "fixture-editor",
            ProcessId: 101,
            Bounds: new WindowBounds(0, 0, 1280, 720),
            IsForeground: true),
        [
            Element("document-state", "Document", documentState, "document-runtime"),
            Element("save-button", "Button", saveLabel, "save-runtime")
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

    private static NodalOsTeachNodalProductProposal ReadDraft(string root)
    {
        var path = Directory.GetFiles(root, "*.json").Single();
        return JsonSerializer.Deserialize<NodalOsTeachNodalProductProposal>(
            File.ReadAllText(path),
            new JsonSerializerOptions(JsonSerializerDefaults.Web))!;
    }

    private static NodalOsTeachNodalProductProposal Proposal(
        string draftId,
        int version,
        DateTimeOffset updatedAtUtc) =>
        new(
            draftId,
            version,
            null,
            NodalOsTeachNodalProposalKind.NewSkill,
            "Metadata proposal",
            "Summary",
            "fixture-editor",
            "app.fixture-editor",
            "fixture-editor",
            TeachNodalCompilationDecision.DraftNeedsReview.ToString(),
            "TEST",
            string.Empty,
            [
                new NodalOsTeachNodalProductStepSnapshot(
                    "step-01-click",
                    "Click",
                    "Save the document.",
                    "Save",
                    "Button",
                    [],
                    "before",
                    "after",
                    true,
                    true,
                    "evidence:test")
            ],
            [],
            updatedAtUtc,
            updatedAtUtc,
            true,
            true,
            false,
            false,
            false,
            false,
            false,
            false,
            false);
}
