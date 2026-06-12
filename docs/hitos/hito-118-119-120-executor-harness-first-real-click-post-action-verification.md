# HITO-118+119+120 - Executor Harness + First Real Supervised Click + Post-Action Verification

## Alcance

Este hito introduce un harness local y controlado para el primer click real benigno de ONE BRAIN.

No habilita clicks generales, playback libre, acciones comerciales ni interaccion con sitios externos.

## HITO-118 - Executor Harness Tests

- Se agrega `OneBrain.Core.ExecutorHarness`.
- El harness modela un target local, benigno y controlado.
- Los tests usan un executor fake para no hacer clicks reales en CI.
- El servicio falla cerrado si falta aprobacion, target seguro o executor seguro.

## HITO-119 - First Real Supervised Click Benigno

El unico camino de click real queda aislado en Pilot:

- Ruta: `/executor-harness`
- Target: `Objetivo benigno del harness`
- Ventana: `ONE BRAIN Pilot`
- Accion: `benign_harness_click`
- Requiere aprobacion humana.
- Requiere `ExecutionAllowed=true`.
- Usa UIA via `UiaActionExecutor` solo para el harness local.

Esto no toca MercadoLibre, sitios comerciales, login, cookies, carrito, compra, pago ni envio.

## HITO-120 - Post-Action Verification + Evidence

Despues del intento de click, ONE BRAIN registra:

- `ApprovalRequest`
- `ApprovalDecision`
- `RunHistoryRecord`
- evidencia local bajo `artifacts/executor-harness/`
- verificacion posterior:
  - target encontrado
  - click observado
  - senales del executor
  - lectura UIA posterior independiente de la ventana local
  - confirmacion de que el target benigno sigue visible despues del click

Si la verificacion no confirma exactamente un click benigno, el resultado queda failed/blocked.

El servicio no acepta targets arbitrarios aunque un caller marque `ControlledSurface=true`. El guard requiere el `HarnessId`, `AppProfileId`, titulo de ventana y target conocidos del harness local de Pilot.

## Safety

- No OpenAI real.
- No API keys reales.
- No MercadoLibre.
- No playback libre.
- No clicks fuera del harness local.
- No cookies accepted.
- No login.
- No carrito.
- No compra.
- No pago.
- No auto-open de HTML/artifacts.

## Como probar manualmente

1. Levantar Pilot:

```powershell
powershell -ExecutionPolicy Bypass -File tools/scripts/run-onebrain-pilot.ps1
```

2. Abrir manualmente:

```txt
http://127.0.0.1:5084/executor-harness
```

3. Revisar el target benigno.
4. Presionar `Aprobar y ejecutar click benigno supervisado`.
5. Verificar resultado, approval, run history y evidencia.

## Limitaciones

- No es un executor general.
- No consume RuntimeId todavia desde action layer.
- No habilita clicks sobre apps externas.
- No reemplaza el playback supervisado existente.
- Si la ventana local de Pilot no esta visible o el target no se puede resolver, falla cerrado.
