---
id: TASK-010
title: 'Source generator: support nested and generic context classes (M13)'
status: To Do
assignee: []
created_date: '2026-06-12 23:29'
labels:
  - source-generator
milestone: m-1
dependencies: []
documentation:
  - docs/source-generator.md
  - docs/internals/g3-security-review.md
priority: medium
ordinal: 6000
---

## Description

<!-- SECTION:DESCRIPTION:BEGIN -->
Documented limitation carried out of the G3.2b source-generator hardening (finding M13): a HumlGeneratedContext subclass that is a nested class or a generic class is not supported — the generator's emitted partials assume a top-level, non-generic context. STJ's JsonSerializerContext supports nesting. Extend HumlSerializationGenerator (src/Huml.Net.SourceGeneration/HumlSerializationGenerator.cs) to emit correctly-nested partial declarations for contexts declared inside other types (walking the containing-type chain), and either support or cleanly diagnose generic contexts (a Roslyn diagnostic with a clear message beats emitted code that fails to compile). Use the existing CSharpGeneratorDriver compilation-test harness (tests/Huml.Net.Tests/SourceGen/GeneratorTestHarness.cs) to prove emitted code compiles.
<!-- SECTION:DESCRIPTION:END -->

## Acceptance Criteria
<!-- AC:BEGIN -->
- [ ] #1 A context class nested one or more levels inside other classes generates compiling code and works end-to-end
- [ ] #2 A generic context class either works or produces a clear Roslyn diagnostic (no CS compile errors in consumer code)
- [ ] #3 Containing types that are records, structs, or have generic parameters are handled or diagnosed
- [ ] #4 Generator robustness tests cover the new shapes via the compilation harness
- [ ] #5 docs/source-generator.md limitation note is updated or removed
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
