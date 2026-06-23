# M683 Public Variant Manual QA Retry

Milestone: M683

Decision: PUBLIC_VARIANT_MANUAL_QA_AFTER_TOKEN_FIX_CONDITIONAL_ENVIRONMENT

M680-M682 fixed the bridge token connection loop and unblocked manual QA, but this milestone did not receive new human Chrome evidence and the agent still cannot safely drive `chrome://extensions`.

Retry classification:

- Public variant loaded: unknown.
- Token required state: ready by M680-M682 fix evidence.
- Token saved/present state: ready by M680-M682 fix evidence.
- Full token exposure: false.
- Bridge unreachable behavior: ready by M680-M682 fix evidence.
- Invalid token behavior: ready by M680-M682 fix evidence.
- Reconnect loop regression: hardened by M680-M682 fix evidence.
- Connected valid-token live QA: conditional environment missing.

No public release is allowed. No package freeze is allowed without live Chrome QA evidence.
