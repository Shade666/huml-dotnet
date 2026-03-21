namespace Huml.Net.Parser;

/// <summary>
/// Represents the root HUML document containing zero or more top-level entries.
/// </summary>
/// <param name="Entries">The top-level mapping entries or list items in the document.</param>
public sealed record HumlDocument(IReadOnlyList<HumlNode> Entries) : HumlNode;
