# NODAL OS / NODRIX - M455 Recipe Risk / DSL Discovery

## Scope

M455 reviewed Recipe Manifest, Step Library, Scheduled Read-Only, Orchestration Command/Facade, Automation Event/Evidence, Selector Safety/Handoff, EvidenceRefBridge, common redaction, and the Automation Layer ADR before adding Recipe Risk Classifier V1 and a DSL Decision Record.

This milestone remains contract-only. It does not implement a DSL parser, runtime, recorder, replay, browser automation, scheduler, queue, or UI.

## Existing Recipe / Step / Risk Surface

- Recipe Manifest already defines recipe status, step manifests, action kinds, local policy, approval flags, runtime-deferred flags, and manifest-policy validation.
- Step Library already defines step kinds, risk levels, capabilities, approval defaults, sensitive defaults, runtime-deferred flags, and step-policy validation.
- Orchestration Command Contracts and Facade preserve `RuntimeExecutionAllowed=false`, `RuntimeExecutionDeferred=true`, and `Executed=false`.
- Scheduled Read-Only contracts already define read-only and no-scheduler invariants.
- Automation Event/Evidence and Selector Safety/Handoff provide evidence, handoff, selector, and redaction contracts.

## Missing For Recipe Risk Classifier

- A dedicated contract to classify recipe steps by risk category.
- A risk profile that rolls up max step risk.
- Explicit approval requirements for submit, payment, destructive, publish/send, credential, file mutation, and high-risk steps.
- Explicit human handoff requirement for login, captcha, two-factor, credentials, and human-decision steps.
- Explicit no-authority classification semantics.

## Missing For DSL Decision

- A formal ADR stating that DSL is representation only.
- A future JSON canonical model requirement.
- Explicit parser/runtime deferral.
- Explicit import validation requirement.
- Explicit direct execution prohibition.
- Clear TagUI inspiration boundary without dependency or code reuse.

## Relationship With Step Library

Step Library remains the descriptive catalog. Recipe Risk Classifier consumes step-like inputs conceptually and does not execute step definitions.

## Relationship With Recipe Manifest

Recipe Manifest remains the governance manifest. Recipe Risk Classifier can profile manifest-like step metadata but cannot make a recipe executable.

## Relationship With Scheduled Read-Only

Read-only and extraction steps can classify as Low or Medium, but runtime remains deferred and read-only schedules still require their own validation.

## Relationship With Automation Event/Evidence

Classifications carry evidence refs and use redaction. Future automation events may include risk profile references, but this milestone creates no automation runtime.

## Relationship With Selector Safety / Handoff

Browser automation and selector categories remain future-only. Login, captcha, two-factor, credentials, ambiguity, and human decisions require handoff semantics.

## DSL Runtime Confusion Risk

The main risk is treating a readable DSL as an executable script. The ADR and contracts explicitly state that DSL is representation, not runtime.

## Direct Import Risk

Imported DSL or templates must never run directly. Future import must validate, normalize, risk-classify, redact, and compile to JSON canonical model before any further use.

## V1 Scope

M455-M457 adds recipe risk contracts, classifier service, serializer, fixtures, DSL ADR, tests, reports, artifact, and roadmap update.

## Not Implemented

No DSL parser, DSL executor, recorder, replay, browser automation, workflow designer, queue, trigger, scheduler, timer, background worker, API/HTTP/gRPC, UI, worker runtime, browser action, desktop action, recipe/skill/step execution, persistence DB, cloud runtime, package install/uninstall, external RPA dependency, TagUI dependency, or direct import implementation is added.

## Decision

Proceed with Recipe Risk Classifier V1 and DSL Decision Record as contract-only safety scaffolding before Claude pre-implementation audit.
