using AwesomeAssertions;
using Huml.Net.Serialization;
using Huml.Net.Versioning;
using Xunit;

namespace Huml.Net.Tests.Serialization;

public sealed class TypeInfoResolverActivationTests
{
    private static readonly string[] SerializingThenSerialized = ["serializing", "serialized"];
    private static readonly string[] DeserializingThenDeserialized = ["deserializing", "deserialized"];

    // ── Helpers ───────────────────────────────────────────────────────────────

    private sealed class TrackingDto
    {
        public string Name { get; set; } = string.Empty;
        public int Count { get; set; }
        public bool SetWasCalled { get; set; }
    }

    private sealed class TestTypeInfo<T> : HumlTypeInfo<T>
    {
        private readonly IReadOnlyList<HumlPropertyInfo>? _properties;
        private readonly Action<object>? _onSerializing;
        private readonly Action<object>? _onSerialized;
        private readonly Action<object>? _onDeserializing;
        private readonly Action<object>? _onDeserialized;

        public TestTypeInfo(
            IReadOnlyList<HumlPropertyInfo>? properties = null,
            Action<object>? onSerializing = null,
            Action<object>? onSerialized = null,
            Action<object>? onDeserializing = null,
            Action<object>? onDeserialized = null)
        {
            _properties = properties;
            _onSerializing = onSerializing;
            _onSerialized = onSerialized;
            _onDeserializing = onDeserializing;
            _onDeserialized = onDeserialized;
        }

        public override IReadOnlyList<HumlPropertyInfo>? Properties => _properties;
        public override Action<object>? OnSerializing => _onSerializing;
        public override Action<object>? OnSerialized => _onSerialized;
        public override Action<object>? OnDeserializing => _onDeserializing;
        public override Action<object>? OnDeserialized => _onDeserialized;
    }

    private sealed class TestResolver<T> : IHumlTypeInfoResolver
    {
        private readonly HumlTypeInfo _typeInfo;

        public TestResolver(HumlTypeInfo typeInfo) => _typeInfo = typeInfo;

        public HumlTypeInfo? GetTypeInfo(Type type, HumlOptions options)
            => type == typeof(T) ? _typeInfo : null;
    }

    private static HumlOptions BuildOptions(IHumlTypeInfoResolver resolver)
        => new()
        {
            VersionSource = VersionSource.Options,
            SpecVersion = HumlSpecVersion.V0_2,
            TypeInfoResolver = resolver
        };

    // ── TRA01 ─────────────────────────────────────────────────────────────────

    [Fact]
    public void TRA01_resolver_drives_serialization()
    {
        var props = new[]
        {
            new HumlPropertyInfo
            {
                Name = "Name",
                PropertyType = typeof(string),
                Get = _ => "INJECTED"
            }
        };
        var typeInfo = new TestTypeInfo<TrackingDto>(properties: props);
        var resolver = new TestResolver<TrackingDto>(typeInfo);
        var opts = BuildOptions(resolver);

        var result = HumlSerializer.Serialize(new TrackingDto { Name = "Alice" }, opts);

        result.Should().Contain("INJECTED");
        result.Should().NotContain("Alice");
    }

    // ── TRA02 ─────────────────────────────────────────────────────────────────

    [Fact]
    public void TRA02_resolver_drives_deserialization()
    {
        var props = new[]
        {
            new HumlPropertyInfo
            {
                Name = "Name",
                PropertyType = typeof(string),
                Set = (obj, _) => ((TrackingDto)obj).Name = "DELEGATE_SET"
            }
        };
        var typeInfo = new TestTypeInfo<TrackingDto>(properties: props);
        var resolver = new TestResolver<TrackingDto>(typeInfo);
        var opts = BuildOptions(resolver);

        var result = HumlSerializer.Deserialize<TrackingDto>("%HUML v0.2.0\nName: \"Alice\"\n", opts);

        result.Name.Should().Be("DELEGATE_SET");
    }

    // ── TRA03 ─────────────────────────────────────────────────────────────────

    [Fact]
    public void TRA03_null_properties_falls_through_to_reflection()
    {
        // Properties = null (default) → resolver present but falls through to reflection
        var typeInfo = new TestTypeInfo<TrackingDto>(properties: null);
        var resolver = new TestResolver<TrackingDto>(typeInfo);
        var opts = BuildOptions(resolver);

        var dto = new TrackingDto { Name = "Bob", Count = 5 };
        var huml = HumlSerializer.Serialize(dto, opts);
        var result = HumlSerializer.Deserialize<TrackingDto>(huml, opts);

        result.Name.Should().Be("Bob");
        result.Count.Should().Be(5);
    }

    // ── TRA04 ─────────────────────────────────────────────────────────────────

    [Fact]
    public void TRA04_null_resolver_falls_through_to_reflection()
    {
        var opts = HumlOptions.LatestSupported;

        var dto = new TrackingDto { Name = "Carol", Count = 3 };
        var huml = HumlSerializer.Serialize(dto, opts);
        var result = HumlSerializer.Deserialize<TrackingDto>(huml, opts);

        result.Name.Should().Be("Carol");
        result.Count.Should().Be(3);
    }

