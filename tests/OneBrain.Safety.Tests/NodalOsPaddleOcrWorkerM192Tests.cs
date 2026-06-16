using Microsoft.VisualStudio.TestTools.UnitTesting;
using OneBrain.BrowserExecutor.Cdp;
using OneBrain.BrowserExecutor.Contracts;

namespace OneBrain.Safety.Tests;

[TestClass]
[TestCategory("PaddleOcrLocalWorker")]
[TestCategory("PaddleOcrWorkerAdapter")]
[TestCategory("PaddleOcrExperimental")]
[TestCategory("OcrVisionActivationGate")]
[TestCategory("OcrVisionNoAuthority")]
[TestCategory("OcrVisionPrivacy")]
[TestCategory("BrowserGroundingSnapshot")]
[TestCategory("PrivatePreviewReadiness")]
[TestCategory("LocalPreviewReleaseCandidate")]
[TestCategory("NodalOsNamingAudit")]
[TestCategory("BrowserRuntimePhaseGate")]
public sealed class NodalOsPaddleOcrWorkerM192Tests
{
    private static NodalOsPaddleOcrWorkerRequest Request(
        NodalOsPixelRedactionResult? redaction = null,
        bool productionMode = false,
        bool allowFullScreen = false,
        bool allowRawPersistence = false,
        NodalOsOcrVisionSensitivity sensitivity = NodalOsOcrVisionSensitivity.Low) =>
        new(
            $"req-{Guid.NewGuid():N}",
            NodalOsPaddleOcrLocalWorkerAdapter.WorkerContractVersion,
            "auth-token",
            [],
            64,
            48,
            "en",
            redaction ?? ValidRedaction(),
            sensitivity,
            allowFullScreen,
            allowRawPersistence,
            productionMode,
            DateTimeOffset.UtcNow);

    private static NodalOsPixelRedactionResult ValidRedaction() =>
        new NodalOsPixelImageRedactor().Redact(
            new NodalOsPixelRedactionRequest(
                $"redact-{Guid.NewGuid():N}",
                SolidWhiteImage(),
                NodalOsPixelRedactionImageFormat.RawRgba32,
                64,
                48,
                new NodalOsOcrBoundingBox(0, 0, 64, 48),
                default,
                NodalOsOcrVisionSensitivity.Low,
                [],
                AllowRawPersistence: false,
                AllowFullScreen: false));

    private static byte[] SolidWhiteImage()
    {
        var bytes = new byte[64 * 48 * 4];
        for (var i = 0; i < 64 * 48; i++)
        {
            bytes[(i * 4) + 0] = 0xFF;
            bytes[(i * 4) + 1] = 0xFF;
            bytes[(i * 4) + 2] = 0xFF;
            bytes[(i * 4) + 3] = 0xFF;
        }
        return bytes;
    }

    [TestMethod]
    public void Adapter_DisabledByDefaultPolicy_IsProductionBlocked()
    {
        var adapter = new NodalOsPaddleOcrLocalWorkerAdapter();
        var policy = adapter.DefaultPolicy() with { AllowedMode = NodalOsPaddleOcrWorkerExecutionMode.Disabled };
        var response = adapter.Invoke(Request(), policy, runtimeAvailable: false);

        Assert.AreEqual(NodalOsPaddleOcrWorkerExecutionMode.BlockedForProduction, response.Mode);
        Assert.IsFalse(response.Success);
        Assert.IsFalse(response.CallsRealOcr);
        Assert.IsTrue(response.NoAuthority);
    }

    [TestMethod]
    public void Adapter_BlocksProductionMode()
    {
        var adapter = new NodalOsPaddleOcrLocalWorkerAdapter();
        var response = adapter.Invoke(Request(productionMode: true), adapter.DefaultPolicy(), runtimeAvailable: false);

        Assert.IsFalse(response.Success);
        Assert.IsTrue(response.Warnings.Any(w => w.Contains("production", StringComparison.OrdinalIgnoreCase)));
    }

