# ADR: Browser Recorder Read-Only Prototype M30

## Status

Accepted for M30.

## Decision

The recorder prototype is read-only. It records navigation/DOM observations, semantic targets, preconditions, verification candidates, risk classification, and evidence refs. It does not record secrets, cookies, bodies, sensitive headers, full local paths, or session storage values.

Recipe drafts are sanitized, non-executable by default, versioned, and local/sandbox oriented.

## Out of Scope

Product recorder, executable replay, credential capture, submit capture as safe, external critical sites, AFIP, banks, ERP, payments, or sensitive accounts.

