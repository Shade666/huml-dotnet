# G2.1 — Backlog & Divergence Disposition

**Date:** 2026-06-10 (G2.1 of the [beta release programme](2026-06-10-beta-release-goals.md))
**Inputs:** the 13 open `999.x` entries from the archived `.planning/ROADMAP.md`, and the deferred divergences from [docs/spec-compliance-report.md](../spec-compliance-report.md), re-verified against the `go-huml` reference implementation (lexer.go/parser.go read directly).
**Dispositions:** `SHIPPED` (stale header — already delivered), `DO-FOR-BETA` (implemented in G2.2), `AUDIT` (G3 input), `DEFERRED` (post-beta, with rationale), `UPSTREAM` (spec/reference clarification issue, no code change).

## 1. The 999.x backlog

| Item | Title | Disposition | Evidence / rationale |
|------|-------|-------------|---------------------|
| 999.29 | Polymorphic (de)serialisation | **SHIPPED** (Phase 62) + **DEFERRED** remainder | `[HumlPolymorphic]`/`[HumlDerivedType]` shipped in 0.2.0-alpha.4. The remainder — a runtime `HumlOptions.PolymorphismOptions` config block — is post-beta: the attribute model plus the `IHumlTypeInfoResolver` seam covers the beta use cases; the options-block design surface is large and shouldn't land days before an API freeze. |
| 999.32 | Source generator | **SHIPPED** (Phases 66–69) | `Huml.Net.SourceGeneration` with `analyzers/dotnet/cs` layout shipped. |
| 999.33 | Number handling modes | **SHIPPED** (Phases 59–60) | `HumlOptions.NumberHandling` + per-member attribute shipped. |
| 999.35 | Per-member naming policy | **SHIPPED** (Phase 61) | `[HumlNamingPolicy]` shipped. |
| 999.39 | `HumlTypeInfo<T>` STJ parity | **SHIPPED** (Phases 64–65) | Properties collection, `HumlPropertyInfo`, `CreateObject` shipped. |
| 999.44 | Fuzz/property tests | **SHIPPED** (Phase 63) + **AUDIT** | `FuzzParserTests` (25 cases) shipped; the full SharpFuzz harness + property-based round-trips remain G3.3 as planned. |
| 999.45 | `IBufferWriter<char>` overload | **DEFERRED** | Post-beta. New public API surface immediately before the freeze for a high-throughput scenario no beta consumer has asked for; the pooled-StringBuilder path is allocation-bounded. |
| 999.59 | ScanComment simplification | **SHIPPED** (Phase 58) | Verified in current code. |
| 999.60 | Nullable default allocation | **SHIPPED** (Phase 58) | |
| 999.61 | Converters mutation hardening | **SHIPPED** (Phase 58) | `_frozenConverters` + `EffectiveConverters` verified at `HumlOptions.cs:256-285`. |
| 999.64 | Misleading comment fix | **SHIPPED** (Phase 58) | |
| 999.67 | Naming-policy XML doc | **SHIPPED** (Phase 58) | Re-check during G4.2 XML-doc pass. |
| 999.68 | Obsolete 3-arg ctor | **SHIPPED** (Phase 58) | `[Obsolete]` verified at `HumlDeserializeException.cs:41`. |

**Net result:** 11 of 13 entries were stale headers — the work shipped in Milestone 5. Nothing from the 999.x list blocks the beta; two items are formally deferred.

## 2. Spec-compliance divergences (re-verified against go-huml)

The go-huml evidence changed several G1.3 recommendations — in five cases the reference implementation shares our "divergence" from the spec text, which re-classifies it from a defect into an ecosystem-consistent behaviour needing a spec clarification instead.

### DO-FOR-BETA (implemented in G2.2)

