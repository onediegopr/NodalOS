# ADR: M262-M264 Extra Class Argmax Frequency Probe Decision

## Status

Accepted.

## Context

PP-OCRv5 English candidate recognizer runtime succeeds under ONNX Runtime `1.22.1`, but exposes output class count `438`. The explicit dictionary has `436` tokens and PaddleOCR CTC blank index `0`, explaining `437` classes. Class `437` remains unexplained.

Previous blocks rejected decode because no official evidence approved unknown/padding/reserved/ignored semantics for the extra class.

## Decision

Run an out-of-process argmax/probability probe over synthetic/redacted normal and extreme tensors. Do not decode. Do not approve any ignored-extra-class policy automatically.

Observed evidence:

- Output shape: `[1,40,438]`
- Extra class index: `437`
- Extra class argmax count: `0`
- Extra class max probability: `0.28353992104530334`
- Extra class average probability max: `0.11832536425208673`
- Invalid empty crop blocked before runtime
- Parent survived and temp cleanup completed

Decision:

`M262+M263+M264 CERRADO / BLOCKED_BY_EXTRA_CLASS_NONTRIVIAL_PROBABILITY`

## Consequences

- Decode remains blocked.
- Shadow mode remains blocked.
- Productive OCR remains blocked.
- Ignoring class `437` is not safe based only on argmax absence because probability is non-trivial.
- The evidence should be reviewed manually/externally before any decode policy approval.

## Safety

- No SaaS OCR.
- No raw persistence.
- No full-screen OCR.
- No sensitive OCR.
- No OCR authority.
- No risky OCR in-process.
- Risky probe ran only out-of-process.
