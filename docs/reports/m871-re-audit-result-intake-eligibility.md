# M871 - Re-Audit Result Intake + Eligibility Matrix

Project: NODAL OS.

The re-audit intake contract records:

- `reAuditResultId`
- `auditorType`
- `auditorModel`
- `auditedCommit`
- `decision`
- `f1Status`
- `f2Status`
- `remainingFindings`
- `blockingFindings`
- `nonBlockingFindings`
- `freezeLockEligible`
- `nextAction`

Eligibility rules:

- `REAUDIT_GO_F1_F2_REMEDIATED` -> freeze lock eligible.
- `REAUDIT_CONDITIONAL_GO_MINOR_QUALITY_NOTES` -> freeze lock eligible with caveat.
- `REAUDIT_NO_GO_F1_STILL_TAUTOLOGICAL` -> freeze lock blocked.
- `REAUDIT_NO_GO_F2_STILL_THEATER` -> freeze lock blocked.
- `REAUDIT_NO_GO_SCOPE_OR_SAFETY_DRIFT` -> freeze lock blocked.
- `REAUDIT_NO_GO_TEST_QUALITY_INSUFFICIENT` -> freeze lock blocked.
- Missing re-audit result -> freeze lock blocked.

Focused re-audit result recorded for this block:

- Auditor type: alternate LLM focused re-audit.
- Auditor model: GPT-5.5 Thinking XHigh.
- Decision: `REAUDIT_GO_F1_F2_REMEDIATED`.
- F1: remediated measured sink proof.
- F2: remediated adversarial redaction proof.
- Freeze lock eligible: true.
- Freeze lock activated: false.

This block decides eligibility only. Freeze lock activation is deferred to M873-M884.
