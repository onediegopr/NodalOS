# Recipe Operator Preview System-Specific Summaries

Block: `NODAL_RECIPE_RUNTIME_PRODUCT_SURFACE_003_OPERATOR_PREVIEW_FLOW_HANDOFF_EXPORT_SURFACE`

Product-surface phase: 3/4.

## Excel / Microsoft 365

Previewable: fixture-safe spreadsheet workflow review, evidence expectations, validation requirements, and workitem draft metadata.

Blocked: real Microsoft 365 connector calls, live sync, live file mutation, secret access, and external system mutation.

Operator review: confirm inputs, evidence refs, validation expectations, and safe handoff summary.

## Google Workspace

Previewable: Google Workspace draft and review-queue summaries.

Blocked: Google API calls, Gmail send, Drive mutation, Calendar mutation, OAuth, and connector execution.

Operator review: confirm attachment or event metadata by reference and approve only future-gated manual or fixture-safe paths.

## SAP

Previewable: report, lookup, and draft workflow explanations.

Blocked: SAP GUI automation, RFC/BAPI/OData calls, credential use, connector execution, and live mutation.

Operator review: confirm future connector-first assumptions, approval requirements, and evidence expectations.

## Mercado Libre / Mercado Pago

Previewable: order import previews, reconciliation summaries, claims/dispute review queues, and draft listing review.

Blocked: API calls, payment execution, stock update, price update, listing publication, marketplace message send, and claim mutation.

Operator review: human approval is required for payment, marketplace, price, stock, listing, message, and dispute-related actions.

## ARCA / Fiscal

Previewable: CUIT validation preview, invoice draft review, comprobante reference review, and monthly batch review metadata.

Blocked: ARCA/AFIP web service calls, fiscal submission, certificate/private-key usage, payment credential usage, and legal submission.

Operator review: fiscal/legal review is required and live submission remains blocked.

## ERP Local LATAM

Previewable: local ERP draft and reconciliation summaries for system-family metadata such as Tango, Bejerman, Contabilium, Alegra, Siigo, Odoo, TOTVS, CONTPAQi, and Aspel.

Blocked: ERP API calls, desktop automation, live mutation, connector execution, credential use, invoice creation, stock update, price update, and accounting export mutation.

Operator review: validate system family, evidence refs, approval path, and manual-only next steps.

## Generic Browser Portals

Previewable: portal readiness, form draft review, table extraction preview, download evidence preview, and session-expired playbook summaries.

Blocked: browser automation, CDP, Playwright, Selenium, Puppeteer, real login, autofill, CAPTCHA/2FA/challenge bypass, and live portal mutation.

Operator review: confirm live browser runtime remains blocked and any challenge/auth state goes to human review.

## Computer Use Legacy

Previewable: legacy app playbooks, manual checkpoint workflows, popup recovery notes, file drop intake preview, and hotkey lookup playbook summaries.

Blocked: desktop automation, computer-use automation, UIA/vision execution, OS hooks, hotkey listeners, file watchers, recorder/playback, and live repair activation.

Operator review: confirm manual-only handling, evidence refs, and blocked live runtime state.
