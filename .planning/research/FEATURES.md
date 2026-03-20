# Feature Research

**Domain:** .NET serialisation/parsing library (custom format — HUML)
**Researched:** 2026-03-20
**Confidence:** HIGH (System.Text.Json and YamlDotNet verified against official docs and GitHub; Tomlyn verified against official site and NuGet)

## Feature Landscape

### Table Stakes (Users Expect These)

Features users assume exist. Missing these = product feels incomplete. Benchmarked against System.Text.Json, YamlDotNet, and Tomlyn.

| Feature | Why Expected | Complexity | Notes |
|---------|--------------|------------|-------|
| `Serialize<T>(T value)` static entry point | System.Text.Json established this as the .NET idiom; any new library without it feels alien | LOW | `Huml.Serialize<T>()` — already in PROJECT.md scope |
| `Deserialize<T>(string huml)` static entry point | Mirror of above; the symmetry is the entire API contract developers check first | LOW | `Huml.Deserialize<T>()` — already in scope |
| `Deserialize<T>(ReadOnlySpan<char>)` overload | Modern .NET callers pass spans; string-only API signals the library is not allocation-aware | MEDIUM | In scope; requires `netstandard2.1` floor — already decided |
| `[HumlProperty("name")]` rename attribute | `[JsonPropertyName]` in STJ, `[YamlMember(Alias = "name")]` in YamlDotNet — renaming fields without changing the C# model is universal | LOW | Attribute on property; feeds deserialiser key lookup |
| `[HumlIgnore]` attribute | `[JsonIgnore]` / `[YamlIgnore]` — every serialiser has this; absence is jarring | LOW | Serialize AND deserialise directions |
| Structured exception with line and column | YamlDotNet reports `(Line: N, Col: N)` in exceptions; STJ reports `LineNumber`/`BytePositionInLine` on `JsonException`; developers copy error location into editor | MEDIUM | `HumlParseException` with `Line`, `Column` properties — in scope |
| Options object (`HumlOptions`) | `JsonSerializerOptions` / `SerializerBuilder` — configuration without subclassing | LOW | Already in scope; must be immutable-after-first-use or thread-safe |
| Collection serialisation (arrays, lists, dicts) | Any serialiser that cannot round-trip `List<T>`, `Dictionary<string,T>`, and arrays is unusable | MEDIUM | Covers HUML sequences and mappings |
| Primitive type round-trip fidelity | string, int, long, float, double, bool, null — must survive a full round-trip without coercion surprises | LOW | HUML mandatory quoting removes most ambiguity; still needs test coverage |
| Null handling (nullable types) | `string?`, `int?`, value-type nullables — callers expect graceful null serialisation | LOW | HUML has explicit `null` literal; straightforward mapping |
| Nested object serialisation | Recursive POCOs; failure here means the library cannot handle real config files | MEDIUM | Core parser capability |
| XML doc comments on all public API | IntelliSense tooltips; without `<summary>` tags the NuGet package feels unfinished | LOW | Infrastructure concern; no runtime cost |
| NuGet package with correct metadata | `<PackageId>`, `<Description>`, `<Authors>`, `<RepositoryUrl>`, README embedded | LOW | Already in PROJECT.md scope |
| Deterministic builds + SourceLink | Developers expect to step into library source; missing PDB/SourceLink erodes trust | LOW | CI concern; `<Deterministic>true</Deterministic>` + SourceLink |
| MIT licence declared in package | License discovery on NuGet.org; commercial users check this before adopting | LOW | Already decided; `<PackageLicenseExpression>MIT</PackageLicenseExpression>` |

### Differentiators (Competitive Advantage)

Features that set Huml.Net apart from a generic parser. These align directly with HUML's design proposition and the System.Text.Json-familiarity angle.

