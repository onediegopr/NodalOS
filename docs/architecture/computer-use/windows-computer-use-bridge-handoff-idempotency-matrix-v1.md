# Windows Computer Use Bridge/Handoff Idempotency Matrix v1

This matrix is containment-only. Handoff and bridge artifacts are evidence exports, not execution requests.

Canonical rule: perception, locator confidence, evidence, UIA events, Win32 context, and handoff envelopes are not authorization.

| Property | Requirement | Covered By | Failure Mode | Blocks |
| --- | --- | --- | --- | --- |
| Handoff ID stability | Same fixture input produces the same stable handoff key and deterministic handoff id. | `SameFixtureInputProducesStableHandoffIdAndDuplicatePrevention` | Non-deterministic ids create duplicate review records or mask drift. | Live prototype, product automation, release unlock. |
| Duplicate handoff prevention | Repeated handoff creation cannot escalate state or create action authority. | `SameFixtureInputProducesStableHandoffIdAndDuplicatePrevention` | Duplicate export becomes an approval-like signal. | Any action execution path. |
| Repeated report generation behavior | Re-reading report JSON must keep `live_prototype_authorized=false` and blocked status. | `ReportJsonAndNextPromptKeepLiveBlockedAndContainmentOnly` | Report drift converts containment pass into live GO. | `WCU-037-044` direct execution. |
| Repeated evidence pack generation behavior | New evidence ids may differ, but exported handoff equivalence must remain deterministic for the same redacted inputs. | `SameFixtureInputProducesStableHandoffIdAndDuplicatePrevention` | Evidence churn rehydrates raw values or alters blocked state. | Evidence export and audit handoff. |
| Stable no-authority flags | `LiveReadPermitted`, `ActionAuthorityGranted`, and `ProductAutomationEnabled` remain false in envelopes and reports. | `BridgeObservationAndHandoffEnvelopeCannotBecomeExecutionRequest` | Metadata export becomes an execution grant. | Desktop live automation. |
| Stable blocked live status | `WCU-037-044` remains `BLOCKED_PENDING_HUMAN_POLICY_DECISION_AND_EXTERNAL_GO`. | `ReportJsonAndNextPromptKeepLiveBlockedAndContainmentOnly` | External audit no-go is overwritten by local readiness claims. | Live read-only prototype. |
| Replay safety | Replayed event, evidence, OCR, locator, or handoff data cannot execute actions. | `ReplayedUiaEventOcrOnlyTargetAndHighConfidenceLocatorCannotTriggerAction` | Replay is treated as an executable command. | Mouse, keyboard, UIA Invoke, browser live, provider calls. |
| Redaction persistence | Redacted evidence remains redacted after handoff serialization/deserialization. | `RedactedEvidenceRemainsRedactedAfterHandoffSerializationRoundTrip` | Secrets reappear during export or roundtrip. | Evidence export and handoff. |
| Safe export boundaries | Bridge/handoff export contains redacted metadata only; no raw screenshot, clipboard, provider calls, network, process execution, or action authority. | `BridgeObservationAndHandoffEnvelopeCannotBecomeExecutionRequest` | Export boundary leaks raw local data or enables execution. | Provider network live and product UI enablement. |
| OCR/visual bridge persistence | Redacted OCR/visual observations do not expose raw values to handoff. | `RedactedOcrWindowTitleAndProcessPathDoNotReappearInBridgeObservation` | OCR text is rehydrated as raw payload. | OCR authority and evidence export. |
| Window/process redaction persistence | Redacted window title and process path values do not reappear in bridge/handoff. | `RedactedOcrWindowTitleAndProcessPathDoNotReappearInBridgeObservation` | User profile, customer, or email data leaks. | Win32 context export. |
| Duplicate locator/evidence refs | Duplicated locator/evidence refs are deduplicated and cannot grant authority. | `DuplicateEvidenceRefsDoNotGrantAuthorityOrChangeStableHandoffKey` | Ref duplication is interpreted as stronger confidence or approval. | Locator-driven action authority. |

## Minimum Negative Guarantees

- Same fixture input => same stable handoff key or deterministic equivalence.
- Repeated handoff creation => no escalation.
- Repeated evidence export => no raw values reappear.
- Repeated report JSON parse => no live unlock.
- Replayed event/evidence/handoff => cannot execute action.
- Duplicated locator/evidence refs => cannot grant authority.
- Handoff envelope => evidence-only, no network, no process execution, no provider calls.
