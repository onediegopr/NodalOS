# Windows Computer Use Report JSON Claim Consistency Matrix v1

This matrix locks WCU containment claims across reports, JSON, handoffs, prompts, matrices, and passive code constants. It is not a live-readiness claim.

Canonical rule: containment PASS is not live GO. Evidence, handoff, replay, locator confidence, OCR, Win32 context, and UIA events are not authorization.

| Canonical claim | Expected value | Source of truth | Artifacts that must match | Forbidden wording | Regression test | Failure behavior |
| --- | --- | --- | --- | --- | --- | --- |
| `contained_artifact` | `true` | containment reports | latest report JSON, report MD, handoff, cross-artifact consistency report | `contained_artifact: false` in current containment report | `WindowsComputerUseClaimConsistencyDrift` | Fail closed and keep live blocked. |
| `live_prototype_authorized` | `false` | `WCU-037A` external no-go reconciliation | latest report JSON, handoff, next prompt, external audit report | `live_prototype_authorized: true`; `live prototype authorized: YES`; `containment PASS = live GO` | `WindowsComputerUseClaimConsistencyDrift` | Critical drift; block live advance. |
| `live_remains_blocked` | `true` | `ComputerUseExternalAuditReconciliation` | latest reports, handoffs, prompts | `live remains blocked: NO`; `live block removed` | `WindowsComputerUseClaimConsistencyDrift` | Critical drift; block live advance. |
| `current_code_defect_found` | `false` unless a new real defect is found | block QA report | latest report JSON and MD | hiding a real defect; treating audit no-go as code defect without evidence | `WindowsComputerUseClaimConsistencyDrift` | Stop and record real defect explicitly. |
| `wcu_037_044_status` | `BLOCKED_PENDING_HUMAN_POLICY_DECISION_AND_EXTERNAL_GO` | `ComputerUseExternalAuditReconciliation.BlockedLivePrototypeStatus` | latest report JSON, handoff, prompt, external audit report | direct `WCU-037-044` execution; unblocked live prototype wording | `WindowsComputerUseClaimConsistencyDrift` | Critical drift; block live advance. |
| `live_read_permitted` | `false` | `ComputerUseReadOnlyLiveGateCatalog` | report JSON authority, gate docs, handoff | `LiveReadPermitted=true`; `safe to start live implementation` | `WindowsComputerUseClaimConsistencyDrift` | Critical drift; block live read. |
| `action_authority_granted` | `false` | locator/evidence/bridge/handoff contracts | report JSON authority, evidence docs, handoff | `ActionAuthorityGranted=true`; `high confidence = authorization`; `evidence = authorization` | `WindowsComputerUseClaimConsistencyDrift` | Critical drift; block action path. |
| `product_automation_enabled` | `false` | read-only live design gate | report JSON authority, prompts, handoffs | `ProductAutomationEnabled=true`; `product automation ready`; `desktop automation enabled` | `WindowsComputerUseClaimConsistencyDrift` | Critical drift; block product UI. |
| `browser_live_cdp_enabled` | `false` | no-live containment policy | report JSON, prompts, handoffs | `browser live enabled`; `CDP live enabled` | `WindowsComputerUseClaimConsistencyDrift` | Critical drift; block browser live/CDP. |
| `safe_injection_live_enabled` | `false` | no-live containment policy | report JSON, prompts, handoffs | `Safe Injection live enabled` | `WindowsComputerUseClaimConsistencyDrift` | Critical drift; block Safe Injection live. |
| `public_release_unlock` | `false` | containment property catalog | report JSON, handoff, containment matrix | `public release unlocked` | `WindowsComputerUseClaimConsistencyDrift` | Critical drift; block release claims. |
| `paid_beta_unlock` | `false` | containment property catalog | report JSON, handoff, containment matrix | `paid beta unlocked` | `WindowsComputerUseClaimConsistencyDrift` | Critical drift; block beta claims. |

## Historical Mentions

Historical prompts or reports may mention `WCU-037-044 — READ-ONLY LIVE PROTOTYPE GATED` only when the same artifact states it is blocked, no-go, or pending human policy decision and external GO.

## Drift Handling

- Current containment reports with contradictory JSON values fail tests.
- Handoff or prompt wording that recommends live implementation as the next executable block fails tests.
- Historical-only artifacts are recorded as historical, not rewritten.
- Any drift keeps `LiveReadPermitted=false`, `ActionAuthorityGranted=false`, and `ProductAutomationEnabled=false`.
