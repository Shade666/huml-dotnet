using Huml.Net.Versioning;

namespace Huml.Net.Serialization;

/// <summary>
/// Marks a class or interface as the polymorphic base for discriminator-based dispatch.
/// The discriminator key (default <c>_type</c>) is emitted as the first mapping entry
/// during serialisation and consumed during deserialisation to select the concrete type.
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Interface, AllowMultiple = false, Inherited = false)]
public sealed class HumlPolymorphicAttribute : Attribute
{
    /// <summary>The HUML key used to identify the derived type. Defaults to <c>_type</c>.</summary>
    public string TypeDiscriminatorPropertyName { get; }

    /// <summary>
    /// Controls behaviour when the discriminator value is not recognised.
    /// Defaults to <see cref="HumlUnknownDerivedTypeHandling.Throw"/>.
    /// </summary>
    public HumlUnknownDerivedTypeHandling UnknownDerivedTypeHandling { get; set; } = HumlUnknownDerivedTypeHandling.Throw;

    /// <summary>
    /// Initialises a new instance with the specified discriminator key name.
    /// </summary>
    /// <param name="typeDiscriminatorPropertyName">
    /// The HUML key name for the type discriminator. Must be a valid HUML bare key.
    /// </param>
    public HumlPolymorphicAttribute(string typeDiscriminatorPropertyName = "_type")
    {
        TypeDiscriminatorPropertyName = typeDiscriminatorPropertyName;
    }
}
