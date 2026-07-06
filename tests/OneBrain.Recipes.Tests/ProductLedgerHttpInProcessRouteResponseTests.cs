using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using OneBrain.Core.Approval;
using OneBrain.Pilot;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace OneBrain.Recipes.Tests;

[TestClass]
[TestCategory("ApprovalHumanReview")]
[TestCategory("PhaseEApprovalHumanReview")]
public sealed class ProductLedgerHttpInProcessRouteResponseTests
{
    [TestMethod]
    public async Task ProductLedgerRouteResponse_DevelopmentHostReturnsCanonicalLocalOnlyHtml()
    {
        await using var app = BuildLocalOnlyApp(Environments.Development);
        await app.StartAsync(TestContext.CancellationTokenSource.Token);

        using var client = new HttpClient { BaseAddress = new Uri(ServerAddress(app)) };
        using var response = await client.GetAsync(
            ProductLedgerLocalDevRoutePreview.RouteTemplatePreview,
            TestContext.CancellationTokenSource.Token);
        var html = await response.Content.ReadAsStringAsync(TestContext.CancellationTokenSource.Token);

        Assert.AreEqual(System.Net.HttpStatusCode.OK, response.StatusCode);
        Assert.AreEqual("text/html", response.Content.Headers.ContentType?.MediaType);
        StringAssert.Contains(response.Content.Headers.ContentType?.CharSet ?? string.Empty, "utf-8");
        StringAssert.Contains(html, "data-testid=\"local-dev-route-preview\"");
        StringAssert.Contains(html, "data-testid=\"canonical-surface-model\"");
        StringAssert.Contains(html, "data-read-model-mode=\"FixtureSafeReadModel\"");
        StringAssert.Contains(html, "data-testid=\"product-ledger-approval-preview\"");
        StringAssert.Contains(html, "data-route-path=\"/internal/product-ledger/operator-surface\"");
        StringAssert.Contains(html, "read-only");
        StringAssert.Contains(html, "preview-only");
        StringAssert.Contains(html, "no product command execution");
        StringAssert.Contains(html, "no write/export");
        StringAssert.Contains(html, "no release/commercial");
        Assert.IsFalse(html.Contains("data-executable=\"true\"", StringComparison.OrdinalIgnoreCase));
        Assert.IsFalse(html.Contains("<form", StringComparison.OrdinalIgnoreCase));
        Assert.IsFalse(html.Contains("<script", StringComparison.OrdinalIgnoreCase));
        Assert.IsFalse(html.Contains("onclick=", StringComparison.OrdinalIgnoreCase));
        Assert.IsFalse(html.Contains("formaction=", StringComparison.OrdinalIgnoreCase));
    }

    [TestMethod]
    public async Task ProductLedgerApprovalDecisionRoute_DevelopmentHostPersistsLocalOnlyDecisionAndSurfaceShowsState()
    {
        using var approvalState = ApprovalStateFixture.Create();
        await using var app = BuildLocalOnlyApp(
            Environments.Development,
            ProductLedgerOperatorSurfaceReadModelSource.FixtureSafe,
            approvalState.Store);
        await app.StartAsync(TestContext.CancellationTokenSource.Token);

        using var client = new HttpClient { BaseAddress = new Uri(ServerAddress(app)) };
        using var post = await client.PostAsync(
            ProductLedgerLocalDevRouteEndpointMapper.LocalApprovalDecisionRoute,
            JsonContent(ReadyApprovalDecisionBody("Approve")),
            TestContext.CancellationTokenSource.Token);
        var postJson = await post.Content.ReadAsStringAsync(TestContext.CancellationTokenSource.Token);
        using var stateResponse = await client.GetAsync(
            ProductLedgerLocalDevRouteEndpointMapper.LocalApprovalDecisionStateRoute,
            TestContext.CancellationTokenSource.Token);
        var stateJson = await stateResponse.Content.ReadAsStringAsync(TestContext.CancellationTokenSource.Token);
        using var surface = await client.GetAsync(
            ProductLedgerLocalDevRoutePreview.RouteTemplatePreview,
            TestContext.CancellationTokenSource.Token);
        var html = await surface.Content.ReadAsStringAsync(TestContext.CancellationTokenSource.Token);

        Assert.AreEqual(System.Net.HttpStatusCode.OK, post.StatusCode);
        Assert.AreEqual(System.Net.HttpStatusCode.OK, stateResponse.StatusCode);
        Assert.AreEqual(System.Net.HttpStatusCode.OK, surface.StatusCode);
        StringAssert.Contains(postJson, "persistedLocalOnly");
        StringAssert.Contains(stateJson, "approvedLocalOnly");
        StringAssert.Contains(html, "data-testid=\"product-ledger-approval-decision-state\"");
        StringAssert.Contains(html, "data-state=\"ApprovedLocalOnly\"");
        StringAssert.Contains(html, "data-product-command-executed=\"false\"");
        StringAssert.Contains(html, "data-public-ui-action=\"false\"");
        StringAssert.Contains(html, "data-product-command-handler=\"false\"");
        StringAssert.Contains(html, "data-file-write-outside-state-store=\"false\"");
        StringAssert.Contains(html, "no product command execution no public UI action no product command handler no write/export no release/commercial");
        Assert.IsTrue(File.Exists(approvalState.StateFilePath));
        Assert.IsFalse(html.Contains("data-executable=\"true\"", StringComparison.OrdinalIgnoreCase));
        Assert.IsFalse(html.Contains("<form", StringComparison.OrdinalIgnoreCase));
        Assert.IsFalse(html.Contains("<script", StringComparison.OrdinalIgnoreCase));
        Assert.IsFalse(html.Contains("onclick=", StringComparison.OrdinalIgnoreCase));
        Assert.IsFalse(html.Contains("formaction=", StringComparison.OrdinalIgnoreCase));
    }

    [TestMethod]
    public async Task ProductLedgerApprovalDecisionRoute_ReplaysSameDecisionAndRejectsConflictingDecision()
    {
        using var approvalState = ApprovalStateFixture.Create();
        await using var app = BuildLocalOnlyApp(
            Environments.Development,
            ProductLedgerOperatorSurfaceReadModelSource.FixtureSafe,
            approvalState.Store);
        await app.StartAsync(TestContext.CancellationTokenSource.Token);

        using var client = new HttpClient { BaseAddress = new Uri(ServerAddress(app)) };
        using var first = await client.PostAsync(
            ProductLedgerLocalDevRouteEndpointMapper.LocalApprovalDecisionRoute,
            JsonContent(ReadyApprovalDecisionBody("Approve")),
            TestContext.CancellationTokenSource.Token);
        using var replay = await client.PostAsync(
            ProductLedgerLocalDevRouteEndpointMapper.LocalApprovalDecisionRoute,
            JsonContent(ReadyApprovalDecisionBody("Approve")),
            TestContext.CancellationTokenSource.Token);
        using var conflict = await client.PostAsync(
            ProductLedgerLocalDevRouteEndpointMapper.LocalApprovalDecisionRoute,
            JsonContent(ReadyApprovalDecisionBody("Reject")),
            TestContext.CancellationTokenSource.Token);
        var replayJson = await replay.Content.ReadAsStringAsync(TestContext.CancellationTokenSource.Token);
        var conflictJson = await conflict.Content.ReadAsStringAsync(TestContext.CancellationTokenSource.Token);

        Assert.AreEqual(System.Net.HttpStatusCode.OK, first.StatusCode);
        Assert.AreEqual(System.Net.HttpStatusCode.OK, replay.StatusCode);
        Assert.AreEqual(System.Net.HttpStatusCode.BadRequest, conflict.StatusCode);
        StringAssert.Contains(replayJson, "idempotentReplay");
        StringAssert.Contains(conflictJson, "existingDecisionConflict");
    }

    [TestMethod]
    public async Task ProductLedgerApprovalExecutionRoute_DevelopmentHostExecutesApprovedNoOpAndSurfaceShowsState()
    {
        using var approvalState = ApprovalStateFixture.Create();
        using var executionState = ApprovalExecutionFixture.Create();
        await using var app = BuildLocalOnlyApp(
            Environments.Development,
            ProductLedgerOperatorSurfaceReadModelSource.FixtureSafe,
            approvalState.Store,
            executionState.Executor);
        await app.StartAsync(TestContext.CancellationTokenSource.Token);

        using var client = new HttpClient { BaseAddress = new Uri(ServerAddress(app)) };
        using var approval = await client.PostAsync(
            ProductLedgerLocalDevRouteEndpointMapper.LocalApprovalDecisionRoute,
            JsonContent(ReadyApprovalDecisionBody("Approve")),
            TestContext.CancellationTokenSource.Token);
        using var execution = await client.PostAsync(
            ProductLedgerLocalDevRouteEndpointMapper.LocalApprovalExecutionRoute,
            JsonContent(ReadyApprovalExecutionBody()),
            TestContext.CancellationTokenSource.Token);
        var executionJson = await execution.Content.ReadAsStringAsync(TestContext.CancellationTokenSource.Token);
        using var stateResponse = await client.GetAsync(
            ProductLedgerLocalDevRouteEndpointMapper.LocalApprovalExecutionStateRoute,
            TestContext.CancellationTokenSource.Token);
        var stateJson = await stateResponse.Content.ReadAsStringAsync(TestContext.CancellationTokenSource.Token);
        using var surface = await client.GetAsync(
            ProductLedgerLocalDevRoutePreview.RouteTemplatePreview,
            TestContext.CancellationTokenSource.Token);
        var html = await surface.Content.ReadAsStringAsync(TestContext.CancellationTokenSource.Token);

        Assert.AreEqual(System.Net.HttpStatusCode.OK, approval.StatusCode);
        Assert.AreEqual(System.Net.HttpStatusCode.OK, execution.StatusCode);
        Assert.AreEqual(System.Net.HttpStatusCode.OK, stateResponse.StatusCode);
        StringAssert.Contains(executionJson, "noOpExecutionCompletedLocalOnly");
        StringAssert.Contains(stateJson, "noOpExecutionCompletedLocalOnly");
        StringAssert.Contains(html, "data-testid=\"product-ledger-approved-action-execution-state\"");
        StringAssert.Contains(html, "data-state=\"NoOpExecutionCompletedLocalOnly\"");
        StringAssert.Contains(html, "data-no-op-only=\"true\"");
        StringAssert.Contains(html, "data-bounded-action-executed=\"false\"");
        StringAssert.Contains(html, "data-product-command-executed=\"false\"");
        StringAssert.Contains(html, "data-public-ui-action=\"false\"");
        StringAssert.Contains(html, "data-product-command-handler=\"false\"");
        StringAssert.Contains(html, "data-productive-service-registration=\"false\"");
        StringAssert.Contains(html, "data-file-write-outside-execution-store=\"false\"");
        StringAssert.Contains(html, "data-provider-cloud-network=\"false\"");
        StringAssert.Contains(html, "data-db-migration=\"false\"");
        StringAssert.Contains(html, "data-live-automation=\"false\"");
        StringAssert.Contains(html, "data-pilot-run=\"false\"");
        StringAssert.Contains(html, "no shell no subprocess no Pilot run no product command execution no public UI");
        Assert.IsTrue(File.Exists(executionState.StateFilePath));
        Assert.IsFalse(html.Contains("data-executable=\"true\"", StringComparison.OrdinalIgnoreCase));
        Assert.IsFalse(html.Contains("<form", StringComparison.OrdinalIgnoreCase));
        Assert.IsFalse(html.Contains("<script", StringComparison.OrdinalIgnoreCase));
        Assert.IsFalse(html.Contains("onclick=", StringComparison.OrdinalIgnoreCase));
        Assert.IsFalse(html.Contains("formaction=", StringComparison.OrdinalIgnoreCase));
    }

    [TestMethod]
    public async Task ProductLedgerApprovalExecutionRoute_ReplaysSameNoOpAndRejectsConflictingExecution()
    {
        using var approvalState = ApprovalStateFixture.Create();
        using var executionState = ApprovalExecutionFixture.Create();
        await using var app = BuildLocalOnlyApp(
            Environments.Development,
            ProductLedgerOperatorSurfaceReadModelSource.FixtureSafe,
            approvalState.Store,
            executionState.Executor);
        await app.StartAsync(TestContext.CancellationTokenSource.Token);

        using var client = new HttpClient { BaseAddress = new Uri(ServerAddress(app)) };
        using var approval = await client.PostAsync(
            ProductLedgerLocalDevRouteEndpointMapper.LocalApprovalDecisionRoute,
            JsonContent(ReadyApprovalDecisionBody("Approve")),
            TestContext.CancellationTokenSource.Token);
        using var first = await client.PostAsync(
            ProductLedgerLocalDevRouteEndpointMapper.LocalApprovalExecutionRoute,
            JsonContent(ReadyApprovalExecutionBody()),
            TestContext.CancellationTokenSource.Token);
        using var replay = await client.PostAsync(
            ProductLedgerLocalDevRouteEndpointMapper.LocalApprovalExecutionRoute,
            JsonContent(ReadyApprovalExecutionBody()),
            TestContext.CancellationTokenSource.Token);
        using var conflict = await client.PostAsync(
            ProductLedgerLocalDevRouteEndpointMapper.LocalApprovalExecutionRoute,
            JsonContent(ReadyApprovalExecutionBody() with { ExecutionId = "approval-route-no-op-execution-002" }),
            TestContext.CancellationTokenSource.Token);
        var replayJson = await replay.Content.ReadAsStringAsync(TestContext.CancellationTokenSource.Token);
        var conflictJson = await conflict.Content.ReadAsStringAsync(TestContext.CancellationTokenSource.Token);

        Assert.AreEqual(System.Net.HttpStatusCode.OK, approval.StatusCode);
        Assert.AreEqual(System.Net.HttpStatusCode.OK, first.StatusCode);
        Assert.AreEqual(System.Net.HttpStatusCode.OK, replay.StatusCode);
        Assert.AreEqual(System.Net.HttpStatusCode.BadRequest, conflict.StatusCode);
        StringAssert.Contains(replayJson, "idempotentReplay");
        StringAssert.Contains(conflictJson, "existingExecutionConflict");
    }

    [TestMethod]
    public async Task ProductLedgerApprovalExecutionRoute_FailsClosedForMissingApprovalMalformedUnsafeOrMismatchedInput()
    {
        var cases = new (bool PersistApproval, StringContent Content, bool ExpectStateFile)[]
        {
            (false, JsonContent(ReadyApprovalExecutionBody()), false),
            (true, new StringContent("{not-json", Encoding.UTF8, "application/json"), false),
            (true, JsonContent(ReadyApprovalExecutionBody() with { CurrentEvidenceHash = new string('b', 64) }), false),
            (true, JsonContent(ReadyApprovalExecutionBody() with { RequestsBoundedAction = true }), false),
            (true, JsonContent(ReadyApprovalExecutionBody() with { RequestsProductCommandExecution = true }), false),
            (true, JsonContent(ReadyApprovalExecutionBody() with { RequestsPublicUiAction = true }), false)
        };

        foreach (var testCase in cases)
        {
            using var approvalState = ApprovalStateFixture.Create();
            using var executionState = ApprovalExecutionFixture.Create();
            await using var app = BuildLocalOnlyApp(
                Environments.Development,
                ProductLedgerOperatorSurfaceReadModelSource.FixtureSafe,
                approvalState.Store,
                executionState.Executor);
            await app.StartAsync(TestContext.CancellationTokenSource.Token);

            using var client = new HttpClient { BaseAddress = new Uri(ServerAddress(app)) };
            if (testCase.PersistApproval)
            {
                using var approval = await client.PostAsync(
                    ProductLedgerLocalDevRouteEndpointMapper.LocalApprovalDecisionRoute,
                    JsonContent(ReadyApprovalDecisionBody("Approve")),
                    TestContext.CancellationTokenSource.Token);
                Assert.AreEqual(System.Net.HttpStatusCode.OK, approval.StatusCode);
            }

            using var execution = await client.PostAsync(
                ProductLedgerLocalDevRouteEndpointMapper.LocalApprovalExecutionRoute,
                testCase.Content,
                TestContext.CancellationTokenSource.Token);

            Assert.AreEqual(System.Net.HttpStatusCode.BadRequest, execution.StatusCode);
            Assert.AreEqual(testCase.ExpectStateFile, File.Exists(executionState.StateFilePath));
        }
    }

