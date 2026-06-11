using AwesomeAssertions;
using Huml.Net.Exceptions;
using Huml.Net.Serialization;
using Huml.Net.Versioning;
using Xunit;

namespace Huml.Net.Tests.Serialization;

public class EnumSerializationTests
{
    // ── Test enums ────────────────────────────────────────────────────────────

    private enum Status { Active, Inactive, Pending }

    [Flags]
    private enum Permissions { None = 0, Read = 1, Write = 2, Execute = 4 }

    private enum Priority
    {
        [HumlEnumValue("low-priority")]  Low,
        [HumlEnumValue("high-priority")] High,
    }

    // ── Test POCOs ────────────────────────────────────────────────────────────

    private class StatusPoco        { public Status  State  { get; set; } }
    private class NullableEnumPoco  { public Status? State  { get; set; } }
    private class ListEnumPoco      { public List<Status> States { get; set; } = new(); }
    private class PriorityPoco      { public Priority Level { get; set; } }
    private class PermissionsPoco   { public Permissions Access { get; set; } }

    // ── Constructor: test isolation ───────────────────────────────────────────

    public EnumSerializationTests()
    {
        PropertyDescriptor.ClearCache();
        EnumNameCache.ClearCache();
    }

    // ── ENUM-SER-01: Serialize enum property emits quoted member name ─────────

    [Fact]
    public void Serialize_EnumProperty_EmitsQuotedMemberName()
    {
        var result = HumlSerializer.Serialize(new StatusPoco { State = Status.Active }, HumlOptions.LatestSupported);
        result.Should().Contain("State: \"Active\"\n");
    }

    // ── ENUM-SER-02: [HumlEnumValue] overrides the emitted name ──────────────

    [Fact]
    public void Serialize_EnumWithHumlEnumValue_EmitsOverrideName()
    {
        var result = HumlSerializer.Serialize(new PriorityPoco { Level = Priority.Low }, HumlOptions.LatestSupported);
        result.Should().Contain("Level: \"low-priority\"\n");
    }

    // ── ENUM-SER-03: Naming policy transforms member name ────────────────────

    [Fact]
    public void Serialize_EnumWithNamingPolicy_TransformsMemberName()
    {
        var options = new HumlOptions { PropertyNamingPolicy = HumlNamingPolicy.KebabCase };
        var result = HumlSerializer.Serialize(new StatusPoco { State = Status.Active }, options);
        // Status.Active → KebabCase → "active" (single word, lowercase)
        result.Should().Contain("\"active\"");
    }

    // ── ENUM-SER-04: [HumlEnumValue] wins over naming policy ─────────────────

    [Fact]
    public void Serialize_EnumWithHumlEnumValueAndPolicy_AttributeWins()
    {
        var options = new HumlOptions { PropertyNamingPolicy = HumlNamingPolicy.KebabCase };
        var result = HumlSerializer.Serialize(new PriorityPoco { Level = Priority.High }, options);
        // [HumlEnumValue("high-priority")] wins over KebabCase transform of "High" → "high"
        result.Should().Contain("\"high-priority\"");
    }

    // ── ENUM-SER-05: Undefined numeric enum value throws HumlSerializeException ─

    [Fact]
    public void Serialize_UndefinedNumericEnumValue_ThrowsHumlSerializeException()
    {
        var poco = new StatusPoco { State = (Status)99 };
        var act = () => HumlSerializer.Serialize(poco, HumlOptions.LatestSupported);
        act.Should().Throw<HumlSerializeException>();
    }

    // ── ENUM-SER-06: [Flags] combination throws HumlSerializeException ────────

    [Fact]
    public void Serialize_FlagsEnumCombination_ThrowsHumlSerializeException()
    {
        var poco = new PermissionsPoco { Access = Permissions.Read | Permissions.Write };
        var act = () => HumlSerializer.Serialize(poco, HumlOptions.LatestSupported);
        act.Should().Throw<HumlSerializeException>();
    }

    // ── ENUM-SER-07: Single named [Flags] member serialises without throwing ────

    [Fact]
    public void Serialize_SingleNamedFlagsEnumMember_EmitsNameWithoutThrowing()
    {
        var poco = new PermissionsPoco { Access = Permissions.Read };
        var act = () => HumlSerializer.Serialize(poco, HumlOptions.LatestSupported);
        act.Should().NotThrow();
        var result = HumlSerializer.Serialize(poco, HumlOptions.LatestSupported);
        result.Should().Contain("\"Read\"");
    }

    // ── ENUM-DES-01: Deserialize exact member name → correct enum value ───────

    [Fact]
    public void Deserialize_ExactMemberName_ReturnsCorrectEnumValue()
    {
        const string huml = """
            %HUML v0.2.0
            State: "Active"
            """;
        var result = HumlSerializer.Deserialize<StatusPoco>(huml, HumlOptions.LatestSupported);
        result.State.Should().Be(Status.Active);
    }

    // ── ENUM-DES-02: Deserialize case-insensitive member name ─────────────────

    [Fact]
    public void Deserialize_CaseInsensitiveMemberName_ReturnsCorrectEnumValue()
    {
        const string huml = """
            %HUML v0.2.0
            State: "active"
            """;
        var result = HumlSerializer.Deserialize<StatusPoco>(huml, HumlOptions.LatestSupported);
        result.State.Should().Be(Status.Active);
    }

    // ── ENUM-DES-03: Deserialize [HumlEnumValue] override name ───────────────

    [Fact]
    public void Deserialize_HumlEnumValueOverrideName_ReturnsCorrectEnumValue()
    {
        const string huml = """
            %HUML v0.2.0
            Level: "low-priority"
            """;
        var result = HumlSerializer.Deserialize<PriorityPoco>(huml, HumlOptions.LatestSupported);
        result.Level.Should().Be(Priority.Low);
    }

