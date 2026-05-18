using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;

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
        Dictionary<string, PropertyDescriptor> ByKey);

    private static readonly ConcurrentDictionary<(Type, HumlNamingPolicy?), PropertyDescriptorCache> Cache = new();

    // ── Public API ────────────────────────────────────────────────────────────

    /// <summary>
    /// Returns the cached array of <see cref="PropertyDescriptor"/> entries for <paramref name="type"/>.
    /// Properties are ordered base-class-first, then by declaration order within each type.
    /// </summary>
    [RequiresUnreferencedCode("Reflection-based property metadata construction.")]
    internal static PropertyDescriptor[] GetDescriptors(Type type, HumlNamingPolicy? policy = null) =>
        Cache.GetOrAdd((type, policy), static key => BuildDescriptors(key.Item1, key.Item2)).Ordered;

    /// <summary>
    /// Returns the cached dictionary of <see cref="PropertyDescriptor"/> entries for
    /// <paramref name="type"/>, keyed by <see cref="HumlKey"/> with ordinal comparison.
    /// Used by the deserialiser for O(1) key lookup.
    /// </summary>
    [RequiresUnreferencedCode("Reflection-based property metadata construction.")]
    internal static Dictionary<string, PropertyDescriptor> GetLookup(Type type, HumlNamingPolicy? policy = null) =>
        Cache.GetOrAdd((type, policy), static key => BuildDescriptors(key.Item1, key.Item2)).ByKey;

    /// <summary>
    /// Clears the descriptor cache. Intended for use in test isolation only.
    /// </summary>
    internal static void ClearCache() => Cache.Clear();

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
                    isInitOnly, defaultValue, inline, converter));
            }
        }

        var ordered = result.ToArray();

        // Build the keyed dictionary for O(1) deserialiser lookup.
        // last-write-wins on duplicate HumlKey — duplicate keys are an application-level misuse.
        var byKey = new Dictionary<string, PropertyDescriptor>(ordered.Length, StringComparer.Ordinal);
        foreach (var d in ordered)
            byKey[d.HumlKey] = d;

        return new PropertyDescriptorCache(ordered, byKey);
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
}
