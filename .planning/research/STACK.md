# Stack Research

**Domain:** .NET parser / serialisation library (NuGet package)
**Researched:** 2026-03-20
**Confidence:** HIGH (all versions verified via NuGet.org and official sources)

---

## Recommended Stack

### Core Technologies

| Technology | Version | Purpose | Why Recommended |
|------------|---------|---------|-----------------|
| .NET SDK | 10.0.201 | Build toolchain | Latest stable LTS SDK (released March 2026). Supports C# 13 and net10.0 TFM. Always build with the newest SDK even when the library multi-targets. |
| C# language | 13 (`<LangVersion>latest</LangVersion>`) | Implementation language | C# 13 is the version shipping with .NET 9/10. `latest` in csproj tracks the SDK, avoiding version drift without pinning a magic number. |
| Target frameworks | `netstandard2.1;net8.0;net9.0;net10.0` | Public API surface | `netstandard2.1` is the compat floor (provides `ReadOnlySpan<char>`, `Span<T>`, `Memory<T>`, `IAsyncEnumerable<T>` — everything needed without external deps). Explicit net8/9/10 TFMs let modern consumers receive optimised builds via NuGet's TFM resolution and allow `#if NET8_0_OR_GREATER` guards for future perf optimisations. |
| SDK-style csproj | (MSBuild built-in) | Project system | Compact, multi-TFM friendly, first-class NuGet metadata support. No packages.config, no legacy `.sln`-level package management. |

### Test Infrastructure

| Package | Version | Purpose | Why Recommended |
|---------|---------|---------|-----------------|
| `xunit.v3` | 3.2.2 | Test framework | xUnit v3 is the current stable generation (January 2026). Supports .NET 8+ test projects. Native Microsoft Testing Platform support means `dotnet run` can drive tests without VSTest. Use on `net8.0` test TFM (test projects are executables, not library targets). |
| `xunit.runner.visualstudio` | 3.1.5 | VS Test Explorer / `dotnet test` VSTest adapter | Required when running via `dotnet test` (VSTest host). Provides IDE integration in Visual Studio and Rider. |
| `Microsoft.NET.Test.Sdk` | 18.3.0 | Test host entry point | Required by VSTest runner. Supplies MSBuild props/targets that make a test project an executable test host. |
| `AwesomeAssertions` | 9.4.0 | Fluent assertion library | Project convention explicitly requires this over FluentAssertions. AwesomeAssertions is the community-maintained Apache 2.0 fork of FluentAssertions (forked after FA introduced commercial licensing for non-OSS projects). API is a drop-in replacement. Supports `netstandard2.0` / `net6.0`+ — fully compatible with the test project's `net8.0` target. |

### NuGet Packaging Infrastructure

| Package | Version | Purpose | Why Recommended |
|---------|---------|---------|-----------------|
| `Microsoft.SourceLink.GitHub` | 10.0.201 | Embeds source control metadata in PDB | Enables debugger step-into-source for NuGet consumers. Zero-cost to add; expected by serious library consumers. Reference with `PrivateAssets="All"` — dev-only, nothing lands in consumer's dependency graph. |
| `MinVer` | 7.0.0 | Git-tag-driven package versioning | Derives `PackageVersion` from the latest semver git tag automatically. Zero config for simple flows. Pre-release heights (e.g., `0.1.0-alpha.5`) work without any extra tooling. Reference with `PrivateAssets="All"`. No CHANGELOG discipline needed beyond tagging. |

### Development / CI Tools

| Tool | Version | Purpose | Notes |
|------|---------|---------|-------|
| `actions/checkout` | v4 | Checkout with submodule support | Use `submodules: recursive` and `fetch-depth: 0` (required by MinVer to walk tag history). |
| `actions/setup-dotnet` | v5 | Install .NET SDK in CI | Supports `global.json`-driven SDK version pinning. |
| `NuGet/login` | v1 | OIDC Trusted Publishing | Exchanges GitHub OIDC token for a temporary NuGet API key. No long-lived secrets stored in repo. Available since NuGet Trusted Publishing GA (September 2025). |

---

## Project File Structure

### `global.json` — Pin SDK version

```json
{
  "sdk": {
    "version": "10.0.201",
    "rollForward": "latestMinor"
  }
}
```

`rollForward: latestMinor` allows CI runners with a newer patch to work without breaking.

---

### `Directory.Build.props` — Shared settings for all projects

Place at the repo root. Every `src/` and `tests/` project inherits these automatically.

