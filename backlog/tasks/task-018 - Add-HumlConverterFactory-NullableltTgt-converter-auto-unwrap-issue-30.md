---
id: TASK-018
title: 'Add HumlConverterFactory + Nullable&lt;T&gt; converter auto-unwrap (issue #30)'
status: Done
assignee: []
created_date: '2026-07-01 15:46'
updated_date: '2026-07-01 15:56'
labels:
  - serializer
  - api
  - converters
milestone: m-2
dependencies: []
references:
  - 'https://github.com/primeBeri/huml-dotnet/issues/30'
  - 'https://github.com/dotnet/runtime/issues/102006'
documentation:
  - 'C:\Users\Shady\.claude\plans\review-github-issue-no-playful-quail.md'
  - docs/custom-converters.md
  - docs/internals/api-freeze.md
  - docs/nuget-publish-checklist.md
modified_files:
  - src/Huml.Net/Serialization/HumlConverterFactory.cs
  - src/Huml.Net/Serialization/ConverterCache.cs
  - src/Huml.Net/Serialization/PropertyDescriptor.cs
  - tests/Huml.Net.Tests/Serialization/HumlConverterTests.cs
  - docs/custom-converters.md
  - docs/options-reference.md
  - docs/attributes-reference.md
  - docs/public-api.txt
  - CHANGELOG.md
priority: medium
ordinal: 8000
---

## Description

<!-- SECTION:DESCRIPTION:BEGIN -->
GitHub issue #30 (author primeBeri) reports a genuine System.Text.Json parity gap: `HumlConverter<T>.CanConvert` defaults to an exact type match (`typeToConvert == typeof(T)`), and `ConverterCache.TryGet` (src/Huml.Net/Serialization/ConverterCache.cs) resolves against the exact requested type with no `Nullable<T>` unwrapping. Consequently a `HumlConverter<TEnum>` registered globally does not apply to a `TEnum?` (Nullable<TEnum>) property — the nullable property silently falls through to native enum parsing and throws HumlDeserializeException on any value the converter was meant to intercept, with no compile-time or obvious runtime signal that the converter "didn't apply".

