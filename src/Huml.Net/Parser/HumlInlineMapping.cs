namespace Huml.Net.Parser;

/// <summary>
/// Represents an inline or empty mapping block within a HUML document.
/// Distinct from <see cref="HumlDocument"/>, which is the root document only.
/// </summary>
/// <param name="Entries">The key-value mapping entries in this inline block.</param>
public sealed record HumlInlineMapping(IReadOnlyList<HumlNode> Entries) : HumlNode;
