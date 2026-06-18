# Axiom Browser Automation Benchmark Decision AX-001

## Role Of Axiom For NODAL OS

Axiom is a benchmark for productive browser automation patterns, not an architectural dependency.

Relevant benchmark areas:

- run reports
- automation JSON or bot definitions
- step library
- scheduling
- orchestration API
- troubleshooting
- run recording and evidence
- desktop and cloud runtime references

## TAKE

- Run Report V1
- Automation JSON / Recipe Manifest V1
- Failure taxonomy
- troubleshooting recommendation mapper
- future Step Library
- future Run Orchestration API
- future scheduled read-only runs
- run recording and evidence

## ADAPT

- recipe manifest governed by NODAL OS Core
- run report connected to Mission, Task, Evidence, Policy and Approval
- failure taxonomy fail-closed
- scheduling only as future read-only capability
- internal API first, not public API first
- step library governed by policy and approval metadata

## REJECT

- bot bypassing
- captcha solving
- 2Captcha
- aggressive scraping
- sensitive cloud runtime without hard boundaries
- no-code freedom that bypasses policy
- sensitive submit without approval
- automatic login
- automatic 2FA
- sensitive actions without human approval

## NODAL OS Rules

- no sensitive submit without approval
- no login, captcha or 2FA automation
- CDP remains the primary runtime
- sidepanel is not the brain
- recipe manifest cannot bypass policy
- global policy dominates recipe-local policy
- evidence and run report remain mandatory
- failures are typed, not ad-hoc strings

## Relation To Next Milestones

- AX-002 Run Report V1
- AX-003 Recipe Manifest / Automation JSON V1
- AX-006 Failure Taxonomy + Troubleshooting
- AX-004 Step Library future
- AX-005 Run Orchestration API future
- AX-007 Scheduled Read-Only Runs future
