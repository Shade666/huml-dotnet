---
phase: 03-lexer-and-token-types
plan: 02
subsystem: lexer
tags: [csharp, lexer, span, zero-allocation, token-stream, tdd]

# Dependency graph
requires:
  - phase: 03-01
    provides: Token readonly record struct, TokenType enum, HumlParseException — the contract this Lexer implements against
  - phase: 02-versioning-foundation
    provides: HumlOptions, HumlSpecVersion with V0_1/V0_2 enum values for version-gated rules
provides:
  - internal sealed Lexer class with NextToken() pull model and string constructor
  - Full HUML v0.2 tokenisation: keys, quoted keys, all scalar types, vectors, inline lists, empty collections, version directive, multiline strings
  - Version-gated backtick multiline: V0_1 succeeds, V0_2 throws HumlParseException
  - Zero-allocation hot path verified by GC.GetAllocatedBytesForCurrentThread allocation tests
affects: [04-lexer-implementation, 05-parser, 06-serializer-deserializer, 07-fixture-compliance]

# Tech tracking
tech-stack:
  added: []
  patterns:
    - "string field + .AsSpan() inside methods for zero-allocation source access"
    - "Fast path (no StringBuilder) for escape-free quoted strings; slow path (StringBuilder) only when escapes present"
    - "using alias (HumlLexer = Huml.Net.Lexer.Lexer) to resolve namespace/class name collision in test project"
    - "Trailing whitespace detection in main token loop by peeking ahead to next non-space char"

key-files:
  created:
    - src/Huml.Net/Lexer/Lexer.cs
    - tests/Huml.Net.Tests/Lexer/LexerTests.cs
    - tests/Huml.Net.Tests/Lexer/LexerAllocationTests.cs
  modified: []

key-decisions:
  - "Test namespace collision: Huml.Net.Tests.Lexer namespace shadows Huml.Net.Lexer.Lexer class — resolved with using alias HumlLexer"
  - "Trailing whitespace detection: checked inline in main token loop by peeking ahead when a space is encountered"
  - "Invalid key start (digit): detected at key position (col == lineIndent) in main dispatch; throws HumlParseException with correct position"

patterns-established:
  - "Pattern: using alias for fully-qualified type when namespace/class name collision exists in test namespace"
  - "Pattern: peek-ahead trailing whitespace check in main loop rather than post-scan check"

requirements-completed: [LEX-03, LEX-04, LEX-05]

# Metrics
duration: 5min
completed: 2026-03-21
---

# Phase 3 Plan 02: Lexer Implementation Summary

**Single-pass ReadOnlyMemory-backed Lexer (936 lines) with NextToken() pull model, full HUML v0.2 tokenisation, version-gated backtick multiline, and GC-verified zero-allocation hot path**

## Performance

- **Duration:** 5 min
- **Started:** 2026-03-21T00:23:12Z
- **Completed:** 2026-03-21T00:28:13Z
- **Tasks:** 2 (TDD: RED commit + GREEN commit per task)
- **Files modified:** 3 (1 production, 2 test)

## Accomplishments
- 936-line Lexer class implementing all HUML v0.2 tokenisation rules with zero build warnings on all 4 TFMs
- 37 functional LexerTests covering: basic tokens, vectors, numerics, escape sequences, multiline strings, position tracking, version gate, and all 5 error conditions
- 2 allocation tests confirming < 1024 bytes allocated on hot path (only value-string materialisations)
- 79 total tests pass across net8.0/net9.0/net10.0 (Phase 1 + 2 + 3 all green)

## Task Commits

Each task was committed atomically (TDD produces multiple commits):

1. **RED — failing LexerTests** - `8825cef` (test)
2. **GREEN — Lexer implementation** - `0fff7f4` (feat)
3. **Task 2: LexerAllocationTests** - `f55cbe9` (feat)

## Files Created/Modified
- `src/Huml.Net/Lexer/Lexer.cs` - 936-line single-pass lexer; internal sealed class with NextToken() pull model; string field + .AsSpan() for zero-allocation; 8 HumlParseException throw sites; version-gate for backtick multiline
- `tests/Huml.Net.Tests/Lexer/LexerTests.cs` - 37 test methods covering all tokenisation rules, error conditions, position tracking, and version gating
- `tests/Huml.Net.Tests/Lexer/LexerAllocationTests.cs` - 2 tests using GC.GetAllocatedBytesForCurrentThread asserting < 1024 bytes on hot path

## Decisions Made
- **Namespace/class collision resolved with using alias:** `Huml.Net.Tests.Lexer` namespace caused `Lexer` to resolve as a namespace reference, not the class. Fixed with `using HumlLexer = Huml.Net.Lexer.Lexer;` in test files.
- **Trailing whitespace detection inline:** Detected in main token dispatch when a space is encountered by peeking ahead to the next non-space character; simpler than post-scan checking.
- **Invalid key start at col == lineIndent:** A digit at the start of a key slot (column equals the line's indent level) throws immediately with correct position.

## Deviations from Plan

### Auto-fixed Issues

**1. [Rule 1 - Bug] Added using alias to resolve namespace/class name collision**
- **Found during:** Task 1 (RED phase — build of tests)
- **Issue:** `LexerTests.cs` lives in namespace `Huml.Net.Tests.Lexer`; within that namespace, `Lexer` resolves to the `Huml.Net.Lexer` namespace rather than the `Lexer` class (CS0118 error)
- **Fix:** Added `using HumlLexer = Huml.Net.Lexer.Lexer;` to both test files and replaced `new Lexer(...)` with `new HumlLexer(...)`
- **Files modified:** tests/Huml.Net.Tests/Lexer/LexerTests.cs, tests/Huml.Net.Tests/Lexer/LexerAllocationTests.cs
- **Verification:** Build succeeded; 37 tests green
- **Committed in:** 0fff7f4 (Task 1 GREEN commit)

---

**Total deviations:** 1 auto-fixed (Rule 1 — bug in test compilation)
**Impact on plan:** Minor naming fix required by C# namespace resolution. No scope change, no architectural impact.

## Issues Encountered

None — both test failures after initial GREEN implementation (trailing whitespace, digit key start) were straightforward logic bugs fixed in the same session before commit.

## Known Stubs

None — the Lexer produces all token types. No placeholder values or hardcoded stubs.

## User Setup Required

None - no external service configuration required.

## Next Phase Readiness
- Lexer contract fully implemented; Phase 5 (Parser) can couple to `new Lexer(string, HumlOptions)` + `NextToken()` immediately
- All 4 TFMs build at zero warnings; `netstandard2.1` covered by existing IsExternalInit shim
- Full test suite green (79 tests); regression baseline established

---
*Phase: 03-lexer-and-token-types*
*Completed: 2026-03-21*
