using System.Security.Cryptography;
using System.Text.Json;
using OneBrain.BrowserExecutor.Contracts;

namespace OneBrain.BrowserExecutor.Cdp;

// M197 — PaddleOCR ONNX model catalog.
// Loads/validates the manifest and exposes required/optional model entries.
public sealed class NodalOsPaddleOcrOnnxModelCatalogService
{
    public const string ManifestVersion = "paddleocr-onnx-model-manifest.v1";
    public const long DefaultMaxTotalModelSizeBytes = 300 * 1024 * 1024; // 300 MB

    public NodalOsPaddleOcrOnnxModelCatalog CreateDefaultCatalog()
    {
        var manifest = CreateDefaultManifest();
        return BuildCatalog(manifest);
    }

    public NodalOsPaddleOcrOnnxModelCatalog BuildCatalog(NodalOsPaddleOcrOnnxModelManifest manifest)
    {
        var required = manifest.Models
            .Where(m => m.Kind is NodalOsPaddleOcrOnnxModelKind.TextDetection or NodalOsPaddleOcrOnnxModelKind.TextRecognition)
            .ToList();
        var optional = manifest.Models
            .Where(m => m.Kind is NodalOsPaddleOcrOnnxModelKind.TextDirectionClassification or NodalOsPaddleOcrOnnxModelKind.LayoutOptional or NodalOsPaddleOcrOnnxModelKind.TableOptional)
            .ToList();

        var allRequiredPresent = required.All(m => m.Status != NodalOsPaddleOcrOnnxModelStatus.Missing);
        var allRequiredVerified = required.All(m => m.Status == NodalOsPaddleOcrOnnxModelStatus.Verified);

        return new NodalOsPaddleOcrOnnxModelCatalog(
            $"catalog-{Guid.NewGuid():N}",
            manifest.ContractVersion,
            required,
            optional,
            manifest.TotalMaxSizeBytes,
            allRequiredPresent,
            allRequiredVerified,
            manifest.LicenseReviewed,
            NoAuthority: true,
            Redacted: true);
    }

