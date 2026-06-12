---
id: TASK-014
title: File the six upstream HUML spec clarification issues
status: To Do
assignee: []
created_date: '2026-06-12 23:29'
labels:
  - upstream
  - spec
milestone: m-3
dependencies: []
documentation:
  - docs/plans/2026-06-10-backlog-disposition.md
  - docs/spec-compliance-report.md
priority: medium
ordinal: 1000
---

## Description

<!-- SECTION:DESCRIPTION:BEGIN -->
The G2.1 disposition (docs/plans/2026-06-10-backlog-disposition.md, section 'UPSTREAM') identified six places where the HUML spec text, EBNF grammar, and the go-huml reference implementation disagree — in five of them Huml.Net deliberately matches go-huml against the spec text. File them as issues on the appropriate huml-lang repository (spec lives in huml-lang/website): (1) EBNF huml_document allows NEWLINE? before %HUML but both implementations require line 1 (S2); (2) tokenizer digit-class vs prose for uppercase base prefixes/exponent 0X/0O/0B/E (L7); (3) multiline_list_item production missing the bare '- []'/'- {}' form both implementations accept (L5); (4) prose multiline-string indentation rules vs both implementations' leniency and error behaviour (L6/S3); (5) go-huml accepts bare '#' at EOL where the grammar 'comment = \"# \"' says error (S5); (6) spec silence on BOM handling and on spaces before inline '#' (L9). Each issue should cite spec section, go-huml source location (recorded in the disposition doc), and Huml.Net's behaviour. Outcomes feed back as code changes only if upstream rules against our behaviour.
<!-- SECTION:DESCRIPTION:END -->

## Acceptance Criteria
<!-- AC:BEGIN -->
- [ ] #1 Six issues filed upstream, each citing spec text, go-huml behaviour with source reference, and Huml.Net behaviour
- [ ] #2 Issue links recorded in docs/plans/2026-06-10-backlog-disposition.md next to the corresponding UPSTREAM rows
- [ ] #3 docs/spec-compliance-report.md deferred-divergence rows link to their issues
- [ ] #4 Any upstream ruling that contradicts current Huml.Net behaviour gets a follow-up task created and linked
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
