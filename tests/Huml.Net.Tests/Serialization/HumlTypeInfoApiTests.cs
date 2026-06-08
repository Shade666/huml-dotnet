using AwesomeAssertions;
using Huml.Net.Serialization;
using Xunit;

namespace Huml.Net.Tests.Serialization;

public sealed class HumlTypeInfoApiTests
{
    // ── Test helpers ──────────────────────────────────────────────────────────

    private sealed class SimpleDto
    {
        public string Value { get; set; } = string.Empty;
    }

    /// <summary>Minimal concrete HumlTypeInfo subclass — no overrides.</summary>
    private sealed class MinimalTypeInfo : HumlTypeInfo
    {
    }

    /// <summary>Minimal concrete HumlTypeInfo&lt;string&gt; subclass — no overrides.</summary>
    private sealed class MinimalStringTypeInfo : HumlTypeInfo<string>
    {
    }

    /// <summary>HumlTypeInfo subclass that overrides Properties.</summary>
    private sealed class TypeInfoWithProperties : HumlTypeInfo
    {
        private readonly List<HumlPropertyInfo> _props;

        public TypeInfoWithProperties(List<HumlPropertyInfo> props) => _props = props;

        public override IReadOnlyList<HumlPropertyInfo>? Properties => _props;
    }

    /// <summary>HumlTypeInfo&lt;SimpleDto&gt; subclass that overrides CreateObject.</summary>
    private sealed class TypeInfoWithFactory : HumlTypeInfo<SimpleDto>
    {
        public override Func<SimpleDto>? CreateObject => () => new SimpleDto();
    }

    /// <summary>HumlTypeInfo subclass that overrides all four lifecycle callbacks.</summary>
    private sealed class TypeInfoWithCallbacks : HumlTypeInfo
    {
        public bool SerializingFired;
        public bool SerializedFired;
        public bool DeserializingFired;
        public bool DeserializedFired;

        public override Action<object>? OnSerializing => _ => SerializingFired = true;
        public override Action<object>? OnSerialized => _ => SerializedFired = true;
        public override Action<object>? OnDeserializing => _ => DeserializingFired = true;
        public override Action<object>? OnDeserialized => _ => DeserializedFired = true;
    }

    // ── TI01: HumlPropertyInfo default shape ─────────────────────────────────

    [Fact]
    public void TI01_HumlPropertyInfo_has_correct_default_shape()
    {
        var info = new HumlPropertyInfo();

        info.Name.Should().Be(string.Empty);
        info.PropertyType.Should().BeNull();
        info.Get.Should().BeNull();
        info.Set.Should().BeNull();
        info.IsRequired.Should().BeFalse();
        info.Order.Should().Be(0);
    }

    // ── TI02: HumlPropertyInfo — values read back correctly after assignment ──

    [Fact]
    public void TI02_HumlPropertyInfo_properties_assignable_and_delegates_execute()
    {
        object? captured = null;
        var info = new HumlPropertyInfo
        {
            Name = "Value",
            PropertyType = typeof(string),
            IsRequired = true,
            Order = 5,
            Get = obj => captured,
            Set = (_, v) => captured = v,
        };

        info.Name.Should().Be("Value");
        info.PropertyType.Should().Be<string>();
        info.IsRequired.Should().BeTrue();
        info.Order.Should().Be(5);

        info.Set!(new object(), "hello");
        var result = info.Get!(new object());
        result.Should().Be("hello");
    }

    // ── TI03: minimal HumlTypeInfo — all callbacks and Properties are null ────

    [Fact]
    public void TI03_minimal_HumlTypeInfo_subclass_all_members_are_null()
    {
        var info = new MinimalTypeInfo();

        info.Properties.Should().BeNull();
        info.OnSerializing.Should().BeNull();
        info.OnSerialized.Should().BeNull();
        info.OnDeserializing.Should().BeNull();
        info.OnDeserialized.Should().BeNull();
    }

    // ── TI04: minimal HumlTypeInfo<string> — Type and CreateObject defaults ──

    [Fact]
    public void TI04_minimal_HumlTypeInfoT_subclass_Type_and_CreateObject_defaults()
    {
        var info = new MinimalStringTypeInfo();

        info.Type.Should().Be<string>();
        info.CreateObject.Should().BeNull();
    }

    // ── TI05: HumlTypeInfo subclass overrides Properties ─────────────────────

    [Fact]
    public void TI05_HumlTypeInfo_override_Properties_returns_populated_list()
    {
        var prop = new HumlPropertyInfo { Name = "Id", PropertyType = typeof(int) };
        var info = new TypeInfoWithProperties(new List<HumlPropertyInfo> { prop });

        info.Properties.Should().NotBeNull();
        info.Properties!.Should().HaveCount(1);
        info.Properties[0].Should().BeSameAs(prop);
    }

    // ── TI06: HumlTypeInfo<SimpleDto> overrides CreateObject ─────────────────

    [Fact]
    public void TI06_HumlTypeInfoT_override_CreateObject_factory_creates_instance()
    {
        var info = new TypeInfoWithFactory();

        info.CreateObject.Should().NotBeNull();
        var instance = info.CreateObject!();
        instance.Should().NotBeNull();
        instance.Should().BeOfType<SimpleDto>();
    }

    // ── TI07: HumlTypeInfo subclass overrides all four lifecycle callbacks ────

    [Fact]
    public void TI07_HumlTypeInfo_override_all_lifecycle_callbacks_each_fires()
    {
        var info = new TypeInfoWithCallbacks();
        var dummy = new object();

        info.OnSerializing.Should().NotBeNull();
        info.OnSerialized.Should().NotBeNull();
        info.OnDeserializing.Should().NotBeNull();
        info.OnDeserialized.Should().NotBeNull();

        info.OnSerializing!(dummy);
        info.OnSerialized!(dummy);
        info.OnDeserializing!(dummy);
        info.OnDeserialized!(dummy);

        info.SerializingFired.Should().BeTrue();
        info.SerializedFired.Should().BeTrue();
        info.DeserializingFired.Should().BeTrue();
        info.DeserializedFired.Should().BeTrue();
    }

    // ── TI08: HumlPropertyInfo round-trips a real DTO property ───────────────

    [Fact]
    public void TI08_HumlPropertyInfo_real_delegates_round_trip_dto_property()
    {
        var info = new HumlPropertyInfo
        {
            Name = "Value",
            PropertyType = typeof(string),
            Get = obj => ((SimpleDto)obj).Value,
            Set = (obj, v) => ((SimpleDto)obj).Value = (string)v!,
        };

        var dto = new SimpleDto();
        info.Set!(dto, "hello");
        var result = info.Get!(dto);

        result.Should().Be("hello");
        dto.Value.Should().Be("hello");
    }
}
