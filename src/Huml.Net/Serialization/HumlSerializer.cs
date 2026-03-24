using System.Collections;
using System.Globalization;
using System.Text;
using Huml.Net.Exceptions;
using Huml.Net.Versioning;

namespace Huml.Net.Serialization;

/// <summary>
/// Converts .NET objects to HUML text using the <see cref="PropertyDescriptor"/> cache
/// for property enumeration in declaration order.
/// </summary>
internal static class HumlSerializer
{
    // ── Public entry points ───────────────────────────────────────────────────

    /// <summary>
    /// Serializes <paramref name="value"/> to a HUML-formatted string.
    /// </summary>
    /// <param name="value">The object to serialize. May be <c>null</c>.</param>
    /// <param name="options">Serialization options. Defaults to <see cref="HumlOptions.Default"/>.</param>
    /// <returns>The HUML text representation.</returns>
    internal static string Serialize(object? value, HumlOptions? options = null)
    {
        options ??= HumlOptions.Default;
        var sb = new StringBuilder();
        sb.Append('%');
        sb.Append("HUML ");
        sb.Append(VersionString(options.SpecVersion));
        sb.Append('\n');

        if (value is null)
        {
            sb.Append("null\n");
        }
        else
        {
            SerializeValue(sb, value, depth: 0, options);
        }

        return sb.ToString();
    }

    /// <summary>
    /// Typed overload — serializes <paramref name="value"/> using <paramref name="type"/> as
    /// the declared type for property reflection. Used by the Phase 7 static entry point
    /// (<c>Huml.Serialize&lt;T&gt;</c>). Nested POCOs still use their runtime type.
    /// </summary>
    internal static string Serialize(object? value, Type type, HumlOptions? options = null)
    {
        options ??= HumlOptions.Default;
        var sb = new StringBuilder();
        sb.Append('%');
        sb.Append("HUML ");
        sb.Append(VersionString(options.SpecVersion));
        sb.Append('\n');

        if (value is null)
        {
            sb.Append("null\n");
        }
        else
        {
            SerializeValue(sb, value, depth: 0, options, declaredType: type);
        }

        return sb.ToString();
    }

    // ── Core serialization logic ──────────────────────────────────────────────

    private static void SerializeValue(StringBuilder sb, object? value, int depth, HumlOptions options, Type? declaredType = null)
    {
        if (value is null)
        {
            sb.Append("null");
            return;
        }

        // string first — must precede IEnumerable since string is enumerable
        if (value is string str)
        {
            sb.Append('"');
            AppendEscapedString(sb, str);
            sb.Append('"');
            return;
        }

        if (value is bool b)
        {
            sb.Append(b ? "true" : "false");
            return;
        }

        // Integer types — emit bare literal
        if (IsIntegerType(value))
        {
            sb.Append(((IFormattable)value).ToString(null, CultureInfo.InvariantCulture));
            return;
        }

        // Floating-point types
        if (value is double d)
        {
            sb.Append(FormatDouble(d));
            return;
        }

        if (value is float f)
        {
            if (float.IsNaN(f))
            {
                sb.Append("nan");
                return;
            }
            if (float.IsPositiveInfinity(f))
            {
                sb.Append("+inf");
                return;
            }
            if (float.IsNegativeInfinity(f))
            {
                sb.Append("-inf");
                return;
            }
            sb.Append(f.ToString("G", CultureInfo.InvariantCulture));
            return;
        }

        // decimal
        if (value is decimal dec)
        {
            sb.Append(dec.ToString(CultureInfo.InvariantCulture));
            return;
        }

        // IDictionary<string, *> — must precede IEnumerable
        if (value is IDictionary dict)
        {
            SerializeDictionaryBody(sb, dict, depth, options);
            return;
        }

        // IEnumerable (arrays, lists, etc.)
        if (value is IEnumerable enumerable)
        {
            EmitSequenceItems(sb, enumerable, depth, options);
            return;
        }

        // POCO — check for unsupported types (delegates, pointers, etc.) before reflecting
        var type = value.GetType();
        if (IsUnsupportedType(type))
        {
            throw new HumlSerializeException(
                $"Cannot serialize type '{type.FullName}': delegates, function pointers, and " +
                "similar non-data types are not supported by HumlSerializer.");
        }

        // POCO — reflect using PropertyDescriptor (pass declaredType for top-level type-directed dispatch)
        SerializeMappingBody(sb, value, depth, options, declaredType);
    }

    // ── Mapping (POCO / dictionary-as-mapping) ────────────────────────────────

    /// <summary>
    /// Emits mapping entries at <paramref name="depth"/> for a POCO.
    /// Each property is emitted as either <c>key: scalar\n</c> or <c>key::\n  ...</c>.
    /// </summary>
    private static void SerializeMappingBody(StringBuilder sb, object obj, int depth, HumlOptions options, Type? declaredType = null)
    {
        var descriptors = PropertyDescriptor.GetDescriptors(declaredType ?? obj.GetType());
        var indent = Indent(depth);

        foreach (var desc in descriptors)
        {
            var propValue = desc.Property.GetValue(obj);

            // OmitIfDefault: skip if value equals the type's default (cached in descriptor)
            if (desc.OmitIfDefault && Equals(propValue, desc.DefaultValue))
                continue;

            EmitEntry(sb, indent, desc.HumlKey, propValue, depth, options, desc.Inline);
        }
    }

