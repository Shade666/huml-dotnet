using AwesomeAssertions;
using Huml.Net.Serialization;
using Huml.Net.Versioning;
using Xunit;

namespace Huml.Net.Tests.Serialization;

public class MixedFixtureRoundTripTests
{
    // ── V01 options (same pattern as SharedSuiteTests.cs) ─────────────────────

#pragma warning disable CS0618
    private static readonly HumlOptions V01Options = new()
    {
        SpecVersion = HumlSpecVersion.V0_1,
        VersionSource = VersionSource.Options,
    };
#pragma warning restore CS0618

    // ── Test POCOs ────────────────────────────────────────────────────────────

    private class IntegersPoco
    {
        [HumlProperty("bar_positive")] public long BarPositive { get; set; }
        [HumlProperty("baz_negative")] public long BazNegative { get; set; }
        [HumlProperty("qux_zero")]     public long QuxZero { get; set; }
        [HumlProperty("corge_hex")]    public long CorgeHex { get; set; }
        [HumlProperty("grault_octal")] public long GraultOctal { get; set; }
        [HumlProperty("garply_binary")] public long GarplyBinary { get; set; }
        [HumlProperty("quux_underscore")] public long QuuxUnderscore { get; set; }
        [HumlProperty("waldo_large")]  public long WaldoLarge { get; set; }
    }

    private class FloatsPoco
    {
        [HumlProperty("bar_simple")]        public double BarSimple { get; set; }
        [HumlProperty("baz_negative")]      public double BazNeg { get; set; }
        [HumlProperty("corge_zero")]        public double CorgeZero { get; set; }
        [HumlProperty("qux_scientific")]    public double QuxScientific { get; set; }
        [HumlProperty("quux_scientific_neg")] public double QuuxSciNeg { get; set; }
        [HumlProperty("garply_large_exp")]  public double GarplyLargeExp { get; set; }
        [HumlProperty("grault_precision")]  public double GraultPrecision { get; set; }
    }

    private class StringsPoco
    {
        [HumlProperty("bar_empty")]       public string? BarEmpty { get; set; }
        [HumlProperty("baz_spaces")]      public string? BazSpaces { get; set; }
        [HumlProperty("corge_unicode")]   public string? CorgeUnicode { get; set; }
        [HumlProperty("garply_long")]     public string? GarplyLong { get; set; }
        [HumlProperty("grault_newlines")] public string? GraultNewlines { get; set; }
        [HumlProperty("quux_path")]       public string? QuuxPath { get; set; }
        [HumlProperty("qux_escaped")]     public string? QuxEscaped { get; set; }
    }

    private class BooleansPoco
    {
        [HumlProperty("bar_true")]      public bool BarTrue { get; set; }
        [HumlProperty("baz_false")]     public bool BazFalse { get; set; }
        [HumlProperty("qux_TRUE")]      public bool QuxTRUE { get; set; }
        [HumlProperty("corge_True")]    public bool CorgeTrueCase { get; set; }
        [HumlProperty("quux_FALSE")]    public bool QuuxFALSE { get; set; }
        [HumlProperty("grault_False")]  public bool GraultFalseCase { get; set; }
    }

    private class NullsPoco
    {
        [HumlProperty("bar_null")]  public string? BarNull { get; set; }
        [HumlProperty("baz_NULL")]  public string? BazNULL { get; set; }
        [HumlProperty("qux_Null")]  public string? QuxNull { get; set; }
    }

    private class BasicScalarsPoco
    {
        [HumlProperty("foo_string")]  public string? FooString { get; set; }
        [HumlProperty("bar_string")]  public string? BarString { get; set; }
        [HumlProperty("baz_int")]     public int BazInt { get; set; }
        [HumlProperty("qux_float")]   public double QuxFloat { get; set; }
        [HumlProperty("quux_bool")]   public bool QuuxBool { get; set; }
        [HumlProperty("corge_bool")]  public bool CorgeBool { get; set; }
        [HumlProperty("grault_null")] public string? GraultNull { get; set; }
    }

    private class GraultDeepPoco
    {
        [HumlProperty("garply_deeper")] public string? GarplyDeeper { get; set; }
        [HumlProperty("waldo_numbers")] public List<int>? WaldoNumbers { get; set; }
    }

