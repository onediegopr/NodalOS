using Microsoft.VisualStudio.TestTools.UnitTesting;
using OneBrain.Core.Approval;

namespace OneBrain.Recipes.Tests;

[TestClass]
[TestCategory("ApprovalHumanReview")]
[TestCategory("PhaseEApprovalHumanReview")]
public sealed class ApprovalMutationStoreDesignOnlyProtectedTests
{
    [TestMethod]
    public void MutationStoreDesign_IsDeterministicDesignOnlyAndReadOnly()
    {
        var first = ApprovalMutationStoreDesignOnlyProtectedPresenter.CreateFixture();
        var second = ApprovalMutationStoreDesignOnlyProtectedPresenter.CreateFixture();

        Assert.AreEqual("nodal-os.approval.mutation-store.design-only.protected.fixture.v1", first.DesignId);
        Assert.AreEqual(ApprovalMutationStoreDesignStatus.DesignOnly, first.Status);
        Assert.AreEqual("DESIGN_ONLY_READ_ONLY_PREVIEW_NO_STORE_NO_MUTATION_NO_RUNTIME", first.Mode);
        Assert.AreEqual(first.DesignId, second.DesignId);
        Assert.AreEqual(first.Mode, second.Mode);
        CollectionAssert.AreEqual(first.AttemptPreviews.Select(attempt => attempt.AttemptId).ToArray(), second.AttemptPreviews.Select(attempt => attempt.AttemptId).ToArray());
        CollectionAssert.AreEqual(first.RecordPreviews.Select(record => record.RecordIdPreview).ToArray(), second.RecordPreviews.Select(record => record.RecordIdPreview).ToArray());
        Assert.IsTrue(first.AntiCapabilityProof.NoSideEffectProof.ReadOnly);
        Assert.IsTrue(first.AntiCapabilityProof.NoSideEffectProof.Deterministic);
        Assert.IsTrue(first.AntiCapabilityProof.NoSideEffectProof.FixtureSafe);
    }

    [TestMethod]
    public void MutationStoreDesign_CoversRequiredStoreDesignAreas()
    {
        var design = ApprovalMutationStoreDesignOnlyProtectedPresenter.CreateFixture();

        Assert.AreEqual(12, design.BlockedReasons.Count);
        Assert.AreEqual(9, design.AttemptPreviews.Count);
        Assert.AreEqual(9, design.RecordPreviews.Count);
        Assert.AreEqual(6, design.ActorBoundaries.Count);
        Assert.AreEqual(10, design.InvalidationDesign.InvalidationReasons.Count);
        Assert.AreEqual(13, design.EvidenceRequirementDesign.RequiredFutureEvidence.Count);
        Assert.AreEqual(8, design.EvidenceRequirementDesign.EvidenceBlockers.Count);
        Assert.AreEqual(6, design.EvidenceRequirementDesign.AuditRequirements.Count);
        Assert.IsTrue(design.FutureImplementationChecklist.RequiredBeforeRealStore.Count >= 9);
        Assert.IsTrue(design.FutureImplementationChecklist.RequiredExternalAudits.Count >= 5);
        Assert.IsTrue(design.CurrentMutationBoundaryBaseline.Count >= 5);
    }

    [TestMethod]
    public void MutationStoreDesign_MapsAllMutationKindsToBlockedAttemptsAndRecords()
    {
        var design = ApprovalMutationStoreDesignOnlyProtectedPresenter.CreateFixture();

        CollectionAssert.AreEquivalent(Enum.GetValues<ApprovalMutationPreviewKind>(), design.AttemptPreviews.Select(attempt => attempt.RequestedTransition).ToArray());
        CollectionAssert.AreEquivalent(Enum.GetValues<ApprovalMutationPreviewKind>(), design.RecordPreviews.Select(record => record.MutationKind).ToArray());
        Assert.IsTrue(design.AttemptPreviews.All(attempt => attempt.BlockedReasons.Contains(ApprovalMutationBlockedReason.NoDurableAuditTrail)));
        Assert.IsTrue(design.AttemptPreviews.All(attempt => attempt.BlockedReasons.Contains(ApprovalMutationBlockedReason.NoMutationPolicy)));
        Assert.IsTrue(design.AttemptPreviews.All(attempt => attempt.BlockedReasons.Contains(ApprovalMutationBlockedReason.NoStateStore)));
        Assert.IsTrue(design.RecordPreviews.All(record => record.AuditTrailRequirement.Contains("required", StringComparison.OrdinalIgnoreCase)));
        Assert.IsTrue(design.RecordPreviews.All(record => record.InvalidationRequirement.Contains("required", StringComparison.OrdinalIgnoreCase)));
        Assert.IsTrue(design.RecordPreviews.All(record => record.ReplayProtectionRequirement.Contains("required", StringComparison.OrdinalIgnoreCase)));
    }

