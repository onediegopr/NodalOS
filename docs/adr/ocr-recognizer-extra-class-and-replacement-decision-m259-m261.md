# ADR - M259-M261 OCR Recognizer Extra Class and Replacement Decision

## Status

Accepted.

## Context

M256-M258 closed as `BLOCKED_BY_EXTRA_CLASS_SEMANTICS`. Both current PP-OCRv4 and candidate PP-OCRv5 recognizers run with ONNX Runtime `1.22.1`, but both expose one more output class than dictionary plus CTC blank explains.

## Decision

Do not approve decode and do not replace the model immediately.

Prepare an external deep audit package and close the next gate as:

`M259+M260+M261 CERRADO / READY_FOR_CLAUDE_EXTRA_CLASS_AUDIT`

## Rationale

Manual approval of an ignored/unknown/padding/reserved class would be possible only with explicit risk acceptance, but it should not be automatic. A replacement search is valid if the deep audit cannot identify an official/verifiable class convention.

## Consequences

- OCR productive mode remains blocked.
- Shadow mode remains blocked.
- Decode remains blocked.
- No model is downloaded or replaced in this block.
- Next route is external semantics audit, followed by either manual decode policy approval, recognizer model replacement search, or alternative local OCR family review.
