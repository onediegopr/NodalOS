# Guarded Synthetic Text OCR Retry M220-M222

## Decision

`M220+M221+M222 CERRADO / BLOCKED_BY_MODEL_RUNTIME`

Shadow mode remains blocked. Productive OCR remains blocked. No OCR result is authoritative.

## Scope

- Worktree: `C:\Users\diego\OneDrive\PERSONAL\ONE Brain\Codigo-m12-audit`
- Base commit: `791a546`
- Branch: `chrome-lab-001-extension-local-ai-bridge`
- Models verified before retry:
  - `ch_PP-OCRv4_det.onnx`
  - `ch_PP-OCRv4_rec.onnx`
  - `ch_ppocr_mobile_v2.0_cls.onnx`

## Model Verification

| Role | File | SHA-256 | Size | Status |
| --- | --- | --- | ---: | --- |
| detection | `ch_PP-OCRv4_det.onnx` | `d2a7720d45a54257208b1e13e36a8479894cb74155a5efe29462512d42f49da9` | 4745517 | PresentAndVerified |
| recognition | `ch_PP-OCRv4_rec.onnx` | `e8770c967605983d1570cdf5352041dfb68fa0c21664f49f47b155abd3e0e318` | 7653044 | PresentAndVerified |
| classification | `ch_ppocr_mobile_v2.0_cls.onnx` | `e47acedf663230f8863ff1ab0e64dd2d82b838fceb5957146dab185a89d6215c` | 585532 | PresentAndVerified |

`.onnx` files are gitignored by `tools/ocr-worker/models/onnx/.gitignore` and are not tracked by git.

## Guarded Retry Matrix

All risky probes ran through the out-of-process guard. No risky synthetic OCR ran in-process.

| Fixture | Text | Size | Render | Result | Exit | Crash kind | Parent survived | Cleanup |
| --- | --- | --- | --- | --- | ---: | --- | --- | --- |
| LargeCenteredText | TEST | 320x128 | PixelFont | NativeRuntimeCrashContained | -1073741676 | NativeAbort | yes | yes |
| WidePaddingText | NODAL | 640x160 | PixelFont | NativeRuntimeCrashContained | -1073741676 | NativeAbort | yes | yes |
| AlphanumericText | ABC123 | 640x160 | AntiAliasedPixelFont | NativeRuntimeCrashContained | -1073741676 | NativeAbort | yes | yes |
| NumericText | 12345 | 640x320 | PixelFont | NativeRuntimeCrashContained | -1073741676 | NativeAbort | yes | yes |
| SoftBorderText | SAFE TEXT | 640x640 | AntiAliasedPixelFont | NativeRuntimeCrashContained | -1073741676 | NativeAbort | yes | yes |
| WhiteBackgroundText | TEST | 640x160 | PixelFont | NativeRuntimeCrashContained | -1073741676 | NativeAbort | yes | yes |
| PixelFontText | TEST | 640x320 | PixelFont | NativeRuntimeCrashContained | -1073741676 | NativeAbort | yes | yes |
| AntiAliasedPixelFontText | TEST | 640x320 | AntiAliasedPixelFont | NativeRuntimeCrashContained | -1073741676 | NativeAbort | yes | yes |

The exit code `-1073741676` is `0xC0000094` (`STATUS_INTEGER_DIVIDE_BY_ZERO`) and is now classified by the guard as a native abort. The crash remains contained in the child process.

## Detection Diagnosis

- Session stage reached by child process, but child exits during detection inference before controlled JSON output.
- Detection boxes: unreachable / not reported.
- Output names/shapes: unreachable because `session.Run` crashes.
- Postprocessing: unreachable.
- Parent process: survived.
- Timeout: no.
- Temp cleanup: yes.
- Runtime/provider: Microsoft.ML.OnnxRuntime CPU provider.

Finding: detection is currently blocked by native ONNX Runtime/model execution on synthetic text fixtures.

## Recognition Diagnosis

Recognition was not executed because no detection boxes were produced before the child crash. No manual safe crop was used in this block.

Recognizer manifest metadata:

- Model: `ch_PP-OCRv4_rec.onnx`
- Expected input: `[1,3,32,320]`
- Expected output: `[40,1,97]`
- Output name: `softmax_2.tmp_0`
- CTC axes inferred from manifest: time, batch, class

## Dictionary / CTC Diagnosis

- Current embedded dictionary: `en-ascii`
- Character count: 86
- Dictionary + CTC blank count: 87
- Recognizer output class count: 97
- Compatibility: `ClassCountMismatch`
- Decode allowed: no
- Positive recognition claim allowed: no

The current ASCII fixture dictionary is not a verified PaddleOCR-compatible dictionary for this model. No text is invented. If detection/runtime is fixed later and boxes become available, dictionary completion remains required before positive recognition can be claimed.

## Privacy And Authority Gates

- No SaaS OCR: passed
- No raw persistence: passed
- No full-screen fixture: passed
- No sensitive fixture: passed
- No productive OCR: passed
- No OCR authority: passed
- No documents/screens OCR: passed
- No shadow mode: passed, remains blocked

## Readiness Result

The verified models allowed a real guarded retry, but every selected text fixture crashes the child during detection. Because recognition is unreachable and dictionary compatibility is also incomplete, the honest closing decision is:

`M220+M221+M222 CERRADO / BLOCKED_BY_MODEL_RUNTIME`

Next candidate route: ONNX runtime/model execution diagnosis before dictionary completion, because the detector crash prevents boxes and recognition entry.
