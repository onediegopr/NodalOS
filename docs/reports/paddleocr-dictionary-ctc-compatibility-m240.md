# NODAL OS M238-M240 - PaddleOCR Dictionary / CTC Compatibility

## Decision

```text
M238+M239+M240 CERRADO / READY_FOR_DICTIONARY_SOURCE_SELECTION
```

## Runtime And Models

- Runtime: `Microsoft.ML.OnnxRuntime 1.22.1`.
- Provider: `CPUExecutionProvider`.
- Detection model: `ch_PP-OCRv4_det.onnx`, verified in the current M235-M237 baseline.
- Recognition model: `ch_PP-OCRv4_rec.onnx`, verified in the current M235-M237 baseline.
- Classification model: `ch_ppocr_mobile_v2.0_cls.onnx`, verified in the current M235-M237 baseline.
- ONNX models remain gitignored and untracked.

During M238-M240 validation, the first `verify-models.ps1` run found `det` and `rec` absent on disk while `cls` was present. The approved model acquisition script was rerun:

```powershell
pwsh -NoProfile -ExecutionPolicy Bypass -File tools/ocr-worker/models/onnx/download-models.ps1 -Confirm
pwsh -NoProfile -ExecutionPolicy Bypass -File tools/ocr-worker/models/onnx/verify-models.ps1
```

Final verification:

- `ch_PP-OCRv4_det.onnx`: verified, `4745517` bytes, SHA-256 `d2a7720d45a54257208b1e13e36a8479894cb74155a5efe29462512d42f49da9`.
- `ch_PP-OCRv4_rec.onnx`: verified, `7653044` bytes, SHA-256 `e8770c967605983d1570cdf5352041dfb68fa0c21664f49f47b155abd3e0e318`.
- `ch_ppocr_mobile_v2.0_cls.onnx`: verified, `585532` bytes, SHA-256 `e47acedf663230f8863ff1ab0e64dd2d82b838fceb5957146dab185a89d6215c`.

## Recognizer Output

Recognizer output metadata remains:

```text
softmax_2.tmp_0=[1,40,97]
```

Candidate width `640` output observed in M235-M237:

```text
softmax_2.tmp_0=[1,80,97]
```

Required class count:

```text
97
```

## Existing Dictionary

Current embedded ASCII fixture dictionary:

```text
characters: 86
characters + blank: 87
```

Compatibility:

```text
97 recognizer classes != 87 ASCII+blank
```

Result:

```text
ClassCountMismatch
DecodeAllowed=false
RecognitionSuccessAllowed=false
```

No decode was attempted with the incompatible ASCII dictionary.

## PaddleOCR Dictionary Manifest

M238 introduced a manifest entry for the required PaddleOCR recognition CTC charset:

- dictionary id: `paddleocr-en-ppocrv4-rec-ctc-dictionary`.
- role: `RecognitionCtcCharset`.
- expected file: `paddleocr-ppocrv4-en-dict.txt`.
- expected relative path: `tools/ocr-worker/models/onnx/dictionaries/paddleocr-ppocrv4-en-dict.txt`.
- expected charset count: `96`.
- expected recognizer class count: `97`.
- blank token policy: `BlankAppendedAtEnd`.
- CTC blank index: `96`.
- newline handling: `UTF-8 one token per line; CRLF/LF normalized before hashing and loading`.
- source URL: not selected.
- expected SHA-256: not selected.
- expected size: not selected.
- acquisition status: `SourceNotSelected`.
- gitignored policy: true.
- committed policy: false.

No source URL, hash, or size was invented.

## Acquisition / Verification

Dictionary acquisition remains blocked until an approved source is selected.

Planned scripts, not created as active downloaders yet:

- `tools/ocr-worker/models/onnx/download-dictionaries.ps1`.
- `tools/ocr-worker/models/onnx/verify-dictionaries.ps1`.
- `tools/ocr-worker/models/onnx/rollback-dictionaries.ps1`.

Planned commands after source/hash/size approval:

```powershell
pwsh -NoProfile -ExecutionPolicy Bypass -File tools/ocr-worker/models/onnx/download-dictionaries.ps1 -Confirm
pwsh -NoProfile -ExecutionPolicy Bypass -File tools/ocr-worker/models/onnx/verify-dictionaries.ps1
```

Scripts were not executed because there is no approved source/hash/size.

## CTC Decode Readiness

Decode compatibility result:

```text
READY_FOR_DICTIONARY_SOURCE_SELECTION
```

Decode attempted:

```text
no
```

Reason:

```text
No compatible dictionary is present and verified. ASCII+blank 87 remains incompatible with recognizer class count 97.
```

No decoded text or confidence exists in this block.

## Safety

- No productive OCR.
- No SaaS OCR.
- No API keys.
- No real documents.
- No real screens.
- No full-screen OCR.
- No sensitive OCR.
- No raw persistence.
- No OCR as authority.
- No shadow mode.
- No CDP pipeline integration.
- No risky OCR in-process.
- No ONNX models committed.
- No dictionary downloaded.
- No dictionary hash invented.
- No text invented.

## Next Step

Select an approved PaddleOCR/RapidOCR dictionary source for `ch_PP-OCRv4_rec.onnx`, record URL/ref, expected SHA-256, expected size, and charset count, then add controlled download/verify/rollback scripts.
