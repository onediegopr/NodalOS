# NODAL OS Private Preview Issues M124-M126

## Summary

One non-blocking issue was captured during the first internal local private preview run.

## Issue List

| Issue | Category | Severity | Decision | Blocks Post-Run GO |
| --- | --- | --- | --- | --- |
| `pp-ux-001` | UX | Low | AcceptForInternalOnly | No |

## Issue Details

### pp-ux-001

Operator copy can be clearer around target-owned proof versus external general-ready.

Recommended action:

- clarify the operator-facing text in a later UX hardening block;
- keep `ReadyWithRestrictions`;
- do not expand external scope.

## No Critical Findings

No security blocker, release gate mismatch, scope inflation, evidence missing condition, real credentials, real billing/email, public SaaS, public API, sensitive site, or submit/pay/sign/delete path was found.

## Next Handling

The low UX issue is accepted for internal-only preview and should be fixed soon, but it does not block continuation.
