using System.Collections;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Text;
using Huml.Net.Exceptions;
using Huml.Net.Parser;
using Huml.Net.Versioning;

namespace Huml.Net.Serialization;

/// <summary>
/// Converts .NET objects to HUML text using the <see cref="PropertyDescriptor"/> cache
/// for property enumeration in declaration order.
/// </summary>
internal static class HumlSerializerImpl
{
    // ── Re-entry guard ────────────────────────────────────────────────────────

    [ThreadStatic]
    private static HashSet<Type>? _activeConverterTypes;

    // ── StringBuilder pool (Phase 18) ─────────────────────────────────────────
    // _pooledSb is reused across Serialize calls on the same thread to eliminate
    // per-call StringBuilder + char[] allocation. _serializationActive guards
    // re-entry from converters so the pool is never shared concurrently.

    [ThreadStatic]
    private static StringBuilder? _pooledSb;

    [ThreadStatic]
    private static bool _serializationActive;

    // ── Public entry points ───────────────────────────────────────────────────

    /// <summary>
    /// Serializes <paramref name="value"/> to a HUML-formatted string.
    /// </summary>
    /// <param name="value">The object to serialize. May be <c>null</c>.</param>
    /// <param name="options">Serialization options. Defaults to <see cref="HumlOptions.Default"/>.</param>
    /// <returns>The HUML text representation.</returns>
    [RequiresUnreferencedCode("Reflection-based HUML serialisation.")]
    [RequiresDynamicCode("Reflection-based HUML serialisation may emit dynamic code.")]
    internal static string Serialize(object? value, HumlOptions? options = null)
    {
        options ??= HumlOptions.Default;

        var usePool = !_serializationActive;
        StringBuilder sb;
        if (usePool)
        {
            _serializationActive = true;
            _pooledSb ??= new StringBuilder();
            sb = _pooledSb;
        }
        else
        {
            sb = new StringBuilder();
        }

        try
        {
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
        finally
        {
            if (usePool)
            {
                sb.Clear();
                _serializationActive = false;
            }
        }
    }

    /// <summary>
    /// Typed overload — serializes <paramref name="value"/> using <paramref name="type"/> as
    /// the declared type for property reflection. Used by the Phase 7 static entry point
    /// (<c>Huml.Serialize&lt;T&gt;</c>). Nested POCOs still use their runtime type.
    /// </summary>
    [RequiresUnreferencedCode("Reflection-based HUML serialisation.")]
    [RequiresDynamicCode("Reflection-based HUML serialisation may emit dynamic code.")]
    internal static string Serialize(object? value, Type type, HumlOptions? options = null)
    {
        options ??= HumlOptions.Default;

        var usePool = !_serializationActive;
        StringBuilder sb;
        if (usePool)
        {
            _serializationActive = true;
            _pooledSb ??= new StringBuilder();
            sb = _pooledSb;
        }
        else
        {
            sb = new StringBuilder();
        }

        try
        {
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
        finally
        {
            if (usePool)
            {
                sb.Clear();
                _serializationActive = false;
            }
        }
    }

    // ── Core serialization logic ──────────────────────────────────────────────

    [RequiresUnreferencedCode("Reflection-based HUML serialisation.")]
    private static void SerializeValue(StringBuilder sb, object? value, int depth, HumlOptions options, Type? declaredType = null, HumlNumberHandling? memberNumberHandling = null)
        => SerializeValueInternal(sb, value, depth, options, declaredType, memberNumberHandling);

    /// <summary>
    /// Core serialization dispatch. Called by <see cref="SerializeValue"/> and by
    /// <see cref="HumlWriterContext.AppendSerializedValue"/>.
    /// </summary>
    [RequiresUnreferencedCode("Reflection-based HUML serialisation.")]
    internal static void SerializeValueInternal(StringBuilder sb, object? value, int depth, HumlOptions options, Type? declaredType = null, HumlNumberHandling? memberNumberHandling = null)
    {
        if (value is null)
        {
            sb.Append("null");
            return;
        }

        // Converter dispatch — must precede all built-in dispatch.
        // Property-level converters are dispatched by EmitEntry; this path handles
        // type-level [HumlConverter] and HumlOptions.Converters.
        if (ConverterCache.TryGet(value.GetType(), options) is { } converter)
        {
            var valueType = value.GetType();
            _activeConverterTypes ??= new HashSet<Type>();
            if (!_activeConverterTypes.Add(valueType))
                throw new InvalidOperationException(
                    $"Converter re-entry detected for type '{valueType.Name}'. " +
                    "A converter must not call AppendSerializedValue with the same type it handles.");
            try
            {
                var ctx = new HumlWriterContext(sb, depth, options);
                converter.WriteObject(ctx, value);
            }
            finally
            {
                _activeConverterTypes.Remove(valueType);
            }
            return;
        }

        // string first — must precede IEnumerable since string is enumerable
        if (value is string str)
        {
            // v0.1 spec: backtick multiline string syntax (```...```) is not supported.
            // When targeting v0.1, any backtick emission path must fall back to AppendEscapedString.
            // This serialiser has no backtick emission path today; this comment is a correctness
            // guard for any future maintainer who introduces one:
            //   if (options.SpecVersion >= HumlSpecVersion.V0_2) { /* emit backtick */ }
            //   else { AppendEscapedString(sb, str); }
            // Until then, AppendEscapedString is always used, satisfying both v0.1 and v0.2.
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

        // Integer types — emit bare literal or quoted string
        if (IsIntegerType(value))
        {
            var formatted = ((IFormattable)value).ToString(null, CultureInfo.InvariantCulture);
            if ((memberNumberHandling ?? options.NumberHandling).HasFlag(HumlNumberHandling.WriteAsString))
            {
                sb.Append('"');
                sb.Append(formatted);
                sb.Append('"');
            }
            else
            {
                sb.Append(formatted);
            }
            return;
        }

        // Floating-point types
        if (value is double d)
        {
            if ((memberNumberHandling ?? options.NumberHandling).HasFlag(HumlNumberHandling.WriteAsString) &&
                !double.IsNaN(d) && !double.IsInfinity(d))
            {
                sb.Append('"');
                sb.Append(d.ToString("R", CultureInfo.InvariantCulture));
                sb.Append('"');
            }
            else
            {
                sb.Append(FormatDouble(d));
            }
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
            if ((memberNumberHandling ?? options.NumberHandling).HasFlag(HumlNumberHandling.WriteAsString))
            {
                sb.Append('"');
                sb.Append(f.ToString("R", CultureInfo.InvariantCulture));
                sb.Append('"');
            }
            else
            {
                sb.Append(f.ToString("G", CultureInfo.InvariantCulture));
            }
            return;
        }

        // decimal
        if (value is decimal dec)
        {
            var formatted = dec.ToString(CultureInfo.InvariantCulture);
            if ((memberNumberHandling ?? options.NumberHandling).HasFlag(HumlNumberHandling.WriteAsString))
            {
                sb.Append('"');
                sb.Append(formatted);
                sb.Append('"');
            }
            else
            {
                sb.Append(formatted);
            }
            return;
        }

        // Enum — emit as quoted string (member name or [HumlEnumValue] override, with optional policy transform)
        {
            var valueType = value.GetType();
            if (valueType.IsEnum)
            {
                var enumName = EnumNameCache.GetName(valueType, value, options.PropertyNamingPolicy);
                sb.Append('"');
                AppendEscapedString(sb, enumName);
                sb.Append('"');
                return;
            }
        }

        // Date/time types — emit as quoted ISO-8601 / canonical strings
        if (value is DateTime dt)
        {
            sb.Append('"');
            AppendEscapedString(sb, dt.ToString("O", CultureInfo.InvariantCulture));
            sb.Append('"');
            return;
        }
        if (value is DateTimeOffset dateTimeOffset)
        {
            sb.Append('"');
            AppendEscapedString(sb, dateTimeOffset.ToString("O", CultureInfo.InvariantCulture));
            sb.Append('"');
            return;
        }
        if (value is TimeSpan ts)
        {
            sb.Append('"');
            AppendEscapedString(sb, ts.ToString("c", CultureInfo.InvariantCulture));
            sb.Append('"');
            return;
        }
#if NET6_0_OR_GREATER
        if (value is DateOnly dateOnly)
        {
            sb.Append('"');
            AppendEscapedString(sb, dateOnly.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture));
            sb.Append('"');
            return;
        }
        if (value is TimeOnly timeOnly)
        {
            sb.Append('"');
            AppendEscapedString(sb, timeOnly.ToString("HH:mm:ss.FFFFFFF", CultureInfo.InvariantCulture));
            sb.Append('"');
            return;
        }
#endif

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
                "similar non-data types are not supported by HumlSerializerImpl.");
        }

        // POCO — reflect using PropertyDescriptor (pass declaredType for top-level type-directed dispatch)
        SerializeMappingBody(sb, value, depth, options, declaredType);
    }

