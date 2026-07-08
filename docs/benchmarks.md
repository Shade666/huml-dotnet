# Performance benchmarks

Huml.Net is benchmarked against `System.Text.Json` using [BenchmarkDotNet](https://benchmarkdotnet.org/).
The benchmark suite lives in the companion
[huml-dotnet-examples](https://github.com/primeBeri/huml-dotnet-examples) repository.

## Headline results (Huml.Net 0.2.0-rc.1 vs System.Text.Json, .NET 10, Windows 11 x64)

Recorded 2026-07-08. The payload is an equivalent nested service-config object encoded as HUML
for Huml.Net and as JSON for System.Text.Json. `Stj_*_Reflection` is the baseline (ratio 1.00)
in each group.

### Serialize

| Method | Mean | Ratio | Allocated |
|---|---:|---:|---:|
| Stj_Serialize_SourceGen | 241.3 ns | 0.68 | 592 B |
| Stj_Serialize_Reflection | 356.1 ns | 1.00 | 904 B |
| Huml_Serialize_SourceGen | 530.0 ns | 1.49 | 1624 B |
| Huml_Serialize_Reflection | 865.4 ns | 2.43 | 1768 B |

### Deserialize

| Method | Mean | Ratio | Allocated |
|---|---:|---:|---:|
| Stj_Deserialize_Reflection | 659.6 ns | 1.00 | 1416 B |
| Stj_Deserialize_SourceGen | 666.9 ns | 1.01 | 1416 B |
| Huml_Deserialize_SourceGen | 1,976.5 ns | 3.00 | 6176 B |
| Huml_Deserialize_Reflection | 2,356.8 ns | 3.57 | 6600 B |

### Parse-only

| Method | Mean | Ratio | Allocated |
|---|---:|---:|---:|
| Stj_Parse | 424.2 ns | 1.00 | 96 B |
| Huml_Parse | 1,202.6 ns | 2.83 | 4848 B |

## Honest commentary

**System.Text.Json is faster, and that's expected.** STJ is a hyper-optimised, years-mature,
UTF-8-native serialiser that ships with the runtime. Huml.Net is a young, UTF-16/string-based
library optimised for correctness and readability first. Being within ~2.4× on serialise and
~3–3.6× on deserialise at the first release candidate is a respectable starting point.

**The source generator earns its keep.** It cuts serialise time by ~39% and deserialise by ~16%
versus the reflection path, with lower allocations. For hot loops or AOT-published apps the
source-gen path is the one to reach for.

**The parse-only row is not apples-to-apples.** `JsonDocument.Parse` is lazy (96 B allocated).
`HumlSerializer.Parse` eagerly builds a full immutable `HumlDocument` AST — compare it to JSON
DOM construction, not to JSON scanning. A future lazy reader is out of scope for the 0.2.0 line.

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
