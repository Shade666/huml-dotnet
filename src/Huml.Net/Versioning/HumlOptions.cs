using System.Collections.Concurrent;
using Huml.Net.Serialization;

namespace Huml.Net.Versioning;

/// <summary>Configuration options for HUML parsing and serialisation.</summary>
/// <remarks>
/// <para>
/// <b>Document size:</b> Huml.Net does not enforce a maximum document size. There is no
/// built-in limit on the number of bytes or characters in an input document beyond
/// <see cref="MaxRecursionDepth"/>. When parsing untrusted input, enforce size constraints
/// in the caller before passing the document to any <c>Huml.*</c> method.
/// </para>
/// </remarks>
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
        LatestSupportedAutoDetect.MakeReadOnly();
        Strict.MakeReadOnly();
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

    /// <summary>
    /// Options that read the <c>%HUML</c> header and fall back to the latest supported spec
    /// version (<see cref="HumlSpecVersion.V0_2"/>) when the declared version is unknown or
    /// outside the support window. Unlike <see cref="Default"/> and <see cref="AutoDetect"/>,
    /// this preset never throws <see cref="Exceptions.HumlUnsupportedVersionException"/> —
    /// unsupported versions are silently treated as the latest supported version.
    /// Use when consuming documents from heterogeneous sources where version drift is expected.
    /// </summary>
    public static readonly HumlOptions LatestSupportedAutoDetect = new()
    {
        VersionSource = VersionSource.Header,
        UnknownVersionBehaviour = UnknownVersionBehaviour.UseLatest,
    };

    /// <summary>
    /// Maximum-strictness preset. Enables every validation toggle:
    /// <list type="bullet">
    /// <item>Reads the <c>%HUML</c> version header and throws
    ///   <see cref="Exceptions.HumlUnsupportedVersionException"/> for unknown versions.</item>
    /// <item>Throws <see cref="Exceptions.HumlDeserializeException"/> for any HUML key that
    ///   does not map to a target property (see <see cref="UnmappedMemberHandling"/>).</item>
    /// <item>Throws <see cref="Exceptions.HumlSerializeException"/> for duplicate dictionary
    ///   keys during serialisation (see <see cref="ValidateDuplicateKeysOnWrite"/>).</item>
    /// </list>
    /// Required-property enforcement via <c>[HumlRequired]</c> is unconditional and is always
    /// active regardless of this preset. Mirrors the STJ .NET 10
    /// <c>JsonSerializerOptions.Strict</c> pattern.
    /// </summary>
    public static readonly HumlOptions Strict = new()
    {
        VersionSource = VersionSource.Header,
        UnknownVersionBehaviour = UnknownVersionBehaviour.Throw,
        UnmappedMemberHandling = UnmappedMemberHandling.Disallow,
        ValidateDuplicateKeysOnWrite = true,
    };

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
    /// Controls how numeric values are handled during serialisation and deserialisation.
    /// </summary>
    /// <remarks>
    /// <para>
    /// When <see cref="HumlNumberHandling.AllowReadingFromString"/> is set, a
    /// <c>ScalarKind.String</c> scalar (e.g. a quoted HUML value such as <c>"42"</c>) may be
    /// coerced to a numeric target type during deserialisation. Without this flag, assigning a
    /// quoted string to a numeric property throws
    /// <see cref="T:Huml.Net.Exceptions.HumlDeserializeException"/>.
    /// </para>
    /// <para>
    /// When <see cref="HumlNumberHandling.WriteAsString"/> is set, finite numeric values
    /// (integers, <c>float</c>, <c>double</c>, <c>decimal</c>) are emitted as quoted HUML
    /// strings rather than bare numeric literals. <c>NaN</c>, <c>+inf</c>, and <c>-inf</c>
    /// are always emitted unquoted regardless of this setting — they are HUML native scalar
    /// kinds.
    /// </para>
    /// <para>
    /// Combining <see cref="HumlNumberHandling.WriteAsString"/> and
    /// <see cref="HumlNumberHandling.AllowReadingFromString"/> produces a round-trip-safe
    /// configuration.
    /// </para>
    /// </remarks>
    public HumlNumberHandling NumberHandling { get; init; } = HumlNumberHandling.Strict;

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
    /// Controls how the deserialiser handles HUML keys that do not map to any property on the
    /// target type and are not captured by a <c>[HumlExtensionData]</c> property.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <see cref="UnmappedMemberHandling.Skip"/> (the default) silently ignores unknown keys,
    /// preserving forward-compatibility with documents produced by newer HUML writers.
    /// </para>
    /// <para>
    /// <see cref="UnmappedMemberHandling.Disallow"/> throws
    /// <see cref="T:Huml.Net.Exceptions.HumlDeserializeException"/> listing the unrecognised
    /// key. If the type has a <c>[HumlExtensionData]</c> property, unknown keys are routed
    /// there and the exception is suppressed — extension data is an explicit opt-in for unknown
    /// keys and takes precedence over <c>Disallow</c>.
    /// </para>
    /// </remarks>
    public UnmappedMemberHandling UnmappedMemberHandling { get; init; } = UnmappedMemberHandling.Skip;

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
    private IReadOnlyList<Serialization.HumlConverter>? _frozenConverters;

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
    public void MakeReadOnly()
    {
        if (_isReadOnly) return;
        Volatile.Write(ref _frozenConverters, Array.AsReadOnly(Converters.ToArray()));
        _isReadOnly = true;
    }

    /// <summary>
    /// Returns the frozen converter list after <see cref="MakeReadOnly"/> has been called,
    /// or the live <see cref="Converters"/> list when the instance has not yet been frozen.
    /// Internal callers in the serialisation hot path should always read this property rather
    /// than <see cref="Converters"/> directly so that post-freeze mutations to the original
    /// list reference have no effect.
    /// </summary>
    internal IReadOnlyList<Serialization.HumlConverter> EffectiveConverters
        => Volatile.Read(ref _frozenConverters) ?? Converters;

    /// <summary>
    /// Clears the converter resolution caches on all pre-built static instances.
    /// Call this in test constructors alongside <c>ConverterCache.ClearCache()</c> to prevent
    /// stale cached converter resolutions from leaking between tests.
    /// </summary>
    internal static void ClearOptionsCaches()
    {
        Default.ConverterResolutionCache.Clear();
        LatestSupported.ConverterResolutionCache.Clear();
        LatestSupportedAutoDetect.ConverterResolutionCache.Clear();
        Strict.ConverterResolutionCache.Clear();
    }

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
