# ADR: Post-Claude Private Preview Hardening M121-M123

## Status

Accepted for NODAL OS internal local private preview.

## Claude Review Summary

Claude reviewed the NODAL OS private preview local evidence pack after M118-M120 and returned GO for internal local private preview with confidence 8/10.

The review was not a production approval. It recommended non-blocking hardening before any scope expansion:

- Release gate decisions must derive from probeable runtime state, not raw optimistic booleans.
- M65 must remain anchored to a real persisted ledger event, not only an `IsRealChromeCdpSession` flag.
- Readiness and freeze summaries must verify `LedgerRef` and `LedgerHash` against the audit ledger.
- Skipped tests audit must validate categories, not just total count.

## M121 Release Gate State-Probing

`NodalOsLocalPrivatePreviewReleaseGate` now accepts `INodalOsRuntimeStateProbe` and `NodalOsReleaseGateStateSnapshot`.

The snapshot includes:

- external general readiness
- public SaaS state
- public API state
- real billing/email state
- real credential state
- sensitive site state
- submit/pay/sign/delete state
- productive recorder/replay state
- canonical worktree state
- evidence availability
- Product/Admin readiness

The former safe fixture is test-only. Product readiness must not be sourced from a generic `SafeInput` factory.

## M122 Ledger Live Verification

NODAL OS now has a ledger verifier for M51/M65 evidence references.

The verifier checks:

- `LedgerRef` exists in `BrowserPersistentAuditLedger`
- `LedgerHash` matches the persisted event hash
- persistence status is `PersistedRedactedLedger`
- probe kind and scope match the expected evidence
- ledger content is redacted
- body/DOM/cookies/tokens/secrets are not persisted

M65 formal closure requires `LedgerVerified=true`. A fake Chrome/CDP flag or a ledger string literal is not enough.

## M123 Skipped Audit Category Guard

Skipped tests audit now validates:

- total skipped count
- expected category set
- unexpected category drift
- local/private preview skips
- missing opt-in environment variables

The current allowed categories are live/opt-in, external target blocked, sandbox/auth, sensitive simulation, recorder/replay opt-in, document workflow opt-in, and safe download/upload opt-in.

## Scope After Hardening

`ReadyWithRestrictions` remains valid only for internal local private preview.

No scope expansion is allowed by this ADR.

Still blocked:

- public SaaS
- public API
- real billing/email
- real credentials
- sensitive sites
- submit/pay/sign/delete
- productive recorder/replay
- external CDP general-ready
- new external targets without dedicated evidence
- embedded runtime
- Chromium fork
- HITO-162 rewrite

## Next Required Actions Before Scope Expansion

- Run private preview locally with evidence capture.
- Keep Claude/auditor findings tracked.
- Re-run ledger verification on any new M51/M65 evidence.
- Reconcile skipped audit categories whenever skipped count changes.
- Do not use M51/M65 as production or general external CDP approval.
