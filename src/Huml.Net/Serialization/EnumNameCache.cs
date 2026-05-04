using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;

namespace Huml.Net.Serialization;

/// <summary>
/// Bidirectional cache mapping enum members to their HUML string names and back.
/// Keyed by (enum type, naming policy) pair — the same cache-key strategy as
/// <see cref="PropertyDescriptor"/>.
/// </summary>
internal static class EnumNameCache
{
    private sealed record EnumNameEntry(
        Dictionary<object, string> ToHuml,
        Dictionary<string, object> FromHuml,
        Dictionary<string, object> FromHumlCI,
        bool IsFlags);

    private static readonly ConcurrentDictionary<(Type, HumlNamingPolicy?), EnumNameEntry> Cache = new();

    /// <summary>
    /// Returns the HUML name for <paramref name="value"/> in <paramref name="enumType"/>.
    /// Checks <see cref="HumlEnumValueAttribute"/> first, then applies <paramref name="policy"/>
    /// or identity. Throws <see cref="Exceptions.HumlSerializeException"/> for undefined numeric
    /// values (CR-01) and for unnamed <see cref="FlagsAttribute"/> combinations.
    /// </summary>
    [RequiresUnreferencedCode("Reflection-based enum name cache construction.")]
    internal static string GetName(Type enumType, object value, HumlNamingPolicy? policy)
    {
        var entry = GetOrBuild(enumType, policy);
        if (entry.ToHuml.TryGetValue(value, out var name))
            return name;

        // Unnamed value — [Flags] or undefined numeric
        if (entry.IsFlags)
            throw new Exceptions.HumlSerializeException(
                $"Cannot serialize [Flags] enum combination '{value}' of type '{enumType.Name}': " +
                "only single named members are supported. Unnamed bit combinations are not serialisable.");

        // Undefined numeric value (e.g. (Status)99) — no fallback; broken round-trip (CR-01)
        throw new Exceptions.HumlSerializeException(
            $"Cannot serialize undefined enum value '{value}' of type '{enumType.Name}': " +
            "the value has no declared member name.");
    }

    /// <summary>
    /// Attempts to find the enum value for <paramref name="humlName"/> in <paramref name="enumType"/>.
    /// Performs an exact ordinal match first, then an OrdinalIgnoreCase fallback.
    /// </summary>
    /// <returns><c>true</c> if a matching member was found; <c>false</c> otherwise.</returns>
    /// <remarks>
    /// When this method returns <c>false</c>, the value of <paramref name="result"/> is undefined
    /// and must not be used by the caller.
    /// </remarks>
    [RequiresUnreferencedCode("Reflection-based enum name cache construction.")]
    internal static bool TryParse(Type enumType, string humlName, HumlNamingPolicy? policy, out object result)
    {
        var entry = GetOrBuild(enumType, policy);
        // Exact match first (case-sensitive)
        if (entry.FromHuml.TryGetValue(humlName, out result!))
            return true;
        // Case-insensitive fallback
        if (entry.FromHumlCI.TryGetValue(humlName, out result!))
            return true;
        result = null!;
        return false;
    }

    /// <summary>Clears the cache. Intended for test isolation only.</summary>
    internal static void ClearCache() => Cache.Clear();

    // ── Private implementation ────────────────────────────────────────────────

    [RequiresUnreferencedCode("Reflection-based enum name cache construction.")]
    private static EnumNameEntry GetOrBuild(Type enumType, HumlNamingPolicy? policy)
        => Cache.GetOrAdd((enumType, policy), static key => Build(key.Item1, key.Item2));

    [RequiresUnreferencedCode("Reflection-based enum name cache construction.")]
    private static EnumNameEntry Build(Type enumType, HumlNamingPolicy? policy)
    {
        var toHuml = new Dictionary<object, string>();
        var fromHuml = new Dictionary<string, object>(StringComparer.Ordinal);
        var fromHumlCI = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);

        foreach (var field in enumType.GetFields(BindingFlags.Public | BindingFlags.Static))
        {
            var value = field.GetValue(null)!;
            var attr = field.GetCustomAttribute<HumlEnumValueAttribute>();

            // [HumlEnumValue] wins over naming policy (same precedence as [HumlProperty] over policy)
            string humlName = attr is not null
                ? attr.Name
                : (policy?.ConvertName(field.Name) ?? field.Name);

            toHuml[value] = humlName;
            fromHuml[humlName] = value;
            // TryAdd collision guard (WR-01): two members with case-colliding HUML names are an error
            if (!fromHumlCI.TryAdd(humlName, value))
                throw new InvalidOperationException(
                    $"Enum type '{enumType.Name}' has two members whose HUML names collide " +
                    $"case-insensitively: '{humlName}'. Use [HumlEnumValue] to give them distinct names.");
        }

        // Detect [Flags] once at build time (WR-02) — cached on the entry, not per GetName call
        bool isFlags = enumType.GetCustomAttribute<FlagsAttribute>() != null;

        return new EnumNameEntry(toHuml, fromHuml, fromHumlCI, isFlags);
    }
}
