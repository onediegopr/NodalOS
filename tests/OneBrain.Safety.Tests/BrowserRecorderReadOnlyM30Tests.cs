using Microsoft.VisualStudio.TestTools.UnitTesting;
using OneBrain.BrowserExecutor.Cdp;
using OneBrain.BrowserExecutor.Contracts;

namespace OneBrain.Safety.Tests;

[TestClass]
public sealed class BrowserRecorderReadOnlyM30Tests
{
    [TestMethod]
    [TestCategory("BrowserRecorderReadOnly")]
    public void BrowserRecorderReadOnlyPrototypeCapturesLocalSandboxDraft()
    {
        RequireRecorderReadOnly();
        var result = Capture();

        Assert.IsTrue(result.Draft.IsSafeDraft);
    }

    [TestMethod]
    public void BrowserRecorderReadOnlyDoesNotStoreSecrets()
    {
        var result = Capture(Observation(new Uri("http://127.0.0.1/page-a?access_token=opaque"), rawBody: "password=secret"));

        Assert.IsTrue(result.SecretsRemoved);
        Assert.IsFalse(result.Draft.ToString()!.Contains("opaque", StringComparison.Ordinal));
        Assert.IsFalse(result.Draft.ToString()!.Contains("password=secret", StringComparison.OrdinalIgnoreCase));
    }

    [TestMethod]
    public void BrowserRecorderReadOnlyDoesNotStoreCookies()
    {
        var result = Capture(Observation(new Uri("http://127.0.0.1/page-a"), rawCookie: "session=opaque"));

        Assert.IsTrue(result.CookiesRemoved);
        Assert.IsFalse(result.Draft.Steps.Any(s => s.StoresCookie));
        Assert.IsFalse(result.Draft.ToString()!.Contains("session=opaque", StringComparison.OrdinalIgnoreCase));
    }

    [TestMethod]
    public void BrowserRecorderReadOnlyDoesNotStoreBodies()
    {
        var result = Capture(Observation(new Uri("http://127.0.0.1/page-a"), rawBody: "body content"));

        Assert.IsTrue(result.BodiesRemoved);
        Assert.IsFalse(result.Draft.Steps.Any(s => s.StoresBody));
    }

    [TestMethod]
    public void BrowserRecorderReadOnlyDoesNotStoreFullLocalPaths()
    {
        var result = Capture(Observation(new Uri("http://127.0.0.1/page-a"), fullLocalPath: "C:\\Users\\diego\\secret.txt"));

        Assert.IsTrue(result.FullPathsRemoved);
        Assert.IsFalse(result.Draft.ToString()!.Contains("C:\\Users\\diego", StringComparison.OrdinalIgnoreCase));
    }

    [TestMethod]
    public void BrowserRecorderReadOnlyRedactsOpaqueQueryStrings()
    {
        var result = Capture(Observation(new Uri("http://127.0.0.1/page-a?id_token=opaque")));

        Assert.IsFalse(result.Draft.ToString()!.Contains("id_token", StringComparison.OrdinalIgnoreCase));
        Assert.AreEqual("http://127.0.0.1/page-a", result.Draft.Steps.Single().Target.SafeUrl);
    }

    [TestMethod]
    public void BrowserRecorderReadOnlyKeepsSemanticTargets()
    {
        var result = Capture();

        Assert.AreEqual("Dashboard Link", result.Draft.Steps.Single().Target.SemanticLabel);
    }

    [TestMethod]
    public void BrowserRecorderReadOnlyCreatesVerificationCandidates()
    {
        var result = Capture();

        Assert.IsTrue(result.Draft.Steps.All(s => s.VerificationCandidate.Required));
    }

    [TestMethod]
    public void BrowserRecorderReadOnlyMarksFormsAsRisky()
    {
        var result = Capture(Observation(new Uri("http://127.0.0.1/form-readonly"), hasForm: true));

        Assert.AreEqual(BrowserRecorderRiskAssessment.Risky, result.Draft.Steps.Single().Risk);
    }

    [TestMethod]
    public void BrowserRecorderReadOnlyBlocksSubmitCapture()
    {
        var result = Capture(Observation(new Uri("http://127.0.0.1/form-readonly"), hasForm: true, hasSubmit: true));

        Assert.AreEqual(BrowserRecordedActionKind.Submit, result.Draft.Steps.Single().ActionKind);
        Assert.IsFalse(result.Draft.Steps.Single().IsSafeReadOnly);
    }

    [TestMethod]
    public void BrowserRecorderReadOnlyDraftIsNotExecutableByDefault()
    {
        var result = Capture();

        Assert.IsFalse(result.Draft.ExecutableByDefault);
    }

    [TestMethod]
    public void BrowserRuntimePhaseGateAllowsRecorderReadOnlyPrototype()
    {
        using var temp = BrowserVaultMinimalM23Tests.TempDir();
        var report = BrowserSafeDownloadM26TestAccess.PhaseReport(temp.Path, BrowserVaultMinimalM23Tests.SafeState(vaultMinimal: true) with { RecorderState = BrowserRuntimeRecorderState.ReadOnlyPrototypeActive });

        Assert.IsTrue(report.Passed, string.Join("; ", report.FailedChecks));
    }

    [TestMethod]
    public void BrowserRuntimePhaseGateFailsRecorderProductive()
    {
        using var temp = BrowserVaultMinimalM23Tests.TempDir();
        var report = BrowserSafeDownloadM26TestAccess.PhaseReport(temp.Path, BrowserVaultMinimalM23Tests.SafeState(vaultMinimal: true) with { RecorderState = BrowserRuntimeRecorderState.ProductiveActive });

        Assert.AreEqual(BrowserRuntimePhaseCloseStatus.Failed, report.Status);
        CollectionAssert.Contains(report.FailedChecks.ToList(), "recorder design-only");
    }

    private static BrowserRecorderSanitizationResult Capture(BrowserRecorderObservation? observation = null)
    {
        var recorder = new BrowserRecorderReadOnlyPrototype();
        var session = recorder.Start(new BrowserRecorderStartRequest("run-recorder", new Uri("http://127.0.0.1/page-a"), Set("127.0.0.1")));
        return recorder.Capture(session, [observation ?? Observation(new Uri("http://127.0.0.1/page-a"))]);
    }

    private static BrowserRecorderObservation Observation(Uri uri, bool hasForm = false, bool hasSubmit = false, string? rawCookie = null, string? rawBody = null, string? fullLocalPath = null) =>
        new(uri, "Dashboard", "Visible dashboard text", ["Dashboard Link"], hasForm, hasSubmit, HasDownloadLink: true, rawCookie, rawBody, fullLocalPath);

    private static IReadOnlySet<string> Set(params string[] values) =>
        new HashSet<string>(values, StringComparer.OrdinalIgnoreCase);

    private static void RequireRecorderReadOnly()
    {
        if (!string.Equals(Environment.GetEnvironmentVariable("ONEBRAIN_RUN_RECORDER_READONLY_TESTS"), "1", StringComparison.Ordinal))
            Assert.Inconclusive("Recorder read-only tests are opt-in.");
    }
}

