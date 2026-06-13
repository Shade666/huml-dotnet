# AGENTS.md

Guidance for AI coding agents working in this repository. For the full architecture and
conventions, see [CLAUDE.md](CLAUDE.md); this file is the short orientation.

## What this is

**Huml.Net** — the first-party .NET implementation of HUML (Human-oriented Markup Language).
Parsing, serialisation, and deserialisation with a `System.Text.Json`-style API and zero runtime
dependencies.

## Public API

The sole public entry point is the static facade **`HumlSerializer`** (`src/Huml.Net/HumlSerializer.cs`):
`Serialize` / `Deserialize` / `Parse` / `Populate`. Everything else is `internal`. The committed
public-surface baseline is `docs/public-api.txt` — treat it as the source of truth, and keep public
API changes additive per `docs/internals/api-freeze.md`.

## Commands

```bash
dotnet build                                            # build all TFMs
dotnet test                                             # run all tests
dotnet test --framework net10.0                         # single TFM
dotnet test --filter "FullyQualifiedName~SharedSuiteTests"   # one class
```

Clone with `--recurse-submodules` — the `fixtures/v0.1` and `fixtures/v0.2` directories are
submodules; without them the fixture Theory tests find zero rows.

## Conventions (non-negotiable)

- **British English** in all docs and comments (`serialisation`, `behaviour`, `recognised`).
- **xUnit v3 + AwesomeAssertions** — never FluentAssertions.
- **Zero warnings** (`TreatWarningsAsErrors`) across `netstandard2.1`/`net8.0`/`net9.0`/`net10.0`.
- **No external runtime dependencies** in `Huml.Net.csproj`.
- **CHANGELOG discipline** — add user-visible changes under `## [Unreleased]` as you go.
- New error-or-no-error parse behaviours: assess against `.claude/rules/fixture-gaps.md` and stage
  language-agnostic cases in `fixtures/extensions/`.

## Task tracking

Work is tracked in `backlog/` via the Backlog.md MCP — see the workflow below.

<!-- BACKLOG.MD MCP GUIDELINES START -->

<CRITICAL_INSTRUCTION>

## BACKLOG WORKFLOW INSTRUCTIONS

This project uses Backlog.md MCP for all task and project management activities.

**CRITICAL GUIDANCE**

- If your client supports MCP resources, read `backlog://workflow/overview` to understand when and how to use Backlog for this project.
- If your client only supports tools or the above request fails, call `backlog.get_backlog_instructions()` to load the tool-oriented overview. Use the `instruction` selector when you need `task-creation`, `task-execution`, or `task-finalization`.

- **First time working here?** Read the overview resource IMMEDIATELY to learn the workflow
- **Already familiar?** You should have the overview cached ("## Backlog.md Overview (MCP)")
- **When to read it**: BEFORE creating tasks, or when you're unsure whether to track work

These guides cover:
- Decision framework for when to create tasks
- Search-first workflow to avoid duplicates
- Links to detailed guides for task creation, execution, and finalization
- MCP tools reference

You MUST read the overview resource to understand the complete workflow. The information is NOT summarized here.

</CRITICAL_INSTRUCTION>

<!-- BACKLOG.MD MCP GUIDELINES END -->
