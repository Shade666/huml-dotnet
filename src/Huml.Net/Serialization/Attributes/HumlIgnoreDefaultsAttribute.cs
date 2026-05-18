namespace Huml.Net.Serialization;

/// <summary>
/// Instructs the HUML serialiser to omit all properties of the decorated type when their
/// runtime value equals the CLR default for their declared type.
/// </summary>
/// <remarks>
/// <para>
/// Applying this attribute to a class or struct is equivalent to adding
/// <c>[HumlProperty(OmitIfDefault = true)]</c> to every property on that type.
/// It is intended for DTOs with many optional properties (e.g. patch payloads, diff objects)
/// where per-property annotation would be repetitive boilerplate.
/// </para>
/// <para>
/// Precedence: per-property <c>[HumlProperty(OmitIfDefault = true)]</c> takes the highest
/// priority and is evaluated independently before this class-level attribute.
/// <c>HumlOptions.DefaultIgnoreCondition</c> is consulted last as the global fallback.
/// </para>
/// <para>
/// The attribute is inherited by derived classes (<c>Inherited = true</c>). A derived class
/// whose base is decorated with <c>[HumlIgnoreDefaults]</c> will also suppress its own
/// properties at their CLR default. However, decorating only the derived class does
/// <em>not</em> retroactively suppress base-class properties.
/// </para>
/// </remarks>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct, AllowMultiple = false, Inherited = true)]
public sealed class HumlIgnoreDefaultsAttribute : Attribute { }
