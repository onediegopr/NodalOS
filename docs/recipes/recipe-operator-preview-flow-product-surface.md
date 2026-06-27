# Recipe Operator Preview Flow Product Surface

Block: `NODAL_RECIPE_RUNTIME_PRODUCT_SURFACE_003_OPERATOR_PREVIEW_FLOW_HANDOFF_EXPORT_SURFACE`

Product-surface phase: 3/4.

Phase name: Operator Preview Flow + Handoff Export Surface.

Built on:

- Product-surface Phase 1: `2b93eb4392f7817d9e13550a9aff83df246f5cb9`
- Product-surface Phase 2: `a8993e132999b7e004ee67bcc9393c158cb79812`
- Recipe Runtime final hardening: `409cd6da0ff902287d85b3dbc6a6b6262cd54162`

## Purpose

The operator preview flow is a read-only product surface for showing what an operator would review before any future safe runtime could be considered. It explains template readiness, required evidence, approval and human-review needs, blocked live modes, unavailable actions, and safe next actions.

The surface is preview-safe and fixture-safe only. It does not start recipes, process workitems, open connectors, request secrets, write export files, create recorder output, or enable browser or desktop automation.

## Preview Contents

An operator preview contains:

- Template id, name, system, region, and category.
- Preview and readiness status.
- Operator review summary.
- Required review sections.
- Required approval summary.
- Required evidence summary.
- Expected human intervention points.
- Blocked live runtime explanation.
- Disabled action labels and unavailable action state.
- Safe next action.
- Not automated summary.
- System-specific preview summary.

## Disabled Actions

Actions that would imply live behavior are represented as unavailable states. The product surface can explain why they are unavailable, but cannot activate them.

Unavailable examples include live runtime, connector execution, secret access, live browser automation, live desktop automation, recorder/playback, real capture, live locator repair activation, automatic workitem processing, fiscal submission, payment execution, marketplace mutation, message send, delete, write, and publication actions.

## Product Boundary

Allowed wording:

- Preview only.
- No live execution.
- Read-only.
- Fixture-safe.
- Operator review required.
- Live runtime blocked.
- Automation not enabled.
- Connector execution not enabled.
- Secrets by reference only.
- Evidence by reference only.
- Observe-only trigger.

This surface must not imply that NODAL OS can execute or live automate these recipes.
