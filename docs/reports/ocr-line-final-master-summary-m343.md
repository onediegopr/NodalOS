# OCR Line Final Master Summary M343

## Final State

The OCR line from M197 through M339 remains architecturally intact and mature:

- ONNX .NET is the active OCR runtime path.
- OCR remains no-authority.
- OCR cannot authorize actions.
- OCR cannot approve click, submit, send, delete, pay, or sign.
- Accepted OCR enters only as auxiliary evidence.
- Rejected and uncertain OCR enter only as diagnostic evidence.
- Verified low-risk assisted verification remains read-only and evidence-only.

## Active OCR Path

- `Microsoft.ML.OnnxRuntime` pinned at `1.22.1` in [OneBrain.BrowserExecutor.Cdp.csproj](C:/DESARROLLO/NodalOS/Codigo-m12-audit/src/OneBrain.BrowserExecutor.Cdp/OneBrain.BrowserExecutor.Cdp.csproj)
- Regional QA-window capture and evidence envelope path
- Audit-ledger consumer
- FSM read-only observation consumer
- Assisted verification policy and controlled fixtures

## Official Decode / Preprocess Policy

- `OfficialSpaceToken`
- blank index `0`
- dictionary `1..436`
- space `437`
- `SoftmaxReapplied=false`
- recognizer resize mode `RatioPreservingRightPad`

## Legacy Status

### Python worker line

Reference graph shows the Python worker still exists in:

- historical diagnostic services and contracts
- historical synthetic-run services
- historical tests
- historical docs and runbooks

It does **not** participate in the active OCR path. The Python worker surface is now explicitly deprecated rather than removed.

### ONNX runtime version experiment

Reference graph shows the runtime-version experiment still exists in:

- recognizer compatibility diagnostics
- historical tests and contracts

It does **not** drive the active OCR path. The experiment surface is now explicitly deprecated rather than removed.

## Artifact Hygiene

- `m177` and `m183` are currently stable after restore/build/test in this worktree.
- They are referenced by tests and audit docs only.
- Active code does not write to those tracked artifact paths.
- Historical tests that previously overwrote those tracked files now write to temporary output under `%TEMP%/nodal-os-ocr-tests/`.

## Non-Blocking Legacy Remaining

- Historical Python worker scripts and adapter remain for auditability.
- Historical ONNX runtime version experiment planner and decision service remain for auditability.

## Final Decision

`M341+M342+M343 CERRADO / OCR_LINE_CLEAN_WITH_LEGACY_DEPRECATION_NOT_REMOVAL`
