namespace Huml.Net.Versioning;

/// <summary>Configuration options for HUML parsing and serialisation.</summary>
public sealed class HumlOptions
{
    /// <summary>
    /// Default options: explicit v0.2, version taken from <c>HumlOptions</c>,
    /// unknown version behaviour is <see cref="UnknownVersionBehaviour.Throw"/>.
    /// </summary>
    public static readonly HumlOptions Default = new();

    /// <summary>
    /// Auto-detect options: version read from the document <c>%HUML</c> header;
    /// falls back to <see cref="HumlSpecVersion.V0_2"/> if no header is present.
    /// </summary>
    public static readonly HumlOptions AutoDetect = new()
    {
        VersionSource = VersionSource.Header,
    };

    /// <summary>The HUML spec version to use when parsing or serialising.</summary>
    public HumlSpecVersion SpecVersion { get; init; } = HumlSpecVersion.V0_2;

    /// <summary>Where to read the spec version from.</summary>
    public VersionSource VersionSource { get; init; } = VersionSource.Options;

    /// <summary>Behaviour when an unsupported version is declared in the document header.</summary>
    public UnknownVersionBehaviour UnknownVersionBehaviour { get; init; }
        = UnknownVersionBehaviour.Throw;

    /// <summary>
    /// Maximum recursion depth allowed during parsing. Exceeding this limit throws
    /// <see cref="T:Huml.Net.Exceptions.HumlParseException"/> instead of risking an unrecoverable
    /// <see cref="StackOverflowException"/>. Default is 512.
    /// </summary>
    public int MaxRecursionDepth { get; init; } = 512;
}