    public NodalOsPaddleOcrOnnxModelManifest CreateDefaultManifest() =>
        new(
            $"manifest-{Guid.NewGuid():N}",
            ManifestVersion,
            new List<NodalOsPaddleOcrOnnxModelRef>
            {
                new(
                    "paddleocr-det-onnx",
                    NodalOsPaddleOcrOnnxModelKind.TextDetection,
                    "v4",
                    "multi",
                    new NodalOsPaddleOcrOnnxModelSource(
                        "PaddleOCR official / Paddle2ONNX conversion",
                        "https://github.com/PaddlePaddle/PaddleOCR/blob/main/docs/version3.x/inference_deployment/others/obtaining_onnx_models.html",
                        "Apache-2.0",
                        "https://github.com/PaddlePaddle/PaddleOCR/blob/main/LICENSE",
                        RequiresAttribution: true),
                    "tools/ocr-worker/models/onnx/ch_PP-OCRv4_det.onnx",
                    new NodalOsPaddleOcrOnnxModelIntegrity("SHA-256", string.Empty, 0),
                    "NCHW [1,3,H,W] where H and W are multiples of 32",
                    "segmentation probability map [1,1,H,W]",
                    17,
                    NodalOsPaddleOcrOnnxModelStatus.Missing)
                {
                    InputNames = new[] { "x" },
                    OutputNames = new[] { "sigmoid_0.tmp_0" },
                    ExpectedInputShape = new[] { 1, 3, 640, 640 },
                    ExpectedOutputShape = new[] { 1, 1, 640, 640 }
                },
                new(
                    "paddleocr-rec-onnx",
                    NodalOsPaddleOcrOnnxModelKind.TextRecognition,
                    "v4",
                    "en",
                    new NodalOsPaddleOcrOnnxModelSource(
                        "PaddleOCR official / Paddle2ONNX conversion",
                        "https://github.com/PaddlePaddle/PaddleOCR/blob/main/docs/version3.x/inference_deployment/others/obtaining_onnx_models.html",
                        "Apache-2.0",
                        "https://github.com/PaddlePaddle/PaddleOCR/blob/main/LICENSE",
                        RequiresAttribution: true),
                    "tools/ocr-worker/models/onnx/ch_PP-OCRv4_rec.onnx",
                    new NodalOsPaddleOcrOnnxModelIntegrity("SHA-256", string.Empty, 0),
                    "NCHW [1,3,32,W] with dynamic width W",
                    "CTC logits [T,1,VocabSize+1]",
                    17,
                    NodalOsPaddleOcrOnnxModelStatus.Missing)
                {
                    InputNames = new[] { "x" },
                    OutputNames = new[] { "softmax_2.tmp_0" },
                    ExpectedInputShape = new[] { 1, 3, 32, 320 },
                    ExpectedOutputShape = new[] { 40, 1, 97 }
                },
                new(
                    "paddleocr-cls-onnx",
                    NodalOsPaddleOcrOnnxModelKind.TextDirectionClassification,
                    "v2",
                    "multi",
                    new NodalOsPaddleOcrOnnxModelSource(
                        "PaddleOCR official / Paddle2ONNX conversion",
                        "https://github.com/PaddlePaddle/PaddleOCR/blob/main/docs/version3.x/inference_deployment/others/obtaining_onnx_models.html",
                        "Apache-2.0",
                        "https://github.com/PaddlePaddle/PaddleOCR/blob/main/LICENSE",
                        RequiresAttribution: true),
                    "tools/ocr-worker/models/onnx/ch_ppocr_mobile_v2.0_cls.onnx",
                    new NodalOsPaddleOcrOnnxModelIntegrity("SHA-256", string.Empty, 0),
                    "NCHW [1,3,48,192]",
                    "direction logits [1,2]",
                    17,
                    NodalOsPaddleOcrOnnxModelStatus.Missing)
                {
                    InputNames = new[] { "x" },
                    OutputNames = new[] { "save_infer_model/Scale_0" },
                    ExpectedInputShape = new[] { 1, 3, 48, 192 },
                    ExpectedOutputShape = new[] { 1, 2 }
                }

            },
            TotalMaxSizeBytes: DefaultMaxTotalModelSizeBytes,
            AllRequiredPresent: false,
            AllVerified: false,
            LicenseReviewed: false,
            NoAuthority: true,
            Redacted: true);

    public NodalOsPaddleOcrOnnxModelManifest? LoadManifestFromJson(string json)
    {
        try
        {
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;
            var contractVersion = root.GetProperty("manifestVersion").GetString() ?? ManifestVersion;
            var totalMaxSize = root.GetProperty("totalMaxSizeBytes").GetInt64();
            var licenseReviewed = root.GetProperty("licenseReviewed").GetBoolean();
            var models = new List<NodalOsPaddleOcrOnnxModelRef>();

            foreach (var m in root.GetProperty("models").EnumerateArray())
            {
                var statusString = m.GetProperty("status").GetString() ?? "Missing";
                var status = Enum.Parse<NodalOsPaddleOcrOnnxModelStatus>(statusString);

                var modelRef = new NodalOsPaddleOcrOnnxModelRef(
                    m.GetProperty("modelId").GetString() ?? string.Empty,
                    Enum.Parse<NodalOsPaddleOcrOnnxModelKind>(m.GetProperty("kind").GetString() ?? "TextDetection"),
                    m.GetProperty("version").GetString() ?? string.Empty,
                    m.GetProperty("language").GetString() ?? string.Empty,
                    new NodalOsPaddleOcrOnnxModelSource(
                        m.GetProperty("source").GetProperty("origin").GetString() ?? string.Empty,
                        m.GetProperty("source").GetProperty("url").GetString() ?? string.Empty,
                        m.GetProperty("source").GetProperty("licenseIdentifier").GetString() ?? string.Empty,
                        m.GetProperty("source").GetProperty("licenseUrl").GetString() ?? string.Empty,
                        m.GetProperty("source").GetProperty("requiresAttribution").GetBoolean()),
                    m.GetProperty("localRelativePath").GetString() ?? string.Empty,
                    new NodalOsPaddleOcrOnnxModelIntegrity(
                        m.GetProperty("integrity").GetProperty("algorithm").GetString() ?? "SHA-256",
                        m.GetProperty("integrity").GetProperty("checksum").GetString() ?? string.Empty,
                        m.GetProperty("integrity").GetProperty("fileSizeBytes").GetInt64()),
                    m.GetProperty("inputShapeDescription").GetString() ?? string.Empty,
                    m.GetProperty("outputShapeDescription").GetString() ?? string.Empty,
                    m.GetProperty("opsetVersion").GetInt32(),
                    status)
                {
                    InputNames = ReadStringArray(m, "inputNames"),
                    OutputNames = ReadStringArray(m, "outputNames"),
                    ExpectedInputShape = ReadIntArray(m, "expectedInputShape"),
                    ExpectedOutputShape = ReadIntArray(m, "expectedOutputShape")
                };

                models.Add(modelRef);
            }

            return new NodalOsPaddleOcrOnnxModelManifest(
                $"manifest-loaded-{Guid.NewGuid():N}",
                contractVersion,
                models,
                totalMaxSize,
                AllRequiredPresent: false,
                AllVerified: false,
                licenseReviewed,
                NoAuthority: true,
                Redacted: true);
        }
        catch
        {
            return null;
        }
    }

