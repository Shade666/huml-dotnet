# Project Research Summary

**Project:** Huml.Net
**Domain:** .NET parser / serialisation library (NuGet package for the HUML format)
**Researched:** 2026-03-20
**Confidence:** HIGH

## Executive Summary

Huml.Net is a zero-external-runtime-dependency .NET library that parses and serialises the HUML (Human-oriented Markup Language) configuration format. The dominant model for this class of library in the .NET ecosystem is System.Text.Json: a static entry-point class (`JsonSerializer.Serialize<T>` / `Deserialize<T>`), a sealed options object, typed exceptions with line/column position, and per-type attribute annotations. Research confirms this is the correct pattern for Huml.Net — it minimises adoption friction and aligns with existing developer expectations. The library must multi-target `netstandard2.1;net8.0;net9.0;net10.0`, use `ReadOnlySpan<char>` in the Lexer for allocation efficiency, and publish to NuGet with embedded PDB (SourceLink) and OIDC Trusted Publishing.

The most significant architectural decision is the Lexer → Parser → AST → Serializer/Deserializer pipeline with a clear single-pass version-gating strategy. A single `Lexer` and `Parser` class with explicit `if (version >= HumlSpecVersion.V0_2)` branches is strongly preferred over forked classes per spec version. The AST (`HumlDocument`, `HumlMapping`, `HumlSequence`, `HumlScalar`) is built as an immutable `abstract record` hierarchy; the Serializer/Deserializer use cached reflection with a `ConcurrentDictionary<Type, PropertyDescriptor[]>`. The version-aware `HumlOptions` object — particularly `SpecVersion`, `VersionSource`, and `UnknownVersionBehaviour` — is a first-class differentiator unique to Huml.Net and must ship in v1.

The principal risks are operational rather than algorithmic: silent multi-TFM packaging failures (`GeneratePackageOnBuild` misuse), SourceLink metadata omissions, vacuous CI passes when git submodules are not initialised, and a C# 14 overload resolution ambiguity introduced by shipping dual `string` + `ReadOnlySpan<char>` overloads. Each of these has a clear, low-cost prevention strategy that must be baked into the project scaffold and CI workflow from day one.

---

## Key Findings

### Recommended Stack

The library is built on .NET SDK 10.0.201 (pinned via `global.json`) targeting `netstandard2.1;net8.0;net9.0;net10.0`. All shared build settings (`<Nullable>enable</Nullable>`, `<TreatWarningsAsErrors>true</TreatWarningsAsErrors>`, `<ContinuousIntegrationBuild>`, SourceLink, MinVer) live in a root `Directory.Build.props` so every project inherits them automatically. The test project targets `net8.0;net9.0;net10.0` only — xUnit v3 test projects are executables and cannot target `netstandard`. NuGet packaging uses `dotnet pack` as a dedicated CI step (not `GeneratePackageOnBuild`) with OIDC Trusted Publishing to avoid long-lived API keys.

**Core technologies:**
- **.NET SDK 10.0.201 / C# 13 (`LangVersion=latest`):** Latest stable SDK; supports all required TFMs without version drift.
- **`netstandard2.1` compat floor:** Provides `ReadOnlySpan<char>` / `Span<T>` / `Memory<T>` without external deps; intentionally excludes .NET Framework.
- **xUnit v3 (3.2.2) + AwesomeAssertions (9.4.0):** Current stable test generation; AwesomeAssertions is the project-mandated Apache 2.0 fork of FluentAssertions.
- **MinVer (7.0.0):** Git-tag-driven versioning; eliminates manual `<Version>` edits on every release.
- **Microsoft.SourceLink.GitHub (10.0.201):** Embeds source-stepping metadata in PDB; `PrivateAssets="All"` keeps it out of consumers' dependency graph.
- **OIDC Trusted Publishing (`NuGet/login@v1`):** Short-lived token exchange; no long-lived API key stored in the repository (GA September 2025).

### Expected Features

