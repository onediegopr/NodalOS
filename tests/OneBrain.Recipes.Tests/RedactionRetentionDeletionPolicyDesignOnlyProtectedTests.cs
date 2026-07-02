using Microsoft.VisualStudio.TestTools.UnitTesting;
using OneBrain.Core.Approval;

namespace OneBrain.Recipes.Tests;

[TestClass]
[TestCategory("ApprovalHumanReview")]
[TestCategory("PhaseEApprovalHumanReview")]
public sealed class RedactionRetentionDeletionPolicyDesignOnlyProtectedTests
{
    [TestMethod]
    public void RedactionRetentionDeletionPolicyDesign_IsDeterministicDesignOnlyAndReadOnly()
    {
        var first = RedactionRetentionDeletionPolicyDesignOnlyProtectedPresenter.CreateFixture();
        var second = RedactionRetentionDeletionPolicyDesignOnlyProtectedPresenter.CreateFixture();

        Assert.AreEqual("nodal-os.approval.redaction-retention-deletion-policy.design-only.protected.fixture.v1", first.DesignId);
        Assert.AreEqual(RedactionRetentionDeletionPolicyDesignStatus.DesignOnly, first.Status);
        Assert.AreEqual("DESIGN_ONLY_READ_ONLY_PREVIEW_NO_REDACTION_RUNTIME_NO_RETENTION_STORE_NO_DELETION_WORKFLOW_NO_IO_NO_EXPORT", first.Mode);
        Assert.AreEqual(first.DesignId, second.DesignId);
        Assert.AreEqual(first.Mode, second.Mode);
        CollectionAssert.AreEqual(first.BlockedReasons.ToArray(), second.BlockedReasons.ToArray());
        CollectionAssert.AreEqual(first.RetentionPolicyPreview.RetentionClassesFuture.ToArray(), second.RetentionPolicyPreview.RetentionClassesFuture.ToArray());
        CollectionAssert.AreEqual(first.DeletionPolicyPreview.DeletionReasonsFuture.ToArray(), second.DeletionPolicyPreview.DeletionReasonsFuture.ToArray());
        Assert.IsTrue(first.AntiCapabilityProof.NoSideEffectProof.ReadOnly);
        Assert.IsTrue(first.AntiCapabilityProof.NoSideEffectProof.Deterministic);
        Assert.IsTrue(first.AntiCapabilityProof.NoSideEffectProof.FixtureSafe);
    }

    [TestMethod]
    public void RedactionRetentionDeletionPolicyDesign_CoversAllFutureRetentionClassesAndDeletionReasons()
    {
        var design = RedactionRetentionDeletionPolicyDesignOnlyProtectedPresenter.CreateFixture();

        CollectionAssert.AreEquivalent(Enum.GetValues<RetentionClassFuture>(), design.RetentionPolicyPreview.RetentionClassesFuture.ToArray());
        CollectionAssert.AreEquivalent(Enum.GetValues<DeletionReasonFuture>(), design.DeletionPolicyPreview.DeletionReasonsFuture.ToArray());
        Assert.IsTrue(design.RetentionPolicyPreview.RetentionClassesFuture.All(value => value.ToString().EndsWith("Future", StringComparison.Ordinal)));
        Assert.IsTrue(design.DeletionPolicyPreview.DeletionReasonsFuture.All(value => value.ToString().EndsWith("Future", StringComparison.Ordinal)));
    }

    [TestMethod]
    public void RedactionRetentionDeletionPolicyDesign_CoversRequiredBlockers()
    {
        var design = RedactionRetentionDeletionPolicyDesignOnlyProtectedPresenter.CreateFixture();

        Assert.AreEqual(16, design.BlockedReasons.Count);
        CollectionAssert.Contains(design.BlockedReasons.ToArray(), RedactionRetentionDeletionBlockedReason.NoRedactionRuntime);
        CollectionAssert.Contains(design.BlockedReasons.ToArray(), RedactionRetentionDeletionBlockedReason.NoSecretScanner);
        CollectionAssert.Contains(design.BlockedReasons.ToArray(), RedactionRetentionDeletionBlockedReason.NoPiiPolicy);
        CollectionAssert.Contains(design.BlockedReasons.ToArray(), RedactionRetentionDeletionBlockedReason.NoRetentionStore);
        CollectionAssert.Contains(design.BlockedReasons.ToArray(), RedactionRetentionDeletionBlockedReason.NoDeletionWorkflow);
        CollectionAssert.Contains(design.BlockedReasons.ToArray(), RedactionRetentionDeletionBlockedReason.NoTombstoneWriter);
        CollectionAssert.Contains(design.BlockedReasons.ToArray(), RedactionRetentionDeletionBlockedReason.NoLegalHoldPolicy);
        CollectionAssert.Contains(design.BlockedReasons.ToArray(), RedactionRetentionDeletionBlockedReason.NoFilesystemPolicy);
        CollectionAssert.Contains(design.BlockedReasons.ToArray(), RedactionRetentionDeletionBlockedReason.NoDatabasePolicy);
        CollectionAssert.Contains(design.BlockedReasons.ToArray(), RedactionRetentionDeletionBlockedReason.NoDurableAuditTrailImplementation);
        CollectionAssert.Contains(design.BlockedReasons.ToArray(), RedactionRetentionDeletionBlockedReason.NoPhysicalExportImplementation);
        CollectionAssert.Contains(design.BlockedReasons.ToArray(), RedactionRetentionDeletionBlockedReason.NoExternalAuditApproval);
        CollectionAssert.Contains(design.BlockedReasons.ToArray(), RedactionRetentionDeletionBlockedReason.NoReleaseCommercialApproval);
    }