| Feature | Value Proposition | Complexity | Notes |
|---------|-------------------|------------|-------|
| Version-aware parsing with explicit `HumlOptions.SpecVersion` | No other .NET config-format library models spec versioning as a first-class concern; `UnknownVersionBehaviour` (error/warn/ignore) lets callers control upgrade path | MEDIUM | `VersionSource` enum (Header/Options/Fallback); `HumlUnsupportedVersionException` |
| `[Obsolete]`-decorated version constants | Makes spec version sunset discoverable in the IDE; callers get a compiler warning when their pinned version exits the support window — no other library does this | LOW | `SpecVersionPolicy` constants; pairs with `HumlUnsupportedVersionException` |
| Shared fixture test suite as part of published CI | Publishing CI results against `huml-lang/tests` gives adoption confidence; developers can verify compliance independently | LOW | CI/badge concern; not API surface |
| `System.Text.Json`-mirrored API shape (not just similar) | YamlDotNet uses builder pattern; Tomlyn is the only other library explicitly copying STJ naming. Huml.Net extends that precedent into version management | LOW | Design consistency; low code cost, high discoverability reward |
| Zero external runtime dependencies | Dependency audits block adoption in some orgs; a zero-dep library clears that gate automatically | LOW | Already decided; tests are xUnit but that is dev-only |
| Properties emitted in declaration order | YamlDotNet and go-huml both sort alphabetically; C# developers mentally model their POCO in source order; declaration-order output reduces surprise when diffing config files | MEDIUM | Requires reflection-order preservation; `Type.GetProperties()` order is reliable in .NET for most scenarios |
| Explicit multi-TFM targeting (`netstandard2.1;net8;net9;net10`) | Consumers get the best TFM build (NuGet resolution) without conditional compilation in their own projects | LOW | Build infrastructure; already decided |

### Anti-Features (Things to Deliberately NOT Build in v1)

| Feature | Why Requested | Why Problematic | Alternative |
|---------|---------------|-----------------|-------------|
| Source generator / AOT support | Developers with trimming/NativeAOT targets ask for it; STJ and Tomlyn both have it | Significant scope; requires Roslyn generator, separate NuGet package, extensive testing. The config-file use case (primary HUML target) almost never runs in NativeAOT | Defer to v2; design `HumlOptions` to accept a `IHumlTypeInfoResolver` seam now so the v2 generator plugs in without breaking changes |
| Streaming / `IAsyncEnumerable` | Developers familiar with STJ streaming ask by reflex | HUML is a config-file format; files are small; streaming adds 3–4x complexity with no real-world payoff | Read entire file, parse as string/span. Document this decision in README |
| Schema validation / strict mode | Users from JSON Schema world expect this | Out of HUML spec scope; would require a separate schema language definition | Document that HUML's mandatory quoting and explicit types are themselves a validation layer |
| HUML → JSON / YAML round-trip converters | Power users want format migration tools | A distinct utility concern; implementation in core couples the parser to other formats and inflates the dependency surface | Provide as a separate `Huml.Net.Converters` package post-v1 if demand emerges |
| `Huml.Net.Linting` in core | Style advisories seem natural to bundle | Linting is opinion; parsing is correctness. Mixing them erodes the "zero opinions on style" contract and complicates the public API | Establish `Huml.Net.Linting` as a separate package boundary in v1 architecture with zero logic in core |
| Dynamic/`JObject`-style DOM API | Newtonsoft.Json trained developers to expect `JObject` manipulation | Adds a full DOM object model on top of the AST; the `HumlNode` AST already provides programmatic access for edge cases | Expose `HumlNode` AST publicly if callers need untyped access; avoid a full mutable DOM layer |
| `[JsonPropertyName]` / STJ attribute interop | Tomlyn does this; reduces friction for codebases already annotated for JSON | Cross-library attribute coupling; harder to test, confusing semantics when STJ and Huml behaviours diverge | Provide `[HumlProperty]` which callers add alongside `[JsonPropertyName]` — two attributes, zero ambiguity |

## Feature Dependencies

```
Deserialize<T>(ReadOnlySpan<char>)
    └──requires──> Lexer (tokenises span without string allocation)
                       └──requires──> HumlNode AST
                                          └──requires──> Deserialiser (reflection mapper)

[HumlProperty] / [HumlIgnore]
    └──requires──> Deserialiser (attribute inspection at reflection time)
    └──requires──> Serialiser (attribute inspection at reflection time)

HumlOptions (SpecVersion, VersionSource, UnknownVersionBehaviour)
    └──requires──> Lexer (version header detection)
    └──requires──> HumlUnsupportedVersionException

SpecVersionPolicy constants + [Obsolete]
    └──requires──> HumlOptions (SpecVersion field must exist)
    └──enhances──> HumlUnsupportedVersionException (error message references constants)

HumlParseException (Line, Column)
    └──requires──> Lexer (position tracking during tokenisation)

Collection serialisation
    └──requires──> HumlNode AST (sequence node type)
    └──enhances──> Deserialise<T> (generic collection mapping)

Declaration-order property emission
    └──requires──> Serialiser
    └──conflicts──> Alphabetical sort (go-huml default) — explicit decision: declaration order wins
```