    // ── Mapping (POCO / dictionary-as-mapping) ────────────────────────────────

    /// <summary>
    /// Emits mapping entries at <paramref name="depth"/> for a POCO.
    /// Each property is emitted as either <c>key: scalar\n</c> or <c>key::\n  ...</c>.
    /// </summary>
    [RequiresUnreferencedCode("Reflection-based HUML serialisation.")]
    private static void SerializeMappingBody(StringBuilder sb, object obj, int depth, HumlOptions options, Type? declaredType = null)
    {
        // Cycle/recursion guard: a self-referencing object graph would otherwise recurse
        // until the stack is exhausted, crashing the process with an uncatchable
        // StackOverflowException. Bounding on MaxRecursionDepth (mirrors the parser) turns
        // that into a catchable HumlSerializeException.
        GuardDepth(depth, options);

        // SGS seam: resolver-driven path. When the resolver supplies property metadata
        // (Properties non-null), use delegate-based emission and bypass reflection.
        // Prefer the runtime type's TypeInfo (covers polymorphic derived types whose own
        // TypeInfo includes inherited properties) before falling back to the declared type.
        var targetType = declaredType ?? obj.GetType();
        var typeInfo = options.TypeInfoResolver?.GetTypeInfo(obj.GetType(), options)
                      ?? options.TypeInfoResolver?.GetTypeInfo(targetType, options);

        // Polymorphic discriminator emit (POLY-05): when the declared type carries
        // [HumlPolymorphic] and the runtime type is a registered derived type, emit
        // the discriminator key as the first mapping entry.
        var polyAttr = PolymorphicMetadataCache.GetPolymorphicAttribute(targetType);
        if (polyAttr != null)
        {
            var runtimeType = obj.GetType();
            if (runtimeType != targetType)
            {
                foreach (var reg in PolymorphicMetadataCache.GetDerivedTypeRegistrations(targetType))
                {
                    if (reg.DerivedType == runtimeType)
                    {
                        sb.Append(Indent(depth));
                        AppendKey(sb, polyAttr.TypeDiscriminatorPropertyName);
                        sb.Append(": \"");
                        AppendEscapedString(sb, reg.TypeDiscriminator);
                        sb.Append("\"\n");
                        break;
                    }
                }
            }
        }

        if (typeInfo?.Properties is { } resolverProps)
        {
            typeInfo.OnSerializing?.Invoke(obj);
            foreach (var propInfo in resolverProps)
            {
                if (propInfo.Get is null) continue;
                var propValue = propInfo.Get(obj);
                EmitEntry(sb, Indent(depth), propInfo.Name, propValue, depth, options,
                          inlineOverride: false, converterOverride: null,
                          declaringType: obj.GetType(), numberHandlingOverride: null);
            }
            typeInfo.OnSerialized?.Invoke(obj);
            return;
        }

        var descriptorType = (polyAttr != null && obj.GetType() != targetType) ? obj.GetType() : targetType;
        var descriptors = PropertyDescriptor.GetDescriptors(descriptorType, options.PropertyNamingPolicy);
        var indent = Indent(depth);

        foreach (var desc in descriptors)
        {
            object? propValue;
            try
            {
                propValue = desc.Property.GetValue(obj);
            }
            catch (System.Reflection.TargetInvocationException ex)
            {
                // A property getter that throws must surface as HumlSerializeException, not
                // leak the reflection wrapper type out of the public API.
                throw new HumlSerializeException(
                    $"The getter for property '{desc.Property.Name}' on type '{obj.GetType().Name}' threw "
                    + $"{ex.InnerException?.GetType().Name ?? "an exception"}: {ex.InnerException?.Message}",
                    ex.InnerException ?? ex);
            }

            // Precedence chain (highest to lowest, per D-09):
            // 1. Per-property [HumlProperty(OmitIfDefault = true)]
            if (desc.OmitIfDefault && Equals(propValue, desc.DefaultValue))
                continue;

            // 2. Class-level [HumlIgnoreDefaults] — WhenWritingDefault semantics
            if (desc.ClassIgnoresDefaults && Equals(propValue, desc.DefaultValue))
                continue;

            // 3. Global HumlOptions.DefaultIgnoreCondition
            if (options.DefaultIgnoreCondition != HumlIgnoreCondition.Never)
            {
                bool shouldOmit = options.DefaultIgnoreCondition switch
                {
                    HumlIgnoreCondition.Always             => true,
                    HumlIgnoreCondition.WhenWritingDefault => Equals(propValue, desc.DefaultValue),
                    HumlIgnoreCondition.WhenWritingNull    => propValue is null,
                    // Defensive fallback for future composite flags (e.g. WhenWritingEmpty = 4).
                    // HasFlag is safe for int-backed enums and is compiler-inlined.
                    _ => (options.DefaultIgnoreCondition.HasFlag(HumlIgnoreCondition.WhenWritingDefault)
                              && Equals(propValue, desc.DefaultValue))
                         || (options.DefaultIgnoreCondition.HasFlag(HumlIgnoreCondition.WhenWritingNull)
                              && propValue is null),
                };
                if (shouldOmit) continue;
            }

            EmitEntry(sb, indent, desc.HumlKey, propValue, depth, options, desc.Inline, desc.Converter, declaringType: obj.GetType(), numberHandlingOverride: desc.NumberHandling, declaredValueType: desc.Property.PropertyType);
        }

        // Emit extension-data entries after all declared properties (EXT-04).
        var extDesc = PropertyDescriptor.GetExtensionDataDescriptor(
            descriptorType, options.PropertyNamingPolicy);
        if (extDesc != null)
        {
            var extVal = extDesc.Property.GetValue(obj);
            if (extVal is Dictionary<string, HumlNode> nodeDict && nodeDict.Count > 0)
            {
                foreach (var kvp in nodeDict)
                    EmitHumlNode(sb, indent, kvp.Key, kvp.Value, depth, options);
            }
            else if (extVal is Dictionary<string, object?> objDict && objDict.Count > 0)
            {
                foreach (var kvp in objDict)
                    EmitEntry(sb, indent, kvp.Key, kvp.Value, depth, options);
            }
        }
    }

