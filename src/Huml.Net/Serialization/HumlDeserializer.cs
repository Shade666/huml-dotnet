using System.Collections;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using Huml.Net.Exceptions;
using Huml.Net.Parser;
using Huml.Net.Versioning;

namespace Huml.Net.Serialization;

/// <summary>
/// Deserialises HUML text (parsed to a <see cref="HumlDocument"/> AST) into .NET objects.
/// Uses the <see cref="PropertyDescriptor"/> cache for property lookup and attribute resolution.
/// </summary>
internal static class HumlDeserializer
{
    // ── Public entry points ───────────────────────────────────────────────────

    /// <summary>
    /// Deserialises HUML text into a typed .NET object of type <typeparamref name="T"/>.
    /// </summary>
    /// <typeparam name="T">Target type to deserialise into.</typeparam>
    /// <param name="huml">HUML-formatted text.</param>
    /// <param name="options">Parsing options; defaults to <see cref="HumlOptions.Default"/> if null.</param>
    /// <returns>A populated instance of <typeparamref name="T"/>.</returns>
    /// <exception cref="HumlDeserializeException">On any mapping, coercion, or constructor failure.</exception>
    [RequiresUnreferencedCode("Reflection-based HUML deserialisation.")]
    [RequiresDynamicCode("Reflection-based HUML deserialisation may emit dynamic code.")]
    internal static T Deserialize<T>(string huml, HumlOptions? options = null)
    {
        var opts = options ?? HumlOptions.Default;
        var doc = new HumlParser(huml, opts).Parse();
        return (T)DeserializeNode(doc, typeof(T), opts)!;
    }

    /// <summary>
    /// Deserialises HUML text (as a span) into a typed .NET object of type <typeparamref name="T"/>.
    /// The span is converted to a <see cref="string"/> via <c>ToString()</c> before lexing;
    /// a true zero-copy path is a v2 enhancement.
    /// </summary>
    [RequiresUnreferencedCode("Reflection-based HUML deserialisation.")]
    [RequiresDynamicCode("Reflection-based HUML deserialisation may emit dynamic code.")]
    internal static T Deserialize<T>(ReadOnlySpan<char> huml, HumlOptions? options = null)
    {
        var opts = options ?? HumlOptions.Default;
        var doc = new HumlParser(huml.ToString(), opts).Parse();
        return (T)DeserializeNode(doc, typeof(T), opts)!;
    }

    /// <summary>
    /// Deserialises HUML text into an object of the given <paramref name="targetType"/>.
    /// Untyped overload for use by the Phase 7 public API entry point.
    /// </summary>
    [RequiresUnreferencedCode("Reflection-based HUML deserialisation.")]
    [RequiresDynamicCode("Reflection-based HUML deserialisation may emit dynamic code.")]
    internal static object? Deserialize(string huml, Type targetType, HumlOptions? options = null)
    {
        var opts = options ?? HumlOptions.Default;
        var doc = new HumlParser(huml, opts).Parse();
        return DeserializeNode(doc, targetType, opts);
    }

    /// <summary>
    /// Populates an existing instance of <typeparamref name="T"/> with values deserialised
    /// from a HUML character span, overlaying values onto the caller-supplied instance
    /// rather than constructing a fresh one.
    /// </summary>
    /// <typeparam name="T">The type of the existing instance to populate.</typeparam>
    /// <param name="huml">The HUML document as a character span.</param>
    /// <param name="existing">The existing instance to populate. Must not be <c>null</c>.</param>
    /// <param name="options">Parsing options; defaults to <see cref="HumlOptions.Default"/> if null.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="existing"/> is <c>null</c>.</exception>
    /// <exception cref="ArgumentException">Thrown when <typeparamref name="T"/> is a value type (struct).</exception>
    [RequiresUnreferencedCode("Reflection-based HUML deserialisation.")]
    [RequiresDynamicCode("Reflection-based HUML deserialisation may emit dynamic code.")]
    internal static void Populate<T>(ReadOnlySpan<char> huml, T existing, HumlOptions? options = null)
    {
        // Guard: value types cannot be populated in-place — C# passes structs by copy.
        if (typeof(T).IsValueType)
            throw new ArgumentException(
                "Populate<T> cannot populate a value type — use Deserialize<T> to create a new instance.",
                nameof(existing));

        // Guard: existing must not be null (only reachable for reference types;
        // the struct guard above already threw for value types).
        // NOTE: ArgumentNullException.ThrowIfNull is not available on netstandard2.1.
        if (existing is null)
            throw new ArgumentNullException(nameof(existing));

        var opts = options ?? HumlOptions.Default;
        var doc = new HumlParser(huml.ToString(), opts).Parse();
        PopulateMappingEntries(doc.Entries, existing, typeof(T), opts);
    }

