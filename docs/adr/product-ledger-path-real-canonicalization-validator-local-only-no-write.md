# Product Ledger Path Real Canonicalization Validator Local-Only No-Write

Date: 2026-07-04

Decision: `GO_WITH_FINDINGS_PRODUCT_LEDGER_PATH_REAL_CANONICALIZATION_VALIDATOR_LOCAL_ONLY_NO_WRITE_READY`

## Scope

This block implements the first bounded real local canonicalization validator for product ledger path candidate readiness.

The validator is local-only and no-write. It canonicalizes an allowed root and candidate path, checks the post-canonicalization boundary, inspects local filesystem attributes for reparse-point risk where possible, and fails closed when evidence is missing or ambiguous.

## Implemented

- `ProductLedgerPathCanonicalizationValidator`.
- `ProductLedgerPathCanonicalizationRequest`.
- `ProductLedgerPathCanonicalizationResult`.
- `ProductLedgerPathCanonicalizationBlocker`.
- Safety tests for fail-closed defaults, null/empty/whitespace, traversal, UNC/network paths, environment-variable attempts, reserved Windows names, ADS/suspicious colon syntax, trailing dot/space risks, Unicode/confusable path segments, displayed-inside/canonical-outside mismatch, missing canonical evidence, unresolved reparse evidence, local-temp product claims, product-ready claims and no-enable source guards.
- Recipes tests for candidate readiness and unsafe corpus rejection.

## Boundary

The component returns candidate readiness only. It does not activate a product ledger path and does not authorize writes.

Explicit non-capabilities:

- no active product ledger path;
- no writer;
- no append-only ledger;
- no product DI/service registration;
- no command handlers;
- no UI product actions;
- no DB/migration/provider/cloud/network;
- no KMS/WORM/external trust;
- no Browser/CDP/WCU/OCR/Recipes live execution;
- no runtime/product enablement;
- no release/commercial readiness.

## Fail-Closed Rules

The validator rejects missing requests, missing local-only/no-write assertions, product-ledger-active claims, product-ready claims, release/commercial claims, external trust claims, WORM/KMS/cloud claims, empty paths, relative paths, traversal, UNC/network paths, environment variable expansion attempts, reserved Windows device names, suspicious colon/ADS syntax, mixed separators, trailing dots/spaces, long path prefixes, Unicode normalization/confusable risks, missing canonicalized paths, canonical paths outside the allowed boundary, displayed-inside/canonical-outside mismatch, missing reparse evidence, unresolved symlink/junction/reparse risk, missing TOCTOU mitigation evidence, local-temp product-ledger claims and unresolved hardlink/mount alias risk.

## Remaining Product Frontier

This is not an active product ledger path, not a ledger writer and not runtime/product enablement. The next product step requiring real write authority, active storage policy, productive registration, command/UI authority, DB/provider/cloud/network, KMS/WORM/external trust or release/commercial readiness still requires explicit manual GO.

## Next Safe Block

`NODAL_OS_PRODUCT_LEDGER_PATH_REAL_CANONICALIZATION_VALIDATOR_EXTERNAL_AUDIT_READ_ONLY`
