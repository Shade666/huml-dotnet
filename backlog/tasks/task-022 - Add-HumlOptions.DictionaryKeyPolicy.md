---
id: TASK-022
title: Add HumlOptions.DictionaryKeyPolicy
status: To Do
assignee: []
created_date: '2026-07-07 08:10'
labels:
  - serializer
  - options
  - stj-parity
milestone: m-1
dependencies: []
priority: medium
ordinal: 12000
---

## Description

<!-- SECTION:DESCRIPTION:BEGIN -->
STJ parity gap (2026-07-07 audit). PropertyNamingPolicy applies to POCO property names but Dictionary&lt;string,T&gt; keys are emitted verbatim (explicitly excluded — src/Huml.Net/Versioning/HumlOptions.cs:119), so a document mixing POCOs and dictionaries comes out with inconsistent key casing (e.g. kebab-case properties next to PascalCase dictionary keys). Mirror System.Text.Json's DictionaryKeyPolicy: an opt-in naming policy applied to dictionary keys on serialisation only (deserialisation reads keys verbatim, as STJ does). Small change reusing the existing HumlNamingPolicy plumbing.
<!-- SECTION:DESCRIPTION:END -->

## Acceptance Criteria
<!-- AC:BEGIN -->
- [ ] #1 HumlOptions.DictionaryKeyPolicy exists, defaults to null (verbatim keys), and applies the chosen HumlNamingPolicy to dictionary keys on serialise
- [ ] #2 Deserialisation is unaffected by the policy (keys read verbatim), matching STJ semantics
- [ ] #3 Round-trip behaviour with a policy set is tested and documented
- [ ] #4 docs cover the option and its interaction with PropertyNamingPolicy
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
