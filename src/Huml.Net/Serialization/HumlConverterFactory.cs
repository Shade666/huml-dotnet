using Huml.Net.Parser;
using Huml.Net.Versioning;

namespace Huml.Net.Serialization;

/// <summary>
/// Abstract base for a HUML converter that produces a different concrete
/// <see cref="HumlConverter"/> instance per requested type, mirroring
/// <c>System.Text.Json.Serialization.JsonConverterFactory</c>.
/// </summary>
/// <remarks>
/// Use a factory when a single converter <em>implementation</em> needs to serve many
/// concrete CLR types dynamically — e.g. a lenient enum converter usable across an entire
/// schema, or a converter derived via <c>MakeGenericType</c> for a family of generic types.
/// For a single fixed type, prefer <see cref="HumlConverter{T}"/>.
/// <para>
/// A factory is never asked to read or write directly — <see cref="ConverterCache"/> replaces
/// it with the converter returned from <see cref="CreateConverter"/> before dispatch, and the
/// produced converter is cached per requested type. Factories, like all converters, must be
/// stateless.
/// </para>
/// </remarks>
public abstract class HumlConverterFactory : HumlConverter
{
    /// <inheritdoc/>
    public abstract override bool CanConvert(Type typeToConvert);

    /// <summary>
    /// Creates the concrete <see cref="HumlConverter"/> to use for <paramref name="typeToConvert"/>,
    /// or <c>null</c> to decline — resolution then falls through to the next candidate converter
    /// or the built-in type dispatch.
    /// </summary>
    /// <param name="typeToConvert">The CLR type a converter is being resolved for.</param>
    /// <param name="options">The active <see cref="HumlOptions"/> for this resolution.</param>
    public abstract HumlConverter? CreateConverter(Type typeToConvert, HumlOptions options);

    internal sealed override object? ReadObject(HumlNode node) =>
        throw new NotSupportedException(
            $"'{GetType().Name}' is a {nameof(HumlConverterFactory)} and cannot read directly — " +
            $"{nameof(CreateConverter)} must have produced the converter actually used for dispatch.");

    internal sealed override void WriteObject(HumlWriterContext context, object? value) =>
        throw new NotSupportedException(
            $"'{GetType().Name}' is a {nameof(HumlConverterFactory)} and cannot write directly — " +
            $"{nameof(CreateConverter)} must have produced the converter actually used for dispatch.");
}
