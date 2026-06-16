# NODAL OS Private Preview Operator Runbook

## Purpose

This runbook is for controlled internal local private preview only.

NODAL OS Core remains the only authority. UI, Admin, and Companion may show status, carry operator intent, and display evidence, but they cannot approve operations outside Core gates.

## What Can Be Tested

- Local Product/Admin shell.
- Private local API in-process.
- Readiness dashboard.
- Operator blocker explanations.
- Local diagnostics and support metadata.
- Issue triage.
- Evidence/log review.
- M51 and M65 status display.

## What Cannot Be Tested

- Public SaaS.
- Public API.
- Real billing or real email.
- Real customer credentials.
- Sensitive sites, AFIP, banks, ERP, fiscal, financial, or government sites.
- Submit/pay/sign/delete.
- Login, checkout, payment, or destructive flows.
- Productive recorder/replay.
- External CDP general-ready claims.

## Current Evidence

- M51: closed for HTTP read-only target-owned proof only.
- M65: closed for target-owned Chrome/CDP/DOM read-only proof only.
- M65 ledger ref: `audit-ledger-edb3e2fbb0a0446788dae17a269c0058`.
- M65 ledger hash: `61f52af1eebf08d59a24e5fbb72e70acf0038e7a329bff6599a0ac00c757f03e`.

## How To Start Internal Local Preview

1. Confirm the canonical worktree is `Codigo-m12-audit`.
2. Confirm build and full tests are green.
3. Confirm readiness shows `ReadyWithRestrictions`.
4. Confirm all dangerous surfaces remain blocked.
5. Start local Product/Admin preview only.
6. Record issues through local triage.

## If Credentials/Login/Submit/Payment/Delete Appears

Stop the flow.

Use the operator blocker explanation and record an issue. Do not enter credentials, do not submit, do not pay, do not sign, and do not delete.

## How To Read Readiness

`ReadyWithRestrictions` means internal local preview only.

It does not mean SaaS public, external CDP general-ready, real credentials, real billing/email, sensitive sites, or irreversible actions are allowed.

## Evidence/Log Handling

Use redacted evidence refs only. Do not copy cookies, tokens, secrets, full DOM, full body, payment card data, document contents, or sensitive local paths.

## Issue Reporting

Every issue should include:

- current state;
- allowed action attempted;
- blocker explanation if any;
- evidence refs;
- local logs summary;
- whether Core granted or denied the operation.
