using Microsoft.VisualStudio.TestTools.UnitTesting;
using OneBrain.Core.Approval;

namespace OneBrain.Safety.Tests;

[TestClass]
[TestCategory("ApprovalHumanReview")]
[TestCategory("PhaseEApprovalHumanReview")]
public sealed class RedactionRetentionDeletionPolicyDesignOnlyProtectedSafetyTests
{
    [TestMethod]
    public void RedactionRetentionDeletionPolicyDesign_KeepsAllImplementationReadinessAtZero()
    {
        var design = RedactionRetentionDeletionPolicyDesignOnlyProtectedPresenter.CreateFixture();

        Assert.AreEqual(0, design.Readiness.RedactionRuntimeReadinessPercent);
        Assert.AreEqual(0, design.Readiness.SecretScanReadinessPercent);
        Assert.AreEqual(0, design.Readiness.PiiScanReadinessPercent);
        Assert.AreEqual(0, design.Readiness.RetentionStoreReadinessPercent);
        Assert.AreEqual(0, design.Readiness.RetentionWorkflowReadinessPercent);
        Assert.AreEqual(0, design.Readiness.DeletionWorkflowReadinessPercent);
        Assert.AreEqual(0, design.Readiness.TombstoneWriteReadinessPercent);
        Assert.AreEqual(0, design.Readiness.LegalHoldStoreReadinessPercent);
        Assert.AreEqual(0, design.Readiness.FilesystemReadinessPercent);
        Assert.AreEqual(0, design.Readiness.DatabaseReadinessPercent);
        Assert.AreEqual(0, design.Readiness.PhysicalExportReadinessPercent);
        Assert.AreEqual(0, design.Readiness.RuntimeLiveReadinessPercent);
        Assert.IsFalse(design.Readiness.ReleaseCommercialReady);
        Assert.IsTrue(design.Readiness.KeepsImplementationBlocked);
    }

    [TestMethod]
    public void RedactionRetentionDeletionPolicyDesign_HasNoRuntimeScannerStoreWorkflowFilesystemDbServiceCommandOrExport()
    {
        var capability = RedactionRetentionDeletionPolicyDesignOnlyProtectedPresenter.CreateFixture().CapabilityStatus;

        Assert.IsFalse(capability.HasRedactionRuntime);
        Assert.IsFalse(capability.HasSecretScanner);
        Assert.IsFalse(capability.HasPiiScanner);
        Assert.IsFalse(capability.HasRetentionStore);
        Assert.IsFalse(capability.HasDeletionWorkflow);
        Assert.IsFalse(capability.HasTombstoneWriter);
        Assert.IsFalse(capability.HasLegalHoldStore);
        Assert.IsFalse(capability.HasFilesystemAccess);
        Assert.IsFalse(capability.HasDatabase);
        Assert.IsFalse(capability.HasPhysicalExport);
        Assert.IsFalse(capability.HasDurableAuditTrail);
        Assert.IsFalse(capability.HasServiceRegistration);
        Assert.IsFalse(capability.HasCommandHandler);
        Assert.IsFalse(capability.CanRedactPayload);
        Assert.IsFalse(capability.CanScanSecrets);
        Assert.IsFalse(capability.CanScanPii);
        Assert.IsFalse(capability.CanRetainData);
        Assert.IsFalse(capability.CanDeleteData);
        Assert.IsFalse(capability.CanWriteTombstone);
        Assert.IsFalse(capability.CanApplyLegalHold);
        Assert.IsFalse(capability.CanExportPrivacyData);
    }

    [TestMethod]
    public void RedactionPolicyPreview_BlocksRawPayloadStorageAndRuntime()
    {
        var design = RedactionRetentionDeletionPolicyDesignOnlyProtectedPresenter.CreateFixture();
        var redaction = design.RedactionPolicyPreview;
        var scan = design.SecretPiiScanRequirement;

        Assert.IsTrue(redaction.RawPayloadExportForbiddenFuture);
        Assert.IsTrue(redaction.EvidenceRefsPreferredFuture);
        Assert.IsTrue(redaction.SecretScanRequiredFuture);
        Assert.IsTrue(redaction.PiiPolicyRequiredFuture);
        Assert.IsTrue(redaction.RedactionRuntimeRequiredFuture);
        Assert.IsFalse(redaction.RedactionRuntimeImplemented);
        Assert.IsFalse(redaction.CanStoreRawPayload);
        Assert.IsFalse(redaction.CanRedactNow);
        Assert.IsTrue(redaction.RequiresExternalAuditFuture);
        Assert.IsFalse(scan.SecretScannerImplemented);
        Assert.IsFalse(scan.PiiScannerImplemented);
        Assert.IsFalse(scan.CanScanSecretsNow);
        Assert.IsFalse(scan.CanScanPiiNow);
        Assert.IsFalse(scan.UsesPatternEngineNow);
        Assert.IsFalse(scan.UsesProviderCloudNow);
        CollectionAssert.Contains(redaction.BlockedReasons.ToArray(), RedactionRetentionDeletionBlockedReason.NoRedactionRuntime);
        CollectionAssert.Contains(scan.BlockedReasons.ToArray(), RedactionRetentionDeletionBlockedReason.NoSecretScanner);
    }