    /// <summary>
    /// Populates an existing object instance by overlaying HUML mapping entries onto its
    /// properties. Does not construct a new instance; uses the
    /// caller-supplied <paramref name="existing"/> instance directly.
    /// </summary>
    [RequiresUnreferencedCode("Reflection-based HUML deserialisation.")]
    private static void PopulateMappingEntries(
        IReadOnlyList<HumlNode> entries, object existing, Type targetType, HumlOptions options)
    {
        // NOTE: keep in sync with DeserializeMappingEntries — the converter dispatch block below
        // is an intentional structural copy. If a bug is fixed in one, apply it to the other.

        // No Activator.CreateInstance — use 'existing' directly.
        // Get property lookup dictionary for the target type (O(1) key access).
        var lookup = PropertyDescriptor.GetLookup(targetType, options.PropertyNamingPolicy);

        // Map each HUML mapping entry to a property on the existing instance.
        foreach (var entry in entries)
        {
            if (entry is not HumlMapping mapping)
                continue;

            // Find matching descriptor by HUML key (case-sensitive, O(1))
            lookup.TryGetValue(mapping.Key, out PropertyDescriptor? descriptor);

            // Unknown key — skip silently (forward compatibility, POP-04)
            if (descriptor is null)
                continue;

            // Init-only properties cannot be set after construction (POP-09)
            if (descriptor.IsInitOnly)
                throw new HumlDeserializeException(
                    $"Property '{descriptor.Property.Name}' on type '{targetType.Name}' is init-only and cannot be deserialised.",
                    mapping.Key,
                    line: mapping.Line,
                    column: mapping.Column);

            // Read-only (no setter) — skip silently (POP-10)
            if (descriptor.Property.SetMethod is null)
                continue;

            // Deserialize the value, with converter-first priority.
            // NOTE: keep in sync with DeserializeMappingEntries converter dispatch block.
            object? deserializedValue;
            if (descriptor.Converter != null)
            {
                // Property-level [HumlConverter] — highest priority, overrides type-level and options (POP-12)
                deserializedValue = descriptor.Converter.ReadObject(mapping.Value);
                ThrowIfNullForNonNullable(
                    deserializedValue,
                    descriptor.Property.PropertyType,
                    mapping.Key,
                    GetNodeLine(mapping.Value),
                    GetNodeColumn(mapping.Value));
            }
            else if (ConverterCache.TryGet(descriptor.Property.PropertyType, options) is { } propConverter)
            {
                // Type-level or options-level converter for this property's type (POP-12)
                deserializedValue = propConverter.ReadObject(mapping.Value);
                ThrowIfNullForNonNullable(
                    deserializedValue,
                    descriptor.Property.PropertyType,
                    mapping.Key,
                    GetNodeLine(mapping.Value),
                    GetNodeColumn(mapping.Value));
            }
            else if (mapping.Value is HumlScalar s)
            {
                // Direct scalar coercion — includes key in diagnostic exception
                deserializedValue = CoerceScalar(s, descriptor.Property.PropertyType, mapping.Key, s.Line, s.Column, options);
            }
            else
            {
                // Complex node (HumlDocument, HumlInlineMapping, or HumlSequence) — route through
                // DeserializeNode (picks up type-level + options converters).
                // DeserializeNode calls DeserializeMappingEntries / DeserializeSequence, which create NEW
                // objects — this naturally gives replace (not merge) semantics for collections (POP-05, POP-06).
                deserializedValue = DeserializeNode(mapping.Value, descriptor.Property.PropertyType, options);
            }

            // Overlay: set property value on the existing instance (POP-03)
            descriptor.Property.SetValue(existing, deserializedValue);
        }
    }

