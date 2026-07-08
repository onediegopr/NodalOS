# NODAL OS Forbidden Phrase Deferred Families Corpus Selection

Date: 2026-07-08

Mode: test-only / docs-minimal / narrow-guard-only.

Block: `AUTHORIZE_NODAL_OS_FORBIDDEN_PHRASE_DEFERRED_FAMILIES_NARROW_GUARD_TEST_ONLY`.

Baseline HEAD: `dcb2a3b0570290119f7a634bcd94356049dc79c2`.

Decision: `GO_WITH_FINDINGS_DEFERRED_FORBIDDEN_PHRASE_FAMILIES_NARROW_GUARD_READY`.

Resulting state: `FORBIDDEN_PHRASE_DEFERRED_FAMILIES_NARROW_GUARD_READY`.

Stop condition: `STOP_FOR_OPERATOR_DECISION_ON_STATIC_GUARD_NEXT_INCREMENT_AFTER_DEFERRED_FAMILIES`.

## Executive Verdict

The deferred forbidden phrase families now have a focal test-only guard against a small high-signal corpus. The implementation does not scan all docs and does not convert external-review packets, no-authority lists or DB/cloud/KMS/WORM no-go wording into positive claims.

This block adds one Safety test guard and updates docs minimally. It does not touch source, enable CI, authorize runtime/product, claim external audit approval, enable DB/cloud/network/provider/KMS/WORM or change release/commercial posture.

## Current Source Of Truth

- Narrow guard selection and current state: `docs/architecture/nodal-os-forbidden-phrase-expansion-corpus-selection.md`.
- Static Guard coverage map: `docs/architecture/nodal-os-static-guard-catalog-coverage-map.md`.
- Global current index: `docs/architecture/nodal-os-global-roadmap-current-index.md`.
- Current backlog: `docs/architecture/nodal-os-simplification-backlog.md`.
- Decision log: `docs/decision-log.md`.
- Current narrow guard: `StaticGuardCatalog_ForbiddenPhraseExpansionNarrowCorpusRespectsNegativeAllowlist`.
- Current deferred-family guard: `StaticGuardCatalog_DeferredForbiddenPhraseFamiliesNarrowCorpusRespectsNegativeAllowlist`.

Confirmed current coverage:

- Runtime/product authority claims: guarded in the narrow corpus.
- Public/product claims: guarded in the narrow corpus.
- Production route claims: guarded in the narrow corpus.
- Latest pointer/read precedence claims: guarded in the narrow corpus.
- CI enforcement claims: guarded in the narrow corpus.
- Release/commercial claims: guarded in the narrow corpus.

Confirmed deferred families:

- External audit approval claims.
- DB/cloud/network/provider/KMS/WORM capability claims.

Current posture after implementation:

- Static Guard Catalog readiness: `96%`.
- Forbidden phrase expansion readiness: `86%`.
- Deferred families readiness: `78%`.
- Runtime/product enablement: `0%`.
- CI enforcement: `0%`.
- Release/commercial: `0% / NO-GO`.

## Selected Deferred-Families Corpus

First deferred-family implementation corpus:

- `docs/architecture/nodal-os-global-roadmap-current-index.md`
- `docs/architecture/nodal-os-forbidden-phrase-expansion-corpus-selection.md`
- `docs/architecture/nodal-os-static-guard-catalog-coverage-map.md`
- `docs/architecture/nodal-os-simplification-backlog.md`
- `docs/decision-log.md`
- `docs/audit/product-ledger-local-dev/current-authority-map.md`
- `docs/audit/product-ledger-local-dev/external-review-response-intake.md`
- `docs/audit/product-ledger-local-dev/internal-packet-closeout-e2-e15.md`
- `docs/audit/product-ledger-local-dev/no-authority-static-scan-contract.md`

Why this corpus:

- It contains the current Static Guard and roadmap canon.
- It includes the highest-signal Product Ledger local/dev external-review/no-authority records.
- It includes explicit no-response/no-approval and no-DB/cloud/KMS/WORM wording.
- It avoids the whole Product Ledger local/dev packet, which contains many review prompts and checklists that would be noisy as a first gate.

Deferred corpus:

- `docs/audit/product-ledger-local-dev/operator-submission-packet.md`: defer because it contains manual reviewer instructions and KMS/WORM question wording.
- `docs/audit/product-ledger-local-dev/external-review-handoff.md`: defer because it is a reviewer handoff packet and has many review prompts.
- `docs/audit/product-ledger-local-dev/operator-review-handoff.md`: defer because checklist wording is high-noise.
- Durable checkpoint/trust design docs: defer because many entries are historical or design-only and should get a separate trust-boundary corpus.
- Runner docs: reject for this family because runner/process vocabulary is unrelated to external audit approval and DB/cloud/KMS/WORM capability claims.
- Entire `docs/` tree: explicitly rejected for the first deferred-family implementation.

## Deferred Phrase Families

