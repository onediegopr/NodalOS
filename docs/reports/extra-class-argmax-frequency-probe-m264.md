# M262-M264 Extra Class Argmax Frequency Probe

## Decision

`M262+M263+M264 CERRADO / BLOCKED_BY_EXTRA_CLASS_NONTRIVIAL_PROBABILITY`

The PP-OCRv5 English candidate recognizer was probed out-of-process with synthetic/redacted tensors only. Output shape was consistently `[1,40,438]` for runnable fixtures. Extra class index `437` did not appear as argmax, but its probability was non-trivial in multiple fixtures, with maximum observed probability `0.28353992104530334` on `12345`.

Decode remains blocked. No productive OCR, SaaS OCR, full-screen OCR, sensitive OCR, raw image persistence, shadow mode, or authority use was enabled.

## Model And Dictionary

- Runtime: `Microsoft.ML.OnnxRuntime 1.22.1`
- Provider: `CPUExecutionProvider`
- Candidate recognizer: `en_PP-OCRv5_rec_mobile.onnx`
- Candidate dictionary: `ppocrv5_en_dict.txt`
- Dictionary tokens: `436`
- PaddleOCR CTC blank index: `0`
- Expected explained classes: `437`
- Observed output classes: `438`
- Extra class index: `437`

## Fixtures

Normal fixtures:

- `TEST`
- `NODAL`
- `12345`
- `ABC123`
- `HighContrastCrop`
- `DetectorDerivedCrop`

Extreme fixtures:

- `Black`
- `White`
- `DeterministicNoise`
- `Gradient`
- `ThinLines`
- `Checkerboard`
- `OutOfDictionary`
- `InvalidEmptyCrop`

`InvalidEmptyCrop` was blocked before recognizer runtime.

## Probe Results

All runnable fixtures executed out-of-process. Parent survived and temp cleanup completed.

| Fixture | Status | Output | Extra Argmax | Extra Max Prob | Extra Avg Prob | Blank Argmax |
|---|---:|---:|---:|---:|---:|---:|
| `TEST` | `ExtraClassProbabilityNonTrivial` | `[1,40,438]` | 0 | 0.006049194373190403 | 0.0003459132923799402 | 14 |
| `NODAL` | `ExtraClassProbabilityNonTrivial` | `[1,40,438]` | 0 | 0.00561612518504262 | 0.0006766940032782998 | 21 |
| `12345` | `ExtraClassProbabilityNonTrivial` | `[1,40,438]` | 0 | 0.28353992104530334 | 0.11832536425208673 | 40 |
| `ABC123` | `ExtraClassProbabilityNonTrivial` | `[1,40,438]` | 0 | 0.0013852466363459826 | 0.0002338095976938348 | 22 |
| `HighContrastCrop` | `ExtraClassProbabilityNonTrivial` | `[1,40,438]` | 0 | 0.009588144719600677 | 0.003846854894072749 | 40 |
| `DetectorDerivedCrop` | `ExtraClassProbabilityNonTrivial` | `[1,40,438]` | 0 | 0.018164407461881638 | 0.005160853853885783 | 37 |
| `Black` | `ExtraClassProbabilityNonTrivial` | `[1,40,438]` | 0 | 0.04457041248679161 | 0.03775421182799619 | 40 |
| `White` | `ExtraClassProbabilityNonTrivial` | `[1,40,438]` | 0 | 0.05238724127411842 | 0.043566674008616246 | 40 |
| `DeterministicNoise` | `ExtraClassProbabilityNonTrivial` | `[1,40,438]` | 0 | 0.03556954115629196 | 0.01450828839733731 | 40 |
| `Gradient` | `ExtraClassProbabilityNonTrivial` | `[1,40,438]` | 0 | 0.02607089839875698 | 0.015231635418604127 | 40 |
| `ThinLines` | `ExtraClassProbabilityNonTrivial` | `[1,40,438]` | 0 | 0.032856691628694534 | 0.015457652113400399 | 40 |
| `Checkerboard` | `ExtraClassProbabilityNonTrivial` | `[1,40,438]` | 0 | 0.002016042824834585 | 0.0011714985434082338 | 39 |
| `OutOfDictionary` | `ExtraClassProbabilityNonTrivial` | `[1,40,438]` | 0 | 0.01840442419052124 | 0.009666402610309887 | 40 |
| `InvalidEmptyCrop` | `ProbeBlockedByInvalidInput` | n/a | n/a | n/a | n/a | n/a |

## Risk Classification

- Extra class argmax count: `0`
- Extra class argmax fixtures: none
- Extra class max probability: `0.28353992104530334`
- Extra class average probability maximum: `0.11832536425208673`
- Threshold: `0.001`
- Risk classification: `ManualReviewRequired`
- Policy candidate: blocked, not approved

Even though class `437` was never argmax, the observed probability is not negligible. Ignoring class `437` would be unsafe without an explicit policy approval from a deeper audit.

## Safety

- Decode attempted: no
- Productive OCR: blocked
- SaaS OCR: not used
- Raw persistence: none
- Full-screen OCR: none
- Sensitive OCR: none
- OCR authority: none
- Shadow mode: blocked
- Risky OCR in-process: no
- Risky OCR out-of-process: yes

## Next Step

Use this probe evidence in the Claude/manual extra-class audit. Do not approve `ignored-extra-class` while class `437` has non-trivial probability.
