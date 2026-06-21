# Sidepanel Token Dry Run - M612-M614

Decision target: `SIDEPANEL_TOKEN_DRY_RUN_READY`.

This pack simulates and plans Research OS visual token integration for the Chrome extension sidepanel. It does not modify `sidepanel.html`, `sidepanel.css`, `sidepanel.js`, or `manifest.json`; it only documents token mapping, patch units, and future no-runtime-coupling tests.

## M612 - Sidepanel Visual Token Integration Dry Run

Product sidepanel files inspected:

- `browser-extension/onebrain-chrome-lab/sidepanel.html`
- `browser-extension/onebrain-chrome-lab/sidepanel.css`
- `browser-extension/onebrain-chrome-lab/sidepanel.js`
- `browser-extension/onebrain-chrome-lab/manifest.json`

No product sidepanel files were modified.

### Token Mapping

Research OS tokens map to sidepanel candidates as follows:

- Background: `--nos-color-bg` maps to `body` background and existing `--bg`.
- Muted background: `--nos-color-bg-muted` maps to sticky header, tabs, and subdued panels.
- Surface: `--nos-color-bg-soft` maps to `.surface`, inputs, and mission cards.
- Text: `--nos-color-text` maps to `--ink` and primary UI copy.
- Muted text: `--nos-color-text-muted` maps to `--muted`, labels, and metadata.
- Borders: `--nos-color-border` maps to `--line`, card borders, tab borders, input borders, and timeline dividers.
- Accent: `--nos-color-accent` maps to primary research highlights, selected navigation, focus rings, and mission status accents.
- Success: `--nos-color-success` maps to ready/done badges.
- Warning: `--nos-color-warning` maps to human intervention and evidence-required states.
- Danger: `--nos-color-danger` maps to stop/danger/blocked states.
- Focus ring: `--nos-focus-ring` maps to focus-visible outlines.
- Radius: `--nos-radius-panel` and `--nos-radius-control` map to `.surface`, `.tab`, buttons, inputs, and timeline cards.
- Spacing: `--nos-space-*` maps to `main`, `.app-header`, `.tabs`, `.grid`, and card internals.
- Card padding: `--nos-card-padding` maps to `.surface` and timeline cards.
- Sidebar width: future-only; no current sidebar exists because the current sidepanel uses top tabs.
- Heading typography: `Georgia` fallback for h1/h2 future shell headings.
- UI typography: current UI can move from Aptos/Segoe UI to Inter/Geist/system fallback without remote fonts.

### Current Style Mapping

Existing variables:

- `--bg`: muted green-gray background.
- `--panel`: near-white panel color.
- `--ink`: dark green-black text.
- `--muted`: secondary text.
- `--line`: border color.
- `--green`, `--red`, `--amber`, `--blue`, `--black`: status and accent colors.

Current style risks:

- `.surface` uses near-white panels and can drift toward generic cards.
- `.tab.active` uses a black active state that does not match Research OS accent direction.
- `.timeline-card` uses a dark technical surface that can bring back the old control-center feel.
- `.log-pane` and details surfaces can dominate as raw diagnostics.
- `button.primary` currently maps to green operational intent and can imply execution authority.
- `button.danger` and `.stop-button` must remain visible and safety-forward.
- Runtime and credential setup selectors are behavior-adjacent and require manual QA even for visual changes.

### Dry-Run Integration

The dry-run artifact lists selectors, current style summaries, proposed Research OS tokens, expected effects, risk levels, manual QA requirements, future safety, and rollback notes.

Key dry-run conclusion:

- First future patch should be CSS variable addition only.
- Second future patch may remap base background/text/border tokens.
- JS must not be touched during token integration unless a separate scoped visual/no-op approval exists.
- Manifest must remain unchanged by default.

## M613 - Sidepanel Diff / Patch Plan

Future patch units:

- Patch 1: Add Research OS CSS variables.
- Patch 2: Apply base background/text/border tokens.
- Patch 3: Apply sidebar/nav visual structure.
- Patch 4: Apply mission hero structure.
- Patch 5: Apply status/badge/block states.
- Patch 6: Apply activity feed human-readable styling.
- Patch 7: Apply accessibility/focus/contrast.
- Patch 8: Remove or quarantine old purely visual dead styles only if safe.

Patch constraints:

- Future HTML/CSS changes can only happen in a separately approved milestone.
- Future JS changes are blocked by default and limited to visual/no-op labels only if separately approved.
- Manifest changes are blocked by default.
- No patch may connect runtime, enable capabilities, persist state, use Provider Calls, use filesystem behavior, or promote artifacts to source of truth.

## M614 - No-Runtime Coupling Test Plan

Future checks must detect:

- New runtime message paths.
- New Provider/cloud paths.
- New network paths.
- Filesystem APIs.
- Direct browser executor references.
- Capability enablement.
- Productive consent persistence.
- Approval mutation.
- Source-of-truth promotion.
- Event routing changes.
- Scheduled dispatch, async backlog, or background operator additions.
- Usage analytics additions.
- System pasteboard writes.
- Executable task graph or execution request surfaces.

Visual regression checks must detect:

- Missing Research OS tokens.
- Missing blocked-state anatomy.
- Generic SaaS dashboard return.
- Dark technical control center return.
- Advisor as chatbot.
- Activity Feed as raw logs.
- Models as provider selector implying calls.
- Runtime appearing active.
- Agents appearing autonomous.
- Settings appearing productive.

Go criteria for a future small token patch:

- Working tree clean.
- Rollback plan exists.
- Patch unit is small.
- No-runtime-coupling tests exist.
- Visual QA baseline exists.
- Full suite green.
- Manual preview reviewed.
- No source-of-truth ambiguity.

No-Go criteria:

- Patch touches behavior-heavy JS.
- Patch modifies manifest permissions.
- Patch changes runtime or capability control flow.
- Patch removes blocked-state explanation.
- Patch introduces remote resources.
- Patch creates Provider/cloud/network path.

## Closeout

M612-M614 is dry-run-ready only. Sidepanel product files are unchanged. Product UI migration, token integration into product files, runtime, filesystem feature behavior, evidence verification, LLM/provider/cloud, productive consent, capability enablement, and source-of-truth promotion remain blocked.
