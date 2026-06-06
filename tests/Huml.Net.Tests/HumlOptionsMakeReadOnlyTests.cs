using AwesomeAssertions;
using Huml.Net.Versioning;
using Xunit;

namespace Huml.Net.Tests;

public sealed class HumlOptionsMakeReadOnlyTests
{
    // ── LOCK-01: new instance starts mutable ──────────────────────────────────

    [Fact]
    public void Lock01_new_instance_is_not_read_only()
    {
        var opts = new HumlOptions();

        opts.IsReadOnly.Should().BeFalse();
    }

    // ── LOCK-02: MakeReadOnly sets IsReadOnly ─────────────────────────────────

    [Fact]
    public void Lock02_MakeReadOnly_sets_IsReadOnly_true()
    {
        var opts = new HumlOptions();
        opts.MakeReadOnly();

        opts.IsReadOnly.Should().BeTrue();
    }

    // ── LOCK-03: MakeReadOnly is idempotent ───────────────────────────────────

    [Fact]
    public void Lock03_MakeReadOnly_is_idempotent()
    {
        var opts = new HumlOptions();

        var act = () =>
        {
            opts.MakeReadOnly();
            opts.MakeReadOnly();
        };

        act.Should().NotThrow();
        opts.IsReadOnly.Should().BeTrue();
    }

    // ── LOCK-04: LatestSupported is pre-frozen ────────────────────────────────

    [Fact]
    public void Lock04_LatestSupported_is_read_only()
    {
        HumlOptions.LatestSupported.IsReadOnly.Should().BeTrue();
    }

    // ── LOCK-05: Default is pre-frozen ────────────────────────────────────────

    [Fact]
    public void Lock05_Default_is_read_only()
    {
        HumlOptions.Default.IsReadOnly.Should().BeTrue();
    }

    // ── LOCK-06: AutoDetect (alias for Default) is pre-frozen ─────────────────

    [Fact]
    public void Lock06_AutoDetect_is_read_only()
    {
        HumlOptions.AutoDetect.IsReadOnly.Should().BeTrue();
    }

    // ── LOCK-07: ThrowIfReadOnly throws on frozen instance ────────────────────

    [Fact]
    public void Lock07_ThrowIfReadOnly_throws_on_frozen_instance()
    {
        var opts = new HumlOptions();
        opts.MakeReadOnly();

        var act = () => opts.ThrowIfReadOnly();

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*read-only*");
    }
}
