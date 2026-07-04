using Microsoft.VisualStudio.TestTools.UnitTesting;
using OneBrain.Core.Approval;

namespace OneBrain.Safety.Tests;

[TestClass]
[TestCategory("ApprovalHumanReview")]
[TestCategory("PhaseEApprovalHumanReview")]
public sealed class ProductLedgerPathActivePolicyTests
{
    [TestMethod]
    public void ProductLedgerPathPolicy_FailsClosedByDefault()
    {
        var result = new ProductLedgerPathActivePolicy().Evaluate(null);

        Assert.AreEqual(ProductLedgerPathActivePolicyDecision.Rejected, result.Decision);
        CollectionAssert.Contains(result.Blockers.ToArray(), ProductLedgerPathActivePolicyBlocker.MissingRequest);
        AssertNoProductEnablement(result);
    }

    [TestMethod]
    public void ProductLedgerPathPolicy_RejectsMissingOrFailedCanonicalization()
    {
        var missing = new ProductLedgerPathActivePolicy().Evaluate(ReadyRequest() with { CanonicalizationResult = null });
        var failed = new ProductLedgerPathActivePolicy().Evaluate(
            ReadyRequest() with
            {
                CanonicalizationResult = new ProductLedgerPathCanonicalizationValidator().Validate(
                    ReadyCanonicalizationRequest() with { HasResolvedReparsePointEvidence = false })
            });

        CollectionAssert.Contains(missing.Blockers.ToArray(), ProductLedgerPathActivePolicyBlocker.MissingCanonicalizationResult);
        CollectionAssert.Contains(failed.Blockers.ToArray(), ProductLedgerPathActivePolicyBlocker.FailedCanonicalizationResult);
        AssertNoProductEnablement(missing);
        AssertNoProductEnablement(failed);
    }

    [TestMethod]
    public void ProductLedgerPathPolicy_BlocksMissingSafetyEvidence()
    {
        var ready = ReadyRequest();
        var cases = new Dictionary<ProductLedgerPathActivePolicyRequest, ProductLedgerPathActivePolicyBlocker>
        {
            [ready with { HasCanonicalAllowedBoundaryEvidence = false }] = ProductLedgerPathActivePolicyBlocker.MissingCanonicalAllowedBoundaryEvidence,
            [ready with { HasNoUnresolvedReparseSymlinkJunctionRiskEvidence = false }] = ProductLedgerPathActivePolicyBlocker.UnresolvedReparseSymlinkJunctionRisk,
            [ready with { HasTocTouMitigationEvidence = false }] = ProductLedgerPathActivePolicyBlocker.TocTouBlockerPresent,
            [ready with { HasRedactionPolicyEvidence = false }] = ProductLedgerPathActivePolicyBlocker.MissingRedactionPolicyEvidence,
            [ready with { HasRetentionPolicyEvidence = false }] = ProductLedgerPathActivePolicyBlocker.MissingRetentionPolicyEvidence,
            [ready with { HasReplayFailureEvidence = false }] = ProductLedgerPathActivePolicyBlocker.MissingReplayFailureEvidence,
            [ready with { HasRollbackNonRollbackClassification = false }] = ProductLedgerPathActivePolicyBlocker.MissingRollbackNonRollbackClassification,
            [ready with { HasAuthorityEvidence = false }] = ProductLedgerPathActivePolicyBlocker.MissingAuthorityEvidence,
            [ready with { AuthorityEvidenceIsNonProduct = false }] = ProductLedgerPathActivePolicyBlocker.MissingAuthorityEvidence
        };

        foreach (var testCase in cases)
        {
            var result = new ProductLedgerPathActivePolicy().Evaluate(testCase.Key);

            Assert.AreEqual(ProductLedgerPathActivePolicyDecision.Blocked, result.Decision, testCase.Value.ToString());
            CollectionAssert.Contains(result.Blockers.ToArray(), testCase.Value, testCase.Value.ToString());
            AssertNoProductEnablement(result);
        }
    }

