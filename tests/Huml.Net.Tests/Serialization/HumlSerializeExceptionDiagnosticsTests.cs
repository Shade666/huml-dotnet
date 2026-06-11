using AwesomeAssertions;
using Huml.Net.Exceptions;
using Huml.Net.Versioning;
using Xunit;

namespace Huml.Net.Tests.Serialization;

public sealed class HumlSerializeExceptionDiagnosticsTests
{
    private static readonly HumlOptions Opts = HumlOptions.LatestSupported;

    private sealed class WithDelegate
    {
        public Action? Handler { get; set; } = () => { };
    }

    private sealed class WithDelegateInList
    {
        public List<object?> Items { get; set; } = [() => { }];
    }

    // ── DIAG-01: exception message includes property name ─────────────────────

    [Fact]
    public void Diag01_exception_message_includes_property_name()
    {
        var dto = new WithDelegate();
        var act = () => HumlSerializer.Serialize(dto, Opts);

        act.Should().Throw<HumlSerializeException>()
            .WithMessage("*Handler*");
    }

    // ── DIAG-02: exception message includes containing type name ──────────────

    [Fact]
    public void Diag02_exception_message_includes_type_name()
    {
        var dto = new WithDelegate();
        var act = () => HumlSerializer.Serialize(dto, Opts);

        act.Should().Throw<HumlSerializeException>()
            .WithMessage("*WithDelegate*");
    }

    // ── DIAG-03: message matches canonical format ─────────────────────────────

    [Fact]
    public void Diag03_exception_message_matches_canonical_format()
    {
        var dto = new WithDelegate();
        var act = () => HumlSerializer.Serialize(dto, Opts);

        act.Should().Throw<HumlSerializeException>()
            .WithMessage("Cannot serialize property 'Handler' on type 'WithDelegate':*");
    }

    // ── DIAG-04: unsupported type in sequence item still throws ───────────────

    [Fact]
    public void Diag04_unsupported_type_in_sequence_item_still_throws()
    {
        var dto = new WithDelegateInList();
        var act = () => HumlSerializer.Serialize(dto, Opts);

        act.Should().Throw<HumlSerializeException>();
    }
}
