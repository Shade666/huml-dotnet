---
id: TASK-002
title: Switch examples repo CI to the published NuGet package by default
status: To Do
assignee: []
created_date: '2026-06-12 23:27'
labels:
  - ci
  - examples-repo
milestone: m-0
dependencies: []
references:
  - >-
    https://github.com/primeBeri/huml-dotnet-examples/blob/main/.github/workflows/examples.yml
priority: medium
ordinal: 2000
---

## Description

<!-- SECTION:DESCRIPTION:BEGIN -->
Now that Huml.Net 0.2.0-beta.1 and Huml.Net.SourceGeneration 0.2.0-beta.1 are live on NuGet.org, the huml-dotnet-examples repo (primeBeri/huml-dotnet-examples) should restore from nuget.org by default instead of packing the main repo into local-feed/ on every push/PR run. The workflow's own comment says to do this once the beta is published. Invert the mode: default = published package (currently requires the workflow_dispatch huml_version input); keep a pack-from-source mode available (e.g. via the dispatch input) for pre-release testing of unreleased changes. Also bump the repo's actions/checkout@v4 and actions/setup-dotnet@v4 to Node-24-safe major versions — this repo has no dependabot config, so consider adding one. Cross-repo task: work happens in huml-dotnet-examples, not this repo.
<!-- SECTION:DESCRIPTION:END -->

## Acceptance Criteria
<!-- AC:BEGIN -->
- [ ] #1 Push/PR runs of examples.yml restore Huml.Net from nuget.org without checking out or packing the main repo
- [ ] #2 A pack-from-source mode remains available for testing unreleased main-repo changes, and is documented in the workflow or README
- [ ] #3 run-examples.ps1 default HumlNetVersion matches the latest published version
- [ ] #4 actions/checkout and actions/setup-dotnet are bumped to Node-24-safe versions
- [ ] #5 A dependabot (or equivalent) config keeps the examples repo's actions and packages updated
- [ ] #6 CI is green in both modes after the change
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
