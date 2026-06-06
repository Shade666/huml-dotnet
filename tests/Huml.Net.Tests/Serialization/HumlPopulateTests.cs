using AwesomeAssertions;
using Huml.Net.Exceptions;
using Huml.Net.Parser;
using Huml.Net.Serialization;
using Huml.Net.Versioning;
using Xunit;

namespace Huml.Net.Tests.Serialization;

public class HumlPopulateTests
{
    // ── Helper types ──────────────────────────────────────────────────────────

    private class ConfigPoco
    {
        public string? Host { get; set; } = "localhost";
        public int Port { get; set; } = 8080;
        public bool Verbose { get; set; }
        public List<string>? Tags { get; set; }
        public Dictionary<string, string>? Labels { get; set; }
    }

    private class ReadOnlyPoco
    {
        public string? Name { get; } = "default";
    }

    private class InitOnlyPoco
    {
        public string? Value { get; init; }
    }

    private class IgnoredPoco
    {
        public string? Kept { get; set; }

        [HumlIgnore]
        public string? Hidden { get; set; }
    }

    private class ConverterPoco
    {
        [HumlConverter(typeof(UpperCaseConverter))]
        public string? Name { get; set; }
    }

    private sealed class UpperCaseConverter : HumlConverter<string?>
    {
        // typeof(string?) at runtime equals typeof(string) — nullability is compile-time only.
        // The property is declared string? but the CLR passes typeof(string) here.
        public override bool CanConvert(Type t) => t == typeof(string);

        public override string? Read(HumlNode node)
        {
            if (node is HumlScalar { Kind: ScalarKind.String, Value: string s })
                return s.ToUpperInvariant();
            return null;
        }

        public override void Write(HumlSerializerContext context, string? value)
            => context.AppendRaw(value is null ? "null" : $"\"{value.ToUpperInvariant()}\"");
    }

    private struct TestStruct
    {
        public int X { get; set; }
    }

    // ── Constructor: test isolation ───────────────────────────────────────────

    public HumlPopulateTests()
    {
        PropertyDescriptor.ClearCache();
        ConverterCache.ClearCache();
        HumlOptions.ClearOptionsCaches();
    }

    // ── Tests ─────────────────────────────────────────────────────────────────

    /// <summary>POP-03: Properties present in the HUML document overwrite the corresponding property on existing.</summary>
    [Fact]
    public void Populate_PresentProperty_OverwritesExistingValue()
    {
        var existing = new ConfigPoco { Host = "localhost", Port = 8080 };
        const string huml = "%HUML v0.2.0\nHost: \"prod.example.com\"\n";

        Huml.Populate(huml, existing, HumlOptions.LatestSupported);

        existing.Host.Should().Be("prod.example.com");
        existing.Port.Should().Be(8080);
    }

    /// <summary>POP-04: Properties absent from the HUML document are left unchanged on existing.</summary>
    [Fact]
    public void Populate_AbsentProperty_LeavesExistingValueUnchanged()
    {
        var existing = new ConfigPoco { Host = "localhost", Port = 9999 };
        const string huml = "%HUML v0.2.0\nVerbose: true\n";

        Huml.Populate(huml, existing, HumlOptions.LatestSupported);

        existing.Port.Should().Be(9999);
        existing.Verbose.Should().Be(true);
    }

    /// <summary>POP-05: Collection properties (List&lt;T&gt;) present in HUML are replaced, not merged.</summary>
    [Fact]
    public void Populate_CollectionProperty_ReplacesNotMerges()
    {
        var existing = new ConfigPoco { Tags = new List<string> { "old" } };
        const string huml = "%HUML v0.2.0\nTags::\n  - \"new1\"\n  - \"new2\"\n";

        Huml.Populate(huml, existing, HumlOptions.LatestSupported);

        existing.Tags.Should().NotBeNull();
        existing.Tags!.Should().HaveCount(2);
        existing.Tags[0].Should().Be("new1");
        existing.Tags[1].Should().Be("new2");
    }