    /// <summary>
    /// Emits a single key-value entry.
    /// Scalars use <c>key: value\n</c>; complex values use <c>key::\n</c> then body.
    /// When <paramref name="inlineOverride"/> is non-null it takes precedence over
    /// <see cref="HumlOptions.CollectionFormat"/> for collection properties.
    /// When <paramref name="converterOverride"/> is non-null it is invoked at highest priority
    /// (property-level converter wins over type-level and options-level).
    /// </summary>
    [RequiresUnreferencedCode("Reflection-based HUML serialisation.")]
    private static void EmitEntry(
        StringBuilder sb,
        string indent,
        string key,
        object? value,
        int depth,
        HumlOptions options,
        bool? inlineOverride = null,
        HumlConverter? converterOverride = null,
        Type? declaringType = null,
        HumlNumberHandling? numberHandlingOverride = null,
        Type? declaredValueType = null)
    {
        // Property-level converter dispatch (highest priority — wins over type-level and options)
        if (converterOverride != null)
        {
            var valueType = value?.GetType();
            // When value is null, fall back to the converter's own type as the re-entry guard key
            // so recursion through null-valued converter calls is still detected.
            var guardType = valueType ?? converterOverride.GetType();
            _activeConverterTypes ??= new HashSet<Type>();
            bool added = _activeConverterTypes.Add(guardType);
            if (!added)
                throw new InvalidOperationException(
                    $"Converter re-entry detected for type '{guardType.Name}'. " +
                    "A converter must not call AppendSerializedValue with the same type it handles.");
            try
            {
                sb.Append(indent);
                AppendKey(sb, key);
                sb.Append(": ");
                var ctx = new HumlWriterContext(sb, depth, options);
                converterOverride.WriteObject(ctx, value);
                sb.Append('\n');
            }
            finally
            {
                if (added)
                    _activeConverterTypes.Remove(guardType);
            }
            return;
        }

        if (IsScalarValue(value, options))
        {
            sb.Append(indent);
            AppendKey(sb, key);
            sb.Append(": ");
            SerializeValue(sb, value, depth + 1, options, memberNumberHandling: numberHandlingOverride);
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
            if (wantInline && AllDictionaryValuesAreScalar(dict, options))
            {
                EmitInlineDictionary(sb, indent, key, dict, options, numberHandlingOverride);
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
            if (wantInline && items.TrueForAll(i => IsScalarValue(i, options)))
            {
                EmitInlineSequence(sb, indent, key, items, depth, options, numberHandlingOverride);
                return;
            }
            sb.Append(indent);
            AppendKey(sb, key);
            sb.Append("::\n");
            EmitSequenceItems(sb, items, depth + 1, options, numberHandlingOverride,
                elementDeclaredType: PolymorphicBaseOrNull(ElementTypeOrNull(declaredValueType)));
            return;
        }

        // POCO object (not null — null was handled by IsScalarValue)
        var valueType2 = value!.GetType();
        if (IsUnsupportedType(valueType2))
        {
            var msg = declaringType != null
                ? $"Cannot serialize property '{key}' on type '{declaringType.Name}': delegates, " +
                  "function pointers, and similar non-data types are not supported by HumlSerializerImpl."
                : $"Cannot serialize type '{valueType2.FullName}': delegates, function pointers, and " +
                  "similar non-data types are not supported by HumlSerializerImpl.";
            throw new HumlSerializeException(msg);
        }
        sb.Append(indent);
        AppendKey(sb, key);
        int marker = sb.Length;
        sb.Append("::\n");
        int bodyStart = sb.Length;
        // Pass the declared property type so a polymorphic base type emits its discriminator
        // even in nested position (the runtime type alone loses the [HumlPolymorphic] base).
        SerializeMappingBody(sb, value!, depth + 1, options,
            declaredType: PolymorphicBaseOrNull(declaredValueType));
        if (sb.Length == bodyStart)
        {
            // A POCO with no serialisable members would otherwise leave a dangling "key::"
            // that fails to re-parse (ambiguous empty vector). Emit the empty-dict signifier.
            sb.Length = marker;
            sb.Append(":: {}\n");
        }
    }

    /// <summary>
    /// Throws <see cref="HumlSerializeException"/> when serialisation recursion exceeds
    /// <see cref="HumlOptions.MaxRecursionDepth"/> — the catchable guard against cyclic or
    /// pathologically deep object graphs that would otherwise overflow the stack.
    /// </summary>
    private static void GuardDepth(int depth, HumlOptions options)
    {
        if (depth > options.MaxRecursionDepth)
            throw new HumlSerializeException(
                $"Serialisation exceeded the maximum depth of {options.MaxRecursionDepth}. "
                + "This usually indicates a cyclic object graph; raise HumlOptions.MaxRecursionDepth "
                + "if the data is genuinely this deep.");
    }

    /// <summary>
    /// Returns <paramref name="declaredType"/> if it carries <c>[HumlPolymorphic]</c>, else null.
    /// Passing a non-polymorphic declared type down would needlessly force the runtime type to be
    /// treated as a declared type; only polymorphic bases need the discriminator emit.
    /// </summary>
    private static Type? PolymorphicBaseOrNull(Type? declaredType) =>
        declaredType != null && PolymorphicMetadataCache.GetPolymorphicAttribute(declaredType) != null
            ? declaredType
            : null;

    /// <summary>
    /// Returns the element type of an array or <c>IEnumerable&lt;T&gt;</c>-implementing type,
    /// or null when none can be determined.
    /// </summary>
    [UnconditionalSuppressMessage("Trimming", "IL2070",
        Justification = "Whole serialiser is [RequiresUnreferencedCode]; only used to read a generic element type for polymorphic dispatch.")]
    private static Type? ElementTypeOrNull(Type? collectionType)
    {
        if (collectionType is null) return null;
        if (collectionType.IsArray) return collectionType.GetElementType();
        foreach (var i in collectionType.GetInterfaces())
            if (i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IEnumerable<>))
                return i.GetGenericArguments()[0];
        return null;
    }

