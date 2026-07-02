using Microsoft.VisualStudio.TestTools.UnitTesting;
using OneBrain.Core.Approval;

namespace OneBrain.Safety.Tests;

[TestClass]
[TestCategory("ApprovalHumanReview")]
[TestCategory("PhaseEApprovalHumanReview")]
public sealed class ControlledExecutionReadinessDesignTrackSafetyTests
{
    [TestMethod]
    public void ControlledExecutionReadinessDesignTrack_KeepsAllImplementationReadinessAtZero()
    {
        var track = ControlledExecutionReadinessDesignTrackPresenter.CreateFixture();

        Assert.AreEqual(0, track.Readiness.ApprovalExecutionImplementationReadinessPercent);
        Assert.AreEqual(0, track.Readiness.ApprovalStateMutationReadinessPercent);
        Assert.AreEqual(0, track.Readiness.RuntimeLiveReadinessPercent);
        Assert.AreEqual(0, track.Readiness.PhysicalExportReadinessPercent);
        Assert.IsFalse(track.Readiness.ReleaseCommercialReady);
        Assert.IsTrue(track.Readiness.KeepsImplementationBlocked);
    }

    [TestMethod]
    public void ControlledExecutionReadinessDesignTrack_HasNoExecutionMutationRuntimeExportOrProductActions()
    {
        var track = ControlledExecutionReadinessDesignTrackPresenter.CreateFixture();

        Assert.IsFalse(track.HasRealExecution);
        Assert.IsFalse(track.HasStateMutation);
        Assert.IsFalse(track.HasRuntimeLive);
        Assert.IsFalse(track.HasPhysicalExport);
        Assert.IsFalse(track.HasProductActions);
        Assert.AreEqual(0, track.ProductActionCount);
        Assert.AreEqual(0, track.StateMutationCount);
        Assert.AreEqual(0, track.ExportActionCount);
        Assert.IsTrue(track.NoSideEffectProof.Passes);
        Assert.IsFalse(track.NoSideEffectProof.ApprovalExecutionStarted);
        Assert.IsFalse(track.NoSideEffectProof.ApprovalStateMutationAttempted);
        Assert.IsFalse(track.NoSideEffectProof.RuntimeTouched);
        Assert.IsFalse(track.NoSideEffectProof.ProductActionExposed);
        Assert.IsFalse(track.NoSideEffectProof.ProductServiceRegistered);
    }

    [TestMethod]
    public void StateMachineTransitions_ArePreviewOnlyAndNeverExecuteOrMutate()
    {
        var stateMachine = ControlledExecutionReadinessDesignTrackPresenter.CreateFixture().StateMachine;

        Assert.IsTrue(stateMachine.PreviewOnly);
        Assert.IsFalse(stateMachine.HasExecution);
        Assert.IsFalse(stateMachine.HasMutation);
        Assert.IsFalse(stateMachine.HasRuntime);
        Assert.IsTrue(stateMachine.Transitions.All(transition => transition.PreviewOnly));
        Assert.IsTrue(stateMachine.Transitions.All(transition => !transition.ExecutesWork));
        Assert.IsTrue(stateMachine.Transitions.All(transition => !transition.MutatesState));
        Assert.IsTrue(stateMachine.Transitions.All(transition => !transition.StartsRuntime));
        Assert.IsTrue(stateMachine.Transitions.Where(transition => transition.To.ToString().EndsWith("Future", StringComparison.Ordinal)).All(transition => transition.FutureStateNotImplemented));
    }

    [TestMethod]
    public void MutationBoundary_BlocksEveryMutationCandidateWithoutStoreOrIo()
    {
        var boundary = ControlledExecutionReadinessDesignTrackPresenter.CreateFixture().MutationBoundary;

        Assert.IsFalse(boundary.HasMutationMethod);
        Assert.IsFalse(boundary.WritesState);
        Assert.IsFalse(boundary.UsesStore);
        Assert.IsTrue(boundary.MutationCandidates.All(candidate => candidate.Status == ControlledExecutionDesignStatus.Blocked));
        Assert.IsTrue(boundary.MutationCandidates.All(candidate => candidate.RequiresActor));
        Assert.IsTrue(boundary.MutationCandidates.All(candidate => candidate.RequiresTimestamp));
        Assert.IsTrue(boundary.MutationCandidates.All(candidate => candidate.RequiresEvidenceRefs));
        Assert.IsTrue(boundary.MutationCandidates.All(candidate => candidate.RequiresPolicyDecision));
        Assert.IsTrue(boundary.MutationCandidates.All(candidate => candidate.RequiresDurableAuditTrailFuture));
        Assert.IsTrue(boundary.MutationCandidates.All(candidate => !candidate.HasMutationMethod));
        Assert.IsTrue(boundary.MutationCandidates.All(candidate => !candidate.WritesState));
        Assert.IsTrue(boundary.MutationCandidates.All(candidate => !candidate.UsesRepository));
        Assert.IsTrue(boundary.MutationCandidates.All(candidate => !candidate.UsesDatabase));
        Assert.IsTrue(boundary.MutationCandidates.All(candidate => !candidate.UsesFilesystem));
    }

