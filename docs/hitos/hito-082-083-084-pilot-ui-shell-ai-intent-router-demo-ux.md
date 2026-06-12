# HITO-082+083+084 - Pilot UI Shell + AI Intent Router + Demo UX

## Alcance

Este bloque abre la primera experiencia local usable de ONE BRAIN Pilot. No implementa un agente autonomo libre ni un planner IA real todavia. El router de intencion es v0 rules-based y solo puede seleccionar recipes de una allowlist fija.

## Entregables

### HITO-082 - Pilot UI Shell

- Proyecto local `src/OneBrain.Pilot`.
- Home local con input de tarea y acciones rapidas.
- Botones:
  - Comparar productos demo.
  - Generar reporte Markdown.
  - Generar reporte HTML.
  - Ver safety guarantees.

### HITO-083 - Intent Router v0

- Intent router v0 con mapeo determinista.
- Router rules-based con allowlist dura.
- No usa LLM real todavia.
- No usa IA local.
- No usa Ollama.
- No usa LM Studio.
- Estrategia futura IA: OpenAI only.
- Futuro routing entre 3 o 4 modelos OpenAI segun costo, complejidad y riesgo.
- La IA nunca puede inventar recipes ni ejecutar acciones fuera de allowlist.

### HITO-084 - Demo UX / Execution Result Viewer

- Plan seguro antes de ejecutar.
- Execution result viewer con:
  - recipe seleccionada.
  - status y exit code.
  - ultima ruta Markdown si existe.
  - ultima ruta HTML si existe.
  - carpeta `artifacts`.
  - safety summary.

## Allowlist inicial

- `tools/recipes/demo-product-evidence-report.json`
- `tools/recipes/demo-product-evidence-html-report.json`
- `tools/recipes/product-evidence-html-report.json`
- `tools/recipes/product-evidence-markdown-report.json`

El prompt del usuario no puede inventar recipes ni ejecutar comandos arbitrarios.

## Router v0

El router actual reconoce intenciones simples:

- `mostrame la demo` -> demo Markdown.
- `genera html` -> HTML report allowlisted.
- `quiero reporte markdown` -> Markdown report allowlisted.
- prompts desconocidos -> `rejected/no_match`.
- requests comerciales peligrosos -> `rejected/safety_rejected`.

## Safety

El shell no abre navegador automaticamente, no abre HTML automaticamente y no usa web/red por si mismo. La ejecucion queda limitada a `OneBrain.Cli recipe run` sobre rutas allowlisted.

Confirmaciones de seguridad del bloque:

- 0 clicks.
- 0 cookies accepted.
- 0 login.
- 0 carrito.
- 0 compra.
- 0 pago.
- No safe.click.
- No invoke/type/submit.
- No comandos arbitrarios desde prompt.
- No Firefox flaky.

## Como correr Pilot local

Opcion recomendada:

```powershell
powershell -ExecutionPolicy Bypass -File tools/scripts/run-onebrain-pilot.ps1
```

El script valida el repo, usa el SDK portable por defecto, imprime `ONE_BRAIN_PILOT_URL` y no abre navegador automaticamente.

Opcion directa:

```powershell
$root = "C:\Users\diego\OneDrive\PERSONAL\ONE Brain\Codigo"
$dotnet = "C:\Users\diego\OneDrive\PERSONAL\ONE Brain\Herramientas\dotnet-sdk-11.0.100-preview.5.26302.115-win-x64\dotnet.exe"

Set-Location $root
& $dotnet run --project src/OneBrain.Pilot -- --root $root --dotnet $dotnet
```

Luego abrir manualmente:

```text
http://127.0.0.1:5084
```

No se abre el browser automaticamente.

## Validacion

Validacion obligatoria del bloque:

- `dotnet build OneBrain.slnx`
- `dotnet test`
- `exit-code-negative.json` con `NEGATIVE_EXIT_CODE=1`
- demo script
- demo Markdown dry-run/run
- HTML report dry-run/run
- demo HTML dry-run/run

## Limites

- No hay LLM real conectado todavia.
- No hay WebView2 desktop wrapper todavia.
- No hay ejecucion de comandos libres.
- No hay recipes dinamicas generadas por IA.
- No hay compra, checkout, login, cookies ni pagos.

## Porcentaje actualizado estimado

- Core tecnico: 91-93%
- Safety: 91-93%
- Cleanup: 88-90%
- Validacion/CI confiable: 90-92%
- Web read-only: 87-89%
- Retail public read-only: 82-84%
- Evidence/reporting: 86-89%
- Demo/reproducibilidad: 91-93%
- Pilot UX local: 18-24%
- Intent routing IA-safe: 20-26%
- MVP comercial serio: 70-73%
- Producto completo/pro: 48-51%
