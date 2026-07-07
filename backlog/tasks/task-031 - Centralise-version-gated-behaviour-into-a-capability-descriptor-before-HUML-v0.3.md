---
id: TASK-031
title: >-
  Centralise version-gated behaviour into a capability descriptor before HUML
  v0.3
status: To Do
assignee: []
created_date: '2026-07-07 08:12'
labels:
  - architecture
  - versioning
milestone: m-4
dependencies: []
priority: low
ordinal: 21000
---

## Description

<!-- SECTION:DESCRIPTION:BEGIN -->
From the 2026-07-07 architecture review. The single-code-path version-gating strategy is clean but under-exercised: only two behavioural gates exist in the whole engine, both in the lexer (src/Huml.Net/Lexer/Lexer.cs:444 preserveSpaces and :527), and none in the parser body — v0.1 and v0.2 barely diverge, so the strategy is untested against real forking pressure. When HUML v0.3 lands, grammar changes would otherwise scatter ad hoc ">= HumlSpecVersion.V0_2" branches across four ~1,000-line files (Lexer, HumlParser, HumlSerializerImpl, HumlDeserializer). Before implementing v0.3: introduce an internal capability descriptor (e.g. a per-version feature table resolved once from HumlSpecVersion, consulted by name at gate sites) so each behavioural difference is declared centrally in SpecVersionPolicy rather than encoded as scattered comparisons. Also consider the parser-writes-lexer back-edge (_lexer.EffectiveSpecVersion authored in HumlParser.cs:801-829) as part of the same consolidation. Internal-only change; no public API impact.
<!-- SECTION:DESCRIPTION:END -->

## Acceptance Criteria
<!-- AC:BEGIN -->
- [ ] #1 An internal per-version capability table exists in the versioning layer and the existing lexer gates consult it instead of raw enum comparisons
- [ ] #2 The version-state handoff between parser and lexer is single-sourced or explicitly documented
- [ ] #3 Existing v0.1/v0.2 behaviour is unchanged: full fixture suite green
- [ ] #4 A short internal doc describes how to add a v0.3 gate
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