    // ── Core dispatch ─────────────────────────────────────────────────────────

    /// <summary>
    /// Dispatches an AST node to the appropriate deserialisation handler based on node type.
    /// </summary>
    [RequiresUnreferencedCode("Reflection-based HUML deserialisation.")]
    private static object? DeserializeNode(HumlNode node, Type targetType, HumlOptions options)
    {
        // Converter dispatch — type-level [HumlConverter] and HumlOptions.Converters.
        // Property-level converters are dispatched in DeserializeMappingEntries (desc.Converter).
        var typeConverter = ConverterCache.TryGet(targetType, options);
        if (typeConverter != null)
        {
            var result = typeConverter.ReadObject(node);
            ThrowIfNullForNonNullable(result, targetType, key: string.Empty, line: GetNodeLine(node), column: GetNodeColumn(node));
            return result;
        }

        if (node is HumlScalar scalar)
            return CoerceScalar(scalar, targetType, key: null, line: scalar.Line, column: scalar.Column, options);

        if (node is HumlDocument doc)
            return DeserializeMappingEntries(doc.Entries, targetType, options);

        if (node is HumlInlineMapping inlineMapping)
            return DeserializeMappingEntries(inlineMapping.Entries, targetType, options);

        if (node is HumlSequence seq)
            return DeserializeSequence(seq, targetType, options);

        throw new HumlDeserializeException("Unexpected AST node type encountered during deserialization.");
    }

    // ── Document (mapping) deserialization ───────────────────────────────────

    /// <summary>
    /// Deserialises mapping entries into either a <c>Dictionary&lt;string, T&gt;</c>
    /// (if <paramref name="targetType"/> is a string-keyed dict) or a POCO with public
    /// settable properties. Shared by <see cref="HumlDocument"/> and <see cref="HumlInlineMapping"/>
    /// dispatch paths.
    /// </summary>
    [RequiresUnreferencedCode("Reflection-based HUML deserialisation.")]
    private static object? DeserializeMappingEntries(IReadOnlyList<HumlNode> entries, Type targetType, HumlOptions options)
    {
        // Dispatch to dictionary path if targetType is Dictionary<string, T>
        if (IsStringKeyedDictionary(targetType))
            return DeserializeDictionary(entries, targetType, options);

        // Create instance via parameterless constructor
        object instance;
        try
        {
            instance = Activator.CreateInstance(targetType)
                ?? throw new HumlDeserializeException(
                    $"Type '{targetType.Name}' has no accessible parameterless constructor.");
        }
        catch (MissingMethodException)
        {
            throw new HumlDeserializeException(
                $"Type '{targetType.Name}' has no accessible parameterless constructor.");
        }

        // Get property lookup dictionary for the target type (O(1) key access)
        var lookup = PropertyDescriptor.GetLookup(targetType, options.PropertyNamingPolicy);

