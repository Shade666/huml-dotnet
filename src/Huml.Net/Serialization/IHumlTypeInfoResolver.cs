using Huml.Net.Versioning;

namespace Huml.Net.Serialization;

/// <summary>
/// Provides <see cref="HumlTypeInfo"/> instances for types encountered during HUML
/// (de)serialisation. Implement this interface to supply pre-computed type metadata
/// (e.g. from a source generator) instead of the default reflection path.
/// Returning <see langword="null"/> from <see cref="GetTypeInfo"/> causes the library to
/// fall through to the built-in reflection path for that type.
/// </summary>
public interface IHumlTypeInfoResolver
{
    /// <summary>
    /// Returns type metadata for <paramref name="type"/>, or <see langword="null"/> if this
    /// resolver does not handle that type.
    /// </summary>
    /// <param name="type">The CLR type being serialised or deserialised.</param>
    /// <param name="options">The active <see cref="HumlOptions"/> for the current operation.</param>
    /// <returns>
    /// A <see cref="HumlTypeInfo"/> instance describing <paramref name="type"/>, or
    /// <see langword="null"/> to indicate that this resolver does not handle the type and the
    /// caller should use the built-in reflection path instead.
    /// </returns>
    HumlTypeInfo? GetTypeInfo(Type type, HumlOptions options);
}
