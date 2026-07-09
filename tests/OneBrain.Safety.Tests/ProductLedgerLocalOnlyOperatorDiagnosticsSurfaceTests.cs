using Microsoft.VisualStudio.TestTools.UnitTesting;
using OneBrain.Core.Approval;

namespace OneBrain.Safety.Tests;

[TestClass]
[TestCategory("ApprovalHumanReview")]
[TestCategory("PhaseEApprovalHumanReview")]
public sealed class ProductLedgerLocalOnlyOperatorDiagnosticsSurfaceTests
{
    [TestMethod]
    public void OperatorDiagnosticsSurface_FailsClosedOnMissingOrUnknownInputs()
    {
        var presenter = new ProductLedgerLocalOnlyOperatorDiagnosticsPresenter();

        var missing = presenter.Render(null);
        using var fixture = LedgerFixture.Create();
        var ready = ReadyRequest(fixture);
        var missingRuntime = presenter.Render(ready with { RuntimeFlag = null });
        var missingActivation = presenter.Render(ready with { ActivationResult = null });
        var missingDiagnostics = presenter.Render(ready with { RuntimeDiagnostics = null });
        var corruptDiagnostics = presenter.Render(
            ready with
            {
                RuntimeDiagnostics = ready.RuntimeDiagnostics! with
                {
                    Decision = ProductLedgerRuntimeLocalOnlyAdapterDecision.Blocked,
                    Blockers = [ProductLedgerRuntimeLocalOnlyBlocker.ExistingLedgerInvalid]
                }
            });

        AssertRejected(missing, ProductLedgerLocalOnlyOperatorDiagnosticsBlocker.MissingRequest);
        AssertRejected(missingRuntime, ProductLedgerLocalOnlyOperatorDiagnosticsBlocker.MissingRuntimeFlag);
        AssertRejected(missingActivation, ProductLedgerLocalOnlyOperatorDiagnosticsBlocker.MissingActivationResult);
        AssertRejected(missingDiagnostics, ProductLedgerLocalOnlyOperatorDiagnosticsBlocker.MissingRuntimeDiagnostics);
        AssertRejected(corruptDiagnostics, ProductLedgerLocalOnlyOperatorDiagnosticsBlocker.UnsafeRuntimeDiagnostics);
    }

    [TestMethod]
    public void OperatorDiagnosticsSurface_RendersRequiredReadOnlySectionsAndStatuses()
    {
        using var fixture = LedgerFixture.Create();
        var result = new ProductLedgerLocalOnlyOperatorDiagnosticsPresenter().Render(ReadyRequest(fixture));

        Assert.AreEqual(ProductLedgerLocalOnlyOperatorDiagnosticsDecision.RenderedReadOnly, result.Decision);
        Assert.AreEqual(0, result.Blockers.Count);
        CollectionAssert.AreEquivalent(
            new[]
            {
                "Runtime Local-Only Gate",
                "Product Ledger Path Policy",
                "Bounded Writer Status",
                "Checkpoint / Head Status",
                "Evidence Gates",
                "Runtime/Product Local-Dev Readiness",
                "Disabled Actions",
                "Safe Next Step"
            },
            result.Sections.Select(section => section.Title).ToArray());
        Assert.AreEqual("LOCAL_DEV_RUNTIME_PRODUCT_READINESS_ACCEPTANCE_THEN_OPERATOR_FRONTIER_DECISION", result.SafeNextStep);
        StringAssert.Contains(result.StatusText, "READ_ONLY_SURFACE_READY");
        StringAssert.Contains(result.StatusText, "NO_RELEASE_COMMERCIAL");
        AssertNoExecutableSurface(result);
    }

