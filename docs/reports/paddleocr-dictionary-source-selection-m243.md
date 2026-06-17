# NODAL OS M241-M243 - PaddleOCR Dictionary Source Selection

## Decision

```text
M241+M242+M243 CERRADO / BLOCKED_BY_DICTIONARY_COUNT_MISMATCH
```

## Runtime And Models

- Runtime: `Microsoft.ML.OnnxRuntime 1.22.1`.
- Provider: `CPUExecutionProvider`.
- Detection model: `ch_PP-OCRv4_det.onnx`, verified.
- Recognition model: `ch_PP-OCRv4_rec.onnx`, verified.
- Classification model: `ch_ppocr_mobile_v2.0_cls.onnx`, verified.
- ONNX models remain gitignored and untracked.

## Recognizer / Dictionary Requirement

Recognizer output class count:

```text
97
```

Current gate requires:

```text
96 dictionary tokens + 1 CTC blank = 97
```

Current embedded ASCII fixture dictionary remains incompatible:

```text
86 tokens + 1 blank = 87
```

No decode was attempted.

## Source Audit

### Candidate 1 - RapidOCR / ModelScope

- source id: `rapidocr-modelscope-v3.8.0-en-ppocrv4-en-dict`.
- URL: `https://www.modelscope.cn/models/RapidAI/RapidOCR/resolve/v3.8.0/paddle/PP-OCRv4/rec/en_PP-OCRv4_rec_mobile/en_dict.txt`.
- provider: `RapidAI/ModelScope`.
- provenance: `RapidAI/RapidOCR v3.8.0` default model config lists this `dict_url` for `en_PP-OCRv4_rec_mobile` in non-ONNX formats.
- license: Apache-2.0 lineage from PaddleOCR/RapidOCR distribution.
- size: `190` bytes.
- SHA-256: `5662df9d2d03f0e8ca0d3b0649d6acbab904b6a14b3d3521463c71c37c668ce3`.
- token count: `95`.
- blank included: no.
- official/verifiable: yes.
- result: rejected, count mismatch.

### Candidate 2 - PaddleOCR GitHub

- source id: `paddleocr-github-release-2.8-en-dict`.
- URL: `https://raw.githubusercontent.com/PaddlePaddle/PaddleOCR/release/2.8/ppocr/utils/en_dict.txt`.
- provider: `PaddlePaddle`.
- provenance: official PaddleOCR repo.
- license: Apache-2.0.
- size: `190` bytes.
- SHA-256: `5662df9d2d03f0e8ca0d3b0649d6acbab904b6a14b3d3521463c71c37c668ce3`.
- token count: `95`.
- blank included: no.
- official/verifiable: yes.
- result: rejected, count mismatch.

### Candidate 3 - ONNX Embedded Character Metadata

- source id: `onnx-recognizer-embedded-character-metadata`.
- ref: `tools/ocr-worker/models/onnx/ch_PP-OCRv4_rec.onnx#metadata:character`.
- provider: `RapidAI/ModelScope`.
- provenance: verified local ONNX recognizer.
- model SHA-256: `e8770c967605983d1570cdf5352041dfb68fa0c21664f49f47b155abd3e0e318`.
- model size: `7653044`.
- observed metadata key: `character`.
- observed token count: `95`.
- official/verifiable: yes, via pinned ONNX model.
- result: rejected, count mismatch.

## M241 Finding

An official/verifiable source exists and its hash/size can be pinned, but it does not satisfy the existing exact compatibility contract:

```text
official source tokens: 95
required tokens: 96
recognizer classes: 97
```

This means M241 found candidates but rejected them as `SourceRejectedCountMismatch`.

## M242 Finding

No manifest entry was promoted to `Pinned` because no candidate matched the required character count.

Hash/size were calculated only for audit:

```text
sha256=5662df9d2d03f0e8ca0d3b0649d6acbab904b6a14b3d3521463c71c37c668ce3
size=190
count=95
```

No dictionary file was added to the repo and no dictionary was treated as verified for decode.

## M243 Acquisition Gate

Acquisition scripts were not created as active downloaders:

- `tools/ocr-worker/models/onnx/download-dictionaries.ps1`: not created.
- `tools/ocr-worker/models/onnx/verify-dictionaries.ps1`: not created.
- `tools/ocr-worker/models/onnx/rollback-dictionaries.ps1`: not created.

Reason:

```text
No source satisfies 96 dictionary tokens + blank = 97.
```

Rollback cannot delete ONNX models because no dictionary rollback script exists yet, and future rollback must be scoped only to dictionary files.

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
- No dictionary downloaded into repo.
- No decode attempted.
- No text invented.

## Next Step

Resolve the model/dictionary semantics before acquisition:

1. Confirm whether `ch_PP-OCRv4_rec.onnx` expects an extra non-dictionary class beyond CTC blank.
2. If yes, update the CTC contract to explicitly model that extra class and add tests.
3. If no, select a different recognizer model or an approved 96-token dictionary source.
