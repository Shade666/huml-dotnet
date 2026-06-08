namespace Huml;

/// <summary>
/// Identifies a built-in naming policy to apply to the annotated property via
/// <c>[HumlNamingPolicy]</c>. Used with <see cref="T:Huml.Net.Serialization.HumlNamingPolicyAttribute"/> to select a
/// per-member key conversion without referencing <c>HumlNamingPolicy</c> singleton instances
/// directly.
/// </summary>
public enum HumlKnownNamingPolicy
{
    /// <summary>No per-member policy; defers to the global <see cref="P:Huml.Net.Versioning.HumlOptions.PropertyNamingPolicy"/>.</summary>
    Unspecified = 0,
    /// <summary>Converts property names to <c>camelCase</c>.</summary>
    CamelCase = 1,
    /// <summary>Converts property names to <c>snake_case</c>.</summary>
    SnakeCase = 2,
    /// <summary>Converts property names to <c>kebab-case</c>.</summary>
    KebabCase = 3,
    /// <summary>Converts property names to <c>PascalCase</c>.</summary>
    PascalCase = 4,
}
