using AwesomeAssertions;
using Huml.Net.Exceptions;
using Huml.Net.Versioning;
using Xunit;

namespace Huml.Net.Tests;

public sealed class HumlOptionsStrictTests
{
    // ── STRICT-01: ValidateDuplicateKeysOnWrite is true ───────────────────────

    [Fact]
    public void Strict01_validates_duplicate_keys_on_write()
    {
        HumlOptions.Strict.ValidateDuplicateKeysOnWrite.Should().BeTrue();
    }

    // ── STRICT-02: UnmappedMemberHandling is Disallow ─────────────────────────

    [Fact]
    public void Strict02_unmapped_member_handling_is_disallow()
    {
        HumlOptions.Strict.UnmappedMemberHandling.Should().Be(UnmappedMemberHandling.Disallow);
    }

    // ── STRICT-03: UnknownVersionBehaviour is Throw ───────────────────────────

    [Fact]
    public void Strict03_unknown_version_behaviour_is_throw()
    {
        HumlOptions.Strict.UnknownVersionBehaviour.Should().Be(UnknownVersionBehaviour.Throw);
    }

    // ── STRICT-04: Strict is pre-frozen ──────────────────────────────────────

    [Fact]
    public void Strict04_is_read_only()
    {
        HumlOptions.Strict.IsReadOnly.Should().BeTrue();
    }

    // ── STRICT-05: functional — unknown key throws with Strict ────────────────

    [Fact]
    public void Strict05_deserialise_unknown_key_throws_with_strict_options()
    {
        const string huml = """
            %HUML v0.2.0
            Name: "Alice"
            UnknownKey: 123
            """;

        var act = () => HumlSerializer.Deserialize<StrictDto>(huml, HumlOptions.Strict);

        act.Should().Throw<HumlDeserializeException>()
            .WithMessage("*UnknownKey*");
    }

    private sealed class StrictDto
    {
        public string Name { get; set; } = string.Empty;
    }
}