        // Map each HUML mapping entry to a property
        foreach (var entry in entries)
        {
            if (entry is not HumlMapping mapping)
                continue;

            // Find matching descriptor by HUML key (case-sensitive, O(1))
            lookup.TryGetValue(mapping.Key, out PropertyDescriptor? descriptor);

            // Unknown key — skip silently (forward compatibility)
            if (descriptor is null)
                continue;

            // Init-only properties cannot be set after construction
            if (descriptor.IsInitOnly)
                throw new HumlDeserializeException(
                    $"Property '{descriptor.Property.Name}' on type '{targetType.Name}' is init-only and cannot be deserialised.",
                    mapping.Key,
                    line: mapping.Line,
                    column: mapping.Column);

            // Read-only (no setter) — skip silently
            if (descriptor.Property.SetMethod is null)
                continue;

            // Deserialize the value, with converter-first priority.
            object? deserializedValue;
            if (descriptor.Converter != null)
            {
                // Property-level [HumlConverter] — highest priority, overrides type-level and options
                deserializedValue = descriptor.Converter.ReadObject(mapping.Value);
                ThrowIfNullForNonNullable(
                    deserializedValue,
                    descriptor.Property.PropertyType,
                    mapping.Key,
                    GetNodeLine(mapping.Value),
                    GetNodeColumn(mapping.Value));
            }
            else if (ConverterCache.TryGet(descriptor.Property.PropertyType, options) is { } propConverter)
            {
                // Type-level or options-level converter for this property's type
                deserializedValue = propConverter.ReadObject(mapping.Value);
                ThrowIfNullForNonNullable(
                    deserializedValue,
                    descriptor.Property.PropertyType,
                    mapping.Key,
                    GetNodeLine(mapping.Value),
                    GetNodeColumn(mapping.Value));
            }
            else if (mapping.Value is HumlScalar s)
            {
                // Direct scalar coercion — includes key in diagnostic exception (WR-01 fix preserved)
                deserializedValue = CoerceScalar(s, descriptor.Property.PropertyType, mapping.Key, s.Line, s.Column, options);
            }
            else
            {
                // Complex node — route through DeserializeNode (picks up type-level + options converters)
                deserializedValue = DeserializeNode(mapping.Value, descriptor.Property.PropertyType, options);
            }

            // Set property value via reflection
            descriptor.Property.SetValue(instance, deserializedValue);
        }

        return instance;
    }

    // ── Sequence deserialization ──────────────────────────────────────────────

    /// <summary>
    /// Deserialises a <see cref="HumlSequence"/> into an array, <see cref="List{T}"/>,
    /// <see cref="System.Collections.Generic.HashSet{T}"/>,
    /// <see cref="System.Collections.Generic.ISet{T}"/>,
    /// <c>IReadOnlySet{T}</c> (NET5_0_OR_GREATER only — absent from netstandard2.1),
    /// or <see cref="IEnumerable{T}"/> based on <paramref name="targetType"/>.
    /// All set types materialise as <see cref="System.Collections.Generic.HashSet{T}"/>;
    /// duplicate input elements are silently deduplicated by natural <c>HashSet</c> semantics.
    /// </summary>
    [RequiresUnreferencedCode("Reflection-based HUML deserialisation.")]
    private static object DeserializeSequence(HumlSequence seq, Type targetType, HumlOptions options)
    {
        // a. Array dispatch
        if (targetType.IsArray)
        {
            var elementType = targetType.GetElementType()!;
            var array = Array.CreateInstance(elementType, seq.Items.Count);
            for (int i = 0; i < seq.Items.Count; i++)
            {
                var item = DeserializeNode(seq.Items[i], elementType, options);
                array.SetValue(item, i);
            }
            return array;
        }

        // b. List<T> dispatch
        if (targetType.IsGenericType && targetType.GetGenericTypeDefinition() == typeof(List<>))
        {
            var elementType = targetType.GetGenericArguments()[0];
            var list = (IList)Activator.CreateInstance(targetType)!;
            foreach (var item in seq.Items)
                list.Add(DeserializeNode(item, elementType, options));
            return list;
        }

        // b.5. IReadOnlySet<T> / ISet<T> / HashSet<T> dispatch (materialise as HashSet<T>)
        if (targetType.IsGenericType)
        {
            var typeDef = targetType.GetGenericTypeDefinition();
            bool isSetType = typeDef == typeof(HashSet<>)
                          || typeDef == typeof(ISet<>)
#if NET5_0_OR_GREATER
                          || typeDef == typeof(IReadOnlySet<>)
#endif
                          ;

            if (isSetType)
            {
                var elementType = targetType.GetGenericArguments()[0];
                var hashSetType = typeof(HashSet<>).MakeGenericType(elementType);
                var set = Activator.CreateInstance(hashSetType)!;
                var addMethod = hashSetType.GetMethod("Add")!;
                foreach (var item in seq.Items)
                {
                    var element = DeserializeNode(item, elementType, options);
                    addMethod.Invoke(set, new object?[] { element });
                }
                return set;
            }
        }

