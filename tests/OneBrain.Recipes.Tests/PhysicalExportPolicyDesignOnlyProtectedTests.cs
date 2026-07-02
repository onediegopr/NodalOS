using Microsoft.VisualStudio.TestTools.UnitTesting;
using OneBrain.Core.Approval;

namespace OneBrain.Recipes.Tests;

[TestClass]
[TestCategory("ApprovalHumanReview")]
[TestCategory("PhaseEApprovalHumanReview")]
public sealed class PhysicalExportPolicyDesignOnlyProtectedTests
{
    [TestMethod]
    public void PhysicalExportPolicyDesign_IsDeterministicDesignOnlyAndReadOnly()
    {
        var first = PhysicalExportPolicyDesignOnlyProtectedPresenter.CreateFixture();
        var second = PhysicalExportPolicyDesignOnlyProtectedPresenter.CreateFixture();

        Assert.AreEqual("nodal-os.approval.physical-export-policy.design-only.protected.fixture.v1", first.DesignId);
        Assert.AreEqual(PhysicalExportPolicyDesignStatus.DesignOnly, first.Status);
        Assert.AreEqual("DESIGN_ONLY_READ_ONLY_PREVIEW_NO_PHYSICAL_EXPORT_NO_IO_NO_RUNTIME", first.Mode);
        Assert.AreEqual(first.DesignId, second.DesignId);
        Assert.AreEqual(first.Mode, second.Mode);
        CollectionAssert.AreEqual(first.ExportFormatPreviews.Select(format => format.FormatName).ToArray(), second.ExportFormatPreviews.Select(format => format.FormatName).ToArray());
        CollectionAssert.AreEqual(first.BlockedReasons.ToArray(), second.BlockedReasons.ToArray());
        Assert.IsTrue(first.AntiCapabilityProof.NoSideEffectProof.ReadOnly);
        Assert.IsTrue(first.AntiCapabilityProof.NoSideEffectProof.Deterministic);
        Assert.IsTrue(first.AntiCapabilityProof.NoSideEffectProof.FixtureSafe);
    }

    [TestMethod]
    public void PhysicalExportPolicyDesign_CoversAllFutureFormatPreviews()
    {
        var design = PhysicalExportPolicyDesignOnlyProtectedPresenter.CreateFixture();
        var kinds = design.ExportFormatPreviews.Select(format => format.FormatKind).ToHashSet();

        Assert.AreEqual(6, design.ExportFormatPreviews.Count);
        Assert.IsTrue(kinds.Contains(PhysicalExportFormatKind.PdfFuture));
        Assert.IsTrue(kinds.Contains(PhysicalExportFormatKind.DocxFuture));
        Assert.IsTrue(kinds.Contains(PhysicalExportFormatKind.JsonFuture));
        Assert.IsTrue(kinds.Contains(PhysicalExportFormatKind.MarkdownFuture));
        Assert.IsTrue(kinds.Contains(PhysicalExportFormatKind.ClipboardFuture));
        Assert.IsTrue(kinds.Contains(PhysicalExportFormatKind.DownloadFuture));
        Assert.IsTrue(design.ExportFormatPreviews.All(format => format.FormatName.EndsWith("Future", StringComparison.Ordinal)));
    }

    [TestMethod]
    public void Requirements_BlockExportThroughRedactionConsentDestinationEvidenceAuditAndRetention()
    {
        var design = PhysicalExportPolicyDesignOnlyProtectedPresenter.CreateFixture();

        CollectionAssert.Contains(design.RedactionRequirement.BlockedReasons.ToArray(), PhysicalExportBlockedReason.NoRedactionRuntime);
        CollectionAssert.Contains(design.ConsentRequirement.BlockedReasons.ToArray(), PhysicalExportBlockedReason.NoUserConsentPolicy);
        CollectionAssert.Contains(design.DestinationRequirement.BlockedReasons.ToArray(), PhysicalExportBlockedReason.NoDestinationPolicy);
        CollectionAssert.Contains(design.EvidenceSelectionRequirement.BlockedReasons.ToArray(), PhysicalExportBlockedReason.NoEvidenceSelectionPolicy);
        CollectionAssert.Contains(design.AuditTrailRequirement.BlockedReasons.ToArray(), PhysicalExportBlockedReason.NoDurableAuditTrailImplementation);
        CollectionAssert.Contains(design.RetentionDeletionRequirement.BlockedReasons.ToArray(), PhysicalExportBlockedReason.NoRetentionPolicy);
        CollectionAssert.Contains(design.RetentionDeletionRequirement.BlockedReasons.ToArray(), PhysicalExportBlockedReason.NoDeletionPolicy);
        Assert.IsTrue(design.EvidenceSelectionRequirement.AllowedEvidenceClassesFuture.Count >= 5);
    }

