# Recipe Human Intervention Contract

Phase: 4/9 - Human Intervention + Approval Narrative 2.0.

Phase 1 commit: `2079a04efe66e6187f7fe018c772ec3f6b51f9d8`.

Phase 2 commit: `29573f36c8ce1e9fef83d2627aaeb34d592b8b2c`.

Phase 3 commit: `edfc1693dd0e067113a523a56479f089679f881e`.

## Scope

`RecipeHumanInterventionRequest` makes blocked or sensitive recipe states reviewable by an operator without enabling live runtime.

It records:

- intervention id,
- recipe id/version,
- run id,
- optional step/block/workitem refs,
- reason and kind,
- status,
- source trigger,
- required operator action summary,
- blocked action summary,
- risk profile ref,
- evidence refs,
- validation refs,
- policy decision refs,
- timeline refs,
- redaction summary ref,
- approval narrative ref,
- safe next action,
- allowed manual outcomes,
- disallowed outcomes,
- operator note ref.

Operator notes are refs or redacted summaries. Raw sensitive content is not included.

## Intervention Kinds

Human intervention covers login, credentials, 2FA, CAPTCHA/challenge, payment, fiscal/legal review, message send review, public posting, marketplace listing changes, price/stock changes, deletion, mutation, file write, external system mutation, personal data, secret handling, locator ambiguity, perception ambiguity, validation failure, policy block, unknown unsafe state, manual checkpoints, and operator choice.

## Safety

Human intervention never implies live runtime execution. Approval does not unlock browser or desktop live runtime. CAPTCHA, 2FA, and challenge states require operator handling or remain blocked; they are not solved automatically.
