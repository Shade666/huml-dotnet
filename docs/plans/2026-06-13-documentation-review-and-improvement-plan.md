# Documentation Review & Improvement Plan — Huml.Net

**Date:** 2026-06-13
**Author:** Documentation review pass (branch `claude/docs-review-improvement-wwlw9j`)
**Scope:** All Markdown documentation in `primeBeri/huml-dotnet`, the DocFX site, and the
companion `huml-dotnet-examples` repo (see access note in §1).
**Status:** Review complete — this document is the proposed implementation plan, not yet executed.

---

## 1. Scope & method

Reviewed in full, cross-checked against the real public surface (`docs/public-api.txt` and
`src/Huml.Net/**`):

- **Root governance/meta docs** — `README.md`, `CONTRIBUTING.md`, `CODE_OF_CONDUCT.md`,
  `SECURITY.md`, `SUPPORT.md`, `AGENTS.md`, `CLAUDE.md`, `BACKLOG.md`, `CHANGELOG.md`,
  `.github/**` (PR + issue templates, `CODEOWNERS`, `dependabot.yml`).
- **DocFX site** — `docfx.json`, `index.md`, `toc.yml`, `docs/toc.yml`, and all 17 conceptual
  pages (tutorial / how-to / reference / explanation).
- **Internal & working docs** — `docs/internals/**`, `docs/plans/**`,
  `docs/spec-compliance-report.md`, `docs/nuget-publish-checklist.md`, `docs/documentation-plan.md`,
  `contrib/**`.

> **"docx"** in the original request maps to **DocFX** — there are no Word/`.docx` files in
> the repo; the documentation site is a DocFX build (`docfx.json` → `_site` → GitHub Pages).

### Access note — companion examples repo

The third pillar, the **`huml-dotnet-examples`** repo (note: plural *examples*; the request said
"example" singular), is **not reachable from this session**. The GitHub MCP is scoped to
`primeberi/huml-dotnet` only, and the remote `add_repo`/`list_repos` tools are not connected, so
its docs could not be read directly. The examples-repo section of this plan (§7) is therefore
derived from the **G5 specification** in `docs/plans/2026-06-10-beta-release-goals.md` and the
backlog tasks that reference it, and is marked **NEEDS ACCESS TO VERIFY**. To complete that
portion, either add `primeBeri/huml-dotnet-examples` to the session scope or run this review
inside that repo.

### Headline finding

`0.2.0-beta.1` shipped **today** (CHANGELOG dated 2026-06-13) and carried one intentional
breaking change: **the static facade `Huml` was renamed to `HumlSerializer`** (commit `f343d1e`).
Several docs were **not** updated alongside that rename and still reference the non-existent
`Huml.Serialize/Deserialize/Parse` API and a `Huml.cs` file that no longer exists. This stale
facade name is the single most pervasive inaccuracy in the docs.

---

## 2. Out-of-date content (accuracy defects)

Priority key: **P0** = factually wrong / won't compile, fix before anything else · **P1** =
currency drift · **P2** = polish.

