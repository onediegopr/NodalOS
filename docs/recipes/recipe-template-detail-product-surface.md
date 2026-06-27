# Recipe Template Detail Product Surface

Product-surface phase: 2/4.

Phase name: Template Detail + Readiness Explanation UX.

Built on Recipe Product Surface Phase 1 commit: `2b93eb4392f7817d9e13550a9aff83df246f5cb9`.

Underlying Recipe Runtime hardening commit: `409cd6da0ff902287d85b3dbc6a6b6262cd54162`.

## Purpose

The Template Detail product surface makes a single Recipe template explainable without adding runtime capability.

It can show:

- what the template does.
- system family, region, country, and category.
- preview or fixture status.
- readiness summary.
- blocking reasons and missing requirements.
- human-review and approval requirements.
- tool trust refs.
- secret refs by alias/id only.
- trigger observe-only status.
- evidence and validation refs.
- locator and capture implications.
- live-blocked explanation.
- safe next action.
- not-included summary.

## Read-only Contract

The detail surface is read-only, preview-safe, fixture-safe, and reference-only. It cannot start a recipe, process a workitem, open a connector, call an API, read a vault, request secret values, enable browser or desktop runtime, create recorder/playback/capture paths, apply locator repair, or authorize live runtime.

## Product Copy Boundary

Allowed copy uses preview-safe words such as `Preview`, `Fixture-safe`, `Read-only`, `Template detail`, `Draft`, `Requires human review`, `Approval required`, `Live runtime blocked`, `Connector execution not enabled`, `Secrets by reference only`, `Evidence by reference only`, `Observe-only trigger`, `Not included`, and `Future-gated`.

Action-oriented live automation wording is blocked for this surface.
