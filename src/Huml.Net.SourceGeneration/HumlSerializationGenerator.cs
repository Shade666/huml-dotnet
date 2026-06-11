using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace Huml.Net.SourceGeneration;

/// <summary>
/// Incremental source generator for Huml.Net. For each type registered via
/// <c>[HumlSerializable(typeof(T))]</c> on a <c>HumlGeneratedContext</c> subclass,
/// emits a concrete <c>HumlTypeInfo&lt;T&gt;</c> and the corresponding dispatch override.
/// </summary>
[Generator(LanguageNames.CSharp)]
public sealed class HumlSerializationGenerator : IIncrementalGenerator
{
    private const string AttributeFqn =
        "Huml.Net.Serialization.Attributes.HumlSerializableAttribute";
    private const string IgnoreAttributeFqn =
        "Huml.Net.Serialization.HumlIgnoreAttribute";
    private const string PropertyAttributeFqn =
        "Huml.Net.Serialization.HumlPropertyAttribute";

    /// <inheritdoc/>
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var contextModels = context.SyntaxProvider
            .ForAttributeWithMetadataName(
                AttributeFqn,
                predicate: static (node, _) => node is ClassDeclarationSyntax,
                transform: static (ctx, _) => TransformContext(ctx))
            .Where(static m => m.HasValue)
            .Select(static (m, _) => m!.Value);

