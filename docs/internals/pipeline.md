# Pipeline Overview

This document describes the internal data flow of Huml.Net from raw input string to typed .NET
object and back. It is aimed at contributors who need to understand how the components interact
before modifying internal code.

## Pipeline Diagram

```
Input string / ReadOnlySpan<char>
    └─► Lexer          (Lexer/Lexer.cs)         — pull-based ref struct tokeniser
         └─► HumlParser (Parser/HumlParser.cs)   — recursive-descent ref struct, produces AST
              └─► HumlDocument (AST root)
                   ├─► HumlDeserializer           — AST → .NET objects
                   └─► HumlSerializer             — .NET objects → HUML text
```

The public entry point is the static `HumlSerializer` facade (`src/Huml.Net/HumlSerializer.cs`). All pipeline classes
are `internal sealed` — consumers never interact with them directly.

## Lexer

**Class:** `internal ref struct Lexer`
**File:** `src/Huml.Net/Lexer/Lexer.cs`

The Lexer is pull-based: the caller calls `NextToken()` each time it needs the next token. The
Parser drives the Lexer by calling `NextToken()` whenever it consumes a token and advances the
lookahead.

Key implementation details:

- **Single-pass** over a `ReadOnlySpan<char>`. CRLF sequences (`\r\n`) and bare
  carriage returns (`\r`) are normalised inline during lexing — the `PeekCurrentChar()`
  method returns `\n` for both, so the rest of the implementation only handles `\n`.
  No upfront `string.Replace` pass is performed; the input span is consumed directly.
- **Position tracking:** `_line`, `_col`, and `_lineIndent` fields are updated on each character
  advance. These feed into `HumlParseException` line/column diagnostics.
- **`EffectiveSpecVersion` property:** Initialised from `HumlOptions.SpecVersion`. After the
  Parser reads a `%HUML` header token, it updates `_lexer.EffectiveSpecVersion` so that
  version-gated lexer rules (e.g. backtick triple-quoted strings) activate for the remainder of
  the document.
- **Key responsibilities:** indentation measurement (`MeasureIndent`), comment stripping, quoted
  and triple-quoted string parsing, numeric literal parsing, version directive scanning, and the
  main token-type dispatch in `NextToken()`.

**Entry point:** `new Lexer(source, options)` stack-allocated ref struct constructor,
then repeated `NextToken()` calls. Because `Lexer` is a `ref struct`, it cannot be
stored on the heap or captured in closures — it lives entirely on the call stack for
the duration of a parse.

## Parser

**Class:** `internal ref struct HumlParser`
**File:** `src/Huml.Net/Parser/HumlParser.cs`

The Parser is a recursive-descent parser with a single token of lookahead. The `_lookahead` field
is primed in the constructor by calling `NextToken()` once before any production method runs.

Key implementation details:

- **Recursion depth guard:** A `_depth` counter is incremented on each recursive call. A
  `HumlParseException` is thrown when `_depth >= _maxDepth`. The default `MaxRecursionDepth` is
  64 (range [1, 1024]) — matching the `System.Text.Json` convention.
- **Version header:** The Parser checks for an optional `%HUML vX.Y.Z` version token at the
  start of the document. If found, it applies the detected version to both `_effectiveSpecVersion`
  (used by Parser production methods) and `_lexer.EffectiveSpecVersion` (used by Lexer rules for
  the remainder of the document).

**Entry point:** `new HumlParser(source, options)` constructor + `Parse()` returns `HumlDocument`.

## AST

All AST node types live in `src/Huml.Net/Parser/` as `public sealed record` types. All extend
the abstract base `public abstract record HumlNode`. Records provide structural equality and
immutability by default.

| Type | Role |
|------|------|
| `HumlDocument` | Root node / mapping block — holds `IReadOnlyList<HumlNode>` entries |
| `HumlMapping` | Single key-value pair (`Key: string`, `Value: HumlNode`) |
| `HumlScalar` | Leaf value (`Kind: ScalarKind`, `Value: object?`) |
| `HumlSequence` | Ordered list of `HumlNode` items |
| `HumlInlineMapping` | Inline `{}` mapping block — holds `IReadOnlyList<HumlNode>` entries |

`HumlDocument` is used for both the document root and nested multiline mapping blocks.
`HumlInlineMapping` is used specifically for inline `{key: value}` syntax and is distinct from
`HumlDocument` so deserialiser dispatch can tell them apart.