| # | File / location | Defect | Correction | Pri |
|---|-----------------|--------|------------|-----|
| A1 | `CLAUDE.md` → "Public API Surface" | Names `src/Huml.Net/Huml.cs` and `Huml.Serialize<T>` / `Huml.Deserialize<T>` / `Huml.Parse`. File and class do **not** exist post-rename. | `src/Huml.Net/HumlSerializer.cs`; `HumlSerializer.Serialize/Deserialize/Parse`; add `Populate`. | P0 |
| A2 | `CLAUDE.md` → pipeline diagram & AST table | Lists `HumlSerializer`/`HumlDeserializer` as *internal* pipeline classes (now collides with the public facade; the internal serialiser is `HumlSerializerImpl`). AST table omits `HumlInlineMapping`, `HumlDocument.DetectedVersion`, and `Line`/`Column` on `HumlNode`; still claims inline dicts produce `HumlDocument` (they now produce `HumlInlineMapping`). | Rename internal refs to `HumlSerializerImpl`/`HumlDeserializer` (internal); add `HumlInlineMapping`; note inline → `HumlInlineMapping`; add source-position + `DetectedVersion` properties. | P0 |
| A3 | `CONTRIBUTING.md` → testing patterns (≈ L77, L81–82) | `Huml.Parse(input, HumlOptions.Default)` in the positive/negative assertion snippets. | `HumlSerializer.Parse(...)`. | P0 |
| A4 | `BACKLOG.md` (multiple rows) | Prose references `Huml.Parse()` / `Huml.Serialize` / `Huml.Deserialize`; multiple `[…](.planning/CODEBASE-REVIEW.md)` links that are **dead in the public repo** (`.planning/` is intentionally uncommitted per CLAUDE.md). | `HumlSerializer.*`; remove or repoint `.planning/` links. (See also §3 — BACKLOG.md may be retired.) | P0 |
| A5 | `docs/constructor-binding.md` (L2, L9) | Prose says "`HumlDeserializer` supports / selects a constructor…". No public `HumlDeserializer`. (Code samples are already correct: `HumlSerializer.Deserialize`.) | "`HumlSerializer.Deserialize`" or "Huml.Net". | P0 |
| A6 | `docs/attributes-reference.md` (L17) | `[HumlNamingPolicy(typeof(P))]` — wrong signature. The ctor takes a `HumlKnownNamingPolicy` **enum** (verified in `HumlNamingPolicyAttribute.cs`). | `[HumlNamingPolicy(HumlKnownNamingPolicy.KebabCase)]`. | P0 |
| A7 | `docs/options-reference.md` | (a) `NumberHandling` property (`HumlNumberHandling`, default `Strict`) is **missing** from the table. (b) `Converters` row lists type `IList<HumlConverter>`; actual is `IReadOnlyList<HumlConverter>`. | Add `NumberHandling` row; fix `Converters` type. | P0 |
| A8 | `docs/custom-converters.md` (≈ L59–62) | Implies `HumlOptions.Converters` is `IList`; it is init-only `IReadOnlyList`. Example assigns a fresh `List<>` (fine) but the type label is inconsistent with reality. | Note the property is `IReadOnlyList`; assign via initialiser. | P1 |
| A9 | `docs/aot-trimming.md` (L52–57) | "Future: Source-Generator Path" describes the generator as **not yet available** and tells readers to remove pragmas "once it ships". It has **shipped** — `HumlGeneratedContext`, `[HumlSerializable]`, `IHumlTypeInfoResolver` are public, and `docs/source-generator.md` documents it as live. The two pages **contradict** each other. | Rewrite as present-tense; cross-link `source-generator.md`. | P0 |
| A10 | `docs/enum-serialisation.md` | Omits that `[Flags]` combinations and undefined numeric enum values throw `HumlSerializeException` (`EnumNameCache.cs`). | Add a "Limitations" note. | P1 |
| A11 | `SECURITY.md`, `SUPPORT.md` | Both say "During the alpha phase (0.x)"; project is at **beta** (`0.2.0-beta.1`). | "beta phase". | P1 |
| A12 | `SUPPORT.md` (L18) | Routes HUML-language questions to `huml-lang/tests` (a *fixture* repo, not a discussion forum). | Point to the HUML spec/discussions; reserve `huml-lang/tests` for fixture issues. | P1 |
| A13 | `.github/ISSUE_TEMPLATE/bug_report.yml` (L16) | Version placeholder `e.g. 0.1.0-alpha.1`. | `e.g. 0.2.0-beta.1`. | P1 |
| A14 | `contrib/notepad-plus-plus/README.md` (L3, L97, L106) | Links to `github.com/Shade666/huml-dotnet` and `"author": "Shade666"` — repo-identity drift vs `primeBeri` everywhere else. | Reconcile to the canonical owner (`primeBeri`) or confirm `Shade666` is the intended UDL author. | P1 |
| A15 | `docs/spec-compliance-report.md` | Several "Deferred divergences" (e.g. L2 `key::1`, L4 `[ ]`, S1 quoted inline keys) were **fixed in G2.2** (CHANGELOG) but are still listed as open. Test-count snapshot (`1216/1216`) and §4 "inputs to later goals" (G2/G3) are stale. | Update the divergence tables; trim audit-process framing (see §3 + §4 publish decision). | P1 |

