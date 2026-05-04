using AwesomeAssertions;
using Huml.Net.Versioning;
using Xunit;

namespace Huml.Net.Tests.Serialization;

public sealed class HumlDateTimeSerializationTests
{
    private static readonly HumlOptions Opts = HumlOptions.LatestSupported;

    // ── DTOs ──────────────────────────────────────────────────────────────────

    private sealed class WithDateTime
    {
        public DateTime When { get; set; }
        public DateTime? MaybeWhen { get; set; }
    }

    private sealed class WithDateTimeOffset
    {
        public DateTimeOffset Stamp { get; set; }
    }

    private sealed class WithTimeSpan
    {
        public TimeSpan Duration { get; set; }
    }

#if NET6_0_OR_GREATER
    private sealed class WithDateOnly
    {
        public DateOnly Day { get; set; }
        public DateOnly? MaybeDay { get; set; }
    }

    private sealed class WithTimeOnly
    {
        public TimeOnly Moment { get; set; }
    }
#endif

    // ── DATE-01: DateTime ─────────────────────────────────────────────────────

    [Fact]
    public void Date01_DateTime_serialises_as_quoted_O_format()
    {
        var dto = new WithDateTime { When = new DateTime(2024, 3, 15, 10, 30, 0, DateTimeKind.Utc) };
        var huml = Huml.Serialize(dto, Opts);
        huml.Should().Contain("When: \"2024-03-15T10:30:00.0000000Z\"");
    }

    [Fact]
    public void Date01_DateTime_round_trips()
    {
        const string huml = """
            %HUML v0.2.0
            When: "2024-03-15T10:30:00.0000000Z"
            MaybeWhen: null
            """;
        var result = Huml.Deserialize<WithDateTime>(huml, Opts);
        result.When.Should().Be(new DateTime(2024, 3, 15, 10, 30, 0, DateTimeKind.Utc));
        result.When.Kind.Should().Be(DateTimeKind.Utc);
        result.MaybeWhen.Should().BeNull();
    }

    [Fact]
    public void Date01_Nullable_DateTime_round_trips_value()
    {
        var dto = new WithDateTime
        {
            When = new DateTime(2024, 3, 15, 10, 30, 0, DateTimeKind.Utc),
            MaybeWhen = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc)
        };
        var huml = Huml.Serialize(dto, Opts);
        var result = Huml.Deserialize<WithDateTime>(huml, Opts);
        result.MaybeWhen.Should().Be(dto.MaybeWhen);
    }

    // ── DATE-02: DateTimeOffset ───────────────────────────────────────────────

    [Fact]
    public void Date02_DateTimeOffset_serialises_preserving_offset()
    {
        var dto = new WithDateTimeOffset
        {
            Stamp = new DateTimeOffset(2024, 3, 15, 10, 30, 0, TimeSpan.FromMinutes(330)) // +05:30
        };
        var huml = Huml.Serialize(dto, Opts);
        huml.Should().Contain("Stamp: \"2024-03-15T10:30:00.0000000+05:30\"");
    }

    [Fact]
    public void Date02_DateTimeOffset_round_trips_with_offset()
    {
        const string huml = """
            %HUML v0.2.0
            Stamp: "2024-03-15T10:30:00.0000000+05:30"
            """;
        var result = Huml.Deserialize<WithDateTimeOffset>(huml, Opts);
        result.Stamp.Offset.Should().Be(TimeSpan.FromMinutes(330));
        result.Stamp.Year.Should().Be(2024);
        result.Stamp.Month.Should().Be(3);
        result.Stamp.Day.Should().Be(15);
    }

    // ── DATE-03: TimeSpan ─────────────────────────────────────────────────────

    [Fact]
    public void Date03_TimeSpan_serialises_as_constant_format()
    {
        var dto = new WithTimeSpan { Duration = new TimeSpan(1, 2, 3, 4, 567) };
        var huml = Huml.Serialize(dto, Opts);
        huml.Should().Contain("Duration: \"1.02:03:04.5670000\"");
    }

    [Fact]
    public void Date03_TimeSpan_round_trips()
    {
        var original = new WithTimeSpan { Duration = new TimeSpan(1, 2, 3, 4, 567) };
        var huml = Huml.Serialize(original, Opts);
        var result = Huml.Deserialize<WithTimeSpan>(huml, Opts);
        result.Duration.Should().Be(original.Duration);
    }

    [Fact]
    public void Date03_Negative_TimeSpan_round_trips()
    {
        var original = new WithTimeSpan { Duration = TimeSpan.FromSeconds(-90) };
        var huml = Huml.Serialize(original, Opts);
        huml.Should().Contain("\"-00:01:30\"");
        var result = Huml.Deserialize<WithTimeSpan>(huml, Opts);
        result.Duration.Should().Be(original.Duration);
    }

    // ── DATE-04: DateOnly (NET6_0_OR_GREATER) ─────────────────────────────────