```xml
<Project>
  <PropertyGroup>
    <!-- Language -->
    <LangVersion>latest</LangVersion>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
    <TreatWarningsAsErrors>true</TreatWarningsAsErrors>

    <!-- Deterministic / reproducible builds -->
    <!-- ContinuousIntegrationBuild is set true by CI env var GITHUB_ACTIONS -->
    <ContinuousIntegrationBuild Condition="'$(GITHUB_ACTIONS)' == 'true'">true</ContinuousIntegrationBuild>
    <Deterministic>true</Deterministic>
    <EmbedUntrackedSources>true</EmbedUntrackedSources>

    <!-- SourceLink (always embed PDB into the package) -->
    <DebugType>embedded</DebugType>
    <IncludeSymbols>false</IncludeSymbols>
  </PropertyGroup>

  <ItemGroup>
    <!-- SourceLink: dev-only, does not flow to consumers -->
    <PackageReference Include="Microsoft.SourceLink.GitHub" Version="10.0.201" PrivateAssets="All" />
    <!-- MinVer: derives PackageVersion from git tags -->
    <PackageReference Include="MinVer" Version="7.0.0" PrivateAssets="All" />
  </ItemGroup>
</Project>
```

**Why `DebugType=embedded` and `IncludeSymbols=false`:** Embedding PDB into the nupkg directly (rather than a separate `.snupkg`) is now the recommended approach for library authors. Consumers get source stepping without needing a symbol server configured.

**Why `ContinuousIntegrationBuild` conditional on `GITHUB_ACTIONS`:** Deterministic builds rewrite local source paths with repository-relative paths, which breaks local debugger source lookup. The conditional restricts this to CI only.

---

### `src/Huml.Net/Huml.Net.csproj` — Library project

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFrameworks>netstandard2.1;net8.0;net9.0;net10.0</TargetFrameworks>
    <AssemblyName>Huml.Net</AssemblyName>
    <RootNamespace>HumlNet</RootNamespace>
    <GenerateDocumentationFile>true</GenerateDocumentationFile>

    <!-- NuGet metadata -->
    <PackageId>Huml.Net</PackageId>
    <Authors>Richard (Radberi)</Authors>
    <Description>A .NET library for parsing and serialising HUML (Human-oriented Markup Language) documents. Zero external runtime dependencies. System.Text.Json-style API.</Description>
    <PackageLicenseExpression>MIT</PackageLicenseExpression>
    <PackageProjectUrl>https://github.com/Shade666/huml-dotnet</PackageProjectUrl>
    <RepositoryUrl>https://github.com/Shade666/huml-dotnet</RepositoryUrl>
    <RepositoryType>git</RepositoryType>
    <PackageTags>huml;serialisation;serialization;configuration;parser;markup</PackageTags>
    <PackageReadmeFile>README.md</PackageReadmeFile>
    <PackageIcon>icon.png</PackageIcon>
  </PropertyGroup>

  <!-- Embed README into the NuGet package -->
  <ItemGroup>
    <None Include="..\..\README.md" Pack="true" PackagePath="\" />
    <!-- <None Include="..\..\icon.png" Pack="true" PackagePath="\" /> -->
  </ItemGroup>
</Project>
```

**Notes:**
- `GenerateDocumentationFile` is required for NuGet consumers to get IntelliSense XML doc comments.
- `PackageReadmeFile` embeds the repo README for display on nuget.org (supported since NuGet 5.10 / SDK 5.0.300).
- No runtime `<PackageReference>` items — zero external dependencies is a hard constraint.
- `PackageVersion` is not set here; MinVer derives it from git tags at build time.

---

### `tests/Huml.Net.Tests/Huml.Net.Tests.csproj` — Test project

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <!-- Test projects are executables; cannot target netstandard -->
    <!-- Multi-target to validate against multiple runtimes -->
    <TargetFrameworks>net8.0;net9.0;net10.0</TargetFrameworks>
    <IsPackable>false</IsPackable>
    <IsPublishable>false</IsPublishable>
  </PropertyGroup>

  <ItemGroup>
    <ProjectReference Include="..\..\src\Huml.Net\Huml.Net.csproj" />
  </ItemGroup>

  <ItemGroup>
    <PackageReference Include="xunit.v3" Version="3.2.2" />
    <PackageReference Include="xunit.runner.visualstudio" Version="3.1.5" PrivateAssets="All" />
    <PackageReference Include="Microsoft.NET.Test.Sdk" Version="18.3.0" />
    <PackageReference Include="AwesomeAssertions" Version="9.4.0" />
  </ItemGroup>
</Project>
```

**Why `net8.0;net9.0;net10.0` for tests, not `netstandard2.1`:** xUnit v3 test projects are always executables; a `netstandard` TFM is not a valid test target (documented by xUnit as a hard constraint — "netstandard is an API, not a platform"). The test project multi-targets the three modern runtimes to catch TFM-specific regressions. The `netstandard2.1` compatibility of the library is exercised via each runtime's interpretation of the standard.

---

## GitHub Actions CI Workflow

