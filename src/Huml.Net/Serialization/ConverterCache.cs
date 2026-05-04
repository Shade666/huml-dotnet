using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Runtime.CompilerServices;
using Huml.Net.Versioning;

namespace Huml.Net.Serialization;

/// <summary>
/// Resolves and caches the effective <see cref="HumlConverter"/> for a given (Type, options)
/// pair using three-level priority: property-level attribute (handled via PropertyDescriptor),
/// then type-level attribute, then HumlOptions.Converters.
/// </summary>
internal static class ConverterCache
{
    // Full resolution cache: (targetType, options-reference-hash) → resolved converter (or null).
    // Once an options instance produces a result for a type, that result is sticky — aligns with
    // the contract that HumlOptions must not be mutated after first use.
    private static readonly ConcurrentDictionary<(Type, int), HumlConverter?> Cache = new();

    // Converter instance cache: converterType → single shared instance (converters are stateless).
    private static readonly ConcurrentDictionary<Type, HumlConverter> InstanceCache = new();

    /// <summary>
    /// Returns the effective converter for <paramref name="targetType"/> from type-level
    /// [HumlConverter] attribute or the options Converters list, or <c>null</c> if none apply.
    /// Property-level converters are already resolved in PropertyDescriptor.Converter and must
    /// be checked by callers before invoking this method.
    /// </summary>
    [RequiresUnreferencedCode("Reflection-based converter resolution.")]
    internal static HumlConverter? TryGet(Type targetType, HumlOptions options)
    {
        int optionsKey = RuntimeHelpers.GetHashCode(options);
        return Cache.GetOrAdd((targetType, optionsKey), static (k, opts) =>
        {
            // Level 2: type-level [HumlConverter] attribute on the target type
            var typeAttr = k.Item1.GetCustomAttribute<HumlConverterAttribute>();
            if (typeAttr != null)
                return GetOrCreate(typeAttr.ConverterType);

            // Level 3: HumlOptions.Converters — first CanConvert match wins
            foreach (var c in opts.Converters)
                if (c.CanConvert(k.Item1)) return c;

            return null;
        }, options);
    }

    /// <summary>Clears all caches. Use in test teardown for isolation.</summary>
    internal static void ClearCache()
    {
        Cache.Clear();
        InstanceCache.Clear();
    }

    [RequiresUnreferencedCode("Reflection-based converter resolution.")]
    private static HumlConverter GetOrCreate(Type converterType)
        => InstanceCache.GetOrAdd(converterType, static t =>
        {
            object? instance;
            try
            {
                instance = Activator.CreateInstance(t);
            }
            catch (MissingMethodException)
            {
                throw new InvalidOperationException(
                    $"Converter type '{t.Name}' has no accessible parameterless constructor.");
            }
            return instance as HumlConverter
                ?? throw new InvalidOperationException(
                    $"Converter type '{t.Name}' does not derive from HumlConverter.");
        });
}