    /// <summary>Formats a dictionary key invariantly (culture-independent), never null.</summary>
    private static string FormatDictionaryKey(object? key) => key switch
    {
        null => "null",
        string s => s,
        IFormattable f => f.ToString(null, CultureInfo.InvariantCulture),
        _ => key.ToString() ?? "null",
    };

    /// <summary>
    /// Emits a scalar-only sequence in inline format: <c>key:: v1, v2, v3\n</c>.
    /// Caller must verify all items are scalar before calling.
    /// </summary>
    [RequiresUnreferencedCode("Reflection-based HUML serialisation.")]
    private static void EmitInlineSequence(
        StringBuilder sb, string indent, string key, List<object?> items, int depth, HumlOptions options,
        HumlNumberHandling? memberNumberHandling = null)
    {
        sb.Append(indent);
        AppendKey(sb, key);
        sb.Append(":: ");
        for (int i = 0; i < items.Count; i++)
        {
            if (i > 0) sb.Append(", ");
            SerializeValue(sb, items[i], depth + 1, options, memberNumberHandling: memberNumberHandling);
        }
        sb.Append('\n');
    }

    /// <summary>
    /// Emits a scalar-valued dictionary in inline format: <c>key:: k1: v1, k2: v2\n</c>.
    /// Caller must verify all values are scalar before calling.
    /// </summary>
    [RequiresUnreferencedCode("Reflection-based HUML serialisation.")]
    private static void EmitInlineDictionary(
        StringBuilder sb, string indent, string key, IDictionary dict, HumlOptions options,
        HumlNumberHandling? memberNumberHandling = null)
    {
        // NOTE: ValidateDuplicateKeysOnWrite is not checked here. This path is only reachable
        // when CollectionFormat.Inline is set and all dictionary values are scalar. Duplicate-key
        // detection for inline dictionaries is deferred to a future phase.
        sb.Append(indent);
        AppendKey(sb, key);
        sb.Append(":: ");
        bool first = true;
        foreach (DictionaryEntry entry in dict)
        {
            if (!first) sb.Append(", ");
            first = false;
            var entryKey = FormatDictionaryKey(entry.Key);
            AppendKey(sb, entryKey);
            sb.Append(": ");
            SerializeValue(sb, entry.Value, 0, options, memberNumberHandling: memberNumberHandling);
        }
        sb.Append('\n');
    }

