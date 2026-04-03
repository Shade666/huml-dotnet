# Huml.Net

## What This Is

`Huml.Net` is a .NET library for parsing, serialising, and deserialising HUML (Human-oriented Markup Language) documents. HUML is a strict, human-readable serialisation format — a safer alternative to YAML with unambiguous syntax, mandatory string quoting, explicit type literals, and comment support. The public API mirrors `System.Text.Json` conventions so .NET developers encounter minimal friction.

## Core Value

Full HUML spec compliance (v0.1 + v0.2), validated against the shared `huml-lang/tests` test suite, with zero external runtime dependencies and a `System.Text.Json`-style API that .NET developers already know.

## Requirements

### Validated

- [x] Version-aware options: `HumlOptions`, `VersionSource`, `UnknownVersionBehaviour` — Validated in Phase 02: Versioning Foundation
- [x] Structured error types: `HumlUnsupportedVersionException` with spec-policy-wired message — Validated in Phase 02: Versioning Foundation
- [x] `[Obsolete]` deprecation process for spec versions exiting support window — Validated in Phase 02: Versioning Foundation
- [x] Structured error types with line/column: `HumlParseException` — Validated in Phase 03: Lexer and Token Types
- [x] Single-pass lexer with full HUML v0.2 tokenisation rules, version-gated backtick multiline — Validated in Phase 03: Lexer and Token Types
- [x] Immutable AST node hierarchy: `HumlNode` (abstract), `HumlDocument`, `HumlMapping`, `HumlSequence`, `HumlScalar` sealed records — Validated in Phase 04: AST Node Hierarchy
- [x] `ScalarKind` enum with 7 members (String, Integer, Float, Bool, Null, NaN, Inf) — Validated in Phase 04: AST Node Hierarchy
- [x] Recursive-descent parser producing `HumlDocument` AST from Lexer token stream (PARS-03) — Validated in Phase 05: Parser
- [x] `HumlOptions` propagated to Lexer; `>=` version gate convention established in parser (PARS-04) — Validated in Phase 05: Parser
- [x] Configurable recursion depth guard (`MaxRecursionDepth = 512`) preventing `StackOverflowException` (PARS-05) — Validated in Phase 05: Parser
- [x] Attribute-driven property mapping: `[HumlProperty]` (rename + OmitIfDefault), `[HumlIgnore]` — Validated in Phase 06: Attributes and Serializer/Deserializer
- [x] `HumlSerializer` — .NET objects → HUML text, declaration-order, version header, all CLR scalar types — Validated in Phase 06: Attributes and Serializer/Deserializer
- [x] `HumlDeserializer` — HUML text → typed .NET objects, full collection dispatch, diagnostic exceptions — Validated in Phase 06: Attributes and Serializer/Deserializer

