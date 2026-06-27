# LATAM Recipe Templates Pack v1

Phase: 8/9 - Global + LATAM Recipe Templates Pack v1.

This pack is fixture-safe, contract-only and preview/draft oriented. It contains no real connectors, APIs, credentials, browser automation, desktop automation, network calls or vault access.

## Mercado Libre / Mercado Pago

- `meli.import_orders_preview`
- `meli.sync_stock_from_erp_draft`
- `meli.sync_prices_from_excel_draft`
- `meli.reconcile_orders_with_mercadopago_preview`
- `meli.claims_disputes_review_queue`
- `meli.publish_listing_draft_review`

Stock, price, listing, message, payment and claim flows require human/approval paths and remain `FutureGated` or `LiveBlocked`. Mercado Pago reconciliation is preview-only and does not execute payment actions.

## ARCA Argentina / Fiscal

- `arca.validate_cuit_preview`
- `arca.prepare_invoice_draft_review`
- `arca.constatar_comprobante_preview`
- `arca.monthly_invoice_batch_review`
- `arca.fiscal_submission_human_review`

ARCA/fiscal templates do not call ARCA/AFIP services, use fiscal certificates, use private keys or submit fiscal/legal data. Fiscal/legal actions require human review and remain non-live.

## ERP Local LATAM

- `erp.import_marketplace_orders_preview`
- `erp.sync_stock_to_marketplaces_draft`
- `erp.create_invoice_from_order_draft`
- `erp.price_list_update_review`
- `erp.cash_register_close_review`
- `erp.accounting_export_preview`

ERP metadata may reference Tango, Bejerman, Contabilium, Alegra, Siigo, Odoo, TOTVS, CONTPAQi and Aspel as system families only. No ERP connector, desktop automation or API integration is implemented. Mutation-like templates require human/approval paths.

## Safety

All LATAM templates expose safe next actions and not-included summaries. They remain fixture-safe, evidence-aware, approval-gated and live-blocked where risk requires it.