    /// <summary>
    /// Emits a <see cref="HumlNode"/> AST value as a HUML mapping entry.
    /// Used by the extension-data serialisation path for
    /// <c>Dictionary&lt;string, HumlNode&gt;</c> properties.
    /// </summary>
    /// <remarks>
    /// <see cref="HumlInlineMapping"/> entries are re-emitted as multiline blocks
    /// (semantically lossless; inline-vs-multiline distinction is a formatting concern only).
    /// </remarks>
    [RequiresUnreferencedCode("Reflection-based HUML serialisation.")]
    private static void EmitHumlNode(
        StringBuilder sb,
        string indent,
        string key,
        HumlNode node,
        int depth,
        HumlOptions options)
    {
        if (node is HumlScalar scalar)
        {
            sb.Append(indent);
            AppendKey(sb, key);
            sb.Append(": ");
            switch (scalar.Kind)
            {
                case ScalarKind.Null: sb.Append("null"); break;
                case ScalarKind.NaN:  sb.Append("nan");  break;
                case ScalarKind.Inf:  sb.Append(scalar.Value as string ?? "inf"); break;
                default:
                    // Bool, String, Integer, Float — Value is the correct CLR type; route through
                    // SerializeValue for consistent string/number/bool formatting.
                    SerializeValue(sb, scalar.Value, depth + 1, options);
                    break;
            }
            sb.Append('\n');
            return;
        }

        if (node is HumlDocument doc)
        {
            sb.Append(indent);
            AppendKey(sb, key);
            if (doc.Entries.Count == 0) { sb.Append(":: {}\n"); return; }
            sb.Append("::\n");
            var childIndent = Indent(depth + 1);
            foreach (var entry in doc.Entries)
            {
                if (entry is HumlMapping m)
                    EmitHumlNode(sb, childIndent, m.Key, m.Value, depth + 1, options);
            }
            return;
        }

        if (node is HumlInlineMapping inlineMap)
        {
            // Inline mappings are re-emitted as multiline blocks (semantically lossless, v1 behaviour).
            sb.Append(indent);
            AppendKey(sb, key);
            if (inlineMap.Entries.Count == 0) { sb.Append(":: {}\n"); return; }
            sb.Append("::\n");
            var childIndent = Indent(depth + 1);
            foreach (var entry in inlineMap.Entries)
            {
                if (entry is HumlMapping m)
                    EmitHumlNode(sb, childIndent, m.Key, m.Value, depth + 1, options);
            }
            return;
        }

        if (node is HumlSequence seq)
        {
            sb.Append(indent);
            AppendKey(sb, key);
            if (seq.Items.Count == 0) { sb.Append(":: []\n"); return; }
            sb.Append("::\n");
            EmitHumlSequenceItems(sb, seq, depth + 1, options);
            return;
        }
        // Defensive: unknown future node types are silently skipped.
    }

