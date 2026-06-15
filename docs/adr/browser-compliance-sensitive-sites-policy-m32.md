# ADR: Browser Compliance Sensitive Sites Policy M32

## Status

Accepted for M32.

## Context

The Browser Runtime can now execute local/sandbox document workflows, safe download/upload, recorder read-only prototypes, and replay safe mode. It must not advance into AFIP, banking, ERP, fiscal, financial, legal, healthcare, payroll, production admin, or customer-data sites without a formal compliance policy.

## Decision

M32 defines sensitive site categories, risk levels, action kinds, approval requirements, audit requirements, and a fail-closed evaluator.

Sensitive categories include fiscal, banking, financial, ERP, payroll, healthcare, legal, government, identity, payments, production admin, and customer data.

Fiscal, banking, financial, payments, and identity are critical by default. ERP, payroll, healthcare, legal, government, production admin, and customer data are high risk unless a future policy raises them.

## Action Rules

Read-only view may be allowed only in simulation/pilot with approval. Document download requires approval and safe download. Document upload requires approval and safe upload.

Submit, pay, delete, publish, approve, sign, change credentials, and raw profile changes remain blocked or prohibited by default.

## Audit Requirements

Audit may include category, risk, action kind, decision, approval refs, gate refs, evidence refs, reason codes, and what remains blocked.

Audit must not include secrets, cookies, session values, request bodies, response bodies, sensitive header values, document contents, or full sensitive local paths.

## Gate

The runtime phase gate allows `SensitiveSitesPolicyDefined` only. It fails if real sensitive pilot is active before M33+, if submit/payment/signing/irreversible sensitive actions are enabled, or if productive recorder/replay/body capture/sensitive header value capture is active.

## Out of Scope

No AFIP, banks, ERP, fiscal/financial sites, real credentials, real client accounts, submits, payments, signatures, sensitive downloads/uploads, productive replay, or productive recorder are enabled in M32.

## Future Work

M33A may introduce sensitive read-only simulation only. Real pilots require a separate milestone, explicit allowlist, legal/compliance approval, human approvals, evidence requirements, and independent audit review.

