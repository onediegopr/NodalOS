using Microsoft.VisualStudio.TestTools.UnitTesting;
using OneBrain.Core.Approval;

namespace OneBrain.Safety.Tests;

[TestClass]
[TestCategory("ApprovalHumanReview")]
[TestCategory("PhaseEApprovalHumanReview")]
public sealed class ProductLedgerInternalOperatorUiPreviewTests
{
    [TestMethod]
    public void InternalOperatorUiPreview_FailsClosedOnMissingCorruptOrUnsafeDiagnostics()
    {
        var presenter = new ProductLedgerInternalOperatorUiPresenter();
        var missing = presenter.Render(null);
        var missingDiagnostics = presenter.Render(ReadyRequest() with { Diagnostics = null });
        var blockedDiagnostics = presenter.Render(
            ReadyRequest() with
            {
                Diagnostics = ReadyDiagnostics() with
                {
                    Decision = ProductLedgerLocalOnlyOperatorDiagnosticsDecision.Rejected,
                    Blockers = [ProductLedgerLocalOnlyOperatorDiagnosticsBlocker.UnsafeRuntimeDiagnostics]
                }
            });
        var unsafeDiagnostics = presenter.Render(
            ReadyRequest() with
            {
                Diagnostics = ReadyDiagnostics() with { PublicUiActionAvailable = true }
            });

        AssertRejected(missing, ProductLedgerInternalOperatorUiPreviewBlocker.MissingRequest);
        AssertRejected(missingDiagnostics, ProductLedgerInternalOperatorUiPreviewBlocker.MissingDiagnosticsSurface);
        AssertRejected(blockedDiagnostics, ProductLedgerInternalOperatorUiPreviewBlocker.UnsafeDiagnosticsSurface);
        AssertRejected(unsafeDiagnostics, ProductLedgerInternalOperatorUiPreviewBlocker.UnsafeDiagnosticsSurface);
    }

    [TestMethod]
    public void InternalOperatorUiPreview_RendersRequiredHeaderSectionsAndNotices()
    {
        var result = new ProductLedgerInternalOperatorUiPresenter().Render(ReadyRequest());

        Assert.AreEqual(ProductLedgerInternalOperatorUiPreviewDecision.RenderedPreview, result.Decision);
        Assert.AreEqual(0, result.Blockers.Count);
        Assert.AreEqual("Product Ledger Local-Only", result.ViewModel.Header.Title);
        Assert.AreEqual("LOCAL_ONLY_INTERNAL_READ_ONLY_PREVIEW", result.ViewModel.Header.Status);
        Assert.AreEqual(86, result.ViewModel.Header.ReadinessPercentage);
        CollectionAssert.Contains(result.ViewModel.Header.Notices.ToArray(), "local-only");
        CollectionAssert.Contains(result.ViewModel.Header.Notices.ToArray(), "internal-only");
        CollectionAssert.Contains(result.ViewModel.Header.Notices.ToArray(), "read-only preview");
        CollectionAssert.Contains(result.ViewModel.Header.Notices.ToArray(), "no public UI action");
        CollectionAssert.Contains(result.ViewModel.Header.Notices.ToArray(), "no release/commercial");
        AssertSection(result, "Runtime Local-Only Gate");
        AssertSection(result, "Product Ledger Path Policy");
        AssertSection(result, "Bounded Writer Status");
        AssertSection(result, "Checkpoint / Head Status");
        AssertSection(result, "Evidence Gates");
        AssertSection(result, "Runtime/Product Local-Dev Readiness");
        AssertSection(result, "Disabled Actions");
        AssertSection(result, "Safe Next Step");
        AssertNoExecutableSurface(result.ViewModel);
    }

