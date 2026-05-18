namespace Huml.Net.Serialization;

/// <summary>
/// Designates a single property as the overflow bucket for HUML keys that do not match any
/// declared property during deserialisation.
/// </summary>
/// <remarks>
/// <para>
/// The decorated property must be of type <c>Dictionary&lt;string, HumlNode&gt;</c> or
/// <c>Dictionary&lt;string, object?&gt;</c>. Applying this attribute to a property of any
/// other type causes <see cref="System.InvalidOperationException"/> to be thrown at first use
/// (descriptor build time).
/// </para>
/// <para>
/// Only one property per type hierarchy may carry <c>[HumlExtensionData]</c>. Declaring it on
/// more than one property causes <see cref="System.InvalidOperationException"/> to be thrown at
/// first use with a message identifying the type and both property names.
/// </para>
/// <para>
/// During deserialisation, every HUML mapping key that does not correspond to a declared
/// property is captured into the annotated dictionary. Keys are stored verbatim; no naming-policy
/// transform is applied. Declared properties continue to bind normally.
/// </para>
/// <para>
/// During serialisation, extension keys are emitted after all declared properties in insertion
/// order, using the same key-quoting rules as declared properties.
/// </para>
/// </remarks>
[AttributeUsage(AttributeTargets.Property, AllowMultiple = false, Inherited = true)]
public sealed class HumlExtensionDataAttribute : Attribute { }
