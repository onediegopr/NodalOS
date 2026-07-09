using Microsoft.VisualStudio.TestTools.UnitTesting;
using OneBrain.Core.Approval;

namespace OneBrain.Safety.Tests;

[TestClass]
[TestCategory("ApprovalHumanReview")]
[TestCategory("PhaseEApprovalHumanReview")]
public sealed class ApprovalDurableAuditTrailDesignOnlyProtectedSafetyTests
{
    [TestMethod]
    public void DurableAuditTrailDesign_KeepsAllImplementationReadinessAtZero()
    {
        var design = ApprovalDurableAuditTrailDesignOnlyProtectedPresenter.CreateFixture();

        Assert.AreEqual(0, design.Readiness.ImplementationReadinessPercent);
        Assert.AreEqual(0, design.Readiness.DurableStoreReadinessPercent);
        Assert.AreEqual(0, design.Readiness.AppendOnlyLedgerReadinessPercent);
        Assert.AreEqual(0, design.Readiness.DatabaseReadinessPercent);
        Assert.AreEqual(0, design.Readiness.FilesystemReadinessPercent);
        Assert.AreEqual(0, design.Readiness.HashChainImplementationReadinessPercent);
        Assert.AreEqual(0, design.Readiness.RedactionRuntimeReadinessPercent);
        Assert.AreEqual(0, design.Readiness.RetentionWorkflowReadinessPercent);
        Assert.AreEqual(0, design.Readiness.DeletionWorkflowReadinessPercent);
        Assert.AreEqual(0, design.Readiness.RuntimeLiveReadinessPercent);
        Assert.IsFalse(design.Readiness.ReleaseCommercialReady);
        Assert.IsTrue(design.Readiness.KeepsImplementationBlocked);
    }

    [TestMethod]
    public void DurableAuditTrailDesign_HasNoStorageLedgerDbFilesystemServiceCommandOrRuntime()
    {
        var design = ApprovalDurableAuditTrailDesignOnlyProtectedPresenter.CreateFixture();

        Assert.IsFalse(design.HasDurableAuditTrail);
        Assert.IsFalse(design.HasAppendOnlyLedger);
        Assert.IsFalse(design.HasAuditRepository);
        Assert.IsFalse(design.HasDatabase);
        Assert.IsFalse(design.HasFilesystemWrite);
        Assert.IsFalse(design.HasFileHashing);
        Assert.IsFalse(design.HasRuntimeLive);
        Assert.IsFalse(design.HasServiceRegistration);
        Assert.IsFalse(design.HasCommandHandler);
        Assert.IsFalse(design.CapabilityStatus.CanAppendAuditEvent);
        Assert.IsFalse(design.CapabilityStatus.CanPersistAuditTrail);
        Assert.IsFalse(design.CapabilityStatus.CanReadWorkspaceFiles);
        Assert.IsFalse(design.CapabilityStatus.CanHashRealFiles);
        Assert.IsFalse(design.CapabilityStatus.CanReplayAuditTrail);
        Assert.IsFalse(design.CapabilityStatus.CanExportAuditTrail);
    }

    [TestMethod]
    public void AuditEventPreviews_AreNeverAppendedPersistedDurableOrExported()
    {
        var design = ApprovalDurableAuditTrailDesignOnlyProtectedPresenter.CreateFixture();

        Assert.AreEqual(15, design.EventPreviews.Count);
        Assert.AreEqual(0, design.AuditAppendCount);
        Assert.AreEqual(0, design.PersistedEventCount);
        Assert.AreEqual(0, design.ExportActionCount);
        Assert.IsTrue(design.EventPreviews.All(auditEvent => auditEvent.IsPreviewOnly));
        Assert.IsTrue(design.EventPreviews.All(auditEvent => !auditEvent.CanAppend));
        Assert.IsTrue(design.EventPreviews.All(auditEvent => !auditEvent.IsPersisted));
        Assert.IsTrue(design.EventPreviews.All(auditEvent => !auditEvent.IsDurable));
        CollectionAssert.AreEquivalent(Enum.GetValues<ApprovalAuditEventKind>(), design.EventPreviews.Select(auditEvent => auditEvent.EventKind).ToArray());
    }

    [TestMethod]
    public void FieldRequirements_ArePreviewOnlyAndDoNotImplementHashIdentityPolicyOrExport()
    {
        var fields = ApprovalDurableAuditTrailDesignOnlyProtectedPresenter.CreateFixture().FieldRequirements;

        Assert.AreEqual(20, fields.Count);
        Assert.IsTrue(fields.All(field => field.IsPreviewOnly));
        Assert.IsTrue(fields.All(field => !field.IsImplementedNow));
        Assert.IsTrue(fields.Any(field => field.FieldName == "PreviousEventHashFuture"));
        Assert.IsTrue(fields.Any(field => field.FieldName == "EventHashFuture"));
        Assert.IsTrue(fields.Any(field => field.FieldName == "RuntimeGateSnapshotFuture"));
        Assert.IsTrue(fields.Any(field => field.FieldName == "ExportPolicySnapshotFuture"));
        Assert.IsTrue(fields.Where(field => field.FieldName.Contains("Hash", StringComparison.Ordinal)).All(field => field.IsPreviewOnly && !field.IsImplementedNow));
        Assert.IsTrue(fields.Where(field => field.RequiredBeforePhysicalExport).All(field => field.IsPreviewOnly));
    }

