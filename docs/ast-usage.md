# AST Usage Guide

`Huml.Parse()` returns a `HumlDocument` — the root of the abstract syntax tree (AST).
Use the AST when you need to inspect or transform a HUML document without mapping it to a known .NET type.

## Required Using Directives

```csharp
using Huml.Net;
using Huml.Net.Parser;
```

## Node Types

All AST nodes are `public sealed record` types in the `Huml.Net.Parser` namespace, all extending the abstract base `HumlNode`.

| Type                | Constructor                                          | Role                          |
| ------------------- | ---------------------------------------------------- | ----------------------------- |
| `HumlDocument`      | `HumlDocument(IReadOnlyList<HumlNode> Entries)`      | Root of every parsed document |
| `HumlMapping`       | `HumlMapping(string Key, HumlNode Value)`            | A single key-value pair       |
| `HumlScalar`        | `HumlScalar(ScalarKind Kind, object? Value)`         | A leaf value                  |
| `HumlSequence`      | `HumlSequence(IReadOnlyList<HumlNode> Items)`        | An ordered list               |
| `HumlInlineMapping` | `HumlInlineMapping(IReadOnlyList<HumlNode> Entries)` | Inline or empty mapping block |

## ScalarKind Values

| Kind      | Runtime Type of `Value`         | Example HUML   |
| --------- | ------------------------------- | -------------- |
| `String`  | `string`                        | `key: "hello"` |
| `Integer` | `long`                          | `key: 42`      |
| `Float`   | `double`                        | `key: 3.14`    |
| `Bool`    | `bool`                          | `key: true`    |
| `Null`    | `null`                          | `key: null`    |
| `NaN`     | `string` (`"nan"`)              | `key: nan`     |
| `Inf`     | `string` (`"+inf"` or `"-inf"`) | `key: +inf`    |

Note: `NaN` and `Inf` scalars carry the raw token string in `Value` rather than `null`, so that sign information (`+inf` vs `-inf`) is preserved for downstream consumers.

## Basic Traversal

```csharp
using Huml.Net;
using Huml.Net.Parser;

HumlDocument doc = Huml.Parse(humlString);

foreach (HumlNode entry in doc.Entries)
{
    if (entry is HumlMapping { Key: var key, Value: HumlScalar scalar })
    {
        Console.WriteLine($"{key} = {scalar.Value} ({scalar.Kind})");
    }
}
```

## Pattern Matching with Nested Structures

Use recursive pattern matching to handle nested mappings and sequences:

```csharp
foreach (HumlNode entry in doc.Entries)
{
    switch (entry)
    {
        case HumlMapping { Key: var key, Value: HumlScalar { Kind: ScalarKind.Integer, Value: long n } }:
            Console.WriteLine($"{key}: integer {n}");
            break;

        case HumlMapping { Key: var key, Value: HumlSequence seq }:
            Console.WriteLine($"{key}: sequence with {seq.Items.Count} items");
            break;

        case HumlMapping { Key: var key, Value: HumlDocument nested }:
            Console.WriteLine($"{key}: nested mapping block");
            // recurse into nested.Entries
            break;
    }
}
```

## HumlInlineMapping

Inline mapping blocks (e.g. `key:: a: 1, b: 2`) and empty mapping blocks (`key:: {}`) produce `HumlInlineMapping` rather than `HumlDocument`.
Both types expose an `Entries` property of type `IReadOnlyList<HumlNode>`, so walking code that handles both should match on either type:

```csharp
case HumlMapping { Value: HumlDocument nested }:
    // process nested.Entries
    break;
case HumlMapping { Value: HumlInlineMapping inline }:
    // process inline.Entries
    break;
```

The distinction exists because `HumlDocument` is always the root node returned by `Huml.Parse()`, while `HumlInlineMapping` only appears as a value within a mapping.

## Parsing Options

Pass `HumlOptions.AutoDetect` to read the spec version from the document header:

```csharp
HumlDocument doc = Huml.Parse(humlString, HumlOptions.AutoDetect);
```

Or use the default options (v0.2, no header required):

```csharp
HumlDocument doc = Huml.Parse(humlString);
```

See [Options Reference](options-reference.md) for all available options.