    // ── TRA05 ─────────────────────────────────────────────────────────────────

    [Fact]
    public void TRA05_onserializing_callback_invoked()
    {
        var callSequence = new List<string>();
        var props = new[]
        {
            new HumlPropertyInfo
            {
                Name = "Name",
                PropertyType = typeof(string),
                Get = obj => ((TrackingDto)obj).Name
            }
        };
        var typeInfo = new TestTypeInfo<TrackingDto>(
            properties: props,
            onSerializing: _ => callSequence.Add("serializing"),
            onSerialized: _ => callSequence.Add("serialized"));
        var resolver = new TestResolver<TrackingDto>(typeInfo);
        var opts = BuildOptions(resolver);

        HumlSerializer.Serialize(new TrackingDto { Name = "Test" }, opts);

        callSequence.Should().BeEquivalentTo(SerializingThenSerialized, opts => opts.WithStrictOrdering());
    }

    // ── TRA06 ─────────────────────────────────────────────────────────────────

    [Fact]
    public void TRA06_onserialized_callback_invoked()
    {
        var callSequence = new List<string>();
        var props = new[]
        {
            new HumlPropertyInfo
            {
                Name = "Name",
                PropertyType = typeof(string),
                Get = obj => ((TrackingDto)obj).Name
            }
        };
        var typeInfo = new TestTypeInfo<TrackingDto>(
            properties: props,
            onSerializing: _ => callSequence.Add("serializing"),
            onSerialized: _ => callSequence.Add("serialized"));
        var resolver = new TestResolver<TrackingDto>(typeInfo);
        var opts = BuildOptions(resolver);

        HumlSerializer.Serialize(new TrackingDto { Name = "Test" }, opts);

        callSequence.Should().Contain("serialized");
        callSequence[1].Should().Be("serialized");
    }

    // ── TRA07 ─────────────────────────────────────────────────────────────────

    [Fact]
    public void TRA07_ondeserializing_callback_invoked()
    {
        var callSequence = new List<string>();
        var props = new[]
        {
            new HumlPropertyInfo
            {
                Name = "Name",
                PropertyType = typeof(string),
                Set = (obj, v) => ((TrackingDto)obj).Name = (string?)v ?? string.Empty
            }
        };
        var typeInfo = new TestTypeInfo<TrackingDto>(
            properties: props,
            onDeserializing: _ => callSequence.Add("deserializing"),
            onDeserialized: _ => callSequence.Add("deserialized"));
        var resolver = new TestResolver<TrackingDto>(typeInfo);
        var opts = BuildOptions(resolver);

        HumlSerializer.Deserialize<TrackingDto>("%HUML v0.2.0\nName: \"X\"\n", opts);

        callSequence.Should().BeEquivalentTo(DeserializingThenDeserialized, opts => opts.WithStrictOrdering());
    }

    // ── TRA08 ─────────────────────────────────────────────────────────────────

    [Fact]
    public void TRA08_ondeserialized_callback_invoked()
    {
        var callSequence = new List<string>();
        var props = new[]
        {
            new HumlPropertyInfo
            {
                Name = "Name",
                PropertyType = typeof(string),
                Set = (obj, v) => ((TrackingDto)obj).Name = (string?)v ?? string.Empty
            }
        };
        var typeInfo = new TestTypeInfo<TrackingDto>(
            properties: props,
            onDeserializing: _ => callSequence.Add("deserializing"),
            onDeserialized: _ => callSequence.Add("deserialized"));
        var resolver = new TestResolver<TrackingDto>(typeInfo);
        var opts = BuildOptions(resolver);

        HumlSerializer.Deserialize<TrackingDto>("%HUML v0.2.0\nName: \"X\"\n", opts);

        callSequence.Should().Contain("deserialized");
        callSequence[1].Should().Be("deserialized");
    }

    // ── TRA09 ─────────────────────────────────────────────────────────────────

    [Fact]
    public void TRA09_resolver_path_skips_unmapped_member_check()
    {
        // Properties only maps Name (not Count). UnmappedMemberHandling.Disallow is set.
        // The resolver path bypasses the unmapped-member check entirely — no exception should be thrown.
        var props = new[]
        {
            new HumlPropertyInfo
            {
                Name = "Name",
                PropertyType = typeof(string),
                Set = (obj, v) => ((TrackingDto)obj).Name = (string?)v ?? string.Empty
            }
        };
        var typeInfo = new TestTypeInfo<TrackingDto>(properties: props);
        var resolver = new TestResolver<TrackingDto>(typeInfo);
        var opts = new HumlOptions
        {
            VersionSource = VersionSource.Options,
            SpecVersion = HumlSpecVersion.V0_2,
            TypeInfoResolver = resolver,
            UnmappedMemberHandling = UnmappedMemberHandling.Disallow
        };

        var act = () => HumlSerializer.Deserialize<TrackingDto>("%HUML v0.2.0\nName: \"Alice\"\nCount: 99\n", opts);

        act.Should().NotThrow();
    }
}
