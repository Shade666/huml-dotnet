# Architecture Research

**Domain:** .NET parser/serialisation library (Lexer → Parser → AST → Ser/Deser pipeline)
**Researched:** 2026-03-20
**Confidence:** HIGH

---

## Standard Architecture

### System Overview

```
┌──────────────────────────────────────────────────────────────────────┐
│                        PUBLIC API LAYER                              │
│  ┌───────────────────────────────────────────────────────────────┐   │
│  │  static class Huml  — Serialize<T> / Deserialize<T> / Parse   │   │
│  └───────────────────────────────────────────────────────────────┘   │
│           │ HumlOptions (SpecVersion, VersionSource, ...)            │
├───────────┼──────────────────────────────────────────────────────────┤
│           │               PIPELINE LAYER                            │
│  ┌────────▼───────┐    ┌───────────────┐    ┌─────────────────────┐ │
│  │     Lexer      │───▶│    Parser     │───▶│   HumlDocument AST  │ │
│  │ (char → Token) │    │ (Token → AST) │    │ (HumlNode hierarchy)│ │
│  └────────────────┘    └───────────────┘    └──────────┬──────────┘ │
│                                                         │            │
│             ┌───────────────────────────────────────────┤            │
│             │                                           │            │
│  ┌──────────▼────────┐                   ┌─────────────▼─────────┐  │
│  │  HumlDeserializer │                   │   HumlSerializer      │  │
│  │  (AST → object)   │                   │   (object → string)   │  │
│  └───────────────────┘                   └───────────────────────┘  │
├─────────────────────────────────────────────────────────────────────┤
│                      SUPPORT LAYER                                  │
│  ┌──────────────┐  ┌─────────────────────┐  ┌────────────────────┐  │
│  │  Versioning  │  │     Attributes      │  │   Exceptions       │  │
│  │  HumlOptions │  │  [HumlProperty]     │  │  HumlParseEx       │  │
│  │  SpecVersion │  │  [HumlIgnore]       │  │  HumlSerializeEx   │  │
│  │  VersionPolicy│  └─────────────────────┘  │  HumlUnsupportedEx │  │
│  └──────────────┘                            └────────────────────┘  │
└─────────────────────────────────────────────────────────────────────┘
```

### Component Responsibilities

| Component | Responsibility | Communicates With |
|-----------|----------------|-------------------|
| `Huml` (static) | Public entry point; routes calls to Lexer/Parser/Ser/Deser; validates options | Lexer, Parser, HumlSerializer, HumlDeserializer |
| `Lexer` | Single-pass, line-oriented character scanning; emits `Token` stream; enforces whitespace/comment/indent rules | Parser (downstream consumer of token stream) |
| `Token` / `TokenType` | Value type carrying token kind, raw value, line, column, indent depth, SpaceBefore flag | Lexer (producer), Parser (consumer) |
| `Parser` | Recursive-descent; consumes token stream; enforces structural grammar; builds `HumlNode` tree | AST nodes (produces), error types |
| `HumlNode` hierarchy | Immutable AST: `HumlDocument`, `HumlMapping`, `HumlSequence`, `HumlScalar` | Parser (producer), Serializer/Deserializer (consumer), Linting package (future) |
| `HumlSerializer` | Reflects over a .NET object graph; emits HUML text; respects `[HumlProperty]` / `[HumlIgnore]`; writes `%HUML` header | HumlNode hierarchy (for `Huml.Parse` round-trip); Reflection; Versioning |
| `HumlDeserializer` | Consumes `HumlDocument` AST; maps keys to target type properties via reflection; handles type coercion | HumlNode hierarchy; Reflection; Attributes; Versioning |
| `HumlOptions` | Consumer-facing config: `SpecVersion`, `VersionSource`, `UnknownVersionBehaviour` | All pipeline stages (passed through) |
| `SpecVersionPolicy` | Internal constants: `MinimumSupported`, `Latest`; referenced by exception messages | Lexer, Parser, Deserializer, exception types |
| `HumlSpecVersion` (enum) | Typed version identifier; supports `>=` comparison; members `[Obsolete]`-tagged when they exit the support window | All version-gated code paths |
| `[HumlProperty]` / `[HumlIgnore]` | Annotation attributes on consumer types; control key name mapping and omission | HumlSerializer, HumlDeserializer (via reflection) |
| Exception types | Structured errors with line/column; `HumlParseException`, `HumlSerializeException`, `HumlUnsupportedVersionException` | All pipeline stages; consumers |

