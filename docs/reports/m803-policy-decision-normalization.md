# M803 Policy Decision Normalization

M803 normalizes simulated runtime policy decisions into five canonical test-only outcomes: `ALLOW_SIMULATED_DRY_RUN`, `DENY_DENYLISTED_CAPABILITY`, `DENY_UNSUPPORTED_CAPABILITY`, `DENY_POLICY_VIOLATION`, and `REQUIRE_MANUAL_APPROVAL_SIMULATED`.

Only allowed simulated capabilities may select fake executors. Deny and manual approval decisions select no executor and still emit evidence envelope, ledger events, redaction proof, and no-execution proof.
