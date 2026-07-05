using Microsoft.VisualStudio.TestTools.UnitTesting;
using OneBrain.Core.Approval;

namespace OneBrain.Safety.Tests;

[TestClass]
[TestCategory("ApprovalHumanReview")]
[TestCategory("PhaseEApprovalHumanReview")]
public sealed class ProductLedgerLocalApprovalExecutionCandidateTests
{
    private static readonly DateTimeOffset Now = new(2026, 7, 5, 12, 0, 0, TimeSpan.Zero);

    [TestMethod]
    public void LocalApprovalExecutionCandidate_CompletesAllowedCommandsAsReadOnlyInMemoryOnly()
    {
        foreach (var command in ProductLedgerLocalApprovalExecutionCandidate.AllowedCommands())
        {
            var result = new ProductLedgerLocalApprovalExecutionCandidate().Execute(ReadyRequest(command));

            Assert.AreEqual(ProductLedgerLocalApprovalExecutionDecision.CompletedReadOnlyInMemory, result.Decision, command.ToString());
            Assert.AreEqual(command, result.CandidateActionKind);
            Assert.AreEqual(ProductLedgerInternalCommandDecision.CompletedReadOnlyInMemory, result.CommandResult!.Decision);
            Assert.IsTrue(result.LocalOnly);
            Assert.IsTrue(result.InternalOnly);
            Assert.IsTrue(result.DefaultOff);
            Assert.IsTrue(result.FailClosed);
            Assert.IsTrue(result.ReadOnlyOrInMemory);
            Assert.IsFalse(result.PublicUiActionAvailable);
            Assert.IsFalse(result.ProductCommandHandlerAvailable);
            Assert.IsFalse(result.ProductiveServiceRegistrationAvailable);
            Assert.IsFalse(result.PhysicalExportCreated);
            Assert.IsFalse(result.FileWritePerformed);
            Assert.IsFalse(result.ProviderCloudNetworkAvailable);
            Assert.IsFalse(result.DbMigrationAvailable);
            Assert.IsFalse(result.KmsWormExternalTrustAvailable);
            Assert.IsFalse(result.BrowserCdpWcuOcrRecipesLiveAvailable);
            Assert.IsFalse(result.ReleaseCommercialReady);
        }
    }

    [TestMethod]
    public void LocalApprovalExecutionCandidate_FailsClosedForMissingStaleMismatchedOrUnverifiedApproval()
    {
        var missing = new ProductLedgerLocalApprovalExecutionCandidate().Execute(null);
        var stale = Execute(ReadyRequest(ProductLedgerInternalCommandKind.ViewDiagnostics) with { ApprovedAtUtc = Now.AddMinutes(-10) });
        var future = Execute(ReadyRequest(ProductLedgerInternalCommandKind.ViewDiagnostics) with { ApprovedAtUtc = Now.AddMinutes(1) });
        var actionMismatch = Execute(ReadyRequest(ProductLedgerInternalCommandKind.ViewDiagnostics) with { ApprovedActionName = ProductLedgerInternalCommandKind.ViewLedgerReadiness.ToString() });
        var evidenceMismatch = Execute(ReadyRequest(ProductLedgerInternalCommandKind.ViewDiagnostics) with { CurrentEvidenceHash = "different-evidence" });
        var policyFailed = Execute(ReadyRequest(ProductLedgerInternalCommandKind.ViewDiagnostics) with { PolicyRecheckPassed = false });
        var readModelFailed = Execute(ReadyRequest(ProductLedgerInternalCommandKind.ViewDiagnostics) with { ReadModelVerified = false });

        AssertRejected(missing, ProductLedgerLocalApprovalExecutionBlocker.MissingRequest);
        AssertRejected(stale, ProductLedgerLocalApprovalExecutionBlocker.ApprovalExpiredOrNotYetValid);
        AssertRejected(future, ProductLedgerLocalApprovalExecutionBlocker.ApprovalExpiredOrNotYetValid);
        AssertRejected(actionMismatch, ProductLedgerLocalApprovalExecutionBlocker.ActionMismatch);
        AssertRejected(evidenceMismatch, ProductLedgerLocalApprovalExecutionBlocker.EvidenceMismatch);
        AssertRejected(policyFailed, ProductLedgerLocalApprovalExecutionBlocker.PolicyRecheckFailed);
        AssertRejected(readModelFailed, ProductLedgerLocalApprovalExecutionBlocker.ReadModelNotVerified);
    }