    [TestMethod]
    public void ProductLedgerPathPolicy_BlocksEvidenceReferenceRisks()
    {
        var ready = ReadyRequest();
        var cases = new Dictionary<ProductLedgerPathActivePolicyRequest, ProductLedgerPathActivePolicyBlocker>
        {
            [ready with { EvidenceReferences = [] }] = ProductLedgerPathActivePolicyBlocker.EvidenceReferenceMissing,
            [ready with { EvidenceReferences = ["../escape"] }] = ProductLedgerPathActivePolicyBlocker.EvidenceReferenceMalformed,
            [ready with { EvidenceReferences = ["docs/qa/ref", "DOCS/QA/REF"] }] = ProductLedgerPathActivePolicyBlocker.EvidenceReferenceDuplicate,
            [ready with { EvidenceReferencesAreStale = true }] = ProductLedgerPathActivePolicyBlocker.EvidenceReferenceStale,
            [ready with { EvidenceReferencesAreInconsistent = true }] = ProductLedgerPathActivePolicyBlocker.EvidenceReferenceInconsistent,
            [ready with { EvidenceReferences = ["docs/qa/raw-payload-secret=abc"] }] = ProductLedgerPathActivePolicyBlocker.EvidenceReferenceContainsRawPayloadOrSecretMarker,
            [ready with { EvidenceReferences = ["docs/qa/writer-ready-product-authority"] }] = ProductLedgerPathActivePolicyBlocker.EvidenceReferenceContainsProductAuthorityClaim
        };

        foreach (var testCase in cases)
        {
            var result = new ProductLedgerPathActivePolicy().Evaluate(testCase.Key);

            Assert.AreEqual(ProductLedgerPathActivePolicyDecision.Blocked, result.Decision, testCase.Value.ToString());
            CollectionAssert.Contains(result.Blockers.ToArray(), testCase.Value, testCase.Value.ToString());
            AssertNoProductEnablement(result);
        }
    }

    [TestMethod]
    public void ProductLedgerPathPolicy_BlocksProductAuthorityAndEnablementRequests()
    {
        var ready = ReadyRequest();
        var cases = new Dictionary<ProductLedgerPathActivePolicyRequest, ProductLedgerPathActivePolicyBlocker>
        {
            [ready with { TreatsHumanGoAsProductAuthority = true }] = ProductLedgerPathActivePolicyBlocker.HumanGoTreatedAsProductAuthority,
            [ready with { ClaimsLocalTempAsProductLedgerPath = true }] = ProductLedgerPathActivePolicyBlocker.LocalTempClaimedAsProductLedgerPath,
            [ready with { NoProductWriteAssertion = false }] = ProductLedgerPathActivePolicyBlocker.ProductWriteRequested,
            [ready with { RequestsWriterActivation = true }] = ProductLedgerPathActivePolicyBlocker.ProductWriteRequested,
            [ready with { NoRuntimeEnablementAssertion = false }] = ProductLedgerPathActivePolicyBlocker.RuntimeEnablementRequested,
            [ready with { RequestsRuntimeEnablement = true }] = ProductLedgerPathActivePolicyBlocker.RuntimeEnablementRequested,
            [ready with { RequestsProductLedgerPathActivation = true }] = ProductLedgerPathActivePolicyBlocker.ProductLedgerPathActivationRequested,
            [ready with { RequestsProductServiceRegistration = true }] = ProductLedgerPathActivePolicyBlocker.ProductServiceRegistrationRequested,
            [ready with { RequestsProductCommandHandler = true }] = ProductLedgerPathActivePolicyBlocker.ProductCommandHandlerRequested,
            [ready with { RequestsUiProductAction = true }] = ProductLedgerPathActivePolicyBlocker.UiProductActionRequested,
            [ready with { NoProviderCloudNetworkAssertion = false }] = ProductLedgerPathActivePolicyBlocker.ProviderCloudNetworkClaimed,
            [ready with { ClaimsProviderCloudNetwork = true }] = ProductLedgerPathActivePolicyBlocker.ProviderCloudNetworkClaimed,
            [ready with { NoWormKmsExternalTrustAssertion = false }] = ProductLedgerPathActivePolicyBlocker.WormKmsExternalTrustClaimed,
            [ready with { ClaimsWormKmsExternalTrust = true }] = ProductLedgerPathActivePolicyBlocker.WormKmsExternalTrustClaimed,
            [ready with { NoReleaseCommercialAssertion = false }] = ProductLedgerPathActivePolicyBlocker.ReleaseCommercialReadinessClaimed,
            [ready with { ClaimsReleaseCommercialReadiness = true }] = ProductLedgerPathActivePolicyBlocker.ReleaseCommercialReadinessClaimed
        };

        foreach (var testCase in cases)
        {
            var result = new ProductLedgerPathActivePolicy().Evaluate(testCase.Key);

            Assert.AreEqual(ProductLedgerPathActivePolicyDecision.Blocked, result.Decision, testCase.Value.ToString());
            CollectionAssert.Contains(result.Blockers.ToArray(), testCase.Value, testCase.Value.ToString());
            AssertNoProductEnablement(result);
        }
    }

