namespace OneBrain.Core.Approval;

public enum ProductLedgerOperatorSurfaceReadModelSourceKind
{
    FixtureSafe,
    TestSafeLiveLedger
}

public sealed record ProductLedgerOperatorSurfaceReadModelSource(
    ProductLedgerOperatorSurfaceReadModelSourceKind SourceKind,
    ProductLedgerPathLocalOnlyActivationResult? ActivationResult,
    string SourceId,
    bool ExplicitTestSafeLocalOnlyReadModel,
    bool AllowsArbitraryPathInput,
    bool AllowsFilesystemScan,
    bool AllowsWrite,
    bool AllowsExport,
    bool AllowsProductCommandExecution,
    bool AllowsExternalNetwork,
    bool AllowsDbMigration,
    bool AllowsReleaseCommercial)
{
    public static ProductLedgerOperatorSurfaceReadModelSource FixtureSafe { get; } =
        new(
            SourceKind: ProductLedgerOperatorSurfaceReadModelSourceKind.FixtureSafe,
            ActivationResult: null,
            SourceId: "fixture-safe-canonical-read-model",
            ExplicitTestSafeLocalOnlyReadModel: false,
            AllowsArbitraryPathInput: false,
            AllowsFilesystemScan: false,
            AllowsWrite: false,
            AllowsExport: false,
            AllowsProductCommandExecution: false,
            AllowsExternalNetwork: false,
            AllowsDbMigration: false,
            AllowsReleaseCommercial: false);

    public static ProductLedgerOperatorSurfaceReadModelSource TestSafeLiveLedger(
        ProductLedgerPathLocalOnlyActivationResult activation,
        string sourceId) =>
        new(
            SourceKind: ProductLedgerOperatorSurfaceReadModelSourceKind.TestSafeLiveLedger,
            ActivationResult: activation,
            SourceId: sourceId,
            ExplicitTestSafeLocalOnlyReadModel: true,
            AllowsArbitraryPathInput: false,
            AllowsFilesystemScan: false,
            AllowsWrite: false,
            AllowsExport: false,
            AllowsProductCommandExecution: false,
            AllowsExternalNetwork: false,
            AllowsDbMigration: false,
            AllowsReleaseCommercial: false);
}

public sealed record ProductLedgerOperatorSurfaceReadModelSnapshot(
    ProductLedgerOperatorSurfaceReadModelMode Mode,
    string SourceId,
    string LedgerAuthority,
    string LedgerAuthorityBoundaryStatus,
    string LedgerPathClassification,
    string LedgerVerificationStatus,
    string CheckpointStatus,
    string RedactionRetentionGuardStatus,
    string ConcurrencyGuardStatus,
    string BoundedExportStatus,
    string OperatorAcceptanceStatus,
    string PublicLocalOnlyActionContractStatus,
    string VisualEvidenceStatus,
    string ScreenshotEvidenceStatus,
    int EntryCount,
    string HeadSequence,
    string HeadHashPrefix,
    string LedgerHashPrefix,
    bool UsesFixtureReadModel,
    bool UsesTestSafeLiveLedger,
    bool AllowsArbitraryPathInput,
    bool AllowsFilesystemScan,
    bool AllowsWrite,
    bool AllowsExport,
    bool AllowsProductCommandExecution,
    bool AllowsExternalNetwork,
    bool AllowsDbMigration,
    bool AllowsReleaseCommercial);

public sealed class ProductLedgerOperatorSurfaceReadModelProvider
{
    private const string RedactedPathClassification =
        "LOCAL_ONLY_BOUNDARY_PATH_REDACTED_NO_ARBITRARY_PATH_INPUT";

    private readonly ProductLedgerPathLocalOnlyActiveWriter writer;

    public ProductLedgerOperatorSurfaceReadModelProvider()
        : this(new ProductLedgerPathLocalOnlyActiveWriter())
    {
    }

