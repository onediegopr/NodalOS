using Microsoft.VisualStudio.TestTools.UnitTesting;
using OneBrain.Core.Approval;

namespace OneBrain.Recipes.Tests;

[TestClass]
[TestCategory("ApprovalHumanReview")]
[TestCategory("PhaseEApprovalHumanReview")]
public sealed class ProductLedgerPathReadinessScaffoldTests
{
    [TestMethod]
    public void ProductLedgerPathScaffold_ProducesDisabledTestOnlyNoWritePreview()
    {
        var result = new ProductLedgerPathReadinessScaffold().Evaluate(
            new ProductLedgerPathReadinessRequest(
                ExplicitTestOnlyMode: true,
                NoProductWriteAssertion: true,
                NoRuntimeEnablementAssertion: true,
                NoReleaseCommercialAssertion: true,
                ClaimsExternalTrust: false,
                ClaimsWormKmsCloud: false,
                Canonicalization: new CanonicalizationRiskPreview(
                    CandidatePath: @"C:\NodalOS\disabled-ledger-preview",
                    RelativePathExplicitlyHandled: false,
                    HasCanonicalPathEvidence: true,
                    HasJailBoundaryEvidence: true,
                    CanonicalPathInsideJail: true,
                    HasTocTouMitigationEvidence: true,
                    TocTouEvidenceStale: false,
                    CasingNormalizationMismatch: false,
                    UnicodeNormalizationMismatch: false,
                    PathAppearsOutsideButStringNormalizedInside: false,
                    ClaimsLocalTempAsProductLedgerPath: false,
                    ClaimsProductLedgerReadyWithoutProductPolicy: false),
                ReparsePointRisk: new ReparsePointRiskPreview(
                    HasSymlinkJunctionReparseEvidence: true,
                    ReparseEvidenceStale: false,
                    ReparseEvidenceConflicting: false,
                    SymlinkRiskUnresolved: false,
                    JunctionRiskUnresolved: false,
                    ReparsePointRiskUnresolved: false,
                    HardlinkOrMountAliasRiskUnresolved: false),
                Authority: new AuthorityReadinessPreview(
                    HasHumanApprovalEvidence: true,
                    TreatsHumanGoAsProductAuthority: false,
                    OperatorIdentityEvidence: "operator:test-only-fixture",
                    LocalOperatorSessionEvidence: "session:test-only-fixture",
                    EvidenceReferences: ["docs/qa/product-ledger-path-readiness/report.md"],
                    ApprovalIsStale: false,
                    ApprovalForDifferentScope: false,
                    ApprovalForDifferentLedgerPath: false,
                    ApprovalForDifferentRuntimeFlag: false,
                    ApprovalReplayOrTamperRisk: false,
                    ApprovalAfterRiskChanges: false,
                    EvidenceReferencesAreStale: false,
                    EvidenceReferencesForWrongRequestId: false,
                    EvidenceReferencesForWrongRiskVersion: false,
                    EvidenceReferencesInconsistent: false,
                    ApprovalAttemptsProviderCloudKmsWormExternalTrust: false,
                    ApprovalAttemptsLiveAutomation: false,
                    ApprovalAttemptsReleaseCommercial: false),
                HasRedactionPolicyEvidence: true,
                HasRetentionPolicyEvidence: true,
                HasReplayFailureEvidence: true,
                HasRollbackNonRollbackClassification: true));

        Assert.AreEqual(ProductLedgerPathReadinessDecision.ReadinessPreviewAllowed, result.Decision);
        Assert.AreEqual(0, result.Blockers.Count);
        Assert.IsFalse(result.ProductLedgerPathActive);
        Assert.IsFalse(result.ProductLedgerWriteAllowed);
        Assert.IsFalse(result.ProductRuntimeEnabled);
        Assert.IsFalse(result.ProductServiceRegistrationAllowed);
        Assert.IsFalse(result.ProductCommandHandlersAllowed);
        Assert.IsFalse(result.UiProductActionsAllowed);
        Assert.IsFalse(result.DbProviderCloudNetworkAllowed);
        Assert.IsFalse(result.KmsWormExternalTrustAllowed);
        Assert.IsFalse(result.LiveAutomationAllowed);
        Assert.IsFalse(result.ReleaseCommercialReady);
        StringAssert.Contains(result.StatusText, "NO_PRODUCT_LEDGER_WRITE");
        StringAssert.Contains(result.StatusText, "NO_PRODUCT_RUNTIME_ENABLEMENT");
    }