- [x] Full HUML v0.2.0 spec compliance validated against `huml-lang/tests` shared suite — Validated in Phase 07: Static Entry Point and Shared Fixture Compliance
- [x] HUML v0.1 support within the 3-version rolling support window — Validated in Phase 07: Static Entry Point and Shared Fixture Compliance
- [x] `System.Text.Json`-style static API: `Huml.Serialize<T>()` / `Huml.Deserialize<T>()` — Validated in Phase 07: Static Entry Point and Shared Fixture Compliance
- [x] `Deserialize<T>(ReadOnlySpan<char> huml, ...)` overload for zero-allocation parsing paths — Validated in Phase 07: Static Entry Point and Shared Fixture Compliance
- [x] Zero external runtime dependencies — Validated in Phase 07: Static Entry Point and Shared Fixture Compliance
- [x] Serialiser hot-path allocations reduced: `AppendEscapedString` (no intermediate string), `PropertyDescriptor.DefaultValue` cached at build time — Validated in Phase 07.2: Code Quality, API Accuracy and Performance Optimisations
- [x] `HumlInlineMapping` AST node disambiguates inline/empty dict values from root `HumlDocument` — Validated in Phase 07.2: Code Quality, API Accuracy and Performance Optimisations
- [x] `HumlUnsupportedVersionException` in canonical `Huml.Net.Exceptions` namespace — Validated in Phase 07.2: Code Quality, API Accuracy and Performance Optimisations
- [x] `MaxRecursionDepth` validates range `[1, 1024]` at construction time — Validated in Phase 07.2 (introduced), tightened in Phase 07.9
- [x] Actionable error message for non-ASCII letter at bare-key position ("Bare keys must start with [a-zA-Z]") — Validated in Phase 07.3: Unicode and RTL Support with Fixture Extensions
- [x] `fixtures/extensions/` infrastructure with 11 gap assertions and 17 Unicode/RTL assertions, integrated into SharedSuiteTests via extension scan — Validated in Phase 07.3: Unicode and RTL Support with Fixture Extensions
- [x] `HumlSerializer` emits quoted keys for non-bare-key strings (non-ASCII, digit-start, spaces, empty, colon-containing); bare keys remain unquoted (no regression); `Dictionary<string,T>` with non-ASCII keys round-trips with value equality — Validated in Phase 07.4: Fix HumlSerializer Key Quoting (D-08 closed)
- [x] `CollectionFormat` enum (`Multiline=0`, `Inline=1`) on `HumlOptions` controls global inline serialisation opt-in — Validated in Phase 07.5: Inline Serialisation Support
- [x] `[HumlProperty(Inline = InlineMode.Inline/Multiline)]` per-property override; `InlineMode.Inherit` (default) defers to `HumlOptions.CollectionFormat` — Validated in Phase 07.5: Inline Serialisation Support
- [x] Scalar-only sequences emit `key:: v1, v2, v3`; scalar-valued dicts emit `key:: k: v, k2: v2`; complex collections silently fall back to multiline — Validated in Phase 07.5: Inline Serialisation Support
- [x] Empty collections always emit `key:: []` / `key:: {}` regardless of `CollectionFormat` — Validated in Phase 07.5: Inline Serialisation Support
- [x] Inline output round-trips through `Huml.Parse` without `HumlParseException` — Validated in Phase 07.5: Inline Serialisation Support
- [x] Comprehensive round-trip tests against mixed fixture documents (v0.1 + v0.2): parse verification, typed sub-section value-equality (integers, floats, strings, booleans, nulls, 3-level nesting, edge-case keys), inline serialisation value-equality (lists, dicts, attribute overrides) — Validated in Phase 07.6: Comprehensive Round-Trip Tests Against Mixed Fixture Files
- [x] NuGet-publishable: production-quality README.md (109 lines, code examples, compatibility table), CHANGELOG.md (Keep a Changelog 1.1.0), 6 docs/ guides (options, versioning, AST usage, error handling, inline serialisation, publish checklist), `Huml.Net.csproj` metadata patched (author, pitch description, repository URLs), `dotnet pack` verified clean across all 4 TFMs — Validated in Phase 07.7: Documentation Suite for First Public NuGet Release
- [x] `HumlOptions.Default` is header-aware (`VersionSource.Header`, falls back to v0.2); `HumlOptions.LatestSupported` is pinned v0.2 (`VersionSource.Options`); `HumlOptions.AutoDetect` is a reference-equal alias for `Default` — Validated in Phase 07.8: Make HumlOptions.Default Use AutoDetect Behaviour
- [x] `MaxRecursionDepth` default lowered from 512 to 64 (matches `System.Text.Json` convention); valid range tightened to `[1, 1024]`; regression test guards old ceiling (65536 now throws) — Validated in Phase 07.9: Lower MaxRecursionDepth Default and Tighten Range
- [x] `CONTRIBUTING.md` (9-section contributor onboarding: `--recurse-submodules`, AwesomeAssertions, TreatWarningsAsErrors, AI contributions welcome, links to `docs/internals/`), `BACKLOG.md` (triage workflow + empty table), and three `docs/internals/` guides (pipeline overview, version gates, extending the library) — Validated in Phase 07.10: Complete Missing Contributor and Developer Internals Documentation
- [x] `Serialize(object?, Type, HumlOptions?)` uses declared `Type` for property reflection — polymorphic callers get only base-type properties emitted; nested POCOs continue using runtime type (SER-TYPE-01, SER-TYPE-02) — Validated in Phase 07.11: Fix Serialize Object Type Ignores Type Parameter
- [x] `HumlDocument` XML docs clarify dual role: document root AND nested multiline mapping blocks (via `::` vector indicator); `HumlInlineMapping` XML docs corrected to inline `{k: v}` / empty `{}` scope only; bi-directional `<see cref>` cross-references added — Validated in Phase 07.13: Document HumlDocument Dual Role
- [x] `PropertyDescriptorCache` record bundles `Ordered: PropertyDescriptor[]` + `ByKey: Dictionary<string, PropertyDescriptor>` built in a single pass; `DeserializeMappingEntries` uses O(1) `TryGetValue` instead of O(n) `foreach` linear scan — Validated in Phase 07.14: Add Property Lookup Dictionary to PropertyDescriptor Cache

