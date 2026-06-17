# ADR - M271-M273 PaddleOCR Extra Class Root Cause Decision

## Status

Accepted.

## Context

From M253 onward, the PaddleOCR English recognizers exposed one class more than `dictionary + blank`:

- PP-OCRv4 English: `95 + blank = 96`, observed `97`.
- PP-OCRv5 English: `436 + blank = 437`, observed `438`.

M256-M258 blocked on unresolved extra-class semantics; M262-M264 blocked further because the extra class,
while never winning argmax, carried non-trivial probability (max `0.2835` on `"12345"`). The semantics were
unknown, so decode stayed blocked.

## Decision

Adopt the **space-token** explanation as the officially evidenced root cause and approve the corresponding
**class policy**.

PaddleOCR `ppocr/postprocess/rec_postprocess.py` `BaseRecLabelDecode.__init__` appends `" "` to the
character list when `use_space_char: true` (after reading the dictionary, before special tokens), and
`CTCLabelDecode.add_special_char` prepends `'blank'` at index 0. The PP-OCRv5 recognition configs set
`use_space_char: true`. The resulting charset is:

```
['blank'] + dictionary + [' ']  =>  N + 2 classes,  space at index N + 1
```

This explains both families with one mechanism. Re-auditing the local dictionary confirmed it has `436`
tokens and contains no space/empty line, so the space at index 437 can only come from `use_space_char`. The
M262-M264 probe behaviour (space never wins, but is the non-trivial runner-up in padding columns) is exactly
how a real space token behaves against the CTC blank.

Final decision:

`M271+M272+M273 CERRADO / READY_FOR_APPROVED_EXTRA_CLASS_POLICY`

## Scope of the approval

- Approves only the **mapping**: extra class at index `N+1` is the space character.
- Does **not** enable productive OCR, shadow mode, or any decode-success claim.
- The hypothesis-only decoder harness stays no-authority and synthetic-fixture only.

## Consequences

- The extra-class semantics blocker is resolved; the PP-OCRv5 English pair is no longer blocked on "what is
  the extra class".
- Alternative-family review (M265-M270) is no longer the only forward path; PaddleOCR remains viable.
- Decoders must use charset `['blank'] + dictionary + [' ']`, treat the output as `[B,T,C]` softmax, and not
  re-apply softmax.
- Enabling actual recognition output remains gated by the separate productive-OCR readiness/activation gate
  and the project's permanent no-productive-OCR / no-shadow / no-authority constraints.
- The previous milestone snapshots (M256-M258, M262-M264) are preserved unchanged as historical records.
