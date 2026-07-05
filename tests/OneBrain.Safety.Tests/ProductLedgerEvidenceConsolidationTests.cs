using Microsoft.VisualStudio.TestTools.UnitTesting;
using OneBrain.Core.Approval;

namespace OneBrain.Safety.Tests;

[TestClass]
[TestCategory("ApprovalHumanReview")]
[TestCategory("PhaseEApprovalHumanReview")]
[TestCategory("ProductLedgerEvidenceConsolidation")]
public sealed class ProductLedgerEvidenceConsolidationTests
{
    [TestMethod]
    public void Taxonomy_DeclaresSingleActiveProductLedgerAuthority()
    {
        var concepts = ProductLedgerLocalLedgerTaxonomy.Concepts;
        var activeAuthorities = concepts.Where(concept => concept.ProductLedgerAuthority).ToArray();

        Assert.AreEqual(1, activeAuthorities.Length);
        Assert.AreEqual(nameof(ProductLedgerPathLocalOnlyActiveWriter), activeAuthorities[0].ComponentName);
        Assert.AreEqual(ProductLedgerLocalLedgerMode.LocalOnlyActive, activeAuthorities[0].Mode);
        Assert.IsTrue(activeAuthorities[0].PhysicalWriteAllowed);
        Assert.IsTrue(activeAuthorities[0].RequiresRedactionRetentionGuard);
        Assert.IsTrue(activeAuthorities[0].RequiresConcurrencyLock);
        Assert.IsFalse(activeAuthorities[0].CanClaimReleaseCommercial);
    }

    [TestMethod]
    public void Taxonomy_ClassifiesTempScaffoldDurableAndFutureModesAsNonAuthoritative()
    {
        var temp = ProductLedgerLocalLedgerTaxonomy.ForComponent(nameof(ProductLedgerPathLocalTempWriterTestOnly));
        var scaffold = ProductLedgerLocalLedgerTaxonomy.ForComponent(nameof(ProductLedgerPathWriterScaffoldDisabled));
        var durable = ProductLedgerLocalLedgerTaxonomy.ForComponent(nameof(DurableAuditTrailAppendOnlyMinimal));
        var future = ProductLedgerLocalLedgerTaxonomy.ForComponent("FutureProductiveLocalOnly");

        Assert.IsFalse(temp.ProductLedgerAuthority);
        Assert.IsTrue(temp.TestOnly);
        Assert.IsFalse(temp.Historical);
        StringAssert.Contains(temp.BoundaryStatus, "NOT_PRODUCT_LEDGER_PATH");

        Assert.IsFalse(scaffold.ProductLedgerAuthority);
        Assert.IsTrue(scaffold.TestOnly);
        Assert.IsTrue(scaffold.Historical);
        Assert.IsFalse(scaffold.PhysicalWriteAllowed);

        Assert.IsFalse(durable.ProductLedgerAuthority);
        Assert.IsTrue(durable.TestOnly);
        StringAssert.Contains(durable.BoundaryStatus, "NOT_PRODUCT_LEDGER_AUTHORITY");

        Assert.IsFalse(future.ProductLedgerAuthority);
        Assert.IsFalse(future.PhysicalWriteAllowed);
        StringAssert.Contains(future.BoundaryStatus, "REQUIRES_SEPARATE_GO");
    }

    [TestMethod]
    public void SharedHashing_PreservesEntryAndLedgerFormat()
    {
        var metadata = new Dictionary<string, string>
        {
            ["authority"] = "local-only-policy-bound",
            ["failure"] = "replay-rollback-evidence",
            ["redaction"] = "redacted-before-persistence"
        };
        var entryHash = ProductLedgerLocalAppendOnlyHashing.ComputeEntryHash(
            sequence: 1,
            candidateId: "ledger-candidate-001",
            safePayloadHash: new string('a', 64),
            metadata,
            previousHash: "GENESIS");
        var ledgerHash = ProductLedgerLocalAppendOnlyHashing.ComputeLedgerHash([(1, entryHash)]);

        Assert.AreEqual("c291e844620257f7e453eb883bcb6b78ff7dfa280c8a36f678526e0a1cb0f190", entryHash);
        Assert.AreEqual("36a9f5edac2574fb546bfd05cbbd00a2b2c24d7f0a210441f9c87300735bd410", ledgerHash);
    }

