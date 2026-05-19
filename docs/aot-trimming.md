# AOT and Trimming

Huml.Net 0.2.0-alpha.2 adds compile-time safety annotations for applications published with
`PublishTrimmed=true` or Native AOT.

## Annotations Added

The following public API methods carry both `[RequiresUnreferencedCode]` and
`[RequiresDynamicCode]`:

- `Huml.Serialize<T>(T, HumlOptions?)`
- `Huml.Serialize(object?, Type, HumlOptions?)`
- `Huml.Deserialize<T>(string, HumlOptions?)`
- `Huml.Deserialize<T>(ReadOnlySpan<char>, HumlOptions?)`
- `Huml.Deserialize(string, Type, HumlOptions?)`
- `Huml.Populate<T>(string, T, HumlOptions?)`
- `Huml.Populate<T>(ReadOnlySpan<char>, T, HumlOptions?)`

## Huml.Parse Is Exempt

`Huml.Parse(string, HumlOptions?)` does **not** carry these annotations. The lexer/parser
pipeline performs no reflection on user types — it produces only a `HumlDocument` AST. It is
safe to call from trimmed or AOT-compiled applications.

## What the Annotations Do

When you publish with `PublishTrimmed=true` or `<PublishAot>true</PublishAot>`, the build
toolchain emits a warning for each call site:

```
ILLink warning IL2026: Huml.Serialize<T> requires unreferenced code...
ILLink warning IL3050: Huml.Serialize<T> requires dynamic code...
```

This is a *compile-time warning*, not a runtime failure. The warnings alert you that
serialisation and deserialisation use reflection and may not work correctly in a fully trimmed
binary unless you preserve the relevant types.

## Suppressing Warnings When You Know It Is Safe

If you know the types you are serialising or deserialising will not be trimmed (for example,
because they are in a `[DynamicDependency]`-rooted assembly), suppress the warnings with a
targeted pragma:

```csharp
#pragma warning disable IL2026, IL3050
var result = Huml.Deserialize<MyDto>(humlText);
#pragma warning restore IL2026, IL3050
```

## Future: Source-Generator Path

The `IHumlTypeInfoResolver` / `HumlTypeInfo<T>` seam (see [Options Reference](options-reference.md))
is the planned integration point for a future `Huml.Net.SourceGeneration` package that will
provide a fully AOT-compatible, reflection-free serialisation path. When that package is
available, you can register a source-generated resolver via `HumlOptions.TypeInfoResolver`
and remove the pragma suppressions entirely.

## IsTrimmable

`<IsTrimmable>true</IsTrimmable>` is set in `Huml.Net.csproj` for `net6.0`-compatible
target frameworks (net8.0, net9.0, net10.0). This tells the ILLink trimmer that Huml.Net
has been audited for trimming and that the trimmer may remove unused code paths within the
library itself. On `netstandard2.1`, trimmer support is absent and `IsTrimmable` is not set.