    [TestMethod]
    public async Task ProductLedgerBoundedApprovalExecutionRoute_DevelopmentHostRecordsCompletionMarkerAndSurfaceShowsState()
    {
        using var approvalState = ApprovalStateFixture.Create();
        using var executionState = ApprovalExecutionFixture.Create();
        using var boundedState = BoundedApprovalExecutionFixture.Create();
        await using var app = BuildLocalOnlyApp(
            Environments.Development,
            ProductLedgerOperatorSurfaceReadModelSource.FixtureSafe,
            approvalState.Store,
            executionState.Executor,
            boundedState.Executor);
        await app.StartAsync(TestContext.CancellationTokenSource.Token);

        using var client = new HttpClient { BaseAddress = new Uri(ServerAddress(app)) };
        using var approval = await client.PostAsync(
            ProductLedgerLocalDevRouteEndpointMapper.LocalApprovalDecisionRoute,
            JsonContent(ReadyApprovalDecisionBody("Approve")),
            TestContext.CancellationTokenSource.Token);
        using var noOp = await client.PostAsync(
            ProductLedgerLocalDevRouteEndpointMapper.LocalApprovalExecutionRoute,
            JsonContent(ReadyApprovalExecutionBody()),
            TestContext.CancellationTokenSource.Token);
        using var bounded = await client.PostAsync(
            ProductLedgerLocalDevRouteEndpointMapper.LocalBoundedApprovalExecutionRoute,
            JsonContent(ReadyBoundedApprovalExecutionBody()),
            TestContext.CancellationTokenSource.Token);
        var boundedJson = await bounded.Content.ReadAsStringAsync(TestContext.CancellationTokenSource.Token);
        using var stateResponse = await client.GetAsync(
            ProductLedgerLocalDevRouteEndpointMapper.LocalBoundedApprovalExecutionStateRoute,
            TestContext.CancellationTokenSource.Token);
        var stateJson = await stateResponse.Content.ReadAsStringAsync(TestContext.CancellationTokenSource.Token);
        using var surface = await client.GetAsync(
            ProductLedgerLocalDevRoutePreview.RouteTemplatePreview,
            TestContext.CancellationTokenSource.Token);
        var html = await surface.Content.ReadAsStringAsync(TestContext.CancellationTokenSource.Token);

        Assert.AreEqual(System.Net.HttpStatusCode.OK, approval.StatusCode);
        Assert.AreEqual(System.Net.HttpStatusCode.OK, noOp.StatusCode);
        Assert.AreEqual(System.Net.HttpStatusCode.OK, bounded.StatusCode);
        Assert.AreEqual(System.Net.HttpStatusCode.OK, stateResponse.StatusCode);
        StringAssert.Contains(boundedJson, "boundedLocalCompletionRecorded");
        StringAssert.Contains(stateJson, "boundedExecutionCompletedLocalOnly");
        StringAssert.Contains(html, "data-testid=\"product-ledger-bounded-approved-action-state\"");
        StringAssert.Contains(html, "data-state=\"BoundedExecutionCompletedLocalOnly\"");
        StringAssert.Contains(html, "data-non-destructive=\"true\"");
        StringAssert.Contains(html, "data-completion-marker=\"true\"");
        StringAssert.Contains(html, "data-touches-user-files=\"false\"");
        StringAssert.Contains(html, "data-shell-subprocess-allowed=\"false\"");
        StringAssert.Contains(html, "data-command-execution-allowed=\"false\"");
        StringAssert.Contains(html, "data-product-command-executed=\"false\"");
        StringAssert.Contains(html, "data-public-ui-action=\"false\"");
        StringAssert.Contains(html, "data-file-write-outside-execution-store=\"false\"");
        StringAssert.Contains(html, "data-provider-cloud-network=\"false\"");
        StringAssert.Contains(html, "data-db-migration=\"false\"");
        StringAssert.Contains(html, "data-live-automation=\"false\"");
        StringAssert.Contains(html, "data-pilot-run=\"false\"");
        StringAssert.Contains(html, "BoundedInternalCompletionMarker");
        StringAssert.Contains(html, "no user file write no shell no subprocess no command execution no Pilot run");
        Assert.IsTrue(File.Exists(boundedState.StateFilePath));
        Assert.IsFalse(html.Contains("data-executable=\"true\"", StringComparison.OrdinalIgnoreCase));
        Assert.IsFalse(html.Contains("<form", StringComparison.OrdinalIgnoreCase));
        Assert.IsFalse(html.Contains("<script", StringComparison.OrdinalIgnoreCase));
        Assert.IsFalse(html.Contains("onclick=", StringComparison.OrdinalIgnoreCase));
        Assert.IsFalse(html.Contains("formaction=", StringComparison.OrdinalIgnoreCase));
    }

    [TestMethod]
    public async Task ProductLedgerBoundedApprovalExecutionRoute_ReplaysSameMarkerAndRejectsConflict()
    {
        using var approvalState = ApprovalStateFixture.Create();
        using var executionState = ApprovalExecutionFixture.Create();
        using var boundedState = BoundedApprovalExecutionFixture.Create();
        await using var app = BuildLocalOnlyApp(
            Environments.Development,
            ProductLedgerOperatorSurfaceReadModelSource.FixtureSafe,
            approvalState.Store,
            executionState.Executor,
            boundedState.Executor);
        await app.StartAsync(TestContext.CancellationTokenSource.Token);

        using var client = new HttpClient { BaseAddress = new Uri(ServerAddress(app)) };
        using var approval = await client.PostAsync(
            ProductLedgerLocalDevRouteEndpointMapper.LocalApprovalDecisionRoute,
            JsonContent(ReadyApprovalDecisionBody("Approve")),
            TestContext.CancellationTokenSource.Token);
        using var noOp = await client.PostAsync(
            ProductLedgerLocalDevRouteEndpointMapper.LocalApprovalExecutionRoute,
            JsonContent(ReadyApprovalExecutionBody()),
            TestContext.CancellationTokenSource.Token);
        using var first = await client.PostAsync(
            ProductLedgerLocalDevRouteEndpointMapper.LocalBoundedApprovalExecutionRoute,
            JsonContent(ReadyBoundedApprovalExecutionBody()),
            TestContext.CancellationTokenSource.Token);
        using var replay = await client.PostAsync(
            ProductLedgerLocalDevRouteEndpointMapper.LocalBoundedApprovalExecutionRoute,
            JsonContent(ReadyBoundedApprovalExecutionBody()),
            TestContext.CancellationTokenSource.Token);
        using var conflict = await client.PostAsync(
            ProductLedgerLocalDevRouteEndpointMapper.LocalBoundedApprovalExecutionRoute,
            JsonContent(ReadyBoundedApprovalExecutionBody() with { ExecutionId = "bounded-route-execution-002" }),
            TestContext.CancellationTokenSource.Token);
        var replayJson = await replay.Content.ReadAsStringAsync(TestContext.CancellationTokenSource.Token);
        var conflictJson = await conflict.Content.ReadAsStringAsync(TestContext.CancellationTokenSource.Token);

        Assert.AreEqual(System.Net.HttpStatusCode.OK, approval.StatusCode);
        Assert.AreEqual(System.Net.HttpStatusCode.OK, noOp.StatusCode);
        Assert.AreEqual(System.Net.HttpStatusCode.OK, first.StatusCode);
        Assert.AreEqual(System.Net.HttpStatusCode.OK, replay.StatusCode);
        Assert.AreEqual(System.Net.HttpStatusCode.BadRequest, conflict.StatusCode);
        StringAssert.Contains(replayJson, "idempotentReplay");
        StringAssert.Contains(conflictJson, "existingBoundedActionConflict");
    }

    [TestMethod]
    public async Task ProductLedgerApprovedHandoffReportDraftRoute_DevelopmentHostCreatesDraftAndSurfaceShowsState()
    {
        using var approvalState = ApprovalStateFixture.Create();
        using var executionState = ApprovalExecutionFixture.Create();
        using var boundedState = BoundedApprovalExecutionFixture.Create();
        using var handoffDraftState = HandoffReportDraftFixture.Create();
        await using var app = BuildLocalOnlyApp(
            Environments.Development,
            ProductLedgerOperatorSurfaceReadModelSource.FixtureSafe,
            approvalState.Store,
            executionState.Executor,
            boundedState.Executor,
            handoffDraftState.Executor);
        await app.StartAsync(TestContext.CancellationTokenSource.Token);

        using var client = new HttpClient { BaseAddress = new Uri(ServerAddress(app)) };
        using var approval = await client.PostAsync(
            ProductLedgerLocalDevRouteEndpointMapper.LocalApprovalDecisionRoute,
            JsonContent(ReadyApprovalDecisionBody("Approve")),
            TestContext.CancellationTokenSource.Token);
        using var noOp = await client.PostAsync(
            ProductLedgerLocalDevRouteEndpointMapper.LocalApprovalExecutionRoute,
            JsonContent(ReadyApprovalExecutionBody()),
            TestContext.CancellationTokenSource.Token);
        using var bounded = await client.PostAsync(
            ProductLedgerLocalDevRouteEndpointMapper.LocalBoundedApprovalExecutionRoute,
            JsonContent(ReadyBoundedApprovalExecutionBody()),
            TestContext.CancellationTokenSource.Token);
        using var draft = await client.PostAsync(
            ProductLedgerLocalDevRouteEndpointMapper.LocalApprovedHandoffReportDraftRoute,
            JsonContent(ReadyHandoffReportDraftBody()),
            TestContext.CancellationTokenSource.Token);
        var draftJson = await draft.Content.ReadAsStringAsync(TestContext.CancellationTokenSource.Token);
        using var draftState = await client.GetAsync(
            ProductLedgerLocalDevRouteEndpointMapper.LocalApprovedHandoffReportDraftStateRoute,
            TestContext.CancellationTokenSource.Token);
        var draftStateJson = await draftState.Content.ReadAsStringAsync(TestContext.CancellationTokenSource.Token);
        using var surface = await client.GetAsync(
            ProductLedgerLocalDevRoutePreview.RouteTemplatePreview,
            TestContext.CancellationTokenSource.Token);
        var html = await surface.Content.ReadAsStringAsync(TestContext.CancellationTokenSource.Token);

        Assert.AreEqual(System.Net.HttpStatusCode.OK, approval.StatusCode);
        Assert.AreEqual(System.Net.HttpStatusCode.OK, noOp.StatusCode);
        Assert.AreEqual(System.Net.HttpStatusCode.OK, bounded.StatusCode);
        Assert.AreEqual(System.Net.HttpStatusCode.OK, draft.StatusCode);
        Assert.AreEqual(System.Net.HttpStatusCode.OK, draftState.StatusCode);
        StringAssert.Contains(draftJson, "draftCreatedLocalOnly");
        StringAssert.Contains(draftJson, ProductLedgerLocalApprovedHandoffReportDraftExecutor.AllowedRelativeOutputBoundary);
        StringAssert.Contains(draftStateJson, "draftCreatedLocalOnly");
        Assert.IsTrue(File.Exists(handoffDraftState.ExpectedReadyPath));
        var draftContent = File.ReadAllText(handoffDraftState.ExpectedReadyPath);
        StringAssert.Contains(draftContent, "redacted local handoff summary");
        StringAssert.Contains(draftContent, "No shell/subprocess.");
        Assert.IsFalse(draftContent.Contains("password=", StringComparison.OrdinalIgnoreCase));
        StringAssert.Contains(html, "data-testid=\"product-ledger-approved-handoff-report-draft-state\"");
        StringAssert.Contains(html, "data-state=\"DraftCreatedLocalOnly\"");
        StringAssert.Contains(html, "data-create-only=\"true\"");
        StringAssert.Contains(html, "data-overwrite-allowed=\"false\"");
        StringAssert.Contains(html, "data-user-file-write=\"false\"");
        StringAssert.Contains(html, "data-shell-allowed=\"false\"");
        StringAssert.Contains(html, "data-network-allowed=\"false\"");
        StringAssert.Contains(html, "data-production-allowed=\"false\"");
        StringAssert.Contains(html, "data-public-product-allowed=\"false\"");
        StringAssert.Contains(html, "data-redaction-applied=\"true\"");
        StringAssert.Contains(html, ProductLedgerLocalApprovedHandoffReportDraftExecutor.AllowedRelativeOutputBoundary);
        StringAssert.Contains(html, "no user workspace write no shell no subprocess no command execution no Pilot run");
        Assert.IsFalse(html.Contains("data-executable=\"true\"", StringComparison.OrdinalIgnoreCase));
        Assert.IsFalse(html.Contains("<form", StringComparison.OrdinalIgnoreCase));
        Assert.IsFalse(html.Contains("<script", StringComparison.OrdinalIgnoreCase));
        Assert.IsFalse(html.Contains("onclick=", StringComparison.OrdinalIgnoreCase));
        Assert.IsFalse(html.Contains("formaction=", StringComparison.OrdinalIgnoreCase));
    }

