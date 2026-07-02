using Microsoft.VisualStudio.TestTools.UnitTesting;
using OneBrain.Core.Approval;

namespace OneBrain.Safety.Tests;

[TestClass]
[TestCategory("ApprovalHumanReview")]
[TestCategory("PhaseEApprovalHumanReview")]
public sealed class ApprovalMutationStoreDesignOnlyProtectedSafetyTests
{
    [TestMethod]
    public void MutationStoreDesign_KeepsAllImplementationReadinessAtZero()
    {
        var design = ApprovalMutationStoreDesignOnlyProtectedPresenter.CreateFixture();

        Assert.AreEqual(0, design.Readiness.ImplementationReadinessPercent);
        Assert.AreEqual(0, design.Readiness.RuntimeReadinessPercent);
        Assert.AreEqual(0, design.Readiness.DurableStoreReadinessPercent);
        Assert.AreEqual(0, design.Readiness.DatabaseReadinessPercent);
        Assert.AreEqual(0, design.Readiness.FilesystemWriteReadinessPercent);
        Assert.IsTrue(design.Readiness.KeepsImplementationBlocked);
        Assert.IsTrue(design.PassesSafetyProof);
    }

    [TestMethod]
    public void MutationStoreDesign_HasNoRealStoreRepositoryDbFilesystemServiceCommandOrRuntime()
    {
        var design = ApprovalMutationStoreDesignOnlyProtectedPresenter.CreateFixture();

        Assert.IsFalse(design.HasRealStore);
        Assert.IsFalse(design.HasRepository);
        Assert.IsFalse(design.HasDatabase);
        Assert.IsFalse(design.HasFilesystemWrite);
        Assert.IsFalse(design.HasRuntimeLive);
        Assert.IsFalse(design.HasServiceRegistration);
        Assert.IsFalse(design.HasCommandHandler);
        Assert.IsFalse(design.CapabilityStatus.HasDurableAuditTrail);
        Assert.IsFalse(design.CapabilityStatus.CanPersistMutation);
        Assert.IsFalse(design.CapabilityStatus.CanReplayMutation);
        Assert.IsFalse(design.CapabilityStatus.CanCommitMutation);
    }

    [TestMethod]
    public void AttemptAndRecordPreviews_CannotMutateOrPersistApprovalState()
    {
        var design = ApprovalMutationStoreDesignOnlyProtectedPresenter.CreateFixture();

        Assert.AreEqual(9, design.AttemptPreviews.Count);
        Assert.AreEqual(9, design.RecordPreviews.Count);
        Assert.AreEqual(0, design.StateMutationCount);
        Assert.IsFalse(design.CanMutate);
        Assert.IsTrue(design.AttemptPreviews.All(attempt => attempt.IsPreviewOnly));
        Assert.IsTrue(design.AttemptPreviews.All(attempt => !attempt.CanMutate));
        Assert.IsTrue(design.AttemptPreviews.All(attempt => attempt.BlockedReasons.Count >= 10));
        Assert.IsTrue(design.RecordPreviews.All(record => !record.IsDurable));
        Assert.IsTrue(design.RecordPreviews.All(record => !record.IsPersisted));
        CollectionAssert.AreEquivalent(Enum.GetValues<ApprovalMutationPreviewKind>(), design.AttemptPreviews.Select(attempt => attempt.RequestedTransition).ToArray());
        CollectionAssert.AreEquivalent(Enum.GetValues<ApprovalMutationPreviewKind>(), design.RecordPreviews.Select(record => record.MutationKind).ToArray());
    }

    [TestMethod]
    public void ActorBoundary_BlocksAnonymousAutomationAndServiceActorsWithoutIdentityProvider()
    {
        var design = ApprovalMutationStoreDesignOnlyProtectedPresenter.CreateFixture();

        Assert.IsTrue(design.ActorBoundaries.All(boundary => !boundary.ServiceActorAllowed));
        Assert.IsTrue(design.ActorBoundaries.All(boundary => !boundary.AnonymousActorAllowed));
        Assert.IsTrue(design.ActorBoundaries.All(boundary => !boundary.AutomationActorAllowed));
        Assert.IsTrue(design.ActorBoundaries.All(boundary => boundary.RequiresIdentityProofFuture));
        Assert.IsTrue(design.ActorBoundaries.All(boundary => !boundary.HasIdentityProvider));
        Assert.IsTrue(design.ActorBoundaries.Any(boundary => boundary.ActorKind == ApprovalMutationActorKind.AutomationActorBlocked));
        Assert.IsTrue(design.ActorBoundaries.Any(boundary => boundary.ActorKind == ApprovalMutationActorKind.UnknownBlocked));
    }

