using AwesomeAssertions;
using Huml.Net.Exceptions;
using Huml.Net.Serialization;
using Huml.Net.Versioning;
using Xunit;

namespace Huml.Net.Tests.Serialization;

/// <summary>
/// Phase 41 — deserialiser type-dispatch and case-sensitivity fixes:
/// CR-04 (TimeOnly.ParseExact), CR-06 (boundKeys Ordinal), WR-06 (ICollection doc),
/// WR-11 (IDictionary materialisation).
/// </summary>
public sealed class DeserializerTypeDispatchTests
{
    private static readonly HumlOptions Opts = HumlOptions.LatestSupported;

    // ── CR-04: TimeOnly round-trip via ParseExact ─────────────────────────────

#if NET6_0_OR_GREATER
    private sealed class TimeOnlyHolder { public TimeOnly Time { get; set; } }

    [Fact]
    public void Cr04_timeonly_round_trips_correctly()
    {
        var original = new TimeOnlyHolder { Time = new TimeOnly(14, 30, 0) };
        var huml = HumlSerializer.Serialize(original, Opts);
        var roundTripped = HumlSerializer.Deserialize<TimeOnlyHolder>(huml, Opts);
        roundTripped.Time.Should().Be(original.Time);
    }

    [Fact]
    public void Cr04_timeonly_with_fractional_seconds_round_trips()
    {
        var original = new TimeOnlyHolder { Time = new TimeOnly(9, 0, 0, 500) };
        var huml = HumlSerializer.Serialize(original, Opts);
        var roundTripped = HumlSerializer.Deserialize<TimeOnlyHolder>(huml, Opts);
        roundTripped.Time.Should().Be(original.Time);
    }

    [Fact]
    public void Cr04_timeonly_midnight_round_trips()
    {
        var original = new TimeOnlyHolder { Time = TimeOnly.MinValue };
        var huml = HumlSerializer.Serialize(original, Opts);
        var roundTripped = HumlSerializer.Deserialize<TimeOnlyHolder>(huml, Opts);
        roundTripped.Time.Should().Be(original.Time);
    }
#endif

    // ── CR-06: boundKeys uses Ordinal — required-property check is consistent ──

    private sealed class RequiredHolder
    {
        [HumlRequired]
        public string Name { get; set; } = string.Empty;
    }

    [Fact]
    public void Cr06_required_property_present_does_not_throw()
    {
        const string huml = """
            %HUML v0.2.0
            Name: "Alice"
            """;

        var act = () => HumlSerializer.Deserialize<RequiredHolder>(huml, Opts);
        act.Should().NotThrow();
    }

    [Fact]
    public void Cr06_required_property_absent_throws()
    {
        const string huml = """
            %HUML v0.2.0
            Other: "ignored"
            """;

        var act = () => HumlSerializer.Deserialize<RequiredHolder>(huml, new HumlOptions
        {
            UnmappedMemberHandling = UnmappedMemberHandling.Skip,
        });
        act.Should().Throw<HumlDeserializeException>()
            .WithMessage("*required*");
    }

    // ── WR-11: IDictionary<string,T> materialises as Dictionary<string,T> ─────

    private sealed class WithIDictionary
    {
        public IDictionary<string, int> Scores { get; set; } = new Dictionary<string, int>();
    }

    [Fact]
    public void Wr11_idictionary_property_deserialises_correctly()
    {
        const string huml = """
            %HUML v0.2.0
            Scores::
              Alice: 100
              Bob: 95
            """;

        var result = HumlSerializer.Deserialize<WithIDictionary>(huml, Opts);
        result.Scores.Should().ContainKey("Alice").WhoseValue.Should().Be(100);
        result.Scores.Should().ContainKey("Bob").WhoseValue.Should().Be(95);
    }

    [Fact]
    public void Wr11_idictionary_string_string_deserialises()
    {
        const string huml = """
            %HUML v0.2.0
            Labels::
              env: "prod"
              region: "eu-west"
            """;

        var result = HumlSerializer.Deserialize<WithStringDict>(huml, Opts);
        result.Labels.Should().ContainKey("env").WhoseValue.Should().Be("prod");
    }

    private sealed class WithStringDict
    {
        public IDictionary<string, string> Labels { get; set; } = new Dictionary<string, string>();
    }

    // ── WR-06: ICollection<T> / IReadOnlyList<T> via IEnumerable<T> fallback ──

    private sealed class WithICollection
    {
        public ICollection<int> Items { get; set; } = new List<int>();
    }

#pragma warning disable CA1859 // interface declared intentionally to test IReadOnlyList<T> deserialization
    private sealed class WithIReadOnlyList
    {
        public IReadOnlyList<string> Names { get; set; } = new List<string>();
    }
#pragma warning restore CA1859

    [Fact]
    public void Wr06_icollection_deserialises_as_list()
    {
        const string huml = """
            %HUML v0.2.0
            Items::
              - 1
              - 2
              - 3
            """;

        var result = HumlSerializer.Deserialize<WithICollection>(huml, Opts);
        result.Items.Should().HaveCount(3);
        result.Items.Should().Contain(2);
    }

    [Fact]
    public void Wr06_ireadonlylist_deserialises_as_list()
    {
        const string huml = """
            %HUML v0.2.0
            Names::
              - "Alice"
              - "Bob"
            """;

        var result = HumlSerializer.Deserialize<WithIReadOnlyList>(huml, Opts);
        result.Names.Should().HaveCount(2);
        result.Names[0].Should().Be("Alice");
    }
}
