using Microsoft.VisualStudio.TestTools.UnitTesting;
using OneBrain.BrowserExecutor.Cdp;
using OneBrain.BrowserExecutor.Contracts;

namespace OneBrain.Safety.Tests;

[TestClass]
[TestCategory("OnnxModelInventory")]
[TestCategory("OnnxModelManifestReconciliation")]
[TestCategory("OnnxModelAvailability")]
[TestCategory("OnnxModelAcquisition")]
[TestCategory("OnnxModelVerification")]
[TestCategory("OnnxModelReadiness")]
[TestCategory("GuardedSyntheticTextOcr")]
[TestCategory("GuardedSyntheticTextProbe")]
[TestCategory("OnnxOutOfProcessGuard")]
[TestCategory("OnnxOcrProbeRunner")]
[TestCategory("OnnxOcrSyntheticTextRun")]
[TestCategory("SyntheticOcrTextFixture")]
[TestCategory("OnnxOcrSyntheticInference")]
[TestCategory("OnnxSyntheticOcrReadiness")]
[TestCategory("OcrVisionNoAuthority")]
[TestCategory("OcrVisionPrivacy")]
[TestCategory("PixelRedaction")]
[TestCategory("OcrRedactionPrecondition")]
[TestCategory("PrivatePreviewReadiness")]
[TestCategory("LocalPreviewReleaseCandidate")]
[TestCategory("BrowserRuntimePhaseGate")]
public sealed class NodalOsOnnxModelAvailabilityM215M217Tests
{
    private static readonly string RepoRoot = AppDomain.CurrentDomain.BaseDirectory.Contains("bin\\Debug") ||
                                              AppDomain.CurrentDomain.BaseDirectory.Contains("bin\\Release")
        ? Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", "..", "..", ".."))
        : AppDomain.CurrentDomain.BaseDirectory;

    [TestMethod]
    public void Inventory_ReportsDetectionRecognitionAvailabilityHonestly()
    {
        var inventory = new NodalOsOnnxModelInventoryService().BuildInventory(RepoRoot);
        var det = inventory.Entries.Single(e => e.Role == NodalOsPaddleOcrOnnxModelKind.TextDetection);
        var rec = inventory.Entries.Single(e => e.Role == NodalOsPaddleOcrOnnxModelKind.TextRecognition);

        Assert.IsTrue(det.AvailabilityStatus is NodalOsOnnxModelAvailabilityStatus.Missing or NodalOsOnnxModelAvailabilityStatus.PresentAndVerified);
        Assert.IsTrue(rec.AvailabilityStatus is NodalOsOnnxModelAvailabilityStatus.Missing or NodalOsOnnxModelAvailabilityStatus.PresentAndVerified);
        Assert.AreEqual(det.Exists, det.AvailabilityStatus == NodalOsOnnxModelAvailabilityStatus.PresentAndVerified);
        Assert.AreEqual(rec.Exists, rec.AvailabilityStatus == NodalOsOnnxModelAvailabilityStatus.PresentAndVerified);
        Assert.IsTrue(inventory.NoSaas);
        Assert.IsTrue(inventory.NoAuthority);
    }

    [TestMethod]
    public void Inventory_ReportsClassificationPresentIfPresent()
    {
        var inventory = new NodalOsOnnxModelInventoryService().BuildInventory(RepoRoot);
        var cls = inventory.Entries.Single(e => e.Role == NodalOsPaddleOcrOnnxModelKind.TextDirectionClassification);

        if (!cls.Exists)
        {
            Assert.Inconclusive("Classifier model not present in this checkout.");
            return;
        }

        Assert.AreEqual(NodalOsOnnxModelAvailabilityStatus.PresentAndVerified, cls.AvailabilityStatus);
        Assert.AreEqual(585532, cls.ActualSizeBytes);
        Assert.AreEqual("e47acedf663230f8863ff1ab0e64dd2d82b838fceb5957146dab185a89d6215c", cls.ActualSha256);
    }

    [TestMethod]
    public void Inventory_DoesNotRequireCommittingModels()
    {
        var inventory = new NodalOsOnnxModelInventoryService().BuildInventory(RepoRoot);

        Assert.IsTrue(inventory.Entries.All(e => e.GitIgnored));
        Assert.IsTrue(inventory.Entries.All(e => !e.Committed));
    }

