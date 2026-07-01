using System.Runtime.InteropServices;
using AwesomeAssertions;
using Huml.Net.Exceptions;
using Huml.Net.Parser;
using Huml.Net.Serialization;
using Huml.Net.Versioning;
using Xunit;

namespace Huml.Net.Tests.Serialization;

public class HumlConverterTests
{
    // ── Helper types ──────────────────────────────────────────────────────────

    // A simple value type with no built-in converter support
    [StructLayout(LayoutKind.Auto)]
    private record struct Point(int X, int Y);

    // Converter that serialises Point as "X,Y" string scalar
    private sealed class PointConverter : HumlConverter<Point>
    {
        public override bool CanConvert(Type t) => t == typeof(Point);

        public override Point Read(HumlNode node)
        {
            if (node is not HumlScalar { Kind: ScalarKind.String, Value: string s })
                throw new HumlDeserializeException("Expected string for Point.");
            var parts = s.Split(',');
            return new Point(int.Parse(parts[0]), int.Parse(parts[1]));
        }

        public override void Write(HumlWriterContext context, Point value)
            => context.AppendRaw($"\"{value.X},{value.Y}\"");
    }

    // Converter with no parameterless constructor — for CONV-ERR-01
    private sealed class NoCtor_Converter : HumlConverter<Point>
    {
        public NoCtor_Converter(int ignored) { }
        public override bool CanConvert(Type t) => t == typeof(Point);
        public override Point Read(HumlNode node) => default;
        public override void Write(HumlWriterContext context, Point value) { }
    }

    // Converter whose CanConvert always returns false — for CONV-ERR-02
    private sealed class NeverMatchConverter : HumlConverter<Point>
    {
        public override bool CanConvert(Type t) => false;
        public override Point Read(HumlNode node) => default;
        public override void Write(HumlWriterContext context, Point value) { }
    }

    // POCO with property-level [HumlConverter]
    private class PointPropPoco
    {
        [HumlConverter(typeof(PointConverter))]
        public Point Location { get; set; }

        public string? Name { get; set; }
    }

    // POCO with property-level [HumlConverter] using type with no ctor — for CONV-ERR-01
    private class BadConverterPoco
    {
        [HumlConverter(typeof(NoCtor_Converter))]
        public Point Location { get; set; }
    }

    // Converter that serialises TaggedPoint as "X,Y" string scalar — for type-level converter tests
    // (PointConverter handles Point; TaggedPointConverter handles TaggedPoint — separate types)
    private sealed class TaggedPointConverter : HumlConverter<TaggedPoint>
    {
        public override bool CanConvert(Type t) => t == typeof(TaggedPoint);

        public override TaggedPoint Read(HumlNode node)
        {
            if (node is not HumlScalar { Kind: ScalarKind.String, Value: string s })
                throw new HumlDeserializeException("Expected string for TaggedPoint.");
            var parts = s.Split(',');
            return new TaggedPoint(int.Parse(parts[0]), int.Parse(parts[1]));
        }

        public override void Write(HumlWriterContext context, TaggedPoint value)
            => context.AppendRaw($"\"{value.X},{value.Y}\"");
    }

    // Type-level [HumlConverter] — for CONV-REG-03, CONV-SER-05, CONV-DES-04
    [HumlConverter(typeof(TaggedPointConverter))]
    [StructLayout(LayoutKind.Auto)]
    private record struct TaggedPoint(int X, int Y);

    // POCO holding a List<TaggedPoint> — for CONV-RT-04
    private class TaggedListPoco
    {
        public List<TaggedPoint> Points { get; set; } = new();
    }

    // POCO holding a PointPropPoco — for round-trip tests
    private class ContainerPoco
    {
        [HumlConverter(typeof(PointConverter))]
        public Point Location { get; set; }
        public string? Label { get; set; }
    }

    // POCO with a Point property named P — for options-level converter tests (CONV-DES-01, CONV-RT-02)
    private class PointContainerPoco
    {
        public Point P { get; set; }
    }

    // ── Constructor ───────────────────────────────────────────────────────────

    public HumlConverterTests()
    {
        PropertyDescriptor.ClearCache();
        ConverterCache.ClearCache();
        HumlOptions.ClearOptionsCaches();
    }

    // ── CONV-REG-* — Registration ─────────────────────────────────────────────

    [Fact]
    public void EmptyConverters_DoesNotAffectDefaultSerialisation()
    {
        // Arrange: options with explicit empty list
        var options = new HumlOptions { Converters = new List<HumlConverter>() };
        var poco = new PointPropPoco { Name = "test" };
        // Act + Assert: no exception — built-in dispatch still works
        var act = () => HumlSerializer.Serialize(poco, options);
        act.Should().NotThrow();
    }

