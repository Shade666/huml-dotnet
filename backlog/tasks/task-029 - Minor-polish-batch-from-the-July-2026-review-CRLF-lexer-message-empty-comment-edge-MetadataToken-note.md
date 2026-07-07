---
id: TASK-029
title: >-
  Minor polish batch from the July 2026 review (CRLF lexer message,
  empty-comment edge, MetadataToken note)
status: To Do
assignee: []
created_date: '2026-07-07 08:11'
labels:
  - lexer
  - hardening
milestone: m-1
dependencies: []
priority: low
ordinal: 19000
---

## Description

<!-- SECTION:DESCRIPTION:BEGIN -->
Low-severity findings L5, L7, and M4 from the 2026-07-07 code-quality review, batched:

1. L5 — the lexer's scalar-indicator check (src/Huml.Net/Lexer/Lexer.cs:682) handles \n but not \r, so a malformed empty scalar written with CRLF (key:\r\n) gets the misleading lexer error "Expected a space after ':'" while the LF form is rejected later by the parser with a better message. The adjacent :: path (line 670) checks both. Align the check; both inputs remain errors, only the message/position changes.
2. L7 — an empty comment with a trailing space ("# " alone on a line) is rejected as trailing whitespace (src/Huml.Net/Lexer/Lexer.cs:280) because the mandatory delimiter space is treated as trailing content. Confirm intended behaviour against the spec and fixture suite first — if "# " should parse, fix; if not, record won't-fix. Assess against .claude/rules/fixture-gaps.md either way (this is a language-agnostic error-or-no-error behaviour).
3. M4 — property declaration ordering relies on MetadataToken (src/Huml.Net/Serialization/PropertyDescriptor.cs:134), which ECMA-335 does not guarantee reflects source order. Works on all current runtimes but underpins a user-visible documented contract. Add a code comment recording the dependency and a canary test that fails loudly if a future runtime breaks the assumption.
<!-- SECTION:DESCRIPTION:END -->

## Acceptance Criteria
<!-- AC:BEGIN -->
- [ ] #1 CRLF and LF forms of a malformed empty scalar produce the same (parser-level) error message and position
- [ ] #2 The empty-comment-with-trailing-space behaviour is confirmed against the spec and either fixed or recorded as won't-fix with rationale
- [ ] #3 The MetadataToken ordering dependency has an explanatory comment and a canary test
- [ ] #4 Any changed parse behaviour is staged in fixtures/extensions/ per .claude/rules/fixture-gaps.md
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