    [TestMethod]
    public void Gate_BlocksHashMismatch()
    {
        var entry = Entry(NodalOsPaddleOcrOnnxModelKind.TextDetection, NodalOsOnnxModelAvailabilityStatus.HashMismatch);
        var inventory = Inventory([entry]);

        var result = new NodalOsOnnxModelAvailabilityReadinessGate().Evaluate(inventory);

        Assert.AreEqual(NodalOsOnnxModelAcquisitionReadinessDecision.BlockedByModelHashMismatch, result.Decision);
        Assert.IsTrue(result.AnyHashMismatch);
    }

    [TestMethod]
    public void Gate_BlocksPathMismatch()
    {
        var entry = Entry(NodalOsPaddleOcrOnnxModelKind.TextDetection, NodalOsOnnxModelAvailabilityStatus.PathMismatch);
        var inventory = Inventory([entry]);

        var result = new NodalOsOnnxModelAvailabilityReadinessGate().Evaluate(inventory);

        Assert.AreEqual(NodalOsOnnxModelAcquisitionReadinessDecision.BlockedByModelPathDiscovery, result.Decision);
        Assert.IsTrue(result.AnyPathMismatch);
    }

    [TestMethod]
    public void Gate_BlocksUnexpectedCommittedModel()
    {
        var entry = Entry(NodalOsPaddleOcrOnnxModelKind.TextDetection, NodalOsOnnxModelAvailabilityStatus.UnexpectedCommittedModel) with
        {
            Committed = true
        };
        var inventory = Inventory([entry]);

        var result = new NodalOsOnnxModelAvailabilityReadinessGate().Evaluate(inventory);

        Assert.AreEqual(NodalOsOnnxModelAcquisitionReadinessDecision.BlockedByUnexpectedCommittedModel, result.Decision);
        Assert.IsTrue(result.AnyUnexpectedCommittedModel);
    }

    [TestMethod]
    public void Gate_BlocksRetryWhenDetectionMissing()
    {
        var det = Entry(NodalOsPaddleOcrOnnxModelKind.TextDetection, NodalOsOnnxModelAvailabilityStatus.Missing);
        var rec = Entry(NodalOsPaddleOcrOnnxModelKind.TextRecognition, NodalOsOnnxModelAvailabilityStatus.PresentAndVerified);
        var result = new NodalOsOnnxModelAvailabilityReadinessGate().Evaluate(Inventory([det, rec]));
        var plan = new NodalOsOnnxModelAvailabilityReadinessGate().BuildRetryPlan(result);

        Assert.AreEqual(NodalOsOnnxModelAcquisitionReadinessDecision.ReadyForModelDownload, result.Decision);
        Assert.IsFalse(plan.RetryAllowed);
        Assert.IsFalse(plan.RetryExecuted);
        Assert.IsTrue(plan.UsesOutOfProcessGuard);
    }

    [TestMethod]
    public void Gate_BlocksRetryWhenRecognitionMissing()
    {
        var det = Entry(NodalOsPaddleOcrOnnxModelKind.TextDetection, NodalOsOnnxModelAvailabilityStatus.PresentAndVerified);
        var rec = Entry(NodalOsPaddleOcrOnnxModelKind.TextRecognition, NodalOsOnnxModelAvailabilityStatus.Missing);
        var result = new NodalOsOnnxModelAvailabilityReadinessGate().Evaluate(Inventory([det, rec]));

        Assert.AreEqual(NodalOsOnnxModelAcquisitionReadinessDecision.ReadyForModelDownload, result.Decision);
    }

    [TestMethod]
    public void Gate_AllowsRetryOnlyWhenDetectionRecognitionVerified()
    {
        var det = Entry(NodalOsPaddleOcrOnnxModelKind.TextDetection, NodalOsOnnxModelAvailabilityStatus.PresentAndVerified);
        var rec = Entry(NodalOsPaddleOcrOnnxModelKind.TextRecognition, NodalOsOnnxModelAvailabilityStatus.PresentAndVerified);
        var result = new NodalOsOnnxModelAvailabilityReadinessGate().Evaluate(Inventory([det, rec]));
        var plan = new NodalOsOnnxModelAvailabilityReadinessGate().BuildRetryPlan(result);

        Assert.AreEqual(NodalOsOnnxModelAcquisitionReadinessDecision.ReadyForGuardedSyntheticTextRetry, result.Decision);
        Assert.IsTrue(plan.RetryAllowed);
        Assert.IsTrue(plan.UsesOutOfProcessGuard);
    }

