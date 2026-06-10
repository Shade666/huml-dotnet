# HUML Spec Compliance Report — Huml.Net

**Date:** 2026-06-10 (G1.3 of the [beta release programme](plans/2026-06-10-beta-release-goals.md))
**Spec sources:** [HUML v0.2.0](https://huml.io/specifications/v0-2-0/) and [v0.1.0](https://huml.io/specifications/v0-1-0/) (verbatim markdown from `huml-lang/website`), cross-checked against the `go-huml` reference implementation where the spec is silent or ambiguous.
**Method:** three parallel audit passes (lexical / scalar / structural rules) enumerating every normative rule, each verified against (a) upstream + extension fixture coverage and (b) implementation code, with 48 runtime probes for anything not statically resolvable.
**Result:** 9 deviations **fixed** in this sweep; 12 minor divergences **deferred with rationale** (below); everything else verified compliant. Suite: 1216/1216 tests green on net8.0/net9.0/net10.0.

---

## 1. Deviations fixed in this sweep

| # | Rule (spec) | Was | Now |
|---|------------|-----|-----|
| F1 | Parse failures must surface as `HumlParseException` (API contract) | `key: 0x` / `0b2` / `0o8` leaked `System.FormatException` from `Huml.Parse` | Lexer requires ≥1 digit after a base prefix; `ParseInt` catches `FormatException`/`ArgumentException` as defence in depth |
| F2 | Tokenizer: list items are `"- "` (dash-space); `('+'\|'-')? digit+` is a number | `-5` at document root parsed as a **one-element list**; `list::\n  -1` accepted | Dash at line start only lexes as a list item when followed by space/EOL; root `-5`/`-inf` are scalars; `-1` as a list item is a parse error |
| F3 | v0.1 §Strip spaces: `"""` strips **all** leading/trailing whitespace per content line | v0.1 documents got v0.2 preserve semantics (no version gate) | `ScanTripleQuoteMultiline` gates on `>= V0_2`; v0.1 trims each content line |
| F4 | Tokenizer digit classes: hex `['0'-'9' 'A'-'F' '_']`, octal/binary/exponent include `_` | `0xCAFE_BABE`, `1e1_0` rejected with "Unexpected character '_'" | Underscores accepted (and ignored) in hex/octal/binary digits and exponent digits |
| F5 | `bool = "true" \| "false"`, `null = "null"`, `nan`, `inf` — lowercase literals; go-huml matches with `bytes.Equal` (case-sensitive) | `TRUE`, `Null`, `NaN`, `INF` accepted case-insensitively | Keyword matching is `Ordinal`; uppercase/mixed-case forms are unquoted-string parse errors |
| F6 | §Spaces: "Trailing spaces are not allowed on any line, including … comment-only lines" (go-huml enforces) | Trailing spaces after comment text accepted | `ScanComment` rejects a space immediately before EOL (including the bare `# ` marker, matching go-huml) |

Regression coverage: `tests/Huml.Net.Tests/Lexer/SpecComplianceFixTests.cs` (35 tests) plus 10 new extension fixture rows in `fixtures/extensions/{v0.1,v0.2}/assertions/gaps.json`. Five pre-existing extension rows asserting case-insensitive keywords (`bool_true_uppercase` etc.) were **flipped to `error: true`** — they contradicted both the spec and go-huml and would have been rejected upstream.

## 2. Deferred divergences (documented, not gating the beta)

### Leniencies — Huml.Net accepts input the spec rejects (superset behaviour)

| # | Behaviour | Spec position | Recommendation |
|---|-----------|---------------|----------------|
| L1 | **CRLF / lone CR accepted as line breaks** (deliberate normalisation in `PeekCurrentChar`/`AdvancePastNewline`) | "Line breaks: Unix-style (`\n`)" | **Needs maintainer ratification.** Recommend keeping (Windows/.NET ergonomics) and documenting as an intentional, opt-out-able divergence — or aligning strictly before 1.0. go-huml rejects CRLF (trailing `\r` reads as trailing whitespace). |
| L2 | `key::1`, `key::"x"` etc. accepted (inline vector with zero spaces after `::`) | Tokenizer: `":: "` literal | Fix alongside `Token.SpaceBefore` enforcement (the parser never reads it); route to G3 review |
| L3 | Two-plus spaces after `-` accepted (`-  1`) | "Only a single space … after the indicators" | Same `SpaceBefore` work as L2 |
| L4 | `[ ]` / `{  }` accepted as empty vectors | Grammar literals `"[]"` / `"{}"` | Trivial fix in `ScanEmptyCollection`; bundle with L2/L3 |
| L5 | Bare `- []` / `- {}` list items accepted (grammar requires `- :: []`) | `multiline_list_item` production | Verify against go-huml before changing; arguably a spec gap |
| L6 | Under-indented `"""` content lines accepted | "content block must be indented by one level (2 spaces)… minimum required indentation" | Enforce minimum indent in `ScanTripleQuoteMultiline` |
| L7 | `0X`/`0O`/`0B` prefixes and uppercase `E` exponent accepted | Lowercase forms only (`0x`, `e`) | Trivial strictness fix; low user impact |
| L8 | Comment after `%HUML vX.Y.Z` on the directive line accepted | Grammar has no comment slot in `huml_version`, but general comment skipping makes this ambiguous | Leave; raise as spec clarification upstream |
| L9 | Inline `#` not required to be preceded by a space (`key: 1# c` parses) | Prose says comments "can have preceding spaces" (not "must"); upstream fixture names suggest error intent but the rows are masked by unrelated errors | Raise upstream for clarification; align with the answer |

### Strictnesses — Huml.Net rejects input the spec accepts

| # | Behaviour | Spec position | Recommendation |
|---|-----------|---------------|----------------|
| S1 | Quoted keys rejected in **inline** dicts (`key:: "a b": 1`, root `a: 1, "b": 2`) | `dict_key = simple_key \| STRING` | Real defect; requires reworking the lexer's quoted-key column heuristic (`ScanDoubleQuoteToken`). Schedule as its own work item in G2/G3 |
| S2 | A single blank line before `%HUML` rejected | `huml_document = (NEWLINE? huml_version NEWLINE)? …` | Small lexer fix (`_line == 1` gate); low priority |
| S3 | Content line starting with `"""` at indent ≠ key indent throws instead of being literal content | "no escaping is necessary; characters are treated literally" | Fix when L6 is done (same scanner) |
| S4 | `key::  # c` (two spaces before a trailing comment on a multiline-vector line) rejected | Tokenizer: `"::" (whitespace*, comment)?, '\n'` | Minor; bundle with L2 |
| S5 | Bare `#` (no trailing space, at EOL) rejected | Grammar `comment = "# ", …` sides with us; **go-huml accepts** `#\n` | Keep; flag to upstream as a reference-impl/spec mismatch |

### Implementation-defined behaviour (documented, spec is silent)

- **Integer width:** int64; decimal overflow throws `HumlParseException`. **Hex/octal/binary wrap via two's complement** (`0xFFFFFFFFFFFFFFFF` → `-1`) — inconsistent with the decimal path; recommend aligning (overflow error) in G3. (Probe S15.)
- **BOM:** a leading U+FEFF is rejected ("Unexpected character"). Spec is silent. go-huml behaviour unverified; consider tolerating and stripping.
- **`\/` escape:** accepted in single-line strings. Not in the spec's escape table, but required by upstream fixture `quoted_string_with_all_escapes`.

## 3. Compliance checklist summary

Verdicts after this sweep's fixes. "Fixture" = covered by upstream or extension assertion rows; "code+test" = verified by implementation reading plus unit tests/probes.

| Spec section | Rules checked | Compliant | Deferred divergences |
|--------------|--------------|-----------|----------------------|
| Encoding & basic structure | 5 (UTF-8, `\n`, blank lines, directive, directive syntax) | 4 | L1 (CRLF), S2 (leading newline), L8 (directive comment) |
| Indentation | 2 (2-space levels, no tabs) | 2 | — |
| Spaces | 5 (trailing spaces, `# `, single space after indicators, `::`+newline, comma rules) | 5 (F6 fixed) | L2/L3/S4 (space-count enforcement gaps) |
| Comments | 3 | 3 (F6) | L9 (space before inline `#`), S5 (bare `#`) |
| Keys & values | 8 (case-sensitivity, bare-key charset, quoting, `:`/`::`, root inference) | 8 | S1 (quoted keys in inline dicts) |
| Scalars — strings | 7 (canonical Unicode, escapes, no `\u`, raw newline, unterminated) | 7 | — |
| Scalars — multiline strings | 7 (open/close discipline, strip indent, literal content, v0.1 gate) | 6 (F3 fixed) | L6/S3 (indent edge cases) |
| Scalars — numbers | 13 (int/float/exponent/hex/octal/binary/underscores/inf/nan/overflow) | 12 (F1, F2, F4, F5 fixed) | L7 (uppercase prefixes); two's-complement note |
| Scalars — bool/null | 3 | 3 (F5 fixed) | — |
| Vectors — inline | 9 (commas, scalar-only, no nesting, duplicates, `[]`/`{}`) | 8 | L4 (`[ ]`), S1 |
| Vectors — multiline | 9 (dash rules, nesting, exact indent, no mixing) | 8 (F2 fixed) | L5 (`- []`) |
| Document root | 5 (inference, no indent, single root, empty invalid) | 5 (F2 fixed root `-5`) | — |
| Grammar/EBNF cross-checks | 6 | 5 | S2 |

**v0.1 differences:** the only spec deltas are multiline-string syntax (backticks = preserve, `"""` = strip). The backtick gate existed; the `"""` strip gate was added in this sweep (F3). All other rules verified identical across versions; no further gates required.

## 4. Inputs to later goals

- **G2.1 disposition:** L2+L3+L4+S4 form one natural "indicator space enforcement" work item (introduce parser-side `Token.SpaceBefore` checks); S1 is its own item; L6+S3 one multiline-indent item; L7 trivial.
- **G3 audit:** two's-complement wrap (silent data change), BOM policy, CRLF decision ratification, and the fuzzing corpus should include every probe input from this sweep (preserved in the fixture rows and `SpecComplianceFixTests`).
- **Upstream contributions:** the extension fixture rows staged here; spec clarification requests for L8/L9/S5/L5; go-huml divergence report for `#\n`.
