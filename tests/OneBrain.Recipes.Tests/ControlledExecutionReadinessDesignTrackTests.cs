using Microsoft.VisualStudio.TestTools.UnitTesting;
using OneBrain.Core.Approval;

namespace OneBrain.Recipes.Tests;

[TestClass]
[TestCategory("ApprovalHumanReview")]
[TestCategory("PhaseEApprovalHumanReview")]
public sealed class ControlledExecutionReadinessDesignTrackTests
{
    [TestMethod]
    public void ControlledExecutionReadinessDesignTrack_IsDeterministicDesignOnlyAndReadOnly()
    {
        var first = ControlledExecutionReadinessDesignTrackPresenter.CreateFixture();
        var second = ControlledExecutionReadinessDesignTrackPresenter.CreateFixture();

        Assert.AreEqual("nodal-os.controlled-execution-readiness.design-track.fixture.v1", first.TrackId);
        Assert.AreEqual(ControlledExecutionDesignStatus.DesignOnly, first.Status);
        Assert.AreEqual("DESIGN_ONLY_READ_ONLY_NO_EXECUTION_NO_MUTATION_NO_RUNTIME", first.Mode);
        Assert.AreEqual(first.TrackId, second.TrackId);
        Assert.AreEqual(first.Mode, second.Mode);
        CollectionAssert.AreEqual(first.StateMachine.Transitions.Select(transition => transition.TransitionId).ToArray(), second.StateMachine.Transitions.Select(transition => transition.TransitionId).ToArray());
        CollectionAssert.AreEqual(first.ProductActionControls.Select(control => control.ControlId).ToArray(), second.ProductActionControls.Select(control => control.ControlId).ToArray());
        Assert.IsTrue(first.NoSideEffectProof.ReadOnly);
        Assert.IsTrue(first.NoSideEffectProof.Deterministic);
        Assert.IsTrue(first.NoSideEffectProof.FixtureSafe);
    }

    [TestMethod]
    public void ControlledExecutionReadinessDesignTrack_CoversExpectedMacroTrackAreas()
    {
        var track = ControlledExecutionReadinessDesignTrackPresenter.CreateFixture();

        Assert.AreEqual(16, track.StateMachine.States.Count);
        Assert.AreEqual(15, track.StateMachine.Transitions.Count);
        Assert.AreEqual(9, track.MutationBoundary.MutationCandidates.Count);
        Assert.AreEqual(6, track.WriterPolicyBoundary.Contracts.Count);
        Assert.AreEqual(9, track.DurableAuditTrail.EventTypes.Count);
        Assert.AreEqual(6, track.PhysicalExportPolicy.ExportTargetsFuture.Count);
        Assert.AreEqual(7, track.ProductActionControls.Count);
        Assert.AreEqual(12, track.RuntimeReadinessGates.Count);
        Assert.AreEqual(1, track.NegativeCapabilities.Count);
        Assert.IsTrue(track.CurrentApprovalReadOnlyDesignMap.Count >= 5);
        Assert.IsTrue(track.FutureProtectedDebt.Count >= 9);
    }

