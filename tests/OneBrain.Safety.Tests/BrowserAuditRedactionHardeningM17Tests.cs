using Microsoft.VisualStudio.TestTools.UnitTesting;
using OneBrain.BrowserExecutor.Cdp;
using OneBrain.BrowserExecutor.Contracts;

namespace OneBrain.Safety.Tests;

[TestClass]
public sealed class BrowserAuditRedactionHardeningM17Tests
{
    [TestMethod]
    public void BrowserNetworkCaptureDoesNotPersistOpaqueAuthorizationHeader()
    {
        var summary = CaptureHeader("authorization", "Bearer abcdef123456opaque");
        var serialized = System.Text.Json.JsonSerializer.Serialize(summary);

        AssertSensitiveHeaderPresenceOnly(summary, "authorization");
        Assert.IsFalse(serialized.Contains("abcdef123456opaque", StringComparison.OrdinalIgnoreCase));
        Assert.IsFalse(serialized.Contains("Bearer abcdef", StringComparison.OrdinalIgnoreCase));
    }

    [TestMethod]
    public void BrowserNetworkCaptureDoesNotPersistOpaqueCookieHeader()
    {
        var summary = CaptureHeader("cookie", "sessionid=a1b2c3opaque");
        var serialized = System.Text.Json.JsonSerializer.Serialize(summary);

        AssertSensitiveHeaderPresenceOnly(summary, "cookie");
        Assert.IsFalse(serialized.Contains("a1b2c3opaque", StringComparison.OrdinalIgnoreCase));
        Assert.IsFalse(serialized.Contains("sessionid=a1b2c3opaque", StringComparison.OrdinalIgnoreCase));
    }

    [TestMethod]
    public void BrowserNetworkCaptureDoesNotPersistOpaqueApiKeyHeader()
    {
        var summary = CaptureHeader("x-api-key", "opaquesecretvalue");
        var serialized = System.Text.Json.JsonSerializer.Serialize(summary);

        AssertSensitiveHeaderPresenceOnly(summary, "x-api-key");
        Assert.IsFalse(serialized.Contains("opaquesecretvalue", StringComparison.OrdinalIgnoreCase));
    }

    [TestMethod]
    public void BrowserNetworkCaptureNeverStoresSensitiveHeaderValuesEvenWhenPolicyRequestsThem()
    {
        var policy = new BrowserNetworkCapturePolicy(BrowserNetworkCaptureMode.MetadataOnly, CaptureSensitiveHeaderPresenceOnly: true, AllowDirectHttpReplay: false, new HashSet<string> { "GET" });
        var raw = EventWithHeader("proxy-authorization", "Bearer abcdef123456opaque");

        var summary = new BrowserNetworkCapture().Capture(policy, [raw]);

        AssertSensitiveHeaderPresenceOnly(summary, "proxy-authorization");
    }

    [TestMethod]
    public void BrowserNetworkCaptureBodiesAreUnsupportedByContract()
    {
        var policy = Policy();

        Assert.AreEqual(BrowserNetworkCaptureMode.MetadataOnly, policy.Mode);
        Assert.IsFalse(policy.BodiesCaptureSupported);
        Assert.IsFalse(policy.RequestBodyCaptureSupported);
        Assert.IsFalse(policy.ResponseBodyCaptureSupported);
    }

    [TestMethod]
    public void BrowserNetworkCaptureCannotEnableRequestOrResponseBodies()
    {
        var raw = EventWithHeader("content-type", "application/json") with { RequestBodyCaptured = true, ResponseBodyCaptured = true };

        var summary = new BrowserNetworkCapture().Capture(Policy(), [raw]);

        Assert.IsTrue(summary.IsSafe);
        Assert.IsFalse(summary.Events.Single().RequestBodyCaptured);
        Assert.IsFalse(summary.Events.Single().ResponseBodyCaptured);
    }

    [TestMethod]
    public void BrowserNetworkCaptureFailsIfBodyCaptureIsRequestedInRawEvent()
    {
        var raw = EventWithHeader("content-type", "application/json") with { RequestBodyCaptured = true };

        Assert.IsFalse(raw.Validate().IsValid);
    }

