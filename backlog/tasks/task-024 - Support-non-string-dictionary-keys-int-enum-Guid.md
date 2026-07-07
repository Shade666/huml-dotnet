---
id: TASK-024
title: 'Support non-string dictionary keys (int, enum, Guid)'
status: To Do
assignee: []
created_date: '2026-07-07 08:11'
labels:
  - serializer
  - deserializer
  - stj-parity
milestone: m-1
dependencies: []
priority: medium
ordinal: 14000
---

## Description

<!-- SECTION:DESCRIPTION:BEGIN -->
STJ parity gap (2026-07-07 audit). Only string-keyed dictionaries deserialise today — the IsStringKeyedDictionary gate (src/Huml.Net/Serialization/HumlDeserializer.cs:997) sends everything else down the POCO path, which fails confusingly. Enum- and int-keyed dictionaries are common in configuration (e.g. Dictionary&lt;LogLevel,string&gt;). Mirror System.Text.Json: support primitive, enum, and Guid key types by converting the HUML string key via the invariant culture on read and formatting it invariantly on write. Note HUML keys may need quoting when the formatted key is not a bare-key-safe token.
<!-- SECTION:DESCRIPTION:END -->

## Acceptance Criteria
<!-- AC:BEGIN -->
- [ ] #1 Dictionaries keyed by integer types, enums, and Guid round-trip (serialise and deserialise)
- [ ] #2 Key conversion uses the invariant culture in both directions
- [ ] #3 An unconvertible key value throws HumlDeserializeException naming the key and target type
- [ ] #4 Keys that are not bare-key-safe are quoted on emit and re-read correctly
- [ ] #5 Unsupported key types keep a clear error rather than falling through to the POCO path
- [ ] #6 docs updated with supported key types
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
