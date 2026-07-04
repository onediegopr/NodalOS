using Microsoft.VisualStudio.TestTools.UnitTesting;
using OneBrain.Core.Approval;

namespace OneBrain.Recipes.Tests;

[TestClass]
[TestCategory("ApprovalHumanReview")]
[TestCategory("PhaseEApprovalHumanReview")]
public sealed class ProductLedgerPathLocalOnlyActiveWriterTests
{
    [TestMethod]
    public void LocalOnlyActiveWriter_ActivatesAppendsAndReadsBoundedLocalLedger()
    {
        using var fixture = LedgerFixture.Create();
        var writer = new ProductLedgerPathLocalOnlyActiveWriter();
        var activation = writer.Activate(ReadyActivationRequest(fixture));
        var append = writer.Append(ReadyAppendRequest(activation));

        Assert.AreEqual(ProductLedgerPathLocalOnlyActivationDecision.ActivatedLocalOnly, activation.Decision);
        Assert.AreEqual(ProductLedgerPathLocalOnlyWriterDecision.AppendedLocalOnly, append.Decision);
        Assert.IsTrue(activation.ProductLedgerPathActive);
        Assert.IsTrue(activation.ProductLedgerWriteAllowed);
        Assert.IsTrue(append.ProductLedgerPathActive);
        Assert.IsTrue(append.ProductLedgerWriteAllowed);
        Assert.IsFalse(activation.ProductRuntimeEnabled);
        Assert.IsFalse(append.ProductRuntimeEnabled);
        Assert.IsFalse(append.ProductServiceRegistrationAllowed);
        Assert.IsFalse(append.UiProductActionsAllowed);
        Assert.IsTrue(append.ActiveLedgerFilePath!.StartsWith(fixture.LedgerRoot, StringComparison.OrdinalIgnoreCase));

        var entries = writer.ReadVerified(activation);
        Assert.AreEqual(1, entries.Count);
        Assert.AreEqual(new string('e', 64), entries[0].SafePayloadHash);
    }

    [TestMethod]
    public void LocalOnlyActiveWriter_RejectsUnsafeRecipeCorpus()
    {
        using var fixture = LedgerFixture.Create();
        var writer = new ProductLedgerPathLocalOnlyActiveWriter();
        var activation = writer.Activate(ReadyActivationRequest(fixture));
        var append = writer.Append(
            ReadyAppendRequest(activation) with
            {
                RuntimeFlagStillDefaultOff = false,
                RequestsRuntimeEnablement = true,
                RequestsProductServiceRegistration = true,
                RequestsProductCommandHandler = true,
                RequestsUiProductAction = true,
                ClaimsProviderCloudNetwork = true,
                ClaimsWormKmsExternalTrust = true,
                ClaimsDbMigration = true,
                ClaimsBrowserCdpWcuOcrRecipesLive = true,
                ClaimsReleaseCommercialReadiness = true
            });

        Assert.AreEqual(ProductLedgerPathLocalOnlyWriterDecision.Blocked, append.Decision);
        CollectionAssert.Contains(append.Blockers.ToArray(), ProductLedgerPathLocalOnlyBlocker.RuntimeFlagNotDefaultOff);
        CollectionAssert.Contains(append.Blockers.ToArray(), ProductLedgerPathLocalOnlyBlocker.RuntimeEnablementRequested);
        CollectionAssert.Contains(append.Blockers.ToArray(), ProductLedgerPathLocalOnlyBlocker.ProductServiceRegistrationRequested);
        CollectionAssert.Contains(append.Blockers.ToArray(), ProductLedgerPathLocalOnlyBlocker.ProductCommandHandlerRequested);
        CollectionAssert.Contains(append.Blockers.ToArray(), ProductLedgerPathLocalOnlyBlocker.UiProductActionRequested);
        CollectionAssert.Contains(append.Blockers.ToArray(), ProductLedgerPathLocalOnlyBlocker.ProviderCloudNetworkClaimed);
        CollectionAssert.Contains(append.Blockers.ToArray(), ProductLedgerPathLocalOnlyBlocker.WormKmsExternalTrustClaimed);
        CollectionAssert.Contains(append.Blockers.ToArray(), ProductLedgerPathLocalOnlyBlocker.DbMigrationClaimed);
        CollectionAssert.Contains(append.Blockers.ToArray(), ProductLedgerPathLocalOnlyBlocker.BrowserCdpWcuOcrRecipesLiveClaimed);
        CollectionAssert.Contains(append.Blockers.ToArray(), ProductLedgerPathLocalOnlyBlocker.ReleaseCommercialReadinessClaimed);
        Assert.IsFalse(append.ProductRuntimeEnabled);
        Assert.IsFalse(append.ProductServiceRegistrationAllowed);
        Assert.IsFalse(append.UiProductActionsAllowed);
    }

