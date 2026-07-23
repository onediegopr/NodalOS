# NODAL OS — Design-partner field validation

Status: private-beta operating runbook. Not a public release plan, customer-data authorization or commercial launch.

## External-session prerequisite

External design-partner sessions may begin only after the owner approves written private-beta evaluation terms and the allowed distribution scope. Until that happens, this runbook is limited to internal participants or sessions run on an operator-controlled test device. It does not authorize sending the package, certificate or install link to external users.

## Purpose

Prepare and, once the prerequisite above is satisfied, run the complete NODAL OS loop with five to ten design partners and learn from real use before expanding product scope.

The field loop is intentionally narrow:

1. install the test-signed Windows private-beta package;
2. configure and test an authorized participant-owned provider in `/models/config`;
3. open a real but non-critical local workspace;
4. create one real mission;
5. review the proposed plan and exact `NODAL_HANDOFF.md` action;
6. approve, execute and verify that action;
7. inspect timeline and evidence;
8. download the canonical Markdown handoff;
9. optionally exercise guarded rollback;
10. record observed friction and the opt-in local timings.

The output of this block is evidence from use and a small set of reproducible fixes. It is not a reason to add browser automation, cloud sync, teams, billing, marketplace, managed AI or broader filesystem authority.

## Cohort

After the external-session prerequisite is satisfied, recruit five to ten people from the current initial ICP:

- technical founders and product builders;
- independent developers;
- software or AI consultants;
- small automation agencies;
- small technical teams already using their own AI providers.

A participant should have:

- a Windows 10/11 x64 test device;
- administrator/elevation access when the build is test-signed;
- a local repository or document workspace that is useful but non-critical;
- a current backup or clean source-control state;
- permission to use that workspace and its contents;
- an OpenAI-compatible local or cloud provider they are authorized to use;
- 50 to 70 minutes for one observed session.

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

For a controlled test-signed build, open PowerShell **as administrator** and run:

```powershell
./Install-NodalOS.ps1 -TrustTestCertificate
```

The elevation is required because the installer verifies and imports the exact test certificate into the local-machine `TrustedPeople` store. Keep the extracted bundle available through uninstall so the exact certificate can be identified and removed. A production/CA-trusted signature would not require this test-certificate step.

Do not install a newer test-signed revision over an already installed test-signed package. Test-signed private-beta revisions require clean uninstall with the previous bundle before installing another signed revision, because the previous bundle is the only artifact that can safely remove its exact certificate trust. Externally signed updates remain monotonic: same package name, same publisher and a strictly greater four-part version.

Default uninstall preserves `%LOCALAPPDATA%\NodalOS`. Removing user data remains a separate explicit action:

```powershell
./Uninstall-NodalOS.ps1 -RemoveUserData
```

For a test-signed bundle, run uninstall from an elevated PowerShell; it removes the package and exact included test-certificate trust. The `-RemoveUserData` switch remains the only destructive local-data action.

### Distribution boundary

The repository currently has no root license and issue `#28` remains open for product terms and third-party notices. This runbook does not authorize sending the package to external users or publishing a download.

Until explicit private-beta terms are approved, run sessions on an operator-controlled test device or with internal participants. After terms are approved, keep each external session inside the written participation and distribution scope. Never post the test-signed bundle, certificate or install link publicly.

## Data boundary

Before the session, explain these facts plainly:

- NODAL OS is local-first and the product server binds to loopback;
- the package is an engineering private-beta artifact, not a production release;
- workspace mutation is limited to the reviewed `NODAL_HANDOFF.md` candidate;
- BYOK credentials remain protected under the current Windows user;
- the facilitator must never ask to view, copy or transcribe an API key;
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

The participant installs the package and opens Mission Control without facilitator takeover. For a test-signed build, the participant or device operator performs the certificate-install step from elevated PowerShell.

Observe:

- certificate/install/elevation friction;
- whether the app launch is understood;
- whether the clean `NotStarted` state is clear;
- unexpected browser, firewall or Windows trust prompts;
- startup duration when local diagnostics were enabled before launch.

Do not teach product concepts until the participant has described what they believe the first screen means.

### 2. BYOK provider configuration — 5 to 10 minutes

The participant opens `/models/config`, configures an authorized local or cloud OpenAI-compatible route and runs the bounded connection test.

