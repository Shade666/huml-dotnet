using AwesomeAssertions;
using Huml.Net.Parser;
using Huml.Net.Serialization;
using Huml.Net.Versioning;
using Xunit;

namespace Huml.Net.Tests.Serialization;

public class HumlExtensionDataTests
{
    // ── Test DTOs ─────────────────────────────────────────────────────────────

    private class NodeExtPoco
    {
        public string? Name { get; set; }
        [HumlExtensionData]
        public Dictionary<string, HumlNode>? Extras { get; set; }
    }

    private class ObjExtPoco
    {
        public string? Name { get; set; }
        [HumlExtensionData]
        public Dictionary<string, object?>? Overflow { get; set; }
    }

    private class BaseExtPoco
    {
        [HumlExtensionData]
        public Dictionary<string, object?>? Extras { get; set; }
    }

    private class DerivedExtPoco : BaseExtPoco
    {
        public int Value { get; set; }
    }

    private class DualExtPoco
    {
        [HumlExtensionData]
        public Dictionary<string, object?>? First { get; set; }
        [HumlExtensionData]
        public Dictionary<string, object?>? Second { get; set; }
    }

    private class BadTypePoco
    {
        [HumlExtensionData]
        public Dictionary<string, string>? Wrong { get; set; }
    }

    private class PlainPoco
    {
        public string? Name { get; set; }
    }

    // ── Constructor ───────────────────────────────────────────────────────────

    public HumlExtensionDataTests()
    {
        PropertyDescriptor.ClearCache();
    }

    // ── Tests ─────────────────────────────────────────────────────────────────

    [Fact]
    public void EXT01_NodeDict_unmapped_scalar_captured()
    {
        // String values must be quoted in HUML v0.2; integers and booleans are bare.
        const string huml = "Name: \"Alice\"\nUnknown: 42\n";
        var poco = HumlSerializer.Deserialize<NodeExtPoco>(huml, HumlOptions.LatestSupported);

        poco.Name.Should().Be("Alice");
        poco.Extras.Should().NotBeNull();
        poco.Extras!.ContainsKey("Unknown").Should().BeTrue();
        var node = poco.Extras["Unknown"];
        node.Should().BeOfType<HumlScalar>();
        var scalar = (HumlScalar)node;
        scalar.Kind.Should().Be(ScalarKind.Integer);
        scalar.Value.Should().Be(42L);
    }

    [Fact]
    public void EXT02_ObjDict_unmapped_scalar_captured()
    {
        const string huml = "Name: \"Bob\"\nUnknown: true\n";
        var poco = HumlSerializer.Deserialize<ObjExtPoco>(huml, HumlOptions.LatestSupported);

        poco.Name.Should().Be("Bob");
        poco.Overflow.Should().NotBeNull();
        poco.Overflow!.ContainsKey("Unknown").Should().BeTrue();
        poco.Overflow["Unknown"].Should().Be(true);
    }

    [Fact]
    public void EXT03_declared_keys_still_bound()
    {
        // 'true' as a bool value for 'Extra'; Name is a quoted string
        const string huml = "Name: \"Carol\"\nExtra: true\n";
        var poco = HumlSerializer.Deserialize<NodeExtPoco>(huml, HumlOptions.LatestSupported);

        poco.Name.Should().Be("Carol");
        poco.Extras.Should().NotBeNull();
        poco.Extras!.ContainsKey("Extra").Should().BeTrue();
        var node = poco.Extras["Extra"];
        node.Should().BeOfType<HumlScalar>();
        ((HumlScalar)node).Kind.Should().Be(ScalarKind.Bool);
    }

    [Fact]
    public void EXT04_no_unknown_keys_extension_null_or_empty()
    {
        const string huml = "Name: \"Dave\"\n";
        var poco = HumlSerializer.Deserialize<NodeExtPoco>(huml, HumlOptions.LatestSupported);

        poco.Name.Should().Be("Dave");
        // Extension dict is either null (not initialised) or empty
        (poco.Extras == null || poco.Extras.Count == 0).Should().BeTrue();
    }

    [Fact]
    public void EXT05_extension_emitted_after_declared_props_NodeDict()
    {
        var poco = new NodeExtPoco
        {
            Name = "Eve",
            Extras = new Dictionary<string, HumlNode>
            {
                ["ext"] = new HumlScalar(ScalarKind.String, "ExtVal")
            }
        };

        var output = HumlSerializer.Serialize(poco, HumlOptions.LatestSupported);

        var lines = output.Split('\n', StringSplitOptions.RemoveEmptyEntries);
        // Find index of the Name line and the ext line
        int nameIdx = Array.FindIndex(lines, l => l.TrimStart().StartsWith("Name:", StringComparison.Ordinal));
        int extIdx  = Array.FindIndex(lines, l => l.TrimStart().StartsWith("ext:", StringComparison.Ordinal));

        nameIdx.Should().BeGreaterThanOrEqualTo(0, "Name line should be present");
        extIdx.Should().BeGreaterThanOrEqualTo(0, "ext line should be present");
        nameIdx.Should().BeLessThan(extIdx, "Name must appear before ext in output");
    }

