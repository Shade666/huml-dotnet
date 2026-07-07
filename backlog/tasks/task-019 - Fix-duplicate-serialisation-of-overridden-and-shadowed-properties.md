---
id: TASK-019
title: Fix duplicate serialisation of overridden and shadowed properties
status: Done
assignee:
  - Claude
created_date: '2026-07-07 08:10'
updated_date: '2026-07-07 18:51'
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
- [x] #1 A virtual/override property serialises exactly once, with the derived-most declaration winning
- [x] #2 A new-shadowed property serialises exactly once with the derived value, matching source-generator behaviour
- [x] #3 Round-trip Serialize then Deserialize succeeds for a hierarchy with overridden properties
- [x] #4 A parity test asserts the reflection path and the source-generated path produce identical output for the same hierarchy
- [x] #5 Regression tests cover override, new-shadowing, and multi-level inheritance chains
<!-- AC:END -->

## Implementation Plan

<!-- SECTION:PLAN:BEGIN -->
## Implementation Plan (TDD)

**Root cause confirmed:** `PropertyDescriptor.BuildDescriptors` (src/Huml.Net/Serialization/PropertyDescriptor.cs:119-225) walks the type chain base-first with `DeclaredOnly` and no name de-duplication, so overridden/shadowed properties produce two descriptors and `HumlSerializerImpl.SerializeMappingBody` emits both keys — invalid HUML that the parser rejects on round-trip (Duplicate key).

**Source-generator baseline:** `HumlSerializationGenerator.TransformContext` walks base-first with a `seen` name-set where FIRST (base) declaration wins position AND declaring type (`((Base)o).Prop` accessor). For virtual overrides this reads the derived value via virtual dispatch; for `new`-shadowing it would read the BASE value — to be verified empirically by the new tests. AC #2 requires the derived value on both paths, so the generator's seen-set may need upgrading to replace-in-place (derived-most declaration wins, base position kept) to preserve parity.

**Steps:**
1. New test file `tests/Huml.Net.Tests/Serialization/InheritanceDedupTests.cs` (AwesomeAssertions) — failing first:
   - virtual/override property serialises exactly once, derived value wins (AC #1)
   - new-shadowed property serialises exactly once, derived value (AC #2)
   - round-trip Serialize→Deserialize for an override hierarchy (AC #3)
   - multi-level chain (3 levels, override at each level + shadowing) (AC #5)
   - parity test: reflection output == source-generated output for the same hierarchy, via a new `[HumlSerializable]` context + SG fixture types (AC #4)
2. Run: confirm the new tests fail on current main (and observe the generator's actual shadowing behaviour).
3. Fix `BuildDescriptors`: name-keyed dedup with replace-in-place — derived-most declaration wins metadata/PropertyInfo, base-most position kept (mirrors generator ordering). Update the XML doc remarks.
4. If step 2 showed the generator returns the base value for shadowed properties, apply the same replace-in-place dedup in `TransformContext` so both paths agree (part of the same H1 fix — the paths must produce identical output).
5. Full suite on net8.0/net9.0/net10.0, zero warnings; CHANGELOG `[Unreleased]` Fixed entry; commit `fix: ...` + push.

**Scope notes:** No public API change (PropertyDescriptor and generator models are internal). No new parse behaviours → nothing for fixtures/extensions per .claude/rules/fixture-gaps.md.
<!-- SECTION:PLAN:END -->

## Implementation Notes

<!-- SECTION:NOTES:BEGIN -->
TDD red phase confirmed the review's H1 repro AND surfaced a second latent defect: the source generator's seen-set kept the BASE declaration (base-first walk, first-wins), and its emitted accessor casts to the declaring type — so a new-shadowed property read the base slot's value on the source-gen path (test Source_generated_path_emits_shadowed_property_with_derived_value failed with 'Legs: 4' instead of 'Legs: 3'). Both paths therefore received the same replace-in-place dedup: derived-most declaration wins metadata/accessor, base-most position kept for stable ordering.

Verification: 13 new tests in InheritanceDedupTests (10 failed pre-fix, all pass post-fix); full suite 1342 tests green on net8.0/net9.0/net10.0; clean rebuild 0 warnings. No public API change (PropertyDescriptor and generator PropertyModel are internal). No new error-or-no-error parse behaviours — the round-trip tests require .NET serialisation, so nothing qualifies for fixtures/extensions per .claude/rules/fixture-gaps.md.
<!-- SECTION:NOTES:END -->

## Final Summary

<!-- SECTION:FINAL_SUMMARY:BEGIN -->
## Fix duplicate serialisation of overridden and shadowed properties (H1)

**Problem:** `PropertyDescriptor.BuildDescriptors` walked the inheritance chain base-first with `DeclaredOnly` and no name de-duplication, so `virtual`/`override` and `new`-shadowed properties produced two descriptors and `HumlSerializerImpl` emitted the key twice — invalid HUML rejected by the library's own parser on round-trip ("Duplicate key").

**Fix (both binding paths):**
- `src/Huml.Net/Serialization/PropertyDescriptor.cs` — name-keyed replace-in-place dedup in `BuildDescriptors`: derived-most declaration wins (its `PropertyInfo`, attributes and metadata), positioned at the base-most declaration's slot so ordering stays base-first. XML doc remarks updated.
- `src/Huml.Net.SourceGeneration/HumlSerializationGenerator.cs` — the generator's seen-name set kept the *base* declaration, whose `((Base)o).Prop` accessor cast read the base slot for `new`-shadowed properties (wrong value, silently). Replaced with the same replace-in-place dedup so the generated accessor casts to the derived-most declaring type. Reflection and source-gen output are now byte-identical for the same hierarchy (pinned by a parity test).

**Tests:** `tests/Huml.Net.Tests/Serialization/InheritanceDedupTests.cs` (13 tests, AwesomeAssertions) covering override-once/derived-value/ordering, shadowing-once/derived-value, round-trips, 3-level override+shadow chains, and reflection-vs-source-gen parity, with SG fixture types `SGDedupAnimal`/`SGDedupDog`/`SGDedupContext`. All failed appropriately pre-fix; full suite 1342 green on net8.0/net9.0/net10.0, zero-warning rebuild.

**Risk/behaviour notes:** Output for affected hierarchies changes from invalid (duplicate keys) to valid — a bug fix, not a behaviour change for any previously-round-trippable type. Source-gen consumers with `new`-shadowed properties now get the derived value (previously base value); this aligns with the documented derived-most-wins contract and the reflection path. CHANGELOG `[Unreleased]` updated.
<!-- SECTION:FINAL_SUMMARY:END -->

## Definition of Done
<!-- DOD:BEGIN -->
- [x] #1 dotnet build succeeds with zero warnings (TreatWarningsAsErrors is on; full rebuild to surface cached analyzer results)
- [x] #2 dotnet test green on net8.0 / net9.0 / net10.0
- [x] #3 CHANGELOG.md [Unreleased] section updated for every user-visible change
- [x] #4 Any public API change is additive and justified in writing per docs/internals/api-freeze.md
- [x] #5 New/changed public members have XML docs; tests use AwesomeAssertions (never FluentAssertions)
- [x] #6 New error-or-no-error parse behaviours assessed against .claude/rules/fixture-gaps.md and staged in fixtures/extensions/ when language-agnostic
<!-- DOD:END -->
