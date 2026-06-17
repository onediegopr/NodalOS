# NODAL OS M271-M273 - PaddleOCR Extra Class Root Cause + Decoder Policy Resolution

## Decision

`M271+M272+M273 CERRADO / READY_FOR_APPROVED_EXTRA_CLASS_POLICY`

## Summary

M256-M270 repeatedly blocked on the recognizer "extra class": both PaddleOCR English recognizers expose
exactly one class more than `dictionary + blank`:

- PP-OCRv4 English: `95` dictionary tokens + blank = `96`, observed `97`.
- PP-OCRv5 English: `436` dictionary tokens + blank = `437`, observed `438`.

M271-M273 resolves the root cause with official evidence: **the extra class is the space character `" "`,
appended by PaddleOCR `use_space_char: true`.** The identical `+1` across both model families is the
signature of a single appended token, and `use_space_char` is the only PaddleOCR mechanism that appends
exactly one character uniformly. No EOS/SOS, unknown, padding, reserved, parser-loss, or export-artifact
hypothesis is needed.

Final charset under PaddleOCR CTC + `use_space_char`:

```
['blank'] + dictionary[1..N] + [' ']   =>   N + 2 classes
index 0      = blank (CTC)
index 1..N   = dictionary tokens
index N + 1  = space  (" ")
```

- PP-OCRv5: `blank(0) + dict(1..436) + space(437) = 438`. **Space is index 437.**
- PP-OCRv4: `blank(0) + dict(1..95) + space(96) = 97`. **Space is index 96.**

## M271 - Root Cause Audit

### Official source evidence

- `PaddleOCR/ppocr/postprocess/rec_postprocess.py`, `BaseRecLabelDecode.__init__`: after reading the
  dictionary file line by line (`line.decode('utf-8').strip("\n").strip("\r\n")`), it runs
  `if use_space_char: self.character_str.append(" ")` — the space is appended **after** the dictionary and
  **before** `add_special_char`.
- `CTCLabelDecode.add_special_char`: `dict_character = ['blank'] + dict_character` — blank is prepended at
  index 0. Net charset = `['blank'] + dictionary + [' ']`.
- `configs/rec/PP-OCRv5/PP-OCRv5_*_rec.yml` set `use_space_char: true` with
  `character_dict_path` pointing at the PP-OCRv5 dictionary. The English PP-OCRv4/PP-OCRv5 recognizers were
  exported the same way.

### Dictionary re-audit (maximum precision)

`tools/ocr-worker/models/onnx/dictionaries/ppocrv5_en_dict.txt`:

- SHA-256 `e025a66d31f327ba0c232e03f407ae8d105e1e709e7ccb3f408aa778c24e70d6`, `1416` bytes.
- `436` LF-terminated lines, no BOM (`30 0a ...`), trailing final newline present (`... e2 98 ba 0a`).
- No standalone space line (`^ $`), no empty line (`^$`), no line containing a space.
- Multibyte tokens preserved (last token is `U+263A`).

So the dictionary contributes exactly `436` tokens and **does not** itself contain a space; the space at
index 437 can only come from `use_space_char`. This rules out "the parser lost a space token" and "terminal
newline is a token".

### Output axis / softmax

- Output shape `[1, 40, 438]` = `[B, T, C]`, class dimension is the **last** axis (C = 438). No transpose is
  required for greedy CTC.
- The output node is a softmax (`softmax_x.tmp_0`); values already sum to ~1 per timestep
  (`LooksLikeProbabilityVector` confirmed in the probe runner). The decoder must **not** re-apply softmax —
  it would not change argmax but would distort confidence.

### Why not the other hypotheses

