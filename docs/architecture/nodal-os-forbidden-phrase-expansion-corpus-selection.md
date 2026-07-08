# NODAL OS Forbidden Phrase Expansion Corpus Selection

Date: 2026-07-08

Mode: read-only / docs-only / audit-only / selection-only.

Block: `AUTHORIZE_NODAL_OS_FORBIDDEN_PHRASE_EXPANSION_CORPUS_SELECTION_AUDIT_ONLY`.

Baseline HEAD: `d4c80ad01cc7537aa685c5bdde283413a89077ea`.

Decision: `GO_WITH_FINDINGS_FORBIDDEN_PHRASE_EXPANSION_CORPUS_SELECTED_READY`.

Resulting state: `FORBIDDEN_PHRASE_EXPANSION_CORPUS_SELECTED_NO_IMPLEMENTATION`.

Stop condition: `STOP_FOR_OPERATOR_DECISION_ON_FORBIDDEN_PHRASE_EXPANSION_IMPLEMENTATION_SCOPE`.

## Executive Verdict

The next safe Static Guard increment is a narrow, test-only forbidden phrase expansion, but only after this corpus selection. The future guard must scan a small current corpus, must honor negative/no-go wording and must not become a broad docs gate or CI enforcement.

This block selects only. It does not add forbidden fragments, edit tests, touch source, enable CI or authorize runtime/product, public/product, Production route, latest pointer, read precedence, product authority, Product Ledger/model consolidation, broad common-contract implementation, DB/cloud/network/KMS/WORM or release/commercial work.

## Current Source Of Truth

- Static Guard coverage map: `docs/architecture/nodal-os-static-guard-catalog-coverage-map.md`.
- Metadata consistency check: `docs/architecture/nodal-os-static-guard-catalog-metadata-consistency-check.md`.
- Next increment selector: `docs/architecture/nodal-os-static-guard-next-increment-after-metadata-consistency-selection.md`.
- Catalog source for future implementation reference only: `tests/OneBrain.Safety.Tests/NodalOsStaticGuardCatalog.cs`.
- Catalog tests for future implementation reference only: `tests/OneBrain.Safety.Tests/NodalOsStaticGuardCatalogTests.cs`.

Current posture:

- Static Guard Catalog readiness: `94%`.
- Forbidden phrase expansion readiness: `67%`.
- Tier 1 label coverage: `70%`.
- Metadata consistency confidence: `82%`.
- Runtime/product enablement: `0%`.
- CI enforcement: `0%`.
- Release/commercial: `0% / NO-GO`.

## Selected Initial Corpus

First implementation corpus:

- `docs/architecture/nodal-os-global-roadmap-current-index.md`
- `docs/architecture/nodal-os-static-guard-catalog-coverage-map.md`
- `docs/architecture/nodal-os-static-guard-catalog-metadata-consistency-check.md`
- `docs/architecture/nodal-os-simplification-backlog.md`
- `docs/decision-log.md`

Why this corpus:

- It is current or current-indexed.
- It contains the main runtime/product, CI and release/commercial anti-capability posture.
- It contains enough historical negative wording to prove the allowlist rules matter.
- It is small enough to avoid turning all historical docs into a noisy gate.

Deferred corpus:

- Product Ledger local/dev docs: defer for the first implementation because they contain many historical no-authority and manual-gate statements; include later after the narrow guard proves low noise.
- Source-refactor closeout docs: defer because source-refactor docs include repeated runtime/product no-go wording and old recommendations.
- Runner guidance docs: defer because runner wording includes command/process terminology that could create false positives around shell/subprocess guards.
- Entire `docs/` tree: explicitly rejected for the first implementation because broad scanning is high-noise and could freeze legitimate historical/no-go records.

## Phrase Families