Observe:

- whether local versus cloud routing is understood;
- whether cloud authorization and privacy copy are clear;
- whether endpoint, model and cost/timeout fields are understandable;
- whether the participant trusts the credential-storage explanation;
- whether connection success, failure and optional preauthorized fallback are understandable;
- whether any raw key, prompt or response appears where it must remain excluded.

Do not ask the participant to expose the credential. Do not deliberately break a working provider merely to manufacture a fallback event.

### 3. Workspace and mission — 10 minutes

The participant selects a non-critical local workspace and writes one useful mission in their own words.

Observe:

- confidence in the local-first explanation;
- workspace-selection clarity;
- whether the mission framing is understood;
- whether paths or private content appear where they should remain redacted;
- whether the reviewed plan matches the participant's intent.

### 4. Review, approval and execution — 10 to 15 minutes

The participant reviews the exact action over `NODAL_HANDOFF.md`, then decides whether to approve it.

Observe:

- whether target, precondition, proposed result and rollback are understandable;
- whether approval feels scoped to the mission rather than a generic permission;
- hesitation, incorrect assumptions or surprise before execution;
- verification result and evidence visibility;
- recovery behavior for any stale, changed or failed-closed state.

Stop the session immediately if NODAL OS attempts to write outside the reviewed target, exposes a secret/private absolute path, binds outside loopback or claims success without verification.

### 5. Evidence, handoff and optional rollback — 10 minutes

The participant inspects the timeline/evidence and downloads `/mission/handoff.md`.

Ask the participant to explain:

- what changed;
- why it changed;
- what evidence proves the result;
- what they would send to a teammate or client;
- whether they trust the rollback boundary.

The first successful canonical handoff download is the current `time-to-first-value` event.

Exercise rollback only when the participant understands the result and the workspace remains in the expected exact-hash state. Never modify the file externally merely to force a positive rollback result.

### 6. Debrief — 10 minutes

Ask only after the participant has completed or stopped the loop:

1. What did you think NODAL OS would do before you used it?
2. At what point did you first receive useful value?
3. What was unclear or required facilitator help?
4. Did provider setup make local/cloud privacy and cost boundaries clear?
5. Did the approval explain enough to make a decision?
6. Did the evidence and handoff make the result trustworthy or transferable?
7. What would prevent you from using this on another non-critical project next week?
8. What is the smallest improvement that would make the next session materially better?

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
- Do not view or handle the participant's API key.
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
- External evaluation terms approved, or internal/operator-controlled session: yes / no
- Test-signed install elevation available: yes / no / not applicable
- Existing backup or clean source-control state: yes / no
- Diagnostics explicitly enabled: yes / no

## Outcomes

- Install completed: yes / no
- Clean Mission Control understood without help: yes / no
- Provider configured without exposing the credential: yes / no
- Bounded provider connection test completed: yes / no
- Provider/fallback result understood: yes / no / not applicable
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

- Provider/privacy interpretation:
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

- Complete provider → mission → approval → verification → handoff loop completed: yes / no
- Smallest next-session improvement:
- Do not build / out-of-scope requests mentioned:
```

## Finding severity

Use the existing audit vocabulary; do not invent another scoring system.

- **P0:** credible safety, secret exposure, data-loss or authority-boundary failure. Stop further sessions until understood.
- **P1:** the participant cannot complete installation/provider setup or the mission → approval → verification → handoff loop.
- **P2:** substantial confusion, recovery failure or repeated facilitator intervention with a viable workaround.
- **P3:** copy, spacing or polish that does not block or materially undermine trust.

One reproducible behavior should become one issue. Include the exact build and state, expected/actual behavior and redacted evidence. Do not attach the participant's workspace or raw diagnostic bundle.

## Cohort review

After five to ten valid sessions:

1. group duplicate findings by reproduced behavior;
2. compare local timing ranges without setting arbitrary targets retroactively;
3. identify the smallest set of P0/P1/P2 fixes that improves the next session;
4. implement and verify only those fixes;
5. repeat affected scenarios with the same product boundary;
6. keep production, licensing, signing and public release decisions separate.

A successful cohort does not automatically authorize a public release. It provides the evidence needed for targeted private-beta corrections and for a later decision on license, production signing and release/update channel.
