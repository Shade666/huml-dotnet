---
id: TASK-012
title: Add IBufferWriter<char> / TextWriter serialise overload (999.45)
status: To Do
assignee: []
created_date: '2026-06-12 23:29'
labels:
  - performance
  - serializer
  - api
milestone: m-2
dependencies: []
documentation:
  - docs/plans/2026-06-10-backlog-disposition.md
  - docs/internals/api-freeze.md
priority: medium
ordinal: 1000
---

## Description

<!-- SECTION:DESCRIPTION:BEGIN -->
Deferred backlog item 999.45 (docs/plans/2026-06-10-backlog-disposition.md): HumlSerializer.Serialize currently returns a string built via the pooled-StringBuilder path. For high-throughput scenarios (writing to files, network streams, response bodies) an overload writing into a caller-supplied IBufferWriter<char> or TextWriter avoids the final string allocation entirely. This is also the single largest serialise-side lever named in huml-dotnet-examples/benchmarks/RESULTS.md. Additive public API — needs a written justification per the freeze policy (docs/internals/api-freeze.md) and mirrors STJ shape conventions (STJ offers Utf8JsonWriter/Stream overloads; ours is char-based since Huml.Net is UTF-16/string-native). After implementing, re-run the benchmark suite in huml-dotnet-examples and update RESULTS.md with the new overload's row.
<!-- SECTION:DESCRIPTION:END -->

## Acceptance Criteria
<!-- AC:BEGIN -->
- [ ] #1 A Serialize overload accepting IBufferWriter<char> (and/or TextWriter — decide and justify) produces byte-identical output to the string overload
- [ ] #2 The overload allocates no intermediate full-document string (verified with an allocation test)
- [ ] #3 Works on all four TFMs including netstandard2.1
- [ ] #4 API addition is justified in docs/internals/api-freeze.md and reflected in the public API baseline
- [ ] #5 Benchmarks re-run and RESULTS.md updated in huml-dotnet-examples showing the new overload
- [ ] #6 Docs/getting-started or serialisation guide mentions the overload
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
