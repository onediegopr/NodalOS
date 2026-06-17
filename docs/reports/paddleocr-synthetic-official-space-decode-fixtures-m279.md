# PaddleOCR Synthetic Official Space Decode Fixtures - M279

## Decision

`M277+M278+M279 CERRADO / READY_FOR_ONNX_SYNTHETIC_RECOGNIZER_DECODE_PROBE`

This block validates PaddleOCR CTC decoding with the official `use_space_char=true` policy using deterministic synthetic probability matrices only. It does not run productive OCR, shadow mode, document OCR, screen OCR, or a CDP OCR pipeline.

## Policy Under Test

```text
blank index: 0
dictionary indexes: 1..N
space index: N+1
output layout: [B,T,C]
output softmax: already applied
softmax reapplied: false
```

`IgnoreExtraClass` remains unsafe because the extra class is the real space token.

## Synthetic Fixtures

The fixture set is probability-only and contains no images, raw crops, documents, screens, or sensitive data.

- `12 34`
- `PVC WALL`
- `A B C`
- `MARMOLES PVC`
- blank-dominant with space top-2
- space argmax in a real timestep
- CTC repeats: `LL`, `OO`, `11`
- intermediate blanks: `P blank V blank C`
- multiple spaces: `A  B`
- leading/trailing space: ` A `

## CTC Behavior

Repeated characters are represented with an intervening blank so CTC collapse emits both characters. Explicit intermediate blanks collapse without text loss. Multiple and boundary spaces are preserved in this synthetic no-authority fixture layer because the policy maps the official space index to `" "`.

## ONNX Probe

The ONNX recognizer synthetic probe was not attempted in this block. It is intentionally deferred to the next out-of-process ONNX recognizer decode probe gate. This avoids mixing a pure decoder-policy milestone with runtime model execution.

## Safety

- Productive OCR: blocked
- Shadow mode: blocked
- SaaS OCR: not used
- Raw persistence: blocked
- Full-screen OCR: blocked
- Sensitive OCR: blocked
- OCR as authority: blocked
- Real documents/screens: not used
- ONNX/dictionary binaries: not committed