| Hypothesis | Explains +1 on both | Evidence | Verdict |
| --- | --- | --- | --- |
| **Space (`use_space_char`)** | **Yes** | **Official source + config + dict re-audit + probe behaviour** | **Approved** |
| Ignored / reserved extra class | Yes (arithmetic only) | None | Rejected (silently loses real spaces) |
| Unknown token | Yes (arithmetic only) | CTCLabelDecode adds none | Rejected |
| Padding token | Yes (arithmetic only) | CTC path has no padding token | Rejected |
| EOS/SOS | No (would add ≥1 elsewhere) | Attn decoders only, not CTC | Rejected |
| Parser loses a space line | No | Dict has no space/empty line | Rejected |
| Export artifact / pair mismatch | Yes (arithmetic only) | None positive | Superseded by space evidence |

## M272 - Experimental Decoder Policy Harness

`NodalOsPaddleOcrSpaceTokenDecoderService` implements a deterministic, **hypothesis-only**, no-authority
greedy CTC decoder over a `[T, C]` softmax matrix, comparing extra-class policies. It is exercised with
synthetic, ONNX-free probability fixtures so it is fully unit-testable.

Synthetic charset = `blank + '0'..'9' + ' '` (`C = 12`, space index `11`).

| Policy | Fixture `"12 34"` decode | Note |
| --- | --- | --- |
| `OfficialSpaceToken` (approved) | `"12 34"` | space index emits `" "` |
| `HypothesisIgnoreExtraClass` | `"1234"` | silently drops the real space |
| `HypothesisUnknownToken` | `"12�34"` | emits replacement char |
| `HypothesisPaddingToken` | `"1234"` | drops as padding |
| `BlankOnlyClassCountMismatch` | (no decode) | expects `N+1`, model exposes `N+2` |

### Top-k / per-timestep evidence (real model, M262-M264 + harness)

- Real model (M262-M264 out-of-process probe): the extra class (index 437) **never** wins argmax across all
  normal and extreme fixtures (`extraClassArgmaxCount = 0`). On `"12345"` its max probability is `0.2835`
  while blank wins all 40 timesteps. Synthetic harness reproduces this exactly: blank wins every timestep,
  space is the **runner-up** (top-2) with probability ~`0.28`, and nothing decodes.
- Interpretation: an elevated-but-never-winning extra class in padding/separator columns is the precise
  behaviour of a real **space** token competing with the CTC **blank**. Dead padding would sit near `0`; an
  unknown token would spike on the `OutOfDictionary` fixture (it does not).

## M273 - Decision

The space-token **class policy** is approved by official evidence. The extra-class semantics gate
(`BLOCKED_BY_EXTRA_CLASS_SEMANTICS` / `BLOCKED_BY_EXTRA_CLASS_NONTRIVIAL_PROBABILITY`) is **resolved**.

Decision: `READY_FOR_APPROVED_EXTRA_CLASS_POLICY`.

This approval covers only the **mapping** (index `N+1` → space). It does **not** enable productive OCR, does
**not** claim decode success, and does **not** lift the project-wide no-productive-OCR / no-shadow posture.
Turning the approved mapping into actual recognition output is a separate downstream readiness/activation
gate with its own constraints.

## Safety

- No productive OCR. No shadow mode. No SaaS. No API keys.
- No raw image persistence. No full-screen. No sensitive data. No-authority.
- No ONNX models or gitignored dictionaries committed.
- Experimental decode is hypothesis-only, runs on synthetic fixtures, and never promotes readiness.

## Artifacts

- `artifacts/ocr-vision-onnx/m273/paddleocr-extra-class-root-cause-summary.json`
- `docs/audits/claude-paddleocr-extra-class-root-cause-audit-m273.md`
- `docs/adr/paddleocr-extra-class-root-cause-decision-m271-m273.md`
- Code: `src/OneBrain.BrowserExecutor.Contracts/NodalOsPaddleOcrSpaceTokenDecoderContracts.cs`,
  `src/OneBrain.BrowserExecutor.Cdp/NodalOsPaddleOcrSpaceTokenDecoderServices.cs`
- Tests: `tests/OneBrain.Safety.Tests/NodalOsPaddleOcrSpaceTokenDecoderM271M273Tests.cs`