    [TestMethod]
    public void LocalApprovalExecutionCandidate_BlocksExportWritePublicProductExternalPathAndRawClaims()
    {
        var ready = ReadyRequest(ProductLedgerInternalCommandKind.ViewDiagnostics);
        var cases = new Dictionary<ProductLedgerLocalApprovalExecutionRequest, ProductLedgerLocalApprovalExecutionBlocker>
        {
            [ready with { ExplicitLocalOnlyInternalApprovalExecutionScope = false }] = ProductLedgerLocalApprovalExecutionBlocker.MissingExplicitLocalOnlyInternalApprovalExecutionScope,
            [ready with { RequestsPublicUiAction = true }] = ProductLedgerLocalApprovalExecutionBlocker.PublicUiActionRequested,
            [ready with { RequestsDestructiveAction = true }] = ProductLedgerLocalApprovalExecutionBlocker.DestructiveActionRequested,
            [ready with { RequestsProductCommandHandler = true }] = ProductLedgerLocalApprovalExecutionBlocker.ProductCommandHandlerRequested,
            [ready with { RequestsProductiveServiceRegistration = true }] = ProductLedgerLocalApprovalExecutionBlocker.ProductiveServiceRegistrationRequested,
            [ready with { ClaimsProviderCloudNetwork = true }] = ProductLedgerLocalApprovalExecutionBlocker.ProviderCloudNetworkClaimed,
            [ready with { ClaimsDbMigration = true }] = ProductLedgerLocalApprovalExecutionBlocker.DbMigrationClaimed,
            [ready with { ClaimsKmsWormExternalTrust = true }] = ProductLedgerLocalApprovalExecutionBlocker.KmsWormExternalTrustClaimed,
            [ready with { ClaimsBrowserCdpWcuOcrRecipesLive = true }] = ProductLedgerLocalApprovalExecutionBlocker.BrowserCdpWcuOcrRecipesLiveClaimed,
            [ready with { ClaimsReleaseCommercial = true }] = ProductLedgerLocalApprovalExecutionBlocker.ReleaseCommercialClaimed,
            [ready with { ClaimsExternalTelemetryOrSync = true }] = ProductLedgerLocalApprovalExecutionBlocker.ExternalTelemetryOrSyncClaimed,
            [ready with { ClaimsBillingLicensingCloud = true }] = ProductLedgerLocalApprovalExecutionBlocker.BillingLicensingCloudClaimed,
            [ready with { RequestsPhysicalExport = true }] = ProductLedgerLocalApprovalExecutionBlocker.PhysicalExportRequested,
            [ready with { RequestsFileWrite = true }] = ProductLedgerLocalApprovalExecutionBlocker.FileWriteRequested,
            [ready with { ClaimsAppendOutsideBoundedWriter = true }] = ProductLedgerLocalApprovalExecutionBlocker.AppendOutsideBoundedWriterClaimed,
            [ready with { ClaimsArbitraryPathInput = true }] = ProductLedgerLocalApprovalExecutionBlocker.ArbitraryPathInputClaimed,
            [ready with { ClaimsRawPayloadOrSecret = true }] = ProductLedgerLocalApprovalExecutionBlocker.RawPayloadOrSecretClaimed
        };

        foreach (var testCase in cases)
        {
            AssertRejected(Execute(testCase.Key), testCase.Value);
        }
    }

    [TestMethod]
    public void LocalApprovalExecutionCandidate_ExcludesBoundedExportAndOtherForbiddenCommands()
    {
        var blocked = new[]
        {
            ProductLedgerInternalCommandKind.LocalReportPhysicalExportBoundedInternal,
            ProductLedgerInternalCommandKind.LocalReportPreviewInMemory,
            ProductLedgerInternalCommandKind.ExportDisabledLocalReportPreview,
            ProductLedgerInternalCommandKind.PropertyCorpusHardeningPreview,
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
            var result = Execute(ReadyRequest(command));

            AssertRejected(result, ProductLedgerLocalApprovalExecutionBlocker.CommandNotAllowedForApprovalExecution);
            Assert.IsNull(result.CommandResult);
        }
    }