    [TestMethod]
    public void OperatorDiagnosticsSurface_RepresentsDefaultOffAndEnabledLocalOnlyInternalFlagStates()
    {
        using var fixture = LedgerFixture.Create();
        var ready = ReadyRequest(fixture);
        var presenter = new ProductLedgerLocalOnlyOperatorDiagnosticsPresenter();
        var enabled = presenter.Render(ready);
        var offFlag = new ProductLedgerRuntimeLocalOnlyInternalEnablement().EvaluateFeatureFlag(
            ReadyFlagRequest() with { FeatureFlagValue = ProductLedgerRuntimeLocalOnlyInternalEnablement.OffValue });
        var defaultOff = presenter.Render(ready with { RuntimeFlag = offFlag });

        Assert.AreEqual(ProductLedgerLocalOnlyOperatorDiagnosticsDecision.RenderedReadOnly, enabled.Decision);
        Assert.AreEqual(ProductLedgerLocalOnlyOperatorDiagnosticsDecision.RenderedReadOnly, defaultOff.Decision);
        Assert.AreEqual(
            "ENABLED_LOCAL_ONLY_INTERNAL",
            enabled.Sections.Single(section => section.Title == "Runtime Local-Only Gate").Status);
        Assert.AreEqual(
            "DEFAULT_OFF",
            defaultOff.Sections.Single(section => section.Title == "Runtime Local-Only Gate").Status);
        StringAssert.Contains(
            string.Join(" ", defaultOff.Sections.Single(section => section.Title == "Runtime Local-Only Gate").Lines),
            "feature_flag=off");
        AssertNoExecutableSurface(defaultOff);
    }

    [TestMethod]
    public void OperatorDiagnosticsSurface_RendersLocalDevRuntimeReadinessWithoutProductionAuthority()
    {
        using var fixture = LedgerFixture.Create();
        var result = new ProductLedgerLocalOnlyOperatorDiagnosticsPresenter().Render(ReadyRequest(fixture));
        var section = result.Sections.Single(section => section.Title == "Runtime/Product Local-Dev Readiness");
        var lines = string.Join(" ", section.Lines);

        Assert.AreEqual("LOCAL_DEV_RUNTIME_PRODUCT_READINESS_SLICE_VISIBLE", section.Status);
        StringAssert.Contains(lines, "runtime_product_local_dev_readiness=36");
        StringAssert.Contains(lines, "runtime_product_production_readiness=0");
        StringAssert.Contains(lines, "product_surface_local_dev_readiness=86");
        StringAssert.Contains(lines, "diagnostics_readiness_surface_local_only=True");
        StringAssert.Contains(lines, "production_runtime_enabled=false");
        StringAssert.Contains(lines, "public_product_surface_enabled=false");
        StringAssert.Contains(lines, "latest_pointer_authority=false");
        StringAssert.Contains(lines, "read_precedence_authority=false");
        StringAssert.Contains(lines, "product_authority=false");
        StringAssert.Contains(lines, "release_commercial_ready=false");
        AssertNoExecutableSurface(result);
    }

