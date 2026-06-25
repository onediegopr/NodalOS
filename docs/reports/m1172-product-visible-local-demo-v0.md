# M1172 — Product visible local demo v0

## Decisión

PRODUCT_VISIBLE_LOCAL_DEMO_V0_READY_WITH_EXTERNAL_SMOKE_CAVEAT

La línea cambia a producto visible. Este bloque agrega una demo local real en el sidepanel de NODAL OS: Mission Control, misión demo, botón de run no-op, timeline visual, logs/evidence y reporte técnico copiable.

## Qué se ve ahora

- Pantalla Mission Control dentro de la pestaña Operar.
- Misión demo: Local Operator Demo.
- Botón principal: Run safe demo.
- Timeline visible con eventos started, no-op command accepted, evidence generated y completed.
- Panel de logs/evidence con run id, command kind, resultado, timestamp y evidence ref.
- Reporte técnico redacted para copiar.
- Cards de estado para host local, bridge, browser claim y BrowserRuntimeSmoke caveat como badge informativo.

## Cómo abrirlo

1. Cargar o recargar la extensión `browser-extension/onebrain-chrome-lab` en Chrome.
2. Abrir el sidepanel de NODAL OS.
3. Entrar en la pestaña Operar.
4. La pantalla Mission Control aparece arriba de la superficie operativa existente.

## Cómo probarlo

1. Verificar que aparece Local Operator Demo.
2. Presionar Run safe demo.
3. Confirmar que el progreso cambia a 100%.
4. Confirmar que el timeline muestra el flujo visible del run.
5. Revisar el panel Logs / evidence.
6. Presionar Copiar resumen para copiar el reporte técnico.

## Qué está simulado/no-op

- El run demo es no-op local/in-memory.
- No abre shell.
- No escribe filesystem.
- No llama provider/cloud.
- No ejecuta browser automation productiva.
- No desbloquea runtime real ni PC Commander real.
- No resuelve el caveat BrowserRuntimeSmoke.

## Qué falta para demo real

- Persistir historial de runs demo.
- Crear misiones desde UI.
- Mostrar runs anteriores.
- Enlazar evidence con una fuente local real autorizada.
- Preparar una demo grabable con estado inicial controlado.

## Próximos pasos de producto

M1173-M1184 — Product Demo v1: Mission Creation + Persistent Demo History.

Objetivo:

- Crear misión desde UI.
- Guardar historial local simple.
- Ver runs anteriores.
- Mejorar timeline/evidence.
- Preparar demo grabable.
