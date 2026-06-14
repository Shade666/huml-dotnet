# AOT and Trimming

Huml.Net 0.2.0-alpha.2 adds compile-time safety annotations for applications published with
`PublishTrimmed=true` or Native AOT.

## Annotations Added

The following public API methods carry both `[RequiresUnreferencedCode]` and
`[RequiresDynamicCode]`:

- `HumlSerializer.Serialize<T>(T, HumlOptions?)`
- `HumlSerializer.Serialize(object?, Type, HumlOptions?)`
- `HumlSerializer.Deserialize<T>(string, HumlOptions?)`
- `HumlSerializer.Deserialize<T>(ReadOnlySpan<char>, HumlOptions?)`
- `HumlSerializer.Deserialize(string, Type, HumlOptions?)`
- `HumlSerializer.Populate<T>(string, T, HumlOptions?)`
- `HumlSerializer.Populate<T>(ReadOnlySpan<char>, T, HumlOptions?)`

## HumlSerializer.Parse Is Exempt

`HumlSerializer.Parse(string, HumlOptions?)` does **not** carry these annotations. The lexer/parser
pipeline performs no reflection on user types — it produces only a `HumlDocument` AST. It is
safe to call from trimmed or AOT-compiled applications.

## What the Annotations Do

When you publish with `PublishTrimmed=true` or `<PublishAot>true</PublishAot>`, the build
toolchain emits a warning for each call site:

```
ILLink warning IL2026: HumlSerializer.Serialize<T> requires unreferenced code...
ILLink warning IL3050: HumlSerializer.Serialize<T> requires dynamic code...
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
var result = HumlSerializer.Deserialize<MyDto>(humlText);
#pragma warning restore IL2026, IL3050
```

## Source-Generator Path (reflection-free)

For a fully AOT-compatible, reflection-free path, use the built-in source generator. Declare a
`partial` `HumlGeneratedContext` subclass annotated with `[HumlSerializable(typeof(MyDto))]` and
register it via `HumlOptions.TypeInfoResolver`; the generator emits the metadata at compile time
so no runtime reflection is needed and the `IL2026`/`IL3050` warnings no longer apply to those
types. See **[Use the source generator](source-generator.md)** for the full walkthrough.

## IsTrimmable

`<IsTrimmable>true</IsTrimmable>` is set in `Huml.Net.csproj` for `net6.0`-compatible
target frameworks (net8.0, net9.0, net10.0). This tells the ILLink trimmer that Huml.Net
has been audited for trimming and that the trimmer may remove unused code paths within the
library itself. On `netstandard2.1`, trimmer support is absent and `IsTrimmable` is not set.

## See also

- [Use the source generator](source-generator.md) — the source-gen path that removes IL2026/IL3050 at call sites.
- [E12.AotPublish](https://github.com/primeBeri/huml-dotnet-examples/tree/main/src/examples/E12.AotPublish) — runnable example (AOT-safe Parse path, pragma suppression, source-gen path).