    /// <summary>
    /// Emits the items of an AST <see cref="HumlSequence"/> at <paramref name="depth"/>.
    /// Vector items use the "- ::" form; inline mappings are re-emitted as multiline
    /// blocks (semantically lossless, consistent with <see cref="EmitHumlNode"/>).
    /// </summary>
    [RequiresUnreferencedCode("Reflection-based HUML serialisation.")]
    private static void EmitHumlSequenceItems(StringBuilder sb, HumlSequence seq, int depth, HumlOptions options)
    {
        var itemIndent = Indent(depth);
        foreach (var item in seq.Items)
        {
            sb.Append(itemIndent);
            sb.Append("- ");
            switch (item)
            {
                case HumlScalar s:
                    switch (s.Kind)
                    {
                        case ScalarKind.Null: sb.Append("null"); break;
                        case ScalarKind.NaN:  sb.Append("nan");  break;
                        case ScalarKind.Inf:  sb.Append(s.Value as string ?? "inf"); break;
                        default: SerializeValue(sb, s.Value, depth + 1, options); break;
                    }
                    sb.Append('\n');
                    break;

                case HumlSequence nestedSeq when nestedSeq.Items.Count == 0:
                    sb.Append(":: []\n");
                    break;

                case HumlSequence nestedSeq:
                    sb.Append("::\n");
                    EmitHumlSequenceItems(sb, nestedSeq, depth + 1, options);
                    break;

                case HumlDocument { Entries.Count: 0 }:
                case HumlInlineMapping { Entries.Count: 0 }:
                    sb.Append(":: {}\n");
                    break;

                case HumlDocument childDoc:
                    sb.Append("::\n");
                    EmitMappingEntries(sb, childDoc.Entries, depth + 1, options);
                    break;

                case HumlInlineMapping inline:
                    sb.Append("::\n");
                    EmitMappingEntries(sb, inline.Entries, depth + 1, options);
                    break;
            }
        }
    }

