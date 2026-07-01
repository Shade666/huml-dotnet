using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using Huml.Net.Parser;
using Huml.Net.Versioning;

namespace Huml.Net.Serialization;

/// <summary>
/// Resolves and caches the effective <see cref="HumlConverter"/> for a given (Type, options)
/// pair using three-level priority: property-level attribute (handled via PropertyDescriptor),
/// then type-level attribute, then HumlOptions.Converters. A matched <see cref="HumlConverterFactory"/>
/// is resolved via <see cref="HumlConverterFactory.CreateConverter"/> before the result is cached.
/// When no converter matches a requested <c>Nullable&lt;U&gt;</c> type directly, a converter
/// registered for the underlying type <c>U</c> is automatically wrapped so it also serves <c>U?</c>
/// (mirrors System.Text.Json's <c>NullableConverterFactory</c> behaviour).
/// </summary>
internal static class ConverterCache
{
    // Converter instance cache: converterType → single shared instance (converters are stateless).
    private static readonly ConcurrentDictionary<Type, HumlConverter> InstanceCache = new();

    /// <summary>
    /// Returns the effective converter for <paramref name="targetType"/> from type-level
    /// [HumlConverter] attribute, the options Converters list, or a <c>Nullable&lt;U&gt;</c>-unwrapped
    /// converter for the underlying type, or <c>null</c> if none apply. Property-level converters
    /// are already resolved in PropertyDescriptor.Converter and must be checked by callers before
    /// invoking this method.
    /// </summary>
    [RequiresUnreferencedCode("Reflection-based converter resolution.")]
    internal static HumlConverter? TryGet(Type targetType, HumlOptions options)
    {
        return options.ConverterResolutionCache.GetOrAdd(targetType, static (t, opts) =>
        {
            var direct = TryResolveDirect(t, opts);
            if (direct != null) return direct;

            // Nullable<U> auto-unwrap: nothing matched t itself — try wrapping a converter for U.
            // Nullable<T> cannot be nested (Nullable<Nullable<U>> is not a valid CLR type), so this
            // recursive TryGet call cannot recurse further via this branch.
            var underlying = Nullable.GetUnderlyingType(t);
            if (underlying != null)
            {
                var inner = TryGet(underlying, opts);
                if (inner != null) return new NullableConverterAdapter(inner, underlying);
            }

            return null;
        }, options);
    }

    // Level 2 (type-level [HumlConverter] attribute) then Level 3 (HumlOptions.EffectiveConverters).
    // A HumlConverterFactory match that declines (returns null from CreateConverter) falls through
    // to the next candidate rather than short-circuiting resolution.
    [RequiresUnreferencedCode("Reflection-based converter resolution.")]
    private static HumlConverter? TryResolveDirect(Type t, HumlOptions opts)
    {
        var typeAttr = t.GetCustomAttribute<HumlConverterAttribute>();
        if (typeAttr != null)
        {
            var candidate = GetOrCreate(typeAttr.ConverterType);
            var resolved = candidate is HumlConverterFactory typeFactory
                ? typeFactory.CreateConverter(t, opts)
                : candidate;
            if (resolved != null) return resolved;
        }

        foreach (var c in opts.EffectiveConverters)
        {
            if (!c.CanConvert(t)) continue;

            if (c is HumlConverterFactory factory)
            {
                var created = factory.CreateConverter(t, opts);
                if (created != null) return created;
                continue; // factory declined — keep scanning remaining converters
            }

            return c;
        }

        return null;
    }

    /// <summary>Clears the converter-instance cache. Use in test teardown for isolation.</summary>
    internal static void ClearCache()
    {
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

    /// <summary>
    /// Wraps a converter registered for the underlying type <c>U</c> of a <c>Nullable&lt;U&gt;</c>
    /// so it transparently serves <c>U?</c>: a <c>null</c> HUML scalar short-circuits to
    /// <c>null</c> without invoking the inner converter (whose <c>Read</c>/<c>Write</c> were
    /// written for the non-nullable <c>U</c> and are not expected to handle null themselves);
    /// any other value delegates straight through.
    /// </summary>
    private sealed class NullableConverterAdapter(HumlConverter inner, Type underlyingType) : HumlConverter
    {
        public override bool CanConvert(Type typeToConvert) =>
            Nullable.GetUnderlyingType(typeToConvert) == underlyingType;

        internal override object? ReadObject(HumlNode node) =>
            node is HumlScalar { Kind: ScalarKind.Null } ? null : inner.ReadObject(node);

        internal override void WriteObject(HumlWriterContext context, object? value)
        {
            if (value is null)
            {
                context.AppendRaw("null");
                return;
            }
            inner.WriteObject(context, value);
        }
    }
}
