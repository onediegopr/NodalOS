# NODAL OS M244-M246 - Recognizer Class Semantics / CTC Token Policy

## Decision

```text
M244+M245+M246 CERRADO / READY_FOR_RECOGNIZER_MODEL_DICTIONARY_SOURCE_REVIEW
```

## Runtime And Models

- Runtime: `Microsoft.ML.OnnxRuntime 1.22.1`.
- Provider: `CPUExecutionProvider`.
- Detection model: `ch_PP-OCRv4_det.onnx`, verified.
- Recognition model: `ch_PP-OCRv4_rec.onnx`, verified.
- Classification model: `ch_ppocr_mobile_v2.0_cls.onnx`, verified.
- ONNX models remain gitignored and untracked.

## Recognizer Metadata

Recognizer output:

```text
softmax_2.tmp_0=[1,40,97]
candidate width 640=[1,80,97]
```

Recognizer class count:

```text
97
```

ONNX embedded metadata includes a `character` entry with the same 95-token character sequence as the official English dictionary candidate.

## Source / Token Evidence

Official/verifiable candidates audited in M241-M243:

- RapidOCR/ModelScope `en_PP-OCRv4_rec_mobile/en_dict.txt`: `95` tokens, SHA-256 `5662df9d2d03f0e8ca0d3b0649d6acbab904b6a14b3d3521463c71c37c668ce3`, size `190`.
- PaddleOCR `release/2.8/ppocr/utils/en_dict.txt`: `95` tokens, same SHA-256 and size.
- ONNX embedded `character` metadata: `95` tokens, tied to recognizer model SHA-256 `e8770c967605983d1570cdf5352041dfb68fa0c21664f49f47b155abd3e0e318`.

PaddleOCR `CTCLabelDecode` evidence:

- `CTCLabelDecode.add_special_char` prepends `blank` before dictionary characters.
- `get_ignored_tokens()` returns `[0]`.
- Therefore official PaddleOCR CTC blank index is `0`.

With the audited 95-token dictionary:

```text
95 dictionary tokens + blank at index 0 = 96 classes
```

This does not explain recognizer output class count `97`.

## Candidate Token Policies

| Policy | Expected Classes | Evidence | Decode |
| --- | ---: | --- | --- |
| `DictionaryCharsOnly` | 95 | baseline only | blocked |
| `DictionaryPlusBlankAtStart` | 96 | official PaddleOCR CTC evidence | blocked by count mismatch |
| `DictionaryPlusBlankAtEnd` | 96 | legacy local assumption only | blocked |
| `DictionaryPlusBlankAndUnknown` | 97 | hypothesis only | blocked |
| `DictionaryPlusBlankAndPadding` | 97 | hypothesis only | blocked |
| `ModelDictionaryMismatch` | unresolved | 95-token source vs 97-class model | blocked |

## Decode Experiment

No real decode was attempted.

Reason:

```text
No approved token policy explains class count 97.
```

Hypothesis-only policies were evaluated as gates only:

- `95 chars + blank at start + unknown`: arithmetically explains 97, no evidence.
- `95 chars + blank at start + padding`: arithmetically explains 97, no evidence.

Both remain decode-blocked.

## Answered Questions

- Why does the recognizer have 97 classes? Unknown with current evidence.
- Is the 95-token source correct? It is official/verifiable and embedded in the ONNX metadata, but it does not explain 97 by itself.
- What tokens explain the 2 extra classes? No approved evidence. `blank + unknown/padding` are hypotheses only.
- Where is blank? Official PaddleOCR CTC convention places blank at index `0`.
- Is there evidence for unknown/padding/extra space? Not for this recognizer.
- Does policy permit decode? No.
- Decode result/confidence: none.

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
- No dictionary download.
- No invented token.
- No invented text.

## Next Step

Review the recognizer/model/dictionary pair at source level:

1. Confirm RapidOCR ONNX postprocessor semantics for `ch_PP-OCRv4_rec.onnx` class count `97`.
2. If a documented extra token exists, encode it as an approved token policy and retest decode.
3. If not, replace or re-export the recognizer/dictionary pair so class count and dictionary policy match.
