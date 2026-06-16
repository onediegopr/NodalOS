using Microsoft.VisualStudio.TestTools.UnitTesting;
using OneBrain.BrowserExecutor.Cdp;
using OneBrain.BrowserExecutor.Contracts;

namespace OneBrain.Safety.Tests;

[TestClass]
[TestCategory("OnnxModelSessionSmoke")]
[TestCategory("OnnxRuntimeDotNet")]
[TestCategory("OnnxOcrOfflineReadiness")]
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
public sealed class NodalOsOnnxModelSessionSmokeM202Tests
{
    private static readonly string RepoRoot = AppDomain.CurrentDomain.BaseDirectory.Contains("bin\\Debug")
        ? Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", "..", "..", ".."))
        : AppDomain.CurrentDomain.BaseDirectory;

    private static NodalOsOnnxRuntimeSessionPolicy OpenPolicy() =>
        new(
            $"policy-{Guid.NewGuid():N}",
            AllowRealModelLoad: true,
            AllowMetadataInspection: true,
            AllowDummyInference: true,
            AllowRealImageInference: false,
            MaxModelSizeBytes: 50 * 1024 * 1024,
            TimeoutSeconds: 30,
            NoAuthority: true);

    private static NodalOsPaddleOcrOnnxModelManifest DefaultManifest()
    {
        var catalog = new NodalOsPaddleOcrOnnxModelCatalogService();
        return catalog.LoadManifestFromFile(Path.Combine(RepoRoot, "tools", "ocr-worker", "models", "onnx", "paddleocr-onnx-model-manifest.json"))
               ?? catalog.CreateDefaultManifest();
    }

    private static bool ModelsPresent()
    {
        var manifest = DefaultManifest();
        return manifest.Models.All(m => File.Exists(Path.Combine(RepoRoot, m.LocalRelativePath)));
    }

    [TestMethod]
    public void MissingModel_SessionSmoke_ReturnsModelMissing()
    {
        var factory = new NodalOsOnnxRuntimeSessionFactory();
        var manifest = new NodalOsPaddleOcrOnnxModelCatalogService().CreateDefaultManifest();
        var model = manifest.Models[0] with { LocalRelativePath = "tools/ocr-worker/models/onnx/does-not-exist.onnx" };

        var result = factory.CreateSession(model, RepoRoot, OpenPolicy());

        Assert.AreEqual(NodalOsOnnxModelSessionSmokeStatus.ModelMissing, result.Status);
        Assert.IsFalse(result.SessionCreated);
    }

