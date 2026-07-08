# Risk Register

| Risk | Level | Current Control | Next Safe Response |
| --- | --- | --- | --- |
| Historical docs misread as product readiness | P3 | E4 cross-links, E2 canon authority and E7 packet review | Operator review handoff read-only, then lower-risk cross-link cleanup if needed |
| Manual gates mistaken for CI enforcement | P3 | E2/E5 wording and gate docs state manual/discovery-only | Safety gate stabilization docs/test-only if ambiguity appears |
| Product Ledger local/dev confused with runtime/product enablement | P3 | Canon guard and no-runtime categories | Continue no-runtime/no-authority/no-double-truth filters |
| External review approval misread as runtime/product authorization | P3 | E9 approval record states review-prep-only and no external submission by Codex | Require explicit future authorization before feedback intake or any runtime/product step |
| Operator submission packet misread as external review completed | P3 | E10 packet states submission is operator-run and no external review result exists yet | Next block after reviewer response should record feedback only |
| Response intake scaffold misread as actual reviewer response | P3 | E11 scaffold status is `PENDING_OPERATOR_SUBMISSION_OR_RESPONSE` and records no findings | Require Diego-provided reviewer response before intake |
| Old QA JSON preserves stale percentages | P4 | Treat as historical/block-specific evidence | Do not rewrite without separate docs-compaction authorization |
| Audit packet treated as external submission | P4 | README, E8 operator handoff and E9 external handoff state no Codex submission | Operator may submit manually outside Codex after E9 |

## Current Decision

Recommended next block:

`STOP_FOR_OPERATOR_TO_SUBMIT_EXTERNAL_REVIEW_AND_RETURN_RESPONSE`

E11 creates a response intake scaffold only. E11 does not submit anything externally, does not record an external response, does not complete external review and does not authorize runtime/product implementation.
