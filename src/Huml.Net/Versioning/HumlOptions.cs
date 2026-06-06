using System.Collections.Concurrent;
using Huml.Net.Serialization;

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

    static HumlOptions()
    {
        LatestSupported.MakeReadOnly();
        Default.MakeReadOnly();
    }

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

    /// <summary>
    /// The naming policy used to convert .NET property names to HUML keys during
    /// serialisation and deserialisation. <c>null</c> (the default) means the .NET
    /// property name is used as-is (ordinal-exact, PascalCase by default in C#).
    /// </summary>
    /// <remarks>
    /// Use <see cref="HumlNamingPolicy.KebabCase"/> for HUML documents
    /// that use <c>kebab-case</c> keys (the most common HUML convention). A
    /// <see cref="Serialization.HumlPropertyAttribute"/> name override always takes
    /// precedence over this policy. This policy applies to .NET property names only —
    /// it does not affect <c>Dictionary&lt;string, T&gt;</c> string keys.
    /// </remarks>
    public HumlNamingPolicy? PropertyNamingPolicy { get; init; }

    /// <summary>
    /// Gets or sets the global condition under which properties are omitted from serialisation output.
    /// Defaults to <see cref="HumlIgnoreCondition.Never"/>, preserving all existing serialisation behaviour.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This option applies globally to every property during serialisation.
    /// The full precedence chain (highest to lowest) is:
    /// per-property <c>[HumlProperty(OmitIfDefault = true)]</c> →
    /// class-level <c>[HumlIgnoreDefaults]</c> →
    /// <c>DefaultIgnoreCondition</c>.
    /// If any higher-priority rule fires, this option is not evaluated for that property.
    /// </para>
    /// <para>
    /// No changes are required from existing consumers — all code using the default
    /// <see cref="HumlIgnoreCondition.Never"/> value behaves identically to previous releases.
    /// </para>
    /// </remarks>
    public HumlIgnoreCondition DefaultIgnoreCondition { get; init; } = HumlIgnoreCondition.Never;

    /// <summary>
    /// When <c>true</c>, <see cref="T:Huml.Net.Serialization.HumlSerializer"/> throws
    /// <see cref="T:Huml.Net.Exceptions.HumlSerializeException"/> if two entries in the same
    /// dictionary produce the same key string (compared using
    /// <see cref="System.StringComparer.Ordinal"/>) during serialisation.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The check fires on the key as emitted — the string produced by calling
    /// <c>ToString()</c> on each dictionary key object. Two entries whose keys are distinct
    /// objects but return the same string from <c>ToString()</c> will collide.
    /// </para>
    /// <para>
    /// Keys are compared using <see cref="System.StringComparer.Ordinal"/>, matching the
    /// deserialiser's key-lookup comparer. Keys that differ only in casing
    /// (e.g. <c>"Foo"</c> vs <c>"foo"</c>) are treated as distinct.
    /// </para>
    /// <para>
    /// Each nested dictionary has its own independent seen-key set; a key present in an
    /// outer dictionary does not collide with a key of the same name in a nested dictionary.
    /// </para>
    /// <para>
    /// Note: this check covers the multiline dictionary path (<c>SerializeDictionaryBody</c>)
    /// only. Inline dictionaries emitted via <c>CollectionFormat.Inline</c> are not checked.
    /// </para>
    /// <para>
    /// Defaults to <c>false</c> to preserve existing behaviour. Set to <c>true</c> in strict
    /// serialisation pipelines to catch write/read asymmetry early: the HUML deserialiser
    /// already rejects duplicate keys at parse time, so silently emitting them creates
    /// documents that cannot be round-tripped.
    /// </para>
    /// </remarks>
    public bool ValidateDuplicateKeysOnWrite { get; init; }

    /// <summary>
    /// A read-only list of <see cref="Serialization.HumlConverter"/> instances consulted during
    /// serialisation and deserialisation when no property-level or type-level
    /// <see cref="Serialization.HumlConverterAttribute"/> is present. The first converter whose
    /// <see cref="Serialization.HumlConverter.CanConvert"/> returns <c>true</c> for a given type is used.
    /// </summary>
    /// <remarks>
    /// Assign a <see cref="List{T}"/> or array at construction time via the object-initialiser
    /// syntax: <c>Converters = new List&lt;HumlConverter&gt; { myConverter }</c>.
    /// The property is read-only after construction — mutation after first use produces
    /// non-deterministic results because converter resolution results are cached.
    /// </remarks>
    public IReadOnlyList<Serialization.HumlConverter> Converters { get; init; }
        = Array.Empty<Serialization.HumlConverter>();

    // Internal per-instance cache: Type → resolved converter (or null = none found).
    // Populated lazily by ConverterCache.TryGet; GC'd with this HumlOptions instance.
    internal readonly ConcurrentDictionary<Type, HumlConverter?> ConverterResolutionCache = new();

    /// <summary>
    /// An optional resolver that provides pre-computed type metadata for HUML (de)serialisation,
    /// bypassing reflection for registered types. Return <see langword="null"/> to use the default
    /// reflection path.
    /// </summary>
    /// <remarks>
    /// Set this property on a <see cref="HumlOptions"/> instance to register a custom
    /// <see cref="Serialization.IHumlTypeInfoResolver"/>. When the resolver returns
    /// <see langword="null"/> for a type, the built-in reflection path is used as a fallback.
    /// In the current release the resolver result is not yet consumed by the deserialiser or
    /// serialiser — this property wires the call site for future source-generator support.
    /// </remarks>
    public Serialization.IHumlTypeInfoResolver? TypeInfoResolver { get; init; }

    private bool _isReadOnly;

    /// <summary>
    /// Gets a value indicating whether this <see cref="HumlOptions"/> instance has been locked
    /// against further mutation. Pre-built instances (<see cref="Default"/>,
    /// <see cref="LatestSupported"/>, <see cref="AutoDetect"/>) are read-only from the moment
    /// the type is first accessed.
    /// </summary>
    public bool IsReadOnly => _isReadOnly;

    /// <summary>
    /// Marks this instance as read-only. Subsequent calls to any future mutable setter will
    /// throw <see cref="InvalidOperationException"/>. This call is idempotent.
    /// </summary>
    public void MakeReadOnly() => _isReadOnly = true;

    /// <summary>
    /// Throws <see cref="InvalidOperationException"/> if <see cref="IsReadOnly"/> is
    /// <see langword="true"/>. Called by mutable setters to enforce the read-only contract.
    /// </summary>
    internal void ThrowIfReadOnly()
    {
        if (_isReadOnly)
            throw new InvalidOperationException(
                "HumlOptions instance is read-only and cannot be modified. " +
                "Create a new HumlOptions instance instead.");
    }

    private int _maxRecursionDepth = 64;

    /// <summary>
    /// Maximum recursion depth allowed during parsing. Exceeding this limit throws
    /// <see cref="T:Huml.Net.Exceptions.HumlParseException"/> instead of risking an unrecoverable
    /// <see cref="StackOverflowException"/>. Default is 64. Valid range: [1, 1024].
    /// </summary>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown if value is less than 1 or greater than 1024.
    /// </exception>
    public int MaxRecursionDepth
    {
        get => _maxRecursionDepth;
        init
        {
            if (value < 1 || value > 1024)
#pragma warning disable MA0015 // nameof convention — init accessor uses 'value' but property name is more informative
                throw new ArgumentOutOfRangeException(nameof(MaxRecursionDepth), value,
                    "MaxRecursionDepth must be between 1 and 1024 inclusive.");
#pragma warning restore MA0015
            _maxRecursionDepth = value;
        }
    }
}
