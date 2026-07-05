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

    public TestContext TestContext { get; set; } = null!;

    private static WebApplication BuildLocalOnlyApp(
        string environmentName,
        ProductLedgerOperatorSurfaceReadModelSource? readModelSource = null)
    {
        var builder = WebApplication.CreateBuilder(new WebApplicationOptions
        {
            EnvironmentName = environmentName
        });

        builder.WebHost.UseUrls("http://127.0.0.1:0");
        var app = builder.Build();
        app.MapProductLedgerLocalDevRoutePreview(
            app.Environment,
            readModelSource ?? ProductLedgerOperatorSurfaceReadModelSource.FixtureSafe);
        return app;
    }

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