    [TestMethod]
    public void BrowserAuditLedgerVerifiesValidHmacChain()
    {
        using var temp = TempDir();
        var ledger = Ledger(temp.Path);
        ledger.Append(AuditEvent("one"));
        ledger.Append(AuditEvent("two"));
        var seal = ledger.HeadSeal;

        Assert.IsTrue(ledger.VerifyIntegrity(seal));
    }

    [TestMethod]
    public void BrowserAuditLedgerDetectsTamperedEvent()
    {
        using var temp = TempDir();
        var ledger = Ledger(temp.Path);
        var original = ledger.Append(AuditEvent("one"));
        var tampered = original with { Reason = "tampered" };
        var provider = BrowserAuditLedgerHmacIntegrityProvider.CreateDevFixtureProvider();

        Assert.IsFalse(provider.VerifyEventIntegrity(tampered));
    }

    [TestMethod]
    public void BrowserAuditLedgerDetectsRecomputedShaWithoutHmac()
    {
        using var temp = TempDir();
        var ledger = Ledger(temp.Path);
        var original = ledger.Append(AuditEvent("one"));
        var tampered = original with { Reason = "tampered" };
        var shaOnly = tampered with { Integrity = tampered.Integrity with { EventHash = BrowserAuditLedgerEvent.ComputeHash(tampered) } };
        var provider = BrowserAuditLedgerHmacIntegrityProvider.CreateDevFixtureProvider();

        Assert.IsFalse(provider.VerifyEventIntegrity(shaOnly));
    }

    [TestMethod]
    public void BrowserAuditLedgerDetectsTruncatedTail()
    {
        using var temp = TempDir();
        var ledger = Ledger(temp.Path);
        ledger.Append(AuditEvent("one"));
        ledger.Append(AuditEvent("two"));
        var seal = ledger.HeadSeal;
        var truncated = ledger.Events.Take(1).ToArray();
        var provider = BrowserAuditLedgerHmacIntegrityProvider.CreateDevFixtureProvider();

        Assert.IsFalse(provider.VerifyHeadSeal(seal, truncated));
    }

    [TestMethod]
    public void BrowserAuditLedgerDetectsWrongEventCountAndSequence()
    {
        using var temp = TempDir();
        var ledger = Ledger(temp.Path);
        ledger.Append(AuditEvent("one"));
        var seal = ledger.HeadSeal;
        var provider = BrowserAuditLedgerHmacIntegrityProvider.CreateDevFixtureProvider();

        Assert.IsFalse(provider.VerifyHeadSeal(seal with { EventCount = 2 }, ledger.Events));
        Assert.IsFalse(provider.VerifyHeadSeal(seal with { LastSequence = 99 }, ledger.Events));
    }

    [TestMethod]
    public void BrowserAuditLedgerCanonicalSerializationIsStable()
    {
        var first = AuditEvent("stable").WithIntegrity(1, "genesis");
        var second = first with { Metadata = new Dictionary<string, string>(first.Metadata.Reverse(), StringComparer.Ordinal) };

        Assert.AreEqual(BrowserAuditLedgerEvent.ToCanonicalString(first), BrowserAuditLedgerEvent.ToCanonicalString(second));
    }

    [TestMethod]
    public void BrowserRedactorDoesNotRedactHostnamesOrFilenamesAsJwt()
    {
        var values = new[]
        {
            "www.google.com",
            "listado.mercadolibre.com.ar",
            "report.final.pdf",
            "invoice.final.v2.pdf"
        };

        foreach (var value in values)
        {
            Assert.AreEqual(value, BrowserCredentialRedactor.Redact(value));
            Assert.IsFalse(BrowserCredentialRedactor.ContainsSecret(value));
        }
    }

    [TestMethod]
    public void BrowserRedactorDoesNotRedactPlainSevenDigitIdsWithoutContext()
    {
        Assert.AreEqual("order id 12345678", BrowserCredentialRedactor.Redact("order id 12345678"));
        Assert.AreEqual("count 1234567", BrowserCredentialRedactor.Redact("count 1234567"));
    }