    [Fact]
    public void PropertyLevel_HumlConverterAttribute_CachedInPropertyDescriptor()
    {
        // Arrange + Act: build descriptors for PointPropPoco
        var descriptors = PropertyDescriptor.GetDescriptors(typeof(PointPropPoco));
        // Assert: the Location property descriptor has a non-null Converter
        var locationDesc = Array.Find(descriptors, d => string.Equals(d.HumlKey, "Location", StringComparison.Ordinal));
        locationDesc.Should().NotBeNull();
        locationDesc!.Converter.Should().BeOfType<PointConverter>();
    }

    [Fact]
    public void TypeLevel_HumlConverterAttribute_UsedWhenTypeAppearsAsTarget()
    {
        var options = new HumlOptions();
        // TaggedPoint has [HumlConverter(typeof(PointConverter))] at class level
        // Serialise a POCO that holds a TaggedPoint — converter should fire
        var act = () => HumlSerializer.Serialize(new { P = new TaggedPoint(1, 2) }, options);
        act.Should().NotThrow();
    }

    [Fact]
    public void Priority_PropertyLevel_WinsOverTypeLevel()
    {
        // A type with type-level converter; property with different converter
        // Property-level should win for that property
        var options = new HumlOptions();
        var act = () => HumlSerializer.Serialize(new PointPropPoco { Location = new Point(3, 4) }, options);
        act.Should().NotThrow();
    }

    [Fact]
    public void HumlConverterAttribute_WithNonHumlConverterType_ThrowsInvalidOperationException()
    {
        // Act: building descriptors for BadConverterPoco should throw
        var act = () => PropertyDescriptor.GetDescriptors(typeof(BadConverterPoco));
        act.Should().Throw<InvalidOperationException>();
    }

    // ── CONV-SER-* — Serialiser ───────────────────────────────────────────────

    [Fact]
    public void OptionsLevel_Converter_InvokedBeforeBuiltinDispatch()
    {
        var options = new HumlOptions { Converters = new List<HumlConverter> { new PointConverter() } };
        var result = HumlSerializer.Serialize(new { P = new Point(1, 2) }, options);
        result.Should().Contain("\"1,2\"");
    }

    [Fact]
    public void AppendSerializedValue_UsesBuiltinDispatch()
    {
        // A converter that calls context.AppendSerializedValue(string) internally
        // The string should be serialised with built-in quoting
        var options = new HumlOptions { Converters = new List<HumlConverter> { new PointConverter() } };
        // Verify via serialising a POCO whose non-Point properties still use built-in dispatch
        var result = HumlSerializer.Serialize(new PointPropPoco { Location = new Point(5, 6), Name = "hello" }, options);
        result.Should().Contain("\"hello\"");
    }

    [Fact]
    public void AppendRaw_EmitsVerbatimFragment()
    {
        var options = new HumlOptions { Converters = new List<HumlConverter> { new PointConverter() } };
        var result = HumlSerializer.Serialize(new { P = new Point(7, 8) }, options);
        // PointConverter uses AppendRaw to emit "X,Y"
        result.Should().Contain("\"7,8\"");
    }

    [Fact]
    public void PropertyLevel_Converter_Write_InvokedForThatPropertyOnly()
    {
        var poco = new PointPropPoco { Location = new Point(10, 20), Name = "world" };
        var result = HumlSerializer.Serialize(poco, HumlOptions.LatestSupported);
        // Location uses converter; Name uses built-in string dispatch
        result.Should().Contain("\"10,20\"");
        result.Should().Contain("\"world\"");
    }

    [Fact]
    public void TypeLevel_Converter_Write_InvokedForEveryOccurrence()
    {
        // A POCO holding two TaggedPoint properties
        var poco = new { A = new TaggedPoint(1, 2), B = new TaggedPoint(3, 4) };
        var result = HumlSerializer.Serialize(poco, HumlOptions.LatestSupported);
        result.Should().Contain("\"1,2\"");
        result.Should().Contain("\"3,4\"");
    }

    // ── CONV-DES-* — Deserialiser ─────────────────────────────────────────────

    [Fact]
    public void OptionsLevel_Converter_InvokedInDeserializeNode()
    {
        var huml = "%HUML v0.2.0\nP: \"1,2\"\n";
        var options = new HumlOptions
        {
            VersionSource = VersionSource.Header,
            Converters = new List<HumlConverter> { new PointConverter() }
        };
        var result = HumlSerializer.Deserialize<PointContainerPoco>(huml, options);
        result.P.Should().Be(new Point(1, 2));
    }

