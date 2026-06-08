namespace Huml.Net.Serialization;

/// <summary>
/// Non-generic base class for type metadata used in HUML (de)serialisation.
/// This base allows <see cref="IHumlTypeInfoResolver.GetTypeInfo"/> to return a covariant
/// result without requiring a type parameter at the call site.
/// </summary>
public abstract class HumlTypeInfo
{
    /// <summary>
    /// The property metadata for this type, or <see langword="null"/> to fall through to
    /// the built-in reflection path. An empty list means "type has no properties";
    /// null means "resolver does not supply property metadata for this type".
    /// </summary>
    public virtual IReadOnlyList<HumlPropertyInfo>? Properties => null;

    /// <summary>Invoked before an instance of this type is serialised. Null if not overridden.</summary>
    public virtual Action<object>? OnSerializing => null;

    /// <summary>Invoked after an instance of this type has been serialised. Null if not overridden.</summary>
    public virtual Action<object>? OnSerialized => null;

    /// <summary>Invoked before deserialisation begins for an instance of this type. Null if not overridden.</summary>
    public virtual Action<object>? OnDeserializing => null;

    /// <summary>Invoked after deserialisation is complete for an instance of this type. Null if not overridden.</summary>
    public virtual Action<object>? OnDeserialized => null;
}

/// <summary>
/// Provides type metadata for HUML (de)serialisation of <typeparamref name="T"/>.
/// </summary>
/// <typeparam name="T">The CLR type this metadata describes.</typeparam>
public abstract class HumlTypeInfo<T> : HumlTypeInfo
{
    /// <summary>The CLR type this metadata describes.</summary>
    public Type Type => typeof(T);

    /// <summary>
    /// Factory delegate for creating a new instance of <typeparamref name="T"/>, or
    /// <see langword="null"/> to fall back to <c>Activator.CreateInstance</c>.
    /// </summary>
    public virtual Func<T>? CreateObject => null;
}
