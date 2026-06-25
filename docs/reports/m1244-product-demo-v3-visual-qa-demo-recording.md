# Product Demo v3 - Visual QA + demo recording

## 1. Decision

PRODUCT_DEMO_V3_VISUAL_QA_DEMO_RECORDING_READY

## 2. Que se reviso visualmente

- QA visual real: sidepanel abierto en Chrome local mediante CDP.
- Se abrio `browser-extension/onebrain-chrome-lab/sidepanel.html` en Chrome local con viewport de sidepanel.
- Mission Control cargo sin errores de consola.
- Se reviso primera impresion, densidad, scroll, cards, timeline, historial, evidencia, guion demo y reporte copiable.
- Hallazgo principal: la demo funcionaba, pero la timeline principal heredaba metadatos internos (`risk`, autoridad interna, next action) que ensuciaban una grabacion.

## 3. Que se ajusto

- Timeline de Mission Control mas limpia: se ocultan metadatos internos solo en la timeline demo.
- Checklist visible renombrado a `Checklist para grabar`.
- Guion demo reescrito para narrar un video corto.
- Copy principal ajustado de `Browser claim`, `Demo scope`, `Logs / evidence` y `Report` a lenguaje de producto.
- Listas de misiones e historial con scroll acotado para evitar cards apelotonadas.
- Reporte copiable agrega `recording_flow` para documentar el flujo de demo.

## 4. Como probarlo

1. Abrir `browser-extension/onebrain-chrome-lab/sidepanel.html` o el sidepanel de la extension.
2. Confirmar que se ve Mission Control.
3. Crear una mision corta.
4. Editar el titulo si hace falta.
5. Ejecutar `Run demo`.
6. Agregar una nota al run.
7. Abrir `Historial` y seleccionar el run.
8. Usar `Copiar script` o `Copiar resumen`.

## 5. Que quedo pendiente

- Verificacion manual final dentro del sidepanel real de Chrome extension instalada.
- Mejorar responsive si se graba en una ventana mas angosta que 390 px.
- Proximo producto: preparar demo v4 grabable con pulido de onboarding y recorrido guiado liviano.

## 6. Validaciones

- `dotnet build .\OneBrain.slnx --no-restore`
- `node --check browser-extension/onebrain-chrome-lab/sidepanel.js`
- `dotnet test .\tests\OneBrain.Safety.Tests\OneBrain.Safety.Tests.csproj --no-build --filter "TestCategory=M1161M1172"`
- `git diff --check`
- Secret scan simple sobre archivos modificados.
- Bad UX wording scan simple en Mission Control principal.

## 7. Archivos modificados

- `browser-extension/onebrain-chrome-lab/sidepanel.html`
- `browser-extension/onebrain-chrome-lab/sidepanel.css`
- `browser-extension/onebrain-chrome-lab/sidepanel.js`
- `tests/OneBrain.Safety.Tests/NodalOsProductVisibleLocalDemoM1161M1172Tests.cs`
- `docs/reports/m1244-product-demo-v3-visual-qa-demo-recording.md`

## 8. Proximo paso

M1245-M1256 - Product Demo v4: guided recording polish + onboarding micro-flow.