    [TestMethod]
    public void OperatorDiagnosticsSurface_BlocksBoundaryClaimsAndEvidenceGaps()
    {
        using var fixture = LedgerFixture.Create();
        var ready = ReadyRequest(fixture);
        var cases = new Dictionary<ProductLedgerLocalOnlyOperatorDiagnosticsRequest, ProductLedgerLocalOnlyOperatorDiagnosticsBlocker>
        {
            [ready with { ExplicitOperatorReadOnlyLocalOnlyScope = false }] = ProductLedgerLocalOnlyOperatorDiagnosticsBlocker.MissingExplicitOperatorReadOnlyLocalOnlyScope,
            [ready with { HasAuthorityEvidence = false }] = ProductLedgerLocalOnlyOperatorDiagnosticsBlocker.MissingAuthorityEvidence,
            [ready with { HasRedactionBeforePersistenceEvidence = false }] = ProductLedgerLocalOnlyOperatorDiagnosticsBlocker.MissingRedactionBeforePersistenceEvidence,
            [ready with { HasRetentionEvidence = false }] = ProductLedgerLocalOnlyOperatorDiagnosticsBlocker.MissingRetentionEvidence,
            [ready with { HasReplayFailureEvidence = false }] = ProductLedgerLocalOnlyOperatorDiagnosticsBlocker.MissingReplayFailureEvidence,
            [ready with { HasRollbackEvidence = false }] = ProductLedgerLocalOnlyOperatorDiagnosticsBlocker.MissingRollbackEvidence,
            [ready with { EvidenceReferencesFresh = false }] = ProductLedgerLocalOnlyOperatorDiagnosticsBlocker.StaleEvidenceReferences,
            [ready with { EvidenceReferencesWellFormed = false }] = ProductLedgerLocalOnlyOperatorDiagnosticsBlocker.MalformedEvidenceReferences,
            [ready with { RequestsPublicUiAction = true }] = ProductLedgerLocalOnlyOperatorDiagnosticsBlocker.PublicUiActionRequested,
            [ready with { RequestsDestructiveUserFacingAction = true }] = ProductLedgerLocalOnlyOperatorDiagnosticsBlocker.DestructiveUserFacingActionRequested,
            [ready with { RequestsProductCommandHandler = true }] = ProductLedgerLocalOnlyOperatorDiagnosticsBlocker.ProductCommandHandlerRequested,
            [ready with { RequestsProductiveServiceRegistration = true }] = ProductLedgerLocalOnlyOperatorDiagnosticsBlocker.ProductiveServiceRegistrationRequested,
            [ready with { ClaimsProviderCloudNetwork = true }] = ProductLedgerLocalOnlyOperatorDiagnosticsBlocker.ProviderCloudNetworkClaimed,
            [ready with { ClaimsDbMigration = true }] = ProductLedgerLocalOnlyOperatorDiagnosticsBlocker.DbMigrationClaimed,
            [ready with { ClaimsKmsWormExternalTrust = true }] = ProductLedgerLocalOnlyOperatorDiagnosticsBlocker.KmsWormExternalTrustClaimed,
            [ready with { ClaimsBrowserCdpWcuOcrRecipesLive = true }] = ProductLedgerLocalOnlyOperatorDiagnosticsBlocker.BrowserCdpWcuOcrRecipesLiveClaimed,
            [ready with { ClaimsReleaseCommercial = true }] = ProductLedgerLocalOnlyOperatorDiagnosticsBlocker.ReleaseCommercialClaimed
        };

        var presenter = new ProductLedgerLocalOnlyOperatorDiagnosticsPresenter();
        foreach (var testCase in cases)
        {
            var result = presenter.Render(testCase.Key);

            AssertRejected(result, testCase.Value);
        }
    }

    [TestMethod]
    public void OperatorDiagnosticsSurface_ActionPreviewsAreDisabledAndNonExecutable()
    {
        using var fixture = LedgerFixture.Create();
        var result = new ProductLedgerLocalOnlyOperatorDiagnosticsPresenter().Render(ReadyRequest(fixture));

        Assert.IsTrue(result.ActionPreviews.Count >= 4);
        foreach (var preview in result.ActionPreviews)
        {
            Assert.IsTrue(preview.Disabled);
            Assert.IsNull(preview.ProductiveCommandId);
            Assert.IsNull(preview.HandlerName);
            Assert.IsNull(preview.CallbackName);
            Assert.IsTrue(preview.RequiredEvidence.Count > 0);
        }

        var nextSlice = result.ActionPreviews.Single(preview => preview.Label == "Advance local/dev runtime readiness next slice");
        StringAssert.Contains(nextSlice.Reason, "preview only");
        StringAssert.Contains(nextSlice.Risk, "no production authority");
        CollectionAssert.Contains(nextSlice.RequiredEvidence.ToArray(), "operator-selected frontier");
        CollectionAssert.Contains(nextSlice.RequiredEvidence.ToArray(), "focal diagnostics/operator UI evidence");
        CollectionAssert.Contains(nextSlice.RequiredEvidence.ToArray(), "no production runtime");
        CollectionAssert.Contains(nextSlice.RequiredEvidence.ToArray(), "no release/commercial");

        CollectionAssert.Contains(result.DisabledActions.ToArray(), "public UI action");
        CollectionAssert.Contains(result.DisabledActions.ToArray(), "destructive user-facing action");
        CollectionAssert.Contains(result.DisabledActions.ToArray(), "product command handler");
        CollectionAssert.Contains(result.DisabledActions.ToArray(), "provider/cloud/network access");
        CollectionAssert.Contains(result.DisabledActions.ToArray(), "release/commercial readiness");
        AssertNoExecutableSurface(result);
    }