Research benchmarked against System.Text.Json, YamlDotNet, and Tomlyn. All features in the P1 column must ship in v1; everything else is v1.x or v2+.

**Must have (table stakes):**
- `Huml.Serialize<T>()` / `Huml.Deserialize<T>()` static entry points — the API contract developers check first.
- `Deserialize<T>(ReadOnlySpan<char>)` overload — required to meet the allocation-efficiency claim.
- `[HumlProperty("name")]` + `[HumlIgnore]` attributes — callers cannot control mapping without these.
- `HumlParseException` with typed `Line` + `Column` int properties — error usability is non-negotiable for adoption.
- `HumlOptions` with `SpecVersion`, `VersionSource`, `UnknownVersionBehaviour` — core differentiator; must ship in v1.
- Collection and nested object round-trips (`List<T>`, `Dictionary<string,T>`, arrays, nested POCOs).
- Full HUML v0.2 spec compliance validated by the `huml-lang/tests` shared fixture suite.
- HUML v0.1 support within the rolling 3-version window.
- XML doc comments on all public surface area — IntelliSense is table stakes for NuGet adoption.
- Deterministic builds + SourceLink + NuGet metadata (README, MIT licence).
- Declaration-order property emission (not alphabetical — explicit departure from go-huml default).

**Should have (competitive differentiators):**
- `HumlUnsupportedVersionException` + `[Obsolete]`-decorated `SpecVersionPolicy` constants — version sunset discoverable in the IDE.
- `IHumlConverter<T>` extensibility seam designed now (no-breaking-change addition in v1.x when demand emerges).
- `HumlOptions.Default` / `HumlOptions.Strict` preset instances — convenience once the options set stabilises.

**Defer to v2+:**
- Source generator / AOT support (`HumlSerializerContext`) — defer until NativeAOT demand is evidenced.
- `Huml.Net.Linting` package — package boundary established in v1 architecture; zero logic until then.
- HUML v0.3 support (when the spec ships).

### Architecture Approach

The library follows a strict left-to-right pipeline: `Lexer` (character → `Token`) → `Parser` (token → `HumlDocument` AST) → `HumlSerializer` / `HumlDeserializer` (AST ↔ .NET objects). The `Huml` static class is the sole public entry point; all internal types are `internal`. The AST node hierarchy uses `abstract record` base types for structural equality in tests and immutability in the pipeline. The `Lexer` accepts `ReadOnlySpan<char>` directly (using an internal `ref struct` scanning state) and materialises token values to `string` only at emission. Version-gated behaviour is expressed as `if (_options.SpecVersion >= HumlSpecVersion.V0_2)` branches inside the single Lexer/Parser rather than forked classes. The `Serializer` and `Deserializer` cache reflection results per type in a `static ConcurrentDictionary<Type, PropertyDescriptor[]>`.

**Major components:**
1. **`Versioning/`** — `HumlSpecVersion` enum, `HumlOptions`, `SpecVersionPolicy` constants, `VersionSource` and `UnknownVersionBehaviour` enums. No dependencies; must exist before any pipeline code.
2. **`Lexer/`** — Single-pass, line-oriented character scanner accepting `ReadOnlySpan<char>`; emits `IReadOnlyList<Token>` where `Token` is a `readonly record struct`.
3. **`Parser/Nodes/`** — Recursive-descent parser producing an immutable `HumlDocument` AST (`HumlMapping`, `HumlSequence`, `HumlScalar` nodes as `abstract record` hierarchy).
4. **`Serialisation/`** — `HumlSerializer` (object → HUML string) and `HumlDeserializer` (AST → typed object), co-located because they share reflection helpers.
5. **`Attributes/`** — `[HumlProperty]` and `[HumlIgnore]`; public API surface consumed by callers, separated from implementation.
6. **`Huml` static class** — Top-level entry point wiring all pipeline stages together; validates options; the only type consumers need to reference directly.

### Critical Pitfalls

