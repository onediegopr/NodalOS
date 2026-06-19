# NODAL OS / NODRIX - Recipe DSL Decision Record

## Context

NODRIX Automation Layer will eventually need a human-readable recipe representation. TagUI-style text workflows are useful as inspiration because they are readable and editable, but NODRIX must not become a classic RPA runtime or copy external RPA products.

## Problem

A textual DSL can be confused with an executable script. Direct import or execution would bypass Mission Control, policy, approval, evidence, timeline, redaction, and risk classification.

## Decision

NODRIX Recipe DSL is representation, not runtime. It is future-only and cannot execute directly.

## Non-goals

- No parser in this milestone.
- No DSL executor.
- No recorder or replay.
- No browser automation.
- No scheduler, queue, trigger, API, UI, or workflow designer.
- No TagUI dependency.
- No direct import execution.

## TagUI Inspiration Boundary

TagUI is considered only as conceptual inspiration for readable commands and examples. No TagUI code, runtime, syntax compatibility promise, package, or dependency is introduced.

## DSL Is Representation, Not Runtime

The DSL is a human-editable representation. It cannot grant runtime permission, cannot dispatch actions, and cannot bypass risk classification.

## JSON Canonical Model

Future DSL input must normalize into a JSON canonical recipe model before any validation or future execution gate. The JSON canonical model is the governance surface, not the raw text.

## Parser Deferred

The parser is deferred. M455-M457 creates only decision records, risk contracts, validators, serializer, fixtures, tests, docs, and artifact.

## Runtime Deferred

Runtime remains deferred. No recipe, skill, step, browser, desktop, worker, scheduler, queue, or API execution is added.

## Import Validation Required

Future import must validate:

- syntax;
- canonical JSON conversion;
- recipe risk profile;
- selector safety;
- evidence requirements;
- redaction;
- approval requirements;
- human handoff requirements.

## Direct Execution Forbidden

Recipe import never means direct execution. Direct execution from DSL text is forbidden.

## Relationship With Recipe Manifest

Recipe Manifest remains the governance contract for recipe metadata and policy. DSL can only become a future input to the manifest/canonical model pipeline after validation.

## Relationship With Step Library

Step Library remains the descriptive step catalog. DSL commands cannot execute step definitions directly.

## Relationship With Recipe Risk Classifier

Every future DSL-derived recipe must pass Recipe Risk Classifier before any runtime decision is considered.

## Relationship With Approval Center

Sensitive or mutable categories require explicit approval. Submit, publish/send, payment, destructive, credential, file mutation, and high-risk operations cannot proceed by DSL alone.

## Relationship With Evidence / Timeline

Future DSL import, validation, risk classification, and handoff events must produce redacted evidence and timeline entries. Raw secrets must never persist.

## Relationship With Expert Advisor

Expert Advisor may audit or explain recipe risk, but it remains an observer and cannot execute DSL.

## Future Phases

1. Decision record and risk contracts.
2. JSON canonical model definition.
3. DSL grammar decision.
4. Parser prototype with no runtime.
5. Import validation pipeline.
6. Dry-run preview only.
7. Runtime only after dedicated security audit.

## Acceptance Criteria

- DSL is representation, not runtime.
- Parser is deferred.
- Runtime is deferred.
- Direct execution is forbidden.
- Import requires validation.
- JSON canonical model is required.
- No TagUI dependency is introduced.
- No classic RPA identity is introduced.
