# Claude Audit Prompt - M255 PP-OCRv5 Recognizer Pair Acquisition

Audit NODAL OS M253-M255.

Verify:

- Candidate manifest and dedicated acquisition scripts.
- Model URL, SHA-256, and pinned size.
- Dictionary URL, SHA-256, pinned size, token count `436`.
- CTC blank policy index `0`.
- Out-of-process recognizer runtime smoke.
- Runtime output class count observed as `438`.
- Expected dictionary+blank class count `437`.
- That class mismatch blocks decode and readiness.
- That no binaries were committed.
- That rollback only targets candidate files and refuses protected current model paths.
- That no raw/no sensitive/no full-screen/no SaaS/no-authority were preserved.

Recommend the next block: PP-OCRv5 class semantics reconciliation before any synthetic decode fixture.
