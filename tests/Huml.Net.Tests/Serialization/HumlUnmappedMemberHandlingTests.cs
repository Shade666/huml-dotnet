using AwesomeAssertions;
using Huml.Net.Exceptions;
using Huml.Net.Serialization;
using Huml.Net.Versioning;
using Xunit;

namespace Huml.Net.Tests.Serialization;

public sealed class HumlUnmappedMemberHandlingTests
{
    // ── DTOs ──────────────────────────────────────────────────────────────────

    private sealed class Simple
    {
        public string Name { get; set; } = string.Empty;
    }

    private sealed class WithExtensionData
    {
        public string Name { get; set; } = string.Empty;
        [HumlExtensionData]
        public Dictionary<string, object?> Extra { get; set; } = [];
    }

    // ── UMH-01: Skip (default) — unknown keys are silently ignored ────────────

    [Fact]
    public void Umh01_skip_silently_ignores_unknown_keys()
    {
        const string huml = """
            %HUML v0.2.0
            Name: "Alice"
            Unknown: "surprise"
            """;

        var opts = new HumlOptions { UnmappedMemberHandling = UnmappedMemberHandling.Skip };
        var act = () => HumlSerializer.Deserialize<Simple>(huml, opts);

        act.Should().NotThrow();
    }

    // ── UMH-02: Disallow — unknown key throws HumlDeserializeException ────────

    [Fact]
    public void Umh02_disallow_throws_on_unknown_key()
    {
        const string huml = """
            %HUML v0.2.0
            Name: "Alice"
            Unknown: "surprise"
            """;

        var opts = new HumlOptions { UnmappedMemberHandling = UnmappedMemberHandling.Disallow };
        var act = () => HumlSerializer.Deserialize<Simple>(huml, opts);

        act.Should().Throw<HumlDeserializeException>();
    }

    // ── UMH-03: Disallow — exception message includes the unrecognised key ────

    [Fact]
    public void Umh03_disallow_exception_message_includes_key_name()
    {
        const string huml = """
            %HUML v0.2.0
            Name: "Alice"
            Unknown: "surprise"
            """;

        var opts = new HumlOptions { UnmappedMemberHandling = UnmappedMemberHandling.Disallow };
        var act = () => HumlSerializer.Deserialize<Simple>(huml, opts);

        act.Should().Throw<HumlDeserializeException>()
            .WithMessage("*Unknown*");
    }

    // ── UMH-04: Disallow — known keys still deserialise correctly ─────────────

    [Fact]
    public void Umh04_disallow_known_keys_still_deserialise()
    {
        const string huml = """
            %HUML v0.2.0
            Name: "Alice"
            """;

        var opts = new HumlOptions { UnmappedMemberHandling = UnmappedMemberHandling.Disallow };
        var result = HumlSerializer.Deserialize<Simple>(huml, opts);

        result.Name.Should().Be("Alice");
    }

    // ── UMH-05: Disallow suppressed by [HumlExtensionData] ───────────────────

    [Fact]
    public void Umh05_extension_data_suppresses_disallow()
    {
        const string huml = """
            %HUML v0.2.0
            Name: "Alice"
            Extra1: "value1"
            """;

        var opts = new HumlOptions { UnmappedMemberHandling = UnmappedMemberHandling.Disallow };
        var act = () => HumlSerializer.Deserialize<WithExtensionData>(huml, opts);

        act.Should().NotThrow();
    }

    // ── UMH-06: default is Skip ───────────────────────────────────────────────

    [Fact]
    public void Umh06_default_unmapped_handling_is_skip()
    {
        var opts = new HumlOptions();

        opts.UnmappedMemberHandling.Should().Be(UnmappedMemberHandling.Skip);
    }

    // ── UMH-07: LatestSupported default is Skip ───────────────────────────────

    [Fact]
    public void Umh07_LatestSupported_unmapped_handling_is_skip()
    {
        HumlOptions.LatestSupported.UnmappedMemberHandling.Should().Be(UnmappedMemberHandling.Skip);
    }
}
