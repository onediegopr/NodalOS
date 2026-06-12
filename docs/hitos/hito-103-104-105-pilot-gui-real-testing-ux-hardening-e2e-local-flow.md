# HITO-103+104+105 — Pilot GUI Real Testing + UX Hardening + End-to-End Local Flow

## Objetivo

Dejar de validar ONE BRAIN solo por CLI/tests y empezar a probarlo como producto visual local usable.

Este bloque no agrega infraestructura grande. Endurece el shell Pilot existente para que Diego pueda levantarlo, abrirlo en navegador local, recorrer secciones clave y ejecutar un flujo demo seguro desde la UI.

## Alcance aplicado

### HITO-103 — Pilot GUI real testing

- `tools/scripts/run-onebrain-pilot.ps1` sigue levantando `src/OneBrain.Pilot` con SDK portable.
- El script imprime `ONE_BRAIN_PILOT_URL`.
- El script imprime rutas manuales de prueba:
  - `/`
  - `/recipes`
  - `/variables`
  - `/memory`
  - `/app-profiles`
  - `/approvals/demo`
  - `/runs`
  - `/ai/config`
  - `/ai/audit`
- El script no abre navegador automáticamente.

### HITO-104 — UX hardening

- Home incluye navegación clara a las áreas principales.
- Las páginas internas muestran navegación común.
- Safety summary queda siempre visible:
  - 0 clicks
  - 0 cookies accepted
  - 0 login
  - 0 carrito
  - 0 compra
  - 0 pago
- Las rutas de Markdown, HTML y artifacts se muestran en campos seleccionables para copiar manualmente.
- Pilot no abre archivos automáticamente.
- Los detalles inexistentes de memory/app profiles muestran un estado `not found` accionable en vez de una tabla vacía ambigua.

### HITO-105 — End-to-end local flow

Flujo esperado desde UI local:

1. Levantar Pilot:

```powershell
powershell -ExecutionPolicy Bypass -File tools/scripts/run-onebrain-pilot.ps1
```

2. Abrir manualmente `ONE_BRAIN_PILOT_URL` en navegador local.
3. Navegar Home, recipes, variables, memory, app profiles, approvals, runs, AI config y AI audit.
4. Ejecutar desde Home una acción demo allowlisted:
   - `Comparar productos demo / run`
   - `Generar reporte Markdown / run`
   - `Generar reporte HTML / run`
5. Ver resultado, exit code, rutas Markdown/HTML/artifacts y safety summary.

## Reglas de seguridad

- No OpenAI real.
- No playback peligroso.
- No ejecución libre desde prompts.
- No recipes inventadas.
- No comandos arbitrarios.
- No browser/web externo desde el flujo demo.
- No clicks comerciales o externos.
- No login.
- No cookies accepted.
- No carrito.
- No compra.
- No pago.
- No envío real.
- No auto-open de HTML/Markdown/artifacts.

Las interacciones locales necesarias para probar la UI de Pilot no representan acciones externas ni comerciales.

## Archivos modificados

- `src/OneBrain.Pilot/PilotHomePageRenderer.cs`
- `tools/scripts/run-onebrain-pilot.ps1`
- `tests/OneBrain.Recipes.Tests/PilotGuiUxHardeningTests.cs`
- `docs/hitos/hito-103-104-105-pilot-gui-real-testing-ux-hardening-e2e-local-flow.md`

## Validación esperada

- Build OK.
- Tests OK.
- `NEGATIVE_EXIT_CODE=1`.
- Demo script OK.
- Demo Markdown dry-run/run OK.
- Demo HTML dry-run/run OK.
- ML readonly/diagnostic OK si se ejecuta.
- `artifacts/` ignorado/no trackeado.
- Firefox flaky no es regresión obligatoria.

## Registro de UX corregido

- Navegación dispersa entre pantallas → se agrega navegación común.
- Safety visible solo en algunas pantallas → se agrega bloque safety persistente.
- Rutas de salida poco copiables → se agregan campos readonly seleccionables.
- Memory/app profile detail inexistente ambiguo → se agrega estado not found claro.
- Script mostraba solo URL base → ahora imprime rutas de prueba principales.

## Porcentaje actualizado

- Core técnico: 91–93%
- Safety: 91–93%
- Cleanup: 89–91%
- Validación/CI confiable: 90–92%
- Web/Pilot local usable: 72–76%
- Retail public read-only: 82–84%
- Evidence/reporting: 87–89%
- Demo/reproducibilidad: 91–93%
- MVP comercial serio: 71–74%
- Producto completo/pro: 49–52%