---

## Recommended Project Structure

```
Huml.Net/
├── src/
│   └── Huml.Net/
│       ├── Huml.Net.csproj              # Multi-TFM: netstandard2.1;net8.0;net9.0;net10.0
│       ├── Huml.cs                      # Static entry point — Serialize / Deserialize / Parse
│       ├── Versioning/
│       │   ├── HumlSpecVersion.cs       # enum with [Obsolete] deprecation path
│       │   ├── HumlOptions.cs           # Consumer-facing options; sealed; init-only
│       │   ├── VersionSource.cs         # enum: Options | Header
│       │   ├── UnknownVersionBehaviour.cs  # enum: Throw | UseLatest | UsePrevious
│       │   └── SpecVersionPolicy.cs     # internal constants; MinimumSupported, Latest
│       ├── Lexer/
│       │   ├── Lexer.cs                 # Character scanner; accepts ReadOnlySpan<char>
│       │   ├── Token.cs                 # readonly record struct Token(...)
│       │   └── TokenType.cs             # enum TokenType
│       ├── Parser/
│       │   ├── Parser.cs                # Recursive descent; consumes IReadOnlyList<Token>
│       │   └── Nodes/
│       │       ├── HumlNode.cs          # abstract record HumlNode(int Line, int Column)
│       │       ├── HumlDocument.cs      # record HumlDocument(IReadOnlyList<HumlMapping>)
│       │       ├── HumlMapping.cs       # record HumlMapping(string Key, HumlNode Value, ...)
│       │       ├── HumlSequence.cs      # record HumlSequence(IReadOnlyList<HumlNode>, ...)
│       │       └── HumlScalar.cs        # record HumlScalar(object? Value, ScalarKind, ...)
│       ├── Serialisation/
│       │   ├── HumlSerializer.cs        # object → HUML string via reflection
│       │   └── HumlDeserializer.cs      # HumlDocument → typed object via reflection
│       └── Attributes/
│           ├── HumlPropertyAttribute.cs # [HumlProperty("name", OmitIfDefault = true)]
│           └── HumlIgnoreAttribute.cs   # [HumlIgnore]
└── tests/
    └── Huml.Net.Tests/
        ├── Huml.Net.Tests.csproj        # xUnit + AwesomeAssertions; no production deps
        ├── LexerTests.cs
        ├── ParserTests.cs
        ├── SerializerTests.cs
        ├── DeserializerTests.cs
        ├── VersioningTests.cs
        └── SharedSuite/
            ├── SharedSuiteTests.cs      # [Theory] / [MemberData] runner
            └── Fixtures/
                ├── v0.1/               # git submodule pinned to huml-lang/tests@v0.1
                └── v0.2/               # git submodule pinned to huml-lang/tests@v0.2
```

### Structure Rationale

- **`Versioning/`:** Isolated so all spec-version logic has a single home; prevents version concerns bleeding into Lexer or Parser at the file level.
- **`Lexer/`:** Self-contained; only `Token` and `TokenType` cross the boundary to `Parser/`.
- **`Parser/Nodes/`:** AST nodes in their own subdirectory — they are consumed by the public API and by the future `Huml.Net.Linting` package; keeping them separate prevents accidental coupling.
- **`Serialisation/`:** Both directions co-located because they share reflection patterns and attribute-reading helpers; splitting them would force duplication.
- **`Attributes/`:** Separate folder because these are public API surface used by consumers, not internal implementation detail.
- **`SharedSuite/Fixtures/`:** Git submodule directories (one per spec version) sit under the test project, not the library — they are test data, never shipped in the NuGet package.

---

## Architectural Patterns

### Pattern 1: Token as `readonly record struct`

