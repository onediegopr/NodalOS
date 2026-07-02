using Microsoft.VisualStudio.TestTools.UnitTesting;
using OneBrain.Core.Approval;

namespace OneBrain.Safety.Tests;

[TestClass]
[TestCategory("ApprovalHumanReview")]
[TestCategory("PhaseEApprovalHumanReview")]
public sealed class PhysicalExportPolicyDesignOnlyProtectedSafetyTests
{
    [TestMethod]
    public void PhysicalExportPolicyDesign_KeepsAllImplementationReadinessAtZero()
    {
        var design = PhysicalExportPolicyDesignOnlyProtectedPresenter.CreateFixture();

        Assert.AreEqual(0, design.Readiness.PhysicalExportImplementationReadinessPercent);
        Assert.AreEqual(0, design.Readiness.FileWriteReadinessPercent);
        Assert.AreEqual(0, design.Readiness.ClipboardReadinessPercent);
        Assert.AreEqual(0, design.Readiness.DownloadReadinessPercent);
        Assert.AreEqual(0, design.Readiness.PdfReadinessPercent);
        Assert.AreEqual(0, design.Readiness.DocxReadinessPercent);
        Assert.AreEqual(0, design.Readiness.JsonFileReadinessPercent);
        Assert.AreEqual(0, design.Readiness.MarkdownFileReadinessPercent);
        Assert.AreEqual(0, design.Readiness.RedactionRuntimeReadinessPercent);
        Assert.AreEqual(0, design.Readiness.DurableAuditTrailImplementationReadinessPercent);
        Assert.AreEqual(0, design.Readiness.RuntimeLiveReadinessPercent);
        Assert.IsFalse(design.Readiness.ReleaseCommercialReady);
        Assert.IsTrue(design.Readiness.KeepsImplementationBlocked);
    }

    [TestMethod]
    public void PhysicalExportPolicyDesign_HasNoWriterClipboardDownloadStreamFilesystemServiceCommandOrRuntime()
    {
        var design = PhysicalExportPolicyDesignOnlyProtectedPresenter.CreateFixture();

        Assert.IsFalse(design.HasPhysicalExport);
        Assert.IsFalse(design.HasFileWriter);
        Assert.IsFalse(design.CapabilityStatus.HasPdfWriter);
        Assert.IsFalse(design.CapabilityStatus.HasDocxWriter);
        Assert.IsFalse(design.CapabilityStatus.HasJsonFileWriter);
        Assert.IsFalse(design.CapabilityStatus.HasMarkdownFileWriter);
        Assert.IsFalse(design.HasClipboard);
        Assert.IsFalse(design.HasDownload);
        Assert.IsFalse(design.CapabilityStatus.HasBrowserDownload);
        Assert.IsFalse(design.HasStreamWriter);
        Assert.IsFalse(design.HasFilesystemWrite);
        Assert.IsFalse(design.CapabilityStatus.HasRedactionRuntime);
        Assert.IsFalse(design.CapabilityStatus.HasDurableAuditTrail);
        Assert.IsFalse(design.HasRuntimeLive);
        Assert.IsFalse(design.HasServiceRegistration);
        Assert.IsFalse(design.HasCommandHandler);
        Assert.IsFalse(design.CapabilityStatus.CanExportPhysicalFile);
        Assert.IsFalse(design.CapabilityStatus.CanWriteExportFile);
        Assert.IsFalse(design.CapabilityStatus.CanCopyToClipboard);
        Assert.IsFalse(design.CapabilityStatus.CanDownload);
    }

    [TestMethod]
    public void ExportFormatPreviews_AreNeverGeneratedDownloadedCopiedOrPhysicalOutput()
    {
        var design = PhysicalExportPolicyDesignOnlyProtectedPresenter.CreateFixture();

        Assert.AreEqual(6, design.ExportFormatPreviews.Count);
        Assert.AreEqual(0, design.ExportActionCount);
        Assert.AreEqual(0, design.FileOutputCount);
        Assert.AreEqual(0, design.ClipboardActionCount);
        Assert.AreEqual(0, design.DownloadActionCount);
        Assert.IsTrue(design.ExportFormatPreviews.All(format => format.IsPreviewOnly));
        Assert.IsTrue(design.ExportFormatPreviews.All(format => !format.IsPhysicalOutput));
        Assert.IsTrue(design.ExportFormatPreviews.All(format => !format.IsGenerated));
        Assert.IsTrue(design.ExportFormatPreviews.All(format => !format.IsDownloaded));
        Assert.IsTrue(design.ExportFormatPreviews.All(format => !format.IsCopiedToClipboard));
        CollectionAssert.AreEquivalent(Enum.GetValues<PhysicalExportFormatKind>(), design.ExportFormatPreviews.Select(format => format.FormatKind).ToArray());
    }

    [TestMethod]
    public void ExportFormatPreviews_RequireAllFutureGatesAndBlockers()
    {
        var design = PhysicalExportPolicyDesignOnlyProtectedPresenter.CreateFixture();

        Assert.AreEqual(14, design.BlockedReasons.Count);
        Assert.IsTrue(design.ExportFormatPreviews.All(format => format.RequiresRedactionRuntimeFuture));
        Assert.IsTrue(design.ExportFormatPreviews.All(format => format.RequiresDurableAuditTrailFuture));
        Assert.IsTrue(design.ExportFormatPreviews.All(format => format.RequiresUserConsentFuture));
        Assert.IsTrue(design.ExportFormatPreviews.All(format => format.RequiresDestinationPolicyFuture));
        Assert.IsTrue(design.ExportFormatPreviews.All(format => format.RequiresExternalAuditFuture));
        Assert.IsTrue(design.ExportFormatPreviews.All(format => format.BlockedReasons.Contains(PhysicalExportBlockedReason.NoRedactionRuntime)));
        Assert.IsTrue(design.ExportFormatPreviews.All(format => format.BlockedReasons.Contains(PhysicalExportBlockedReason.NoDurableAuditTrailImplementation)));
        Assert.IsTrue(design.ExportFormatPreviews.All(format => format.BlockedReasons.Contains(PhysicalExportBlockedReason.NoRuntimeGateApproval)));
    }

