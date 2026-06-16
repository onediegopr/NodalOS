using Microsoft.VisualStudio.TestTools.UnitTesting;
using OneBrain.BrowserExecutor.Cdp;
using OneBrain.BrowserExecutor.Contracts;

namespace OneBrain.Safety.Tests;

[TestClass]
public sealed class BrowserRuntimeCompatibilityM70Tests
{
    [TestMethod]
    public void BrowserRuntimeCompatibilityAllowsChromeCdpPrimary()
    {
        var report = Evaluate(BrowserRuntimeProviderCompatibilityEvaluator.ChromeCdpPrimary());

        Assert.AreEqual(BrowserRuntimeProviderDecisionKind.Compatible, report.Decision.Decision);
        Assert.IsTrue(report.ChromeCdpRemainsPrimary);
    }

    [TestMethod]
    public void BrowserRuntimeCompatibilityMarksWebView2SandboxOnly()
    {
        var report = Evaluate(BrowserRuntimeProviderCompatibilityEvaluator.WebView2Sandbox());

        Assert.AreEqual(BrowserRuntimeProviderDecisionKind.SandboxOnly, report.Decision.Decision);
        Assert.IsTrue(report.EmbeddedRuntimeProductionDisabled);
    }

    [TestMethod]
    public void BrowserRuntimeCompatibilityMarksCefSandboxOnly()
    {
        var report = Evaluate(BrowserRuntimeProviderCompatibilityEvaluator.CefSandbox());

        Assert.AreEqual(BrowserRuntimeProviderDecisionKind.SandboxOnly, report.Decision.Decision);
        Assert.IsTrue(report.Provider.DesignOnly);
    }

    [TestMethod]
    public void BrowserRuntimeCompatibilityFailsAuthoritativeRuntime() =>
        AssertBlocked(BadProfile(runtimeAuthoritative: true), "runtime cannot be authoritative");

    [TestMethod]
    public void BrowserRuntimeCompatibilityFailsRuntimeCapturingBodies() =>
        AssertBlocked(BadProfile(capturesBodies: true), "runtime captures request/response bodies");

    [TestMethod]
    public void BrowserRuntimeCompatibilityFailsRuntimeExposingCookies() =>
        AssertBlocked(BadProfile(exposesCookies: true), "runtime exposes cookies/session");

    [TestMethod]
    public void BrowserRuntimeCompatibilityFailsRuntimeWithoutEvidence() =>
        AssertBlocked(BadProfile(producesEvidence: false), "runtime must produce evidence refs");

    [TestMethod]
    public void BrowserRuntimeCompatibilityFailsUnsafeDownloadUpload() =>
        AssertBlocked(BadProfile(unsafeDownloadUpload: true), "runtime allows unsafe download/upload");

    [TestMethod]
    public void BrowserRuntimeCompatibilityFailsIrreversibleActions() =>
        AssertBlocked(BadProfile(irreversible: true), "runtime allows irreversible actions");

    [TestMethod]
    public void BrowserRuntimeCompatibilityRequiresCoreAuthority() =>
        AssertBlocked(BadProfile(coreAuthorityRequired: false), "runtime cannot be authoritative");

    [TestMethod]
    public void BrowserRuntimePhaseGatePassesWithEmbeddedRuntimeReadiness()
    {
        var report = BrowserVaultMinimalM23Tests.SafeState() with
        {
            EmbeddedRuntimeArchitectureDecisionDefined = true,
            EmbeddedRuntimeSandboxPrototypeDefined = true,
            RuntimeAbstractionCompatibilityGateDefined = true,
            ChromeCdpRemainsPrimaryRuntime = true,
            EmbeddedRuntimeProductionDisabled = true
        };

        Assert.IsTrue(report.EmbeddedRuntimeCompatibilityAllowed);
    }

    [TestMethod]
    public void BrowserRuntimePhaseGateFailsWhenEmbeddedRuntimeAuthoritative()
    {
        var state = BrowserVaultMinimalM23Tests.SafeState() with
        {
            EmbeddedRuntimeArchitectureDecisionDefined = true,
            EmbeddedRuntimeSandboxPrototypeDefined = true,
            RuntimeAbstractionCompatibilityGateDefined = true,
            EmbeddedRuntimeAuthoritative = true
        };

        Assert.IsFalse(state.EmbeddedRuntimeCompatibilityAllowed);
    }

    private static BrowserRuntimeProviderCompatibilityReport Evaluate(BrowserRuntimeProvider provider) =>
        new BrowserRuntimeProviderCompatibilityEvaluator().Evaluate(provider);

    private static void AssertBlocked(BrowserRuntimeProviderSafetyProfile profile, string reason)
    {
        var report = Evaluate(BrowserRuntimeProviderCompatibilityEvaluator.Unsafe(BrowserRuntimeProviderKind.WebView2EmbeddedSandbox, profile));

        Assert.AreEqual(BrowserRuntimeProviderDecisionKind.Blocked, report.Decision.Decision);
        CollectionAssert.Contains(report.Decision.ReasonCodes.ToList(), reason);
    }

    private static BrowserRuntimeProviderSafetyProfile BadProfile(
        bool coreAuthorityRequired = true,
        bool runtimeAuthoritative = false,
        bool exposesCookies = false,
        bool capturesBodies = false,
        bool unsafeDownloadUpload = false,
        bool irreversible = false,
        bool producesEvidence = true) =>
        new(
            CoreAuthorityRequired: coreAuthorityRequired,
            RuntimeAuthoritative: runtimeAuthoritative,
            ExposesCookiesOrSession: exposesCookies,
            CapturesBodies: capturesBodies,
            CapturesSensitiveHeaderValues: false,
            AllowsUnsafeDownloadUpload: unsafeDownloadUpload,
            AllowsIrreversibleActions: irreversible,
            ProducesEvidenceRefs: producesEvidence,
            RespectsCoreFsmSafety: true,
            ProductionActive: false,
            ReplacesChromeCdpPrimary: false);
}
