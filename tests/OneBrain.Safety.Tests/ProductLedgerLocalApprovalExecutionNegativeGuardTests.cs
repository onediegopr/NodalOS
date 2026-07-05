using Microsoft.VisualStudio.TestTools.UnitTesting;
using OneBrain.Core.Approval;

namespace OneBrain.Safety.Tests;

[TestClass]
[TestCategory("ApprovalHumanReview")]
[TestCategory("PhaseEApprovalHumanReview")]
public sealed class ProductLedgerLocalApprovalExecutionNegativeGuardTests
{
    private static readonly ProductLedgerInternalCommandKind[] ApprovalExecutionReadOnlyAllowlist =
    [
        ProductLedgerInternalCommandKind.ViewDiagnostics,
        ProductLedgerInternalCommandKind.ViewLedgerReadiness,
        ProductLedgerInternalCommandKind.ViewRuntimeGateStatus,
        ProductLedgerInternalCommandKind.ViewCheckpointHeadStatus,
        ProductLedgerInternalCommandKind.ViewEvidenceGates,
        ProductLedgerInternalCommandKind.StaticScanPreview,
        ProductLedgerInternalCommandKind.RequestExternalAuditPreview
    ];

    [TestMethod]
    public void LocalApprovalExecutionBoundary_DocsExcludeImplementationStateMutationWriteExportAndPublicExposure()
    {
        var adr = ReadRepoFile("docs", "adr", "product-ledger-local-approval-execution-design-only-boundary.md");
        var audit = ReadRepoFile("docs", "adr", "product-ledger-local-approval-execution-design-only-external-audit-read-only.md");

        StringAssert.Contains(adr, "No implementation in this block.");
        StringAssert.Contains(adr, "No approval state mutation.");
        StringAssert.Contains(adr, "No product ledger append/write/export.");
        StringAssert.Contains(adr, "No public UI action.");
        StringAssert.Contains(adr, "No productive command handler exposure.");
        StringAssert.Contains(adr, "No arbitrary path input.");
        StringAssert.Contains(audit, "No code implementation.");
        StringAssert.Contains(audit, "No append/write/export.");
        StringAssert.Contains(audit, "No public UI action.");
    }

    [TestMethod]
    public void LocalApprovalExecutionBoundary_UsesNarrowAllowlistAndExcludesBoundedExport()
    {
        CollectionAssert.DoesNotContain(
            ApprovalExecutionReadOnlyAllowlist,
            ProductLedgerInternalCommandKind.LocalReportPhysicalExportBoundedInternal);

        var publicActionSurface = ReadRepoFile("src", "OneBrain.Core", "Approval", "ProductLedgerPublicUiActionSurface.cs");
        StringAssert.Contains(publicActionSurface, "LocalReportPhysicalExportBoundedInternal");

        var adr = ReadRepoFile("docs", "adr", "product-ledger-local-approval-execution-design-only-boundary.md");
        StringAssert.Contains(adr, "`LocalReportPhysicalExportBoundedInternal` is intentionally excluded");
    }

    [TestMethod]
    public void LocalApprovalExecutionBoundary_AllowlistedCommandsCompleteOnlyReadOnlyInMemory()
    {
        foreach (var command in ApprovalExecutionReadOnlyAllowlist)
        {
            var result = new ProductLedgerInternalCommandHandler().Execute(ReadyRequest(RouterPreview(command)));

            Assert.AreEqual(ProductLedgerInternalCommandDecision.CompletedReadOnlyInMemory, result.Decision, command.ToString());
            Assert.IsTrue(result.InternalOnly);
            Assert.IsTrue(result.LocalOnly);
            Assert.IsTrue(result.NonDestructive);
            Assert.IsTrue(result.ReadOnlyOrInMemory);
            Assert.IsTrue(result.ExecutionPreview.InMemoryOnly);
            Assert.IsFalse(result.ExecutionPreview.PhysicalExportCreated);
            Assert.IsFalse(result.ExecutionPreview.FileWritePerformed);
            Assert.IsFalse(result.ExecutionPreview.ExecutableCallbackInvoked);
            Assert.IsFalse(result.PublicCommandExposureAvailable);
            Assert.IsFalse(result.PublicUiActionAvailable);
            Assert.IsFalse(result.ProductCommandHandlerAvailable);
            Assert.IsFalse(result.ProductiveServiceRegistrationAvailable);
            Assert.IsFalse(result.ProviderCloudNetworkAvailable);
            Assert.IsFalse(result.DbMigrationAvailable);
            Assert.IsFalse(result.KmsWormExternalTrustAvailable);
            Assert.IsFalse(result.BrowserCdpWcuOcrRecipesLiveAvailable);
            Assert.IsFalse(result.ReleaseCommercialReady);
        }
    }

    [TestMethod]
    public void LocalApprovalExecutionBoundary_BlockedCommandsRemainRejectedForApprovalExecutionCandidate()
    {
        var blocked = new[]
        {
            ProductLedgerInternalCommandKind.LocalReportPhysicalExportBoundedInternal,
            ProductLedgerInternalCommandKind.EnablePublicUi,
            ProductLedgerInternalCommandKind.ExecuteAction,
            ProductLedgerInternalCommandKind.DestructiveWrite,
            ProductLedgerInternalCommandKind.RegisterCommandHandler,
            ProductLedgerInternalCommandKind.RegisterProductDI,
            ProductLedgerInternalCommandKind.ConnectProvider,
            ProductLedgerInternalCommandKind.EnableCloud,
            ProductLedgerInternalCommandKind.RunMigration,
            ProductLedgerInternalCommandKind.EnableKms,
            ProductLedgerInternalCommandKind.EnableWorm,
            ProductLedgerInternalCommandKind.EnableExternalTrust,
            ProductLedgerInternalCommandKind.RunBrowserCdp,
            ProductLedgerInternalCommandKind.RunWcu,
            ProductLedgerInternalCommandKind.RunOcr,
            ProductLedgerInternalCommandKind.RunRecipesLive,
            ProductLedgerInternalCommandKind.Release,
            ProductLedgerInternalCommandKind.CommercialLaunch
        };

        foreach (var command in blocked)
        {
            CollectionAssert.DoesNotContain(ApprovalExecutionReadOnlyAllowlist, command, command.ToString());
        }
    }

