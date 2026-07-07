---
id: TASK-025
title: 'Source generator: emit diagnostics for silently-dropped constructs'
status: To Do
assignee: []
created_date: '2026-07-07 08:11'
labels:
  - source-generator
  - diagnostics
milestone: m-1
dependencies:
  - TASK-010
priority: medium
ordinal: 15000
---

## Description

<!-- SECTION:DESCRIPTION:BEGIN -->
From the 2026-07-07 architecture review. The source generator currently emits only Name/PropertyType/Get/Set per property (src/Huml.Net.SourceGeneration/HumlSerializationGenerator.cs:255-266) and produces no Roslyn diagnostics, so constructs it cannot honour — required members, property-level [HumlConverter], [HumlNamingPolicy], Order — are silently ignored at compile time and the generated path quietly behaves differently from the reflection path. Until the metadata gap itself is closed (see the binding-metadata unification task), consumers deserve a compile-time signal: emit warning-level diagnostics (with IDs, e.g. HUML001+) when a registered type uses a construct the generated metadata does not carry. Related: TASK-008, TASK-010, TASK-011.
<!-- SECTION:DESCRIPTION:END -->

## Acceptance Criteria
<!-- AC:BEGIN -->
- [ ] #1 A documented diagnostic ID range exists for the generator
- [ ] #2 Registering a type whose members use constructs the generated metadata cannot represent produces a warning diagnostic naming the member and construct
- [ ] #3 No diagnostics are produced for fully-supported types
- [ ] #4 Generator compilation tests assert each diagnostic fires and links to its documentation
- [ ] #5 docs/source-generator.md lists the diagnostics and their remediations
<!-- AC:END -->

## Definition of Done
<!-- DOD:BEGIN -->
- [ ] #1 dotnet build succeeds with zero warnings (TreatWarningsAsErrors is on; full rebuild to surface cached analyzer results)
- [ ] #2 dotnet test green on net8.0 / net9.0 / net10.0
- [ ] #3 CHANGELOG.md [Unreleased] section updated for every user-visible change
- [ ] #4 Any public API change is additive and justified in writing per docs/internals/api-freeze.md
- [ ] #5 New/changed public members have XML docs; tests use AwesomeAssertions (never FluentAssertions)
- [ ] #6 New error-or-no-error parse behaviours assessed against .claude/rules/fixture-gaps.md and staged in fixtures/extensions/ when language-agnostic
<!-- DOD:END -->
