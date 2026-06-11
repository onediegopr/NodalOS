# Product Evidence Demo Walkthrough

Guion para mostrar la demo estable de ONE BRAIN a un perfil comercial, inversor o usuario tecnico.

## Pitch de 30 segundos

ONE BRAIN no solo ejecuta automatizaciones: produce evidencia local auditable. Esta demo toma samples versionados, genera un summary con scoring de completitud y exporta un Markdown claro para revision humana. No navega web, no hace clicks y no inventa campos faltantes.

## Pitch de 2 minutos

ONE BRAIN separa tres capas: runtime, evidence y reporting. El runtime puede leer paginas o apps bajo reglas de safety; la capa de evidence normaliza lo capturado; y la capa de reporting resume la calidad de esa evidencia con score, grade y decision readiness.

En esta demo usamos samples versionados para que la presentacion sea repetible. El reporte muestra tres productos: dos parciales, donde falta precio visible confirmado, y un fixture demo completo. La clave es la honestidad del sistema: si falta precio, aparece como `missing_price`; si hay una senal cruda no confirmada, no se convierte en precio normalizado.

## Guion paso a paso

1. Abrir PowerShell en la raiz del repo.
2. Ejecutar:

```powershell
powershell -ExecutionPolicy Bypass -File tools/scripts/run-demo-product-evidence.ps1
```

3. Leer la linea:

```text
LATEST_DEMO_MARKDOWN=<ruta>
```

4. Abrir manualmente el Markdown generado desde esa ruta.
5. Mostrar la seccion `Summary`.
6. Explicar:
   - `Source artifacts`: samples locales leidos.
   - `Products with price`: items con precio visible normalizado.
   - `Products needing price verification`: items identificados sin precio visible confirmado.
   - `Ready for comparison`: items con evidencia suficiente para comparacion demo.
7. Mostrar la tabla `Products`.
8. Explicar columnas:
   - `Score`: puntaje de completitud.
   - `Grade`: lectura humana del score.
   - `Readiness`: decision practica siguiente.
   - `Missing fields`: campos faltantes explicitos.
9. Mostrar por que el precio faltante no se inventa:
   - Suministros y Sodimac quedan con `Price` como `—`.
   - Sodimac puede tener `rawSignals`, pero eso no se normaliza como precio visible.
10. Mostrar el `Demo Fixture Product` como comparacion completa controlada.

## Mensaje comercial

- ONE BRAIN no solo automatiza acciones: produce evidencia auditable.
- Runtime, evidence, report y quality gates son capas separadas.
- El reporte es local, reproducible y facil de revisar.
- Los campos faltantes quedan explicitos, no ocultos.
- La salida es human-review friendly.

## Riesgos y limites

- Sitios externos pueden bloquear, pedir login, cookies, geoloc o challenge.
- Corridas live pueden ser `partial` o `diagnostic`.
- La demo estable no representa todos los casos reales.
- La demo no prueba compras ni pagos.
- La demo no intenta evadir bloqueos.
- La comparacion no reemplaza verificacion humana final.

## Frases prohibidas

No usar estas frases como claims:

- "compra automaticamente"
- "evita todos los bloqueos"
- "extrae todos los precios"
- "100% autonomo sin supervision"

## Frases seguras

- "read-only evidence"
- "auditable local report"
- "explicit missing fields"
- "decision readiness"
- "human-review friendly"

## Cierre recomendado

La demo muestra una base seria para evidencia read-only y reporting auditable. El siguiente paso comercial no es prometer autonomia total, sino conectar mas fuentes controladas, mejorar cobertura de campos y mantener quality gates estrictos.
