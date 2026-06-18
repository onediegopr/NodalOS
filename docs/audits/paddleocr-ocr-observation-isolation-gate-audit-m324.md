# Audit: PaddleOCR OCR Observation Isolation Gate M324

## Base

- base commit: `d756d0c`
- branch: `chrome-lab-001-extension-local-ai-bridge`

## Audit targets

- QA-window isolation
- bounded region verification
- fingerprint parity
- confidence gate
- evidence-only enforcement

## Findings

- The previous contamination path came from desktop-surface capture.
- The QA host now emits capture metadata and a non-sensitive fingerprint from a client-surface render.
- The runner verifies:
  - title/process
  - window and region bounds
  - bounded region placement
  - capture technique
  - fingerprint equality
  - acceptance state

## Result

- isolation gate attempted: yes
- region verification attempted: yes
- confidence gate attempted: yes
- accepted evidence: `3`
- rejected evidence: `0`
- uncertain evidence: `0`

## Residual limitations

- `PVC WALL` and `12 34` still show one-character OCR drift
- those drifts are bounded and recorded as non-authoritative evidence
- foreground activation may remain false under Windows focus rules; this is tolerated only because capture is taken from the QA window surface, not from the shared desktop

## Conclusion

The observation path is sufficiently isolated for internal evidence integration under the low-risk QA-window policy.