**Note — `[HumlPolymorphic]` default discriminator:** verified **correct** in
`attributes-reference.md` — the ctor default really is `_type` (`HumlPolymorphicAttribute.cs`).

---

## 3. Redundant / archivable content

The **G1–G5 beta-release programme is complete** (beta shipped today). Its dated working
artifacts are now historical and form a natural archive batch. Recommendation: introduce a
`docs/archive/` folder (excluded from DocFX) and move completed point-in-time artifacts there —
**keep for audit trail, don't masquerade as live guidance.**

| File | Recommendation | Reason |
|------|---------------|--------|
| `docs/internals/api-freeze.md` | **ARCHIVE** | Freeze window closed (beta shipped); self-flags a broken verify step + defers durable mechanism to "post-beta" (= now). |
| `docs/internals/g3-security-review.md` | **ARCHIVE** | Completed point-in-time review report; extract still-open DEFERRED items (M4/M5/M8/M10/M15) to the backlog. |
| `docs/internals/g3-review-raw.json` | **ARCHIVE** | 105 KB raw machine dump backing the above; no living value. |
| `docs/internals/threat-model.md` | **KEEP (refresh)** *or* ARCHIVE | A threat model is legitimately living, but this one is framed as the one-time G3.2 brief and its "AUDIT" notes are now resolved. Decide: decouple + refresh, or archive with the review. |
| `docs/documentation-plan.md` | **ARCHIVE** | Executed plan — the site it designs now exists (`docfx.json` + `toc.yml`). |
| `docs/plans/2026-06-10-backlog-disposition.md` | **ARCHIVE (in place)** | Executed G2.1 disposition; superseded by the Backlog.md MCP task system. |
| `docs/plans/2026-06-10-beta-release-goals.md` | **KEEP (in place)** | Still the reference for the *as-yet-unbuilt* G5 examples repo; archive once G5 lands. |
| `contrib/FIXTURE-MERGE-INSTRUCTIONS.md` | **KEEP → DELETE after upstream PR** | Open one-off: `fixtures/extensions/**` are still present, so the upstream `huml-lang/tests` PR has **not** merged (the fixture-gaps rule removes them post-merge). |
| `contrib/PR-SUMMARY.md` | **MERGE into above → DELETE after PR** | ~80 % redundant with the merge-instructions (same 30 fixtures). |
| `AGENTS.md` | **REPLACE CONTENT** | Currently *only* the auto-generated Backlog-MCP block — byte-identical to a block in `BACKLOG.md` and `CLAUDE.md` (triplicated). Has zero agent-specific guidance. |
| `BACKLOG.md` | **RETIRE / SLIM** | Hand-maintained backlog table that risks drifting from the MCP-tracked `backlog/tasks/*.md` (the real task system). Leaks internal "999.x" numbering to a public-facing doc and links to uncommitted `.planning/`. Decide single source of truth → point readers at `backlog/` (or GitHub issues). |

**Triplicated Backlog-MCP block:** the same guidance block appears in `AGENTS.md`, `BACKLOG.md`,
and `CLAUDE.md`. Keep it in **one** canonical place (CLAUDE.md already has it) and have the
others link.

---

## 4. Gap analysis (missing documentation)

### 4a. Missing how-to pages (public API supports the feature; no page exists)

The public surface (`public-api.txt`) ships features with **no dedicated how-to** and, in two
cases, **no `toc.yml` entry at all**:

