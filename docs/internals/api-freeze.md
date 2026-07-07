# Public API Freeze — 0.2.0-beta.1

**Declared:** 2026-06-11 (G3.4 of the beta release programme).
**Amended:** 2026-06-11 (G5) — the freeze was deliberately re-opened once for a single
justified breaking change: the facade type `Huml.Net.Huml` was renamed to
`Huml.Net.HumlSerializer`. The original name collided with the root `Huml` namespace, so
`using Huml.Net; Huml.Deserialize(...)` did not compile for any consumer outside the
`Huml.Net.*` namespace tree — a defect the G5 examples-against-the-package work surfaced on the
first example. The rename is the only change since the original freeze; the baseline below
reflects it.

The public API surface of `Huml.Net` is **frozen** for the `0.2.0-beta.1` release as of this
date. The frozen surface is captured in the `Microsoft.CodeAnalysis.PublicApiAnalyzers`
baselines under `src/Huml.Net/PublicAPI/` (adopted 2026-07-07, TASK-003; they supersede the
hand-maintained `docs/public-api.txt`, which has been retired — see
[Automated enforcement](#automated-enforcement)).

## What "frozen" means

From now until `0.2.0-beta.1` ships, the public API must not change **except** for changes that:

1. Are **purely additive** (new types/members that do not alter existing signatures), **and**
2. Have an explicit, written justification in the commit message referencing this document.

Specifically prohibited without re-opening the freeze (a deliberate, documented decision):

- Removing or renaming any public type or member.
- Changing the signature, return type, or parameters of any existing public member.
- Changing the namespace of any public type.
- Tightening accessibility (e.g. `public` → `internal`).

The downstream goals build on this surface: G4 (documentation site, XML-doc coverage) and G5
(worked examples, benchmarks) are written against exactly these types. A late breaking change
forces re-work in both.

## Why the freeze is declared now

The freeze follows the G3 audit (threat model, adversarial review, fuzzing) — see
[`g3-security-review.md`](g3-security-review.md). That audit is the last phase that intentionally
changes behaviour and surface; closing it is the natural point to lock the API. The audit added
exactly one public member that did not previously exist (`HumlDeserializeException(string,
Exception)`, the inner-exception constructor needed to preserve thrown constructor/setter
exceptions) and changed no existing signatures — the surface is otherwise unchanged from
`0.2.0-alpha.4`.

## Automated enforcement

Since 2026-07-07 (TASK-003) the freeze is enforced at **build time** by
[`Microsoft.CodeAnalysis.PublicApiAnalyzers`](https://github.com/dotnet/roslyn/blob/main/src/RoslynAnalyzers/PublicApiAnalyzers/PublicApiAnalyzers.Help.md),
referenced `PrivateAssets="All"` in `src/Huml.Net.csproj` (analyzer-only; no runtime dependency).
With `TreatWarningsAsErrors` on, any drift fails `dotnet build` — locally and in CI, which runs
the same build; no extra CI wiring exists or is needed.

- **`RS0016`** — a public symbol exists that is not declared in the baseline (undeclared
  addition, or an accidental exposure).
- **`RS0017`** — a declared symbol no longer exists (removal/rename/signature change).

### Baseline files

```
src/Huml.Net/PublicAPI/
  netstandard2.1/PublicAPI.Shipped.txt + PublicAPI.Unshipped.txt
  net/           PublicAPI.Shipped.txt + PublicAPI.Unshipped.txt   (shared by net8.0/net9.0/net10.0)
```

The baselines are **per-TFM** because record `<Clone>$` methods use covariant returns on
net8.0+ (`-> HumlDocument!`) but return the base type on netstandard2.1, which lacks
covariant-return support; the surfaces are otherwise identical. `PublicAPI.Shipped.txt` holds
the published surface (seeded from `0.2.0-beta.2`, a strict additive superset of the frozen
`0.2.0-beta.1` surface). `PublicAPI.Unshipped.txt` holds additions made since the last release.

### Workflow for an additive change

1. Make the (justified, additive) public API change.
2. The build fails with `RS0016`. Add the new symbol lines to **`PublicAPI.Unshipped.txt`** in
   *both* TFM folders (or run `dotnet format analyzers src/Huml.Net/Huml.Net.csproj
   --diagnostics RS0016 --severity info` to apply the analyzer's code fix, then check both
   folders), and reference this document in the commit message.
3. At release time, move the `Unshipped` entries into `Shipped` (keeping each file's
   `#nullable enable` header and sorted order).

### Retirement of docs/public-api.txt

The hand-maintained reflection dump `docs/public-api.txt` (290 lines, `net10.0`-only, verified
by manual diffing) is **retired** in favour of the analyzer baselines: they cover every TFM,
capture nullability, and are enforced mechanically on every build rather than by convention.
The git history preserves the original file for archaeology.

## Excluded from the freeze

- Anything `internal` — the entire pipeline (`Lexer`, `HumlParser`, `HumlDeserializer`,
  `HumlSerializer`, the caches) is internal and may change freely.
- `Huml.Net.SourceGeneration` **generated output shape** — the generator is opt-in and its
  emitted code is an implementation detail; the *attributes* that drive it
  (`[HumlSerializable]`, etc.) are part of the frozen surface.