    [TestMethod]
    public void ProductLedgerPathPolicy_AllowsOnlyPolicyAcceptedCandidateNoWrite()
    {
        var result = new ProductLedgerPathActivePolicy().Evaluate(ReadyRequest());

        Assert.AreEqual(ProductLedgerPathActivePolicyDecision.CandidateAcceptedNoWrite, result.Decision);
        Assert.IsTrue(result.CandidateAcceptedNoWrite);
        Assert.AreEqual(0, result.Blockers.Count);
        StringAssert.Contains(result.StatusText, "CANDIDATE_ACCEPTED_NO_WRITE");
        StringAssert.Contains(result.StatusText, "POLICY_ACCEPTED_CANDIDATE_ONLY");
        StringAssert.Contains(result.StatusText, "NO_ACTIVE_PRODUCT_LEDGER_PATH");
        StringAssert.Contains(result.StatusText, "NO_PRODUCT_LEDGER_WRITE");
        StringAssert.Contains(result.StatusText, "NO_PRODUCT_RUNTIME_ENABLEMENT");
        StringAssert.Contains(result.StatusText, "NO_RELEASE_COMMERCIAL");
        StringAssert.Contains(result.StatusText, "NO_EXTERNAL_TRUST");
        StringAssert.Contains(result.StatusText, "NO_WORM_KMS_CLOUD");
        AssertNoProductEnablement(result);
    }

    [TestMethod]
    public void ProductLedgerPathPolicy_SourceContainsNoProductRegistrationHandlersWritersOrProviders()
    {
        var source = File.ReadAllText(Path.Combine(
            RepoRoot(),
            "src",
            "OneBrain.Core",
            "Approval",
            "ProductLedgerPathActivePolicy.cs"));
        var forbiddenFragments = new[]
        {
            "Add" + "Singleton",
            "Add" + "Scoped",
            "Add" + "Transient",
            "IHosted" + "Service",
            "Map" + "Post",
            "Map" + "Get",
            "Add" + "CommandHandler",
            "ICommand" + "Handler",
            "Run" + "ProductAction",
            "Product" + "ActionButton",
            "Http" + "Client",
            "Web" + "Socket",
            "Db" + "Context",
            "Migration" + "Builder",
            "Save" + "Changes",
            "File." + "AppendAllText",
            "File." + "WriteAllText",
            "Directory." + "CreateDirectory",
            "Release" + "Ready = true",
            "Commercial" + "Ready = true"
        };

        foreach (var fragment in forbiddenFragments)
        {
            Assert.IsFalse(source.Contains(fragment, StringComparison.Ordinal), fragment);
        }
    }

    private static ProductLedgerPathActivePolicyRequest ReadyRequest() =>
        new(
            CanonicalizationResult: new ProductLedgerPathCanonicalizationValidator().Validate(ReadyCanonicalizationRequest()),
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

    private static ProductLedgerPathCanonicalizationRequest ReadyCanonicalizationRequest() =>
        new(
            CandidatePath: Path.Combine(RepoRoot(), "src", "OneBrain.Core"),
            AllowedRootPath: RepoRoot(),
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
            HardlinkOrMountAliasRiskUnresolved: false);

    private static void AssertNoProductEnablement(ProductLedgerPathActivePolicyResult result)
    {
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
                "ProductLedgerPathActivePolicy.cs")))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        Assert.Fail("repo root not found");
        return string.Empty;
    }
}
