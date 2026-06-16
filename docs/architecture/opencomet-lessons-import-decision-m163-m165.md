# NODAL OS OpenComet Lessons Import Decision M163-M165

## Decision

NODAL OS will selectively absorb operator UX patterns inspired by OpenComet-style browser agents, but will not fork, vendor, depend on, or re-base on OpenCometAI.

Core remains the authority. Browser Runtime executes. UI, sidepanel and companion render, explain and transport state only.

## Benchmark Summary

OpenComet-style products are useful as UX references for:

- sidepanel as an operator cab
- visible plan before execution
- future anti-loop / stagnation recovery UX
- future DOM + screenshot grounding
- future recipes / skills presentation

They are not suitable as NODAL OS architecture because NODAL OS already has Core policy, evidence ledger, release gates, Chrome/CDP runtime boundaries and local private preview scope locks.

## TAKE / ADAPT / REJECT Matrix

| pattern | decision | NODAL OS handling |
| --- | --- | --- |
| Sidepanel operator cab | TAKE | Use the existing sidepanel as a compact operator cockpit, without replacing timeline/stepper UI. |
| Visible plan before execution | TAKE | Add Core-owned plan preview contract that can render into the existing timeline. |
| Anti-loop / stagnation detector | ADAPT LATER | Future block only; not implemented in M163-M165. |
| DOM + screenshot grounding | ADAPT LATER | Future block only; Chrome/CDP remains the primary runtime. |
| Recipes / skills | ADAPT LATER | Future block only; no marketplace/sharing now. |
| Service worker as brain | REJECT | Service worker must not become policy/decision authority. |
| Full OpenCometAI architecture | REJECT | NODAL OS keeps Core/Runtime/UI separation. |
| Research mode | REJECT NOW | Out of scope for this block. |
| Cost dashboard | REJECT NOW | Out of scope for this block. |
| Ollama/local/multi-provider | REJECT NOW | OpenAI remains the initial primary provider. |
| Wider browser permissions | REJECT BY DEFAULT | Requires explicit justification and evidence. |
| Login/captcha/2FA automation | REJECT | Human approval/intervention remains required. |

## Patterns Absorbed Now

- Sidepanel operator cab as a conceptual map.
- Plan visible before execution, modeled as a Core-owned preview.
- Timeline compatibility so plan preview can feed the existing vertical stepper later.

## Patterns Deferred

- Anti-loop / stagnation detection.
- Recovery UX.
- DOM + screenshot grounding.
- Visual grounding overlay.
- Recipes / skills V1.

## Explicit Rejections

- No OpenCometAI fork.
- No OpenCometAI dependency.
- No service worker as brain.
- No research mode now.
- No cost dashboard now.
- No Ollama/local/multi-provider now.
- No permission expansion without dedicated approval.
- No login/captcha/2FA automation.
- No safety relaxation.

## Timeline Protection Rules

- Do not replace `renderTimeline`.
- Do not create a second parallel timeline.
- Do not delete or duplicate `nodal-timeline` CSS.
- Do not rewrite the sidepanel layout.
- Extend through adapters, props or sections that reuse the existing timeline/stepper.

## Authority and Gates

- Core authority remains mandatory.
- Policy decides.
- Evidence records.
- Human approval can unblock only when policy allows it.
- UI/Admin/Companion remain non-authoritative.
- Chrome/CDP remains the primary runtime.
- External general CDP remains blocked.
- Release gates remain active.

## Scope Lock

This decision does not open production, SaaS public, public API real, billing/email real, real credentials, sensitive sites, submit/pay/sign/delete, productive recorder/replay, new external targets, embedded runtime or Chromium fork.