    [RequiresUnreferencedCode("Reflection-based HUML serialisation.")]
    private static void EmitMappingEntries(
        StringBuilder sb, IReadOnlyList<HumlNode> entries, int depth, HumlOptions options)
    {
        var childIndent = Indent(depth);
        foreach (var entry in entries)
        {
            if (entry is HumlMapping m)
                EmitHumlNode(sb, childIndent, m.Key, m.Value, depth, options);
        }
    }

    /// <summary>
    /// Returns <c>true</c> when every value in <paramref name="dict"/> is a scalar
    /// (eligible for inline dictionary format).
    /// </summary>
    [RequiresUnreferencedCode("Reflection-based HUML serialisation.")]
    private static bool AllDictionaryValuesAreScalar(IDictionary dict, HumlOptions options)
    {
        foreach (DictionaryEntry e in dict)
            if (!IsScalarValue(e.Value, options)) return false;
        return true;
    }

    // ── Sequence (list / array) ───────────────────────────────────────────────

    /// <summary>
    /// Emits items of an <see cref="IEnumerable"/> as sequence entries at <paramref name="depth"/>.
    /// This is the single shared implementation for all sequence serialisation paths.
    /// </summary>
    [RequiresUnreferencedCode("Reflection-based HUML serialisation.")]
    private static void EmitSequenceItems(
        StringBuilder sb, IEnumerable items, int depth, HumlOptions options,
        HumlNumberHandling? memberNumberHandling = null,
        Type? elementDeclaredType = null)
    {
        GuardDepth(depth, options);
        var indent = Indent(depth);
        foreach (var item in items)
        {
            sb.Append(indent);
            sb.Append("- ");
            if (IsScalarValue(item, options))
            {
                SerializeValue(sb, item, depth + 1, options, memberNumberHandling: memberNumberHandling);
                sb.Append('\n');
            }
            else
            {
                // Vector list items use the "- ::" form (grammar: multiline_list_item =
                // "- " MULTILINE_VECTOR_START …) with the block one level deeper.
                int mark = sb.Length;
                sb.Append("::\n");
                int bodyStart = sb.Length;
                bool isListLike = false;
                if (item is IDictionary dict2)
                    SerializeDictionaryBody(sb, dict2, depth + 1, options);
                else if (item is IEnumerable nested and not string)
                {
                    isListLike = true;
                    EmitSequenceItems(sb, nested, depth + 1, options, memberNumberHandling,
                        elementDeclaredType: PolymorphicBaseOrNull(ElementTypeOrNull(elementDeclaredType)));
                }
                else if (item != null)
                {
                    var itemType = item.GetType();
                    if (IsUnsupportedType(itemType))
                        throw new HumlSerializeException(
                            $"Cannot serialize type '{itemType.FullName}': delegates, function pointers, and " +
                            "similar non-data types are not supported by HumlSerializerImpl.");
                    // Pass the declared element type so polymorphic collection elements emit
                    // their discriminator.
                    SerializeMappingBody(sb, item, depth + 1, options, declaredType: elementDeclaredType);
                }
                if (sb.Length == bodyStart)
                {
                    // Empty vector item: a bare "- ::" is the ambiguous-empty-vector error,
                    // so fall back to the inline empty signifier.
                    sb.Length = mark;
                    sb.Append(isListLike ? ":: []\n" : ":: {}\n");
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
    [RequiresUnreferencedCode("Reflection-based HUML serialisation.")]
    private static void SerializeDictionaryBody(
        StringBuilder sb,
        IDictionary dict,
        int depth,
        HumlOptions options)
    {
        var indent = Indent(depth);
        HashSet<string>? seenKeys = options.ValidateDuplicateKeysOnWrite
            ? new HashSet<string>(StringComparer.Ordinal)
            : null;

        foreach (DictionaryEntry entry in dict)
        {
            var key = FormatDictionaryKey(entry.Key);
            var value = entry.Value;

            if (seenKeys != null && !seenKeys.Add(key))
                throw new HumlSerializeException(
                    $"Duplicate key '{key}' encountered during serialisation of Dictionary. " +
                    "Set ValidateDuplicateKeysOnWrite = false to permit duplicates, or remove " +
                    "the duplicate before serialising.");

            // Dictionary entries are always emitted multiline — inline is a POCO-property-level concern
            EmitEntry(sb, indent, key, value, depth, options, inlineOverride: false);
        }
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    /// <summary>Returns <c>true</c> if <paramref name="value"/> should be emitted inline after <c>: </c>.</summary>
    [RequiresUnreferencedCode("Reflection-based HUML serialisation.")]
    private static bool IsScalarValue(object? value, HumlOptions? options = null)
    {
        if (value is null) return true;
        if (value is string) return true;
        if (value is bool) return true;
        if (IsIntegerType(value)) return true;
        if (value is double or float or decimal) return true;
        if (value.GetType().IsEnum) return true;

        if (value is DateTime or DateTimeOffset or TimeSpan) return true;
#if NET6_0_OR_GREATER
        if (value is DateOnly or TimeOnly) return true;
#endif

        // Converter-handled types are treated as scalar (inline after key: )
        if (options != null && ConverterCache.TryGet(value.GetType(), options) != null) return true;

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

    private static readonly string[] IndentCache = BuildIndentCache(64);

    private static string[] BuildIndentCache(int maxDepth)
    {
        var cache = new string[maxDepth + 1];
        for (int i = 0; i <= maxDepth; i++)
            cache[i] = new string(' ', i * 2);
        return cache;
    }

    private static string Indent(int depth) =>
        (uint)depth < (uint)IndentCache.Length
            ? IndentCache[depth]
            : new string(' ', depth * 2);
}
