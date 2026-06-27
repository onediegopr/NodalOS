# Evidence Intelligence Layer QA Report

Decision ID: `GO_NODAL_OS_EVIDENCE_INTELLIGENCE_LAYER_IMPLEMENT_NOW`

## Decision

Implementation validation complete with one unrelated existing Safety Evidence hash-gate failure documented below.

## Scope

Implemented a local, fixture-safe Evidence Intelligence Layer in `OneBrain.Core.Evidence`.

The implementation includes:

- typed evidence contracts;
- in-memory local evidence index;
- stable text normalization and hashing;
- lexical retrieval with source/confidence/staleness weighting;
- explicit disabled semantic backend status;
- claim evidence scanner;
- action evidence scanner;
- typed evidence graph;
- action readiness matrix;
- stable JSON and Markdown reports;
- focused tests under `TestCategory=EvidenceIntelligence`.

## No-Runtime Boundary

EIL did not add or enable runtime.

Not added:

- executable adapter;
- browser launch;
- CDP connection;
- Cloak mutation;
- Playwright/Selenium/Puppeteer;
- desktop/UIA/Win32 live path;
- OCR live activation;
- screenshot capture;
- recorder runtime;
- sandbox/VM/container runtime;
- provider/LLM call;
- cloud/network/shell/process runner.

Existing protected runtime/OCR/browser scopes remain present in the repo and untouched by EIL.

## Evidence Behavior

EIL stores local evidence records with type, source reference, workspace/session, normalized text, stable hash, confidence, sensitivity, redaction, staleness, policy scope and metadata.

The default behavior is fail-closed:

- missing evidence blocks action readiness;
- stale evidence blocks sensitive/non-read actions;
- policy blocks override confidence;
- contradiction beats support;
- unsafe live action is blocked;
- fixture-safe approval does not enable runtime.

## Retrieval Behavior

Lexical retrieval is real and deterministic.

Semantic/vector retrieval is not implemented. The backend status is explicitly `Disabled`; no fake semantic success is exposed.

## Validation Commands

- `dotnet restore .\OneBrain.slnx`: PASS.
- `dotnet build .\OneBrain.slnx --no-restore`: PASS. Full first build reported the known preexisting 32 Safety/OCR warnings; subsequent incremental build reported 0 warnings.
- `dotnet test .\tests\OneBrain.Recipes.Tests\OneBrain.Recipes.Tests.csproj --no-build --filter TestCategory=EvidenceIntelligence`: PASS, 15/15.
- `dotnet test .\tests\OneBrain.Recipes.Tests\OneBrain.Recipes.Tests.csproj --no-build`: PASS, 1300/1300.
- `dotnet test .\tests\OneBrain.Safety.Tests\OneBrain.Safety.Tests.csproj --no-build --filter FullyQualifiedName~Evidence`: FAIL, 33 existing sidepanel/bridge hash-gate tests fail because expected hashes do not match current sidepanel/bridge files. EIL did not modify those files.
- `dotnet test .\tests\OneBrain.Safety.Tests\OneBrain.Safety.Tests.csproj --no-build --filter FullyQualifiedName~Recipe`: PASS, 155 passed / 1 skipped.
- `git diff --check`: PASS.
- `git diff --cached --check`: PASS.
- Protected scope scan: PASS for EIL changed files; existing protected runtime scopes remain present and untouched.
- OCR protected scan: PASS for EIL changed files; OCR internals untouched.
- Browser/runtime no-live scan: PASS for production EIL source; docs mention forbidden capabilities only as blocked/not-added scope.
- Recorder no-live scan: PASS.
- Sandbox no-runtime scan: PASS.
- Secret scan: PASS.
- Dependency scan: PASS; no project/package/dependency files changed.
- Fake semantic search scan: PASS; semantic backend is explicitly disabled.
- Fake success scan: PASS; no success without evidence claim added.

## Known Limits

- Storage is in-memory only.
- No semantic/vector search is implemented.
- No UI route or runtime adapter is mounted.
- No live evidence capture exists in this line.
- Reports are local deterministic objects, not external persistence.

## Audit Position

EIL is suitable for local, typed, auditable evidence reasoning in fixture/read-only contexts. It is not runtime-ready and does not make NODAL OS automation-ready.