    [TestMethod]
    public void ProductLedgerPathScaffold_RejectsCorpusPreviewWithoutProductEffects()
    {
        var result = new ProductLedgerPathReadinessScaffold().Evaluate(
            new ProductLedgerPathReadinessRequest(
                ExplicitTestOnlyMode: true,
                NoProductWriteAssertion: true,
                NoRuntimeEnablementAssertion: true,
                NoReleaseCommercialAssertion: true,
                ClaimsExternalTrust: false,
                ClaimsWormKmsCloud: false,
                Canonicalization: new CanonicalizationRiskPreview(
                    CandidatePath: @"C:\safe\ledger:stream",
                    RelativePathExplicitlyHandled: false,
                    HasCanonicalPathEvidence: true,
                    HasJailBoundaryEvidence: true,
                    CanonicalPathInsideJail: true,
                    HasTocTouMitigationEvidence: true,
                    TocTouEvidenceStale: true,
                    CasingNormalizationMismatch: false,
                    UnicodeNormalizationMismatch: true,
                    PathAppearsOutsideButStringNormalizedInside: true,
                    ClaimsLocalTempAsProductLedgerPath: false,
                    ClaimsProductLedgerReadyWithoutProductPolicy: false),
                ReparsePointRisk: new ReparsePointRiskPreview(
                    HasSymlinkJunctionReparseEvidence: true,
                    ReparseEvidenceStale: true,
                    ReparseEvidenceConflicting: true,
                    SymlinkRiskUnresolved: false,
                    JunctionRiskUnresolved: false,
                    ReparsePointRiskUnresolved: false,
                    HardlinkOrMountAliasRiskUnresolved: true),
                Authority: new AuthorityReadinessPreview(
                    HasHumanApprovalEvidence: true,
                    TreatsHumanGoAsProductAuthority: false,
                    OperatorIdentityEvidence: "operator:test-only-fixture",
                    LocalOperatorSessionEvidence: "session:test-only-fixture",
                    EvidenceReferences: ["docs/qa/product-enabled"],
                    ApprovalIsStale: false,
                    ApprovalForDifferentScope: false,
                    ApprovalForDifferentLedgerPath: false,
                    ApprovalForDifferentRuntimeFlag: false,
                    ApprovalReplayOrTamperRisk: true,
                    ApprovalAfterRiskChanges: true,
                    EvidenceReferencesAreStale: true,
                    EvidenceReferencesForWrongRequestId: true,
                    EvidenceReferencesForWrongRiskVersion: true,
                    EvidenceReferencesInconsistent: true,
                    ApprovalAttemptsProviderCloudKmsWormExternalTrust: true,
                    ApprovalAttemptsLiveAutomation: true,
                    ApprovalAttemptsReleaseCommercial: true),
                HasRedactionPolicyEvidence: true,
                HasRetentionPolicyEvidence: true,
                HasReplayFailureEvidence: true,
                HasRollbackNonRollbackClassification: true));

        Assert.AreEqual(ProductLedgerPathReadinessDecision.Rejected, result.Decision);
        CollectionAssert.Contains(result.Blockers.ToArray(), ProductLedgerPathBlocker.AlternateDataStreamRisk);
        CollectionAssert.Contains(result.Blockers.ToArray(), ProductLedgerPathBlocker.ReparsePointEvidenceStale);
        CollectionAssert.Contains(result.Blockers.ToArray(), ProductLedgerPathBlocker.ReparsePointEvidenceConflicting);
        CollectionAssert.Contains(result.Blockers.ToArray(), ProductLedgerPathBlocker.ApprovalEvidenceRefsContainLiveProductWording);
        Assert.IsFalse(result.ProductLedgerPathActive);
        Assert.IsFalse(result.ProductLedgerWriteAllowed);
        Assert.IsFalse(result.ProductRuntimeEnabled);
        Assert.IsFalse(result.ReleaseCommercialReady);
    }
}
