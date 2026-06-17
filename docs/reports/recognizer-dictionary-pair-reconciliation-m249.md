# NODAL OS M249 - Recognizer Dictionary Pair Reconciliation

## Decision

`M247+M248+M249 CERRADO / READY_FOR_RECOGNIZER_MODEL_DICTIONARY_PAIR_REPLACEMENT`

## Scope

This report reconciles the mismatch between the verified recognizer output class count (`97`), PaddleOCR documentation that describes `ppocr/utils/en_dict.txt` as an English dictionary with `96` characters, and raw dictionary/config sources that expose `95` effective PaddleOCR tokens.

No productive OCR was enabled. No document/screen OCR was executed. No decode was attempted.

## Runtime And Model State

- ONNX Runtime: `Microsoft.ML.OnnxRuntime 1.22.1`
- Provider: `CPUExecutionProvider`
- Detection model: verified.
- Recognition model: verified.
- Classification model: verified.
- Recognizer output metadata: `softmax_2.tmp_0=[1,40,97]`, width-640 candidate `[1,80,97]`.
- Recognizer output class count: `97`.

## Sources Audited

| Source | Ref | Branch/tag | Bytes | SHA-256 | Raw line segments | PaddleOCR parser tokens | Space token | Terminal empty line |
| --- | --- | --- | ---: | --- | ---: | ---: | --- | --- |
| PaddleOCR `en_dict.txt` | `https://raw.githubusercontent.com/PaddlePaddle/PaddleOCR/release/2.8/ppocr/utils/en_dict.txt` | `release/2.8` | `190` | `5662df9d2d03f0e8ca0d3b0649d6acbab904b6a14b3d3521463c71c37c668ce3` | `96` | `95` | yes | yes |
| PaddleOCR `en_dict.txt` | `https://raw.githubusercontent.com/PaddlePaddle/PaddleOCR/main/ppocr/utils/en_dict.txt` | `main` | `190` | `5662df9d2d03f0e8ca0d3b0649d6acbab904b6a14b3d3521463c71c37c668ce3` | `96` | `95` | yes | yes |
| RapidOCR/ModelScope dictionary | `https://www.modelscope.cn/models/RapidAI/RapidOCR/resolve/v3.8.0/paddle/PP-OCRv4/rec/en_PP-OCRv4_rec_mobile/en_dict.txt` | `RapidOCR v3.8.0` | `190` | `5662df9d2d03f0e8ca0d3b0649d6acbab904b6a14b3d3521463c71c37c668ce3` | `96` | `95` | yes | yes |
| HuggingFace config `character_dict` | `https://huggingface.co/PaddlePaddle/en_PP-OCRv4_mobile_rec/raw/main/config.json` | `main` | `4114` | `c1ebffdece1c8eb30515cc3df9bed55c8806fbc57e4231ec7be83f43c57a3660` | n/a | `95` characters | yes | n/a |

## Parser Findings

PaddleOCR's `BaseRecLabelDecode` reads dictionary files line by line and strips only CR/LF from each physical line. The single-space line is preserved as a token. A terminal newline does not become an additional token under the documented parser behavior.

PaddleOCR's `CTCLabelDecode` prepends `blank` and ignores token index `0`.

Therefore:

- Effective dictionary tokens under PaddleOCR parser: `95`.
- CTC blank index: `0`.
- Expected CTC classes from this dictionary: `96`.
- Current recognizer output classes: `97`.

The documentation statement that `en_dict.txt` has `96` characters appears to align with raw LF segment count, not effective PaddleOCR parser tokens. There is no approved evidence that the terminal empty segment is a valid model token.

## ONNX Metadata

The recognizer ONNX metadata `character` observed in prior milestones exposes `95` tokens. This matches the raw source dictionaries and HuggingFace config character list, not the `97` output class count.

## Compatibility Finding

The current recognizer/dictionary pair remains unresolved:

- `95` official/verifiable dictionary tokens.
- `+1` CTC blank at index `0`.
- Expected classes: `96`.
- Actual recognizer output classes: `97`.

No verified `96`-token source was found. No space/empty-line handling policy can be approved to explain `97` without inventing a token.

## Readiness

- Decode attempted: no.
- Decode allowed: no.
- Productive OCR: blocked.
- Shadow mode: blocked.
- No raw persistence: preserved.
- No sensitive/full-screen OCR: preserved.
- No SaaS: preserved.
- No-authority: preserved.

## Next Gate

Recommended next block: review or replace the recognizer/model/dictionary pair. The current model artifact may not match the official English CTC dictionary semantics despite being sourced from the English PP-OCRv4 recognizer URL.
