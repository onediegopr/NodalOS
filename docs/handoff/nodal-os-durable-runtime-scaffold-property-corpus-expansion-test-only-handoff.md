# Durable Runtime Scaffold Property Corpus Expansion Test-Only Handoff

Decision: `GO_WITH_FINDINGS_DURABLE_RUNTIME_SCAFFOLD_PROPERTY_CORPUS_EXPANSION_TEST_ONLY_READY`

Date: 2026-07-04

## Summary

The Durable runtime safety scaffold was hardened with a test-only property/corpus expansion. New blockers cover path traversal, env-var paths, reserved Windows device names, mixed separators, symlink/junction/reparse-point unresolved risk, canonical realpath evidence gaps, malformed/duplicate/stale/inconsistent evidence refs and product-authority overclaims.

## Still Blocked

- Real product runtime enablement.
- Active product ledger path.
- Real symlink/junction/reparse protection.
- Real human authorization/auth/approval service.
- Product authority.
- Release/commercial readiness.

## Next Safe Option

`NODAL_OS_DURABLE_RUNTIME_SCAFFOLD_READ_MODEL_AND_EVIDENCE_PACK_TEST_ONLY`

Do not proceed to product/runtime enablement without explicit manual GO.
