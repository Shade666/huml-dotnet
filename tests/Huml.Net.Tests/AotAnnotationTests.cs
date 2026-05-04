#pragma warning disable IL2026, IL2111

using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using AwesomeAssertions;
using Huml.Net.Versioning;
using Xunit;

namespace Huml.Net.Tests;

/// <summary>
/// Verifies that the correct AOT/trim safety attributes are present or absent on the
/// public API methods of <see cref="Huml"/>. Each test corresponds to one TRIM-xx requirement.
/// </summary>
public class AotAnnotationTests
{
    // TRIM-01: Serialize<T> carries RequiresUnreferencedCode
    [Fact]
    public void Serialize_Generic_CarriesRequiresUnreferencedCodeAttribute()
    {
        var method = typeof(Huml).GetMethods(BindingFlags.Public | BindingFlags.Static)
            .Single(m => string.Equals(m.Name, "Serialize", StringComparison.Ordinal)
                      && m.IsGenericMethod
                      && m.GetParameters().Length == 2
                      && string.Equals(m.GetParameters()[0].Name, "value", StringComparison.Ordinal)
                      && m.GetParameters()[1].ParameterType == typeof(HumlOptions));

        method.GetCustomAttribute<RequiresUnreferencedCodeAttribute>()
              .Should().NotBeNull(because: "Serialize<T> uses reflection on T and must be annotated for trim safety (TRIM-01)");
    }

    // TRIM-02: Serialize<T> carries RequiresDynamicCode
    [Fact]
    public void Serialize_Generic_CarriesRequiresDynamicCodeAttribute()
    {
        var method = typeof(Huml).GetMethods(BindingFlags.Public | BindingFlags.Static)
            .Single(m => string.Equals(m.Name, "Serialize", StringComparison.Ordinal)
                      && m.IsGenericMethod
                      && m.GetParameters().Length == 2
                      && string.Equals(m.GetParameters()[0].Name, "value", StringComparison.Ordinal)
                      && m.GetParameters()[1].ParameterType == typeof(HumlOptions));

        method.GetCustomAttribute<RequiresDynamicCodeAttribute>()
              .Should().NotBeNull(because: "Serialize<T> may emit dynamic code and must carry [RequiresDynamicCode] (TRIM-02)");
    }

    // TRIM-03: Deserialize<T>(string) carries both annotations
    [Fact]
    public void Deserialize_StringGeneric_CarriesBothAnnotations()
    {
        var method = typeof(Huml).GetMethods(BindingFlags.Public | BindingFlags.Static)
            .Single(m => string.Equals(m.Name, "Deserialize", StringComparison.Ordinal)
                      && m.IsGenericMethod
                      && m.GetParameters()[0].ParameterType == typeof(string));

        method.GetCustomAttribute<RequiresUnreferencedCodeAttribute>()
              .Should().NotBeNull(because: "Deserialize<T>(string) must carry [RequiresUnreferencedCode] (TRIM-03)");
        method.GetCustomAttribute<RequiresDynamicCodeAttribute>()
              .Should().NotBeNull(because: "Deserialize<T>(string) must carry [RequiresDynamicCode] (TRIM-03)");
    }

    // TRIM-04: Deserialize<T>(ReadOnlySpan<char>) carries both annotations
    [Fact]
    public void Deserialize_SpanGeneric_CarriesBothAnnotations()
    {
        var method = typeof(Huml).GetMethods(BindingFlags.Public | BindingFlags.Static)
            .Single(m => string.Equals(m.Name, "Deserialize", StringComparison.Ordinal)
                      && m.IsGenericMethod
                      && m.GetParameters()[0].ParameterType == typeof(ReadOnlySpan<char>));

        method.GetCustomAttribute<RequiresUnreferencedCodeAttribute>()
              .Should().NotBeNull(because: "Deserialize<T>(ReadOnlySpan<char>) must carry [RequiresUnreferencedCode] (TRIM-04)");
        method.GetCustomAttribute<RequiresDynamicCodeAttribute>()
              .Should().NotBeNull(because: "Deserialize<T>(ReadOnlySpan<char>) must carry [RequiresDynamicCode] (TRIM-04)");
    }

