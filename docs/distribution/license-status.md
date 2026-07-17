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

- Desktop Package workflow run: `29615053896`;
- PR head used by the validated candidate: `861798ca0284138454b9726151167f224b63ccc7`;
- Actions test-merge commit embedded in package metadata: `98b0feac0e851c3af340a2b56d4ad0c698bfbb0d`;
- package version: `0.1.0.3`;
- MSIX SHA-256: `acd9620ac60cb01453cee420b55ee1792cc037eb3328f1dd505d59a0741dfcab`;
- private-beta bundle SHA-256: `d61f99bf475f1760411c2dfde0cc4c0e72dd8f5d6608a62d90a65b417d63ca0e`;
- dependency source inside the package: `OneBrain.Pilot.deps.json`;
- dependency audit workflow run: `29615053925`, with no vulnerable or deprecated package reported.

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