| Family | Include first implementation | Positive examples for future tests | Required negative allowlist examples | Likely false positives | Strategy |
| --- | --- | --- | --- | --- | --- |
| External audit approval claims | Yes | `external audit passed`, `external reviewer approved`, `external approval granted`, `reviewer approval recorded`, `external audit completed as approval`, `external review approved product readiness` | `no external audit pass`, `no external response recorded`, `no external approval claimed`, `external review wait closed without response`, `internal/operator-attested only`, `external audit approval not claimed`, `future external review requires explicit response content` | Product Ledger local/dev packet contains external review handoff and response-intake wording without an approval result | Require explicit positive approval/pass verbs without local negative/no-response markers |
| DB/cloud/network/provider/KMS/WORM capability claims | Yes | `DB enabled`, `database migration active`, `cloud backed`, `provider enabled`, `network enabled`, `KMS guaranteed`, `WORM guaranteed`, `durable WORM active`, `external trust guaranteed` | `no DB/cloud/network/provider`, `DB/cloud/network/provider remains blocked`, `KMS/WORM not claimed`, `no KMS/WORM guarantee`, `design-only`, `future gate requires explicit authorization`, `NOT_AUTHORIZED_NOW` | Current docs repeatedly list these terms as blocked capabilities and no-go boundaries | Start with exact phrase detection plus mandatory negative allowlist; do not include source-token terms such as `HttpClient` or `DbContext` in docs guard |

## Negative Allowlist Rules

Allowed local negative markers:

- `no`
- `not`
- `not claimed`
- `no external response recorded`
- `no external approval claimed`
- `no external audit pass`
- `wait closed without response`
- `without response`
- `internal/operator-attested only`
- `operator-run`
- `operator submission`
- `not submitted`
- `blocked`
- `remains blocked`
- `NO-GO`
- `NOT_AUTHORIZED_NOW`
- `design-only`
- `future`
- `requires explicit authorization`
- `requires explicit response content`
- `does not authorize`
- `does not grant`

Required policy:

- Negative wording can allow only the line or local sentence containing the negative marker.
- A negative line cannot suppress a separate positive approval or capability claim on another line.
- Review packet existence is not external audit approval.
- Operator submission packet existence is not external review completion.
- DB/cloud/KMS/WORM design vocabulary is not capability enablement unless a positive enablement/guarantee phrase appears without a negative marker.

## Implemented Test Contract

Implemented block:

`NODAL_OS_FORBIDDEN_PHRASE_DEFERRED_FAMILIES_NARROW_GUARD_TEST_ONLY`

Allowed scope:

- Add or update focused Safety tests only.
- Implement the two deferred families against the selected deferred-family corpus.
- Add mandatory negative/no-response/no-authority allowlist behavior.
- Keep docs/source scan entrypoints explicit.
- Keep all implementation test-only plus docs continuity.

Blocked scope:

- no `src/`;
- no broad `docs/` scan as a gate;
- no test-infra fix;
- no CI/workflows or CI enforcement;
- no runtime/product;
- no external audit approval claim;
- no DB/cloud/network/provider enablement;
- no KMS/WORM guarantee or external trust claim;
- no public/product, Production route, latest pointer, read precedence or product authority;
- no Product Ledger/model consolidation;
- no broad common-contract implementation;
- no release/commercial.

Implemented focal tests:

- Positive samples fail for external audit approval claims.
- Positive samples fail for DB/cloud/network/provider/KMS/WORM capability claims.
- Negative/no-response/no-authority samples pass for both families.
- A negative line does not suppress a separate positive line.
- The selected deferred-family corpus produces zero active approval/capability matches.
- Operator packet and response-intake wording must remain non-authority unless explicit approval/pass content is recorded.

Guard added:

`StaticGuardCatalog_DeferredForbiddenPhraseFamiliesNarrowCorpusRespectsNegativeAllowlist`

Residual NO-GO conditions:

- External review packet wording cannot be separated from actual approval/pass claims.
- DB/cloud/KMS/WORM design/no-go wording cannot be separated from enablement/guarantee claims.
- The selected corpus is noisy enough to require broad allowlists.
- Implementation requires source, CI, runtime/product, broad docs scan or Product Ledger model consolidation.
- P0/P1/P2 or TRUE_RISK appears.

## Findings

P0: 0.

P1: 0.

P2: 0.

P3:

- Product Ledger local/dev external-review docs contain many review/packet terms that can be misread as external approval unless response/pass claims are kept separate.
- DB/cloud/network/provider/KMS/WORM wording appears frequently as blocked/no-go text, so the first implementation must be exact-phrase and line-local.
- Durable checkpoint/trust docs are related but too noisy for the first deferred-family corpus.

P4:

- The deferred families are covered by one narrow test-only guard, but remain unsuitable for broad docs scanning or CI enforcement.

## Final Boundary

This block implements a deferred-family guard only in Safety tests. It does not touch source, enable CI, authorize runtime/product, claim external audit approval, enable DB/cloud/network/provider/KMS/WORM or claim release/commercial readiness.
