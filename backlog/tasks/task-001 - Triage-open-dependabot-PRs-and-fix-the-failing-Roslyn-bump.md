---
id: TASK-001
title: Triage open dependabot PRs and fix the failing Roslyn bump
status: Done
assignee:
  - claude
created_date: '2026-06-12 23:27'
updated_date: '2026-06-12 23:47'
labels:
  - ci
  - dependencies
milestone: m-0
dependencies: []
references:
  - 'https://github.com/primeBeri/huml-dotnet/pull/23'
  - >-
    https://github.blog/changelog/2025-09-19-deprecation-of-node-20-on-github-actions-runners/
modified_files:
  - .github/workflows/docs.yml
  - .github/dependabot.yml
  - Directory.Build.props
priority: high
ordinal: 1000
---

## Description

<!-- SECTION:DESCRIPTION:BEGIN -->
Six dependabot PRs are open on primeBeri/huml-dotnet: #17-#20 bump docs.yml GitHub Actions (upload-pages-artifact 5, checkout 6, setup-dotnet 5, deploy-pages 5 — these also resolve the Node 20 deprecation warning; GitHub forces Node 24 from 16 June 2026), #21 bumps Meziantou.Analyzer 3.0.102, and #23 (Microsoft.CodeAnalysis.Analyzers + Microsoft.CodeAnalysis.CSharp for the source generator) has FAILING CI that needs investigation — likely new analyzer diagnostics surfacing under TreatWarningsAsErrors, or a Roslyn API change in the generator. Merge what is green, fix what is not. Note docs.yml currently uses unpinned @v4 tags while ci.yml/publish.yml pin by SHA — pin docs.yml to SHAs while you are there for consistency.
<!-- SECTION:DESCRIPTION:END -->

## Acceptance Criteria
<!-- AC:BEGIN -->
- [x] #1 All six open dependabot PRs are merged or closed with a written reason
- [x] #2 PR #23's CI failure root cause is identified and fixed (or the bump is declined with rationale recorded on the PR)
- [x] #3 docs.yml actions are pinned by commit SHA like ci.yml and publish.yml
- [x] #4 No workflow in the repo still triggers the Node 20 deprecation annotation
- [x] #5 Docs site still deploys successfully after the action bumps (verify the Pages run)
<!-- AC:END -->

## Implementation Plan

<!-- SECTION:PLAN:BEGIN -->
Investigation findings: PRs #17-#21 are CI-green; #23 fails with CS9057 — the generator DLL, compiled against Microsoft.CodeAnalysis.CSharp 5.3.0, cannot be loaded by the SDK's Roslyn 5.0 compiler. A source generator must compile against the OLDEST Roslyn it intends to support; 4.9.2 is a deliberate compatibility floor, so the bump is wrong by design, not broken by accident.

Plan:
1. Merge PR #21 (Meziantou.Analyzer 3.0.102) — green, plain package bump.
2. Close PRs #17-#20 (docs.yml action tag bumps) as superseded: apply one local commit to docs.yml that bumps AND pins all four actions by commit SHA (checkout v6.0.3 df4cb1c0..., setup-dotnet v5.3.0 9a946fdb..., upload-pages-artifact v5.0.0 fc324d35..., deploy-pages v5.0.0 cd2ce8fc...), matching ci.yml/publish.yml pinning style. Merging the four tag-based PRs and then re-pinning would churn docs.yml five times.
3. Close PR #23 with the CS9057 rationale; add dependabot ignore rules for Microsoft.CodeAnalysis.CSharp and Microsoft.CodeAnalysis.Analyzers in .github/dependabot.yml so the bump is not re-proposed.
4. Pull main, full local rebuild + three-TFM test run (Meziantou bump can surface new analyzer diagnostics under TreatWarningsAsErrors).
5. Push; verify CI green and the Pages deploy run succeeds with the new pinned actions.
<!-- SECTION:PLAN:END -->

## Implementation Notes

<!-- SECTION:NOTES:BEGIN -->
#21 squash-merged (09724c2); full clean rebuild after the bump: 0 warnings, 1319/1319 tests on net8/9/10. #17-#20 closed as superseded by commit e125617 (single SHA-pinned bump of all four docs.yml actions). #23 closed with the CS9057 compatibility-floor rationale; dependabot ignore rules added for Microsoft.CodeAnalysis.CSharp and Microsoft.CodeAnalysis.Analyzers in .github/dependabot.yml. Awaiting CI + Pages deploy runs for AC#4/#5.
<!-- SECTION:NOTES:END -->

## Final Summary

<!-- SECTION:FINAL_SUMMARY:BEGIN -->
All six dependabot PRs dispositioned; the repo is Node-24-ready ahead of GitHub's 16 June forcing date.

What changed:
- PR #21 (Meziantou.Analyzer 3.0.101→3.0.102) squash-merged as 09724c2. Verified locally with a full clean Release rebuild (0 warnings under TreatWarningsAsErrors) and 1319/1319 tests on net8.0/net9.0/net10.0.
- PRs #17–#20 closed as superseded: commit e125617 bumps all four docs.yml actions in one change AND pins them by commit SHA (checkout v6.0.3, setup-dotnet v5.3.0, upload-pages-artifact v5.0.0, deploy-pages v5.0.0), matching ci.yml/publish.yml style. The post-bump Docs run deployed successfully with zero annotations — the Node 20 deprecation warning is gone repo-wide.
- PR #23 (Microsoft.CodeAnalysis.* bump) declined with rationale on the PR: the failure is CS9057 — a source generator's Roslyn reference is a compatibility FLOOR (it loads inside the consumer's compiler), so compiling against 5.3.0 makes the generator unloadable on SDKs shipping older compilers, including the current .NET 10 SDK (Roslyn 5.0). 4.9.2 stays as the deliberate floor. Dependabot ignore rules added for both packages so the bump is not re-proposed; future floor bumps are a conscious drop-older-SDKs decision.

DoD notes: CHANGELOG untouched deliberately — analyzer/CI-infrastructure changes are not user-visible package behaviour. No public API, XML-doc, or parse-behaviour surface involved.

Verification: main CI run 27449535042 success; Docs deploy run 27449535005 success with no annotations.
<!-- SECTION:FINAL_SUMMARY:END -->

## Definition of Done
<!-- DOD:BEGIN -->
- [x] #1 dotnet build succeeds with zero warnings (TreatWarningsAsErrors is on; full rebuild to surface cached analyzer results)
- [x] #2 dotnet test green on net8.0 / net9.0 / net10.0
- [x] #3 CHANGELOG.md [Unreleased] section updated for every user-visible change
- [x] #4 Any public API change is additive and justified in writing per docs/internals/api-freeze.md
- [x] #5 New/changed public members have XML docs; tests use AwesomeAssertions (never FluentAssertions)
- [x] #6 New error-or-no-error parse behaviours assessed against .claude/rules/fixture-gaps.md and staged in fixtures/extensions/ when language-agnostic
<!-- DOD:END -->
