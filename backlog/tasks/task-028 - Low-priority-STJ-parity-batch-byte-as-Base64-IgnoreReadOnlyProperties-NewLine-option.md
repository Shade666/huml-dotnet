---
id: TASK-028
title: >-
  Low-priority STJ parity batch: byte[] as Base64, IgnoreReadOnlyProperties,
  NewLine option
status: To Do
assignee: []
created_date: '2026-07-07 08:11'
labels:
  - serializer
  - options
  - stj-parity
milestone: m-1
dependencies: []
priority: low
ordinal: 18000
---

## Description

<!-- SECTION:DESCRIPTION:BEGIN -->
Grouped low-priority gaps from the 2026-07-07 STJ parity audit, batched because each is small and none justifies a standalone task:

1. byte[] as Base64 — STJ emits byte arrays as a Base64 string; Huml.Net falls through to IEnumerable&lt;byte&gt; and emits a verbose integer sequence. Emit/read Base64 strings for byte[] (decide behaviour for existing integer-sequence documents: accepting both on read is the compatible choice).
2. IgnoreReadOnlyProperties — get-only computed properties are always emitted and can pollute output; add the opt-in STJ-equivalent option.
3. NewLine — output currently uses a fixed newline; add an option (STJ NewLine equivalent) so cross-platform writers can choose LF vs CRLF. Note the PARSER already accepts both; this is write-side only. IndentCharacter/IndentSize were considered and rejected — HUML's 2-space indent is spec-conventional.

Deliberately deferred (recorded here so they are not re-raised): IncludeFields/[HumlInclude] (build if consumers ask), PreferredObjectCreationHandling deep-populate, ReferenceHandler.Preserve ($id/$ref is a poor fit for a human-readable format — treat as anti-feature unless demand appears).
<!-- SECTION:DESCRIPTION:END -->

## Acceptance Criteria
<!-- AC:BEGIN -->
- [ ] #1 byte[] serialises as a Base64 string and deserialises from one; read behaviour for legacy integer sequences is decided and tested
- [ ] #2 HumlOptions.IgnoreReadOnlyProperties exists, defaults to false, and suppresses get-only properties when set
- [ ] #3 A write-side newline option exists and is honoured by all emission paths
- [ ] #4 docs updated for each option; deferred items above recorded as won't-do-for-now in the docs or task notes
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
