# ADR M44: Production Configuration Profiles

## Status

Accepted for M44.

## Context

The product now has admin, licensing, diagnostics, mock onboarding, and mock billing. Before production rollout, runtime behavior needs explicit configuration profiles by environment, tenant posture, and risk level.

## Decision

M44 introduces configuration profiles:

- Development;
- Test;
- LocalSandbox;
- InternalPreview;
- PrivateBeta;
- ProductionLocked;
- EnterpriseControlled.

Profiles define enabled/disabled features, required approvals, vault mode, browser runtime mode, recorder/replay mode, sensitive site mode, diagnostics mode, support mode, billing mode, and email mode.

## Development and Test

Development/Test may allow fixtures, synthetic secrets, sandbox vault, DPAPI synthetic vault, local CDP read-only, and verbose redacted diagnostics.

They block real billing, real email, public SaaS activation, SensitiveRealPilot, ProductiveVault by default, ProfileRaw, RecorderProductive, and ReplayProductive.

## ProductionLocked

ProductionLocked is fail-closed:

- SensitiveRealPilot disabled;
- ProductiveVault disabled unless future explicit enterprise/compliance override;
- RecorderProductive disabled;
- ReplayProductive disabled;
- real billing disabled;
- real email disabled;
- support metadata-only;
- diagnostics redacted.

## Evaluation

`NexaConfigurationProfileEvaluator` fails closed when:

- profile is unknown;
- feature is incompatible with the profile;
- SensitiveRealPilot lacks compliance decision;
- ProductiveVault lacks entitlement, admin override, and compliance decision;
- real billing/email/public SaaS is requested;
- productive recorder/replay is requested;
- ProfileRaw is exposed.

## Out of Scope

M44 does not deploy production, enable public SaaS, enable real billing/email, enable real sensitive sites, or expose raw profiles.

## Consequences

Runtime/product behavior can now be evaluated against explicit environment profiles before release channel decisions.