    [TestMethod]
    public void LocalApprovalExecutionCandidate_SourceHasNoRouteUiDiWriteExportNetworkDbKmsLiveOrRelease()
    {
        var source = ReadRepoFile("src", "OneBrain.Core", "Approval", "ProductLedgerLocalApprovalExecutionCandidate.cs");
        var forbidden = new[]
        {
            "IServiceCollection",
            "AddSingleton",
            "AddScoped",
            "AddTransient",
            "IHostedService",
            "Controller",
            "MapPost",
            "MapGet",
            "HttpContext",
            "Request.Query",
            "QueryString",
            "File.WriteAllText",
            "File.AppendAllText",
            "File.WriteAllBytes",
            "File.Delete",
            "Directory.GetFiles",
            "Directory.EnumerateFiles",
            "Process.Start",
            "HttpClient",
            "WebSocket",
            "DbContext",
            "MigrationBuilder",
            "KmsClient",
            "WormStore",
            "ProductLedgerLocalReportExportService",
            ".Export(",
            ".Append(",
            "PilotRecipeExecutor",
            "PilotRecipeExecutionGate.Evaluate(",
            "NODAL_OS_ENABLE_PILOT_RECIPE_EXECUTION",
            "ReleaseCommercialReady: true",
            "PublicUiActionAvailable: true",
            "ProductCommandHandlerAvailable: true",
            "ProductiveServiceRegistrationAvailable: true",
            "PhysicalExportCreated: true",
            "FileWritePerformed: true"
        };

        foreach (var fragment in forbidden)
        {
            Assert.IsFalse(source.Contains(fragment, StringComparison.Ordinal), fragment);
        }
    }

    private static ProductLedgerLocalApprovalExecutionResult Execute(ProductLedgerLocalApprovalExecutionRequest request) =>
        new ProductLedgerLocalApprovalExecutionCandidate().Execute(request);

    private static ProductLedgerLocalApprovalExecutionRequest ReadyRequest(ProductLedgerInternalCommandKind command) =>
        new(
            ExplicitLocalOnlyInternalApprovalExecutionScope: true,
            ApprovalId: "approval-local-only-001",
            ApprovedAtUtc: Now.AddMinutes(-1),
            NowUtc: Now,
            MaxApprovalAge: TimeSpan.FromMinutes(5),
            CandidateActionKind: command,
            ApprovedActionName: command.ToString(),
            ApprovedEvidenceHash: "evidence-hash-001",
            CurrentEvidenceHash: "evidence-hash-001",
            PolicyRecheckPassed: true,
            ReadModelVerified: true,
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
            ClaimsAppendOutsideBoundedWriter: false,
            ClaimsArbitraryPathInput: false,
            ClaimsRawPayloadOrSecret: false);

    private static void AssertRejected(
        ProductLedgerLocalApprovalExecutionResult result,
        ProductLedgerLocalApprovalExecutionBlocker blocker)
    {
        Assert.AreEqual(ProductLedgerLocalApprovalExecutionDecision.Rejected, result.Decision, blocker.ToString());
        CollectionAssert.Contains(result.Blockers.ToArray(), blocker, blocker.ToString());
        Assert.IsTrue(result.LocalOnly);
        Assert.IsTrue(result.InternalOnly);
        Assert.IsTrue(result.DefaultOff);
        Assert.IsTrue(result.FailClosed);
        Assert.IsFalse(result.PublicUiActionAvailable);
        Assert.IsFalse(result.ProductCommandHandlerAvailable);
        Assert.IsFalse(result.ProductiveServiceRegistrationAvailable);
        Assert.IsFalse(result.PhysicalExportCreated);
        Assert.IsFalse(result.FileWritePerformed);
        Assert.IsFalse(result.ProviderCloudNetworkAvailable);
        Assert.IsFalse(result.DbMigrationAvailable);
        Assert.IsFalse(result.KmsWormExternalTrustAvailable);
        Assert.IsFalse(result.BrowserCdpWcuOcrRecipesLiveAvailable);
        Assert.IsFalse(result.ReleaseCommercialReady);
    }

    private static string ReadRepoFile(params string[] segments) =>
        File.ReadAllText(Path.Combine(new[] { RepoRoot() }.Concat(segments).ToArray()));

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
                "ProductLedgerLocalApprovalExecutionCandidate.cs")))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        Assert.Fail("repo root not found");
        return string.Empty;
    }
}

