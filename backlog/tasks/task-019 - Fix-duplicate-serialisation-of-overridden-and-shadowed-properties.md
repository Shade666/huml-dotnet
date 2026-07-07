---
id: TASK-019
title: Fix duplicate serialisation of overridden and shadowed properties
status: To Do
assignee: []
created_date: '2026-07-07 08:10'
labels:
  - serializer
  - bug
milestone: m-0
dependencies: []
references:
  - src/Huml.Net/Serialization/PropertyDescriptor.cs
  - src/Huml.Net.SourceGeneration/HumlSerializationGenerator.cs
priority: high
ordinal: 9000
---

## Description

<!-- SECTION:DESCRIPTION:BEGIN -->
Verified bug (2026-07-07 review, finding H1): the reflection binding path serialises virtual/override and new-shadowed properties twice, producing invalid HUML that the library's own parser rejects on round-trip.

Mechanism: PropertyDescriptor.BuildDescriptors (src/Huml.Net/Serialization/PropertyDescriptor.cs:119-225) walks the inheritance chain base-first and collects GetProperties(...DeclaredOnly) per type with no de-duplication by property name. An override is re-declared in the derived type's metadata, so both the base and derived declarations land in the ordered descriptor array, and HumlSerializerImpl.SerializeMappingBody emits both.

Verified repro: Serialize(new Dog()) where Dog : Animal overrides Name emits "Name: \"Rex\"" twice; Deserialize<Dog> of that output throws HumlParseException "Duplicate key 'Name'". A new-shadowed int Id emits both values.

The source generator already handles this correctly with a seen-name set where derived-most wins (HumlSerializationGenerator.cs:76) — mirror that in BuildDescriptors so the reflection and generated paths agree. Any type hierarchy using virtual/override cannot round-trip today, so this should land before 0.2.0 stable.
<!-- SECTION:DESCRIPTION:END -->

## Acceptance Criteria
<!-- AC:BEGIN -->
- [ ] #1 A virtual/override property serialises exactly once, with the derived-most declaration winning
- [ ] #2 A new-shadowed property serialises exactly once with the derived value, matching source-generator behaviour
- [ ] #3 Round-trip Serialize then Deserialize succeeds for a hierarchy with overridden properties
- [ ] #4 A parity test asserts the reflection path and the source-generated path produce identical output for the same hierarchy
- [ ] #5 Regression tests cover override, new-shadowing, and multi-level inheritance chains
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
