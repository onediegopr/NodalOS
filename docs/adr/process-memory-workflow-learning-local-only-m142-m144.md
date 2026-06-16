# ADR - Process Memory / Workflow Learning Local-Only - M142-M144

## Status

Accepted.

## Context

HITO-162 was rewritten as a NODAL OS local-first sequence. Identity/Fingerprint v2, robust perception, and safe action expansion now provide redacted local fixture signals. The next recovered intent is process memory and workflow learning.

## Decision

NODAL OS adds process memory and workflow learning as local-only, fixture-first, redacted metadata.

Allowed memory can store:

- redacted action category
- redacted surface identity
- identity/fingerprint reference
- perception summary reference
- safe action decision reference
- operator decision reference
- issue/reference id
- timestamp
- confidence
- evidence refs

## Blocked Memory

Memory must reject:

- credentials
- cookies
- tokens
- payment info
- personal/customer data
- raw DOM/body
- raw sensitive UIA trees
- unredacted logs
- submit payloads
- screenshots with secrets
- production scope
- external general scope
- productive recorder/replay

## Scope

Process memory is a private preview signal only. It does not authorize actions, replace Core approval, train on real data, or enable production recorder/replay.

## Relation to Previous Hitos

- Identity/Fingerprint v2 supplies redacted identity refs.
- Robust perception supplies redacted perception refs.
- Safe action expansion supplies action decision refs.
- Core remains authoritative.

## Next Steps

After M144, future work can prepare broader internal local preview iteration and later product hardening. Production, SaaS public, credentials, sensitive sites, submit/pay/sign/delete, external general CDP, embedded runtime, and Chromium fork remain blocked.

