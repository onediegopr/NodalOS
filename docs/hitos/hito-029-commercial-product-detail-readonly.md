# HITO-029 — Commercial Product Detail Read-Only

## 1. Objetivo
Abrir una página pública directa de producto comercial individual en Mercado Libre Argentina, leer title/text visible, cerrar browser y generar reporte. Sin clicks, sin login, sin carrito, sin compra, sin pago.

## 2. URL real
`https://articulo.mercadolibre.com.ar/MLA-1141892349-notebook-lenovo-ideapad-1-14-intel-celeron-n4020-4gb-64gb-emmc-128gb-_JM`

## 3. Receta
`mercadolibre-product-readonly.json`: load profile → open session → delay 6s → read title/text → close → assert → note.

## 4. Resultados
- Title: Notebook Lenovo Ideapad 1 14... | MercadoLibre 📦
- Text: nombre del producto, categorías, ofertas, etc.
- Palabras sensibles detectadas: "comprar" (read-only, sin acción)
- Precio "$" no visible vía UIA en esta página
- Sin CAPTCHA, sin login, sin carrito

## 5. Safety
- Sin clicks, sin type, sin invoke, sin formularios
- Solo lectura UIA title/text
- browser.close antes de asserts
- Sin relajar MinimalSafetyGuard ni ApprovalPolicy
