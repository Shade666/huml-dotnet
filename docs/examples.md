# Examples & benchmarks

Every feature of Huml.Net has a runnable, self-asserting example in the companion
**[huml-dotnet-examples](https://github.com/primeBeri/huml-dotnet-examples)** repository. Each
example is a small console app that demonstrates one feature *and* asserts its own behaviour, so
the set doubles as an end-to-end test suite run against the published NuGet package.

```bash
git clone https://github.com/primeBeri/huml-dotnet-examples
cd huml-dotnet-examples
./run-examples.ps1        # runs all examples and aggregates results
```

Or run one at a time:

```bash
dotnet run --project src/examples/E01.GettingStarted -c Release
```

## The examples

| Example | Feature | Related guide |
|---------|---------|---------------|
| [E01 GettingStarted](https://github.com/primeBeri/huml-dotnet-examples/tree/main/src/examples/E01.GettingStarted) | Deserialise, serialise, round-trip | [Getting started](getting-started.md) |
| [E02 NamingPolicies](https://github.com/primeBeri/huml-dotnet-examples/tree/main/src/examples/E02.NamingPolicies) | Kebab-case keys, `[HumlProperty]` overrides | [Customize property names](naming-policy.md) |
| [E03 Enums](https://github.com/primeBeri/huml-dotnet-examples/tree/main/src/examples/E03.Enums) | `[HumlEnumValue]` custom wire names | [Work with enums](enum-serialisation.md) |
| [E04 Polymorphism](https://github.com/primeBeri/huml-dotnet-examples/tree/main/src/examples/E04.Polymorphism) | `[HumlPolymorphic]` incl. nested/collection elements | [Serialize polymorphic types](polymorphism.md) |
| [E05 CustomConverters](https://github.com/primeBeri/huml-dotnet-examples/tree/main/src/examples/E05.CustomConverters) | A `HumlConverter<T>` for a value type | [Write a custom converter](custom-converters.md) |
| [E06 Populate](https://github.com/primeBeri/huml-dotnet-examples/tree/main/src/examples/E06.Populate) | Overlay a partial document onto an instance | [Overlay onto an instance](populate.md) |
| [E07 ErrorHandling](https://github.com/primeBeri/huml-dotnet-examples/tree/main/src/examples/E07.ErrorHandling) | The exception contract; `HumlOptions.Strict` | [Handle errors](error-handling.md) |
| [E08 SourceGeneration](https://github.com/primeBeri/huml-dotnet-examples/tree/main/src/examples/E08.SourceGeneration) | Reflection-free metadata via `HumlGeneratedContext` | [Use the source generator](source-generator.md) |
| [E09 Options](https://github.com/primeBeri/huml-dotnet-examples/tree/main/src/examples/E09.Options) | Naming policy, collection format, ignore condition, number handling | [Options reference](options-reference.md) |
| [E10 ConstructorBinding](https://github.com/primeBeri/huml-dotnet-examples/tree/main/src/examples/E10.ConstructorBinding) | Records, `[HumlConstructor]`, `init`-only properties | [Bind constructors & records](constructor-binding.md) |
| [E11 ExtensionData](https://github.com/primeBeri/huml-dotnet-examples/tree/main/src/examples/E11.ExtensionData) | `[HumlExtensionData]` overflow bucket; `Disallow` | [Capture unknown keys](extension-data.md) |
| [E12 AotPublish](https://github.com/primeBeri/huml-dotnet-examples/tree/main/src/examples/E12.AotPublish) | AOT-safe Parse path; source-gen path avoiding IL2026/IL3050 | [Publish AOT / trimmed](aot-trimming.md) |
| [E13 Versioning](https://github.com/primeBeri/huml-dotnet-examples/tree/main/src/examples/E13.Versioning) | `DetectedVersion`, `VersionSource`, `UnknownVersionBehaviour` | [Versioning model](versioning.md) |

## Benchmarks

The same repository hosts the BenchmarkDotNet suite comparing Huml.Net against
System.Text.Json in reflection and source-generated modes:

```bash
dotnet run --project benchmarks/HumlNet.Benchmarks -c Release
```

Recorded figures and commentary: [Performance benchmarks](benchmarks.md) here, or
[benchmarks/RESULTS.md](https://github.com/primeBeri/huml-dotnet-examples/blob/main/benchmarks/RESULTS.md)
in the examples repository.
