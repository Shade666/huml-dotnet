namespace Huml.Net.Versioning;

/// <summary>Configuration options for HUML parsing and serialisation.</summary>
public sealed class HumlOptions
{
    /// <summary>
    /// Options pinned to the latest supported spec version (<see cref="HumlSpecVersion.V0_2"/>)
    /// with version taken from <see cref="VersionSource.Options"/>, ignoring any <c>%HUML</c>
    /// header in the document. Use when you always want v0.2 rules regardless of document content.
    /// </summary>
    public static readonly HumlOptions LatestSupported = new();

    /// <summary>
    /// Default options: reads the <c>%HUML</c> header to determine spec version
    /// (<see cref="VersionSource.Header"/>), falling back to
    /// <see cref="HumlSpecVersion.V0_2"/> when no header is present.
    /// Unknown version behaviour is <see cref="UnknownVersionBehaviour.Throw"/>.
    /// Equivalent to <see cref="AutoDetect"/>.
    /// </summary>
    public static readonly HumlOptions Default = new()
    {
        VersionSource = VersionSource.Header,
    };

    /// <summary>
    /// Auto-detect options: reads the <c>%HUML vX.Y</c> directive from the document header,
    /// validates the declared version against <see cref="SpecVersionPolicy.MinimumSupported"/> and
    /// <see cref="SpecVersionPolicy.Latest"/>, and dispatches <see cref="UnknownVersionBehaviour"/>
    /// (<c>Throw</c> / <c>UseLatest</c> / <c>UsePrevious</c>) when the version is unrecognised.
    /// Falls back to <see cref="HumlSpecVersion.V0_2"/> when no header is present.
    /// Equivalent to <see cref="Default"/>.
    /// </summary>
    public static readonly HumlOptions AutoDetect = Default;

    /// <summary>The HUML spec version to use when parsing or serialising.</summary>
    public HumlSpecVersion SpecVersion { get; init; } = HumlSpecVersion.V0_2;

    /// <summary>Where to read the spec version from.</summary>
    public VersionSource VersionSource { get; init; } = VersionSource.Options;

    /// <summary>Behaviour when an unsupported version is declared in the document header.</summary>
    public UnknownVersionBehaviour UnknownVersionBehaviour { get; init; }
        = UnknownVersionBehaviour.Throw;

    /// <summary>
    /// Controls the default output format for collections during serialisation.
    /// <see cref="CollectionFormat.Multiline"/> (the default) emits indented block format.
    /// <see cref="CollectionFormat.Inline"/> emits <c>key:: a, b, c</c> for scalar-only
    /// sequences and <c>key:: k: v, k2: v2</c> for scalar-valued dictionaries.
    /// Collections containing non-scalar items silently fall back to multiline.
    /// </summary>
    public CollectionFormat CollectionFormat { get; init; } = CollectionFormat.Multiline;

    private int _maxRecursionDepth = 512;

    /// <summary>
    /// Maximum recursion depth allowed during parsing. Exceeding this limit throws
    /// <see cref="T:Huml.Net.Exceptions.HumlParseException"/> instead of risking an unrecoverable
    /// <see cref="StackOverflowException"/>. Default is 512. Valid range: [1, 65536].
    /// </summary>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown if value is less than 1 or greater than 65536.
    /// </exception>
    public int MaxRecursionDepth
    {
        get => _maxRecursionDepth;
        init
        {
            if (value < 1 || value > 65536)
#pragma warning disable MA0015 // nameof convention — init accessor uses 'value' but property name is more informative
                throw new ArgumentOutOfRangeException(nameof(MaxRecursionDepth), value,
                    "MaxRecursionDepth must be between 1 and 65536 inclusive.");
#pragma warning restore MA0015
            _maxRecursionDepth = value;
        }
    }
}