## Serialiser

**Class:** `internal static class HumlSerializer`
**File:** `src/Huml.Net/Serialization/HumlSerializer.cs`

The Serialiser converts a .NET object graph to a HUML text document.

Key implementation details:

- **Property enumeration:** Uses `PropertyDescriptor.GetDescriptors(type)` for a cached,
  declaration-order list of public readable properties. Base-class properties precede derived-class
  properties.
- **Version header:** Always emits `%HUML vX.Y.Z` as the first line of the output.
- **Type dispatch order in `SerializeValue`:** `string` is checked first (before the `IEnumerable`
  branch to prevent `string` being treated as `IEnumerable<char>`), then `bool`, integers,
  floats, decimals, NaN/Inf, `null`, `IEnumerable`, `Dictionary`, and finally the POCO fallback.
- **Key emission:** `AppendKey(sb, key)` calls `NeedsQuoting()` to decide between bare and quoted
  key syntax. `AppendEscapedString(sb, value)` handles escaping for string values.
- **Converter dispatch:** Before built-in type dispatch, `SerializeValue()` checks for a property-level `[HumlConverter]` attribute, then a type-level `[HumlConverter]`, then `HumlOptions.Converters`. The first matching converter's `Write(HumlWriterContext, value)` method is called.
- **Enum serialisation:** Enum values are serialised as quoted strings. The name is resolved via `EnumNameCache` — honouring `[HumlEnumValue]` overrides and `HumlOptions.PropertyNamingPolicy` transforms.

## Deserialiser

**Class:** `internal static class HumlDeserializer`
**File:** `src/Huml.Net/Serialization/HumlDeserializer.cs`

The Deserialiser maps a `HumlDocument` AST to a typed .NET object.

Key implementation details:

- **Dispatch:** `DeserializeNode()` is the central dispatch method; it pattern-matches on the
  concrete `HumlNode` subtype and delegates to type-specific helpers.
- **POCO mapping:** Uses `PropertyDescriptor.GetLookup(type, policy)` for O(1) dictionary key lookup keyed by the HUML key (after naming policy transform). The ordering array (`GetDescriptors`) is used only by the serialiser.
- **Naming policy:** `HumlOptions.PropertyNamingPolicy` transforms .NET property names at descriptor build time. The resulting HUML key is used for both the serialised output and the deserialise lookup dictionary key, ensuring round-trip symmetry.
- **Converter dispatch:** Before built-in type dispatch, `DeserializeNode()` checks for a property-level `[HumlConverter]` attribute, then a type-level `[HumlConverter]` attribute, then `HumlOptions.Converters`. The first matching converter's `Read(HumlNode)` method is called.
- **Collection dispatch:** Handles `T[]`, `List<T>`, `IEnumerable<T>`, and `Dictionary<string,T>`.
- **Populate path:** `HumlSerializer.Populate<T>()` reuses `PopulateMappingEntries()` — the same property-assignment logic as `Deserialize<T>()` but targeting an existing instance rather than a freshly constructed one. Only properties present in the HUML document are assigned.
- **`init`-only properties:** Detected via the `IsInitOnly` flag on `PropertyDescriptor`.
  `PropertyInfo.SetValue` is used to assign the value after the object is constructed —
  `init` semantics are enforced by the C# compiler at compile time, but CLR reflection
  can write to init-only backing fields after construction. `HumlSerializer.Populate<T>()` also
  writes init-only properties on the supplied instance.

## Where Things Live

| Component | File |
|-----------|------|
| Lexer | `src/Huml.Net/Lexer/Lexer.cs` |
| Token types | `src/Huml.Net/Lexer/TokenType.cs` |
| Token struct | `src/Huml.Net/Lexer/Token.cs` |
| Parser | `src/Huml.Net/Parser/HumlParser.cs` |
| AST nodes | `src/Huml.Net/Parser/Huml*.cs` |
| Serialiser | `src/Huml.Net/Serialization/HumlSerializer.cs` |
| Deserialiser | `src/Huml.Net/Serialization/HumlDeserializer.cs` |
| Property cache | `src/Huml.Net/Serialization/PropertyDescriptor.cs` |
| Public facade | `src/Huml.Net/HumlSerializer.cs` |
| Options | `src/Huml.Net/Versioning/HumlOptions.cs` |
