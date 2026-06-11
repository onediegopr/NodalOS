# ONE BRAIN Public Storyline

## One-line

ONE BRAIN turns safe local automation runs into auditable evidence reports.

## Problema

Las automatizaciones tradicionales ejecutan acciones, pero muchas veces no dejan evidencia entendible para revision posterior.

RPA y agentes pueden ser fragiles ante UI externa, cambios de sitios, cookies, login, geoloc o challenges.

Los equipos necesitan trazabilidad, campos faltantes explicitos y una salida que una persona pueda revisar antes de decidir.

## Solucion

ONE BRAIN organiza el flujo en capas:

- Runtime seguro read-only cuando el caso lo requiere.
- Evidence normalization para convertir lecturas en datos auditables.
- Artifacts locales para conservar evidencia.
- Summaries para comparar varios productos.
- Human-readable reports en Markdown.
- Quality gates y decision readiness para separar evidencia suficiente, parcial o diagnostica.

## Demo story

1. Usar samples versionados bajo `samples/product-evidence/`.
2. Correr el one-command script `tools/scripts/run-demo-product-evidence.ps1`.
3. Leer `LATEST_DEMO_MARKDOWN`.
4. Abrir el Markdown generado.
5. Mostrar Summary y Products.
6. Explicar `partial` vs `ready_for_comparison`.
7. Explicar que `missing_price` no se inventa y queda marcado para verificacion.

## Differentiators

- Safety-first.
- Explicit missing fields.
- Local auditable artifacts.
- Reproducible demo.
- Human-review friendly.
- No claim de compra automatica ni autonomia peligrosa.

## Safe claims

- read-only evidence
- auditable local report
- explicit confidence/readiness
- explicit missing fields
- human-review friendly
- reproducible local demo

## Anti-claims

No usar estas frases como promesas:

- compra automaticamente
- evita todos los bloqueos
- extrae todos los precios
- 100% autonomo
- bypass
- garantiza precio

## Roadmap proximo

- HTML/PDF export.
- Richer app profiles.
- Approval UX.
- Live evidence stabilization.
- Local/private mode.