### `.github/workflows/ci.yml` — Build and test

```yaml
name: CI

on:
  push:
    branches: [main]
  pull_request:
    branches: [main]

jobs:
  test:
    name: Build and Test
    runs-on: ubuntu-latest
    steps:
      - name: Checkout (with submodules)
        uses: actions/checkout@v4
        with:
          submodules: recursive
          # fetch-depth 0 required for MinVer to walk full tag history
          fetch-depth: 0

      - name: Setup .NET
        uses: actions/setup-dotnet@v5
        with:
          # global.json in repo root pins the exact SDK version
          global-json-file: global.json

      - name: Restore
        run: dotnet restore

      - name: Build
        run: dotnet build --no-restore --configuration Release

      - name: Test
        run: dotnet test --no-build --configuration Release --verbosity normal
```

### `.github/workflows/publish.yml` — Pack and publish (on release tag)

```yaml
name: Publish NuGet

on:
  push:
    tags:
      - '[0-9]+.[0-9]+.[0-9]+*'   # semver tags: 0.1.0, 1.0.0-beta.1, etc.

permissions:
  id-token: write   # required for OIDC Trusted Publishing

jobs:
  publish:
    name: Pack and Publish
    runs-on: ubuntu-latest
    environment: nuget-publish   # protect with GitHub Environment approval
    steps:
      - name: Checkout
        uses: actions/checkout@v4
        with:
          fetch-depth: 0          # MinVer needs full tag history

      - name: Setup .NET
        uses: actions/setup-dotnet@v5
        with:
          global-json-file: global.json

      - name: Restore
        run: dotnet restore

      - name: Build
        run: dotnet build --no-restore --configuration Release

      - name: Test
        run: dotnet test --no-build --configuration Release

      - name: Pack
        run: dotnet pack src/Huml.Net/Huml.Net.csproj --no-build --configuration Release --output ./nupkg

      - name: Login to nuget.org (OIDC Trusted Publishing)
        uses: NuGet/login@v1
        with:
          service-index: https://api.nuget.org/v3/index.json

      - name: Push to nuget.org
        run: dotnet nuget push ./nupkg/*.nupkg --source https://api.nuget.org/v3/index.json
```

**Why OIDC Trusted Publishing over API keys:** NuGet.org added Trusted Publishing in September 2025. The OIDC token exchange produces a short-lived (~1 hour) API key. No long-lived secrets are stored in the repository. The `permissions: id-token: write` block is the only repo configuration required beyond registering the policy on nuget.org.

**Why tag-triggered publish:** Avoids accidental releases from main branch pushes. MinVer reads the tag to set the `PackageVersion` automatically — the tag is both the trigger and the version source.

---

## Git Submodule Setup

The shared test suite is consumed as two pinned submodules:

```bash
git submodule add https://github.com/huml-lang/tests tests/Huml.Net.Tests/SharedSuite/Fixtures/v0.1
git -C tests/Huml.Net.Tests/SharedSuite/Fixtures/v0.1 checkout v0.1

git submodule add https://github.com/huml-lang/tests tests/Huml.Net.Tests/SharedSuite/Fixtures/v0.2
git -C tests/Huml.Net.Tests/SharedSuite/Fixtures/v0.2 checkout v0.2
```

The CI workflow uses `submodules: recursive` in `actions/checkout@v4` to hydrate both submodules automatically. No manual `git submodule update` step is needed.

---

## Alternatives Considered

| Recommended | Alternative | Why Not |
|-------------|-------------|---------|
| `xunit.v3` 3.2.2 | `xunit` 2.9.3 (v2) | xUnit v2 is the legacy branch. v3 is the actively developed line. v2 lacks Microsoft Testing Platform support and has a more complex extensibility model. |
| `AwesomeAssertions` | `FluentAssertions` 7.x | FluentAssertions introduced a commercial licence for non-OSS projects (effective FA 7.x). Project convention is AwesomeAssertions, the Apache 2.0 community fork with identical API. |
| `MinVer` | Manual `<Version>` in csproj | Manual versioning requires editing the csproj on every release and is error-prone in CI. MinVer reads the git tag; version is always authoritative and derivable from source. |
| `MinVer` | `GitVersion` | GitVersion has a significantly larger configuration surface and opinionated branching strategies. MinVer's subset is all that's needed for a simple library: tag = version. |
| Embedded PDB (`DebugType=embedded`) | `.snupkg` symbol package | `.snupkg` requires consumers to configure a symbol server. Embedded PDB works out of the box in every IDE without configuration. Slight package size increase is acceptable for a library this size. |
| OIDC Trusted Publishing | Stored `NUGET_API_KEY` secret | Long-lived API keys are a rotation and leakage risk. Trusted Publishing (GA September 2025) is the current best practice for GitHub-hosted .NET library publishing. |
| `netstandard2.1` compat floor | `netstandard2.0` | `netstandard2.0` does not include `ReadOnlySpan<char>` / `Span<T>`, which are required for the `Deserialize<T>(ReadOnlySpan<char>)` overload. Dropping `netstandard2.0` also drops .NET Framework support cleanly (which is intentional). |