        // c. IEnumerable<T> dispatch (materialise as List<T>)
        // Covers: IEnumerable<T> itself, ICollection<T>, IReadOnlyList<T>, etc.
        if (targetType.IsGenericType)
        {
            var typeDef = targetType.GetGenericTypeDefinition();
            // Check if targetType is IEnumerable<> itself or implements IEnumerable<>
            bool isAssignableFromList = false;
            var elementType = targetType.GetGenericArguments()[0];

            // Is the target type the IEnumerable<T> interface directly?
            if (typeDef == typeof(IEnumerable<>))
            {
                isAssignableFromList = true;
            }
            else
            {
                // Check if target implements IEnumerable<T>
                var iface = targetType.GetInterface(typeof(IEnumerable<>).FullName!);
                isAssignableFromList = iface != null;
            }

            if (isAssignableFromList)
            {
                var listType = typeof(List<>).MakeGenericType(elementType);
                var list = (IList)Activator.CreateInstance(listType)!;
                foreach (var item in seq.Items)
                    list.Add(DeserializeNode(item, elementType, options));
                return list;
            }
        }

        throw new HumlDeserializeException(
            $"Cannot deserialize sequence into type '{targetType.Name}'.");
    }

    // ── Dictionary deserialization ────────────────────────────────────────────

    /// <summary>
    /// Deserialises mapping entries into a <c>Dictionary&lt;string, T&gt;</c>.
    /// Accepts <see cref="IReadOnlyList{HumlNode}"/> so it can be called from both
    /// <see cref="HumlDocument"/> and <see cref="HumlInlineMapping"/> dispatch paths.
    /// </summary>
    [RequiresUnreferencedCode("Reflection-based HUML deserialisation.")]
    private static object DeserializeDictionary(IReadOnlyList<HumlNode> entries, Type targetType, HumlOptions options)
    {
        var valueType = targetType.GetGenericArguments()[1];
        var dict = (IDictionary)Activator.CreateInstance(targetType)!;

        foreach (var entry in entries)
        {
            if (entry is not HumlMapping mapping)
                continue;

            var value = DeserializeNode(mapping.Value, valueType, options);
            dict[mapping.Key] = value;
        }

        return dict;
    }

    // ── Scalar coercion ───────────────────────────────────────────────────────

    /// <summary>
    /// Coerces a <see cref="HumlScalar"/> to <paramref name="targetType"/>.
    /// Handles null, bool, string, integer, float, NaN, and Inf kinds with diagnostic
    /// exceptions carrying <paramref name="key"/>, <paramref name="line"/>, and
    /// <paramref name="column"/> on failure.
    /// Pass <c>null</c> for <paramref name="key"/> when there is no enclosing mapping key
    /// (e.g. a root-level scalar document); the resulting exception omits the key prefix.
    /// </summary>
    [RequiresUnreferencedCode("Reflection-based HUML deserialisation.")]
    private static object? CoerceScalar(HumlScalar scalar, Type targetType, string? key, int line, int column, HumlOptions options)
    {
        // Unwrap Nullable<T> to its underlying type for comparison
        var underlying = Nullable.GetUnderlyingType(targetType) ?? targetType;
        bool isNullable = underlying != targetType || !targetType.IsValueType;

