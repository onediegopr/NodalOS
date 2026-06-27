# Recipe Product Surface Navigation Messaging

Line: `NODAL_RECIPE_PRODUCT_SURFACE_NAVIGATION_MESSAGING_READ_ONLY`

Phase: 1/3 - Navigation + Capability Label Taxonomy

This line is separate from the closed Recipe Runtime Product Surface line. It only defines read-only navigation labels, capability badges, disabled action messages, and product copy guardrails for the already-closed surface.

Closed Product Surface status: `COMPLETE_READ_ONLY_PREVIEW_SAFE_FIXTURE_SAFE_PRODUCT_SURFACE_CLOSED`

Final close commit: `df92f6fb4c86f246e1d956ede9fd4876e1d0080d`

## Allowed Claim

NODAL OS has a fixture-safe Recipe Runtime product surface with read-only catalog, lab, templates, readiness explanations, operator previews and handoff/export preview summaries.

## Forbidden Claim

NODAL OS can execute/live automate these recipes.

## Navigation Labels

- Recipe Catalog
- Recipe Lab
- Template Detail
- Readiness Explanation
- Operator Preview
- Handoff/Export Preview
- Safe Demo

These labels are read-only entrypoints. They do not start recipes, process workitems, open connectors, request secrets, enable automation, create capture/replay, or unlock live runtime.

## Capability Badges

- Read-only
- Preview-safe
- Fixture-safe
- Demo-safe
- Live runtime blocked
- Connector execution disabled
- Secrets by reference only
- Export preview only
- Human approval path required
- Not automated

Badges are explanatory only. They grant no runtime capability, connector execution, secret access, external mutation, or live runtime.

## Disabled Actions

Disabled action messaging covers:

- Recipe execution
- Workitem processing
- Connector/API
- Vault/secrets
- Browser automation
- Desktop automation
- Recording/playback/capture-draft
- Export file generation
- Fiscal/payment/marketplace/message/delete/write actions

Every disabled action states that the capability is not enabled and provides a safe next action, such as reviewing readiness, checking refs, or preparing a human approval path.

## Product Copy Policy

Product-facing copy must stay read-only, preview-safe, fixture-safe, and demo-safe. It must not imply live runtime, live automation, connector/API calls, vault access, recorder/replay/capture, automatic workitem processing, fiscal/payment/marketplace/message/delete/write live actions, or real export generation.

Future work remains outside this line.
