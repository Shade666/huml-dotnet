namespace Huml.Net.Versioning;

/// <summary>
/// Controls how <see cref="T:Huml.Net.Serialization.HumlSerializerImpl"/> and
/// <see cref="T:Huml.Net.Serialization.HumlDeserializer"/> handle numeric values.
/// </summary>
/// <remarks>
/// <para>
/// When <see cref="AllowReadingFromString"/> is set, a <c>ScalarKind.String</c> scalar
/// (i.e. a quoted HUML value such as <c>"42"</c>) may be coerced to a numeric target type
/// during deserialisation. Without this flag, assigning a quoted string to a numeric
/// property throws <see cref="T:Huml.Net.Exceptions.HumlDeserializeException"/>.
/// </para>
/// <para>
/// When <see cref="WriteAsString"/> is set, finite numeric values (integers, <c>float</c>,
/// <c>double</c>, <c>decimal</c>) are emitted as quoted HUML strings rather than bare
/// numeric literals. <c>NaN</c>, <c>+inf</c>, and <c>-inf</c> are always emitted
/// unquoted regardless of this setting — they are HUML native scalar kinds and must
/// remain unquoted for round-trip correctness.
/// </para>
/// <para>
/// Combining <see cref="WriteAsString"/> and <see cref="AllowReadingFromString"/> produces
/// a round-trip-safe configuration where every numeric value survives a
/// serialise-then-deserialise cycle.
/// </para>
/// </remarks>
[Flags]
public enum HumlNumberHandling
{
    /// <summary>
    /// Default strict behaviour: numeric values are emitted as bare HUML literals and
    /// a quoted-string scalar cannot be coerced to a numeric target type during
    /// deserialisation.
    /// </summary>
    Strict = 0,

    /// <summary>
    /// Permits coercing a <c>ScalarKind.String</c> scalar (a quoted value such as
    /// <c>"42"</c>) to a numeric target type during deserialisation.
    /// Without this flag, such an assignment throws
    /// <see cref="T:Huml.Net.Exceptions.HumlDeserializeException"/>.
    /// </summary>
    AllowReadingFromString = 1,

    /// <summary>
    /// Emits finite numeric values as quoted HUML strings during serialisation.
    /// <c>NaN</c>, <c>+inf</c>, and <c>-inf</c> are never quoted — they are HUML
    /// native scalar kinds.
    /// </summary>
    WriteAsString = 2,
}