| Topic | Public surface | Today | Action |
|-------|----------------|-------|--------|
| **Polymorphism** | `[HumlPolymorphic]`, `[HumlDerivedType]`, `HumlUnknownDerivedTypeHandling` (`Throw`/`FallBackToBaseType`), `TypeDiscriminatorPropertyName` | Mentioned only in the attributes table; **not in toc** | **NEW how-to** "Serialize polymorphic types"; add to toc How-to. STJ parallel: "How to serialize polymorphic types". |
| **Number handling** | `[HumlNumberHandling]`, `HumlOptions.NumberHandling`, `HumlNumberHandling` (`Strict`/`AllowReadingFromString`/`WriteAsString`) | Undocumented; absent from options table | **NEW how-to** "Read & write numbers as strings"; add the options row (see A7). |
| **Ignore / omit values** | `[HumlIgnore]`, `[HumlIgnoreDefaults]`, `HumlIgnoreCondition`, `HumlOptions.DefaultIgnoreCondition` | Only in tables | **NEW how-to** "Ignore properties & omit default/null values" (mirrors STJ "How to ignore properties"). |
| **Serialization callbacks** | `HumlTypeInfo.OnSerializing/OnSerialized/OnDeserializing/OnDeserialized` | Entirely undocumented | **NEW** short how-to or an Explanation note. |
| **Unmapped members / strictness** | `UnmappedMemberHandling`, `HumlOptions.Strict` | In options/error-handling only | Optional focused how-to "Reject unknown keys"; otherwise cross-link. |

### 4b. Governance/meta gaps

- **`CODE_OF_CONDUCT.md`** — claims Contributor Covenant v2.1 but omits the standard
  **Enforcement Guidelines** (consequence ladder) and **Scope** sections. Restore them.
- **`CONTRIBUTING.md`** — no cross-links to `CODE_OF_CONDUCT.md` / `SECURITY.md` / `SUPPORT.md`;
  no DCO/sign-off statement despite the "AI-assisted contributions" responsibility clause.
- **`AGENTS.md`** — needs real content (build/test commands, the `HumlSerializer` facade pointer,
  code standards) rather than only the Backlog block.
- **Issue templates** — `bug_report.yml` would benefit from a **spec-version dropdown (v0.1/v0.2)**
  (a core triage axis); `feature_request.yml` from a "willing to contribute a PR?" prompt.
- **Cross-links to the examples repo** — none of the user-facing docs (README "Documentation",
  SUPPORT.md) point readers at `huml-dotnet-examples` for runnable samples.

### 4c. DocFX publish discrepancy (decision needed)

`documentation-plan.md` §2 lists `spec-compliance-report.md` on the **public** site under
Explanation ("Spec compliance & divergences"), but `docfx.json` (L29) **excludes** it. Resolve:
either (a) clean it into a living "Spec compliance & divergences" page and publish, or
(b) keep it internal. The living *user* value is the small set of intentional divergences
(CRLF/L1, BOM, `\/` escape) — those deserve a public page regardless.

---

## 5. Best-practice improvements (against Diátaxis + technical-writing norms)

1. **Consistent "Next steps" / "See also" footers.** Present on getting-started, source-generator,
   attributes/options; **absent** on inline-serialisation, enum-serialisation, custom-converters,
   date-time. Add a short cross-link footer to every how-to.
2. **Use `<xref:…>` for API names.** Only `index.md`, getting-started, source-generator, and
   attributes-reference use DocFX xrefs; reference/explanation pages use plain prose. Linking API
   mentions to the generated reference improves navigation.
3. **Verify sidebar links resolve.** `docs/toc.yml` surfaces `internals/pipeline.md`,
   `version-gates.md`, `extending.md` under Explanation — confirm none are in the `docfx.json`
   exclude set (they currently aren't, but re-check after any archival move so the sidebar doesn't 404).
4. **Diátaxis mode-mixing (minor).** `error-handling.md` and `date-time.md` are filed as how-tos
   but are ~70 % reference tables / changelog notes. Acceptable, but consider splitting the
   reference content out or re-labelling.
