namespace OneBrain.BrowserExecutor.Contracts;

// M295-M297 - PaddleOCR-aligned recognizer crop preprocessing.
//
// Root cause of the residual synthetic-pipeline decode failures: the detector->recognizer crop bridge
// stretched every crop to a fixed [48,320] tensor with per-axis nearest-neighbour sampling, destroying the
// crop aspect ratio (narrow text widened, wide text squished). PaddleOCR `resize_norm_img` instead resizes
// to a fixed height (48), width = min(maxWidth, ceil(height * aspect)) preserving aspect ratio, then
// right-pads the remainder with 0.0 in normalized space. These contracts model that aligned behaviour and
// keep the legacy stretch available for A/B evidence.

public enum NodalOsPaddleOcrRecognizerResizeMode
{
    // Legacy behaviour: stretch the crop to fill the full target width (aspect ratio NOT preserved).
    StretchToFixedWidth,
    // PaddleOCR resize_norm_img: preserve aspect ratio, then right-pad to maxWidth with the pad value.
    RatioPreservingRightPad
}

public sealed record NodalOsPaddleOcrRecognizerCropTensor(
    string TensorId,
    float[] Tensor,
    int[] Shape,
    int TargetHeight,
    int TargetWidth,
    int ResizedWidth,
    int PaddedColumns,
    bool AspectRatioPreserved,
    bool WidthCapped,
    double SourceAspect,
    double ResizedAspect,
    NodalOsPaddleOcrRecognizerResizeMode Mode,
    string NormalizationFormula,
    double PadValue,
    bool NoRawPersistence,
    bool NoAuthority,
    string Reason);
