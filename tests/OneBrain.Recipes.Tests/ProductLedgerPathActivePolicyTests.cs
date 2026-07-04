using Microsoft.VisualStudio.TestTools.UnitTesting;
using OneBrain.Core.Approval;

namespace OneBrain.Recipes.Tests;

[TestClass]
[TestCategory("ApprovalHumanReview")]
[TestCategory("PhaseEApprovalHumanReview")]
public sealed class ProductLedgerPathActivePolicyTests
{
    [TestMethod]
    public void ProductLedgerPathPolicy_ProducesPolicyAcceptedCandidateOnly()
    {
        var result = new ProductLedgerPathActivePolicy().Evaluate(ReadyRequest());

        Assert.AreEqual(ProductLedgerPathActivePolicyDecision.CandidateAcceptedNoWrite, result.Decision);
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
        StringAssert.Contains(result.StatusText, "CANDIDATE_ACCEPTED_NO_WRITE");
        StringAssert.Contains(result.StatusText, "NO_ACTIVE_PRODUCT_LEDGER_PATH");
        StringAssert.Contains(result.StatusText, "NO_PRODUCT_LEDGER_WRITE");
        StringAssert.Contains(result.StatusText, "NO_PRODUCT_RUNTIME_ENABLEMENT");
    }

    [TestMethod]
    public void ProductLedgerPathPolicy_RejectsUnsafeRecipeAuthorityCorpus()
    {
        var result = new ProductLedgerPathActivePolicy().Evaluate(
            ReadyRequest() with
            {
                EvidenceReferences = ["docs/qa/raw-payload-writer-ready-secret=abc"],
                TreatsHumanGoAsProductAuthority = true,
                RequestsProductLedgerPathActivation = true,
                RequestsWriterActivation = true,
                RequestsRuntimeEnablement = true,
                RequestsProductServiceRegistration = true,
                RequestsProductCommandHandler = true,
                RequestsUiProductAction = true,
                ClaimsProviderCloudNetwork = true,
                ClaimsWormKmsExternalTrust = true,
                ClaimsReleaseCommercialReadiness = true
            });

        Assert.AreEqual(ProductLedgerPathActivePolicyDecision.Blocked, result.Decision);
        CollectionAssert.Contains(result.Blockers.ToArray(), ProductLedgerPathActivePolicyBlocker.EvidenceReferenceContainsRawPayloadOrSecretMarker);
        CollectionAssert.Contains(result.Blockers.ToArray(), ProductLedgerPathActivePolicyBlocker.EvidenceReferenceContainsProductAuthorityClaim);
        CollectionAssert.Contains(result.Blockers.ToArray(), ProductLedgerPathActivePolicyBlocker.HumanGoTreatedAsProductAuthority);
        CollectionAssert.Contains(result.Blockers.ToArray(), ProductLedgerPathActivePolicyBlocker.ProductLedgerPathActivationRequested);
        CollectionAssert.Contains(result.Blockers.ToArray(), ProductLedgerPathActivePolicyBlocker.ProductWriteRequested);
        CollectionAssert.Contains(result.Blockers.ToArray(), ProductLedgerPathActivePolicyBlocker.RuntimeEnablementRequested);
        CollectionAssert.Contains(result.Blockers.ToArray(), ProductLedgerPathActivePolicyBlocker.ProductServiceRegistrationRequested);
        CollectionAssert.Contains(result.Blockers.ToArray(), ProductLedgerPathActivePolicyBlocker.ProductCommandHandlerRequested);
        CollectionAssert.Contains(result.Blockers.ToArray(), ProductLedgerPathActivePolicyBlocker.UiProductActionRequested);
        CollectionAssert.Contains(result.Blockers.ToArray(), ProductLedgerPathActivePolicyBlocker.ProviderCloudNetworkClaimed);
        CollectionAssert.Contains(result.Blockers.ToArray(), ProductLedgerPathActivePolicyBlocker.WormKmsExternalTrustClaimed);
        CollectionAssert.Contains(result.Blockers.ToArray(), ProductLedgerPathActivePolicyBlocker.ReleaseCommercialReadinessClaimed);
        Assert.IsFalse(result.ProductLedgerPathActive);
        Assert.IsFalse(result.ProductLedgerWriteAllowed);
        Assert.IsFalse(result.ProductRuntimeEnabled);
        Assert.IsFalse(result.ReleaseCommercialReady);
    }

    private static ProductLedgerPathActivePolicyRequest ReadyRequest()
    {
        var root = RepoRoot();
        var canonicalization = new ProductLedgerPathCanonicalizationValidator().Validate(
            new ProductLedgerPathCanonicalizationRequest(
                CandidatePath: Path.Combine(root, "src", "OneBrain.Core"),
                AllowedRootPath: root,
                ExplicitLocalOnlyMode: true,
                NoProductLedgerWriteAssertion: true,
                NoRuntimeEnablementAssertion: true,
                NoReleaseCommercialAssertion: true,
                ClaimsProductLedgerActive: false,
                ClaimsProductReady: false,
                ClaimsExternalTrust: false,
                ClaimsWormKmsCloud: false,
                ClaimsLocalTempAsProductLedgerPath: false,
                HasResolvedReparsePointEvidence: true,
                HasTocTouMitigationEvidence: true,
                HardlinkOrMountAliasRiskUnresolved: false));

        return new ProductLedgerPathActivePolicyRequest(
            CanonicalizationResult: canonicalization,
            HasCanonicalAllowedBoundaryEvidence: true,
            HasNoUnresolvedReparseSymlinkJunctionRiskEvidence: true,
            HasTocTouMitigationEvidence: true,
            HasRedactionPolicyEvidence: true,
            HasRetentionPolicyEvidence: true,
            HasReplayFailureEvidence: true,
            HasRollbackNonRollbackClassification: true,
            HasAuthorityEvidence: true,
            AuthorityEvidenceIsNonProduct: true,
            TreatsHumanGoAsProductAuthority: false,
            EvidenceReferences: ["docs/qa/product-ledger-path-active-policy-local-only-no-write/report.md"],
            EvidenceReferencesAreStale: false,
            EvidenceReferencesAreInconsistent: false,
            ClaimsLocalTempAsProductLedgerPath: false,
            NoProductWriteAssertion: true,
            NoRuntimeEnablementAssertion: true,
            NoReleaseCommercialAssertion: true,
            NoProviderCloudNetworkAssertion: true,
            NoWormKmsExternalTrustAssertion: true,
            RequestsProductLedgerPathActivation: false,
            RequestsWriterActivation: false,
            RequestsRuntimeEnablement: false,
            RequestsProductServiceRegistration: false,
            RequestsProductCommandHandler: false,
            RequestsUiProductAction: false,
            ClaimsReleaseCommercialReadiness: false,
            ClaimsProviderCloudNetwork: false,
            ClaimsWormKmsExternalTrust: false);
    }

    private static string RepoRoot()
    {
        var directory = new DirectoryInfo(Directory.GetCurrentDirectory());
        while (directory is not null)
        {
            if (File.Exists(Path.Combine(
                directory.FullName,
                "src",
                "OneBrain.Core",
                "Approval",
                "ProductLedgerPathReadinessScaffold.cs")))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        Assert.Fail("repo root not found");
        return string.Empty;
    }
}