    [TestMethod]
    public void StateMachine_SeparatesConceptualStatesFromFutureNotImplementedStates()
    {
        var transitions = ControlledExecutionReadinessDesignTrackPresenter.CreateFixture().StateMachine.Transitions;

        Assert.IsTrue(transitions.Any(transition => transition.To == ApprovalExecutionConceptualState.ApprovedConceptual));
        Assert.IsTrue(transitions.Any(transition => transition.To == ApprovalExecutionConceptualState.RejectedConceptual));
        Assert.IsTrue(transitions.Any(transition => transition.To == ApprovalExecutionConceptualState.ExpiredConceptual));
        Assert.IsTrue(transitions.Any(transition => transition.To == ApprovalExecutionConceptualState.InvalidatedConceptual));
        Assert.IsTrue(transitions.Any(transition => transition.To == ApprovalExecutionConceptualState.SupersededConceptual));
        Assert.IsTrue(transitions.Where(transition => transition.To == ApprovalExecutionConceptualState.ExecutionEligibleFuture).All(transition => transition.FutureStateNotImplemented));
        Assert.IsTrue(transitions.Where(transition => transition.To == ApprovalExecutionConceptualState.ExecutionStartedFuture).All(transition => transition.FutureStateNotImplemented));
        Assert.IsTrue(transitions.Where(transition => transition.To == ApprovalExecutionConceptualState.ExecutionCompletedFuture).All(transition => transition.FutureStateNotImplemented));
        Assert.IsTrue(transitions.Where(transition => transition.To == ApprovalExecutionConceptualState.RollbackRequiredFuture).All(transition => transition.FutureStateNotImplemented));
        Assert.IsTrue(transitions.All(transition => transition.RequiredEvidence.Count > 0));
        Assert.IsTrue(transitions.All(transition => transition.RequiredHumanApproval.Count > 0));
        Assert.IsTrue(transitions.All(transition => transition.BlockedReasons.Count > 0));
    }

    [TestMethod]
    public void MutationBoundary_ModelsExpectedMutationCandidatesAsBlockedOnly()
    {
        var candidates = ControlledExecutionReadinessDesignTrackPresenter.CreateFixture().MutationBoundary.MutationCandidates;
        var kinds = candidates.Select(candidate => candidate.Kind).ToHashSet();

        CollectionAssert.AreEquivalent(Enum.GetValues<ApprovalMutationCandidateKind>(), kinds.ToArray());
        Assert.IsTrue(candidates.All(candidate => candidate.Status == ControlledExecutionDesignStatus.Blocked));
        Assert.IsTrue(candidates.All(candidate => candidate.BlockedReasons.Contains("no durable audit trail")));
        Assert.IsTrue(candidates.All(candidate => candidate.BlockedReasons.Contains("no writer/policy integration")));
        Assert.IsTrue(candidates.All(candidate => candidate.BlockedReasons.Contains("no state store")));
    }

    [TestMethod]
    public void WriterPolicyBoundary_PreventsApprovalLaunderingAndPolicyBypass()
    {
        var boundary = ControlledExecutionReadinessDesignTrackPresenter.CreateFixture().WriterPolicyBoundary;
        var text = string.Join("\n", boundary.ConceptualFlow, string.Join("\n", boundary.Rules), string.Join("\n", boundary.Risks));

        StringAssert.Contains(text, "Approval preview -> Human review -> Policy gate -> Writer candidate -> Execution future");
        StringAssert.Contains(text, "Approval never equals execution.");
        StringAssert.Contains(text, "Approval never skips policy.");
        StringAssert.Contains(text, "Policy preview cannot dispatch commands or write state.");
        StringAssert.Contains(text, "Writer candidate cannot register services or bind command handlers.");
        StringAssert.Contains(text, "Approval cannot bypass policy or runtime gates.");
        StringAssert.Contains(text, "approval laundering");
        StringAssert.Contains(text, "stale approval");
        StringAssert.Contains(text, "policy mismatch");
        Assert.IsFalse(boundary.ApprovalImpliesExecution);
        Assert.IsFalse(boundary.PolicyPreviewCanWrite);
        Assert.IsFalse(boundary.WriterCandidateCanRun);
        Assert.IsFalse(boundary.ApprovalCanBypassPolicy);
        Assert.IsFalse(boundary.ProductivePolicyPathAvailable);
        Assert.IsFalse(boundary.WriterInvoked);
        Assert.IsFalse(boundary.ServiceRegistered);
        Assert.IsFalse(boundary.CommandHandlerRegistered);
        Assert.IsTrue(boundary.ExecutionBlocked);
    }

