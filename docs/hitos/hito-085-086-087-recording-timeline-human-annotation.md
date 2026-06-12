# HITO-085+086+087 - Recording/Playback v0 + Recipe Timeline + Human Annotation

## Alcance

Este bloque inicia el modo observar y aprender. ONE BRAIN puede representar una sesion de recording/shadow, convertir observaciones en timeline candidata y permitir anotacion humana minima.

No implementa playback real todavia. No automatiza acciones reales. No genera una recipe ejecutable final cuando no hay confianza suficiente.

## HITO-085 - Recording / Shadow Mode v0

Se agregaron clases core para recording seguro:

- `RecordingSession`
- `RecordingEvent`
- `RecordingObservationInput`
- `RecordingSessionBuilder`
- `SensitiveTextSanitizer`
- `RecordingDemoFixture`

Eventos soportados:

- `window_changed`
- `focus_changed`
- `text_field_detected`
- `button_detected`
- `manual_action_observed`
- `unknown_observation`

El builder consume snapshots ya disponibles y produce eventos observados. No hace clicks, no escribe, no envia, no cierra apps y no ejecuta acciones.

## Sanitizacion

La capa v0 sanitiza texto sensible con placeholder `[REDACTED]` cuando detecta patrones o terminos de riesgo como:

- password/contrasena
- secret/token/api key
- tarjeta/card/cvv
- emails
- numeros largos compatibles con tarjetas u otros identificadores sensibles

Si hay duda, el dato debe quedar redactado.

## HITO-086 - Recipe Timeline

Se agregaron:

- `RecipeTimeline`
- `TimelineStep`
- `RecipeTimelineBuilder`

La timeline humana incluye:

- step number
- time offset
- event type
- window/app
- element summary
- confidence
- suggested action label
- risk level
- requires approval
- `CanGenerateExecutableRecipe = false`

La salida es una `CandidateTimeline`, no una `CandidateRecipe` final.

## HITO-087 - Human Annotation

Se agregaron:

- `HumanAnnotation`
- `HumanAnnotationBuilder`

Tipos minimos de anotacion:

- este bloque es buscar cliente -> `search_customer`
- este bloque es preparar mensaje -> `prepare_message`
- este paso requiere aprobacion -> `requires_approval`
- este dato debe ser variable -> `variable`
- este paso se puede ignorar -> `ignore`
- este paso es sensible -> `sensitive`
- nota libre -> `free_note`

## Artifact writer

Se agrego `RecordingArtifactWriter` con salida local bajo:

- `artifacts/recordings/`
- `artifacts/recipe-timelines/`

Estos artifacts son runtime, quedan ignorados por Git y no deben commitearse.

## Integracion Pilot

Pilot agrega una seccion `Observe and learn` y la ruta:

```text
/recording/demo
```

La demo de recording es fixture/mock local. Muestra timeline y formulario de anotacion humana minima. No captura eventos reales todavia y no ejecuta playback.

## Safety

Confirmaciones:

- No clicks.
- No type/write.
- No submit.
- No cerrar apps.
- No aceptar cookies.
- No login.
- No carrito.
- No compra.
- No pago.
- No playback activo.
- No recipe ejecutable final generada.
- No Firefox flaky como regresion obligatoria.

## Validacion

Validacion obligatoria:

- `dotnet build OneBrain.slnx`
- `dotnet test`
- `exit-code-negative.json` con `NEGATIVE_EXIT_CODE=1`
- demo script
- demo Markdown dry-run/run
- demo HTML dry-run/run
- ML readonly/diagnostic si no tarda demasiado

## Porcentaje actualizado estimado

- Core tecnico: 92-94%
- Safety: 92-94%
- Cleanup: 88-90%
- Validacion/CI confiable: 91-93%
- Web read-only: 87-89%
- Retail public read-only: 82-84%
- Evidence/reporting: 86-89%
- Demo/reproducibilidad: 91-93%
- Pilot UX local: 24-30%
- Intent routing IA-safe: 22-28%
- Recording/shadow learning: 15-22%
- MVP comercial serio: 72-75%
- Producto completo/pro: 50-53%