**What:** The `Token` type is a `readonly record struct` — value semantics, stack-allocated, compiler-synthesized equality, no heap allocation per token.

**When to use:** Whenever a type is small, has no identity (two tokens with the same fields are the same token), and is produced in large quantities.

**Trade-offs:** Stack allocation only works when the token stream is processed without long-lived storage. Storing tokens in a `List<Token>` still boxes them on the heap — use `ImmutableArray<Token>` or process the stream with an index to avoid that.

```csharp
public readonly record struct Token(
    TokenType Type,
    string? Value,
    int Line,
    int Column,
    int Indent,
    bool SpaceBefore = false
);
```

**Confidence:** HIGH — recommended by Microsoft and the C# community for small, short-lived value types in hot parsing paths. [Source](https://nietras.com/2021/06/14/csharp-10-record-struct/)

---

### Pattern 2: AST Nodes as `abstract record` hierarchy

**What:** The AST node base is an `abstract record`; subtypes are positional records. This gives: structural equality for free (useful in tests), immutability by default, `with`-expression support for tree transformations, and concise declaration syntax.

**When to use:** Read-only tree data structures where value equality simplifies testing and record inheritance is sufficient to model the hierarchy.

**Trade-offs:** `abstract record` inherits from `object`, not from another record class — the positional constructor chaining across inheritance levels requires explicit `: base(Line, Column)` calls. Records are reference types (unless `record struct`), so AST nodes do go on the heap — appropriate for a tree that outlives the parse call.

```csharp
public abstract record HumlNode(int Line, int Column);

public record HumlDocument(IReadOnlyList<HumlMapping> Entries)
    : HumlNode(0, 0);

public record HumlScalar(object? Value, ScalarKind Kind, int Line, int Column)
    : HumlNode(Line, Column);
```

**Confidence:** HIGH — record inheritance is well-supported in C# 9+ across all target TFMs (netstandard2.1 with C# 9 is supported via LangVersion=latest).

---

### Pattern 3: `ReadOnlySpan<char>` for Lexer Input

**What:** The lexer accepts `ReadOnlySpan<char>` as its input parameter, slicing subspans for token values rather than calling `string.Substring`. This eliminates heap allocations during scanning.

**When to use:** Any character-by-character scanner where the input is already in memory. Not suitable for streaming (Span cannot be stored on the heap or across await boundaries).

**Trade-offs:** `ReadOnlySpan<char>` is a `ref struct` — it cannot be a field, stored in a class, or passed across async boundaries. The lexer must complete synchronously, which is fine for file-based config parsing. Token values that must persist (e.g., key names, string literals) must be materialised to `string` before the lexer returns; slicing spans is safe for *reading* but not for *storing*.

```csharp
// Lexer constructor — span stays on stack; lexer is used and discarded synchronously
internal ref struct LexerState
{
    private ReadOnlySpan<char> _input;
    private int _pos;
    // ...
}

// Public Lexer class stores string internally; internal state uses span
internal sealed class Lexer
{
    public IReadOnlyList<Token> Tokenize(ReadOnlySpan<char> input) { ... }
}
```

Note: Because `ref struct` cannot be a class field, the lexer design should keep span-based scanning inside a method or a separate `ref struct` helper. The outer `Lexer` class receives the span, processes it, and returns materialised `Token` objects.

