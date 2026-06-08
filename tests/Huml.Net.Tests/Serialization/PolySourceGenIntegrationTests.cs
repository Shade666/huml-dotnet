using AwesomeAssertions;
using Huml.Net.Versioning;
using Xunit;

namespace Huml.Net.Tests.Serialization;

public sealed class PolySourceGenIntegrationTests
{
    private static readonly HumlOptions Options = new() { TypeInfoResolver = SGShapeContext.Default };

    [Fact(DisplayName = "PSG01: SGShapeContext provides TypeInfo for both SGShape and SGCircle")]
    public void PSG01_context_has_type_info_for_both_types()
    {
        SGShapeContext.Default.GetTypeInfo(typeof(SGShape), Options).Should().NotBeNull();
        SGShapeContext.Default.GetTypeInfo(typeof(SGCircle), Options).Should().NotBeNull();
    }

    [Fact(DisplayName = "PSG02: SGCircle TypeInfo includes inherited Color and own Radius (2 properties)")]
    public void PSG02_circle_type_info_includes_inherited_and_own_properties()
    {
        var typeInfo = SGShapeContext.Default.SGCircle;
        typeInfo.Properties.Should().NotBeNull();
        typeInfo.Properties!.Count.Should().Be(2);
    }

    [Fact(DisplayName = "PSG03: Serialising SGCircle via SGShape emits discriminator and both properties")]
    public void PSG03_serialise_derived_type_emits_discriminator_and_all_properties()
    {
        var circle = new SGCircle { Color = "red", Radius = 5.0 };
        var huml = Huml.Serialize<SGShape>(circle, Options);

        huml.Should().Contain("\"_type\": \"circle\"");
        huml.Should().Contain("Color: \"red\"");
        huml.Should().Contain("Radius:");
    }

    [Fact(DisplayName = "PSG04: Deserialising a discriminated document via SGShape restores SGCircle with all properties")]
    public void PSG04_deserialise_derived_type_restores_all_properties()
    {
        const string huml = "%HUML v0.2.0\n\"_type\": \"circle\"\nColor: \"red\"\nRadius: 5\n";

        var shape = Huml.Deserialize<SGShape>(huml, Options);
        shape.Should().NotBeNull();

        var circle = shape!.Should().BeOfType<SGCircle>().Subject;
        circle.Color.Should().Be("red");
        circle.Radius.Should().Be(5.0);
    }

    [Fact(DisplayName = "PSG05: Full round-trip for SGCircle via SGShape preserves all properties")]
    public void PSG05_round_trip_sgcircle_via_sgshape()
    {
        var original = new SGCircle { Color = "blue", Radius = 3.14 };
        var huml = Huml.Serialize<SGShape>(original, Options);
        var restored = Huml.Deserialize<SGShape>(huml, Options);

        restored.Should().NotBeNull();
        var circle = restored!.Should().BeOfType<SGCircle>().Subject;
        circle.Color.Should().Be("blue");
        circle.Radius.Should().BeApproximately(3.14, 0.001);
    }

    [Fact(DisplayName = "PSG06: Serialising a base SGShape instance emits no discriminator and uses source-gen path")]
    public void PSG06_round_trip_base_type_without_discriminator()
    {
        var original = new SGShape { Color = "green" };
        var huml = Huml.Serialize<SGShape>(original, Options);

        huml.Should().NotContain("_type");
        huml.Should().Contain("Color: \"green\"");

        var restored = Huml.Deserialize<SGShape>(huml, Options);
        restored.Should().NotBeNull();
        restored!.Should().BeOfType<SGShape>();
        restored.Color.Should().Be("green");
    }

    [Fact(DisplayName = "PSG07: Source-gen CreateObject factory works for both SGShape and SGCircle")]
    public void PSG07_create_object_factory_works_for_both_types()
    {
        SGShapeContext.Default.SGShape.CreateObject.Should().NotBeNull();
        SGShapeContext.Default.SGCircle.CreateObject.Should().NotBeNull();

        var shape = SGShapeContext.Default.SGShape.CreateObject!();
        shape.Should().BeOfType<SGShape>();

        var circle = SGShapeContext.Default.SGCircle.CreateObject!();
        circle.Should().BeOfType<SGCircle>();
    }
}
