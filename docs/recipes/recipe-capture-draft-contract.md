# Recipe Capture Draft Contract

Phase: 9/9 - Recipe Capture Draft.

Recipe Capture Draft is an original NODAL OS contract layer inspired by recorder/capture ideas from workflow systems, but it does not implement recording, replay, browser capture, desktop capture or live automation.

## Contract

The capture layer defines:

- `RecipeCaptureSession`
- `RecipeCapturedStep`
- `RecipeCapturedInput`
- `RecipeCapturedLocator`
- `RecipeCapturedEvidenceRef`
- `RecipeDraft`
- `RecipeDraftGenerationResult`
- `RecipeDraftTemplateMapping`

Capture sessions are draft-only and can use only fixture, manual-description, imported-trace-ref or reference-only modes. Future browser, desktop and connector capture modes are represented as blocked states.

## Output

Capture may produce:

- a capture draft.
- a suggested recipe draft.
- suggested variables.
- suggested locators.
- suggested validations.
- suggested evidence expectations.
- suggested approval/human gates.
- suggested template mappings.

Capture must not produce:

- run-ready recipes.
- auto-executable recipes.
- live browser actions.
- live desktop actions.
- replayable recorder output.
- raw secrets.
- raw sensitive payloads.

## Safety Boundary

All capture evidence is reference-only. Inputs store labels, roles, categories and redaction status only. Secret-like inputs become warnings and block run-ready conversion.