    [TestMethod]
    public void RedactionRetentionDeletion_HaveNoRuntimeStoreWorkflowOrRawPayloadStorage()
    {
        var design = ApprovalDurableAuditTrailDesignOnlyProtectedPresenter.CreateFixture();

        Assert.IsTrue(design.RedactionRequirement.PayloadMustBeRedactedFuture);
        Assert.IsTrue(design.RedactionRequirement.EvidenceRefsOnlyFuture);
        Assert.IsTrue(design.RedactionRequirement.SecretScanRequiredFuture);
        Assert.IsTrue(design.RedactionRequirement.PiiPolicyRequiredFuture);
        Assert.IsFalse(design.RedactionRequirement.RedactionRuntimeImplemented);
        Assert.IsFalse(design.RedactionRequirement.CanStoreRawPayload);
        Assert.IsFalse(design.RedactionRequirement.PrivacyExportAvailable);
        Assert.AreEqual(7, design.RetentionRequirements.Count);
        Assert.IsTrue(design.RetentionRequirements.All(retention => retention.RetentionPolicyRequiredFuture));
        Assert.IsTrue(design.RetentionRequirements.All(retention => !retention.RetentionStoreImplemented));
        Assert.IsTrue(design.DeletionRequirement.DeletionPolicyRequiredFuture);
        Assert.IsTrue(design.DeletionRequirement.AuditTombstoneFuture);
        Assert.IsFalse(design.DeletionRequirement.DeletionWorkflowImplemented);
    }

    [TestMethod]
    public void HashChainAndReplayProtection_AreFutureOnlyWithoutRealHashReplayOrIdempotencyStore()
    {
        var design = ApprovalDurableAuditTrailDesignOnlyProtectedPresenter.CreateFixture();

        Assert.IsTrue(design.HashChainDesign.PreviousEventHashRequiredFuture);
        Assert.IsTrue(design.HashChainDesign.EventHashRequiredFuture);
        Assert.IsTrue(design.HashChainDesign.ChainValidationRequiredFuture);
        Assert.IsTrue(design.HashChainDesign.TamperEvidenceRequiredFuture);
        Assert.IsFalse(design.HashChainDesign.HashChainImplemented);
        Assert.IsFalse(design.HashChainDesign.CanHashRealFiles);
        Assert.IsFalse(design.HashChainDesign.CanHashRealEvents);
        Assert.IsTrue(design.ReplayProtectionDesign.ReplayNonceRequiredFuture);
        Assert.IsTrue(design.ReplayProtectionDesign.IdempotencyKeyRequiredFuture);
        Assert.IsTrue(design.ReplayProtectionDesign.DuplicateEventDetectionFuture);
        Assert.IsFalse(design.ReplayProtectionDesign.ReplayProtectionImplemented);
        Assert.IsFalse(design.ReplayProtectionDesign.CanDetectReplayNow);
        Assert.IsFalse(design.ReplayProtectionDesign.IdempotencyStoreImplemented);
    }

    [TestMethod]
    public void ExternalAuditRequirements_DoNotEnableProviderNetworkOrService()
    {
        var requirements = ApprovalDurableAuditTrailDesignOnlyProtectedPresenter.CreateFixture().ExternalAuditRequirements;

        Assert.AreEqual(4, requirements.Count);
        Assert.IsTrue(requirements.All(requirement => requirement.RequiredBeforeImplementation));
        Assert.IsTrue(requirements.All(requirement => requirement.RequiredBeforeMutationStore));
        Assert.IsTrue(requirements.All(requirement => requirement.RequiredBeforeRuntime));
        Assert.IsTrue(requirements.All(requirement => requirement.RequiredBeforePhysicalExport));
        Assert.IsTrue(requirements.All(requirement => !requirement.ExternalProviderEnabled));
        Assert.IsTrue(requirements.All(requirement => !requirement.NetworkEnabled));
        Assert.IsTrue(requirements.All(requirement => !requirement.ServiceRegistered));
    }

