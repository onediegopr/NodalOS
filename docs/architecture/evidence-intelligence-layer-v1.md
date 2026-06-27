# Evidence Intelligence Layer v1

Decision ID: `GO_NODAL_OS_EVIDENCE_INTELLIGENCE_LAYER_IMPLEMENT_NOW`

## Purpose

The Evidence Intelligence Layer (EIL) gives NODAL OS a local, typed and auditable way to reason about what was observed before trusting a claim or proposed action.

EIL does not rely on an agent saying that it saw something. It stores fixture/local evidence items, indexes them, searches them lexically, compares supporting and contradicting signals, builds typed graph edges and emits deterministic claim/action readiness reports.

## Boundary

EIL is local, no-runtime and fail-closed.

It does not add or enable:

- browser runtime, CDP, Cloak, Playwright, Selenium or Puppeteer;
- desktop live control, UIA/Win32 live capture or screen capture;
- OCR live activation or screenshot capture;
- recorder runtime, mouse/keyboard/clipboard/window capture;
- sandbox, VM, Docker or container runtime;
- provider, LLM, cloud, network, shell or process runner;
- productive filesystem behavior.

Existing protected runtime/OCR/browser scopes remain present in the repo and untouched by this line. EIL only introduces `OneBrain.Core.Evidence` contracts/evaluators and read-only tests/docs.

## Core Model

`EvidenceItem` represents a local evidence record with:

- source type (`Ocr`, `Uia`, `Win32`, `Cdp`, `Dom`, `ScreenshotFixture`, `Timeline`, `Policy`, `Approval`, `AgentObservation`, `ExecutionProposal`, `ValidationReport`, `PreviousEvidenceRecord`);
- source reference;
- workspace/session;
- text and normalized text;
- stable hash;
- confidence;
- sensitivity and redaction status;
- staleness;
- policy scope;
- deterministic metadata.

`EvidenceEdge` links evidence to claims/actions through typed relations such as `supports_claim`, `contradicts_claim`, `supports_action`, `contradicts_action`, `policy_blocks`, `requires_approval`, `stale_source`, `missing_source` and `redacted_source`.

`ActionReadinessMatrix` presents the final decision as rows by signal, with source, status, confidence, risk, decision, evidence id and rationale.

## Local Index

`IEvidenceIntelligenceIndex` defines fixture-safe ingestion and query. `InMemoryEvidenceIntelligenceIndex` is the implemented local store.

It supports:

- ingest;
- get by id;
- stable hash deduplication;
- source filters;
- workspace/session filters;
- stale filtering;
- redaction-preserving storage;
- metadata retention.

This is not fake persistence. It is explicitly an in-memory local index.

## Retrieval

`EvidenceIntelligenceRetrievalRouter` implements real lexical retrieval:

- token normalization;
- lexical overlap scoring;
- source weighting;
- confidence weighting;
- staleness penalty/exclusion;
- deduplication via index hash;
- contradiction-aware ordering.

Semantic/vector search is not implemented. `SemanticBackendStatus` is `Disabled`, and reports say the semantic backend is disabled. There is no fake semantic success.

## Claim Scan

`ClaimEvidenceScanner` verifies a claim against indexed evidence and returns:

- `SUPPORTED`;
- `CONTRADICTED`;
- `INSUFFICIENT_EVIDENCE`;
- `STALE_EVIDENCE`;
- `LOW_CONFIDENCE`;
- `REQUIRES_HUMAN_REVIEW`.

Contradiction takes precedence over support. If OCR supports a claim but UIA contradicts it, the result is not `SUPPORTED`.

## Action Scan

`ActionEvidenceScanner` evaluates proposed actions:

- `ALLOW_READ_ONLY`;
- `ALLOW_FIXTURE_SAFE`;
- `REQUIRES_APPROVAL`;
- `BLOCKED_BY_POLICY`;
- `BLOCKED_BY_CONTRADICTION`;
- `BLOCKED_BY_MISSING_EVIDENCE`;
- `BLOCKED_BY_STALE_EVIDENCE`;
- `BLOCKED_UNSAFE_LIVE_ACTION`.

Fail-closed rules:

- missing critical evidence blocks;
- contradictions block;
- policy blocks;
- stale evidence blocks sensitive or non-read action;
- unsafe live action blocks;
- fixture-safe is allowed only when explicitly marked fixture-safe and evidence is sufficient;
- approval is required for sensitive or irreversible actions.

Example: OCR says `Pagar` while UIA says `Eliminar cuenta`. EIL blocks by contradiction and shows both signals in the readiness matrix.

## Integration Points

EIL can consume and emit local report objects that align with existing NODAL OS evidence/timeline/policy concepts. This line intentionally does not wire EIL into any live runtime or protected adapter path.

Safe future integration points:

- Recipe Lab read-only panels;
- Reliable Recipe evidence/preflight reports;
- policy gates;
- audit handoff packs;
- fixture-only eval reports.

Unsafe integration points remain blocked:

- live browser/CDP/desktop execution;
- live OCR/screenshot capture;
- recorder runtime;
- sandbox runtime;
- provider/LLM augmentation.

## Reports

Claim and action scan results emit stable JSON and Markdown reports. Reports include:

- verdict;
- confidence;
- rationale;
- evidence ids;
- graph/matrix rows;
- blocking reasons;
- `runtime: not_enabled`.

## Future Backlog

Before any runtime-adjacent work, EIL should receive a separate protected-scope audit for:

- persistence strategy, if needed;
- explicit policy profile integration;
- richer contradiction taxonomy;
- optional real semantic backend, only if implemented honestly and locally;
- Recipe Lab read-only UI rendering.

Runtime remains blocked by default.