        try
        {
            if (underlying.IsEnum)
            {
                if (scalar.Kind == ScalarKind.Null)
                {
                    if (isNullable)
                        return null;
                    throw new HumlDeserializeException(
                        $"Cannot assign null to non-nullable enum type '{underlying.Name}'.",
                        key, line, column);
                }
                if (scalar.Kind == ScalarKind.String)
                {
                    var raw = scalar.Value as string ?? string.Empty;
                    if (EnumNameCache.TryParse(underlying, raw, options.PropertyNamingPolicy, out var enumResult))
                        return enumResult;
                    throw new HumlDeserializeException(
                        $"Value \"{raw}\" is not a valid member of enum '{underlying.Name}'.",
                        key, line, column);
                }
                if (scalar.Kind == ScalarKind.Integer)
                {
                    return Enum.ToObject(underlying, Convert.ToInt64(scalar.Value, CultureInfo.InvariantCulture));
                }
                throw new HumlDeserializeException(
                    $"Cannot convert {scalar.Kind} to enum '{underlying.Name}'.",
                    key, line, column);
            }

            switch (scalar.Kind)
            {
                case ScalarKind.Null:
                    if (isNullable || !targetType.IsValueType)
                        return null;
                    throw new HumlDeserializeException(
                        $"Cannot assign null to non-nullable type '{targetType.Name}'.",
                        key, line, column);

                case ScalarKind.Bool:
                    if (underlying == typeof(bool))
                        return (bool)scalar.Value!;
                    return Convert.ChangeType(scalar.Value, underlying, CultureInfo.InvariantCulture);

                case ScalarKind.String:
                    if (underlying == typeof(string))
                        return (string?)scalar.Value;
                    return Convert.ChangeType(scalar.Value, underlying, CultureInfo.InvariantCulture);

                case ScalarKind.Integer:
                    // Parser produces long; convert to target numeric type
                    return Convert.ChangeType(scalar.Value, underlying, CultureInfo.InvariantCulture);

                case ScalarKind.Float:
                    // Parser produces double; convert to target numeric type
                    return Convert.ChangeType(scalar.Value, underlying, CultureInfo.InvariantCulture);

                case ScalarKind.NaN:
                    if (underlying == typeof(double))
                        return double.NaN;
                    if (underlying == typeof(float))
                        return float.NaN;
                    throw new HumlDeserializeException(
                        $"Cannot convert NaN to type '{targetType.Name}'.",
                        key, line, column);

                case ScalarKind.Inf:
                {
                    // Value is the raw token string: "+inf", "-inf", or "inf"
                    var raw = scalar.Value as string ?? string.Empty;
                    bool isNegative = string.Equals(raw, "-inf", StringComparison.OrdinalIgnoreCase);

                    if (underlying == typeof(double))
                        return isNegative ? double.NegativeInfinity : double.PositiveInfinity;
                    if (underlying == typeof(float))
                        return isNegative ? float.NegativeInfinity : float.PositiveInfinity;

                    throw new HumlDeserializeException(
                        $"Cannot convert Inf to type '{targetType.Name}'.",
                        key, line, column);
                }

                default:
                    throw new HumlDeserializeException(
                        $"Unhandled scalar kind '{scalar.Kind}'.",
                        key, line, column);
            }
        }
        catch (HumlDeserializeException)
        {
            throw; // re-throw our own exceptions as-is
        }
        catch (InvalidCastException ex)
        {
            throw new HumlDeserializeException(
                $"Cannot convert {scalar.Kind} to '{targetType.Name}': {ex.Message}",
                key, line, column);
        }
        catch (FormatException ex)
        {
            throw new HumlDeserializeException(
                $"Cannot convert {scalar.Kind} to '{targetType.Name}': {ex.Message}",
                key, line, column);
        }
        catch (OverflowException ex)
        {
            throw new HumlDeserializeException(
                $"Cannot convert {scalar.Kind} to '{targetType.Name}': {ex.Message}",
                key, line, column);
        }
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    /// <summary>
    /// Throws <see cref="HumlDeserializeException"/> if <paramref name="value"/> is null but
    /// <paramref name="targetType"/> is a non-nullable value type.
    /// </summary>
    private static void ThrowIfNullForNonNullable(object? value, Type targetType, string key, int line, int column)
    {
        if (value is null && targetType.IsValueType && Nullable.GetUnderlyingType(targetType) == null)
            throw new HumlDeserializeException(
                $"Converter returned null for non-nullable value type '{targetType.Name}'.",
                key, line, column);
    }

    /// <summary>Returns the source line from a HumlNode. All nodes inherit Line from HumlNode.</summary>
    private static int GetNodeLine(HumlNode node) => node.Line;

    /// <summary>Returns the source column from a HumlNode. All nodes inherit Column from HumlNode.</summary>
    private static int GetNodeColumn(HumlNode node) => node.Column;

    /// <summary>
    /// Returns <c>true</c> if <paramref name="type"/> is <c>Dictionary&lt;string, T&gt;</c>
    /// for any T.
    /// </summary>
    private static bool IsStringKeyedDictionary(Type type)
    {
        if (!type.IsGenericType)
            return false;
        if (type.GetGenericTypeDefinition() != typeof(Dictionary<,>))
            return false;
        var args = type.GetGenericArguments();
        return args[0] == typeof(string);
    }
}
