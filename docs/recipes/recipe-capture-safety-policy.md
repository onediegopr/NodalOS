# Recipe Capture Safety Policy

Phase: 9/9 - Recipe Capture Draft.

`RecipeCaptureSafetyPolicy` evaluates capture sessions and drafts without executing anything.

## Blocked

The policy blocks:

- future browser capture.
- future desktop capture.
- future connector capture.
- disabled capture mode.
- raw input values.
- evidence that is not reference-only.
- evidence requiring real capture.
- secret-like inputs for run-ready conversion.
- 2FA/CAPTCHA/challenge capture as automation.
- browser or desktop action drafts as live actions.
- unknown draft steps.
- template mapping that tries to bypass composite readiness.

## Warnings

The policy emits warnings for:

- personal, fiscal and payment data.
- submit-like actions.
- missing validation.
- missing evidence.
- missing tool trust.
- missing secret refs.
- ambiguous locators.
- relative coordinates.
- AI fallback suggestions.

Warnings can still block run-ready conversion. Draft review remains mandatory.

## Guarantees

Capture readiness always reports no recorder, no replay, no action authority and no live runtime.