| Family | Include first implementation | Positive examples for future tests | Required negative allowlist examples | Likely false positives | Strategy |
| --- | --- | --- | --- | --- | --- |
| Runtime/product authority claims | Yes | `runtime enabled`, `product enabled`, `runtime/product ready`, `product authority granted` | `no runtime/product enabled`, `does not authorize runtime/product`, `runtime/product remains 0%`, `NOT_AUTHORIZED_NOW` | Current docs frequently say runtime/product remains blocked | Match positive claim only when not preceded by a local negation/no-go marker |
| Public/product claims | Yes | `public product enabled`, `public route live`, `product surface live` | `no public/product`, `public/product remains blocked`, `public product not authorized` | Product Ledger docs often describe blocked public/product exposure | Require clear positive/live verbs and ignore explicit blocked/no/no-go lines |
| Production route claims | Yes | `production route enabled`, `production route active`, `production route ready` | `No Production route`, `Production route remains blocked`, `Production route coverage remains 404` | Coverage and decision docs intentionally repeat no Production route claims | Start with exact positive fragments only |
| Latest pointer / read precedence claims | Yes | `latest pointer promoted`, `read precedence changed`, `authoritative read path changed` | `No latest pointer`, `No active read precedence`, `latest pointer remains blocked`, `read precedence remains blocked` | Latest-state evidence docs distinguish candidate evidence from authority | Require positive authority wording, not neutral design terms |
| CI enforcement claims | Yes | `CI enforced`, `CI gate active`, `CI blocks release` | `no CI enforcement`, `CI enforcement remains 0%`, `CI changed: none` | Almost every closeout says CI remains 0% | Include only after negative allowlist is proven in the same future test |
| Release/commercial claims | Yes | `release approved`, `commercial ready`, `launch ready`, `production-ready` | `No release/commercial`, `release/commercial NO-GO`, `release/commercial remains 0%` | NO-GO docs often contain the words release/commercial | Keep high priority, but exact positive phrases only |
| External audit approval claims | Defer from first implementation | `external audit passed`, `external reviewer approved`, `external approval granted` | `no external audit pass`, `no external response recorded`, `operator submission only` | Product Ledger E-series has external-review packet and no-response history | Defer until external-review docs get their own corpus/allowlist |
| DB/cloud/network/KMS/WORM capability claims | Defer from first implementation | `DB enabled`, `cloud backed`, `provider enabled`, `KMS/WORM guaranteed` | `No DB/migration`, `No provider/cloud/network`, `No KMS/WORM`, `KMS/WORM remains unimplemented` | Existing static guard has source-like tokens such as `HttpClient` and `DbContext`; docs use many no-go lists | Defer until implementation distinguishes capability claims from anti-capability lists |

## Negative Allowlist Rules

The future guard must allow a line or local sentence when the forbidden phrase appears inside a clear negative, historical or future-not-authorized context.

Allowed negative markers:

- `no`
- `not`
- `does not authorize`
- `does not enable`
- `remains blocked`
- `remains 0%`
- `NO-GO`
- `NOT_AUTHORIZED_NOW`
- `blocked`
- `denied`
- `not claimed`
- `historical`
- `superseded`
- `future`
- `requires explicit operator authorization`
- `CI changed: none`
- `Runtime/product changed: none`
- `Release/commercial: 0% / NO-GO`

Required policy:

- A positive fragment must fail only when it is not clearly negated in its own line or immediately local sentence.
- Negative/no-go wording must not suppress a separate positive claim on another line.
- Historical and superseded recommendations must not be interpreted as current authorization.
- The allowlist is mandatory before any phrase family is expanded.

## Future Test Contract

Exact next block:

`NODAL_OS_FORBIDDEN_PHRASE_EXPANSION_NARROW_GUARD_TEST_ONLY`

Allowed scope:

- Add or update focused Safety tests only.
- Add narrow forbidden phrase fragments for the selected first-implementation families.
- Add mandatory negative/no-go allowlist behavior.
- Use the selected initial corpus only.
- Keep docs/source scan entrypoints explicit.
- Keep all changes test-only plus docs continuity.

Blocked scope:

- no `src/`;
- no broad `docs/` scan as a gate;
- no CI/workflows or CI enforcement;
- no runtime/product;
- no public/product;
- no Production route;
- no latest pointer/read precedence/product authority;
- no Product Ledger/model consolidation;
- no broad common-contract implementation;
- no DB/cloud/network/provider;
- no KMS/WORM;
- no release/commercial.

Required future tests:

- Positive samples fail for runtime/product, public/product, Production route, latest pointer/read precedence, CI enforcement and release/commercial claims.
- Negative/no-go samples pass for each included family.
- A nearby negative line does not suppress a separate positive line.
- The selected corpus either produces no forbidden matches or expected, documented review-only findings.
- Product Ledger local/dev docs remain deferred unless explicitly added by a later block.

NO-GO conditions for the next block:

- Negative/no-go wording cannot be distinguished from positive claims.
- The selected corpus produces noisy or ambiguous failures.
- Implementing the guard requires source, CI, runtime/product or broad docs scanning.
- External audit approval or DB/cloud/KMS/WORM families cannot be safely distinguished from historical packet wording.
- P0/P1/P2 or TRUE_RISK appears.

## Findings

P0: 0.

P1: 0.

P2: 0.

P3:

- Forbidden phrase expansion is worthwhile but only for a narrow current corpus.
- Release/commercial, CI and runtime/product wording is intentionally repeated in negative form across closeout records.
- Product Ledger local/dev and external-review docs should remain out of the first implementation corpus to avoid false positives.

P4:

- DB/cloud/network/KMS/WORM and external audit approval families are valid future candidates, but they should not be included in the first narrow implementation.

## Final Boundary

This block selects corpus, phrase families, allowlist rules and the next test-only contract. It does not implement forbidden phrase expansion, edit tests, touch source, change CI, broaden static scans, authorize runtime/product, authorize release/commercial or claim product authority.
