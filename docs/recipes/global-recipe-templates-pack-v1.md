# Global Recipe Templates Pack v1

Phase: 8/9 - Global + LATAM Recipe Templates Pack v1.

This pack is original NODAL OS contract data. OpenRPA/OpenCore catalog ideas are inspiration only. No dependency, code copy or XAML import exists.

## Excel / Microsoft 365

- `excel.extract_rows_to_workitems`
- `excel.reconcile_two_files`
- `excel.generate_report_with_validation`
- `excel.normalize_supplier_price_list`
- `excel.bank_statement_reconciliation_preview`

Excel templates are fixture-safe and reference-only. Price, stock, payment and personal data handling remain human/approval gated.

## Google Workspace

- `google.sheets_extract_rows_to_workitems`
- `google.drive_file_intake_preview`
- `google.gmail_attachment_to_review_queue`
- `google.calendar_event_to_workitem_draft`

Google templates use refs and review queues only. They do not send email, read Gmail, call Google APIs, access Drive or create calendar events.

## SAP

- `sap.export_report_and_verify`
- `sap.purchase_order_status_check`
- `sap.vendor_invoice_validation_draft`
- `sap.material_master_lookup_preview`
- `sap.sales_order_from_excel_draft`

SAP templates are future-gated and connector-first by policy. They do not use SAP GUI automation, RFC, BAPI, OData, credentials or real connector calls. Draft or mutation-like templates require human/approval review.

## Generic Browser Portals

- `browser.portal_login_readiness_check`
- `browser.form_fill_draft_review`
- `browser.table_extract_preview`
- `browser.download_file_evidence_preview`
- `browser.session_expired_detector_preview`

Browser portal templates are `LiveBlocked`. They do not use browser automation, CDP, Playwright, Selenium, Puppeteer, extension/native messaging, real login, CAPTCHA bypass or 2FA bypass.

## Computer Use Legacy

- `desktop.legacy_app_export_report_preview`
- `desktop.popup_recovery_playbook`
- `desktop.file_drop_intake_preview`
- `desktop.manual_checkpoint_workflow`
- `desktop.hotkey_lookup_playbook`

Computer Use legacy templates are `LiveBlocked`. They do not use UIA/Vision execution, desktop automation, OS hooks, hotkeys, file watchers or live screen capture.