    // ── ENUM-DES-04: Deserialize unknown string throws HumlDeserializeException ─

    [Fact]
    public void Deserialize_UnknownEnumString_ThrowsHumlDeserializeException()
    {
        const string huml = """
            %HUML v0.2.0
            State: "NotARealStatus"
            """;
        var act = () => HumlSerializer.Deserialize<StatusPoco>(huml, HumlOptions.LatestSupported);
        act.Should().Throw<HumlDeserializeException>();
    }

    // ── ENUM-DES-05: Deserialize integer scalar → enum via numeric coercion ───

    [Fact]
    public void Deserialize_IntegerScalar_ReturnsEnumValueViaNumericCoercion()
    {
        const string huml = """
            %HUML v0.2.0
            State: 1
            """;
        var result = HumlSerializer.Deserialize<StatusPoco>(huml, HumlOptions.LatestSupported);
        result.State.Should().Be(Status.Inactive);
    }

    // ── ENUM-DES-06: Deserialize null → Nullable<MyEnum> returns null ─────────

    [Fact]
    public void Deserialize_NullIntoNullableEnum_ReturnsNull()
    {
        const string huml = """
            %HUML v0.2.0
            State: null
            """;
        var result = HumlSerializer.Deserialize<NullableEnumPoco>(huml, HumlOptions.LatestSupported);
        result.State.Should().BeNull();
    }

    // ── ENUM-DES-07: Deserialize null → non-nullable enum throws ─────────────

    [Fact]
    public void Deserialize_NullIntoNonNullableEnum_ThrowsHumlDeserializeException()
    {
        const string huml = """
            %HUML v0.2.0
            State: null
            """;
        var act = () => HumlSerializer.Deserialize<StatusPoco>(huml, HumlOptions.LatestSupported);
        act.Should().Throw<HumlDeserializeException>();
    }

    // ── ENUM-DES-08: Deserialize with naming policy (policy-transformed HUML key) ─

    [Fact]
    public void Deserialize_WithNamingPolicy_PolicyTransformedNameRoundTrips()
    {
        const string huml = """
            %HUML v0.2.0
            State: "active"
            """;
        var options = new HumlOptions { PropertyNamingPolicy = HumlNamingPolicy.KebabCase };
        var result = HumlSerializer.Deserialize<StatusPoco>(huml, options);
        // "active" matches Status.Active after KebabCase policy converts "Active" → "active"
        result.State.Should().Be(Status.Active);
    }

    // ── ENUM-DES-09: Out-of-range integer scalar returns undefined enum value ────

    [Fact]
    public void Deserialize_OutOfRangeIntegerScalar_ReturnsUndefinedEnumValue()
    {
        const string huml = """
            %HUML v0.2.0
            State: 99
            """;
        var result = HumlSerializer.Deserialize<StatusPoco>(huml, HumlOptions.LatestSupported);
        // Enum.ToObject silently returns an undefined value; this documents the contract.
        result.State.Should().Be((Status)99);
    }

    // ── ENUM-RT-01: Round-trip preserves value equality (no policy) ───────────

    [Fact]
    public void RoundTrip_EnumProperty_PreservesValueEquality()
    {
        var original = new StatusPoco { State = Status.Pending };
        var huml = HumlSerializer.Serialize(original, HumlOptions.LatestSupported);
        var result = HumlSerializer.Deserialize<StatusPoco>(huml, HumlOptions.LatestSupported);
        result.State.Should().Be(original.State);
    }

    // ── ENUM-RT-02: Round-trip with KebabCase policy ──────────────────────────

    [Fact]
    public void RoundTrip_EnumWithKebabCasePolicy_PreservesValueEquality()
    {
        var options = new HumlOptions { PropertyNamingPolicy = HumlNamingPolicy.KebabCase };
        var original = new StatusPoco { State = Status.Inactive };
        var huml = HumlSerializer.Serialize(original, options);
        var result = HumlSerializer.Deserialize<StatusPoco>(huml, options);
        result.State.Should().Be(original.State);
    }

    // ── ENUM-RT-03: Round-trip with [HumlEnumValue] ───────────────────────────

    [Fact]
    public void RoundTrip_EnumWithHumlEnumValue_PreservesValueEquality()
    {
        var original = new PriorityPoco { Level = Priority.High };
        var huml = HumlSerializer.Serialize(original, HumlOptions.LatestSupported);
        var result = HumlSerializer.Deserialize<PriorityPoco>(huml, HumlOptions.LatestSupported);
        result.Level.Should().Be(original.Level);
    }

    // ── ENUM-RT-04: Round-trip List<MyEnum> ──────────────────────────────────

    [Fact]
    public void RoundTrip_ListOfEnum_PreservesAllElements()
    {
        var original = new ListEnumPoco { States = new List<Status> { Status.Active, Status.Pending, Status.Inactive } };
        var huml = HumlSerializer.Serialize(original, HumlOptions.LatestSupported);
        var result = HumlSerializer.Deserialize<ListEnumPoco>(huml, HumlOptions.LatestSupported);
        result.States.Should().Equal(original.States);
    }

    // ── ENUM-RT-05: Round-trip nullable enum with null value ──────────────────

    [Fact]
    public void RoundTrip_NullableEnumWithNull_PreservesNull()
    {
        var original = new NullableEnumPoco { State = null };
        var huml = HumlSerializer.Serialize(original, HumlOptions.LatestSupported);
        var result = HumlSerializer.Deserialize<NullableEnumPoco>(huml, HumlOptions.LatestSupported);
        result.State.Should().BeNull();
    }
}
