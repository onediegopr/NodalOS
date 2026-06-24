# M792 Simulated Runtime Result Object Projection

M792 updates the simulated runtime result so it exposes object projections, not only booleans.

`SimulatedRuntimeResult` now carries `RuntimeType`, `FixtureType`, `EvidenceEnvelope`, `LedgerEvents`, `RedactionProof`, and `NoExecutionProof`. Existing flags remain for backward-compatible safety tests.

Terminology is aligned: `runtimeType=SIMULATED_FAKE_ONLY_IN_MEMORY` describes the execution environment, while `fixtureType=SIMULATED_FAKE_ONLY` describes input fixtures.

Status: READY.