    /// <summary>
    /// Emits a single key-value entry.
    /// Scalars use <c>key: value\n</c>; complex values use <c>key::\n</c> then body.
    /// When <paramref name="inlineOverride"/> is non-null it takes precedence over
    /// <see cref="HumlOptions.CollectionFormat"/> for collection properties.
    /// </summary>
    private static void EmitEntry(
        StringBuilder sb,
        string indent,
        string key,
        object? value,
        int depth,
        HumlOptions options,
        bool? inlineOverride = null)
    {
        if (IsScalarValue(value))
        {
            sb.Append(indent);
            AppendKey(sb, key);
            sb.Append(": ");
            SerializeValue(sb, value, depth + 1, options);
            sb.Append('\n');
            return;
        }

        // Compute effective inline intent (scalar properties are unaffected — handled above)
        bool wantInline = inlineOverride ?? (options.CollectionFormat == CollectionFormat.Inline);

        // null is also scalar — handled above (IsScalarValue returns true for null)

        // Collection or POCO — use :: indicator
        if (value is IDictionary dict)
        {
            if (dict.Count == 0)
            {
                sb.Append(indent);
                AppendKey(sb, key);
                sb.Append(":: {}\n");
                return;
            }
            if (wantInline && AllDictionaryValuesAreScalar(dict))
            {
                EmitInlineDictionary(sb, indent, key, dict, options);
                return;
            }
            sb.Append(indent);
            AppendKey(sb, key);
            sb.Append("::\n");
            SerializeDictionaryBody(sb, dict, depth + 1, options);
            return;
        }

        if (value is IEnumerable enumerable and not string)
        {
            // Materialise once to check empty / all-scalar without double-enumerating
            var items = new List<object?>();
            foreach (var item in enumerable)
                items.Add(item);

            if (items.Count == 0)
            {
                sb.Append(indent);
                AppendKey(sb, key);
                sb.Append(":: []\n");
                return;
            }
            if (wantInline && items.TrueForAll(IsScalarValue))
            {
                EmitInlineSequence(sb, indent, key, items, depth, options);
                return;
            }
            sb.Append(indent);
            AppendKey(sb, key);
            sb.Append("::\n");
            EmitSequenceItems(sb, items, depth + 1, options);
            return;
        }

        // POCO object (not null — null was handled by IsScalarValue)
        var valueType = value!.GetType();
        if (IsUnsupportedType(valueType))
            throw new HumlSerializeException(
                $"Cannot serialize type '{valueType.FullName}': delegates, function pointers, and " +
                "similar non-data types are not supported by HumlSerializer.");
        sb.Append(indent);
        AppendKey(sb, key);
        sb.Append("::\n");
        SerializeMappingBody(sb, value!, depth + 1, options);
    }

    /// <summary>
    /// Emits a scalar-only sequence in inline format: <c>key:: v1, v2, v3\n</c>.
    /// Caller must verify all items are scalar before calling.
    /// </summary>
    private static void EmitInlineSequence(
        StringBuilder sb, string indent, string key, List<object?> items, int depth, HumlOptions options)
    {
        sb.Append(indent);
        AppendKey(sb, key);
        sb.Append(":: ");
        for (int i = 0; i < items.Count; i++)
        {
            if (i > 0) sb.Append(", ");
            SerializeValue(sb, items[i], depth + 1, options);
        }
        sb.Append('\n');
    }

    /// <summary>
    /// Emits a scalar-valued dictionary in inline format: <c>key:: k1: v1, k2: v2\n</c>.
    /// Caller must verify all values are scalar before calling.
    /// </summary>
    private static void EmitInlineDictionary(
        StringBuilder sb, string indent, string key, IDictionary dict, HumlOptions options)
    {
        sb.Append(indent);
        AppendKey(sb, key);
        sb.Append(":: ");
        bool first = true;
        foreach (DictionaryEntry entry in dict)
        {
            if (!first) sb.Append(", ");
            first = false;
            var entryKey = entry.Key?.ToString() ?? "null";
            AppendKey(sb, entryKey);
            sb.Append(": ");
            SerializeValue(sb, entry.Value, 0, options);
        }
        sb.Append('\n');
    }

    /// <summary>
    /// Returns <c>true</c> when every value in <paramref name="dict"/> is a scalar
    /// (eligible for inline dictionary format).
    /// </summary>
    private static bool AllDictionaryValuesAreScalar(IDictionary dict)
    {
        foreach (DictionaryEntry e in dict)
            if (!IsScalarValue(e.Value)) return false;
        return true;
    }

    // ── Sequence (list / array) ───────────────────────────────────────────────

