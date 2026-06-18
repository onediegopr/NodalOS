# Agent Workboard Decision Record BB-001

## Problem

Current agent work in NODAL OS and NEXA still spreads across chats, ad-hoc reports and manual handoffs.

- blockers can be lost between turns
- commits, tests and milestones are not always attached to a formal task
- handoff between chats or agents is manual
- task ownership is implicit instead of modeled
- verification-before-done is not formalized as a task closure rule

## Decision

NODAL OS will create an internal Agent Workboard model.

BotBoard is treated as a conceptual benchmark only. NODAL OS will not depend on BotBoard, embed BotBoard, or delegate task state to an external board.

## Principles

- Core-owned
- Evidence-first
- Verification before done
- Human owner
- Tasks auditable
- Blockers explicit
- Handoff bundles explicit
- Task closure requires evidence or explicit reason

## TAKE

- agent-native task board concept
- backlog for agent work
- assigned tasks
- progress notes
- blocker reports
- context attachments and evidence references
- verification before completion
- handoff bundles
- human owner and operator ownership

## ADAPT

- workboard as a Core model, not a UI-first artifact
- board integrated with Mission, AgentTask, Run, Evidence, Approval and Recipe
- board rendered in a future sidepanel surface without replacing the existing timeline or vertical stepper
- blockers aligned with a future failure taxonomy
- commit and test evidence attached to task closure

## REJECT

- external BotBoard dependency
- board as the system brain
- task closure without evidence or verification
- UI that bypasses policy
- task status decided by extension or service worker
- public marketplace behavior
- free-form multi-agent execution without governance

## Relation To NODAL OS

- Mission
- AgentTask
- Run
- Evidence
- Recipe
- Approval
- Commit/Test report
- Failure/Blocker
- Handoff

## Non-Goals

- no workboard UI implementation now
- no orchestration API now
- no scheduling now
- no external BotBoard dependency
- no runtime action policy changes

## Closure Rule

No task should be marked done without verification evidence or an explicit closure reason recorded by policy-governed Core surfaces.
