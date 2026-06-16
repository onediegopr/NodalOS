using Microsoft.VisualStudio.TestTools.UnitTesting;
using OneBrain.BrowserExecutor.Cdp;
using OneBrain.BrowserExecutor.Contracts;

namespace OneBrain.Safety.Tests;

[TestClass]
[TestCategory("PaddleOcrSyntheticRun")]
[TestCategory("PaddleOcrExperimental")]
[TestCategory("OcrVisionActivationGate")]
[TestCategory("OcrVisionNoAuthority")]
[TestCategory("OcrVisionPrivacy")]
[TestCategory("BrowserGroundingSnapshot")]
[TestCategory("PrivatePreviewReadiness")]
[TestCategory("LocalPreviewReleaseCandidate")]
[TestCategory("NodalOsNamingAudit")]
[TestCategory("BrowserRuntimePhaseGate")]
public sealed class NodalOsPaddleOcrSyntheticRunM193Tests
{
    [TestMethod]
    public void SyntheticRun_RequiresPixelRedaction()
    {
        var run = new NodalOsPaddleOcrSyntheticRunService().Run();

        Assert.IsTrue(
            run.RedactionDecision is NodalOsPixelRedactionDecision.CleanNoRedactionRequired
                or NodalOsPixelRedactionDecision.RedactedPixels,
            $"unexpected redaction decision: {run.RedactionDecision}");
        Assert.IsFalse(run.OriginalRawPersisted);
    }

    [TestMethod]
    public void SyntheticRun_BlockedByEnvironment_WhenPaddleOcrNotInstalled()
    {
        var run = new NodalOsPaddleOcrSyntheticRunService().Run();

        Assert.AreEqual(NodalOsPaddleOcrSyntheticRunDecision.BlockedByEnvironment, run.Decision);
        Assert.IsFalse(run.RuntimeAvailable);
        Assert.IsFalse(run.CallsRealOcr);
        Assert.IsFalse(run.CallsSaas);
        Assert.IsFalse(run.RawPersisted);
        Assert.IsTrue(run.NoAuthority);
    }

    [TestMethod]
    public void SyntheticRun_NoFullScreen_NoSensitive()
    {
        var run = new NodalOsPaddleOcrSyntheticRunService().Run();

        Assert.IsTrue(run.WorkerMode is NodalOsPaddleOcrWorkerExecutionMode.RedactedCropShadow
            or NodalOsPaddleOcrWorkerExecutionMode.HealthCheckOnly);
    }

    [TestMethod]
    public void ArtifactAndReportAndPromptExist()
    {
        var root = AppDomain.CurrentDomain.BaseDirectory.Contains("bin\\Debug")
            ? Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", "..", "..", ".."))
            : AppDomain.CurrentDomain.BaseDirectory;

        Assert.IsTrue(Directory.Exists(Path.Combine(root, "artifacts", "ocr-vision-paddleocr", "m193")));
        Assert.IsTrue(File.Exists(Path.Combine(root, "artifacts", "ocr-vision-paddleocr", "m193", "paddleocr-synthetic-run-summary.json")));
        Assert.IsTrue(File.Exists(Path.Combine(root, "docs", "reports", "paddleocr-synthetic-redacted-crop-run-m193.md")));
        Assert.IsTrue(File.Exists(Path.Combine(root, "docs", "audits", "claude-paddleocr-synthetic-run-audit-m193.md")));
    }
}
