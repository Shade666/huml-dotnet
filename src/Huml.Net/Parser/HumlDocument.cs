using Huml.Net.Versioning;

namespace Huml.Net.Parser;

/// <summary>
/// Represents a mapping block in the HUML AST. Used for both the document root
/// and for every nested multiline mapping block introduced by the <c>::</c> vector indicator.
/// </summary>
/// <remarks>
/// A single type is used for all multiline mapping contexts to keep the AST hierarchy shallow.
/// Inline <c>{ key: value }</c> notation and empty <c>{}</c> dicts produce a
/// <see cref="HumlInlineMapping"/> node instead.
/// </remarks>
/// <param name="Entries">The mapping entries or list items in this block.</param>
public sealed record HumlDocument(IReadOnlyList<HumlNode> Entries) : HumlNode
{
    /// <summary>
    /// The HUML spec version detected from the <c>%HUML</c> header in the source document,
    /// or <c>null</c> when no header was present or the document was constructed directly in code.
    /// </summary>
    /// <remarks>
    /// Populated by <see cref="HumlParser"/> during parsing. Use this property to preserve
    /// the original spec version when round-tripping:
    /// <code>
    /// var doc = Huml.Parse(input);
    /// var output = Huml.Serialize(dto,
    ///     new HumlOptions { SpecVersion = doc.DetectedVersion ?? HumlSpecVersion.V0_2 });
    /// </code>
    /// </remarks>
    public HumlSpecVersion? DetectedVersion { get; init; }
}
