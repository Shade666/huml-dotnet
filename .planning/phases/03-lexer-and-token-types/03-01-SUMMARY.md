---
phase: 03-lexer-and-token-types
plan: 01
subsystem: lexer
tags: [csharp, enum, record-struct, exception, token]

# Dependency graph
requires:
  - phase: 02-versioning-foundation
    provides: IsExternalInit shim for init-only setters on netstandard2.1, HumlUnsupportedVersionException pattern for sealed exceptions without binary serialisation constructor
provides:
  - TokenType public enum with 18 members covering all HUML token categories
  - Token public readonly record struct with 6 init-only properties and compiler-synthesised value-equality
  - HumlParseException public sealed exception with typed int Line and int Column
affects: [04-lexer-implementation, 05-parser, 06-serializer-deserializer, 07-fixture-compliance]

# Tech tracking
tech-stack:
  added: []
  patterns:
    - "readonly record struct for zero-cost value-equality on stack-allocated tokens"
    - "Sealed exception with typed position properties (Line/Column) and [line:col] message prefix"
    - "null Value for structural tokens (Eof, Comma, etc.) to avoid heap allocation on hot path"

key-files:
  created:
    - src/Huml.Net/Lexer/TokenType.cs
    - src/Huml.Net/Lexer/Token.cs
    - src/Huml.Net/Exceptions/HumlParseException.cs
    - tests/Huml.Net.Tests/Lexer/TokenTypeTests.cs
    - tests/Huml.Net.Tests/Lexer/TokenTests.cs
    - tests/Huml.Net.Tests/Exceptions/HumlParseExceptionTests.cs
  modified: []

key-decisions:
  - "HumlParseException placed in Huml.Net.Exceptions (not Huml.Net.Lexer.Exceptions) — thrown by both Lexer and Parser"
  - "Token.Value is string? (nullable) so structural tokens carry null, eliminating heap allocations on the hot path"
  - "No binary serialisation constructor on HumlParseException — SYSLIB0051 pattern established in Phase 02 maintained"

patterns-established:
  - "Pattern 1: readonly record struct Token — compiler-only feature on netstandard2.1 with LangVersion 13; no runtime requirement"
  - "Pattern 2: Structural tokens (Eof, Comma, ListItem, etc.) use Value = null; value tokens (Key, String, Int, etc.) materialise a string"

requirements-completed: [LEX-01, LEX-02, LEX-06]

# Metrics
duration: 3min
completed: 2026-03-21
---

# Phase 3 Plan 01: Token Contract Types Summary

**18-member TokenType enum, Token readonly record struct with 6 init-only properties, and HumlParseException with typed Line/Column — the lexer/parser contract layer locked before implementation begins**

## Performance

- **Duration:** 3 min
- **Started:** 2026-03-21T00:17:59Z
- **Completed:** 2026-03-21T00:20:15Z
- **Tasks:** 1 (TDD: test commit + feat commit)
- **Files modified:** 6 (3 production, 3 test)

## Accomplishments
- TokenType enum with exactly 18 members, each with XML doc comment, zero build warnings across all 4 TFMs
- Token readonly record struct with 6 init-only properties (Type, Value, Line, Column, Indent, SpaceBefore) and compiler-synthesised value-equality via `==`
- HumlParseException sealed exception with typed `int Line` and `int Column`, message formatted as `[line:column] message`
- 13 new tests all pass across net8.0, net9.0, net10.0 (40 total in suite, no regressions)

## Task Commits

Each task was committed atomically (TDD produces two commits):

1. **RED — failing tests** - `da5a104` (test)
2. **GREEN — production types** - `80a7334` (feat)

## Files Created/Modified
- `src/Huml.Net/Lexer/TokenType.cs` - Public enum with 18 members; all HUML token categories covered; XML docs on each member
- `src/Huml.Net/Lexer/Token.cs` - Public readonly record struct with 6 init-only properties; null Value for structural tokens
- `src/Huml.Net/Exceptions/HumlParseException.cs` - Sealed exception; typed Line/Column; [line:col] message prefix; no binary serialisation ctor
- `tests/Huml.Net.Tests/Lexer/TokenTypeTests.cs` - 2 tests: member count == 18, all 18 names defined
- `tests/Huml.Net.Tests/Lexer/TokenTests.cs` - 6 tests: value type check, construction, equality, inequality, null value, non-null value
- `tests/Huml.Net.Tests/Exceptions/HumlParseExceptionTests.cs` - 5 tests: Line, Column, message prefix, message content, sealed+assignable

## Decisions Made
- HumlParseException uses namespace `Huml.Net.Exceptions` (not `Huml.Net.Lexer.Exceptions`) because it will be thrown by both the Lexer (Phase 3) and Parser (Phase 5) — it belongs to the shared exceptions namespace
- Token.Value is `string?` (nullable) so structural tokens carry `null`, eliminating heap allocations on the hot path for `Eof`, `Comma`, `ListItem`, `ScalarIndicator`, `VectorIndicator`, `EmptyList`, `EmptyDict`
- No binary serialisation constructor on HumlParseException — maintains the SYSLIB0051 pattern established by HumlUnsupportedVersionException in Phase 02

## Deviations from Plan

None - plan executed exactly as written.

## Issues Encountered

None.

## User Setup Required

None - no external service configuration required.

## Next Phase Readiness
- Token contract is locked; Phase 3 Plan 02 (Lexer implementation) can now reference `Token`, `TokenType`, and `HumlParseException` without concern for post-hoc field changes
- All 4 TFMs build cleanly at zero warnings, including `netstandard2.1` which requires the `IsExternalInit` shim (already in place from Phase 02)

---
*Phase: 03-lexer-and-token-types*
*Completed: 2026-03-21*
