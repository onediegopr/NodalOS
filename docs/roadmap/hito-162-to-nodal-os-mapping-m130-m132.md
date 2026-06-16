# HITO-162 to NODAL OS Mapping Matrix - M130-M132

## Classification Rules

Categories used in this matrix:

- AbsorbedByBrowserRuntime
- AbsorbedByPrivatePreview
- StillValid
- NeedsRewrite
- Deprecated
- Deferred
- UnknownNeedsAudit
- SupersededByNodalOs

If evidence is incomplete, the item stays UnknownNeedsAudit. This matrix does not close HITO-162 or M65 beyond their already documented scope.

## Mapping Matrix

| Legacy item / hito | Original intent | Current status | Absorbed by | Replaced by | Still valid | Requires rewrite | Risk | Current dependency | Suggested new hito | Priority | Category |
| --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- |
| HITO-161 | Approved input binding unification | Last reliable standalone point | Current Core authority and private preview gates | NODAL OS Core-governed action model | Yes | No | Low | Existing M51/M65 evidence discipline | Foundation only | High | StillValid |
| HITO-162 | Identity/Fingerprint v2 | Paused/not forgotten/UnknownNeedsAudit | Partially by Browser Runtime evidence discipline | NODAL OS Identity/Fingerprint v2 sequence | Yes | Yes | Scope inflation if resumed blindly | HITO-161, release gate, local fixtures | M133-M135 | High | NeedsRewrite |
| HITO-163 | UIA truncation detection | No standalone proof found | Not absorbed fully | Robust perception stabilization | Yes | Yes | Misdetecting blocked or truncated surfaces | Identity/Fingerprint v2 | M136-M138 | Medium | UnknownNeedsAudit |
| Robust perception line | Window liveness, overlays, UIA empty/block, semantic fallback, OCR/vision | Still relevant but not closed as one legacy hito | Browser Runtime local/sandbox and evidence gates | NODAL OS robust perception hardening | Yes | Yes | Mixing auxiliary OCR/vision with authority | Core authority and local fixture catalog | M136-M138 | High | NeedsRewrite |
| Safe action expansion | safe.select, safe.download, safe.upload, safe.form.fill, safe.modal.confirm | Deferred | Safety/governance core | Local fixture-first safe action plan | Partially | Yes | accidental submit/pay/sign/delete | robust perception and identity checks | M139-M141 | Medium | Deferred |
| Process memory line | flow ledger, process memory, repeated workflow learning, recipe suggestions | Deferred | Evidence ledger concepts only | future local-only memory sequence | Partially | Yes | leaking workflow or sensitive state | private preview hardening | M142-M144 | Medium | Deferred |
| Product/Admin private preview | operator-facing controlled local preview | Current stable local-only path | Private preview M71-M129 | NODAL OS ReadyWithRestrictions | Yes | No | UI/Admin authority inflation | release gate state-probing | continue internal preview | High | AbsorbedByPrivatePreview |
| Browser Runtime local/sandbox | local controlled browser/runtime execution | 97% local/sandbox | Browser Runtime M51-M65 line | Core-governed runtime execution | Yes | No | external-general overclaim | phase gate | maintain | High | AbsorbedByBrowserRuntime |
| External HTTP proof M51 | external target-owned read-only proof | Closed with HTTP read-only scope | Browser Runtime/external proof line | RealHttpClient + ledger evidence | Yes | No | overclaiming browser/DOM proof | ledger verifier | reference only | High | AbsorbedByBrowserRuntime |
| External CDP proof M65 | target-owned Chrome/CDP/DOM read-only proof | Closed with limited target-owned scope | Browser Runtime/external proof line | RealChromeCdp + ledger evidence | Yes | No | external general-ready inflation | scope lock | reference only | High | AbsorbedByBrowserRuntime |
| Release gate local | controlled private preview decision | ReadyWithRestrictions confirmed | Private preview release gate | state-probed NODAL OS release gate | Yes | No | boolean-only readiness | runtime state probe | maintain | High | AbsorbedByPrivatePreview |
| HITO-162 rewrite future sequence | convert legacy intent into actionable roadmap | Required now | Current roadmap reconciliation | NODAL OS M133+ sequence | Yes | Yes | vague roadmap debt | this mapping | M133-M141 | High | SupersededByNodalOs |
| SaaS/public/API/billing/email track | broad external product operation if implied by legacy work | Blocked | Not absorbed | Future explicit release track only | No for this sequence | Yes | production exposure | separate release gates | not in M133-M141 | Low | Deprecated |
| Embedded runtime/WebView2/CEF | possible future controlled runtime | Future only | M68-M70 architecture decision | M145+ evaluation only if needed | Maybe | Yes | runtime authority confusion | proven limitation | M145+ | Low | Deferred |

## Recommended Next Action

Rewrite HITO-162 as an Identity/Fingerprint v2 and robust perception sequence for NODAL OS, starting with local fixture-first evidence and keeping all public, sensitive, mutating, credential, and production surfaces blocked.

