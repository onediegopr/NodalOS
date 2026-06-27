# Recipe Capture Template Mapping

Phase: 9/9 - Recipe Capture Draft.

`RecipeCaptureTemplateMapper` maps a capture draft to Phase 8 template catalog metadata when possible.

## Rules

- Mapping uses template refs and metadata only.
- Mapping cannot override `RecipeTemplateReadinessEvaluator`.
- Mapping cannot convert a capture draft into a fixture-ready, run-ready or ready-for-live-runtime recipe.
- Unknown systems map to future-gated manual review.
- Mercado Libre, Mercado Pago, ARCA, fiscal, ERP, browser portal and Computer Use mappings preserve their preview/human-gated/live-blocked status.

## Composite Readiness

Template mappings must preserve composite readiness. If the mapped template remains blocked because of tool trust, secret refs, connector eligibility, trigger observe-only state, evidence, validation, approval, lab safety or live runtime status, the capture draft remains blocked too.

## Out Of Scope

No template mapping performs connector execution, API calls, browser automation, desktop automation, network calls, vault access, scheduler, replay or capture.
