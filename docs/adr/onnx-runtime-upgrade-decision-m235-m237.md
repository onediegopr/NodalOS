# ADR - M235-M237 ONNX Runtime Upgrade Decision

## Status

Accepted.

## Context

`Microsoft.ML.OnnxRuntime 1.18.1` allowed detector execution but caused recognizer `session.Run` to crash with:

```text
-1073741676 / 0xC0000094
```

M232-M234 tested candidate versions and found that `1.22.1` was the minimum tested version that avoided the recognizer crash while preserving detector sanity.

## Decision

Permanently upgrade:

```text
Microsoft.ML.OnnxRuntime 1.18.1 -> 1.22.1
```

## Evidence

- Restore succeeded.
- Build succeeded.
- ONNX model verification succeeded.
- Detector sanity succeeded.
- Recognizer zero/ones/gradient/checker/crop probes succeeded out-of-process.
- Recognizer output shape: `softmax_2.tmp_0=[1,40,97]`.
- Previous native crash `0xC0000094` did not recur.

## Consequences

- Runtime crash blocker is resolved for recognizer probes.
- Recognition decode is still blocked by dictionary/CTC mismatch.
- Next route is controlled dictionary completion.
- Productive OCR and shadow mode remain blocked.

## Decision Text

```text
M235+M236+M237 CERRADO / READY_FOR_DICTIONARY_COMPLETION
```
