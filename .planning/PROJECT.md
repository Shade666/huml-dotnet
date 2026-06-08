# Huml.Net

## What This Is

`Huml.Net` is a .NET library for parsing, serialising, and deserialising HUML (Human-oriented Markup Language) documents. HUML is a strict, human-readable serialisation format — a safer alternative to YAML with unambiguous syntax, mandatory string quoting, explicit type literals, and comment support. The public API mirrors `System.Text.Json` conventions so .NET developers encounter minimal friction.

## Core Value

Full HUML spec compliance (v0.1 + v0.2), validated against the shared `huml-lang/tests` test suite, with zero external runtime dependencies and a `System.Text.Json`-style API that .NET developers already know.

## Requirements

### Validated

All 143 v1 requirements are complete across four shipped milestones. See `.planning/milestones/v0.2.0-alpha.3-REQUIREMENTS.md` for the full archived requirements with traceability.

**Milestone 1 — v0.1.0-alpha.1 (Phases 1–8):**
- [x] Multi-TFM solution (`netstandard2.1;net8.0;net9.0;net10.0`) with CI, SourceLink, and OIDC NuGet Trusted Publishing — Validated Phase 1
- [x] Version-aware options: `HumlOptions`, `VersionSource`, `UnknownVersionBehaviour` — Validated Phase 2
- [x] Single-pass `ReadOnlySpan<char>` lexer with version-gated tokenisation rules — Validated Phase 3
- [x] Immutable AST node hierarchy: `HumlNode`, `HumlDocument`, `HumlMapping`, `HumlSequence`, `HumlScalar` — Validated Phase 4
- [x] Recursive-descent parser covering full HUML v0.1 and v0.2 grammar — Validated Phase 5
- [x] Reflection-based `HumlSerializer` and `HumlDeserializer` with attribute-driven mapping and declaration-order emission — Validated Phase 6
- [x] `System.Text.Json`-style static `Huml` facade; all `huml-lang/tests` fixtures passing in CI — Validated Phase 7
- [x] `HumlInlineMapping` AST node; O(1) property-lookup dictionary; hot-path allocation optimisations (AppendEscapedString, IndentCache, DefaultValue caching) — Validated Phases 07.2–07.15
- [x] Unicode/RTL error messages; `fixtures/extensions/` infrastructure — Validated Phase 07.3
- [x] Quoted key emission for non-bare-key dictionary keys (D-08 resolved) — Validated Phase 07.4
- [x] Inline serialisation via `CollectionFormat` and `[HumlProperty(Inline = InlineMode...)]` — Validated Phase 07.5
- [x] `HumlOptions.Default` header-aware; `LatestSupported` pinned v0.2; `AutoDetect` alias — Validated Phase 07.8
- [x] `MaxRecursionDepth` default 64, valid range [1, 1024] — Validated Phase 07.9
- [x] NuGet-publishable with complete metadata, README, CHANGELOG, docs/ guides — Validated Phase 07.7 / Phase 8

**Milestone 2 — v0.2.0-alpha.1 (Phases 9–14):**
- [x] `Line` and `Column` on all AST nodes; `HumlDeserializeException` carries real source positions — Validated Phase 9
- [x] `HumlNamingPolicy` with `KebabCase`, `SnakeCase`, `CamelCase`, `PascalCase`; symmetric serialise/deserialise — Validated Phase 10
- [x] Enum serialisation as quoted strings with `[HumlEnumValue]` and naming-policy transforms — Validated Phase 11
- [x] `HumlConverter<T>` abstract base, `[HumlConverter]`, `HumlOptions.Converters`; full priority chain — Validated Phase 12
- [x] `Huml.Populate<T>` (string and span overloads) for overlay deserialisation — Validated Phase 13

