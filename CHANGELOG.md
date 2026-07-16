# NODAL OS — Changelog

## Unreleased — productization in progress

NODAL OS has no production release, signed desktop installer, public update channel or commercial release at this point.

Current validated foundations include:

- canonical dark-first Mission Control root shell with central timeline, model/fallback context, evidence and collapsed diagnostics;
- explicit real local workspace selection through a loopback-only product surface;
- absolute workspace root protected through Windows current-user DPAPI and represented elsewhere by opaque/redacted references;
- atomic local workspace metadata persistence, revalidation and rehydration after process restart;
- bounded read-only scan and reviewed plan over the selected workspace without modifying it;
- selected workspace identity, evidence and health projected into Mission Control without exposing absolute paths or secret-like content;
- persisted real mission goal bound to the selected workspace identity, fingerprint, path-jail reference and evidence;
- one reviewed reversible candidate over `NODAL_HANDOFF.md`, expressed as create-only or exact-hash update according to the current target state;
- candidate preconditions, risk, approval scope, proposed hash, rollback plan and expected evidence visible before execution;
- stale-precondition detection and mission/candidate rehydration after process restart;
- one-shot mission-scope approval bound to mission, workspace fingerprint, action, `filesystem.write.safe`, target and reviewed hashes;
- approval contracts that remain non-authoritative by themselves and do not grant general runtime or product authority;
- controlled create-only execution with atomic move, exact byte verification and SHA-256 evidence;
- controlled exact-hash update with verified app-local snapshot, atomic replace, post-write verification and guarded restore plan;
- rollback restricted by operation identity, workspace fingerprint, allowlisted target and exact current result hash;
- automatic rollback refusal when the verified result changed externally after execution;
- persisted execution, verification, evidence and rollback state rehydrated after process restart;
- `/mission/execution`, `/api/mission/execution` and `/mission/rollback` as loopback-only, same-origin, one-time-token product surfaces;
- real mission goal, reviewed plan, approval, execution, verification and rollback projected into the canonical Mission Control timeline without creating a second source of truth;
- process-level CI smoke covering workspace selection, mission creation, scope approval, execution, SHA-256 verification, restart, rehydration, rollback and restoration of the original workspace state;
- `/models/config`, `/api/models/config`, `/models/test` and `/models/clear` as the canonical loopback-only BYOK connection surface;
- primary and optional fallback OpenAI-compatible routes with explicit local/cloud type, endpoint, model, privacy permission, timeout and cost limits;
- default product credentials persisted only as opaque references backed by Windows current-user DPAPI;
- local endpoints restricted to loopback and cloud endpoints restricted to HTTPS with explicit cloud authorization;
- one real bounded provider connection test through the existing `PolicyAwareModelRouter`, with automatic fallback only across preconfigured privacy/capability/budget-compatible routes;
- provider response content and raw prompts excluded from persistence while SHA-256, route, attempts, token usage, estimated cost, evidence and canonical timeline remain auditable;
- operator cancellation stopping the model fallback chain without recording a successful connection;
- verified BYOK provider/model/fallback state projected into canonical Mission Control without creating another router, policy engine, timeline or ledger;
- restart rehydration and guarded clearing of BYOK metadata and stored credential references;
- process-level CI smoke covering secure configuration, DPAPI plaintext checks, primary HTTP 503, authorized fallback HTTP 200, response-hash verification, Mission Control projection, restart, rehydration and credential cleanup;
- one-time request tokens, strict same-origin POST, no-store and closed CSP on workspace, mission, execution and model surfaces;
- former ONE BRAIN Pilot demo home preserved under an explicit legacy lab route instead of defining the primary product experience;
- environment-only AI configuration preserved explicitly under `/pilot/legacy/ai/config` instead of competing with the canonical BYOK product surface;
- local/dev Runtime Inspector, bounded workspace understanding and verified handoff export;
- test-owned create and exact-hash update operations retained as regression fixtures with atomic verification, snapshot/rollback and cleanup;
- mission-level approval, evidence and semantic verification foundations;
- policy-aware model routing and fallback foundations;
- Expert Advisor as a deterministic non-executor;
- CognitiveSnapshotV2, Trusted Control Flow and verified skill memory;
- Teach NODAL fixture compilation, bounded capture sessions and application-scoped Windows UIA observation;
- ChromeLab security validation as `LAB_LEGACY_TRANSITION`;
- CloakBrowser as the canonical browser target, with live CDP validation still blocked when the pinned external binary is unavailable.

The selected workspace can now be mutated only through the exact approved and revalidated `NODAL_HANDOFF.md` candidate, with deterministic verification and guarded rollback. A real BYOK route can now be configured, tested, rehydrated and cleared without exposing its credential or provider response. Broader user-workspace mutation, arbitrary patching, shell execution, using the verified BYOK route for unrestricted mission inference, customer-data workflows, desktop packaging, public deployment, billing, licensing, auto-update and release claims remain closed until separately implemented and validated.

## Historical experimental milestone — 2026-06-25

An earlier changelog labeled an experimental stealth/browser line as `v1.0.0-production`. That label did not correspond to a published NODAL OS production release and is superseded by the current canonical status above.

Historical work in that line explored browser stealth, CAPTCHA detection, proxy management, remote handoff, metrics and deployment concepts. Those experiments do not grant current product authority, do not make ChromeLab the product runtime and do not establish release readiness.
