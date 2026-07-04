using Microsoft.VisualStudio.TestTools.UnitTesting;
using OneBrain.Core.Approval;

namespace OneBrain.Recipes.Tests;

[TestClass]
[TestCategory("ApprovalHumanReview")]
[TestCategory("PhaseEApprovalHumanReview")]
public sealed class ProductLedgerPathWriterScaffoldDisabledTests
{
    [TestMethod]
    public void WriterScaffold_ProducesDisabledTestOnlyNoWriteScaffold()
    {
        var result = new ProductLedgerPathWriterScaffoldDisabled().Evaluate(ReadyRequest());

        Assert.AreEqual(ProductLedgerPathWriterScaffoldDecision.DisabledWriterScaffoldReady, result.Decision);
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
        StringAssert.Contains(result.StatusText, "DISABLED_WRITER_SCAFFOLD_TEST_ONLY");
        StringAssert.Contains(result.StatusText, "NO_PRODUCT_LEDGER_WRITE");
    }

    [TestMethod]
    public void WriterScaffold_RejectsUnsafeRecipeCorpus()
    {
        var result = new ProductLedgerPathWriterScaffoldDisabled().Evaluate(
            ReadyRequest() with
            {
                RequestsWriterActivation = true,
                RequestsRuntimeEnablement = true,
                RequestsProductLedgerPathActivation = true,
                RequestsProductServiceRegistration = true,
                RequestsProductCommandHandler = true,
                RequestsUiProductAction = true,
                ClaimsProviderCloudNetwork = true,
                ClaimsWormKmsExternalTrust = true,
                ClaimsReleaseCommercialReadiness = true
            });

        Assert.AreEqual(ProductLedgerPathWriterScaffoldDecision.Blocked, result.Decision);
        CollectionAssert.Contains(result.Blockers.ToArray(), ProductLedgerPathWriterScaffoldBlocker.ProductWriteRequested);
        CollectionAssert.Contains(result.Blockers.ToArray(), ProductLedgerPathWriterScaffoldBlocker.RuntimeEnablementRequested);
        CollectionAssert.Contains(result.Blockers.ToArray(), ProductLedgerPathWriterScaffoldBlocker.ProductLedgerPathActivationRequested);
        CollectionAssert.Contains(result.Blockers.ToArray(), ProductLedgerPathWriterScaffoldBlocker.ProductServiceRegistrationRequested);
        CollectionAssert.Contains(result.Blockers.ToArray(), ProductLedgerPathWriterScaffoldBlocker.ProductCommandHandlerRequested);
        CollectionAssert.Contains(result.Blockers.ToArray(), ProductLedgerPathWriterScaffoldBlocker.UiProductActionRequested);
        CollectionAssert.Contains(result.Blockers.ToArray(), ProductLedgerPathWriterScaffoldBlocker.ProviderCloudNetworkClaimed);
        CollectionAssert.Contains(result.Blockers.ToArray(), ProductLedgerPathWriterScaffoldBlocker.WormKmsExternalTrustClaimed);
        CollectionAssert.Contains(result.Blockers.ToArray(), ProductLedgerPathWriterScaffoldBlocker.ReleaseCommercialReadinessClaimed);
        Assert.IsFalse(result.ProductLedgerPathActive);
        Assert.IsFalse(result.ProductLedgerWriteAllowed);
        Assert.IsFalse(result.ProductRuntimeEnabled);
    }

    private static ProductLedgerPathWriterScaffoldRequest ReadyRequest() =>
        new(
            PersistedCandidateResult: ReadyPersistedCandidate(),
            ExplicitDisabledTestOnlyMode: true,
            NoProductWriterAssertion: true,
            NoProductWriteAssertion: true,
            NoRuntimeEnablementAssertion: true,
            NoProductLedgerPathActivationAssertion: true,
            NoProductServiceRegistrationAssertion: true,
            NoProductCommandHandlerAssertion: true,
            NoUiProductActionAssertion: true,
            NoProviderCloudNetworkAssertion: true,
            NoWormKmsExternalTrustAssertion: true,
            NoReleaseCommercialAssertion: true,
            HasRedactionBeforePersistenceEvidence: true,
            HasFailureReplayRollbackEvidence: true,
            RequestsWriterActivation: false,
            RequestsRuntimeEnablement: false,
            RequestsProductLedgerPathActivation: false,
            RequestsProductServiceRegistration: false,
            RequestsProductCommandHandler: false,
            RequestsUiProductAction: false,
            ClaimsProviderCloudNetwork: false,
            ClaimsWormKmsExternalTrust: false,
            ClaimsReleaseCommercialReadiness: false);

    private static ProductLedgerPathPersistedCandidateResult ReadyPersistedCandidate()
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
        var policy = new ProductLedgerPathActivePolicy().Evaluate(
            new ProductLedgerPathActivePolicyRequest(
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
                ClaimsWormKmsExternalTrust: false));
        return new ProductLedgerPathPersistedCandidateRegistry().Persist(
            new ProductLedgerPathPersistedCandidateRequest(
                CandidateId: "ledger-candidate-001",
                ActivePolicyResult: policy,
                CanonicalizationResult: canonicalization,
                EvidenceReferences: ["docs/qa/product-ledger-path-persisted-candidate-local-only-no-write/report.md"],
                ClaimsLocalTempAsProductLedgerPath: false,
                RequestsProductLedgerPathActivation: false,
                RequestsWriterActivation: false,
                RequestsRuntimeEnablement: false,
                RequestsProductServiceRegistration: false,
                RequestsProductCommandHandler: false,
                RequestsUiProductAction: false,
                ClaimsProviderCloudNetwork: false,
                ClaimsWormKmsExternalTrust: false,
                ClaimsReleaseCommercialReadiness: false));
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
