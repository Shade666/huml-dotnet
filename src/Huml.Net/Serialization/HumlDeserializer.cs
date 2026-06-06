using System.Collections;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Reflection;
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
    // ── Caches ────────────────────────────────────────────────────────────────

    /// <summary>
    /// Caches the result of <c>GetInterface(IEnumerable&lt;&gt;.FullName)</c> per type to avoid
    /// per-call reflection cost for collection targets not caught by earlier dispatch branches.
    /// Stores <c>null</c> for types that do not implement <c>IEnumerable&lt;T&gt;</c>.
    /// </summary>
    private static readonly System.Collections.Concurrent.ConcurrentDictionary<Type, Type?> IEnumerableInterfaceCache = new();

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
        var doc = new HumlParser(huml.AsSpan(), opts).Parse();
        return (T)DeserializeNode(doc, typeof(T), opts)!;
    }

    /// <summary>
    /// Deserialises HUML text (as a span) into a typed .NET object of type <typeparamref name="T"/>.
    /// The span is passed directly to the ref struct lexer; no intermediate string
    /// allocation occurs for the input buffer.
    /// </summary>
    [RequiresUnreferencedCode("Reflection-based HUML deserialisation.")]
    [RequiresDynamicCode("Reflection-based HUML deserialisation may emit dynamic code.")]
    internal static T Deserialize<T>(ReadOnlySpan<char> huml, HumlOptions? options = null)
    {
        var opts = options ?? HumlOptions.Default;
        var doc = new HumlParser(huml, opts).Parse();
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
        var doc = new HumlParser(huml.AsSpan(), opts).Parse();
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
        var doc = new HumlParser(huml, opts).Parse();
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
        // No Activator.CreateInstance — use 'existing' directly.
        // Get property lookup dictionary for the target type (O(1) key access).
        var lookup = PropertyDescriptor.GetLookup(targetType, options.PropertyNamingPolicy);

        // Hoist extension-data descriptor lookup outside the loop (avoids per-key ConcurrentDictionary hit).
        var extDesc = PropertyDescriptor.GetExtensionDataDescriptor(targetType, options.PropertyNamingPolicy);

        // Map each HUML mapping entry to a property on the existing instance.
        foreach (var entry in entries)
        {
            if (entry is not HumlMapping mapping)
                continue;

            // Find matching descriptor by HUML key (case-sensitive, O(1))
            lookup.TryGetValue(mapping.Key, out PropertyDescriptor? descriptor);

            // Unknown key handling (forward compatibility, POP-04)
            if (descriptor is null)
            {
                // Extension data capture: route unmapped keys to the [HumlExtensionData] property if present.
                // Extension data is an explicit opt-in for unknown keys and suppresses UnmappedMemberHandling.Disallow.
                if (extDesc != null)
                {
                    var extDictObj = extDesc.Property.GetValue(existing);
                    if (extDictObj is null)
                    {
                        extDictObj = Activator.CreateInstance(extDesc.Property.PropertyType)!;
                        extDesc.Property.SetValue(existing, extDictObj);
                    }
                    if (extDictObj is Dictionary<string, HumlNode> nd)
                        nd[mapping.Key] = mapping.Value;
                    else if (extDictObj is Dictionary<string, object?> od)
                        od[mapping.Key] = CoerceExtensionValue(mapping.Value, options);
                }
                else if (options.UnmappedMemberHandling == Versioning.UnmappedMemberHandling.Disallow)
                    throw new HumlDeserializeException(
                        $"Unrecognised key '{mapping.Key}' encountered during deserialisation of type '{targetType.Name}'. " +
                        "Set HumlOptions.UnmappedMemberHandling to Skip to ignore unknown keys.");
                continue;
            }

            // Read-only (no setter) — skip silently (POP-10)
            if (descriptor.Property.SetMethod is null)
                continue;

            var deserializedValue = ResolvePropertyValue(mapping.Value, descriptor, mapping.Key, options);

            // Overlay: set property value on the existing instance (POP-03)
            descriptor.Property.SetValue(existing, deserializedValue);
        }
    }

    [RequiresUnreferencedCode("Reflection-based HUML deserialisation.")]
    private static object? ResolvePropertyValue(
        HumlNode valueNode, PropertyDescriptor descriptor, string key, HumlOptions options)
    {
        if (descriptor.Converter != null)
        {
            var v = descriptor.Converter.ReadObject(valueNode);
            ThrowIfNullForNonNullable(v, descriptor.Property.PropertyType, key,
                GetNodeLine(valueNode), GetNodeColumn(valueNode));
            return v;
        }
        if (ConverterCache.TryGet(descriptor.Property.PropertyType, options) is { } c)
        {
            var v = c.ReadObject(valueNode);
            ThrowIfNullForNonNullable(v, descriptor.Property.PropertyType, key,
                GetNodeLine(valueNode), GetNodeColumn(valueNode));
            return v;
        }
        if (valueNode is HumlScalar s)
            return CoerceScalar(s, descriptor.Property.PropertyType, key, s.Line, s.Column, options);
        return DeserializeNode(valueNode, descriptor.Property.PropertyType, options);
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

        // Phase 23: raise ambiguous-constructor error at deserialise time (not BuildDescriptors —
        // BuildDescriptors runs for serialize/populate too, so we defer the throw here).
        if (PropertyDescriptor.GetHasAmbiguousConstructors(targetType, options.PropertyNamingPolicy))
        {
            var ctors = targetType.GetConstructors(BindingFlags.Public | BindingFlags.Instance);
            int annotatedCount = 0;
            foreach (var c in ctors)
                if (c.GetCustomAttribute<HumlConstructorAttribute>() != null)
                    annotatedCount++;

            throw annotatedCount > 1
                ? new HumlDeserializeException(
                    $"Type '{targetType.Name}' has multiple [HumlConstructor]-annotated constructors — only one is allowed.")
                : new HumlDeserializeException(
                    $"Type '{targetType.Name}' has multiple non-parameterless constructors — annotate one with [HumlConstructor].");
        }

        object instance;
        HashSet<string>? alreadyBound = null;
        var boundKeys = new HashSet<string>(StringComparer.Ordinal);
        var selectedCtor = PropertyDescriptor.GetSelectedConstructor(targetType, options.PropertyNamingPolicy);

        if (selectedCtor != null)
        {
            (instance, alreadyBound) = InvokeConstructor(selectedCtor, entries, targetType, options);
        }
        else
        {
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
        }

        // SGS seam: custom resolver hook. Currently a no-op — HumlTypeInfo<T> carries no
        // property metadata yet. The call site is wired so future phases can activate it.
        _ = options.TypeInfoResolver?.GetTypeInfo(targetType, options);

        // Get property lookup dictionary for the target type (O(1) key access)
        var lookup = PropertyDescriptor.GetLookup(targetType, options.PropertyNamingPolicy);

        // Hoist extension-data descriptor lookup outside the loop (avoids per-key ConcurrentDictionary hit).
        var extDesc = PropertyDescriptor.GetExtensionDataDescriptor(targetType, options.PropertyNamingPolicy);

        // Map each HUML mapping entry to a property
        foreach (var entry in entries)
        {
            if (entry is not HumlMapping mapping)
                continue;

            // Find matching descriptor by HUML key (case-sensitive, O(1))
            lookup.TryGetValue(mapping.Key, out PropertyDescriptor? descriptor);

            // Unknown key handling (forward compatibility)
            if (descriptor is null)
            {
                // Extension data capture: route unmapped keys to the [HumlExtensionData] property if present.
                // Extension data is an explicit opt-in for unknown keys and suppresses UnmappedMemberHandling.Disallow.
                if (extDesc != null)
                {
                    var extDictObj = extDesc.Property.GetValue(instance);
                    if (extDictObj is null)
                    {
                        extDictObj = Activator.CreateInstance(extDesc.Property.PropertyType)!;
                        extDesc.Property.SetValue(instance, extDictObj);
                    }
                    if (extDictObj is Dictionary<string, HumlNode> nd)
                        nd[mapping.Key] = mapping.Value;
                    else if (extDictObj is Dictionary<string, object?> od)
                        od[mapping.Key] = CoerceExtensionValue(mapping.Value, options);
                }
                else if (options.UnmappedMemberHandling == Versioning.UnmappedMemberHandling.Disallow)
                    throw new HumlDeserializeException(
                        $"Unrecognised key '{mapping.Key}' encountered during deserialisation of type '{targetType.Name}'. " +
                        "Set HumlOptions.UnmappedMemberHandling to Skip to ignore unknown keys.");
                continue;
            }

            // Skip keys already supplied as constructor arguments (D-07/D-08).
            if (alreadyBound != null && alreadyBound.Contains(mapping.Key))
                continue;

            // Read-only (no setter) — skip silently
            if (descriptor.Property.SetMethod is null)
                continue;

            var deserializedValue = ResolvePropertyValue(mapping.Value, descriptor, mapping.Key, options);

            // Set property value via reflection
            descriptor.Property.SetValue(instance, deserializedValue);
            boundKeys.Add(descriptor.HumlKey);
        }

        // Required-property check (D-06): collect ALL missing required keys, throw once.
        // Uses GetDescriptors (declaration order) so the error message is deterministic.
        // Does not run for Populate<T> — PopulateMappingEntries is a separate method (D-09).
        var descriptors = PropertyDescriptor.GetDescriptors(targetType, options.PropertyNamingPolicy);
        List<string>? missing = null;
        foreach (var desc in descriptors)
        {
            if (!desc.IsRequired) continue;
            // alreadyBound (OrdinalIgnoreCase) covers constructor-bound keys; boundKeys (Ordinal) covers
            // property-bound keys — desc.HumlKey is always the canonical descriptor key so Ordinal is correct.
            bool wasBound = (alreadyBound != null && alreadyBound.Contains(desc.HumlKey))
                         || boundKeys.Contains(desc.HumlKey);
            if (!wasBound)
                (missing ??= new List<string>()).Add(desc.HumlKey);
        }
        if (missing != null)
            throw new HumlDeserializeException(
                $"Missing required member(s) on type '{targetType.Name}': " +
                $"{string.Join(", ", missing.Select(k => $"'{k}'"))}.");

        return instance;
    }

    // ── Constructor invocation ────────────────────────────────────────────────

    /// <summary>
    /// Invokes <paramref name="ctor"/> with arguments bound from <paramref name="entries"/>,
    /// returning the constructed instance and the set of HUML keys consumed as constructor args.
    /// The caller's post-construction loop must skip keys in the returned set.
    /// </summary>
    [RequiresUnreferencedCode("Reflection-based HUML deserialisation.")]
    private static (object Instance, HashSet<string> AlreadyBound) InvokeConstructor(
        ConstructorInfo ctor, IReadOnlyList<HumlNode> entries, Type targetType, HumlOptions options)
    {
        var ctorParams = ctor.GetParameters();
        var args = new object?[ctorParams.Length];
        var alreadyBound = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        for (int i = 0; i < ctorParams.Length; i++)
        {
            var param = ctorParams[i];
            HumlMapping? matched = null;

            // Pass 1: case-insensitive exact name match — exact match always wins (D-04, Pitfall 3)
            foreach (var entry in entries)
            {
                if (entry is not HumlMapping m) continue;
                if (string.Equals(m.Key, param.Name, StringComparison.OrdinalIgnoreCase))
                {
                    matched = m;
                    break;
                }
            }

            // Pass 2: naming-policy-derived match — only if pass 1 found nothing and policy is set
            if (matched == null && options.PropertyNamingPolicy != null)
            {
                var policyKey = options.PropertyNamingPolicy.ConvertName(param.Name!);
                foreach (var entry in entries)
                {
                    if (entry is not HumlMapping m) continue;
                    if (string.Equals(m.Key, policyKey, StringComparison.Ordinal))
                    {
                        matched = m;
                        break;
                    }
                }
            }

            if (matched != null)
            {
                args[i] = DeserializeNode(matched.Value, param.ParameterType, options);
                alreadyBound.Add(matched.Key); // store HUML key (not param.Name) — Pitfall 4
            }
            else if (param.HasDefaultValue) // gate on HasDefaultValue, NOT null check — Pitfall 1
            {
                args[i] = param.DefaultValue;
            }
            else
            {
                throw new HumlDeserializeException(
                    $"Type '{targetType.Name}' constructor requires parameter '{param.Name}' " +
                    $"(type '{param.ParameterType.Name}') — no matching HUML key found.");
            }
        }

        var instance = ctor.Invoke(args);
        return (instance, alreadyBound);
    }

    // ── Sequence deserialization ──────────────────────────────────────────────

    /// <summary>
    /// Deserialises a <see cref="HumlSequence"/> into an array, <see cref="List{T}"/>,
    /// <see cref="System.Collections.Generic.HashSet{T}"/>,
    /// <see cref="System.Collections.Generic.ISet{T}"/>,
    /// <c>IReadOnlySet{T}</c> (NET5_0_OR_GREATER only — absent from netstandard2.1),
    /// <see cref="System.Collections.Generic.SortedSet{T}"/>,
    /// or <see cref="IEnumerable{T}"/> based on <paramref name="targetType"/>.
    /// Set interface types (<c>ISet{T}</c>, <c>IReadOnlySet{T}</c>) materialise as
    /// <see cref="System.Collections.Generic.HashSet{T}"/>.
    /// <see cref="System.Collections.Generic.SortedSet{T}"/> materialises as
    /// <see cref="System.Collections.Generic.SortedSet{T}"/> with the default comparer,
    /// preserving natural sort order and deduplicating duplicate input elements.
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

        // b.6. SortedSet<T> dispatch (materialise as SortedSet<T> with default comparer)
        if (targetType.IsGenericType && targetType.GetGenericTypeDefinition() == typeof(SortedSet<>))
        {
            var elementType = targetType.GetGenericArguments()[0];
            var sortedSetType = typeof(SortedSet<>).MakeGenericType(elementType);
            var set = Activator.CreateInstance(sortedSetType)!;
            var addMethod = sortedSetType.GetMethod("Add")!;
            foreach (var item in seq.Items)
            {
                var element = DeserializeNode(item, elementType, options);
                addMethod.Invoke(set, new object?[] { element });
            }
            return set;
        }

        // c. IEnumerable<T> dispatch (materialise as List<T>)
        // Covers: IEnumerable<T> itself, ICollection<T>, IReadOnlyCollection<T>, IReadOnlyList<T>,
        // and any other interface that implements IEnumerable<T>. List<T> satisfies all of them.
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
                // Check if target implements IEnumerable<T> — result cached per type.
                if (!IEnumerableInterfaceCache.TryGetValue(targetType, out var iface))
                {
                    iface = targetType.GetInterface(typeof(IEnumerable<>).FullName!);
                    IEnumerableInterfaceCache.TryAdd(targetType, iface);
                }
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
        // IDictionary<string,T> is an interface — instantiate Dictionary<string,T> instead.
        var concreteType = targetType.GetGenericTypeDefinition() == typeof(IDictionary<,>)
            ? typeof(Dictionary<,>).MakeGenericType(typeof(string), valueType)
            : targetType;
        var dict = (IDictionary)Activator.CreateInstance(concreteType)!;

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
                {
                    if (underlying == typeof(string))
                        return (string?)scalar.Value;

                    var raw = scalar.Value as string ?? string.Empty;

                    if (underlying == typeof(DateTime))
                        return DateTime.ParseExact(raw, "O", CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind);

                    if (underlying == typeof(DateTimeOffset))
                        return DateTimeOffset.ParseExact(raw, "O", CultureInfo.InvariantCulture, DateTimeStyles.None);

                    if (underlying == typeof(TimeSpan))
                        return TimeSpan.ParseExact(raw, "c", CultureInfo.InvariantCulture);

#if NET6_0_OR_GREATER
                    if (underlying == typeof(DateOnly))
                        return DateOnly.ParseExact(raw, "yyyy-MM-dd", CultureInfo.InvariantCulture);

                    if (underlying == typeof(TimeOnly))
                        return TimeOnly.ParseExact(raw, "HH:mm:ss.FFFFFFF", CultureInfo.InvariantCulture);
#endif

                    return Convert.ChangeType(scalar.Value, underlying, CultureInfo.InvariantCulture);
                }

                case ScalarKind.Integer:
                {
                    // Parser produces long; use checked casts for common integral types to avoid
                    // boxing/unboxing on the hot path while preserving OverflowException semantics.
                    var rawLong = (long)scalar.Value!;
                    return underlying switch
                    {
                        _ when underlying == typeof(long)   => (object)rawLong,
                        _ when underlying == typeof(int)    => (object)checked((int)rawLong),
                        _ when underlying == typeof(short)  => (object)checked((short)rawLong),
                        _ when underlying == typeof(byte)   => (object)checked((byte)rawLong),
                        _ when underlying == typeof(sbyte)  => (object)checked((sbyte)rawLong),
                        _ when underlying == typeof(uint)   => (object)checked((uint)rawLong),
                        _ when underlying == typeof(ulong)  => (object)checked((ulong)rawLong),
                        _ when underlying == typeof(ushort) => (object)checked((ushort)rawLong),
                        _                                   => Convert.ChangeType(scalar.Value, underlying, CultureInfo.InvariantCulture),
                    };
                }

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

    // ── Extension value coercion ──────────────────────────────────────────────

    /// <summary>
    /// Materialises a <see cref="HumlNode"/> to a natural CLR value for storage in a
    /// <c>Dictionary&lt;string, object?&gt;</c> extension-data property.
    /// <list type="bullet">
    /// <item><see cref="HumlScalar"/> → <c>Value</c> directly (already the right CLR type).</item>
    /// <item><see cref="HumlDocument"/> → <c>Dictionary&lt;string, object?&gt;</c> (recursive).</item>
    /// <item><see cref="HumlInlineMapping"/> → <c>Dictionary&lt;string, object?&gt;</c> (recursive).</item>
    /// <item><see cref="HumlSequence"/> → <c>List&lt;object?&gt;</c> (recursive).</item>
    /// </list>
    /// </summary>
    [RequiresUnreferencedCode("Reflection-based HUML deserialisation.")]
    private static object? CoerceExtensionValue(HumlNode node, HumlOptions options)
    {
        if (node is HumlScalar scalar)
            return scalar.Value;

        if (node is HumlDocument doc)
        {
            var dict = new Dictionary<string, object?>(StringComparer.Ordinal);
            foreach (var entry in doc.Entries)
            {
                if (entry is HumlMapping m)
                    dict[m.Key] = CoerceExtensionValue(m.Value, options);
            }
            return dict;
        }

        if (node is HumlInlineMapping inline)
        {
            var dict = new Dictionary<string, object?>(StringComparer.Ordinal);
            foreach (var entry in inline.Entries)
            {
                if (entry is HumlMapping m)
                    dict[m.Key] = CoerceExtensionValue(m.Value, options);
            }
            return dict;
        }

        if (node is HumlSequence seq)
        {
            var list = new List<object?>(seq.Items.Count);
            foreach (var item in seq.Items)
                list.Add(CoerceExtensionValue(item, options));
            return list;
        }

        return null; // defensive — future node types
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
        var def = type.GetGenericTypeDefinition();
        if (def != typeof(Dictionary<,>) && def != typeof(IDictionary<,>))
            return false;
        return type.GetGenericArguments()[0] == typeof(string);
    }
}