    private static IReadOnlyList<string> ReadStringArray(JsonElement element, string propertyName)
    {
        if (!element.TryGetProperty(propertyName, out var array))
            return Array.Empty<string>();
        return array.EnumerateArray().Select(e => e.GetString() ?? string.Empty).ToList();
    }

    private static IReadOnlyList<int> ReadIntArray(JsonElement element, string propertyName)
    {
        if (!element.TryGetProperty(propertyName, out var array))
            return Array.Empty<int>();
        return array.EnumerateArray().Select(e => e.GetInt32()).ToList();
    }

    public NodalOsPaddleOcrOnnxModelManifest? LoadManifestFromFile(string path)
    {
        if (!File.Exists(path))
            return null;
        return LoadManifestFromJson(File.ReadAllText(path));
    }

    public NodalOsPaddleOcrOnnxModelManifest UpdateModelStatus(
        NodalOsPaddleOcrOnnxModelManifest manifest,
        string modelId,
        NodalOsPaddleOcrOnnxModelStatus status,
        long fileSizeBytes,
        string checksum)
    {
        var updated = manifest.Models.Select(m =>
        {
            if (m.ModelId != modelId)
                return m;
            return m with
            {
                Status = status,
                Integrity = new NodalOsPaddleOcrOnnxModelIntegrity(m.Integrity.Algorithm, checksum, fileSizeBytes)
            };
        }).ToList();

        var required = updated.Where(m => m.Kind is NodalOsPaddleOcrOnnxModelKind.TextDetection or NodalOsPaddleOcrOnnxModelKind.TextRecognition).ToList();
        var allRequiredPresent = required.All(m => m.Status != NodalOsPaddleOcrOnnxModelStatus.Missing);
        var allVerified = required.All(m => m.Status == NodalOsPaddleOcrOnnxModelStatus.Verified);

        return manifest with
        {
            Models = updated,
            AllRequiredPresent = allRequiredPresent,
            AllVerified = allVerified
        };
    }

    public static string ComputeChecksum(string filePath, string algorithm = "SHA-256")
    {
        using var stream = File.OpenRead(filePath);
        return algorithm.ToUpperInvariant() switch
        {
            "SHA-256" => Convert.ToHexStringLower(SHA256.HashData(stream)),
            "SHA-512" => Convert.ToHexStringLower(SHA512.HashData(stream)),
            _ => throw new NotSupportedException($"hash algorithm {algorithm} not supported")
        };
    }
}
