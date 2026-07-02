using Microsoft.VisualStudio.TestTools.UnitTesting;
using OneBrain.Core.Approval;

namespace OneBrain.Recipes.Tests;

[TestClass]
[TestCategory("ApprovalHumanReview")]
[TestCategory("PhaseEApprovalHumanReview")]
public sealed class ApprovalDurableAuditTrailDesignOnlyProtectedTests
{
    [TestMethod]
    public void DurableAuditTrailDesign_IsDeterministicDesignOnlyAndReadOnly()
    {
        var first = ApprovalDurableAuditTrailDesignOnlyProtectedPresenter.CreateFixture();
        var second = ApprovalDurableAuditTrailDesignOnlyProtectedPresenter.CreateFixture();

        Assert.AreEqual("nodal-os.approval.durable-audit-trail.design-only.protected.fixture.v1", first.DesignId);
        Assert.AreEqual(ApprovalAuditTrailDesignStatus.DesignOnly, first.Status);
        Assert.AreEqual("DESIGN_ONLY_READ_ONLY_PREVIEW_NO_LEDGER_NO_STORAGE_NO_RUNTIME", first.Mode);
        Assert.AreEqual(first.DesignId, second.DesignId);
        Assert.AreEqual(first.Mode, second.Mode);
        CollectionAssert.AreEqual(first.EventPreviews.Select(auditEvent => auditEvent.EventIdPreview).ToArray(), second.EventPreviews.Select(auditEvent => auditEvent.EventIdPreview).ToArray());
        CollectionAssert.AreEqual(first.FieldRequirements.Select(field => field.FieldName).ToArray(), second.FieldRequirements.Select(field => field.FieldName).ToArray());
        Assert.IsTrue(first.AntiCapabilityProof.NoSideEffectProof.ReadOnly);
        Assert.IsTrue(first.AntiCapabilityProof.NoSideEffectProof.Deterministic);
        Assert.IsTrue(first.AntiCapabilityProof.NoSideEffectProof.FixtureSafe);
    }

    [TestMethod]
    public void DurableAuditTrailDesign_CoversExpectedEventAndFieldModel()
    {
        var design = ApprovalDurableAuditTrailDesignOnlyProtectedPresenter.CreateFixture();

        Assert.AreEqual(14, design.BlockedReasons.Count);
        Assert.AreEqual(15, design.EventPreviews.Count);
        Assert.AreEqual(20, design.FieldRequirements.Count);
        Assert.AreEqual(7, design.RetentionRequirements.Count);
        Assert.AreEqual(4, design.ExternalAuditRequirements.Count);
        Assert.IsTrue(design.FutureImplementationChecklist.RequiredBeforeRealAuditTrail.Count >= 9);
        Assert.IsTrue(design.FutureImplementationChecklist.RequiredExternalAudits.Count >= 5);
        Assert.IsTrue(design.CurrentApprovalAuditTrailBaseline.Count >= 5);
        CollectionAssert.AreEquivalent(Enum.GetValues<ApprovalAuditEventKind>(), design.EventPreviews.Select(auditEvent => auditEvent.EventKind).ToArray());
    }

    [TestMethod]
    public void AuditEventPreviews_CoverMutationPolicyRuntimeExportAndExternalAuditEvents()
    {
        var kinds = ApprovalDurableAuditTrailDesignOnlyProtectedPresenter.CreateFixture().EventPreviews.Select(auditEvent => auditEvent.EventKind).ToHashSet();

        Assert.IsTrue(kinds.Contains(ApprovalAuditEventKind.ApprovalProposedPreview));
        Assert.IsTrue(kinds.Contains(ApprovalAuditEventKind.HumanReviewedPreview));
        Assert.IsTrue(kinds.Contains(ApprovalAuditEventKind.MutationAttemptedFuture));
        Assert.IsTrue(kinds.Contains(ApprovalAuditEventKind.MutationBlockedFuture));
        Assert.IsTrue(kinds.Contains(ApprovalAuditEventKind.MutationAcceptedFuture));
        Assert.IsTrue(kinds.Contains(ApprovalAuditEventKind.PolicyEvaluatedFuture));
        Assert.IsTrue(kinds.Contains(ApprovalAuditEventKind.RuntimeGateEvaluatedFuture));
        Assert.IsTrue(kinds.Contains(ApprovalAuditEventKind.ExportRequestedFuture));
        Assert.IsTrue(kinds.Contains(ApprovalAuditEventKind.ExportBlockedFuture));
        Assert.IsTrue(kinds.Contains(ApprovalAuditEventKind.ExternalAuditRequestedFuture));
        Assert.IsTrue(kinds.Contains(ApprovalAuditEventKind.ExternalAuditCompletedFuture));
    }

