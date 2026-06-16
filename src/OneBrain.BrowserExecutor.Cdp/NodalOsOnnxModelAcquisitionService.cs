using OneBrain.BrowserExecutor.Contracts;

namespace OneBrain.BrowserExecutor.Cdp;

// M200/M201 — ONNX model acquisition source decision and controlled acquisition service.
public sealed class NodalOsOnnxModelAcquisitionService
{
    public NodalOsOnnxModelSourceDecision DecideSource()
    {
        var recommendedSource = new NodalOsOnnxModelAcquisitionSource(
            "rapidocr-modelscope-v3.8.0",
            "RapidOCR community ONNX conversion hosted on ModelScope",
            "https://www.modelscope.cn/models/RapidAI/RapidOCR/resolve/v3.8.0/onnx/PP-OCRv4",
            "Apache-2.0",
            "https://github.com/RapidAI/RapidOCR/blob/main/LICENSE",
            RequiresAttribution: true,
            NodalOsOnnxModelSupplyChainRisk.CommunityVerified,
            PinnedVersion: true,
            ChecksumPublished: true,
            NoAuthority: true);

        var conversionPlan = new NodalOsOnnxModelConversionPlan(
            "paddle2onnx-v1",
            "PaddleX + paddle2onnx plugin",
            new List<string>
            {
                "A. Create isolated Python 3.10/3.11 virtual environment.",
                "B. Install paddlepaddle and paddlex with paddle2onnx plugin.",
                "C. Download official PaddleOCR inference models (pdmodel + pdiparams).",
                "D. Run paddlex --paddle2onnx for det, rec, cls.",
                "E. Rename outputs to match manifest.",
                "F. Compute SHA-256 and update manifest.",
                "G. Run verify-models.ps1.",
                "H. Run M202 session smoke tests."
            },
            PythonVersion: "3.10 or 3.11",
            RequiresVirtualEnvironment: true,
            RequiresWindows: true,
            EstimatedToolchainSizeBytes: 600 * 1024 * 1024,
            RequiresLicenseReview: true,
            NoAuthority: true);

        var rollbackPlan = new NodalOsOnnxModelRollbackPlan(
            "rollback-onnx-models-v1",
            "tools/ocr-worker/models/onnx",
            new List<string>
            {
                "tools/ocr-worker/models/onnx/ch_PP-OCRv4_det.onnx",
                "tools/ocr-worker/models/onnx/ch_PP-OCRv4_rec.onnx",
                "tools/ocr-worker/models/onnx/ch_ppocr_mobile_v2.0_cls.onnx"
            },
            DeletesOnlyOnnx: true,
            NoAuthority: true);

        return new NodalOsOnnxModelSourceDecision(
            $"source-decision-{Guid.NewGuid():N}",
            NodalOsOnnxModelAcquisitionDecision.VerifiedOnnxDownload,
            NodalOsOnnxModelAcquisitionDecision.ControlledConversion,
            NodalOsOnnxModelAcquisitionDecision.CppPaddleInference,
            NodalOsOnnxModelAcquisitionDecision.PythonPaddleOcrLegacy,
            recommendedSource,
            conversionPlan,
            rollbackPlan,
            "RapidOCR/ModelScope provides pinned, hash-verified, small ONNX models. No heavy toolchain needed for primary path.",
            OnnxRuntimeDotNetRemainsPrimary: true,
            PythonNotPrimary: true,
            SaasDisabled: true,
            ProductionPublicOcrBlocked: true,
            NoAuthority: true);
    }

    public bool ValidateSource(NodalOsOnnxModelAcquisitionSource source)
    {
        if (source.SupplyChainRisk == NodalOsOnnxModelSupplyChainRisk.BlockedByPolicy)
            return false;
        if (!source.PinnedVersion)
            return false;
        if (!source.ChecksumPublished)
            return false;
        if (string.IsNullOrWhiteSpace(source.Url))
            return false;
        return true;
    }
}