### Dependency Notes

- **`Deserialize<T>(ReadOnlySpan<char>)` requires Lexer:** The Lexer must accept `ReadOnlySpan<char>` directly; routing through a `string` overload first defeats the allocation benefit.
- **`[HumlProperty]` / `[HumlIgnore]` require both Serialiser and Deserialiser:** Attributes must be respected in both directions; a one-directional implementation is a bug surface.
- **`HumlOptions` requires Lexer:** Version detection reads the `#huml v0.x` header token; this must happen before any other parsing decisions are made.
- **`SpecVersionPolicy` constants enhance `HumlUnsupportedVersionException`:** The exception message should embed the supported range from the constants so the message is always accurate.
- **Declaration-order emission conflicts with alphabetical sort:** This is a deliberate departure from go-huml; must be documented and test-covered.

## MVP Definition

### Launch With (v1)

- [x] `Huml.Serialize<T>()` / `Huml.Deserialize<T>()` static entry points — without these there is no library
- [x] `Deserialize<T>(ReadOnlySpan<char>)` overload — required to meet stated zero-allocation goal
- [x] `[HumlProperty]` + `[HumlIgnore]` attributes — without these callers cannot control mapping
- [x] `HumlOptions` with `SpecVersion`, `VersionSource`, `UnknownVersionBehaviour` — core differentiator; must ship in v1
- [x] `HumlParseException` with `Line` + `Column` — error usability; non-negotiable for adoption
- [x] `HumlSerializeException` + `HumlUnsupportedVersionException` — complete error taxonomy
- [x] Collection + nested object round-trips — library is non-functional without these
- [x] Full HUML v0.2 spec compliance validated by `huml-lang/tests` — correctness guarantee
- [x] HUML v0.1 support within rolling window — spec states last 3 minor versions
- [x] XML doc comments on all public surface — IntelliSense is table stakes for NuGet adoption
- [x] NuGet metadata + README + MIT licence — discoverability and legal clarity
- [x] SourceLink + deterministic builds — professional quality signal
- [x] Declaration-order property emission — .NET convention; test-covered

### Add After Validation (v1.x)

- [ ] `IHumlConverter<T>` extensibility seam — add when users report a type they cannot map natively; design the interface now so it is a non-breaking addition
- [ ] `HumlOptions` preset instances (`HumlOptions.Default`, `HumlOptions.Strict`) — convenience once the options set stabilises
- [ ] Expanded collection support (ImmutableArray, ReadOnlyCollection, etc.) — add per demand signals

### Future Consideration (v2+)

- [ ] Source generator / AOT support (`HumlSerializerContext`) — defer until NativeAOT demand is evidenced
- [ ] `Huml.Net.Linting` package — package boundary established in v1 architecture; no logic until then
- [ ] HUML v0.3 support (when spec ships) — rolling window will add v0.3 and retire v0.1

## Feature Prioritization Matrix

| Feature | User Value | Implementation Cost | Priority |
|---------|------------|---------------------|----------|
| Static Serialize/Deserialize API | HIGH | LOW | P1 |
| ReadOnlySpan<char> overload | HIGH | MEDIUM | P1 |
| HumlProperty / HumlIgnore attributes | HIGH | LOW | P1 |
| HumlParseException with line/column | HIGH | MEDIUM | P1 |
| Version-aware HumlOptions | HIGH | MEDIUM | P1 |
| Collection + nested object support | HIGH | MEDIUM | P1 |
| XML doc comments + NuGet metadata | HIGH | LOW | P1 |
| Declaration-order emission | MEDIUM | MEDIUM | P1 |
| SourceLink + deterministic builds | MEDIUM | LOW | P1 |
| HumlUnsupportedVersionException + [Obsolete] constants | MEDIUM | LOW | P1 |
| IHumlConverter<T> extensibility hook | MEDIUM | MEDIUM | P2 |
| HumlOptions preset instances | LOW | LOW | P2 |
| Expanded collection type coverage | MEDIUM | MEDIUM | P2 |
| Source generator / AOT | HIGH | HIGH | P3 |
| Huml.Net.Linting package | LOW | HIGH | P3 |

