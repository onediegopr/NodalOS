# Risk Register

| Risk | Level | Current Control | Next Safe Response |
| --- | --- | --- | --- |
| Historical docs misread as product readiness | P3 | E4 cross-links, E2 canon authority and E7 packet review | Operator review handoff read-only, then lower-risk cross-link cleanup if needed |
| Manual gates mistaken for CI enforcement | P3 | E2/E5 wording and gate docs state manual/discovery-only | Safety gate stabilization docs/test-only if ambiguity appears |
| Product Ledger local/dev confused with runtime/product enablement | P3 | Canon guard and no-runtime categories | Continue no-runtime/no-authority/no-double-truth filters |
| Old QA JSON preserves stale percentages | P4 | Treat as historical/block-specific evidence | Do not rewrite without separate docs-compaction authorization |
| Audit packet treated as external submission | P4 | README and scope state packet-only/no-submission | E7 review remains read-only |

## Current Decision

Recommended next block:

`NODAL_OS_BLOCK_E8_EXTERNAL_AUDIT_PACKET_OPERATOR_REVIEW_HANDOFF_READ_ONLY`

E7 recommends but does not authorize E8. No runtime/product implementation is selected.
