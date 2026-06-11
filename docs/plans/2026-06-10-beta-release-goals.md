# Huml.Net 0.2.0-beta.1 — Beta Release Programme

**Status:** Agreed 2026-06-10
**Definition of done:** `Huml.Net 0.2.0-beta.1` published to NuGet.org, gated on goals G1–G5 below.

---

## Scope decisions (settled during scoping, 2026-06-10)

| Decision | Outcome |
|----------|---------|
| STJ parity scope | Serialiser/deserialiser feature set only. Standing anti-features stand: **no** streaming/`IAsyncEnumerable`, **no** mutable DOM (`JsonNode` equivalent), **no** `[JsonPropertyName]` interop, **no** JSON Schema export. |
| Beta version | `0.2.0-beta.1` — promote the current 0.2.0 line from alpha to beta. |
| Examples repo gating | Worked examples **and** the STJ benchmark suite gate the beta. Cross-language (go/rust) benchmarks are a post-beta stretch goal. |
| Audit depth | Adversarial multi-pass code review **plus** fuzzing and property-based testing. A scheduled CI fuzz job is out of scope (harness committed and runnable on demand). |
| Documentation format | Published documentation site (DocFX or similar) on GitHub Pages: generated API reference + curated guides. |
| Tracking | **GSD is disabled before work starts.** Programme tracked via this document and the native Claude Code task list. `.planning/` remains as historical record only. |

## Working method

- **Step 0 (before any goal work):** disable the GSD plugin, skills, and hooks. `.planning/` is no longer the source of truth.
- **Goal ordering:** G1 → G2 → G3 → (G4 ∥ G5) → release. Code-changing goals run first; the audit reviews *final* code; docs and examples are written against a frozen API.
- **API freeze checkpoint:** declared at the end of G3. Any later public-surface change requires explicit justification, because G4/G5 build on it.
- **Standing conventions:** TDD throughout, xUnit v3 + AwesomeAssertions (never FluentAssertions), zero external runtime dependencies in `Huml.Net.csproj`, incremental CHANGELOG discipline (`## [Unreleased]` always present), British English in all documentation.

---

## G1 — Verified HUML 0.1/0.2 spec compliance

The suite already passes; this goal is *verification and currency*, not new features.

1. **Submodule currency:** update `fixtures/v0.1` and `fixtures/v0.2` to the latest upstream `huml-lang/tests`; confirm all fixtures pass.
2. **Fixture-gap audit:** run the process defined in `.claude/rules/fixture-gaps.md` — cross-reference every .NET parse test against upstream coverage, add genuine language-agnostic gaps to `fixtures/extensions/`, and stage them for upstream contribution.
3. **Prose-spec sweep:** cross-check the implementation against the prose spec (huml.io) for behaviour the fixtures do not exercise. Deliverable: a written compliance report listing every checked clause and any deviations found/fixed.
4. **Green suite:** resolve the one known failing test (`LexerAllocationTests`, pre-existing) — fix it or formally waive it with written rationale. A beta ships with a green suite.

**Exit criteria:** submodules current, fixture-gap audit complete, compliance report written, 100% of tests passing.

## G2 — STJ parity closure

The heavy parity work shipped in Milestones 3–5. This goal dispositions all 13 open `999.x` backlog items into three buckets, in writing:

- **Do for beta** — genuine parity/correctness items. Candidates: 999.39 (full `HumlTypeInfo<T>` STJ parity), 999.61 (converter-list mutation hardening), plus 999.33/999.35 *if not already delivered* — the roadmap headers look stale (Milestone 5 claims polymorphism and per-member naming shipped, yet 999.29/999.35 still read BACKLOG). **First task: reconcile the roadmap against the shipped code.**
- **Do during audit** — 999.44 (fuzz/property-based tests) moves into G3.
- **Explicitly deferred** — documented as post-beta with one line of rationale each (e.g. 999.45 `IBufferWriter<char>` overload).

**Exit criteria:** every 999.x item dispositioned in writing; all "do for beta" items implemented TDD-style with tests; CHANGELOG updated incrementally.

## G3 — Security & correctness audit

### Threat model (written first, directs the review)

A parser handling untrusted input. Primary threats:

- **Stack exhaustion** — recursion depth (partially mitigated by `MaxRecursionDepth`; needs adversarial verification)
- **Allocation bombs** — huge strings, pathological multiline scalars, StringBuilder-pooling abuse
- **Quadratic-time inputs** — backtracking, repeated indent scanning
- **Malformed Unicode** — bidi control characters, surrogate pairs, invisible codepoints
- **Integer overflow** in numeric parsing