| # | Item | Fix | go-huml evidence |
|---|------|-----|------------------|
| B1 | **S1 — quoted keys rejected in inline dicts** | Replace the lexer's column heuristic with go's same-line lookahead: after a closing quote, skip spaces; if `:` follows on the same line → `QuotedKey`, else `String` | go classifies via lookahead (lexer.go:390-404) and `parseInlineDict` accepts `TokenQuotedKey` (parser.go:492) |
| B2 | **L2 — `key::1` accepted (zero spaces before inline value)** | Enforce a space between `::` and an inline value; keep `key::# c` and newline forms valid | go's `parseVector` calls `skipRequiredSpace("after '::'")` (parser.go:410) |
| B3 | **L4 — `[ ]`/`{  }` accepted as empty vectors** | Require literal `[]`/`{}` | go uses `peekString("[]")` — literal match only (lexer.go:255-268) |
| B4 | **S6 (new) — comment after opening `"""` rejected** | Allow `(whitespace*, comment)` between the opening delimiter and the newline (also for v0.1 backticks) | Spec tokenizer `"\"\"\"", (whitespace*, comment)?, '\n'`; go validates via `validateRemaining` which permits comments (lexer.go:650-653) |
| B5 | **S4 — `key::  # c` rejected (multiple spaces before trailing comment)** | Permit any number of spaces before a `#` comment after `::`; keep "exactly one space" for values | Spec tokenizer `"::" (whitespace*, comment)?`; go's `consumeLine` permits spaces-then-comment |

### KEEP (ours matches the reference; spec text disagrees → UPSTREAM clarification)

| # | Item | Our behaviour | go-huml |
|---|------|---------------|---------|
| L3 | Extra spaces after `-` accepted | lenient | lenient — extra spaces silently skipped (`scanToken` space loop) |
| L5 | Bare `- []` / `- {}` accepted | lenient | lenient — `parseListItemValue` → `parseInlineValue` accepts empty vectors |
| L6 | Under-indented `"""` content accepted | lenient | lenient — strips `keyIndent+2` only when present (lexer.go:712-716), no minimum enforcement |
| L7 | `0X`/`0O`/`0B`/uppercase `E` accepted | lenient | lenient — `case 'x','X'` etc. (lexer.go:570-575, 588) |
| S2 | Leading newline before `%HUML` rejected | strict | strict — `lineNum == 1 && pos == 0` required; the EBNF's `(NEWLINE? huml_version …)` appears to be a grammar bug |
| S3 | `"""` content line at indent ≠ key throws | strict | strict — identical error (lexer.go:680-686) |
| L8 | Comment after `%HUML vX.Y.Z` accepted | lenient | (directive scanning shares the comment-permitting line validation) |
| L1 | CRLF accepted (ratified 2026-06-10) | lenient — **intentional, documented divergence**; go rejects | n/a — ours is the deliberate outlier, documented in G4 |

### AUDIT (G3 inputs, unchanged)

- Two's-complement wrap for 64-bit hex/octal/binary literals (`0xFFFFFFFFFFFFFFFF` → `-1`) vs decimal overflow error — consistency decision.
- BOM policy (currently "Unexpected character"; spec silent).
- L9 / S5 — inline `#` without preceding space; bare `#` at EOL (go accepts `#\n`, grammar says `"# "`): fold into the G3 review with the upstream answer if one lands first.
- One unreproducible single-test failure was observed on net9.0 during a parallel three-TFM run on 2026-06-10 (passed on immediate re-run and on two subsequent full runs). Suspected allocation-measurement sensitivity under parallel execution — G3 should review the GC-measuring tests for parallel-run robustness.

### UPSTREAM (issues to file, no code change)

1. EBNF `huml_document = (NEWLINE? huml_version …)` vs both implementations requiring line 1 (S2).
2. Tokenizer digit-class vs prose for uppercase base prefixes/exponent (L7).
3. `multiline_list_item` production missing the bare `- []`/`- {}` form both implementations accept (L5).
4. Prose "content block must be indented one level" vs both implementations' leniency (L6); prose "characters are treated literally" vs both implementations erroring on mis-indented `"""` content lines (S3).
5. go-huml accepts bare `#` at EOL; grammar `comment = "# "` says error (S5) — reference/spec mismatch.
6. Spec is silent on BOM and on spaces before inline `#` (L9).

## 3. G2.2 implementation order

B3 (trivial) → B4 (small) → B5+B2 (same scanner region) → B1 (lookahead rework, largest blast radius — last so the suite is green before it lands). Each TDD: failing test first, then fix, full suite between items.
