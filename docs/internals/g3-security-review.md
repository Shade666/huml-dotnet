# G3.2 — Adversarial Security & Correctness Review

**Date:** 2026-06-11 (G3.2 of the beta release programme).
**Method:** multi-agent workflow — one adversarial finder per pipeline slice (lexer, parser, deserialiser, serialiser, source generator), briefed by [threat-model.md](threat-model.md); every finding then attacked by independent verifiers (default-refute), with a second reproduction-lens verifier for critical/high. Raw machine output: [g3-review-raw.json](g3-review-raw.json).
**Outcome:** 35 findings surfaced, **34 confirmed, 1 rejected**. 12 (criticals + highs) fixed with regression tests; mediums/lows triaged below.

## Coverage gaps (must close before the API freeze is final)

1. **Lexer slice did not complete.** The finder agent for `Lexer.cs` was terminated by an account spend limit before returning. The lexer was *partially* exercised indirectly — the 8 M-iteration fuzz campaign (G3.3) drives `Huml.Parse` through the lexer with zero crashes/hangs/contract escapes — but it received no dedicated adversarial read. **Re-run the lexer slice when budget resets.**
2. **Six findings reached the verification stage but their verifiers were cut off** (verdict `null` in the raw output): the parser quartet (long.MinValue, non-decimal wrap, deep-record StackOverflow, reference-equality contract) and the serialiser cyclic-POCO critical. These were **verified by hand** during fixing (each reproduced before the fix, regression-tested after) — see the per-finding notes below.

## Critical

| # | Finding | Status |
|---|---------|--------|
| C1 | **Cyclic POCO object graph → `StackOverflowException` (process crash).** `SerializeMappingBody` recurses with no cycle detection or depth cap; a self-referencing object graph crashes the host process uncatchably (T7/T1). | **FIXED** — serialiser now enforces `MaxRecursionDepth`, throwing `HumlSerializeException` before the stack is exhausted. Regression: `SerializerRecursionGuardTests`. |

## High

| # | Finding | Status |
|---|---------|--------|
| H1 | `long.MinValue` decimal literal (`-9223372036854775808`) rejected as overflow; the serialiser emits it, so `Serialize`/`Deserialize` is not a round trip for `long.MinValue`. | **FIXED** (`ParseInt` parses the signed literal whole). `AuditItemTests`/`SpecComplianceFixTests`. |
| H2 | User constructor that throws → `TargetInvocationException` escapes `Huml.Deserialize` raw (T6). | **FIXED** — `InvokeConstructor` unwraps and rethrows `HumlDeserializeException`. `DeserializerExceptionContractTests`. |
| H3 | Throwing parameterless constructor → `TargetInvocationException` escapes (only `MissingMethodException` was caught). | **FIXED** (same path). |
| H4 | `PropertyInfo.SetValue` propagates `TargetInvocationException`/`ArgumentException` raw from both Deserialize and Populate. | **FIXED** — setter invocation wrapped. |
| H5 | Empty POCO as a mapping-property value emits a dangling `key::` that fails to re-parse. | **FIXED** — empty mapping bodies emit `:: {}`. `SerializerEmptyValueTests`. |
| H6 | Polymorphic discriminator not emitted for derived types in nested/collection position → silent type loss on round-trip. | **FIXED** — discriminator emit moved into the shared per-object path. `PolymorphicNestedTests`. |
| H7 | Source generator: two context classes sharing a simple name crash the generator (duplicate `AddSource` hint). | **FIXED** — hint names fully qualified. `SourceGeneratorRobustnessTests`. |
| H8 | Source generator: `init`-only properties emit `CS8852` — registering any record/`init` POCO breaks the consumer build. | **FIXED** — generated setter path handles init-only via the supported pattern. |
| H9 | Source generator: `CreateObject` emits `new T()` unconditionally — parameterised-ctor, `required`-member, and abstract types break the build. | **FIXED** — guarded emission; unsupported shapes get a diagnostic, not broken code. |
| H10 | Source generator: drops `[HumlIgnore]`/`[HumlProperty]`/naming metadata — generated path serialises ignored properties (data leak) and uses wrong keys. | **FIXED** — generator reads the same attribute metadata as the reflection path. |
| H11 | Serialiser: dictionary keys formatted with current-culture `ToString()` (non-invariant output). *(verifier downgraded to medium, but folded in here as a correctness/parity fix.)* | **FIXED** — invariant formatting throughout. |
| H12 | Serialiser: property getter that throws leaks `TargetInvocationException`. *(verifier downgraded to medium.)* | **FIXED** — getter invocation wrapped. |

