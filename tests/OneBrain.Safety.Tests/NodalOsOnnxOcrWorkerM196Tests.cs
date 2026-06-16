using Microsoft.VisualStudio.TestTools.UnitTesting;
using OneBrain.BrowserExecutor.Cdp;
using OneBrain.BrowserExecutor.Contracts;

namespace OneBrain.Safety.Tests;

[TestClass]
[TestCategory("OnnxOcrWorker")]
[TestCategory("OnnxOcrWorkerReadiness")]
[TestCategory("OnnxOcrModelMissing")]
[TestCategory("OnnxOcrNoAuthority")]
[TestCategory("OcrVisionActivationGate")]
[TestCategory("OcrVisionNoAuthority")]
[TestCategory("OcrVisionPrivacy")]
[TestCategory("BrowserGroundingSnapshot")]
[TestCategory("PrivatePreviewReadiness")]
[TestCategory("LocalPreviewReleaseCandidate")]
[TestCategory("NodalOsNamingAudit")]
[TestCategory("BrowserRuntimePhaseGate")]
public sealed class NodalOsOnnxOcrWorkerM196Tests
{
    private static NodalOsOnnxOcrWorkerRequest Request(
        NodalOsPixelRedactionResult? redaction = null,
        bool productionMode = false,
        bool allowFullScreen = false,
        bool allowRawPersistence = false,
        NodalOsOcrVisionSensitivity sensitivity = NodalOsOcrVisionSensitivity.Low) =>
        new(
            $"req-{Guid.NewGuid():N}",
            NodalOsOnnxOcrWorker.WorkerContractVersion,
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

    private static NodalOsOnnxOcrModelSet MissingModelSet() =>
        new(
            $"modelset-{Guid.NewGuid():N}",
            DetectionModelPath: null,
            RecognitionModelPath: null,
            ClassificationModelPath: null,
            AllRequiredPresent: false,
            AllVerified: false,
            NoAuthority: true);

    [TestMethod]
    public void Worker_DisabledByProductionMode()
    {
        var worker = new NodalOsOnnxOcrWorker();
        var response = worker.Invoke(Request(productionMode: true), MissingModelSet(), productionEnabled: false);

        Assert.AreEqual(NodalOsOnnxOcrWorkerExecutionMode.BlockedForProduction, response.Mode);
        Assert.IsFalse(response.Success);
        Assert.IsFalse(response.CallsRealOcr);
    }

    [TestMethod]
    public void Worker_BlocksFullScreen()
    {
        var worker = new NodalOsOnnxOcrWorker();
        var response = worker.Invoke(Request(allowFullScreen: true), MissingModelSet(), productionEnabled: false);

        Assert.IsFalse(response.Success);
        Assert.IsTrue(response.Warnings.Any(w => w.Contains("full-screen", StringComparison.OrdinalIgnoreCase)));
    }

    [TestMethod]
    public void Worker_BlocksSensitive()
    {
        var worker = new NodalOsOnnxOcrWorker();
        var response = worker.Invoke(Request(sensitivity: NodalOsOcrVisionSensitivity.Credentials), MissingModelSet(), productionEnabled: false);

        Assert.IsFalse(response.Success);
        Assert.IsTrue(response.Warnings.Any(w => w.Contains("sensitive", StringComparison.OrdinalIgnoreCase)));
    }

    [TestMethod]
    public void Worker_BlocksMissingRedaction()
    {
        var worker = new NodalOsOnnxOcrWorker();
        var badRedaction = new NodalOsPixelRedactionResult(
            $"redact-{Guid.NewGuid():N}",
            NodalOsPixelRedactionDecision.RedactionFailed,
            "", "", [], NodalOsPixelRedactionVerificationStatus.VerificationFailed,
            SafeForOcr: false, SafeForPersistence: false, OriginalRawPersisted: false,
            NoAuthority: true, [], [], new NodalOsPixelRedactionEvidence($"ev-{Guid.NewGuid():N}", "", "", false, [], "", true), true);
        var response = worker.Invoke(Request(redaction: badRedaction), MissingModelSet(), productionEnabled: false);

        Assert.IsFalse(response.Success);
        Assert.IsTrue(response.Warnings.Any(w => w.Contains("redaction", StringComparison.OrdinalIgnoreCase)));
    }

    [TestMethod]
    public void Worker_BlocksRawPersistence()
    {
        var worker = new NodalOsOnnxOcrWorker();
        var response = worker.Invoke(Request(allowRawPersistence: true), MissingModelSet(), productionEnabled: false);

        Assert.IsFalse(response.Success);
        Assert.IsTrue(response.Warnings.Any(w => w.Contains("raw", StringComparison.OrdinalIgnoreCase)));
    }

    [TestMethod]
    public void Worker_ModelMissing_WhenModelsAbsent()
    {
        var worker = new NodalOsOnnxOcrWorker();
        var response = worker.Invoke(Request(), MissingModelSet(), productionEnabled: false);

        Assert.AreEqual(NodalOsOnnxOcrWorkerExecutionMode.ModelMissing, response.Mode);
        Assert.IsFalse(response.Success);
        Assert.IsFalse(response.CallsRealOcr);
        Assert.IsFalse(response.CallsSaas);
        Assert.IsFalse(response.RawPersisted);
        Assert.IsTrue(response.NoAuthority);
    }

    [TestMethod]
    public void HealthCheck_ReportsOnnxRuntimeAvailable()
    {
        var worker = new NodalOsOnnxOcrWorker();
        var health = worker.HealthCheck(MissingModelSet());

        Assert.IsTrue(health.OnnxRuntimeAvailable);
        Assert.IsFalse(health.ModelSetPresent);
        Assert.IsFalse(health.ModelSetVerified);
        Assert.IsTrue(health.NoAuthority);
    }

    [TestMethod]
    public void ReadinessReport_ModelMissing()
    {
        var worker = new NodalOsOnnxOcrWorker();
        var report = worker.ReadinessReport(MissingModelSet());

        Assert.IsFalse(report.Ready);
        Assert.IsTrue(report.OnnxRuntimePackageAvailable);
        Assert.IsFalse(report.ModelSetPresent);
        Assert.IsTrue(report.PixelRedactionV2Required);
        Assert.IsTrue(report.CropOnly);
        Assert.IsTrue(report.NoFullScreen);
        Assert.IsTrue(report.NoSensitive);
        Assert.IsTrue(report.NoRawPersistence);
        Assert.IsTrue(report.NoSaas);
        Assert.IsTrue(report.NoPython);
        Assert.IsTrue(report.NoAuthority);
        Assert.IsTrue(report.ProductionPublicOcrBlocked);
    }

    [TestMethod]
    public void ReadinessReport_Ready_WhenModelsVerified()
    {
        var worker = new NodalOsOnnxOcrWorker();
        var modelSet = MissingModelSet() with
        {
            DetectionModelPath = "det.onnx",
            RecognitionModelPath = "rec.onnx",
            AllRequiredPresent = true,
            AllVerified = true
        };
        var report = worker.ReadinessReport(modelSet);

        Assert.IsTrue(report.Ready);
        Assert.IsTrue(report.ModelSetPresent);
        Assert.IsTrue(report.ModelSetVerified);
    }

    [TestMethod]
    public void DocsForClaudeAuditExist()
    {
        var root = AppDomain.CurrentDomain.BaseDirectory.Contains("bin\\Debug")
            ? Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", "..", "..", ".."))
            : AppDomain.CurrentDomain.BaseDirectory;

        Assert.IsTrue(File.Exists(Path.Combine(root, "docs", "reports", "onnx-dotnet-ocr-worker-readiness-m196.md")));
        Assert.IsTrue(File.Exists(Path.Combine(root, "docs", "audits", "claude-onnx-dotnet-ocr-runtime-audit-m196.md")));
    }
}
