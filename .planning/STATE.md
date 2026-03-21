---
gsd_state_version: 1.0
milestone: v0.1
milestone_name: milestone
status: unknown
stopped_at: Completed 04-01-PLAN.md
last_updated: "2026-03-21T01:01:24.101Z"
progress:
  total_phases: 8
  completed_phases: 4
  total_plans: 6
  completed_plans: 6
---

# Project State

## Project Reference

See: .planning/PROJECT.md (updated 2026-03-20)

**Core value:** Full HUML spec compliance (v0.1 + v0.2), validated against the shared `huml-lang/tests` test suite, with zero external runtime dependencies and a `System.Text.Json`-style API that .NET developers already know.
**Current focus:** Phase 04 — ast-node-hierarchy

## Current Position

Phase: 04 (ast-node-hierarchy) — EXECUTING
Plan: 1 of 1

## Performance Metrics

**Velocity:**

- Total plans completed: 0
- Average duration: -
- Total execution time: 0 hours

**By Phase:**

| Phase | Plans | Total | Avg/Plan |
|-------|-------|-------|----------|
| - | - | - | - |

**Recent Trend:**

- Last 5 plans: -
- Trend: -

*Updated after each plan completion*
| Phase 01 P01 | 8 | 2 tasks | 8 files |
| Phase 01 P02 | 1 | 2 tasks | 2 files |
| Phase 02 P01 | 4 | 3 tasks | 12 files |
| Phase 03 P01 | 3 | 1 tasks | 6 files |
| Phase 03 P02 | 5 | 2 tasks | 3 files |
| Phase 04 P01 | 2 | 2 tasks | 8 files |

## Accumulated Context

### Decisions

Decisions are logged in PROJECT.md Key Decisions table.
Recent decisions affecting current work:

- All phases: Single Lexer/Parser code path with `>=` version gates — no forked classes per spec version
- Phase 1: `dotnet pack` as dedicated CI step only — `GeneratePackageOnBuild` must NOT be used
- Phase 3: `ReadOnlySpan<char>` is the single implementation overload for Deserialize; `string` variant wraps via `AsSpan()` to avoid C# 14 CS0121 ambiguity
- Phase 6: `init`-only setter detection via `IsExternalInit` custom modifier; throw `HumlDeserializeException` rather than silently skip
- [Phase 01]: huml-lang/tests tags are v0.1.0 and v0.2.0, not v0.1/v0.2 as initially assumed
- [Phase 01]: dotnet new sln requires --format sln flag on SDK 10+ to produce .sln instead of .slnx
- [Phase 01]: OIDC Trusted Publishing via NuGet/login@v1 eliminates long-lived API key secrets from the repository
- [Phase 01]: CI uses submodules: recursive + fetch-depth: 0 for fixture submodules and MinVer tag walking
- [Phase 02]: IsExternalInit shim in IsExternalInit.cs required for init-only setters on netstandard2.1 — guarded by #if NETSTANDARD2_1
- [Phase 02]: HumlUnsupportedVersionException omits binary serialisation constructor — SYSLIB0051 fires on .NET 8+ under TreatWarningsAsErrors; new library has no BinaryFormatter requirement
- [Phase 02]: SpecVersionPolicy is internal with InternalsVisibleTo Huml.Net.Tests — allows testing constants without making them public API
- [Phase 03]: HumlParseException placed in Huml.Net.Exceptions (not Huml.Net.Lexer.Exceptions) — thrown by both Lexer and Parser
- [Phase 03]: Token.Value is string? (nullable) so structural tokens carry null, eliminating heap allocations on the hot path
- [Phase 03]: No binary serialisation constructor on HumlParseException — SYSLIB0051 pattern from Phase 02 maintained
- [Phase 03]: Test namespace collision resolved with using alias: Huml.Net.Tests.Lexer namespace shadows Lexer class — using HumlLexer = Huml.Net.Lexer.Lexer in test files
- [Phase 04]: ScalarKind.Integer (not Int) — asymmetry from TokenType.Int for semantic clarity at AST level
- [Phase 04]: HumlScalar.Value is object? — accommodates heterogeneous runtime values (string, long, double, bool, null) without generics
- [Phase 04]: IReadOnlyList<HumlNode> for collection nodes — readonly contract with reference equality for structural equality tests

### Pending Todos

None yet.

### Blockers/Concerns

- Phase 5 (Parser): HUML grammar spec details (indent rules, inline list syntax per version) are MEDIUM confidence — `huml-lang/go-huml` source must be consulted during planning
- Phase 6 (Ser/Deser): `init`-only constructor-binding design decision not yet made (throw vs constructor fallback); decide at Phase 6 planning time
- Phase 7 (Fixture Compliance): `huml-lang/tests` fixture file format (valid vs invalid subdirectory layout, expectation format) must be inspected from submodule content before writing `SharedSuiteTests.cs`

## Session Continuity

Last session: 2026-03-21T01:01:24.098Z
Stopped at: Completed 04-01-PLAN.md
Resume file: None
