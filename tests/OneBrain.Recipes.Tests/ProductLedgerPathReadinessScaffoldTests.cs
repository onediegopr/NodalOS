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
                    CasingNormalizationMismatch: false,
                    UnicodeNormalizationMismatch: false,
                    ClaimsLocalTempAsProductLedgerPath: false,
                    ClaimsProductLedgerReadyWithoutProductPolicy: false),
                ReparsePointRisk: new ReparsePointRiskPreview(
                    HasSymlinkJunctionReparseEvidence: true,
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
}
