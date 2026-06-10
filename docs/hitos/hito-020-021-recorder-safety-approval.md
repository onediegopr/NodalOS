# HITO-020+021 — Recorder + Safety Approval Pack

## 1. Objetivo
Permitir que ONE BRAIN genere recetas JSON iniciales de forma controlada y agregar mecanismos de dry-run / approval para que recetas generadas o acciones sensibles no se ejecuten sin control.

## 2. Recorder v1

### Comandos implementados

| Comando | Descripción |
|---|---|
| `record new --out PATH --name NAME [--description DESC]` | Crea receta vacía |
| `record add-note --out PATH --message TEXT` | Agrega step note |
| `record add-app-open --out PATH --app APP [--path PATH]` | Agrega step app.open |
| `record add-type --out PATH --process PROC [--window WIN] --text TEXT` | Agrega step actv.type |
| `record add-key --out PATH --process PROC [--window WIN] --keys COMB` | Agrega step key |
| `record validate --out PATH` | Valida campos requeridos |
| `record inspect --out PATH` | Muestra estructura |

### Uso
```powershell
record new --out artifacts/recipes/demo.json --name "demo"
record add-note --out artifacts/recipes/demo.json --message "Start"
record add-app-open --out artifacts/recipes/demo.json --app notepad
record validate --out artifacts/recipes/demo.json
recipe run artifacts/recipes/demo.json
```

## 3. Dry-run

```powershell
recipe run PATH --dry-run
recipe dry-run PATH
```

- No abre apps, no toca teclado, no cierra nada, no lanza browser
- Clasifica pasos: false = seguro (note, wait, delay, assert), true = físico (app.open, actv.type, key, browser.open, browser.close)
- Requiere aprobación para pasos sensibles
- Muestra warnings por step

## 4. Approval modes

| Modo | CLI | Comportamiento |
|---|---|---|
| allow | `--approve allow` o sin flag | Ejecuta todos los steps (backward compat) |
| deny | `--approve deny` | Bloquea steps sensibles: "requires approval" |

Steps sensibles: actv.invoke, actv.type, key, app.open, browser.open, browser.close

## 5. Risk levels

- `none`: note, wait, delay, assert, snapshot, visual (no acción física)
- `physical`: app.open, browser.open, browser.close, actv.invoke, actv.type, key (acción física real)

## 6. Limitaciones

- Recorder manual-asistido, no captura automática de interacciones
- No hay UI gráfica
- Approval es todo-o-nada por receta (no granular por step)
- Dry-run clasifica por kind, no analiza parámetros

## 7. Validación

- Build: 0 errors, 0 warnings
- Tests: 16/16 PASS (sin nuevos tests - deuda documentada)
- recorded-notepad-sample: 2/3 (type fallback foreground blocked - safety feature)
- dry-run-safety-sample: 8 steps, 5 sensitive, 0 actions
- approval-deny-negative: note runs, app.open blocked
- approval-required-sample: blocked with --approve deny
- Regresiones: calculator PASS, browser-session PASS
- Safety: invoke/click/press Close bloqueados
