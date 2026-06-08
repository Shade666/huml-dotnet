namespace Huml.Net.Versioning;

/// <summary>Controls deserialiser behaviour when a discriminator value is not recognised.</summary>
public enum HumlUnknownDerivedTypeHandling
{
    /// <summary>
    /// Throw <c>HumlDeserializeException</c> when the discriminator value
    /// does not match any registered derived type. This is the default.
    /// </summary>
    Throw = 0,

    /// <summary>
    /// Deserialise as the base type when the discriminator value is unrecognised.
    /// The discriminator entry is still stripped before fallback so
    /// <c>UnmappedMemberHandling.Disallow</c> is not triggered.
    /// </summary>
    FallBackToBaseType = 1,
}