    /// <summary>
    /// Emits items of an <see cref="IEnumerable"/> as sequence entries at <paramref name="depth"/>.
    /// This is the single shared implementation for all sequence serialisation paths.
    /// </summary>
    private static void EmitSequenceItems(
        StringBuilder sb, IEnumerable items, int depth, HumlOptions options)
    {
        var indent = Indent(depth);
        foreach (var item in items)
        {
            sb.Append(indent);
            sb.Append("- ");
            if (IsScalarValue(item))
            {
                SerializeValue(sb, item, depth + 1, options);
                sb.Append('\n');
            }
            else
            {
                sb.Append('\n');
                if (item is IDictionary dict2)
                    SerializeDictionaryBody(sb, dict2, depth + 1, options);
                else if (item is IEnumerable nested and not string)
                    EmitSequenceItems(sb, nested, depth + 1, options);
                else if (item != null)
                {
                    var itemType = item.GetType();
                    if (IsUnsupportedType(itemType))
                        throw new HumlSerializeException(
                            $"Cannot serialize type '{itemType.FullName}': delegates, function pointers, and " +
                            "similar non-data types are not supported by HumlSerializer.");
                    SerializeMappingBody(sb, item, depth + 1, options);
                }
            }
        }
    }

    // ── Dictionary ────────────────────────────────────────────────────────────

    /// <summary>
    /// Emits dictionary entries at <paramref name="depth"/>. Assumes caller already emitted
    /// the <c>key::\n</c> header line.
    /// Dictionary entries do not inherit per-property inline overrides; they always use
    /// multiline unless a per-entry override is explicitly supplied.
    /// </summary>
    private static void SerializeDictionaryBody(
        StringBuilder sb,
        IDictionary dict,
        int depth,
        HumlOptions options)
    {
        var indent = Indent(depth);
        foreach (DictionaryEntry entry in dict)
        {
            var key = entry.Key?.ToString() ?? "null";
            var value = entry.Value;
            // Dictionary entries are always emitted multiline — inline is a POCO-property-level concern
            EmitEntry(sb, indent, key, value, depth, options, inlineOverride: false);
        }
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    /// <summary>Returns <c>true</c> if <paramref name="value"/> should be emitted inline after <c>: </c>.</summary>
    private static bool IsScalarValue(object? value)
    {
        if (value is null) return true;
        if (value is string) return true;
        if (value is bool) return true;
        if (IsIntegerType(value)) return true;
        if (value is double or float or decimal) return true;

        // Anything else (collections, POCOs) is complex
        return false;
    }

    private static bool IsIntegerType(object value) =>
        value is int or long or short or byte or sbyte or ushort or uint or ulong;

    private static bool IsUnsupportedType(Type type)
    {
        if (typeof(Delegate).IsAssignableFrom(type)) return true;
        if (type.IsPointer) return true;
        return false;
    }

    private static string FormatDouble(double d)
    {
        if (double.IsNaN(d)) return "nan";
        if (double.IsPositiveInfinity(d)) return "+inf";
        if (double.IsNegativeInfinity(d)) return "-inf";
        return d.ToString("G", CultureInfo.InvariantCulture);
    }

    private static void AppendEscapedString(StringBuilder sb, string s)
    {
        foreach (char c in s)
        {
            switch (c)
            {
                case '\\': sb.Append("\\\\"); break;
                case '"':  sb.Append("\\\""); break;
                case '\n': sb.Append("\\n");  break;
                case '\r': sb.Append("\\r");  break;
                case '\t': sb.Append("\\t");  break;
                default:   sb.Append(c);      break;
            }
        }
    }

    /// <summary>
    /// Returns <c>true</c> when <paramref name="key"/> cannot be emitted as a bare HUML key.
    /// The bare-key grammar is <c>[a-zA-Z][a-zA-Z0-9_-]*</c>; anything outside this requires quoting.
    /// </summary>
    private static bool NeedsQuoting(string key)
    {
        if (key.Length == 0) return true;

        char first = key[0];
        if (!((first >= 'a' && first <= 'z') || (first >= 'A' && first <= 'Z')))
            return true;

        for (int i = 1; i < key.Length; i++)
        {
            char c = key[i];
            bool valid = (c >= 'a' && c <= 'z') || (c >= 'A' && c <= 'Z')
                      || (c >= '0' && c <= '9') || c == '_' || c == '-';
            if (!valid) return true;
        }

        return false;
    }

    /// <summary>
    /// Appends <paramref name="key"/> to <paramref name="sb"/>, quoting and escaping if
    /// the key does not satisfy the bare-key grammar.
    /// </summary>
    private static void AppendKey(StringBuilder sb, string key)
    {
        if (NeedsQuoting(key))
        {
            sb.Append('"');
            AppendEscapedString(sb, key);
            sb.Append('"');
        }
        else
        {
            sb.Append(key);
        }
    }

    private static string VersionString(HumlSpecVersion version) =>
#pragma warning disable CS0618 // V0_1 is deprecated but we must still handle it
        version == HumlSpecVersion.V0_1 ? "v0.1.0" : "v0.2.0";
#pragma warning restore CS0618

    private static string Indent(int depth) => new(' ', depth * 2);
}
