---
id: TASK-015
title: Contribute extension fixtures upstream to huml-lang/tests
status: To Do
assignee: []
created_date: '2026-06-12 23:29'
labels:
  - upstream
  - fixtures
milestone: m-3
dependencies: []
documentation:
  - .claude/rules/fixture-gaps.md
priority: medium
ordinal: 2000
---

## Description

<!-- SECTION:DESCRIPTION:BEGIN -->
fixtures/extensions/ holds locally-staged, language-agnostic {name, input, error} assertion rows (gaps.json under v0.1 and v0.2) written during the beta programme — they are a staging area for upstream contribution by design. Follow the contribution workflow in .claude/rules/fixture-gaps.md section 5: fork huml-lang/tests, add the assertion rows (and any document pairs), open a PR explaining each behaviour, and once merged roll the fixtures/v0.1 / fixtures/v0.2 submodule pointers and DELETE the corresponding local extension rows (leaving them duplicated would create duplicate Theory rows in SharedSuiteTests). Note some rows assert behaviours where the spec text and go-huml disagree — coordinate with the spec-clarification issues task so the PR does not assert behaviours upstream has not yet ruled on; contribute the uncontroversial rows first if the clarifications stall.
<!-- SECTION:DESCRIPTION:END -->

## Acceptance Criteria
<!-- AC:BEGIN -->
- [ ] #1 A PR to huml-lang/tests contains the uncontroversial extension fixture rows with explanations
- [ ] #2 Rows depending on unresolved spec clarifications are held back and noted on this task
- [ ] #3 After merge: submodule pointers rolled, merged rows deleted from fixtures/extensions/, and the SharedSuiteTests Theory counts verified (no duplicates, no losses)
- [ ] #4 Full test suite green after the submodule roll
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
