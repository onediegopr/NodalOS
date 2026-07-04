using Microsoft.VisualStudio.TestTools.UnitTesting;
using OneBrain.Core.Approval;

namespace OneBrain.Recipes.Tests;

[TestClass]
[TestCategory("ApprovalHumanReview")]
[TestCategory("PhaseEApprovalHumanReview")]
public sealed class ProductLedgerLocalOnlyOperatorDiagnosticsSurfaceTests
{
    [TestMethod]
    public void OperatorDiagnosticsSurface_RendersLocalOnlyOperatorSnapshotRecipe()
    {
        using var fixture = LedgerFixture.Create();
        var runtime = new ProductLedgerRuntimeLocalOnlyInternalEnablement();
        var flag = runtime.EvaluateFeatureFlag(ReadyFlagRequest());
        var activation = Activate(fixture);
        var append = runtime.ExecuteInternal(
            ReadyAdapterRequest(flag, activation) with
            {
                CommandKind = ProductLedgerRuntimeLocalOnlyCommandKind.AppendSafeHashOnly,
                SafePayloadHash = new string('e', 64)
            });
        var diagnostics = runtime.ExecuteInternal(ReadyAdapterRequest(flag, activation));
        var result = new ProductLedgerLocalOnlyOperatorDiagnosticsPresenter().Render(
            ReadySurfaceRequest(flag, activation, diagnostics));

        Assert.AreEqual(ProductLedgerRuntimeLocalOnlyAdapterDecision.AppendedLocalOnly, append.Decision);
        Assert.AreEqual(ProductLedgerRuntimeLocalOnlyAdapterDecision.DiagnosticsReadOnly, diagnostics.Decision);
        Assert.AreEqual(ProductLedgerLocalOnlyOperatorDiagnosticsDecision.RenderedReadOnly, result.Decision);
        Assert.AreEqual(1, diagnostics.VerifiedEntryCount);
        Assert.AreEqual(append.HeadEntryHash, diagnostics.HeadEntryHash);
        Assert.IsTrue(result.ReadOnly);
        Assert.IsTrue(result.LocalOnly);
        Assert.IsTrue(result.InternalOnly);
        Assert.IsTrue(result.FailClosed);
        Assert.IsFalse(result.PublicUiActionAvailable);
        Assert.IsFalse(result.ProviderCloudNetworkAvailable);
        Assert.IsFalse(result.DbMigrationAvailable);
        Assert.IsFalse(result.KmsWormExternalTrustAvailable);
        Assert.IsFalse(result.BrowserCdpWcuOcrRecipesLiveAvailable);
        Assert.IsFalse(result.ReleaseCommercialReady);
        Assert.AreEqual("EXTERNAL_AUDIT_READ_ONLY_THEN_STATIC_SCAN_HARDENING", result.SafeNextStep);
    }

    [TestMethod]
    public void OperatorDiagnosticsSurface_BlocksProductExternalAndReleaseRecipeClaims()
    {
        using var fixture = LedgerFixture.Create();
        var runtime = new ProductLedgerRuntimeLocalOnlyInternalEnablement();
        var flag = runtime.EvaluateFeatureFlag(ReadyFlagRequest());
        var activation = Activate(fixture);
        var diagnostics = runtime.ExecuteInternal(ReadyAdapterRequest(flag, activation));
        var result = new ProductLedgerLocalOnlyOperatorDiagnosticsPresenter().Render(
            ReadySurfaceRequest(flag, activation, diagnostics) with
            {
                RequestsPublicUiAction = true,
                RequestsProductCommandHandler = true,
                ClaimsProviderCloudNetwork = true,
                ClaimsDbMigration = true,
                ClaimsKmsWormExternalTrust = true,
                ClaimsBrowserCdpWcuOcrRecipesLive = true,
                ClaimsReleaseCommercial = true
            });

        Assert.AreEqual(ProductLedgerLocalOnlyOperatorDiagnosticsDecision.Rejected, result.Decision);
        CollectionAssert.Contains(result.Blockers.ToArray(), ProductLedgerLocalOnlyOperatorDiagnosticsBlocker.PublicUiActionRequested);
        CollectionAssert.Contains(result.Blockers.ToArray(), ProductLedgerLocalOnlyOperatorDiagnosticsBlocker.ProductCommandHandlerRequested);
        CollectionAssert.Contains(result.Blockers.ToArray(), ProductLedgerLocalOnlyOperatorDiagnosticsBlocker.ProviderCloudNetworkClaimed);
        CollectionAssert.Contains(result.Blockers.ToArray(), ProductLedgerLocalOnlyOperatorDiagnosticsBlocker.DbMigrationClaimed);
        CollectionAssert.Contains(result.Blockers.ToArray(), ProductLedgerLocalOnlyOperatorDiagnosticsBlocker.KmsWormExternalTrustClaimed);
        CollectionAssert.Contains(result.Blockers.ToArray(), ProductLedgerLocalOnlyOperatorDiagnosticsBlocker.BrowserCdpWcuOcrRecipesLiveClaimed);
        CollectionAssert.Contains(result.Blockers.ToArray(), ProductLedgerLocalOnlyOperatorDiagnosticsBlocker.ReleaseCommercialClaimed);
        Assert.IsTrue(result.ActionPreviews.All(preview => preview.Disabled));
        Assert.IsTrue(result.ActionPreviews.All(preview => preview.ProductiveCommandId is null));
        Assert.IsTrue(result.ActionPreviews.All(preview => preview.HandlerName is null));
        Assert.IsTrue(result.ActionPreviews.All(preview => preview.CallbackName is null));
        Assert.IsFalse(result.PublicUiActionAvailable);
        Assert.IsFalse(result.ProductCommandHandlerAvailable);
        Assert.IsFalse(result.ReleaseCommercialReady);
    }

    private static ProductLedgerLocalOnlyOperatorDiagnosticsRequest ReadySurfaceRequest(
        ProductLedgerRuntimeLocalOnlyFlagResult flag,
        ProductLedgerPathLocalOnlyActivationResult activation,
        ProductLedgerRuntimeLocalOnlyAdapterResult diagnostics) =>
        new(
            ExplicitOperatorReadOnlyLocalOnlyScope: true,
            RuntimeFlag: flag,
            ActivationResult: activation,
            RuntimeDiagnostics: diagnostics,
            HasAuthorityEvidence: true,
            HasRedactionBeforePersistenceEvidence: true,
            HasRetentionEvidence: true,
            HasReplayFailureEvidence: true,
            HasRollbackEvidence: true,
            EvidenceReferencesFresh: true,
            EvidenceReferencesWellFormed: true,
            RequestsPublicUiAction: false,
            RequestsDestructiveUserFacingAction: false,
            RequestsProductCommandHandler: false,
            RequestsProductiveServiceRegistration: false,
            ClaimsProviderCloudNetwork: false,
            ClaimsDbMigration: false,
            ClaimsKmsWormExternalTrust: false,
            ClaimsBrowserCdpWcuOcrRecipesLive: false,
            ClaimsReleaseCommercial: false);

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
                CandidateId: "ledger-candidate-operator-diagnostics-recipe-001",
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
                "ProductLedgerLocalOnlyOperatorDiagnosticsSurface.cs")))
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
            var allowedRoot = Path.Combine(RepoRoot(), ".tmp-product-ledger-operator-diagnostics-recipes-tests", Guid.NewGuid().ToString("N"));
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