    [TestMethod]
    public void FutureImplementationChecklist_RequiresAuditBeforeAnyPhysicalExport()
    {
        var checklist = PhysicalExportPolicyDesignOnlyProtectedPresenter.CreateFixture().FutureImplementationChecklist;
        var text = string.Join(
            "\n",
            string.Join("\n", checklist.RequiredBeforeRealPhysicalExport),
            string.Join("\n", checklist.RequiredExternalAudits),
            string.Join("\n", checklist.RequiredPolicyDecisions),
            string.Join("\n", checklist.ExplicitNonGoals));

        StringAssert.Contains(text, "redaction runtime design external audit");
        StringAssert.Contains(text, "durable audit trail implementation approval");
        StringAssert.Contains(text, "destination policy and workspace boundary approval");
        StringAssert.Contains(text, "physical export policy external audit");
        StringAssert.Contains(text, "no physical export");
        StringAssert.Contains(text, "no file read or write");
        StringAssert.Contains(text, "no clipboard");
        StringAssert.Contains(text, "no download");
        StringAssert.Contains(text, "no runtime/live");
    }

    [TestMethod]
    public void CurrentBaseline_AnchorsExistingHumanReviewExportAsInMemoryPreview()
    {
        var baseline = PhysicalExportPolicyDesignOnlyProtectedPresenter.CreateFixture().CurrentPhysicalExportBaseline;
        var text = string.Join("\n", baseline);

        Assert.IsTrue(baseline.Count >= 5);
        StringAssert.Contains(text, "HumanReviewPacketExportReadOnlyPreview");
        StringAssert.Contains(text, "in memory only");
        StringAssert.Contains(text, "PhysicalFileCreated, ClipboardUsed, DownloadStarted and ExportActionsCount at zero");
        StringAssert.Contains(text, "No file writer");
    }

    [TestMethod]
    public void PhysicalExportPolicyDesign_ReportTextHasNoActiveReadinessOverclaim()
    {
        var design = PhysicalExportPolicyDesignOnlyProtectedPresenter.CreateFixture();
        var text = string.Join(
            "\n",
            design.Title,
            design.Mode,
            string.Join("\n", design.Warnings),
            string.Join("\n", design.FutureImplementationChecklist.ExplicitNonGoals),
            string.Join("\n", design.CurrentPhysicalExportBaseline));

        Assert.IsFalse(text.Contains("production" + "-ready", StringComparison.OrdinalIgnoreCase));
        Assert.IsFalse(text.Contains("release" + "-ready", StringComparison.OrdinalIgnoreCase));
        Assert.IsFalse(text.Contains("physical export enabled", StringComparison.OrdinalIgnoreCase));
        Assert.IsFalse(text.Contains("download enabled", StringComparison.OrdinalIgnoreCase));
        Assert.IsFalse(text.Contains("clipboard enabled", StringComparison.OrdinalIgnoreCase));
        Assert.IsFalse(text.Contains("file writer enabled", StringComparison.OrdinalIgnoreCase));
        Assert.IsFalse(text.Contains("PDF generated", StringComparison.OrdinalIgnoreCase));
        Assert.IsFalse(text.Contains("DOCX generated", StringComparison.OrdinalIgnoreCase));
        Assert.IsFalse(text.Contains("runtime enabled", StringComparison.OrdinalIgnoreCase));
        Assert.IsFalse(text.Contains("service registered", StringComparison.OrdinalIgnoreCase));
        Assert.IsFalse(text.Contains("command handler registered", StringComparison.OrdinalIgnoreCase));
    }
}
