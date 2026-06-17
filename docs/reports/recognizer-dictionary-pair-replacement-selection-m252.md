# NODAL OS M252 - Recognizer Dictionary Pair Replacement Selection

## Decision

`M250+M251+M252 CERRADO / READY_FOR_RECOGNIZER_DICTIONARY_PAIR_ACQUISITION`

## Scope

This block selects a replacement recognizer+dictionary pair after the current PP-OCRv4 English pair could not explain `97` recognizer output classes with the official `95`-token dictionary and CTC blank at index `0`.

No new model was downloaded. No decode was attempted. No productive OCR or shadow mode was enabled.

## Current Pair Status

- Current recognizer: `en_PP-OCRv4_rec_mobile.onnx` stored locally as `ch_PP-OCRv4_rec.onnx`.
- Current recognizer hash: `e8770c967605983d1570cdf5352041dfb68fa0c21664f49f47b155abd3e0e318`.
- Current output classes: `97`.
- Current dictionary: `en_dict.txt`.
- Current dictionary tokens under PaddleOCR parser: `95`.
- Current expected classes with CTC blank index `0`: `96`.
- Status: rejected for decode.

## Candidate Matrix

| Candidate | Source | ONNX | Explicit dictionary | Dict tokens | Expected classes | Hash/size pinning | Decision |
| --- | --- | --- | --- | ---: | ---: | --- | --- |
| `rapidocr-modelscope-ppocrv5-en-mobile-onnx` | RapidOCR/ModelScope v3.8.0 | yes | yes, `ppocrv5_en_dict.txt` | `436` | `437` | model SHA pinned; dictionary SHA+size pinned | accepted for acquisition |
| `rapidocr-modelscope-ppocrv5-latin-mobile-onnx` | RapidOCR/ModelScope v3.8.0 | yes | yes, `ppocrv5_latin_dict.txt` | `502` | `503` | model SHA pinned; dictionary SHA+size pinned | manual review due broader charset/migration impact |
| `rapidocr-modelscope-ppocrv4-en-current-onnx` | RapidOCR/ModelScope v3.8.0 | yes | yes, `en_dict.txt` | `95` | `97` actual, `96` expected | pinned | rejected count mismatch |
| `paddleocr-huggingface-ppocrv4-mobile-rec` | PaddlePaddle/HuggingFace | no approved ONNX | no explicit dictionary | unknown | unknown | not pinned | rejected metadata-only/no ONNX |

## Selected Candidate

- Candidate id: `rapidocr-modelscope-ppocrv5-en-mobile-onnx`.
- Model URL: `https://www.modelscope.cn/models/RapidAI/RapidOCR/resolve/v3.8.0/onnx/PP-OCRv5/rec/en_PP-OCRv5_rec_mobile.onnx`.
- Model SHA-256: `c3461add59bb4323ecba96a492ab75e06dda42467c9e3d0c18db5d1d21924be8`.
- Dictionary URL: `https://www.modelscope.cn/models/RapidAI/RapidOCR/resolve/v3.8.0/paddle/PP-OCRv5/rec/en_PP-OCRv5_rec_mobile/ppocrv5_en_dict.txt`.
- Dictionary SHA-256: `e025a66d31f327ba0c232e03f407ae8d105e1e709e7ccb3f408aa778c24e70d6`.
- Dictionary size: `1416` bytes.
- Dictionary tokens: `436`.
- Expected classes with CTC blank index `0`: `437`.
- License/provenance: Apache-2.0 lineage, RapidOCR/ModelScope model distribution.

## Acquisition Plan For M253-M255

1. Add a new recognizer model manifest entry without replacing the current recognizer yet.
2. Add a dictionary manifest entry for `ppocrv5_en_dict.txt`.
3. Create or extend controlled download/verify/rollback scripts.
4. Download the new ONNX recognizer only through the approved scripts.
5. Verify SHA-256 and file size.
6. Verify ONNX metadata/output class count in an out-of-process probe.
7. Verify dictionary token count and CTC blank policy.
8. Run recognizer runtime probes out-of-process.
9. Keep fallback to current recognizer until the new pair passes runtime and decode gates.

## Safety

- Decode attempted: no.
- Productive OCR: blocked.
- Shadow mode: blocked.
- No raw persistence: preserved.
- No full-screen/sensitive OCR: preserved.
- No SaaS: preserved.
- No-authority: preserved.