    [TestMethod]
    public void FutureImplementationChecklist_RequiresExternalAuditBeforeRuntimeWorkflowOrExport()
    {
        var checklist = RedactionRetentionDeletionPolicyDesignOnlyProtectedPresenter.CreateFixture().FutureImplementationChecklist;

        var text = string.Join(
            "\n",
            checklist.RequiredBeforeRedactionRuntime
                .Concat(checklist.RequiredBeforeRetentionDeletionWorkflow)
                .Concat(checklist.RequiredBeforePhysicalExport)
                .Concat(checklist.NonGoals));

        StringAssert.Contains(text, "external audit of redaction policy");
        StringAssert.Contains(text, "durable audit trail implementation approval");
        StringAssert.Contains(text, "physical export policy external audit");
        StringAssert.Contains(text, "no redaction runtime");
        StringAssert.Contains(text, "no secret or PII scan");
        StringAssert.Contains(text, "no retention store or workflow");
        StringAssert.Contains(text, "no deletion workflow, tombstone write or legal hold store");
        StringAssert.Contains(text, "no filesystem or database access");
        StringAssert.Contains(text, "no physical export");
        StringAssert.Contains(text, "no runtime/live");
    }

    [TestMethod]
    public void CurrentBaseline_AnchorsDurableAuditPhysicalExportAndHumanReviewEvidenceGuards()
    {
        var baseline = RedactionRetentionDeletionPolicyDesignOnlyProtectedPresenter.CreateFixture().CurrentRedactionRetentionDeletionBaseline;
        var text = string.Join("\n", baseline);

        Assert.IsTrue(baseline.Count >= 5);
        StringAssert.Contains(text, "Approval durable audit trail design");
        StringAssert.Contains(text, "Physical export policy design blocks every export format");
        StringAssert.Contains(text, "Human review evidence link guards exclude raw payload and secret-like evidence");
        StringAssert.Contains(text, "No redaction runtime, secret scanner, PII scanner, retention store, deletion workflow, tombstone writer or legal hold store is registered.");
        StringAssert.Contains(text, "No filesystem, database, provider/cloud, runtime/live, mutation, execution or physical export capability is introduced by this fixture.");
    }

    [TestMethod]
    public void RedactionRetentionDeletionPolicyDesign_ReportTextHasNoActiveImplementationOverclaim()
    {
        var design = RedactionRetentionDeletionPolicyDesignOnlyProtectedPresenter.CreateFixture();
        var text = string.Join(
            "\n",
            design.Warnings
                .Concat(design.FutureImplementationChecklist.NonGoals)
                .Concat(design.CurrentRedactionRetentionDeletionBaseline));

        Assert.IsFalse(text.Contains("production" + "-ready", StringComparison.OrdinalIgnoreCase));
        Assert.IsFalse(text.Contains("release" + "-ready", StringComparison.OrdinalIgnoreCase));
        Assert.IsFalse(text.Contains("redaction implemented", StringComparison.OrdinalIgnoreCase));
        Assert.IsFalse(text.Contains("retention implemented", StringComparison.OrdinalIgnoreCase));
        Assert.IsFalse(text.Contains("deletion implemented", StringComparison.OrdinalIgnoreCase));
        Assert.IsFalse(text.Contains("privacy export available", StringComparison.OrdinalIgnoreCase));
        Assert.IsFalse(text.Contains("runtime enabled", StringComparison.OrdinalIgnoreCase));
        Assert.IsFalse(text.Contains("service registered", StringComparison.OrdinalIgnoreCase));
        Assert.IsFalse(text.Contains("command handler registered", StringComparison.OrdinalIgnoreCase));
        Assert.IsFalse(text.Contains("physical export enabled", StringComparison.OrdinalIgnoreCase));
    }
}
