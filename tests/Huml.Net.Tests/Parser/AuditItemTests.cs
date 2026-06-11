using AwesomeAssertions;
using Huml.Net.Exceptions;
using Huml.Net.Parser;
using Huml.Net.Versioning;
using Xunit;

namespace Huml.Net.Tests.Parser;

/// <summary>
/// Regression tests for the G3 AUDIT items from docs/plans/2026-06-10-backlog-disposition.md:
/// hex/octal/binary literals must overflow loudly like decimals (no silent two's-complement
/// wrap), and a leading BOM gets a self-explanatory error.
/// </summary>
public class AuditItemTests
{
    // ── Two's-complement wrap: 0xFFFFFFFFFFFFFFFF must not silently become -1 ──

    [Theory]
    [InlineData("key: 0xFFFFFFFFFFFFFFFF")]
    [InlineData("key: 0x8000000000000000")]
    [InlineData("key: 0o1777777777777777777777")]
    [InlineData("key: 0b1111111111111111111111111111111111111111111111111111111111111111")]
    public void Base_prefixed_literal_exceeding_int64_throws(string input)
    {
        var act = () => HumlSerializer.Parse(input, HumlOptions.LatestSupported);
        act.Should().Throw<HumlParseException>().WithMessage("*overflow*");
    }

    [Theory]
    [InlineData("key: 0x7FFFFFFFFFFFFFFF", long.MaxValue)]
    [InlineData("key: -0x8000000000000000", long.MinValue)]
    [InlineData("key: 0xFF", 255L)]
    [InlineData("key: -0xFF", -255L)]
    public void Base_prefixed_literal_within_range_parses(string input, long expected)
    {
        var doc = HumlSerializer.Parse(input, HumlOptions.LatestSupported);
        var scalar = ((HumlMapping)doc.Entries[0]).Value.Should().BeOfType<HumlScalar>().Subject;
        scalar.Value.Should().Be(expected);
    }

    // ── BOM: rejected (spec-canonical content, go-huml-aligned) with a clear message ──

    [Fact]
    public void Leading_bom_throws_with_self_explanatory_message()
    {
        var act = () => HumlSerializer.Parse("﻿key: 1", HumlOptions.LatestSupported);
        act.Should().Throw<HumlParseException>().WithMessage("*byte-order mark*");
    }
}
