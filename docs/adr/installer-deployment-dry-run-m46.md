# ADR M46: Installer / Deployment Dry Run

## Status

Accepted for M46.

## Context

NEXA has packaging, diagnostics, configuration profiles, and release/update models. Before a real installer exists, the product needs a dry-run deployment model that can explain what would be installed and what prerequisites are missing without changing the machine.

## Decision

M46 introduces model-only installer and deployment contracts:

- installer plan;
- installer plan steps;
- file layout;
- permission requirements;
- preflight checks;
- release manifest integrity dependency;
- rollback dry-run plan;
- deployment rollback dry-run decision.

The dry-run reports the directories, files, components, permissions, prerequisites, enabled/disabled features, and rollback steps that would apply to a deployment profile.

## Safety Rules

The dry-run cannot:

- create files outside a sandbox/temp model;
- register real services;
- create scheduled tasks;
- touch the registry;
- install browser/runtime dependencies;
- open public ports;
- enable real auto-update;
- execute rollback.

## Preflight

The evaluator checks OS support, .NET runtime compatibility, browser/CDP declarations, vault provider declaration, diagnostics redaction, tenant governance, admin runtime, license evaluator, and release manifest hash/signature metadata.

## Profiles

Deployment profiles use the M44 configuration profile model. Unknown profiles, dangerous ProductionLocked defaults, real billing, real email, public SaaS activation, SensitiveRealPilot, ProductiveVault without controls, and real auto-update fail closed.

## Rollback

Rollback is represented as a plan and decision only. No rollback command is executed in M46.

## Out of Scope

M46 does not create an installer, deploy a public SaaS service, download binaries, execute updates, register services, or modify the host system.