STJ has the identical limitation (dotnet/runtime#102006) and solves it two ways: a public `JsonConverterFactory` base class (one implementation, many concrete requested types, resolved via `CreateConverter`), and a built-in `NullableConverterFactory` that wraps the underlying-type converter so a plain `JsonConverter<TEnum>` transparently serves `TEnum?`.

Decision (confirmed with user, see full plan): implement BOTH parts —
1. New public `HumlConverterFactory : HumlConverter` abstract base mirroring `JsonConverterFactory`, with `CreateConverter(Type, HumlOptions)`.
2. Make `ConverterCache.TryGet` auto-unwrap `Nullable<U>`: any converter/factory registered for `U` automatically serves `U?` via an internal null-aware adapter, curing the reported symptom directly (not just giving factory authors better ergonomics).

This is an additive public API change under the active freeze (docs/internals/api-freeze.md) — requires a written justification referencing that doc, and docs/public-api.txt must be regenerated. Full design detail (file-by-file plan) is in the approved plan file at C:\Users\Shady\.claude\plans\review-github-issue-no-playful-quail.md — read it before starting implementation.

Key files: src/Huml.Net/Serialization/HumlConverter.cs, HumlConverterT.cs, ConverterCache.cs, Versioning/HumlOptions.cs, HumlDeserializer.cs (lines ~176, ~200), HumlSerializerImpl.cs (lines ~162, ~1012), tests/Huml.Net.Tests/Serialization/HumlConverterTests.cs, docs/custom-converters.md.

Release note: once merged, this ships as 0.2.0-beta.2 per docs/nuget-publish-checklist.md (MinVer tag-driven). The release/publish steps are a post-merge follow-on, not part of this task's Definition of Done — tag push will be confirmed with the user separately since it's an irreversible outward-facing action.
<!-- SECTION:DESCRIPTION:END -->

## Acceptance Criteria
<!-- AC:BEGIN -->
- [x] #1 New public abstract class HumlConverterFactory : HumlConverter exists with public abstract HumlConverter? CreateConverter(Type typeToConvert, HumlOptions options); its ReadObject/WriteObject are sealed overrides throwing NotSupportedException
- [x] #2 ConverterCache.TryGet detects a matched HumlConverterFactory at both the type-level [HumlConverter] attribute level and the HumlOptions.EffectiveConverters level, and returns CreateConverter(t, options); a factory returning null falls through to the next candidate instead of short-circuiting resolution
- [x] #3 ConverterCache.TryGet unwraps Nullable<U>: when the target type is U? and no direct converter matches U? itself, it resolves a converter for U and wraps it in a null-aware adapter so the plain converter serves U? (null maps to null, non-null delegates to the underlying converter)
- [x] #4 A globally-registered HumlConverter<TEnum> (via HumlOptions.Converters) applies to a TEnum? property on both serialise and deserialise -- regression test reproducing the exact scenario from issue #30
- [x] #5 Factory-produced converters and nullable adapters are memoised per target type -- a test proves CreateConverter is not invoked repeatedly for the same requested type
- [x] #6 docs/custom-converters.md documents HumlConverterFactory (with a worked example) and the automatic Nullable<T> converter behaviour, cross-linked from options-reference.md and attributes-reference.md as appropriate
- [x] #7 docs/public-api.txt baseline is regenerated to include the new additive public surface, with the diff justified per docs/internals/api-freeze.md
- [x] #8 CHANGELOG.md [Unreleased] section has an Added entry describing HumlConverterFactory and automatic Nullable<T> converter application, referencing issue #30
<!-- AC:END -->

## Implementation Plan

<!-- SECTION:PLAN:BEGIN -->
1. HumlConverterFactory (src/Huml.Net/Serialization/HumlConverterFactory.cs) — new public abstract class deriving HumlConverter, with abstract CanConvert (override) and CreateConverter(Type, HumlOptions); ReadObject/WriteObject sealed-overridden to throw NotSupportedException (a factory is never dispatched to directly).
2. ConverterCache.TryGet (src/Huml.Net/Serialization/ConverterCache.cs) — split into TryGet (cache lookup + Nullable<U> unwrap) and a new TryResolveDirect helper (type-attribute level then EffectiveConverters level). A HumlConverterFactory match calls CreateConverter; null result falls through to the next candidate rather than short-circuiting. When nothing matches the requested type t directly, Nullable.GetUnderlyingType(t) is checked — if t is Nullable<U>, TryGet(U, options) is resolved recursively and wrapped in a new private nested NullableConverterAdapter (null scalar -> null without invoking inner; non-null delegates straight through).
3. PropertyDescriptor.cs (line ~201) — added a guard: a HumlConverterFactory used via a property-level [HumlConverter] attribute now throws InvalidOperationException at descriptor-build time with an actionable message, instead of a confusing NotSupportedException surfacing later. Property-level converters are resolved directly (bypassing ConverterCache), so CreateConverter never had a HumlOptions context to run against there -- this was a real gap discovered while implementing, not in the original acceptance criteria, and is called out explicitly in docs.
4. Tests added to tests/Huml.Net.Tests/Serialization/HumlConverterTests.cs (13 new: factory-via-options, factory-decline-falls-through, factory-via-type-attribute + round-trip, memoisation-count, the issue #30 regression on both serialise/deserialise directions, null-scalar short-circuit both directions, and the new property-level-factory-throws guard).
5. Docs: docs/custom-converters.md gained "Nullable Types" and "Converter Factories" sections (including the property-level restriction); docs/options-reference.md and docs/attributes-reference.md cross-link and note the restriction; docs/public-api.txt regenerated (verified byte-for-byte against live reflection over the built net10.0 assembly, not just typed by hand); CHANGELOG.md [Unreleased] has two Added entries referencing issue #30 and dotnet/runtime#102006.
<!-- SECTION:PLAN:END -->

## Final Summary

<!-- SECTION:FINAL_SUMMARY:BEGIN -->
## Summary

Implements GitHub issue #30: adds `HumlConverterFactory` (mirroring `System.Text.Json.Serialization.JsonConverterFactory`) and, going further per the confirmed decision, makes `ConverterCache` auto-unwrap `Nullable<T>` so a plain `HumlConverter<T>` registered globally now serves `T?` transparently — curing the reported symptom directly, not just giving factory authors better ergonomics.

## Changes

- **New public API** (additive, justified under docs/internals/api-freeze.md): `HumlConverterFactory : HumlConverter` with `CreateConverter(Type, HumlOptions)`. `docs/public-api.txt` regenerated and cross-checked against live reflection over the built assembly.
- **ConverterCache.TryGet**: now resolves factories at both type-attribute and `HumlOptions.Converters` levels (a factory returning `null` falls through, it doesn't short-circuit), and unwraps `Nullable<U>` — wrapping a `U`-converter in a new internal `NullableConverterAdapter` so it serves `U?` too. Recursion is structurally bounded because `Nullable<Nullable<T>>` is not a valid CLR type.
- **PropertyDescriptor guard (scope addition beyond the original plan, judged necessary)**: discovered that property-level `[HumlConverter]` bypasses `ConverterCache` entirely (it's resolved and invoked directly, with no `HumlOptions` in scope for `CreateConverter`). Attaching a factory there would have silently thrown `NotSupportedException` deep inside serialise/deserialise. Added a fail-fast `InvalidOperationException` at descriptor-build time instead, with an actionable message, and documented the restriction everywhere the feature is described.
- **Tests**: 13 new tests in `HumlConverterTests.cs` — factory resolution via both registration paths, decline-and-fall-through, memoisation (CreateConverter called exactly once per type), the exact issue #30 regression (global `HumlConverter<TEnum>` now applies to `TEnum?` on both serialise and deserialise), null-scalar short-circuiting in both directions, and the property-level-factory guard.
- **Docs**: `docs/custom-converters.md` gained "Nullable Types" and "Converter Factories" sections; `docs/options-reference.md` and `docs/attributes-reference.md` cross-link and note the property-level restriction; `CHANGELOG.md [Unreleased]` has two `Added` entries referencing issue #30 and the analogous dotnet/runtime#102006.

## Tests run

- `dotnet build` (Debug and Release, full clean rebuild) — 0 warnings, 0 errors, all 4 TFMs (netstandard2.1/net8.0/net9.0/net10.0).
- `dotnet test` (Debug and Release) — 1,329/1,329 passed on net8.0/net9.0/net10.0, zero regressions.
- Manually verified `docs/public-api.txt`'s new entry against `Type.GetMembers` reflection over the built net10.0 DLL rather than trusting a hand-typed guess.

## Fixture-gap assessment

Per `.claude/rules/fixture-gaps.md`: all new tests assert .NET-specific converter/registration behaviour (property values, `HumlOptions` configuration, exception messages) — none are language-agnostic parse error/no-error assertions, so no `fixtures/extensions/` additions apply.

## Follow-up (not part of this task)

Per the approved plan, shipping this as `0.2.0-beta.2` via `docs/nuget-publish-checklist.md` is a separate post-merge step — moving `[Unreleased]` into a dated version, packing, and pushing the `v0.2.0-beta.2` tag. That tag push is outward-facing/irreversible and will be confirmed with the user separately before executing.
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
