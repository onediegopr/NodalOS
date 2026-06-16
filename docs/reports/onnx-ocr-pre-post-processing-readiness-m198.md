# ONNX OCR Pre/Post Processing Readiness — M198

## Scope
M198 implements realistic pre/post-processing for the ONNX Runtime .NET OCR path, using synthetic fixtures and known PP-OCRv4 shapes. Real inference is still blocked because models are missing.

## Pre-Processing Implemented
- `NodalOsOnnxOcrImagePreProcessor`
  - Blocks missing redaction, raw persistence, full-screen, sensitive surfaces.
  - Decodes synthetic RGBA32 crops (real PNG/JPEG/BMP decoding deferred until real decoder approved).
  - Normalizes pixel values to [0,1].
- `NodalOsOnnxOcrDetectorPreProcessor`
  - Resizes to multiple-of-32 dimensions.
  - Applies ImageNet mean/std normalization.
  - Produces NCHW tensor and scaling metadata.
- `NodalOsOnnxOcrRecognizerPreProcessor`
  - Resizes text-line crop to height 32 with aspect-ratio-preserving width.
  - Produces NCHW tensor.

## Post-Processing Implemented
- `NodalOsOnnxOcrDetectorPostProcessor`
  - DBNet-style segmentation decoding.
  - Connected-component box extraction.
  - Scaling back to crop coordinates.
  - Min-size and max-box filters.
  - Unsupported shape rejection.
- `NodalOsOnnxOcrCharacterDictionary`
  - ASCII English and digits-only charsets.
  - CTC decode with blank/repeat handling.
- `NodalOsOnnxOcrRecognizerPostProcessor`
  - Greedy CTC decode.
  - Confidence aggregation.
  - Low-confidence => human review.
- `NodalOsOnnxOcrConfidenceAggregator`
  - Average/min confidence.
  - Threshold check (default 0.6).

## Fixture Support
- `NodalOsOnnxOcrSyntheticFixtureSet.PpOcrV4En` provides known detector/recognizer shapes.
- Processing is `ReadyForKnownFixture` and `WaitingForVerifiedModelShape` when shapes are unknown.

## Compliance
- No real OCR executed.
- No fake boxes claimed as real.
- No raw persistence.
- No full-screen or sensitive OCR.
- No authority claimed.

## Next Step
Once M197 model files are available, run the pre/post-processing pipeline against real ONNX outputs and calibrate thresholds.