    [TestMethod]
    public void FieldRequirements_LinkFutureMutationExecutionAndExportWithoutImplementation()
    {
        var fields = ApprovalDurableAuditTrailDesignOnlyProtectedPresenter.CreateFixture().FieldRequirements;
        var byName = fields.ToDictionary(field => field.FieldName);

        Assert.IsTrue(byName["MutationAttemptIdFuture"].RequiredBeforeRealMutation);
        Assert.IsFalse(byName["MutationAttemptIdFuture"].RequiredBeforeRealExecution);
        Assert.IsTrue(byName["RuntimeGateSnapshotFuture"].RequiredBeforeRealExecution);
        Assert.IsTrue(byName["ExportPolicySnapshotFuture"].RequiredBeforePhysicalExport);
        Assert.IsTrue(byName["PreviousEventHashFuture"].RequiredBeforeRealMutation);
        Assert.IsTrue(byName["EventHashFuture"].RequiredBeforeRealExecution);
        Assert.IsTrue(fields.All(field => !field.IsImplementedNow));
    }

    [TestMethod]
    public void RetentionClasses_AreDeterministicAndFutureOnly()
    {
        var design = ApprovalDurableAuditTrailDesignOnlyProtectedPresenter.CreateFixture();

        CollectionAssert.AreEquivalent(Enum.GetValues<ApprovalAuditRetentionClass>(), design.RetentionRequirements.Select(retention => retention.RetentionClass).ToArray());
        Assert.IsTrue(design.EventPreviews.Any(auditEvent => auditEvent.RetentionClassFuture == ApprovalAuditRetentionClass.MutationAttemptFuture));
        Assert.IsTrue(design.EventPreviews.Any(auditEvent => auditEvent.RetentionClassFuture == ApprovalAuditRetentionClass.PolicyDecisionFuture));
        Assert.IsTrue(design.EventPreviews.Any(auditEvent => auditEvent.RetentionClassFuture == ApprovalAuditRetentionClass.RuntimeGateFuture));
        Assert.IsTrue(design.EventPreviews.Any(auditEvent => auditEvent.RetentionClassFuture == ApprovalAuditRetentionClass.ExportRequestFuture));
        Assert.IsTrue(design.EventPreviews.Any(auditEvent => auditEvent.RetentionClassFuture == ApprovalAuditRetentionClass.ExternalAuditFuture));
        Assert.IsTrue(design.RetentionRequirements.All(retention => !retention.RetentionStoreImplemented));
    }

    [TestMethod]
    public void FutureImplementationChecklist_RequiresAuditBeforeAnyRealTrail()
    {
        var checklist = ApprovalDurableAuditTrailDesignOnlyProtectedPresenter.CreateFixture().FutureImplementationChecklist;
        var text = string.Join(
            "\n",
            string.Join("\n", checklist.RequiredBeforeRealAuditTrail),
            string.Join("\n", checklist.RequiredExternalAudits),
            string.Join("\n", checklist.RequiredPolicyDecisions),
            string.Join("\n", checklist.ExplicitNonGoals));

        StringAssert.Contains(text, "durable storage architecture and migration review");
        StringAssert.Contains(text, "append-only ledger policy");
        StringAssert.Contains(text, "hash-chain algorithm and chain validation policy");
        StringAssert.Contains(text, "durable audit trail design external audit");
        StringAssert.Contains(text, "no durable audit trail real");
        StringAssert.Contains(text, "no database");
        StringAssert.Contains(text, "no file read or file hash real");
        StringAssert.Contains(text, "no runtime/live");
    }

    [TestMethod]
    public void DurableAuditTrailDesign_ReportTextHasNoActiveReadinessOverclaim()
    {
        var design = ApprovalDurableAuditTrailDesignOnlyProtectedPresenter.CreateFixture();
        var text = string.Join(
            "\n",
            design.Title,
            design.Mode,
            string.Join("\n", design.Warnings),
            string.Join("\n", design.FutureImplementationChecklist.ExplicitNonGoals),
            string.Join("\n", design.CurrentApprovalAuditTrailBaseline));

        Assert.IsFalse(text.Contains("production" + "-ready", StringComparison.OrdinalIgnoreCase));
        Assert.IsFalse(text.Contains("release" + "-ready", StringComparison.OrdinalIgnoreCase));
        Assert.IsFalse(text.Contains("audit trail implemented", StringComparison.OrdinalIgnoreCase));
        Assert.IsFalse(text.Contains("ledger implemented", StringComparison.OrdinalIgnoreCase));
        Assert.IsFalse(text.Contains("event persisted", StringComparison.OrdinalIgnoreCase));
        Assert.IsFalse(text.Contains("runtime enabled", StringComparison.OrdinalIgnoreCase));
        Assert.IsFalse(text.Contains("physical export enabled", StringComparison.OrdinalIgnoreCase));
        Assert.IsFalse(text.Contains("service registered", StringComparison.OrdinalIgnoreCase));
        Assert.IsFalse(text.Contains("command handler registered", StringComparison.OrdinalIgnoreCase));
    }
}
