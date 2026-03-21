---
phase: 05-parser
plan: 01
subsystem: parser
tags: [csharp, recursive-descent, ast, huml, tdd]

requires:
  - phase: 04-ast-node-hierarchy
    provides: HumlNode, HumlDocument, HumlMapping, HumlSequence, HumlScalar, ScalarKind
  - phase: 03-lexer
    provides: Lexer, Token, TokenType, HumlParseException

provides:
  - HumlParser internal sealed class in Huml.Net.Parser namespace
  - Full HUML v0.2 grammar coverage: scalars, mappings, multiline/inline lists/dicts, nested structures
  - Version gating via _options field forwarded to Lexer (PARS-04)
  - Recursion depth guard with _maxDepth = 512 (PARS-05)

affects:
  - 05-parser-plan-02
  - 06-serializer-deserializer
  - 07-fixture-compliance

tech-stack:
  added: []
  patterns:
    - "Recursive-descent parser with single-token lookahead (LL(1) grammar)"
    - "Inline vs multiline dispatch via VectorIndicator line-number comparison"
    - "HashSet<string> duplicate-key detection at every dict nesting level"
    - "Depth guard with ++_depth > _maxDepth at every recursive entry, --_depth in finally"

key-files:
  created:
    - src/Huml.Net/Parser/HumlParser.cs
    - tests/Huml.Net.Tests/Parser/HumlParserTests.cs
  modified:
    - src/Huml.Net/Lexer/Lexer.cs
    - tests/Huml.Net.Tests/Lexer/LexerTests.cs

key-decisions:
  - "Inline vs multiline vector dispatch uses VectorIndicator.Line vs next-token.Line comparison — not SpaceBefore (Key tokens always have SpaceBefore=false in the lexer)"
  - "Lexer no longer throws for digit at line-start; root integer/float scalars are valid HUML; integer-as-key error is now a parser-level concern"
  - "Root inline dict flattens to top-level HumlDocument entries; root inline list wraps in one HumlSequence inside HumlDocument"
  - "ParseVector receives indicatorLine parameter (not relying on SpaceBefore) for correct inline/multiline detection"
  - "LexerTests updated: Integer_at_line_start_produces_Int_token replaces Invalid_key_start_throws to reflect spec-correct behavior"

patterns-established:
  - "ParseVector(int childIndent, int indicatorLine) — always pass indicator line to ParseVector callers"
  - "ParseMappingEntries loops until Eof or indent < expected; throws on indent != expected (bad indentation)"
  - "Depth guard: if (++_depth > _maxDepth) throw; try { ... } finally { _depth--; }"

requirements-completed: [PARS-03, PARS-04]

duration: 7min
completed: 2026-03-21
---

# Phase 5 Plan 1: Parser Implementation Summary

**Recursive-descent HumlParser covering full HUML v0.2 grammar — scalars, mappings, inline/multiline lists and dicts, nested structures, duplicate key detection, and version-gated options forwarding**

## Performance

- **Duration:** 7 min
- **Started:** 2026-03-21T08:07:46Z
- **Completed:** 2026-03-21T08:15:04Z
- **Tasks:** 2 (RED + GREEN TDD cycle)
- **Files modified:** 4

## Accomplishments

- Implemented `HumlParser` as `internal sealed class` with full recursive-descent grammar coverage
- All 24 parser unit tests pass on net8.0 / net9.0 / net10.0 (135 total, zero regressions)
- Fixed lexer bug that incorrectly rejected root-level integer/float scalars, enabling HUML's valid `123` root document form
- Parser propagates `HumlOptions` to the `Lexer` for version gating (PARS-04); backtick multiline accepts in v0.1, rejects in v0.2

## Task Commits

1. **Task 1: Write failing parser tests (RED phase)** - `e4b76c4` (test)
2. **Task 2: Implement recursive-descent parser (GREEN phase)** - `2c5e33a` (feat)

**Plan metadata:** (docs commit follows)

_Note: TDD plan — test commit first, then implementation._

## Files Created/Modified

- `src/Huml.Net/Parser/HumlParser.cs` - Full recursive-descent parser: 665 lines covering all grammar rules
- `tests/Huml.Net.Tests/Parser/HumlParserTests.cs` - 24 unit tests covering scalars, mappings, vectors, error cases, version gating
- `src/Huml.Net/Lexer/Lexer.cs` - Bug fix: removed incorrect digit-at-line-start throw; now always scans number token
- `tests/Huml.Net.Tests/Lexer/LexerTests.cs` - Updated lexer test to match corrected behavior