    [TestMethod]
    public void DurableAuditTrailAndPhysicalExportPolicy_AreFutureContractsOnly()
    {
        var track = ControlledExecutionReadinessDesignTrackPresenter.CreateFixture();

        StringAssert.Contains(string.Join("\n", track.DurableAuditTrail.RequiredBeforeRealMutation), "storage policy");
        StringAssert.Contains(string.Join("\n", track.DurableAuditTrail.RequiredBeforeRealMutation), "replay protection");
        StringAssert.Contains(string.Join("\n", track.PhysicalExportPolicy.ExportBlockers), "no redaction proof");
        StringAssert.Contains(string.Join("\n", track.PhysicalExportPolicy.ExportBlockers), "no durable audit trail");
        Assert.IsFalse(track.DurableAuditTrail.DurableTrailImplemented);
        Assert.IsFalse(track.PhysicalExportPolicy.PhysicalFileCreated);
        Assert.IsFalse(track.PhysicalExportPolicy.ClipboardUsed);
        Assert.IsFalse(track.PhysicalExportPolicy.DownloadStarted);
    }

    [TestMethod]
    public void ProductControlsAndRuntimeGates_RemainDisabledAndBlocked()
    {
        var track = ControlledExecutionReadinessDesignTrackPresenter.CreateFixture();

        Assert.IsTrue(track.ProductActionControls.All(control => control.Status == ControlledExecutionDesignStatus.Blocked));
        Assert.IsTrue(track.ProductActionControls.All(control => control.DisabledReasons.Contains("design-only")));
        Assert.IsTrue(track.ProductActionControls.All(control => control.DisabledReasons.Contains("no runtime")));
        Assert.IsTrue(track.ProductActionControls.All(control => control.FutureEnablementChecklist.Contains("explicit user approval")));
        Assert.IsTrue(track.RuntimeReadinessGates.All(gate => gate.BlocksRuntime));
        Assert.IsTrue(track.RuntimeReadinessGates.All(gate => !gate.AllowsRuntimeLive));
        Assert.IsTrue(track.RuntimeReadinessGates.All(gate => !gate.AllowsReleaseCommercial));
        CollectionAssert.AreEquivalent(Enum.GetValues<ControlledExecutionReadinessGateCategory>(), track.RuntimeReadinessGates.Select(gate => gate.Category).ToArray());
    }

    [TestMethod]
    public void ControlledExecutionReadinessDesignTrack_ReportsNextExternalAuditAndNoOverclaim()
    {
        var track = ControlledExecutionReadinessDesignTrackPresenter.CreateFixture();
        var text = string.Join(
            "\n",
            track.NextSafeStep,
            string.Join("\n", track.CurrentApprovalReadOnlyDesignMap),
            string.Join("\n", track.FutureProtectedDebt),
            string.Join("\n", track.Warnings));

        Assert.AreEqual("NODAL_OS_CONTROLLED_EXECUTION_READINESS_DESIGN_EXTERNAL_AUDIT", track.NextSafeStep);
        StringAssert.Contains(text, "ApprovalPacketReadOnlySurface");
        StringAssert.Contains(text, "HumanReviewPacketExportReadOnlyPreview");
        StringAssert.Contains(text, "ApprovalExecutionDesignOnlyProtected");
        StringAssert.Contains(text, "Design readiness may increase. Runtime readiness remains 0%.");
        Assert.IsFalse(text.Contains("production" + "-ready", StringComparison.OrdinalIgnoreCase));
        Assert.IsFalse(text.Contains("release" + "-ready", StringComparison.OrdinalIgnoreCase));
        Assert.IsFalse(text.Contains("approval executed", StringComparison.OrdinalIgnoreCase));
        Assert.IsFalse(text.Contains("state mutation completed", StringComparison.OrdinalIgnoreCase));
        Assert.IsFalse(text.Contains("runtime enabled", StringComparison.OrdinalIgnoreCase));
        Assert.IsFalse(text.Contains("physical export enabled", StringComparison.OrdinalIgnoreCase));
    }
}
