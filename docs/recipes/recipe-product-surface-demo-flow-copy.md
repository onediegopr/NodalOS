# Recipe Product Surface Demo Flow Copy

Line: `NODAL_RECIPE_PRODUCT_SURFACE_NAVIGATION_MESSAGING_READ_ONLY`

Phase: 2/3 - Demo Flow Copy

This phase adds read-only, preview-safe, fixture-safe copy for explaining the already-closed Recipe Product Surface. It does not reopen the closed Product Surface line and does not add runtime behavior.

Closed Product Surface status: `COMPLETE_READ_ONLY_PREVIEW_SAFE_FIXTURE_SAFE_PRODUCT_SURFACE_CLOSED`

Phase 1 commit: `103e22fe32de79d10438328fcb221c9ee46e54cf`

## Demo Flow

1. Browse Recipe Catalog.
2. Review Recipe Lab.
3. Open Template Detail.
4. Read Readiness Explanation.
5. Review Operator Preview.
6. Review Handoff/Export Preview.
7. Understand blocked live runtime.
8. Understand safe next action.

Each step includes a title, subtitle, operator-facing description, safety badges, blocked action note, safe next action, unavailable action labels, and claim guardrail reminder.

## Empty States

- No live runtime available.
- No connector connected.
- No credentials requested.
- No export file generated.
- No workitems processed.
- No browser or desktop automation performed.
- Preview data only.

## Disabled Control Copy

Disabled controls explain that recipe execution, connector/API calls, secret reading, browser and desktop automation, recording/playback/capture-draft, real export file generation, and automatic workitem processing are not enabled.

## Allowed Claim

NODAL OS has a fixture-safe Recipe Runtime product surface with read-only catalog, lab, templates, readiness explanations, operator previews and handoff/export preview summaries.

## Forbidden Claim

NODAL OS can execute/live automate these recipes.

## Boundary

This line remains read-only / preview-safe / fixture-safe only. It adds no live recipe execution, live runtime, browser automation, desktop automation, connector/API/network calls, vault access, scheduler/background worker, watcher/hook/listener, recorder/replay/capture, automatic workitem processing, fiscal/payment/marketplace/message/delete/write live actions, real export file generation, live locator repair apply, protected browser/live execution changes, or Docker/runner/remote-control/proxy/challenge changes.