## Decisions Made

- **Inline vs multiline detection:** Uses `VectorIndicator.Line == nextToken.Line` comparison instead of `SpaceBefore` — Key tokens have `SpaceBefore = false` regardless of whitespace, making the `SpaceBefore` approach unreliable for dict-valued inline vectors.
- **Root scalar for digits:** HUML spec confirms `123` is a valid root document. The old lexer-level throw was too aggressive; moved integer-as-key error to parser level.
- **Root inline dict shape:** Entries become direct top-level entries in the returned `HumlDocument` (not wrapped). Root inline list becomes `HumlDocument([HumlSequence(...)])`.

## Deviations from Plan

### Auto-fixed Issues

**1. [Rule 1 - Bug] Lexer incorrectly threw for digit tokens at line-start (root scalar inputs)**
- **Found during:** Task 2 (GREEN implementation)
- **Issue:** Lexer threw `HumlParseException("Keys must start with a letter")` for any digit at `_col == _lineIndent`, preventing root integer/float/hex scalars from parsing
- **Fix:** Removed special-case throw; digits now always produce Int/Float tokens. `ScanNumber` called with `spaceBefore = (_col > _lineIndent)` to correctly reflect inline vs root position
- **Files modified:** `src/Huml.Net/Lexer/Lexer.cs`, `tests/Huml.Net.Tests/Lexer/LexerTests.cs`
- **Verification:** All 135 tests pass; `Parse_RootIntegerScalar`, `Parse_FloatScalar`, `Parse_HexInt` all green
- **Committed in:** `2c5e33a` (Task 2 commit)

**2. [Rule 1 - Bug] `IsAtEndOfLine()` unreliable for inline Key-valued vectors**
- **Found during:** Task 2 (GREEN implementation)
- **Issue:** `Parse_InlineDict_ReturnsMappings` failed because Key tokens always have `SpaceBefore = false`; `IsAtEndOfLine()` using `!SpaceBefore` incorrectly treated inline `data:: a: 1, b: 2` as multiline
- **Fix:** Removed `IsAtEndOfLine()`. Refactored `ParseVector` to accept `indicatorLine int` parameter. Multiline if `next.Line != indicatorLine`
- **Files modified:** `src/Huml.Net/Parser/HumlParser.cs`
- **Verification:** `Parse_InlineDict_ReturnsMappings` now passes; all 24 parser tests green
- **Committed in:** `2c5e33a` (Task 2 commit)

**3. [Rule 1 - Bug] Version gating test used incorrect v0.1 input syntax**
- **Found during:** Task 2 (GREEN implementation)
- **Issue:** Test used `key::\n\`\`\`\nline one\n\`\`\`` which places backtick string at indent 0 under a `::` vector expecting content at indent 2 — throws "Ambiguous empty vector"
- **Fix:** Changed test input to `key: \`\`\`\nline one\n\`\`\`` using ScalarIndicator; lexer accepts backtick as String value in v0.1
- **Files modified:** `tests/Huml.Net.Tests/Parser/HumlParserTests.cs`
- **Verification:** `Parse_WithV01Options_PropagatesOptionsToLexer` passes in both TFMs
- **Committed in:** `2c5e33a` (Task 2 commit)

---

**Total deviations:** 3 auto-fixed (all Rule 1 - Bug)
**Impact on plan:** All auto-fixes necessary for spec correctness. Fixes 1 and 2 reveal lexer/parser design gaps not visible from Phase 3. No scope creep.

## Known Stubs

None. All parsed node types are fully wired from lexer tokens to AST nodes.

## Issues Encountered

- Key tokens having `SpaceBefore = false` regardless of preceding whitespace made `IsAtEndOfLine()` unreliable. The line-number comparison approach is more robust and matches the go-huml intent precisely.

## Next Phase Readiness

- `HumlParser` is complete and ready for Phase 5 Plan 2 (PARS-05 recursion depth limit tests) and Phase 6 (serializer/deserializer)
- The parser produces a fully typed `HumlDocument` AST with `HumlMapping`, `HumlSequence`, and `HumlScalar` nodes
- PARS-05 depth guard is already implemented in Plan 01 (depth guard exists in `ParseVector`, `ParseMultilineDict`, `ParseMultilineList`); Plan 02 adds dedicated depth tests

---
*Phase: 05-parser*
*Completed: 2026-03-21*