    [TestMethod]
    public void RedactionConsentDestinationEvidenceAuditAndRetention_BlockPhysicalExport()
    {
        var design = PhysicalExportPolicyDesignOnlyProtectedPresenter.CreateFixture();

        Assert.IsTrue(design.RedactionRequirement.RedactionRuntimeRequiredFuture);
        Assert.IsTrue(design.RedactionRequirement.RawPayloadExportForbidden);
        Assert.IsFalse(design.RedactionRequirement.RedactionRuntimeImplemented);
        Assert.IsFalse(design.RedactionRequirement.CanExportRawPayload);
        Assert.IsTrue(design.ConsentRequirement.ExplicitUserConsentRequiredFuture);
        Assert.IsFalse(design.ConsentRequirement.ConsentImplemented);
        Assert.IsFalse(design.ConsentRequirement.ConsentCanTriggerExport);
        Assert.IsTrue(design.DestinationRequirement.DestinationPolicyRequiredFuture);
        Assert.IsTrue(design.DestinationRequirement.ExternalDestinationBlocked);
        Assert.IsFalse(design.DestinationRequirement.DestinationValidationImplemented);
        Assert.IsTrue(design.EvidenceSelectionRequirement.EvidenceSelectionPolicyRequiredFuture);
        Assert.IsTrue(design.EvidenceSelectionRequirement.SensitiveEvidenceExcludedByDefaultFuture);
        Assert.IsFalse(design.EvidenceSelectionRequirement.EvidenceSelectionImplemented);
        Assert.IsTrue(design.AuditTrailRequirement.DurableAuditTrailImplementationRequiredFuture);
        Assert.IsFalse(design.AuditTrailRequirement.AuditAppendImplemented);
        Assert.AreEqual(0, design.AuditTrailRequirement.AuditAppendCount);
        Assert.AreEqual(0, design.AuditTrailRequirement.PersistedExportEventCount);
        Assert.IsFalse(design.RetentionDeletionRequirement.RetentionWorkflowImplemented);
        Assert.IsFalse(design.RetentionDeletionRequirement.DeletionWorkflowImplemented);
    }

    [TestMethod]
    public void AntiCapabilityProof_BlocksEveryPhysicalExportAndRuntimeCapability()
    {
        var proof = PhysicalExportPolicyDesignOnlyProtectedPresenter.CreateFixture().AntiCapabilityProof;

        Assert.IsTrue(proof.Passes);
        Assert.IsTrue(proof.CannotExportPhysicalFile);
        Assert.IsTrue(proof.CannotWriteExportFile);
        Assert.IsTrue(proof.CannotGeneratePdf);
        Assert.IsTrue(proof.CannotGenerateDocx);
        Assert.IsTrue(proof.CannotWriteJsonFile);
        Assert.IsTrue(proof.CannotWriteMarkdownFile);
        Assert.IsTrue(proof.CannotCopyToClipboard);
        Assert.IsTrue(proof.CannotDownload);
        Assert.IsTrue(proof.CannotUseBrowserDownload);
        Assert.IsTrue(proof.CannotUseFilesystem);
        Assert.IsTrue(proof.CannotUseStreamWriter);
        Assert.IsTrue(proof.CannotRunRedactionRuntime);
        Assert.IsTrue(proof.CannotAppendAuditEvent);
        Assert.IsTrue(proof.CannotPersistAuditTrail);
        Assert.IsTrue(proof.CannotRegisterService);
        Assert.IsTrue(proof.CannotCreateCommandHandler);
        Assert.IsTrue(proof.CannotMutateApprovalState);
        Assert.IsTrue(proof.CannotExecuteApproval);
        Assert.IsTrue(proof.CannotStartRuntime);
        Assert.IsTrue(proof.CannotClaimReleaseReady);
        Assert.IsFalse(proof.NoSideEffectProof.RuntimeTouched);
        Assert.IsFalse(proof.NoSideEffectProof.ProductActionExposed);
    }

    [TestMethod]
    public void PhysicalExportPolicyDesign_HasNoProductActionExportFileClipboardDownloadOrAuditAppendCounts()
    {
        var design = PhysicalExportPolicyDesignOnlyProtectedPresenter.CreateFixture();

        Assert.AreEqual(0, design.ProductActionCount);
        Assert.AreEqual(0, design.ExportActionCount);
        Assert.AreEqual(0, design.FileOutputCount);
        Assert.AreEqual(0, design.ClipboardActionCount);
        Assert.AreEqual(0, design.DownloadActionCount);
        Assert.AreEqual(0, design.AuditTrailRequirement.AuditAppendCount);
        Assert.AreEqual(0, design.AuditTrailRequirement.PersistedExportEventCount);
        Assert.IsTrue(design.PassesSafetyProof);
        Assert.AreEqual("NODAL_OS_PHYSICAL_EXPORT_POLICY_EXTERNAL_AUDIT", design.NextSafeStep);
    }
}
