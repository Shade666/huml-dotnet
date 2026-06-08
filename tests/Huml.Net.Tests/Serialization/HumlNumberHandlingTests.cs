using AwesomeAssertions;
using Huml.Net.Exceptions;
using Huml.Net.Versioning;
using Xunit;

namespace Huml.Net.Tests.Serialization;

/// <summary>
/// Tests for <see cref="HumlNumberHandling"/> enum and <see cref="HumlOptions.NumberHandling"/>
/// property wired through <c>HumlDeserializer.CoerceScalar</c> and
/// <c>HumlSerializer.SerializeValueInternal</c>.
/// </summary>
public sealed class HumlNumberHandlingTests
{
    // ── NUM-01: Strict (default) deserialisation — string → numeric throws ─────

    [Fact]
    public void Num01_strict_rejects_string_to_int()
    {
        const string huml = "%HUML v0.2.0\nValue: \"42\"";
        var act = () => Huml.Deserialize<IntDto>(huml, HumlOptions.LatestSupported);

        act.Should().Throw<HumlDeserializeException>();
    }

    [Fact]
    public void Num13_strict_rejects_string_to_long()
    {
        const string huml = "%HUML v0.2.0\nValue: \"100\"";
        var act = () => Huml.Deserialize<LongDto>(huml, HumlOptions.LatestSupported);

        act.Should().Throw<HumlDeserializeException>();
    }

    [Fact]
    public void Num14_strict_rejects_string_to_double()
    {
        const string huml = "%HUML v0.2.0\nValue: \"3.14\"";
        var act = () => Huml.Deserialize<DoubleDto>(huml, HumlOptions.LatestSupported);

        act.Should().Throw<HumlDeserializeException>();
    }

    // ── NUM-01: AllowReadingFromString — string → numeric succeeds ────────────

    [Fact]
    public void Num02_allow_reading_from_string_coerces_int()
    {
        const string huml = "%HUML v0.2.0\nValue: \"42\"";
        var opts = new HumlOptions { NumberHandling = HumlNumberHandling.AllowReadingFromString };

        var result = Huml.Deserialize<IntDto>(huml, opts);

        result!.Value.Should().Be(42);
    }

    // ── NUM-01: AllowReadingFromString must not break temporal coercion ────────

    [Fact]
    public void Num15_allow_reading_still_allows_temporal()
    {
        const string huml = "%HUML v0.2.0\nValue: \"2024-01-15T00:00:00.0000000\"";
        var opts = new HumlOptions { NumberHandling = HumlNumberHandling.AllowReadingFromString };
        var act = () => Huml.Deserialize<DateDto>(huml, opts);

        act.Should().NotThrow();
    }

    // ── NUM-02: Strict (default) serialisation — bare numerics ───────────────

    [Fact]
    public void Num03_strict_emits_bare_integer()
    {
        var output = Huml.Serialize(new IntDto { Value = 42 }, HumlOptions.LatestSupported);

        output.Should().Contain("Value: 42\n");
    }

    // ── NUM-02: WriteAsString — finite values are quoted ─────────────────────

    [Fact]
    public void Num04_write_as_string_quotes_integer()
    {
        var opts = new HumlOptions { NumberHandling = HumlNumberHandling.WriteAsString };
        var output = Huml.Serialize(new IntDto { Value = 42 }, opts);

        output.Should().Contain("Value: \"42\"\n");
    }

    [Fact]
    public void Num05_write_as_string_quotes_double()
    {
        var opts = new HumlOptions { NumberHandling = HumlNumberHandling.WriteAsString };
        var output = Huml.Serialize(new DoubleDto { Value = 3.14 }, opts);

        output.Should().Contain("Value: \"3.14\"\n");
    }

    [Fact]
    public void Num06_write_as_string_quotes_float()
    {
        var opts = new HumlOptions { NumberHandling = HumlNumberHandling.WriteAsString };
        var output = Huml.Serialize(new FloatDto { Value = 1.5f }, opts);

        output.Should().Contain("Value: \"1.5\"\n");
    }

    [Fact]
    public void Num07_write_as_string_quotes_decimal()
    {
        var opts = new HumlOptions { NumberHandling = HumlNumberHandling.WriteAsString };
        var output = Huml.Serialize(new DecimalDto { Value = 9.99m }, opts);

        output.Should().Contain("Value: \"9.99\"\n");
    }

    // ── NUM-02: WriteAsString — NaN/Inf are NEVER quoted ─────────────────────

    [Fact]
    public void Num08_nan_never_quoted()
    {
        var opts = new HumlOptions { NumberHandling = HumlNumberHandling.WriteAsString };
        var output = Huml.Serialize(new DoubleDto { Value = double.NaN }, opts);

        output.Should().Contain("Value: nan\n");
        output.Should().NotContain("Value: \"nan\"");
    }

    [Fact]
    public void Num09_positive_inf_never_quoted()
    {
        var opts = new HumlOptions { NumberHandling = HumlNumberHandling.WriteAsString };
        var output = Huml.Serialize(new DoubleDto { Value = double.PositiveInfinity }, opts);

        output.Should().Contain("Value: +inf\n");
        output.Should().NotContain("Value: \"+inf\"");
    }

    [Fact]
    public void Num10_negative_inf_never_quoted()
    {
        var opts = new HumlOptions { NumberHandling = HumlNumberHandling.WriteAsString };
        var output = Huml.Serialize(new DoubleDto { Value = double.NegativeInfinity }, opts);

        output.Should().Contain("Value: -inf\n");
        output.Should().NotContain("Value: \"-inf\"");
    }

    // ── NUM-01+NUM-02: Round-trip — WriteAsString + AllowReadingFromString ────

    [Fact]
    public void Num11_round_trip_int()
    {
        var serOpts = new HumlOptions { NumberHandling = HumlNumberHandling.WriteAsString };
        var desOpts = new HumlOptions { NumberHandling = HumlNumberHandling.AllowReadingFromString };

        var serialised = Huml.Serialize(new IntDto { Value = 99 }, serOpts);
        var result = Huml.Deserialize<IntDto>(serialised, desOpts);

        result!.Value.Should().Be(99);
    }

    [Fact]
    public void Num12_round_trip_double()
    {
        var serOpts = new HumlOptions { NumberHandling = HumlNumberHandling.WriteAsString };
        var desOpts = new HumlOptions { NumberHandling = HumlNumberHandling.AllowReadingFromString };

        var serialised = Huml.Serialize(new DoubleDto { Value = 2.718 }, serOpts);
        var result = Huml.Deserialize<DoubleDto>(serialised, desOpts);

        result!.Value.Should().BeApproximately(2.718, 0.0001);
    }

    // ── DTOs ─────────────────────────────────────────────────────────────────

    private sealed class IntDto
    {
        public int Value { get; set; }
    }

    private sealed class LongDto
    {
        public long Value { get; set; }
    }

    private sealed class DoubleDto
    {
        public double Value { get; set; }
    }

    private sealed class FloatDto
    {
        public float Value { get; set; }
    }

    private sealed class DecimalDto
    {
        public decimal Value { get; set; }
    }

    private sealed class DateDto
    {
        public DateTime Value { get; set; }
    }
}