**Milestone 3 — v0.2.0-alpha.2 (Phases 15–28):**
- [x] `[RequiresUnreferencedCode]` / `[RequiresDynamicCode]` on all reflection-using public API; `<IsTrimmable>true</IsTrimmable>` — Validated Phase 15
- [x] `HashSet<T>`, `ISet<T>`, `IReadOnlySet<T>` deserialisation — Validated Phase 16
- [x] Native `DateTime`, `DateTimeOffset`, `TimeSpan`, `DateOnly`, `TimeOnly` round-trips — Validated Phase 17
- [x] `[ThreadStatic]` StringBuilder pooling in serialiser; no per-call allocation — Validated Phase 18
- [x] `HumlIgnoreCondition` flags enum; `[HumlIgnoreDefaults]`; `HumlOptions.DefaultIgnoreCondition` — Validated Phase 19
- [x] `HumlOptions.ValidateDuplicateKeysOnWrite` — Validated Phase 20
- [x] `HumlDocument.DetectedVersion`; version-preserving round-trip — Validated Phase 21
- [x] `[HumlExtensionData]` capturing unmatched HUML keys — Validated Phase 22
- [x] `[HumlConstructor]` + init-only setter support; constructor-parameter binding — Validated Phase 23
- [x] `[HumlRequired]` / C# `required` modifier enforcement — Validated Phase 24
- [x] `IHumlTypeInfoResolver` / `HumlTypeInfo<T>` seam for future source generator — Validated Phase 25
- [x] `Lexer` and `HumlParser` as `ref struct`; genuine zero-copy span deserialisation — Validated Phase 26
- [x] `ResolvePropertyValue` extracted from Deserialize/Populate dispatcher duplication — Validated Phase 27.5
- [x] `ConverterResolutionCache` migrated to `HumlOptions` instance field — Validated Phase 27.6

**Milestone 4 — v0.2.0-alpha.3 (Phases 29–44, 57):**
- [x] `SortedSet<T>` deserialisation; checked casts in `CoerceScalar`; `IEnumerable<T>` interface lookup cached — Validated Phases 29–31
- [x] `HumlSerializeException` enriched with property name and containing type — Validated Phase 32
- [x] `HumlOptions.MakeReadOnly()` / `IsReadOnly`; pre-built instances frozen at static initialisation — Validated Phase 33
- [x] `HumlOptions.LatestSupportedAutoDetect` preset — Validated Phase 34
- [x] `HumlOptions.UnmappedMemberHandling` (Skip/Disallow) — Validated Phase 35
- [x] `HumlOptions.Strict` maximum-strictness preset — Validated Phase 36
- [x] Concurrency test for `PropertyDescriptor` cache — Validated Phase 37
- [x] Document-size limitation documented in `HumlOptions` XML docs and `docs/options-reference.md` — Validated Phase 38
- [x] 14 correctness fixes from CODEBASE-REVIEW.md (CR-01–06, WR-01/06/07/08/09/11/12, IN-01/02/04) — Validated Phases 40–44

## Current Milestone: v0.2.0-alpha.4 — V3 Backlog Clear

**Goal:** Ship all 12 V3 backlog items — polymorphic deserialisation, source-generator seam completion, full `HumlTypeInfo<T>` parity, `NumberHandling` modes, per-member naming-policy override, fuzz tests, and a batch of small quality/documentation fixes.

**Target features:**
- Polymorphic deserialisation (`[HumlDerivedType]`, discriminator-based dispatch)
- `Huml.Net.SourceGeneration` — Roslyn incremental source generator, new NuGet package
- Full `HumlTypeInfo<T>` parity (`Properties`, `CreateObject`, callbacks) — requires source generator
- `HumlOptions.NumberHandling` (AllowReadingFromString / WriteAsString / AllowNamedFloatingPointLiterals)
- `[HumlNamingPolicy(typeof(...))]` per-member naming-policy override attribute
- Fuzz / property-based parse tests (adversarial grammar boundary inputs)
- Quality/doc batch: `ScanComment` simplification, `Nullable<T>` default skip, `Converters` hardening, misleading-comment fix, `HumlNamingPolicy` digit-boundary doc, `HumlDeserializeException` 3-arg ctor `[Obsolete]`

### Active

