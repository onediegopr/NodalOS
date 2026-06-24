# M804 Unsupported Capability Guard

M804 adds an explicit guard for unknown, malformed, future, typo, mixed-case, forbidden-prefix, and release-like capabilities that are not registered.

Unsupported capabilities return `DENY_UNSUPPORTED_CAPABILITY`, `selectedExecutor=null`, `reasonCode=denied_unsupported_capability`, and preserve all no-execution invariants.