    [TestMethod]
    public void RetentionPolicyPreview_IsFutureOnlyWithoutStoreWorkflowOrRetainAction()
    {
        var retention = RedactionRetentionDeletionPolicyDesignOnlyProtectedPresenter.CreateFixture().RetentionPolicyPreview;

        Assert.AreEqual(Enum.GetValues<RetentionClassFuture>().Length, retention.RetentionClassesFuture.Count);
        Assert.IsTrue(retention.RetentionClassesFuture.All(value => value.ToString().EndsWith("Future", StringComparison.Ordinal)));
        Assert.IsTrue(retention.RetentionClassRequiredFuture);
        Assert.IsTrue(retention.RetentionPolicyRequiredFuture);
        Assert.IsTrue(retention.WorkspacePolicyRequiredFuture);
        Assert.IsTrue(retention.UserConsentRequiredFuture);
        Assert.IsFalse(retention.RetentionStoreImplemented);
        Assert.IsFalse(retention.RetentionWorkflowImplemented);
        Assert.IsFalse(retention.CanRetainNow);
        CollectionAssert.Contains(retention.BlockedReasons.ToArray(), RedactionRetentionDeletionBlockedReason.NoRetentionStore);
    }

    [TestMethod]
    public void DeletionTombstoneLegalHoldPreviews_AreFutureOnlyWithoutDeleteWriteOrLegalHoldAction()
    {
        var design = RedactionRetentionDeletionPolicyDesignOnlyProtectedPresenter.CreateFixture();
        var deletion = design.DeletionPolicyPreview;

        Assert.AreEqual(Enum.GetValues<DeletionReasonFuture>().Length, deletion.DeletionReasonsFuture.Count);
        Assert.IsTrue(deletion.DeletionReasonsFuture.All(value => value.ToString().EndsWith("Future", StringComparison.Ordinal)));
        Assert.IsTrue(deletion.DeletionEligibilityRequiredFuture);
        Assert.IsTrue(deletion.TombstoneRequiredFuture);
        Assert.IsTrue(deletion.LegalHoldPolicyRequiredFuture);
        Assert.IsFalse(deletion.DeletionWorkflowImplemented);
        Assert.IsFalse(deletion.TombstoneWriterImplemented);
        Assert.IsFalse(deletion.LegalHoldStoreImplemented);
        Assert.IsFalse(deletion.CanDeleteNow);
        Assert.IsFalse(deletion.CanWriteTombstoneNow);
        Assert.IsFalse(deletion.CanApplyLegalHoldNow);
        Assert.IsFalse(design.TombstonePolicyPreview.TombstoneWriterImplemented);
        Assert.IsFalse(design.TombstonePolicyPreview.CanWriteTombstoneNow);
        Assert.IsFalse(design.LegalHoldPolicyPreview.LegalHoldStoreImplemented);
        Assert.IsFalse(design.LegalHoldPolicyPreview.CanApplyLegalHoldNow);
    }

