# NODAL OS - Redaction Before Persistence Service Design-Only Handoff

Decision: `GO_WITH_FINDINGS_REDACTION_BEFORE_PERSISTENCE_SERVICE_DESIGN_ONLY_READY`

Date: 2026-07-03

## What Changed

Docs-only artifacts were added for the future redaction-before-persistence service design:

- ADR: `docs/adr/redaction-before-persistence-service-design-only.md`
- QA report: `docs/qa/nodal-os-redaction-before-persistence-service-design-only/report.md`
- QA JSON: `docs/qa/nodal-os-redaction-before-persistence-service-design-only/report.json`
- Handoff: this file
- Decision log entry: `docs/decision-log.md`

No source, tests, runtime, service registration, command handlers, UI actions, product ledger paths, DB/migration, provider/cloud/network, Browser/CDP/WCU/OCR/Recipes live paths or stash state were changed.

## Current State

The service is design-only. Current Stage 2 remains test-only/local-temp and relies on caller-attested redaction proof plus deterministic sensitive-data rejection before persistence. Product redaction service implementation remains `0% / NO-GO`.

Runtime/live product enablement, release/commercial readiness and product ledger paths remain prohibited.

## Design Summary

The future conceptual `NodalOsRedactionBeforePersistenceService` must:

- classify every candidate field before persistence;
- reject by default when policy is absent, unknown, ambiguous or unsupported;
- redact or omit only with explicit deterministic policy;
- produce non-leaking evidence;
- block append when evidence is missing, stale, failed or mismatched;
- never persist or log raw rejected values;
- keep replay/read-model/checkpoint consumers on redacted envelopes only.

## Findings

| Severity | Count |
| --- | ---: |
| P0 | 0 |
| P1 | 0 |
| P2 | 0 |
| P3 | 3 |
| P4 | 1 |

P3 items are future implementation risks: service still design-only, classifier/redactor corpus expansion required, and ordering/evidence/error leakage must be audited before implementation.

## Required Next Step

Recommended next safe block:

`NODAL_OS_REDACTION_BEFORE_PERSISTENCE_SERVICE_DESIGN_EXTERNAL_AUDIT_READ_ONLY`

Implementation, runtime/product enablement, service registration, product ledger path, command handlers, UI actions, DB/cloud/network, Browser/CDP/WCU/OCR/Recipes live execution and release/commercial readiness still require a separate manual GO.