**Confidence:** HIGH — this is the pattern used by `Utf8JsonReader` in System.Text.Json and is widely documented. [Source](https://learn.microsoft.com/en-us/dotnet/standard/serialization/system-text-json/use-utf8jsonreader)

---

### Pattern 4: Single-Pass Version-Gated Code Path

**What:** Version-specific behaviour is expressed as `if (_options.SpecVersion >= HumlSpecVersion.V0_2)` branches inside the single Lexer/Parser code path. No subclassing, no strategy objects, no forked files.

**When to use:** When spec versions differ in a small number of rules rather than a fundamentally different grammar. Keeps divergence points searchable by grep.

**Trade-offs:** Gate count grows with each new version — manageable while the spec is in pre-1.0. If a v1.0 → v2.0 change rewrites the grammar fundamentally, revisit. For now, `>=` comparisons on an `int`-backed enum are essentially free.

```csharp
private void ValidateComment(int line, int col)
{
    // Rule added in v0.2: '#' must be followed by exactly one space
    if (_options.SpecVersion >= HumlSpecVersion.V0_2 && Peek() != ' ')
        throw new HumlParseException("Comment '#' must be followed by a space", line, col);
}
```

**Confidence:** HIGH — this is the approach described in the PRD and mirrors the go-huml implementation intent. The `>=` convention makes the direction of change self-documenting.

---

### Pattern 5: Reflection-Based Ser/Deser with Attribute Caching

**What:** Serializer and Deserializer use `System.Reflection` to enumerate `PropertyInfo` objects and read `[HumlProperty]` / `[HumlIgnore]` attributes. For v1, raw reflection is acceptable; attribute results should be cached per-type in a `ConcurrentDictionary<Type, PropertyDescriptor[]>` to avoid re-reflecting on every call.

**When to use:** Any serialisation library targeting arbitrary consumer types without source generation. Sufficient for config-file parsing workloads.

**Trade-offs:** Reflection is slower than source-generated code. For the config-file use case, the parse overhead dominates, and per-call reflection cost is acceptable. Source generators (a v2 concern per the PRD) would replace this for AOT / trimming scenarios.

```csharp
private static readonly ConcurrentDictionary<Type, PropertyDescriptor[]> _cache = new();

private static PropertyDescriptor[] GetDescriptors(Type type) =>
    _cache.GetOrAdd(type, t => t
        .GetProperties(BindingFlags.Public | BindingFlags.Instance)
        .Where(p => p.GetCustomAttribute<HumlIgnoreAttribute>() is null)
        .Select(p => new PropertyDescriptor(p, p.GetCustomAttribute<HumlPropertyAttribute>()))
        .ToArray());
```

**Confidence:** HIGH — established pattern in System.Text.Json, Newtonsoft.Json, and all major .NET serialisers.

---

### Pattern 6: Multi-TFM with `#if` for API Surface Polyfills Only

**What:** Multi-target `netstandard2.1;net8.0;net9.0;net10.0`. Conditional compilation (`#if NET8_0_OR_GREATER`) should be used sparingly and only for API surface that genuinely differs across TFMs (e.g., `SearchValues<char>` in .NET 8, `CollectionsMarshal` helpers). The core pipeline logic must be identical across all TFMs.

**When to use:** When a modern TFM provides a meaningfully faster or allocation-reduced API for the same operation.

**Trade-offs:** `#if` blocks increase maintenance surface. Prefer writing netstandard2.1-compatible code and using `#if` only when the performance gain is measurable (proven by benchmarks). Avoid using `#if` to gate behaviour — that is the job of `HumlSpecVersion` gates.

```csharp
// Example: SearchValues is .NET 8+ only
#if NET8_0_OR_GREATER
private static readonly SearchValues<char> _bareKeyChars =
    SearchValues.Create("abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789_-");
#endif

private bool IsBareKeyChar(char c)
{
#if NET8_0_OR_GREATER
    return _bareKeyChars.Contains(c);
#else
    return char.IsLetterOrDigit(c) || c == '_' || c == '-';
#endif
}
```

**Confidence:** MEDIUM — established best practice for .NET library authors ([Cross-platform targeting guide](https://learn.microsoft.com/en-us/dotnet/standard/library-guidance/cross-platform-targeting)), but the specific APIs worth gating require benchmarking per project.

---

### Pattern 7: xUnit Theory + MemberData for File-Fixture-Driven Tests

**What:** The shared suite runner reads all `.huml` fixture files from a version-specific directory and feeds them as `[Theory]` rows via a static `MemberData` method. Each test row contains the input text and, for valid-document fixtures, the expected parsed result (loaded from a companion `.json` or `.yaml` expectation file if the suite provides them).

**When to use:** Any spec-compliance test suite where fixtures live on disk and must be enumerated at test discovery time.

**Trade-offs:** `MemberData` methods run at test *discovery*, not just at test *execution* — if fixture loading throws, the test runner reports a collection error, not a test failure. Wrap file I/O defensively. Also: `Environment.CurrentDirectory` during test runs is not the project root; use `Path.Combine(AppContext.BaseDirectory, "Fixtures", version)` or embed fixture paths relative to the test assembly.

```csharp
public static IEnumerable<object[]> ValidDocumentFixtures(string version)
{
    var dir = Path.Combine(AppContext.BaseDirectory, "Fixtures", version, "valid");
    foreach (var file in Directory.EnumerateFiles(dir, "*.huml"))
        yield return new object[] { File.ReadAllText(file), Path.GetFileNameWithoutExtension(file) };
}

[Theory]
[MemberData(nameof(ValidDocumentFixtures), "v0.2")]
public void Parse_ValidDocument_ShouldSucceed(string input, string fixtureName)
{
    var act = () => Huml.Parse(input, new HumlOptions { SpecVersion = HumlSpecVersion.V0_2 });
    act.Should().NotThrow(because: $"fixture '{fixtureName}' is valid HUML");
}
```

Fixture directories must be marked `<CopyToOutputDirectory>PreserveNewest</CopyToOutputDirectory>` in the test `.csproj`, or the git submodule paths must be resolved relative to the repo root via a `Directory.Build.props` constant.

**Confidence:** HIGH — this pattern is standard for compliance-suite-driven .NET libraries. [Source](https://andrewlock.net/creating-parameterised-tests-in-xunit-with-inlinedata-classdata-and-memberdata/)

---

## Data Flow

### Deserialisation Flow

```
string / ReadOnlySpan<char>
    │
    ▼
Lexer.Tokenize(input, options)
    │   ReadOnlySpan<char> scanned character-by-character
    │   Version gates applied (e.g. comment rules, trailing whitespace)
    │   Token values materialised to string for keys/literals
    ▼
IReadOnlyList<Token>
    │
    ▼
Parser.Parse(tokens, options)
    │   Recursive descent: document → mappings → scalars/sequences
    │   Indent tracking drives nesting depth
    │   Version gates applied (e.g. inline list rules)
    │   HumlParseException thrown with line+col on structural errors
    ▼
HumlDocument (root AST node)
    │
    ▼
HumlDeserializer.Deserialize<T>(document, options)
    │   Reflects target type T
    │   Maps HumlMapping keys to PropertyInfo (respecting [HumlProperty])
    │   Skips [HumlIgnore] properties
    │   Recursively maps HumlSequence → IEnumerable<T>
    │   Recursively maps nested HumlDocument → nested POCO
    ▼
T (populated instance)
```

### Serialisation Flow

```
T (any .NET object)
    │
    ▼
HumlSerializer.Serialize<T>(value, options)
    │   Reflects type T; enumerates properties in declaration order
    │   Applies [HumlProperty] name override and OmitIfDefault
    │   Skips [HumlIgnore] properties
    │   Recursively handles IEnumerable<T>, Dictionary<K,V>, nested POCOs
    │   Version gates: e.g. multiline """ strings only for V0_2+
    │   Emits %HUML vX.Y.Z header matching options.SpecVersion
    ▼
string (valid HUML document)
```

### Version Resolution Flow (Header Auto-Detect)

```
Document string
    │
    ▼  (VersionSource.Header)
Lexer scans first line for %HUML directive
    │
    ├─ Found: parse version string → look up HumlSpecVersion enum member
    │     ├─ In support window  → use that version
    │     └─ Outside window     → apply UnknownVersionBehaviour
    │           ├─ Throw        → HumlUnsupportedVersionException
    │           ├─ UseLatest    → SpecVersionPolicy.Latest
    │           └─ UsePrevious  → nearest older supported version
    │
    └─ Not found: fall back to HumlOptions.SpecVersion (explicit)
```

---

## Build Order (Phase Dependencies)

The components have hard dependencies that dictate implementation order:

```
1. Versioning types (HumlSpecVersion, HumlOptions, SpecVersionPolicy)
       │  — no dependencies; must exist before any pipeline code
       ▼
2. Token types (TokenType enum, Token readonly record struct)
       │  — no dependencies; required by Lexer and Parser
       ▼
3. Lexer
       │  — depends on: Token types, Versioning types
       ▼
4. AST Node hierarchy (HumlNode, HumlDocument, HumlMapping, ...)
       │  — no dependencies; required by Parser, Serializer, Deserializer
       ▼
5. Parser
       │  — depends on: Token types, AST nodes, Versioning types, exceptions
       ▼
6. Attributes ([HumlProperty], [HumlIgnore])
       │  — no dependencies; required by Serializer and Deserializer
       ▼
7. HumlSerializer
       │  — depends on: AST nodes (optional, for low-level round-trip), Versioning, Attributes
       ▼
8. HumlDeserializer
       │  — depends on: AST nodes, Versioning, Attributes
       ▼
9. Static Huml entry point
       └  — depends on: all of the above
```

The AST nodes (step 4) can be defined in parallel with the Lexer (step 3) because neither depends on the other; they converge in the Parser (step 5).

---

## Anti-Patterns

### Anti-Pattern 1: One Lexer/Parser Class Per Spec Version

**What people do:** Create `LexerV01` and `LexerV02` (or subdirectories) to keep version logic separate.

**Why it's wrong:** Doubles the maintenance surface immediately. When v0.3 ships, you need `LexerV03`. Common bug fixes must be applied in multiple places. Divergence points are scattered across files rather than being greppable `if (>= V0_2)` branches.

**Do this instead:** Single `Lexer` and `Parser` with explicit version-gate `if` blocks at each point of behavioural divergence. Use `// VERSION GATE: introduced v0.2` comments for discoverability.

---

### Anti-Pattern 2: Storing `ReadOnlySpan<char>` in a Class Field

**What people do:** Try to hold onto a span in a class-level field to avoid re-passing it through method calls.

**Why it's wrong:** `ReadOnlySpan<char>` is a `ref struct` — the compiler forbids storing it in a class or non-ref struct field. Attempting this causes a compile error on all target TFMs.

**Do this instead:** Pass the span as a method parameter or encapsulate scanning state in a `ref struct` helper that lives entirely on the stack within a single synchronous method call. Materialise substrings (`.ToString()`) when a value must outlive the span.

---

### Anti-Pattern 3: Reflecting on Every Deserialisation Call Without Caching

**What people do:** Call `typeof(T).GetProperties()` and `prop.GetCustomAttribute<HumlPropertyAttribute>()` inside the hot deserialisation loop on every call.

**Why it's wrong:** Reflection is significantly slower than compiled code. For repeated deserialisation of the same type (the common case in a config library), the property list and attribute values never change — re-computing them per call is pure waste.

**Do this instead:** Cache `PropertyDescriptor[]` in a `static ConcurrentDictionary<Type, PropertyDescriptor[]>`. Populate lazily on first use per type; all subsequent calls hit the dictionary.

---

### Anti-Pattern 4: Emitting Properties in Alphabetical Order

**What people do:** Sort properties alphabetically during serialisation (go-huml's default behaviour).

**Why it's wrong:** .NET developers expect properties to appear in the order they are declared in source. `System.Text.Json` preserves declaration order. Alphabetical output surprises C# consumers who write `Host` before `Port` in their class and get `Port` first in the HUML file.

**Do this instead:** Use `GetProperties()` with `BindingFlags.Instance | BindingFlags.Public` and rely on the runtime's reflection ordering, which preserves declaration order for POCO classes. Do not sort.

---

### Anti-Pattern 5: Letting Linting Logic Accrete Into the Core Parser

**What people do:** Add "advisory" diagnostics, version-drift checks, or style warnings inside `Lexer` or `Parser` as they seem related.

**Why it's wrong:** It violates single responsibility and makes the parser output non-deterministic from a consumer perspective (should `HumlParseException` be thrown? should a warning list be returned?). The `Huml.Net.Linting` package is designed to consume `HumlDocument` from outside — it needs no access to parser internals.

**Do this instead:** The parser throws or succeeds. No warnings, no advisories, no diagnostics. All version-drift and style logic belongs in a `HumlLinter` class in a separate `Huml.Net.Linting` package that takes `HumlDocument` as input.

---

### Anti-Pattern 6: Catching Version Errors Silently Inside the Lexer

**What people do:** Quietly fall back to the latest version when an unknown `%HUML` header is encountered, regardless of options.

**Why it's wrong:** Silent fallback hides misconfigured documents. The spec version is a correctness concern — parsing a v0.3 document with v0.2 rules may silently produce wrong results rather than failing loudly.

**Do this instead:** Default `UnknownVersionBehaviour` is `Throw`. The caller opts in to silent fallback explicitly by setting `UseLatest` or `UsePrevious`. Never make the silent behaviour the default.

---

## Integration Points

### External Boundaries

| Boundary | Direction | Notes |
|----------|-----------|-------|
| `Huml.cs` public API → consumer code | Outbound | Only public surface; all internal types are `internal` |
| `HumlDocument` AST → `Huml.Net.Linting` | Outbound (future) | Linting package references Huml.Net; core has zero reference back |
| git submodule fixtures → test project | Inbound (test only) | `huml-lang/tests` at `v0.1` and `v0.2` tags; never shipped in NuGet |
| NuGet package → consumer | Outbound | `netstandard2.1` fallback + per-TFM optimised builds; single package ID |

### Internal Boundaries

| Boundary | Communication | Rule |
|----------|---------------|------|
| Lexer → Parser | `IReadOnlyList<Token>` passed by value | Parser must not call back into Lexer |
| Parser → Ser/Deser | `HumlDocument` (root AST node) | Serializer/Deserializer must not re-invoke Parser |
| Pipeline → Options | `HumlOptions` passed at call site | Options are read-only within the pipeline; never mutated |
| Ser/Deser → Attributes | `GetCustomAttribute<T>()` | Reflection; cached per type; no direct attribute dependency in pipeline |
| Core library → Linting | None (zero coupling) | Core has no `using` or project reference to linting package |

---

## Scaling Considerations

This is a library, not a service — "scale" means throughput per call, not concurrent users. Relevant throughput tiers for a config-file library:

| Scenario | Architecture Adjustments |
|----------|--------------------------|
| Single config load at startup | Raw reflection is fine; no caching needed |
| Hot-path repeated deserialisation (e.g. per-request config reload) | Type cache in `ConcurrentDictionary` becomes critical |
| AOT / trimmed deployments (future v2) | Source generators replace reflection; current architecture cannot support this without significant rework |
| Very large HUML files (>1 MB) | `ReadOnlySpan<char>` lexer avoids per-token allocations; parser still builds full AST in memory — streaming would require redesign (out of scope) |

---

## Sources

- [go-huml reference implementation](https://github.com/huml-lang/go-huml) — primary architecture reference; MEDIUM confidence (source code structure inferred from repository overview)
- [System.Text.Json / Utf8JsonReader internals](https://learn.microsoft.com/en-us/dotnet/standard/serialization/system-text-json/use-utf8jsonreader) — HIGH confidence; official Microsoft documentation
- [C# record structs deep dive](https://nietras.com/2021/06/14/csharp-10-record-struct/) — HIGH confidence; authoritative technical post with benchmarks
- [Cross-platform targeting for .NET libraries](https://learn.microsoft.com/en-us/dotnet/standard/library-guidance/cross-platform-targeting) — HIGH confidence; official Microsoft library guidance
- [xUnit MemberData parameterised tests](https://andrewlock.net/creating-parameterised-tests-in-xunit-with-inlinedata-classdata-and-memberdata/) — HIGH confidence; Andrew Lock's authoritative xUnit guide
- [ReadOnlySpan<char> performance patterns](https://nhonvo.github.io/posts/2025-09-07-high-performance-net-with-span-and-memory/) — MEDIUM confidence; community post, consistent with official docs
- [C# record types — Microsoft Learn](https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/builtin-types/record) — HIGH confidence; official language reference

---

*Architecture research for: Huml.Net — .NET HUML parser/serialisation library*
*Researched: 2026-03-20*
