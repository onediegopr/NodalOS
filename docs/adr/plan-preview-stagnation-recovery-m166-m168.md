# ADR: Plan Preview, Stagnation Detector and Recovery UX M166-M168

## Decision

NODAL OS binds Core-owned plan previews and runtime recovery states into the existing sidepanel vertical timeline / stepper. It also introduces a runtime stagnation detector V1 and recovery UX contracts.

This does not create a second timeline, does not replace the stable sidepanel layout and does not grant UI authority.

## Relation to OpenComet Lessons

The absorbed pattern is limited to:

- visible plan before execution
- operator-facing recovery explanation
- loop/stagnation awareness

The rejected parts remain rejected:

- no OpenComet dependency or fork
- no service worker as brain
- no research mode
- no cost dashboard
- no Ollama/local/multi-provider
- no broader browser permissions

## Plan Preview Before Execution

`NodalOsExecutionPlanPreview` is Core-owned. The sidepanel only renders it. The preview includes goal, status, domains, risks, approvals, evidence requirements, sensitive actions and policy summary.

Plan preview never executes automatically. Rejected and policy-blocked plans do not execute.

## Runtime Stagnation V1

The detector identifies:

- repeated URL
- repeated DOM hash
- repeated screenshot hash
- repeated action
- selector repeated failure
- scroll/no progress class signals
- click with no visual change
- input already applied class signals
- repeated runtime error
- unexpected modal class signals
- page not loaded
- captcha/login/2FA
- same target repeated action

When threshold is exceeded, NODAL OS must stop repeating blindly and show warning/block/recovery.

## Recovery UX

Recovery is visible as a timeline step/card with cause, recommendation, safe options, blocked options and redacted evidence refs.

Safe visible options:

- Reintentar
- Replanificar
- Pedir ayuda humana
- Continuar con evidencia parcial
- Finalizar
- Copiar LOG
- Ver evidencia
- Reportar issue

Recovery does not execute sensitive workarounds and does not bypass login, captcha or 2FA.

## Core Authority

- Core decides.
- UI/Admin/Companion do not authorize actions.
- Recovery UX does not grant authority.
- Human intervention can provide input, but policy still controls execution.

## What Is Not Implemented

- No production/SaaS/public API enablement.
- No real billing/email/credentials.
- No sensitive sites.
- No submit/pay/sign/delete.
- No productive recorder/replay.
- No external CDP general-ready.
- No embedded runtime.
- No Chromium fork.
- No live proof.

## Timeline Protection

The implementation reuses `renderTimeline`, `renderTimelineStep`, `operatorTimeline` and existing `.nodal-timeline` CSS. No second timeline is created.
