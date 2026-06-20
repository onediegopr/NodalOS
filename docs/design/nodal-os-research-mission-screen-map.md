# NODAL OS Research Mission Screen Map

## Navigation

Sidebar:

- Mission Control.
- Timeline.
- Missions.
- Workspace.
- Consent.
- Evidence.
- Decisions.
- Advisor.
- Models.
- Agents.
- Runtime.
- Settings.

## Mission Control

Purpose: replace classic dashboard with a mission-centered operating surface.

Textual wireframe:

```text
Mission Control

Current Mission
Build Local AI Workspace
72% Complete

Current Phase
Consent Governance

Secondary blocks:
Pending Decisions: 3
Advisor Warnings: 2
Evidence Artifacts: 12

Next Safe Action
Review storage audit readiness before implementation.
```

Rules:

- One dominant mission hero.
- Maximum three secondary blocks.
- No widget wall.
- No generic KPI grid as the main pattern.

## Timeline

Purpose: mission journal, technical roadmap, and evidence line.

Each phase shows:

- Status.
- Percentage.
- Decision associated.
- Evidence generated.
- Blocker if present.
- Readiness gate.

## Mission Detail

Shows mission title, objective, current phase, percent complete, linked decisions, evidence stack, blocked capabilities, and advisor notes.

## Consent / Permissions

Purpose: Governance Console.

Textual wireframe:

```text
Capability: Content Access
Status: Blocked / Draft / Ready / Approved
Scope: Workspace-only
Evidence: ADR + Test Matrix
Risk: High
User Action: Approval Required
```

Must clarify:

- No productive consent accidental.
- No capability enablement without approval.
- No content access, content fingerprinting, or folder enumeration without gates.
- No cloud or provider activity without explicit policy.

## Evidence

Purpose: research archive and case-file system.

Textual wireframe:

```text
Artifact: Consent Governance Closeout
Type: ADR / Matrix / Report / Test Result
Status: Approved
Linked Mission: M576-M578
Commit: 613351b...
Validation: 14 passed
```

Evidence should feel like research papers, case files, compliance records, and architecture decision records.

## Decisions

Purpose: executive decision room.

Textual wireframe:

```text
Decision: Enable productive consent storage?
Recommendation: Do not enable yet.
Risk: High
Evidence: ADR + Test Matrix + Closeout
Options: Approve / Defer / Reject
```

## Advisor

Purpose: strategic architecture advisor.

Textual wireframe:

```text
Advisor Note
Concern: Path jail is still design-only.
Potential Impact: Users may assume content access is already safe.
Recommendation: Keep capability disabled until containment and enforcement are implemented.
Critical Question: What evidence proves the gate fails closed?
```

Do not use chatbot bubbles as the primary Advisor pattern.

## Models

Purpose: model policy and readiness.

Textual wireframe:

```text
Primary Model: OpenAI GPT-5.5
Fallback: GPT-5.5 Mini / cheaper model
Status: Configured / Not configured
Policy: No cloud activity without explicit approval
```

## Agents

Purpose: supervised local agents.

Show role, authority boundary, assignment, allowed actions, blocked actions, evidence produced, and current review status.

## Runtime

Purpose: local-first safety surface.

Textual wireframe:

```text
Runtime Status: Local-only
Network: Disabled
Content Access: Blocked
Indexing: Disabled
Representation Build: Disabled
OCR: Synthetic-only / Not productive
Provider Activity: Disabled
```

## Settings

Purpose: policy and environment configuration. Group settings by local environment, model policy, evidence, accessibility, and visual preferences.

## Activity Feed

Purpose: mission activity feed instead of raw logs.

Examples:

```text
14:32 - System - Full suite passed - 4265 passed, 37 skipped
14:35 - Advisor - Warning generated - Productive consent still blocked
14:38 - User - Decision approved - Closeout ready
14:41 - Runtime - Capability blocked - Content access remains disabled
```

## Readiness Gates

Shows gate, readiness, missing requirement, evidence needed, owner/action, and whether it blocks productive use.

## Error / Blocked State

Must explain:

- Why this is blocked.
- What is missing.
- What evidence is required.
- What user action is needed.
- What remains intentionally disabled.

## Empty States

Empty states should guide the next safe step, not fill space. Example: "No evidence yet. Start by creating a mission or importing an ADR reference."
