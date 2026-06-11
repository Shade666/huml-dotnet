using AwesomeAssertions;
using Huml.Net.Exceptions;
using Huml.Net.Serialization;
using Huml.Net.Versioning;
using Xunit;

namespace Huml.Net.Tests.Serialization;

public class HumlRequiredTests
{
    // ── Test DTOs ─────────────────────────────────────────────────────────────

    private class RequiredAttrPoco
    {
        [HumlRequired] public string Name { get; set; } = "";
        public int Count { get; set; }
    }

    private class RequiredModPoco
    {
        public required string Name { get; set; } = null!;
        public int Count { get; set; }
    }

    private class BothRequiredPoco
    {
        [HumlRequired] public required string Name { get; set; } = null!;
    }

    private class MultiRequiredPoco
    {
        [HumlRequired] public string First { get; set; } = "";
        [HumlRequired] public string Second { get; set; } = "";
        public string? Optional { get; set; }
    }

    // ── Constructor ───────────────────────────────────────────────────────────

    public HumlRequiredTests()
    {
        PropertyDescriptor.ClearCache();
    }

    // ── Tests ─────────────────────────────────────────────────────────────────

    [Fact]
    public void REQ1_ATTR_missing_required_attr_property_throws()
    {
        // Name absent — required via [HumlRequired]
        var input = "%HUML v0.2\nCount: 0\n";
        var act = () => HumlSerializer.Deserialize<RequiredAttrPoco>(input, HumlOptions.LatestSupported);
        act.Should().Throw<HumlDeserializeException>();
    }

    [Fact]
    public void REQ2_CSMOD_missing_required_modifier_property_throws()
    {
        // Name absent — required via C# required modifier
        var input = "%HUML v0.2\nCount: 0\n";
        var act = () => HumlSerializer.Deserialize<RequiredModPoco>(input, HumlOptions.LatestSupported);
        act.Should().Throw<HumlDeserializeException>();
    }

    [Fact]
    public void REQ3_BOTH_required_attr_and_modifier_together_throws_exactly_once()
    {
        // Name absent — both [HumlRequired] and required modifier; dummy key makes document non-empty
        var input = "%HUML v0.2\nDummy: 0\n";
        var act = () => HumlSerializer.Deserialize<BothRequiredPoco>(input, HumlOptions.LatestSupported);
        var ex = act.Should().Throw<HumlDeserializeException>().Which;
        ex.Message.Should().Contain("'Name'");
        // 'Name' must appear exactly once — not doubled
        ex.Message.Split("'Name'").Length.Should().Be(2);
    }

    [Fact]
    public void REQ4_MULTI_multiple_missing_required_produce_one_exception_with_all_keys()
    {
        // First and Second both absent; Optional has a quoted string value
        var input = "%HUML v0.2\nOptional: \"hello\"\n";
        var act = () => HumlSerializer.Deserialize<MultiRequiredPoco>(input, HumlOptions.LatestSupported);
        var ex = act.Should().Throw<HumlDeserializeException>().Which;
        ex.Message.Should().Contain("'First'");
        ex.Message.Should().Contain("'Second'");
    }

    [Fact]
    public void REQ5_PRESENT_required_property_present_deserialises_without_throw()
    {
        // String values must be quoted in HUML v0.2
        var input = "%HUML v0.2\nName: \"Alice\"\nCount: 42\n";
        var act = () => HumlSerializer.Deserialize<RequiredAttrPoco>(input, HumlOptions.LatestSupported);
        act.Should().NotThrow();
        var result = HumlSerializer.Deserialize<RequiredAttrPoco>(input, HumlOptions.LatestSupported);
        result.Name.Should().Be("Alice");
        result.Count.Should().Be(42);
    }

    [Fact]
    public void REQ6_POPULATE_does_not_enforce_required_checks()
    {
        // Name absent from HUML — Populate must not throw (D-09)
        var existing = new RequiredAttrPoco { Name = "Pre-existing" };
        var input = "%HUML v0.2\nCount: 7\n";
        var act = () => HumlSerializer.Populate<RequiredAttrPoco>(input.AsSpan(), existing, HumlOptions.LatestSupported);
        act.Should().NotThrow();
        existing.Count.Should().Be(7);
        existing.Name.Should().Be("Pre-existing");
    }

    [Fact]
    public void REQ7_ORDER_error_message_lists_missing_keys_in_declaration_order()
    {
        // Both First and Second absent; dummy key makes document non-empty
        var input = "%HUML v0.2\nDummy: 0\n";
        var act = () => HumlSerializer.Deserialize<MultiRequiredPoco>(input, HumlOptions.LatestSupported);
        var ex = act.Should().Throw<HumlDeserializeException>().Which;
        // "First" must appear before "Second" in the message
        ex.Message.IndexOf("'First'", StringComparison.Ordinal)
            .Should().BeLessThan(ex.Message.IndexOf("'Second'", StringComparison.Ordinal));
    }

    [Fact]
    public void REQ8_NONREQ_missing_non_required_property_does_not_throw()
    {
        // Count absent — not required, must not throw; string value must be quoted in HUML v0.2
        var input = "%HUML v0.2\nName: \"Bob\"\n";
        var act = () => HumlSerializer.Deserialize<RequiredAttrPoco>(input, HumlOptions.LatestSupported);
        act.Should().NotThrow();
        var result = HumlSerializer.Deserialize<RequiredAttrPoco>(input, HumlOptions.LatestSupported);
        result.Count.Should().Be(0); // default value
    }

    [Fact]
    public void REQ9_ROUNDTRIP_required_property_round_trips_correctly()
    {
        var original = new RequiredAttrPoco { Name = "RoundTrip", Count = 99 };
        var huml = HumlSerializer.Serialize(original, HumlOptions.LatestSupported);
        var act = () => HumlSerializer.Deserialize<RequiredAttrPoco>(huml, HumlOptions.LatestSupported);
        act.Should().NotThrow();
        var result = HumlSerializer.Deserialize<RequiredAttrPoco>(huml, HumlOptions.LatestSupported);
        result.Name.Should().Be("RoundTrip");
        result.Count.Should().Be(99);
    }

    [Fact]
    public void ERR_FORMAT_error_message_matches_spec_exactly()
    {
        // Name absent — single missing required member; Count present makes document non-empty
        var input = "%HUML v0.2\nCount: 0\n";
        var act = () => HumlSerializer.Deserialize<RequiredAttrPoco>(input, HumlOptions.LatestSupported);
        var ex = act.Should().Throw<HumlDeserializeException>().Which;
        ex.Message.Should().Be("Missing required member(s) on type 'RequiredAttrPoco': 'Name'.");
    }
}
