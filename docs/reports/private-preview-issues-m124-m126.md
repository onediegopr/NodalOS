# NODAL OS Private Preview Issues M124-M126

## Summary

One non-blocking issue was captured during the first internal local private preview run.

## Issue List

| Issue | Category | Severity | Decision | Blocks Post-Run GO |
| --- | --- | --- | --- | --- |
| `pp-ux-001` | UX | Low | Fixed / VerifiedInSecondRun | No |

## Issue Details

### pp-ux-001

Operator copy can be clearer around target-owned proof versus external general-ready.

Recommended action:

- clarify the operator-facing text so M65 target-owned proof is not confused with external general-ready;
- keep `ReadyWithRestrictions`;
- do not expand external scope.

Status after M127:

- Fixed in operator UX copy.
- Verified in the second internal local run.
- No scope expansion introduced.

## No Critical Findings

No security blocker, release gate mismatch, scope inflation, evidence missing condition, real credentials, real billing/email, public SaaS, public API, sensitive site, or submit/pay/sign/delete path was found.

## Next Handling

The low UX issue is fixed and verified in the second internal local preview run. It no longer blocks stabilization.