5. **`dependabot.yml`** — add a `groups:` directive to batch minor/patch bumps and cut PR noise.
6. **PR template** — add an "AI-assisted change was human-reviewed" attestation checkbox to match
   the CONTRIBUTING policy.
7. **Owner casing (cosmetic).** Brand is `primeBeri`; README GitHub-Pages URLs use lowercase
   `primeberi.github.io` (correct — Pages hostnames are lowercased). Leave the URLs; just be aware
   it's intentional, not a bug.

---

## 6. Phased implementation plan

Each phase is independently shippable. Suggested order front-loads correctness.

### Phase 0 — Critical accuracy (P0) — *~half a day*
Fix everything that is factually wrong or won't compile. Items **A1, A2, A3, A4, A5, A6, A7, A9**.
This is mostly the `Huml.` → `HumlSerializer.` sweep plus the four doc-specific corrections
(attributes signature, options `NumberHandling`/`Converters`, AOT↔source-gen contradiction).
*Verification:* `grep -rn "Huml\.\(Parse\|Serialize\|Deserialize\|Populate\)" docs CLAUDE.md CONTRIBUTING.md BACKLOG.md` returns nothing; `dotnet build` of any doc code sample compiles.

### Phase 1 — Currency drift (P1) — *~1–2 hours*
Items **A8, A10, A11, A12, A13, A14, A15**. Alpha→beta wording, version placeholders, enum
limitations note, `Shade666`/`primeBeri` reconciliation, spec-compliance divergence update.

### Phase 2 — Redundancy & archival — *~half a day*
- Create `docs/archive/` (add to `docfx.json` exclude list).
- Move: `api-freeze.md`, `g3-security-review.md`, `g3-review-raw.json`, `documentation-plan.md`
  (and decide on `threat-model.md`).
- Extract still-open G3 DEFERRED findings into `backlog/tasks/` (confirm task-005…008 already
  cover them — they appear to).
- De-triplicate the Backlog-MCP block (canonical = CLAUDE.md; AGENTS.md gets real content per
  Phase 3; BACKLOG.md retired or slimmed).
- Resolve the `spec-compliance-report.md` publish discrepancy (§4c).

### Phase 3 — Fill gaps — *~1–2 days*
- New how-tos: **polymorphism**, **number handling**, **ignore/omit defaults** (+ optional
  callbacks, unmapped-members). Add each to `docs/toc.yml` (How-to) and add "See also" footers.
- `CODE_OF_CONDUCT.md` enforcement ladder + scope.
- `CONTRIBUTING.md` cross-links + DCO line.
- `AGENTS.md` real content.
- Issue-template spec-version dropdown + PR-willingness prompt.
- Add examples-repo cross-links to README "Documentation" and SUPPORT.md.

### Phase 4 — Best-practice polish — *~half a day*
"See also" footers + xref pass across remaining pages; `dependabot.yml` groups; PR-template
attestation; sidebar link re-verification; DocFX rebuild + eyeball the rendered site.

### Phase 5 — Companion examples repo (`huml-dotnet-examples`) — *NEEDS ACCESS*
See §7. Blocked on session access to the repo.

---

## 7. Companion examples repo — `huml-dotnet-examples` (NEEDS ACCESS TO VERIFY)

Per the G5 spec, the repo should contain `examples/` (12–15 runnable console projects),
`benchmarks/` (BenchmarkDotNet vs System.Text.Json), and `datasets/` (shared HUML/JSON pairs).
Proposed documentation deliverables, to be validated once the repo is reachable:

1. **Top-level `README.md`** — what the repo is, how to run all examples against the **published**
   package, the relationship to the main library and docs site, and a per-example index table.
2. **Per-example `README.md`** (or header comment) — each project states the feature it
   demonstrates, the expected output, and links back to the matching how-to on the docs site.
   This is what lets G4 "pull tutorial code from compiled examples".