    [TestMethod]
    public void WriterSources_UseSharedHashingWithoutChangingWriteAuthority()
    {
        var activeSource = File.ReadAllText(Path.Combine(RepoRoot(), "src", "OneBrain.Core", "Approval", "ProductLedgerPathLocalOnlyActiveWriter.cs"));
        var tempSource = File.ReadAllText(Path.Combine(RepoRoot(), "src", "OneBrain.Core", "Approval", "ProductLedgerPathLocalTempWriterTestOnly.cs"));
        var hashingSource = File.ReadAllText(Path.Combine(RepoRoot(), "src", "OneBrain.Core", "Approval", "ProductLedgerLocalAppendOnlyHashing.cs"));

        StringAssert.Contains(activeSource, "ProductLedgerLocalAppendOnlyHashing.ComputeEntryHash");
        StringAssert.Contains(activeSource, "ProductLedgerLocalAppendOnlyHashing.ComputeLedgerHash");
        StringAssert.Contains(tempSource, "ProductLedgerLocalAppendOnlyHashing.ComputeEntryHash");
        StringAssert.Contains(tempSource, "ProductLedgerLocalAppendOnlyHashing.ComputeLedgerHash");
        StringAssert.Contains(hashingSource, "SHA256.HashData");
        Assert.IsFalse(activeSource.Contains("SHA256.HashData", StringComparison.Ordinal));
        Assert.IsFalse(tempSource.Contains("SHA256.HashData", StringComparison.Ordinal));
    }

    [TestMethod]
    public void HistoricalScaffold_RemainsDisabledAndNonAuthoritative()
    {
        var result = new ProductLedgerPathWriterScaffoldDisabled().Evaluate(null);

        Assert.AreEqual(ProductLedgerPathWriterScaffoldDecision.Rejected, result.Decision);
        Assert.IsFalse(result.DisabledWriterScaffoldReady);
        Assert.IsFalse(result.ProductLedgerPathActive);
        Assert.IsFalse(result.ProductLedgerWriteAllowed);
        Assert.IsFalse(result.ProductRuntimeEnabled);
        Assert.IsFalse(result.ProductCommandHandlersAllowed);
        Assert.IsFalse(result.ReleaseCommercialReady);
        StringAssert.Contains(ProductLedgerPathWriterScaffoldDisabled.DisabledWriterScaffoldStatus, "HISTORICAL_NON_AUTHORITATIVE");
    }

    [TestMethod]
    public void CommandHandler_RemainsPreviewOnlyWithoutPublicProductExecutionAuthority()
    {
        var router = new ProductLedgerInternalCommandPreviewRouter();
        var preview = router.Preview(new ProductLedgerInternalCommandPreviewRequest(
            ExplicitInternalLocalOnlyNoOpReadOnlyScope: true,
            CommandKind: ProductLedgerInternalCommandKind.ViewDiagnostics,
            RawCommandName: nameof(ProductLedgerInternalCommandKind.ViewDiagnostics),
            SourcePreview: null,
            RequestsPublicUiAction: false,
            RequestsDestructiveAction: false,
            RequestsProductCommandHandler: false,
            RequestsProductiveServiceRegistration: false,
            ClaimsProviderCloudNetwork: false,
            ClaimsDbMigration: false,
            ClaimsKmsWormExternalTrust: false,
            ClaimsBrowserCdpWcuOcrRecipesLive: false,
            ClaimsReleaseCommercial: false,
            ClaimsExternalTelemetryOrSync: false,
            ClaimsBillingLicensingCloud: false,
            ClaimsWriterExecutionOutsideValidatedLocalOnlyPolicy: false));
        var result = new ProductLedgerInternalCommandHandler().Execute(new ProductLedgerInternalCommandRequest(
            ExplicitInternalLocalOnlyNonDestructiveScope: true,
            RouterPreview: preview,
            RequestsPublicUiAction: false,
            RequestsDestructiveAction: false,
            RequestsProductCommandHandler: false,
            RequestsProductiveServiceRegistration: false,
            ClaimsProviderCloudNetwork: false,
            ClaimsDbMigration: false,
            ClaimsKmsWormExternalTrust: false,
            ClaimsBrowserCdpWcuOcrRecipesLive: false,
            ClaimsReleaseCommercial: false,
            ClaimsExternalTelemetryOrSync: false,
            ClaimsBillingLicensingCloud: false,
            RequestsPhysicalExport: false,
            RequestsFileWrite: false,
            ClaimsAppendOutsideBoundedWriter: false));

        Assert.AreEqual(ProductLedgerInternalCommandDecision.CompletedReadOnlyInMemory, result.Decision);
        Assert.IsTrue(result.PreviewOnly);
        Assert.IsTrue(result.ExecutionPreview.PreviewOnly);
        Assert.IsTrue(result.ExecutionPreview.NoOp);
        Assert.IsFalse(result.PublicExecutionAllowed);
        Assert.IsFalse(result.ProductCommandExecutionAllowed);
        Assert.IsFalse(result.ExecutionPreview.PublicExecutionAllowed);
        Assert.IsFalse(result.ExecutionPreview.ProductCommandExecutionAllowed);
        Assert.IsFalse(result.PublicCommandExposureAvailable);
        Assert.IsFalse(result.ProductCommandHandlerAvailable);
        Assert.IsFalse(result.FileWritePerformed);
        StringAssert.Contains(result.StatusText, "PREVIEW_ONLY");
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
                "ProductLedgerLocalAppendOnlyHashing.cs")))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        Assert.Fail("repo root not found");
        return string.Empty;
    }
}
