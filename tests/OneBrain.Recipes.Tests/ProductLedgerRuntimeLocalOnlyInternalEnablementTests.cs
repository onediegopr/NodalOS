using Microsoft.VisualStudio.TestTools.UnitTesting;
using OneBrain.Core.Approval;

namespace OneBrain.Recipes.Tests;

[TestClass]
[TestCategory("ApprovalHumanReview")]
[TestCategory("PhaseEApprovalHumanReview")]
public sealed class ProductLedgerRuntimeLocalOnlyInternalEnablementTests
{
    [TestMethod]
    public void RuntimeLocalOnlyInternalEnablement_ArmsDiagnosticsAndBoundedAppendRecipe()
    {
        using var fixture = LedgerFixture.Create();
        var runtime = new ProductLedgerRuntimeLocalOnlyInternalEnablement();
        var flag = runtime.EvaluateFeatureFlag(ReadyFlagRequest());
        var activation = Activate(fixture);
        var append = runtime.ExecuteInternal(
            ReadyAdapterRequest(flag, activation) with
            {
                CommandKind = ProductLedgerRuntimeLocalOnlyCommandKind.AppendSafeHashOnly,
                SafePayloadHash = new string('c', 64)
            });
        var diagnostics = runtime.ExecuteInternal(ReadyAdapterRequest(flag, activation));

        Assert.AreEqual(ProductLedgerRuntimeLocalOnlyFlagDecision.ArmedLocalOnlyInternal, flag.Decision);
        Assert.AreEqual(ProductLedgerRuntimeLocalOnlyAdapterDecision.AppendedLocalOnly, append.Decision);
        Assert.AreEqual(ProductLedgerRuntimeLocalOnlyAdapterDecision.DiagnosticsReadOnly, diagnostics.Decision);
        Assert.AreEqual(1, diagnostics.VerifiedEntryCount);
        Assert.AreEqual(append.HeadEntryHash, diagnostics.HeadEntryHash);
        Assert.IsFalse(flag.ProductRuntimeEnabled);
        Assert.IsFalse(flag.PublicUiProductActionsAllowed);
        Assert.IsFalse(append.ProviderCloudNetworkAllowed);
        Assert.IsFalse(append.ReleaseCommercialReady);
    }

    [TestMethod]
    public void RuntimeLocalOnlyInternalEnablement_BlocksProductAndExternalRecipeClaims()
    {
        using var fixture = LedgerFixture.Create();
        var runtime = new ProductLedgerRuntimeLocalOnlyInternalEnablement();
        var unsafeFlag = runtime.EvaluateFeatureFlag(
            ReadyFlagRequest() with
            {
                RequestsRuntimeEnabledByDefault = true,
                RequestsPublicUiAction = true,
                ClaimsProviderCloudNetwork = true,
                ClaimsReleaseCommercial = true
            });
        var blockedAdapter = runtime.ExecuteInternal(
            ReadyAdapterRequest(unsafeFlag, Activate(fixture)) with
            {
                ClaimsDestructiveActionOutsideBoundedWriter = true
            });

        Assert.AreEqual(ProductLedgerRuntimeLocalOnlyFlagDecision.Rejected, unsafeFlag.Decision);
        CollectionAssert.Contains(unsafeFlag.Blockers.ToArray(), ProductLedgerRuntimeLocalOnlyBlocker.RuntimeEnabledByDefault);
        CollectionAssert.Contains(unsafeFlag.Blockers.ToArray(), ProductLedgerRuntimeLocalOnlyBlocker.PublicUiActionRequested);
        CollectionAssert.Contains(unsafeFlag.Blockers.ToArray(), ProductLedgerRuntimeLocalOnlyBlocker.ProviderCloudNetworkClaimed);
        CollectionAssert.Contains(unsafeFlag.Blockers.ToArray(), ProductLedgerRuntimeLocalOnlyBlocker.ReleaseCommercialClaimed);
        Assert.AreEqual(ProductLedgerRuntimeLocalOnlyAdapterDecision.Blocked, blockedAdapter.Decision);
        CollectionAssert.Contains(blockedAdapter.Blockers.ToArray(), ProductLedgerRuntimeLocalOnlyBlocker.FailedArmedFeatureFlag);
        CollectionAssert.Contains(blockedAdapter.Blockers.ToArray(), ProductLedgerRuntimeLocalOnlyBlocker.DestructiveActionOutsideBoundedWriterClaimed);
        Assert.IsFalse(blockedAdapter.ProductRuntimeEnabled);
        Assert.IsFalse(blockedAdapter.PublicUiProductActionsAllowed);
    }

    private static ProductLedgerRuntimeLocalOnlyFeatureFlagRequest ReadyFlagRequest() =>
        new(
            ExplicitLocalOnlyRuntimeScope: true,
            LocalRuntimeFlagDefaultOff: true,
            FeatureFlagValue: ProductLedgerRuntimeLocalOnlyInternalEnablement.EnabledLocalOnlyInternalValue,
            RequestsRuntimeEnabledByDefault: false,
            RequestsPublicUiAction: false,
            RequestsProductCommandHandler: false,
            RequestsProductiveServiceRegistration: false,
            ClaimsProviderCloudNetwork: false,
            ClaimsDbMigration: false,
            ClaimsKmsWormExternalTrust: false,
            ClaimsBrowserCdpWcuOcrRecipesLive: false,
            ClaimsReleaseCommercial: false,
            ClaimsDestructiveActionOutsideBoundedWriter: false);

    private static ProductLedgerRuntimeLocalOnlyAdapterRequest ReadyAdapterRequest(
        ProductLedgerRuntimeLocalOnlyFlagResult flag,
        ProductLedgerPathLocalOnlyActivationResult activation) =>
        new(
            FeatureFlag: flag,
            ActivationResult: activation,
            CommandKind: ProductLedgerRuntimeLocalOnlyCommandKind.DiagnosticsReadOnly,
            ExplicitTestOnlyCommandAdapter: true,
            SafePayloadHash: null,
            EvidenceMetadata: new Dictionary<string, string>
            {
                ["authority"] = "local-only-policy-bound",
                ["redaction"] = "redacted-before-persistence",
                ["failure"] = "replay-rollback-evidence"
            },
            RequestsPublicUiAction: false,
            RequestsProductCommandHandler: false,
            RequestsProductiveServiceRegistration: false,
            ClaimsProviderCloudNetwork: false,
            ClaimsDbMigration: false,
            ClaimsKmsWormExternalTrust: false,
            ClaimsBrowserCdpWcuOcrRecipesLive: false,
            ClaimsReleaseCommercial: false,
            ClaimsDestructiveActionOutsideBoundedWriter: false);

    private static ProductLedgerPathLocalOnlyActivationResult Activate(LedgerFixture fixture)
    {
        var activation = new ProductLedgerPathLocalOnlyActiveWriter().Activate(
            new ProductLedgerPathLocalOnlyActivationRequest(
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
                ClaimsLocalTempAsProductLedgerPath: false));
        Assert.AreEqual(ProductLedgerPathLocalOnlyActivationDecision.ActivatedLocalOnly, activation.Decision);
        return activation;
    }

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
                CandidateId: "ledger-candidate-runtime-recipe-001",
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
                "ProductLedgerRuntimeLocalOnlyInternalEnablement.cs")))
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
            var allowedRoot = Path.Combine(RepoRoot(), ".tmp-product-ledger-runtime-local-only-recipes-tests", Guid.NewGuid().ToString("N"));
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
