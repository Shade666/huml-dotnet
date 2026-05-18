namespace Huml.Net.Serialization;

/// <summary>
/// Controls which properties are omitted from HUML serialisation output based on their runtime value.
/// </summary>
/// <remarks>
/// <para>
/// This is a <see cref="FlagsAttribute"/> enum, so values can be combined with the bitwise OR operator.
/// <see cref="Always"/> is the combination of <see cref="WhenWritingNull"/> and <see cref="WhenWritingDefault"/>
/// (value <c>3</c>).
/// </para>
/// <para>
/// The naming and semantics are intentionally aligned with
/// <c>System.Text.Json.JsonIgnoreCondition</c> to ease adoption for developers already
/// familiar with the STJ API.
/// </para>
/// <para>
/// Precedence chain (highest to lowest): per-property <c>[HumlProperty(OmitIfDefault = true)]</c>
/// overrides class-level <c>[HumlIgnoreDefaults]</c>, which overrides
/// <c>HumlOptions.DefaultIgnoreCondition</c>.
/// </para>
/// </remarks>
[Flags]
public enum HumlIgnoreCondition
{
    /// <summary>
    /// No properties are omitted based on their value.
    /// This is the default behaviour, preserving all existing serialisation output unchanged.
    /// </summary>
    Never = 0,

    /// <summary>
    /// Omit a property when its runtime value is <c>null</c>.
    /// This flag never fires for value-type properties (e.g. <c>int</c>, <c>bool</c>),
    /// because value types cannot hold a <c>null</c> reference.
    /// </summary>
    WhenWritingNull = 1,

    /// <summary>
    /// Omit a property when its runtime value equals the CLR default for its type:
    /// <c>null</c> for reference types, or the zero-value (e.g. <c>0</c>, <c>false</c>)
    /// for value types.
    /// </summary>
    /// <remarks>
    /// For reference types this is a superset of <see cref="WhenWritingNull"/>:
    /// a reference-type property is omitted when its value is <c>null</c>.
    /// For value types, properties are omitted when their value equals the result of
    /// <c>Activator.CreateInstance(propertyType)</c> (i.e. the parameterless default).
    /// </remarks>
    WhenWritingDefault = 2,

    /// <summary>
    /// Omit every property unconditionally, regardless of its runtime value.
    /// Equivalent to <see cref="WhenWritingNull"/> | <see cref="WhenWritingDefault"/> (value <c>3</c>).
    /// </summary>
    Always = 3,
}