    [TestMethod]
    public void StalenessInvalidationSupersedingReplayAndConcurrency_RemainConceptualOnly()
    {
        var design = ApprovalMutationStoreDesignOnlyProtectedPresenter.CreateFixture();

        Assert.IsTrue(design.StalenessDesign.ContextHashRequiredFuture);
        Assert.IsTrue(design.StalenessDesign.EvidenceSnapshotRequiredFuture);
        Assert.IsTrue(design.StalenessDesign.PolicyVersionRequiredFuture);
        Assert.IsTrue(design.StalenessDesign.TargetFingerprintRequiredFuture);
        Assert.IsFalse(design.StalenessDesign.PerformsContextScanNow);
        Assert.IsFalse(design.StalenessDesign.ComputesFilesystemHashNow);
        Assert.IsFalse(design.InvalidationDesign.CanInvalidateNow);
        Assert.IsFalse(design.InvalidationDesign.UpdatesStateNow);
        CollectionAssert.AreEquivalent(Enum.GetValues<ApprovalMutationInvalidationReason>(), design.InvalidationDesign.InvalidationReasons.ToArray());
        Assert.IsTrue(design.SupersedingDesign.RequiresAuditTrailFuture);
        Assert.IsFalse(design.SupersedingDesign.CanSupersedeNow);
        Assert.IsTrue(design.ReplayProtectionDesign.MutationNonceRequiredFuture);
        Assert.IsTrue(design.ReplayProtectionDesign.IdempotencyKeyRequiredFuture);
        Assert.IsFalse(design.ReplayProtectionDesign.CanReplayNow);
        Assert.IsTrue(design.ConcurrencyModelDesign.ConcurrentMutationBlocked);
        Assert.IsFalse(design.ConcurrencyModelDesign.LastWriterWinsAllowed);
        Assert.IsFalse(design.ConcurrencyModelDesign.HasLocksNow);
        Assert.IsFalse(design.ConcurrencyModelDesign.HasTransactionsNow);
        Assert.IsFalse(design.ConcurrencyModelDesign.HasMutableGlobalStateNow);
        Assert.IsFalse(design.IdempotencyDesign.IdempotencyStoreImplemented);
    }

    [TestMethod]
    public void EvidenceAndAuditRequirements_DoNotPersistWriteExportOrRunRedaction()
    {
        var evidence = ApprovalMutationStoreDesignOnlyProtectedPresenter.CreateFixture().EvidenceRequirementDesign;

        Assert.IsTrue(evidence.RequiredFutureEvidence.Contains("original approval packet"));
        Assert.IsTrue(evidence.RequiredFutureEvidence.Contains("human review packet"));
        Assert.IsTrue(evidence.RequiredFutureEvidence.Contains("policy decision"));
        Assert.IsTrue(evidence.RequiredFutureEvidence.Contains("actor identity proof"));
        Assert.IsTrue(evidence.RequiredFutureEvidence.Contains("runtime readiness gate"));
        Assert.IsTrue(evidence.EvidenceBlockers.Contains("MissingDurableAuditTrail"));
        Assert.IsTrue(evidence.AuditRequirements.Contains("AppendOnlyFuture"));
        Assert.IsFalse(evidence.EvidencePersistedNow);
        Assert.IsFalse(evidence.AuditTrailWrittenNow);
        Assert.IsFalse(evidence.RedactionRuntimeAvailable);
        Assert.IsFalse(evidence.ExportAvailable);
    }

    [TestMethod]
    public void AntiCapabilityProof_AllMutationStoreCapabilitiesRemainBlocked()
    {
        var proof = ApprovalMutationStoreDesignOnlyProtectedPresenter.CreateFixture().AntiCapabilityProof;

        Assert.IsTrue(proof.Passes);
        Assert.IsTrue(proof.CannotPersistMutation);
        Assert.IsTrue(proof.CannotCommitMutation);
        Assert.IsTrue(proof.CannotReplayMutation);
        Assert.IsTrue(proof.CannotUpdateApprovalState);
        Assert.IsTrue(proof.CannotWriteAuditTrail);
        Assert.IsTrue(proof.CannotUseDatabase);
        Assert.IsTrue(proof.CannotUseFilesystem);
        Assert.IsTrue(proof.CannotRegisterService);
        Assert.IsTrue(proof.CannotCreateCommandHandler);
        Assert.IsTrue(proof.CannotInvokeWriter);
        Assert.IsTrue(proof.CannotInvokePolicyProductivePath);
        Assert.IsTrue(proof.CannotStartRuntime);
        Assert.IsTrue(proof.CannotExportPhysicalFile);
        Assert.IsTrue(proof.CannotUseClipboardDownload);
        Assert.IsTrue(proof.CannotUseProviderCloud);
        Assert.IsTrue(proof.CannotUseLlmLive);
        Assert.IsTrue(proof.CannotUseDurableMemory);
        Assert.IsTrue(proof.CannotUseBrowserCdp);
        Assert.IsTrue(proof.CannotUseWcuOcr);
        Assert.IsTrue(proof.CannotExecuteRecipe);
        Assert.IsTrue(proof.CannotClaimReleaseReady);
        Assert.IsTrue(proof.NoSideEffectProof.Passes);
        Assert.IsFalse(proof.NoSideEffectProof.ApprovalStateMutationAttempted);
        Assert.IsFalse(proof.NoSideEffectProof.RuntimeTouched);
    }

    [TestMethod]
    public void MutationStoreDesign_HasNoProductActionStateMutationOrExportCounts()
    {
        var design = ApprovalMutationStoreDesignOnlyProtectedPresenter.CreateFixture();

        Assert.AreEqual(0, design.ProductActionCount);
        Assert.AreEqual(0, design.StateMutationCount);
        Assert.AreEqual(0, design.ExportActionCount);
        Assert.IsFalse(design.EvidenceRequirementDesign.ExportAvailable);
        Assert.AreEqual("NODAL_OS_APPROVAL_MUTATION_STORE_DESIGN_EXTERNAL_AUDIT", design.NextSafeStep);
    }
}