    [TestMethod]
    public void ActorIdentityBoundary_DocumentsFutureHumanActorWithoutAuthImplementation()
    {
        var design = ApprovalMutationStoreDesignOnlyProtectedPresenter.CreateFixture();
        var humanActors = design.ActorBoundaries.Where(boundary =>
            boundary.ActorKind is ApprovalMutationActorKind.HumanReviewerFuture
                or ApprovalMutationActorKind.OwnerFuture
                or ApprovalMutationActorKind.AuditorFuture).ToList();

        Assert.AreEqual(3, humanActors.Count);
        Assert.IsTrue(humanActors.All(boundary => boundary.RequiredHumanActor));
        Assert.IsTrue(design.ActorBoundaries.All(boundary => !boundary.HasIdentityProvider));
        Assert.IsTrue(design.ActorBoundaries.All(boundary => boundary.PermissionBlockers.Contains(ApprovalMutationBlockedReason.NoActorIdentityBoundary)));
        Assert.IsFalse(design.ActorBoundaries.Single(boundary => boundary.ActorKind == ApprovalMutationActorKind.SystemObserverPreview).ServiceActorAllowed);
    }

    [TestMethod]
    public void StaleApprovalReplayConcurrencyAndIdempotency_BlockFutureMutation()
    {
        var design = ApprovalMutationStoreDesignOnlyProtectedPresenter.CreateFixture();

        Assert.IsTrue(design.StalenessDesign.CurrentContextMatchRequiredFuture);
        Assert.IsTrue(design.InvalidationDesign.InvalidationReasons.Contains(ApprovalMutationInvalidationReason.ContextChanged));
        Assert.IsTrue(design.InvalidationDesign.InvalidationReasons.Contains(ApprovalMutationInvalidationReason.PolicyVersionChanged));
        Assert.IsTrue(design.InvalidationDesign.InvalidationReasons.Contains(ApprovalMutationInvalidationReason.SupersededByNewReview));
        Assert.IsFalse(design.SupersedingDesign.CanSupersedeNow);
        Assert.IsTrue(design.ReplayProtectionDesign.PreviousEventHashRequiredFuture);
        Assert.IsTrue(design.ReplayProtectionDesign.ActorSessionBindingRequiredFuture);
        Assert.IsTrue(design.ConcurrencyModelDesign.ExpectedStateRequiredFuture);
        Assert.IsTrue(design.ConcurrencyModelDesign.CompareAndSwapRequiredFuture);
        Assert.IsTrue(design.ConcurrencyModelDesign.VersionTokenRequiredFuture);
        Assert.IsTrue(design.ConcurrencyModelDesign.RequiresDurableStoreFuture);
        Assert.IsTrue(design.IdempotencyDesign.DuplicateAttemptBlockedFuture.Contains("durable idempotency proof", StringComparison.OrdinalIgnoreCase));
    }

    [TestMethod]
    public void FutureImplementationChecklist_RequiresExternalAuditBeforeAnyRealStore()
    {
        var checklist = ApprovalMutationStoreDesignOnlyProtectedPresenter.CreateFixture().FutureImplementationChecklist;
        var text = string.Join(
            "\n",
            string.Join("\n", checklist.RequiredBeforeRealStore),
            string.Join("\n", checklist.RequiredExternalAudits),
            string.Join("\n", checklist.RequiredPolicyDecisions),
            string.Join("\n", checklist.ExplicitNonGoals));

        StringAssert.Contains(text, "mutation store design external audit");
        StringAssert.Contains(text, "durable audit trail design external audit");
        StringAssert.Contains(text, "actor identity and authority model");
        StringAssert.Contains(text, "concurrency and replay protection model");
        StringAssert.Contains(text, "no real store");
        StringAssert.Contains(text, "no database");
        StringAssert.Contains(text, "no runtime/live");
    }

    [TestMethod]
    public void MutationStoreDesign_ReportTextHasNoActiveReadinessOverclaim()
    {
        var design = ApprovalMutationStoreDesignOnlyProtectedPresenter.CreateFixture();
        var text = string.Join(
            "\n",
            design.Title,
            design.Mode,
            string.Join("\n", design.Warnings),
            string.Join("\n", design.FutureImplementationChecklist.ExplicitNonGoals),
            string.Join("\n", design.CurrentMutationBoundaryBaseline));

        Assert.IsFalse(text.Contains("production" + "-ready", StringComparison.OrdinalIgnoreCase));
        Assert.IsFalse(text.Contains("release" + "-ready", StringComparison.OrdinalIgnoreCase));
        Assert.IsFalse(text.Contains("mutation store implemented", StringComparison.OrdinalIgnoreCase));
        Assert.IsFalse(text.Contains("approval state mutated", StringComparison.OrdinalIgnoreCase));
        Assert.IsFalse(text.Contains("runtime enabled", StringComparison.OrdinalIgnoreCase));
        Assert.IsFalse(text.Contains("physical export enabled", StringComparison.OrdinalIgnoreCase));
        Assert.IsFalse(text.Contains("service registered", StringComparison.OrdinalIgnoreCase));
        Assert.IsFalse(text.Contains("command handler registered", StringComparison.OrdinalIgnoreCase));
    }
}