**Priority key:**
- P1: Must have for launch
- P2: Should have, add when possible
- P3: Nice to have, future consideration

## Competitor Feature Analysis

| Feature | System.Text.Json | YamlDotNet | Tomlyn | Huml.Net approach |
|---------|-----------------|------------|--------|-------------------|
| Static serialise/deserialise entry point | `JsonSerializer.Serialize<T>()` | `new Serializer().Serialize(obj)` (builder pattern) | `Toml.FromModel<T>()` | `Huml.Serialize<T>()` — mirrors STJ, not builder |
| Rename attribute | `[JsonPropertyName]` | `[YamlMember(Alias = "x")]` | `[JsonPropertyName]` (reused) | `[HumlProperty("x")]` — own attribute, no STJ coupling |
| Ignore attribute | `[JsonIgnore]` | `[YamlIgnore]` | `[JsonIgnore]` (reused) | `[HumlIgnore]` |
| Required property | `[JsonRequired]` / `required` keyword | None built-in | `[JsonRequired]` | Not in v1 scope; add when HUML spec defines it |
| Custom type converters | `JsonConverter<T>` subclass | Custom type converters via builder | Resolver-based (`ITomlTypeInfoResolver`) | `IHumlConverter<T>` — v1.x concern; seam designed now |
| Options object | `JsonSerializerOptions` | `SerializerBuilder` / `DeserializerBuilder` | `TomlSerializerOptions` | `HumlOptions` |
| Error location in exceptions | `JsonException.LineNumber` + `BytePositionInLine` | `(Line: N, Col: N, Idx: N)` string | `TomlException` with span info | `HumlParseException.Line` + `Column` as typed int properties |
| Spec versioning | N/A | N/A (YAML version is implicit) | N/A | `HumlOptions.SpecVersion` + `HumlUnsupportedVersionException` — unique differentiator |
| Source generation / AOT | Yes (`JsonSerializerContext`) | No | Yes (`TomlSerializerContext`) | v2 |
| Low-level reader/writer | `Utf8JsonReader` / `Utf8JsonWriter` | `IParser` / `IEmitter` | Not public | Not in v1 — expose `HumlNode` AST for edge cases |
| DOM / untyped access | `JsonDocument` (immutable), `JsonNode` (mutable) | `YamlDocument` object model | Model-level `TomlTable` | `HumlNode` AST — read-only, sufficient for edge cases |
| Multi-TFM targeting | `netstandard2.0;net8;net9;net10` | `netstandard2.0;netstandard2.1;net6;net8` | `netstandard2.0;net8;net10` | `netstandard2.1;net8;net9;net10` |
| Zero external runtime deps | Yes (inbox since .NET Core 3.0) | No (own NuGet package) | Yes | Yes — explicit constraint |
| Property emission order | Declared order | Alphabetical | Declared order | Declared order — .NET convention |

## Sources

- [System.Text.Json overview — Microsoft Learn](https://learn.microsoft.com/en-us/dotnet/standard/serialization/system-text-json/overview) — HIGH confidence
- [System.Text.Json.Serialization namespace — Microsoft Learn](https://learn.microsoft.com/en-us/dotnet/api/system.text.json.serialization?view=net-10.0) — HIGH confidence
- [YamlDotNet GitHub repository](https://github.com/aaubry/YamlDotNet) — HIGH confidence
- [Tomlyn official site and GitHub](https://xoofx.github.io/Tomlyn/) — HIGH confidence
- [System.Text.Json attribute documentation — Microsoft Learn](https://learn.microsoft.com/en-us/dotnet/standard/serialization/system-text-json/customize-properties) — HIGH confidence
- [Migrate from Newtonsoft.Json to System.Text.Json — Microsoft Learn](https://learn.microsoft.com/en-us/dotnet/standard/serialization/system-text-json/migrate-from-newtonsoft) — HIGH confidence; used to identify Newtonsoft features that became table stakes
- [5 steps to better NuGet package — Alex Klaus](https://alex-klaus.com/better-nuget/) — MEDIUM confidence; community best practices
- [Source Link and .NET libraries — Microsoft Learn](https://learn.microsoft.com/en-us/dotnet/standard/library-guidance/sourcelink) — HIGH confidence

---
*Feature research for: Huml.Net — .NET HUML parser/serialiser library*
*Researched: 2026-03-20*
