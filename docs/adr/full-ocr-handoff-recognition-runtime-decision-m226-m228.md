# ADR - M226-M228 Full OCR Handoff + Recognition Runtime Decision

## Status

Accepted.

## Context

M223-M225 proved that the detector alone is not the demonstrated blocker:

- detector session creation works
- detector `session.Run` works for zero/ones/gradient/synthetic/current-preprocessed/safe tensors
- detector postprocessing is reached
- detector postprocessing can produce a box

The remaining crash path is downstream:

`detector output/postprocessing -> box normalization -> crop extraction -> recognizer tensor preparation -> recognizer session.Run -> recognizer postprocessing -> dictionary/CTC`

## Decision

Add guarded out-of-process probes for:

- detector-to-recognizer handoff
- recognizer runtime
- dictionary/CTC class-count gate

Do not enable productive OCR or shadow mode.

Do not run risky OCR in-process.

Do not decode with an incompatible dictionary.

## Consequences

The system can now distinguish:

- handoff/crop failures before runtime
- recognizer runtime crashes
- recognizer output metadata success
- dictionary class-count mismatch
- postprocessing-only blockers

Current evidence shows recognizer session creation succeeds and output metadata is available, but recognizer `session.Run` crashes with `0xC0000094` even for zero/ones/gradient tensors. Dictionary mismatch remains documented but is not the primary blocker until recognizer runtime succeeds.

## Current Decision

`M226+M227+M228 CERRADO / READY_FOR_RECOGNIZER_RUNTIME_EXPERIMENT`

This decision does not authorize shadow mode, productive OCR, real documents, real screens, SaaS OCR, or OCR-as-authority.
