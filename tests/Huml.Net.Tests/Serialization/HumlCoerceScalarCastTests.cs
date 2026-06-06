using AwesomeAssertions;
using Huml.Net.Exceptions;
using Huml.Net.Versioning;
using Xunit;

namespace Huml.Net.Tests.Serialization;

public sealed class HumlCoerceScalarCastTests
{
    private static readonly HumlOptions Opts = HumlOptions.LatestSupported;

    // ── DTOs ──────────────────────────────────────────────────────────────────

    private sealed class WithInt   { public int   Value { get; set; } }
    private sealed class WithByte  { public byte  Value { get; set; } }
    private sealed class WithULong { public ulong Value { get; set; } }

    // ── CAST-01: int round-trip ───────────────────────────────────────────────

    [Fact]
    public void Cast01_int_round_trip()
    {
        const string huml = """
            %HUML v0.2.0
            Value: 42
            """;

        var result = Huml.Deserialize<WithInt>(huml, Opts);

        result.Value.Should().Be(42);
    }

    // ── CAST-02: byte round-trip ──────────────────────────────────────────────

    [Fact]
    public void Cast02_byte_round_trip()
    {
        const string huml = """
            %HUML v0.2.0
            Value: 255
            """;

        var result = Huml.Deserialize<WithByte>(huml, Opts);

        result.Value.Should().Be((byte)255);
    }

    // ── CAST-03: int overflow throws HumlDeserializeException ────────────────

    [Fact]
    public void Cast03_int_overflow_throws_HumlDeserializeException()
    {
        const string huml = """
            %HUML v0.2.0
            Value: 2147483648
            """;

        var act = () => Huml.Deserialize<WithInt>(huml, Opts);

        act.Should().Throw<HumlDeserializeException>();
    }

    // ── CAST-04: byte overflow throws HumlDeserializeException ───────────────

    [Fact]
    public void Cast04_byte_overflow_throws_HumlDeserializeException()
    {
        const string huml = """
            %HUML v0.2.0
            Value: 256
            """;

        var act = () => Huml.Deserialize<WithByte>(huml, Opts);

        act.Should().Throw<HumlDeserializeException>();
    }

    // ── CAST-05: ulong round-trip (long.MaxValue fits ulong) ─────────────────

    [Fact]
    public void Cast05_ulong_round_trip()
    {
        const string huml = """
            %HUML v0.2.0
            Value: 9223372036854775807
            """;

        var result = Huml.Deserialize<WithULong>(huml, Opts);

        result.Value.Should().Be((ulong)long.MaxValue);
    }
}
