using AwesomeAssertions;
using Huml.Net.Exceptions;
using Huml.Net.Serialization;
using Huml.Net.Versioning;
using Xunit;

namespace Huml.Net.Tests.Serialization;

public sealed class PolymorphicTests
{
    // Hierarchy 1: default discriminator key "_type"
    [HumlPolymorphic]
    [HumlDerivedType(typeof(SubA), "sub-a")]
    [HumlDerivedType(typeof(SubB), "sub-b")]
    private class PolyBase { public string Name { get; set; } = ""; }
    private class SubA : PolyBase { public int Count { get; set; } }
    private class SubB : PolyBase { public double Value { get; set; } }

    // Hierarchy 2: custom discriminator key "kind"
    [HumlPolymorphic("kind")]
    [HumlDerivedType(typeof(KindSub), "k-sub")]
    private class KindBase { public string Tag { get; set; } = ""; }
    private class KindSub : KindBase { public int Num { get; set; } }

    // Hierarchy 3: FallBackToBaseType
    [HumlPolymorphic(UnknownDerivedTypeHandling = HumlUnknownDerivedTypeHandling.FallBackToBaseType)]
    [HumlDerivedType(typeof(FallbackSub), "fb-sub")]
    private class FallbackBase { public string Name { get; set; } = ""; }
    private class FallbackSub : FallbackBase { public int Extra { get; set; } }

    [Fact]
    public void Poly01_RoundTrip_SubA_ThroughBase()
    {
        var huml = Huml.Serialize<PolyBase>(new SubA { Name = "x", Count = 3 });
        var result = Huml.Deserialize<PolyBase>(huml);
        result.Should().BeOfType<SubA>();
        var sub = (SubA)result!;
        sub.Name.Should().Be("x");
        sub.Count.Should().Be(3);
    }

    [Fact]
    public void Poly02_DiscriminatorStripped_WithDisallow_NoException()
    {
        // "_type" starts with '_' which is not a valid bare-key start in HUML — must be quoted.
        const string huml = "%HUML v0.2.0\n\"_type\": \"sub-a\"\nName: \"y\"\nCount: 7\n";
        var opts = new HumlOptions { UnmappedMemberHandling = UnmappedMemberHandling.Disallow };
        var act = () => Huml.Deserialize<PolyBase>(huml, opts);
        act.Should().NotThrow();
    }

    [Fact]
    public void Poly03_UnknownLabel_Throws()
    {
        const string huml = "%HUML v0.2.0\n\"_type\": \"no-such-type\"\nName: \"z\"\n";
        var act = () => Huml.Deserialize<PolyBase>(huml);
        act.Should().Throw<HumlDeserializeException>().WithMessage("*Unknown derived type discriminator value*");
    }

    [Fact]
    public void Poly04_UnknownLabel_FallBackToBaseType()
    {
        const string huml = "%HUML v0.2.0\n\"_type\": \"unknown-label\"\nName: \"w\"\n";
        var result = Huml.Deserialize<FallbackBase>(huml);
        result.Should().NotBeNull();
        result!.GetType().Should().Be(typeof(FallbackBase));
        result.Name.Should().Be("w");
    }

    [Fact]
    public void Poly05_MissingDiscriminatorKey_ReturnsBaseType()
    {
        const string huml = "%HUML v0.2.0\nName: \"base-only\"\n";
        var result = Huml.Deserialize<PolyBase>(huml);
        result.Should().NotBeNull();
        result!.GetType().Should().Be(typeof(PolyBase));
        result.Name.Should().Be("base-only");
    }

    [Fact]
    public void Poly06_Serialiser_FirstKeyIsDiscriminator()
    {
        var huml = Huml.Serialize<PolyBase>(new SubA { Name = "n", Count = 1 });
        var lines = huml.Split('\n');
        var firstDataLine = Array.Find(lines, l => l.Length > 0 && l[0] != '%');
        firstDataLine.Should().NotBeNull();
        // "_type" is not a valid HUML bare key (starts with '_'), so it is emitted quoted.
        firstDataLine!.Should().Contain("_type");
    }

    [Fact]
    public void Poly07_CustomDiscriminatorKey_RoundTrip()
    {
        var huml = Huml.Serialize<KindBase>(new KindSub { Tag = "t", Num = 42 });
        huml.Should().Contain("kind:").And.NotContain("_type:");
        var result = Huml.Deserialize<KindBase>(huml);
        result.Should().BeOfType<KindSub>();
        ((KindSub)result!).Num.Should().Be(42);
    }

    [Fact]
    public void Poly08_MultipleSubtypes_EachRoundTrips()
    {
        var humlA = Huml.Serialize<PolyBase>(new SubA { Name = "a", Count = 5 });
        var resultA = Huml.Deserialize<PolyBase>(humlA);
        resultA.Should().BeOfType<SubA>();
        ((SubA)resultA!).Count.Should().Be(5);

        var humlB = Huml.Serialize<PolyBase>(new SubB { Name = "b", Value = 3.14 });
        var resultB = Huml.Deserialize<PolyBase>(humlB);
        resultB.Should().BeOfType<SubB>();
        ((SubB)resultB!).Value.Should().Be(3.14);
    }
}