## Medium (triaged — fix in this goal where cheap, else dispositioned)

| # | Finding | Disposition |
|---|---------|-------------|
| M1 | Non-decimal 64-bit literals silently wrap two's-complement (`0xFFFF…FF` → −1). | **FIXED already** in the G3 AUDIT pass (range-checked `Convert.ToUInt64`). `AuditItemTests`. |
| M2 | Deep `HumlMapping` chain → `StackOverflowException` in record `Equals`/`GetHashCode` (not reachable via `Huml.Parse`; only consumer-built deep ASTs). | **FIXED** — iterative structural equality on AST records (see M3). |
| M3 | Record equality on `HumlSequence`/`HumlInlineMapping`/`HumlDocument` uses reference equality, breaking the documented structural-equality contract. | **FIXED** — element-wise, depth-bounded `Equals`/`GetHashCode`. `AstEqualityTests`. |
| M4 | Silent integer truncation coercing out-of-range integers into narrow-backed enums. | DEFERRED post-beta — documented; low real-world impact, fix risks behaviour churn. |
| M5 | Silent lossy `Convert.ChangeType` coercions (float→int rounds, int→bool, bool→string). | DEFERRED — needs a coercion-policy design (STJ has `NumberHandling`; parallels M4). Tracked. |
| M6 | Mapping into a scalar target silently returns `default(T)`. | **FIXED** as part of the G3.3 root-shape work (now throws `HumlDeserializeException`). |
| M7 | `Huml.Populate` partial-mutation on mid-document failure undocumented. | **FIXED** (documentation) in G4.2 XML-doc pass — contract stated on `Populate`. |
| M8 | `string`→`Guid` (non-`IConvertible`) fails despite a comment claiming support. | DEFERRED — add `Guid`/`Uri`/`Version` coercion post-beta; comment corrected now. |
| M9 | `InvalidOperationException` for enums with case-insensitively colliding member names. | **FIXED** — wrapped as `HumlDeserializeException`. |
| M10 | Unregistered runtime derived type serialises with no discriminator (data loss). | DEFERRED — STJ throws here; align post-beta (needs an options switch). Documented. |
| M11–M14 | Source-generator mediums: struct `CS0445`, keyword identifiers unescaped, nested/generic context classes, same-simple-name member collision. | **FIXED** with H7–H10 (same generator hardening pass). `SourceGeneratorRobustnessTests`. |
| M15 | Resolver-driven deserialise skips required-member/extension-data/`Disallow`; binds case-insensitively; ignores `DefaultIgnoreCondition`. | PARTIALLY FIXED (case-sensitivity aligned); the seam-parity remainder tracked for post-beta (the resolver path is opt-in and documented as a fast path). |

## Low (documented, not gating)

Six low findings (struct property throwing ctor, misregistered `[HumlDerivedType]` `InvalidCastException`, unescaped names from referenced assemblies, silently-dropped invalid registrations) are recorded in the raw output and tracked as post-beta hardening. None are reachable from untrusted *document* input; all require the consumer to mis-declare their own types.

## Rejected (1)

One finding was refuted by both verifiers and is not a defect (see raw output) — retained for audit trail.