#if NET6_0_OR_GREATER
    [Fact]
    public void Date04_DateOnly_serialises_as_yyyy_MM_dd()
    {
        var dto = new WithDateOnly { Day = new DateOnly(2024, 3, 15) };
        var huml = Huml.Serialize(dto, Opts);
        huml.Should().Contain("Day: \"2024-03-15\"");
    }

    [Fact]
    public void Date04_DateOnly_round_trips()
    {
        var original = new WithDateOnly { Day = new DateOnly(2024, 3, 15) };
        var huml = Huml.Serialize(original, Opts);
        var result = Huml.Deserialize<WithDateOnly>(huml, Opts);
        result.Day.Should().Be(original.Day);
    }

    [Fact]
    public void Date04_Nullable_DateOnly_null_round_trips()
    {
        var original = new WithDateOnly { Day = new DateOnly(2024, 3, 15), MaybeDay = null };
        var huml = Huml.Serialize(original, Opts);
        var result = Huml.Deserialize<WithDateOnly>(huml, Opts);
        result.MaybeDay.Should().BeNull();
    }

    [Fact]
    public void Date04_Nullable_DateOnly_value_round_trips()
    {
        var original = new WithDateOnly
        {
            Day = new DateOnly(2024, 3, 15),
            MaybeDay = new DateOnly(2025, 12, 31)
        };
        var huml = Huml.Serialize(original, Opts);
        var result = Huml.Deserialize<WithDateOnly>(huml, Opts);
        result.MaybeDay.Should().Be(original.MaybeDay);
    }

    // ── DATE-05: TimeOnly (NET6_0_OR_GREATER) ─────────────────────────────────

    [Fact]
    public void Date05_TimeOnly_serialises_zero_fraction_without_trailing_zeros()
    {
        var dto = new WithTimeOnly { Moment = new TimeOnly(10, 30, 0) };
        var huml = Huml.Serialize(dto, Opts);
        huml.Should().Contain("Moment: \"10:30:00\"");
    }

    [Fact]
    public void Date05_TimeOnly_serialises_nonzero_fraction()
    {
        var dto = new WithTimeOnly { Moment = new TimeOnly(10, 30, 0, 123) };
        var huml = Huml.Serialize(dto, Opts);
        huml.Should().Contain("Moment: \"10:30:00.123\"");
    }

    [Fact]
    public void Date05_TimeOnly_round_trips_zero_fraction()
    {
        var original = new WithTimeOnly { Moment = new TimeOnly(10, 30, 0) };
        var huml = Huml.Serialize(original, Opts);
        var result = Huml.Deserialize<WithTimeOnly>(huml, Opts);
        result.Moment.Should().Be(original.Moment);
    }

    [Fact]
    public void Date05_TimeOnly_round_trips_nonzero_fraction()
    {
        var original = new WithTimeOnly { Moment = new TimeOnly(10, 30, 0, 123) };
        var huml = Huml.Serialize(original, Opts);
        var result = Huml.Deserialize<WithTimeOnly>(huml, Opts);
        result.Moment.Should().Be(original.Moment);
    }
#endif
}