    [TestMethod]
    public void LinkageRequirements_BlockExportAuditTrailAndRawPayload()
    {
        var design = RedactionRetentionDeletionPolicyDesignOnlyProtectedPresenter.CreateFixture();

        Assert.IsTrue(design.EvidenceLinkageRequirement.EvidenceRefsPreferredFuture);
        Assert.IsTrue(design.EvidenceLinkageRequirement.SensitiveEvidenceExcludedByDefaultFuture);
        Assert.IsTrue(design.EvidenceLinkageRequirement.EvidenceClassPolicyRequiredFuture);
        Assert.IsFalse(design.EvidenceLinkageRequirement.EvidenceSelectionImplemented);
        Assert.IsTrue(design.EvidenceLinkageRequirement.RawPayloadBlocked);
        Assert.IsTrue(design.ExportLinkageRequirement.PhysicalExportBlockedUntilRedactionRuntime);
        Assert.IsTrue(design.ExportLinkageRequirement.PhysicalExportBlockedUntilRetentionDeletionPolicy);
        Assert.IsTrue(design.ExportLinkageRequirement.PhysicalExportBlockedUntilDestinationPolicy);
        Assert.IsTrue(design.ExportLinkageRequirement.PhysicalExportBlockedUntilDurableAuditTrailImplementation);
        Assert.IsTrue(design.ExportLinkageRequirement.PhysicalExportBlockedUntilExternalAudit);
        Assert.IsFalse(design.ExportLinkageRequirement.PhysicalExportAvailableNow);
        Assert.IsTrue(design.AuditTrailLinkageRequirement.AuditTrailCannotPersistRawPayload);
        Assert.IsTrue(design.AuditTrailLinkageRequirement.RedactionStatusRequiredFuture);
        Assert.IsTrue(design.AuditTrailLinkageRequirement.RetentionClassRequiredFuture);
        Assert.IsTrue(design.AuditTrailLinkageRequirement.DeletionEligibilityRequiredFuture);
        Assert.IsFalse(design.AuditTrailLinkageRequirement.AuditTrailImplementationAvailableNow);
    }

    [TestMethod]
    public void AntiCapabilityProof_BlocksEveryPrivacyRuntimeStorageExportAndExecutionCapability()
    {
        var proof = RedactionRetentionDeletionPolicyDesignOnlyProtectedPresenter.CreateFixture().AntiCapabilityProof;

        Assert.IsTrue(proof.Passes);
        Assert.IsTrue(proof.CannotRunRedactionRuntime);
        Assert.IsTrue(proof.CannotScanSecrets);
        Assert.IsTrue(proof.CannotScanPii);
        Assert.IsTrue(proof.CannotStoreRawPayload);
        Assert.IsTrue(proof.CannotRetainData);
        Assert.IsTrue(proof.CannotRunRetentionWorkflow);
        Assert.IsTrue(proof.CannotDeleteData);
        Assert.IsTrue(proof.CannotRunDeletionWorkflow);
        Assert.IsTrue(proof.CannotWriteTombstone);
        Assert.IsTrue(proof.CannotApplyLegalHold);
        Assert.IsTrue(proof.CannotUseFilesystem);
        Assert.IsTrue(proof.CannotUseDatabase);
        Assert.IsTrue(proof.CannotAppendAuditEvent);
        Assert.IsTrue(proof.CannotPersistAuditTrail);
        Assert.IsTrue(proof.CannotExportPhysicalFile);
        Assert.IsTrue(proof.CannotUseClipboardDownload);
        Assert.IsTrue(proof.CannotRegisterService);
        Assert.IsTrue(proof.CannotCreateCommandHandler);
        Assert.IsTrue(proof.CannotMutateApprovalState);
        Assert.IsTrue(proof.CannotExecuteApproval);
        Assert.IsTrue(proof.CannotStartRuntime);
        Assert.IsTrue(proof.CannotUseProviderCloud);
        Assert.IsTrue(proof.CannotUseLlmLive);
        Assert.IsTrue(proof.CannotUseBrowserCdp);
        Assert.IsTrue(proof.CannotUseWcuOcr);
        Assert.IsTrue(proof.CannotExecuteRecipe);
        Assert.IsTrue(proof.CannotClaimReleaseReady);
        Assert.IsFalse(proof.NoSideEffectProof.RuntimeTouched);
        Assert.IsFalse(proof.NoSideEffectProof.ProductActionExposed);
    }

    [TestMethod]
    public void RedactionRetentionDeletionPolicyDesign_HasNoActionCounts()
    {
        var design = RedactionRetentionDeletionPolicyDesignOnlyProtectedPresenter.CreateFixture();

        Assert.AreEqual(0, design.RedactionActionCount);
        Assert.AreEqual(0, design.RetentionActionCount);
        Assert.AreEqual(0, design.DeletionActionCount);
        Assert.AreEqual(0, design.TombstoneCount);
        Assert.AreEqual(0, design.LegalHoldActionCount);
        Assert.AreEqual(0, design.ExportActionCount);
        Assert.AreEqual(0, design.ProductActionCount);
        Assert.IsTrue(design.PassesSafetyProof);
        Assert.AreEqual("NODAL_OS_REDACTION_RETENTION_DELETION_EXTERNAL_AUDIT", design.NextSafeStep);
    }
}
