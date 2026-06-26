# Windows Computer Use Capability Matrix v1

Date: 2026-06-26

Project: NODAL OS

| Capability | Current Status | Type | Evidence | Decision |
| --- | --- | --- | --- | --- |
| Snapshot contracts | Implemented | fixture-safe | `ComputerUseSnapshot` | Closed foundation |
| Window context model | Implemented | fixture-safe | `WindowContext` | Closed foundation |
| UI element identity | Implemented | fixture-safe | `UiElementIdentity` | Closed foundation |
| Fixture snapshot builder | Implemented | fixture-safe | fixture builder methods | Closed foundation |
| Capability classifier | Implemented | fixture-safe | classifier tests | Closed foundation |
| Blockage detector | Implemented | fixture-safe | blockage tests | Closed foundation |
| Sensitive surface detector | Implemented | fixture-safe | sensitive tests | Closed foundation |
| Locator scoring | Implemented | fixture-safe | locator tests | Candidate-only |
| Safe action planner | Implemented | dry-run only | planner tests | No execution |
| Evidence pack/redaction | Implemented | fixture-safe | evidence tests | Metadata-only |
| UIA live read-only adapter | Not implemented | future design | none | NO-GO now |
| Real mouse/keyboard | Not implemented | forbidden | no code | NO-GO |
| Live UIA actions | Not implemented | forbidden | no code | NO-GO |
| Clipboard capture | Not implemented | forbidden | no code | NO-GO |
| Productive Windows automation | Not implemented | disabled | no code | NO-GO |

## Percentages

- WCU fixture-safe foundation: 100%
- WCU observability design readiness: 40%
- WCU UIA live read-only readiness: 5%
- WCU controlled action readiness: 0%
- WCU product automation readiness: 0%
