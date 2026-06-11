# Use the source generator

By default Huml.Net uses reflection to discover how to serialise your types. For **AOT** or
**trimmed** apps — or to remove reflection from a hot path — the `Huml.Net.SourceGeneration`
package emits the type metadata at compile time instead.

## 1. Add the generator package

```bash
dotnet add package Huml.Net.SourceGeneration
```

It is an analyzer/source-generator package: it adds no runtime assembly, and it transitively
brings in `Huml.Net` so you do not need a separate reference.

## 2. Declare a context and register your types

Create a `partial` class deriving from `HumlGeneratedContext` and annotate it with one
`[HumlSerializable(typeof(T))]` per type you want generated:

```csharp
using Huml.Net.Serialization;
using Huml.Net.Serialization.Attributes;

[HumlSerializable(typeof(ServerConfig))]
[HumlSerializable(typeof(Endpoint))]
public partial class AppHumlContext : HumlGeneratedContext
{
}
```

The generator fills in the `partial` class with a `HumlTypeInfo<T>` per registered type and a
`Default` singleton.

## 3. Point HumlOptions at the generated context

```csharp
var options = new HumlOptions { TypeInfoResolver = AppHumlContext.Default };

var config = HumlSerializer.Deserialize<ServerConfig>(document, options);
string huml = HumlSerializer.Serialize(config, options);
```

When the resolver supplies metadata for a type, Huml.Net uses the generated delegates and
bypasses reflection. For any type the resolver does not know, it falls back to the reflection
path automatically — so partial adoption is fine.

## What the generator honours

- `[HumlIgnore]` — the property is excluded from the generated metadata.
- `[HumlProperty("name")]` — the HUML key override is used.
- Inherited properties — base-class properties are included, base-first.

## Supported type shapes

The generator emits a parameterless `CreateObject` only when `new T()` is valid. Types with a
**parameterised constructor**, **`required` members**, or that are **abstract** still work — the
generated `CreateObject` is `null` and the deserialiser uses its constructor-binding path for
them. `init`-only properties bind via the constructor rather than a generated setter.

> **Beta limitation:** the *context class itself* must be a top-level, non-generic `partial`
> class (it may live in any namespace). Nested or generic context classes are not yet supported.

## See also

- [Publish AOT / trimmed](aot-trimming.md) — the wider AOT story.
- [Extending the pipeline](internals/extending.md) — the <xref:Huml.Net.Serialization.IHumlTypeInfoResolver> seam the
  generated context plugs into.