    [Fact]
    public void EXT06_extension_emitted_after_declared_props_ObjDict()
    {
        var poco = new ObjExtPoco
        {
            Name = "Frank",
            Overflow = new Dictionary<string, object?> { ["extra"] = "value" }
        };

        var output = HumlSerializer.Serialize(poco, HumlOptions.LatestSupported);

        var lines = output.Split('\n', StringSplitOptions.RemoveEmptyEntries);
        int nameIdx  = Array.FindIndex(lines, l => l.TrimStart().StartsWith("Name:", StringComparison.Ordinal));
        int extraIdx = Array.FindIndex(lines, l => l.TrimStart().StartsWith("extra:", StringComparison.Ordinal));

        nameIdx.Should().BeGreaterThanOrEqualTo(0);
        extraIdx.Should().BeGreaterThanOrEqualTo(0);
        nameIdx.Should().BeLessThan(extraIdx);
    }

    [Fact]
    public void EXT07_round_trip_NodeDict()
    {
        const string huml = "Name: \"Grace\"\ntag: \"demo\"\n";

        var first = HumlSerializer.Deserialize<NodeExtPoco>(huml, HumlOptions.LatestSupported);
        var serialised = HumlSerializer.Serialize(first, HumlOptions.LatestSupported);
        var second = HumlSerializer.Deserialize<NodeExtPoco>(serialised, HumlOptions.LatestSupported);

        first.Extras.Should().NotBeNull();
        second.Extras.Should().NotBeNull();

        var firstTag  = (HumlScalar)first.Extras!["tag"];
        var secondTag = (HumlScalar)second.Extras!["tag"];

        firstTag.Kind.Should().Be(secondTag.Kind);
        firstTag.Value.Should().Be(secondTag.Value);
    }

    [Fact]
    public void EXT08_round_trip_ObjDict()
    {
        const string huml = "Name: \"Henry\"\nscore: 99\n";

        var first = HumlSerializer.Deserialize<ObjExtPoco>(huml, HumlOptions.LatestSupported);
        var serialised = HumlSerializer.Serialize(first, HumlOptions.LatestSupported);
        var second = HumlSerializer.Deserialize<ObjExtPoco>(serialised, HumlOptions.LatestSupported);

        first.Overflow!["score"].Should().Be(99L);
        second.Overflow!["score"].Should().Be(99L);
    }

    [Fact]
    public void EXT09_nested_mapping_captured_ObjDict()
    {
        const string huml = "Name: \"Alice\"\nnested::\n  a: 1\n";
        var poco = HumlSerializer.Deserialize<ObjExtPoco>(huml, HumlOptions.LatestSupported);

        poco.Overflow.Should().NotBeNull();
        poco.Overflow!.ContainsKey("nested").Should().BeTrue();
        var nested = poco.Overflow["nested"] as Dictionary<string, object?>;
        nested.Should().NotBeNull();
        nested!["a"].Should().Be(1L);
    }

    [Fact]
    public void EXT10_sequence_captured_ObjDict()
    {
        const string huml = "Name: \"Alice\"\nitems::\n  - \"foo\"\n  - \"bar\"\n";
        var poco = HumlSerializer.Deserialize<ObjExtPoco>(huml, HumlOptions.LatestSupported);

        poco.Overflow.Should().NotBeNull();
        poco.Overflow!.ContainsKey("items").Should().BeTrue();
        var items = poco.Overflow["items"] as List<object?>;
        items.Should().NotBeNull();
        items!.Count.Should().Be(2);
    }

    [Fact]
    public void EXT11_multiple_extension_attrs_throws()
    {
        var act = () => HumlSerializer.Deserialize<DualExtPoco>("Name: \"x\"\n", HumlOptions.LatestSupported);
        act.Should().Throw<InvalidOperationException>().WithMessage("*DualExtPoco*");
    }

    [Fact]
    public void EXT12_bad_type_throws()
    {
        var act = () => HumlSerializer.Deserialize<BadTypePoco>("Name: \"x\"\n", HumlOptions.LatestSupported);
        act.Should().Throw<InvalidOperationException>().WithMessage("*not supported*");
    }

    [Fact]
    public void EXT13_inherited_extension_data()
    {
        const string huml = "Value: 7\nExtra: \"inherited\"\n";
        var poco = HumlSerializer.Deserialize<DerivedExtPoco>(huml, HumlOptions.LatestSupported);

        poco.Value.Should().Be(7);
        poco.Extras.Should().NotBeNull();
        poco.Extras!.ContainsKey("Extra").Should().BeTrue();
    }

    [Fact]
    public void EXT14_key_requiring_quoting_emitted_correctly()
    {
        var poco = new NodeExtPoco
        {
            Name = "Quoter",
            Extras = new Dictionary<string, HumlNode>
            {
                ["needs quoting"] = new HumlScalar(ScalarKind.String, "val")
            }
        };

        var output = HumlSerializer.Serialize(poco, HumlOptions.LatestSupported);
        output.Should().Contain("\"needs quoting\":");
    }

    [Fact]
    public void EXT15_null_extension_dict_no_nullref_on_serialise()
    {
        var poco = new NodeExtPoco { Name = "Null", Extras = null };
        var act = () => HumlSerializer.Serialize(poco, HumlOptions.LatestSupported);
        act.Should().NotThrow();
        var result = HumlSerializer.Serialize(poco, HumlOptions.LatestSupported);
        // Only the declared Name property should appear
        result.Should().Contain("Name:");
    }

    [Fact]
    public void EXT16_no_regression_plain_poco()
    {
        var original = new PlainPoco { Name = "Plain" };
        var serialised = HumlSerializer.Serialize(original, HumlOptions.LatestSupported);
        var roundTripped = HumlSerializer.Deserialize<PlainPoco>(serialised, HumlOptions.LatestSupported);
        roundTripped.Name.Should().Be("Plain");
    }
}