- [ ] **POLY-01:** Polymorphic deserialisation — `[HumlDerivedType(Type, discriminator)]` attribute and discriminator-based dispatch
- [ ] **POLY-02:** `HumlOptions.PolymorphismOptions` configuration block (discriminator key, ignore-on-collision, unknown fallback)
- [ ] **GEN-01:** `Huml.Net.SourceGeneration` Roslyn incremental source generator emitting compiled `HumlTypeInfo<T>` per registered type
- [ ] **GEN-02:** `HumlSerializerContext` base class with `GetTypeInfo<T>()` mirroring STJ generic form
- [ ] **GEN-03:** Generator integrates with `HumlOptions.TypeInfoResolver` seam (Phase 25)
- [ ] **TI-01:** `HumlTypeInfo<T>.Properties` collection (`HumlPropertyInfo<T>` per-property metadata)
- [ ] **TI-02:** `HumlTypeInfo<T>.CreateObject` factory delegate
- [ ] **TI-03:** On-serialising / on-deserialised callbacks on `HumlTypeInfo<T>`
- [ ] **NUM-01:** `HumlOptions.NumberHandling` flags enum with `AllowReadingFromString`, `WriteAsString`, `AllowNamedFloatingPointLiterals`
- [ ] **ATTR-01:** `[HumlNamingPolicy(typeof(ConcretePolicy))]` per-member naming-policy override attribute
- [ ] **TEST-01:** Fuzz / property-based parse tests — 20+ adversarial inputs, no `StackOverflowException` / `NullReferenceException`
- [ ] **QUAL-01:** Simplify `ScanComment` double-condition (999.59)
- [ ] **QUAL-02:** Skip `Activator.CreateInstance` for `Nullable<T>` defaults (999.60)
- [ ] **QUAL-03:** Harden `HumlOptions.Converters` against post-resolution mutation (999.61)
- [ ] **QUAL-04:** Fix misleading comment in `InferScalarOrInlineListRootType` (999.64)
- [ ] **QUAL-05:** Document digit-as-word-boundary in `HumlNamingPolicy` XML doc (999.67)
- [ ] **QUAL-06:** Mark `HumlDeserializeException` 3-argument constructor `[Obsolete]` (999.68)

### Out of Scope

- Streaming / `IAsyncEnumerable` parsing — complexity not justified for config-file use case
- `IBufferWriter<char>` output overload — V4; deferred (999.45)
- Schema validation — outside HUML spec scope
- HUML → JSON / YAML round-trip converters — distinct utility concern
- `Huml.Net.Linting` package — v2+ concern; package boundary established in architecture; no logic accretes into core parser
- .NET Framework support — `netstandard2.1` compat floor requires `Span<T>` in public API
- Source generator / AOT support for previous milestones — `IHumlTypeInfoResolver` seam (Phase 25) was the pre-requisite; source generator now in scope for this milestone
- Streaming / `IAsyncEnumerable` parsing — complexity not justified for config-file use case
- Schema validation — outside HUML spec scope
- HUML → JSON / YAML round-trip converters — distinct utility concern
- `Huml.Net.Linting` package — v2+ concern; package boundary established in architecture; no logic accretes into core parser
- .NET Framework support — `netstandard2.1` compat floor requires `Span<T>` in public API

## Context

