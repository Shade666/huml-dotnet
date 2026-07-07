---
id: TASK-026
title: Unify the binding metadata model across reflection and resolver paths
status: To Do
assignee: []
created_date: '2026-07-07 08:11'
labels:
  - architecture
  - source-generator
  - deserializer
  - serializer
milestone: m-1
dependencies:
  - TASK-008
priority: medium
ordinal: 16000
---

## Description

<!-- SECTION:DESCRIPTION:BEGIN -->
Central finding of the 2026-07-07 architecture review. The library has two binding implementations that disagree: the reflection path (PropertyDescriptor) carries the full feature set (converters, naming, OmitIfDefault, required, extension data, UnmappedMemberHandling.Disallow, constructor selection), while the resolver/source-gen path (src/Huml.Net/Serialization/HumlDeserializer.cs:360-382, src/Huml.Net/Serialization/HumlSerializerImpl.cs:411-424) iterates minimal HumlPropertyInfo metadata and bypasses those checks. HumlPropertyInfo carries no converter, number-handling, ignore, or naming metadata, so path parity is structurally impossible without extending the model.

TASK-008 covers enforcing the individual checks; this task is the structural remainder: extend HumlTypeInfo/HumlPropertyInfo so the resolver seam can carry the missing metadata (converter, required, order, naming, omit-if-default), then either route both paths through a single binder or document the resolver contract as explicitly reduced. This also unlocks the m-2 compiled-delegate work, since a converged binder gives the delegate path full fidelity. Public-surface additions must be justified per docs/internals/api-freeze.md (additive).
<!-- SECTION:DESCRIPTION:END -->

## Acceptance Criteria
<!-- AC:BEGIN -->
- [ ] #1 HumlTypeInfo/HumlPropertyInfo can represent converter, required, order, naming, and omit-if-default metadata (or a written decision records the resolver path as a reduced contract)
- [ ] #2 The serialiser and deserialiser consume that metadata identically on both paths
- [ ] #3 The source generator emits the extended metadata for registered types
- [ ] #4 A reflection-vs-resolver parity test matrix over a shared DTO set asserts identical output and identical accepted/rejected documents
- [ ] #5 docs/source-generator.md reflects the final contract
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
