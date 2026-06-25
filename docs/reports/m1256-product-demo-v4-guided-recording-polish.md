# Product Demo v4 - Guided recording polish

## 1. Decision

PRODUCT_DEMO_V4_GUIDED_RECORDING_POLISH_READY

## 2. Que se hizo

- Se agrego onboarding liviano en Mission Control.
- Se agrego stepper no bloqueante:
  - Paso 1: Crear mision.
  - Paso 2: Run demo.
  - Paso 3: Copiar resumen.
- Se agrego estado visual `Lista para grabar` cuando hay mision, run, timeline y resumen.
- Se agrego preferencia local para minimizar/mostrar la guia.
- Se pulio el guion de demo para una narracion de 60-90 segundos.
- Se mejoraron empty states de misiones e historial.
- Se mantuvo el flujo local/no-op sin runtime real ni provider/cloud.

## 3. Que se ve en producto

- Card `Guia rapida` con cuatro acciones cortas para primer uso.
- Stepper con progreso real segun la demo.
- Indicador de demo lista para grabar.
- Guion demo mas cercano a pitch de producto.
- Checklist de grabacion compacto.

## 4. Como probarlo

1. Abrir Mission Control.
2. Revisar la card `Guia rapida`.
3. Crear una mision.
4. Ejecutar `Run demo`.
5. Confirmar que el stepper marca progreso.
6. Agregar una nota al run.
7. Abrir Historial.
8. Copiar resumen.
9. Minimizar la guia y recargar para verificar que la preferencia local se mantiene.

## 5. Que se valido visualmente

- QA visual local por Chrome/CDP.
- Consola limpia en perfil Chrome nuevo.
- Flujo create/edit/run/note/history/copy por CDP.

## 6. Que quedo pendiente

- Validar sidepanel real instalado desde `chrome://extensions`.
- Grabar una demo real de 60-90 segundos y ajustar microcopy final con feedback.

## 7. Validaciones

- `dotnet build .\OneBrain.slnx --no-restore`
- `node --check browser-extension/onebrain-chrome-lab/sidepanel.js`
- `dotnet test .\tests\OneBrain.Safety.Tests\OneBrain.Safety.Tests.csproj --no-build --filter "TestCategory=M1161M1172"`
- `git diff --check`
- Secret scan simple.
- Bad UX wording scan en Mission Control principal.

## 8. Archivos modificados

- `browser-extension/onebrain-chrome-lab/sidepanel.html`
- `browser-extension/onebrain-chrome-lab/sidepanel.css`
- `browser-extension/onebrain-chrome-lab/sidepanel.js`
- `tests/OneBrain.Safety.Tests/NodalOsProductVisibleLocalDemoM1161M1172Tests.cs`
- `docs/reports/m1256-product-demo-v4-guided-recording-polish.md`

## 9. Proximo hito

M1257-M1268 - Product Demo v5: installed sidepanel verification + demo recording dry run.