1. **`GeneratePackageOnBuild=true` in a multi-TFM project** — Silently produces a `.nupkg` with only a subset of TFMs in `lib/`. Avoid entirely; use `dotnet pack -c Release` as a dedicated CI step and inspect the package with NuGet Package Explorer.

2. **SourceLink silently broken** — Missing `PublishRepositoryUrl=true`, `EmbedUntrackedSources=true`, or the `ContinuousIntegrationBuild` conditional results in zero-filled commit SHAs in PDB. Add all three properties from project scaffolding day one and validate with `dotnet sourcelink test` in CI.

3. **Git submodule not initialised in CI — fixture tests pass vacuously** — `actions/checkout` defaults to `submodules: false`. xUnit Theory with zero MemberData rows does not fail. Use `submodules: recursive` and add a sentinel `[Fact]` that asserts the fixture directory is non-empty.

4. **C# 14 overload resolution ambiguity with `string` + `ReadOnlySpan<char>` dual overloads** — In .NET 10 / C# 14, `Deserialize("literal")` becomes ambiguous between the two overloads (CS0121). Ship `ReadOnlySpan<char>` as the single implementation overload; wrap the `string` variant as a thin `AsSpan()` call. Never ship both as first-class peers.

5. **Reflection deserialiser silently skips `init`-only properties** — `PropertyInfo.CanWrite` returns `true` for `init` setters but `SetValue()` throws at runtime. Properties with `private set` are silently skipped. Detect `IsExternalInit` via `GetRequiredCustomModifiers()` and either bind via constructor or throw a clear `HumlDeserializeException`. Cover with a fixture test.

6. **Recursive-descent parser without depth limit** — Pathologically nested HUML causes an unrecoverable `StackOverflowException`. Add an explicit recursion counter; throw `HumlParseException` at a configurable depth limit (default 512).

---

## Implications for Roadmap

Architecture research identifies a strict component build-order dependency graph. The phase structure below follows that graph directly so each phase has all its dependencies satisfied by prior phases.

### Phase 1: Project Scaffold and CI Foundations
**Rationale:** All subsequent work requires a working build, test, and packaging pipeline. The pitfalls research shows that multi-TFM pack, SourceLink, and submodule initialisation failures are easiest to prevent at day zero and extremely costly to recover from after a public release. This phase eliminates those failure modes before a single line of library code is written.
**Delivers:** Compilable multi-TFM solution skeleton, green CI pipeline (build + test + pack + SourceLink validation), NuGet packaging workflow with OIDC Trusted Publishing, git submodule checkout for shared test fixtures, sentinel guard test for fixture directory.
**Addresses:** NuGet metadata, MIT licence, deterministic builds, SourceLink (table-stakes features from FEATURES.md).
**Avoids:** Pitfalls 1 (GeneratePackageOnBuild), 2 (SourceLink broken), 7 (submodule not initialised), 8 (CurrentDirectory path bug).

### Phase 2: Versioning Foundation
**Rationale:** `HumlSpecVersion`, `HumlOptions`, `SpecVersionPolicy`, `VersionSource`, and `UnknownVersionBehaviour` have zero dependencies and must exist before any pipeline code. The version-gating strategy (`>=` comparisons on an int-backed enum) must be locked before the Lexer is written to avoid retroactive refactoring. This is also when boundary-value tests for the rolling support window must be established.
**Delivers:** Full versioning type hierarchy, `HumlUnsupportedVersionException`, `[Obsolete]` annotations on deprecated version constants, version boundary unit tests.
**Addresses:** `HumlOptions` with SpecVersion/VersionSource/UnknownVersionBehaviour, HumlUnsupportedVersionException (P1 features).
**Avoids:** Pitfall 6 (off-by-one version gate).