    [TestMethod]
    public void OperatorDiagnosticsSurface_BlocksUnsafeRuntimeActivationAndDiagnosticsResults()
    {
        using var fixture = LedgerFixture.Create();
        var ready = ReadyRequest(fixture);
        var presenter = new ProductLedgerLocalOnlyOperatorDiagnosticsPresenter();

        AssertRejected(
            presenter.Render(ready with { RuntimeFlag = ready.RuntimeFlag! with { ReleaseCommercialReady = true } }),
            ProductLedgerLocalOnlyOperatorDiagnosticsBlocker.UnsafeRuntimeFlag);
        AssertRejected(
            presenter.Render(ready with { ActivationResult = ready.ActivationResult! with { UiProductActionsAllowed = true } }),
            ProductLedgerLocalOnlyOperatorDiagnosticsBlocker.UnsafeActivationResult);
        AssertRejected(
            presenter.Render(ready with { RuntimeDiagnostics = ready.RuntimeDiagnostics! with { AppendResult = FakeAppendResult() } }),
            ProductLedgerLocalOnlyOperatorDiagnosticsBlocker.UnsafeRuntimeDiagnostics);
    }

    [TestMethod]
    public void OperatorDiagnosticsSurface_SourceHasNoRegistrationPublicEndpointNetworkDbKmsLiveAutomationOrReleaseEnablement()
    {
        var source = File.ReadAllText(Path.Combine(
            RepoRoot(),
            "src",
            "OneBrain.Core",
            "Approval",
            "ProductLedgerLocalOnlyOperatorDiagnosticsSurface.cs"));
        var forbiddenFragments = new[]
        {
            "IService" + "Collection",
            "Add" + "Singleton",
            "Add" + "Scoped",
            "Add" + "Transient",
            "IHosted" + "Service",
            "ICommand" + "Handler",
            "Handle" + "Async(",
            "Control" + "ler",
            "Map" + "Post",
            "Map" + "Get",
            "Http" + "Client",
            "Web" + "Socket",
            "Db" + "Context",
            "Migration" + "Builder",
            "Kms" + "Client",
            "Worm" + "Store",
            "ProductRuntimeEnabled:" + " true",
            "RuntimeEnabledByDefault:" + " true",
            "ProductCommandHandlerAvailable:" + " true",
            "ProductiveServiceRegistrationAvailable:" + " true",
            "PublicUiActionAvailable:" + " true",
            "DestructiveUserFacingActionAvailable:" + " true",
            "ProviderCloudNetworkAvailable:" + " true",
            "DbMigrationAvailable:" + " true",
            "KmsWormExternalTrustAvailable:" + " true",
            "BrowserCdpWcuOcrRecipesLiveAvailable:" + " true",
            "ReleaseCommercialReady:" + " true"
        };

        foreach (var fragment in forbiddenFragments)
        {
            Assert.IsFalse(source.Contains(fragment, StringComparison.Ordinal), fragment);
        }

        StringAssert.Contains(source, "ALL_ACTIONS_DISABLED");
        StringAssert.Contains(source, "LOCAL_DEV_RUNTIME_PRODUCT_READINESS_ACCEPTANCE_THEN_OPERATOR_FRONTIER_DECISION");
    }

