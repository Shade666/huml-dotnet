# How-to guides

Each guide solves one specific problem, assumes you already know the basics from
[Getting started](getting-started.md), and ends with a runnable example in the companion
[examples repository](examples.md).

## Shaping the output

- [Customize property names](naming-policy.md) — naming policies and `[HumlProperty]` overrides.
- [Ignore properties & omit defaults](ignore-properties.md) — `[HumlIgnore]`, `[HumlIgnoreDefaults]`, and ignore conditions.
- [Control inline vs multiline](inline-serialisation.md) — inline dicts/lists per property or globally.
- [Serialize dates & times](date-time.md) — `DateTime`, `DateTimeOffset`, `TimeSpan`, `DateOnly`, `TimeOnly`.
- [Work with enums](enum-serialisation.md) — enum round-trips and `[HumlEnumValue]` wire names.
- [Read & write numbers as strings](number-handling.md) — `HumlNumberHandling` for quoted numerics.

## Mapping to .NET types

- [Bind constructors & records](constructor-binding.md) — records, `[HumlConstructor]`, `init`-only setters.
- [Require properties](required-properties.md) — `[HumlRequired]` and the C# `required` modifier.
- [Capture unknown keys](extension-data.md) — `[HumlExtensionData]` overflow buckets.
- [Serialize polymorphic types](polymorphism.md) — discriminator-based dispatch with `[HumlPolymorphic]`.
- [Overlay onto an instance](populate.md) — `HumlSerializer.Populate` for defaults + overrides.
- [Write a custom converter](custom-converters.md) — `HumlConverter<T>` and converter factories.

## Robustness and deployment

- [Handle errors](error-handling.md) — the exception contract and strict parsing.
- [Publish AOT / trimmed](aot-trimming.md) — trim-safe usage and annotations.
- [Use the source generator](source-generator.md) — reflection-free metadata via `HumlGeneratedContext`.
