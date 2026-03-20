# Pitfalls Research

**Domain:** .NET parser/serialisation library — multi-TFM, reflection-based deserialiser, NuGet packaging, git submodule CI
**Researched:** 2026-03-20
**Confidence:** HIGH (multi-TFM, NuGet, CI), MEDIUM (version-gating, reflection edge cases)

---

## Critical Pitfalls

### Pitfall 1: `GeneratePackageOnBuild` produces single-TFM `.nupkg` in multi-TFM projects

**What goes wrong:**
When `GeneratePackageOnBuild` is set and the project targets multiple frameworks (`netstandard2.1;net8.0;net9.0;net10.0`), the SDK pack runs during the inner build for each TFM separately. Only the last (or the outer) build produces the final `.nupkg`, and depending on SDK version and csproj layout, the `lib/` folder in the package may contain assemblies for only a subset of the targeted TFMs. The package installs silently on a consumer's machine but resolves to the wrong (usually the alphabetically last) TFM assembly.

**Why it happens:**
`GeneratePackageOnBuild` is designed for single-TFM projects. In a multi-TFM project the SDK runs an "outer build" (no TFM) and then "inner builds" (one per TFM). The outer build triggers pack, but at that point not all inner-build outputs are merged. This is a known MSBuild/NuGet SDK issue (dotnet/msbuild#7911).

**How to avoid:**
Do NOT use `GeneratePackageOnBuild=true` in multi-TFM projects. Instead, run `dotnet pack -c Release` as a dedicated CI step after `dotnet build`. Verify the produced `.nupkg` with NuGet Package Explorer or `dotnet nuget verify` before publishing. Add a CI lint step: `unzip -p Huml.Net.*.nupkg '*/[Content_Types].xml' | grep -c 'netstandard2.1'` to assert all TFMs appear in the `lib/` directory.

**Warning signs:**
- The `.nupkg` file size is suspiciously small for a multi-TFM library.
- NuGet Package Explorer shows only one `lib/` entry.
- NU5128 warning appears during pack ("lib/\<tfm\>/ directory contains assemblies but no dependency group exists for this TFM").

**Phase to address:** Project scaffolding / CI setup phase (the very first CI pipeline commit).

---

### Pitfall 2: SourceLink silently broken — embedded commit SHA does not match repository

**What goes wrong:**
`Microsoft.SourceLink.GitHub` is referenced but SourceLink metadata is absent or wrong in the published `.nupkg`. Consumers who try to step into Huml.Net source in their debugger get "source not found" errors. The package passes all functional tests, so the failure is invisible until a user reports it.

**Why it happens:**
Three common causes: (1) `PublishRepositoryUrl=true` is missing from the `.csproj`, so the `RepositoryUrl` element is never written to the `.nuspec`; (2) `EmbedUntrackedSources=true` is missing, so files not tracked by git (e.g., generated AssemblyInfo) have no embedded source path; (3) the CI workflow does not set `ContinuousIntegrationBuild=true`, so the build is non-deterministic and the commit hash embedded in the PDB is `0000000000000000000000000000000000000000`.

**How to avoid:**
Add all three properties to the `.csproj` (not just one):
```xml
<PublishRepositoryUrl>true</PublishRepositoryUrl>
<EmbedUntrackedSources>true</EmbedUntrackedSources>
<IncludeSymbols>true</IncludeSymbols>
<SymbolPackageFormat>snupkg</SymbolPackageFormat>
```
Set `ContinuousIntegrationBuild=true` in GitHub Actions only:
```xml
<ContinuousIntegrationBuild Condition="'$(GITHUB_ACTIONS)' == 'true'">true</ContinuousIntegrationBuild>
```
Validate with `dotnet sourcelink test Huml.Net.*.snupkg` in the CI "verify" job after pack.

**Warning signs:**
- `dotnet sourcelink test` exits non-zero in CI.
- NuGet Package Explorer "SourceLink" panel shows no mappings.
- `<RepositoryUrl>` is absent from the packed `.nuspec`.

**Phase to address:** CI setup / NuGet packaging phase.

---

### Pitfall 3: Conditional compilation symbols ordered least-specific-first — wrong code path compiled per TFM

**What goes wrong:**
Version-gated behaviour (e.g., v0.2-only parsing rules) uses `#if` / `#elif` blocks. When a `NETSTANDARD2_1` branch is placed before `NET8_0` in a chain, the net8.0 build compiles the netstandard2.1 branch because `NETSTANDARD2_1` is also defined for net8.0 under some SDK versions (it is a compatibility alias). The net8.0 consumer silently misses optimisations or API surface.

**Why it happens:**
.NET SDK defines cumulative preprocessor symbols. `NET8_0` implies `NET8_0_OR_GREATER`, but `NETSTANDARD2_1` is a distinct symbol that is NOT implied by `NET8_0` — however, the *project* implicitly satisfies netstandard2.1 compatibility when targeting net8.0. Developers confuse "targets netstandard2.1" with "NETSTANDARD2_1 is defined". The real trap is ordering: if a block uses `#if NETSTANDARD2_1` without `&& !NET8_0_OR_GREATER`, net8.0 builds fall through to it when another symbol is not defined.

**How to avoid:**
Order `#if` chains from most specific (newest) to least specific (oldest):
```csharp
#if NET10_0_OR_GREATER
    // net10.0 path
#elif NET9_0_OR_GREATER
    // net9.0 path
#elif NET8_0_OR_GREATER
    // net8.0 path
#else
    // netstandard2.1 fallback
#endif
```
Never use bare `#if NETSTANDARD2_1` without a `!NET8_0_OR_GREATER` guard. Add a per-TFM test that asserts a sentinel constant (or a method return value) to catch cross-TFM symbol leakage.

**Warning signs:**
- The build matrix runs only for one TFM in CI.
- Tests pass on `net8.0` but fail when the package is consumed from a `netstandard2.1` project.
- IDE shows unexpected greyed-out `#else` branches.

**Phase to address:** Core parser / version-gating implementation phase.

---

### Pitfall 4: `ReadOnlySpan<char>` overload introduced to `netstandard2.1` — breaks C# 14 caller code

**What goes wrong:**
`Deserialize<T>(ReadOnlySpan<char> huml, ...)` is defined on the `netstandard2.1` TFM. In C# 14 (net10.0 SDK), new built-in span conversions change overload resolution: a call-site that compiled cleanly with C# 13 (`Deserialize("literal")`) becomes ambiguous between the `string` overload and the `ReadOnlySpan<char>` overload, causing a CS0121 compile error in consumer code or silently resolving to a different overload.

**Why it happens:**
C# 14 adds span-compatible implicit conversions for string literals and introduces new overload resolution tiebreakers. Adding a `ReadOnlySpan<char>` overload alongside a `string` overload creates an ambiguity that did not exist in C# 13. This is a documented breaking change in .NET 10 (dotnet/runtime compatibility docs for core-libraries/10.0/csharp-overload-resolution).

**How to avoid:**
Do not ship both a `string` and a `ReadOnlySpan<char>` overload for the same method with the same logical parameter. Use `ReadOnlySpan<char>` as the primary overload (the implementation) and provide the `string` overload as a thin wrapper that calls `AsSpan()`. Alternatively, name them differently (e.g., `DeserializeFromSpan`) if both must exist. Test with the C# 14 compiler in the `net10.0` TFM build.

**Warning signs:**
- Consumer projects on `net10.0` / C# 14 report CS0121 compile errors after upgrading Huml.Net.
- CI build matrix does not include a net10.0 consumer project test.

**Phase to address:** Public API design phase (before first public release).

---

### Pitfall 5: Reflection deserialiser silently skips properties with no public setter

**What goes wrong:**
When `HumlDeserializer` uses reflection to populate a target type, properties with `private set` or `init`-only setters are discovered by `GetProperties()` but `PropertyInfo.CanWrite` is `true` for `init` setters while `SetValue()` throws `TargetInvocationException` at runtime. Properties with `private set` report `CanWrite = false` and are silently skipped. The caller gets a partially-populated object with no error.

**Why it happens:**
`PropertyInfo.CanWrite` returns `true` for `init`-only properties because the setter is technically a setter — it just enforces object-initialiser-only use at the language level, not the reflection level. The check `CanWrite == false` is correct for `private set` but incorrect for `init`. The difference is only detectable by inspecting `SetMethod.ReturnParameter.GetRequiredCustomModifiers()` for `System.Runtime.CompilerServices.IsExternalInit`.

**How to avoid:**
In the deserialiser property-binding loop, use a helper that checks both `CanWrite` AND detects init-only setters:
```csharp
bool IsInitOnly(PropertyInfo p) =>
    p.SetMethod?.ReturnParameter
      .GetRequiredCustomModifiers()
      .Contains(typeof(System.Runtime.CompilerServices.IsExternalInit)) == true;
```
For init-only properties, fall back to constructor-based binding or throw `HumlDeserializeException` with a clear message rather than silently skipping. Add a fixture test that deserialises into a type with `init` setters to lock this behaviour.

**Warning signs:**
- Deserialised object fields are `default` / `null` for properties declared with `init`.
- No exception is thrown; the caller assumes success.

**Phase to address:** Reflection / deserialiser implementation phase.

---

### Pitfall 6: Version gate uses `>=` comparison but HUML spec uses inclusive lower-bound — off-by-one behaviour

**What goes wrong:**
The parser branch for v0.2 features uses `if (version >= new HumlVersion(0, 2))` which is correct, but a subtle variant occurs when comparing pre-release or patch versions. If `HumlVersion` stores only `major.minor` and a future spec adds `v0.2.1`, a document declaring `version: 0.2.1` may or may not match the branch depending on how equality is implemented. More immediately: when the "rolling support window" logic checks `if (version < SpecVersionPolicy.OldestSupported)`, an off-by-one means v0.1 documents are rejected one spec version too early.

**Why it happens:**
Developers implement version comparison against ad-hoc integer pairs without unit-testing the boundary values. The spec's "rolling 3-version window" is a policy that must be tested at every boundary: oldest supported, newest supported + 1, exact current.

**How to avoid:**
Implement `HumlVersion` as a proper comparable value type with unit-tested `CompareTo`. Write explicit tests for boundary cases:
- `version == OldestSupported` → accepted
- `version == OldestSupported - 1 minor` → rejected with `HumlUnsupportedVersionException`
- `version == NewestSupported + 1 minor` → handled by `UnknownVersionBehaviour`
Use `SpecVersionPolicy` constants in all comparisons — never inline literal version numbers in the parser.

**Warning signs:**
- The support-window boundary is tested only with "typical" versions, not edge cases.
- `HumlVersion` comparison uses `>` instead of `>=` for the lower bound.
- Version literals appear in the Lexer or Parser rather than in `SpecVersionPolicy`.

**Phase to address:** Version-aware options / `HumlOptions` implementation phase.

---

### Pitfall 7: git submodule not initialised in CI — fixture files missing, tests pass vacuously

**What goes wrong:**
The `huml-lang/tests` submodules are not checked out in CI because `actions/checkout` defaults to `submodules: false`. The fixture directory is empty, the `MemberData` provider yields zero entries, and xUnit Theory tests with zero data rows pass by default (or are silently skipped). The CI reports green while the parser is completely untested against the shared suite.

**Why it happens:**
`actions/checkout@v4` requires explicit opt-in for submodule checkout: `submodules: recursive`. Without it, `.gitmodules` is present but the submodule directories are empty. xUnit `Theory` with a `MemberData` that returns an empty enumerable does not fail — it produces zero test cases and the run appears successful.

**How to avoid:**
In the GitHub Actions workflow:
```yaml
- uses: actions/checkout@v4
  with:
    submodules: recursive
```
Add a guard test (not a Theory) that asserts the fixture directory is non-empty:
```csharp
[Fact]
public void FixtureDirectoryIsPopulated()
{
    var dir = Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "fixtures", "v0.2");
    Assert.True(Directory.GetFiles(dir, "*.huml").Length > 0,
        "Fixture directory is empty — likely a missing submodule checkout in CI.");
}
```
Add a CI step that fails loudly if the submodule directory is empty before running tests.

**Warning signs:**
- CI test run reports 0 Theory test cases for fixture-driven tests.
- The fixture directory size is 0 bytes in the workflow artifact.
- Build log shows "Initialized empty submodule" without a subsequent fetch.

**Phase to address:** Test infrastructure / CI setup phase (before any fixture-driven tests are written).

---

### Pitfall 8: `AppContext.BaseDirectory` vs `Environment.CurrentDirectory` — fixture paths break in CI

**What goes wrong:**
Test fixture file resolution uses `Environment.CurrentDirectory` or a hard-coded relative path. Locally, the working directory is the project root so `../../fixtures/v0.2/` resolves correctly. In GitHub Actions, `dotnet test` is invoked from the repository root but the working directory during test execution is the test output directory (e.g., `bin/Debug/net8.0/`). Relative paths resolve to non-existent locations; the fixture provider returns empty results; tests pass vacuously (see Pitfall 7).

**Why it happens:**
`Environment.CurrentDirectory` is the process working directory, which is controlled by the test runner and differs between `dotnet test` invocations. `AppContext.BaseDirectory` is the test assembly's output directory, which is stable and consistent across all environments.

**How to avoid:**
Always resolve fixture paths relative to `AppContext.BaseDirectory`:
```csharp
var fixturesRoot = Path.GetFullPath(
    Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "tests", "fixtures"));
```
Adjust the `..` depth to match the project structure. Verify with both `dotnet test` from the repo root and from within the test project directory. Document the resolved path in a `[Fact]` that prints it on failure.

**Warning signs:**
- Tests pass locally but fail (or silently produce 0 cases) in CI.
- `Directory.Exists(fixturesRoot)` returns `false` only in CI.
- The test project uses `Environment.CurrentDirectory` anywhere.

**Phase to address:** Test infrastructure phase.

---

## Technical Debt Patterns

| Shortcut | Immediate Benefit | Long-term Cost | When Acceptable |
|----------|-------------------|----------------|-----------------|
| Inline version literals in Lexer/Parser instead of `SpecVersionPolicy` constants | Faster to write | Version policy drift — error messages go stale; search for version boundaries is unreliable | Never |
| Skip `.snupkg` (symbols package) to simplify CI | Fewer CI steps | Consumers cannot step into source; debugging NuGet issues is painful | Never for a public library |
| Use `GeneratePackageOnBuild=true` | Pack on every build locally | Silently broken multi-TFM packages (see Pitfall 1) | Only for single-TFM projects |
| Reflection scan without `IsExternalInit` check | Simpler deserialiser code | Silent property-skipping for `init`-only types (see Pitfall 5) | Never |
| Single CI matrix row (`net8.0` only) | Faster CI | TFM-specific regressions are invisible | MVP only, before public release |
| `Environment.CurrentDirectory` for fixture paths | Works locally | CI-only test failures that are hard to diagnose | Never in committed test code |

---

## Integration Gotchas

| Integration | Common Mistake | Correct Approach |
|-------------|----------------|------------------|
| `actions/checkout` + git submodules | Omit `submodules: recursive` | `submodules: recursive` in every job that needs fixtures |
| `Microsoft.SourceLink.GitHub` | Reference the package but omit `PublishRepositoryUrl=true` | Add all three required properties: `PublishRepositoryUrl`, `EmbedUntrackedSources`, `ContinuousIntegrationBuild` |
| `dotnet pack` in CI | Run `dotnet pack` without prior `dotnet build` in Release mode | Always `dotnet build -c Release` then `dotnet pack -c Release --no-build` |
| NuGet.org push | Push without validating `.snupkg` | Run `dotnet sourcelink test *.snupkg` before push |
| xUnit MemberData + empty enumerable | Theory data provider returns `IEnumerable<object[]>` backed by `Directory.GetFiles()` on missing path | Assert directory is non-empty before yielding; add a sentinel `[Fact]` |

---

## Performance Traps

| Trap | Symptoms | Prevention | When It Breaks |
|------|----------|------------|----------------|
| Recursive-descent parser without depth limit | `StackOverflowException` on pathologically nested HUML (e.g., 10,000 nested maps) — unrecoverable crash, not a catchable exception | Maintain an explicit recursion counter; throw `HumlParseException` at a configurable depth limit (default: 512) | Any untrusted input; library-in-library embedding scenarios |
| Reflection `GetProperties()` called per deserialise call | Slow for tight loops (config reloading) | Cache `PropertyInfo[]` per `Type` in a `ConcurrentDictionary<Type, PropertyInfo[]>` | At ~1,000 calls/sec with complex types |
| `string.Split()` on HUML lines during lexing | Allocations per line; GC pressure | Lex over `ReadOnlySpan<char>` / `ReadOnlyMemory<char>` slices; avoid materialising strings until token emission | Large HUML documents (>1MB) |

---

## "Looks Done But Isn't" Checklist

- [ ] **Multi-TFM NuGet package:** Inspect the `.nupkg` with NuGet Package Explorer — verify `lib/netstandard2.1/`, `lib/net8.0/`, `lib/net9.0/`, `lib/net10.0/` all appear.
- [ ] **SourceLink:** Run `dotnet sourcelink test Huml.Net.*.snupkg` — must exit 0.
- [ ] **XML documentation:** Verify `<GenerateDocumentationFile>true</GenerateDocumentationFile>` is set and the `.nupkg` contains `Huml.Net.xml` in each `lib/<tfm>/` folder.
- [ ] **Fixture coverage:** Run tests with `--logger "console;verbosity=detailed"` and confirm the Theory count matches the number of `.huml` files in both `v0.1/` and `v0.2/` fixture directories.
- [ ] **Version boundary tests:** Confirm `HumlUnsupportedVersionException` is thrown for a document one minor version below the oldest supported, and NOT thrown for the oldest supported.
- [ ] **`init`-only property deserialisation:** Add a test type with `public string Foo { get; init; }` and confirm deserialisation either populates it or throws a clear error — not silently returns `null`.
- [ ] **CI submodule state:** Check that the CI workflow log shows `Cloning into '...'` for each submodule, not just "Initialized empty".
- [ ] **`[Obsolete]` annotation on deprecated spec version support:** Confirm the `HumlOptions.Version` path for v0.1 carries `[Obsolete("HUML v0.1 support will be removed when v0.3 ships.")]` so consumers get a compile warning.

---

## Recovery Strategies

| Pitfall | Recovery Cost | Recovery Steps |
|---------|---------------|----------------|
| Broken multi-TFM package already published | HIGH | Yank the broken version from NuGet.org; fix `dotnet pack` invocation; republish as a patch version; document the bad version in the README |
| SourceLink broken in published package | LOW | Fix properties; republish patch; the `.snupkg` can be pushed independently to NuGet.org symbols server |
| Vacuous CI tests discovered late (submodule issue) | MEDIUM | Add `submodules: recursive`; re-run CI; assess whether any parser bugs were missed; add sentinel fixture guard test |
| Reflection silent-skip discovered after consumers are using `init` types | MEDIUM | Add `IsExternalInit` detection; add a constructor-binding fallback or explicit error; release as minor version (behaviour change) |
| Stack overflow in production from malicious input | HIGH | Add max-depth guard immediately; patch release with CVE advisory; semantic version bump from `1.x` to `1.x+1` |

---

## Pitfall-to-Phase Mapping

| Pitfall | Prevention Phase | Verification |
|---------|------------------|--------------|
| `GeneratePackageOnBuild` broken multi-TFM pack | CI/NuGet packaging setup | NuGet Package Explorer inspection in CI artifact upload step |
| SourceLink silent failure | CI/NuGet packaging setup | `dotnet sourcelink test` step in CI |
| Conditional compilation ordering | Core parser / TFM scaffolding | Per-TFM test matrix asserting sentinel values per TFM |
| `ReadOnlySpan<char>` overload ambiguity (C# 14) | Public API design | Compile a consumer test project targeting `net10.0` / C# 14 |
| Reflection `init`-only setter silent skip | Deserialiser implementation | Fixture test with `init`-only POCO |
| Version gate off-by-one | Version-aware options implementation | Explicit boundary-value tests for `SpecVersionPolicy` |
| Submodule not initialised in CI | Test infrastructure / CI setup | Sentinel `[Fact]` asserting non-empty fixture directory |
| `Environment.CurrentDirectory` path bug | Test infrastructure / CI setup | Run `dotnet test` from multiple working directories in CI |
| Recursive-descent stack overflow | Parser implementation | Fuzz test with 1,000-deep nested map literal |

---

## Sources

- [Cross-platform targeting for .NET libraries — Microsoft Learn](https://learn.microsoft.com/en-us/dotnet/standard/library-guidance/cross-platform-targeting)
- [Target frameworks in SDK-style projects — Microsoft Learn](https://learn.microsoft.com/en-us/dotnet/standard/frameworks)
- [Conditional compilation depending on framework — Microsoft Q&A](https://learn.microsoft.com/en-us/answers/questions/1684807/conditional-compilation-depending-on-the-framework)
- [net5.0 TFM defines both NET5_0 and NETCOREAPP3_1 — dotnet/sdk #13377](https://github.com/dotnet/sdk/issues/13377)
- [Breaking change: C# 14 overload resolution with span parameters — Microsoft Learn](https://learn.microsoft.com/en-us/dotnet/core/compatibility/core-libraries/10.0/csharp-overload-resolution)
- [Breaking change: Ambiguous overload resolution (StringValues) — .NET 9 — Microsoft Learn](https://learn.microsoft.com/en-us/dotnet/core/compatibility/core-libraries/9.0/ambiguous-overload)
- [Source Link and .NET libraries — Microsoft Learn](https://learn.microsoft.com/en-us/dotnet/standard/library-guidance/sourcelink)
- [Producing Packages with Source Link — .NET Blog](https://devblogs.microsoft.com/dotnet/producing-packages-with-source-link/)
- [dotnet/sourcelink README](https://github.com/dotnet/sourcelink/blob/main/docs/README.md)
- [SourceLink deterministic builds — clairernovotny/DeterministicBuilds](https://github.com/clairernovotny/DeterministicBuilds)
- [NuGet NU5128 warning — Microsoft Learn](https://learn.microsoft.com/en-us/nuget/reference/errors-and-warnings/nu5128)
- [Dependent project does not generate NuGet package with multiple TFMs — dotnet/msbuild #7911](https://github.com/dotnet/msbuild/issues/7911)
- [Reflection-based deserialiser resolves metadata eagerly — .NET 8 breaking change](https://learn.microsoft.com/en-us/dotnet/core/compatibility/serialization/8.0/metadata-resolving)
- [Surprising deserialisation behaviour with private setters — Newtonsoft.Json #322](https://github.com/JamesNK/Newtonsoft.Json/issues/322)
- [System.Text.Json null for non-nullable property discussion — dotnet/runtime #62722](https://github.com/dotnet/runtime/discussions/62722)
- [Inconsistent current directory in dotnet-test-xunit — xunit/xunit #978](https://github.com/xunit/xunit/issues/978)
- [GitHub Actions: Checkout submodules doesn't work — community discussion #160568](https://github.com/orgs/community/discussions/160568)
- [Scriban uncontrolled recursion in parser leads to stack overflow — GitLab Advisory](https://advisories.gitlab.com/pkg/nuget/scriban/GHSA-wgh7-7m3c-fx25/)
- [CVE-2024-21907: Newtonsoft Json.NET DoS Vulnerability — SentinelOne](https://www.sentinelone.com/vulnerability-database/cve-2024-21907/)

---
*Pitfalls research for: Huml.Net — .NET HUML parser library*
*Researched: 2026-03-20*
