# Attributes reference

Every attribute Huml.Net recognises, in one place. All live in the `Huml.Net.Serialization`
namespace unless noted; the source-generation attributes are in
`Huml.Net.Serialization.Attributes`.

## Property & member attributes

| Attribute | Applies to | Effect |
| --------- | ---------- | ------ |
| `[HumlProperty(name)]` | property, ctor parameter | Overrides the HUML key. `OmitIfDefault = true` suppresses the property when its value equals the type default. `Inline = …` overrides the collection format for this member. |
| `[HumlIgnore]` | property | Excludes the property from both serialisation and deserialisation. |
| `[HumlRequired]` | property | The key must be present when deserialising, else `HumlDeserializeException`. (The C# `required` modifier is honoured equivalently.) |
| `[HumlExtensionData]` | property of `Dictionary<string, HumlNode>` or `Dictionary<string, object?>` | Captures HUML keys that match no property. Suppresses `UnmappedMemberHandling.Disallow`. |
| `[HumlConverter(typeof(C))]` | property, type | Uses converter `C` for this member/type. Highest precedence in the converter chain. At **type level**, `C` may be a `HumlConverterFactory` — see [Custom Converters — Converter Factories](custom-converters.md#converter-factories). At **property level**, `C` must be a concrete converter; a factory throws `InvalidOperationException` (property-level converters resolve without an `HumlOptions` context, so `CreateConverter` cannot run). |
| `[HumlNumberHandling(…)]` | property, type | Per-member number-handling (e.g. read/write numbers as strings). |
| `[HumlNamingPolicy(HumlKnownNamingPolicy.KebabCase)]` | property | Overrides the container's naming policy for this one member. Takes a `HumlKnownNamingPolicy` value (`CamelCase`, `KebabCase`, `PascalCase`, `SnakeCase`, or `Unspecified` to defer to the global policy). |

## Type-level attributes

| Attribute | Applies to | Effect |
| --------- | ---------- | ------ |
| `[HumlIgnoreDefaults]` | type | Suppresses CLR-default-valued properties across the whole type (`WhenWritingDefault` semantics). |
| `[HumlConstructor]` | constructor | Selects which constructor is used for deserialisation when a type has more than one. |
| `[HumlPolymorphic(discriminator?)]` | base class / interface | Marks a polymorphic base. The discriminator key defaults to `_type`. `UnknownDerivedTypeHandling` controls behaviour for unrecognised discriminator values. |
| `[HumlDerivedType(typeof(D), "label")]` | base class / interface | Registers a concrete derived type and its discriminator label. Repeatable. |

## Enum attribute

| Attribute | Applies to | Effect |
| --------- | ---------- | ------ |
| `[HumlEnumValue("name")]` | enum member | Overrides the HUML string used for this enum member. |

## Source-generation attribute (`Huml.Net.Serialization.Attributes`)

| Attribute | Applies to | Effect |
| --------- | ---------- | ------ |
| `[HumlSerializable(typeof(T))]` | `partial` `HumlGeneratedContext` subclass | Registers `T` for compile-time metadata generation. Repeatable. See [Use the source generator](source-generator.md). |

## See also

- [Options reference](options-reference.md) — the `HumlOptions` knobs that interact with these attributes.
- <xref:Huml.Net.HumlSerializer> — full signatures and members.
