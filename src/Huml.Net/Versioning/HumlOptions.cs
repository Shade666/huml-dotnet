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
    /// Auto-detect options: reads the <c>%HUML vX.Y</c> directive from the document header,
    /// validates the declared version against <see cref="SpecVersionPolicy.MinimumSupported"/> and
    /// <see cref="SpecVersionPolicy.Latest"/>, and dispatches <see cref="UnknownVersionBehaviour"/>
    /// (<c>Throw</c> / <c>UseLatest</c> / <c>UsePrevious</c>) when the version is unrecognised.
    /// Falls back to <see cref="HumlSpecVersion.V0_2"/> when no header is present.
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