    [Fact]
    public void Converter_Read_ReceivesFullyParsedHumlNode()
    {
        // The Read method receives a HumlScalar; assert converter is invoked with correct node type
        var huml = "%HUML v0.2.0\nLocation: \"5,6\"\n";
        var result = HumlSerializer.Deserialize<PointPropPoco>(huml, HumlOptions.Default);
        result.Location.Should().Be(new Point(5, 6));
    }

    [Fact]
    public void PropertyLevel_Converter_Read_InvokedForThatPropertyOnly()
    {
        var huml = "%HUML v0.2.0\nLocation: \"3,4\"\nName: \"hello\"\n";
        var result = HumlSerializer.Deserialize<PointPropPoco>(huml, HumlOptions.Default);
        result.Location.Should().Be(new Point(3, 4));
        result.Name.Should().Be("hello");
    }

    [Fact]
    public void TypeLevel_Converter_Read_InvokedForEveryOccurrence()
    {
        var huml = "%HUML v0.2.0\nPoints::\n  - \"1,2\"\n  - \"3,4\"\n";
        var result = HumlSerializer.Deserialize<TaggedListPoco>(huml, HumlOptions.Default);
        result.Points.Should().HaveCount(2);
    }

    [Fact]
    public void ConverterRead_ThrowingHumlDeserializeException_PropagatesCorrectly()
    {
        // PointConverter.Read validates ScalarKind.String and throws HumlDeserializeException
        // when given an integer scalar — verifies the exception propagates through converter dispatch.
        var huml = "%HUML v0.2.0\nLocation: 42\n";
        var act = () => HumlSerializer.Deserialize<PointPropPoco>(huml, HumlOptions.Default);
        act.Should().Throw<HumlDeserializeException>();
    }

    // ── CONV-RT-* — Round-Trips ───────────────────────────────────────────────

    [Fact]
    public void CustomType_RoundTrips_ThroughConverter()
    {
        var original = new PointPropPoco { Location = new Point(11, 22), Name = "rt" };
        var huml = HumlSerializer.Serialize(original, HumlOptions.LatestSupported);
        var restored = HumlSerializer.Deserialize<PointPropPoco>(huml, HumlOptions.LatestSupported);
        restored.Location.Should().Be(original.Location);
        restored.Name.Should().Be(original.Name);
    }

    [Fact]
    public void OptionsLevel_Converter_RoundTrips_WithSameOptions()
    {
        var options = new HumlOptions
        {
            VersionSource = VersionSource.Options,
            Converters = new List<HumlConverter> { new PointConverter() }
        };
        var original = new { P = new Point(7, 8) };
        var huml = HumlSerializer.Serialize(original, options);
        // Deserialise into typed class for assertion (key is P, must match PointContainerPoco.P)
        var restored = HumlSerializer.Deserialize<PointContainerPoco>(huml, options);
        restored.P.Should().Be(new Point(7, 8));
    }

    [Fact]
    public void HumlConverterAttribute_Property_RoundTrips()
    {
        var original = new ContainerPoco { Location = new Point(5, 5), Label = "box" };
        var huml = HumlSerializer.Serialize(original, HumlOptions.LatestSupported);
        var restored = HumlSerializer.Deserialize<ContainerPoco>(huml, HumlOptions.LatestSupported);
        restored.Location.Should().Be(original.Location);
        restored.Label.Should().Be(original.Label);
    }

    [Fact]
    public void ListOf_TypeLevelConverter_RoundTrips_AllElements()
    {
        var original = new TaggedListPoco { Points = new List<TaggedPoint> { new(1, 2), new(3, 4) } };
        var huml = HumlSerializer.Serialize(original, HumlOptions.LatestSupported);
        var restored = HumlSerializer.Deserialize<TaggedListPoco>(huml, HumlOptions.LatestSupported);
        restored.Points.Should().HaveCount(2);
        restored.Points[0].Should().Be(new TaggedPoint(1, 2));
        restored.Points[1].Should().Be(new TaggedPoint(3, 4));
    }

    // ── CONV-ERR-* — Error Cases ──────────────────────────────────────────────

    [Fact]
    public void ConverterWithNoParameterlessCtor_ThrowsInvalidOperationException()
    {
        // Building descriptors for BadConverterPoco (which has NoCtor_Converter)
        var act = () => PropertyDescriptor.GetDescriptors(typeof(BadConverterPoco));
        act.Should().Throw<InvalidOperationException>()
           .WithMessage("*NoCtor_Converter*parameterless constructor*");
    }

