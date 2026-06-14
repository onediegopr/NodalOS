# NEXA Browser-001 - Web Target Resolution + Verified Actions

## Objetivo

Este hito endurece `NEXA Chrome Operator Lab` para que la accion web no dependa de selectores inventados por el LLM.

El flujo pasa a estar centrado en:

1. `observePage`
2. `resolveTarget`
3. accion por `elementId`
4. verificacion post-accion
5. observacion siguiente

No toca Windows/UIA.

## Cambios implementados

### 1. Element catalog fuerte

`content_script.js` ahora construye un catalogo de elementos interactivos visibles con:

* `elementId`
* `tagName`
* `role`
* `type`
* `visibleText`
* `accessibleName`
* `ariaLabel`
* `title`
* `placeholder`
* `name`
* `id`
* `href`
* `value` redactado si es sensible
* `inputKind`
* `isVisible`
* `isEnabled`
* `isPassword`
* `isCredentialLike`
* `formContext`
* `nearbyText`
* `bounds`
* `viewportPosition`
* `stableSelectors`
* `riskFlags`
* `scoreHints`

Se detectan:

* `button`
* `a[href]`
* `input`
* `textarea`
* `select`
* `option`
* roles interactivos
* `tabindex`
* `onclick`
* `contenteditable`
* `submit/reset/button`
* `label[for]`
* candidatos visuales con `cursor:pointer`

### 2. Stable locator generation

Cada elemento genera selectores rankeados:

1. `id` unico
2. `data-testid`
3. `data-test`
4. `data-cy`
5. `data-automation-id`
6. `aria-label`
7. `name + tag`
8. `role + accessibleName` via XPath
9. `href`
10. `form + input`
11. CSS path
12. XPath
13. `nth-child` fallback

Cada selector incluye:

* `selector`
* `type`
* `confidence`
* `reason`

### 3. Nueva tool `resolveTarget`

`resolveTarget` ahora rankea candidatos por:

* texto visible
* `accessibleName`
* `aria-label`
* `placeholder`
* `title`
* `href`
* contexto cercano
* kind/intencion
* visibilidad
* enabled
* viewport
* confianza del mejor selector
* penalizacion por ambiguedad
* penalizacion por riesgo credential-like

Devuelve:

* `bestCandidate`
* `candidates`
* `elementId`
* `bestSelector`
* `reason`
* `score`

### 4. Acciones por `elementId`

Se agregaron tools nuevas:

* `getElementCatalog`
* `clickElement`
* `setElementValue`
* `focusElement`
* `readElement`
* `highlightElement`
* `scrollElementIntoView`

Las tools legacy por selector se mantienen como fallback, pero el prompt y el bridge ahora priorizan `elementId`.

### 5. Verificacion post-accion

`clickElement` y `setElementValue` devuelven evidencia estructurada:

* `beforeUrl`
* `afterUrl`
* `beforeTitle`
* `afterTitle`
* `urlChanged`
* `titleChanged`
* `domChanged`
* `expectedConditionMet`
* `verificationStatus`
* `reason`

Si no se observa efecto despues de un click, la verificacion puede devolver:

* `uncertain`
* razon equivalente a `click posiblemente no efectivo`

### 6. Integracion del bridge

El bridge ahora:

* allowlistea las tools nuevas
* instruye al modelo a usar `resolveTarget` antes de actuar
* normaliza decisiones legacy:
  * `click` -> `clickElement`
  * `read` -> `readElement`
  * `setValue` -> `setElementValue`
  * `highlight` -> `highlightElement`
  * `scrollIntoView` -> `scrollElementIntoView`
* inyecta `elementId` y `stableSelectors` desde `bestCandidate`
* pausa si `resolveTarget` no devuelve candidato util
* pausa si la confianza es demasiado baja
* sigue observando despues de `resume`

### 7. Side panel

El panel ahora muestra:

* `current tool`
* `last result`
* `selected elementId`
* mejor selector
* score del target
* lista de candidatos
* resultado de verificacion
* resumen del catalogo observado

## Guardas mantenidas

* STOP global
* pausa humana
* no credenciales automaticas
* no lectura de password values
* no key en extension
* no `eval`
* no scripts remotos
* no `chrome://`, `edge://`, `extension://`
* no `javascript:` / `data:` URLs
* no OCR
* no Playwright/CDP
* no desktop automation

## Smoke manual recomendado

### Caso 1 - Pagina simple

Instruccion:

```text
Toca iniciar sesion.
```

Esperado:

* `observePage` devuelve catalogo
* `resolveTarget` encuentra candidato claro
* `clickElement` usa `elementId`
* `verificationStatus` devuelve `passed`, `uncertain` o `failed` con razon

### Caso 2 - Botones similares

Fixture con:

* `Ingresar`
* `Iniciar sesion`
* `Registrarse`

Esperado:

* candidatos rankeados
* score visible
* razon visible
* mejor selector visible

### Caso 3 - Formulario

Esperado:

* inputs detectados
* `setElementValue` funciona en campo no sensible
* `password` no se lee
* campos credential-like quedan marcados como riesgo

### Caso 4 - AFIP/ARCA smoke generico

Instruccion:

```text
Entra a AFIP/ARCA y toca iniciar sesion. Cuando pida credenciales, pausa y avisame.
```

Esperado:

* navega
* resuelve el target de inicio
* hace click por `elementId`
* pausa cuando aparece credencial/captcha/2FA
* despues de `resume`, observa de nuevo antes de continuar

Importante:

No hay hardcode por sitio. AFIP/ARCA es solo smoke real.

## Limitaciones abiertas

* la resolucion sigue siendo heuristica; no existe todavia identidad web v2
* no hay suite JS dedicada para fixtures DOM complejos
* el panel muestra trazabilidad suficiente para depurar, pero no es aun una consola de inspeccion avanzada

## Proximo hito recomendado

`NEXA Browser-002`

Enfocado en:

* fixtures HTML automatizados para scoring/resolution
* verificacion mas rica por mutacion/landmark
* policy de confidence thresholds
* paginacion/filtrado avanzado de catalogo
