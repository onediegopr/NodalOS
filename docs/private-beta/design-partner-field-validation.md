# NODAL OS — Design-partner field validation

Status: private-beta operating runbook. Not a public release plan, customer-data authorization or commercial launch.

## Purpose

Run the complete NODAL OS loop with five to ten design partners and learn from real use before expanding product scope.

The field loop is intentionally narrow:

1. install the test-signed Windows private-beta package;
2. open a real but non-critical local workspace;
3. create one real mission;
4. review the proposed plan and exact `NODAL_HANDOFF.md` action;
5. approve, execute and verify that action;
6. inspect timeline and evidence;
7. download the canonical Markdown handoff;
8. optionally exercise guarded rollback;
9. record observed friction and the opt-in local timings.

The output of this block is evidence from use and a small set of reproducible fixes. It is not a reason to add browser automation, cloud sync, teams, billing, marketplace, managed AI or broader filesystem authority.

## Cohort

Recruit five to ten people from the current initial ICP:

- technical founders and product builders;
- independent developers;
- software or AI consultants;
- small automation agencies;
- small technical teams already using their own AI providers.

A participant should have:

- a Windows 10/11 x64 test device;
- a local repository or document workspace that is useful but non-critical;
- a current backup or clean source-control state;
- permission to use that workspace and its contents;
- an OpenAI-compatible local or cloud provider they are authorized to use;
- 45 to 60 minutes for one observed session.

Do not use regulated, production-only, unrecoverable or customer-confidential material in the first cohort.

## Build used in a session

Each session record must identify:

- exact `main` commit;
- package version;
- SHA-256 from the update manifest;
- Windows version;
- whether the package is test-signed;
- whether the provider route is local or explicitly authorized cloud.

Use one validated private-beta bundle built from the recorded `main` state. Do not mix artifacts from unrelated PRs during a cohort.

Installation and uninstall instructions remain canonical in:

- `docs/distribution/windows-private-beta-packaging.md`;
- `docs/distribution/privacy-and-local-data.md`.

For a controlled test-signed build:

```powershell
./Install-NodalOS.ps1 -TrustTestCertificate
```

Default uninstall preserves `%LOCALAPPDATA%\NodalOS`. Removing user data remains a separate explicit action:

```powershell
./Uninstall-NodalOS.ps1 -RemoveUserData
```

### Distribution boundary

The repository currently has no root license and issue `#28` remains open for product terms and third-party notices. This runbook does not authorize sending the package to external users or publishing a download.

Until explicit private-beta terms are approved, run sessions on an operator-controlled test device or under separately approved written participation terms. Never post the test-signed bundle, certificate or install link publicly.

## Data boundary

Before the session, explain these facts plainly:

- NODAL OS is local-first and the product server binds to loopback;
- the package is an engineering private-beta artifact, not a production release;
- workspace mutation is limited to the reviewed `NODAL_HANDOFF.md` candidate;
- BYOK credentials remain protected under the current Windows user;
- diagnostics and activation timings are disabled by default;
- enabling diagnostics stores a bounded local JSONL file and performs no automatic upload;
- sharing a handoff, screenshot, diagnostic file or session note is always a human decision.

Never collect in a GitHub issue or shared session note:

- API keys, tokens or credential references;
- absolute workspace paths or Windows usernames;
- raw repository/customer content;
- provider prompts or responses;
- private screenshots containing source material;
- diagnostic files the participant has not reviewed;
- participant names, email addresses or company secrets.

Use an alias such as `DP-01`. Record workspace and provider only by broad class, for example `small .NET repo` and `authorized cloud OpenAI-compatible route`.

This repository and its issue tracker are public. Keep per-participant session records in an approved private location and never commit them here. Only a reproducible product finding, stripped of participant and workspace data, may become a public issue.

## Session flow

### 1. Install and first launch — 5 to 10 minutes

The participant installs the package and opens Mission Control without facilitator takeover.

Observe:

- certificate/install friction;
- whether the app launch is understood;
- whether the clean `NotStarted` state is clear;
- unexpected browser, firewall or Windows trust prompts;
- startup duration when local diagnostics were enabled before launch.

Do not teach product concepts until the participant has described what they believe the first screen means.

### 2. Workspace and mission — 10 minutes

The participant selects a non-critical local workspace and writes one useful mission in their own words.

Observe:

- confidence in the local-first explanation;
- workspace-selection clarity;
- whether the mission framing is understood;
- whether paths or private content appear where they should remain redacted;
- whether the reviewed plan matches the participant's intent.

### 3. Review, approval and execution — 10 to 15 minutes

The participant reviews the exact action over `NODAL_HANDOFF.md`, then decides whether to approve it.

Observe:

- whether target, precondition, proposed result and rollback are understandable;
- whether approval feels scoped to the mission rather than a generic permission;
- hesitation, incorrect assumptions or surprise before execution;
- verification result and evidence visibility;
- recovery behavior for any stale, changed or failed-closed state.

