using Huml.Net.Parser;

namespace Huml.Net.Serialization;

/// <summary>
/// Abstract base class for all HUML converters. Provides a non-generic entry point
/// for collection storage in <c>HumlOptions.Converters</c> and type-check dispatch
/// via <see cref="CanConvert(Type)"/>.
/// </summary>
/// <remarks>
/// Converters must be stateless — a single instance is cached and shared across threads.
/// Do not store per-call or per-thread state in converter instance fields.
/// </remarks>
public abstract class HumlConverter
{
    /// <summary>
    /// Returns <c>true</c> when this converter can handle <paramref name="typeToConvert"/>.
    /// </summary>
    /// <param name="typeToConvert">The CLR type to evaluate.</param>
    public abstract bool CanConvert(Type typeToConvert);

    /// <summary>Internal hook — reads a <see cref="HumlNode"/> without knowing T.</summary>
    internal abstract object? ReadObject(HumlNode node);

    /// <summary>Internal hook — writes <paramref name="value"/> without knowing T.</summary>
    internal abstract void WriteObject(HumlWriterContext context, object? value);
}