### Phase 3: Lexer and Token Types
**Rationale:** The Lexer depends only on Versioning types (Phase 2). Building it next establishes the allocation-free scanning foundation — `Token` as `readonly record struct`, `ReadOnlySpan<char>` input — before the Parser couples to the token contract. This is also when the `ReadOnlySpan<char>` / `string` overload API shape must be decided to avoid the C# 14 ambiguity.
**Delivers:** `Token`, `TokenType`, `Lexer` accepting `ReadOnlySpan<char>` with version-gated rules, `HumlParseException` with Line/Column, full lexer unit test coverage.
**Addresses:** `Deserialize<T>(ReadOnlySpan<char>)` overload requirement, `HumlParseException` (P1 features).
**Avoids:** Pitfalls 3 (conditional compilation ordering), 4 (C# 14 overload ambiguity), 9 (string.Split allocations in Lexer).

### Phase 4: AST Node Hierarchy
**Rationale:** AST nodes have no dependencies and can be defined in parallel with the Lexer, but are separated here as a discrete deliverable to keep the Parser phase focused purely on grammar. Immutable `abstract record` nodes must be public (consumed by future `Huml.Net.Linting`) so their surface needs careful design review before the Parser commits to producing them.
**Delivers:** `HumlNode`, `HumlDocument`, `HumlMapping`, `HumlSequence`, `HumlScalar`, `ScalarKind`; all with `Line`/`Column`; structural equality verified in tests.
**Implements:** AST Node hierarchy architecture component.
**Addresses:** Nested object support, collection support (prerequisite features).

### Phase 5: Parser
**Rationale:** The Parser depends on Token types (Phase 3), AST nodes (Phase 4), Versioning types (Phase 2), and exceptions. This is the most complex single component; all its dependencies must be stable before work begins. Version-gate `if` branches are introduced here for the first time in the pipeline.
**Delivers:** Recursive-descent `Parser` with depth-limit guard, full HUML v0.1 and v0.2 grammar coverage, version-gated rule branches, all parser unit tests.
**Avoids:** Pitfall 5 (stack overflow — depth limit); Pitfall 6 (version gate ordering).

### Phase 6: Attributes and Serializer/Deserializer
**Rationale:** `[HumlProperty]` and `[HumlIgnore]` are pure attribute classes with no dependencies; they can be implemented at any point but are needed by both Serializer and Deserializer so they gate this phase. The Serializer and Deserializer are co-located in `Serialisation/` and share reflection helpers; building them together avoids duplication. The `init`-only setter detection and reflection cache are implemented here.
**Delivers:** `HumlPropertyAttribute`, `HumlIgnoreAttribute`, `HumlSerializer` (declaration-order emission, version-gated output), `HumlDeserializer` (reflection with cache + IsExternalInit detection), `HumlSerializeException`, full ser/deser unit tests including `init`-only POCO fixture.
**Addresses:** All P1 attribute and serialisation features; declaration-order property emission.
**Avoids:** Pitfall 5 (init-only silent skip); Pitfall (reflection without caching).

### Phase 7: Static Entry Point and Shared Fixture Compliance
**Rationale:** The `Huml` static class depends on all prior phases. Once it exists, the full end-to-end pipeline can be wired and the shared `huml-lang/tests` fixture suite can be run against the implementation. This phase's primary deliverable is spec compliance certification — CI must pass with a non-zero Theory count for both v0.1 and v0.2 fixtures.
**Delivers:** `Huml` static class (`Serialize<T>`, `Deserialize<T>`, `Parse`), `SharedSuiteTests.cs` Theory runner, full green CI across all TFMs with verified fixture counts.
**Addresses:** Full HUML v0.2 spec compliance, HUML v0.1 rolling-window support, XML doc comments on all public surface (final review pass).
**Avoids:** Pitfall 7 (submodule not initialised); Pitfall 8 (CurrentDirectory fixture path).

### Phase 8: NuGet Release Preparation
**Rationale:** A final hardening phase before the first public NuGet push. This is when the full "looks done but isn't" checklist from PITFALLS.md is executed, the README is finalised, and the publish workflow is exercised against a pre-release tag.
**Delivers:** Verified multi-TFM `.nupkg` (all four `lib/` entries present), `dotnet sourcelink test` green, XML docs in package, `0.1.0-alpha.1` pre-release publish to NuGet.org, `nuget-publish` GitHub Environment protection rule active.
**Avoids:** Pitfall 1 (multi-TFM pack), Pitfall 2 (SourceLink).

### Phase Ordering Rationale

- The dependency graph from ARCHITECTURE.md (Versioning → Tokens → Lexer → AST → Parser → Ser/Deser → Huml entry point) directly drives phase order; no phase begins before its prerequisites are green.
- CI/packaging foundations (Phase 1) precede all library code because multi-TFM and SourceLink failures discovered after publishing require version yanking (HIGH recovery cost per PITFALLS.md).
- Versioning types (Phase 2) are isolated before pipeline work because the version-gate strategy must be locked — retroactively adding `HumlOptions` threading to a completed Lexer requires touching every method signature.
- Attributes and Ser/Deser (Phase 6) are co-located in one phase because they share reflection helpers and the `init`-only detection logic is relevant to both directions.

### Research Flags

Phases that follow well-documented patterns and do not need deeper research during planning:
- **Phase 1 (Scaffold/CI):** Standard SDK-style multi-TFM project setup; OIDC Trusted Publishing is now GA and documented. STACK.md provides complete project file content.
- **Phase 2 (Versioning):** Pure C# types; no external integration points.
- **Phase 3 (Lexer):** `ReadOnlySpan<char>` scanning pattern is well-documented (mirrors `Utf8JsonReader`).
- **Phase 4 (AST):** `abstract record` hierarchy is standard C# 9+.
- **Phase 8 (Release Prep):** Checklist-driven; all steps are documented in PITFALLS.md.

Phases that may benefit from deeper research during planning:
- **Phase 5 (Parser):** The HUML grammar spec details (indent rules, inline list syntax, comment rules per version) are not fully documented in the research files. The `huml-lang/go-huml` reference implementation will need to be consulted during implementation. Confidence on specific grammar rules is MEDIUM.
- **Phase 6 (Ser/Deser):** The `init`-only constructor-binding fallback strategy (when `IsExternalInit` is detected, fall back to constructor-based binding or throw?) requires a deliberate design decision not yet made. The seam for `IHumlConverter<T>` extensibility also needs interface design before implementation.
- **Phase 7 (Fixture Compliance):** The `huml-lang/tests` shared suite structure (fixture file format, expectation file format, valid vs invalid subdirectory layout) is referenced but not fully described in the research. Needs inspection of the actual submodule content before SharedSuiteTests.cs is written.

---

## Confidence Assessment

| Area | Confidence | Notes |
|------|------------|-------|
| Stack | HIGH | All package versions verified on NuGet.org; all CI patterns verified against official Microsoft docs and GA NuGet Trusted Publishing. |
| Features | HIGH | Benchmarked directly against System.Text.Json, YamlDotNet, and Tomlyn official docs; all API shapes verified. |
| Architecture | HIGH | Pipeline pattern mirrors System.Text.Json internals; `readonly record struct`, `abstract record`, and reflection caching are all established .NET patterns with official documentation. go-huml reference implementation consulted at MEDIUM confidence for grammar specifics. |
| Pitfalls | HIGH (operational), MEDIUM (grammar/version-gate edge cases) | CI, packaging, and reflection pitfalls are verified against official bug trackers and Microsoft breaking-change docs. Grammar-specific pitfalls rely on go-huml inference. |

**Overall confidence:** HIGH

### Gaps to Address

- **HUML grammar specifics (v0.1 and v0.2):** Research does not enumerate every grammar rule. During the Parser phase (Phase 5), the `huml-lang/go-huml` source and the `huml-lang/tests` fixture suite must be the authoritative specification. Do not invent rules from inference.
- **`init`-only constructor-binding design decision:** When an `init`-only property is detected, the library must choose between (a) constructor-parameter binding, (b) throwing `HumlDeserializeException`, or (c) silently skipping (confirmed wrong by PITFALLS.md). This decision must be made at Phase 6 planning time and should be validated against how System.Text.Json handles the same case.
- **`huml-lang/tests` fixture format:** The exact structure of the shared suite (valid vs invalid directories, expectation file format) is referenced but not described in detail. Inspect the submodule content when initialising the repository before writing SharedSuiteTests.cs.
- **`IHumlConverter<T>` interface shape:** The extensibility seam is identified as a v1.x concern but the interface must be designed during Phase 6 to ensure its absence in v1.0 is a non-breaking addition. Consult `JsonConverter<T>` and Tomlyn's `ITomlTypeInfoResolver` as models.

---

## Sources

### Primary (HIGH confidence)
- [NuGet Gallery: xunit.v3 3.2.2](https://www.nuget.org/packages/xunit.v3) — version and framework support
- [NuGet Gallery: AwesomeAssertions 9.4.0](https://www.nuget.org/packages/AwesomeAssertions) — version verified
- [NuGet Gallery: MinVer 7.0.0](https://www.nuget.org/packages/MinVer/) — latest stable
- [NuGet Gallery: Microsoft.SourceLink.GitHub 10.0.201](https://www.nuget.org/packages/Microsoft.SourceLink.GitHub) — version verified
- [.NET 10 download page](https://dotnet.microsoft.com/en-us/download/dotnet/10.0) — SDK 10.0.201 current as of March 2026
- [NuGet Trusted Publishing — Microsoft Learn](https://learn.microsoft.com/en-us/nuget/nuget-org/trusted-publishing) — OIDC workflow
- [System.Text.Json overview — Microsoft Learn](https://learn.microsoft.com/en-us/dotnet/standard/serialization/system-text-json/overview) — API shape benchmarking
- [System.Text.Json / Utf8JsonReader internals — Microsoft Learn](https://learn.microsoft.com/en-us/dotnet/standard/serialization/system-text-json/use-utf8jsonreader) — Lexer pattern
- [C# record types — Microsoft Learn](https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/builtin-types/record) — AST node pattern
- [Cross-platform targeting for .NET libraries — Microsoft Learn](https://learn.microsoft.com/en-us/dotnet/standard/library-guidance/cross-platform-targeting) — multi-TFM guidance
- [Source Link and .NET libraries — Microsoft Learn](https://learn.microsoft.com/en-us/dotnet/standard/library-guidance/sourcelink) — SourceLink configuration
- [Breaking change: C# 14 overload resolution with span parameters — Microsoft Learn](https://learn.microsoft.com/en-us/dotnet/core/compatibility/core-libraries/10.0/csharp-overload-resolution) — Pitfall 4
- [NuGet NU5128 warning — Microsoft Learn](https://learn.microsoft.com/en-us/nuget/reference/errors-and-warnings/nu5128) — Pitfall 1
- [YamlDotNet GitHub repository](https://github.com/aaubry/YamlDotNet) — feature benchmarking
- [Tomlyn official site](https://xoofx.github.io/Tomlyn/) — feature benchmarking
- [xUnit v3 Getting Started](https://xunit.net/docs/getting-started/v3/getting-started) — test framework requirements

### Secondary (MEDIUM confidence)
- [go-huml reference implementation](https://github.com/huml-lang/go-huml) — architecture reference (source code structure inferred from repository overview)
- [Andrew Lock: Trusted Publishing from GitHub Actions](https://andrewlock.net/easily-publishing-nuget-packages-from-github-actions-with-trusted-publishing/) — CI publish workflow pattern
- [xUnit MemberData parameterised tests — Andrew Lock](https://andrewlock.net/creating-parameterised-tests-in-xunit-with-inlinedata-classdata-and-memberdata/) — fixture test pattern
- [C# record structs deep dive — nietras.com](https://nietras.com/2021/06/14/csharp-10-record-struct/) — Token performance rationale
- [dotnet/msbuild #7911](https://github.com/dotnet/msbuild/issues/7911) — multi-TFM GeneratePackageOnBuild issue

---
*Research completed: 2026-03-20*
*Ready for roadmap: yes*