    private class QuxNestedPoco
    {
        [HumlProperty("corge_sub")]   public bool CorgeSub { get; set; }
        [HumlProperty("quux_sub")]    public string? QuuxSub { get; set; }
        [HumlProperty("grault_deep")] public GraultDeepPoco? GraultDeep { get; set; }
    }

    private class FooDictPoco
    {
        [HumlProperty("bar_key")]    public string? BarKey { get; set; }
        [HumlProperty("baz_key")]    public long BazKey { get; set; }
        [HumlProperty("qux_nested")] public QuxNestedPoco? QuxNested { get; set; }
    }

    private class EmptyCollectionsPoco
    {
        [HumlProperty("foo_empty_list")] public List<string>? EmptyList { get; set; }
        [HumlProperty("bar_empty_dict")] public Dictionary<string, string>? EmptyDict { get; set; }
    }

    private class SpecialKeysWrapper
    {
        [HumlProperty("foo_special_keys")] public Dictionary<string, string>? Keys { get; set; }
    }

    private class EdgeCaseKeysWrapper
    {
        [HumlProperty("foo_edge_cases")] public Dictionary<string, string>? Keys { get; set; }
    }

    // ── Inline POCO types ─────────────────────────────────────────────────────

    private class InlineIntListPoco
    {
        public List<int> Tags { get; set; } = new();
    }

    private class InlineStringListPoco
    {
        public List<string> Items { get; set; } = new();
    }

    private class InlineBoolListPoco
    {
        public List<bool> Flags { get; set; } = new();
    }

    private class InlineDictCountsPoco
    {
        public Dictionary<string, int> Counts { get; set; } = new();
    }

    private class InlineAttrPoco
    {
        [HumlProperty(Inline = InlineMode.Inline)]
        public List<int> Tags { get; set; } = new();
    }

    private class InlineMixedStringListPoco
    {
        public List<string> Items { get; set; } = new();
    }

    // ── Constructor ───────────────────────────────────────────────────────────

    public MixedFixtureRoundTripTests()
    {
        PropertyDescriptor.ClearCache();
    }

    // ── Helper ────────────────────────────────────────────────────────────────

