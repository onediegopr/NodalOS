# NEXA Vercel Test-Owned Target Runbook

## Purpose

Prepare a controlled, read-only, synthetic external target for future M51/M65 proof. This runbook does not execute proof live and does not close M51/M65.

## Project

- Source folder: `apps/nexa-test-owned-target`
- Deployment provider: Vercel
- Plan: Hobby only, lab/no commercial use
- Recommended domain: `nexa-lab.nodalos.com.ar`
- Required verification paths: `/health`, `/ownership`

## Vercel Setup

1. Create or import a Vercel project from `apps/nexa-test-owned-target`.
2. Keep the project static/read-only.
3. Do not add authentication, analytics, cookies, server functions, or mutating APIs.
4. Use Vercel Hobby only for this lab target.
5. Add `nexa-lab.nodalos.com.ar` in Project Settings > Domains.

## DNS Modes

If delegating the full domain to Vercel DNS:

- `ns1.vercel-dns.com`
- `ns2.vercel-dns.com`

If keeping existing DNS and using only the subdomain:

- `CNAME nexa-lab -> cname.vercel-dns.com`

Do not assume the root domain `nodalos.com.ar` is allowed when the selected target is the `nexa-lab` subdomain.

## Verification

1. Wait for DNS propagation.
2. Confirm HTTPS is ready for `https://nexa-lab.nodalos.com.ar`.
3. Open `https://nexa-lab.nodalos.com.ar/health` and confirm `NEXA_EXTERNAL_READONLY_TARGET_OK`.
4. Open `https://nexa-lab.nodalos.com.ar/ownership` and confirm `ownership: test-owned`, `risk: low`, and `mode: read-only`.
5. Record operator approval ref before any opt-in live proof.

## Safety Limits

- No real users.
- No real credentials.
- No real personal data.
- No real payments.
- No submit/pay/sign/delete.
- No NEXA SaaS public API.
- No billing/email real provider.
- No external proof unless opt-in is explicitly enabled.

## What This Enables

This creates readiness for a future opt-in read-only proof. M51/M65 remain deferred until a passed evidence pack exists and is accepted by a separate closure decision.