    [TestMethod]
    public void LocalApprovalExecutionBoundary_RouteAndPreviewSourcesHaveNoExecutionHandlerPostPathWriteOrExport()
    {
        var source = string.Join(
            Environment.NewLine,
            ReadRepoFile("src", "OneBrain.Core", "Approval", "ProductLedgerLocalDevRoutePreview.cs"),
            ReadRepoFile("src", "OneBrain.Core", "Approval", "ProductLedgerLocalApprovalPreviewLoop.cs"),
            ReadRepoFile("src", "OneBrain.Core", "Approval", "ProductLedgerOperatorSurfaceModel.cs"));
        var forbidden = new[]
        {
            "MapPost",
            "Request.Query",
            "QueryString",
            "HttpContext",
            "ProductLedgerInternalCommandHandler",
            "ProductLedgerLocalReportExportService().Export",
            ".Append(",
            ".Export(",
            "File.WriteAllText",
            "File.AppendAllText",
            "File.WriteAllBytes",
            "File.Delete",
            "Directory.GetFiles",
            "Directory.EnumerateFiles",
            "DbContext",
            "MigrationBuilder",
            "HttpClient",
            "WebSocket",
            "PilotRecipeExecutor",
            "PilotRecipeExecutionGate.Evaluate(",
            "NODAL_OS_ENABLE_PILOT_RECIPE_EXECUTION"
        };

        foreach (var fragment in forbidden)
        {
            Assert.IsFalse(source.Contains(fragment, StringComparison.OrdinalIgnoreCase), fragment);
        }

        var mapper = ReadRepoFile("src", "OneBrain.Pilot", "ProductLedgerLocalDevRouteEndpointMapper.cs");
        StringAssert.Contains(mapper, "environment.IsDevelopment()");
        StringAssert.Contains(mapper, "LocalApprovalDecisionRoute");
        StringAssert.Contains(mapper, "LocalApprovalExecutionRoute");
        StringAssert.Contains(mapper, "ProductLedgerLocalApprovalDecisionStateStore");
        StringAssert.Contains(mapper, "ProductLedgerLocalApprovedActionNoOpExecutor");
        Assert.AreEqual(2, Count(mapper, "endpoints.MapPost("));
        foreach (var fragment in new[]
        {
            "ProductLedgerInternalCommandHandler",
            "ProductLedgerLocalReportExportService().Export",
            ".Append(",
            ".Export(",
            "Process.Start",
            "HttpClient",
            "WebSocket",
            "DbContext",
            "MigrationBuilder",
            "PilotRecipeExecutor",
            "PilotRecipeExecutionGate.Evaluate(",
            "NODAL_OS_ENABLE_PILOT_RECIPE_EXECUTION",
            "Request.Query",
            "QueryString"
        })
        {
            Assert.IsFalse(mapper.Contains(fragment, StringComparison.OrdinalIgnoreCase), fragment);
        }
    }

    [TestMethod]
    public void LocalApprovalExecutionBoundary_DocsRequireFreshApprovalPolicyRecheckVerifiedReadModelAndNoPathInput()
    {
        var adr = ReadRepoFile("docs", "adr", "product-ledger-local-approval-execution-design-only-boundary.md");

        StringAssert.Contains(adr, "approval decision is explicit, fresh and tied to the rendered candidate action");
        StringAssert.Contains(adr, "policy is re-evaluated immediately before execution");
        StringAssert.Contains(adr, "read model is verified from fixture-safe or injected test-safe live ledger source");
        StringAssert.Contains(adr, "no arbitrary path is accepted from query, body, headers, environment or UI");
        StringAssert.Contains(adr, "Every missing or malformed state must fail closed.");
    }

    private static ProductLedgerInternalCommandPreviewResult RouterPreview(ProductLedgerInternalCommandKind command) =>
        new ProductLedgerInternalCommandPreviewRouter().Preview(new ProductLedgerInternalCommandPreviewRequest(
            ExplicitInternalLocalOnlyNoOpReadOnlyScope: true,
            CommandKind: command,
            RawCommandName: command.ToString(),
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

    private static ProductLedgerInternalCommandRequest ReadyRequest(ProductLedgerInternalCommandPreviewResult? preview) =>
        new(
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
            ClaimsAppendOutsideBoundedWriter: false);

    private static string ReadRepoFile(params string[] segments) =>
        File.ReadAllText(Path.Combine(new[] { RepoRoot() }.Concat(segments).ToArray()));

    private static int Count(string source, string value)
    {
        var count = 0;
        var index = 0;
        while ((index = source.IndexOf(value, index, StringComparison.Ordinal)) >= 0)
        {
            count++;
            index += value.Length;
        }

        return count;
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
                "ProductLedgerLocalApprovalPreviewLoop.cs")))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        Assert.Fail("repo root not found");
        return string.Empty;
    }
}