Stop the session immediately if NODAL OS attempts to write outside the reviewed target, exposes a secret/private absolute path, binds outside loopback or claims success without verification.

### 4. Evidence, handoff and optional rollback — 10 minutes

The participant inspects the timeline/evidence and downloads `/mission/handoff.md`.

Ask the participant to explain:

- what changed;
- why it changed;
- what evidence proves the result;
- what they would send to a teammate or client;
- whether they trust the rollback boundary.

The first successful canonical handoff download is the current `time-to-first-value` event.

Exercise rollback only when the participant understands the result and the workspace remains in the expected exact-hash state. Never modify the file externally merely to force a positive rollback result.

### 5. Debrief — 10 minutes

Ask only after the participant has completed or stopped the loop:

1. What did you think NODAL OS would do before you used it?
2. At what point did you first receive useful value?
3. What was unclear or required facilitator help?
4. Did the approval explain enough to make a decision?
5. Did the evidence and handoff make the result trustworthy or transferable?
6. What would prevent you from using this on another non-critical project next week?
7. What is the smallest improvement that would make the next session materially better?

Avoid feature brainstorming until the observed core-loop problems are captured.

## Local metrics to transcribe

When the participant explicitly enables `/settings/diagnostics`, record only the values shown in the UI:

- startup to ready;
- time to first value;
- mission creation to verified completion;
- measured mission count;
- error count.

These numbers establish a baseline. They are descriptive, not release thresholds, until the first cohort provides a real distribution.

Mission completion is recorded only when verified execution completes in the current process. After a restart, record the value as `not measured` rather than reconstructing it from private state.

Do not copy the local JSONL file by default. A participant may inspect and share it voluntarily after confirming that it contains only the disclosed redacted fields.

## Facilitator rules

- Let the participant drive the device.
- Do not paste a prepared mission unless the participant cannot formulate one.
- Do not explain a control before the participant attempts to interpret it.
- Do not bypass, pre-approve or weaken product safety behavior.
- Do not fix code during the session.
- Record the exact screen/state where friction happened.
- Separate observed behavior from participant opinion and facilitator inference.
- Create no speculative roadmap item without a reproduced need.

## Session record template

Copy this section once per participant. Store the redacted record in an approved private location; do not commit per-participant records to this public repository.

```markdown
# Design-partner session — DP-XX

## Context

- Date UTC:
- Facilitator alias:
- Main commit:
- Package version:
- Package SHA-256:
- Windows version:
- Workspace class:
- Provider class: local / authorized cloud
- Operator-controlled device or approved private-beta terms confirmed: yes / no
- Existing backup or clean source-control state: yes / no
- Diagnostics explicitly enabled: yes / no

## Outcomes

- Install completed: yes / no
- Clean Mission Control understood without help: yes / no
- Workspace selected: yes / no
- Real mission created: yes / no
- Exact action understood before approval: yes / no
- Execution completed and verified: yes / no
- Timeline/evidence understood: yes / no
- Handoff downloaded: yes / no
- Rollback exercised: yes / no / not applicable
- Session stopped early: yes / no
- Stop reason, when applicable:

## Local timings

- Startup to ready:
- Time to first value:
- Mission creation to verified completion:
- Measured mission count:
- Error count:

## Observations

### First useful value

- Participant-described moment:
- Facilitator observation:

### Friction

1. Exact screen/state:
   - Participant action:
   - Expected behavior:
   - Actual behavior:
   - Facilitator help required:
   - Reproducible: yes / no / unknown

### Trust and control

- Approval interpretation:
- Evidence interpretation:
- Handoff usefulness:
- Rollback confidence:

## Candidate findings

1. Short title:
   - Severity: P0 / P1 / P2 / P3
   - Reproduction steps:
   - Expected:
   - Actual:
   - Redacted evidence:
   - Existing issue, when any:

## Session conclusion

- Core loop completed: yes / no
- Smallest next-session improvement:
- Do not build / out-of-scope requests mentioned:
```

## Finding severity

Use the existing audit vocabulary; do not invent another scoring system.

- **P0:** credible safety, secret exposure, data-loss or authority-boundary failure. Stop further sessions until understood.
- **P1:** the participant cannot complete install or the core mission → approval → verification → handoff loop.
- **P2:** substantial confusion, recovery failure or repeated facilitator intervention with a viable workaround.
- **P3:** copy, spacing or polish that does not block or materially undermine trust.

One reproducible behavior should become one issue. Include the exact build and state, expected/actual behavior and redacted evidence. Do not attach the participant's workspace or raw diagnostic bundle.

## Cohort review

After five to ten sessions:

1. group duplicate findings by reproduced behavior;
2. compare local timing ranges without setting arbitrary targets retroactively;
3. identify the smallest set of P0/P1/P2 fixes that improves the next session;
4. implement and verify only those fixes;
5. repeat affected scenarios with the same product boundary;
6. keep production, licensing, signing and public release decisions separate.

A successful cohort does not automatically authorize a public release. It provides the evidence needed for targeted private-beta corrections and for a later decision on license, production signing and release/update channel.
