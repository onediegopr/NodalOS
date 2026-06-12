# Capability Map Real vs Planned After HITO-129

## Purpose

This map distinguishes:

- what ONE BRAIN already implements
- what is partial or constrained
- what is still planned and not started

The goal is to prevent demo drift, roadmap drift, and false assumptions about execution maturity.

## Capability Status Map

### Product and supervision surface

- Pilot UI: Done
- Approvals / HITL: Done / Partial
  Notes: approval models, UI, audit, and policy exist; execution remains intentionally constrained and fail-closed.
- AI router / config: Done
  Notes: routing and configuration exist, but there are no real provider calls in the current line.
- Recipe editor / variables / linter: Done

### Memory and process modeling

- Process memory: Done / Partial
  Notes: store and retrieval base exists; operational learning depth is still limited.
- Workflow retrieval: Done / Partial
  Notes: deterministic retrieval exists; no semantic retrieval or model-backed ranking.
- App profiles: Done / Partial
  Notes: profile model and policy base exist; coverage and governance still need expansion.

### Observation and identity

- UIA CacheRequest: Done
- RuntimeId / wait events: Done

### Executor and supervised action layer

- Executor harness: Done
- Target resolution hardening: Done
- Safety matrix: Done
- Dry-run / replay / evidence index: Done

### Recorder and fallback perception

- Recorder real: Partial / Needs design
- Fallback Win32 / OCR: Not started / Needs design

### AI and external execution

- IA real: Not started
- BPM / RPA / API / MCP capability publishing: Not started

### Business flows

- First business read-only flow: Not started
- Enterprise E2E: Not started

## Interpretation

ONE BRAIN has strong internal scaffolding in:

- safety
- auditability
- replay/evidence
- Pilot UX
- controlled executor harness work

ONE BRAIN is still not complete in:

- real AI execution
- generalized recorder-to-flow learning
- business-grade read-only app automation
- fallback perception stack
- enterprise-grade end-to-end orchestration

## Planning Implication

Future planning should optimize for the gap between:

- the mature internal control plane already built
- the still-incomplete real-world execution and perception plane

That gap is the basis for the next official execution plan after HITO-129.
