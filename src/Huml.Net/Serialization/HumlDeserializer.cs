using System.Collections;
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
    internal static T Deserialize<T>(string huml, HumlOptions? options = null)
    {
        var doc = new HumlParser(huml, options ?? HumlOptions.Default).Parse();
        return (T)DeserializeNode(doc, typeof(T))!;
    }

    /// <summary>
    /// Deserialises HUML text (as a span) into a typed .NET object of type <typeparamref name="T"/>.
    /// </summary>
    internal static T Deserialize<T>(ReadOnlySpan<char> huml, HumlOptions? options = null)
    {
        var doc = new HumlParser(huml.ToString(), options ?? HumlOptions.Default).Parse();
        return (T)DeserializeNode(doc, typeof(T))!;
    }

    /// <summary>
    /// Deserialises HUML text into an object of the given <paramref name="targetType"/>.
    /// Untyped overload for use by the Phase 7 public API entry point.
    /// </summary>
    internal static object? Deserialize(string huml, Type targetType, HumlOptions? options = null)
    {
        var doc = new HumlParser(huml, options ?? HumlOptions.Default).Parse();
        return DeserializeNode(doc, targetType);
    }

    // ── Core dispatch ─────────────────────────────────────────────────────────

    /// <summary>
    /// Dispatches an AST node to the appropriate deserialisation handler based on node type.
    /// </summary>
    private static object? DeserializeNode(HumlNode node, Type targetType)
    {
        if (node is HumlScalar scalar)
            return CoerceScalar(scalar, targetType, key: string.Empty, line: 0);

        if (node is HumlDocument doc)
            return DeserializeDocument(doc, targetType);

        if (node is HumlSequence seq)
            return DeserializeSequence(seq, targetType);

        throw new HumlDeserializeException("Unexpected AST node type encountered during deserialization.");
    }

    // ── Document (mapping) deserialization ───────────────────────────────────

    /// <summary>
    /// Deserialises a <see cref="HumlDocument"/> (key-value mapping) into either a
    /// <c>Dictionary&lt;string, T&gt;</c> (if <paramref name="targetType"/> is a string-keyed dict)
    /// or a POCO with public settable properties.
    /// </summary>
    private static object? DeserializeDocument(HumlDocument doc, Type targetType)
    {
        // Dispatch to dictionary path if targetType is Dictionary<string, T>
        if (IsStringKeyedDictionary(targetType))
            return DeserializeDictionary(doc, targetType);

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

        // Get property descriptors for the target type
        var descriptors = PropertyDescriptor.GetDescriptors(targetType);

        // Map each HUML mapping entry to a property
        foreach (var entry in doc.Entries)
        {
            if (entry is not HumlMapping mapping)
                continue;

            // Find matching descriptor by HUML key (case-sensitive)
            PropertyDescriptor? descriptor = null;
            foreach (var d in descriptors)
            {
                if (string.Equals(d.HumlKey, mapping.Key, StringComparison.Ordinal))
                {
                    descriptor = d;
                    break;
                }
            }

            // Unknown key — skip silently (forward compatibility)
            if (descriptor is null)
                continue;

            // Init-only properties cannot be set after construction
            if (descriptor.IsInitOnly)
                throw new HumlDeserializeException(
                    $"Property '{descriptor.Property.Name}' on type '{targetType.Name}' is init-only and cannot be deserialized.",
                    mapping.Key,
                    line: 0);

            // Read-only (no setter) — skip silently
            if (descriptor.Property.SetMethod is null)
                continue;

            // Deserialize the value recursively targeting the property type
            var deserializedValue = DeserializeNode(mapping.Value, descriptor.Property.PropertyType);

            // Set property value via reflection
            descriptor.Property.SetValue(instance, deserializedValue);
        }

        return instance;
    }

    // ── Sequence deserialization ──────────────────────────────────────────────

    /// <summary>
    /// Deserialises a <see cref="HumlSequence"/> into an array, <see cref="List{T}"/>,
    /// or <see cref="IEnumerable{T}"/> based on <paramref name="targetType"/>.
    /// </summary>
    private static object DeserializeSequence(HumlSequence seq, Type targetType)
    {
        // a. Array dispatch
        if (targetType.IsArray)
        {
            var elementType = targetType.GetElementType()!;
            var array = Array.CreateInstance(elementType, seq.Items.Count);
            for (int i = 0; i < seq.Items.Count; i++)
            {
                var item = DeserializeNode(seq.Items[i], elementType);
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
                list.Add(DeserializeNode(item, elementType));
            return list;
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
                    list.Add(DeserializeNode(item, elementType));
                return list;
            }
        }

        throw new HumlDeserializeException(
            $"Cannot deserialize sequence into type '{targetType.Name}'.");
    }

    // ── Dictionary deserialization ────────────────────────────────────────────

    /// <summary>
    /// Deserialises a <see cref="HumlDocument"/> into a <c>Dictionary&lt;string, T&gt;</c>.
    /// </summary>
    private static object DeserializeDictionary(HumlDocument doc, Type targetType)
    {
        var valueType = targetType.GetGenericArguments()[1];
        var dict = (IDictionary)Activator.CreateInstance(targetType)!;

        foreach (var entry in doc.Entries)
        {
            if (entry is not HumlMapping mapping)
                continue;

            var value = DeserializeNode(mapping.Value, valueType);
            dict[mapping.Key] = value;
        }

        return dict;
    }

    // ── Scalar coercion ───────────────────────────────────────────────────────

    /// <summary>
    /// Coerces a <see cref="HumlScalar"/> to <paramref name="targetType"/>.
    /// Handles null, bool, string, integer, float, NaN, and Inf kinds with diagnostic
    /// exceptions carrying <paramref name="key"/> and <paramref name="line"/> on failure.
    /// </summary>
    private static object? CoerceScalar(HumlScalar scalar, Type targetType, string key, int line)
    {
        // Unwrap Nullable<T> to its underlying type for comparison
        var underlying = Nullable.GetUnderlyingType(targetType) ?? targetType;
        bool isNullable = underlying != targetType || !targetType.IsValueType;

        try
        {
            switch (scalar.Kind)
            {
                case ScalarKind.Null:
                    if (isNullable || !targetType.IsValueType)
                        return null;
                    throw new HumlDeserializeException(
                        $"Cannot assign null to non-nullable type '{targetType.Name}'.",
                        key, line);

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
                        key, line);

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
                        key, line);
                }

                default:
                    throw new HumlDeserializeException(
                        $"Unhandled scalar kind '{scalar.Kind}'.",
                        key, line);
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
                key, line);
        }
        catch (FormatException ex)
        {
            throw new HumlDeserializeException(
                $"Cannot convert {scalar.Kind} to '{targetType.Name}': {ex.Message}",
                key, line);
        }
        catch (OverflowException ex)
        {
            throw new HumlDeserializeException(
                $"Cannot convert {scalar.Kind} to '{targetType.Name}': {ex.Message}",
                key, line);
        }
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

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
