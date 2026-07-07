---
id: TASK-030
title: Compiled-delegate member access for the reflection binding path
status: To Do
assignee: []
created_date: '2026-07-07 08:12'
labels:
  - performance
  - serializer
  - deserializer
milestone: m-2
dependencies:
  - TASK-026
priority: medium
ordinal: 20000
---

## Description

<!-- SECTION:DESCRIPTION:BEGIN -->
Performance-architecture finding from the 2026-07-07 review. The full-featured reflection path binds members via PropertyInfo.GetValue/SetValue reflection-invoke on the hot loop (src/Huml.Net/Serialization/HumlSerializerImpl.cs:435, src/Huml.Net/Serialization/HumlDeserializer.cs:435), while the faster delegate-based path is the metadata-poor resolver path — i.e. the complete path is the slow one. Replace reflection-invoke with cached compiled delegates (expression-tree compile where available, with a reflection fallback for platforms where compilation is unavailable/AOT-constrained — note the netstandard2.1 target and the AOT/trim annotations already shipped). Cache alongside PropertyDescriptor so the descriptor build cost is paid once per type. This is a key lever for the m-2 goal of closing the ~2.4x serialise / ~3.5x deserialise gap vs STJ, and converges naturally with the binding-metadata unification work in m-1 (TASK-026).
<!-- SECTION:DESCRIPTION:END -->

## Acceptance Criteria
<!-- AC:BEGIN -->
- [ ] #1 Property get/set on the reflection binding path no longer uses reflection-invoke per member access on the hot loop
- [ ] #2 Behaviour is identical: full test suite green with no test modifications
- [ ] #3 AOT/trimming compatibility is preserved (annotations still valid; fallback path covered by tests)
- [ ] #4 Benchmark before/after numbers recorded against the RESULTS.md baseline
- [ ] #5 Exception translation semantics (user getter/setter exceptions surfacing as Huml exception types) are preserved and tested
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
