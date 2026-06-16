namespace OneBrain.BrowserExecutor.Cdp;

// M199 — ONNX OCR synthetic fixture set.
// Defines known model shapes for offline testing without real model files.
public sealed record NodalOsOnnxOcrSyntheticFixtureSet(
    string FixtureSetId,
    string Description,
    int[] DetectorInputShape,
    int[] DetectorOutputShape,
    int[] RecognizerInputShape,
    int[] RecognizerOutputShape,
    int MaxBoxes,
    int MaxTextLength,
    bool KnownShapes,
    bool NoAuthority)
{
    public static readonly NodalOsOnnxOcrSyntheticFixtureSet PpOcrV4En = new(
        $"fixture-ppocrv4-en",
        "PP-OCRv4 English synthetic fixtures",
        DetectorInputShape: new[] { 1, 3, 640, 640 },
        DetectorOutputShape: new[] { 1, 1, 640, 640 },
        RecognizerInputShape: new[] { 1, 3, 32, 320 },
        RecognizerOutputShape: new[] { 40, 1, 97 }, // 40 time steps, blank + 96 chars
        MaxBoxes: 100,
        MaxTextLength: 40,
        KnownShapes: true,
        NoAuthority: true);

    public static readonly NodalOsOnnxOcrSyntheticFixtureSet UnknownShapes = new(
        $"fixture-unknown",
        "Unknown model shapes",
        DetectorInputShape: Array.Empty<int>(),
        DetectorOutputShape: Array.Empty<int>(),
        RecognizerInputShape: Array.Empty<int>(),
        RecognizerOutputShape: Array.Empty<int>(),
        MaxBoxes: 0,
        MaxTextLength: 0,
        KnownShapes: false,
        NoAuthority: true);
}
