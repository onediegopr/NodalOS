# ADR M52 — Leak-Hardening por superficies

## Estado

Aceptado.

## Contexto

M51 sigue deferred por falta de target externo test-owned. El roadmap permite avanzar con private preview local, pero exige endurecer las superficies internas creadas para producto/admin antes de cualquier uso operativo.

## Decision

M52 define un corpus de secretos sintéticos y un evaluador de superficies que verifica que DTOs, bundles, reports y render models no contengan valores opacos, cookies, tokens, paths sensibles, bodies ni payloads raw.

Superficies cubiertas:

- Admin audit.
- Diagnostics bundle.
- Support bundle.
- Audit export.
- Public API DTOs design-only.
- Local product shell render models.
- Onboarding audit.
- Billing mock invoice preview.
- Email outbox mock.
- Release/update manifest.
- Installer dry-run report.
- Pre-production checkpoint report.

## Redaction

La redacción reutiliza `BrowserCredentialRedactor` y añade reemplazo exacto del corpus M52 para valores opacos sintéticos. El fuzz básico valida que tokens, cookies, bearer/api keys y paths se redactan, mientras hostnames y filenames inocuos se preservan.

## Skipped tests

M52 agrega `NexaSkippedTestsAuditReport` para listar skipped tests, razón, categoría, variable opt-in y si bloquean private preview local. Los skipped externos M51 no bloquean private preview local, pero sí bloquean preview externa.

## Fuera de alcance

- No convierte skipped live en passed.
- No declara M51 validado.
- No habilita SaaS público.
- No habilita billing/email real.
- No habilita sitios sensibles reales.