- **Reference implementation:** [`go-huml`](https://github.com/huml-lang/go-huml) (primary), [`huml-rs`](https://github.com/huml-lang/huml-rs) (secondary)
- **HUML spec:** [huml.io](https://huml.io)
- **Shared test suite:** [`huml-lang/tests`](https://github.com/huml-lang/tests) — consumed as git submodules pinned to per-version tags (`v0.1`, `v0.2`)
- **Architecture mirrors go-huml:** single-pass `Lexer` (ref struct) → token stream → recursive-descent `HumlParser` (ref struct) → `HumlNode` AST → `HumlSerializer` / `HumlDeserializer` via reflection
- **TDD discipline:** shared suite fixtures drive Red/Green cycle before any production code
- **Properties in declaration order** (not alphabetically) — .NET convention differs from go-huml's alphabetical sort
- **Current state:** 4 milestones shipped (v0.1.0-alpha.1 through v0.2.0-alpha.3); 143/143 v1 requirements complete; all planning artifacts archived to `.planning/milestones/`; Milestone 5 (v0.2.0-alpha.4) in planning

## Constraints

- **Tech stack:** C# 13, `netstandard2.1;net8.0;net9.0;net10.0` — `netstandard2.1` as compat floor gives `ReadOnlySpan<char>` in public API and covers .NET Core 3.x / .NET 5–10; deliberately excludes .NET Framework
- **Runtime dependencies:** Zero — no external packages in the main library; test-only deps are `xUnit` + `AwesomeAssertions`
- **Licence:** MIT
- **Author:** Richard (Radberi / primeBeri)

## Key Decisions

| Decision | Rationale | Outcome |
|---|---|---|
| Multi-target `netstandard2.1;net8.0;net9.0;net10.0` | `Span` in public API requires ns2.1+; multi-targeting lets modern consumers get optimised TFM builds via NuGet resolution | ✓ Working well — CI confirms all 4 TFMs green |
| Drop .NET Framework support | `netstandard2.1` compat floor is required for `ReadOnlySpan<char>` overload | ✓ Correct — no Framework requests from users |
| Single parser code path with version gates | No forked `Lexer`/`Parser` classes per spec version — explicit `>=` branch points make divergence searchable | ✓ Maintained through all 4 milestones |
| Properties emitted in declaration order | .NET convention; alphabetical sorting (go-huml) would surprise C# consumers | ✓ Validated via round-trip tests |
| `Huml.Net.Linting` is a separate package | Parser has zero opinions on style/advisories; linting logic must never accrete into core | ✓ No linting in core after 4 milestones |
| v0.1 + v0.2 both in v1 scope | Support window is last 3 minor versions; v0.1 remains supported until v0.3 ships | ✓ Both versions pass fixture suite |
| `SpecVersionPolicy` constants as code | `HumlUnsupportedVersionException` references them directly — error message stays accurate without manual updates | ✓ Working as designed |
| `HumlInlineMapping` AST semantic split | Disambiguates inline/empty dict blocks from root `HumlDocument`; cleaner deserialiser dispatch | ✓ Phase 07.2 decision validated by Phase 07.6 round-trip tests |
| `HumlOptions.Default` header-aware | Ignoring `%HUML` header by default silently misclassifies v0.1 documents | ✓ Phase 07.8 decision — correct default behaviour |
| `MaxRecursionDepth` default 64, ceiling 1024 | Matches `System.Text.Json` convention; bounds adversarial inputs; 512 was unnecessarily generous | ✓ Phase 07.9 decision validated |
| `ref struct` Lexer and Parser | Genuine zero-copy span deserialisation; eliminates intermediate `string` allocation | ✓ Phase 26 decision — allocation tests passing |
| `ResolvePropertyValue` extracted | `// NOTE: keep in sync` comment is a smell; single-entry-point for converter dispatch | ✓ Phase 27.5 decision — cleaner than duplication |
| `ConverterResolutionCache` on `HumlOptions` instance | Static dict keyed on options hash code leaks permanently (one entry per unique options reference) | ✓ Phase 27.6 fix — GC'd with options instance |
| `HumlOptions.Strict` preset | Composites all validation toggles; mirrors STJ .NET 10 `JsonSerializerOptions.Strict` | ✓ Phase 36 decision — `MakeReadOnly()` applied at static init |

## Evolution

This document evolves at phase transitions and milestone boundaries.

**After each phase transition** (via `/gsd-transition`):
1. Requirements invalidated? → Move to Out of Scope with reason
2. Requirements validated? → Move to Validated with phase reference
3. New requirements emerged? → Add to Active
4. Decisions to log? → Add to Key Decisions
5. "What This Is" still accurate? → Update if drifted

**After each milestone** (via `/gsd:complete-milestone`):
1. Full review of all sections
2. Core Value check — still the right priority?
3. Audit Out of Scope — reasons still valid?
4. Update Context with current state

---
*Last updated: 2026-06-07 — Milestone 5 started (v0.2.0-alpha.4 planning). 143/143 v1 requirements validated.*