### Pass 1 — adversarial code review

Multi-agent review of the full pipeline in slices: Lexer, Parser, Deserialiser (reflection paths, constructor binding, converter dispatch), Serialiser, and the **source generator** (generated code is a distinct attack/correctness surface). Every finding is adversarially verified before acceptance. Severity-classified report; **critical and high findings are patched before beta**, mediums triaged, lows documented.

### Pass 2 — fuzzing & property-based testing (absorbs 999.44)

- SharpFuzz harness over `HumlSerializer.Parse`, seeded with the fixture corpus, run to saturation locally. Any crash, hang, or non-`HumlParseException` escape is a bug.
- Property-based round-trip tests: `Deserialize(Serialize(x)) == x` for generated object graphs; parse → serialise → parse stability for valid documents.
- Time/memory budget assertions on pathological inputs (nesting at the depth limit, megabyte-scale scalars, thousands of keys).

**Exit criteria:** zero crashes/hangs from the fuzz corpus; all critical/high findings fixed with regression tests; threat model documented in `docs/internals/`; fuzz harness committed (likely to the companion repo, runnable on demand); **API freeze declared**.

## G4 — Beta-grade documentation

### Research step (time-boxed)

Survey best-in-class examples before restructuring: Go's pkg.go.dev model (reference + runnable examples co-located), the **Diátaxis** framework (tutorials / how-to / reference / explanation), Microsoft's System.Text.Json docs (the audience's home turf — mirror their structure), and Serde's guide. Output: a one-page documentation plan mapping the existing 18 `docs/*.md` files into the chosen structure.

### Build

DocFX (or similar, per research outcome) published to GitHub Pages:

- **API reference** — generated from XML doc comments. Prerequisite: XML-doc coverage pass over the entire public surface, with `<example>` blocks on the core facade methods; CS1591 enforced for the public API.
- **Conceptual docs** — existing `docs/*.md` reorganised per Diátaxis: getting-started tutorial; task-oriented how-tos (converters, naming, enums, polymorphism, …); reference (options, attributes, exceptions); explanation (`docs/internals/`).
- **Landing surfaces** — README rewritten as the shop window (install, 30-second example, feature table vs STJ, links into the site); NuGet package description aligned.
- Code samples pulled from the companion examples repo (G5) where possible, so tutorial code is compiled code.

**Exit criteria:** site published via GitHub Pages CI; 100% XML-doc coverage on public API; every public feature has a guide page; README rewritten.

## G5 — Companion examples repo & benchmarks

**Repo:** `huml-dotnet-examples` (proposed name; sibling repo under the same account).

- **`examples/`** — one runnable console project per feature area (~12–15: getting started, options, naming policies, enums, converters, polymorphism, constructor binding, extension data, populate, source generation, AOT publish, error handling, versioning). Each contains assertions and doubles as an end-to-end test. A CI job builds and runs all of them against the **published beta package** (not a project reference) — this also validates NuGet packaging itself.
- **`benchmarks/`** — BenchmarkDotNet suite: Huml.Net vs System.Text.Json on equivalent payloads (small config, medium document, large collection-heavy graph, deep nesting), each dataset authored in both HUML and JSON. Measures serialise, deserialise, and parse-only paths plus allocations; reflection and source-generated modes both benchmarked. Results published as a docs page with honest commentary — HUML is human-oriented and will lose some races; saying so credibly beats silence.
- **`datasets/`** — shared HUML/JSON payload pairs, reused by examples, benchmarks, and the G3 fuzz seed corpus.

**Exit criteria:** all examples green in CI against the release candidate; benchmark results page published into the G4 docs site.

---

## Release gate

Beta ships when **all** of the following hold:

1. G1 compliance report clean; suite 100% green.
2. G2 dispositions complete; all "for-beta" items merged.
3. G3 zero fuzz crashes/hangs; criticals/highs fixed; API frozen.
4. G4 docs site live.
5. G5 examples green in CI against the release candidate; benchmark page published.
6. CHANGELOG rolled from `## [Unreleased]` to `## [0.2.0-beta.1]`; fresh `[Unreleased]` section inserted.
7. Tagged `v0.2.0-beta.1` and pushed to NuGet.org.

## Post-beta stretch goals (recorded, not gating)

- Cross-language benchmark harness vs go-huml and Rust HUML implementations (needs a multi-toolchain or Docker harness).
- Scheduled CI fuzz job (continuous fuzzing post-beta).
- All 999.x items dispositioned as "deferred" in G2.
- Upstreaming `fixtures/extensions/` contributions to `huml-lang/tests` and removing local duplicates once merged.