    [TestMethod]
    public void Gate_GeneratesAcquisitionCommands()
    {
        var inventory = new NodalOsOnnxModelInventoryService().BuildInventory(RepoRoot);
        var result = new NodalOsOnnxModelAvailabilityReadinessGate().Evaluate(inventory);

        Assert.IsTrue(result.DownloadCommand.Contains("download-models.ps1", StringComparison.Ordinal));
        Assert.IsTrue(result.VerifyCommand.Contains("verify-models.ps1", StringComparison.Ordinal));
        Assert.IsTrue(result.RollbackCommand.Contains("rollback-models.ps1", StringComparison.Ordinal));
        Assert.IsTrue(result.ScriptsPresent);
    }

    [TestMethod]
    public void Report_Artifact_ClaudePrompt_Adr_Exist_ForM217()
    {
        Assert.IsTrue(File.Exists(Path.Combine(RepoRoot, "docs", "reports", "onnx-model-availability-reconciliation-m217.md")));
        Assert.IsTrue(File.Exists(Path.Combine(RepoRoot, "artifacts", "ocr-vision-onnx", "m217", "onnx-model-availability-reconciliation-summary.json")));
        Assert.IsTrue(File.Exists(Path.Combine(RepoRoot, "docs", "audits", "claude-onnx-model-availability-reconciliation-audit-m217.md")));
        Assert.IsTrue(File.Exists(Path.Combine(RepoRoot, "docs", "adr", "onnx-model-availability-reconciliation-m215-m217.md")));
    }

    private static NodalOsOnnxModelInventory Inventory(IReadOnlyList<NodalOsOnnxModelInventoryEntry> entries) =>
        new(
            $"inventory-{Guid.NewGuid():N}",
            RepoRoot,
            Path.Combine(RepoRoot, "tools", "ocr-worker", "models", "onnx", "paddleocr-onnx-model-manifest.json"),
            ManifestExists: true,
            entries,
            NoSaas: true,
            NoRawPersistence: true,
            NoAuthority: true,
            DateTimeOffset.UtcNow);

    private static NodalOsOnnxModelInventoryEntry Entry(
        NodalOsPaddleOcrOnnxModelKind role,
        NodalOsOnnxModelAvailabilityStatus status)
    {
        var name = role switch
        {
            NodalOsPaddleOcrOnnxModelKind.TextDetection => "ch_PP-OCRv4_det.onnx",
            NodalOsPaddleOcrOnnxModelKind.TextRecognition => "ch_PP-OCRv4_rec.onnx",
            _ => "ch_ppocr_mobile_v2.0_cls.onnx"
        };
        var relative = $"tools/ocr-worker/models/onnx/{name}";
        var resolution = new NodalOsOnnxModelPathResolution(
            $"model-{role}",
            relative,
            name,
            Path.Combine(RepoRoot, relative),
            PathUnderRepository: status != NodalOsOnnxModelAvailabilityStatus.PathMismatch,
            FileNameMatches: status != NodalOsOnnxModelAvailabilityStatus.PathMismatch,
            NoAuthority: true);

        return new NodalOsOnnxModelInventoryEntry(
            $"model-{role}",
            role,
            name,
            relative,
            resolution,
            Exists: status != NodalOsOnnxModelAvailabilityStatus.Missing,
            ExpectedSha256: "expected",
            ActualSha256: status == NodalOsOnnxModelAvailabilityStatus.HashMismatch ? "actual" : "expected",
            ExpectedSizeBytes: 1,
            ActualSizeBytes: status == NodalOsOnnxModelAvailabilityStatus.SizeMismatch ? 2 : 1,
            ManifestSourceUrl: "https://www.modelscope.cn/models/RapidAI/RapidOCR/resolve/v3.8.0/onnx/PP-OCRv4/det/ch_PP-OCRv4_det_mobile.onnx",
            GitIgnored: true,
            Committed: status == NodalOsOnnxModelAvailabilityStatus.UnexpectedCommittedModel,
            status,
            status.ToString());
    }
}