---

## What NOT to Use

| Avoid | Why | Use Instead |
|-------|-----|-------------|
| `FluentAssertions` (any version) | Project convention disallows it; FA licensing changed for commercial use in later versions | `AwesomeAssertions` 9.4.0 — identical API, Apache 2.0 |
| `xunit` (v2 package `xunit` 2.9.x) | Legacy generation; xUnit v3 is the current stable line since July 2025 | `xunit.v3` 3.2.2 |
| `<Version>` hardcoded in csproj | Error-prone; must be changed on every release; CI has to read/write files | `MinVer` reads version from git tag |
| `.snupkg` symbol packages | Requires symbol server configuration in consumer's IDE | `DebugType=embedded` in PDB |
| `packages.config` | Legacy NuGet format, not compatible with SDK-style projects | `<PackageReference>` in SDK-style csproj |
| Separate `netstandard2.0` TFM | `Span<T>` / `ReadOnlySpan<char>` not available; .NET Framework exclusion is intentional | `netstandard2.1` is the floor |
| `ContinuousIntegrationBuild=true` in local builds | Remaps source paths to repository-relative; breaks local debugger source lookup | Set conditionally via `GITHUB_ACTIONS` env var (see `Directory.Build.props` above) |
| Runtime `<PackageReference>` in `Huml.Net.csproj` | Violates the zero-external-runtime-dependencies constraint | Implement everything in standard BCL; `System.Reflection` for deserialiser |

---

## Version Compatibility Matrix

| Package | Supports test TFMs? | Notes |
|---------|---------------------|-------|
| `xunit.v3` 3.2.2 | net8.0, net9.0, net10.0 | Min .NET is net8.0. Cannot use netstandard2.1 for test project. |
| `AwesomeAssertions` 9.4.0 | netstandard2.0+, net6.0+, net8.0+ | Compatible with all test TFMs. |
| `Microsoft.SourceLink.GitHub` 10.0.201 | Build-time only (`PrivateAssets="All"`) | Does not ship with the package; no consumer compat concern. |
| `MinVer` 7.0.0 | Build-time only (`PrivateAssets="All"`) | Does not ship with the package; no consumer compat concern. |
| Library (`netstandard2.1`) | Runs on: .NET Core 3.x, .NET 5–10, Unity (IL2CPP) | Deliberately excludes .NET Framework (ns2.0 territory only). |

---

## Sources

- [NuGet Gallery: xunit.v3 3.2.2](https://www.nuget.org/packages/xunit.v3) — version and framework support verified (January 2026)
- [NuGet Gallery: AwesomeAssertions 9.4.0](https://www.nuget.org/packages/AwesomeAssertions) — version verified (February 2026)
- [NuGet Gallery: Microsoft.SourceLink.GitHub 10.0.201](https://www.nuget.org/packages/Microsoft.SourceLink.GitHub) — version verified
- [NuGet Gallery: MinVer 7.0.0](https://www.nuget.org/packages/MinVer/) — latest stable verified
- [NuGet Gallery: xunit.runner.visualstudio 3.1.5](https://www.nuget.org/packages/xunit.runner.visualstudio) — version verified
- [NuGet Gallery: Microsoft.NET.Test.Sdk 18.3.0](https://www.nuget.org/packages/microsoft.net.test.sdk) — version verified
- [xUnit v3 Getting Started](https://xunit.net/docs/getting-started/v3/getting-started) — framework minimums and package list
- [xUnit: Why no netstandard for test projects](https://xunit.net/docs/why-no-netstandard) — rationale for net8.0 test TFM (MEDIUM confidence — confirmed by official xUnit docs)
- [.NET 10 download page](https://dotnet.microsoft.com/en-us/download/dotnet/10.0) — SDK 10.0.201 current as of March 2026
- [NuGet Trusted Publishing (Microsoft Learn)](https://learn.microsoft.com/en-us/nuget/nuget-org/trusted-publishing) — OIDC workflow pattern
- [Andrew Lock: Trusted Publishing from GitHub Actions](https://andrewlock.net/easily-publishing-nuget-packages-from-github-actions-with-trusted-publishing/) — CI workflow pattern (MEDIUM confidence)
- [dotnet/sourcelink GitHub](https://github.com/dotnet/sourcelink) — SourceLink configuration patterns

---
*Stack research for: Huml.Net — .NET HUML parser/serialisation library*
*Researched: 2026-03-20*
