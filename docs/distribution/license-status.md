# NODAL OS — License status

Status: owner decision and appropriate legal review required before external distribution.

The repository does not currently declare source or product distribution terms. In the absence of explicit terms, no public package, customer installer, marketplace listing or commercial release is authorized by this repository.

The private-beta MSIX pipeline validates technical packaging, installation, launch, update metadata and uninstall behavior. Its artifacts are engineering evidence for controlled internal evaluation, not permission for external distribution or for use of third-party code outside its applicable terms.

## Product recommendation for owner review

This is a product recommendation, not legal advice or an adopted license.

The current business model is best aligned with:

1. proprietary or all-rights-reserved source/product terms;
2. separate evaluation-only terms for private-beta participants;
3. an end-user license for the signed desktop package;
4. exact third-party notices generated from the final release-candidate payload.

This path matches the current paid desktop-product strategy, BYOK positioning and optional cloud model without granting broad redistribution rights by accident. The owner may instead choose source-available, open-source or split licensing, but that choice must be explicit and consistently reflected in the repository, installer and release process.

## Validated MSIX dependency snapshot

Engineering evidence source:

- Desktop Package workflow run: `29616288590`;
- PR head used by the validated candidate: `f7ba903a0463c191d14c31a68fa02bb68b30e5e5`;
- Actions test-merge commit embedded in package metadata: `83563af37877d669283c296125d7cd12aaa844ac`;
- package version: `0.1.0.3`;
- MSIX SHA-256: `14d6ca8d326cee78669a6f9304a8d697b4a891bcd6f62f8c3a4d3aac11010a96`;
- private-beta bundle SHA-256: `914b9f25c6024df53fc41c2134f32e9beb61d76b1506b92ede21e465d8a92621`;
- update manifest SHA-256: `15e4ec2ba981e6c8f6be2aa213e3cd8b75fcd8b5b3f22ea0ba3f312fcfc7753a`;
- GitHub artifact digest: `f77710a9f3fddea67aaf889cd72ae15bf155f60a93f0d0c787864db7af82a9a6`;
- dependency source inside the package: `OneBrain.Pilot.deps.json`;
- dependency audit workflow run: `29616288545`, with no vulnerable or deprecated package reported.

The validated self-contained package declares these external NuGet dependencies:

| Package | Version | Relationship | License signal verified from package publisher |
| --- | ---: | --- | --- |
| `FlaUI.Core` | `5.0.0` | direct runtime dependency | MIT |
| `FlaUI.UIA3` | `5.0.0` | direct runtime dependency | MIT |
| `Interop.UIAutomationClient` | `10.19041.0` | transitive through FlaUI.UIA3 | MIT |
| `System.Management` | `8.0.0` | transitive through FlaUI.Core | MIT |

The package embeds supported self-contained Windows runtime packs:

- `runtimepack.Microsoft.NETCore.App.Runtime.win-x64/10.0.10`;
- `runtimepack.Microsoft.AspNetCore.App.Runtime.win-x64/10.0.10`;
- `runtimepack.Microsoft.WindowsDesktop.App.Runtime.win-x64/10.0.10`.

This table is an inventory, not a complete notice file. Windows self-contained .NET distributions have authoritative license and third-party-notice material tied to the exact distributed build. Those files must be obtained from the final supported runtime distribution and reviewed with the final package payload.

The validated `0.1.0.3` MSIX and private-beta bundle do not contain a root license or third-party-notice file. That is acceptable only for the present controlled engineering artifact and remains a release blocker.

## Decisions still required

The owner must explicitly select and record:

- repository source terms: proprietary, source-available, open-source or split;
- desktop product/end-user terms;
- private-beta evaluation terms and who may receive the package;
- permitted redistribution, copying and reverse engineering, subject to applicable law;
- warranty, liability, support and governing-law language for intended markets;
- whether the public repository remains public under the selected source terms;
- the exact third-party notice set produced from the final supported runtime and dependency graph.

## Implementation after the decision

Do not add placeholder legal text that appears authoritative. Once the owner-approved direction exists:

1. add the chosen root `LICENSE` or proprietary source-terms file;
2. add private-beta evaluation terms and the desktop EULA in the approved release location;
3. generate `THIRD_PARTY_NOTICES` from the exact final release-candidate package, including runtime-pack notices;
4. include the approved terms and notices in the private-beta/release bundle and installer documentation;
5. reconcile README, privacy, security, website and changelog wording;
6. make the release checklist fail closed when terms, notices, production signing or release authorization are absent;
7. repeat the clean-install/package validation against the final signed candidate.

## Release boundary

External design-partner distribution remains blocked until either:

- approved written private-beta evaluation terms cover the participants and package; or
- the complete owner-approved product/source licensing path is implemented.

Internal or operator-controlled evaluation does not close this blocker and does not grant production, customer-data or commercial authority.

CI and documentation must continue to report public release as `NO-GO` until the approved terms and exact notice set exist in the repository or in a controlled release process.
