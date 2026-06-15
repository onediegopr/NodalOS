# ADR M62 — Real Email Delivery Design + Sandbox Provider

## Estado

Aceptado.

## Contexto

El onboarding mock usa outbox local. M62 disena email real futuro y agrega un provider sandbox sin enviar emails reales.

## Decision

M62 define provider kinds `MockOutboxOnly`, `SandboxProvider`, `RealProviderFuture` y `Disabled`.

Reglas:

- Mock/sandbox permitido en private preview local.
- Real provider queda design-only.
- Real email delivery queda disabled.
- Templates son redacted.
- Sandbox outbox no envia emails reales.

Templates iniciales:

- FreeLicenseRequested.
- TrialCreated.
- LicenseExpiring.
- PlanUpgradeInterest.
- SupportBundleReady.
- PrivatePreviewFeedbackReceived.

## Fuera de alcance

- SMTP/API real.
- Usuarios externos reales.
- Secretos/cookies/session material en templates.
