# ONE BRAIN Demo Pack

Demo pack versionado para presentar la demo estable de evidencia de producto.

## Contenido

- `demo/product-evidence-demo-pack.md`: indice operativo del paquete.
- `demo/VERSION.md`: snapshot corto de version del demo pack.
- `docs/demo/`: README tecnico y walkthrough comercial/tecnico.
- `samples/product-evidence/`: evidencia de producto versionada para demo.
- `samples/product-evidence-html/`: snapshot HTML versionado para referencia visual estable.
- `tools/scripts/run-demo-product-evidence.ps1`: script one-command canonico.

## Comando unico

```powershell
powershell -ExecutionPolicy Bypass -File tools/scripts/run-demo-product-evidence.ps1
```

El script imprime `LATEST_DEMO_MARKDOWN` con la ruta del ultimo Markdown generado.

HTML local opcional:

```powershell
& $dotnet run --project src/OneBrain.Cli -- recipe run tools/recipes/demo-product-evidence-html-report.json
```

El HTML runtime se genera bajo `artifacts/` y no se versiona. No se abre navegador automaticamente.

## Outputs runtime

Los outputs se escriben bajo `artifacts/`, que esta ignorado por Git:

- `artifacts/product-evidence-demo-summary/`
- `artifacts/product-evidence-demo-reports/`
- `artifacts/product-evidence-demo-html-reports/`

No se commitean artifacts runtime.

## Snapshot HTML versionado

El fixture `samples/product-evidence-html/demo-product-evidence-report.html` es una referencia estatica y sanitizada del reporte demo. Usa `GENERATED_AT_UTC`, no contiene rutas locales absolutas y no contiene paths runtime bajo `artifacts/`.

## Safety

Esta demo local no usa browser, no usa web, no hace clicks, no login, no cookies, no carrito, no compra y no pago. El HTML no se abre automaticamente.

## Limitaciones

La demo usa samples versionados. No es extraccion live, no representa todos los casos reales y no reemplaza revision humana final.