    [TestMethod]
    public void InternalOperatorUiPreview_RendersRuntimeWriterCheckpointEvidenceAndSafeNextStep()
    {
        var result = new ProductLedgerInternalOperatorUiPresenter().Render(ReadyRequest());
        var allLines = string.Join(" ", result.ViewModel.Sections.SelectMany(section => section.Lines));

        StringAssert.Contains(allLines, "feature_flag=enabled:local-only-internal");
        StringAssert.Contains(allLines, "writer_policy_allows_bounded_local_only=True");
        StringAssert.Contains(allLines, "operator_surface_write_allowed=false");
        StringAssert.Contains(allLines, "head_entry_hash=abc123");
        StringAssert.Contains(allLines, "checkpoint_path=C:/local-only/product-ledger.local-only.head.json");
        StringAssert.Contains(allLines, "redaction_before_persistence=True");
        StringAssert.Contains(allLines, "retention=True");
        StringAssert.Contains(allLines, "authority=True");
        StringAssert.Contains(allLines, "replay_failure=True");
        StringAssert.Contains(allLines, "rollback_non_rollback=True");
        StringAssert.Contains(allLines, "no_external_trust=true");
        Assert.IsTrue(result.ViewModel.Sections.Any(section =>
            section.Title == "Runtime/Product Local-Dev Readiness"
            && section.Lines.Contains("runtime_product_local_dev_readiness=36")
            && section.Lines.Contains("runtime_product_production_readiness=0")
            && section.Lines.Contains("product_authority=false")));
        StringAssert.Contains(result.ViewModel.SafeNextStep, "LOCAL_DEV_RUNTIME_PRODUCT_READINESS");
        Assert.IsTrue(result.ViewModel.Warnings.Any(warning => warning.Contains("Runtime/product local-dev readiness", StringComparison.Ordinal)));
    }

    [TestMethod]
    public void InternalOperatorUiPreview_RuntimeReadinessSectionStaysConsistentWithDiagnosticsSurface()
    {
        var diagnostics = ReadyDiagnostics();
        var result = new ProductLedgerInternalOperatorUiPresenter().Render(ReadyRequest() with { Diagnostics = diagnostics });
        var diagnosticsSection = diagnostics.Sections.Single(section => section.Title == "Runtime/Product Local-Dev Readiness");
        var previewSection = result.ViewModel.Sections.Single(section => section.Title == "Runtime/Product Local-Dev Readiness");

        Assert.AreEqual(diagnosticsSection.Status, previewSection.Status);
        CollectionAssert.AreEqual(diagnosticsSection.Lines.ToArray(), previewSection.Lines.ToArray());
        CollectionAssert.Contains(previewSection.Lines.ToArray(), "runtime_product_local_dev_readiness=36");
        CollectionAssert.Contains(previewSection.Lines.ToArray(), "runtime_product_production_readiness=0");
        CollectionAssert.Contains(previewSection.Lines.ToArray(), "product_surface_local_dev_readiness=86");
        CollectionAssert.Contains(previewSection.Lines.ToArray(), "latest_pointer_authority=false");
        CollectionAssert.Contains(previewSection.Lines.ToArray(), "read_precedence_authority=false");
        CollectionAssert.Contains(previewSection.Lines.ToArray(), "product_authority=false");
        CollectionAssert.Contains(previewSection.Lines.ToArray(), "release_commercial_ready=false");
        AssertNoExecutableSurface(result.ViewModel);
    }

    [TestMethod]
    public void InternalOperatorUiPreview_DisabledActionsAndActionPreviewsAreNonExecutable()
    {
        var viewModel = new ProductLedgerInternalOperatorUiPresenter().Render(ReadyRequest()).ViewModel;
        var labels = viewModel.ActionPreviews.Select(action => action.Label).ToArray();

        CollectionAssert.Contains(labels, "enable public UI");
        CollectionAssert.Contains(labels, "run destructive action");
        CollectionAssert.Contains(labels, "register command handler");
        CollectionAssert.Contains(labels, "connect provider/cloud");
        CollectionAssert.Contains(labels, "create DB migration");
        CollectionAssert.Contains(labels, "enable KMS/WORM");
        CollectionAssert.Contains(labels, "enable Browser/CDP/WCU/OCR/Recipes live");
        CollectionAssert.Contains(labels, "release/commercial");
        foreach (var action in viewModel.ActionPreviews)
        {
            Assert.IsTrue(action.Disabled);
            Assert.IsNull(action.ProductiveCommandId);
            Assert.IsNull(action.HandlerId);
            Assert.IsNull(action.CallbackName);
            Assert.IsTrue(action.RequiredEvidence.Count > 0);
            StringAssert.Contains(action.BlockedReason, "read-only");
        }

        AssertNoExecutableSurface(viewModel);
    }

