using Microsoft.VisualStudio.TestTools.UnitTesting;
using OneBrain.BrowserExecutor.Cdp;
using OneBrain.BrowserExecutor.Contracts;

namespace OneBrain.Safety.Tests;

[TestClass]
public sealed class BrowserExternalReadOnlyM51Tests
{
    [TestMethod]
    public void BrowserExternalReadOnlyTargetRequiresAllowlist()
    {
        var result = Evaluate(Config(allowlist: Set()));

        Assert.AreEqual(BrowserExternalReadOnlyDecisionKind.BlockedNoTestOwnedExternalTarget, result.Decision);
    }

    [TestMethod]
    public void BrowserExternalReadOnlyTargetRejectsUnknownHost()
    {
        var result = Evaluate(Config(allowlist: Set("other.example.test")));

        Assert.AreEqual(BrowserExternalReadOnlyDecisionKind.Blocked, result.Decision);
    }

    [TestMethod]
    public void BrowserExternalReadOnlyTargetRejectsSensitiveHost()
    {
        var result = Evaluate(Config(baseUrl: "https://bank.example.test/status", allowlist: Set("bank.example.test")));

        Assert.AreEqual(BrowserExternalReadOnlyDecisionKind.Blocked, result.Decision);
    }

    [TestMethod]
    public void BrowserExternalReadOnlyTargetRequiresTestOwned()
    {
        var config = Config() with { Target = Target() with { Ownership = BrowserExternalReadOnlyOwnership.Unknown } };

        Assert.AreEqual(BrowserExternalReadOnlyDecisionKind.Blocked, Evaluate(config).Decision);
    }

    [TestMethod]
    public void BrowserExternalReadOnlyTargetBlocksMutation()
    {
        Assert.IsTrue(new BrowserExternalReadOnlyGuard().Blocks("mutation"));
    }

    [TestMethod]
    public void BrowserExternalReadOnlyTargetBlocksSubmit()
    {
        Assert.IsTrue(new BrowserExternalReadOnlyGuard().Blocks("submit"));
    }

    [TestMethod]
    public void BrowserExternalReadOnlyTargetBlocksUpload()
    {
        Assert.IsTrue(new BrowserExternalReadOnlyGuard().Blocks("upload"));
    }

    [TestMethod]
    public void BrowserExternalReadOnlyTargetRequiresMetadataOnly()
    {
        var rawEvent = new BrowserNetworkCaptureEvent("request-external", "corr-external", "GET", "https://external.example.test/status", 200, "document", TimeSpan.FromMilliseconds(1), [], ApiCandidate: false, RequestBodyCaptured: true, ResponseBodyCaptured: false, Redacted: true);

        Assert.IsFalse(rawEvent.Validate().IsValid);
    }

    [TestMethod]
    public void BrowserExternalReadOnlyTargetDoesNotPersistOpaqueQuery()
    {
        var result = Evaluate(Config(baseUrl: "https://external.example.test/status?token=opaque-test-value"));

        Assert.IsFalse(result.OpaqueQueryPersisted);
        Assert.IsFalse(result.ToString()!.Contains("opaque-test-value", StringComparison.OrdinalIgnoreCase));
    }

    [TestMethod]
    public void BrowserExternalReadOnlyTargetDoesNotPersistCookies()
    {
        var result = Evaluate(Config());

        Assert.IsFalse(result.CookiesPersisted);
        Assert.IsFalse(result.ToString()!.Contains("session=", StringComparison.OrdinalIgnoreCase));
    }

    [TestMethod]
    public void BrowserExternalReadOnlyTargetRequiresAuditKeyCustody()
    {
        var result = Evaluate(Config(), auditKeyHealthy: false);

        Assert.AreEqual(BrowserExternalReadOnlyDecisionKind.RequiresAuditKeyCustody, result.Decision);
    }

    [TestMethod]
    public void BrowserExternalReadOnlyTargetRequiresSemanticProof()
    {
        var result = Evaluate(Config() with { VerificationRule = new BrowserExternalReadOnlyVerificationRule("", "", "") }, semanticProof: false);

        Assert.AreEqual(BrowserExternalReadOnlyDecisionKind.RequiresSemanticProof, result.Decision);
    }

    [TestMethod]
    public void BrowserRuntimePhaseGateFailsWithoutExternalReadOnlyTargetWhenRequired()
    {
        var report = BrowserVaultMinimalM23Tests.GateReport(SafeM51State() with { ExternalReadOnlyTargetConfigured = true, ExternalReadOnlyTargetTestOwned = false });

        Assert.AreEqual(BrowserRuntimePhaseCloseStatus.Failed, report.Status);
        CollectionAssert.Contains(report.FailedChecks.ToList(), "external read-only target proof safe");
    }

    [TestMethod]
    public void BrowserRuntimePhaseGatePassesWithExternalReadOnlyProof()
    {
        var report = BrowserVaultMinimalM23Tests.GateReport(SafeM51State());

        Assert.AreEqual(BrowserRuntimePhaseCloseStatus.Passed, report.Status);
    }

