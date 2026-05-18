using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using Huml.Net.Parser;

namespace Huml.Net.Serialization;

/// <summary>
/// Per-type cached property metadata used by the serialiser and deserialiser.
/// </summary>
/// <remarks>
/// Properties are ordered base-class-first within each type, then by <c>MetadataToken</c>
/// (declaration order). Properties decorated with <see cref="HumlIgnoreAttribute"/> are excluded.
/// <see cref="HumlPropertyAttribute"/> name overrides and <c>OmitIfDefault</c> flags are resolved once at
/// build time and cached.
/// <c>ClassIgnoresDefaults</c> is <c>true</c> when the declaring type carries
/// <c>[HumlIgnoreDefaults]</c> (directly or via inheritance); it is resolved once per
/// declaring type during <c>BuildDescriptors</c> and cached alongside all other metadata.
/// </remarks>
internal sealed record PropertyDescriptor(
    string HumlKey,
    PropertyInfo Property,
    bool OmitIfDefault,
    bool ClassIgnoresDefaults,   // cached from [HumlIgnoreDefaults] on declaring type
    bool IsInitOnly,
    bool IsRequired,             // true when [HumlRequired] or C# required modifier is detected
    object? DefaultValue,
    bool? Inline,
    HumlConverter? Converter)   // property-level [HumlConverter] resolved at cache-build time
{
    // ── Cache ─────────────────────────────────────────────────────────────────

    /// <summary>
    /// Pairs the ordered array (for serialiser declaration-order traversal) with the keyed
    /// dictionary (for deserialiser O(1) lookup) in a single
    /// <see cref="System.Collections.Concurrent.ConcurrentDictionary{TKey,TValue}"/> entry.
    /// </summary>
    private sealed record PropertyDescriptorCache(
        PropertyDescriptor[] Ordered,
        Dictionary<string, PropertyDescriptor> ByKey,
        PropertyDescriptor? ExtensionDataDescriptor,
        ConstructorInfo? SelectedConstructor,     // null = parameterless ctor path (D-09)
        bool HasAmbiguousConstructors);           // set true when ambiguous — throw at deserialise time, not here

    private static readonly ConcurrentDictionary<(Type, HumlNamingPolicy?), PropertyDescriptorCache> Cache = new();

    // ── Public API ────────────────────────────────────────────────────────────

    /// <summary>
    /// Returns the cached array of <see cref="PropertyDescriptor"/> entries for <paramref name="type"/>.
    /// Properties are ordered base-class-first, then by declaration order within each type.
    /// </summary>
    [RequiresUnreferencedCode("Reflection-based property metadata construction.")]
    internal static PropertyDescriptor[] GetDescriptors(Type type, HumlNamingPolicy? policy = null) =>
        GetCache(type, policy).Ordered;

    /// <summary>
    /// Returns the cached dictionary of <see cref="PropertyDescriptor"/> entries for
    /// <paramref name="type"/>, keyed by <see cref="HumlKey"/> with ordinal comparison.
    /// Used by the deserialiser for O(1) key lookup.
    /// </summary>
    [RequiresUnreferencedCode("Reflection-based property metadata construction.")]
    internal static Dictionary<string, PropertyDescriptor> GetLookup(Type type, HumlNamingPolicy? policy = null) =>
        GetCache(type, policy).ByKey;

    /// <summary>
    /// Returns the cached <see cref="PropertyDescriptor"/> for the property marked with
    /// <see cref="HumlExtensionDataAttribute"/> on <paramref name="type"/>, or <c>null</c>
    /// if no such property exists.
    /// </summary>
    [RequiresUnreferencedCode("Reflection-based property metadata construction.")]
    internal static PropertyDescriptor? GetExtensionDataDescriptor(Type type, HumlNamingPolicy? policy = null) =>
        GetCache(type, policy).ExtensionDataDescriptor;

    /// <summary>
    /// Returns the constructor selected for HUML deserialisation of <paramref name="type"/>,
    /// or <c>null</c> if the parameterless constructor path is used.
    /// </summary>
    [RequiresUnreferencedCode("Reflection-based property metadata construction.")]
    internal static ConstructorInfo? GetSelectedConstructor(Type type, HumlNamingPolicy? policy = null) =>
        GetCache(type, policy).SelectedConstructor;

    /// <summary>
    /// Returns <c>true</c> when <paramref name="type"/> has multiple public non-parameterless
    /// constructors and no <see cref="HumlConstructorAttribute"/> disambiguates them, or when
    /// multiple constructors carry <see cref="HumlConstructorAttribute"/>. The deserialiser
    /// raises <see cref="Exceptions.HumlDeserializeException"/> in this case.
    /// </summary>
    [RequiresUnreferencedCode("Reflection-based property metadata construction.")]
    internal static bool GetHasAmbiguousConstructors(Type type, HumlNamingPolicy? policy = null) =>
        GetCache(type, policy).HasAmbiguousConstructors;

    /// <summary>
    /// Clears the descriptor cache. Intended for use in test isolation only.
    /// </summary>
    internal static void ClearCache() => Cache.Clear();

    [RequiresUnreferencedCode("Reflection-based property metadata construction.")]
    private static PropertyDescriptorCache GetCache(Type type, HumlNamingPolicy? policy) =>
        Cache.GetOrAdd((type, policy), static key => BuildDescriptors(key.Item1, key.Item2));

    // ── Private implementation ────────────────────────────────────────────────

    [RequiresUnreferencedCode("Reflection-based property metadata construction.")]
    private static PropertyDescriptorCache BuildDescriptors(Type type, HumlNamingPolicy? policy)
    {
        // Walk the inheritance chain from root to derived, collecting types in order.
        var typeChain = new List<Type>();
        var current = type;
        while (current != null && current != typeof(object))
        {
            typeChain.Insert(0, current); // prepend so base comes first
            current = current.BaseType;
        }

        var result = new List<PropertyDescriptor>();

        foreach (var t in typeChain)
        {
            // Scan [HumlIgnoreDefaults] once per declaring type (inherit:true so a type decorated
            // at a derived level propagates correctly when t IS the decorated type).
            // Per D-07: scan t, not type — prevents base-type properties from incorrectly
            // inheriting an attribute placed only on the derived type.
            bool classIgnoresDefaults = t.GetCustomAttribute<HumlIgnoreDefaultsAttribute>(inherit: true) != null;

            // DeclaredOnly: each type contributes its own properties only.
            // Sort by MetadataToken to get declaration order within this type.
            var props = t.GetProperties(
                BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly);

            Array.Sort(props, (a, b) => a.MetadataToken.CompareTo(b.MetadataToken));

            foreach (var prop in props)
            {
                // Exclude [HumlIgnore] properties
                if (prop.GetCustomAttribute<HumlIgnoreAttribute>() != null)
                    continue;

                // Exclude [HumlExtensionData] properties — handled separately in the extension scan below.
                // They must not appear in Ordered[] or ByKey.
                if (prop.GetCustomAttribute<HumlExtensionDataAttribute>() != null)
                    continue;

                // Resolve [HumlProperty] name and OmitIfDefault
                var humlProp = prop.GetCustomAttribute<HumlPropertyAttribute>();
                string humlKey = (humlProp?.Name is { Length: > 0 } explicitName)
                    ? explicitName                                        // [HumlProperty] explicit name WINS — policy never applied
                    : (policy?.ConvertName(prop.Name) ?? prop.Name);     // policy or identity
                bool omitIfDefault = humlProp?.OmitIfDefault ?? false;
                bool? inline = humlProp?.Inline switch
                {
                    InlineMode.Inline    => true,
                    InlineMode.Multiline => false,
                    _                   => null,
                };

                // Detect init-only setter via IsExternalInit custom modifier
                bool isInitOnly = DetectInitOnly(prop);

                // Detect required property — [HumlRequired] attribute or C# required modifier
                bool isRequired = DetectRequired(prop);

                // Always compute DefaultValue so ClassIgnoresDefaults and DefaultIgnoreCondition
                // can check it at emit time without a second reflection call (per D-06).
                object? defaultValue = prop.PropertyType.IsValueType
                    ? Activator.CreateInstance(prop.PropertyType)
                    : null;

                // Resolve property-level [HumlConverter] attribute
                var converterAttr = prop.GetCustomAttribute<HumlConverterAttribute>();
                HumlConverter? converter = null;
                if (converterAttr != null)
                {
                    object? instance;
                    try
                    {
                        instance = Activator.CreateInstance(converterAttr.ConverterType);
                    }
                    catch (MissingMethodException)
                    {
                        throw new InvalidOperationException(
                            $"Converter type '{converterAttr.ConverterType.Name}' has no accessible " +
                            "parameterless constructor.");
                    }
                    converter = instance as HumlConverter
                        ?? throw new InvalidOperationException(
                            $"Converter type '{converterAttr.ConverterType.Name}' does not derive from HumlConverter.");
                }

                result.Add(new PropertyDescriptor(
                    humlKey, prop, omitIfDefault, classIgnoresDefaults,
                    isInitOnly, isRequired, defaultValue, inline, converter));
            }
        }

        var ordered = result.ToArray();

        // Build the keyed dictionary for O(1) deserialiser lookup.
        // last-write-wins on duplicate HumlKey — duplicate keys are an application-level misuse.
        var byKey = new Dictionary<string, PropertyDescriptor>(ordered.Length, StringComparer.Ordinal);
        foreach (var d in ordered)
            byKey[d.HumlKey] = d;

        // Scan for [HumlExtensionData] — must be exactly one; type must be a supported concrete dict.
        PropertyDescriptor? extensionDataDescriptor = null;
        string? firstExtPropName = null;

        foreach (var t in typeChain)
        {
            var extProps = t.GetProperties(
                BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly);
            Array.Sort(extProps, (a, b) => a.MetadataToken.CompareTo(b.MetadataToken));

            foreach (var prop in extProps)
            {
                if (prop.GetCustomAttribute<HumlExtensionDataAttribute>() == null)
                    continue;

                // Validate supported type (exact closed-generic equality).
                var pt = prop.PropertyType;
                bool isNodeDict   = pt == typeof(Dictionary<string, HumlNode>);
                bool isObjectDict = pt == typeof(Dictionary<string, object?>);
                if (!isNodeDict && !isObjectDict)
                    throw new InvalidOperationException(
                        $"[HumlExtensionData] on '{type.Name}.{prop.Name}': property type must be " +
                        $"Dictionary<string, HumlNode> or Dictionary<string, object?> — " +
                        $"'{pt.Name}' is not supported.");

                // Validate init-only — SetValue after construction would throw FieldAccessException.
                if (DetectInitOnly(prop))
                    throw new InvalidOperationException(
                        $"[HumlExtensionData] on '{type.Name}.{prop.Name}' has an init-only setter. " +
                        "Extension data properties must have a regular (non-init) setter.");

                // Validate no public setter missing entirely.
                if (prop.GetSetMethod(nonPublic: false) == null)
                    throw new InvalidOperationException(
                        $"[HumlExtensionData] on '{type.Name}.{prop.Name}' has no public setter. " +
                        "Extension data properties require a public, non-init setter.");

                // Validate uniqueness — at most one [HumlExtensionData] per type hierarchy.
                if (extensionDataDescriptor != null)
                    throw new InvalidOperationException(
                        $"Type '{type.Name}' declares [HumlExtensionData] on both " +
                        $"'{firstExtPropName}' and '{prop.Name}'. " +
                        "Only one [HumlExtensionData] property is permitted per type.");

                // Build descriptor for the extension-data slot.
                // NOT added to result — must not appear in Ordered[] or ByKey.
                extensionDataDescriptor = new PropertyDescriptor(
                    HumlKey: prop.Name,
                    Property: prop,
                    OmitIfDefault: false,
                    ClassIgnoresDefaults: false,
                    IsInitOnly: false,
                    IsRequired: false,
                    DefaultValue: null,
                    Inline: null,
                    Converter: null);
                firstExtPropName = prop.Name;
            }
        }

        // Constructor selection — D-02 priority order (stored for deserialise path; not used by serialiser).
        // IMPORTANT: never throw here; BuildDescriptors runs for serialize + populate too. Set
        // HasAmbiguousConstructors = true and leave SelectedConstructor = null; the deserialiser checks and throws.
        var ctors = type.GetConstructors(BindingFlags.Public | BindingFlags.Instance); // D-03: public only

        var annotated = new List<ConstructorInfo>();
        foreach (var c in ctors)
            if (c.GetCustomAttribute<HumlConstructorAttribute>() != null)
                annotated.Add(c);

        ConstructorInfo? selectedConstructor = null;
        bool hasAmbiguousConstructors = false;

        if (annotated.Count > 1)
        {
            hasAmbiguousConstructors = true; // multiple [HumlConstructor] — error at deserialise time
        }
        else if (annotated.Count == 1)
        {
            selectedConstructor = annotated[0]; // D-02 priority 1
        }
        else
        {
            // D-02 priority 2: single non-parameterless public constructor
            var nonParameterless = new List<ConstructorInfo>();
            foreach (var c in ctors)
                if (c.GetParameters().Length > 0)
                    nonParameterless.Add(c);

            if (nonParameterless.Count == 1)
                selectedConstructor = nonParameterless[0];
            else if (nonParameterless.Count > 1)
                hasAmbiguousConstructors = true;
            // else: no non-parameterless ctors — parameterless path, selectedConstructor stays null
        }

        return new PropertyDescriptorCache(ordered, byKey, extensionDataDescriptor, selectedConstructor, hasAmbiguousConstructors);
    }

    /// <summary>
    /// Returns <c>true</c> if <paramref name="property"/> has an <c>init</c>-only setter.
    /// Detection is based on the <c>IsExternalInit</c> required custom modifier on the setter's
    /// return parameter — the same mechanism the C# compiler uses.
    /// </summary>
    [RequiresUnreferencedCode("Reflection-based property metadata construction.")]
    private static bool DetectInitOnly(PropertyInfo property)
    {
        var setMethod = property.GetSetMethod(nonPublic: false);
        if (setMethod == null)
            return false;

        var modifiers = setMethod.ReturnParameter.GetRequiredCustomModifiers();
        foreach (var m in modifiers)
        {
            if (string.Equals(m.FullName, "System.Runtime.CompilerServices.IsExternalInit", StringComparison.Ordinal))
                return true;
        }

        return false;
    }

    /// <summary>
    /// Returns <c>true</c> if <paramref name="property"/> is marked as required via the
    /// <see cref="HumlRequiredAttribute"/> attribute or the C# <c>required</c> modifier
    /// (detected via <c>RequiredMemberAttribute</c>, BCL on .NET 7+, shimmed for older TFMs).
    /// </summary>
    [RequiresUnreferencedCode("Reflection-based property metadata construction.")]
    private static bool DetectRequired(PropertyInfo property)
    {
        // [HumlRequired] attribute check
        if (property.GetCustomAttribute<HumlRequiredAttribute>() != null)
            return true;

        // C# required modifier — RequiredMemberAttribute (BCL in .NET 7+; shimmed for older TFMs).
        // FullName string-match avoids compile-time coupling to the shim vs BCL type.
        foreach (var attr in property.GetCustomAttributes(inherit: false))
            if (string.Equals(attr.GetType().FullName,
                "System.Runtime.CompilerServices.RequiredMemberAttribute",
                StringComparison.Ordinal))
                return true;

        return false;
    }
}