    private static string LoadFixture(string version, string filename)
        => File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "fixtures", version, "documents", filename));

    // ── Task 1: Parse verification ────────────────────────────────────────────

    [Fact]
    public void V02_MixedDocument_ParsesWithoutError()
    {
        var huml = LoadFixture("v0.2", "mixed.huml");
        var act = () => HumlSerializer.Parse(huml, HumlOptions.AutoDetect);
        act.Should().NotThrow();
    }

    [Fact]
    public void V01_MixedDocument_ParsesWithoutError()
    {
        var huml = LoadFixture("v0.1", "mixed.huml");
        var act = () => HumlSerializer.Parse(huml, V01Options);
        act.Should().NotThrow();
    }

    // ── Task 1: Typed sub-section round-trips ─────────────────────────────────

    [Fact]
    public void RoundTrip_IntegersPoco_PreservesValues()
    {
        var original = new IntegersPoco
        {
            BarPositive   = 1234567,
            BazNegative   = -987654,
            QuxZero       = 0,
            CorgeHex      = 3735928559L,    // 0xDEADBEEF
            GraultOctal   = 511,            // 0o777
            GarplyBinary  = 85,             // 0b1010101
            QuuxUnderscore = 1000000,       // 1_000_000
            WaldoLarge    = 9223372036854775807L, // Int64.MaxValue
        };
        var huml = HumlSerializer.Serialize(original);
        var result = HumlSerializer.Deserialize<IntegersPoco>(huml, HumlOptions.AutoDetect);
        result.BarPositive.Should().Be(1234567);
        result.BazNegative.Should().Be(-987654);
        result.QuxZero.Should().Be(0);
        result.CorgeHex.Should().Be(3735928559L);
        result.GraultOctal.Should().Be(511);
        result.GarplyBinary.Should().Be(85);
        result.QuuxUnderscore.Should().Be(1000000);
        result.WaldoLarge.Should().Be(9223372036854775807L);
    }

    [Fact]
    public void RoundTrip_FloatsPoco_PreservesValues()
    {
        var original = new FloatsPoco
        {
            BarSimple      = 123.456,
            BazNeg         = -78.9,
            CorgeZero      = 0.0,
            QuxScientific  = 12300000000.0,
            QuuxSciNeg     = -4.56e-7,
            GarplyLargeExp = 6.022e+23,
            GraultPrecision = 0.123456789,
        };
        var huml = HumlSerializer.Serialize(original);
        var result = HumlSerializer.Deserialize<FloatsPoco>(huml, HumlOptions.AutoDetect);
        result.BarSimple.Should().Be(123.456);
        result.BazNeg.Should().Be(-78.9);
        result.CorgeZero.Should().Be(0.0);
        result.QuxScientific.Should().Be(12300000000.0);
        result.QuuxSciNeg.Should().Be(-4.56e-7);
        result.GarplyLargeExp.Should().BeApproximately(6.022e+23, 1e+17);
        result.GraultPrecision.Should().Be(0.123456789);
    }

    [Fact]
    public void RoundTrip_StringsPoco_PreservesValues()
    {
        var original = new StringsPoco
        {
            BarEmpty       = "",
            BazSpaces      = "   spaces   ",
            CorgeUnicode   = "Unicode: \u03b1\u03b2\u03b3\u03b4\u03b5 \u4e2d\u6587 \U0001F680",
            GarplyLong     = "This is a very long string that contains many words and might test the parser's ability to handle extended content without issues",
            GraultNewlines = "Line1\nLine2\tTabbed",
            QuuxPath       = @"C:\path\to\file.txt",
            QuxEscaped     = "Hello \"World\" with 'quotes'",
        };
        var huml = HumlSerializer.Serialize(original);
        var result = HumlSerializer.Deserialize<StringsPoco>(huml, HumlOptions.AutoDetect);
        result.BarEmpty.Should().Be("");
        result.BazSpaces.Should().Be("   spaces   ");
        result.CorgeUnicode.Should().Be("Unicode: \u03b1\u03b2\u03b3\u03b4\u03b5 \u4e2d\u6587 \U0001F680");
        result.GarplyLong.Should().Be("This is a very long string that contains many words and might test the parser's ability to handle extended content without issues");
        result.GraultNewlines.Should().Be("Line1\nLine2\tTabbed");
        result.QuuxPath.Should().Be(@"C:\path\to\file.txt");
        result.QuxEscaped.Should().Be("Hello \"World\" with 'quotes'");
    }

    [Fact]
    public void RoundTrip_BooleansPoco_PreservesValues()
    {
        var original = new BooleansPoco
        {
            BarTrue       = true,
            BazFalse      = false,
            QuxTRUE       = true,
            CorgeTrueCase = true,
            QuuxFALSE     = false,
            GraultFalseCase = false,
        };
        var huml = HumlSerializer.Serialize(original);
        var result = HumlSerializer.Deserialize<BooleansPoco>(huml, HumlOptions.AutoDetect);
        result.BarTrue.Should().BeTrue();
        result.BazFalse.Should().BeFalse();
        result.QuxTRUE.Should().BeTrue();
        result.CorgeTrueCase.Should().BeTrue();
        result.QuuxFALSE.Should().BeFalse();
        result.GraultFalseCase.Should().BeFalse();
    }

    [Fact]
    public void RoundTrip_NullsPoco_PreservesValues()
    {
        var original = new NullsPoco
        {
            BarNull = null,
            BazNULL = null,
            QuxNull = null,
        };
        var huml = HumlSerializer.Serialize(original);
        var result = HumlSerializer.Deserialize<NullsPoco>(huml, HumlOptions.AutoDetect);
        result.BarNull.Should().BeNull();
        result.BazNULL.Should().BeNull();
        result.QuxNull.Should().BeNull();
    }

    [Fact]
    public void RoundTrip_BasicScalars_PreservesAllTypes()
    {
        var original = new BasicScalarsPoco
        {
            FooString  = "bar_value",
            BarString  = "baz with spaces",
            BazInt     = 42,
            QuxFloat   = 3.14159,
            QuuxBool   = true,
            CorgeBool  = false,
            GraultNull = null,
        };
        var huml = HumlSerializer.Serialize(original);
        var result = HumlSerializer.Deserialize<BasicScalarsPoco>(huml, HumlOptions.AutoDetect);
        result.FooString.Should().Be("bar_value");
        result.BarString.Should().Be("baz with spaces");
        result.BazInt.Should().Be(42);
        result.QuxFloat.Should().Be(3.14159);
        result.QuuxBool.Should().BeTrue();
        result.CorgeBool.Should().BeFalse();
        result.GraultNull.Should().BeNull();
    }

    [Fact]
    public void RoundTrip_NestedDict_PreservesThreeLevelNesting()
    {
        var original = new FooDictPoco
        {
            BarKey = "bar_value",
            BazKey = 789,
            QuxNested = new QuxNestedPoco
            {
                CorgeSub = true,
                QuuxSub  = "quux_value",
                GraultDeep = new GraultDeepPoco
                {
                    GarplyDeeper = "deepest_value",
                    WaldoNumbers = new List<int> { 1, 2, 3, 4 },
                },
            },
        };
        var huml = HumlSerializer.Serialize(original);
        var result = HumlSerializer.Deserialize<FooDictPoco>(huml, HumlOptions.AutoDetect);
        result.BarKey.Should().Be("bar_value");
        result.BazKey.Should().Be(789);
        result.QuxNested.Should().NotBeNull();
        result.QuxNested!.CorgeSub.Should().BeTrue();
        result.QuxNested.QuuxSub.Should().Be("quux_value");
        result.QuxNested.GraultDeep.Should().NotBeNull();
        result.QuxNested.GraultDeep!.GarplyDeeper.Should().Be("deepest_value");
        result.QuxNested.GraultDeep.WaldoNumbers.Should().Equal(1, 2, 3, 4);
    }

    [Fact]
    public void RoundTrip_EmptyCollections_PreservesEmptyListAndDict()
    {
        var original = new EmptyCollectionsPoco
        {
            EmptyList = new List<string>(),
            EmptyDict = new Dictionary<string, string>(),
        };
        var huml = HumlSerializer.Serialize(original);
        var result = HumlSerializer.Deserialize<EmptyCollectionsPoco>(huml, HumlOptions.AutoDetect);
        result.EmptyList.Should().NotBeNull();
        result.EmptyList!.Should().BeEmpty();
        result.EmptyDict.Should().NotBeNull();
        result.EmptyDict!.Should().BeEmpty();
    }

    [Fact]
    public void RoundTrip_SpecialKeys_PreservesAllKeys()
    {
        var original = new SpecialKeysWrapper
        {
            Keys = new Dictionary<string, string>
            {
                ["key with spaces"]      = "spaced_value",
                ["key-with-dashes"]      = "dashed_value",
                ["key.with.dots"]        = "dotted_value",
                ["key_with_underscores"] = "underscore_value",
                ["quoted-key"]           = "quoted_value",
                ["123numeric_start"]     = "numeric_key",
                ["special!@#$%"]         = "special_chars",
            },
        };
        var huml = HumlSerializer.Serialize(original);
        var result = HumlSerializer.Deserialize<SpecialKeysWrapper>(huml, HumlOptions.AutoDetect);
        result.Keys.Should().NotBeNull();
        result.Keys!.Should().ContainKey("key with spaces").WhoseValue.Should().Be("spaced_value");
        result.Keys.Should().ContainKey("key-with-dashes").WhoseValue.Should().Be("dashed_value");
        result.Keys.Should().ContainKey("key.with.dots").WhoseValue.Should().Be("dotted_value");
        result.Keys.Should().ContainKey("key_with_underscores").WhoseValue.Should().Be("underscore_value");
        result.Keys.Should().ContainKey("quoted-key").WhoseValue.Should().Be("quoted_value");
        result.Keys.Should().ContainKey("123numeric_start").WhoseValue.Should().Be("numeric_key");
        result.Keys.Should().ContainKey("special!@#$%").WhoseValue.Should().Be("special_chars");
    }

    [Fact]
    public void RoundTrip_EdgeCaseKeys_PreservesEmptyAndKeywordKeys()
    {
        var original = new EdgeCaseKeysWrapper
        {
            Keys = new Dictionary<string, string>
            {
                [""]      = "empty_key",
                [" "]     = "space_key",
                ["true"]  = "boolean_string_key",
                ["null"]  = "null_string_key",
                ["123"]   = "numeric_string_key",
            },
        };
        var huml = HumlSerializer.Serialize(original);
        var result = HumlSerializer.Deserialize<EdgeCaseKeysWrapper>(huml, HumlOptions.AutoDetect);
        result.Keys.Should().NotBeNull();
        result.Keys!.Should().ContainKey("").WhoseValue.Should().Be("empty_key");
        result.Keys.Should().ContainKey(" ").WhoseValue.Should().Be("space_key");
        result.Keys.Should().ContainKey("true").WhoseValue.Should().Be("boolean_string_key");
        result.Keys.Should().ContainKey("null").WhoseValue.Should().Be("null_string_key");
        result.Keys.Should().ContainKey("123").WhoseValue.Should().Be("numeric_string_key");
    }

    // ── Task 2: Inline serialisation value-equality round-trips ──────────────

    [Fact]
    public void InlineRoundTrip_ScalarIntList_PreservesValues()
    {
        var opts = new HumlOptions { CollectionFormat = CollectionFormat.Inline };
        var original = new InlineIntListPoco { Tags = new List<int> { 1, 2, 3, 4, 5 } };
        var huml = HumlSerializer.Serialize(original, opts);
        var result = HumlSerializer.Deserialize<InlineIntListPoco>(huml, HumlOptions.AutoDetect);
        result.Tags.Should().Equal(1, 2, 3, 4, 5);
    }

    [Fact]
    public void InlineRoundTrip_ScalarStringList_PreservesValues()
    {
        var opts = new HumlOptions { CollectionFormat = CollectionFormat.Inline };
        var original = new InlineStringListPoco { Items = new List<string> { "alpha", "beta", "gamma" } };
        var huml = HumlSerializer.Serialize(original, opts);
        var result = HumlSerializer.Deserialize<InlineStringListPoco>(huml, HumlOptions.AutoDetect);
        result.Items.Should().Equal("alpha", "beta", "gamma");
    }

    [Fact]
    public void InlineRoundTrip_ScalarBoolList_PreservesValues()
    {
        var opts = new HumlOptions { CollectionFormat = CollectionFormat.Inline };
        var original = new InlineBoolListPoco { Flags = new List<bool> { true, false, true } };
        var huml = HumlSerializer.Serialize(original, opts);
        var result = HumlSerializer.Deserialize<InlineBoolListPoco>(huml, HumlOptions.AutoDetect);
        result.Flags.Should().Equal(true, false, true);
    }

    [Fact]
    public void InlineRoundTrip_ScalarDict_PreservesValues()
    {
        var opts = new HumlOptions { CollectionFormat = CollectionFormat.Inline };
        var original = new InlineDictCountsPoco
        {
            Counts = new Dictionary<string, int> { ["a"] = 1, ["b"] = 2, ["c"] = 3 },
        };
        var huml = HumlSerializer.Serialize(original, opts);
        var result = HumlSerializer.Deserialize<InlineDictCountsPoco>(huml, HumlOptions.AutoDetect);
        result.Counts.Should().ContainKey("a").WhoseValue.Should().Be(1);
        result.Counts.Should().ContainKey("b").WhoseValue.Should().Be(2);
        result.Counts.Should().ContainKey("c").WhoseValue.Should().Be(3);
    }

    [Fact]
    public void InlineRoundTrip_AttributeOverride_PreservesValues()
    {
        // [HumlProperty(Inline = InlineMode.Inline)] forces inline even with default (multiline) options
        var original = new InlineAttrPoco { Tags = new List<int> { 10, 20, 30 } };
        var huml = HumlSerializer.Serialize(original); // default multiline, but attribute forces inline
        var result = HumlSerializer.Deserialize<InlineAttrPoco>(huml, HumlOptions.AutoDetect);
        result.Tags.Should().Equal(10, 20, 30);
    }

    [Fact]
    public void InlineRoundTrip_MixedScalarList_ParsesSuccessfully()
    {
        var opts = new HumlOptions { CollectionFormat = CollectionFormat.Inline };
        var original = new InlineMixedStringListPoco { Items = new List<string> { "1", "mixed", "true" } };
        var huml = HumlSerializer.Serialize(original, opts);
        var act = () => HumlSerializer.Parse(huml, HumlOptions.AutoDetect);
        act.Should().NotThrow();
    }
}