    private static ProductLedgerPathLocalOnlyActivationRequest ReadyActivationRequest(LedgerFixture fixture) =>
        new(
            PersistedCandidateResult: ReadyPersistedCandidate(fixture),
            ExplicitLocalOnlyActivationMode: true,
            HasAuthorityEvidence: true,
            HasRedactionBeforePersistenceEvidence: true,
            HasFailureReplayRollbackEvidence: true,
            HasRetentionEvidence: true,
            LocalRuntimeFlagDefaultOff: true,
            RequestsRuntimeEnablement: false,
            RequestsProductServiceRegistration: false,
            RequestsProductCommandHandler: false,
            RequestsUiProductAction: false,
            ClaimsProviderCloudNetwork: false,
            ClaimsWormKmsExternalTrust: false,
            ClaimsDbMigration: false,
            ClaimsBrowserCdpWcuOcrRecipesLive: false,
            ClaimsReleaseCommercialReadiness: false,
            ClaimsLocalTempAsProductLedgerPath: false);

    private static ProductLedgerPathLocalOnlyAppendRequest ReadyAppendRequest(ProductLedgerPathLocalOnlyActivationResult activation) =>
        new(
            ActivationResult: activation,
            SafePayloadHash: new string('e', 64),
            EvidenceMetadata: new Dictionary<string, string>
            {
                ["authority"] = "local-only-policy-bound",
                ["redaction"] = "redacted-before-persistence",
                ["failure"] = "replay-rollback-evidence"
            },
            RuntimeFlagStillDefaultOff: true,
            RequestsRuntimeEnablement: false,
            RequestsProductServiceRegistration: false,
            RequestsProductCommandHandler: false,
            RequestsUiProductAction: false,
            ClaimsProviderCloudNetwork: false,
            ClaimsWormKmsExternalTrust: false,
            ClaimsDbMigration: false,
            ClaimsBrowserCdpWcuOcrRecipesLive: false,
            ClaimsReleaseCommercialReadiness: false);

    private static ProductLedgerPathPersistedCandidateResult ReadyPersistedCandidate(LedgerFixture fixture)
    {
        var canonicalization = new ProductLedgerPathCanonicalizationValidator().Validate(
            new ProductLedgerPathCanonicalizationRequest(
                CandidatePath: fixture.LedgerRoot,
                AllowedRootPath: fixture.AllowedRoot,
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
                "ProductLedgerPathLocalOnlyActiveWriter.cs")))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        Assert.Fail("repo root not found");
        return string.Empty;
    }

    private sealed class LedgerFixture : IDisposable
    {
        private LedgerFixture(string allowedRoot, string ledgerRoot)
        {
            AllowedRoot = allowedRoot;
            LedgerRoot = ledgerRoot;
        }

        public string AllowedRoot { get; }

        public string LedgerRoot { get; }

        public static LedgerFixture Create()
        {
            var allowedRoot = Path.Combine(RepoRoot(), ".tmp-product-ledger-local-only-recipes-tests");
            var ledgerRoot = Path.Combine(allowedRoot, Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(ledgerRoot);
            return new LedgerFixture(allowedRoot, ledgerRoot);
        }

        public void Dispose()
        {
            if (AllowedRoot.StartsWith(RepoRoot(), StringComparison.OrdinalIgnoreCase) && Directory.Exists(AllowedRoot))
            {
                Directory.Delete(AllowedRoot, recursive: true);
            }
        }
    }
}
