namespace Huml.Net.Serialization;

/// <summary>
/// Overrides the global <see cref="P:Huml.Net.Versioning.HumlOptions.PropertyNamingPolicy"/> for the annotated property.
/// Specify <see cref="HumlKnownNamingPolicy.Unspecified"/> (or omit the attribute) to defer to the
/// global policy.
/// </summary>
[AttributeUsage(AttributeTargets.Property, AllowMultiple = false, Inherited = true)]
public sealed class HumlNamingPolicyAttribute : Attribute
{
    /// <summary>The per-member naming policy.</summary>
    public HumlKnownNamingPolicy Policy { get; }

    /// <summary>Initialises a new instance specifying the per-member naming policy.</summary>
    /// <param name="policy">The naming policy for this property.</param>
    public HumlNamingPolicyAttribute(HumlKnownNamingPolicy policy)
    {
        Policy = policy;
    }
}
