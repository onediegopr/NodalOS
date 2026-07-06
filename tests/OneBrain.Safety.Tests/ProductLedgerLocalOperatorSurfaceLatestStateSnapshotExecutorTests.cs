using System.Text.Json;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using OneBrain.Core.Approval;

namespace OneBrain.Safety.Tests;

[TestClass]
[TestCategory("ApprovalHumanReview")]
[TestCategory("PhaseEApprovalHumanReview")]
public sealed class ProductLedgerLocalOperatorSurfaceLatestStateSnapshotExecutorTests
{
    [TestMethod]
    public void LatestStateSnapshotExecutor_CreatesJsonSnapshotUnderAllowedBoundary()
    {
        using var fixture = LatestStateSnapshotFixture.Create();
        var request = ReadyRequest();

        var result = fixture.Executor.CreateSnapshot(request);
        var fullPath = fixture.FullPath(result);
        var json = File.ReadAllText(fullPath);

        Assert.AreEqual(ProductLedgerLocalOperatorSurfaceLatestStateSnapshotDecision.SnapshotCreatedLocalOnly, result.Decision);
        Assert.AreEqual(ProductLedgerLocalOperatorSurfaceLatestStateSnapshotState.SnapshotCreatedLocalOnly, result.State);
        Assert.AreEqual(0, result.Blockers.Count);
        Assert.IsTrue(File.Exists(fullPath));
        Assert.IsTrue(Path.GetFullPath(fullPath).StartsWith(Path.GetFullPath(fixture.AllowedBoundaryRoot), StringComparison.OrdinalIgnoreCase));
        Assert.IsTrue(result.SnapshotRelativePath.EndsWith(".json", StringComparison.Ordinal));
        StringAssert.StartsWith(result.SnapshotRelativePath, ProductLedgerLocalOperatorSurfaceLatestStateSnapshotExecutor.AllowedRelativeOutputBoundary);
        StringAssert.Contains(result.SnapshotRelativePath, "operator-surface-latest-state-snapshot-latest-state-snapshot-001-");
        Assert.IsTrue(result.CreateOnly);
        Assert.IsTrue(result.Versioned);
        Assert.IsTrue(result.JsonOnly);
        Assert.IsFalse(result.OverwriteAllowed);
        Assert.IsFalse(result.LatestPointerOverwriteAllowed);
        Assert.IsFalse(result.UserSelectedPathAllowed);
        Assert.IsFalse(result.PayloadControlledRootAllowed);
        Assert.IsTrue(result.CanonicalizationPassed);
        Assert.IsTrue(result.ReparseValidationPassed);
        Assert.IsTrue(result.RedactionApplied);
        Assert.IsTrue(result.HistoricalEvidenceOnly);
        Assert.IsFalse(result.AuthorityLiveProduct);
        Assert.IsFalse(result.ProductionAllowed);
        Assert.IsFalse(result.PublicProductAllowed);
        Assert.IsFalse(result.ShellAllowed);
        Assert.IsFalse(result.CommandExecutionAllowed);
        Assert.IsFalse(result.ProviderCloudNetworkAvailable);
        Assert.IsFalse(result.DbMigrationAvailable);
        Assert.IsFalse(result.KmsWormExternalTrustAvailable);
        Assert.IsFalse(result.BrowserCdpWcuOcrRecipesLiveAvailable);
        Assert.IsFalse(result.PilotRunAvailable);
        Assert.IsFalse(result.ReleaseCommercialReady);
        Assert.AreEqual(ProductLedgerLocalOperatorSurfaceLatestStateSnapshotExecutor.Classification, result.Classification);
        Assert.AreEqual(ProductLedgerLocalOperatorSurfaceLatestStateSnapshotExecutor.StaleStateClassification, result.StaleStateClassification);
        Assert.AreEqual(request.OperatorSurfaceModelHash, result.OperatorSurfaceModelHash);
        Assert.AreEqual(64, result.SnapshotContentHash.Length);
        Assert.AreEqual(64, result.CheckpointHash.Length);
        CollectionAssert.Contains(result.NegativeFlags.ToArray(), "not authority live/product");
        CollectionAssert.Contains(result.NegativeFlags.ToArray(), "no latest pointer overwrite");
        StringAssert.Contains(json, "\"classification\": \"LOCAL_INTERNAL_DEV_ONLY_HISTORICAL_SNAPSHOT\"");
        StringAssert.Contains(json, "\"historicalEvidenceOnly\": true");
        StringAssert.Contains(json, "\"authorityLiveProduct\": false");
        StringAssert.Contains(json, "\"publicProduct\": false");
        StringAssert.Contains(json, "\"production\": false");
        StringAssert.Contains(json, "\"releaseCommercial\": false");
        StringAssert.Contains(json, "\"complianceCustody\": false");
        StringAssert.Contains(json, ProductLedgerLocalOperatorSurfaceLatestStateSnapshotExecutor.AllowedRelativeOutputBoundary);
        _ = JsonSerializer.Deserialize<JsonElement>(json);
        Assert.IsFalse(json.Contains("password=", StringComparison.OrdinalIgnoreCase));
        Assert.IsFalse(json.Contains(@"C:\Users\", StringComparison.OrdinalIgnoreCase));
        Assert.IsFalse(json.Contains(fixture.WorkspaceRoot, StringComparison.OrdinalIgnoreCase));
        Assert.IsFalse(File.Exists(Path.Combine(fixture.AllowedBoundaryRoot, "latest.json")));
    }

    [TestMethod]
    public void LatestStateSnapshotExecutor_ReplaysExactSnapshotAndBlocksConflictingExistingFile()
    {
        using var fixture = LatestStateSnapshotFixture.Create();
        var first = fixture.Executor.CreateSnapshot(ReadyRequest());
        var replay = fixture.Executor.CreateSnapshot(ReadyRequest());

        Assert.AreEqual(ProductLedgerLocalOperatorSurfaceLatestStateSnapshotDecision.SnapshotCreatedLocalOnly, first.Decision);
        Assert.AreEqual(ProductLedgerLocalOperatorSurfaceLatestStateSnapshotDecision.IdempotentReplay, replay.Decision);
        Assert.AreEqual(first.SnapshotContentHash, replay.SnapshotContentHash);

        using var conflictFixture = LatestStateSnapshotFixture.Create();
        var expected = conflictFixture.FullPath(conflictFixture.Executor.CreateSnapshot(ReadyRequest()));
        File.WriteAllText(expected, "{\"safe\":\"different\"}");
        var conflict = conflictFixture.Executor.CreateSnapshot(ReadyRequest());

        Assert.AreEqual(ProductLedgerLocalOperatorSurfaceLatestStateSnapshotDecision.Rejected, conflict.Decision);
        CollectionAssert.Contains(conflict.Blockers.ToArray(), ProductLedgerLocalOperatorSurfaceLatestStateSnapshotBlocker.ExistingSnapshotConflict);
    }

    [TestMethod]
    public void LatestStateSnapshotExecutor_NormalizesWhitespaceIdsAndRejectsUnsafeIdCorpus()
    {
        using var positiveFixture = LatestStateSnapshotFixture.Create();
        var positive = positiveFixture.Executor.CreateSnapshot(ReadyRequest() with
        {
            SnapshotId = " Latest.State_002 ",
            ActionId = " Action.002 "
        });

        Assert.AreEqual(ProductLedgerLocalOperatorSurfaceLatestStateSnapshotDecision.SnapshotCreatedLocalOnly, positive.Decision);
        StringAssert.Contains(positive.SnapshotRelativePath, "operator-surface-latest-state-snapshot-latest-state-002-");
        Assert.IsFalse(positive.SnapshotRelativePath.Contains(' '));
        Assert.IsTrue(File.Exists(positiveFixture.FullPath(positive)));

        var ready = ReadyRequest();
        var unsafeCases = new (ProductLedgerLocalOperatorSurfaceLatestStateSnapshotRequest Request, ProductLedgerLocalOperatorSurfaceLatestStateSnapshotBlocker Blocker)[]
        {
            (ready with { SnapshotId = null }, ProductLedgerLocalOperatorSurfaceLatestStateSnapshotBlocker.MissingSnapshotId),
            (ready with { SnapshotId = "   " }, ProductLedgerLocalOperatorSurfaceLatestStateSnapshotBlocker.MissingSnapshotId),
            (ready with { SnapshotId = "../unsafe" }, ProductLedgerLocalOperatorSurfaceLatestStateSnapshotBlocker.UnsafeSnapshotId),
            (ready with { SnapshotId = "%2e%2e%2funsafe" }, ProductLedgerLocalOperatorSurfaceLatestStateSnapshotBlocker.UnsafeSnapshotId),
            (ready with { SnapshotId = "C:unsafe" }, ProductLedgerLocalOperatorSurfaceLatestStateSnapshotBlocker.UnsafeSnapshotId),
            (ready with { SnapshotId = "unsafe/name" }, ProductLedgerLocalOperatorSurfaceLatestStateSnapshotBlocker.UnsafeSnapshotId),
            (ready with { SnapshotId = new string('s', 81) }, ProductLedgerLocalOperatorSurfaceLatestStateSnapshotBlocker.UnsafeSnapshotId),
            (ready with { ActionId = null }, ProductLedgerLocalOperatorSurfaceLatestStateSnapshotBlocker.MissingActionId),
            (ready with { ActionId = "unsafe\\name" }, ProductLedgerLocalOperatorSurfaceLatestStateSnapshotBlocker.UnsafeActionId),
            (ready with { OperatorSurface = null }, ProductLedgerLocalOperatorSurfaceLatestStateSnapshotBlocker.MissingOperatorSurface),
            (ready with { OperatorSurfaceModelHash = null }, ProductLedgerLocalOperatorSurfaceLatestStateSnapshotBlocker.MissingOperatorSurfaceModelHash),
            (ready with { EvidenceReferences = [] }, ProductLedgerLocalOperatorSurfaceLatestStateSnapshotBlocker.MissingEvidenceReferences)
        };

        foreach (var testCase in unsafeCases)
        {
            using var fixture = LatestStateSnapshotFixture.Create();
            var result = fixture.Executor.CreateSnapshot(testCase.Request);

            Assert.AreEqual(ProductLedgerLocalOperatorSurfaceLatestStateSnapshotDecision.Rejected, result.Decision, testCase.Blocker.ToString());
            CollectionAssert.Contains(result.Blockers.ToArray(), testCase.Blocker, testCase.Blocker.ToString());
            Assert.IsFalse(Directory.Exists(fixture.AllowedBoundaryRoot) && Directory.GetFiles(fixture.AllowedBoundaryRoot, "*.json").Length > 0);
        }
    }

    [TestMethod]
    public void LatestStateSnapshotExecutor_BlocksMissingChainPathCommandNetworkOverwriteAndUnsafeClaims()
    {
        var ready = ReadyRequest();
        var cases = new (ProductLedgerLocalOperatorSurfaceLatestStateSnapshotRequest? Request, ProductLedgerLocalOperatorSurfaceLatestStateSnapshotBlocker Blocker)[]
        {
            (null, ProductLedgerLocalOperatorSurfaceLatestStateSnapshotBlocker.MissingRequest),
            (ready with { ExplicitLatestStateSnapshotScope = false }, ProductLedgerLocalOperatorSurfaceLatestStateSnapshotBlocker.MissingExplicitSnapshotScope),
            (ready with { DevelopmentMode = false }, ProductLedgerLocalOperatorSurfaceLatestStateSnapshotBlocker.NonDevelopmentMode),
            (ready with { LocalMode = false }, ProductLedgerLocalOperatorSurfaceLatestStateSnapshotBlocker.NonLocalMode),
            (ready with { InternalMode = false }, ProductLedgerLocalOperatorSurfaceLatestStateSnapshotBlocker.NonInternalMode),
            (ready with { SnapshotId = "..\\unsafe" }, ProductLedgerLocalOperatorSurfaceLatestStateSnapshotBlocker.UnsafeSnapshotId),
            (ready with { ActionKind = null }, ProductLedgerLocalOperatorSurfaceLatestStateSnapshotBlocker.UnknownActionKind),
            (ready with { OperatorSurfaceModelHash = new string('b', 64) }, ProductLedgerLocalOperatorSurfaceLatestStateSnapshotBlocker.OperatorSurfaceModelHashMismatch),
            (ready with { OperatorSurface = ReadySurface(ProductLedgerLocalApprovalDecisionSnapshot.PendingPreviewOnly) }, ProductLedgerLocalOperatorSurfaceLatestStateSnapshotBlocker.ApprovalDecisionNotApproved),
            (ready with { ProposedPath = @"C:\Users\fixture\unsafe.json" }, ProductLedgerLocalOperatorSurfaceLatestStateSnapshotBlocker.PayloadPathFieldRejected),
            (ready with { ProposedRoot = @"C:\Users\fixture" }, ProductLedgerLocalOperatorSurfaceLatestStateSnapshotBlocker.PayloadPathFieldRejected),
            (ready with { ProposedFilename = "..\\unsafe.json" }, ProductLedgerLocalOperatorSurfaceLatestStateSnapshotBlocker.PayloadPathFieldRejected),
            (ready with { ProposedCommand = "cmd /c whoami" }, ProductLedgerLocalOperatorSurfaceLatestStateSnapshotBlocker.PayloadCommandFieldRejected),
            (ready with { ProposedUrl = "https://local.invalid/action" }, ProductLedgerLocalOperatorSurfaceLatestStateSnapshotBlocker.PayloadNetworkProviderFieldRejected),
            (ready with { ProposedProvider = "provider-live" }, ProductLedgerLocalOperatorSurfaceLatestStateSnapshotBlocker.PayloadNetworkProviderFieldRejected),
            (ready with { ProposedDbMigration = "migration" }, ProductLedgerLocalOperatorSurfaceLatestStateSnapshotBlocker.PayloadNetworkProviderFieldRejected),
            (ready with { ClaimsArbitraryPathInput = true }, ProductLedgerLocalOperatorSurfaceLatestStateSnapshotBlocker.ClaimsArbitraryPathInput),
            (ready with { ClaimsFilesystemScan = true }, ProductLedgerLocalOperatorSurfaceLatestStateSnapshotBlocker.ClaimsFilesystemScan),
            (ready with { RequestsOverwrite = true }, ProductLedgerLocalOperatorSurfaceLatestStateSnapshotBlocker.RequestsOverwrite),
            (ready with { RequestsLatestPointerOverwrite = true }, ProductLedgerLocalOperatorSurfaceLatestStateSnapshotBlocker.RequestsLatestPointerOverwrite),
            (ready with { RequestsUserSelectedPath = true }, ProductLedgerLocalOperatorSurfaceLatestStateSnapshotBlocker.RequestsUserSelectedPath),
            (ready with { RequestsShellOrSubprocess = true }, ProductLedgerLocalOperatorSurfaceLatestStateSnapshotBlocker.RequestsShellOrSubprocess),
            (ready with { ClaimsArbitraryCommandExecution = true }, ProductLedgerLocalOperatorSurfaceLatestStateSnapshotBlocker.ClaimsArbitraryCommandExecution),
            (ready with { ClaimsProviderCloudNetwork = true }, ProductLedgerLocalOperatorSurfaceLatestStateSnapshotBlocker.ClaimsProviderCloudNetwork),
            (ready with { ClaimsDbMigration = true }, ProductLedgerLocalOperatorSurfaceLatestStateSnapshotBlocker.ClaimsDbMigration),
            (ready with { ClaimsKmsWormExternalTrust = true }, ProductLedgerLocalOperatorSurfaceLatestStateSnapshotBlocker.ClaimsKmsWormExternalTrust),
            (ready with { ClaimsBrowserCdpWcuOcrRecipesLive = true }, ProductLedgerLocalOperatorSurfaceLatestStateSnapshotBlocker.ClaimsBrowserCdpWcuOcrRecipesLive),
            (ready with { ClaimsPilotRun = true }, ProductLedgerLocalOperatorSurfaceLatestStateSnapshotBlocker.ClaimsPilotRun),
            (ready with { ClaimsReleaseCommercial = true }, ProductLedgerLocalOperatorSurfaceLatestStateSnapshotBlocker.ClaimsReleaseCommercial)
        };

        foreach (var testCase in cases)
        {
            using var fixture = LatestStateSnapshotFixture.Create();
            var request = testCase.Request;
            if (request?.OperatorSurface is not null
                && testCase.Blocker != ProductLedgerLocalOperatorSurfaceLatestStateSnapshotBlocker.OperatorSurfaceModelHashMismatch)
            {
                request = request with
                {
                    OperatorSurfaceModelHash = ProductLedgerLocalOperatorSurfaceLatestStateSnapshotExecutor.ComputeOperatorSurfaceModelHash(request.OperatorSurface)
                };
            }

            var result = fixture.Executor.CreateSnapshot(request);

            Assert.AreEqual(ProductLedgerLocalOperatorSurfaceLatestStateSnapshotDecision.Rejected, result.Decision, testCase.Blocker.ToString());
            CollectionAssert.Contains(result.Blockers.ToArray(), testCase.Blocker, testCase.Blocker.ToString());
            Assert.IsFalse(Directory.Exists(fixture.AllowedBoundaryRoot) && Directory.GetFiles(fixture.AllowedBoundaryRoot, "*.json").Length > 0);
        }
    }

    [TestMethod]
    public void LatestStateSnapshotExecutor_OptionCorpusFailsClosedForUnsafeBoundaryCapabilities()
    {
        var unsafeOptions = new Func<string, ProductLedgerLocalOperatorSurfaceLatestStateSnapshotOptions>[]
        {
            root => LatestStateSnapshotFixture.Options(root) with { ExplicitLatestStateSnapshotBoundary = false },
            root => LatestStateSnapshotFixture.Options(root) with { AllowsArbitraryPathInput = true },
            root => LatestStateSnapshotFixture.Options(root) with { AllowsFilesystemScan = true },
            root => LatestStateSnapshotFixture.Options(root) with { AllowsOverwrite = true },
            root => LatestStateSnapshotFixture.Options(root) with { AllowsLatestPointerOverwrite = true },
            root => LatestStateSnapshotFixture.Options(root) with { AllowsUserSelectedPath = true },
            root => LatestStateSnapshotFixture.Options(root) with { AllowsShellOrSubprocess = true },
            root => LatestStateSnapshotFixture.Options(root) with { AllowsCommandExecution = true },
            root => LatestStateSnapshotFixture.Options(root) with { AllowsNetwork = true },
            root => LatestStateSnapshotFixture.Options(root) with { AllowsDb = true },
            root => LatestStateSnapshotFixture.Options(root) with { AllowsKmsWormExternalTrust = true },
            root => LatestStateSnapshotFixture.Options(root) with { AllowsReleaseCommercial = true },
            _ => LatestStateSnapshotFixture.Options(string.Empty)
        };

        foreach (var optionsFactory in unsafeOptions)
        {
            using var fixture = LatestStateSnapshotFixture.Create();
            var executor = new ProductLedgerLocalOperatorSurfaceLatestStateSnapshotExecutor(optionsFactory(fixture.WorkspaceRoot));
            var result = executor.CreateSnapshot(ReadyRequest());

            Assert.AreEqual(ProductLedgerLocalOperatorSurfaceLatestStateSnapshotDecision.Rejected, result.Decision);
            CollectionAssert.Contains(result.Blockers.ToArray(), ProductLedgerLocalOperatorSurfaceLatestStateSnapshotBlocker.OutputBoundaryRejected);
            Assert.IsFalse(Directory.Exists(fixture.AllowedBoundaryRoot) && Directory.GetFiles(fixture.AllowedBoundaryRoot, "*.json").Length > 0);
        }
    }

    [TestMethod]
    public void LatestStateSnapshotExecutor_SurfaceDomShowsHistoricalSnapshotState()
    {
        using var fixture = LatestStateSnapshotFixture.Create();
        var result = fixture.Executor.CreateSnapshot(ReadyRequest());
        var html = new ProductLedgerLocalDevRoutePreview()
            .Render(
                ProductLedgerLocalDevRoutePreview.CreateDefaultLocalDevRequest(),
                ProductLedgerOperatorSurfaceReadModelSource.FixtureSafe,
                ReadyApproval(),
                ReadyNoOpExecution(),
                ReadyBoundedExecution(),
                ReadyLocalDraft(),
                ReadyWorkspaceTestJailDraft(),
                ReadyUserWorkspaceAllowlistedDraft(),
                result)
            .HtmlSnapshot;

        StringAssert.Contains(html, "data-testid=\"product-ledger-latest-state-snapshot-state\"");
        StringAssert.Contains(html, "data-state=\"SnapshotCreatedLocalOnly\"");
        StringAssert.Contains(html, "data-historical-evidence-only=\"true\"");
        StringAssert.Contains(html, "data-authority-live-product=\"false\"");
        StringAssert.Contains(html, "data-overwrite-allowed=\"false\"");
        StringAssert.Contains(html, "data-latest-pointer-overwrite-allowed=\"false\"");
        StringAssert.Contains(html, ProductLedgerLocalOperatorSurfaceLatestStateSnapshotExecutor.AllowedRelativeOutputBoundary);
        StringAssert.Contains(html, "json-only immutable versioned create-only no-overwrite no latest pointer overwrite");
        StringAssert.Contains(html, ProductLedgerLocalOperatorSurfaceLatestStateSnapshotExecutor.StaleStateClassification);
        Assert.IsFalse(html.Contains("password=", StringComparison.OrdinalIgnoreCase));
        Assert.IsFalse(html.Contains(@"C:\Users\", StringComparison.OrdinalIgnoreCase));
        Assert.IsFalse(html.Contains("data-executable=\"true\"", StringComparison.OrdinalIgnoreCase));
    }

    [TestMethod]
    public void LatestStateSnapshotExecutor_SourceHasOnlyBoundedCreateOnlyJsonWriteAndNoForbiddenRuntime()
    {
        var source = ReadRepoFile("src", "OneBrain.Core", "Approval", "ProductLedgerLocalOperatorSurfaceLatestStateSnapshotExecutor.cs");
        foreach (var fragment in new[]
        {
            "Process.Start",
            "ShellExecute",
            "System.Diagnostics.Process",
            "HttpClient",
            "WebSocket",
            "DbContext",
            "MigrationBuilder",
            "KmsClient",
            "WormStore",
            "PilotRecipeExecutor",
            "PilotRecipeExecutionGate.Evaluate(",
            "NODAL_OS_ENABLE_PILOT_RECIPE_EXECUTION",
            "ProductLedgerInternalCommandHandler",
            "Directory.GetFiles",
            "Directory.EnumerateFiles",
            "FileMode.OpenOrCreate",
            "FileMode.Create,",
            "OverwriteAllowed: true",
            "LatestPointerOverwriteAllowed: true",
            "UserSelectedPathAllowed: true",
            "PayloadControlledRootAllowed: true",
            "AuthorityLiveProduct: true",
            "ProductionAllowed: true",
            "PublicProductAllowed: true",
            "ReleaseCommercialReady: true"
        })
        {
            Assert.IsFalse(source.Contains(fragment, StringComparison.Ordinal), fragment);
        }

        StringAssert.Contains(source, "FileMode.CreateNew");
        StringAssert.Contains(source, "FileAttributes.ReparsePoint");
        StringAssert.Contains(source, ProductLedgerLocalOperatorSurfaceLatestStateSnapshotExecutor.AllowedRelativeOutputBoundary);
        StringAssert.Contains(source, "NO_OVERWRITE");
        StringAssert.Contains(source, "NO_LATEST_POINTER_OVERWRITE");
        StringAssert.Contains(source, "NO_COMMAND_EXECUTION");
        StringAssert.Contains(source, "NO_SHELL_SUBPROCESS");
        StringAssert.Contains(source, "HISTORICAL_EVIDENCE_ONLY");
    }

    private static ProductLedgerLocalOperatorSurfaceLatestStateSnapshotRequest ReadyRequest()
    {
        var surface = ReadySurface();
        var hash = ProductLedgerLocalOperatorSurfaceLatestStateSnapshotExecutor.ComputeOperatorSurfaceModelHash(surface);
        return new(
            ExplicitLatestStateSnapshotScope: true,
            DevelopmentMode: true,
            LocalMode: true,
            InternalMode: true,
            SnapshotId: "latest-state-snapshot-001",
            ActionId: "latest-state-snapshot-action-001",
            ActionKind: ProductLedgerLocalOperatorSurfaceLatestStateSnapshotActionKind.LocalOperatorSurfaceLatestStateSnapshotCreateOnly,
            OperatorSurface: surface,
            OperatorSurfaceModelHash: hash,
            EvidenceReferences: ["docs/qa/product-ledger-local-operator-surface-latest-state-snapshot-implementation/report.md"],
            ProposedPath: null,
            ProposedRoot: null,
            ProposedFilename: null,
            ProposedCommand: null,
            ProposedUrl: null,
            ProposedProvider: null,
            ProposedDbMigration: null,
            ClaimsArbitraryPathInput: false,
            ClaimsFilesystemScan: false,
            RequestsOverwrite: false,
            RequestsLatestPointerOverwrite: false,
            RequestsUserSelectedPath: false,
            RequestsPublicUiAction: false,
            RequestsProductCommandExecution: false,
            RequestsProductCommandHandler: false,
            RequestsProductiveServiceRegistration: false,
            RequestsShellOrSubprocess: false,
            ClaimsArbitraryCommandExecution: false,
            ClaimsProviderCloudNetwork: false,
            ClaimsDbMigration: false,
            ClaimsKmsWormExternalTrust: false,
            ClaimsBrowserCdpWcuOcrRecipesLive: false,
            ClaimsPilotRun: false,
            ClaimsReleaseCommercial: false);
    }

    private static ProductLedgerOperatorSurfaceModel ReadySurface(
        ProductLedgerLocalApprovalDecisionSnapshot? approval = null) =>
        new ProductLedgerLocalDevRoutePreview()
            .Render(
                ProductLedgerLocalDevRoutePreview.CreateDefaultLocalDevRequest(),
                ProductLedgerOperatorSurfaceReadModelSource.FixtureSafe,
                approval ?? ReadyApproval(),
                ReadyNoOpExecution(),
                ReadyBoundedExecution(),
                ReadyLocalDraft(),
                ReadyWorkspaceTestJailDraft(),
                ReadyUserWorkspaceAllowlistedDraft())
            .CanonicalSurface;

    private static ProductLedgerLocalApprovalDecisionSnapshot ReadyApproval() =>
        ProductLedgerLocalApprovalDecisionSnapshot.PendingPreviewOnly with
        {
            Decision = ProductLedgerLocalApprovalDecisionStoreDecision.LoadedLocalOnly,
            State = ProductLedgerLocalApprovalDecisionState.ApprovedLocalOnly,
            ApprovalId = "approval-latest-state-snapshot-001",
            CandidateActionKind = ProductLedgerInternalCommandKind.ViewLedgerReadiness.ToString(),
            CandidateEvidenceHash = new string('a', 64),
            CandidateEvidenceHashPrefix = new string('a', 12),
            OperatorDecision = ProductLedgerLocalApprovalOperatorDecisionKind.Approve.ToString(),
            EvidenceReferences = ["docs/qa/nodal-os-local-approval-real-operator-input-state-persistence/report.md"],
            StatusText = ProductLedgerLocalApprovalDecisionStateStore.PersistedStatus
        };

    private static ProductLedgerLocalApprovedActionExecutionSnapshot ReadyNoOpExecution() =>
        ProductLedgerLocalApprovedActionExecutionSnapshot.Pending with
        {
            Decision = ProductLedgerLocalApprovedActionExecutionDecision.LoadedLocalOnly,
            State = ProductLedgerLocalApprovedActionExecutionState.NoOpExecutionCompletedLocalOnly,
            ExecutionId = "approved-no-op-latest-state-snapshot-001",
            ApprovalId = "approval-latest-state-snapshot-001",
            CandidateActionKind = ProductLedgerInternalCommandKind.ViewLedgerReadiness.ToString(),
            CandidateEvidenceHash = new string('a', 64),
            CandidateEvidenceHashPrefix = new string('a', 12),
            ExecutionResultHashPrefix = "noophash0001",
            EvidenceReferences = ["docs/qa/nodal-os-approved-action-execution-local-only-no-op-to-bounded-action/report.md"],
            StatusText = ProductLedgerLocalApprovedActionNoOpExecutor.CompletedStatus
        };

    private static ProductLedgerLocalBoundedApprovedActionSnapshot ReadyBoundedExecution() =>
        ProductLedgerLocalBoundedApprovedActionSnapshot.Pending with
        {
            Decision = ProductLedgerLocalBoundedApprovedActionDecision.LoadedLocalOnly,
            State = ProductLedgerLocalBoundedApprovedActionState.BoundedExecutionCompletedLocalOnly,
            ExecutionId = "bounded-latest-state-snapshot-001",
            ActionId = "bounded-internal-completion-marker-001",
            ActionKind = ProductLedgerLocalBoundedApprovedActionKind.BoundedInternalCompletionMarker.ToString(),
            ApprovalId = "approval-latest-state-snapshot-001",
            NoOpExecutionId = "approved-no-op-latest-state-snapshot-001",
            CandidateActionKind = ProductLedgerInternalCommandKind.ViewLedgerReadiness.ToString(),
            CandidateEvidenceHash = new string('a', 64),
            CandidateEvidenceHashPrefix = new string('a', 12),
            ResultHashPrefix = "boundedhash1",
            EvidenceReferences = ["docs/qa/nodal-os-approved-action-execution-local-only-no-op-to-bounded-action/report.md"],
            StatusText = ProductLedgerLocalBoundedApprovedActionExecutor.CompletedStatus
        };

    private static ProductLedgerLocalApprovedHandoffReportDraftSnapshot ReadyLocalDraft() =>
        ProductLedgerLocalApprovedHandoffReportDraftSnapshot.Pending with
        {
            Decision = ProductLedgerLocalApprovedHandoffReportDraftDecision.DraftCreatedLocalOnly,
            State = ProductLedgerLocalApprovedHandoffReportDraftState.DraftCreatedLocalOnly,
            DraftId = "draft-handoff-draft-action-001-abcdef123456",
            DraftRelativePath = ProductLedgerLocalApprovedHandoffReportDraftExecutor.AllowedRelativeOutputBoundary + "local-approved-handoff-draft-handoff-draft-action-001-aaaaaaaaaaaa.md",
            RedactionApplied = true,
            EvidenceRefs = ["docs/qa/product-ledger-local-approved-handoff-report-draft-implementation/report.md"],
            ContentHash = new string('d', 64),
            ContentHashPrefix = new string('d', 12),
            StatusText = ProductLedgerLocalApprovedHandoffReportDraftExecutor.CompletedStatus
        };

    private static ProductLedgerLocalWorkspaceTestJailHandoffDraftSnapshot ReadyWorkspaceTestJailDraft() =>
        ProductLedgerLocalWorkspaceTestJailHandoffDraftSnapshot.Pending with
        {
            Decision = ProductLedgerLocalWorkspaceTestJailHandoffDraftDecision.DraftCreatedWorkspaceTestJailOnly,
            State = ProductLedgerLocalWorkspaceTestJailHandoffDraftState.DraftCreatedWorkspaceTestJailOnly,
            DraftId = "workspace-test-jail-draft-workspace-draft-action-001-eeeeeeeeeeee",
            DraftRelativePath = ProductLedgerLocalWorkspaceTestJailHandoffDraftExecutor.AllowedRelativeOutputBoundary + "workspace-test-jail-handoff-draft-workspace-draft-action-001-aaaaaaaaaaaa.md",
            RedactionApplied = true,
            EvidenceRefs = ["docs/qa/product-ledger-workspace-test-jail-handoff-draft-implementation/report.md"],
            ContentHash = new string('e', 64),
            ContentHashPrefix = new string('e', 12),
            StatusText = ProductLedgerLocalWorkspaceTestJailHandoffDraftExecutor.CompletedStatus
        };

    private static ProductLedgerLocalUserWorkspaceAllowlistedHandoffDraftSnapshot ReadyUserWorkspaceAllowlistedDraft() =>
        ProductLedgerLocalUserWorkspaceAllowlistedHandoffDraftSnapshot.Pending with
        {
            Decision = ProductLedgerLocalUserWorkspaceAllowlistedHandoffDraftDecision.DraftCreatedUserWorkspaceAllowlistedOnly,
            State = ProductLedgerLocalUserWorkspaceAllowlistedHandoffDraftState.DraftCreatedUserWorkspaceAllowlistedOnly,
            DraftId = "user-workspace-allowlisted-draft-user-workspace-draft-action-001-ffffffffffff",
            DraftRelativePath = ProductLedgerLocalUserWorkspaceAllowlistedHandoffDraftExecutor.AllowedRelativeOutputBoundary + "user-workspace-allowlisted-handoff-draft-user-workspace-draft-action-001-aaaaaaaaaaaa.md",
            RedactionApplied = true,
            EvidenceRefs = ["docs/qa/product-ledger-user-workspace-allowlisted-handoff-draft-implementation/report.md"],
            ContentHash = new string('f', 64),
            ContentHashPrefix = new string('f', 12),
            StatusText = ProductLedgerLocalUserWorkspaceAllowlistedHandoffDraftExecutor.CompletedStatus
        };

    private static string ReadRepoFile(params string[] segments) =>
        File.ReadAllText(Path.Combine(new[] { RepoRoot() }.Concat(segments).ToArray()));

    private static string RepoRoot()
    {
        var directory = new DirectoryInfo(Directory.GetCurrentDirectory());
        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "OneBrain.slnx")))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        Assert.Fail("repo root not found");
        return string.Empty;
    }

    private sealed class LatestStateSnapshotFixture : IDisposable
    {
        private LatestStateSnapshotFixture(string workspaceRoot)
        {
            WorkspaceRoot = workspaceRoot;
            Executor = new ProductLedgerLocalOperatorSurfaceLatestStateSnapshotExecutor(Options(workspaceRoot));
        }

        public static ProductLedgerLocalOperatorSurfaceLatestStateSnapshotOptions Options(string workspaceRoot) =>
            new(
                WorkspaceRootPath: workspaceRoot,
                ExplicitLatestStateSnapshotBoundary: true,
                AllowsArbitraryPathInput: false,
                AllowsFilesystemScan: false,
                AllowsOverwrite: false,
                AllowsLatestPointerOverwrite: false,
                AllowsUserSelectedPath: false,
                AllowsShellOrSubprocess: false,
                AllowsCommandExecution: false,
                AllowsNetwork: false,
                AllowsDb: false,
                AllowsKmsWormExternalTrust: false,
                AllowsReleaseCommercial: false);

        public string WorkspaceRoot { get; }

        public string AllowedBoundaryRoot =>
            Path.Combine(WorkspaceRoot, "docs", "test-output", "product-ledger", "operator-surface-latest-state-snapshots");

        public ProductLedgerLocalOperatorSurfaceLatestStateSnapshotExecutor Executor { get; }

        public string FullPath(ProductLedgerLocalOperatorSurfaceLatestStateSnapshotResult snapshot) =>
            Path.Combine(WorkspaceRoot, snapshot.SnapshotRelativePath.Replace('/', Path.DirectorySeparatorChar));

        public static LatestStateSnapshotFixture Create()
        {
            var root = Path.Combine(RepoRoot(), ".tmp-product-ledger-latest-state-snapshot-tests", Guid.NewGuid().ToString("N"));
            return new LatestStateSnapshotFixture(root);
        }

        public void Dispose()
        {
            var tempRoot = Path.Combine(RepoRoot(), ".tmp-product-ledger-latest-state-snapshot-tests");
            if (Directory.Exists(tempRoot))
            {
                Directory.Delete(tempRoot, recursive: true);
            }
        }
    }
}