    internal ProductLedgerOperatorSurfaceReadModelProvider(ProductLedgerPathLocalOnlyActiveWriter writer)
    {
        this.writer = writer;
    }

    public ProductLedgerOperatorSurfaceReadModelSnapshot Read(
        ProductLedgerOperatorSurfaceReadModelSource? source)
    {
        var safeSource = source ?? ProductLedgerOperatorSurfaceReadModelSource.FixtureSafe;
        return safeSource.SourceKind == ProductLedgerOperatorSurfaceReadModelSourceKind.TestSafeLiveLedger
            ? ReadTestSafeLiveLedger(safeSource)
            : FixtureSafe(safeSource.SourceId);
    }

    private ProductLedgerOperatorSurfaceReadModelSnapshot ReadTestSafeLiveLedger(
        ProductLedgerOperatorSurfaceReadModelSource source)
    {
        var activation = source.ActivationResult;
        if (!source.ExplicitTestSafeLocalOnlyReadModel
            || source.AllowsArbitraryPathInput
            || source.AllowsFilesystemScan
            || source.AllowsWrite
            || source.AllowsExport
            || source.AllowsProductCommandExecution
            || source.AllowsExternalNetwork
            || source.AllowsDbMigration
            || source.AllowsReleaseCommercial
            || activation is null
            || activation.Decision != ProductLedgerPathLocalOnlyActivationDecision.ActivatedLocalOnly
            || activation.Blockers.Count > 0
            || string.IsNullOrWhiteSpace(activation.ActiveLedgerFilePath)
            || string.IsNullOrWhiteSpace(activation.ActiveCheckpointFilePath)
            || !activation.ProductLedgerPathActive
            || !activation.ProductLedgerWriteAllowed
            || activation.ProductRuntimeEnabled
            || activation.ProductServiceRegistrationAllowed
            || activation.ProductCommandHandlersAllowed
            || activation.UiProductActionsAllowed
            || activation.DbProviderCloudNetworkAllowed
            || activation.KmsWormExternalTrustAllowed
            || activation.LiveAutomationAllowed
            || activation.ReleaseCommercialReady)
        {
            return FixtureSafe("fail-closed-fixture-safe-after-invalid-live-source");
        }

        var entries = writer.ReadVerified(activation);
        var head = entries.LastOrDefault();
        var ledgerHash = ProductLedgerLocalAppendOnlyHashing.ComputeLedgerHash(
            entries.Select(entry => (entry.Sequence, entry.EntryHash)));
        return new ProductLedgerOperatorSurfaceReadModelSnapshot(
            Mode: ProductLedgerOperatorSurfaceReadModelMode.TestSafeLiveLedgerReadModel,
            SourceId: source.SourceId,
            LedgerAuthority: nameof(ProductLedgerPathLocalOnlyActiveWriter),
            LedgerAuthorityBoundaryStatus: ProductLedgerPathLocalOnlyActiveWriter.ActiveLocalOnlyStatus,
            LedgerPathClassification: RedactedPathClassification,
            LedgerVerificationStatus: $"TEST_SAFE_LIVE_LEDGER_VERIFIED_READ_ONLY entry_count={entries.Count}",
            CheckpointStatus: $"TEST_SAFE_LIVE_LEDGER_CHECKPOINT_MATCHED head_sequence={entries.Count}",
            RedactionRetentionGuardStatus: "REDACTION_RETENTION_GUARDS_VERIFIED_FROM_SAFE_METADATA",
            ConcurrencyGuardStatus: "ACTIVE_WRITER_CONCURRENCY_LOCK_EVIDENCE_READ_ONLY_VISIBLE",
            BoundedExportStatus: "BOUNDED_LOCAL_EXPORT_STATUS_VISIBLE_NO_EXPORT_CALL",
            OperatorAcceptanceStatus: "OPERATOR_ACCEPTANCE_MATRIX_LINKABLE",
            PublicLocalOnlyActionContractStatus: "PUBLIC_LOCAL_ONLY_ACTION_CONTRACT_LINKABLE_ACTIONS_DISABLED",
            VisualEvidenceStatus: "LOCAL_DEV_VISUAL_QA_EVIDENCE_LINKABLE_STATIC_HTML_ONLY",
            ScreenshotEvidenceStatus: "SCREENSHOT_EVIDENCE_FIXTURE_LINKABLE_NO_LIVE_BROWSER",
            EntryCount: entries.Count,
            HeadSequence: entries.Count.ToString(System.Globalization.CultureInfo.InvariantCulture),
            HeadHashPrefix: Prefix(head?.EntryHash),
            LedgerHashPrefix: Prefix(ledgerHash),
            UsesFixtureReadModel: false,
            UsesTestSafeLiveLedger: true,
            AllowsArbitraryPathInput: false,
            AllowsFilesystemScan: false,
            AllowsWrite: false,
            AllowsExport: false,
            AllowsProductCommandExecution: false,
            AllowsExternalNetwork: false,
            AllowsDbMigration: false,
            AllowsReleaseCommercial: false);
    }