    // TRIM-05: Deserialize(string, Type) carries both annotations
    [Fact]
    public void Deserialize_StringType_CarriesBothAnnotations()
    {
        var method = typeof(Huml).GetMethod(
            "Deserialize",
            BindingFlags.Public | BindingFlags.Static,
            binder: null,
            new[] { typeof(string), typeof(Type), typeof(HumlOptions) },
            modifiers: null);

        method!.GetCustomAttribute<RequiresUnreferencedCodeAttribute>()
               .Should().NotBeNull(because: "Deserialize(string, Type) must carry [RequiresUnreferencedCode] (TRIM-05)");
        method!.GetCustomAttribute<RequiresDynamicCodeAttribute>()
               .Should().NotBeNull(because: "Deserialize(string, Type) must carry [RequiresDynamicCode] (TRIM-05)");
    }

    // TRIM-06: Populate<T>(string) carries both annotations
    [Fact]
    public void Populate_StringGeneric_CarriesBothAnnotations()
    {
        var method = typeof(Huml).GetMethods(BindingFlags.Public | BindingFlags.Static)
            .Single(m => string.Equals(m.Name, "Populate", StringComparison.Ordinal)
                      && m.IsGenericMethod
                      && m.GetParameters()[0].ParameterType == typeof(string));

        method.GetCustomAttribute<RequiresUnreferencedCodeAttribute>()
              .Should().NotBeNull(because: "Populate<T>(string) must carry [RequiresUnreferencedCode] (TRIM-06)");
        method.GetCustomAttribute<RequiresDynamicCodeAttribute>()
              .Should().NotBeNull(because: "Populate<T>(string) must carry [RequiresDynamicCode] (TRIM-06)");
    }

    // TRIM-07: Populate<T>(ReadOnlySpan<char>) carries both annotations
    [Fact]
    public void Populate_SpanGeneric_CarriesBothAnnotations()
    {
        var method = typeof(Huml).GetMethods(BindingFlags.Public | BindingFlags.Static)
            .Single(m => string.Equals(m.Name, "Populate", StringComparison.Ordinal)
                      && m.IsGenericMethod
                      && m.GetParameters()[0].ParameterType == typeof(ReadOnlySpan<char>));

        method.GetCustomAttribute<RequiresUnreferencedCodeAttribute>()
              .Should().NotBeNull(because: "Populate<T>(ReadOnlySpan<char>) must carry [RequiresUnreferencedCode] (TRIM-07)");
        method.GetCustomAttribute<RequiresDynamicCodeAttribute>()
              .Should().NotBeNull(because: "Populate<T>(ReadOnlySpan<char>) must carry [RequiresDynamicCode] (TRIM-07)");
    }

    // TRIM-08: Serialize(object?, Type) carries both annotations
    [Fact]
    public void Serialize_ObjectType_CarriesBothAnnotations()
    {
        var method = typeof(Huml).GetMethod(
            "Serialize",
            BindingFlags.Public | BindingFlags.Static,
            binder: null,
            new[] { typeof(object), typeof(Type), typeof(HumlOptions) },
            modifiers: null);

        method!.GetCustomAttribute<RequiresUnreferencedCodeAttribute>()
               .Should().NotBeNull(because: "Serialize(object?, Type) must carry [RequiresUnreferencedCode] (TRIM-08)");
        method!.GetCustomAttribute<RequiresDynamicCodeAttribute>()
               .Should().NotBeNull(because: "Serialize(object?, Type) must carry [RequiresDynamicCode] (TRIM-08)");
    }

    // TRIM-09: Parse does NOT carry trim annotations
    [Fact]
    public void Parse_DoesNotCarryTrimAnnotations()
    {
        var method = typeof(Huml).GetMethod(
            "Parse",
            BindingFlags.Public | BindingFlags.Static,
            binder: null,
            new[] { typeof(string), typeof(HumlOptions) },
            modifiers: null);

        method!.GetCustomAttribute<RequiresUnreferencedCodeAttribute>()
               .Should().BeNull(because: "Parse only produces an AST — it does not reflect on user types, so it must not carry [RequiresUnreferencedCode] (TRIM-09)");
        method!.GetCustomAttribute<RequiresDynamicCodeAttribute>()
               .Should().BeNull(because: "Parse does not use dynamic code generation (TRIM-09)");
    }
}

#pragma warning restore IL2026, IL2111
