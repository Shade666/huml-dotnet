# Performance benchmarks

Huml.Net is benchmarked against `System.Text.Json` using [BenchmarkDotNet](https://benchmarkdotnet.org/).
The benchmark suite lives in the companion
[huml-dotnet-examples](https://github.com/primeBeri/huml-dotnet-examples) repository.

## Headline results (Huml.Net 0.2.0-beta.1 vs System.Text.Json, .NET 10, Windows 11 x64)

The payload is an equivalent nested service-config object encoded as HUML for Huml.Net and
as JSON for System.Text.Json. `Stj_*_Reflection` is the baseline (ratio 1.00) in each group.

### Serialize

| Method | Mean | Ratio | Allocated |
|---|---:|---:|---:|
| Stj_Serialize_SourceGen | 250.5 ns | 0.66 | 592 B |
| Stj_Serialize_Reflection | 380.2 ns | 1.00 | 904 B |
| Huml_Serialize_SourceGen | 581.2 ns | 1.53 | 1624 B |
| Huml_Serialize_Reflection | 904.0 ns | 2.38 | 1768 B |

### Deserialize

| Method | Mean | Ratio | Allocated |
|---|---:|---:|---:|
| Stj_Deserialize_Reflection | 678.6 ns | 1.00 | 1416 B |
| Stj_Deserialize_SourceGen | 682.8 ns | 1.01 | 1416 B |
| Huml_Deserialize_SourceGen | 1,983.5 ns | 2.92 | 6176 B |
| Huml_Deserialize_Reflection | 2,355.0 ns | 3.47 | 6600 B |

### Parse-only

| Method | Mean | Ratio | Allocated |
|---|---:|---:|---:|
| Stj_Parse | 438.2 ns | 1.00 | 96 B |
| Huml_Parse | 1,259.4 ns | 2.87 | 4848 B |

## Honest commentary

**System.Text.Json is faster, and that's expected.** STJ is a hyper-optimised, years-mature,
UTF-8-native serialiser that ships with the runtime. Huml.Net is a young, UTF-16/string-based
library optimised for correctness and readability first. Being within ~2.4× on serialise and
~3× on deserialise for a first beta is a respectable starting point.

**The source generator earns its keep.** It cuts serialise time by ~36% and deserialise by ~16%
versus the reflection path, with lower allocations. For hot loops or AOT-published apps the
source-gen path is the one to reach for.

**The parse-only row is not apples-to-apples.** `JsonDocument.Parse` is lazy (96 B allocated).
`HumlSerializer.Parse` eagerly builds a full immutable `HumlDocument` AST — compare it to JSON
DOM construction, not to JSON scanning. A future lazy reader is out of scope for this beta.

**Why use HUML at all?** Configuration and document formats are typically parsed once at startup,
not in a request hot path. HUML's value is human readability and strictness — no YAML footguns —
with a `System.Text.Json`-style API that .NET developers already know.

## Reproduce

```bash
dotnet run -c Release --project benchmarks/HumlNet.Benchmarks
```

Full results with methodology notes:
[benchmarks/RESULTS.md](https://github.com/primeBeri/huml-dotnet-examples/blob/main/benchmarks/RESULTS.md)

## See also

- [Use the source generator](source-generator.md) — the source-gen path that closes the reflection gap.
- [AOT and trimming](aot-trimming.md) — source gen also enables trim-safe publishing.
