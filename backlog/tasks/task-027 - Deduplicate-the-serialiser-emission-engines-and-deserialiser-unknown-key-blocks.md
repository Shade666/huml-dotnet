---
id: TASK-027
title: >-
  Deduplicate the serialiser emission engines and deserialiser unknown-key
  blocks
status: To Do
assignee: []
created_date: '2026-07-07 08:11'
labels:
  - refactor
  - serializer
  - deserializer
milestone: m-1
dependencies: []
priority: medium
ordinal: 17000
---

## Description

<!-- SECTION:DESCRIPTION:BEGIN -->
Maintainability finding M3 from the 2026-07-07 code-quality review. Three duplications in the serialisation layer invite correctness drift:

1. Two parallel emission engines in src/Huml.Net/Serialization/HumlSerializerImpl.cs — object emission (EmitEntry/EmitSequenceItems/SerializeMappingBody) and AST re-emission for extension data (EmitHumlNode/EmitHumlSequenceItems/EmitMappingEntries, lines 748-882) independently implement the key::, "- ::", and empty-vector-signifier rules, so any spec change to block structure must be made twice.
2. The two public Serialize overloads (lines 43-86 and 95-138) duplicate the pooled-StringBuilder setup verbatim; one should delegate to the other.
3. The unknown-key / extension-data / UnmappedMemberHandling block is copied near-verbatim between PopulateMappingEntries (src/Huml.Net/Serialization/HumlDeserializer.cs:130-152) and DeserializeMappingEntries (lines 400-422).

Pure refactor: no behaviour change, existing tests must stay green, and the shared helpers should make the G3-era block-structure rules single-sourced.
<!-- SECTION:DESCRIPTION:END -->

## Acceptance Criteria
<!-- AC:BEGIN -->
- [ ] #1 Block-structure emission rules (key::, - ::, empty-vector signifiers) are implemented in exactly one place shared by object emission and AST re-emission
- [ ] #2 One Serialize overload delegates to the other with no duplicated setup
- [ ] #3 The unknown-key handling block is a single shared helper used by Populate and Deserialize paths
- [ ] #4 No behaviour change: full test suite green on all target frameworks with no test modifications required
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
