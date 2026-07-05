using Microsoft.VisualStudio.TestTools.UnitTesting;
using OneBrain.Core.Approval;

namespace OneBrain.Recipes.Tests;

[TestClass]
[TestCategory("ApprovalHumanReview")]
[TestCategory("PhaseEApprovalHumanReview")]
public sealed class ProductLedgerLocalApprovalExecutionCandidateRecipeTests
{
    private static readonly DateTimeOffset Now = new(2026, 7, 5, 12, 0, 0, TimeSpan.Zero);

    [TestMethod]
    public void LocalApprovalExecutionCandidate_ViewLedgerReadinessCompletesAsInternalInMemoryOnly()
    {
        var result = new ProductLedgerLocalApprovalExecutionCandidate().Execute(
            ReadyRequest(ProductLedgerInternalCommandKind.ViewLedgerReadiness));

        Assert.AreEqual(ProductLedgerLocalApprovalExecutionDecision.CompletedReadOnlyInMemory, result.Decision);
        Assert.AreEqual(ProductLedgerInternalCommandKind.ViewLedgerReadiness, result.CandidateActionKind);
        Assert.AreEqual(ProductLedgerInternalCommandDecision.CompletedReadOnlyInMemory, result.CommandResult!.Decision);
        Assert.IsTrue(result.LocalOnly);
        Assert.IsTrue(result.InternalOnly);
        Assert.IsTrue(result.DefaultOff);
        Assert.IsTrue(result.ReadOnlyOrInMemory);
        Assert.IsFalse(result.PublicUiActionAvailable);
        Assert.IsFalse(result.ProductCommandHandlerAvailable);
        Assert.IsFalse(result.PhysicalExportCreated);
        Assert.IsFalse(result.FileWritePerformed);
        Assert.IsFalse(result.ReleaseCommercialReady);
    }

    [TestMethod]
    public void LocalApprovalExecutionCandidate_RejectsBoundedExportAndEvidenceMismatch()
    {
        var export = new ProductLedgerLocalApprovalExecutionCandidate().Execute(
            ReadyRequest(ProductLedgerInternalCommandKind.LocalReportPhysicalExportBoundedInternal));
        var mismatch = new ProductLedgerLocalApprovalExecutionCandidate().Execute(
            ReadyRequest(ProductLedgerInternalCommandKind.ViewLedgerReadiness) with { CurrentEvidenceHash = "changed" });

        Assert.AreEqual(ProductLedgerLocalApprovalExecutionDecision.Rejected, export.Decision);
        CollectionAssert.Contains(
            export.Blockers.ToArray(),
            ProductLedgerLocalApprovalExecutionBlocker.CommandNotAllowedForApprovalExecution);
        Assert.IsNull(export.CommandResult);
        Assert.IsFalse(export.PhysicalExportCreated);
        Assert.IsFalse(export.FileWritePerformed);

        Assert.AreEqual(ProductLedgerLocalApprovalExecutionDecision.Rejected, mismatch.Decision);
        CollectionAssert.Contains(
            mismatch.Blockers.ToArray(),
            ProductLedgerLocalApprovalExecutionBlocker.EvidenceMismatch);
    }

    private static ProductLedgerLocalApprovalExecutionRequest ReadyRequest(ProductLedgerInternalCommandKind command) =>
        new(
            ExplicitLocalOnlyInternalApprovalExecutionScope: true,
            ApprovalId: "approval-local-only-001",
            ApprovedAtUtc: Now.AddMinutes(-1),
            NowUtc: Now,
            MaxApprovalAge: TimeSpan.FromMinutes(5),
            CandidateActionKind: command,
            ApprovedActionName: command.ToString(),
            ApprovedEvidenceHash: "evidence-hash-001",
            CurrentEvidenceHash: "evidence-hash-001",
            PolicyRecheckPassed: true,
            ReadModelVerified: true,
            RequestsPublicUiAction: false,
            RequestsDestructiveAction: false,
            RequestsProductCommandHandler: false,
            RequestsProductiveServiceRegistration: false,
            ClaimsProviderCloudNetwork: false,
            ClaimsDbMigration: false,
            ClaimsKmsWormExternalTrust: false,
            ClaimsBrowserCdpWcuOcrRecipesLive: false,
            ClaimsReleaseCommercial: false,
            ClaimsExternalTelemetryOrSync: false,
            ClaimsBillingLicensingCloud: false,
            RequestsPhysicalExport: false,
            RequestsFileWrite: false,
            ClaimsAppendOutsideBoundedWriter: false,
            ClaimsArbitraryPathInput: false,
            ClaimsRawPayloadOrSecret: false);
}

