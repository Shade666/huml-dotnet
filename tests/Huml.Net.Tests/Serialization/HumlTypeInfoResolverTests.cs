using AwesomeAssertions;
using Huml.Net.Serialization;
using Huml.Net.Versioning;
using Xunit;

namespace Huml.Net.Tests.Serialization;

public sealed class HumlTypeInfoResolverTests
{
    // ── Test helpers ──────────────────────────────────────────────────────────

    private sealed class SimpleDto
    {
        public string Name { get; set; } = string.Empty;
        public int Count { get; set; }
    }

    /// <summary>Resolver that always returns null — simulates "I don't handle this type".</summary>
    private sealed class AlwaysNullResolver : IHumlTypeInfoResolver
    {
        public HumlTypeInfo? GetTypeInfo(Type type, HumlOptions options) => null;
    }

    /// <summary>Concrete instantiable subclass of HumlTypeInfo&lt;T&gt; for testing.</summary>
    private sealed class ConcreteTypeInfo<T> : HumlTypeInfo<T>
    {
    }

    /// <summary>Resolver that always returns a non-null HumlTypeInfo — simulates a stub source-gen resolver.</summary>
    private sealed class StubResolver : IHumlTypeInfoResolver
    {
        public HumlTypeInfo? GetTypeInfo(Type type, HumlOptions options) => new ConcreteTypeInfo<object>();
    }

    // Representative valid HUML v0.2 document for SimpleDto
    private const string HumlInput = "%HUML v0.2.0\nName: \"Alice\"\nCount: 42\n";

    // ── SGS-01: null resolver (not set) — deserialise falls through to reflection ──

    [Fact]
    public void SGS_01_NullResolver_Deserialize_ReturnsCorrectValues()
    {
        var opts = HumlOptions.LatestSupported;

        var result = HumlSerializer.Deserialize<SimpleDto>(HumlInput, opts);

        result.Name.Should().Be("Alice");
        result.Count.Should().Be(42);
    }

    // ── SGS-02: null resolver (not set) — serialise falls through to reflection ──

    [Fact]
    public void SGS_02_NullResolver_Serialize_ProducesValidHuml()
    {
        var opts = HumlOptions.LatestSupported;
        var dto = new SimpleDto { Name = "Bob", Count = 7 };

        var huml = HumlSerializer.Serialize(dto, opts);

        huml.Should().Contain("Name:").And.Contain("Bob");
    }

    // ── SGS-03: AlwaysNullResolver — deserialise falls through to reflection ────

    [Fact]
    public void SGS_03_AlwaysNullResolver_Deserialize_ReturnsCorrectValues()
    {
        var opts = new HumlOptions
        {
            VersionSource = VersionSource.Options,
            SpecVersion = HumlSpecVersion.V0_2,
            TypeInfoResolver = new AlwaysNullResolver(),
        };

        var result = HumlSerializer.Deserialize<SimpleDto>(HumlInput, opts);

        result.Name.Should().Be("Alice");
        result.Count.Should().Be(42);
    }

    // ── SGS-04: AlwaysNullResolver — serialise falls through to reflection ──────

    [Fact]
    public void SGS_04_AlwaysNullResolver_Serialize_DoesNotThrow()
    {
        var opts = new HumlOptions
        {
            VersionSource = VersionSource.Options,
            SpecVersion = HumlSpecVersion.V0_2,
            TypeInfoResolver = new AlwaysNullResolver(),
        };
        var dto = new SimpleDto { Name = "Carol", Count = 3 };

        var act = () => HumlSerializer.Serialize(dto, opts);

        act.Should().NotThrow();
    }

    // ── SGS-05: StubResolver (returns non-null) — deserialise does not throw ────

    [Fact]
    public void SGS_05_StubResolver_Deserialize_DoesNotThrow()
    {
        var opts = new HumlOptions
        {
            VersionSource = VersionSource.Options,
            SpecVersion = HumlSpecVersion.V0_2,
            TypeInfoResolver = new StubResolver(),
        };

        var act = () => HumlSerializer.Deserialize<SimpleDto>(HumlInput, opts);

        act.Should().NotThrow();
    }

    // ── SGS-06: StubResolver (returns non-null) — serialise does not throw ──────

    [Fact]
    public void SGS_06_StubResolver_Serialize_DoesNotThrow()
    {
        var opts = new HumlOptions
        {
            VersionSource = VersionSource.Options,
            SpecVersion = HumlSpecVersion.V0_2,
            TypeInfoResolver = new StubResolver(),
        };
        var dto = new SimpleDto { Name = "Dave", Count = 99 };

        var act = () => HumlSerializer.Serialize(dto, opts);

        act.Should().NotThrow();
    }

    // ── SGS-07: ConcreteTypeInfo<T>.Type returns typeof(T) ──────────────────────

    [Fact]
    public void SGS_07_ConcreteTypeInfo_Type_ReturnsExpectedClrType()
    {
        var info = new ConcreteTypeInfo<string>();

        info.Type.Should().Be<string>();
    }
}
