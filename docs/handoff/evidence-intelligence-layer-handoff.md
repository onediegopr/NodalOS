# Evidence Intelligence Layer Handoff

## Summary

The Evidence Intelligence Layer adds local typed evidence reasoning to NODAL OS.

It lets NODAL OS ingest fixture/local evidence, search it lexically, verify claims, scan proposed actions, detect contradictions, build typed evidence graph edges, generate readiness matrices and emit deterministic JSON/Markdown reports.

## What Exists

- `OneBrain.Core.Evidence.EvidenceItem`
- `IEvidenceIntelligenceIndex`
- `InMemoryEvidenceIntelligenceIndex`
- `EvidenceIntelligenceRetrievalRouter`
- `ClaimEvidenceScanner`
- `ActionEvidenceScanner`
- `EvidenceGraphBuilder`
- `EvidenceReadinessMatrixBuilder`
- `EvidenceReportFormatter`

## Safety Boundary

Runtime is not enabled.

EIL does not start browsers, connect CDP, call OCR live, capture screenshots, capture desktop state, run recorders, launch sandboxes, call providers, call network, invoke shell/process runners or mutate productive systems.

Existing protected browser/OCR/runtime scopes remain present in the repo and untouched.

## Semantic Search Status

Semantic search is intentionally disabled. EIL performs deterministic lexical search only. Any future semantic backend must be real, local or explicitly approved, and must not claim success unless implemented.

## Operator Notes

Use EIL reports as evidence reasoning inputs for:

- read-only Recipe Lab panels;
- audit reviews;
- policy gates;
- fixture eval reviews;
- operator handoff packs.

Do not treat EIL as proof that a live action can run. It only supports local evidence reasoning and fail-closed decision reports.

## Next Safe Work

The next safe line is read-only integration of EIL results into product surfaces or policy/eval fixtures. Runtime remains blocked unless a separate external audit explicitly opens a protected runtime gate.
