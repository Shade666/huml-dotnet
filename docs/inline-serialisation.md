# Inline Serialisation

Huml.Net can emit collections in compact inline format instead of the default multiline block format.

## Syntax

### Inline Sequence

A scalar-only list is emitted as a comma-separated single line:

```huml
Tags:: 1, 2, 3
Colours:: "red", "green", "blue"
```

### Inline Dictionary

A scalar-valued dictionary is emitted as comma-separated `key: value` pairs on a single line:

```huml
Counts:: a: 1, b: 2, c: 3
```

## Enabling Globally — `HumlOptions.CollectionFormat`

Set `CollectionFormat.Inline` on your options to request inline output for all eligible collections:

```csharp
var options = new HumlOptions { CollectionFormat = CollectionFormat.Inline };
string huml = HumlSerializer.Serialize(myObject, options);
```

The default is `CollectionFormat.Multiline`, which produces the traditional indented block format:

```huml
Tags::
  - 1
  - 2
  - 3
```

## Per-Property Override — `[HumlProperty(Inline = ...)]`

Use the `Inline` property on `[HumlProperty]` to override the global setting on a specific property.

Force inline on a property even when the global setting is `Multiline`:

```csharp
public class Article
{
    [HumlProperty(Inline = InlineMode.Inline)]
    public List<string> Tags { get; set; } = new();

    public List<string> Categories { get; set; } = new();
}
```

With `HumlOptions.Default` (Multiline), `Tags` still emits inline while `Categories` uses block format:

```huml
%HUML v0.2.0
Tags:: "dotnet", "huml"
Categories::
  - "news"
  - "release"
```

Force multiline on a property even when the global setting is `Inline`:

```csharp
public class Config
{
    public List<int> Ports { get; set; } = new();

    [HumlProperty(Inline = InlineMode.Multiline)]
    public List<string> Hosts { get; set; } = new();
}
```

With `CollectionFormat.Inline`, `Ports` emits inline while `Hosts` is forced to block format:

```huml
%HUML v0.2.0
Ports:: 8080, 9090
Hosts::
  - "alpha.example.com"
  - "beta.example.com"
```

`InlineMode.Inherit` (the default when `[HumlProperty]` has no `Inline` argument) inherits whatever
the global `HumlOptions.CollectionFormat` is set to.

## Precedence Rules

1. `[HumlProperty(Inline = InlineMode.Inline)]` or `[HumlProperty(Inline = InlineMode.Multiline)]` wins
   over `HumlOptions.CollectionFormat`.
2. `InlineMode.Inherit` (not set) defers to `HumlOptions.CollectionFormat`.

## Scalar-Only Constraint and Silent Fallback

Inline format only applies when **every item in the collection is a scalar value**. If any item is a
POCO, a nested collection, or a dictionary, the serialiser silently falls back to multiline — no
exception is thrown.

Scalar types eligible for inline emission:

`string`, `bool`, `int`, `long`, `short`, `byte`, `sbyte`, `ushort`, `uint`, `ulong`,
`double`, `float`, `decimal`, `null`

Example of silent fallback:

```csharp
public class Report
{
    // Items contains POCOs — inline is requested but silently falls back
    [HumlProperty(Inline = InlineMode.Inline)]
    public List<Row> Items { get; set; } = new();
}
```

Output (multiline, not inline):

```huml
Items::
  -
    Value: 42
```

## Empty Collections

Empty collections always use the empty literal syntax regardless of `CollectionFormat` or
per-property overrides:

```huml
Tags:: []
Counts:: {}
```

## Non-Collection Properties

The `Inline` setting on `[HumlProperty]` has no effect on scalar properties or POCO properties —
it is silently ignored. Only list and dictionary properties are affected.

## Version Compatibility

Inline format is valid in both HUML v0.1 and v0.2. The serialiser emits the same inline syntax
regardless of `HumlOptions.SpecVersion`. Inline output round-trips through `HumlSerializer.Parse` without
error on both spec versions.