    [TestMethod]
    public void InternalOperatorUiPreview_BlocksPublicProductExternalTelemetryBillingAndReleaseClaims()
    {
        var ready = ReadyRequest();
        var cases = new Dictionary<ProductLedgerInternalOperatorUiPreviewRequest, ProductLedgerInternalOperatorUiPreviewBlocker>
        {
            [ready with { ExplicitInternalLocalOnlyReadOnlyPreviewScope = false }] = ProductLedgerInternalOperatorUiPreviewBlocker.MissingExplicitInternalLocalOnlyReadOnlyPreviewScope,
            [ready with { RequestsPublicUiAction = true }] = ProductLedgerInternalOperatorUiPreviewBlocker.PublicUiActionRequested,
            [ready with { RequestsDestructiveUserFacingAction = true }] = ProductLedgerInternalOperatorUiPreviewBlocker.DestructiveUserFacingActionRequested,
            [ready with { RequestsProductCommandHandler = true }] = ProductLedgerInternalOperatorUiPreviewBlocker.ProductCommandHandlerRequested,
            [ready with { RequestsProductiveServiceRegistration = true }] = ProductLedgerInternalOperatorUiPreviewBlocker.ProductiveServiceRegistrationRequested,
            [ready with { ClaimsProviderCloudNetwork = true }] = ProductLedgerInternalOperatorUiPreviewBlocker.ProviderCloudNetworkClaimed,
            [ready with { ClaimsDbMigration = true }] = ProductLedgerInternalOperatorUiPreviewBlocker.DbMigrationClaimed,
            [ready with { ClaimsKmsWormExternalTrust = true }] = ProductLedgerInternalOperatorUiPreviewBlocker.KmsWormExternalTrustClaimed,
            [ready with { ClaimsBrowserCdpWcuOcrRecipesLive = true }] = ProductLedgerInternalOperatorUiPreviewBlocker.BrowserCdpWcuOcrRecipesLiveClaimed,
            [ready with { ClaimsReleaseCommercial = true }] = ProductLedgerInternalOperatorUiPreviewBlocker.ReleaseCommercialClaimed,
            [ready with { ClaimsExternalTelemetryOrSync = true }] = ProductLedgerInternalOperatorUiPreviewBlocker.ExternalTelemetryOrSyncClaimed,
            [ready with { ClaimsBillingLicensingCloud = true }] = ProductLedgerInternalOperatorUiPreviewBlocker.BillingLicensingCloudClaimed
        };

        var presenter = new ProductLedgerInternalOperatorUiPresenter();
        foreach (var testCase in cases)
        {
            AssertRejected(presenter.Render(testCase.Key), testCase.Value);
        }
    }

    [TestMethod]
    public void InternalOperatorUiPreview_BlocksDiagnosticsMissingRequiredSectionsOrExecutablePreview()
    {
        var presenter = new ProductLedgerInternalOperatorUiPresenter();
        var missingSection = ReadyDiagnostics() with
        {
            Sections = ReadyDiagnostics().Sections.Where(section => section.Title != "Evidence Gates").ToArray()
        };
        var executableAction = ReadyDiagnostics() with
        {
            ActionPreviews =
            [
                new ProductLedgerLocalOnlyOperatorDiagnosticsActionPreview(
                    "unsafe",
                    "unsafe",
                    "unsafe",
                    [],
                    Disabled: false,
                    ProductiveCommandId: "product.execute",
                    HandlerName: "handler",
                    CallbackName: "callback")
            ]
        };

        AssertRejected(
            presenter.Render(ReadyRequest() with { Diagnostics = missingSection }),
            ProductLedgerInternalOperatorUiPreviewBlocker.MissingRequiredDiagnosticsSection);
        AssertRejected(
            presenter.Render(ReadyRequest() with { Diagnostics = executableAction }),
            ProductLedgerInternalOperatorUiPreviewBlocker.UnsafeDiagnosticsSurface);
    }

    [TestMethod]
    public void InternalOperatorUiPreview_SourceHasNoProductRegistrationPublicEndpointNetworkDbKmsLiveOrReleaseEnablement()
    {
        var source = File.ReadAllText(Path.Combine(
            RepoRoot(),
            "src",
            "OneBrain.Core",
            "Approval",
            "ProductLedgerInternalOperatorUiPreview.cs"));
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
            "PublicUiActionAvailable:" + " true",
            "DestructiveUserFacingActionAvailable:" + " true",
            "ProductCommandHandlerAvailable:" + " true",
            "ProductiveServiceRegistrationAvailable:" + " true",
            "ProviderCloudNetworkAvailable:" + " true",
            "DbMigrationAvailable:" + " true",
            "KmsWormExternalTrustAvailable:" + " true",
            "BrowserCdpWcuOcrRecipesLiveAvailable:" + " true",
            "ExternalTelemetryOrSyncAvailable:" + " true",
            "BillingLicensingCloudAvailable:" + " true",
            "ReleaseCommercialReady:" + " true"
        };

