# NODAL OS Legacy HITO Absorption Matrix

Categories:

- `Absorbed`
- `StillValid`
- `NeedsRewrite`
- `Deprecated`
- `Deferred`
- `Superseded`
- `UnknownNeedsAudit`

| Legacy item / hito | Original intent | Current status | Absorbed by | Replaced by | Still valid | Recommended action | Current blocker | Evidence / commit / doc | Priority |
|---|---|---|---|---|---|---|---|---|---|
| HITO-162 / post-HITO-161 line | Continue after Approved-Input Binding Unification, likely into perception robustness or safe action expansion | `UnknownNeedsAudit`; paused/not forgotten | Partially by Browser Runtime safety/evidence gates | Not replaced wholesale | Unknown | Audit legacy intent, then rewrite as NODAL OS hito block | No standalone HITO-162 doc found in canonical worktree | `docs/hitos/hito-161-approved-input-binding-unification.md`; `docs/architecture/one-brain-engine-master.md` | High |
| HITO-161 | Manifest-bound approved input authority for `safe.type` | Implemented legacy foundation | Safety/governance core | Not replaced | Yes | Preserve as foundation | None known | `docs/hitos/hito-161-approved-input-binding-unification.md` | Medium |
| Chrome / extension / Browser Runtime transition | Move authority away from extension and into Core-governed runtime | Absorbed | M1-M18, M50, M68-M70, M87-M93 | Core-governed Browser Runtime line | Yes | Continue under runtime gates | External Chrome/CDP/DOM proof pending | ADRs M1-M18, M50, M68-M70, M91-M93 | High |
| M51 | Prove external read-only target | Closed with strict HTTP-only scope | M77-M93 | External HTTP read-only proof with ledger | Yes, limited | Treat as closed only for HTTP read-only proof | Does not prove Chrome/CDP/DOM | `2d822ed`, `artifacts/m91-m93-live-proof-ledger`, ADR M91-M93 | High |
| M65 | External low-risk auth/live target setup | Deferred | Not absorbed by M51 | Needs dedicated evidence | Yes | Build dedicated M65 evidence plan | HTTP read-only proof is insufficient | ADR M65; ADR M91-M93 | High |
| Browser Runtime local/sandbox | Govern local CDP/runtime safely | Absorbed and active | M1-M50, M68-M70 | Core-governed runtime gates | Yes | Preserve, do not weaken | External CDP/DOM proof pending | Browser runtime ADRs; phase gate tests | High |
| External HTTP proof | Prove read-only HTTP target ownership and metadata safety | Closed evidence path | M77-M93 | `RealHttpClient` proof + HMAC ledger | Yes, limited | Keep as M51 evidence, not CDP evidence | Browser proof still pending | `artifacts/m91-m93-live-proof-ledger` | High |
| External Chrome/CDP/DOM proof | Prove browser runtime against external target | Pending | Not absorbed | Future M103-M105 candidate | Yes | Implement only after dedicated gate and opt-in | No external CDP/DOM run yet | M91-M93 explicitly says not proven | High |
| Private preview local | Operate local/private product/admin preview | Absorbed and active | M52-M76 | NODAL OS local private preview line | Yes | Continue hardening locally | No SaaS/public users | ADRs M52-M76 | Medium |
| Product/Admin readiness | Local admin shell, readiness dashboard, operations | Absorbed and active | M48-M76 | Product/Admin private preview local | Yes | Continue local-only hardening | Public SaaS blocked | ADRs M48-M76 | Medium |
| Safety/governance core | Core authority, audit, redaction, vault, gates | Absorbed and strengthened | M17-M50, M52-M93 | Core-governed NODAL OS safety spine | Yes | Preserve as non-negotiable foundation | Real credentials/sensitive sites blocked | M50, M67, M90, M91-M93 | High |
| Rename NEXA to NODAL OS | Align technical names with official product name | Deferred | Not absorbed | Future rename block M97-M99 | Yes | Execute separately after roadmap lock | Do not mix with security/proof work | Current code still uses historical NEXA names | Medium |
| SaaS/API public readiness | Public product/API exposure | Deferred | Not absorbed | Future dedicated SaaS/API phase | No for current phase | Keep blocked | Public exposure prohibited | Private Local API M59-M63 only | Low |
| Billing/email real readiness | Real money and email delivery | Deferred | Sandbox/design only | Future dedicated compliance phase | No for current phase | Keep blocked | No real billing/email allowed | M62-M64 sandbox/design | Low |

## Matrix Decision

The legacy line remains valuable, but HITO-162 must be rewritten or explicitly mapped before implementation.

The next roadmap must not infer that external browser automation is ready from the M51 HTTP-only proof.

