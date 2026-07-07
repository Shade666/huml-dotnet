---
id: TASK-020
title: >-
  Add HumlOptions.PropertyNameCaseInsensitive and unify key matching across
  binding paths
status: To Do
assignee: []
created_date: '2026-07-07 08:10'
labels:
  - deserializer
  - options
  - stj-parity
milestone: m-1
dependencies:
  - TASK-008
references:
  - src/Huml.Net/Serialization/PropertyDescriptor.cs
  - src/Huml.Net/Serialization/HumlDeserializer.cs
priority: high
ordinal: 10000
---

## Description

<!-- SECTION:DESCRIPTION:BEGIN -->
Top recommendation of the 2026-07-07 STJ parity audit, confirmed in code. HUML documents are hand-edited, and a key that differs from the property name only by case currently fails to bind silently on the main path — the standout robustness gap for a human-oriented format.

Today the deserialiser uses three different comparison behaviours: reflection property lookup is case-SENSITIVE (StringComparer.Ordinal, src/Huml.Net/Serialization/PropertyDescriptor.cs:231), the resolver/source-gen path is case-INSENSITIVE (OrdinalIgnoreCase, src/Huml.Net/Serialization/HumlDeserializer.cs:370), and constructor-parameter matching is case-INSENSITIVE (HumlDeserializer.cs:488). The same document can bind differently depending on which path handles it.

Mirror System.Text.Json: add a PropertyNameCaseInsensitive option (default false, matching STJ), make ALL paths honour it consistently, and align the resolver path's default to Ordinal (its current OrdinalIgnoreCase default is the divergence recorded against G3 finding M15 — see TASK-008). Decide and document whether constructor-parameter matching stays case-insensitive (STJ matches ctor params case-insensitively regardless of the option — mirroring that is acceptable if written down).
<!-- SECTION:DESCRIPTION:END -->

## Acceptance Criteria
<!-- AC:BEGIN -->
- [ ] #1 HumlOptions.PropertyNameCaseInsensitive exists, defaults to false, and is honoured by the reflection property path
- [ ] #2 The resolver/source-generated path uses the same comparer as the reflection path for the same options (no per-path divergence)
- [ ] #3 Constructor-parameter matching behaviour under the option is decided, tested, and documented
- [ ] #4 With the option off, a case-mismatched key follows UnmappedMemberHandling (skip or disallow) rather than silently vanishing only on some paths
- [ ] #5 Tests cover both option states across reflection, resolver, and constructor binding
- [ ] #6 docs cover the new option and its STJ equivalence
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
