# ADR M49: Final Pre-Production Checkpoint

## Status

Accepted for M49.

## Context

After M17-M48, NEXA has strong browser runtime governance, product/admin foundations, local shell models, mock billing/onboarding, diagnostics, dry-run deployment, and API boundary design. Production-sensitive capabilities still require explicit decisions and audits.

## Decision

M49 introduces a final pre-production checkpoint report with:

- capability matrix;
- risk register;
- blockers;
- decision matrix;
- roadmap recommendation.

The checkpoint is redacted and does not enable production behavior.

## Current Blocking Conditions

The checkpoint records that M25B external low-risk target setup remains blocked unless a test-owned target exists. It also blocks M28 external workflow, sensitive real pilot, public SaaS, real billing, real email, auto-update real, productive recorder/replay, raw personal profile, and real client credentials.

## Decision Matrix

The next paths evaluated are:

- external test-owned target setup / M25B real;
- product/admin private preview;
- productive vault hardening;
- WebView2/CEF architecture;
- public API implementation private/local;
- billing real design;
- sensitive real pilot preparation;
- external audit with Claude.

## Recommendation

The recommended path is Product/Admin private preview plus external audit checkpoint. Sensitive real pilots remain blocked until external audit, compliance/legal/operational approval, and explicit roadmap decision.

## Out of Scope

M49 does not enable public SaaS, real billing, real email, deploy/update execution, sensitive real sites, real client credentials, profile raw, productive replay/recorder, or irreversible browser actions.
