namespace Huml.Net.Serialization;

/// <summary>
/// Describes a single property for use during HUML (de)serialisation.
/// Mirroring <c>JsonPropertyInfo</c>, delegates are boxed non-generic to avoid
/// covariance and contravariance issues across the type hierarchy.
/// </summary>
public class HumlPropertyInfo
{
    /// <summary>The HUML key name for this property.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>The CLR type of this property's value.</summary>
    public Type? PropertyType { get; set; }

    /// <summary>Reads the property value from a boxed owner instance.</summary>
    public Func<object, object?>? Get { get; set; }

    /// <summary>Writes the property value to a boxed owner instance.</summary>
    public Action<object, object?>? Set { get; set; }

    /// <summary>Whether this property is required during deserialisation.</summary>
    public bool IsRequired { get; set; }

    /// <summary>Serialisation order; lower values are emitted first.</summary>
    public int Order { get; set; }
}
