# ADR - M250-M252 Recognizer Dictionary Pair Replacement Selection

## Status

Accepted.

## Context

The current PP-OCRv4 English recognizer exposes `97` classes, while the official/verifiable English dictionary exposes `95` effective PaddleOCR parser tokens. With CTC blank at index `0`, that explains `96` classes, not `97`.

## Decision

Select the RapidOCR/ModelScope PP-OCRv5 English mobile recognizer pair for controlled acquisition:

- Model: `en_PP-OCRv5_rec_mobile.onnx`
- Dictionary: `ppocrv5_en_dict.txt`
- Dictionary tokens: `436`
- Expected output classes: `437`
- Model SHA-256 pinned from RapidOCR `default_models.yaml`
- Dictionary SHA-256 and size pinned from audited dictionary text

Final decision:

`M250+M251+M252 CERRADO / READY_FOR_RECOGNIZER_DICTIONARY_PAIR_ACQUISITION`

## Consequences

- No current model replacement is performed in this block.
- No decode is attempted.
- M253-M255 must add manifest/script support, download via controlled scripts, verify hash/size, and run out-of-process runtime probes before any decode gate.
