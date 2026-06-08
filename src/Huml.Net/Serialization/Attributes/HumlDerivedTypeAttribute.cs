namespace Huml.Net.Serialization;

/// <summary>
/// Registers a concrete derived type and its discriminator label on the polymorphic base class.
/// Repeatable — add one attribute per concrete subtype.
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Interface, AllowMultiple = true, Inherited = false)]
public sealed class HumlDerivedTypeAttribute : Attribute
{
    /// <summary>The concrete derived type.</summary>
    public Type DerivedType { get; }

    /// <summary>The discriminator string label that identifies this derived type.</summary>
    public string TypeDiscriminator { get; }

    /// <summary>Registers a concrete derived type with its discriminator label.</summary>
    /// <param name="derivedType">The concrete derived type.</param>
    /// <param name="typeDiscriminator">The discriminator string label that identifies this derived type.</param>
    public HumlDerivedTypeAttribute(Type derivedType, string typeDiscriminator)
    {
#pragma warning disable CA1510 // ThrowIfNull is .NET 6+; library targets netstandard2.1
        if (derivedType == null) throw new ArgumentNullException(nameof(derivedType));
        if (typeDiscriminator == null) throw new ArgumentNullException(nameof(typeDiscriminator));
#pragma warning restore CA1510
        DerivedType = derivedType;
        TypeDiscriminator = typeDiscriminator;
    }
}
