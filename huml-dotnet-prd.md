# PRD: Huml.Net — HUML Parser Library for .NET

**Status:** Draft  
**Author:** Richard (Radberi)  
**Reference Implementation:** [go-huml](https://github.com/huml-lang/go-huml)  
**Spec:** [huml.io](https://huml.io)  
**Shared Test Suite:** [huml-lang/tests](https://github.com/huml-lang/tests)  
**Target Framework:** .NET 9+ (netstandard2.1 secondary target for compatibility)  
**Language:** C# 13  
**Licence:** MIT

---

## 1. Overview

`Huml.Net` is a .NET library for parsing, serialising, and deserialising HUML (Human-oriented Markup Language) documents. HUML is a strict, human-readable serialisation format with unambiguous syntax, mandatory quoting of strings, explicit type literals, and comment support — positioned as a safer alternative to YAML.

The public API surface mirrors the conventions of `System.Text.Json` so that .NET developers encounter minimal friction.

---

## 2. Goals

- Full HUML v0.2.0 spec compliance, validated against the shared `huml-lang/tests` test suite.
- `System.Text.Json`-style API: `Huml.Serialize<T>()` / `Huml.Deserialize<T>()`.
- Attribute-driven property mapping: `[HumlProperty]`, `[HumlIgnore]`.
- Zero external runtime dependencies.
- TDD from day one — shared test suite consumed as Red/Green fixtures before implementation.
- NuGet-publishable with correct metadata, XML doc comments, and a README.

### Out of Scope (v1)

- Source generator / AOT support (future v2 concern).
- Streaming / `IAsyncEnumerable` parsing.
- Schema validation.
- HUML → JSON / YAML round-trip converters.
- Linting, version drift diagnostics, and style advisories (future `Huml.Net.Linting` package — see §14).

---

## 3. Reference Implementation Mapping

The Go implementation (`go-huml`) is the primary reference. Its architecture maps cleanly to C#:

| Go file | Responsibility | C# target |
|---|---|---|
| `token.go` | Token types and Token struct | `TokenType` enum + `Token` readonly record struct |
| `lexer.go` | Character-level tokenisation (~794 LOC) | `Lexer` class with `ReadOnlySpan<char>`-based scanning |
| `parser.go` | Recursive descent AST construction | `Parser` class producing `HumlNode` hierarchy |
| `decode.go` | Reflection-based deserialisation | `HumlDeserializer` using `System.Reflection` |
| `encode.go` | Reflection-based serialisation | `HumlSerializer` using `System.Reflection` |

---

## 4. Project Structure

```
Huml.Net/
├── src/
│   └── Huml.Net/
│       ├── Huml.Net.csproj
│       ├── Huml.cs                  # Static entry point
│       ├── Versioning/
│       │   ├── HumlSpecVersion.cs   # Enum of supported spec versions
│       │   ├── HumlOptions.cs       # Consumer-facing options incl. version
│       │   └── SpecVersionPolicy.cs # Internal support window constants
│       ├── Lexer/
│       │   ├── Lexer.cs
│       │   ├── Token.cs
│       │   └── TokenType.cs
│       ├── Parser/
│       │   ├── Parser.cs
│       │   └── Nodes/
│       │       ├── HumlNode.cs      # Abstract base
│       │       ├── HumlDocument.cs
│       │       ├── HumlMapping.cs
│       │       ├── HumlSequence.cs
│       │       └── HumlScalar.cs
│       ├── Serialisation/
│       │   ├── HumlSerializer.cs
│       │   └── HumlDeserializer.cs
│       └── Attributes/
│           ├── HumlPropertyAttribute.cs
│           └── HumlIgnoreAttribute.cs
└── tests/
    └── Huml.Net.Tests/
        ├── Huml.Net.Tests.csproj
        ├── LexerTests.cs
        ├── ParserTests.cs
        ├── SerializerTests.cs
        ├── DeserializerTests.cs
        ├── VersioningTests.cs       # Version dispatch, unsupported version errors
        └── SharedSuite/
            ├── SharedSuiteTests.cs  # Theory-driven runner
            └── Fixtures/
                ├── v0.1/            # git submodule pinned to tests@v0.1
                └── v0.2/            # git submodule pinned to tests@v0.2
```

---

## 5. Public API

### 5.1 Static Entry Point

```csharp
namespace HumlNet;

public static class Huml
{
    // Deserialisation
    public static T? Deserialize<T>(string huml, HumlOptions? options = null);
    public static T? Deserialize<T>(ReadOnlySpan<char> huml, HumlOptions? options = null);
    public static object? Deserialize(string huml, Type targetType, HumlOptions? options = null);

    // Serialisation
    public static string Serialize<T>(T value, HumlOptions? options = null);
    public static string Serialize(object? value, Type inputType, HumlOptions? options = null);

    // Low-level access
    public static HumlDocument Parse(string huml, HumlOptions? options = null);
}
```

### 5.2 Version Options

```csharp
public sealed class HumlOptions
{
    /// <summary>Parse/serialise using the declared spec version. Version source is Options.</summary>
    public static readonly HumlOptions Default = new();

    /// <summary>Detect spec version from the %HUML header in the document.</summary>
    public static readonly HumlOptions AutoDetect = new()
        { VersionSource = VersionSource.Header };

    /// <summary>Which spec version to use when VersionSource is Options.</summary>
    public HumlSpecVersion SpecVersion { get; init; } = HumlSpecVersion.V0_2;

    /// <summary>Whether to use the version from Options or from the document header.</summary>
    public VersionSource VersionSource { get; init; } = VersionSource.Options;

    /// <summary>Behaviour when the document declares a version outside the support window.</summary>
    public UnknownVersionBehaviour UnknownVersionBehaviour { get; init; }
        = UnknownVersionBehaviour.Throw;
}

public enum VersionSource
{
    Options,   // Use HumlOptions.SpecVersion explicitly
    Header,    // Detect from %HUML header; fall back to SpecVersion if absent
}

public enum UnknownVersionBehaviour
{
    Throw,       // Throw HumlUnsupportedVersionException (default; safe)
    UseLatest,   // Silently parse with the latest supported version
    UsePrevious, // Silently parse with the nearest older supported version
}
```

### 5.3 Attributes

```csharp
// Rename a property in HUML output/input (analogous to [JsonPropertyName])
[AttributeUsage(AttributeTargets.Property)]
public sealed class HumlPropertyAttribute(string name) : Attribute
{
    public string Name { get; } = name;
    public bool OmitIfDefault { get; init; } = false;
}

// Exclude a property from serialisation/deserialisation (analogous to [JsonIgnore])
[AttributeUsage(AttributeTargets.Property)]
public sealed class HumlIgnoreAttribute : Attribute;
```

### 5.4 Usage Examples

```csharp
// Deserialise
var config = Huml.Deserialize<ServerConfig>(humlString);

// Serialise
string output = Huml.Serialize(config);

// Type with attributes
public class ServerConfig
{
    [HumlProperty("host")]
    public string Host { get; set; } = string.Empty;

    [HumlProperty("port", OmitIfDefault = true)]
    public int Port { get; set; }

    [HumlIgnore]
    public string InternalToken { get; set; } = string.Empty;
}
```

---

## 6. Lexer Specification

The lexer is the most complex component. Implemented as a single-pass, line-oriented tokeniser. Key behaviours ported from `go-huml/lexer.go`:

### 6.1 Token Types

```csharp
public enum TokenType
{
    // Structure
    Eof, Error,
    // Directives
    Version,          // %HUML v0.x.x header
    // Keys
    Key,              // bare key:
    QuotedKey,        // "quoted key":
    // Indicators
    ScalarIndicator,  // :  (single colon — scalar value follows)
    VectorIndicator,  // :: (double colon — list/map block follows)
    ListItem,         // -  (list item marker)
    Comma,            // ,  (inline list separator)
    // Scalars
    String,           // "value" or """ multiline
    Int,              // 42, 0xFF, 0o77, 0b1010
    Float,            // 3.14, 1e10
    Bool,             // true | false
    Null,             // null
    NaN,              // nan
    Inf,              // +inf | -inf
    // Empty collections
    EmptyList,        // []
    EmptyDict,        // {}
}
```

### 6.2 Lexer Rules (from spec and go-huml)

- **Version directive:** First line only. `%HUML v0.2.0`. Consumed silently; not exposed as a token to the parser.
- **Indentation:** Spaces only. Two spaces per level. Tabs are an error.
- **Trailing whitespace:** Any trailing space on any line is a parse error.
- **Comments:** `#` must be followed by a single space (`# comment text`). Inline comments are allowed after values.
- **Bare keys:** `[a-zA-Z][a-zA-Z0-9_-]*` — no spaces, no quotes required.
- **Quoted keys:** Double-quoted. Same escape rules as quoted strings.
- **Strings:** Must be double-quoted. Unquoted bare words that are not keywords (`true`, `false`, `null`, `nan`, `inf`) are parse errors.
- **Multiline strings:** Delimited by `"""`. Opening `"""` must be at end of key line. Content indented `keyIndent + 2` spaces. Closing `"""` at `keyIndent`. Leading `keyIndent + 2` spaces stripped per line. Trailing newline stripped from result.
- **Numbers:** Decimal, `0x` hex, `0o` octal, `0b` binary. Underscores allowed as separators (`1_000_000`). `+inf`, `-inf`, `nan` are valid float keywords.
- **Inline lists:** Comma-separated values. Space before comma is an error. Single space after comma required.

### 6.3 Token Record

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

---

## 7. Parser Specification

Recursive descent parser consuming the token stream produced by the Lexer.

### 7.1 AST Node Hierarchy

```csharp
public abstract record HumlNode(int Line, int Column);

public record HumlDocument(IReadOnlyList<HumlMapping> Entries) : HumlNode(0, 0);

public record HumlMapping(
    string Key,
    HumlNode Value,
    int Line, int Column
) : HumlNode(Line, Column);

public record HumlSequence(
    IReadOnlyList<HumlNode> Items,
    int Line, int Column
) : HumlNode(Line, Column);

public record HumlScalar(
    object? Value,      // string | long | double | bool | null
    ScalarKind Kind,
    int Line, int Column
) : HumlNode(Line, Column);

public enum ScalarKind { String, Integer, Float, Bool, Null, NaN, Inf }
```

### 7.2 Parser Rules

- A document is a sequence of key-value mappings at indent 0.
- After a `ScalarIndicator` (`:`), the next token is a scalar value on the same line.
- After a `VectorIndicator` (`::`), the next block is either:
  - An indented sequence of `- value` list items, or
  - An indented mapping block (nested object).
- Inline lists: comma-separated values between the `ScalarIndicator` and end-of-line.
- Indent changes drive nesting; a decrease in indent signals the end of a block.
- Parser tracks a current indent level and validates against expected indent on each key.

---

## 8. Serialiser Specification

### 8.1 Type Mapping

| .NET type | HUML output |
|---|---|
| `string` | `"value"` (double-quoted, escaped) |
| `bool` | `true` / `false` |
| `int`, `long`, `short` | bare integer |
| `float`, `double`, `decimal` | bare float |
| `double.NaN` | `nan` |
| `double.PositiveInfinity` | `+inf` |
| `double.NegativeInfinity` | `-inf` |
| `null` | `null` |
| `IEnumerable<T>` | `::` vector block with `- item` lines |
| POCO/record | `::` nested mapping block |
| `Dictionary<string, T>` | `::` nested mapping block |
| Empty `IEnumerable` | `[]` |
| Empty `IDictionary` | `{}` |

### 8.2 Serialiser Behaviour

- Emits `%HUML vX.Y.Z` header matching `HumlOptions.SpecVersion` (defaults to latest supported).
- Two-space indent per level.
- Keys are bare if they match `[a-zA-Z][a-zA-Z0-9_-]*`, otherwise double-quoted.
- String values with newlines serialised as multiline `"""` blocks (where supported by the target spec version; falls back to escaped `\n` for older versions).
- `[HumlProperty(OmitIfDefault = true)]` skips default/zero/empty values.
- `[HumlIgnore]` skips property entirely.
- Properties are emitted in declaration order (not sorted, unlike go-huml which sorts alphabetically — .NET convention is declaration order).

---

## 9. Error Handling

All errors include line and column number. Public API surfaces exceptions:

```csharp
public class HumlParseException(string message, int line, int column)
    : Exception($"Line {line}, Col {column}: {message}");

public class HumlSerializeException(string message, Exception? inner = null)
    : Exception(message, inner);

public class HumlUnsupportedVersionException(string declaredVersion)
    : Exception(
        $"HUML spec version '{declaredVersion}' is outside the supported window. " +
        $"Supported versions: {SpecVersionPolicy.MinimumSupported} – {SpecVersionPolicy.Latest}.");
```

Lexer errors terminate parsing immediately (no error recovery in v1).

---

## 10. Testing Strategy

### 10.1 Shared Suite (Priority 1 — TDD Red/Green)

Consume [`huml-lang/tests`](https://github.com/huml-lang/tests) as git submodules pinned to per-version tags. Each fixture file represents a valid or invalid HUML document for a specific spec version. These drive the initial TDD cycle before any production implementation.

Fixture directories map to submodule pins:

| Directory | Submodule tag | Purpose |
|---|---|---|
| `tests/Fixtures/v0.1/` | `tests@v0.1` | Legacy spec regression |
| `tests/Fixtures/v0.2/` | `tests@v0.2` | Current minimum supported spec |

```csharp
// Version-parameterised theory runner
[Theory]
[MemberData(nameof(ValidDocumentFixtures), "v0.2")]
public void Parse_V02_ValidDocument_ShouldSucceed(string input, object expected)
{
    var doc = Huml.Parse(input, new HumlOptions { SpecVersion = HumlSpecVersion.V0_2 });
    doc.Should().BeEquivalentTo(expected);
}

[Theory]
[MemberData(nameof(InvalidDocumentFixtures), "v0.2")]
public void Parse_V02_InvalidDocument_ShouldThrowHumlParseException(string input)
{
    var act = () => Huml.Parse(input, new HumlOptions { SpecVersion = HumlSpecVersion.V0_2 });
    act.Should().Throw<HumlParseException>();
}
```

Both fixture suites must pass in CI. Adding a new spec version requires adding a fixture directory and a corresponding suite class before any parser changes are made — the Red/Green discipline applies at the version level too.

### 10.2 Unit Test Coverage Targets

| Component | Focus areas |
|---|---|
| Lexer | All token types; trailing space; bad comment format; unclosed strings; multiline edge cases; version-gated rule differences |
| Parser | Nesting; indent errors; inline lists; empty collections; mixed types |
| Serialiser | Round-trip fidelity; special floats; multiline strings; OmitIfDefault; HumlIgnore; correct version header emitted |
| Deserialiser | Struct mapping; quoted/bare keys; list-to-array; list-to-`List<T>`; type coercion errors |
| Versioning | AutoDetect from header; unsupported version throws; `UnknownVersionBehaviour` variants; `[Obsolete]` on dropped versions |

### 10.3 Test Dependencies

- `xUnit`
- `AwesomeAssertions` (per project convention — not FluentAssertions)

---

## 11. NuGet Package Metadata

```xml
<PropertyGroup>
  <PackageId>Huml.Net</PackageId>
  <Version>0.1.0</Version>
  <Authors>Richard [Radberi]</Authors>
  <Description>A .NET library for parsing and serialising HUML (Human-oriented Markup Language) documents.</Description>
  <PackageLicenseExpression>MIT</PackageLicenseExpression>
  <PackageProjectUrl>https://github.com/[repo]</PackageProjectUrl>
  <RepositoryUrl>https://github.com/[repo]</RepositoryUrl>
  <PackageTags>huml;serialisation;configuration;parser</PackageTags>
  <GenerateDocumentationFile>true</GenerateDocumentationFile>
  <Nullable>enable</Nullable>
  <LangVersion>latest</LangVersion>
</PropertyGroup>
```

---

## 12. Implementation Phases

### Phase 1 — Tokeniser (Week 1–2)

- [ ] Define `HumlSpecVersion` enum, `HumlOptions`, `SpecVersionPolicy` constants
- [ ] Define `TokenType` enum and `Token` record struct
- [ ] Implement `Lexer` class from `go-huml/lexer.go` reference
- [ ] Version-gated rule stubs in lexer (even if only one version active initially)
- [ ] Lexer unit tests covering all token types
- [ ] Shared suite `v0.2/` invalid-document tests passing

### Phase 2 — Parser (Week 2–3)

- [ ] Define AST node hierarchy
- [ ] Implement recursive descent `Parser`
- [ ] Parser unit tests: nesting, indent validation, inline lists
- [ ] Shared suite `v0.2/` valid-document parse tests passing
- [ ] Shared suite `v0.1/` tests passing (add version gates as needed)

### Phase 3 — Serialiser + Deserialiser (Week 3–4)

- [ ] Implement `HumlSerializer` with reflection and version-aware header/feature output
- [ ] Implement `HumlDeserializer` with reflection
- [ ] Implement `[HumlProperty]` and `[HumlIgnore]` attributes
- [ ] Round-trip tests per supported spec version
- [ ] `HumlUnsupportedVersionException` and `UnknownVersionBehaviour` tests

### Phase 4 — Polish + Packaging (Week 4–5)

- [ ] XML doc comments on all public members
- [ ] `[Obsolete]` applied to any spec versions dropping out of support window
- [ ] README with usage examples including versioning options
- [ ] CI pipeline (GitHub Actions, `dotnet test` — both fixture suites)
- [ ] NuGet pack + publish workflow

---

## 13. Versioning Strategy

### 13.1 Version-Aware Options

Spec version is a first-class concern, not an afterthought. All parse and serialise operations accept `HumlOptions` (§5.2). The version source can be explicit (caller-specified) or automatic (read from the `%HUML` header).

```csharp
// Explicit version
var doc = Huml.Parse(input, new HumlOptions { SpecVersion = HumlSpecVersion.V0_2 });

// Auto-detect from %HUML header; throw if unsupported
var doc = Huml.Parse(input, HumlOptions.AutoDetect);

// Auto-detect; fall back to latest if header absent or unrecognised
var doc = Huml.Parse(input, new HumlOptions
{
    VersionSource = VersionSource.Header,
    UnknownVersionBehaviour = UnknownVersionBehaviour.UseLatest
});
```

### 13.2 Behaviour Gating

Breaking spec changes are isolated as versioned behaviour gates inside the existing lexer and parser pipeline. There are **no forked parser classes** — a single code path, with explicit conditional branches at each point of divergence.

Convention: all gates use `>=` comparisons against the `HumlSpecVersion` enum, making the divergence points searchable and the direction of change self-documenting.

```csharp
// Inside Lexer — example of a spec-gated rule
private void ValidateComment()
{
    // Rule introduced in v0.2: # must be followed by a space
    if (_options.SpecVersion >= HumlSpecVersion.V0_2)
    {
        if (Peek() != ' ')
            throw new HumlParseException("Comment '#' must be followed by a space", ...);
    }
}
```

When a new spec version ships: add a constant to the enum, add gates for each behavioural change, write tests for both sides of each gate. No existing code paths are deleted until the old version exits the support window.

### 13.3 Internal Policy Constants

The support window is enforced in code, not just documentation:

```csharp
internal static class SpecVersionPolicy
{
    /// <summary>Oldest spec version within the current support window.</summary>
    public const HumlSpecVersion MinimumSupported = HumlSpecVersion.V0_2;

    /// <summary>Latest known spec version.</summary>
    public const HumlSpecVersion Latest = HumlSpecVersion.V0_2;
}
```

`HumlUnsupportedVersionException` references these constants in its message, ensuring the error is always accurate without manual updates.

---

## 14. Version Support Policy

### 14.1 Pre-1.0 (Current)

**Support window: last 3 minor versions.**

The HUML spec is in active pre-release development. Older pre-1.0 versions have negligible real-world adoption, so a three-version rolling window balances ecosystem coverage against maintenance burden.

| Spec version | Status |
|---|---|
| v0.1 | Supported (within window) |
| v0.2 | Supported — current minimum |
| v0.3 (hypothetical) | Supported — when released, v0.1 exits window |

### 14.2 Post-1.0

**Support window: current major + one previous major.**

Once HUML reaches 1.0, breaking changes are gated behind major version bumps. The one-back policy mirrors .NET LTS conventions and gives consumers a full major cycle to migrate.

| Scenario | Supported versions |
|---|---|
| v1.x current | v1.x |
| v2.x released | v1.x + v2.x |
| v3.x released | v2.x + v3.x (v1.x exits window) |

### 14.3 Deprecation Process

Versions do not disappear silently. When a version exits the support window:

1. Apply `[Obsolete]` to the corresponding `HumlSpecVersion` enum member with a migration message. This surfaces as a **compiler warning** to consumers, not a runtime failure.
2. Keep the enum member and all associated gate branches in place for one further library release (grace period).
3. Remove the gate branches and the enum member in the subsequent release, noted as a breaking change in the changelog.

```csharp
public enum HumlSpecVersion
{
    [Obsolete("HUML v0.1 is outside the support window. Migrate to v0.2 or later.")]
    V0_1,

    V0_2,  // Current minimum supported
}
```

### 14.4 `huml-lang/tests` Submodule Pinning

Each supported spec version has a corresponding fixture directory pinned to the tagged commit of the shared test suite. When a version exits the support window, its fixture directory is retained in git history but removed from the active test run — the passing CI baseline should only cover the current window.

---

## 15. Future Packages

### 15.1 `Huml.Net.Linting`

A separate NuGet package providing diagnostics, version advisories, and style rules. It operates on the `HumlDocument` AST produced by `Huml.Net` — no re-parsing.

**Responsibilities (linter, not parser):**

| Diagnostic | Code | Description |
|---|---|---|
| Version drift (upgrade) | `HUML-L001` | File declares an older version but uses no features exclusive to it — consider bumping the header |
| Version drift (downgrade) | `HUML-L002` | File declares a newer version but only uses constructs valid in an older version — compatible with legacy tooling |
| Deprecated construct | `HUML-L003` | File uses a construct removed in a later spec version |
| Style violation | `HUML-L004+` | Configurable style rules (key naming conventions, blank line usage, etc.) |

```csharp
// Consumer API
var document = Huml.Parse(humlString);

var linter = new HumlLinter(LintOptions.Default);
IReadOnlyList<HumlDiagnostic> diagnostics = linter.Analyse(document);

// e.g.:
// HumlDiagnostic {
//   Code    = "HUML-L001",
//   Severity = DiagnosticSeverity.Advisory,
//   Message = "File declares v0.1 but is fully compatible with v0.2.",
//   Line    = 1, Column = 1
// }
```

**Design principles:**
- `Huml.Net` has zero dependency on `Huml.Net.Linting`. The parser has no opinions on style or version advisories.
- The linter takes an `HumlDocument`; it does not re-lex or re-parse.
- Diagnostics are advisory by default. Consumers can configure severity escalation (e.g. treat `HUML-L001` as an error in CI).

**Timeline:** `Huml.Net.Linting` is a v2+ concern. It has no value until at least two spec versions exist to compare against. The package boundary must be established in architecture now to prevent linting logic accreting into the parser.

---

## 16. Reference Links

| Resource | URL |
|---|---|
| HUML specification | https://huml.io |
| go-huml (primary reference) | https://github.com/huml-lang/go-huml |
| huml-rs (secondary reference) | https://github.com/huml-lang/huml-rs |
| Shared test suite | https://github.com/huml-lang/tests |
| huml-lang organisation | https://github.com/huml-lang |