### Active

- [ ] CI pipeline: GitHub Actions running both fixture suite versions

### Out of Scope

- Source generator / AOT support — v2 concern; reflection-based is sufficient for v1
- Streaming / `IAsyncEnumerable` parsing — complexity not justified for config-file use case
- Schema validation — outside HUML spec scope
- HUML → JSON / YAML round-trip converters — distinct utility concern
- `Huml.Net.Linting` package — v2+ concern; package boundary established in architecture but no logic accretes into core parser for v1

## Context

- **Reference implementation:** [`go-huml`](https://github.com/huml-lang/go-huml) (primary), [`huml-rs`](https://github.com/huml-lang/huml-rs) (secondary)
- **HUML spec:** [huml.io](https://huml.io)
- **Shared test suite:** [`huml-lang/tests`](https://github.com/huml-lang/tests) — consumed as git submodules pinned to per-version tags (`v0.1`, `v0.2`)
- **Architecture mirrors go-huml:** single-pass `Lexer` → token stream → recursive-descent `Parser` → `HumlNode` AST → `HumlSerializer` / `HumlDeserializer` via reflection
- **TDD discipline:** shared suite fixtures drive Red/Green cycle before any production code; this applies at the spec-version level too (new version = new fixture directory before parser changes)
- **Properties in declaration order** (not alphabetically) — .NET convention differs from go-huml's alphabetical sort

## Constraints

- **Tech stack:** C# 13, `netstandard2.1;net8.0;net9.0;net10.0` — `netstandard2.1` as compat floor gives `ReadOnlySpan<char>` in public API and covers .NET Core 3.x / .NET 5–10; deliberately excludes .NET Framework
- **Runtime dependencies:** Zero — no external packages in the main library; test-only deps are `xUnit` + `AwesomeAssertions`
- **Licence:** MIT
- **Author:** Richard (Radberi)

## Key Decisions

| Decision | Rationale | Outcome |
|----------|-----------|---------|
| Multi-target `netstandard2.1;net8.0;net9.0;net10.0` | `Span` in public API requires ns2.1+; multi-targeting lets modern consumers get optimised TFM builds via NuGet resolution | — Pending |
| Drop .NET Framework support | `netstandard2.1` compat floor is required for `ReadOnlySpan<char>` overload; .NET Framework was netstandard2.0 territory only | — Pending |
| Single parser code path with version gates | No forked `Lexer`/`Parser` classes per spec version — explicit `>=` branch points make divergence searchable and direction of change self-documenting | — Pending |
| Properties emitted in declaration order | .NET convention; alphabetical sorting (go-huml) would surprise C# consumers | — Pending |
| `Huml.Net.Linting` is a separate package | Parser has zero opinions on style/advisories; linting logic must never accrete into core | — Pending |
| v0.1 + v0.2 both in v1 scope | Support window is last 3 minor versions; v0.1 remains supported until v0.3 ships | — Pending |
| `SpecVersionPolicy` constants as code | `HumlUnsupportedVersionException` references them directly — error message stays accurate without manual updates | — Pending |

---
*Last updated: 2026-04-03 — Phase 07.14 complete: `PropertyDescriptorCache` introduces O(1) dictionary lookup in `DeserializeMappingEntries`, replacing O(n) linear scan. 718 tests green across net8.0/net9.0/net10.0.*