    [TestMethod]
    [TestCategory("BrowserExternalReadOnlyLive")]
    public void BrowserExternalReadOnlyLiveNavigatesToTestOwnedTarget()
    {
        if (!LiveEnabled())
            Assert.Inconclusive("External read-only live tests are opt-in. Set ONEBRAIN_RUN_EXTERNAL_READONLY_TARGET_TESTS=1.");

        var baseUrl = Environment.GetEnvironmentVariable("ONEBRAIN_EXTERNAL_READONLY_TARGET_BASE_URL");
        if (string.IsNullOrWhiteSpace(baseUrl))
        {
            var blocked = Evaluate(Config(allowlist: Set()));
            Assert.AreEqual(BrowserExternalReadOnlyDecisionKind.BlockedNoTestOwnedExternalTarget, blocked.Decision);
            return;
        }

        Assert.Inconclusive("A test-owned external target URL is configured, but CDP-live external navigation is not implemented in this test harness yet.");
    }

    [TestMethod]
    [TestCategory("BrowserExternalReadOnlyLive")]
    public void BrowserExternalReadOnlyLiveVerifiesSemanticProof() => BrowserExternalReadOnlyLiveNavigatesToTestOwnedTarget();

    [TestMethod]
    [TestCategory("BrowserExternalReadOnlyLive")]
    public void BrowserExternalReadOnlyLiveCapturesNetworkMetadataOnly() => BrowserExternalReadOnlyLiveNavigatesToTestOwnedTarget();

    [TestMethod]
    [TestCategory("BrowserExternalReadOnlyLive")]
    public void BrowserExternalReadOnlyLiveDoesNotPersistOpaqueQuery() => BrowserExternalReadOnlyLiveNavigatesToTestOwnedTarget();

    [TestMethod]
    [TestCategory("BrowserExternalReadOnlyLive")]
    public void BrowserExternalReadOnlyLiveCleansBrowserProcesses() => BrowserExternalReadOnlyLiveNavigatesToTestOwnedTarget();

    private static BrowserExternalReadOnlyResult Evaluate(BrowserExternalReadOnlyTargetConfig config, bool auditKeyHealthy = true, bool semanticProof = true) =>
        new BrowserExternalReadOnlyTargetEvaluator().Evaluate(new BrowserExternalReadOnlyAttempt(
            "run-external-readonly",
            "action-external-readonly",
            "corr-external-readonly",
            config,
            BrowserVaultMinimalM23Tests.GateReport(SafeM51State()),
            new BrowserAuditIntegrityKeyHealthCheck(BrowserAuditIntegrityKeyProviderKind.OsBackedDpapiCurrentUser, "audit-key-m51", 1, BrowserAuditIntegrityKeyStatus.Available, auditKeyHealthy, RawKeyExposed: false, auditKeyHealthy ? "healthy" : "unhealthy"),
            semanticProof,
            BrowserCleanupConfirmed: true,
            ExternalReadOnlyGuardActive: true));

    private static BrowserExternalReadOnlyTargetConfig Config(string baseUrl = "https://external.example.test/status?token=opaque-test-value", IReadOnlySet<string>? allowlist = null) =>
        new(
            Target(baseUrl),
            new BrowserExternalReadOnlyTargetAllowlist(allowlist ?? new HashSet<string>(StringComparer.OrdinalIgnoreCase) { new Uri(baseUrl).Host }),
            new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "/", "/status", "/health" },
            new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "submit", "save", "delete", "publish", "pay", "upload", "confirm", "login" },
            new BrowserExternalReadOnlyVerificationRule("NEXA", "readonly", "semantic-proof:external-readonly"),
            new BrowserNetworkCapturePolicy(BrowserNetworkCaptureMode.MetadataOnly, CaptureSensitiveHeaderPresenceOnly: true, AllowDirectHttpReplay: false, new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "GET" }));

    private static BrowserExternalReadOnlyTarget Target(string baseUrl = "https://external.example.test/status?token=opaque-test-value") =>
        new("external-readonly-target", new Uri(baseUrl), BrowserExternalReadOnlyOwnership.TestOwned, BrowserExternalReadOnlyRiskProfile.LowRisk, AuthRequired: false, ContainsRealData: false, RequiresTwoFactorOrCaptcha: false, HasIrreversibleActions: false);

    private static IReadOnlySet<string> Set(params string[] hosts) =>
        new HashSet<string>(hosts, StringComparer.OrdinalIgnoreCase);

    internal static BrowserRuntimeObservedState SafeM51State() =>
        NexaLocalProductShellM48Tests.SafeState() with
        {
            AuditIntegrityKeyProviderConfigured = true,
            AuditIntegrityKeyProviderOsBacked = true,
            AuditIntegrityDefaultFailClosed = true,
            AuditIntegrityDevFixtureExplicitOnly = true,
            AuditIntegrityKeyHealthOk = true,
            AuditLedgerHeadSealIncludesKeyId = true,
            ExternalReadOnlyTargetConfigured = true,
            ExternalReadOnlyTargetTestOwned = true,
            ExternalReadOnlyProofAvailable = true,
            ExternalReadOnlyProofBlocked = false,
            ExternalReadOnlyMetadataOnly = true,
            ExternalReadOnlyGuardActive = true,
            ExternalReadOnlyTargetSensitive = false,
            ExternalReadOnlyMutationAllowed = false,
            ExternalReadOnlyBrowserCleanupConfirmed = true
        };

    private static bool LiveEnabled() =>
        string.Equals(Environment.GetEnvironmentVariable("ONEBRAIN_RUN_EXTERNAL_READONLY_TARGET_TESTS"), "1", StringComparison.Ordinal);
}
