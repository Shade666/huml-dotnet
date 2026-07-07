---
id: doc-001
title: 'Comprehensive Review 2026-07-07 — code quality, architecture, STJ parity'
type: other
created_date: '2026-07-07 08:13'
tags:
  - review
  - stj-parity
  - architecture
---
# Comprehensive Review — 2026-07-07

Full-repository review at 0.2.0-beta.2 (commit f90042f) covering code quality, architecture, and System.Text.Json feature parity. Three independent review passes were run; load-bearing findings were verified in code (and, for H1, by runtime repro) before being actioned.

**Baseline:** 1,329 tests pass on net8.0/net9.0/net10.0, zero failures. Working tree clean; no open GitHub issues; dependabot PRs #31 and #28 open.

## Headline findings

### Code quality (health: good)

- **H1 (High, verified):** Overridden and `new`-shadowed properties serialise twice on the reflection path — `PropertyDescriptor.BuildDescriptors` collects `DeclaredOnly` properties per type in the inheritance chain with no name de-duplication, producing invalid HUML that the library's own parser rejects on round-trip (`Duplicate key`). The source generator already dedupes (derived-most wins), so the two paths diverge. → **TASK-019 (m-0, gates stable)**
- **M2 (verified):** `Deserialize<int>("true")` silently returns `1` — bool scalars fall through to an ungated `Convert.ChangeType`. → folded into **TASK-005**
- **M3:** Two parallel emission engines in `HumlSerializerImpl` (object emission vs AST re-emission for extension data) plus duplicated `Serialize` overload setup and copy-pasted unknown-key blocks in the deserialiser. → **TASK-027**
- **M4/L5/L7:** MetadataToken ordering assumption; CRLF lexer error-message inconsistency; `# ` empty-comment edge. → **TASK-029**
- **L6:** `decimal.MaxValue` serialises to output the parser cannot re-read. → folded into **TASK-005**
- Positives: invariant culture consistently applied at every format site; sound thread-safety (ConcurrentDictionary caches, [ThreadStatic] state); diligent exception translation; recursion depth guarded on both paths.

### Architecture (sound overall; one central issue)

- **Central finding:** the extensibility story is two incoherent worlds. The reflection path carries the full feature set; the resolver/source-gen path iterates minimal `HumlPropertyInfo` metadata and bypasses required/extension-data/Disallow/OmitIfDefault — and matches keys with a *different string comparer* (OrdinalIgnoreCase at `HumlDeserializer.cs:370` vs Ordinal at `PropertyDescriptor.cs:231`; constructor params OrdinalIgnoreCase at `:488`). Verified live. → **TASK-020, TASK-026** (with TASK-008 as the enforcement layer)
- No common exception base — `catch (HumlException)` is impossible; cheap now, costly post-1.0. → **TASK-021**
- Source generator emits no diagnostics for constructs it silently drops. → **TASK-025**
- Version gating: only two behavioural gates exist (both lexer); strategy untested against real forking pressure before v0.3. → **TASK-031 (new milestone m-4)**
- Performance ceilings: the complete binding path uses reflection-invoke on the hot loop (the fast delegate path is the incomplete one) → **TASK-030**; UTF-16-only output funnel noted on **TASK-012**.
- Layering, AST design, converter model, and test architecture rated sound.

### STJ feature parity (~80–85% of the surface a HUML library should want)

Present and faithful: options bag, full attribute set (naming, ignore, converters + factory, polymorphism, extension data, number handling, required, constructor), records/init/required binding, read-only AST DOM, Populate, working source-gen resolver path, DateOnly/TimeOnly, Strict preset, MakeReadOnly.

New gaps actioned: **PropertyNameCaseInsensitive** (High — silent bind failure on hand-edited files; TASK-020), **DictionaryKeyPolicy** (TASK-022), **[HumlPropertyOrder]** (TASK-023), **non-string dictionary keys** (TASK-024), low batch — byte[] Base64 / IgnoreReadOnlyProperties / NewLine (TASK-028).

Explicitly deferred, recorded on TASK-028 so they are not re-raised: IncludeFields/[HumlInclude], PreferredObjectCreationHandling, ReferenceHandler.Preserve (recommend declaring anti-feature). Confirmed standing anti-features: streaming/async, mutable DOM, [JsonPropertyName] interop, JSON Schema export, PipeReader/ASP.NET integration. Spec-fixed N/A: comment handling, trailing commas, Encoder.

## Changes made during this review

1. **Code fix:** stale XML doc on `HumlOptions.TypeInfoResolver` corrected (claimed the resolver "is not yet consumed"; it has been consumed by both serialiser and deserialiser since 0.2.0-alpha.4). CHANGELOG `[Unreleased]` updated. Build clean, zero warnings.
2. **New milestone:** m-4 "HUML v0.3 readiness".
3. **New tasks:** TASK-019 (m-0, High); TASK-020 (High), TASK-021…TASK-029 (m-1); TASK-030 (m-2); TASK-031 (m-4).
4. **Edited tasks:** TASK-004 (beta.2 + dependabot + TASK-019 gate note), TASK-005 (two new ACs: bool coercion, decimal round-trip), TASK-008 (correction: comparer divergence is live, not fixed; links to TASK-020/026), TASK-012 (UTF-8 writer design note).

## Suggested sequencing

1. **m-0 before stable:** TASK-019 (round-trip-breaking bug), merge dependabot #31/#28, then TASK-003/TASK-004 as planned.
2. **m-1 order:** TASK-008 → TASK-020 (comparer + case-insensitivity) → TASK-026 (metadata unification) unlock everything else; TASK-005 needs its design agreed first since it is a behaviour change that cannot ship on the 0.2.x bug-fix line.
3. **m-2:** TASK-026 feeds TASK-030 (compiled delegates) and TASK-012/013.
4. **m-4:** TASK-031 only when HUML v0.3 becomes concrete.