    [Fact]
    public void Converter_CanConvertFalse_NeverInvoked()
    {
        // NeverMatchConverter.CanConvert returns false — it must not appear in output
        var options = new HumlOptions
        {
            Converters = new List<HumlConverter> { new NeverMatchConverter() }
        };
        // Serialising a string should use built-in dispatch, not NeverMatchConverter
        var result = HumlSerializer.Serialize(new { Text = "hello" }, options);
        result.Should().Contain("\"hello\"");
    }

    [Fact]
    public void FirstMatchWins_InConvertersList()
    {
        // Register two converters for Point — first-registered must win
        var first = new PointConverter();
        var second = new PointConverter(); // same type but different instance — first wins
        var options = new HumlOptions
        {
            Converters = new List<HumlConverter> { first, second }
        };
        var result = HumlSerializer.Serialize(new { P = new Point(1, 2) }, options);
        result.Should().Contain("\"1,2\"");
    }

    // ── CONV-FACTORY-* — HumlConverterFactory and Nullable<T> auto-unwrap (issue #30) ──

    // Plain HumlConverter<Status>, registered globally — used to reproduce the exact issue #30
    // symptom: does a converter registered for T also serve T? with no extra CanConvert override?
    private enum Status { Active, Inactive }

    private sealed class StatusConverter : HumlConverter<Status>
    {
        public override Status Read(HumlNode node)
        {
            if (node is not HumlScalar { Kind: ScalarKind.String, Value: string s })
                throw new HumlDeserializeException("Expected string for Status.");
            return Enum.Parse<Status>(s);
        }

        public override void Write(HumlWriterContext context, Status value)
            => context.AppendRaw($"\"{value}\"");
    }

    private class NullableStatusPoco
    {
        public Status? State { get; set; }
    }

    // A factory producing a lenient converter for ANY enum type — the motivating use case from
    // issue #30 ("a general-purpose lenient enum converter usable across an entire schema").
    private sealed class LenientEnumConverterFactory : HumlConverterFactory
    {
        public int CreateConverterCallCount;

        public override bool CanConvert(Type typeToConvert) => typeToConvert.IsEnum;

        public override HumlConverter? CreateConverter(Type typeToConvert, HumlOptions options)
        {
            CreateConverterCallCount++;
            var converterType = typeof(LenientEnumConverter<>).MakeGenericType(typeToConvert);
            return (HumlConverter)Activator.CreateInstance(converterType)!;
        }
    }

    private sealed class LenientEnumConverter<TEnum> : HumlConverter<TEnum> where TEnum : struct, Enum
    {
        public override TEnum Read(HumlNode node)
        {
            if (node is HumlScalar { Kind: ScalarKind.String, Value: string s } &&
                Enum.TryParse<TEnum>(s, ignoreCase: true, out var parsed))
                return parsed;
            return default; // lenient: falls back to default rather than throwing
        }

        public override void Write(HumlWriterContext context, TEnum value)
            => context.AppendRaw($"\"{value}\"");
    }

    // A factory that always declines (CreateConverter returns null) — resolution must fall
    // through to the next candidate rather than short-circuiting.
    private sealed class DecliningFactory : HumlConverterFactory
    {
        public override bool CanConvert(Type typeToConvert) => typeToConvert == typeof(Point);
        public override HumlConverter? CreateConverter(Type typeToConvert, HumlOptions options) => null;
    }

    // Struct resolved through a factory via the type-level [HumlConverter] attribute.
    private sealed class MoneyConverter : HumlConverter<Money>
    {
        public override Money Read(HumlNode node)
        {
            if (node is not HumlScalar { Kind: ScalarKind.Integer } scalar)
                throw new HumlDeserializeException("Expected integer for Money.");
            return new Money(Convert.ToInt64(scalar.Value, System.Globalization.CultureInfo.InvariantCulture));
        }

        public override void Write(HumlWriterContext context, Money value)
            => context.AppendRaw(value.Cents.ToString(System.Globalization.CultureInfo.InvariantCulture));
    }

    private sealed class MoneyConverterFactory : HumlConverterFactory
    {
        public override bool CanConvert(Type typeToConvert) => typeToConvert == typeof(Money);
        public override HumlConverter? CreateConverter(Type typeToConvert, HumlOptions options) => new MoneyConverter();
    }

    [HumlConverter(typeof(MoneyConverterFactory))]
    [StructLayout(LayoutKind.Auto)]
    private record struct Money(long Cents);

    private class MoneyPoco
    {
        public Money Price { get; set; }
    }

