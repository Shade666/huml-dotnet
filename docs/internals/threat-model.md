# Threat Model — Huml.Net

**Scope:** `Huml.Parse`, `Huml.Deserialize<T>`, `Huml.Populate<T>` consuming **untrusted input** (config files from disk, documents over the network, user uploads), plus `Huml.Serialize<T>` consuming untrusted *object graphs*, and the `Huml.Net.SourceGeneration` analyzer consuming user source code at build time.
**Written:** 2026-06-11 (G3.1 of the beta release programme). Directs the G3.2 adversarial review; update when the pipeline changes.

## Trust boundaries and assets

| Boundary | Untrusted input | Asset at risk |
|----------|-----------------|---------------|
| `Huml.Parse(string/Span)` | Document text | Process availability (CPU, memory, stack); exception contract |
| `Huml.Deserialize<T>` | Document text + target type shape | As above, plus type-safety of materialised objects |
| `Huml.Populate<T>` | Document text + existing instance | As above, plus integrity of the caller's object |
| `Huml.Serialize<T>` | Object graph (cycles, hostile converters, exotic types) | Process availability; output integrity |
| Source generator | User C# source | Build-process availability; generated-code correctness |

**Security promise to consumers:** for any input string, the parse/deserialise entry points either succeed or throw `HumlParseException`/`HumlDeserializeException`/`HumlUnsupportedVersionException`. They never crash the process, never hang unboundedly, never consume memory disproportionate to input size, and never throw undeclared exception types. (One escape already found and fixed in G1.3: `FormatException` from digitless base prefixes — the class of bug is proven present historically; the review must hunt for siblings.)

## T1 — Stack exhaustion (recursion)

`HumlParser` is recursive-descent; `HumlDeserializer`/`HumlSerializer` recurse over nested objects.

- **Mitigation:** `MaxRecursionDepth` (default 64, max 1024) guards parser depth. `StackOverflowException` is uncatchable in .NET — the guard is the *only* defence.
- **Review focus:** every recursive call site must be covered by the depth guard — parser (`ParseMappingEntries`/`ParseVector`/`ParseMultilineList`, inline forms), deserialiser re-dispatch (polymorphic `DeserializeMappingEntries` recursion strips entries and recurses — is *that* depth-counted?), serialiser object-graph recursion (cyclic graphs: `[ThreadStatic]` converter re-entry guard exists, but plain cyclic POCOs without converters?), and `HumlNode` record `Equals`/`GetHashCode` on deep ASTs.

## T2 — Allocation bombs (memory amplification)

Inputs engineered so a small document allocates disproportionate memory.

- **Current posture:** span-based lexer avoids input copies; StringBuilder pooling on serialise; allocation-budget tests exist (`LexerAllocationTests`, `HumlSerializerAllocationTests`).
- **Attack shapes to verify:** multiline strings declaring huge content via repetition; `EnumNameCache`/`PropertyDescriptor`/`ConverterCache` poisoning via deserialising to attacker-influenced types (caches are unbounded by design — only a concern if target types are attacker-chosen, document this assumption); `HumlExtensionData` capturing unbounded unmatched keys; token `Value` string materialisation per scalar (linear, acceptable — confirm no quadratic concatenation).
- **Review focus:** any `StringBuilder` growth in loops keyed to input; `sb.Append` of slices inside multiline scanning (linear? yes — confirm); `List<HumlNode>` pre-sizing from attacker-controlled counts.

## T3 — Quadratic/exponential time (CPU exhaustion)

- **Attack shapes:** deeply nested inline structures re-scanned per level; backtracking in root-type inference (`InferRootType` uses lookahead — bounded?); duplicate-key detection per block (`seenKeys` HashSet — O(1) per key, fine); naming-policy transforms on long keys; indent measurement re-walking blank-line runs (the 500-blank-line test exists — verify O(n) overall).
- **Review focus:** any nested loop where both bounds derive from input length; `ScanQuotedStringContent` two-pass approach (escape detection then build — 2n, fine); parser lookahead/pending-token interplay (`_pending`, `_lookahead`) for re-lexing the same span more than a constant number of times.

## T4 — Malformed Unicode and invisible characters

- **Current posture:** non-Latin bare keys rejected with a targeted error; bidi controls and zero-width characters are *content* inside quoted strings (extension fixtures cover this); a leading U+FEFF BOM is rejected as "Unexpected character" (AUDIT: decide policy — see disposition doc).
- **Attack shapes:** lone surrogates in input strings (UTF-16 `string` can hold them — do they round-trip or corrupt?); bidi controls in *keys* spoofing reviewed config; `char`-based scanning splitting astral-plane codepoints (span indexing is per-`char` — verify no `char` is interpreted as a full codepoint where it matters).
- **Review focus:** every `_source[_pos]` comparison against ASCII constants is safe per-`char`; string materialisation paths never slice between surrogate halves (slices always run to a structural ASCII delimiter — confirm).

## T5 — Numeric edge cases

- **Known issue (AUDIT, this goal):** hex/octal/binary 64-bit values wrap via two's complement (`0xFFFFFFFFFFFFFFFF` → `-1`) while decimal overflow throws — inconsistent, silent data change.
- **Review focus:** `double.Parse` round-trips for extreme exponents (`1e309` → `Infinity`? — what `ScalarKind` results, and does it serialise back losslessly?); `-0.0` handling; underscore-only digit runs (`1__0`); `Convert.ToInt64` radix paths after the G2.2 digit-count guard.

## T6 — Deserialisation-specific threats

- **Type-shape attacks:** polymorphic dispatch is allow-list only (`[HumlDerivedType]` registrations — no arbitrary-type instantiation; verify the discriminator path cannot reach `Activator.CreateInstance` with an unregistered type). Constructor binding invokes user constructors — exceptions from them must surface as `HumlDeserializeException`, not escape raw.
- **`Populate` integrity:** a parse error mid-populate must not leave the caller's object half-mutated without documentation of that contract.
- **Converter trust:** user converters run arbitrary code by design (documented trust assumption); the `[ThreadStatic]` re-entry guard must also reset on exception paths (try/finally?) or one thrown converter poisons the thread.

## T7 — Serialiser availability

- Cyclic object graphs (depth guard or cycle detection?); properties whose getters throw; `IEnumerable` implementations that never terminate (documented trust assumption — caller owns the graph); culture-sensitive formatting (must be invariant — verify all `ToString`/`Parse` paths pass `CultureInfo.InvariantCulture`).

## T8 — Source generator (build-time)

- Malformed/adversarial user source must produce diagnostics, not generator crashes (a generator exception degrades the user's build). Generated code must not introduce injection points (string literals from user type/property names must be escaped when embedded in generated source).

## Out of scope / accepted

- XML-style entity expansion, YAML-style anchors/aliases, arbitrary type-name deserialisation gadgets: **structurally impossible** in HUML (no references, no type names in documents outside the polymorphic allow-list).
- Hostile `HumlOptions`/converters/resolvers: the options object is caller-owned code; trusting it is the API contract.
- Timing side-channels: not meaningful for a markup parser.

## Verification map

| Threat | Verified by |
|--------|-------------|
| T1–T5 | G3.3 fuzzing harness (crash/hang/exception-contract oracle) + pathological-input budget tests |
| T1–T8 | G3.2 adversarial review (this document is the reviewers' brief) |
| T5, T6 | Property-based round-trip tests (`Deserialize(Serialize(x)) == x`) |