    [TestMethod]
    public async Task ProductLedgerWorkspaceTestJailHandoffDraftRoute_DevelopmentHostCreatesDraftAndSurfaceShowsState()
    {
        using var approvalState = ApprovalStateFixture.Create();
        using var executionState = ApprovalExecutionFixture.Create();
        using var boundedState = BoundedApprovalExecutionFixture.Create();
        using var handoffDraftState = HandoffReportDraftFixture.Create();
        using var workspaceDraftState = WorkspaceTestJailHandoffDraftFixture.Create();
        await using var app = BuildLocalOnlyApp(
            Environments.Development,
            ProductLedgerOperatorSurfaceReadModelSource.FixtureSafe,
            approvalState.Store,
            executionState.Executor,
            boundedState.Executor,
            handoffDraftState.Executor,
            workspaceDraftState.Executor);
        await app.StartAsync(TestContext.CancellationTokenSource.Token);

        using var client = new HttpClient { BaseAddress = new Uri(ServerAddress(app)) };
        using var approval = await client.PostAsync(
            ProductLedgerLocalDevRouteEndpointMapper.LocalApprovalDecisionRoute,
            JsonContent(ReadyApprovalDecisionBody("Approve")),
            TestContext.CancellationTokenSource.Token);
        using var noOp = await client.PostAsync(
            ProductLedgerLocalDevRouteEndpointMapper.LocalApprovalExecutionRoute,
            JsonContent(ReadyApprovalExecutionBody()),
            TestContext.CancellationTokenSource.Token);
        using var bounded = await client.PostAsync(
            ProductLedgerLocalDevRouteEndpointMapper.LocalBoundedApprovalExecutionRoute,
            JsonContent(ReadyBoundedApprovalExecutionBody()),
            TestContext.CancellationTokenSource.Token);
        using var predecessor = await client.PostAsync(
            ProductLedgerLocalDevRouteEndpointMapper.LocalApprovedHandoffReportDraftRoute,
            JsonContent(ReadyHandoffReportDraftBody()),
            TestContext.CancellationTokenSource.Token);
        using var workspaceDraft = await client.PostAsync(
            ProductLedgerLocalDevRouteEndpointMapper.LocalWorkspaceTestJailHandoffDraftRoute,
            JsonContent(ReadyWorkspaceTestJailHandoffDraftBody(handoffDraftState.Executor.Read().ContentHash)),
            TestContext.CancellationTokenSource.Token);
        var workspaceDraftJson = await workspaceDraft.Content.ReadAsStringAsync(TestContext.CancellationTokenSource.Token);
        using var workspaceDraftStateResponse = await client.GetAsync(
            ProductLedgerLocalDevRouteEndpointMapper.LocalWorkspaceTestJailHandoffDraftStateRoute,
            TestContext.CancellationTokenSource.Token);
        var workspaceDraftStateJson = await workspaceDraftStateResponse.Content.ReadAsStringAsync(TestContext.CancellationTokenSource.Token);
        using var surface = await client.GetAsync(
            ProductLedgerLocalDevRoutePreview.RouteTemplatePreview,
            TestContext.CancellationTokenSource.Token);
        var html = await surface.Content.ReadAsStringAsync(TestContext.CancellationTokenSource.Token);

        Assert.AreEqual(System.Net.HttpStatusCode.OK, approval.StatusCode);
        Assert.AreEqual(System.Net.HttpStatusCode.OK, noOp.StatusCode);
        Assert.AreEqual(System.Net.HttpStatusCode.OK, bounded.StatusCode);
        Assert.AreEqual(System.Net.HttpStatusCode.OK, predecessor.StatusCode);
        Assert.AreEqual(System.Net.HttpStatusCode.OK, workspaceDraft.StatusCode);
        Assert.AreEqual(System.Net.HttpStatusCode.OK, workspaceDraftStateResponse.StatusCode);
        StringAssert.Contains(workspaceDraftJson, "draftCreatedWorkspaceTestJailOnly");
        StringAssert.Contains(workspaceDraftJson, ProductLedgerLocalWorkspaceTestJailHandoffDraftExecutor.AllowedRelativeOutputBoundary);
        StringAssert.Contains(workspaceDraftStateJson, "draftCreatedWorkspaceTestJailOnly");
        Assert.IsTrue(File.Exists(workspaceDraftState.ExpectedReadyPath));
        Assert.IsTrue(Path.GetFullPath(workspaceDraftState.ExpectedReadyPath).StartsWith(Path.GetFullPath(workspaceDraftState.JailRoot), StringComparison.OrdinalIgnoreCase));
        var draftContent = File.ReadAllText(workspaceDraftState.ExpectedReadyPath);
        StringAssert.Contains(draftContent, "redacted workspace test-jail handoff summary");
        StringAssert.Contains(draftContent, "Workspace test-jail only.");
        StringAssert.Contains(draftContent, "No user-selected path.");
        Assert.IsFalse(draftContent.Contains("password=", StringComparison.OrdinalIgnoreCase));
        Assert.IsFalse(draftContent.Contains(@"C:\Users\", StringComparison.OrdinalIgnoreCase));
        StringAssert.Contains(html, "data-testid=\"product-ledger-workspace-test-jail-handoff-draft-state\"");
        StringAssert.Contains(html, "data-state=\"DraftCreatedWorkspaceTestJailOnly\"");
        StringAssert.Contains(html, "data-workspace-test-jail-only=\"true\"");
        StringAssert.Contains(html, "data-create-only=\"true\"");
        StringAssert.Contains(html, "data-overwrite-allowed=\"false\"");
        StringAssert.Contains(html, "data-user-selected-path-allowed=\"false\"");
        StringAssert.Contains(html, "data-payload-controlled-root-allowed=\"false\"");
        StringAssert.Contains(html, "data-canonicalization-passed=\"true\"");
        StringAssert.Contains(html, "data-reparse-validation-passed=\"true\"");
        StringAssert.Contains(html, ProductLedgerLocalWorkspaceTestJailHandoffDraftExecutor.AllowedRelativeOutputBoundary);
        StringAssert.Contains(html, "no user-selected path no payload-controlled root no shell no subprocess no command execution no Pilot run");
        Assert.IsFalse(html.Contains("data-executable=\"true\"", StringComparison.OrdinalIgnoreCase));
        Assert.IsFalse(html.Contains("<form", StringComparison.OrdinalIgnoreCase));
        Assert.IsFalse(html.Contains("<script", StringComparison.OrdinalIgnoreCase));
        Assert.IsFalse(html.Contains("onclick=", StringComparison.OrdinalIgnoreCase));
        Assert.IsFalse(html.Contains("formaction=", StringComparison.OrdinalIgnoreCase));
    }

    [TestMethod]
    public async Task ProductLedgerUserWorkspaceAllowlistedHandoffDraftRoute_DevelopmentHostCreatesDraftAndSurfaceShowsState()
    {
        using var approvalState = ApprovalStateFixture.Create();
        using var executionState = ApprovalExecutionFixture.Create();
        using var boundedState = BoundedApprovalExecutionFixture.Create();
        using var handoffDraftState = HandoffReportDraftFixture.Create();
        using var workspaceDraftState = WorkspaceTestJailHandoffDraftFixture.Create();
        using var userWorkspaceDraftState = UserWorkspaceAllowlistedHandoffDraftFixture.Create();
        await using var app = BuildLocalOnlyApp(
            Environments.Development,
            ProductLedgerOperatorSurfaceReadModelSource.FixtureSafe,
            approvalState.Store,
            executionState.Executor,
            boundedState.Executor,
            handoffDraftState.Executor,
            workspaceDraftState.Executor,
            userWorkspaceDraftState.Executor);
        await app.StartAsync(TestContext.CancellationTokenSource.Token);

        using var client = new HttpClient { BaseAddress = new Uri(ServerAddress(app)) };
        using var approval = await client.PostAsync(
            ProductLedgerLocalDevRouteEndpointMapper.LocalApprovalDecisionRoute,
            JsonContent(ReadyApprovalDecisionBody("Approve")),
            TestContext.CancellationTokenSource.Token);
        using var noOp = await client.PostAsync(
            ProductLedgerLocalDevRouteEndpointMapper.LocalApprovalExecutionRoute,
            JsonContent(ReadyApprovalExecutionBody()),
            TestContext.CancellationTokenSource.Token);
        using var bounded = await client.PostAsync(
            ProductLedgerLocalDevRouteEndpointMapper.LocalBoundedApprovalExecutionRoute,
            JsonContent(ReadyBoundedApprovalExecutionBody()),
            TestContext.CancellationTokenSource.Token);
        using var localDraft = await client.PostAsync(
            ProductLedgerLocalDevRouteEndpointMapper.LocalApprovedHandoffReportDraftRoute,
            JsonContent(ReadyHandoffReportDraftBody()),
            TestContext.CancellationTokenSource.Token);
        using var workspaceDraft = await client.PostAsync(
            ProductLedgerLocalDevRouteEndpointMapper.LocalWorkspaceTestJailHandoffDraftRoute,
            JsonContent(ReadyWorkspaceTestJailHandoffDraftBody(handoffDraftState.Executor.Read().ContentHash)),
            TestContext.CancellationTokenSource.Token);
        using var userWorkspaceDraft = await client.PostAsync(
            ProductLedgerLocalDevRouteEndpointMapper.LocalUserWorkspaceAllowlistedHandoffDraftRoute,
            JsonContent(ReadyUserWorkspaceAllowlistedHandoffDraftBody(
                handoffDraftState.Executor.Read().ContentHash,
                workspaceDraftState.Executor.Read().ContentHash)),
            TestContext.CancellationTokenSource.Token);
        var userWorkspaceDraftJson = await userWorkspaceDraft.Content.ReadAsStringAsync(TestContext.CancellationTokenSource.Token);
        using var userWorkspaceDraftStateResponse = await client.GetAsync(
            ProductLedgerLocalDevRouteEndpointMapper.LocalUserWorkspaceAllowlistedHandoffDraftStateRoute,
            TestContext.CancellationTokenSource.Token);
        var userWorkspaceDraftStateJson = await userWorkspaceDraftStateResponse.Content.ReadAsStringAsync(TestContext.CancellationTokenSource.Token);
        using var surface = await client.GetAsync(
            ProductLedgerLocalDevRoutePreview.RouteTemplatePreview,
            TestContext.CancellationTokenSource.Token);
        var html = await surface.Content.ReadAsStringAsync(TestContext.CancellationTokenSource.Token);

        Assert.AreEqual(System.Net.HttpStatusCode.OK, approval.StatusCode);
        Assert.AreEqual(System.Net.HttpStatusCode.OK, noOp.StatusCode);
        Assert.AreEqual(System.Net.HttpStatusCode.OK, bounded.StatusCode);
        Assert.AreEqual(System.Net.HttpStatusCode.OK, localDraft.StatusCode);
        Assert.AreEqual(System.Net.HttpStatusCode.OK, workspaceDraft.StatusCode);
        Assert.AreEqual(System.Net.HttpStatusCode.OK, userWorkspaceDraft.StatusCode);
        Assert.AreEqual(System.Net.HttpStatusCode.OK, userWorkspaceDraftStateResponse.StatusCode);
        StringAssert.Contains(userWorkspaceDraftJson, "draftCreatedUserWorkspaceAllowlistedOnly");
        StringAssert.Contains(userWorkspaceDraftJson, ProductLedgerLocalUserWorkspaceAllowlistedHandoffDraftExecutor.AllowedRelativeOutputBoundary);
        StringAssert.Contains(userWorkspaceDraftStateJson, "draftCreatedUserWorkspaceAllowlistedOnly");
        Assert.IsTrue(File.Exists(userWorkspaceDraftState.ExpectedReadyPath));
        Assert.IsTrue(Path.GetFullPath(userWorkspaceDraftState.ExpectedReadyPath).StartsWith(Path.GetFullPath(userWorkspaceDraftState.AllowedBoundaryRoot), StringComparison.OrdinalIgnoreCase));
        var draftContent = File.ReadAllText(userWorkspaceDraftState.ExpectedReadyPath);
        StringAssert.Contains(draftContent, "redacted user workspace allowlisted handoff summary");
        StringAssert.Contains(draftContent, "User workspace allowlisted boundary only.");
        StringAssert.Contains(draftContent, "No user-selected path.");
        Assert.IsFalse(draftContent.Contains("password=", StringComparison.OrdinalIgnoreCase));
        Assert.IsFalse(draftContent.Contains(@"C:\Users\", StringComparison.OrdinalIgnoreCase));
        Assert.IsFalse(draftContent.Contains(userWorkspaceDraftState.WorkspaceRoot, StringComparison.OrdinalIgnoreCase));
        StringAssert.Contains(html, "data-testid=\"product-ledger-user-workspace-allowlisted-handoff-draft-state\"");
        StringAssert.Contains(html, "data-state=\"DraftCreatedUserWorkspaceAllowlistedOnly\"");
        StringAssert.Contains(html, "data-user-workspace-allowlisted-boundary-only=\"true\"");
        StringAssert.Contains(html, "data-create-only=\"true\"");
        StringAssert.Contains(html, "data-overwrite-allowed=\"false\"");
        StringAssert.Contains(html, "data-user-selected-path-allowed=\"false\"");
        StringAssert.Contains(html, "data-payload-controlled-root-allowed=\"false\"");
        StringAssert.Contains(html, "data-canonicalization-passed=\"true\"");
        StringAssert.Contains(html, "data-reparse-validation-passed=\"true\"");
        StringAssert.Contains(html, ProductLedgerLocalUserWorkspaceAllowlistedHandoffDraftExecutor.AllowedRelativeOutputBoundary);
        StringAssert.Contains(html, "no user-selected path no payload-controlled root no shell no subprocess no command execution no Pilot run");
        Assert.IsFalse(html.Contains(userWorkspaceDraftState.WorkspaceRoot, StringComparison.OrdinalIgnoreCase));
        Assert.IsFalse(html.Contains("data-executable=\"true\"", StringComparison.OrdinalIgnoreCase));
        Assert.IsFalse(html.Contains("<form", StringComparison.OrdinalIgnoreCase));
        Assert.IsFalse(html.Contains("<script", StringComparison.OrdinalIgnoreCase));
        Assert.IsFalse(html.Contains("onclick=", StringComparison.OrdinalIgnoreCase));
        Assert.IsFalse(html.Contains("formaction=", StringComparison.OrdinalIgnoreCase));
    }

    [TestMethod]
    public async Task ProductLedgerLatestStateSnapshotRoute_DevelopmentHostCreatesJsonSnapshotAndSurfaceShowsState()
    {
        using var approvalState = ApprovalStateFixture.Create();
        using var executionState = ApprovalExecutionFixture.Create();
        using var boundedState = BoundedApprovalExecutionFixture.Create();
        using var handoffDraftState = HandoffReportDraftFixture.Create();
        using var workspaceDraftState = WorkspaceTestJailHandoffDraftFixture.Create();
        using var userWorkspaceDraftState = UserWorkspaceAllowlistedHandoffDraftFixture.Create();
        using var latestStateSnapshotState = LatestStateSnapshotFixture.Create();
        await using var app = BuildLocalOnlyApp(
            Environments.Development,
            ProductLedgerOperatorSurfaceReadModelSource.FixtureSafe,
            approvalState.Store,
            executionState.Executor,
            boundedState.Executor,
            handoffDraftState.Executor,
            workspaceDraftState.Executor,
            userWorkspaceDraftState.Executor,
            latestStateSnapshotState.Executor);
        await app.StartAsync(TestContext.CancellationTokenSource.Token);

        using var client = new HttpClient { BaseAddress = new Uri(ServerAddress(app)) };
        await CreateFullUserWorkspaceAllowlistedChainAsync(
            client,
            handoffDraftState,
            workspaceDraftState,
            TestContext.CancellationTokenSource.Token);
        var surfaceModel = new ProductLedgerLocalDevRoutePreview()
            .Render(
                ProductLedgerLocalDevRoutePreview.CreateDefaultLocalDevRequest(),
                ProductLedgerOperatorSurfaceReadModelSource.FixtureSafe,
                approvalState.Store.Read(),
                executionState.Executor.Read(),
                boundedState.Executor.Read(),
                handoffDraftState.Executor.Read(),
                workspaceDraftState.Executor.Read(),
                userWorkspaceDraftState.Executor.Read(),
                latestStateSnapshotState.Executor.Read())
            .CanonicalSurface;
        var surfaceHash = ProductLedgerLocalOperatorSurfaceLatestStateSnapshotExecutor.ComputeOperatorSurfaceModelHash(surfaceModel);

        using var snapshot = await client.PostAsync(
            ProductLedgerLocalDevRouteEndpointMapper.LocalOperatorSurfaceLatestStateSnapshotRoute,
            JsonContent(ReadyLatestStateSnapshotBody(surfaceHash)),
            TestContext.CancellationTokenSource.Token);
        var snapshotJson = await snapshot.Content.ReadAsStringAsync(TestContext.CancellationTokenSource.Token);
        using var snapshotState = await client.GetAsync(
            ProductLedgerLocalDevRouteEndpointMapper.LocalOperatorSurfaceLatestStateSnapshotStateRoute,
            TestContext.CancellationTokenSource.Token);
        var snapshotStateJson = await snapshotState.Content.ReadAsStringAsync(TestContext.CancellationTokenSource.Token);
        using var surface = await client.GetAsync(
            ProductLedgerLocalDevRoutePreview.RouteTemplatePreview,
            TestContext.CancellationTokenSource.Token);
        var html = await surface.Content.ReadAsStringAsync(TestContext.CancellationTokenSource.Token);

        Assert.AreEqual(System.Net.HttpStatusCode.OK, snapshot.StatusCode);
        Assert.AreEqual(System.Net.HttpStatusCode.OK, snapshotState.StatusCode);
        StringAssert.Contains(snapshotJson, "snapshotCreatedLocalOnly");
        StringAssert.Contains(snapshotJson, ProductLedgerLocalOperatorSurfaceLatestStateSnapshotExecutor.AllowedRelativeOutputBoundary);
        StringAssert.Contains(snapshotJson, "historicalEvidenceOnly");
        StringAssert.Contains(snapshotJson, "latestPointerOverwriteAllowed\":false");
        StringAssert.Contains(snapshotStateJson, "snapshotCreatedLocalOnly");
        Assert.IsTrue(Directory.Exists(latestStateSnapshotState.AllowedBoundaryRoot));
        var files = Directory.GetFiles(latestStateSnapshotState.AllowedBoundaryRoot, "*.json");
        Assert.AreEqual(1, files.Length);
        var content = File.ReadAllText(files[0]);
        StringAssert.Contains(content, "\"classification\": \"LOCAL_INTERNAL_DEV_ONLY_HISTORICAL_SNAPSHOT\"");
        StringAssert.Contains(content, "\"historicalEvidenceOnly\": true");
        StringAssert.Contains(content, "\"authorityLiveProduct\": false");
        StringAssert.Contains(content, "\"releaseCommercial\": false");
        Assert.IsFalse(File.Exists(Path.Combine(latestStateSnapshotState.AllowedBoundaryRoot, "latest.json")));
        Assert.IsFalse(content.Contains("password=", StringComparison.OrdinalIgnoreCase));
        Assert.IsFalse(content.Contains(@"C:\Users\", StringComparison.OrdinalIgnoreCase));
        Assert.IsFalse(content.Contains(latestStateSnapshotState.WorkspaceRoot, StringComparison.OrdinalIgnoreCase));
        StringAssert.Contains(html, "data-testid=\"product-ledger-latest-state-snapshot-state\"");
        StringAssert.Contains(html, "data-state=\"SnapshotCreatedLocalOnly\"");
        StringAssert.Contains(html, "data-historical-evidence-only=\"true\"");
        StringAssert.Contains(html, "data-authority-live-product=\"false\"");
        StringAssert.Contains(html, "data-latest-pointer-overwrite-allowed=\"false\"");
        StringAssert.Contains(html, "json-only immutable versioned create-only no-overwrite no latest pointer overwrite");
        StringAssert.Contains(html, ProductLedgerLocalOperatorSurfaceLatestStateSnapshotExecutor.StaleStateClassification);
        Assert.IsFalse(html.Contains("data-executable=\"true\"", StringComparison.OrdinalIgnoreCase));
        Assert.IsFalse(html.Contains("<form", StringComparison.OrdinalIgnoreCase));
        Assert.IsFalse(html.Contains("<script", StringComparison.OrdinalIgnoreCase));
    }

    [TestMethod]
    public async Task ProductLedgerLatestStateManifestRoute_DevelopmentHostCreatesJsonManifestAndSurfaceShowsState()
    {
        using var approvalState = ApprovalStateFixture.Create();
        using var executionState = ApprovalExecutionFixture.Create();
        using var boundedState = BoundedApprovalExecutionFixture.Create();
        using var handoffDraftState = HandoffReportDraftFixture.Create();
        using var workspaceDraftState = WorkspaceTestJailHandoffDraftFixture.Create();
        using var userWorkspaceDraftState = UserWorkspaceAllowlistedHandoffDraftFixture.Create();
        using var latestStateSnapshotState = LatestStateSnapshotFixture.Create();
        using var latestStateManifestState = LatestStateManifestFixture.Create();
        await using var app = BuildLocalOnlyApp(
            Environments.Development,
            ProductLedgerOperatorSurfaceReadModelSource.FixtureSafe,
            approvalState.Store,
            executionState.Executor,
            boundedState.Executor,
            handoffDraftState.Executor,
            workspaceDraftState.Executor,
            userWorkspaceDraftState.Executor,
            latestStateSnapshotState.Executor,
            latestStateManifestState.Writer);
        await app.StartAsync(TestContext.CancellationTokenSource.Token);

        using var client = new HttpClient { BaseAddress = new Uri(ServerAddress(app)) };
        await CreateFullUserWorkspaceAllowlistedChainAsync(
            client,
            handoffDraftState,
            workspaceDraftState,
            TestContext.CancellationTokenSource.Token);
        var surfaceModel = new ProductLedgerLocalDevRoutePreview()
            .Render(
                ProductLedgerLocalDevRoutePreview.CreateDefaultLocalDevRequest(),
                ProductLedgerOperatorSurfaceReadModelSource.FixtureSafe,
                approvalState.Store.Read(),
                executionState.Executor.Read(),
                boundedState.Executor.Read(),
                handoffDraftState.Executor.Read(),
                workspaceDraftState.Executor.Read(),
                userWorkspaceDraftState.Executor.Read(),
                latestStateSnapshotState.Executor.Read(),
                latestStateManifestState.Writer.Read())
            .CanonicalSurface;
        var surfaceHash = ProductLedgerLocalOperatorSurfaceLatestStateSnapshotExecutor.ComputeOperatorSurfaceModelHash(surfaceModel);

        using var snapshot = await client.PostAsync(
            ProductLedgerLocalDevRouteEndpointMapper.LocalOperatorSurfaceLatestStateSnapshotRoute,
            JsonContent(ReadyLatestStateSnapshotBody(surfaceHash)),
            TestContext.CancellationTokenSource.Token);
        Assert.AreEqual(System.Net.HttpStatusCode.OK, snapshot.StatusCode);
        var snapshotState = latestStateSnapshotState.Executor.Read();

        using var manifest = await client.PostAsync(
            ProductLedgerLocalDevRouteEndpointMapper.LocalOperatorSurfaceLatestStateManifestRoute,
            JsonContent(ReadyLatestStateManifestBody(snapshotState.SnapshotContentHash, snapshotState.CheckpointHash)),
            TestContext.CancellationTokenSource.Token);
        var manifestJson = await manifest.Content.ReadAsStringAsync(TestContext.CancellationTokenSource.Token);
        using var manifestState = await client.GetAsync(
            ProductLedgerLocalDevRouteEndpointMapper.LocalOperatorSurfaceLatestStateManifestStateRoute,
            TestContext.CancellationTokenSource.Token);
        var manifestStateJson = await manifestState.Content.ReadAsStringAsync(TestContext.CancellationTokenSource.Token);
        using var surface = await client.GetAsync(
            ProductLedgerLocalDevRoutePreview.RouteTemplatePreview,
            TestContext.CancellationTokenSource.Token);
        var html = await surface.Content.ReadAsStringAsync(TestContext.CancellationTokenSource.Token);

        Assert.AreEqual(System.Net.HttpStatusCode.OK, manifest.StatusCode);
        Assert.AreEqual(System.Net.HttpStatusCode.OK, manifestState.StatusCode);
        StringAssert.Contains(manifestJson, "manifestCreatedLocalOnly");
        StringAssert.Contains(manifestJson, ProductLedgerLocalOperatorSurfaceLatestStateManifestWriter.AllowedRelativeOutputBoundary);
        StringAssert.Contains(manifestJson, "historicalIndexEvidenceOnly");
        StringAssert.Contains(manifestJson, "latestPointerAvailable\":false");
        StringAssert.Contains(manifestJson, "readPrecedenceAllowed\":false");
        StringAssert.Contains(manifestStateJson, "manifestCreatedLocalOnly");
        Assert.IsTrue(Directory.Exists(latestStateManifestState.AllowedBoundaryRoot));
        var files = Directory.GetFiles(latestStateManifestState.AllowedBoundaryRoot, "*.json");
        Assert.AreEqual(1, files.Length);
        var content = File.ReadAllText(files[0]);
        StringAssert.Contains(content, "\"classification\": \"LOCAL_INTERNAL_DEV_ONLY_VERSIONED_MANIFEST_NOT_AUTHORITY\"");
        StringAssert.Contains(content, "\"historicalIndexEvidenceOnly\": true");
        StringAssert.Contains(content, "\"authorityLiveProduct\": false");
        StringAssert.Contains(content, "\"productAuthority\": false");
        StringAssert.Contains(content, "\"latestPointer\": false");
        StringAssert.Contains(content, "\"latestPointerOverwrite\": false");
        StringAssert.Contains(content, "\"readPrecedence\": false");
        StringAssert.Contains(content, "\"releaseCommercial\": false");
        Assert.IsFalse(File.Exists(Path.Combine(latestStateManifestState.AllowedBoundaryRoot, "latest.json")));
        Assert.IsFalse(content.Contains("password=", StringComparison.OrdinalIgnoreCase));
        Assert.IsFalse(content.Contains(@"C:\Users\", StringComparison.OrdinalIgnoreCase));
        Assert.IsFalse(content.Contains(latestStateManifestState.WorkspaceRoot, StringComparison.OrdinalIgnoreCase));
        StringAssert.Contains(html, "data-testid=\"product-ledger-latest-state-manifest-state\"");
        StringAssert.Contains(html, "data-state=\"ManifestCreatedLocalOnly\"");
        StringAssert.Contains(html, "data-historical-index-evidence-only=\"true\"");
        StringAssert.Contains(html, "data-authority-live-product=\"false\"");
        StringAssert.Contains(html, "data-product-authority=\"false\"");
        StringAssert.Contains(html, "data-latest-pointer=\"false\"");
        StringAssert.Contains(html, "data-read-precedence=\"false\"");
        StringAssert.Contains(html, "json-only immutable versioned create-only no-overwrite no latest pointer no read precedence");
        Assert.IsFalse(html.Contains("data-executable=\"true\"", StringComparison.OrdinalIgnoreCase));
        Assert.IsFalse(html.Contains("<form", StringComparison.OrdinalIgnoreCase));
        Assert.IsFalse(html.Contains("<script", StringComparison.OrdinalIgnoreCase));
    }

    [TestMethod]
    public async Task ProductLedgerDurableLatestStateReaderCandidateRoute_DevelopmentHostValidatesCandidateAndSurfaceShowsState()
    {
        using var approvalState = ApprovalStateFixture.Create();
        using var executionState = ApprovalExecutionFixture.Create();
        using var boundedState = BoundedApprovalExecutionFixture.Create();
        using var handoffDraftState = HandoffReportDraftFixture.Create();
        using var workspaceDraftState = WorkspaceTestJailHandoffDraftFixture.Create();
        using var userWorkspaceDraftState = UserWorkspaceAllowlistedHandoffDraftFixture.Create();
        using var latestStateSnapshotState = LatestStateSnapshotFixture.Create();
        using var latestStateManifestState = LatestStateManifestFixture.Create(latestStateSnapshotState.WorkspaceRoot);
        using var durableReaderCandidateState = DurableLatestStateReaderCandidateFixture.Create(latestStateSnapshotState.WorkspaceRoot);
        await using var app = BuildLocalOnlyApp(
            Environments.Development,
            ProductLedgerOperatorSurfaceReadModelSource.FixtureSafe,
            approvalState.Store,
            executionState.Executor,
            boundedState.Executor,
            handoffDraftState.Executor,
            workspaceDraftState.Executor,
            userWorkspaceDraftState.Executor,
            latestStateSnapshotState.Executor,
            latestStateManifestState.Writer,
            durableReaderCandidateState.Validator);
        await app.StartAsync(TestContext.CancellationTokenSource.Token);

        using var client = new HttpClient { BaseAddress = new Uri(ServerAddress(app)) };
        await CreateFullUserWorkspaceAllowlistedChainAsync(
            client,
            handoffDraftState,
            workspaceDraftState,
            TestContext.CancellationTokenSource.Token);
        var surfaceModel = new ProductLedgerLocalDevRoutePreview()
            .Render(
                ProductLedgerLocalDevRoutePreview.CreateDefaultLocalDevRequest(),
                ProductLedgerOperatorSurfaceReadModelSource.FixtureSafe,
                approvalState.Store.Read(),
                executionState.Executor.Read(),
                boundedState.Executor.Read(),
                handoffDraftState.Executor.Read(),
                workspaceDraftState.Executor.Read(),
                userWorkspaceDraftState.Executor.Read(),
                latestStateSnapshotState.Executor.Read(),
                latestStateManifestState.Writer.Read(),
                durableReaderCandidateState.Validator.Read())
            .CanonicalSurface;
        var surfaceHash = ProductLedgerLocalOperatorSurfaceLatestStateSnapshotExecutor.ComputeOperatorSurfaceModelHash(surfaceModel);

        using var snapshot = await client.PostAsync(
            ProductLedgerLocalDevRouteEndpointMapper.LocalOperatorSurfaceLatestStateSnapshotRoute,
            JsonContent(ReadyLatestStateSnapshotBody(surfaceHash)),
            TestContext.CancellationTokenSource.Token);
        Assert.AreEqual(System.Net.HttpStatusCode.OK, snapshot.StatusCode);
        var snapshotState = latestStateSnapshotState.Executor.Read();

        using var manifest = await client.PostAsync(
            ProductLedgerLocalDevRouteEndpointMapper.LocalOperatorSurfaceLatestStateManifestRoute,
            JsonContent(ReadyLatestStateManifestBody(snapshotState.SnapshotContentHash, snapshotState.CheckpointHash)),
            TestContext.CancellationTokenSource.Token);
        Assert.AreEqual(System.Net.HttpStatusCode.OK, manifest.StatusCode);

        using var candidate = await client.GetAsync(
            ProductLedgerLocalDevRouteEndpointMapper.LocalDurableLatestStateReaderCandidateRoute,
            TestContext.CancellationTokenSource.Token);
        var candidateJson = await candidate.Content.ReadAsStringAsync(TestContext.CancellationTokenSource.Token);
        using var surface = await client.GetAsync(
            ProductLedgerLocalDevRoutePreview.RouteTemplatePreview,
            TestContext.CancellationTokenSource.Token);
        var html = await surface.Content.ReadAsStringAsync(TestContext.CancellationTokenSource.Token);
        using var queryOverride = await client.GetAsync(
            ProductLedgerLocalDevRouteEndpointMapper.LocalDurableLatestStateReaderCandidateRoute + "?path=C%3A%5CUsers%5Cfixture%5Cunsafe.json",
            TestContext.CancellationTokenSource.Token);
        var queryOverrideJson = await queryOverride.Content.ReadAsStringAsync(TestContext.CancellationTokenSource.Token);
        using var headerOverrideRequest = new HttpRequestMessage(HttpMethod.Get, ProductLedgerLocalDevRouteEndpointMapper.LocalDurableLatestStateReaderCandidateRoute);
        headerOverrideRequest.Headers.Add("x-product-ledger-path-override", @"C:\Users\fixture\unsafe.json");
        using var headerOverride = await client.SendAsync(headerOverrideRequest, TestContext.CancellationTokenSource.Token);
        var headerOverrideJson = await headerOverride.Content.ReadAsStringAsync(TestContext.CancellationTokenSource.Token);

        Assert.AreEqual(System.Net.HttpStatusCode.OK, candidate.StatusCode);
        Assert.AreEqual(System.Net.HttpStatusCode.BadRequest, queryOverride.StatusCode);
        Assert.AreEqual(System.Net.HttpStatusCode.BadRequest, headerOverride.StatusCode);
        Assert.AreEqual(System.Net.HttpStatusCode.OK, surface.StatusCode);
        StringAssert.Contains(candidateJson, "validatedCandidateNotAuthority");
        StringAssert.Contains(candidateJson, ProductLedgerLocalDurableLatestStateReaderCandidateValidator.Classification);
        StringAssert.Contains(candidateJson, "\"authority\":false");
        StringAssert.Contains(candidateJson, "\"liveAuthority\":false");
        StringAssert.Contains(candidateJson, "\"productAuthority\":false");
        StringAssert.Contains(candidateJson, "\"readPrecedence\":false");
        StringAssert.Contains(candidateJson, "\"latestPointer\":false");
        StringAssert.Contains(candidateJson, "\"candidateEvidenceOnly\":true");
        StringAssert.Contains(candidateJson, "\"readOnly\":true");
        StringAssert.Contains(queryOverrideJson, "queryOverrideRejected");
        StringAssert.Contains(headerOverrideJson, "headerOverrideRejected");
        StringAssert.Contains(html, "data-testid=\"product-ledger-durable-latest-state-reader-candidate-state\"");
        StringAssert.Contains(html, "data-state=\"CandidateValidatedNotAuthority\"");
        StringAssert.Contains(html, "data-authority=\"false\"");
        StringAssert.Contains(html, "data-product-authority=\"false\"");
        StringAssert.Contains(html, "data-read-precedence=\"false\"");
        StringAssert.Contains(html, "data-latest-pointer=\"false\"");
        StringAssert.Contains(html, "data-stale-aware=\"true\"");
        StringAssert.Contains(html, "not authority not live/product candidate evidence only");
        StringAssert.Contains(html, "no latest pointer no read precedence");
        Assert.IsFalse(html.Contains(@"C:\Users\", StringComparison.OrdinalIgnoreCase));
        Assert.IsFalse(html.Contains("password=", StringComparison.OrdinalIgnoreCase));
        Assert.IsFalse(html.Contains("<form", StringComparison.OrdinalIgnoreCase));
        Assert.IsFalse(html.Contains("<script", StringComparison.OrdinalIgnoreCase));
    }

    [TestMethod]
    public async Task ProductLedgerApprovedHandoffReportDraftRoute_FailsClosedForMissingBoundedMalformedUnsafeOrMismatchedInput()
    {
        var cases = new (bool PersistApproval, bool ExecuteNoOp, bool ExecuteBounded, StringContent Content, bool ExpectDraftFile)[]
        {
            (true, true, false, JsonContent(ReadyHandoffReportDraftBody()), false),
            (true, true, true, new StringContent("{not-json", Encoding.UTF8, "application/json"), false),
            (true, true, true, JsonContent(ReadyHandoffReportDraftBody() with { ActionKind = "UnknownHandoffDraft" }), false),
            (true, true, true, JsonContent(ReadyHandoffReportDraftBody() with { CurrentEvidenceHash = new string('b', 64) }), false),
            (true, true, true, JsonContent(ReadyHandoffReportDraftBody() with { ProposedPath = @"C:\Users\fixture\unsafe.txt" }), false),
            (true, true, true, JsonContent(ReadyHandoffReportDraftBody() with { ProposedCommand = "cmd /c whoami" }), false),
            (true, true, true, JsonContent(ReadyHandoffReportDraftBody() with { ProposedUrl = "https://local.invalid/action" }), false),
            (true, true, true, JsonContent(ReadyHandoffReportDraftBody() with { RequestsOverwrite = true }), false),
            (true, true, true, JsonContent(ReadyHandoffReportDraftBody() with { RequestsUserFileWrite = true }), false),
            (true, true, true, JsonContent(ReadyHandoffReportDraftBody() with { RequestsShellOrSubprocess = true }), false),
            (true, true, true, JsonContent(ReadyHandoffReportDraftBody() with { RequestsProductCommandExecution = true }), false),
            (true, true, true, JsonContent(ReadyHandoffReportDraftBody() with { ClaimsPilotRun = true }), false),
            (true, true, true, JsonContent(ReadyHandoffReportDraftBody() with { RedactedDraftSummary = "password=unsafe" }), false)
        };

        foreach (var testCase in cases)
        {
            using var approvalState = ApprovalStateFixture.Create();
            using var executionState = ApprovalExecutionFixture.Create();
            using var boundedState = BoundedApprovalExecutionFixture.Create();
            using var handoffDraftState = HandoffReportDraftFixture.Create();
            await using var app = BuildLocalOnlyApp(
                Environments.Development,
                ProductLedgerOperatorSurfaceReadModelSource.FixtureSafe,
                approvalState.Store,
                executionState.Executor,
                boundedState.Executor,
                handoffDraftState.Executor);
            await app.StartAsync(TestContext.CancellationTokenSource.Token);

            using var client = new HttpClient { BaseAddress = new Uri(ServerAddress(app)) };
            if (testCase.PersistApproval)
            {
                using var approval = await client.PostAsync(
                    ProductLedgerLocalDevRouteEndpointMapper.LocalApprovalDecisionRoute,
                    JsonContent(ReadyApprovalDecisionBody("Approve")),
                    TestContext.CancellationTokenSource.Token);
                Assert.AreEqual(System.Net.HttpStatusCode.OK, approval.StatusCode);
            }

            if (testCase.ExecuteNoOp)
            {
                using var noOp = await client.PostAsync(
                    ProductLedgerLocalDevRouteEndpointMapper.LocalApprovalExecutionRoute,
                    JsonContent(ReadyApprovalExecutionBody()),
                    TestContext.CancellationTokenSource.Token);
                Assert.AreEqual(System.Net.HttpStatusCode.OK, noOp.StatusCode);
            }

            if (testCase.ExecuteBounded)
            {
                using var bounded = await client.PostAsync(
                    ProductLedgerLocalDevRouteEndpointMapper.LocalBoundedApprovalExecutionRoute,
                    JsonContent(ReadyBoundedApprovalExecutionBody()),
                    TestContext.CancellationTokenSource.Token);
                Assert.AreEqual(System.Net.HttpStatusCode.OK, bounded.StatusCode);
            }

            using var draft = await client.PostAsync(
                ProductLedgerLocalDevRouteEndpointMapper.LocalApprovedHandoffReportDraftRoute,
                testCase.Content,
                TestContext.CancellationTokenSource.Token);

            Assert.AreEqual(System.Net.HttpStatusCode.BadRequest, draft.StatusCode);
            Assert.AreEqual(testCase.ExpectDraftFile, File.Exists(handoffDraftState.ExpectedReadyPath));
        }
    }

    [TestMethod]
    public async Task ProductLedgerWorkspaceTestJailHandoffDraftRoute_FailsClosedForMissingPredecessorMalformedUnsafeOrMismatchedInput()
    {
        var cases = new (bool CreatePredecessor, StringContent Content, bool ExpectDraftFile)[]
        {
            (false, JsonContent(ReadyWorkspaceTestJailHandoffDraftBody(new string('d', 64))), false),
            (true, new StringContent("{not-json", Encoding.UTF8, "application/json"), false),
            (true, JsonContent(ReadyWorkspaceTestJailHandoffDraftBody(new string('d', 64)) with { ActionKind = "UnknownWorkspaceDraft" }), false),
            (true, JsonContent(ReadyWorkspaceTestJailHandoffDraftBody(new string('d', 64)) with { CurrentEvidenceHash = new string('b', 64) }), false),
            (true, JsonContent(ReadyWorkspaceTestJailHandoffDraftBody(new string('d', 64)) with { PredecessorDraftContentHash = new string('c', 64) }), false),
            (true, JsonContent(ReadyWorkspaceTestJailHandoffDraftBody(new string('d', 64)) with { ProposedPath = @"C:\Users\fixture\unsafe.txt" }), false),
            (true, JsonContent(ReadyWorkspaceTestJailHandoffDraftBody(new string('d', 64)) with { ProposedRoot = @"C:\Users\fixture" }), false),
            (true, JsonContent(ReadyWorkspaceTestJailHandoffDraftBody(new string('d', 64)) with { ProposedFilename = "..\\unsafe.md" }), false),
            (true, JsonContent(ReadyWorkspaceTestJailHandoffDraftBody(new string('d', 64)) with { ProposedCommand = "cmd /c whoami" }), false),
            (true, JsonContent(ReadyWorkspaceTestJailHandoffDraftBody(new string('d', 64)) with { ProposedUrl = "https://local.invalid/action" }), false),
            (true, JsonContent(ReadyWorkspaceTestJailHandoffDraftBody(new string('d', 64)) with { RequestsOverwrite = true }), false),
            (true, JsonContent(ReadyWorkspaceTestJailHandoffDraftBody(new string('d', 64)) with { RequestsUserSelectedPath = true }), false),
            (true, JsonContent(ReadyWorkspaceTestJailHandoffDraftBody(new string('d', 64)) with { RequestsShellOrSubprocess = true }), false),
            (true, JsonContent(ReadyWorkspaceTestJailHandoffDraftBody(new string('d', 64)) with { RequestsProductCommandExecution = true }), false),
            (true, JsonContent(ReadyWorkspaceTestJailHandoffDraftBody(new string('d', 64)) with { ClaimsPilotRun = true }), false),
            (true, JsonContent(ReadyWorkspaceTestJailHandoffDraftBody(new string('d', 64)) with { RedactedDraftSummary = "password=unsafe" }), false)
        };

        foreach (var testCase in cases)
        {
            using var approvalState = ApprovalStateFixture.Create();
            using var executionState = ApprovalExecutionFixture.Create();
            using var boundedState = BoundedApprovalExecutionFixture.Create();
            using var handoffDraftState = HandoffReportDraftFixture.Create();
            using var workspaceDraftState = WorkspaceTestJailHandoffDraftFixture.Create();
            await using var app = BuildLocalOnlyApp(
                Environments.Development,
                ProductLedgerOperatorSurfaceReadModelSource.FixtureSafe,
                approvalState.Store,
                executionState.Executor,
                boundedState.Executor,
                handoffDraftState.Executor,
                workspaceDraftState.Executor);
            await app.StartAsync(TestContext.CancellationTokenSource.Token);

            using var client = new HttpClient { BaseAddress = new Uri(ServerAddress(app)) };
            using var approval = await client.PostAsync(
                ProductLedgerLocalDevRouteEndpointMapper.LocalApprovalDecisionRoute,
                JsonContent(ReadyApprovalDecisionBody("Approve")),
                TestContext.CancellationTokenSource.Token);
            using var noOp = await client.PostAsync(
                ProductLedgerLocalDevRouteEndpointMapper.LocalApprovalExecutionRoute,
                JsonContent(ReadyApprovalExecutionBody()),
                TestContext.CancellationTokenSource.Token);
            using var bounded = await client.PostAsync(
                ProductLedgerLocalDevRouteEndpointMapper.LocalBoundedApprovalExecutionRoute,
                JsonContent(ReadyBoundedApprovalExecutionBody()),
                TestContext.CancellationTokenSource.Token);
            Assert.AreEqual(System.Net.HttpStatusCode.OK, approval.StatusCode);
            Assert.AreEqual(System.Net.HttpStatusCode.OK, noOp.StatusCode);
            Assert.AreEqual(System.Net.HttpStatusCode.OK, bounded.StatusCode);

            var content = testCase.Content;
            if (testCase.CreatePredecessor)
            {
                using var predecessor = await client.PostAsync(
                    ProductLedgerLocalDevRouteEndpointMapper.LocalApprovedHandoffReportDraftRoute,
                    JsonContent(ReadyHandoffReportDraftBody()),
                    TestContext.CancellationTokenSource.Token);
                Assert.AreEqual(System.Net.HttpStatusCode.OK, predecessor.StatusCode);
                ProductLedgerWorkspaceTestJailHandoffDraftRouteBody? body;
                try
                {
                    body = JsonSerializer.Deserialize<ProductLedgerWorkspaceTestJailHandoffDraftRouteBody>(
                        await testCase.Content.ReadAsStringAsync(TestContext.CancellationTokenSource.Token));
                }
                catch (JsonException)
                {
                    body = null;
                }

                if (body is not null)
                {
                    var predecessorHash = body.PredecessorDraftContentHash == new string('d', 64)
                        ? handoffDraftState.Executor.Read().ContentHash
                        : body.PredecessorDraftContentHash;
                    content = JsonContent(body with { PredecessorDraftContentHash = predecessorHash });
                }
            }

            using var draft = await client.PostAsync(
                ProductLedgerLocalDevRouteEndpointMapper.LocalWorkspaceTestJailHandoffDraftRoute,
                content,
                TestContext.CancellationTokenSource.Token);

            Assert.AreEqual(System.Net.HttpStatusCode.BadRequest, draft.StatusCode);
            Assert.AreEqual(testCase.ExpectDraftFile, File.Exists(workspaceDraftState.ExpectedReadyPath));
        }
    }

    [TestMethod]
    public async Task ProductLedgerBoundedApprovalExecutionRoute_FailsClosedForMissingNoOpMalformedPayloadOrAuthorityClaims()
    {
        var cases = new (bool PersistApproval, bool ExecuteNoOp, StringContent Content, bool ExpectStateFile)[]
        {
            (true, false, JsonContent(ReadyBoundedApprovalExecutionBody()), false),
            (true, true, new StringContent("{not-json", Encoding.UTF8, "application/json"), false),
            (true, true, JsonContent(ReadyBoundedApprovalExecutionBody() with { ActionKind = "UnknownBoundedAction" }), false),
            (true, true, JsonContent(ReadyBoundedApprovalExecutionBody() with { CurrentEvidenceHash = new string('b', 64) }), false),
            (true, true, JsonContent(ReadyBoundedApprovalExecutionBody() with { ProposedPath = @"C:\Users\fixture\unsafe.txt" }), false),
            (true, true, JsonContent(ReadyBoundedApprovalExecutionBody() with { ProposedCommand = "cmd /c whoami" }), false),
            (true, true, JsonContent(ReadyBoundedApprovalExecutionBody() with { ProposedUrl = "https://local.invalid/action" }), false),
            (true, true, JsonContent(ReadyBoundedApprovalExecutionBody() with { RequestsUserFileWrite = true }), false),
            (true, true, JsonContent(ReadyBoundedApprovalExecutionBody() with { RequestsShellOrSubprocess = true }), false),
            (true, true, JsonContent(ReadyBoundedApprovalExecutionBody() with { RequestsProductCommandExecution = true }), false),
            (true, true, JsonContent(ReadyBoundedApprovalExecutionBody() with { RequestsPublicUiAction = true }), false)
        };

        foreach (var testCase in cases)
        {
            using var approvalState = ApprovalStateFixture.Create();
            using var executionState = ApprovalExecutionFixture.Create();
            using var boundedState = BoundedApprovalExecutionFixture.Create();
            await using var app = BuildLocalOnlyApp(
                Environments.Development,
                ProductLedgerOperatorSurfaceReadModelSource.FixtureSafe,
                approvalState.Store,
                executionState.Executor,
                boundedState.Executor);
            await app.StartAsync(TestContext.CancellationTokenSource.Token);

            using var client = new HttpClient { BaseAddress = new Uri(ServerAddress(app)) };
            if (testCase.PersistApproval)
            {
                using var approval = await client.PostAsync(
                    ProductLedgerLocalDevRouteEndpointMapper.LocalApprovalDecisionRoute,
                    JsonContent(ReadyApprovalDecisionBody("Approve")),
                    TestContext.CancellationTokenSource.Token);
                Assert.AreEqual(System.Net.HttpStatusCode.OK, approval.StatusCode);
            }

            if (testCase.ExecuteNoOp)
            {
                using var noOp = await client.PostAsync(
                    ProductLedgerLocalDevRouteEndpointMapper.LocalApprovalExecutionRoute,
                    JsonContent(ReadyApprovalExecutionBody()),
                    TestContext.CancellationTokenSource.Token);
                Assert.AreEqual(System.Net.HttpStatusCode.OK, noOp.StatusCode);
            }

            using var bounded = await client.PostAsync(
                ProductLedgerLocalDevRouteEndpointMapper.LocalBoundedApprovalExecutionRoute,
                testCase.Content,
                TestContext.CancellationTokenSource.Token);

            Assert.AreEqual(System.Net.HttpStatusCode.BadRequest, bounded.StatusCode);
            Assert.AreEqual(testCase.ExpectStateFile, File.Exists(boundedState.StateFilePath));
        }
    }

    [TestMethod]
    public async Task ProductLedgerApprovalDecisionRoute_FailsClosedForMalformedUnsafeOrMismatchedInput()
    {
        using var approvalState = ApprovalStateFixture.Create();
        await using var app = BuildLocalOnlyApp(
            Environments.Development,
            ProductLedgerOperatorSurfaceReadModelSource.FixtureSafe,
            approvalState.Store);
        await app.StartAsync(TestContext.CancellationTokenSource.Token);

        using var client = new HttpClient { BaseAddress = new Uri(ServerAddress(app)) };
        using var malformed = await client.PostAsync(
            ProductLedgerLocalDevRouteEndpointMapper.LocalApprovalDecisionRoute,
            new StringContent("{not-json", Encoding.UTF8, "application/json"),
            TestContext.CancellationTokenSource.Token);
        using var mismatch = await client.PostAsync(
            ProductLedgerLocalDevRouteEndpointMapper.LocalApprovalDecisionRoute,
            JsonContent(ReadyApprovalDecisionBody("Approve") with { CurrentEvidenceHash = new string('b', 64) }),
            TestContext.CancellationTokenSource.Token);
        using var unsafeNote = await client.PostAsync(
            ProductLedgerLocalDevRouteEndpointMapper.LocalApprovalDecisionRoute,
            JsonContent(ReadyApprovalDecisionBody("Approve") with { OperatorNote = "password=super-secret" }),
            TestContext.CancellationTokenSource.Token);
        using var publicAction = await client.PostAsync(
            ProductLedgerLocalDevRouteEndpointMapper.LocalApprovalDecisionRoute,
            JsonContent(ReadyApprovalDecisionBody("Approve") with { RequestsPublicUiAction = true }),
            TestContext.CancellationTokenSource.Token);

        Assert.AreEqual(System.Net.HttpStatusCode.BadRequest, malformed.StatusCode);
        Assert.AreEqual(System.Net.HttpStatusCode.BadRequest, mismatch.StatusCode);
        Assert.AreEqual(System.Net.HttpStatusCode.BadRequest, unsafeNote.StatusCode);
        Assert.AreEqual(System.Net.HttpStatusCode.BadRequest, publicAction.StatusCode);
        Assert.IsFalse(File.Exists(approvalState.StateFilePath));
    }

    [TestMethod]
    public async Task ProductLedgerRouteResponse_TestSafeLiveLedgerReturnsVerifiedHeadWithoutMutatingLedger()
    {
        using var fixture = LedgerFixture.Create();
        var setup = CreateLiveLedger(fixture);
        var beforeLedgerHash = Sha256File(setup.Activation.ActiveLedgerFilePath!);
        var beforeCheckpointHash = Sha256File(setup.Activation.ActiveCheckpointFilePath!);
        await using var app = BuildLocalOnlyApp(
            Environments.Development,
            ProductLedgerOperatorSurfaceReadModelSource.TestSafeLiveLedger(
                setup.Activation,
                "recipes-live-read-model-test-source"));
        await app.StartAsync(TestContext.CancellationTokenSource.Token);

        using var client = new HttpClient { BaseAddress = new Uri(ServerAddress(app)) };
        using var first = await client.GetAsync(
            ProductLedgerLocalDevRoutePreview.RouteTemplatePreview,
            TestContext.CancellationTokenSource.Token);
        using var second = await client.GetAsync(
            ProductLedgerLocalDevRoutePreview.RouteTemplatePreview + "?path=C:\\Users\\synthetic\\must-not-be-used",
            TestContext.CancellationTokenSource.Token);
        var html = await first.Content.ReadAsStringAsync(TestContext.CancellationTokenSource.Token);
        var htmlWithIgnoredQuery = await second.Content.ReadAsStringAsync(TestContext.CancellationTokenSource.Token);

        Assert.AreEqual(System.Net.HttpStatusCode.OK, first.StatusCode);
        Assert.AreEqual(System.Net.HttpStatusCode.OK, second.StatusCode);
        StringAssert.Contains(html, "data-read-model-mode=\"TestSafeLiveLedgerReadModel\"");
        StringAssert.Contains(html, "TEST_SAFE_LIVE_LEDGER_VERIFIED_READ_ONLY entry_count=2");
        StringAssert.Contains(html, "TEST_SAFE_LIVE_LEDGER_CHECKPOINT_MATCHED head_sequence=2");
        StringAssert.Contains(html, "data-testid=\"product-ledger-entry-count\">entry_count=2");
        StringAssert.Contains(html, "data-testid=\"product-ledger-head-sequence\">head_sequence=2");
        StringAssert.Contains(html, $"head_hash_prefix={setup.Second.Entry!.EntryHash[..12]}");
        StringAssert.Contains(html, "LOCAL_ONLY_BOUNDARY_PATH_REDACTED_NO_ARBITRARY_PATH_INPUT");
        StringAssert.Contains(html, "REDACTION_RETENTION_GUARDS_VERIFIED_FROM_SAFE_METADATA");
        StringAssert.Contains(html, "ACTIVE_WRITER_CONCURRENCY_LOCK_EVIDENCE_READ_ONLY_VISIBLE");
        StringAssert.Contains(html, "BOUNDED_LOCAL_EXPORT_STATUS_VISIBLE_NO_EXPORT_CALL");
        Assert.IsFalse(html.Contains(fixture.LedgerRoot, StringComparison.OrdinalIgnoreCase));
        Assert.IsFalse(htmlWithIgnoredQuery.Contains("C:\\Users\\synthetic", StringComparison.OrdinalIgnoreCase));
        Assert.IsFalse(html.Contains("data-executable=\"true\"", StringComparison.OrdinalIgnoreCase));
        Assert.IsFalse(html.Contains("<form", StringComparison.OrdinalIgnoreCase));
        Assert.IsFalse(html.Contains("<script", StringComparison.OrdinalIgnoreCase));
        Assert.IsFalse(html.Contains("onclick=", StringComparison.OrdinalIgnoreCase));
        Assert.IsFalse(html.Contains("formaction=", StringComparison.OrdinalIgnoreCase));
        Assert.AreEqual(beforeLedgerHash, Sha256File(setup.Activation.ActiveLedgerFilePath!));
        Assert.AreEqual(beforeCheckpointHash, Sha256File(setup.Activation.ActiveCheckpointFilePath!));
        Assert.AreEqual(2, new ProductLedgerPathLocalOnlyActiveWriter().ReadVerified(setup.Activation).Count);
    }

    [TestMethod]
    public async Task ProductLedgerRouteResponse_NonDevelopmentHostDoesNotMapRoute()
    {
        await using var app = BuildLocalOnlyApp(Environments.Production);
        await app.StartAsync(TestContext.CancellationTokenSource.Token);

        using var client = new HttpClient { BaseAddress = new Uri(ServerAddress(app)) };
        using var response = await client.GetAsync(
            ProductLedgerLocalDevRoutePreview.RouteTemplatePreview,
            TestContext.CancellationTokenSource.Token);

        Assert.AreEqual(System.Net.HttpStatusCode.NotFound, response.StatusCode);
    }

    [TestMethod]
    public async Task ProductLedgerApprovalDecisionRoute_NonDevelopmentHostDoesNotMapPostOrStateRead()
    {
        using var approvalState = ApprovalStateFixture.Create();
        using var executionState = ApprovalExecutionFixture.Create();
        using var boundedState = BoundedApprovalExecutionFixture.Create();
        using var handoffDraftState = HandoffReportDraftFixture.Create();
        using var workspaceDraftState = WorkspaceTestJailHandoffDraftFixture.Create();
        using var userWorkspaceDraftState = UserWorkspaceAllowlistedHandoffDraftFixture.Create();
        using var latestStateSnapshotState = LatestStateSnapshotFixture.Create();
        using var latestStateManifestState = LatestStateManifestFixture.Create();
        using var durableReaderCandidateState = DurableLatestStateReaderCandidateFixture.Create(latestStateManifestState.WorkspaceRoot);
        await using var app = BuildLocalOnlyApp(
            Environments.Production,
            ProductLedgerOperatorSurfaceReadModelSource.FixtureSafe,
            approvalState.Store,
            executionState.Executor,
            boundedState.Executor,
            handoffDraftState.Executor,
            workspaceDraftState.Executor,
            userWorkspaceDraftState.Executor,
            latestStateSnapshotState.Executor,
            latestStateManifestState.Writer,
            durableReaderCandidateState.Validator);
        await app.StartAsync(TestContext.CancellationTokenSource.Token);

        using var client = new HttpClient { BaseAddress = new Uri(ServerAddress(app)) };
        using var post = await client.PostAsync(
            ProductLedgerLocalDevRouteEndpointMapper.LocalApprovalDecisionRoute,
            JsonContent(ReadyApprovalDecisionBody("Approve")),
            TestContext.CancellationTokenSource.Token);
        using var state = await client.GetAsync(
            ProductLedgerLocalDevRouteEndpointMapper.LocalApprovalDecisionStateRoute,
            TestContext.CancellationTokenSource.Token);
        using var execution = await client.PostAsync(
            ProductLedgerLocalDevRouteEndpointMapper.LocalApprovalExecutionRoute,
            JsonContent(ReadyApprovalExecutionBody()),
            TestContext.CancellationTokenSource.Token);
        using var executionStateRead = await client.GetAsync(
            ProductLedgerLocalDevRouteEndpointMapper.LocalApprovalExecutionStateRoute,
            TestContext.CancellationTokenSource.Token);
        using var bounded = await client.PostAsync(
            ProductLedgerLocalDevRouteEndpointMapper.LocalBoundedApprovalExecutionRoute,
            JsonContent(ReadyBoundedApprovalExecutionBody()),
            TestContext.CancellationTokenSource.Token);
        using var boundedStateRead = await client.GetAsync(
            ProductLedgerLocalDevRouteEndpointMapper.LocalBoundedApprovalExecutionStateRoute,
            TestContext.CancellationTokenSource.Token);
        using var draft = await client.PostAsync(
            ProductLedgerLocalDevRouteEndpointMapper.LocalApprovedHandoffReportDraftRoute,
            JsonContent(ReadyHandoffReportDraftBody()),
            TestContext.CancellationTokenSource.Token);
        using var draftStateRead = await client.GetAsync(
            ProductLedgerLocalDevRouteEndpointMapper.LocalApprovedHandoffReportDraftStateRoute,
            TestContext.CancellationTokenSource.Token);
        using var workspaceDraft = await client.PostAsync(
            ProductLedgerLocalDevRouteEndpointMapper.LocalWorkspaceTestJailHandoffDraftRoute,
            JsonContent(ReadyWorkspaceTestJailHandoffDraftBody(new string('d', 64))),
            TestContext.CancellationTokenSource.Token);
        using var workspaceDraftStateRead = await client.GetAsync(
            ProductLedgerLocalDevRouteEndpointMapper.LocalWorkspaceTestJailHandoffDraftStateRoute,
            TestContext.CancellationTokenSource.Token);
        using var userWorkspaceDraft = await client.PostAsync(
            ProductLedgerLocalDevRouteEndpointMapper.LocalUserWorkspaceAllowlistedHandoffDraftRoute,
            JsonContent(ReadyUserWorkspaceAllowlistedHandoffDraftBody(new string('d', 64), new string('e', 64))),
            TestContext.CancellationTokenSource.Token);
        using var userWorkspaceDraftStateRead = await client.GetAsync(
            ProductLedgerLocalDevRouteEndpointMapper.LocalUserWorkspaceAllowlistedHandoffDraftStateRoute,
            TestContext.CancellationTokenSource.Token);
        using var latestStateSnapshot = await client.PostAsync(
            ProductLedgerLocalDevRouteEndpointMapper.LocalOperatorSurfaceLatestStateSnapshotRoute,
            JsonContent(ReadyLatestStateSnapshotBody(new string('a', 64))),
            TestContext.CancellationTokenSource.Token);
        using var latestStateSnapshotStateRead = await client.GetAsync(
            ProductLedgerLocalDevRouteEndpointMapper.LocalOperatorSurfaceLatestStateSnapshotStateRoute,
            TestContext.CancellationTokenSource.Token);
        using var latestStateManifest = await client.PostAsync(
            ProductLedgerLocalDevRouteEndpointMapper.LocalOperatorSurfaceLatestStateManifestRoute,
            JsonContent(ReadyLatestStateManifestBody(new string('b', 64), new string('c', 64))),
            TestContext.CancellationTokenSource.Token);
        using var latestStateManifestStateRead = await client.GetAsync(
            ProductLedgerLocalDevRouteEndpointMapper.LocalOperatorSurfaceLatestStateManifestStateRoute,
            TestContext.CancellationTokenSource.Token);
        using var durableReaderCandidate = await client.GetAsync(
            ProductLedgerLocalDevRouteEndpointMapper.LocalDurableLatestStateReaderCandidateRoute,
            TestContext.CancellationTokenSource.Token);

        Assert.AreEqual(System.Net.HttpStatusCode.NotFound, post.StatusCode);
        Assert.AreEqual(System.Net.HttpStatusCode.NotFound, state.StatusCode);
        Assert.AreEqual(System.Net.HttpStatusCode.NotFound, execution.StatusCode);
        Assert.AreEqual(System.Net.HttpStatusCode.NotFound, executionStateRead.StatusCode);
        Assert.AreEqual(System.Net.HttpStatusCode.NotFound, bounded.StatusCode);
        Assert.AreEqual(System.Net.HttpStatusCode.NotFound, boundedStateRead.StatusCode);
        Assert.AreEqual(System.Net.HttpStatusCode.NotFound, draft.StatusCode);
        Assert.AreEqual(System.Net.HttpStatusCode.NotFound, draftStateRead.StatusCode);
        Assert.AreEqual(System.Net.HttpStatusCode.NotFound, workspaceDraft.StatusCode);
        Assert.AreEqual(System.Net.HttpStatusCode.NotFound, workspaceDraftStateRead.StatusCode);
        Assert.AreEqual(System.Net.HttpStatusCode.NotFound, userWorkspaceDraft.StatusCode);
        Assert.AreEqual(System.Net.HttpStatusCode.NotFound, userWorkspaceDraftStateRead.StatusCode);
        Assert.AreEqual(System.Net.HttpStatusCode.NotFound, latestStateSnapshot.StatusCode);
        Assert.AreEqual(System.Net.HttpStatusCode.NotFound, latestStateSnapshotStateRead.StatusCode);
        Assert.AreEqual(System.Net.HttpStatusCode.NotFound, latestStateManifest.StatusCode);
        Assert.AreEqual(System.Net.HttpStatusCode.NotFound, latestStateManifestStateRead.StatusCode);
        Assert.AreEqual(System.Net.HttpStatusCode.NotFound, durableReaderCandidate.StatusCode);
        Assert.IsFalse(File.Exists(approvalState.StateFilePath));
        Assert.IsFalse(File.Exists(executionState.StateFilePath));
        Assert.IsFalse(File.Exists(boundedState.StateFilePath));
        Assert.IsFalse(File.Exists(handoffDraftState.ExpectedReadyPath));
        Assert.IsFalse(File.Exists(userWorkspaceDraftState.ExpectedReadyPath));
        Assert.IsFalse(Directory.Exists(latestStateSnapshotState.AllowedBoundaryRoot));
        Assert.IsFalse(Directory.Exists(latestStateManifestState.AllowedBoundaryRoot));
    }

    public TestContext TestContext { get; set; } = null!;

    private static WebApplication BuildLocalOnlyApp(
        string environmentName,
        ProductLedgerOperatorSurfaceReadModelSource? readModelSource = null,
        ProductLedgerLocalApprovalDecisionStateStore? approvalDecisionStateStore = null,
        ProductLedgerLocalApprovedActionNoOpExecutor? noOpExecutor = null,
        ProductLedgerLocalBoundedApprovedActionExecutor? boundedActionExecutor = null,
        ProductLedgerLocalApprovedHandoffReportDraftExecutor? handoffReportDraftExecutor = null,
        ProductLedgerLocalWorkspaceTestJailHandoffDraftExecutor? workspaceTestJailHandoffDraftExecutor = null,
        ProductLedgerLocalUserWorkspaceAllowlistedHandoffDraftExecutor? userWorkspaceAllowlistedHandoffDraftExecutor = null,
        ProductLedgerLocalOperatorSurfaceLatestStateSnapshotExecutor? latestStateSnapshotExecutor = null,
        ProductLedgerLocalOperatorSurfaceLatestStateManifestWriter? latestStateManifestWriter = null,
        ProductLedgerLocalDurableLatestStateReaderCandidateValidator? durableLatestStateReaderCandidateValidator = null)
    {
        var builder = WebApplication.CreateBuilder(new WebApplicationOptions
        {
            EnvironmentName = environmentName
        });

        builder.WebHost.UseUrls("http://127.0.0.1:0");
        var app = builder.Build();
        if (approvalDecisionStateStore is null)
        {
            app.MapProductLedgerLocalDevRoutePreview(
                app.Environment,
                readModelSource ?? ProductLedgerOperatorSurfaceReadModelSource.FixtureSafe);
        }
        else if (noOpExecutor is not null
            && boundedActionExecutor is not null
            && handoffReportDraftExecutor is not null
            && workspaceTestJailHandoffDraftExecutor is not null
            && userWorkspaceAllowlistedHandoffDraftExecutor is not null
            && latestStateSnapshotExecutor is not null
            && latestStateManifestWriter is not null
            && durableLatestStateReaderCandidateValidator is not null)
        {
            app.MapProductLedgerLocalDevRoutePreview(
                app.Environment,
                readModelSource ?? ProductLedgerOperatorSurfaceReadModelSource.FixtureSafe,
                approvalDecisionStateStore,
                noOpExecutor,
                boundedActionExecutor,
                handoffReportDraftExecutor,
                workspaceTestJailHandoffDraftExecutor,
                userWorkspaceAllowlistedHandoffDraftExecutor,
                latestStateSnapshotExecutor,
                latestStateManifestWriter,
                durableLatestStateReaderCandidateValidator);
        }
        else if (noOpExecutor is not null
            && boundedActionExecutor is not null
            && handoffReportDraftExecutor is not null
            && workspaceTestJailHandoffDraftExecutor is not null
            && userWorkspaceAllowlistedHandoffDraftExecutor is not null
            && latestStateSnapshotExecutor is not null
            && latestStateManifestWriter is not null)
        {
            app.MapProductLedgerLocalDevRoutePreview(
                app.Environment,
                readModelSource ?? ProductLedgerOperatorSurfaceReadModelSource.FixtureSafe,
                approvalDecisionStateStore,
                noOpExecutor,
                boundedActionExecutor,
                handoffReportDraftExecutor,
                workspaceTestJailHandoffDraftExecutor,
                userWorkspaceAllowlistedHandoffDraftExecutor,
                latestStateSnapshotExecutor,
                latestStateManifestWriter);
        }
        else if (noOpExecutor is not null
            && boundedActionExecutor is not null
            && handoffReportDraftExecutor is not null
            && workspaceTestJailHandoffDraftExecutor is not null
            && userWorkspaceAllowlistedHandoffDraftExecutor is not null
            && latestStateSnapshotExecutor is not null)
        {
            app.MapProductLedgerLocalDevRoutePreview(
                app.Environment,
                readModelSource ?? ProductLedgerOperatorSurfaceReadModelSource.FixtureSafe,
                approvalDecisionStateStore,
                noOpExecutor,
                boundedActionExecutor,
                handoffReportDraftExecutor,
                workspaceTestJailHandoffDraftExecutor,
                userWorkspaceAllowlistedHandoffDraftExecutor,
                latestStateSnapshotExecutor);
        }
        else if (noOpExecutor is not null
            && boundedActionExecutor is not null
            && handoffReportDraftExecutor is not null
            && workspaceTestJailHandoffDraftExecutor is not null
            && userWorkspaceAllowlistedHandoffDraftExecutor is not null)
        {
            app.MapProductLedgerLocalDevRoutePreview(
                app.Environment,
                readModelSource ?? ProductLedgerOperatorSurfaceReadModelSource.FixtureSafe,
                approvalDecisionStateStore,
                noOpExecutor,
                boundedActionExecutor,
                handoffReportDraftExecutor,
                workspaceTestJailHandoffDraftExecutor,
                userWorkspaceAllowlistedHandoffDraftExecutor);
        }
        else if (noOpExecutor is not null
            && boundedActionExecutor is not null
            && handoffReportDraftExecutor is not null
            && workspaceTestJailHandoffDraftExecutor is not null)
        {
            app.MapProductLedgerLocalDevRoutePreview(
                app.Environment,
                readModelSource ?? ProductLedgerOperatorSurfaceReadModelSource.FixtureSafe,
                approvalDecisionStateStore,
                noOpExecutor,
                boundedActionExecutor,
                handoffReportDraftExecutor,
                workspaceTestJailHandoffDraftExecutor);
        }
        else if (noOpExecutor is not null && boundedActionExecutor is not null && handoffReportDraftExecutor is not null)
        {
            app.MapProductLedgerLocalDevRoutePreview(
                app.Environment,
                readModelSource ?? ProductLedgerOperatorSurfaceReadModelSource.FixtureSafe,
                approvalDecisionStateStore,
                noOpExecutor,
                boundedActionExecutor,
                handoffReportDraftExecutor);
        }
        else if (noOpExecutor is not null && boundedActionExecutor is not null)
        {
            app.MapProductLedgerLocalDevRoutePreview(
                app.Environment,
                readModelSource ?? ProductLedgerOperatorSurfaceReadModelSource.FixtureSafe,
                approvalDecisionStateStore,
                noOpExecutor,
                boundedActionExecutor);
        }
        else if (noOpExecutor is not null)
        {
            app.MapProductLedgerLocalDevRoutePreview(
                app.Environment,
                readModelSource ?? ProductLedgerOperatorSurfaceReadModelSource.FixtureSafe,
                approvalDecisionStateStore,
                noOpExecutor);
        }
        else
        {
            app.MapProductLedgerLocalDevRoutePreview(
                app.Environment,
                readModelSource ?? ProductLedgerOperatorSurfaceReadModelSource.FixtureSafe,
                approvalDecisionStateStore);
        }

        return app;
    }

    private static StringContent JsonContent(object value) =>
        new(JsonSerializer.Serialize(value), Encoding.UTF8, "application/json");

    private static async Task CreateFullUserWorkspaceAllowlistedChainAsync(
        HttpClient client,
        HandoffReportDraftFixture handoffDraftState,
        WorkspaceTestJailHandoffDraftFixture workspaceDraftState,
        CancellationToken cancellationToken)
    {
        using var approval = await client.PostAsync(
            ProductLedgerLocalDevRouteEndpointMapper.LocalApprovalDecisionRoute,
            JsonContent(ReadyApprovalDecisionBody("Approve")),
            cancellationToken);
        using var noOp = await client.PostAsync(
            ProductLedgerLocalDevRouteEndpointMapper.LocalApprovalExecutionRoute,
            JsonContent(ReadyApprovalExecutionBody()),
            cancellationToken);
        using var bounded = await client.PostAsync(
            ProductLedgerLocalDevRouteEndpointMapper.LocalBoundedApprovalExecutionRoute,
            JsonContent(ReadyBoundedApprovalExecutionBody()),
            cancellationToken);
        using var localDraft = await client.PostAsync(
            ProductLedgerLocalDevRouteEndpointMapper.LocalApprovedHandoffReportDraftRoute,
            JsonContent(ReadyHandoffReportDraftBody()),
            cancellationToken);
        using var workspaceDraft = await client.PostAsync(
            ProductLedgerLocalDevRouteEndpointMapper.LocalWorkspaceTestJailHandoffDraftRoute,
            JsonContent(ReadyWorkspaceTestJailHandoffDraftBody(handoffDraftState.Executor.Read().ContentHash)),
            cancellationToken);
        using var userWorkspaceDraft = await client.PostAsync(
            ProductLedgerLocalDevRouteEndpointMapper.LocalUserWorkspaceAllowlistedHandoffDraftRoute,
            JsonContent(ReadyUserWorkspaceAllowlistedHandoffDraftBody(
                handoffDraftState.Executor.Read().ContentHash,
                workspaceDraftState.Executor.Read().ContentHash)),
            cancellationToken);

        Assert.AreEqual(System.Net.HttpStatusCode.OK, approval.StatusCode);
        Assert.AreEqual(System.Net.HttpStatusCode.OK, noOp.StatusCode);
        Assert.AreEqual(System.Net.HttpStatusCode.OK, bounded.StatusCode);
        Assert.AreEqual(System.Net.HttpStatusCode.OK, localDraft.StatusCode);
        Assert.AreEqual(System.Net.HttpStatusCode.OK, workspaceDraft.StatusCode);
        Assert.AreEqual(System.Net.HttpStatusCode.OK, userWorkspaceDraft.StatusCode);
    }

    private static ProductLedgerApprovalDecisionRouteBody ReadyApprovalDecisionBody(string decision) =>
        new(
            ExplicitLocalOnlyStatePersistenceScope: true,
            ApprovalId: "approval-route-state-test-001",
            CandidateEvidenceHash: new string('a', 64),
            CurrentEvidenceHash: new string('a', 64),
            OperatorDecision: decision,
            DecidedAtUtc: new DateTimeOffset(2026, 7, 5, 12, 45, 0, TimeSpan.Zero),
            OperatorClassification: "local-internal-operator",
            OperatorNote: "safe route approval note",
            EvidenceReferences:
            [
                "docs/qa/nodal-os-local-approval-execution-final-local-only-readiness-packet/report.md",
                "docs/qa/nodal-os-local-route-live-ledger-read-model-test-safe/report.md"
            ],
            RequestsPublicUiAction: false,
            RequestsProductCommandExecution: false,
            RequestsProductCommandHandler: false,
            RequestsProductiveServiceRegistration: false,
            RequestsPhysicalExport: false,
            RequestsFileWriteOutsideApprovalStateStore: false,
            ClaimsArbitraryPathInput: false,
            ClaimsProviderCloudNetwork: false,
            ClaimsDbMigration: false,
            ClaimsKmsWormExternalTrust: false,
            ClaimsBrowserCdpWcuOcrRecipesLive: false,
            ClaimsPilotRun: false,
            ClaimsReleaseCommercial: false);

    private static ProductLedgerApprovedHandoffReportDraftRouteBody ReadyHandoffReportDraftBody() =>
        new(
            ExplicitLocalApprovedHandoffDraftScope: true,
            DevelopmentMode: true,
            LocalMode: true,
            InternalMode: true,
            ActionId: "handoff-draft-action-001",
            CandidateId: "candidate-local-handoff-001",
            ActionKind: ProductLedgerLocalApprovedHandoffReportDraftActionKind.LocalApprovedHandoffReportDraft.ToString(),
            CandidateActionKind: ProductLedgerInternalCommandKind.ViewLedgerReadiness.ToString(),
            CandidateEvidenceHash: new string('a', 64),
            CurrentEvidenceHash: new string('a', 64),
            DraftTitle: "Local Approved Handoff Report Draft",
            RedactedDraftSummary: "redacted local handoff summary",
            EvidenceReferences:
            [
                "docs/qa/product-ledger-local-approved-handoff-report-draft-implementation/report.md",
                "docs/qa/nodal-os-approved-action-execution-local-only-no-op-to-bounded-action/report.md"
            ],
            ProposedPath: null,
            ProposedCommand: null,
            ProposedUrl: null,
            ProposedProvider: null,
            ProposedDbMigration: null,
            ClaimsArbitraryPathInput: false,
            ClaimsFilesystemScan: false,
            RequestsOverwrite: false,
            RequestsUserFileWrite: false,
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

    private static ProductLedgerWorkspaceTestJailHandoffDraftRouteBody ReadyWorkspaceTestJailHandoffDraftBody(
        string predecessorDraftContentHash) =>
        new(
            ExplicitWorkspaceTestJailScope: true,
            DevelopmentMode: true,
            LocalMode: true,
            InternalMode: true,
            ActionId: "workspace-draft-action-001",
            CandidateId: "candidate-local-handoff-001",
            ActionKind: ProductLedgerLocalWorkspaceTestJailHandoffDraftActionKind.LocalWorkspaceTestJailHandoffDraftCreateOnly.ToString(),
            CandidateActionKind: ProductLedgerInternalCommandKind.ViewLedgerReadiness.ToString(),
            CandidateEvidenceHash: new string('a', 64),
            CurrentEvidenceHash: new string('a', 64),
            PredecessorDraftContentHash: predecessorDraftContentHash,
            DraftTitle: "Local Workspace Test-Jail Handoff Draft",
            RedactedDraftSummary: "redacted workspace test-jail handoff summary",
            EvidenceReferences:
            [
                "docs/qa/product-ledger-workspace-test-jail-handoff-draft-implementation/report.md",
                "docs/qa/product-ledger-local-approved-handoff-report-draft-implementation/report.md"
            ],
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
            RequestsUserSelectedPath: false,
            RequestsUserFileWrite: false,
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

    private static ProductLedgerUserWorkspaceAllowlistedHandoffDraftRouteBody ReadyUserWorkspaceAllowlistedHandoffDraftBody(
        string localApprovedHandoffDraftContentHash,
        string workspaceTestJailHandoffDraftContentHash) =>
        new(
            ExplicitUserWorkspaceAllowlistedScope: true,
            DevelopmentMode: true,
            LocalMode: true,
            InternalMode: true,
            ActionId: "user-workspace-draft-action-001",
            CandidateId: "candidate-local-handoff-001",
            ActionKind: ProductLedgerLocalUserWorkspaceAllowlistedHandoffDraftActionKind.LocalUserWorkspaceAllowlistedHandoffDraftCreateOnly.ToString(),
            CandidateActionKind: ProductLedgerInternalCommandKind.ViewLedgerReadiness.ToString(),
            CandidateEvidenceHash: new string('a', 64),
            CurrentEvidenceHash: new string('a', 64),
            LocalApprovedHandoffDraftContentHash: localApprovedHandoffDraftContentHash,
            WorkspaceTestJailHandoffDraftContentHash: workspaceTestJailHandoffDraftContentHash,
            DraftTitle: "Local User Workspace Allowlisted Handoff Draft",
            RedactedDraftSummary: "redacted user workspace allowlisted handoff summary",
            EvidenceReferences:
            [
                "docs/qa/product-ledger-user-workspace-allowlisted-handoff-draft-implementation/report.md",
                "docs/qa/product-ledger-workspace-test-jail-handoff-draft-implementation/report.md"
            ],
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
            RequestsUserSelectedPath: false,
            RequestsUserFileWrite: false,
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

    private static ProductLedgerLatestStateSnapshotRouteBody ReadyLatestStateSnapshotBody(string operatorSurfaceModelHash) =>
        new(
            ExplicitLatestStateSnapshotScope: true,
            DevelopmentMode: true,
            LocalMode: true,
            InternalMode: true,
            SnapshotId: "latest-state-snapshot-route-001",
            ActionId: "latest-state-snapshot-route-action-001",
            ActionKind: ProductLedgerLocalOperatorSurfaceLatestStateSnapshotActionKind.LocalOperatorSurfaceLatestStateSnapshotCreateOnly.ToString(),
            OperatorSurfaceModelHash: operatorSurfaceModelHash,
            EvidenceReferences:
            [
                "docs/qa/product-ledger-local-operator-surface-latest-state-snapshot-implementation/report.md"
            ],
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

    private static ProductLedgerLatestStateManifestRouteBody ReadyLatestStateManifestBody(
        string sourceSnapshotContentHash,
        string sourceSnapshotCheckpointHash) =>
        new(
            ExplicitLatestStateManifestScope: true,
            DevelopmentMode: true,
            LocalMode: true,
            InternalMode: true,
            ManifestId: "latest-state-manifest-route-001",
            ActionId: "latest-state-manifest-route-action-001",
            ActionKind: ProductLedgerLocalOperatorSurfaceLatestStateManifestActionKind.LocalOperatorSurfaceLatestStateManifestCreateOnly.ToString(),
            ExpectedSourceSnapshotContentHash: sourceSnapshotContentHash,
            ExpectedSourceSnapshotCheckpointHash: sourceSnapshotCheckpointHash,
            EvidenceReferences:
            [
                "docs/qa/product-ledger-durable-latest-state-manifest-create-only-implementation/report.md",
                "docs/qa/product-ledger-local-operator-surface-latest-state-snapshot-implementation/report.md"
            ],
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
            RequestsLatestPointer: false,
            RequestsLatestPointerOverwrite: false,
            RequestsReadPrecedence: false,
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
            ClaimsReleaseCommercial: false,
            ClaimsLiveAuthority: false,
            ClaimsProductAuthority: false,
            ClaimsComplianceCustody: false,
            ClaimsCloudBackedDurability: false);

    private static ProductLedgerApprovalExecutionRouteBody ReadyApprovalExecutionBody() =>
        new(
            ExplicitLocalOnlyNoOpExecutionScope: true,
            DevelopmentMode: true,
            LocalMode: true,
            InternalMode: true,
            ExecutionId: "approval-route-no-op-execution-001",
            CandidateActionKind: ProductLedgerInternalCommandKind.ViewLedgerReadiness.ToString(),
            CandidateEvidenceHash: new string('a', 64),
            CurrentEvidenceHash: new string('a', 64),
            EvidenceReferences:
            [
                "docs/qa/nodal-os-local-approval-execution-final-local-only-readiness-packet/report.md",
                "docs/qa/nodal-os-local-route-live-ledger-read-model-test-safe/report.md"
            ],
            RequestsBoundedAction: false,
            RequestsPublicUiAction: false,
            RequestsProductCommandExecution: false,
            RequestsProductCommandHandler: false,
            RequestsProductiveServiceRegistration: false,
            RequestsPhysicalExport: false,
            RequestsFileWriteOutsideExecutionStore: false,
            ClaimsArbitraryPathInput: false,
            ClaimsFilesystemScan: false,
            ClaimsProviderCloudNetwork: false,
            ClaimsDbMigration: false,
            ClaimsKmsWormExternalTrust: false,
            ClaimsBrowserCdpWcuOcrRecipesLive: false,
            ClaimsPilotRun: false,
            ClaimsReleaseCommercial: false);

    private static ProductLedgerBoundedApprovalExecutionRouteBody ReadyBoundedApprovalExecutionBody() =>
        new(
            ExplicitLocalBoundedActionScope: true,
            DevelopmentMode: true,
            LocalMode: true,
            InternalMode: true,
            ExecutionId: "bounded-route-execution-001",
            ActionId: "bounded-internal-completion-marker-route-001",
            ActionKind: ProductLedgerLocalBoundedApprovedActionKind.BoundedInternalCompletionMarker.ToString(),
            CandidateActionKind: ProductLedgerInternalCommandKind.ViewLedgerReadiness.ToString(),
            CandidateEvidenceHash: new string('a', 64),
            CurrentEvidenceHash: new string('a', 64),
            EvidenceReferences:
            [
                "docs/qa/nodal-os-approved-action-execution-local-only-no-op-to-bounded-action/report.md",
                "docs/qa/nodal-os-local-approval-real-operator-input-state-persistence/report.md"
            ],
            ProposedPath: null,
            ProposedCommand: null,
            ProposedUrl: null,
            RequestsPublicUiAction: false,
            RequestsProductCommandExecution: false,
            RequestsProductCommandHandler: false,
            RequestsProductiveServiceRegistration: false,
            RequestsPhysicalExport: false,
            RequestsFileWriteOutsideExecutionStore: false,
            RequestsUserFileWrite: false,
            RequestsShellOrSubprocess: false,
            ClaimsArbitraryCommandExecution: false,
            ClaimsArbitraryPathInput: false,
            ClaimsFilesystemScan: false,
            ClaimsProviderCloudNetwork: false,
            ClaimsDbMigration: false,
            ClaimsKmsWormExternalTrust: false,
            ClaimsBrowserCdpWcuOcrRecipesLive: false,
            ClaimsPilotRun: false,
            ClaimsReleaseCommercial: false);

    private static string ServerAddress(WebApplication app)
    {
        var feature = app.Services.GetRequiredService<IServer>().Features.Get<IServerAddressesFeature>();
        var address = feature?.Addresses.SingleOrDefault();
        Assert.IsFalse(string.IsNullOrWhiteSpace(address), "Server address was not assigned.");
        return address!;
    }

    private static LiveLedgerSetup CreateLiveLedger(LedgerFixture fixture)
    {
        var writer = new ProductLedgerPathLocalOnlyActiveWriter();
        var activation = writer.Activate(ReadyActivationRequest(fixture));
        var first = writer.Append(ReadyAppendRequest(activation));
        var second = writer.Append(ReadyAppendRequest(activation) with { SafePayloadHash = new string('b', 64) });

        Assert.AreEqual(ProductLedgerPathLocalOnlyActivationDecision.ActivatedLocalOnly, activation.Decision);
        Assert.AreEqual(ProductLedgerPathLocalOnlyWriterDecision.AppendedLocalOnly, first.Decision);
        Assert.AreEqual(ProductLedgerPathLocalOnlyWriterDecision.AppendedLocalOnly, second.Decision);
        return new LiveLedgerSetup(activation, first, second);
    }

    private static ProductLedgerPathLocalOnlyActivationRequest ReadyActivationRequest(LedgerFixture fixture) =>
        new(
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
            ClaimsLocalTempAsProductLedgerPath: false);

    private static ProductLedgerPathLocalOnlyAppendRequest ReadyAppendRequest(
        ProductLedgerPathLocalOnlyActivationResult activation) =>
        new(
            ActivationResult: activation,
            SafePayloadHash: new string('a', 64),
            EvidenceMetadata: new Dictionary<string, string>
            {
                ["authority"] = "local-only-policy-bound",
                ["redaction"] = "redacted-before-persistence",
                ["failure"] = "replay-rollback-evidence"
            },
            RuntimeFlagStillDefaultOff: true,
            RequestsRuntimeEnablement: false,
            RequestsProductServiceRegistration: false,
            RequestsProductCommandHandler: false,
            RequestsUiProductAction: false,
            ClaimsProviderCloudNetwork: false,
            ClaimsWormKmsExternalTrust: false,
            ClaimsDbMigration: false,
            ClaimsBrowserCdpWcuOcrRecipesLive: false,
            ClaimsReleaseCommercialReadiness: false);

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
                CandidateId: "ledger-route-live-read-001",
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

    private static string Sha256File(string path)
    {
        var hash = SHA256.HashData(File.ReadAllBytes(path));
        return Convert.ToHexString(hash).ToLowerInvariant();
    }

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

    private sealed record LiveLedgerSetup(
        ProductLedgerPathLocalOnlyActivationResult Activation,
        ProductLedgerPathLocalOnlyAppendResult First,
        ProductLedgerPathLocalOnlyAppendResult Second);

    private sealed record ProductLedgerApprovalDecisionRouteBody(
        bool ExplicitLocalOnlyStatePersistenceScope,
        string ApprovalId,
        string CandidateEvidenceHash,
        string CurrentEvidenceHash,
        string OperatorDecision,
        DateTimeOffset DecidedAtUtc,
        string OperatorClassification,
        string OperatorNote,
        IReadOnlyList<string> EvidenceReferences,
        bool RequestsPublicUiAction,
        bool RequestsProductCommandExecution,
        bool RequestsProductCommandHandler,
        bool RequestsProductiveServiceRegistration,
        bool RequestsPhysicalExport,
        bool RequestsFileWriteOutsideApprovalStateStore,
        bool ClaimsArbitraryPathInput,
        bool ClaimsProviderCloudNetwork,
        bool ClaimsDbMigration,
        bool ClaimsKmsWormExternalTrust,
        bool ClaimsBrowserCdpWcuOcrRecipesLive,
        bool ClaimsPilotRun,
        bool ClaimsReleaseCommercial);

    private sealed record ProductLedgerApprovalExecutionRouteBody(
        bool ExplicitLocalOnlyNoOpExecutionScope,
        bool DevelopmentMode,
        bool LocalMode,
        bool InternalMode,
        string ExecutionId,
        string CandidateActionKind,
        string CandidateEvidenceHash,
        string CurrentEvidenceHash,
        IReadOnlyList<string> EvidenceReferences,
        bool RequestsBoundedAction,
        bool RequestsPublicUiAction,
        bool RequestsProductCommandExecution,
        bool RequestsProductCommandHandler,
        bool RequestsProductiveServiceRegistration,
        bool RequestsPhysicalExport,
        bool RequestsFileWriteOutsideExecutionStore,
        bool ClaimsArbitraryPathInput,
        bool ClaimsFilesystemScan,
        bool ClaimsProviderCloudNetwork,
        bool ClaimsDbMigration,
        bool ClaimsKmsWormExternalTrust,
        bool ClaimsBrowserCdpWcuOcrRecipesLive,
        bool ClaimsPilotRun,
        bool ClaimsReleaseCommercial);

    private sealed record ProductLedgerBoundedApprovalExecutionRouteBody(
        bool ExplicitLocalBoundedActionScope,
        bool DevelopmentMode,
        bool LocalMode,
        bool InternalMode,
        string ExecutionId,
        string ActionId,
        string ActionKind,
        string CandidateActionKind,
        string CandidateEvidenceHash,
        string CurrentEvidenceHash,
        IReadOnlyList<string> EvidenceReferences,
        string? ProposedPath,
        string? ProposedCommand,
        string? ProposedUrl,
        bool RequestsPublicUiAction,
        bool RequestsProductCommandExecution,
        bool RequestsProductCommandHandler,
        bool RequestsProductiveServiceRegistration,
        bool RequestsPhysicalExport,
        bool RequestsFileWriteOutsideExecutionStore,
        bool RequestsUserFileWrite,
        bool RequestsShellOrSubprocess,
        bool ClaimsArbitraryCommandExecution,
        bool ClaimsArbitraryPathInput,
        bool ClaimsFilesystemScan,
        bool ClaimsProviderCloudNetwork,
        bool ClaimsDbMigration,
        bool ClaimsKmsWormExternalTrust,
        bool ClaimsBrowserCdpWcuOcrRecipesLive,
        bool ClaimsPilotRun,
        bool ClaimsReleaseCommercial);

    private sealed record ProductLedgerApprovedHandoffReportDraftRouteBody(
        bool ExplicitLocalApprovedHandoffDraftScope,
        bool DevelopmentMode,
        bool LocalMode,
        bool InternalMode,
        string ActionId,
        string CandidateId,
        string ActionKind,
        string CandidateActionKind,
        string CandidateEvidenceHash,
        string CurrentEvidenceHash,
        string DraftTitle,
        string RedactedDraftSummary,
        IReadOnlyList<string> EvidenceReferences,
        string? ProposedPath,
        string? ProposedCommand,
        string? ProposedUrl,
        string? ProposedProvider,
        string? ProposedDbMigration,
        bool ClaimsArbitraryPathInput,
        bool ClaimsFilesystemScan,
        bool RequestsOverwrite,
        bool RequestsUserFileWrite,
        bool RequestsPublicUiAction,
        bool RequestsProductCommandExecution,
        bool RequestsProductCommandHandler,
        bool RequestsProductiveServiceRegistration,
        bool RequestsShellOrSubprocess,
        bool ClaimsArbitraryCommandExecution,
        bool ClaimsProviderCloudNetwork,
        bool ClaimsDbMigration,
        bool ClaimsKmsWormExternalTrust,
        bool ClaimsBrowserCdpWcuOcrRecipesLive,
        bool ClaimsPilotRun,
        bool ClaimsReleaseCommercial);

    private sealed record ProductLedgerWorkspaceTestJailHandoffDraftRouteBody(
        bool ExplicitWorkspaceTestJailScope,
        bool DevelopmentMode,
        bool LocalMode,
        bool InternalMode,
        string ActionId,
        string CandidateId,
        string ActionKind,
        string CandidateActionKind,
        string CandidateEvidenceHash,
        string CurrentEvidenceHash,
        string PredecessorDraftContentHash,
        string DraftTitle,
        string RedactedDraftSummary,
        IReadOnlyList<string> EvidenceReferences,
        string? ProposedPath,
        string? ProposedRoot,
        string? ProposedFilename,
        string? ProposedCommand,
        string? ProposedUrl,
        string? ProposedProvider,
        string? ProposedDbMigration,
        bool ClaimsArbitraryPathInput,
        bool ClaimsFilesystemScan,
        bool RequestsOverwrite,
        bool RequestsUserSelectedPath,
        bool RequestsUserFileWrite,
        bool RequestsPublicUiAction,
        bool RequestsProductCommandExecution,
        bool RequestsProductCommandHandler,
        bool RequestsProductiveServiceRegistration,
        bool RequestsShellOrSubprocess,
        bool ClaimsArbitraryCommandExecution,
        bool ClaimsProviderCloudNetwork,
        bool ClaimsDbMigration,
        bool ClaimsKmsWormExternalTrust,
        bool ClaimsBrowserCdpWcuOcrRecipesLive,
        bool ClaimsPilotRun,
        bool ClaimsReleaseCommercial);

    private sealed record ProductLedgerUserWorkspaceAllowlistedHandoffDraftRouteBody(
        bool ExplicitUserWorkspaceAllowlistedScope,
        bool DevelopmentMode,
        bool LocalMode,
        bool InternalMode,
        string ActionId,
        string CandidateId,
        string ActionKind,
        string CandidateActionKind,
        string CandidateEvidenceHash,
        string CurrentEvidenceHash,
        string LocalApprovedHandoffDraftContentHash,
        string WorkspaceTestJailHandoffDraftContentHash,
        string DraftTitle,
        string RedactedDraftSummary,
        IReadOnlyList<string> EvidenceReferences,
        string? ProposedPath,
        string? ProposedRoot,
        string? ProposedFilename,
        string? ProposedCommand,
        string? ProposedUrl,
        string? ProposedProvider,
        string? ProposedDbMigration,
        bool ClaimsArbitraryPathInput,
        bool ClaimsFilesystemScan,
        bool RequestsOverwrite,
        bool RequestsUserSelectedPath,
        bool RequestsUserFileWrite,
        bool RequestsPublicUiAction,
        bool RequestsProductCommandExecution,
        bool RequestsProductCommandHandler,
        bool RequestsProductiveServiceRegistration,
        bool RequestsShellOrSubprocess,
        bool ClaimsArbitraryCommandExecution,
        bool ClaimsProviderCloudNetwork,
        bool ClaimsDbMigration,
        bool ClaimsKmsWormExternalTrust,
        bool ClaimsBrowserCdpWcuOcrRecipesLive,
        bool ClaimsPilotRun,
        bool ClaimsReleaseCommercial);

    private sealed record ProductLedgerLatestStateSnapshotRouteBody(
        bool ExplicitLatestStateSnapshotScope,
        bool DevelopmentMode,
        bool LocalMode,
        bool InternalMode,
        string SnapshotId,
        string ActionId,
        string ActionKind,
        string OperatorSurfaceModelHash,
        IReadOnlyList<string> EvidenceReferences,
        string? ProposedPath,
        string? ProposedRoot,
        string? ProposedFilename,
        string? ProposedCommand,
        string? ProposedUrl,
        string? ProposedProvider,
        string? ProposedDbMigration,
        bool ClaimsArbitraryPathInput,
        bool ClaimsFilesystemScan,
        bool RequestsOverwrite,
        bool RequestsLatestPointerOverwrite,
        bool RequestsUserSelectedPath,
        bool RequestsPublicUiAction,
        bool RequestsProductCommandExecution,
        bool RequestsProductCommandHandler,
        bool RequestsProductiveServiceRegistration,
        bool RequestsShellOrSubprocess,
        bool ClaimsArbitraryCommandExecution,
        bool ClaimsProviderCloudNetwork,
        bool ClaimsDbMigration,
        bool ClaimsKmsWormExternalTrust,
        bool ClaimsBrowserCdpWcuOcrRecipesLive,
        bool ClaimsPilotRun,
        bool ClaimsReleaseCommercial);

    private sealed record ProductLedgerLatestStateManifestRouteBody(
        bool ExplicitLatestStateManifestScope,
        bool DevelopmentMode,
        bool LocalMode,
        bool InternalMode,
        string ManifestId,
        string ActionId,
        string ActionKind,
        string ExpectedSourceSnapshotContentHash,
        string ExpectedSourceSnapshotCheckpointHash,
        IReadOnlyList<string> EvidenceReferences,
        string? ProposedPath,
        string? ProposedRoot,
        string? ProposedFilename,
        string? ProposedCommand,
        string? ProposedUrl,
        string? ProposedProvider,
        string? ProposedDbMigration,
        bool ClaimsArbitraryPathInput,
        bool ClaimsFilesystemScan,
        bool RequestsOverwrite,
        bool RequestsLatestPointer,
        bool RequestsLatestPointerOverwrite,
        bool RequestsReadPrecedence,
        bool RequestsUserSelectedPath,
        bool RequestsPublicUiAction,
        bool RequestsProductCommandExecution,
        bool RequestsProductCommandHandler,
        bool RequestsProductiveServiceRegistration,
        bool RequestsShellOrSubprocess,
        bool ClaimsArbitraryCommandExecution,
        bool ClaimsProviderCloudNetwork,
        bool ClaimsDbMigration,
        bool ClaimsKmsWormExternalTrust,
        bool ClaimsBrowserCdpWcuOcrRecipesLive,
        bool ClaimsPilotRun,
        bool ClaimsReleaseCommercial,
        bool ClaimsLiveAuthority,
        bool ClaimsProductAuthority,
        bool ClaimsComplianceCustody,
        bool ClaimsCloudBackedDurability);

    private sealed class BoundedApprovalExecutionFixture : IDisposable
    {
        private const string StateFileName = "product-ledger-local-bounded-approved-action.json";

        private BoundedApprovalExecutionFixture(string root)
        {
            Root = root;
            Executor = new ProductLedgerLocalBoundedApprovedActionExecutor(new ProductLedgerLocalApprovedActionExecutionStoreOptions(
                StoreRootPath: root,
                ExplicitLocalOnlyExecutionStore: true,
                AllowsArbitraryPathInput: false,
                AllowsFilesystemScan: false,
                AllowsExport: false,
                AllowsNetwork: false,
                AllowsDb: false,
                AllowsReleaseCommercial: false));
        }

        public string Root { get; }

        public string StateFilePath => Path.Combine(Root, StateFileName);

        public ProductLedgerLocalBoundedApprovedActionExecutor Executor { get; }

        public static BoundedApprovalExecutionFixture Create()
        {
            var root = Path.Combine(RepoRoot(), ".tmp-product-ledger-bounded-approval-execution-route-tests", Guid.NewGuid().ToString("N"));
            return new BoundedApprovalExecutionFixture(root);
        }

        public void Dispose()
        {
            var tempRoot = Path.Combine(RepoRoot(), ".tmp-product-ledger-bounded-approval-execution-route-tests");
            if (Directory.Exists(tempRoot))
            {
                Directory.Delete(tempRoot, recursive: true);
            }
        }
    }

    private sealed class HandoffReportDraftFixture : IDisposable
    {
        private HandoffReportDraftFixture(string root, string outputRoot)
        {
            Root = root;
            OutputRoot = outputRoot;
            Executor = new ProductLedgerLocalApprovedHandoffReportDraftExecutor(new ProductLedgerLocalApprovedHandoffReportDraftOptions(
                OutputRootPath: outputRoot,
                ExplicitLocalApprovedHandoffDraftBoundary: true,
                AllowsArbitraryPathInput: false,
                AllowsFilesystemScan: false,
                AllowsOverwrite: false,
                AllowsUserFileWrite: false,
                AllowsShellOrSubprocess: false,
                AllowsCommandExecution: false,
                AllowsNetwork: false,
                AllowsDb: false,
                AllowsKmsWormExternalTrust: false,
                AllowsReleaseCommercial: false));
        }

        public string Root { get; }

        public string OutputRoot { get; }

        public string ExpectedReadyPath =>
            Path.Combine(OutputRoot, "local-approved-handoff-draft-handoff-draft-action-001-aaaaaaaaaaaa.md");

        public ProductLedgerLocalApprovedHandoffReportDraftExecutor Executor { get; }

        public static HandoffReportDraftFixture Create()
        {
            var root = Path.Combine(RepoRoot(), ".tmp-product-ledger-handoff-draft-route-tests", Guid.NewGuid().ToString("N"));
            var outputRoot = Path.Combine(root, "docs", "test-output", "product-ledger", "approved-local-handoff-drafts");
            return new HandoffReportDraftFixture(root, outputRoot);
        }

        public void Dispose()
        {
            var tempRoot = Path.Combine(RepoRoot(), ".tmp-product-ledger-handoff-draft-route-tests");
            if (Directory.Exists(tempRoot))
            {
                Directory.Delete(tempRoot, recursive: true);
            }
        }
    }

    private sealed class WorkspaceTestJailHandoffDraftFixture : IDisposable
    {
        private WorkspaceTestJailHandoffDraftFixture(string root, string jailRoot)
        {
            Root = root;
            JailRoot = jailRoot;
            Executor = new ProductLedgerLocalWorkspaceTestJailHandoffDraftExecutor(new ProductLedgerLocalWorkspaceTestJailHandoffDraftOptions(
                WorkspaceTestJailRootPath: jailRoot,
                ExplicitWorkspaceTestJailBoundary: true,
                AllowsArbitraryPathInput: false,
                AllowsFilesystemScan: false,
                AllowsOverwrite: false,
                AllowsUserSelectedPath: false,
                AllowsShellOrSubprocess: false,
                AllowsCommandExecution: false,
                AllowsNetwork: false,
                AllowsDb: false,
                AllowsKmsWormExternalTrust: false,
                AllowsReleaseCommercial: false));
        }

        public string Root { get; }

        public string JailRoot { get; }

        public string ExpectedReadyPath =>
            Path.Combine(JailRoot, ".nodal", "product-ledger", "handoff-drafts", "workspace-test-jail-handoff-draft-workspace-draft-action-001-aaaaaaaaaaaa.md");

        public ProductLedgerLocalWorkspaceTestJailHandoffDraftExecutor Executor { get; }

        public static WorkspaceTestJailHandoffDraftFixture Create()
        {
            var root = Path.Combine(RepoRoot(), ".tmp-product-ledger-workspace-test-jail-route-tests", Guid.NewGuid().ToString("N"));
            var jailRoot = Path.Combine(root, "docs", "test-output", "product-ledger", "workspace-test-jail");
            return new WorkspaceTestJailHandoffDraftFixture(root, jailRoot);
        }

        public void Dispose()
        {
            var tempRoot = Path.Combine(RepoRoot(), ".tmp-product-ledger-workspace-test-jail-route-tests");
            if (Directory.Exists(tempRoot))
            {
                Directory.Delete(tempRoot, recursive: true);
            }
        }
    }

    private sealed class UserWorkspaceAllowlistedHandoffDraftFixture : IDisposable
    {
        private UserWorkspaceAllowlistedHandoffDraftFixture(string workspaceRoot)
        {
            WorkspaceRoot = workspaceRoot;
            Executor = new ProductLedgerLocalUserWorkspaceAllowlistedHandoffDraftExecutor(new ProductLedgerLocalUserWorkspaceAllowlistedHandoffDraftOptions(
                TrustedWorkspaceRootPath: workspaceRoot,
                WorkspaceClassification: ProductLedgerLocalUserWorkspaceAllowlistedHandoffDraftExecutor.WorkspaceClassification,
                ExplicitUserWorkspaceAllowlistedBoundary: true,
                AllowsArbitraryPathInput: false,
                AllowsFilesystemScan: false,
                AllowsOverwrite: false,
                AllowsUserSelectedPath: false,
                AllowsShellOrSubprocess: false,
                AllowsCommandExecution: false,
                AllowsNetwork: false,
                AllowsDb: false,
                AllowsKmsWormExternalTrust: false,
                AllowsReleaseCommercial: false));
        }

        public string WorkspaceRoot { get; }

        public string AllowedBoundaryRoot =>
            Path.Combine(WorkspaceRoot, "docs", "nodal-os", "handoffs");

        public string ExpectedReadyPath =>
            Path.Combine(AllowedBoundaryRoot, "user-workspace-allowlisted-handoff-draft-user-workspace-draft-action-001-aaaaaaaaaaaa.md");

        public ProductLedgerLocalUserWorkspaceAllowlistedHandoffDraftExecutor Executor { get; }

        public static UserWorkspaceAllowlistedHandoffDraftFixture Create()
        {
            var root = Path.Combine(RepoRoot(), ".tmp-product-ledger-user-workspace-allowlisted-route-tests", Guid.NewGuid().ToString("N"));
            return new UserWorkspaceAllowlistedHandoffDraftFixture(root);
        }

        public void Dispose()
        {
            var tempRoot = Path.Combine(RepoRoot(), ".tmp-product-ledger-user-workspace-allowlisted-route-tests");
            if (Directory.Exists(tempRoot))
            {
                Directory.Delete(tempRoot, recursive: true);
            }
        }
    }

    private sealed class LatestStateSnapshotFixture : IDisposable
    {
        private LatestStateSnapshotFixture(string workspaceRoot)
        {
            WorkspaceRoot = workspaceRoot;
            Executor = new ProductLedgerLocalOperatorSurfaceLatestStateSnapshotExecutor(new ProductLedgerLocalOperatorSurfaceLatestStateSnapshotOptions(
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
                AllowsReleaseCommercial: false));
        }

        public string WorkspaceRoot { get; }

        public string AllowedBoundaryRoot =>
            Path.Combine(WorkspaceRoot, "docs", "test-output", "product-ledger", "operator-surface-latest-state-snapshots");

        public ProductLedgerLocalOperatorSurfaceLatestStateSnapshotExecutor Executor { get; }

        public static LatestStateSnapshotFixture Create()
        {
            var root = Path.Combine(RepoRoot(), ".tmp-product-ledger-latest-state-snapshot-route-tests", Guid.NewGuid().ToString("N"));
            return new LatestStateSnapshotFixture(root);
        }

        public void Dispose()
        {
            var tempRoot = Path.Combine(RepoRoot(), ".tmp-product-ledger-latest-state-snapshot-route-tests");
            if (Directory.Exists(tempRoot))
            {
                Directory.Delete(tempRoot, recursive: true);
            }
        }
    }

    private sealed class LatestStateManifestFixture : IDisposable
    {
        private LatestStateManifestFixture(string workspaceRoot)
        {
            WorkspaceRoot = workspaceRoot;
            Writer = new ProductLedgerLocalOperatorSurfaceLatestStateManifestWriter(new ProductLedgerLocalOperatorSurfaceLatestStateManifestOptions(
                WorkspaceRootPath: workspaceRoot,
                ExplicitLatestStateManifestBoundary: true,
                AllowsArbitraryPathInput: false,
                AllowsFilesystemScan: false,
                AllowsOverwrite: false,
                AllowsLatestPointer: false,
                AllowsLatestPointerOverwrite: false,
                AllowsReadPrecedence: false,
                AllowsUserSelectedPath: false,
                AllowsShellOrSubprocess: false,
                AllowsCommandExecution: false,
                AllowsNetwork: false,
                AllowsDb: false,
                AllowsKmsWormExternalTrust: false,
                AllowsReleaseCommercial: false));
        }

        public string WorkspaceRoot { get; }

        public string AllowedBoundaryRoot =>
            Path.Combine(WorkspaceRoot, "docs", "test-output", "product-ledger", "operator-surface-latest-state-manifests");

        public ProductLedgerLocalOperatorSurfaceLatestStateManifestWriter Writer { get; }

        public static LatestStateManifestFixture Create()
        {
            var root = Path.Combine(RepoRoot(), ".tmp-product-ledger-latest-state-manifest-route-tests", Guid.NewGuid().ToString("N"));
            return new LatestStateManifestFixture(root);
        }

        public static LatestStateManifestFixture Create(string workspaceRoot) =>
            new(workspaceRoot);

        public void Dispose()
        {
            var tempRoot = Path.Combine(RepoRoot(), ".tmp-product-ledger-latest-state-manifest-route-tests");
            if (Directory.Exists(tempRoot))
            {
                Directory.Delete(tempRoot, recursive: true);
            }
        }
    }

    private sealed class DurableLatestStateReaderCandidateFixture : IDisposable
    {
        private DurableLatestStateReaderCandidateFixture(string workspaceRoot)
        {
            WorkspaceRoot = workspaceRoot;
            Validator = new ProductLedgerLocalDurableLatestStateReaderCandidateValidator(new ProductLedgerLocalDurableLatestStateReaderCandidateOptions(
                WorkspaceRootPath: workspaceRoot,
                ExplicitReaderCandidateBoundary: true,
                AllowsArbitraryPathInput: false,
                AllowsFilesystemScan: false,
                AllowsLatestPointer: false,
                AllowsLatestPointerOverwrite: false,
                AllowsReadPrecedence: false,
                AllowsAuthority: false,
                AllowsProductAuthority: false,
                AllowsPublicProduct: false,
                AllowsProductionRoute: false,
                AllowsShellOrSubprocess: false,
                AllowsCommandExecution: false,
                AllowsNetwork: false,
                AllowsDb: false,
                AllowsKmsWormExternalTrust: false,
                AllowsReleaseCommercial: false));
        }

        public string WorkspaceRoot { get; }

        public ProductLedgerLocalDurableLatestStateReaderCandidateValidator Validator { get; }

        public static DurableLatestStateReaderCandidateFixture Create(string workspaceRoot) =>
            new(workspaceRoot);

        public void Dispose()
        {
        }
    }

    private sealed class ApprovalExecutionFixture : IDisposable
    {
        private const string StateFileName = "product-ledger-local-approved-no-op-execution.json";

        private ApprovalExecutionFixture(string root)
        {
            Root = root;
            Executor = new ProductLedgerLocalApprovedActionNoOpExecutor(new ProductLedgerLocalApprovedActionExecutionStoreOptions(
                StoreRootPath: root,
                ExplicitLocalOnlyExecutionStore: true,
                AllowsArbitraryPathInput: false,
                AllowsFilesystemScan: false,
                AllowsExport: false,
                AllowsNetwork: false,
                AllowsDb: false,
                AllowsReleaseCommercial: false));
        }

        public string Root { get; }

        public string StateFilePath => Path.Combine(Root, StateFileName);

        public ProductLedgerLocalApprovedActionNoOpExecutor Executor { get; }

        public static ApprovalExecutionFixture Create()
        {
            var root = Path.Combine(RepoRoot(), ".tmp-product-ledger-approval-execution-route-tests", Guid.NewGuid().ToString("N"));
            return new ApprovalExecutionFixture(root);
        }

        public void Dispose()
        {
            var tempRoot = Path.Combine(RepoRoot(), ".tmp-product-ledger-approval-execution-route-tests");
            if (Directory.Exists(tempRoot))
            {
                Directory.Delete(tempRoot, recursive: true);
            }
        }
    }

    private sealed class ApprovalStateFixture : IDisposable
    {
        private const string StateFileName = "product-ledger-local-approval-state.json";

        private ApprovalStateFixture(string root)
        {
            Root = root;
            Store = new ProductLedgerLocalApprovalDecisionStateStore(new ProductLedgerLocalApprovalDecisionStateStoreOptions(
                StoreRootPath: root,
                ExplicitLocalOnlyStateStore: true,
                AllowsArbitraryPathInput: false,
                AllowsExport: false,
                AllowsNetwork: false,
                AllowsDb: false,
                AllowsReleaseCommercial: false));
        }

        public string Root { get; }

        public string StateFilePath => Path.Combine(Root, StateFileName);

        public ProductLedgerLocalApprovalDecisionStateStore Store { get; }

        public static ApprovalStateFixture Create()
        {
            var root = Path.Combine(RepoRoot(), ".tmp-product-ledger-approval-route-tests", Guid.NewGuid().ToString("N"));
            return new ApprovalStateFixture(root);
        }

        public void Dispose()
        {
            var tempRoot = Path.Combine(RepoRoot(), ".tmp-product-ledger-approval-route-tests");
            if (Directory.Exists(tempRoot))
            {
                Directory.Delete(tempRoot, recursive: true);
            }
        }
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
            var allowedRoot = Path.Combine(RepoRoot(), ".tmp-product-ledger-route-live-read-tests");
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