    [TestMethod]
    public void WriterPolicyAuditExportControlsAndRuntimeGates_RemainDisconnectedAndBlocked()
    {
        var track = ControlledExecutionReadinessDesignTrackPresenter.CreateFixture();

        Assert.IsFalse(track.WriterPolicyBoundary.ApprovalImpliesExecution);
        Assert.IsFalse(track.WriterPolicyBoundary.ProductivePolicyPathAvailable);
        Assert.IsFalse(track.WriterPolicyBoundary.WriterInvoked);
        Assert.IsFalse(track.WriterPolicyBoundary.PolicyPreviewCanWrite);
        Assert.IsFalse(track.WriterPolicyBoundary.WriterCandidateCanRun);
        Assert.IsFalse(track.WriterPolicyBoundary.ApprovalCanBypassPolicy);
        Assert.IsFalse(track.WriterPolicyBoundary.ServiceRegistered);
        Assert.IsFalse(track.WriterPolicyBoundary.CommandHandlerRegistered);
        Assert.IsTrue(track.WriterPolicyBoundary.ExecutionBlocked);
        Assert.IsFalse(track.DurableAuditTrail.DurableTrailImplemented);
        Assert.IsFalse(track.DurableAuditTrail.DatabaseUsed);
        Assert.IsFalse(track.DurableAuditTrail.FilesystemWriteUsed);
        Assert.IsFalse(track.DurableAuditTrail.MigrationRunnerUsed);
        Assert.IsFalse(track.DurableAuditTrail.CloudLogUsed);
        Assert.IsFalse(track.PhysicalExportPolicy.PhysicalFileCreated);
        Assert.IsFalse(track.PhysicalExportPolicy.ClipboardUsed);
        Assert.IsFalse(track.PhysicalExportPolicy.DownloadStarted);
        Assert.IsFalse(track.PhysicalExportPolicy.StreamWritten);
        Assert.IsFalse(track.PhysicalExportPolicy.FilesystemWritten);
        Assert.IsFalse(track.PhysicalExportPolicy.ExternalProcessStarted);
        Assert.IsTrue(track.ProductActionControls.All(control => control.PreviewOnly));
        Assert.IsTrue(track.ProductActionControls.All(control => !control.EnabledNow));
        Assert.IsTrue(track.ProductActionControls.All(control => !control.CommandBindingAvailable));
        Assert.IsTrue(track.ProductActionControls.All(control => !control.ServiceRouteAvailable));
        Assert.IsTrue(track.ProductActionControls.All(control => !control.HandlerAvailable));
        Assert.IsTrue(track.RuntimeGateBlocked);
        Assert.IsTrue(track.RuntimeReadinessGates.All(gate => gate.Status == ControlledExecutionDesignStatus.Blocked));
        Assert.IsTrue(track.RuntimeReadinessGates.All(gate => !gate.AllowsReleaseCommercial));
    }

    [TestMethod]
    public void NegativeCapabilityContracts_AllPass()
    {
        var track = ControlledExecutionReadinessDesignTrackPresenter.CreateFixture();

        Assert.IsTrue(track.PassesNegativeCapabilities);
        Assert.IsTrue(track.NegativeCapabilities.All(contract => contract.CannotExecuteApproval));
        Assert.IsTrue(track.NegativeCapabilities.All(contract => contract.CannotMutateApprovalState));
        Assert.IsTrue(track.NegativeCapabilities.All(contract => contract.CannotInvokeWriter));
        Assert.IsTrue(track.NegativeCapabilities.All(contract => contract.CannotInvokePolicyProductivePath));
        Assert.IsTrue(track.NegativeCapabilities.All(contract => contract.CannotStartRuntime));
        Assert.IsTrue(track.NegativeCapabilities.All(contract => contract.CannotRegisterService));
        Assert.IsTrue(track.NegativeCapabilities.All(contract => contract.CannotCreateCommandHandler));
        Assert.IsTrue(track.NegativeCapabilities.All(contract => contract.CannotWriteFilesystem));
        Assert.IsTrue(track.NegativeCapabilities.All(contract => contract.CannotUseDatabase));
        Assert.IsTrue(track.NegativeCapabilities.All(contract => contract.CannotUseProviderCloud));
        Assert.IsTrue(track.NegativeCapabilities.All(contract => contract.CannotUseLlmLive));
        Assert.IsTrue(track.NegativeCapabilities.All(contract => contract.CannotUseVectorBackend));
        Assert.IsTrue(track.NegativeCapabilities.All(contract => contract.CannotUseDurableMemory));
        Assert.IsTrue(track.NegativeCapabilities.All(contract => contract.CannotUseBrowserCdp));
        Assert.IsTrue(track.NegativeCapabilities.All(contract => contract.CannotUseWcuOcr));
        Assert.IsTrue(track.NegativeCapabilities.All(contract => contract.CannotExecuteRecipe));
        Assert.IsTrue(track.NegativeCapabilities.All(contract => contract.CannotExportPhysicalFile));
        Assert.IsTrue(track.NegativeCapabilities.All(contract => contract.CannotUseClipboardDownload));
        Assert.IsTrue(track.NegativeCapabilities.All(contract => contract.CannotClaimReleaseCommercialReady));
    }

    [TestMethod]
    public void ControlledExecutionReadinessDesignTrack_TextHasNoActiveReadinessOverclaim()
    {
        var track = ControlledExecutionReadinessDesignTrackPresenter.CreateFixture();
        var text = string.Join(
            "\n",
            track.Title,
            track.Mode,
            string.Join("\n", track.Warnings),
            string.Join("\n", track.FutureProtectedDebt),
            string.Join("\n", track.ProductActionControls.SelectMany(control => control.DisabledReasons)),
            string.Join("\n", track.RuntimeReadinessGates.SelectMany(gate => gate.MissingRequirements)));

        var forbidden = new[]
        {
            "production" + "-ready",
            "release" + "-ready",
            "approval executed",
            "approval execution completed",
            "approval state mutated",
            "state mutation completed",
            "physical export enabled",
            "runtime enabled",
            "live enabled",
            "provider enabled",
            "LLM live enabled",
            "durable memory active",
            "browser/CDP enabled"
        };

        foreach (var term in forbidden)
        {
            Assert.IsFalse(text.Contains(term, StringComparison.OrdinalIgnoreCase), term);
        }
    }
}