    [TestMethod]
    public void Adapter_BlocksFullScreen()
    {
        var adapter = new NodalOsPaddleOcrLocalWorkerAdapter();
        var response = adapter.Invoke(Request(allowFullScreen: true), adapter.DefaultPolicy(), runtimeAvailable: false);

        Assert.IsFalse(response.Success);
        Assert.IsTrue(response.Warnings.Any(w => w.Contains("full-screen", StringComparison.OrdinalIgnoreCase)));
    }

    [TestMethod]
    public void Adapter_BlocksSensitive()
    {
        var adapter = new NodalOsPaddleOcrLocalWorkerAdapter();
        var response = adapter.Invoke(Request(sensitivity: NodalOsOcrVisionSensitivity.Credentials), adapter.DefaultPolicy(), runtimeAvailable: false);

        Assert.IsFalse(response.Success);
        Assert.IsTrue(response.Warnings.Any(w => w.Contains("sensitive", StringComparison.OrdinalIgnoreCase)));
    }

    [TestMethod]
    public void Adapter_BlocksMissingRedaction()
    {
        var adapter = new NodalOsPaddleOcrLocalWorkerAdapter();
        var badRedaction = new NodalOsPixelRedactionResult(
            $"redact-{Guid.NewGuid():N}",
            NodalOsPixelRedactionDecision.RedactionFailed,
            "", "", [], NodalOsPixelRedactionVerificationStatus.VerificationFailed,
            SafeForOcr: false, SafeForPersistence: false, OriginalRawPersisted: false,
            NoAuthority: true, [], [], new NodalOsPixelRedactionEvidence($"ev-{Guid.NewGuid():N}", "", "", false, [], "", true), true);
        var response = adapter.Invoke(Request(redaction: badRedaction), adapter.DefaultPolicy(), runtimeAvailable: false);

        Assert.IsFalse(response.Success);
        Assert.IsTrue(response.Warnings.Any(w => w.Contains("redaction", StringComparison.OrdinalIgnoreCase)));
    }

    [TestMethod]
    public void Adapter_BlocksRawPersistence()
    {
        var adapter = new NodalOsPaddleOcrLocalWorkerAdapter();
        var response = adapter.Invoke(Request(allowRawPersistence: true), adapter.DefaultPolicy(), runtimeAvailable: false);

        Assert.IsFalse(response.Success);
        Assert.IsTrue(response.Warnings.Any(w => w.Contains("raw", StringComparison.OrdinalIgnoreCase)));
    }

    [TestMethod]
    public void Adapter_RuntimeNotInstalled_BlockedCleanly()
    {
        var adapter = new NodalOsPaddleOcrLocalWorkerAdapter();
        var response = adapter.Invoke(Request(), adapter.DefaultPolicy(), runtimeAvailable: false);

        Assert.IsFalse(response.Success);
        Assert.IsTrue(response.Warnings.Any(w => w.Contains("runtime", StringComparison.OrdinalIgnoreCase) || w.Contains("installed", StringComparison.OrdinalIgnoreCase)));
        Assert.IsFalse(response.CallsRealOcr);
        Assert.IsFalse(response.CallsSaas);
    }

    [TestMethod]
    public void Adapter_HealthCheck_ReportsRuntimeNotAvailable()
    {
        var adapter = new NodalOsPaddleOcrLocalWorkerAdapter();
        var health = adapter.HealthCheck();

        Assert.IsFalse(health.RuntimeAvailable); // Python 3.13 blocks install
        Assert.IsTrue(health.WorkerScriptPresent);
        Assert.IsTrue(health.InvocationPolicyValid);
        Assert.IsTrue(health.NoAuthority);
    }

    [TestMethod]
    public void Normalizer_ProducesLocalOcrResult()
    {
        var adapter = new NodalOsPaddleOcrLocalWorkerAdapter();
        var response = adapter.Invoke(Request(), adapter.DefaultPolicy(), runtimeAvailable: false);
        var normalizer = new NodalOsPaddleOcrResultNormalizer();
        var result = normalizer.Normalize(response);

        Assert.IsNotNull(result);
        Assert.AreEqual(NodalOsOcrAuthorityFlag.NoAuthority, result.AuthorityFlag);
        Assert.IsFalse(result.CanApproveAction);
        Assert.IsFalse(result.CanClick);
        Assert.IsFalse(result.CanSubmit);
    }
}