3. **`benchmarks/RESULTS.md`** — published results with honest commentary (referenced by
   `backlog/milestones/m-2` and task-012/017 — confirm it exists and is current). Wire it into the
   G4 docs site as a "Benchmarks" page (the beta-goals doc says benchmark results publish into the
   docs site).
4. **`datasets/README.md`** — describes the shared payload pairs and that they double as the G3
   fuzz seed corpus.
5. **CI doc** — note that examples run against the published NuGet package by default
   (task-002, done) so the README's run instructions match CI reality.

**To proceed:** add `primeBeri/huml-dotnet-examples` to the session scope (or run a review pass
inside it). Until then, treat §7 as a checklist, not findings.

---

## 8. Quick-reference: file-by-file disposition

| File | Verdict | Phase |
|------|---------|-------|
| `README.md` | Accurate & current (uses `HumlSerializer`, lists polymorphism/source-gen). Add examples-repo link. | 3 |
| `CLAUDE.md` | **Stale facade section (A1/A2)** + de-triplicate block | 0, 2 |
| `CONTRIBUTING.md` | Facade fix (A3) + cross-links/DCO | 0, 3 |
| `CODE_OF_CONDUCT.md` | Add enforcement ladder + scope | 3 |
| `SECURITY.md` / `SUPPORT.md` | alpha→beta; SUPPORT routing fix | 1 |
| `AGENTS.md` | Replace with real content | 2/3 |
| `BACKLOG.md` | Retire/slim; facade + `.planning` link fixes | 0, 2 |
| `CHANGELOG.md` | Current & correct | — |
| `.github/**` | Version placeholder, spec-version dropdown, PR attestation, dependabot groups | 1, 3, 4 |
| `index.md`, `toc.yml`, `docs/toc.yml` | Current; add new how-to entries | 3 |
| `docs/getting-started.md` | Clean | — |
| `docs/naming-policy.md`, `populate.md`, `extension-data.md`, `required-properties.md`, `versioning.md`, `ast-usage.md`, `source-generator.md` | Accurate | 4 (footers/xref) |
| `docs/constructor-binding.md` | Prose `HumlDeserializer` fix (A5) | 0 |
| `docs/attributes-reference.md` | `[HumlNamingPolicy]` signature (A6) | 0 |
| `docs/options-reference.md` | Add `NumberHandling`; fix `Converters` type (A7) | 0 |
| `docs/custom-converters.md` | `Converters` type note (A8) + footer | 1, 4 |
| `docs/aot-trimming.md` | Source-gen contradiction (A9) | 0 |
| `docs/enum-serialisation.md` | Flags/undefined-value note (A10) + footer | 1, 4 |
| `docs/inline-serialisation.md`, `date-time.md`, `error-handling.md` | Add footers; minor Diátaxis | 4 |
| `docs/spec-compliance-report.md` | Update divergences; publish decision (A15, §4c) | 1, 2 |
| `docs/internals/pipeline.md`, `version-gates.md`, `extending.md` | KEEP (current, on-site) | — |
| `docs/internals/api-freeze.md`, `g3-security-review.md`, `g3-review-raw.json`, `threat-model.md` | ARCHIVE | 2 |
| `docs/documentation-plan.md`, `docs/plans/2026-06-10-backlog-disposition.md` | ARCHIVE | 2 |
| `docs/nuget-publish-checklist.md` | KEEP (living checklist) | — |
| `contrib/FIXTURE-MERGE-INSTRUCTIONS.md`, `PR-SUMMARY.md` | KEEP→DELETE after upstream PR; consolidate | 2 |
| `contrib/notepad-plus-plus/README.md` | Reconcile `Shade666`/`primeBeri` (A14) | 1 |
| `huml-dotnet-examples/**` | NEEDS ACCESS | 5 |

---

*Generated by a documentation review pass. No source or doc content was modified by the review
itself — this plan is the deliverable.*
