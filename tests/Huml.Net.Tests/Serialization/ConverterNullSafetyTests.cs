using AwesomeAssertions;
using Huml.Net.Parser;
using Huml.Net.Serialization;
using Huml.Net.Versioning;
using Xunit;

namespace Huml.Net.Tests.Serialization;

/// <summary>
/// Phase 42 — serialiser/converter null-safety fixes:
/// CR-05 (EmitEntry re-entry guard covers null value),
/// IN-04 (WriteObject emits null rather than calling Write).
/// </summary>
public sealed class ConverterNullSafetyTests
{
    public ConverterNullSafetyTests()
    {
        PropertyDescriptor.ClearCache();
        ConverterCache.ClearCache();
        HumlOptions.ClearOptionsCaches();
    }

    private static readonly HumlOptions Opts = HumlOptions.LatestSupported;

    // ── IN-04: WriteObject emits "null" for reference-type null values ─────────

    private sealed class NullableStringConverter : HumlConverter<string>
    {
        public override string? Read(HumlNode node) => node is HumlScalar s ? s.Value as string : null;

        public override void Write(HumlWriterContext context, string value)
        {
            // value must never be null — IN-04 fix ensures WriteObject doesn't pass null here.
            if (value is null)
                throw new InvalidOperationException("Write called with null — should not happen after IN-04 fix.");
            context.AppendRaw($"\"{value}\"");
        }
    }

    private sealed class DtoWithNullableString
    {
        [HumlConverter(typeof(NullableStringConverter))]
        public string? Label { get; set; }
    }

    [Fact]
    public void In04_converter_write_not_called_for_null_reference_value()
    {
        // The converter's Write method throws if called with null; if IN-04 is wrong,
        // WriteObject would call Write(context, null) and this test would throw.
        var dto = new DtoWithNullableString { Label = null };
        var act = () => HumlSerializer.Serialize(dto, Opts);
        act.Should().NotThrow();
    }

    [Fact]
    public void In04_null_reference_value_emits_null_keyword()
    {
        var dto = new DtoWithNullableString { Label = null };
        var huml = HumlSerializer.Serialize(dto, Opts);
        huml.Should().Contain("Label: null");
    }

    [Fact]
    public void In04_non_null_reference_value_calls_converter_write()
    {
        var dto = new DtoWithNullableString { Label = "hello" };
        var huml = HumlSerializer.Serialize(dto, Opts);
        huml.Should().Contain("Label: \"hello\"");
    }

    // ── CR-05: Re-entry guard fires for recursive converter call ──────────────
    // TreeNode has a Child property with the same converter — so when a converter's
    // Write passes the same value to AppendSerializedValue, POCO serialization of that
    // value calls EmitEntry for Child again, hitting the guard on the second level.

    private sealed class TreeNode
    {
        public string Name { get; set; } = string.Empty;

        [HumlConverter(typeof(TreeNodeConverter))]
        public TreeNode? Child { get; set; }
    }

    private sealed class TreeNodeConverter : HumlConverter<TreeNode>
    {
        public override TreeNode? Read(HumlNode n) => null;

        public override void Write(HumlWriterContext ctx, TreeNode value)
        {
            // Intentionally passes the same value back — triggers re-entry for the Child property.
            ctx.AppendSerializedValue(value);
        }
    }

    private sealed class TreeRoot { public TreeNode Root { get; set; } = new(); }

    [Fact]
    public void Cr05_reentry_guard_fires_on_recursive_converter_call()
    {
        // 3-level tree: root → child → grandchild.
        // EmitEntry for 'Child' of root adds TreeNode to the active-converter guard.
        // Write passes child back to AppendSerializedValue → POCO serializes child →
        // EmitEntry for 'Child' of child with the same converter → guard fires.
        var dto = new TreeRoot
        {
            Root = new TreeNode
            {
                Name = "root",
                Child = new TreeNode
                {
                    Name = "child",
                    Child = new TreeNode { Name = "grandchild" },
                },
            },
        };

        var act = () => HumlSerializer.Serialize(dto, Opts);
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*re-entry*");
    }
}
