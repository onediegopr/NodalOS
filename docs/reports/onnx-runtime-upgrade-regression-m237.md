# NODAL OS M235-M237 - ONNX Runtime Upgrade Regression

## Decision

```text
M235+M236+M237 CERRADO / READY_FOR_DICTIONARY_COMPLETION
```

## Runtime Upgrade

- Previous runtime: `Microsoft.ML.OnnxRuntime 1.18.1`.
- Final runtime: `Microsoft.ML.OnnxRuntime 1.22.1`.
- Reason: M232-M234 proved `1.22.1` was the minimum tested candidate that avoided the recognizer `session.Run` native crash while preserving detector sanity.

## Model Verification

`verify-models.ps1` passed:

- `ch_PP-OCRv4_det.onnx`: verified.
- `ch_PP-OCRv4_rec.onnx`: verified.
- `ch_ppocr_mobile_v2.0_cls.onnx`: verified.

ONNX files remained gitignored and untracked.

## Detector Sanity

Detector-only sanity passed through the out-of-process probe runner.

Evidence:

- detector model loaded;
- session created;
- `session.Run` succeeded;
- output shape included `sigmoid_0.tmp_0=[1,1,640,640]`;
- detector postprocessing succeeded;
- boxes detected: `1`;
- no raw persistence;
- no SaaS;
- no-authority preserved.

## Recognizer Retest

Recognizer-only probes ran out-of-process with `Microsoft.ML.OnnxRuntime 1.22.1`.

Default recognizer results:

| Tensor | Result | Output |
| --- | --- | --- |
| zero | `RunSucceeded` | `softmax_2.tmp_0=[1,40,97]` |
| ones | `RunSucceeded` | `softmax_2.tmp_0=[1,40,97]` |
| gradient | `RunSucceeded` | `softmax_2.tmp_0=[1,40,97]` |
| checker | `RunSucceeded` | `softmax_2.tmp_0=[1,40,97]` |
| synthetic crop | `RunSucceeded` | `softmax_2.tmp_0=[1,40,97]` |
| high-contrast crop | `RunSucceeded` | `softmax_2.tmp_0=[1,40,97]` |
| detector-derived crop | `RunSucceeded` | `softmax_2.tmp_0=[1,40,97]` |

Candidate width `640` produced:

```text
softmax_2.tmp_0=[1,80,97]
```

The previous recognizer crash did not reproduce:

```text
0xC0000094 resolved: yes
```

## Dictionary / CTC Gate

Recognizer output class count remains:

```text
97
```

Current ASCII dictionary:

```text
86 characters / 87 with blank
```

Decode remains blocked by `ClassCountMismatch`. No dictionary was downloaded, no text was invented, and no positive OCR recognition was claimed.

## Safety

- No productive OCR.
- No SaaS OCR.
- No raw persistence.
- No full-screen OCR.
- No sensitive OCR.
- No OCR as authority.
- No shadow mode.
- No CDP pipeline integration.
- No risky OCR in-process.
- No `.onnx` committed.

## Next Step

Proceed to dictionary/CTC completion using a controlled manifest/source and verified hash path.
