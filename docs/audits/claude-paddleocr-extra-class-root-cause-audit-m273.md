# Claude Audit Prompt - PaddleOCR Extra Class Root Cause (M271-M273)

## Purpose

Independently verify that the PaddleOCR recognizer "extra class" (`dictionary + blank + 1`) is the space
character appended by `use_space_char: true`, and that the approved class policy stays no-authority.

## Claims to verify

1. **Mechanism.** In `PaddleOCR/ppocr/postprocess/rec_postprocess.py`, `BaseRecLabelDecode.__init__` appends
   `" "` when `use_space_char=true` (after reading the dictionary, before `add_special_char`), and
   `CTCLabelDecode.add_special_char` prepends `'blank'` at index 0. Net charset = `['blank'] + dict + [' ']`.
2. **Config.** PP-OCRv5 recognition configs set `use_space_char: true`.
3. **Arithmetic.** PP-OCRv4: `95 + blank + space = 97`. PP-OCRv5: `436 + blank + space = 438`. Space is the
   last index (`N+1`): 96 for v4, 437 for v5.
4. **Dictionary.** `ppocrv5_en_dict.txt` has `436` LF lines, no BOM, no space/empty line, SHA-256
   `e025a66d31f327ba0c232e03f407ae8d105e1e709e7ccb3f408aa778c24e70d6`. The space is not in the dictionary.
5. **Axis / softmax.** Output `[1,40,438]` is `[B,T,C]` (class = last axis); output is softmax
   (`softmax_x.tmp_0`); do not re-apply softmax.
6. **Behaviour.** M262-M264: extra class never wins argmax; max probability `0.2835` on `"12345"` while blank
   wins all 40 timesteps -> space as runner-up in padding columns, not dead padding, not unknown.

## What must NOT be claimed

- No productive OCR, no shadow mode, no decode-success claim from this milestone.
- The approval covers the index→space mapping only, not enabling recognition output.

## Checks

- [ ] Source lines for `use_space_char` append and `add_special_char` blank prepend confirmed upstream.
- [ ] PP-OCRv5 config `use_space_char: true` confirmed.
- [ ] Local dictionary re-audit reproduces `436` tokens, no space/empty line, matching SHA-256.
- [ ] `BuildCharsetLayout(436)` yields blank 0, dict 1..436, space 437, total 438.
- [ ] Harness: `OfficialSpaceToken` decodes `"12 34"`; `IgnoreExtraClass` collapses to `"1234"`.
- [ ] Harness: blank-dominant fixture -> empty decode, space is top-2 runner-up at ~0.28, never argmax.
- [ ] Decision = `READY_FOR_APPROVED_EXTRA_CLASS_POLICY`, `DecodeSuccessClaimed=false`,
      `ProductiveOcrBlocked=true`, `ShadowModeBlocked=true`, `NoAuthority=true`.
- [ ] No ONNX models or gitignored dictionaries committed.