    [TestMethod]
    public void BrowserRedactorRedactsJwtWithDecodableHeader()
    {
        var jwt = SyntheticJwt();

        Assert.AreEqual(BrowserCredentialRedactor.Redacted, BrowserCredentialRedactor.Redact(jwt));
        Assert.IsTrue(BrowserCredentialRedactor.ContainsSecret(jwt));
    }

    [TestMethod]
    public void BrowserRedactorRedactsAccessRefreshIdTokensAndBearerOpaqueToken()
    {
        var raw = $"authorization: Bearer abcdef123456opaque access_token=opaque refresh_token=opaque id_token={SyntheticJwt()} api_key=opaque";
        var redacted = BrowserCredentialRedactor.Redact(raw);

        Assert.IsFalse(redacted.Contains("abcdef123456opaque", StringComparison.OrdinalIgnoreCase));
        Assert.IsFalse(redacted.Contains("opaque", StringComparison.OrdinalIgnoreCase));
        Assert.IsFalse(redacted.Contains(SyntheticJwt(), StringComparison.OrdinalIgnoreCase));
        Assert.IsTrue(BrowserCredentialRedactor.ContainsSecret(raw));
    }

    private static BrowserNetworkCaptureSummary CaptureHeader(string name, string value) =>
        new BrowserNetworkCapture().Capture(Policy(), [EventWithHeader(name, value)]);

    private static void AssertSensitiveHeaderPresenceOnly(BrowserNetworkCaptureSummary summary, string name)
    {
        Assert.IsTrue(summary.IsSafe);
        var header = summary.Events.Single().ResponseHeaders.Single(h => string.Equals(h.HeaderName, name, StringComparison.OrdinalIgnoreCase));
        Assert.IsTrue(header.Present);
        Assert.IsFalse(header.ValueCaptured);
        Assert.AreEqual("[NOT_CAPTURED]", header.Value);
        Assert.AreEqual(BrowserNetworkHeaderRedactionReason.SensitiveHeaderValueNotCaptured, header.RedactionReason);
    }

    private static BrowserNetworkCapturePolicy Policy() =>
        new(BrowserNetworkCaptureMode.MetadataOnly, CaptureSensitiveHeaderPresenceOnly: true, AllowDirectHttpReplay: false, new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "GET", "POST" });

    private static BrowserNetworkCaptureEvent EventWithHeader(string name, string value) =>
        new("request-1", "corr-1", "GET", "https://fixture.local/api?token=synthetic", 200, "fetch", TimeSpan.FromMilliseconds(1), [new BrowserNetworkHeaderMetadata(name, true, true, value, BrowserNetworkHeaderRedactionReason.None)], ApiCandidate: true, RequestBodyCaptured: false, ResponseBodyCaptured: false, Redacted: false);

    private static BrowserPersistentAuditLedger Ledger(string path) =>
        new(new BrowserAuditLedgerPolicy(path, AllowFilePersistence: true, RedactBeforePersist: true, new BrowserAuditLedgerRetentionPolicy(null, null, DeleteOnCleanup: true)), BrowserAuditLedgerHmacIntegrityProvider.CreateDevFixtureProvider());

    private static BrowserAuditLedgerEvent AuditEvent(string reason) =>
        BrowserPersistentAuditLedger.Create(BrowserAuditLedgerEventKind.PolicyBlocked, "run-1", "action-1", "corr-1", "profile-1", "session-1", null, null, null, "Blocked", reason, new Dictionary<string, string>
        {
            ["b"] = "2",
            ["a"] = "1"
        });

    private static string SyntheticJwt() =>
        "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJzdWIiOiJzeW50aGV0aWMifQ.c2lnbmF0dXJlMTIzNDU2";

    private static TempDirectory TempDir() => new();

    private sealed class TempDirectory : IDisposable
    {
        public TempDirectory()
        {
            Path = System.IO.Path.Combine(System.IO.Path.GetTempPath(), $"onebrain-m17-{Guid.NewGuid():N}");
            Directory.CreateDirectory(Path);
        }

        public string Path { get; }

        public void Dispose()
        {
            if (Directory.Exists(Path))
                Directory.Delete(Path, recursive: true);
        }
    }
}
