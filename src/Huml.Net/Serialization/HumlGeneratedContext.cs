using Huml.Net.Versioning;

namespace Huml.Net.Serialization;

/// <summary>
/// Abstract base class for source-generated HUML serialisation contexts.
/// Subclass with <c>partial</c> and annotate with <c>[HumlSerializable(typeof(T))]</c> to
/// register types; the source generator will emit the <see cref="HumlTypeInfo{T}"/> implementations.
/// </summary>
/// <example>
/// <code>
/// [HumlSerializable(typeof(WeatherForecast))]
/// public partial class MyContext : HumlGeneratedContext { }
/// </code>
/// </example>
public abstract class HumlGeneratedContext : IHumlTypeInfoResolver
{
    /// <summary>
    /// Returns type metadata for <paramref name="type"/>, or <see langword="null"/> if this
    /// context does not handle that type. Source-generated subclasses override this method
    /// to dispatch by registered type.
    /// </summary>
    public abstract HumlTypeInfo? GetTypeInfo(Type type, HumlOptions options);

    /// <summary>
    /// Returns strongly-typed metadata for <typeparamref name="T"/>, or
    /// <see langword="null"/> if this context does not handle that type.
    /// </summary>
    public virtual HumlTypeInfo<T>? GetTypeInfo<T>()
        => GetTypeInfo(typeof(T), HumlOptions.LatestSupported) as HumlTypeInfo<T>;
}