    [TestMethod]
    public void HashMismatch_SessionSmoke_ReturnsModelUnverified()
    {
        var catalog = new NodalOsPaddleOcrOnnxModelCatalogService();
        var manifest = catalog.CreateDefaultManifest() with { LicenseReviewed = true };
        var tempDir = Path.Combine(Path.GetTempPath(), $"nodal-os-test-{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempDir);
        var tempPath = Path.Combine(tempDir, "bad-det.onnx");
        File.WriteAllText(tempPath, "bad-hash-content");
        try
        {
            var badModel = manifest.Models[0] with
            {
                Status = NodalOsPaddleOcrOnnxModelStatus.Downloaded,
                LocalRelativePath = tempPath,
                Integrity = manifest.Models[0].Integrity with { FileSizeBytes = new FileInfo(tempPath).Length }
            };
            var verifier = new NodalOsPaddleOcrOnnxModelVerifierService();
            var verified = verifier.Verify(badModel, RepoRoot, licenseAccepted: true);
            Assert.AreEqual(NodalOsPaddleOcrOnnxModelStatus.Invalid, verified.Status);
        }
        finally
        {
            Directory.Delete(tempDir, recursive: true);
        }
    }

    [TestMethod]
    public void VerifiedModel_SessionSmoke_CreatesSession()
    {
        if (!ModelsPresent())
        {
            Assert.Inconclusive("Real ONNX models not present; skipping session smoke test.");
            return;
        }

        var factory = new NodalOsOnnxRuntimeSessionFactory();
        var manifest = DefaultManifest();
        var model = manifest.Models[0];

        var result = factory.CreateSession(model, RepoRoot, OpenPolicy());

        Assert.AreEqual(NodalOsOnnxModelSessionSmokeStatus.Success, result.Status);
        Assert.IsTrue(result.SessionCreated);
        Assert.IsTrue(result.InputNames.Count > 0);
        Assert.IsTrue(result.OutputNames.Count > 0);
        Assert.IsFalse(result.RealImageInferenceRun);
        Assert.IsTrue(result.NoAuthority);
    }

    [TestMethod]
    public void VerifiedModel_DummyInference_NoRealImage()
    {
        if (!ModelsPresent())
        {
            Assert.Inconclusive("Real ONNX models not present; skipping dummy inference test.");
            return;
        }

        var factory = new NodalOsOnnxRuntimeSessionFactory();
        var manifest = DefaultManifest();
        var model = manifest.Models[0];

        var result = factory.RunDummyInference(model, RepoRoot, OpenPolicy());

        Assert.AreEqual(NodalOsOnnxModelSessionSmokeStatus.Success, result.Status);
        Assert.IsTrue(result.DummyInferenceRun);
        Assert.IsFalse(result.RealImageInferenceRun);
    }

    [TestMethod]
    public void SessionSmokeTester_Readiness_Ready_WhenModelsVerified()
    {
        if (!ModelsPresent())
        {
            Assert.Inconclusive("Real ONNX models not present; skipping readiness test.");
            return;
        }

        var tester = new NodalOsOnnxModelSessionSmokeTester();
        var manifest = DefaultManifest();
        var results = tester.SmokeTestAll(manifest, RepoRoot, licenseAccepted: true, OpenPolicy());
        var readiness = tester.EvaluateReadiness(results);

        Assert.IsTrue(readiness.Ready, readiness.Reason);
        Assert.IsTrue(readiness.AllRequiredModelsVerified);
        Assert.IsTrue(readiness.AllSessionsLoaded);
    }

    [TestMethod]
    public void OfflineReadiness_WithSessionReadiness_ReadyForSyntheticRun()
    {
        if (!ModelsPresent())
        {
            Assert.Inconclusive("Real ONNX models not present; skipping end-to-end readiness test.");
            return;
        }

        var gate = new NodalOsOnnxOcrOfflineReadinessGate();
        var tester = new NodalOsOnnxModelSessionSmokeTester();
        var manifest = DefaultManifest();
        var results = tester.SmokeTestAll(manifest, RepoRoot, licenseAccepted: true, OpenPolicy());
        var sessionReadiness = tester.EvaluateReadiness(results);

        var report = gate.Evaluate(
            manifest,
            RepoRoot,
            licenseAccepted: true,
            preProcessorReady: true,
            detectorPostProcessorReady: true,
            recognizerPostProcessorReady: true,
            NodalOsOnnxOcrSyntheticFixtureSet.PpOcrV4En,
            sessionReadiness);

        Assert.AreEqual(NodalOsOnnxOcrOfflineReadinessDecision.ReadyForOnnxSyntheticRun, report.Decision);
        Assert.IsTrue(report.CanAttemptSyntheticRun);
    }

    [TestMethod]
    public void SessionSmoke_NoAuthority()
    {
        var policy = OpenPolicy();
        Assert.IsTrue(policy.NoAuthority);

        var factory = new NodalOsOnnxRuntimeSessionFactory();
        var manifest = new NodalOsPaddleOcrOnnxModelCatalogService().CreateDefaultManifest();
        var result = factory.CreateSession(manifest.Models[0], RepoRoot, policy);
        Assert.IsTrue(result.NoAuthority);
    }

    [TestMethod]
    public void SessionSmokeReport_Exists()
    {
        var path = Path.Combine(RepoRoot, "docs", "reports", "onnx-model-session-smoke-m202.md");
        Assert.IsTrue(File.Exists(path));
    }

    [TestMethod]
    public void SessionSmokeArtifact_Exists()
    {
        var path = Path.Combine(RepoRoot, "artifacts", "ocr-vision-onnx", "m202", "onnx-model-session-smoke-summary.json");
        Assert.IsTrue(File.Exists(path));
    }

    [TestMethod]
    public void SessionSmokeClaudePrompt_Exists()
    {
        var path = Path.Combine(RepoRoot, "docs", "audits", "claude-onnx-model-session-smoke-audit-m202.md");
        Assert.IsTrue(File.Exists(path));
    }
}