        foreach (var fragment in forbiddenFragments)
        {
            Assert.IsFalse(source.Contains(fragment, StringComparison.Ordinal), fragment);
        }

        StringAssert.Contains(source, "READ_ONLY_PREVIEW_READY");
        StringAssert.Contains(source, "ACTIONS_DISABLED");
    }

    private static ProductLedgerInternalOperatorUiPreviewRequest ReadyRequest() =>
        new(
            ExplicitInternalLocalOnlyReadOnlyPreviewScope: true,
            Diagnostics: ReadyDiagnostics(),
            RequestsPublicUiAction: false,
            RequestsDestructiveUserFacingAction: false,
            RequestsProductCommandHandler: false,
            RequestsProductiveServiceRegistration: false,
            ClaimsProviderCloudNetwork: false,
            ClaimsDbMigration: false,
            ClaimsKmsWormExternalTrust: false,
            ClaimsBrowserCdpWcuOcrRecipesLive: false,
            ClaimsReleaseCommercial: false,
            ClaimsExternalTelemetryOrSync: false,
            ClaimsBillingLicensingCloud: false);

    private static ProductLedgerLocalOnlyOperatorDiagnosticsResult ReadyDiagnostics() =>
        new(
            Decision: ProductLedgerLocalOnlyOperatorDiagnosticsDecision.RenderedReadOnly,
            Blockers: [],
            Sections:
            [
                Section("Runtime Local-Only Gate", "ENABLED_LOCAL_ONLY_INTERNAL", ["feature_flag=enabled:local-only-internal", "default_off=True", "forged_flag_blocked=True", "unknown_command_fail_closed=True"]),
                Section("Product Ledger Path Policy", "ACTIVE_LOCAL_ONLY_POLICY_BOUND", ["candidate_id=ledger-candidate-ui-preview-001", "product_runtime_enabled=false", "release_commercial_ready=false"]),
                Section("Bounded Writer Status", "WRITER_BOUNDED_LOCAL_ONLY_SURFACE_READ_ONLY", ["writer_policy_allows_bounded_local_only=True", "append_read_verification=True", "safe_payload_hash=sha256-only", "metadata_verification=True", "operator_surface_write_allowed=false", "no_external_writer=true", "no_worm_kms_cloud=true"]),
                Section("Checkpoint / Head Status", "VERIFIED_HEAD_PRESENT", ["head_entry_hash=abc123", "checkpoint_path=C:/local-only/product-ledger.local-only.head.json", "same_boundary_trust=true", "checkpoint_limitation=same-boundary", "tail_deletion_limitation=local-only", "no_external_recovery_claim=true"]),
                Section("Evidence Gates", "EVIDENCE_REFERENCES_FRESH_AND_WELL_FORMED", ["redaction_before_persistence=True", "retention=True", "authority=True", "replay_failure=True", "rollback_non_rollback=True", "missing_stale_malformed_blockers=0", "no_external_trust=true"]),
                Section("Runtime/Product Local-Dev Readiness", "LOCAL_DEV_RUNTIME_PRODUCT_READINESS_SLICE_VISIBLE", ["runtime_product_local_dev_readiness=36", "runtime_product_production_readiness=0", "product_surface_local_dev_readiness=86", "production_runtime_enabled=false", "public_product_surface_enabled=false", "latest_pointer_authority=false", "read_precedence_authority=false", "product_authority=false", "release_commercial_ready=false"]),
                Section("Disabled Actions", "ALL_ACTIONS_DISABLED", ["enable public UI", "run destructive action", "register command handler", "connect provider/cloud", "create DB migration", "enable KMS/WORM", "enable Browser/CDP/WCU/OCR/Recipes live", "release/commercial"]),
                Section("Safe Next Step", "LOCAL_DEV_RUNTIME_PRODUCT_READINESS_NEXT_OPERATOR_FRONTIER", ["LOCAL_DEV_RUNTIME_PRODUCT_READINESS_ACCEPTANCE_THEN_OPERATOR_FRONTIER_DECISION", "NO_RELEASE_COMMERCIAL", "NO_PUBLIC_DESTRUCTIVE_ACTION", "NO_PRODUCTION_RUNTIME"])
            ],
            ActionPreviews:
            [
                new("View local-only diagnostics snapshot", "read-only preview only", "operator visibility without execution authority", ["runtime gate"], Disabled: true, ProductiveCommandId: null, HandlerName: null, CallbackName: null)
            ],
            DisabledActions: ["public UI action", "destructive user-facing action", "product command handler", "provider/cloud/network access", "release/commercial readiness"],
            SafeNextStep: "EXTERNAL_AUDIT_READ_ONLY_THEN_STATIC_SCAN_HARDENING",
            ReadOnly: true,
            LocalOnly: true,
            InternalOnly: true,
            FailClosed: true,
            PublicUiActionAvailable: false,
            DestructiveUserFacingActionAvailable: false,
            ProductCommandHandlerAvailable: false,
            ProductiveServiceRegistrationAvailable: false,
            ProviderCloudNetworkAvailable: false,
            DbMigrationAvailable: false,
            KmsWormExternalTrustAvailable: false,
            BrowserCdpWcuOcrRecipesLiveAvailable: false,
            ReleaseCommercialReady: false,
            StatusText: ProductLedgerLocalOnlyOperatorDiagnosticsPresenter.ReadyStatus);

    private static ProductLedgerLocalOnlyOperatorDiagnosticsSection Section(
        string title,
        string status,
        IReadOnlyList<string> lines) =>
        new(title, status, lines, ProductLedgerLocalOnlyOperatorDiagnosticsSeverity.Info);

    private static void AssertRejected(
        ProductLedgerInternalOperatorUiPreviewResult result,
        ProductLedgerInternalOperatorUiPreviewBlocker blocker)
    {
        Assert.AreEqual(ProductLedgerInternalOperatorUiPreviewDecision.Rejected, result.Decision, blocker.ToString());
        CollectionAssert.Contains(result.Blockers.ToArray(), blocker, blocker.ToString());
        StringAssert.Contains(result.ViewModel.StatusText, "REJECTED");
        AssertNoExecutableSurface(result.ViewModel);
    }

    private static void AssertSection(ProductLedgerInternalOperatorUiPreviewResult result, string title) =>
        Assert.IsTrue(result.ViewModel.Sections.Any(section => section.Title == title), title);

    private static void AssertNoExecutableSurface(ProductLedgerInternalOperatorUiPreviewViewModel viewModel)
    {
        Assert.IsTrue(viewModel.LocalOnly);
        Assert.IsTrue(viewModel.InternalOnly);
        Assert.IsTrue(viewModel.ReadOnly);
        Assert.IsTrue(viewModel.FailClosed);
        Assert.IsFalse(viewModel.PublicUiActionAvailable);
        Assert.IsFalse(viewModel.DestructiveUserFacingActionAvailable);
        Assert.IsFalse(viewModel.ProductCommandHandlerAvailable);
        Assert.IsFalse(viewModel.ProductiveServiceRegistrationAvailable);
        Assert.IsFalse(viewModel.ProviderCloudNetworkAvailable);
        Assert.IsFalse(viewModel.DbMigrationAvailable);
        Assert.IsFalse(viewModel.KmsWormExternalTrustAvailable);
        Assert.IsFalse(viewModel.BrowserCdpWcuOcrRecipesLiveAvailable);
        Assert.IsFalse(viewModel.ExternalTelemetryOrSyncAvailable);
        Assert.IsFalse(viewModel.BillingLicensingCloudAvailable);
        Assert.IsFalse(viewModel.ReleaseCommercialReady);
        Assert.IsTrue(viewModel.ActionPreviews.All(action => action.Disabled));
        Assert.IsTrue(viewModel.ActionPreviews.All(action => action.ProductiveCommandId is null));
        Assert.IsTrue(viewModel.ActionPreviews.All(action => action.HandlerId is null));
        Assert.IsTrue(viewModel.ActionPreviews.All(action => action.CallbackName is null));
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
                "ProductLedgerInternalOperatorUiPreview.cs")))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        Assert.Fail("repo root not found");
        return string.Empty;
    }
}
