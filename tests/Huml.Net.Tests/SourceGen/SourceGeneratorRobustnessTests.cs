using AwesomeAssertions;
using Xunit;

namespace Huml.Net.Tests.SourceGen;

/// <summary>
/// G3.2b: the source generator must never crash and must never emit code that fails to compile,
/// regardless of the shape of the registered type. Each test registers an exotic type shape from
/// the G3.2 review (docs/internals/g3-security-review.md) and asserts a clean compilation.
/// </summary>
public class SourceGeneratorRobustnessTests
{
    private static string Wrap(string body) => $$"""
        using Huml.Net.Serialization;
        using Huml.Net.Serialization.Attributes;

        {{body}}
        """;

    [Fact]
    public void Record_with_init_only_properties_compiles() // H8
    {
        var result = GeneratorTestHarness.Run(Wrap("""
            public record Person { public string Name { get; init; } = ""; public int Age { get; init; } }

            [HumlSerializable(typeof(Person))]
            public partial class Ctx : HumlGeneratedContext { }
            """));

        result.HasErrors.Should().BeFalse(because: ErrorReport(result));
    }

    [Fact]
    public void Type_with_parameterised_constructor_only_compiles() // H9
    {
        var result = GeneratorTestHarness.Run(Wrap("""
            public class Point { public Point(int x) { X = x; } public int X { get; } }

            [HumlSerializable(typeof(Point))]
            public partial class Ctx : HumlGeneratedContext { }
            """));

        result.HasErrors.Should().BeFalse(because: ErrorReport(result));
    }

    [Fact]
    public void Type_with_required_members_compiles() // H9
    {
        var result = GeneratorTestHarness.Run(Wrap("""
            public class Account { public required string Id { get; set; } }

            [HumlSerializable(typeof(Account))]
            public partial class Ctx : HumlGeneratedContext { }
            """));

        result.HasErrors.Should().BeFalse(because: ErrorReport(result));
    }

    [Fact]
    public void Two_context_classes_with_the_same_simple_name_compile() // H7
    {
        var result = GeneratorTestHarness.Run(Wrap("""
            public class Dto { public int X { get; set; } }

            namespace A { [HumlSerializable(typeof(global::Dto))] public partial class Ctx : Huml.Net.Serialization.HumlGeneratedContext { } }
            namespace B { [HumlSerializable(typeof(global::Dto))] public partial class Ctx : Huml.Net.Serialization.HumlGeneratedContext { } }
            """));

        result.HasErrors.Should().BeFalse(because: ErrorReport(result));
    }

    [Fact]
    public void Two_registered_types_with_the_same_simple_name_compile() // M14
    {
        var result = GeneratorTestHarness.Run(Wrap("""
            namespace X { public class Dto { public int A { get; set; } } }
            namespace Y { public class Dto { public int B { get; set; } } }

            [HumlSerializable(typeof(X.Dto))]
            [HumlSerializable(typeof(Y.Dto))]
            public partial class Ctx : HumlGeneratedContext { }
            """));

        result.HasErrors.Should().BeFalse(because: ErrorReport(result));
    }

    [Fact]
    public void Property_named_with_a_keyword_compiles() // M12
    {
        var result = GeneratorTestHarness.Run(Wrap("""
            public class KeywordProps { public int @class { get; set; } public string @event { get; set; } = ""; }

            [HumlSerializable(typeof(KeywordProps))]
            public partial class Ctx : HumlGeneratedContext { }
            """));

        result.HasErrors.Should().BeFalse(because: ErrorReport(result));
    }

    [Fact]
    public void Ignored_and_renamed_properties_are_honoured_in_generated_code() // H10
    {
        var result = GeneratorTestHarness.Run(Wrap("""
            public class Dto
            {
                [HumlIgnore] public string Secret { get; set; } = "";
                [HumlProperty("display_name")] public string Name { get; set; } = "";
            }

            [HumlSerializable(typeof(Dto))]
            public partial class Ctx : HumlGeneratedContext { }
            """));

        result.HasErrors.Should().BeFalse(because: ErrorReport(result));
        result.GeneratedCode.Should().NotContain("Secret", because: "[HumlIgnore] properties must be excluded from generated metadata");
        result.GeneratedCode.Should().Contain("display_name", because: "[HumlProperty] name override must appear as the HUML key");
    }

    [Fact]
    public void Simple_poco_still_compiles_and_has_no_diagnostics() // baseline
    {
        var result = GeneratorTestHarness.Run(Wrap("""
            public class Simple { public string Name { get; set; } = ""; public int Count { get; set; } }

            [HumlSerializable(typeof(Simple))]
            public partial class Ctx : HumlGeneratedContext { }
            """));

        result.HasErrors.Should().BeFalse(because: ErrorReport(result));
        result.GeneratedCode.Should().Contain("SimpleHumlTypeInfo");
    }

    private static string ErrorReport(GeneratorTestHarness.Result r) =>
        "no compilation errors expected, but got:\n" +
        string.Join("\n", r.Errors.Select(e => e.ToString())) +
        "\n\n--- generated ---\n" + r.GeneratedCode;
}
