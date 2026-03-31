using Huml.Net.Exceptions;
using Huml.Net.Parser;
using Huml.Net.Versioning;

namespace Huml.Net;

/// <summary>
/// Provides static methods for serialising and deserialising HUML documents.
/// This is the single public entry point for the library, mirroring the
/// <c>System.Text.Json.JsonSerializer</c> pattern. All internal pipeline classes
/// (<see cref="Serialization.HumlSerializer"/>, <see cref="Serialization.HumlDeserializer"/>,
/// <see cref="HumlParser"/>) are internal — consumers interact only through this class.
/// </summary>
public static class Huml
{
    /// <summary>Serialises <paramref name="value"/> to a HUML string.</summary>
    /// <typeparam name="T">The type of the value to serialise.</typeparam>
    /// <param name="value">The value to serialise.</param>
    /// <param name="options">Serialisation options; defaults to <see cref="HumlOptions.Default"/>.</param>
    /// <returns>A HUML-formatted string.</returns>
    /// <exception cref="HumlSerializeException">Thrown when serialisation fails.</exception>
    public static string Serialize<T>(T value, HumlOptions? options = null)
        => Serialization.HumlSerializer.Serialize(value, options);

    /// <summary>Serialises <paramref name="value"/> of the given <paramref name="type"/> to a HUML string.</summary>
    /// <param name="value">The value to serialise. May be <c>null</c>.</param>
    /// <param name="type">The declared type to use for serialisation.</param>
    /// <param name="options">Serialisation options; defaults to <see cref="HumlOptions.Default"/>.</param>
    /// <returns>A HUML-formatted string.</returns>
    /// <exception cref="HumlSerializeException">Thrown when serialisation fails.</exception>
    public static string Serialize(object? value, Type type, HumlOptions? options = null)
        => Serialization.HumlSerializer.Serialize(value, type, options);

    /// <summary>
    /// Deserialises a HUML string into <typeparamref name="T"/>.
    /// This overload converts <paramref name="huml"/> to a span and delegates to the
    /// <see cref="Deserialize{T}(ReadOnlySpan{char}, HumlOptions?)"/> implementation.
    /// </summary>
    /// <typeparam name="T">The target type.</typeparam>
    /// <param name="huml">The HUML string.</param>
    /// <param name="options">Parsing options; defaults to <see cref="HumlOptions.Default"/>.</param>
    /// <returns>A populated instance of <typeparamref name="T"/>.</returns>
    /// <exception cref="HumlParseException">Thrown when the HUML input is invalid.</exception>
    /// <exception cref="HumlDeserializeException">Thrown when mapping to <typeparamref name="T"/> fails.</exception>
    public static T Deserialize<T>(string huml, HumlOptions? options = null)
        => Deserialize<T>(huml.AsSpan(), options);

    /// <summary>
    /// Deserialises a HUML character span into <typeparamref name="T"/>. This is the single
    /// implementation overload; the <see cref="Deserialize{T}(string, HumlOptions?)"/>
    /// overload delegates here via <c>AsSpan()</c>.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The span is materialised to a managed <see cref="string"/> via <c>ToString()</c> internally,
    /// because the current lexer accepts only <see cref="string"/> input. This means accepting
    /// <see cref="ReadOnlySpan{T}"/> at the API boundary does not avoid a heap allocation today.
    /// </para>
    /// <para>
    /// A true zero-copy parse path using a <c>ref struct</c> lexer that operates directly on
    /// <see cref="ReadOnlySpan{T}"/> is planned as a v2 enhancement.
    /// </para>
    /// </remarks>
    /// <typeparam name="T">The target type.</typeparam>
    /// <param name="huml">The HUML document as a character span.</param>
    /// <param name="options">Parsing options; defaults to <see cref="HumlOptions.Default"/>.</param>
    /// <returns>A populated instance of <typeparamref name="T"/>.</returns>
    /// <exception cref="HumlParseException">Thrown when the HUML input is invalid.</exception>
    /// <exception cref="HumlDeserializeException">Thrown when mapping to <typeparamref name="T"/> fails.</exception>
    public static T Deserialize<T>(ReadOnlySpan<char> huml, HumlOptions? options = null)
        => Serialization.HumlDeserializer.Deserialize<T>(huml, options);

    /// <summary>Deserialises a HUML string into an object of <paramref name="targetType"/>.</summary>
    /// <param name="huml">The HUML string.</param>
    /// <param name="targetType">The target type.</param>
    /// <param name="options">Parsing options; defaults to <see cref="HumlOptions.Default"/>.</param>
    /// <returns>A populated object, or <c>null</c> if the HUML value is null.</returns>
    /// <exception cref="HumlParseException">Thrown when the HUML input is invalid.</exception>
    /// <exception cref="HumlDeserializeException">Thrown when mapping to <paramref name="targetType"/> fails.</exception>
    public static object? Deserialize(string huml, Type targetType, HumlOptions? options = null)
        => Serialization.HumlDeserializer.Deserialize(huml, targetType, options);

    /// <summary>
    /// Parses a HUML string and returns the document AST without mapping to a .NET type.
    /// Useful for validation — throws <see cref="HumlParseException"/> if the input is invalid.
    /// </summary>
    /// <param name="huml">The HUML string to parse.</param>
    /// <param name="options">Parsing options; defaults to <see cref="HumlOptions.Default"/>.</param>
    /// <returns>The <see cref="HumlDocument"/> AST root.</returns>
    /// <exception cref="HumlParseException">Thrown when the HUML input is invalid.</exception>
    public static HumlDocument Parse(string huml, HumlOptions? options = null)
        => new HumlParser(huml, options ?? HumlOptions.Default).Parse();
}
