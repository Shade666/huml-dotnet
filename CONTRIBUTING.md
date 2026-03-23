# Contributing to Huml.Net

## Welcome

Contributions to Huml.Net are welcome — including AI-assisted ones. Whether you are reporting a
bug, requesting a feature, or opening a pull request, your involvement is appreciated.

## Getting Started

1. Fork the repository on GitHub.
2. Clone your fork with `--recurse-submodules` — the fixture directories (`fixtures/v0.1/` and
   `fixtures/v0.2/`) are git submodules. Omitting this flag leaves them empty and the Theory
   tests will find zero fixtures.

   ```bash
   git clone --recurse-submodules https://github.com/primeBeri/huml-dotnet.git
   ```

   If you have already cloned without the flag, initialise the submodules manually:

   ```bash
   git submodule update --init --recursive
   ```

3. Create a branch from `main` for your change.

## Development Setup

**Prerequisite:** .NET 10 SDK. It includes the `net8.0` and `net9.0` targeting packs, so all
three test target frameworks are available out of the box.

**Build:**

```bash
dotnet build
```

**Run all tests:**

```bash
dotnet test
```

**Run tests against a single target framework:**

```bash
dotnet test --framework net8.0
dotnet test --framework net9.0
dotnet test --framework net10.0
```

## Code Standards

- **Language version:** C# 13.
- **Editor config:** An `.editorconfig` file is checked into the repo root. Follow it — it sets
  indent style, line endings, and naming conventions.
- **Zero warnings policy:** `TreatWarningsAsErrors` is active across all target frameworks. New
  code must compile without warnings on `netstandard2.1`, `net8.0`, `net9.0`, and `net10.0`.
- **One type per file:** The Meziantou MA0048 analyser rule is enforced. Each public type lives in
  its own file, named after the type.
- **British English** in all documentation and code comments: `serialisation`, `deserialisation`,
  `behaviour`, `recognised`, `initialise`.

## Testing Requirements

- **Test framework:** xUnit v3 (3.2.2)
- **Assertion library:** AwesomeAssertions 9.4.0. Use only AwesomeAssertions — no other
  assertion library is a project dependency and its extension methods will not resolve.
- All tests must pass on `net8.0`, `net9.0`, and `net10.0`.
- Both fixture suites (`fixtures/v0.1/` and `fixtures/v0.2/`) must run with a non-zero Theory
  count. Extension fixtures in `fixtures/extensions/` run automatically alongside them.

**Assertion patterns:**

```csharp
// Positive — expect successful parse
var act = () => Huml.Parse(input, HumlOptions.Default);
act.Should().NotThrow();

// Negative — expect a parse error
var act = () => Huml.Parse(input, HumlOptions.Default);
act.Should().Throw<HumlParseException>();
```

## Constraints

- **No external runtime dependencies** in `Huml.Net.csproj`. `MinVer` and `SourceLink` are
  build-only (`PrivateAssets="All"`).
- **Linting logic belongs in `Huml.Net.Linting`**, a future separate package. No style or
  advisory logic belongs in the core parser.
- **Library targets:** `netstandard2.1`, `net8.0`, `net9.0`, `net10.0`.
- **Test targets:** `net8.0`, `net9.0`, `net10.0`.

## AI-Assisted Contributions

AI-assisted contributions are explicitly welcome, subject to these conditions:

- The contribution must be **human-reviewed** before submission. The submitter takes
  responsibility for its correctness.
- `dotnet test` must pass — green on all three target frameworks.
- Both fixture suites must pass with a non-zero Theory count.
- The contribution must be submitted as a pull request for maintainer review.

## Understanding the Codebase

These guides explain the internal architecture for contributors:

- [Pipeline Overview](docs/internals/pipeline.md) — end-to-end data flow from input string to
  serialised output and back again.
- [Version Gates](docs/internals/version-gates.md) — how spec-version branching works and how
  to add support for a new HUML spec version.
- [Extending the Library](docs/internals/extending.md) — checklists for adding new AST nodes,
  token types, and serialisation/deserialisation support for new .NET types.

## Pull Request Process

1. Fork the repository and create a branch from `main`.
2. Implement your change with tests.
3. Ensure `dotnet test` is green before opening the PR.
4. In the PR description, explain **what** changed and **why**.
5. Link any related GitHub Issues in the description.
6. Open the pull request against `main`.
