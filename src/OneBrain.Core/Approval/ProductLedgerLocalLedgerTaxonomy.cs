namespace OneBrain.Core.Approval;

public enum ProductLedgerLocalLedgerMode
{
    DisabledScaffoldHistorical,
    LocalTempTestOnly,
    LocalOnlyActive,
    DurableAuditTrailTestOnly,
    FutureProductiveLocalOnly
}

public enum ProductLedgerLocalLedgerRole
{
    Writer,
    ReaderVerifier,
    Checkpoint,
    Exporter,
    EvidenceLinker,
    OperatorSurfaceReadModel
}

public sealed record ProductLedgerLocalLedgerConcept(
    ProductLedgerLocalLedgerMode Mode,
    string ComponentName,
    IReadOnlyList<ProductLedgerLocalLedgerRole> Roles,
    bool ProductLedgerAuthority,
    bool TestOnly,
    bool Historical,
    bool PhysicalWriteAllowed,
    bool RequiresRedactionRetentionGuard,
    bool RequiresConcurrencyLock,
    bool CanClaimReleaseCommercial,
    string BoundaryStatus);

public static class ProductLedgerLocalLedgerTaxonomy
{
    public static IReadOnlyList<ProductLedgerLocalLedgerConcept> Concepts { get; } =
    [
        new(
            Mode: ProductLedgerLocalLedgerMode.LocalOnlyActive,
            ComponentName: nameof(ProductLedgerPathLocalOnlyActiveWriter),
            Roles:
            [
                ProductLedgerLocalLedgerRole.Writer,
                ProductLedgerLocalLedgerRole.ReaderVerifier,
                ProductLedgerLocalLedgerRole.Checkpoint,
                ProductLedgerLocalLedgerRole.EvidenceLinker
            ],
            ProductLedgerAuthority: true,
            TestOnly: false,
            Historical: false,
            PhysicalWriteAllowed: true,
            RequiresRedactionRetentionGuard: true,
            RequiresConcurrencyLock: true,
            CanClaimReleaseCommercial: false,
            BoundaryStatus: "ACTIVE_PRODUCT_LEDGER_PATH_LOCAL_ONLY"),
        new(
            Mode: ProductLedgerLocalLedgerMode.LocalTempTestOnly,
            ComponentName: nameof(ProductLedgerPathLocalTempWriterTestOnly),
            Roles:
            [
                ProductLedgerLocalLedgerRole.Writer,
                ProductLedgerLocalLedgerRole.ReaderVerifier,
                ProductLedgerLocalLedgerRole.Checkpoint
            ],
            ProductLedgerAuthority: false,
            TestOnly: true,
            Historical: false,
            PhysicalWriteAllowed: true,
            RequiresRedactionRetentionGuard: false,
            RequiresConcurrencyLock: false,
            CanClaimReleaseCommercial: false,
            BoundaryStatus: "LOCAL_TEMP_WRITER_TEST_ONLY_NOT_PRODUCT_LEDGER_PATH"),
        new(
            Mode: ProductLedgerLocalLedgerMode.DisabledScaffoldHistorical,
            ComponentName: nameof(ProductLedgerPathWriterScaffoldDisabled),
            Roles: [ProductLedgerLocalLedgerRole.EvidenceLinker],
            ProductLedgerAuthority: false,
            TestOnly: true,
            Historical: true,
            PhysicalWriteAllowed: false,
            RequiresRedactionRetentionGuard: false,
            RequiresConcurrencyLock: false,
            CanClaimReleaseCommercial: false,
            BoundaryStatus: "DISABLED_WRITER_SCAFFOLD_HISTORICAL_NON_AUTHORITATIVE"),
        new(
            Mode: ProductLedgerLocalLedgerMode.DurableAuditTrailTestOnly,
            ComponentName: nameof(DurableAuditTrailAppendOnlyMinimal),
            Roles:
            [
                ProductLedgerLocalLedgerRole.Writer,
                ProductLedgerLocalLedgerRole.ReaderVerifier,
                ProductLedgerLocalLedgerRole.Checkpoint
            ],
            ProductLedgerAuthority: false,
            TestOnly: true,
            Historical: false,
            PhysicalWriteAllowed: true,
            RequiresRedactionRetentionGuard: true,
            RequiresConcurrencyLock: false,
            CanClaimReleaseCommercial: false,
            BoundaryStatus: "DURABLE_AUDIT_TRAIL_TEST_ONLY_NOT_PRODUCT_LEDGER_AUTHORITY"),
        new(
            Mode: ProductLedgerLocalLedgerMode.FutureProductiveLocalOnly,
            ComponentName: "FutureProductiveLocalOnly",
            Roles: [ProductLedgerLocalLedgerRole.OperatorSurfaceReadModel],
            ProductLedgerAuthority: false,
            TestOnly: false,
            Historical: false,
            PhysicalWriteAllowed: false,
            RequiresRedactionRetentionGuard: true,
            RequiresConcurrencyLock: true,
            CanClaimReleaseCommercial: false,
            BoundaryStatus: "FUTURE_NOT_ACTIVE_REQUIRES_SEPARATE_GO")
    ];

    public static ProductLedgerLocalLedgerConcept ForComponent(string componentName) =>
        Concepts.FirstOrDefault(concept => string.Equals(concept.ComponentName, componentName, StringComparison.Ordinal))
        ?? throw new InvalidOperationException("unknown Product Ledger local ledger concept");
}