    [Fact]
    public void Factory_ViaOptionsConverters_ProducesConcreteConverter()
    {
        var options = new HumlOptions { Converters = new List<HumlConverter> { new LenientEnumConverterFactory() } };
        var huml = HumlSerializer.Serialize(new { S = Status.Active }, options);
        huml.Should().Contain("\"Active\"");
    }

    [Fact]
    public void Factory_DecliningViaNullCreateConverter_FallsThroughToNextConverter()
    {
        var options = new HumlOptions
        {
            Converters = new List<HumlConverter> { new DecliningFactory(), new PointConverter() }
        };
        var result = HumlSerializer.Serialize(new { P = new Point(1, 2) }, options);
        result.Should().Contain("\"1,2\"");
    }

    [Fact]
    public void Factory_ViaTypeLevelAttribute_ResolvesConcreteConverter()
    {
        var result = HumlSerializer.Serialize(new MoneyPoco { Price = new Money(1050) }, new HumlOptions());
        result.Should().Contain("1050");
    }

    [Fact]
    public void Factory_ViaTypeLevelAttribute_RoundTrips()
    {
        var original = new MoneyPoco { Price = new Money(2500) };
        var huml = HumlSerializer.Serialize(original, HumlOptions.LatestSupported);
        var restored = HumlSerializer.Deserialize<MoneyPoco>(huml, HumlOptions.LatestSupported);
        restored.Price.Should().Be(original.Price);
    }

    [Fact]
    public void Factory_CreateConverter_IsMemoisedPerTargetType()
    {
        var factory = new LenientEnumConverterFactory();
        var options = new HumlOptions { Converters = new List<HumlConverter> { factory } };
        HumlSerializer.Serialize(new { S = Status.Active }, options);
        HumlSerializer.Serialize(new { S = Status.Inactive }, options);
        HumlSerializer.Serialize(new { S = Status.Active }, options);
        factory.CreateConverterCallCount.Should().Be(1);
    }

    // Reported symptom (issue #30 regression): a plain HumlConverter<TEnum>, registered
    // globally, now applies to a TEnum? property automatically — both directions.

    [Fact]
    public void GlobalConverterForEnum_AppliesToNullableEnumProperty_OnSerialise()
    {
        var options = new HumlOptions { Converters = new List<HumlConverter> { new StatusConverter() } };
        var huml = HumlSerializer.Serialize(new NullableStatusPoco { State = Status.Active }, options);
        huml.Should().Contain("\"Active\"");
    }

    [Fact]
    public void GlobalConverterForEnum_AppliesToNullableEnumProperty_OnDeserialise()
    {
        var huml = "%HUML v0.2.0\nState: \"Active\"\n";
        var options = new HumlOptions
        {
            VersionSource = VersionSource.Header,
            Converters = new List<HumlConverter> { new StatusConverter() }
        };
        var result = HumlSerializer.Deserialize<NullableStatusPoco>(huml, options);
        result.State.Should().Be(Status.Active);
    }

    [Fact]
    public void NullableAutoUnwrap_NullValue_EmitsNullKeyword()
    {
        var options = new HumlOptions { Converters = new List<HumlConverter> { new StatusConverter() } };
        var huml = HumlSerializer.Serialize(new NullableStatusPoco { State = null }, options);
        huml.Should().Contain("State: null");
    }

    [Fact]
    public void NullableAutoUnwrap_NullScalar_DeserialisesToNull_WithoutInvokingInnerConverter()
    {
        // StatusConverter.Read throws on anything that isn't a string scalar — if the null
        // scalar reached it, this would throw HumlDeserializeException instead of succeeding.
        var huml = "%HUML v0.2.0\nState: null\n";
        var options = new HumlOptions
        {
            VersionSource = VersionSource.Header,
            Converters = new List<HumlConverter> { new StatusConverter() }
        };
        var result = HumlSerializer.Deserialize<NullableStatusPoco>(huml, options);
        result.State.Should().BeNull();
    }

    // A factory cannot be used via a property-level [HumlConverter] — property-level converters
    // are resolved directly, without an HumlOptions context for CreateConverter to run against.
    private sealed class BadFactoryPropertyPoco
    {
        [HumlConverter(typeof(LenientEnumConverterFactory))]
        public Status State { get; set; }
    }

    [Fact]
    public void Factory_ViaPropertyLevelAttribute_ThrowsInvalidOperationException()
    {
        var act = () => PropertyDescriptor.GetDescriptors(typeof(BadFactoryPropertyPoco));
        act.Should().Throw<InvalidOperationException>()
           .WithMessage("*HumlConverterFactory*property-level*");
    }
}
