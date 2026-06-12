---
id: TASK-001
title: Triage open dependabot PRs and fix the failing Roslyn bump
status: To Do
assignee: []
created_date: '2026-06-12 23:27'
labels:
  - ci
  - dependencies
milestone: m-0
dependencies: []
references:
  - 'https://github.com/primeBeri/huml-dotnet/pull/23'
  - >-
    https://github.blog/changelog/2025-09-19-deprecation-of-node-20-on-github-actions-runners/
priority: high
ordinal: 1000
---

## Description

<!-- SECTION:DESCRIPTION:BEGIN -->
Six dependabot PRs are open on primeBeri/huml-dotnet: #17-#20 bump docs.yml GitHub Actions (upload-pages-artifact 5, checkout 6, setup-dotnet 5, deploy-pages 5 — these also resolve the Node 20 deprecation warning; GitHub forces Node 24 from 16 June 2026), #21 bumps Meziantou.Analyzer 3.0.102, and #23 (Microsoft.CodeAnalysis.Analyzers + Microsoft.CodeAnalysis.CSharp for the source generator) has FAILING CI that needs investigation — likely new analyzer diagnostics surfacing under TreatWarningsAsErrors, or a Roslyn API change in the generator. Merge what is green, fix what is not. Note docs.yml currently uses unpinned @v4 tags while ci.yml/publish.yml pin by SHA — pin docs.yml to SHAs while you are there for consistency.
<!-- SECTION:DESCRIPTION:END -->

## Acceptance Criteria
<!-- AC:BEGIN -->
- [ ] #1 All six open dependabot PRs are merged or closed with a written reason
- [ ] #2 PR #23's CI failure root cause is identified and fixed (or the bump is declined with rationale recorded on the PR)
- [ ] #3 docs.yml actions are pinned by commit SHA like ci.yml and publish.yml
- [ ] #4 No workflow in the repo still triggers the Node 20 deprecation annotation
- [ ] #5 Docs site still deploys successfully after the action bumps (verify the Pages run)
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
