# Documentation Plan — Huml.Net beta docs

> **Status — executed / historical.** This plan (G4.1) has been implemented: the DocFX site,
> `docfx.json`, `toc.yml`, and the GitHub Pages workflow all now exist and embody these decisions.
> It is retained for rationale and is excluded from the published site. The live structure is
> defined by `docfx.json` + `docs/toc.yml`, not by this document.

**Date:** 2026-06-11 (G4.1 of the beta release programme).
**Purpose:** decide the structure of the beta documentation site before writing it, grounded in
the best-in-class examples the goal calls out.

## 1. Best-practice research (time-boxed)

| Source | What we take from it |
|--------|----------------------|
| **[Diátaxis](https://diataxis.fr/)** | The organising spine. Four documentation modes on two axes (practical↔theoretical, study↔work): **Tutorials** (learning-oriented, hand-held first success), **How-to guides** (task-oriented, "how do I X?"), **Reference** (information-oriented, dry and complete), **Explanation** (understanding-oriented, the "why"). The core rule: never mix modes on one page — a reference page that tries to teach, or a tutorial that tries to be exhaustive, fails at both. |
| **Go / pkg.go.dev** | Reference and runnable examples live *next to* the API. We mirror this by generating API reference from XML doc comments and putting `<example>` blocks on the core facade so the example shows up inline in the reference. |
| **Microsoft System.Text.Json docs** | The audience's home turf. STJ uses a "How to serialize/deserialize" + "How to customize" + per-feature how-to structure under a conceptual overview, with a separate API reference. Migrating developers should feel the same shape — so our how-to titles echo theirs ("How to ignore properties", "How to customize names"). |
| **Serde (Rust)** | The strongest serialisation-library guide: a short conceptual data-model explanation, then task pages, then derive/attribute reference. Confirms the explanation→how-to→reference flow for a serializer specifically. |
| **DocFX 2.77** | The generator. Builds API reference from .NET assemblies + XML comments, ingests markdown conceptual docs, organises both via `toc.yml`, emits a static `_site` for GitHub Pages. Chosen because it is the native .NET tool — zero impedance with our XML-doc source of truth. |

**Decision:** organise the site by Diátaxis quadrant; generate the reference from XML docs with
DocFX; keep how-to titles aligned with STJ where a direct parallel exists.

## 2. Target structure

```
docs site (DocFX)
├── Home (landing — mirrors README shop window)
├── Tutorial          [learning]      → getting-started.md  (NEW — the only true tutorial)
├── Guides (how-to)   [task]          → the existing feature docs, re-headed as tasks
│     ├── Serialize & deserialize
│     ├── Customize property names      (naming-policy)
│     ├── Ignore & default values       (from required-properties / options)
│     ├── Control inline vs multiline    (inline-serialisation)
│     ├── Work with enums                (enum-serialisation)
│     ├── Bind constructors & records    (constructor-binding)
│     ├── Require properties             (required-properties)
│     ├── Capture unknown keys           (extension-data)
│     ├── Overlay onto an instance       (populate)
│     ├── Write a custom converter       (custom-converters)
│     ├── Serialize dates & times        (date-time)
│     ├── Handle errors                  (error-handling)
│     ├── Publish AOT / trimmed          (aot-trimming)
│     └── Use the source generator       (NEW — short, from HumlGeneratedContext)
├── Reference         [information]   → generated API reference (DocFX) + curated tables
│     ├── API reference (generated from XML docs)
│     ├── Options reference              (options-reference — already a table)
│     └── Attributes reference           (NEW — one table of every [Huml*] attribute)
└── Explanation       [understanding] → the "why" pages
      ├── Versioning model               (versioning)
      ├── The pipeline                   (internals/pipeline)
      ├── Version gates                  (internals/version-gates)
      ├── Working with the AST           (ast-usage — advanced/explanatory)
      ├── Extending the pipeline         (internals/extending)
      └── Spec compliance & divergences  (spec-compliance-report)
```

**Not on the public site** (project-internal, stay in `docs/internals/` but excluded from `toc.yml`):
`threat-model.md`, `g3-security-review.md`, `g3-review-raw.json`, `api-freeze.md`,
`nuget-publish-checklist.md`, `public-api.txt`, and everything under `docs/plans/`.

## 3. Mapping the 13 existing docs

Every existing conceptual doc already maps onto a quadrant (table above) — **no content is
discarded**. The reorganisation is: (a) add a `toc.yml` that groups them by quadrant; (b) write
the two genuinely missing pieces — the **getting-started tutorial** and the **source-generator
how-to** — plus the **attributes reference table**; (c) re-head a few how-to pages so their title
states the task ("How to…") rather than the feature name.

## 4. Build & deploy

- `docfx.json` at repo root: `metadata` section points at `src/Huml.Net/Huml.Net.csproj` (net10.0
  TFM) to generate API reference from XML docs; `build` section includes `docs/**/*.md` (minus the
  internal set) and the generated `api/`. Base URL `/huml-dotnet/` for the project page.
- GitHub Actions workflow `docs.yml`: build job (`docfx build`) → `upload-pages-artifact`; deploy
  job (`deploy-pages`) gated to the `github-pages` environment. Triggered on push to `main`,
  path-filtered to `docs/**`, `src/**/*.cs`, `docfx.json`.
- Published at `https://primeberi.github.io/huml-dotnet/`.

## 5. Prerequisites tracked

- **G4.2 (XML-doc coverage)** must complete first — the reference quality is only as good as the
  XML docs. CS1591 enforced on the public API; `<example>` blocks on the `Huml` facade.
- The published-site render must be eyeballed by the maintainer after first deploy (the CI run
  proves it builds and uploads; it cannot prove the rendered page looks right).
