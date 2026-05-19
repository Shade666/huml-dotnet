namespace Huml.Net.Serialization;

/// <summary>
/// Non-generic base class for type metadata used in HUML (de)serialisation.
/// This base allows <see cref="IHumlTypeInfoResolver.GetTypeInfo"/> to return a covariant
/// result without requiring a type parameter at the call site.
/// </summary>
public abstract class HumlTypeInfo
{
}

/// <summary>
/// Provides type metadata for HUML (de)serialisation of <typeparamref name="T"/>.
/// </summary>
/// <typeparam name="T">The CLR type this metadata describes.</typeparam>
public abstract class HumlTypeInfo<T> : HumlTypeInfo
{
    /// <summary>The CLR type this metadata describes.</summary>
    public Type Type => typeof(T);
}