    private static ProductLedgerOperatorSurfaceReadModelSnapshot FixtureSafe(string sourceId)
    {
        var authority = ProductLedgerLocalLedgerTaxonomy.ForComponent(nameof(ProductLedgerPathLocalOnlyActiveWriter));
        return new ProductLedgerOperatorSurfaceReadModelSnapshot(
            Mode: ProductLedgerOperatorSurfaceReadModelMode.FixtureSafeReadModel,
            SourceId: sourceId,
            LedgerAuthority: nameof(ProductLedgerPathLocalOnlyActiveWriter),
            LedgerAuthorityBoundaryStatus: authority.BoundaryStatus,
            LedgerPathClassification: "FIXTURE_SAFE_NO_LEDGER_FILE_READ",
            LedgerVerificationStatus: "FIXTURE_SAFE_READ_MODEL_VERIFIED_NO_LEDGER_FILE_READ",
            CheckpointStatus: "FIXTURE_SAFE_CHECKPOINT_HEAD_VISIBLE",
            RedactionRetentionGuardStatus: "REDACTION_RETENTION_GUARDS_REQUIRED_AND_VISIBLE",
            ConcurrencyGuardStatus: "ACTIVE_WRITER_CONCURRENCY_LOCK_REQUIRED_AND_VISIBLE",
            BoundedExportStatus: "BOUNDED_LOCAL_EXPORT_STATUS_VISIBLE_NO_EXPORT_CALL",
            OperatorAcceptanceStatus: "OPERATOR_ACCEPTANCE_MATRIX_LINKABLE",
            PublicLocalOnlyActionContractStatus: "PUBLIC_LOCAL_ONLY_ACTION_CONTRACT_LINKABLE_ACTIONS_DISABLED",
            VisualEvidenceStatus: "LOCAL_DEV_VISUAL_QA_EVIDENCE_LINKABLE_STATIC_HTML_ONLY",
            ScreenshotEvidenceStatus: "SCREENSHOT_EVIDENCE_FIXTURE_LINKABLE_NO_LIVE_BROWSER",
            EntryCount: 0,
            HeadSequence: "fixture",
            HeadHashPrefix: "fixture",
            LedgerHashPrefix: "fixture",
            UsesFixtureReadModel: true,
            UsesTestSafeLiveLedger: false,
            AllowsArbitraryPathInput: false,
            AllowsFilesystemScan: false,
            AllowsWrite: false,
            AllowsExport: false,
            AllowsProductCommandExecution: false,
            AllowsExternalNetwork: false,
            AllowsDbMigration: false,
            AllowsReleaseCommercial: false);
    }

    private static string Prefix(string? value) =>
        string.IsNullOrWhiteSpace(value)
            ? "none"
            : value[..Math.Min(12, value.Length)];
}