    /// <summary>POP-06: Dictionary properties present in HUML are replaced on the existing instance.</summary>
    [Fact]
    public void Populate_DictionaryProperty_Replaces()
    {
        var existing = new ConfigPoco { Labels = new Dictionary<string, string> { ["k"] = "v" } };
        const string huml = "%HUML v0.2.0\nLabels::\n  a: \"b\"\n";

        Huml.Populate(huml, existing, HumlOptions.LatestSupported);

        existing.Labels.Should().NotBeNull();
        existing.Labels!.Should().HaveCount(1);
        existing.Labels["a"].Should().Be("b");
    }

    /// <summary>POP-07: If existing is null, ArgumentNullException is thrown immediately before any parsing occurs.</summary>
    [Fact]
    public void Populate_NullExisting_ThrowsArgumentNullException()
    {
        ConfigPoco? existing = null;
        var act = () => Huml.Populate<ConfigPoco>("@@invalid@@", existing!);

        act.Should().Throw<ArgumentNullException>();
    }

    /// <summary>POP-08: If T is a value type (struct), ArgumentException is thrown.</summary>
    [Fact]
    public void Populate_StructT_ThrowsArgumentException()
    {
        var act = () => Huml.Populate<TestStruct>("%HUML v0.2.0\n", new TestStruct());

        act.Should().Throw<ArgumentException>().WithMessage("*value type*");
    }

    [Fact]
    public void Populate_InitOnlyProperty_SetsValueSuccessfully()
    {
        var existing = new InitOnlyPoco();
        const string huml = "%HUML v0.2.0\nValue: \"updated\"\n";
        Huml.Populate(huml, existing, HumlOptions.LatestSupported);

        existing.Value.Should().Be("updated");
    }

    /// <summary>POP-10: Read-only properties (no setter) in the HUML document are skipped silently.</summary>
    [Fact]
    public void Populate_ReadOnlyProperty_SkipsSilently()
    {
        var existing = new ReadOnlyPoco();
        const string huml = "%HUML v0.2.0\nName: \"overridden\"\n";

        Huml.Populate(huml, existing, HumlOptions.LatestSupported);

        existing.Name.Should().Be("default");
    }

    /// <summary>POP-11: [HumlIgnore] properties are excluded from population.</summary>
    [Fact]
    public void Populate_HumlIgnoreProperty_Excluded()
    {
        var existing = new IgnoredPoco { Kept = "yes", Hidden = "secret" };
        const string huml = "%HUML v0.2.0\nKept: \"updated\"\nHidden: \"overwritten\"\n";

        Huml.Populate(huml, existing, HumlOptions.LatestSupported);

        existing.Kept.Should().Be("updated");
        existing.Hidden.Should().Be("secret");
    }

    /// <summary>POP-12: The full converter priority chain is honoured during population.</summary>
    [Fact]
    public void Populate_ConverterChainHonoured()
    {
        var existing = new ConverterPoco { Name = "original" };
        const string huml = "%HUML v0.2.0\nName: \"hello\"\n";

        Huml.Populate(huml, existing, HumlOptions.LatestSupported);

        existing.Name.Should().Be("HELLO");
    }

    /// <summary>POP-13: HumlParseException is propagated when the HUML input is invalid.</summary>
    [Fact]
    public void Populate_InvalidHuml_ThrowsHumlParseException()
    {
        var existing = new ConfigPoco();
        var act = () => Huml.Populate<ConfigPoco>("@@invalid@@", existing);

        act.Should().Throw<HumlParseException>();
    }

    /// <summary>POP-01: The string overload delegates to the span overload and mutates existing.</summary>
    [Fact]
    public void Populate_StringOverload_DelegatesToSpanOverload()
    {
        var existing = new ConfigPoco { Host = "before" };
        const string huml = "%HUML v0.2.0\nHost: \"after\"\n";

        Huml.Populate(huml, existing, HumlOptions.LatestSupported);

        existing.Host.Should().Be("after");
    }

    /// <summary>POP-02: The span overload is the single implementation overload, returns void, and mutates existing.</summary>
    [Fact]
    public void Populate_SpanOverload_ReturnsVoid()
    {
        var existing = new ConfigPoco { Port = 1234 };
        const string huml = "%HUML v0.2.0\nPort: 5678\n";

        Huml.Populate<ConfigPoco>(huml.AsSpan(), existing, HumlOptions.LatestSupported);

        existing.Port.Should().Be(5678);
    }
}