    [TestMethod]
    public void AntiCapabilityProof_BlocksEveryRealAuditTrailCapability()
    {
        var proof = ApprovalDurableAuditTrailDesignOnlyProtectedPresenter.CreateFixture().AntiCapabilityProof;

        Assert.IsTrue(proof.Passes);
        Assert.IsTrue(proof.CannotAppendAuditEvent);
        Assert.IsTrue(proof.CannotPersistAuditTrail);
        Assert.IsTrue(proof.CannotUseAppendOnlyLedger);
        Assert.IsTrue(proof.CannotUseAuditRepository);
        Assert.IsTrue(proof.CannotUseDatabase);
        Assert.IsTrue(proof.CannotUseFilesystem);
        Assert.IsTrue(proof.CannotReadWorkspaceFiles);
        Assert.IsTrue(proof.CannotHashRealFiles);
        Assert.IsTrue(proof.CannotHashRealEvents);
        Assert.IsTrue(proof.CannotRunRedactionRuntime);
        Assert.IsTrue(proof.CannotRunRetentionWorkflow);
        Assert.IsTrue(proof.CannotRunDeletionWorkflow);
        Assert.IsTrue(proof.CannotRegisterService);
        Assert.IsTrue(proof.CannotCreateCommandHandler);
        Assert.IsTrue(proof.CannotMutateApprovalState);
        Assert.IsTrue(proof.CannotExecuteApproval);
        Assert.IsTrue(proof.CannotInvokeWriter);
        Assert.IsTrue(proof.CannotInvokePolicyProductivePath);
        Assert.IsTrue(proof.CannotStartRuntime);
        Assert.IsTrue(proof.CannotExportPhysicalFile);
        Assert.IsTrue(proof.CannotClaimReleaseReady);
        Assert.IsFalse(proof.NoSideEffectProof.ApprovalStateMutationAttempted);
        Assert.IsFalse(proof.NoSideEffectProof.RuntimeTouched);
    }

    [TestMethod]
    public void DurableAuditTrailDesign_HasNoProductActionMutationAppendPersistedEventOrExportCounts()
    {
        var design = ApprovalDurableAuditTrailDesignOnlyProtectedPresenter.CreateFixture();

        Assert.AreEqual(0, design.ProductActionCount);
        Assert.AreEqual(0, design.StateMutationCount);
        Assert.AreEqual(0, design.AuditAppendCount);
        Assert.AreEqual(0, design.PersistedEventCount);
        Assert.AreEqual(0, design.ExportActionCount);
        Assert.IsTrue(design.PassesSafetyProof);
        Assert.AreEqual("NODAL_OS_DURABLE_APPROVAL_AUDIT_TRAIL_DESIGN_EXTERNAL_AUDIT", design.NextSafeStep);
    }

    [TestMethod]
    public void DurableAuditTrailDesign_DoesNotClaimMinimalAppendOnlyLedgerAuthority()
    {
        var design = ApprovalDurableAuditTrailDesignOnlyProtectedPresenter.CreateFixture();
        var designSource = SourceText("src/OneBrain.Core/Approval/ApprovalDurableAuditTrailDesignOnlyProtected.cs");
        var minimalLedgerSource = SourceText("src/OneBrain.Core/Approval/DurableAuditTrailAppendOnlyMinimal.cs");

        Assert.IsFalse(design.HasDurableAuditTrail);
        Assert.IsFalse(design.HasAppendOnlyLedger);
        Assert.IsFalse(design.CapabilityStatus.CanAppendAuditEvent);
        Assert.IsFalse(design.CapabilityStatus.CanPersistAuditTrail);
        Assert.IsTrue(design.AntiCapabilityProof.CannotUseAppendOnlyLedger);
        Assert.IsTrue(design.AntiCapabilityProof.CannotInvokeWriter);
        Assert.IsTrue(design.AntiCapabilityProof.CannotInvokePolicyProductivePath);
        Assert.IsTrue(design.PassesSafetyProof);

        Assert.IsFalse(designSource.Contains(nameof(DurableAuditTrailAppendOnlyMinimal), StringComparison.Ordinal));
        Assert.IsFalse(designSource.Contains("AppendStage2TestOnly", StringComparison.Ordinal));
        StringAssert.Contains(minimalLedgerSource, "AppendStage2TestOnly");
        StringAssert.Contains(minimalLedgerSource, "AllowLocalTestStorageOnly");
        StringAssert.Contains(minimalLedgerSource, "StorageRootOutsideLocalTestBoundary");
        StringAssert.Contains(minimalLedgerSource, "ProductActionAllowed: false");
        StringAssert.Contains(minimalLedgerSource, "NetworkAllowed: false");
        StringAssert.Contains(minimalLedgerSource, "DbMigrationAllowed: false");
        StringAssert.Contains(minimalLedgerSource, "ReleaseCommercialReady: false");
    }

    private static string SourceText(string relativePath)
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null)
        {
            var candidate = Path.Combine(directory.FullName, relativePath);
            if (File.Exists(candidate))
            {
                return File.ReadAllText(candidate);
            }

            directory = directory.Parent;
        }

        Assert.Fail($"Could not locate repository source file: {relativePath}");
        return string.Empty;
    }
}
