# ADR - Robust Perception Stabilization - M136-M138

## Status

Accepted.

## Context

M130-M132 rewrote legacy HITO-162 into a NODAL OS sequence. M133-M135 implemented Identity/Fingerprint v2 as fixture-first evidence and explicitly kept identity as a signal, not action authority.

The next recovered legacy intent is robust perception: window liveness, overlay detection, UIA empty/block detection, semantic access fallback, and future OCR/vision as auxiliary evidence only.

## Decision

NODAL OS adds robust perception stabilization as a local fixture-first model.

It covers:

- WindowLivenessMonitor states
- surface stability states
- overlay and blocked-surface detection
- empty UIA tree detection
- empty DOM metadata detection
- truncated and ambiguous surface detection
- semantic access fallback from redacted local fixture descriptors

## Non-Scope

Robust perception does not authorize actions.

It does not open:

- SaaS public
- public API real
- billing/email real
- real credentials
- sensitive sites
- submit/pay/sign/delete
- productive recorder/replay
- external CDP general-ready
- new external targets
- embedded runtime
- Chromium fork

OCR/vision remains future auxiliary work and is not productively enabled in this hito.

## Core Authority

Perception, identity, semantic fallback, UI/Admin, and Companion can only provide signals. Core remains the only decision authority. Unknown, frozen, crashed, stale, blocked, ambiguous, sensitive, or unsafe perception must block or require Core/human review.

## Human Review

Human intervention is required when:

- a permission/system overlay blocks perception
- UIA/DOM evidence is empty or truncated
- the surface is ambiguous
- semantic evidence is insufficient
- future OCR/vision would be required

## Next Steps

M139-M141 may design safe action expansion against local fixtures, but it must preserve no submit/pay/sign/delete and must not treat perception confidence as action authority.

