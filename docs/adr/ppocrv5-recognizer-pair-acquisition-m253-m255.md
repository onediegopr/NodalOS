# ADR - M253-M255 PP-OCRv5 Recognizer Pair Acquisition

## Status

Accepted.

## Context

M250-M252 selected the RapidOCR/ModelScope PP-OCRv5 English recognizer+dictionary pair for acquisition because the pair was explicit and pinnable.

## Decision

Acquire and verify the candidate pair, but do not approve decode.

The model and dictionary verified successfully:

- `en_PP-OCRv5_rec_mobile.onnx`: SHA-256 `c3461add59bb4323ecba96a492ab75e06dda42467c9e3d0c18db5d1d21924be8`, size `7872351`.
- `ppocrv5_en_dict.txt`: SHA-256 `e025a66d31f327ba0c232e03f407ae8d105e1e709e7ccb3f408aa778c24e70d6`, size `1416`, `436` tokens.

Runtime smoke succeeded out-of-process, but the recognizer output class count is `438`, while the explicit dictionary plus CTC blank index `0` explains `437`.

Final decision:

`M253+M254+M255 PARCIAL`

## Consequences

- Candidate pair remains verified but not decode-ready.
- Productive OCR and shadow mode remain blocked.
- Next block must reconcile PP-OCRv5 token/class semantics before decode.
