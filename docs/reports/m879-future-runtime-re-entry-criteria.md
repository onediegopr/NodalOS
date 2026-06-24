# M879 - Future Runtime Re-Entry Criteria

Project: NODAL OS.

Simulated-only continuation is allowed under freeze change control. Planning-only work can be conditional if it stays docs/tests only.

Any productive runtime request requires a new safety gate, explicit approval and separate audit. Provider/cloud requires a provider audit and secret/redaction gate. Filesystem write requires path jail, rollback and transition audit. Browser automation requires a browser safety gate. Capability unlock requires explicit manual owner gate. Release/store remains NO-GO without separate owner acceptance and release audit. Product/Bridge/CSP requires a separate product gate.