    private static ProductLedgerLocalOnlyOperatorDiagnosticsRequest ReadyRequest(LedgerFixture fixture)
    {
        var runtime = new ProductLedgerRuntimeLocalOnlyInternalEnablement();
        var flag = runtime.EvaluateFeatureFlag(ReadyFlagRequest());
        var activation = Activate(fixture);
        var append = runtime.ExecuteInternal(
            ReadyAdapterRequest(flag, activation) with
            {
                CommandKind = ProductLedgerRuntimeLocalOnlyCommandKind.AppendSafeHashOnly,
                SafePayloadHash = new string('d', 64)
            });
        Assert.AreEqual(ProductLedgerRuntimeLocalOnlyAdapterDecision.AppendedLocalOnly, append.Decision);
        var diagnostics = runtime.ExecuteInternal(ReadyAdapterRequest(flag, activation));
        Assert.AreEqual(ProductLedgerRuntimeLocalOnlyAdapterDecision.DiagnosticsReadOnly, diagnostics.Decision);

        return new ProductLedgerLocalOnlyOperatorDiagnosticsRequest(
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
                CandidateId: "ledger-candidate-operator-diagnostics-001",
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

    private static ProductLedgerPathLocalOnlyAppendResult FakeAppendResult() =>
        new(
            Decision: ProductLedgerPathLocalOnlyWriterDecision.AppendedLocalOnly,
            Blockers: [],
            Entry: null,
            ActiveLedgerFilePath: "local-only",
            ActiveCheckpointFilePath: "local-only",
            ProductLedgerPathActive: true,
            ProductLedgerWriteAllowed: true,
            ProductRuntimeEnabled: false,
            ProductServiceRegistrationAllowed: false,
            ProductCommandHandlersAllowed: false,
            UiProductActionsAllowed: false,
            DbProviderCloudNetworkAllowed: false,
            KmsWormExternalTrustAllowed: false,
            LiveAutomationAllowed: false,
            ReleaseCommercialReady: false,
            StatusText: ProductLedgerPathLocalOnlyActiveWriter.ActiveLocalOnlyStatus);

    private static void AssertRejected(
        ProductLedgerLocalOnlyOperatorDiagnosticsResult result,
        ProductLedgerLocalOnlyOperatorDiagnosticsBlocker blocker)
    {
        Assert.AreEqual(ProductLedgerLocalOnlyOperatorDiagnosticsDecision.Rejected, result.Decision, blocker.ToString());
        CollectionAssert.Contains(result.Blockers.ToArray(), blocker, blocker.ToString());
        StringAssert.Contains(result.StatusText, "REJECTED");
        AssertNoExecutableSurface(result);
    }

    private static void AssertNoExecutableSurface(ProductLedgerLocalOnlyOperatorDiagnosticsResult result)
    {
        Assert.IsTrue(result.ReadOnly);
        Assert.IsTrue(result.LocalOnly);
        Assert.IsTrue(result.InternalOnly);
        Assert.IsTrue(result.FailClosed);
        Assert.IsFalse(result.PublicUiActionAvailable);
        Assert.IsFalse(result.DestructiveUserFacingActionAvailable);
        Assert.IsFalse(result.ProductCommandHandlerAvailable);
        Assert.IsFalse(result.ProductiveServiceRegistrationAvailable);
        Assert.IsFalse(result.ProviderCloudNetworkAvailable);
        Assert.IsFalse(result.DbMigrationAvailable);
        Assert.IsFalse(result.KmsWormExternalTrustAvailable);
        Assert.IsFalse(result.BrowserCdpWcuOcrRecipesLiveAvailable);
        Assert.IsFalse(result.ReleaseCommercialReady);
        Assert.IsTrue(result.ActionPreviews.All(preview => preview.Disabled));
        Assert.IsTrue(result.ActionPreviews.All(preview => preview.ProductiveCommandId is null));
        Assert.IsTrue(result.ActionPreviews.All(preview => preview.HandlerName is null));
        Assert.IsTrue(result.ActionPreviews.All(preview => preview.CallbackName is null));
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
            var allowedRoot = Path.Combine(RepoRoot(), ".tmp-product-ledger-operator-diagnostics-tests", Guid.NewGuid().ToString("N"));
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