        context.RegisterSourceOutput(contextModels,
            static (spc, model) => EmitSource(spc, model));
    }

    private static ContextModel? TransformContext(GeneratorAttributeSyntaxContext ctx)
    {
        if (ctx.TargetSymbol is not INamedTypeSymbol contextSymbol)
            return null;

        var types = new List<TypeModel>();

        foreach (var attr in ctx.Attributes)
        {
            if (attr.ConstructorArguments.Length == 0) continue;
            if (attr.ConstructorArguments[0].Value is not INamedTypeSymbol typeSymbol) continue;

            // Walk the type hierarchy from base to derived so properties appear base-first
            // (matching the reflection path's declaration-order convention). This ensures
            // derived types include inherited properties without the serialiser needing to
            // chain multiple TypeInfo lookups.
            var hierarchy = new List<INamedTypeSymbol>();
            var current = typeSymbol;
            while (current != null && current.SpecialType != SpecialType.System_Object)
            {
                hierarchy.Add(current);
                current = current.BaseType;
            }
            hierarchy.Reverse();

            var seen = new HashSet<string>(StringComparer.Ordinal);
            var properties = new List<PropertyModel>();
            foreach (var t in hierarchy)
            {
                var declaringFqn = t.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
                foreach (var member in t.GetMembers())
                {
                    if (member is not IPropertySymbol prop) continue;
                    if (prop.DeclaredAccessibility != Accessibility.Public) continue;
                    if (prop.IsStatic || prop.IsIndexer || prop.IsAbstract) continue;
                    if (!seen.Add(prop.Name)) continue; // skip overrides / hidden properties

                    // [HumlIgnore] excludes the property entirely (parity with the reflection path).
                    if (HasAttribute(prop, IgnoreAttributeFqn)) continue;

                    var canGet = prop.GetMethod?.DeclaredAccessibility == Accessibility.Public;
                    if (!canGet) continue;

                    // Only a genuinely settable (non-init) public setter can be assigned from a
                    // compiled delegate; init-only setters would emit CS8852, so HasSet excludes them.
                    var canSet = prop.SetMethod is { DeclaredAccessibility: Accessibility.Public, IsInitOnly: false };

                    // [HumlProperty("name")] overrides the HUML key.
                    var humlKey = GetHumlPropertyName(prop) ?? prop.Name;

                    properties.Add(new PropertyModel(
                        prop.Name,
                        humlKey,
                        prop.Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat),
                        canGet,
                        canSet,
                        declaringFqn));
                }
            }

            var fqn = typeSymbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
            // PropertyName is filled in below once all types are known (collision detection).
            types.Add(new TypeModel(typeSymbol.Name, typeSymbol.Name, MakeUniqueId(fqn), fqn,
                CanConstruct(typeSymbol),
                new EquatableArray<PropertyModel>(properties.ToArray())));
        }

        if (types.Count == 0) return null;

        // Public accessor names keep the simple type name where it is unique within the context;
        // types whose simple name collides fall back to the collision-free UniqueId so the
        // generated members do not clash (CS0102).
        var nameCounts = new Dictionary<string, int>(StringComparer.Ordinal);
        foreach (var t in types)
            nameCounts[t.Name] = nameCounts.TryGetValue(t.Name, out var c) ? c + 1 : 1;
        for (int i = 0; i < types.Count; i++)
            if (nameCounts[types[i].Name] > 1)
                types[i] = types[i] with { PropertyName = types[i].UniqueId };

        var ns = contextSymbol.ContainingNamespace.IsGlobalNamespace ? ""
            : contextSymbol.ContainingNamespace.ToDisplayString();

        return new ContextModel(contextSymbol.Name, ns,
            new EquatableArray<TypeModel>(types.ToArray()));
    }

    private static bool HasAttribute(ISymbol symbol, string attributeFqn)
    {
        foreach (var attr in symbol.GetAttributes())
            if (string.Equals(attr.AttributeClass?.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat),
                "global::" + attributeFqn, StringComparison.Ordinal))
                return true;
        return false;
    }

    private static string? GetHumlPropertyName(IPropertySymbol prop)
    {
        foreach (var attr in prop.GetAttributes())
        {
            if (!string.Equals(attr.AttributeClass?.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat),
                "global::" + PropertyAttributeFqn, StringComparison.Ordinal)) continue;
            // [HumlProperty(name)] — the first constructor argument, when a non-empty string.
            if (attr.ConstructorArguments.Length > 0
                && attr.ConstructorArguments[0].Value is string s && s.Length > 0)
                return s;
        }
        return null;
    }

    private static bool CanConstruct(INamedTypeSymbol type)
    {
        if (type.IsAbstract || type.IsStatic) return false;

        // A required member cannot be satisfied by a bare new T().
        if (HasRequiredMember(type)) return false;

        // Value types always have an implicit parameterless constructor.
        if (type.IsValueType) return true;

        foreach (var ctor in type.InstanceConstructors)
            if (ctor.Parameters.Length == 0 && ctor.DeclaredAccessibility == Accessibility.Public)
                return true;
        return false;
    }

    private static bool HasRequiredMember(INamedTypeSymbol type)
    {
        for (var t = type; t != null && t.SpecialType != SpecialType.System_Object; t = t.BaseType)
            foreach (var member in t.GetMembers())
                if (member is IPropertySymbol { IsRequired: true } or IFieldSymbol { IsRequired: true })
                    return true;
        return false;
    }

    /// <summary>Derives a collision-free C# identifier from a fully-qualified type name.</summary>
    private static string MakeUniqueId(string fqn)
    {
        var sb = new StringBuilder(fqn.Length);
        foreach (var c in fqn)
            sb.Append(char.IsLetterOrDigit(c) ? c : '_');
        return sb.ToString();
    }

    /// <summary>Escapes a C# identifier that collides with a keyword by prefixing <c>@</c>.</summary>
    private static string Escape(string identifier) =>
        Microsoft.CodeAnalysis.CSharp.SyntaxFacts.GetKeywordKind(identifier) != SyntaxKind.None
        || Microsoft.CodeAnalysis.CSharp.SyntaxFacts.GetContextualKeywordKind(identifier) != SyntaxKind.None
            ? "@" + identifier
            : identifier;

    /// <summary>Escapes a string literal for embedding in generated source.</summary>
    private static string EscapeLiteral(string value) =>
        value.Replace("\\", "\\\\").Replace("\"", "\\\"");

    private static void EmitSource(SourceProductionContext spc, ContextModel model)
    {
        var sb = new StringBuilder();

        sb.AppendLine("// <auto-generated/>");
        sb.AppendLine("#nullable enable");
        sb.AppendLine("#pragma warning disable CS1591 // Missing XML comment — generated code");
        sb.AppendLine();

        if (!string.IsNullOrEmpty(model.Namespace))
        {
            sb.AppendLine($"namespace {model.Namespace};");
            sb.AppendLine();
        }

        sb.AppendLine($"partial class {model.ClassName}");
        sb.AppendLine("{");

        sb.AppendLine($"    public static {model.ClassName} Default {{ get; }} = new {model.ClassName}();");
        sb.AppendLine();

        foreach (var type in model.Types.ToArray())
        {
            sb.AppendLine($"    public global::Huml.Net.Serialization.HumlTypeInfo<{type.FullyQualifiedName}> {type.PropertyName} {{ get; }}");
            sb.AppendLine($"        = new {type.UniqueId}HumlTypeInfo();");
            sb.AppendLine();
        }

        sb.AppendLine("    /// <inheritdoc/>");
        sb.AppendLine($"    public override global::Huml.Net.Serialization.HumlTypeInfo? GetTypeInfo(global::System.Type type, global::Huml.Net.Versioning.HumlOptions options)");
        sb.AppendLine("    {");
        foreach (var type in model.Types.ToArray())
            sb.AppendLine($"        if (type == typeof({type.FullyQualifiedName})) return {type.PropertyName};");
        sb.AppendLine("        return null;");
        sb.AppendLine("    }");

        foreach (var type in model.Types.ToArray())
        {
            sb.AppendLine();
            EmitTypeInfoClass(sb, type);
        }

        sb.AppendLine("}");

        // Hint name includes the namespace so two context classes that share a simple name
        // (in different namespaces) do not collide on the AddSource key.
        var hint = string.IsNullOrEmpty(model.Namespace)
            ? $"{model.ClassName}.Huml.g.cs"
            : $"{model.Namespace}.{model.ClassName}.Huml.g.cs";
        spc.AddSource(hint, SourceText.From(sb.ToString(), Encoding.UTF8));
    }

    private static void EmitTypeInfoClass(StringBuilder sb, TypeModel type)
    {
        sb.AppendLine($"    private sealed class {type.UniqueId}HumlTypeInfo : global::Huml.Net.Serialization.HumlTypeInfo<{type.FullyQualifiedName}>");
        sb.AppendLine("    {");
        sb.AppendLine($"        private static readonly global::System.Collections.Generic.IReadOnlyList<global::Huml.Net.Serialization.HumlPropertyInfo> _properties");
        sb.AppendLine("            = new global::Huml.Net.Serialization.HumlPropertyInfo[]");
        sb.AppendLine("            {");

        foreach (var prop in type.Properties.ToArray())
        {
            var access = Escape(prop.Name);
            sb.Append("                new() {");
            sb.Append($" Name = \"{EscapeLiteral(prop.HumlKey)}\",");
            sb.Append($" PropertyType = typeof({prop.TypeName}),");
            if (prop.HasGet)
                sb.Append($" Get = static o => (({prop.DeclaringTypeFqn})o).{access},");
            if (prop.HasSet)
                sb.Append($" Set = static (o, v) => (({prop.DeclaringTypeFqn})o).{access} = ({prop.TypeName})v!,");
            sb.AppendLine(" },");
        }

        sb.AppendLine("            };");
        sb.AppendLine();
        sb.AppendLine($"        public override global::System.Collections.Generic.IReadOnlyList<global::Huml.Net.Serialization.HumlPropertyInfo> Properties => _properties;");
        sb.AppendLine();
        // CreateObject is only emitted when new T() is valid; otherwise it is null and the
        // deserialiser falls back to its constructor-binding / reflection path.
        if (type.CanConstruct)
            sb.AppendLine($"        public override global::System.Func<{type.FullyQualifiedName}>? CreateObject => static () => new {type.FullyQualifiedName}();");
        else
            sb.AppendLine($"        public override global::System.Func<{type.FullyQualifiedName}>? CreateObject => null;");
        sb.AppendLine("    }");
    }
}
